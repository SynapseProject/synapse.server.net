using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

using Synapse.Common.WebApi;
using Synapse.Core;


namespace Synapse.Services
{
    [RoutePrefix( "synapse/node" )]
    public class NodeController : ApiController
    {
        static PlanScheduler _scheduler = null;

        public NodeController()
        {
            InitPlanScheduler();
        }

        public static Action DrainstopCallback { get; set; }

        public static void InitPlanScheduler()
        {
            if( _scheduler == null )
            {
                _scheduler = new PlanScheduler( SynapseServer.Config.Node.MaxServerThreads );
                _scheduler.PlanCompleted += Scheduler_PlanCompleted;
                SynapseServer.Logger.Info( $"Initialized PlanScheduler, MaxThreads: {SynapseServer.Config.Node.MaxServerThreads}" );
            }
        }


        bool _isDrainstopped;
        List<Task> _tasks = new List<Task>();
        Dictionary<long, InProcPlanInfo> _plans = new Dictionary<long, InProcPlanInfo>();


        [HttpGet]
        [Route( "hello" )]
        public string Hello()
        {
            string context = GetContext( nameof( Hello ) );

            try
            {
                SynapseServer.Logger.Debug( context );
                return "Hello from SynapseNode, World!";
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

        [Route( "{planInstanceId}/" )]
        [HttpPost]
        public void StartPlanAsync(long planInstanceId, bool dryRun, [FromBody]Plan plan)
        {
            string context = GetContext( nameof( StartPlanAsync ),
                nameof( plan ), plan.Name, nameof( dryRun ), dryRun, nameof( planInstanceId ), planInstanceId );

            try
            {
                SynapseServer.Logger.Debug( context );
                plan.InstanceId = planInstanceId;
                PlanRuntimePod p = new PlanRuntimePod( plan, dryRun, null, plan.InstanceId );
                _scheduler.StartPlan( p );
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        private static void Scheduler_PlanCompleted(object sender, PlanCompletedEventArgs e)
        {
            SynapseServer.Logger.Info( $"Plan Completed: InstanceId: {e.PlanContainer.PlanInstanceId}, Name: {e.PlanContainer.Plan.Name}" );  //, At: {e.TimeCompleted}
        }

        [Route( "{planInstanceId}/" )]
        [HttpDelete]
        public bool CancelPlan(long planInstanceId)
        {
            string context = GetContext( nameof( CancelPlan ), nameof( planInstanceId ), planInstanceId );

            try
            {
                SynapseServer.Logger.Debug( context );
                bool found = _plans.ContainsKey( planInstanceId );
                if( found )
                    _plans[planInstanceId].CancellationToken.Cancel();
                string foundMsg = found ?
                    "Found executing Plan and signaled Cancel request." :
                    "Could not find executing Plan; Plan may have already completed execution.";
                SynapseServer.Logger.Info( $"CancelPlan {planInstanceId}: {foundMsg}" );
                return found;
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }


        //[Route( "{planUniqueName}/{planInstanceId}/" )]
        //[HttpGet]
        //public Plan GetPlanStatus(string planUniqueName, long planInstanceId)
        //{
        //    string context = GetContext( nameof( GetPlanStatus ),
        //        nameof( planUniqueName ), planUniqueName, nameof( planInstanceId ), planInstanceId );

        //    try
        //    {
        //        SynapseServer.Logger.Debug( context );
        //        return _server.GetPlanStatus( planUniqueName, planInstanceId );
        //    }
        //    catch( Exception ex )
        //    {
        //        SynapseServer.Logger.Error(
        //            Utilities.UnwindException( context, ex, asSingleLine: true ) );
        //        throw;
        //    }
        //}

        //[Route( "{planUniqueName}/{planInstanceId}/" )]
        //[HttpPost]
        //public void SetStatus(string planUniqueName, long planInstanceId, [FromBody]Plan plan)
        //{
        //    string context = GetContext( nameof( SetStatus ),
        //        nameof( planUniqueName ), planUniqueName, nameof( planInstanceId ), planInstanceId,
        //        nameof( plan ), plan );

        //    try
        //    {
        //        SynapseServer.Logger.Debug( context );
        //        _server.UpdatePlanStatus( plan );
        //    }
        //    catch( Exception ex )
        //    {
        //        SynapseServer.Logger.Error(
        //            Utilities.UnwindException( context, ex, asSingleLine: true ) );
        //        throw;
        //    }
        //}

        //[Route( "{planUniqueName}/{planInstanceId}/action/" )]
        //[HttpPost]
        //public void SetStatus(string planUniqueName, long planInstanceId, [FromBody]ActionItem actionItem)
        //{
        //    string context = GetContext( nameof( SetStatus ),
        //        nameof( planUniqueName ), planUniqueName, nameof( planInstanceId ), planInstanceId,
        //        nameof( actionItem ), actionItem );

        //    try
        //    {
        //        SynapseServer.Logger.Debug( context );
        //        _server.UpdatePlanActionStatus( planUniqueName, planInstanceId, actionItem );
        //    }
        //    catch( Exception ex )
        //    {
        //        SynapseServer.Logger.Error(
        //            Utilities.UnwindException( context, ex, asSingleLine: true ) );
        //        throw;
        //    }
        //}

        //[Route( "{planUniqueName}/{planInstanceId}/" )]
        //[HttpDelete]
        //public void CancelPlan(string planUniqueName, long planInstanceId)
        //{
        //    string context = GetContext( nameof( GetPlanStatus ),
        //        nameof( planUniqueName ), planUniqueName, nameof( planInstanceId ), planInstanceId );

        //    try
        //    {
        //        SynapseServer.Logger.Debug( context );
        //        _server.CancelPlan( planInstanceId );
        //    }
        //    catch( Exception ex )
        //    {
        //        SynapseServer.Logger.Error(
        //            Utilities.UnwindException( context, ex, asSingleLine: true ) );
        //        throw;
        //    }
        //}


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