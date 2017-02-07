using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Synapse.Common;
using Synapse.Core;
using Synapse.Core.Runtime;

namespace Synapse.Services
{
    public class PlanRuntimePod : IPlanRuntimeContainer
    {
        LogUtility _log = new LogUtility();
        DirectoryInfo _logRootPath = null;
        bool _wantsCancel = false;
        string _ticks = null;
        ControllerServiceHttpApiClient _controllerService =
            new ControllerServiceHttpApiClient( SynapseServer.Config.Node.ControllerUrl );

        public PlanRuntimePod(Plan plan, bool isDryRun = false, Dictionary<string, string> dynamicData = null, long planInstanceId = 0)
        {
            Plan = plan;
            IsDryRun = isDryRun;
            DynamicData = dynamicData;
            PlanInstanceId = planInstanceId;

            _ticks = $"{planInstanceId}_{DateTime.Now.Ticks}";

            InitializeLogger();

            Plan.Progress += Plan_Progress;
            Plan.LogMessage += Plan_LogMessage;
        }

        public void InitializeLogger()
        {
            string logFilePath = null;
            try
            {
                string logFileName = $"{_ticks}_{Plan.Name}";
                _logRootPath = Directory.CreateDirectory( SynapseServer.Config.Node.GetResolvedAuditLogRootPath() );
                logFilePath = $"{_logRootPath.FullName}\\{logFileName}.log";
                _log.InitDynamicFileAppender( logFileName, logFileName, logFilePath, SynapseServer.Config.Node.Log4NetConversionPattern, "all" );
            }
            catch( Exception ex )
            {
                throw new FileNotFoundException( $"Could not find/acceess log file: {logFilePath}, AuditLogRootPath: {SynapseServer.Config.Node.AuditLogRootPath}", ex );
            }
        }

        public void Start(CancellationToken token, Action<IPlanRuntimeContainer> callback)
        {
            token.Register( () => CancelPlanExecution() );
            Plan.Start( DynamicData, IsDryRun );

            SynapseServer.Logger.Info( $"SerializeResultPlan: {SynapseServer.Config.Node.SerializeResultPlan}, {_logRootPath.FullName}\\{_ticks}_{Plan.Name}.result.yaml" );
            if( SynapseServer.Config.Node.SerializeResultPlan )
                File.WriteAllText( $"{_logRootPath.FullName}\\{_ticks}_{Plan.Name}.result.yaml", Plan.ResultPlan.ToYaml() );

            //send final message home
            _controllerService.SetPlanStatusAsync( Plan.Name, PlanInstanceId, Plan.ResultPlan );

            callback?.Invoke( this );
        }

        private void CancelPlanExecution()
        {
            _wantsCancel = true;
        }

        public Plan Plan { get; }
        public bool IsDryRun { get; }
        public Dictionary<string, string> DynamicData { get; }
        public long PlanInstanceId { get; }


        private void Plan_Progress(object sender, HandlerProgressCancelEventArgs e)
        {
            if( _wantsCancel )
                e.Cancel = true;

            //send intermediate message home
            _controllerService.SetPlanActionStatusAsync( Plan.Name, PlanInstanceId, e.ToActionItem() );
        }

        private void Plan_LogMessage(object sender, LogMessageEventArgs e)
        {
            _log.Write( e.SerializeSimple() );
        }
    }
}