using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Web.Http;
using System.Web.Http.Routing;

using Synapse.Core;
using Synapse.Common.WebApi;
using Synapse.Core.Utilities;

namespace Synapse.Services
{
    [RoutePrefix( "synapse/execute" )]
    public class ExecuteController : ApiController, IExecuteController
    {
        PlanServer _server = null;

        [HttpGet]
        [Route( "hello" )]
        public string Hello()
        {
            string context = GetContext( nameof( Hello ) );

            try
            {
                SynapseServer.Logger.Debug( context );
                return "Hello from SynapseController, World!";
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [HttpGet]
        [Route( "hello/whoami" )]
        public string WhoAmI()
        {
            string context = GetContext( nameof( WhoAmI ) );

            try
            {
                SynapseServer.Logger.Debug( context );
                return CurrentUserName;
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [HttpGet]
        [Route( "" )]
        public IEnumerable<string> GetPlanList(string filter = null, bool isRegexFilter = true)
        {
            InitPlanServer();

            string context = GetContext( nameof( GetPlanList ), nameof( filter ), filter, nameof( isRegexFilter ), isRegexFilter );

            try
            {
                SynapseServer.Logger.Debug( context );
                return _server.GetPlanList( filter, isRegexFilter );
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planUniqueName}/" )]
        [HttpGet]
        public IEnumerable<long> GetPlanInstanceIdList(string planUniqueName)
        {
            InitPlanServer();

            string context = GetContext( nameof( GetPlanInstanceIdList ), nameof( planUniqueName ), planUniqueName );

            try
            {
                SynapseServer.Logger.Debug( context );
                return _server.GetPlanInstanceIdList( planUniqueName );
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planUniqueName}/start/" )]
        [HttpGet]
        public long StartPlan(string planUniqueName, bool dryRun = false, string requestNumber = null, string nodeRootUrl = null)
        {
            InitPlanServer();

            Uri uri = this.Url.Request.RequestUri;
            string context = GetContext( nameof( StartPlan ), nameof( CurrentUserName ), CurrentUserName,
                nameof( planUniqueName ), planUniqueName, nameof( dryRun ), dryRun,
                nameof( requestNumber ), requestNumber, nameof( nodeRootUrl ), nodeRootUrl, "QueryString", uri.Query );

            try
            {
                SynapseServer.Logger.Debug( context );
                Dictionary<string, string> dynamicParameters = uri.ParseQueryString();
                if( dynamicParameters.ContainsKey( nameof( dryRun ) ) ) dynamicParameters.Remove( nameof( dryRun ) );
                return _server.StartPlan( CurrentUserName, planUniqueName, dryRun, requestNumber, dynamicParameters, nodeRootUrl: nodeRootUrl,
                    referrer: this.Url.Request.RequestUri );
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planUniqueName}/start/" )]
        [HttpPost]
        public long StartPlan([FromBody]StartPlanEnvelope planEnvelope, string planUniqueName, bool dryRun = false, string requestNumber = null, string nodeRootUrl = null)
        {
            InitPlanServer();

            bool failedToDeserialize = false;
            Dictionary<string, string> dynamicParameters = planEnvelope?.DynamicParameters;

            StringBuilder parms = new StringBuilder();
            if( dynamicParameters != null )
            {
                string s = string.Empty;
                foreach( string key in dynamicParameters.Keys )
                {
                    parms.Append( $"{s}{key}: {dynamicParameters[key]}" );
                    s = ", ";
                }
            }
            else
            {
                string rawBody = Request.Properties["body"].ToString();
                failedToDeserialize = !string.IsNullOrWhiteSpace( rawBody );
                if( failedToDeserialize )
                    parms.Append( rawBody );
            }

            string context = GetContext( nameof( StartPlan ), nameof( CurrentUserName ), CurrentUserName,
                nameof( planUniqueName ), planUniqueName, nameof( dryRun ), dryRun,
                nameof( requestNumber ), requestNumber, nameof( nodeRootUrl ), nodeRootUrl, "planParameters", parms.ToString() );

            try
            {
                SynapseServer.Logger.Info( context );

                if( failedToDeserialize )
                    throw new Exception( $"Failed to deserialize message body:\r\n{parms.ToString()}" );

                return _server.StartPlan( CurrentUserName, planUniqueName, dryRun, requestNumber, dynamicParameters,
                    postDynamicParameters: true, nodeRootUrl: nodeRootUrl, referrer: this.Url?.Request?.RequestUri );
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planUniqueName}/{planInstanceId}/" )]
        [HttpGet]
        public Plan GetPlanStatus(string planUniqueName, long planInstanceId)
        {
            InitPlanServer();

            string context = GetContext( nameof( GetPlanStatus ),
                nameof( planUniqueName ), planUniqueName, nameof( planInstanceId ), planInstanceId );

            try
            {
                SynapseServer.Logger.Debug( context );
                return _server.GetPlanStatus( planUniqueName, planInstanceId );
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planUniqueName}/{planInstanceId}/" )]
        [HttpPost]
        public void SetStatus(string planUniqueName, long planInstanceId, [FromBody]string planString)
        {
            InitPlanServer();

            string context = GetContext( nameof( SetStatus ),
                nameof( planUniqueName ), planUniqueName, nameof( planInstanceId ), planInstanceId,
                nameof( planString ), planString );

            planString = CryptoHelpers.Decode( planString );
            Plan plan = Plan.FromYaml( new StringReader( planString ) );

            try
            {
                SynapseServer.Logger.Debug( context );
                _server.UpdatePlanStatus( plan );
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planUniqueName}/{planInstanceId}/action/" )]
        [HttpPost]
        public void SetStatus(string planUniqueName, long planInstanceId, [FromBody]ActionItem actionItem)
        {
            InitPlanServer();

            string context = GetContext( nameof( SetStatus ),
                nameof( planUniqueName ), planUniqueName, nameof( planInstanceId ), planInstanceId,
                nameof( actionItem ), actionItem );

            try
            {
                SynapseServer.Logger.Debug( context );
                _server.UpdatePlanActionStatus( planUniqueName, planInstanceId, actionItem );
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planUniqueName}/{planInstanceId}/" )]
        [HttpDelete]
        public void CancelPlan(string planUniqueName, long planInstanceId, string nodeRootUrl = null)
        {
            InitPlanServer();

            string context = GetContext( nameof( GetPlanStatus ),
                nameof( planUniqueName ), planUniqueName, nameof( planInstanceId ), planInstanceId,
                nameof( nodeRootUrl ), nodeRootUrl );

            try
            {
                SynapseServer.Logger.Debug( context );
                _server.CancelPlan( planInstanceId, nodeRootUrl, referrer: this.Url.Request.RequestUri );
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planUniqueName}/{planInstanceId}/part/" )]
        [HttpGet]
        public object GetPlanElements(string planUniqueName, long planInstanceId, string elementPath, SerializationType serializationType = SerializationType.Json)
        {
            InitPlanServer();

            string context = GetContext( nameof( GetPlanStatus ),
                nameof( planUniqueName ), planUniqueName, nameof( planInstanceId ), planInstanceId,
                nameof( elementPath ), elementPath, nameof( serializationType ), serializationType );

            try
            {
                SynapseServer.Logger.Debug( context );

                PlanElementParms pep = new PlanElementParms();
                pep.Type = serializationType;
                pep.ElementPaths.Add( elementPath );

                return _server.GetPlanElements( planUniqueName, planInstanceId, pep );
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planUniqueName}/{planInstanceId}/part/" )]
        [HttpPost]
        public object GetPlanElements(string planUniqueName, long planInstanceId, [FromBody]PlanElementParms elementParms)
        {
            InitPlanServer();

            string context = GetContext( nameof( GetPlanStatus ),
                nameof( planUniqueName ), planUniqueName, nameof( planInstanceId ), planInstanceId );

            try
            {
                SynapseServer.Logger.Debug( context );
                return _server.GetPlanElements( planUniqueName, planInstanceId, elementParms );
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }


        #region utility methods
        void InitPlanServer()
        {
            if( _server == null )
                _server = new PlanServer();
        }

        string GetContext(string context, params object[] parms)
        {
            StringBuilder c = new StringBuilder();
            c.Append( $"{context}(" );
            for( int i = 0; i < parms.Length; i += 2 )
                c.Append( $"{parms[i]}: {parms[i + 1]}, " );

            return $"{c.ToString().TrimEnd( ',', ' ' )})";
        }

        string GetContext(string context, Dictionary<string, object> d)
        {
            StringBuilder c = new StringBuilder();
            c.Append( $"{context}(" );
            foreach( string key in d.Keys )
                c.Append( $"{key}: {d[key]}, " );

            return $"{c.ToString().TrimEnd( ',', ' ' )})";
        }

        string CurrentUserName
        {
            get
            {
                return CurrentUser?.Identity?.Name ?? "Anonymous";
            }
        }

        UrlHelper _currentUrl = null;
        public UrlHelper CurrentUrl
        {
            get { return _currentUrl ?? this.Url; }
            set { _currentUrl = value; }
        }

        IPrincipal _currentUser = null;
        public IPrincipal CurrentUser
        {
            get { return _currentUser ?? this.User; }
            set { _currentUser = value; }
        }
        #endregion
    }
}

//////[Route( "{domainUId:Guid}" )]
//////[HttpGet]
////// GET api/demo 
//[Route( "foo/" )]
//public IEnumerable<string> Get()
//{
//    return new string[] { "Hello", "World", CurrentUser };
//}

//// GET api/demo/5 
//public string Get(int id)
//{
//    return "Hello, World!";
//}

////[Route( "" )]
////[Route( "byrls/" )]
////[HttpPost]
//// POST api/demo 
//public void Post([FromBody]string value) { }
//// PUT api/demo/5 
//public void Put(int id, [FromBody]string value) { }
//// DELETE api/demo/5 
//public void Delete(int id) { }