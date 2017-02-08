using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Synapse.Common.WebApi;
using Synapse.Core;
using Synapse.Core.Utilities;

namespace Synapse.Services
{
    public class NodeServiceHttpApiClient : HttpApiClientBase
    {
        string _rootPath = "/synapse/node";

        public NodeServiceHttpApiClient(string baseUrl, string messageFormatType = "application/json") : base( baseUrl, messageFormatType )
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


        public ExecuteResult StartPlanFile(long planInstanceId, bool dryRun, string filePath)
        {
            return StartPlanFileAsync( planInstanceId, dryRun, filePath ).Result;
        }

        public async Task<ExecuteResult> StartPlanFileAsync(long planInstanceId, bool dryRun, string filePath)
        {
            if( File.Exists( filePath ) )
            {
                Plan plan = YamlHelpers.DeserializeFile<Plan>( filePath );
                string requestUri = $"{_rootPath}/{planInstanceId}/?dryRun={dryRun}";
                return await PostAsync<Plan, ExecuteResult>( plan, requestUri );
            }
            else
                throw new FileNotFoundException( "Unable to start Plan.", filePath );
        }

        public ExecuteResult StartPlan(long planInstanceId, bool dryRun, string filePath)
        {
            return StartPlanAsync( planInstanceId, dryRun, filePath ).Result;
        }

        public async Task<ExecuteResult> StartPlanAsync(long planInstanceId, bool dryRun, string filePath)
        {
            if( File.Exists( filePath ) )
            {
                Plan plan = YamlHelpers.DeserializeFile<Plan>( filePath );
                string requestUri = $"{_rootPath}/{planInstanceId}/?dryRun={dryRun}";
                return await PostAsync<Plan, ExecuteResult>( plan, requestUri );
            }
            else
                throw new FileNotFoundException( "Unable to start Plan.", filePath );
        }

        public ExecuteResult StartPlan(long planInstanceId, bool dryRun, Plan plan)
        {
            return StartPlanAsync( planInstanceId, dryRun, plan ).Result;
        }

        public async Task<ExecuteResult> StartPlanAsync(long planInstanceId, bool dryRun, Plan plan)
        {
            string requestUri = $"{_rootPath}/{planInstanceId}/?dryRun={dryRun}";
            return await PostAsync<Plan, ExecuteResult>( plan, requestUri );
        }

        public void CancelPlan(long planInstanceId)
        {
            CancelPlanAsync( planInstanceId ).Wait();
        }

        public async Task CancelPlanAsync(long planInstanceId)
        {
            string requestUri = $"{_rootPath}/{planInstanceId}/";
            await DeleteAsync( requestUri );
        }


        public void Drainstop(bool shutdown = true)
        {
            DrainstopAsync( shutdown ).Wait();
        }

        public async Task DrainstopAsync(bool shutdown = true)
        {
            string requestUri = $"{_rootPath}/drainstop/?shutdown={shutdown}";
            await GetAsync( requestUri );
        }

        public void CancelDrainstop()
        {
            CancelDrainstopAsync().Wait();
        }

        public async Task CancelDrainstopAsync()
        {
            string requestUri = $"{_rootPath}/drainstop/cancel";
            await GetAsync( requestUri );
        }

        public bool GetIsDrainstopComplete() { return GetIsDrainstopCompleteAsync().Result; }

        public async Task<bool> GetIsDrainstopCompleteAsync()
        {
            string requestUri = $"{_rootPath}/drainstop/iscomplete";
            return await GetAsync<bool>( requestUri );
        }

        public int GetCurrentQueueDepth() { return GetCurrentQueueDepthAsync().Result; }

        public async Task<int> GetCurrentQueueDepthAsync()
        {
            string requestUri = $"{_rootPath}/queue/count";
            return await GetAsync<int>( requestUri );
        }

        public List<string> GetCurrentQueueItems() { return GetCurrentQueueItemsAsync().Result; }

        public async Task<List<string>> GetCurrentQueueItemsAsync()
        {
            string requestUri = $"{_rootPath}/queue";
            return await GetAsync<List<string>>( requestUri );
        }
    }
}