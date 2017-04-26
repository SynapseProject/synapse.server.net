using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using Owin;

namespace Synapse.Services
{
    public class WebServerConfig
    {
        public void Configuration(IAppBuilder app)
        {
            HttpListener listener = (HttpListener)app.Properties["System.Net.HttpListener"];
            listener.AuthenticationSchemes = SynapseServer.Config.AuthenticationScheme;

            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();

            if( SynapseServer.Config.ServerIsController && SynapseServer.Config.Controller.HasAssemblies )
                config.Services.Replace( typeof( IAssembliesResolver ), new CustomAssembliesResolver() );

            // Web API configuration and services
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "synapse/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            config.MessageHandlers
                   .Add( new RawContentHandler() );

            var appXmlType = config.Formatters.XmlFormatter.SupportedMediaTypes.FirstOrDefault( t => t.MediaType == "application/xml" );
            config.Formatters.XmlFormatter.SupportedMediaTypes.Remove( appXmlType );

            app.UseWebApi( config );
        }
    }
}