using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using Owin;

namespace Synapse.Services
{
    public class WebServerConfig
    {
        public void Configuration(IAppBuilder app)
        {
            HttpListener listener = (HttpListener)app.Properties["System.Net.HttpListener"];
            listener.AuthenticationSchemes = SynapseControllerService.Config.AuthenticationScheme;

            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();

            // Web API configuration and services
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "synapse/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            var appXmlType = config.Formatters.XmlFormatter.SupportedMediaTypes.FirstOrDefault( t => t.MediaType == "application/xml" );
            config.Formatters.XmlFormatter.SupportedMediaTypes.Remove( appXmlType );

            app.UseWebApi( config );
        }
    }
}