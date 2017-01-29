using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Suplex.Security;
using Synapse.Core;
using Synapse.Core.Utilities;


namespace Synapse.Services.Controller.Dal
{
    public partial class FileSystemDal : IControllerDal
    {
        static readonly string CurrentPath = $"{Path.GetDirectoryName( typeof( FileSystemDal ).Assembly.Location )}";

        string _planPath = null;
        string _histPath = null;
        string _splxPath = null;

        SuplexDal _splxDal = null;

        //this is a stub feature
        static long PlanInstanceIdCounter = DateTime.Now.Ticks;

        public FileSystemDal()
        {
            _planPath = $"{CurrentPath}\\Plans\\";
            _histPath = $"{CurrentPath}\\History\\";
            _splxPath = $"{CurrentPath}\\Security\\";

            EnsurePaths();

            ProcessPlansOnSingleton = false;
            ProcessActionsOnSingleton = true;

            LoadSuplex();
        }

        public FileSystemDal(string basePath, bool processPlansOnSingleton = false, bool processActionsOnSingleton = true) : this()
        {
            if( string.IsNullOrWhiteSpace( basePath ) )
                basePath = CurrentPath;

            _planPath = $"{basePath}\\Plans\\";
            _histPath = $"{basePath}\\History\\";
            _splxPath = $"{basePath}\\Security\\";

            EnsurePaths();

            ProcessPlansOnSingleton = processPlansOnSingleton;
            ProcessActionsOnSingleton = processActionsOnSingleton;

            LoadSuplex();
        }

        void EnsurePaths()
        {
            Directory.CreateDirectory( _planPath );
            Directory.CreateDirectory( _histPath );
        }

        void LoadSuplex()
        {
            string splx = $"{_splxPath}security.splx";
            if( File.Exists( splx ) )
                _splxDal = new SuplexDal( splx );
        }


        public bool ProcessPlansOnSingleton { get; set; }
        public bool ProcessActionsOnSingleton { get; set; }


        public IEnumerable<string> GetPlanList()
        {
            return new string[] { "Hello,", "World,", "from", "FileSystemDal!" };
        }

        public IEnumerable<long> GetPlanInstanceIdList(string planUniqueName)
        {
            return new long[] { 1, 2, 3 };
        }

        public Plan GetPlan(string planUniqueName)
        {
            _splxDal?.TrySecurityOrException( planUniqueName, AceType.FileSystem, FileSystemRight.Execute, "Plan" );

            string planFile = $"{_planPath}{planUniqueName}.yaml";
            return YamlHelpers.DeserializeFile<Plan>( planFile );
        }

        public Plan CreatePlanInstance(string planUniqueName)
        {
            string planFile = $"{_planPath}{planUniqueName}.yaml";
            Plan plan = YamlHelpers.DeserializeFile<Plan>( planFile );
            plan.InstanceId = PlanInstanceIdCounter++;
            return plan;
        }

        public Plan GetPlanStatus(string planUniqueName, long planInstanceId)
        {
            string planFile = $"{_histPath}{planUniqueName}_{planInstanceId}.yaml";
            return YamlHelpers.DeserializeFile<Plan>( planFile );
        }

        public void UpdatePlanStatus(Plan plan)
        {
            PlanUpdateItem item = new PlanUpdateItem() { Plan = plan };

            if( ProcessPlansOnSingleton )
                PlanItemSingletonProcessor.Instance.Queue.Enqueue( item );
            else
                UpdatePlanStatus( item );
        }

        public void UpdatePlanStatus(PlanUpdateItem item)
        {
            try
            {
                YamlHelpers.SerializeFile( $"{_histPath}{item.Plan.UniqueName}_{item.Plan.InstanceId}.yaml",
                    item.Plan, emitDefaultValues: true );
            }
            catch( Exception ex )
            {
                PlanItemSingletonProcessor.Instance.Exceptions.Enqueue( ex );

                if( item.RetryAttempts++ < 5 )
                    PlanItemSingletonProcessor.Instance.Queue.Enqueue( item );
                else
                    PlanItemSingletonProcessor.Instance.Fatal.Enqueue( ex );
            }
        }

        public void UpdatePlanActionStatus(string planUniqueName, long planInstanceId, ActionItem actionItem)
        {
            ActionUpdateItem item = new ActionUpdateItem()
            {
                PlanUniqueName = planUniqueName,
                PlanInstanceId = planInstanceId,
                ActionItem = actionItem
            };

            if( ProcessActionsOnSingleton )
                ActionItemSingletonProcessor.Instance.Queue.Enqueue( item );
            else
                UpdatePlanActionStatus( item );
        }

        public void UpdatePlanActionStatus(ActionUpdateItem item)
        {
            try
            {
                Plan plan = GetPlanStatus( item.PlanUniqueName, item.PlanInstanceId );
                bool ok = DalUtilities.FindActionAndReplace( plan.Actions, item.ActionItem );
                if( ok )
                    YamlHelpers.SerializeFile( $"{_histPath}{plan.UniqueName}_{plan.InstanceId}.yaml", plan, emitDefaultValues: true );
                else
                    throw new Exception( $"Could not find Plan.InstanceId = [{item.PlanInstanceId}], Action:{item.ActionItem.Name}.ParentInstanceId = [{item.ActionItem.ParentInstanceId}] in Plan outfile." );
            }
            catch( Exception ex )
            {
                ActionItemSingletonProcessor.Instance.Exceptions.Enqueue( ex );

                if( item.RetryAttempts++ < 5 )
                    ActionItemSingletonProcessor.Instance.Queue.Enqueue( item );
                else
                    ActionItemSingletonProcessor.Instance.Fatal.Enqueue( ex );
            }
        }
    }
}