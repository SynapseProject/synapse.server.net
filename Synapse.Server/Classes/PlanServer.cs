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
        NodeServiceHttpApiClient _nodeClient = new NodeServiceHttpApiClient( SynapseServer.Config.Controller.NodeUrl );
        IControllerDal _dal = null;

        public PlanServer()
        {
            LoadDal();
        }

        void LoadDal()
        {
            if( SynapseServer.Config.ServerIsController )
            {
                string defaultType = "Synapse.Controller.Dal.FileSystem:Synapse.Services.Controller.Dal.FileSystemDal";
                _dal = AssemblyLoader.Load<IControllerDal>( SynapseServer.Config.Controller.Dal, defaultType );
            }
        }

        public IEnumerable<string> GetPlanList()
        {
            return _dal.GetPlanList();
        }

        public IEnumerable<long> GetPlanInstanceIdList(string planUniqueName)
        {
            return _dal.GetPlanInstanceIdList( planUniqueName );
        }

        public long StartPlan(string planUniqueName, bool dryRun = false)
        {
            Plan plan = _dal.CreatePlanInstance( planUniqueName );
            _nodeClient.StartPlan( plan.InstanceId, dryRun, plan );
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
    }
}