using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Synapse.Common.WebApi;
using Synapse.Core;
using Synapse.Core.Utilities;

namespace Synapse.Services
{
    public class ControllerServiceHttpApiClient : HttpApiClientBase
    {
        string _rootPath = "/synapse/execute";

        public ControllerServiceHttpApiClient(string baseUrl, string messageFormatType = "application/json") : base( baseUrl, messageFormatType )
        {
        }


        public string Hello() { return HelloAsync().Result; }

        public async Task<string> HelloAsync()
        {
            string requestUri = $"{_rootPath}/hello";
            return await GetAsync<string>( requestUri );
        }

        public string WhoAmI() { return WhoAmIAsync().Result; }

        public async Task<string> WhoAmIAsync()
        {
            string requestUri = $"{_rootPath}/hello/whoami";
            return await GetAsync<string>( requestUri );
        }


        public List<string> GetPlanList(string filter = null, bool isRegexFilter = true)
        {
            return GetPlanListAsync( filter, isRegexFilter ).Result;
        }

        public async Task<List<string>> GetPlanListAsync(string filter = null, bool isRegexFilter = true)
        {
            filter = !string.IsNullOrWhiteSpace( filter ) ? $"/?filter={filter}&isRegexFilter={isRegexFilter}" : null;
            string requestUri = $"{_rootPath}{filter}";
            return await GetAsync<List<string>>( requestUri );
        }


        public List<long> GetPlanInstanceIdList(string planName)
        {
            return GetPlanInstanceIdListAsync( planName ).Result;
        }

        public async Task<List<long>> GetPlanInstanceIdListAsync(string planName)
        {
            string requestUri = $"{_rootPath}/{planName}";
            return await GetAsync<List<long>>( requestUri );
        }


        #region async plan execution
        public long StartPlan(string planName, bool dryRun = false, string requestNumber = null,
            Dictionary<string, string> dynamicParameters = null, bool postDynamicParameters = false, string nodeRootUrl = null)
        {
            if( postDynamicParameters )
                return StartPlanAsyncAsPost( planName, dryRun, requestNumber, dynamicParameters, nodeRootUrl ).Result;
            else
                return StartPlanAsync( planName, dryRun, requestNumber, dynamicParameters, nodeRootUrl ).Result;
        }

        public async Task<long> StartPlanAsync(string planName, bool dryRun = false, string requestNumber = null,
            Dictionary<string, string> dynamicParameters = null, string nodeRootUrl = null)
        {
            requestNumber = !string.IsNullOrWhiteSpace( requestNumber ) ? $"&requestNumber={requestNumber}" : null;
            nodeRootUrl = !string.IsNullOrWhiteSpace( nodeRootUrl ) ? $"&nodeRootUrl={nodeRootUrl}" : null;
            string qs = $"?dryRun={dryRun}{requestNumber}{nodeRootUrl}{dynamicParameters?.ToQueryString( asPartialQueryString: true )}";
            string requestUri = $"{_rootPath}/{planName}/start/{qs}";
            return await GetAsync<long>( requestUri );
        }


        public async Task<long> StartPlanAsyncAsPost(string planName, bool dryRun = false, string requestNumber = null,
            Dictionary<string, string> dynamicParameters = null, string nodeRootUrl = null)
        {
            StartPlanEnvelope planEnvelope = new StartPlanEnvelope() { DynamicParameters = dynamicParameters };

            requestNumber = !string.IsNullOrWhiteSpace( requestNumber ) ? $"&requestNumber={requestNumber}" : null;
            nodeRootUrl = !string.IsNullOrWhiteSpace( nodeRootUrl ) ? $"&nodeRootUrl={nodeRootUrl}" : null;
            string qs = $"?dryRun={dryRun}{requestNumber}{nodeRootUrl}";
            string requestUri = $"{_rootPath}/{planName}/start/{qs}";
            return await PostAsync<StartPlanEnvelope, long>( planEnvelope, requestUri );
        }
        #endregion


        #region sync plan execution - Controller waits for -> ( Plan.Status >= Complete || Timeout )
        public object StartPlanWait(string planName, bool dryRun = false, string requestNumber = null,
            Dictionary<string, string> dynamicParameters = null, bool postDynamicParameters = false,
            string path = "Actions[0]:Result:ExitData",
            SerializationType serializationType = SerializationType.Json, bool setContentType = true,
            int pollingIntervalSeconds = 1, int timeoutSeconds = 120, string nodeRootUrl = null)
        {
            if( postDynamicParameters )
                return StartPlanWaitAsyncAsPost( planName, dryRun, requestNumber, dynamicParameters, path,
                    serializationType, setContentType, pollingIntervalSeconds, timeoutSeconds, nodeRootUrl ).Result;
            else
                return StartPlanWaitAsync( planName, dryRun, requestNumber, dynamicParameters, path,
                    serializationType, setContentType, pollingIntervalSeconds, timeoutSeconds, nodeRootUrl ).Result;
        }

        public async Task<object> StartPlanWaitAsync(string planName, bool dryRun = false, string requestNumber = null,
            Dictionary<string, string> dynamicParameters = null, string path = "Actions[0]:Result:ExitData",
            SerializationType serializationType = SerializationType.Json, bool setContentType = true,
            int pollingIntervalSeconds = 1, int timeoutSeconds = 120, string nodeRootUrl = null)
        {
            requestNumber = !string.IsNullOrWhiteSpace( requestNumber ) ? $"&requestNumber={requestNumber}" : null;
            nodeRootUrl = !string.IsNullOrWhiteSpace( nodeRootUrl ) ? $"&nodeRootUrl={nodeRootUrl}" : null;
            path = !string.IsNullOrWhiteSpace( path ) ? $"&path={path}" : null;
            string st = !string.IsNullOrWhiteSpace( path ) ? $"&serializationType={serializationType}" : null;
            string sct = !string.IsNullOrWhiteSpace( path ) ? $"&setContentType={setContentType}" : null;
            string pi = pollingIntervalSeconds > 1 ? $"&pollingIntervalSeconds={pollingIntervalSeconds}" : null;
            string to = timeoutSeconds > 0 && timeoutSeconds != 120 ? $"&timeoutSeconds={timeoutSeconds}" : null;
            string qs = $"?dryRun={dryRun}{requestNumber}{path}{st}{sct}{pi}{to}{nodeRootUrl}{dynamicParameters?.ToQueryString( asPartialQueryString: true )}";
            string requestUri = $"{_rootPath}/{planName}/start/sync/{qs}";
            return await GetAsync<object>( requestUri );
        }


        public async Task<object> StartPlanWaitAsyncAsPost(string planName, bool dryRun = false, string requestNumber = null,
            Dictionary<string, string> dynamicParameters = null, string path = "Actions[0]:Result:ExitData",
            SerializationType serializationType = SerializationType.Json, bool setContentType = true,
            int pollingIntervalSeconds = 1, int timeoutSeconds = 120, string nodeRootUrl = null)
        {
            StartPlanEnvelope planEnvelope = new StartPlanEnvelope() { DynamicParameters = dynamicParameters };

            requestNumber = !string.IsNullOrWhiteSpace( requestNumber ) ? $"&requestNumber={requestNumber}" : null;
            nodeRootUrl = !string.IsNullOrWhiteSpace( nodeRootUrl ) ? $"&nodeRootUrl={nodeRootUrl}" : null;
            path = !string.IsNullOrWhiteSpace( path ) ? $"&path={path}" : null;
            string st = !string.IsNullOrWhiteSpace( path ) ? $"&serializationType={serializationType}" : null;
            string sct = !string.IsNullOrWhiteSpace( path ) ? $"&setContentType={setContentType}" : null;
            string pi = pollingIntervalSeconds > 1 ? $"&pollingIntervalSeconds={pollingIntervalSeconds}" : null;
            string to = timeoutSeconds > 0 && timeoutSeconds != 120 ? $"&timeoutSeconds={timeoutSeconds}" : null;
            string qs = $"?dryRun={dryRun}{requestNumber}{path}{st}{sct}{pi}{to}{nodeRootUrl}";
            string requestUri = $"{_rootPath}/{planName}/start/sync/{qs}";
            return await PostAsync<StartPlanEnvelope, object>( planEnvelope, requestUri );
        }
        #endregion


        public Plan GetPlanStatus(string planName, long planInstanceId)
        {
            return GetPlanStatusAsync( planName, planInstanceId ).Result;
        }

        public async Task<Plan> GetPlanStatusAsync(string planName, long planInstanceId)
        {
            string requestUri = $"{_rootPath}/{planName}/{planInstanceId}/";
            return await GetAsync<Plan>( requestUri );
        }


        public object GetPlanElement(string planUniqueName, long planInstanceId, string elementPath,
            SerializationType serializationType = SerializationType.Json, bool setContentType = true)
        {
            return GetPlanElementAsync( planUniqueName, planInstanceId, elementPath, serializationType, setContentType ).Result;
        }

        public async Task<object> GetPlanElementAsync(string planUniqueName, long planInstanceId, string elementPath,
            SerializationType serializationType = SerializationType.Json, bool setContentType = true)
        {
            string requestUri = $"{_rootPath}/{planUniqueName}/{planInstanceId}/part/?{nameof( elementPath )}={elementPath}&{nameof( serializationType )}={serializationType}&{nameof( setContentType )}={setContentType}";
            return await GetAsync<object>( requestUri );
        }


        public object GetPlanElements(string planUniqueName, long planInstanceId, PlanElementParms elementParms)
        {
            return GetPlanElementsAsync( planUniqueName, planInstanceId, elementParms ).Result;
        }

        public async Task<object> GetPlanElementsAsync(string planUniqueName, long planInstanceId, PlanElementParms elementParms)
        {
            string requestUri = $"{_rootPath}/{planUniqueName}/{planInstanceId}/part/";
            return await PostAsync<PlanElementParms, object>( elementParms, requestUri );
        }


        public void SetPlanStatus(string planName, long planInstanceId, Plan plan)
        {
            SetPlanStatusAsync( planName, planInstanceId, plan ).Wait();
        }

        public async Task SetPlanStatusAsync(string planName, long planInstanceId, Plan plan)
        {
            string requestUri = $"{_rootPath}/{planName}/{planInstanceId}/";
            string planString = plan.ToYaml();
            planString = CryptoHelpers.Encode( planString );
            await PostAsyncVoid<string>( planString, requestUri );
        }


        public void SetPlanActionStatus(string planName, long planInstanceId, ActionItem actionItem)
        {
            SetPlanActionStatusAsync( planName, planInstanceId, actionItem ).Wait();
        }

        public async Task SetPlanActionStatusAsync(string planName, long planInstanceId, ActionItem actionItem)
        {
            string requestUri = $"{_rootPath}/{planName}/{planInstanceId}/action/";
            await PostAsyncVoid<ActionItem>( actionItem, requestUri );
        }


        public void CancelPlan(string planName, long planInstanceId, string nodeRootUrl = null)
        {
            CancelPlanAsync( planName, planInstanceId, nodeRootUrl ).Wait();
        }

        public async Task CancelPlanAsync(string planName, long planInstanceId, string nodeRootUrl = null)
        {
            string requestUri = $"{_rootPath}/{planName}/{planInstanceId}/?nodeRootUrl={nodeRootUrl}";
            await DeleteAsync( requestUri );
        }
    }
}