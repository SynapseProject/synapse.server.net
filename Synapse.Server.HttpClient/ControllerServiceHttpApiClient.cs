using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Synapse.Common.WebApi;
using Synapse.Core;

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


        public List<string> GetPlanList()
        {
            return GetPlanListAsync().Result;
        }

        public async Task<List<string>> GetPlanListAsync()
        {
            string requestUri = $"{_rootPath}";
            return await GetAsync<List<string>>( requestUri );
        }


        public List<int> GetPlanInstanceIdList(string planName)
        {
            return GetPlanInstanceIdListAsync( planName ).Result;
        }

        public async Task<List<int>> GetPlanInstanceIdListAsync(string planName)
        {
            string requestUri = $"{_rootPath}/{planName}";
            return await GetAsync<List<int>>( requestUri );
        }


        public long StartPlan(string planName, bool dryRun = false, Dictionary<string, string> dynamicParameters = null)
        {
            return StartPlanAsync( planName, dryRun, dynamicParameters ).Result;
        }

        public async Task<long> StartPlanAsync(string planName, bool dryRun = false, Dictionary<string, string> dynamicParameters = null)
        {
            string qs = $"?dryRun={dryRun}{dynamicParameters?.ToQueryString( asPartialQueryString: true )}";
            string requestUri = $"{_rootPath}/{planName}/start/{qs}";
            return await GetAsync<long>( requestUri );
        }


        public Plan GetPlanStatus(string planName, long planInstanceId)
        {
            return GetPlanStatusAsync( planName, planInstanceId ).Result;
        }

        public async Task<Plan> GetPlanStatusAsync(string planName, long planInstanceId)
        {
            string requestUri = $"{_rootPath}/{planName}/{planInstanceId}/";
            return await GetAsync<Plan>( requestUri );
        }


        public void SetPlanStatus(string planName, long planInstanceId, Plan plan)
        {
            SetPlanStatusAsync( planName, planInstanceId, plan ).Wait();
        }

        public async Task SetPlanStatusAsync(string planName, long planInstanceId, Plan plan)
        {
            string requestUri = $"{_rootPath}/{planName}/{planInstanceId}/";
            await PostAsyncVoid<Plan>( plan, requestUri );
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


        public void CancelPlan(string planName, long planInstanceId)
        {
            CancelPlanAsync( planName, planInstanceId ).Wait();
        }

        public async Task CancelPlanAsync(string planName, long planInstanceId)
        {
            string requestUri = $"{_rootPath}/{planName}/{planInstanceId}/";
            await DeleteAsync( requestUri );
        }
    }
}