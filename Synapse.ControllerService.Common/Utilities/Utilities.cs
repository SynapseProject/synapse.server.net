using System;
using System.Collections.Generic;

using Synapse.Core;

namespace Synapse.ControllerService.Dal
{
    public class Utilities
    {
        public static string GetActionInstanceMaterialzedPath(long actionInstanceIdToFind, List<ActionItem> actions, string path = "")
        {
            int i = 0;
            foreach( ActionItem a in actions )
            {
                if( a.InstanceId == actionInstanceIdToFind )
                {
                    path = $"{path}.Actions.{i}";
                }
                else
                {
                    if( a.HasActionGroup )
                    {
                        if( a.ActionGroup.InstanceId == actionInstanceIdToFind )
                            path += "ActionGroup";
                        else if( a.ActionGroup.HasActions )
                            path = GetActionInstanceMaterialzedPath( actionInstanceIdToFind, a.ActionGroup.Actions, path );
                    }

                    if( a.HasActions )
                        path = GetActionInstanceMaterialzedPath( actionInstanceIdToFind, a.Actions, $"{path}.Actions.{i}" );
                }

                i++;
            }

            return path.TrimStart( '.' );
        }

        public static bool FindActionAndReplace(List<ActionItem> actions, ActionItem item)
        {
            bool found = false;

            for( int i = 0; i < actions.Count; i++ )
            {
                ActionItem a = actions[i];

                if( a.InstanceId == item.InstanceId )
                {
                    //only replace if item has higher Result.Status
                    if( a.Result.Status < item.Result.Status )
                        actions[i] = item;

                    found = true;
                    break;
                }

                if( a.HasActionGroup )
                {
                    if( a.ActionGroup.InstanceId == item.InstanceId )
                    {
                        //only replace if item has higher Result.Status
                        if( a.ActionGroup.Result.Status < item.Result.Status )
                            a.ActionGroup = item;

                        found = true;
                        break;
                    }

                    if( a.ActionGroup.HasActions )
                        found = FindActionAndReplace( a.ActionGroup.Actions, item );
                }

                if( found ) break;

                if( a.HasActions )
                    found = FindActionAndReplace( a.Actions, item );

                if( found ) break;
            }

            return found;
        }
    }
}