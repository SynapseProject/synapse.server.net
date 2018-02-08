using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace Synapse.Services
{
    public class AdminAuthorizer : AuthorizeAttribute
    {
        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            return SynapseServer.Config.WebApi.AdminAuthorization.HasAccess( actionContext.RequestContext.Principal?.Identity?.Name );

            //string[] admins = "LAPTOP-TK2D9TB6\\steve".Split( ',' );

            //if( admins.Contains( actionContext.RequestContext.Principal.Identity.Name ) )
            //    return true;

            //return false;
        }
    }
}