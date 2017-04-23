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
        NodeServiceHttpApiClient _nodeClient = new NodeServiceHttpApiClient( SynapseServer.Config.Controller.NodeUrl );
        IControllerDal _dal = null;

        static bool once = false;

        public PlanServer()
        {
            if( SynapseServer.Config.ServerIsController && _dal == null )
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

        public long StartPlan(string securityContext, string planUniqueName, bool dryRun = false, string requestNumber = null, Dictionary<string, string> dynamicParameters = null, bool postDynamicParameters = false)
        {
            _dal.HasAccessOrException( securityContext, planUniqueName );

            Plan plan = _dal.CreatePlanInstance( planUniqueName );
            plan.StartInfo = new PlanStartInfo() { RequestUser = securityContext, RequestNumber = requestNumber };

            if( SynapseServer.Config.Controller.SignPlan )
            {
                SynapseServer.Logger.Debug( $"Signing Plan {plan.Name}/{plan.InstanceId}." );

                if( !File.Exists( SynapseServer.Config.SignatureKeyFile ) )
                    throw new FileNotFoundException( SynapseServer.Config.SignatureKeyFile );

                plan.Sign( SynapseServer.Config.SignatureKeyContainerName, SynapseServer.Config.SignatureKeyFile, SynapseServer.Config.SignatureCspProviderFlags );
                //plan.Name += "foo";  //testing: intentionally crash the sig
            }

            _nodeClient.StartPlan( plan, plan.InstanceId, dryRun, dynamicParameters, postDynamicParameters );

            return plan.InstanceId;
        }

        public void CancelPlan(long instanceId)
        {
            _nodeClient.CancelPlanAsync( instanceId );
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

        public object GetPlanElements(string planUniqueName, long planInstanceId, string elementPath)
        {
            Plan plan = _dal.GetPlanStatus( planUniqueName, planInstanceId );
            object result = YamlHelpers.SelectElements( plan, new string[] { elementPath } );
            Newtonsoft.Json.Linq.JObject json = Newtonsoft.Json.Linq.JObject.Parse( result.ToString() );
            return json;
        }
    }
}