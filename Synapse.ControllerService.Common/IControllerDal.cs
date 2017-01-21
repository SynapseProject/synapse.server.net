using Synapse.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synapse.ControllerService.Common
{
    public interface IControllerDal
    {
        Plan GetPlan(string planUniqueName);

        Plan GetPlanStatus(string planUniqueName, long planInstanceId);

        void UpdatePlanStatus(Plan plan);

        void UpdatePlanActionStatus(string planUniqueName, long planInstanceId, ActionItem actionItem);
    }
}
