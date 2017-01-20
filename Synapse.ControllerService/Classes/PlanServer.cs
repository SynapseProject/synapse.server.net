using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Synapse.Core;
using Synapse.Core.Utilities;

namespace Synapse.Services
{
    public class PlanServer
    {
        public static int PlanInstanceId = 0;

        public PlanServer() { }

        NodeServiceHttpApiClient _nodeClient = new NodeServiceHttpApiClient( SynapseControllerService.Config.NodeServiceUrl );

        public int StartPlan(string planName, bool dryRun = false)
        {
            string planFile = $"{SynapseControllerConfig.CurrentPath}\\Plans\\{planName}.yaml";
            Plan plan = YamlHelpers.DeserializeFile<Plan>( planFile );
            int pIId = PlanInstanceId++;
            _nodeClient.StartPlan( pIId, dryRun, plan );
            return pIId;
        }

        public void CancelPlan(long instanceId)
        {
            _nodeClient.CancelPlanAsync( instanceId );
        }

        public Plan GetPlanStatus(string planName, long id)
        {
            string planFile = $"{SynapseControllerConfig.CurrentPath}\\Plans\\{planName}.yaml";
            return YamlHelpers.DeserializeFile<Plan>( planFile );
        }


        public void WriteStatus(string msg)
        {
            SynapseControllerService.Logger.Info( msg );
        }
    }
}