using System;
using System.Collections.Generic;
using System.IO;

using Synapse.ControllerService.Common;
using Synapse.Core;
using Synapse.Core.Utilities;

namespace Synapse.ControllerService.Dal
{
    public class DiskDal : IControllerDal
    {
        static readonly string CurrentPath = $"{Path.GetDirectoryName( typeof( DiskDal ).Assembly.Location )}";

        string _planPath = null;
        string _histPath = null;

        public DiskDal()
        {
            _planPath = $"{CurrentPath}\\Plans\\";
            _histPath = $"{CurrentPath}\\History\\";
        }

        public DiskDal(string basePath)
        {
            _planPath = $"{basePath}\\Plans\\";
            _histPath = $"{basePath}\\History\\";
        }


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
            int maxRetry = 3;
            int currentRetry = 0;

            while( currentRetry < maxRetry )
            {
                try
                {
                    YamlHelpers.SerializeFile( $"{_histPath}{plan.UniqueName}_{plan.InstanceId}.yaml",
                        plan, emitDefaultValues: true );

                    break;
                }
                catch( Exception ex )
                {
                    System.Threading.Thread.Sleep( 100 );
                    currentRetry++;
                }
            }
        }

        public void UpdatePlanActionStatus(string planUniqueName, long planInstanceId, ActionItem actionItem)
        {
            Plan plan = GetPlanStatus( planUniqueName, planInstanceId );
            UpdateAction( plan.Actions, actionItem );
            UpdatePlanStatus( plan );
        }

        bool UpdateAction(List<ActionItem> actions, ActionItem item)
        {
            bool found = false;

            for( int i = 0; i < actions.Count; i++ )
            {
                ActionItem a = actions[i];

                if( a.InstanceId == item.InstanceId )
                {
                    actions[i] = item;
                    found = true;
                    break;
                }

                if( a.HasActionGroup )
                {
                    if( a.ActionGroup.InstanceId == item.InstanceId )
                    {
                        a.ActionGroup = item;
                        found = true;
                        break;
                    }

                    if( a.ActionGroup.HasActions )
                        found = UpdateAction( a.ActionGroup.Actions, item );
                }

                if( found ) break;

                if( a.HasActions )
                    found = UpdateAction( a.Actions, item );

                if( found ) break;
            }

            return found;
        }
    }
}