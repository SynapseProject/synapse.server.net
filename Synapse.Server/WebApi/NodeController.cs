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
        public void CancelPlan(long planInstanceId)
        {
            string context = GetContext( nameof( CancelPlan ), nameof( planInstanceId ), planInstanceId );

            try
            {
                SynapseServer.Logger.Debug( context );
                bool found = _scheduler.CancelPlan( planInstanceId );
                string foundMsg = found ?
                    "Found executing Plan and signaled Cancel request." :
                    "Could not find executing Plan; Plan may have already completed execution.";
                SynapseServer.Logger.Info( $"CancelPlan {planInstanceId}: {foundMsg}" );
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }


        #region drainstop
        [HttpGet]
        [Route( "drainstop/" )]
        public void Drainstop(bool shutdown = true)
        {
            string context = GetContext( nameof( Drainstop ), nameof( shutdown ), shutdown );

            try
            {
                SynapseServer.Logger.Debug( context );
                SynapseServer.Logger.Info( $"Drainstop starting, CurrentQueueDepth: {_scheduler.CurrentQueueDepth}.  Shutdown when complete: {shutdown}." );
                _scheduler.Drainstop();
                SynapseServer.Logger.Info( $"Drainstop complete, CurrentQueueDepth: {_scheduler.CurrentQueueDepth}" );
                if( shutdown && _scheduler.IsDrainstopped )
                {
                    SynapseServer.Logger.Info( $"Drainstop complete, initiating Shutdown." );
                    DrainstopCallback?.Invoke();
                }
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [HttpGet]
        [Route( "drainstop/cancel" )]
        public void CancelDrainstop()
        {
            string context = GetContext( nameof( CancelDrainstop ) );

            try
            {
                SynapseServer.Logger.Debug( context );
                SynapseServer.Logger.Info( $"Undrainstop starting, CurrentQueueDepth: {_scheduler.CurrentQueueDepth}" );
                _scheduler.CancelDrainstop();
                SynapseServer.Logger.Info( $"Undrainstop complete, CurrentQueueDepth: {_scheduler.CurrentQueueDepth}" );
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [HttpGet]
        [Route( "drainstop/iscomplete" )]
        public bool GetIsDrainstopComplete()
        {
            string context = GetContext( nameof( GetIsDrainstopComplete ) );

            try
            {
                SynapseServer.Logger.Debug( context );
                return _scheduler.IsDrainstopComplete;
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [HttpGet]
        [Route( "queue/count" )]
        public int GetCurrentQueueDepth()
        {
            string context = GetContext( nameof( GetCurrentQueueDepth ) );

            try
            {
                SynapseServer.Logger.Debug( context );
                return _scheduler.CurrentQueueDepth;
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [HttpGet]
        [Route( "queue" )]
        public List<string> GetCurrentQueueItems()
        {
            string context = GetContext( nameof( GetCurrentQueueItems ) );

            try
            {
                SynapseServer.Logger.Debug( context );
                return _scheduler.CurrentQueue;
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }
        #endregion

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