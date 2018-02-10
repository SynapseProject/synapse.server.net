using System;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace Synapse.Services
{
    public class SynapseAuthorize : AuthorizeAttribute
    {
        ServerRole _serverRole = ServerRole.Universal;

        public SynapseAuthorize(ServerRole serverRole) : base()
        {
            _serverRole = serverRole;
        }

        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            bool? ok = SynapseServer.Config.WebApi.Authorization?.HasProviders;
            if( ok.HasValue && ok.Value )
                return SynapseServer.Config.WebApi.Authorization.IsAuthorized( actionContext.RequestContext.Principal?.Identity?.Name, _serverRole );
            else
                return true;

            //string[] admins = "LAPTOP-TK2D9TB6\\steve".Split( ',' );

            //if( admins.Contains( actionContext.RequestContext.Principal.Identity.Name ) )
            //    return true;

            //return false;
        }
    }
}