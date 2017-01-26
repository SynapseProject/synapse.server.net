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
        [Route( "" )]
        public IEnumerable<string> GetPlanList()
        {
            try
            {
                string[] foo = new string[] { "Hello", "World", CurrentUser, DateTime.Now.ToString() };
                SynapseControllerService.Logger.Debug( foo );
                return foo;
            }
            catch( Exception ex )
            {
                SynapseControllerService.Logger.Error(
                    Utilities.UnwindException( nameof( GetPlanList ), ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planName}/" )]
        [HttpGet]
        public IEnumerable<long> GetPlanInstanceIdList(string planName)
        {
            //Dictionary<string, object> d = new Dictionary<string, object>();
            //d.Add( nameof( planName ), planName );
            //string context = GetContext( nameof( GetPlanInstanceIdList ), d );

            string context = GetContext( nameof( GetPlanInstanceIdList ), nameof( planName ), planName );

            try
            {
                SynapseControllerService.Logger.Debug( context );
                return new long[] { 1, 2, 3 };
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
            try
            {
                return _server.StartPlan( planName, dryRun );
            }
            catch( Exception ex )
            {
                Dictionary<string, object> d = new Dictionary<string, object>();
                d.Add( nameof( planName ), planName );
                d.Add( nameof( dryRun ), dryRun );
                string context = GetContext( nameof( StartPlan ), d );

                SynapseControllerService.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planName}/{planInstanceId}/" )]
        [HttpGet]
        public Plan GetPlanStatus(string planName, long planInstanceId)
        {
            try
            {
                return _server.GetPlanStatus( planName, planInstanceId );
            }
            catch( Exception ex )
            {
                Dictionary<string, object> d = new Dictionary<string, object>();
                d.Add( nameof( planName ), planName );
                d.Add( nameof( planInstanceId ), planInstanceId );
                string context = GetContext( nameof( GetPlanStatus ), d );

                SynapseControllerService.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planName}/{planInstanceId}/" )]
        [HttpPost]
        public void WriteStatus(string planName, long planInstanceId, [FromBody]string msg)
        {
            try
            {
                _server.WriteStatus( msg );
            }
            catch( Exception ex )
            {
                Dictionary<string, object> d = new Dictionary<string, object>();
                d.Add( nameof( planName ), planName );
                d.Add( nameof( planInstanceId ), planInstanceId );
                d.Add( nameof( msg ), msg );
                string context = GetContext( nameof( WriteStatus ), d );

                SynapseControllerService.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planName}/{planInstanceId}/" )]
        [HttpDelete]
        public void CancelPlan(string planName, long planInstanceId)
        {
            try
            {
                _server.CancelPlan( planInstanceId );
            }
            catch( Exception ex )
            {
                Dictionary<string, object> d = new Dictionary<string, object>();
                d.Add( nameof( planName ), planName );
                d.Add( nameof( planInstanceId ), planInstanceId );
                string context = GetContext( nameof( CancelPlan ), d );

                SynapseControllerService.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        public string GetContext(string context, params object[] parms)
        {
            StringBuilder c = new StringBuilder();
            c.Append( $"{context}( " );
            for( int i = 0; i < parms.Length; i+=2 )
                c.Append( $"{parms[i]}: {parms[i+1]}, " );

            return $"{c.ToString().TrimEnd( ',', ' ' )} )";
        }

        public string GetContext(string context, Dictionary<string, object> d)
        {
            StringBuilder c = new StringBuilder();
            c.Append( $"{context}( " );
            foreach( string key in d.Keys )
                c.Append( $"{key}: {d[key]}, " );

            return $"{c.ToString().TrimEnd( ',', ' ' )} )";
        }

        public string CurrentUser
        {
            get
            {
                return User != null && User.Identity != null ? User.Identity.Name : "Anonymous";
            }
        }
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