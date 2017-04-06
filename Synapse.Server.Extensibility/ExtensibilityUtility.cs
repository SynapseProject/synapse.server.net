using System;
using System.Reflection;

namespace Synapse.Services
{
    public class ExtensibilityUtility
    {
        public static IExecuteController GetExecuteControllerInstance()
        {
            Assembly a = Assembly.Load( "Synapse.Server" );
            Type t = a.GetType( "Synapse.Services.ExecuteController", true );
            return Activator.CreateInstance( t ) as IExecuteController;
        }
    }
}