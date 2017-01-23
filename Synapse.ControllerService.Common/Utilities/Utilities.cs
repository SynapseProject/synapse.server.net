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
            ActionItem parentItem = null;
            bool found = FindActionAndReplace( actions, item, item.ParentInstanceId, ref parentItem );
            if( !found )
            {
                if( parentItem != null )
                {
                    parentItem.Actions.Add( item );
                    found = true;
                }
                else if( item.ParentInstanceId == 0 )
                {
                    actions.Add( item );
                    found = true;
                }
            }

            return found;
        }
        //todo: added code around parent stuff to fix a bug, now can make this more efficient by using parent info to go directly to child Actions collection
        internal static bool FindActionAndReplace(List<ActionItem> actions, ActionItem item, long actionItemParentInstanceId, ref ActionItem actionItemParent)
        {
            bool found = false;

            for( int i = 0; i < actions.Count; i++ )
            {
                ActionItem a = actions[i];

                if( a.InstanceId == actionItemParentInstanceId )
                    actionItemParent = a;

                if( a.InstanceId == item.InstanceId )
                {
                    //only replace if item has higher Result.Status
                    if( a.Result.Status < item.Result.Status )
                    {
                        item.Actions = a.Actions;
                        actions[i] = item;
                    }

                    found = true;
                    break;
                }

                if( a.HasActionGroup )
                {
                    if( a.ActionGroup.InstanceId == actionItemParentInstanceId )
                        actionItemParent = a;

                    if( a.ActionGroup.InstanceId == item.InstanceId )
                    {
                        //only replace if item has higher Result.Status
                        if( a.ActionGroup.Result.Status < item.Result.Status )
                        {
                            item.Actions = a.ActionGroup.Actions;
                            a.ActionGroup = item;
                        }

                        found = true;
                        break;
                    }

                    if( a.ActionGroup.HasActions )
                        found = FindActionAndReplace( a.ActionGroup.Actions, item, actionItemParentInstanceId, ref actionItemParent );
                }

                if( found ) break;

                if( a.HasActions )
                    found = FindActionAndReplace( a.Actions, item, actionItemParentInstanceId, ref actionItemParent );

                if( found ) break;
            }

            return found;
        }
    }
}