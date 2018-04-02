using System;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace Synapse.Services
{
    public class SynapseCustomAuthorize : AuthorizeAttribute
    {
        string _topic = null;

        public SynapseCustomAuthorize(string topic = null) : base()
        {
            _topic = topic;
        }

        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            Assembly a = Assembly.Load( "Synapse.Server" );
            Type t = a.GetType( "Synapse.Services.SynapseAuthorize", true );
            AuthorizeAttribute aatt = Activator.CreateInstance( t, "Custom", _topic ) as AuthorizeAttribute;
            MethodInfo isAuthed = t.GetMethod( "IsAuthorizedWrapper", new Type[] { typeof( HttpActionContext ) } );
            return (bool)isAuthed.Invoke( aatt, new object[] { actionContext } );
        }
    }
}