using System.Collections.Generic;
using System.Web.Http;
using Synapse.Core;

namespace Synapse.Services
{
    public interface IExecuteController
    {
        void CancelPlan(string planUniqueName, long planInstanceId, string nodeUrlSchemeHostPort = null);
        IEnumerable<long> GetPlanInstanceIdList(string planUniqueName);
        IEnumerable<string> GetPlanList(string filter = null, bool isRegexFilter = true);
        Plan GetPlanStatus(string planUniqueName, long planInstanceId);
        string Hello();
        void SetStatus(string planUniqueName, long planInstanceId, [FromBody] string planString);
        void SetStatus(string planUniqueName, long planInstanceId, [FromBody] ActionItem actionItem);
        long StartPlan(string planUniqueName, bool dryRun = false, string requestNumber = null, string nodeUrlSchemeHostPort = null);
        long StartPlan([FromBody] StartPlanEnvelope planEnvelope, string planUniqueName, bool dryRun = false, string requestNumber = null, string nodeUrlSchemeHostPort = null);
        string WhoAmI();
    }
}