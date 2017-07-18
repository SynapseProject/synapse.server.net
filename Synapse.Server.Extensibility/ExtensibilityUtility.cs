using System;
using System.Reflection;
using System.Security.Principal;
using System.Web.Http.Routing;
using System.Net.Http.Headers;

namespace Synapse.Services
{
    public class ExtensibilityUtility
    {
        public static IExecuteController GetExecuteControllerInstance(UrlHelper currentUrl, IPrincipal currentUser, AuthenticationHeaderValue authenticationHeader)
        {
            Assembly a = Assembly.Load( "Synapse.Server" );
            Type t = a.GetType( "Synapse.Services.ExecuteController", true );
            IExecuteController c = Activator.CreateInstance( t ) as IExecuteController;
            if( c != null )
            {
                c.CurrentUrl = currentUrl;
                c.CurrentUser = currentUser;
                c.AuthenticationHeader = authenticationHeader;
            }
            return c;
        }
    }
}