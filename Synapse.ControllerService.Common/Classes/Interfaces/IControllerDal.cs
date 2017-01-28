using Synapse.Core;

namespace Synapse.Services.Controller.Dal
{
    public interface IControllerDal
    {
        Plan GetPlan(string planUniqueName);

        Plan GetPlanStatus(string planUniqueName, long planInstanceId);

        void UpdatePlanStatus(Plan plan);

        void UpdatePlanStatus(PlanUpdateItem item);

        void UpdatePlanActionStatus(string planUniqueName, long planInstanceId, ActionItem actionItem);

        void UpdatePlanActionStatus(ActionUpdateItem item);
    }
}