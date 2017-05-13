using System;
using System.Collections.Generic;
using System.IO;

using Synapse.Core;
using Synapse.Core.Utilities;
using Synapse.Services.Controller.Dal;

namespace Synapse.Services
{
    public class PlanServer
    {
        IControllerDal _dal = null;

        static bool once = false;

        public PlanServer()
        {
            if( SynapseServer.Config.Service.RoleIsController && _dal == null )
                try
                {
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
                    SynapseServer.Logger.Fatal( "Failed to load Dal.", ex );
                    throw;
                }
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
            string nodeUrlSchemeHostPort = null, Uri referrer = null)
        {
            _dal.HasAccessOrException( securityContext, planUniqueName );

            Plan plan = _dal.CreatePlanInstance( planUniqueName );
            plan.StartInfo = new PlanStartInfo() { RequestUser = securityContext, RequestNumber = requestNumber };

            if( SynapseServer.Config.Controller.SignPlan )
            {
                SynapseServer.Logger.Debug( $"Signing Plan {plan.Name}/{plan.InstanceId}." );

                if( !File.Exists( SynapseServer.Config.Signature.KeyUri ) )
                    throw new FileNotFoundException( SynapseServer.Config.Signature.KeyUri );

                plan.Sign( SynapseServer.Config.Signature.KeyContainerName, SynapseServer.Config.Signature.KeyUri, SynapseServer.Config.Signature.CspProviderFlags );
                //plan.Name += "foo";  //testing: intentionally crash the sig
            }

            GetNodeClientInstance( nodeUrlSchemeHostPort, referrer ).StartPlan( plan, plan.InstanceId, dryRun, dynamicParameters, postDynamicParameters );

            return plan.InstanceId;
        }

        public void CancelPlan(long instanceId, string nodeUrlSchemeHostPort = null, Uri referrer = null)
        {
            GetNodeClientInstance( nodeUrlSchemeHostPort, referrer ).CancelPlanAsync( instanceId );
        }

        public Plan GetPlanStatus(string planUniqueName, long planInstanceId)
        {
            return _dal.GetPlanStatus( planUniqueName, planInstanceId );
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
            Plan plan = _dal.GetPlanStatus( planUniqueName, planInstanceId );
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

        NodeServiceHttpApiClient GetNodeClientInstance(string nodeUrlSchemeHostPort, Uri referrer)
        {
            if( string.IsNullOrWhiteSpace( nodeUrlSchemeHostPort ) )
                nodeUrlSchemeHostPort = SynapseServer.Config.Controller.NodeUrl;
            else
                nodeUrlSchemeHostPort = $"{nodeUrlSchemeHostPort}/synapse/node";

            SynapseServer.Logger.Info( $"nodeClient.Headers.Referrer: {referrer.AbsoluteUri}" );

            NodeServiceHttpApiClient nodeClient = new NodeServiceHttpApiClient( nodeUrlSchemeHostPort );
            nodeClient.Headers.Referrer = referrer;
            return nodeClient;
        }
    }
}