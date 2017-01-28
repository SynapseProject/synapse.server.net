using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Synapse.Core;

using Synapse.Common.WebApi;

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
                SynapseControllerService.Logger.Debug( context );
                return "Hello from SynapseControllerServer, World!";
            }
            catch( Exception ex )
            {
                SynapseControllerService.Logger.Error(
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
                SynapseControllerService.Logger.Debug( context );
                return CurrentUser;
            }
            catch( Exception ex )
            {
                SynapseControllerService.Logger.Error(
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
                SynapseControllerService.Logger.Debug( context );
                string[] foo = new string[] { "Hello", "World", CurrentUser, DateTime.Now.ToString() };
                return foo;
            }
            catch( Exception ex )
            {
                SynapseControllerService.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planName}/" )]
        [HttpGet]
        public IEnumerable<long> GetPlanInstanceIdList(string planName)
        {
            string context = GetContext( nameof( GetPlanInstanceIdList ), nameof( planName ), planName );

            try
            {
                SynapseControllerService.Logger.Debug( context );
                long[] foo = new long[] { 1, 2, 3 };
                return foo;
            }
            catch( Exception ex )
            {
                SynapseControllerService.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planName}/start/" )]
        [HttpGet]
        public long StartPlan(string planName, bool dryRun = false)
        {
            string context = GetContext( nameof( StartPlan ),
                nameof( planName ), planName, nameof( dryRun ), dryRun );

            try
            {
                SynapseControllerService.Logger.Debug( context );
                return _server.StartPlan( planName, dryRun );
            }
            catch( Exception ex )
            {
                SynapseControllerService.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planName}/{planInstanceId}/" )]
        [HttpGet]
        public Plan GetPlanStatus(string planName, long planInstanceId)
        {
            string context = GetContext( nameof( GetPlanStatus ),
                nameof( planName ), planName, nameof( planInstanceId ), planInstanceId );

            try
            {
                SynapseControllerService.Logger.Debug( context );
                return _server.GetPlanStatus( planName, planInstanceId );
            }
            catch( Exception ex )
            {
                SynapseControllerService.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planName}/{planInstanceId}/" )]
        [HttpPost]
        public void WriteStatus(string planName, long planInstanceId, [FromBody]string msg)
        {
            string context = GetContext( nameof( WriteStatus ),
                nameof( planName ), planName, nameof( planInstanceId ), planInstanceId,
                nameof( msg ), msg );

            try
            {
                SynapseControllerService.Logger.Debug( context );
                _server.WriteStatus( msg );
            }
            catch( Exception ex )
            {
                SynapseControllerService.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planName}/{planInstanceId}/" )]
        [HttpDelete]
        public void CancelPlan(string planName, long planInstanceId)
        {
            string context = GetContext( nameof( GetPlanStatus ),
                nameof( planName ), planName, nameof( planInstanceId ), planInstanceId );

            try
            {
                SynapseControllerService.Logger.Debug( context );
                _server.CancelPlan( planInstanceId );
            }
            catch( Exception ex )
            {
                SynapseControllerService.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }


        #region utility methods
        string GetContext(string context, params object[] parms)
        {
            StringBuilder c = new StringBuilder();
            c.Append( $"{context}(" );
            for( int i = 0; i < parms.Length; i+=2 )
                c.Append( $"{parms[i]}: {parms[i+1]}, " );

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