using System;
using System.Collections.Generic;

using Synapse.Core;

namespace Synapse.Services.Controller.Dal
{
    public interface IPlanHistoryWriter : IControllerDalConfig
    {
        IEnumerable<long> GetPlanInstanceIdList(string planUniqueName);

        Plan CreatePlanInstance(string planUniqueName);

        Plan GetPlanStatus(string planUniqueName, long planInstanceId);

        void UpdatePlanStatus(Plan plan);

        void UpdatePlanStatus(PlanUpdateItem item);

        void UpdatePlanActionStatus(string planUniqueName, long planInstanceId, ActionItem actionItem);

        void UpdatePlanActionStatus(ActionUpdateItem item);
    }
}