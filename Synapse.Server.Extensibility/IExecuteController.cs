using System.Collections.Generic;
using System.Web.Http;
using Synapse.Core;

namespace Synapse.Services
{
    public interface IExecuteController
    {
        void CancelPlan(string planUniqueName, long planInstanceId, string nodeRootUrl = null);
        object GetPlanElements(string planUniqueName, long planInstanceId, string elementPath, SerializationType serializationType = SerializationType.Json);
        object GetPlanElements(string planUniqueName, long planInstanceId, [FromBody] PlanElementParms elementParms);
        IEnumerable<long> GetPlanInstanceIdList(string planUniqueName);
        IEnumerable<string> GetPlanList(string filter = null, bool isRegexFilter = true);
        Plan GetPlanStatus(string planUniqueName, long planInstanceId);
        string Hello();
        void SetStatus(string planUniqueName, long planInstanceId, [FromBody] string planString);
        void SetStatus(string planUniqueName, long planInstanceId, [FromBody] ActionItem actionItem);
        long StartPlan(string planUniqueName, bool dryRun = false, string requestNumber = null, string nodeRootUrl = null);
        long StartPlan([FromBody] StartPlanEnvelope planEnvelope, string planUniqueName, bool dryRun = false, string requestNumber = null, string nodeRootUrl = null);
        string WhoAmI();
    }
}