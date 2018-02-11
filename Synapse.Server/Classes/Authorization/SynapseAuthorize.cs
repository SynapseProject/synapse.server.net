using System;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace Synapse.Services
{
    public class SynapseAuthorize : AuthorizeAttribute
    {
        ServerRole _serverRole = ServerRole.Universal;
        string _topic = null;

        public SynapseAuthorize(ServerRole serverRole, string topic = null) : base()
        {
            _serverRole = serverRole;
            _topic = topic;
        }

        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            bool? ok = SynapseServer.Config.WebApi.Authorization?.HasProviders;
            if( ok.HasValue && ok.Value )
                return SynapseServer.Config.WebApi.Authorization.IsAuthorized( actionContext.RequestContext.Principal?.Identity?.Name, _serverRole, _topic );
            else
                return true;
        }
    }
}