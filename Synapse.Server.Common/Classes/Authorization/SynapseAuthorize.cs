using System;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace Synapse.Services
{
    public class SynapseAuthorize : AuthorizeAttribute
    {
        public SynapseAuthorize() : base() { }
        public SynapseAuthorize(ServerRole serverRole, string topic = null) : base()
        {
            ServerRole = serverRole;
            Topic = topic;
        }
        public SynapseAuthorize(string serverRole, string topic = null) : base()
        {
            ServerRole = (ServerRole)Enum.Parse( typeof( ServerRole ), serverRole, true );
            Topic = topic;
        }


        public ServerRole ServerRole { get; set; } = ServerRole.Universal;
        public string Topic { get; set; }  = null;


        public bool IsAuthorizedWrapper(HttpActionContext actionContext)
        {
            return IsAuthorized( actionContext );
        }

        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            bool? ok = ServerGlobal.Config.WebApi.Authorization?.HasProviders;
            if( ok.HasValue && ok.Value )
                return ServerGlobal.Config.WebApi.Authorization.IsAuthorized( actionContext.RequestContext.Principal?.Identity?.Name, ServerRole, Topic );
            else
                return true;
        }
    }
}