using System;
using System.Reflection;
using System.Security.Principal;
using System.Web.Http.Routing;

namespace Synapse.Services
{
    public class ExtensibilityUtility
    {
        public static IExecuteController GetExecuteControllerInstance(UrlHelper currentUrl, IPrincipal currentUser)
        {
            Assembly a = Assembly.Load( "Synapse.Server" );
            Type t = a.GetType( "Synapse.Services.ExecuteController", true );
            IExecuteController c = Activator.CreateInstance( t ) as IExecuteController;
            if( c != null )
            {
                c.CurrentUrl = currentUrl;
                c.CurrentUser = currentUser;
            }
            return c;
        }
    }
}