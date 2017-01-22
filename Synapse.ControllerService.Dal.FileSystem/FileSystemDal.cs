using System;
using System.IO;

using Synapse.Core;
using Synapse.Core.Utilities;

namespace Synapse.ControllerService.Dal
{
    public class FileSystemDal : IControllerDal
    {
        static readonly string CurrentPath = $"{Path.GetDirectoryName( typeof( FileSystemDal ).Assembly.Location )}";

        string _planPath = null;
        string _histPath = null;

        public FileSystemDal()
        {
            _planPath = $"{CurrentPath}\\Plans\\";
            _histPath = $"{CurrentPath}\\History\\";

            EnsurePaths();

            ProcessPlansOnSingleton = false;
            ProcessActionsOnSingleton = true;
        }

        public FileSystemDal(string basePath, bool processPlansOnSingleton = false, bool processActionsOnSingleton = true) : this()
        {
            _planPath = $"{basePath}\\Plans\\";
            _histPath = $"{basePath}\\History\\";

            EnsurePaths();

            ProcessPlansOnSingleton = processPlansOnSingleton;
            ProcessActionsOnSingleton = processActionsOnSingleton;
        }

        void EnsurePaths()
        {
            Directory.CreateDirectory( _planPath );
            Directory.CreateDirectory( _histPath );
        }


        public bool ProcessPlansOnSingleton { get; set; }
        public bool ProcessActionsOnSingleton { get; set; }


        public Plan GetPlan(string planUniqueName)
        {
            string planFile = $"{_planPath}{planUniqueName}.yaml";
            return YamlHelpers.DeserializeFile<Plan>( planFile );
        }

        public Plan GetPlanStatus(string planUniqueName, long planInstanceId)
        {
            string planFile = $"{_histPath}{planUniqueName}_{planInstanceId}.yaml";
            return YamlHelpers.DeserializeFile<Plan>( planFile );
        }

        public void UpdatePlanStatus(Plan plan)
        {
            PlanUpdateItem item = new PlanUpdateItem() { Plan = plan };

            if( ProcessActionsOnSingleton )
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
                Utilities.FindActionAndReplace( plan.Actions, item.ActionItem );
                UpdatePlanStatus( plan );
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