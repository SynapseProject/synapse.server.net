using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;

using Synapse.Core;
using Synapse.Core.Utilities;
using Synapse.Services.Controller.Dal;
using Synapse.Common.WebApi;

namespace Synapse.Services
{
    public class PlanServer
    {
		static IControllerDal _dal = null;

        static bool once = false;

        public PlanServer()
        {
            if( SynapseServer.Config.Service.IsRoleController && _dal == null )
                try
                {
					SynapseServer.Logger.Debug( $"Loading Dal: {SynapseServer.Config.Controller.Dal.Type}." );

					_dal = AssemblyLoader.Load<IControllerDal>(
                        SynapseServer.Config.Controller.Dal.Type, SynapseServer.Config.Controller.Dal.DefaultType );
                    Dictionary<string, string> props = _dal.Configure( SynapseServer.Config.Controller.Dal );

                    if( !once )
                    {
                        if( props != null )
                            foreach( string key in props.Keys )
                                SynapseServer.Logger.Info( $"{key}: {props[key]}" );
                        once = true;
                    }
                }
                catch( Exception ex )
                {
                    SynapseServer.Logger.Fatal( $"Failed to load Dal: {SynapseServer.Config.Controller.Dal.Type}.", ex );
                    throw;
                }
        }


        public Plan GetPlan(string planUniqueName)
        {
            return _dal.GetPlan( planUniqueName );
        }

        public IEnumerable<string> GetPlanList(string filter = null, bool isRegexFilter = true)
        {
            return _dal.GetPlanList( filter, isRegexFilter );
        }

        public IEnumerable<long> GetPlanInstanceIdList(string planUniqueName)
        {
            return _dal.GetPlanInstanceIdList( planUniqueName );
        }

        public long StartPlan(string securityContext, string planUniqueName, bool dryRun = false,
            string requestNumber = null, Dictionary<string, string> dynamicParameters = null, bool postDynamicParameters = false,
            string nodeRootUrl = null, Uri referrer = null, AuthenticationHeaderValue authHeader = null)
        {
            _dal.HasAccessOrException( securityContext, planUniqueName );

            Plan plan = _dal.CreatePlanInstance( planUniqueName );
            plan.StartInfo = new PlanStartInfo() { RequestUser = securityContext, RequestNumber = requestNumber };

            //record "New" status
            Plan initResultPlan = new Plan()
            {
                Name = plan.Name,
                UniqueName = plan.UniqueName,
                InstanceId = plan.InstanceId,
                StartInfo = plan.StartInfo,
                Result = new ExecuteResult()
                {
                    Status = StatusType.New,
                    BranchStatus = StatusType.New,
                    Message = $"New Instance of Plan [{plan.UniqueName}/{plan.InstanceId}]."
                }
            };
            _dal.UpdatePlanStatus( initResultPlan );

            //sign the plan
            if ( SynapseServer.Config.Controller.SignPlan )
            {
                SynapseServer.Logger.Debug( $"Signing Plan [{plan.Name}/{plan.InstanceId}]." );

                if( !File.Exists( SynapseServer.Config.Signature.KeyUri ) )
                    throw new FileNotFoundException( SynapseServer.Config.Signature.KeyUri );

                plan.Sign( SynapseServer.Config.Signature.KeyContainerName, SynapseServer.Config.Signature.KeyUri, SynapseServer.Config.Signature.CspProviderFlags );
                //plan.Name += "foo";  //testing: intentionally crash the sig
            }

            //send plan to Node to start the work
            NodeServiceHttpApiClient nodeClient = GetNodeClientInstance( nodeRootUrl, referrer, authHeader );
            nodeClient.StartPlan( plan, plan.InstanceId, dryRun, dynamicParameters, postDynamicParameters );

            return plan.InstanceId;
        }

        public void CancelPlan(long instanceId, string nodeRootUrl = null, Uri referrer = null, AuthenticationHeaderValue authHeader = null)
        {
            GetNodeClientInstance( nodeRootUrl, referrer, authHeader ).CancelPlanAsync( instanceId );
        }

        //eat the error here, always return something valid.
        // - possible reasons for Plan failing to fetch: history record has been purged, other error?
        public Plan GetPlanStatus(string planUniqueName, long planInstanceId)
        {
            try
            {
                return _dal.GetPlanStatus( planUniqueName, planInstanceId );
            }
            catch
            {
                return new Plan()
                {
                    Name = planUniqueName,
                    UniqueName = planUniqueName,
                    InstanceId = planInstanceId,
                    Result = new ExecuteResult()
                    {
                        Status = StatusType.None,
                        BranchStatus = StatusType.None,
                        Message = $"Could not fetch Plan [{planUniqueName}/{planInstanceId}]."
                    }
                };
            }
        }


        public void UpdatePlanStatus(Plan plan)
        {
            _dal.UpdatePlanStatus( plan );
        }

        public void UpdatePlanActionStatus(string planUniqueName, long planInstanceId, ActionItem actionItem)
        {
            _dal.UpdatePlanActionStatus( planUniqueName, planInstanceId, actionItem );
        }

        public object GetPlanElements(string planUniqueName, long planInstanceId, PlanElementParms elementParms)
        {
            Plan plan = GetPlanStatus( planUniqueName, planInstanceId );
            object result = YamlHelpers.SelectElements( plan, elementParms.ElementPaths );

            List<object> results = new List<object>();
            if( result is List<object> )
                result = (List<object>)result;
            else
                results.Add( result );

            for( int i = 0; i < results.Count; i++ )
                if( results[i] != null )
                    switch( elementParms.Type )
                    {
                        case SerializationType.Yaml:
                        {
                            string yaml = results[i] is Dictionary<object, object> ?
                                YamlHelpers.Serialize( results[i] ) : results[i].ToString();
                            try { results[i] = YamlHelpers.Deserialize( yaml ); }
                            catch { results[i] = yaml; }
                            break;
                        }
                        case SerializationType.Json:
                        {
                            string json = results[i] is Dictionary<object, object> ?
                                YamlHelpers.Serialize( results[i], serializeAsJson: true ) : results[i].ToString();
                            try { results[i] = Newtonsoft.Json.Linq.JObject.Parse( json ); }
                            catch { results[i] = json; }
                            break;
                        }
                        case SerializationType.Xml:
                        {
                            try
                            {
                                System.Xml.XmlDocument xml = new System.Xml.XmlDocument();
                                xml.LoadXml( results[i].ToString() );
                                results[i] = xml;
                            }
                            catch { }
                            break;
                        }
                        case SerializationType.Html:
                        case SerializationType.Unspecified:
                        {
                            //no-op
                            //results[i] = results[i].ToString();
                            break;
                        }
                    }

            if( results.Count == 1 )
                return results[0];
            else
                return results;
        }

        NodeServiceHttpApiClient GetNodeClientInstance(string nodeRootUrl, Uri referrer, AuthenticationHeaderValue authHeader)
        {
            if( string.IsNullOrWhiteSpace( nodeRootUrl ) )
                nodeRootUrl = SynapseServer.Config.Controller.NodeUrl;
            else
                nodeRootUrl = $"{nodeRootUrl}/synapse/node";

            SynapseServer.Logger.Info( $"nodeClient.Headers.Referrer: {referrer?.AbsoluteUri}" );

            NodeServiceHttpApiClient nodeClient = new NodeServiceHttpApiClient( nodeRootUrl );
            nodeClient.Headers.Referrer = referrer;
            if ( authHeader != null)
            {
                if ( authHeader.Scheme.ToLower() == "basic" )
                    nodeClient.Options.Authentication = new BasicAuthentication( authHeader );
            }
            return nodeClient;
        }
    }
}