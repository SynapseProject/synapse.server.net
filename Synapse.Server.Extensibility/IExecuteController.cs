using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Web.Http;
using System.Web.Http.Routing;
using System.Net.Http.Headers;

using Synapse.Core;

namespace Synapse.Services
{
    public interface IExecuteController
    {
        string Hello();
        string WhoAmI();
        Plan GetPlan(string planUniqueName);
        IEnumerable<string> GetPlanList(string filter = null, bool isRegexFilter = true);
        IEnumerable<long> GetPlanInstanceIdList(string planUniqueName);

        long StartPlan(string planUniqueName, bool dryRun = false, string requestNumber = null, string nodeRootUrl = null);
        long StartPlan([FromBody] StartPlanEnvelope planEnvelope, string planUniqueName, bool dryRun = false, string requestNumber = null, string nodeRootUrl = null);
        object StartPlanSync(string planUniqueName, bool dryRun = false, string requestNumber = null,
            string path = "Actions[0]:Result:ExitData", SerializationType serializationType = SerializationType.Json,
            bool setContentType = true, int pollingIntervalSeconds = 1, int timeoutSeconds = 120, string nodeRootUrl = null);
        object StartPlanSync([FromBody]StartPlanEnvelope planEnvelope, string planUniqueName, bool dryRun = false, string requestNumber = null,
            string path = "Actions[0]:Result:ExitData", SerializationType serializationType = SerializationType.Json,
            bool setContentType = true, int pollingIntervalSeconds = 1, int timeoutSeconds = 120, string nodeRootUrl = null);

        Plan GetPlanStatus(string planUniqueName, long planInstanceId);
        void SetStatus(string planUniqueName, long planInstanceId, [FromBody] string planString);
        void SetStatus(string planUniqueName, long planInstanceId, [FromBody] ActionItem actionItem);
        void CancelPlan(string planUniqueName, long planInstanceId, string nodeRootUrl = null);
        object GetPlanElements(string planUniqueName, long planInstanceId, string elementPath,
            SerializationType serializationType = SerializationType.Json, bool setContentType = true);
        object GetPlanElements(string planUniqueName, long planInstanceId, [FromBody] PlanElementParms elementParms);

        UrlHelper CurrentUrl { get; set; }
        IPrincipal CurrentUser { get; set; }
        AuthenticationHeaderValue AuthenticationHeader { get; set; }
    }
}