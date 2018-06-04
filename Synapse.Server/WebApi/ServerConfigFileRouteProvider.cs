using System;
using System.Web.Http.Routing;
using System.Web.Http.Controllers;
using System.Collections.Generic;

namespace Synapse.Services
{
    public class ServerConfigFileRouteProvider : DefaultDirectRouteProvider
    {
        private Dictionary<string, string> CustomRoutePrefix = null;

        protected override string GetRoutePrefix(HttpControllerDescriptor controllerDescriptor)
        {
            string prefix = base.GetRoutePrefix( controllerDescriptor );

            if( SynapseServer.Config.Service.IsRoleController && SynapseServer.Config.Controller.HasAssemblies )
            {
                if( CustomRoutePrefix == null )
                {
                    // Load Custom Route Prefixes From Config
                    CustomRoutePrefix = new Dictionary<string, string>();
                    foreach( CustomAssemblyConfig assConfig in SynapseServer.Config.Controller.Assemblies )
                    {
                        if( !CustomRoutePrefix.ContainsKey( assConfig.Name ) )
                        {
                            CustomRoutePrefix.Add( assConfig.Name, assConfig.RoutePrefix );
                            if( !String.IsNullOrWhiteSpace( assConfig.RoutePrefix ) )
                                SynapseServer.Logger.Debug( $"Custom Route Prefix [{assConfig.RoutePrefix}] Added For [{assConfig.Name}]." );
                        }
                    }
                }

                string assemblyName = controllerDescriptor.ControllerType.Assembly.FullName;
                assemblyName = assemblyName.Substring( 0, assemblyName.IndexOf( "," ) );

                if( CustomRoutePrefix.ContainsKey( assemblyName ) )
                    if( CustomRoutePrefix[assemblyName] != null )
                        prefix = CustomRoutePrefix[assemblyName].ToString();
            }

            return prefix;
        }
    }
}