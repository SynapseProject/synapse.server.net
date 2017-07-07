using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using Owin;
using Swashbuckle.Application;

namespace Synapse.Services
{
    public class WebServerConfig
    {
        public void Configuration(IAppBuilder app)
        {
            HttpListener listener = (HttpListener)app.Properties["System.Net.HttpListener"];
            listener.AuthenticationSchemes = SynapseServer.Config.WebApi.Authentication.Scheme;

            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();

            if( SynapseServer.Config.Service.IsRoleController && SynapseServer.Config.Controller.HasAssemblies )
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
            
            config.MessageHandlers.Add( new RawContentHandler() );

            if( SynapseServer.Config.WebApi.Authentication.Scheme == AuthenticationSchemes.Basic )
            {
                IAuthenticationProvider auth = AuthenticationProviderUtil.GetInstance(
                    "Synapse.Authentication", "Synapse.Authentication.AuthenticationProvider", config );
                string authConfig = Core.Utilities.YamlHelpers.Serialize( SynapseServer.Config.WebApi.Authentication.Config );
                BasicAuthenticationConfig ac = Core.Utilities.YamlHelpers.Deserialize<BasicAuthenticationConfig>( authConfig );
                auth.ConfigureBasicAuthentication( ac.LdapRoot, ac.Domain, SynapseServer.Config.WebApi.IsSecure );
            }

            var appXmlType = config.Formatters.XmlFormatter.SupportedMediaTypes.FirstOrDefault( t => t.MediaType == "application/xml" );
            config.Formatters.XmlFormatter.SupportedMediaTypes.Remove( appXmlType );

            config.EnableSwagger( x => x.SingleApiVersion( "v1", "Synapse Server" ) ).EnableSwaggerUi();
            //didn't work :(.
            //config.EnableSwagger( "synapse/{apiVersion}/swagger", x => x.SingleApiVersion( "v1", "Synapse Server" ) ).EnableSwaggerUi( "synapse/swagger/ui/{*assetPath}" );


            app.UseWebApi( config );
        }
    }
}