using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Http;

using Synapse.Core;
using Synapse.Common.WebApi;
using Synapse.Core.Utilities;

namespace Synapse.Services
{
    [RoutePrefix( "synapse/execute" )]
    public class ExecuteController : ApiController
    {
        PlanServer _server = new PlanServer();

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
                return CurrentUser;
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
        public IEnumerable<string> GetPlanList()
        {
            string context = GetContext( nameof( GetPlanList ) );

            try
            {
                SynapseServer.Logger.Debug( context );
                return _server.GetPlanList();
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
        public long StartPlan(string planUniqueName, bool dryRun = false)
        {
            Uri uri = this.Url.Request.RequestUri;
            string context = GetContext( nameof( StartPlan ),
                nameof( planUniqueName ), planUniqueName, nameof( dryRun ), dryRun, "QueryString", uri.Query );

            try
            {
                SynapseServer.Logger.Debug( context );
                Dictionary<string, string> dynamicParameters = uri.ParseQueryString();
                if( dynamicParameters.ContainsKey( nameof( dryRun ) ) ) dynamicParameters.Remove( nameof( dryRun ) );
                return _server.StartPlan( planUniqueName, dryRun, dynamicParameters );
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
        public void CancelPlan(string planUniqueName, long planInstanceId)
        {
            string context = GetContext( nameof( GetPlanStatus ),
                nameof( planUniqueName ), planUniqueName, nameof( planInstanceId ), planInstanceId );

            try
            {
                SynapseServer.Logger.Debug( context );
                _server.CancelPlan( planInstanceId );
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }


        #region utility methods
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

        string CurrentUser
        {
            get
            {
                return User != null && User.Identity != null ? User.Identity.Name : "Anonymous";
            }
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