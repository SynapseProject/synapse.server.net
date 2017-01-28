using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Synapse.Core;
using Synapse.Core.Utilities;
using Synapse.Services.Controller.Dal;

namespace Synapse.Services
{
    public class PlanServer
    {
        public static int PlanInstanceId = 0;
        NodeServiceHttpApiClient _nodeClient = new NodeServiceHttpApiClient( SynapseControllerService.Config.NodeServiceUrl );
        IControllerDal _dal = null;

        public PlanServer()
        {
            LoadDal();
        }

        void LoadDal()
        {
            string defaultType = "Synapse.Controller.Dal.FileSystem:Synapse.Services.Controller.Dal.FileSystemDal";
            _dal = AssemblyLoader.Load<IControllerDal>( SynapseControllerService.Config.DalProvider, defaultType );
        }

        public int StartPlan(string planUniqueName, bool dryRun = false)
        {
            //string planFile = $"{SynapseControllerConfig.CurrentPath}\\Plans\\{planName}.yaml";
            //Plan plan = YamlHelpers.DeserializeFile<Plan>( planFile );

            Plan plan = _dal.GetPlan( planUniqueName );
            int pIId = PlanInstanceId++;
            _nodeClient.StartPlan( pIId, dryRun, plan );
            return pIId;
        }

        public void CancelPlan(long instanceId)
        {
            _nodeClient.CancelPlanAsync( instanceId );
        }

        public Plan GetPlanStatus(string planUniqueName, long planInstanceId)
        {
            //string planFile = $"{SynapseControllerConfig.CurrentPath}\\Plans\\{planName}.yaml";
            //return YamlHelpers.DeserializeFile<Plan>( planFile );

            return _dal.GetPlanStatus( planUniqueName, planInstanceId );
        }


        public void WriteStatus(string msg)
        {
            SynapseControllerService.Logger.Info( msg );
        }
    }
}