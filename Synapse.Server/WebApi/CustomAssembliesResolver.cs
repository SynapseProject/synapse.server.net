using System;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Http.Dispatcher;

namespace Synapse.Services
{
    public class CustomAssembliesResolver : DefaultAssembliesResolver
    {
        public override ICollection<Assembly> GetAssemblies()
        {
            ICollection<Assembly> baseAssemblies = base.GetAssemblies();
            List<Assembly> assemblies = new List<Assembly>( baseAssemblies );

            foreach( string assm in SynapseServer.Config.Controller.Assemblies.Keys )
            {
                Assembly a = Assembly.Load( assm );
                assemblies.Add( a );
            }

            return assemblies;
        }
    }
}