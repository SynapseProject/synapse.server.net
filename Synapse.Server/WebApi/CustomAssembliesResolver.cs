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

            foreach( CustomAssemblyConfig assm in ServerGlobal.Config.Controller.Assemblies )
            {
                Assembly a = Assembly.Load( assm.Name );
                assemblies.Add( a );
            }

            return assemblies;
        }
    }
}