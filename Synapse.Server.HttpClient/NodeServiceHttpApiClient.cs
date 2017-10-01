using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Synapse.Common.WebApi;
using Synapse.Common.Utilities;
using Synapse.Core;
using Synapse.Core.Utilities;

namespace Synapse.Services
{
    public class NodeServiceHttpApiClient : HttpApiClientBase
    {
        string _rootPath = "/synapse/node";

        public NodeServiceHttpApiClient(string baseUrl, string messageFormatType = "application/json", string referrer = null) : base( baseUrl, messageFormatType )
        {
            if( !string.IsNullOrWhiteSpace( referrer ) )
                Headers.Referrer = new Uri( referrer );
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

        public object About(bool asCsv = false) { return AboutAsync( asCsv ).Result; }

        public async Task<object> AboutAsync(bool asCsv = false)
        {
            string requestUri = $"{_rootPath}/hello/about/?asCsv={asCsv}";
            return await GetAsync<object>( requestUri );
        }


        public ExecuteResult StartPlanFile(string filePath, long planInstanceId, bool dryRun = false, Dictionary<string, string> dynamicParameters = null)
        {
            return StartPlanAsync( filePath, planInstanceId, dryRun, dynamicParameters ).Result;
        }

        public async Task<ExecuteResult> StartPlanFileAsync(string filePath, long planInstanceId, bool dryRun = false, Dictionary<string, string> dynamicParameters = null)
        {
            return await StartPlanAsync( filePath, planInstanceId, dryRun, dynamicParameters );
        }

        public ExecuteResult StartPlan(string filePath, long planInstanceId, bool dryRun = false, Dictionary<string, string> dynamicParameters = null)
        {
            return StartPlanAsync( filePath, planInstanceId, dryRun, dynamicParameters ).Result;
        }

        public async Task<ExecuteResult> StartPlanAsync(string filePath, long planInstanceId, bool dryRun = false, Dictionary<string, string> dynamicParameters = null)
        {
            if( File.Exists( filePath ) )
            {
                Plan plan = YamlHelpers.DeserializeFile<Plan>( filePath );
                string planString = plan.ToYaml();
                planString = CryptoHelpers.Encode( planString );
                string requestUri = $"{_rootPath}/{planInstanceId}/?dryRun={dryRun}{dynamicParameters?.ToQueryString( asPartialQueryString: true )}";
                return await PostAsync<string, ExecuteResult>( planString, requestUri );
            }
            else
                throw new FileNotFoundException( "Unable to start Plan.", filePath );
        }


        public ExecuteResult StartPlan(Plan plan, long planInstanceId, bool dryRun = false, Dictionary<string, string> dynamicParameters = null, bool postDynamicParameters = false)
        {
            if( postDynamicParameters )
                return StartPlanAsyncWithParametersAsPost( plan, planInstanceId, dryRun, dynamicParameters ).Result;
            else
                return StartPlanAsyncWithParametersAsQueryString( plan, planInstanceId, dryRun, dynamicParameters ).Result;
        }

        public async Task<ExecuteResult> StartPlanAsyncWithParametersAsQueryString(Plan plan, long planInstanceId, bool dryRun = false, Dictionary<string, string> dynamicParameters = null)
        {
            string planString = plan.ToYaml();
            planString = CryptoHelpers.Encode( planString );
            string requestUri = $"{_rootPath}/{planInstanceId}/?dryRun={dryRun}{dynamicParameters?.ToQueryString( asPartialQueryString: true )}";
            return await PostAsync<string, ExecuteResult>( planString, requestUri );
        }

        public async Task<ExecuteResult> StartPlanAsyncWithParametersAsPost(Plan plan, long planInstanceId, bool dryRun = false, Dictionary<string, string> dynamicParameters = null)
        {
            dynamicParameters?.PrepareValuesForPost();
            StartPlanEnvelope planEnvelope = new StartPlanEnvelope()
            {
                Plan = plan,
                DynamicParameters = dynamicParameters
            };

            string planString = planEnvelope.ToYaml( encode: true );
            string requestUri = $"{_rootPath}/{planInstanceId}/p/?dryRun={dryRun}";
            return await PostAsync<string, ExecuteResult>( planString, requestUri );
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