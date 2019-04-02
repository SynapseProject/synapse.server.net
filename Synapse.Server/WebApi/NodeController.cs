using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Principal;
using System.Web.Http;

using Synapse.Common.WebApi;
using Synapse.Core;
using Synapse.Core.Utilities;
using Synapse.Common;

namespace Synapse.Services
{
    [SynapseAuthorize( serverRole: ServerRole.Node )]
    [RoutePrefix( "synapse/node" )]
    public class NodeController : ApiController
    {
        static PlanTaskScheduler _scheduler = null;

        public NodeController()
        {
            IsNodeOrException();
            InitPlanScheduler();
        }

        void IsNodeOrException()
        {
            Exception ex = null;

            if( !ServerGlobal.Config.Service.IsRoleNode )
                ex = new NotSupportedException( $"This instance of Synapse is not configured as a Node.  Check the settings at {SynapseServerConfig.FileName}." );
            else if( ServerGlobal.Config.Node == null )
                ex = new Exception( $"This instance of Synapse is missing required configuration to execute as a Node.  Check the settings at {SynapseServerConfig.FileName}." );

            if( ex != null )
            {
                ServerGlobal.Logger.Fatal( ex.Message, ex );
                throw ex;
            }
        }

        public static Action DrainstopCallback { get; set; }

        public static void InitPlanScheduler()
        {
            if( _scheduler == null )
            {
                _scheduler = new PlanTaskScheduler( ServerGlobal.Config.Node.MaxServerThreads );
                _scheduler.PlanCompleted += Scheduler_PlanCompleted;
                ServerGlobal.Logger.Info( $"Initialized PlanScheduler, MaxThreads: {ServerGlobal.Config.Node.MaxServerThreads}" );
            }
        }



        [HttpGet]
        [Route( "hello" )]
        public string Hello(bool log = true)
        {
            string context = GetContext( nameof( Hello ) );

            try
            {
                if( log )
                    ServerGlobal.Logger.Debug( context );
                return "Hello from SynapseNode, World!";
            }
            catch( Exception ex )
            {
                ServerGlobal.Logger.Error(
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
                ServerGlobal.Logger.Debug( context );
                return CurrentUser;
            }
            catch( Exception ex )
            {
                ServerGlobal.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planInstanceId}/" )]
        [HttpPost]
        public void StartPlanAsync(long planInstanceId, bool dryRun, [FromBody]string planString)
        {
            Uri uri = this.Url.Request.RequestUri;
            planString = CryptoHelpers.Decode( planString );
            Plan plan = Plan.FromYaml( new StringReader( planString ) );

            string context = GetContext( nameof( StartPlanAsync ),
                nameof( plan ), plan.Name, nameof( dryRun ), dryRun, nameof( planInstanceId ), planInstanceId, "QueryString", uri.Query );

            Impersonator runAsUser = null;
            try
            {
                ServerGlobal.Logger.Debug( context );
                plan.InstanceId = planInstanceId;

                ValidatePlanSignature( plan );

                Dictionary<string, string> dynamicParameters = uri.ParseQueryString();
                if( dynamicParameters.ContainsKey( nameof( dryRun ) ) ) dynamicParameters.Remove( nameof( dryRun ) );
                PlanRuntimePod p = new PlanRuntimePod( plan, dryRun, dynamicParameters, plan.InstanceId, this.Url.Request.Headers.Referrer, this.Request?.Headers?.Authorization );

                if( SynapseServer.UseImpersonation( User?.Identity ) )
                {
                    if( Request?.Headers?.Authorization?.Scheme?.ToLower() == "basic" )
                        runAsUser = new Impersonator( Request.Headers.Authorization );
                    else
                        runAsUser = new Impersonator( (WindowsIdentity)User.Identity );

                    ServerGlobal.Logger.Info( $"Impersonation Started.  Now Running As User [{Impersonator.WhoAmI().Name}]." );

                    WindowsIdentity.RunImpersonated( runAsUser.Identity.AccessToken, () =>
                    {
                        _scheduler.StartPlan( p );
                    } );
                }
                else
                    _scheduler.StartPlan( p );
            }
            catch( Exception ex )
            {
                ServerGlobal.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
            finally
            {
                if( runAsUser != null )
                {
                    runAsUser.Logoff();
                    ServerGlobal.Logger.Info( $"Impersonation Stopped.  Now Running As User [{Impersonator.WhoAmI().Name}]." );
                }
            }
        }

        [Route( "{planInstanceId}/p/" )]
        [HttpPost]
        public void StartPlanAsyncWithParametersAsPost(long planInstanceId, bool dryRun, [FromBody]string planString)
        {
            StartPlanEnvelope planEnvelope = StartPlanEnvelope.FromYaml( planString, isEncoded: true );
            Plan plan = planEnvelope.Plan;

            string context = GetContext( nameof( StartPlanAsyncWithParametersAsPost ),
                nameof( plan ), plan.Name, nameof( dryRun ), dryRun, nameof( planInstanceId ), planInstanceId );

            Impersonator runAsUser = null;
            try
            {
                ServerGlobal.Logger.Debug( context );
                plan.InstanceId = planInstanceId;

                ValidatePlanSignature( plan );

                PlanRuntimePod p = new PlanRuntimePod( plan, dryRun, planEnvelope.GetCaseInsensitiveDynamicParameters(), plan.InstanceId, this.Url.Request.Headers.Referrer, this.Request?.Headers?.Authorization );

                if( SynapseServer.UseImpersonation( User?.Identity ) )
                {
                    if( Request?.Headers?.Authorization?.Scheme?.ToLower() == "basic" )
                        runAsUser = new Impersonator( Request.Headers.Authorization );
                    else
                        runAsUser = new Impersonator( (WindowsIdentity)User.Identity );

                    ServerGlobal.Logger.Info( $"Impersonation Started.  Now Running As User [{Impersonator.WhoAmI().Name}]." );

                    WindowsIdentity.RunImpersonated( runAsUser.Identity.AccessToken, () =>
                    {
                        _scheduler.StartPlan( p );
                    } );
                }
                else
                    _scheduler.StartPlan( p );
            }
            catch( Exception ex )
            {
                ServerGlobal.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
            finally
            {
                if( runAsUser != null )
                {
                    runAsUser.Logoff();
                    ServerGlobal.Logger.Info( $"Impersonation Stopped.  Now Running As User [{Impersonator.WhoAmI().Name}]." );
                }
            }
        }

        void ValidatePlanSignature(Plan plan)
        {
            if( ServerGlobal.Config.Node.ValidatePlanSignature )
            {
                ServerGlobal.Logger.Debug( $"Checking Plan signature on {plan.Name}/{plan.InstanceId}." );

                if( !File.Exists( ServerGlobal.Config.Signature.KeyUri ) )
                    throw new FileNotFoundException( ServerGlobal.Config.Signature.KeyUri );

                if( !plan.VerifySignature( ServerGlobal.Config.Signature.KeyContainerName, ServerGlobal.Config.Signature.KeyUri, ServerGlobal.Config.Signature.CspProviderFlags ) )
                    throw new System.Security.SecurityException( $"Plan signature validation failed on {plan.Name}/{plan.InstanceId}." );
                else
                    ServerGlobal.Logger.Debug( $"Plan signature validation succeeded on {plan.Name}/{plan.InstanceId}." );
            }
        }

        private static void Scheduler_PlanCompleted(object sender, PlanCompletedEventArgs e)
        {
            ServerGlobal.Logger.Info( $"Plan Completed: InstanceId: {e.PlanContainer.PlanInstanceId}, Name: {e.PlanContainer.Plan.Name}" );  //, At: {e.TimeCompleted}
        }

        [Route( "{planInstanceId}/" )]
        [HttpDelete]
        public void CancelPlan(long planInstanceId)
        {
            string context = GetContext( nameof( CancelPlan ), nameof( planInstanceId ), planInstanceId );

            try
            {
                ServerGlobal.Logger.Debug( context );
                bool found = _scheduler.CancelPlan( planInstanceId );
                string foundMsg = found ?
                    "Found executing Plan and signaled Cancel request." :
                    "Could not find executing Plan; Plan may have already completed execution.";
                ServerGlobal.Logger.Info( $"CancelPlan {planInstanceId}: {foundMsg}" );
            }
            catch( Exception ex )
            {
                ServerGlobal.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }


        #region drainstop
        [SynapseAuthorize( serverRole: ServerRole.Node, topic: "Drainstop" )]
        [HttpGet]
        [Route( "drainstop/" )]
        public void Drainstop(bool shutdown = true)
        {
            string context = GetContext( nameof( Drainstop ), nameof( shutdown ), shutdown );

            try
            {
                ServerGlobal.Logger.Debug( context );
                ServerGlobal.Logger.Info( $"Drainstop starting, CurrentQueueDepth: {_scheduler.CurrentQueueDepth}.  Shutdown when complete: {shutdown}." );
                _scheduler.Drainstop();
                ServerGlobal.Logger.Info( $"Drainstop complete, CurrentQueueDepth: {_scheduler.CurrentQueueDepth}" );
                if( shutdown && _scheduler.IsDrainstopped )
                {
                    ServerGlobal.Logger.Info( $"Drainstop complete, initiating Shutdown." );
                    DrainstopCallback?.Invoke();
                }
            }
            catch( Exception ex )
            {
                ServerGlobal.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [SynapseAuthorize( serverRole: ServerRole.Node, topic: "Drainstop" )]
        [HttpGet]
        [Route( "drainstop/cancel" )]
        public void CancelDrainstop()
        {
            string context = GetContext( nameof( CancelDrainstop ) );

            try
            {
                ServerGlobal.Logger.Debug( context );
                ServerGlobal.Logger.Info( $"Undrainstop starting, CurrentQueueDepth: {_scheduler.CurrentQueueDepth}" );
                _scheduler.CancelDrainstop();
                ServerGlobal.Logger.Info( $"Undrainstop complete, CurrentQueueDepth: {_scheduler.CurrentQueueDepth}" );
            }
            catch( Exception ex )
            {
                ServerGlobal.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [SynapseAuthorize( serverRole: ServerRole.Node, topic: "Drainstop" )]
        [HttpGet]
        [Route( "drainstop/iscomplete" )]
        public bool GetIsDrainstopComplete()
        {
            string context = GetContext( nameof( GetIsDrainstopComplete ) );

            try
            {
                ServerGlobal.Logger.Debug( context );
                return _scheduler.IsDrainstopComplete;
            }
            catch( Exception ex )
            {
                ServerGlobal.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [SynapseAuthorize( serverRole: ServerRole.Node, topic: "Drainstop" )]
        [HttpGet]
        [Route( "queue/count" )]
        public int GetCurrentQueueDepth()
        {
            string context = GetContext( nameof( GetCurrentQueueDepth ) );

            try
            {
                ServerGlobal.Logger.Debug( context );
                return _scheduler.CurrentQueueDepth;
            }
            catch( Exception ex )
            {
                ServerGlobal.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [SynapseAuthorize( serverRole: ServerRole.Node, topic: "Drainstop" )]
        [HttpGet]
        [Route( "queue" )]
        public List<string> GetCurrentQueueItems()
        {
            string context = GetContext( nameof( GetCurrentQueueItems ) );

            try
            {
                ServerGlobal.Logger.Debug( context );
                return _scheduler.CurrentQueue;
            }
            catch( Exception ex )
            {
                ServerGlobal.Logger.Error(
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