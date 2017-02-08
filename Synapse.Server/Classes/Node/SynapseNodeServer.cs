using System;
using Synapse.Core;
using Synapse.Core.Runtime;
using System.Collections.Generic;
using System.Text;
using Synapse.Common.WebApi;

namespace Synapse.Services
{
    public class SynapseNodeServer : ISynapseNodeServer
    {
        static PlanScheduler _scheduler = null;

        public SynapseNodeServer()
        {
            InitPlanScheduler();
        }

        public static Action DrainstopCallback { get; set; }

        public static void InitPlanScheduler()
        {
            if( _scheduler == null )
            {
                _scheduler = new PlanScheduler( SynapseNodeService.Config.MaxServerThreads );
                _scheduler.PlanCompleted += Scheduler_PlanCompleted;
                SynapseNodeService.Logger.Info( $"Initialized PlanScheduler, MaxThreads: {SynapseNodeService.Config.MaxServerThreads}" );
            }
        }

        #region ISynapseServer Members

        public string Hello()
        {
            string context = GetContext( nameof( Hello ) );
            SynapseNodeService.Logger.Debug( context );
            return "Hello from SynapseNodeServer, World!";
        }

        public string WhoAmI()
        {
            string context = GetContext( nameof( WhoAmI ) );
            SynapseNodeService.Logger.Debug( context );
            return "WhoAmI from SynapseNodeServer, World!";
        }

        public ExecuteResult StartPlan(string planInstanceId, bool dryRun, Plan plan)
        {
            string context = GetContext( nameof( StartPlan ),
                nameof( plan ), plan.Name, nameof( dryRun ), dryRun, nameof( planInstanceId ), planInstanceId );

            try
            {
                SynapseNodeService.Logger.Debug( context );
                plan.InstanceId = long.Parse( planInstanceId );
                return plan.Start( null, dryRun );
            }
            catch( Exception ex )
            {
                SynapseNodeService.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        public void StartPlanAsync(string planInstanceId, bool dryRun, Plan plan)
        {
            string context = GetContext( nameof( StartPlanAsync ),
                nameof( plan ), plan.Name, nameof( dryRun ), dryRun, nameof( planInstanceId ), planInstanceId );

            try
            {
                SynapseNodeService.Logger.Debug( context );
                plan.InstanceId = long.Parse( planInstanceId );
                PlanRuntimePod p = new PlanRuntimePod( plan, dryRun, null, plan.InstanceId );
                _scheduler.StartPlan( p );  //_scheduler.StartPlan( null, dryRun, plan );
            }
            catch( Exception ex )
            {
                SynapseNodeService.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        private static void Scheduler_PlanCompleted(object sender, PlanCompletedEventArgs e)
        {
            SynapseNodeService.Logger.Info( $"Plan Completed: InstanceId: {e.PlanContainer.PlanInstanceId}, Name: {e.PlanContainer.Plan.Name}" );  //, At: {e.TimeCompleted}
        }

        public void CancelPlan(string planInstanceId)
        {
            string context = GetContext( nameof( CancelPlan ), nameof( planInstanceId ), planInstanceId );

            try
            {
                SynapseNodeService.Logger.Debug( context );
                int planInstId = int.Parse( planInstanceId );
                bool found = _scheduler.CancelPlan( planInstId );
                string foundMsg = found ?
                    "Found executing Plan and signaled Cancel request." :
                    "Could not find executing Plan; Plan may have already completed execution.";
                SynapseNodeService.Logger.Info( $"CancelPlan {planInstId}: {foundMsg}" );
            }
            catch( Exception ex )
            {
                SynapseNodeService.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        public void Drainstop(bool shutdown)
        {
            string context = GetContext( nameof( Drainstop ), nameof( shutdown ), shutdown );

            try
            {
                SynapseNodeService.Logger.Debug( context );
                SynapseNodeService.Logger.Info( $"Drainstop starting, CurrentQueueDepth: {_scheduler.CurrentQueueDepth}.  Shutdown when complete: {shutdown}." );
                _scheduler.Drainstop();
                SynapseNodeService.Logger.Info( $"Drainstop complete, CurrentQueueDepth: {_scheduler.CurrentQueueDepth}" );
                if( shutdown )
                {
                    SynapseNodeService.Logger.Info( $"Drainstop initiating Shutdown." );
                    DrainstopCallback?.Invoke();
                }
            }
            catch( Exception ex )
            {
                SynapseNodeService.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        public void Undrainstop()
        {
            string context = GetContext( nameof( Undrainstop ));

            try
            {
                SynapseNodeService.Logger.Debug( context );
                SynapseNodeService.Logger.Info( $"Undrainstop starting, CurrentQueueDepth: {_scheduler.CurrentQueueDepth}" );
                _scheduler.Undrainstop();
                SynapseNodeService.Logger.Info( $"Undrainstop complete, CurrentQueueDepth: {_scheduler.CurrentQueueDepth}" );
            }
            catch( Exception ex )
            {
                SynapseNodeService.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        public bool GetIsDrainstopComplete()
        {
            string context = GetContext( nameof( GetIsDrainstopComplete ) );

            try
            {
                SynapseNodeService.Logger.Debug( context );
                return _scheduler.IsDrainstopComplete;
            }
            catch( Exception ex )
            {
                SynapseNodeService.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        public int GetCurrentQueueDepth()
        {
            string context = GetContext( nameof( GetCurrentQueueDepth ) );

            try
            {
                SynapseNodeService.Logger.Debug( context );
                return _scheduler.CurrentQueueDepth;
            }
            catch( Exception ex )
            {
                SynapseNodeService.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        public List<string> GetCurrentQueueItems()
        {
            string context = GetContext( nameof( GetCurrentQueueItems ) );

            try
            {
                SynapseNodeService.Logger.Debug( context );
                return _scheduler.CurrentQueue;
            }
            catch( Exception ex )
            {
                SynapseNodeService.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }
        #endregion


        string GetContext(string context, params object[] parms)
        {
            StringBuilder c = new StringBuilder();
            c.Append( $"{context}(" );
            for( int i = 0; i < parms.Length; i += 2 )
                c.Append( $"{parms[i]}: {parms[i + 1]}, " );

            return $"{c.ToString().TrimEnd( ',', ' ' )})";
        }
    }
}