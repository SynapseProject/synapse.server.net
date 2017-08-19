using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using Synapse.Core.Utilities;


namespace Synapse.Services
{
    /// <summary>
    /// Holds the startup config for Synapse.Server; written as an independent class (not using .NET config) for cross-platform compatibility.
    /// </summary>
    public class SynapseServerConfig
    {
        public SynapseServerConfig() { }

        public static readonly string CurrentPath = $"{Path.GetDirectoryName( typeof( SynapseServerConfig ).Assembly.Location )}";


        public static string FileName { get; private set; } = $"{Path.GetDirectoryName( typeof( SynapseServerConfig ).Assembly.Location )}\\Synapse.Server.config.yaml";
        //public string ConfigFileName { get { return FileName; } }  //ss: i recall putting this here, but it doesn't seem to be in use
        public ServiceConfig Service { get; set; } = new ServiceConfig();
        public WebApiConfig WebApi { get; set; } = new WebApiConfig();
        public SignatureConfig Signature { get; set; } = new SignatureConfig();
        public SynapseControllerConfig Controller { get; set; } = new SynapseControllerConfig();
        public SynapseNodeConfig Node { get; set; } = new SynapseNodeConfig();


        public void Serialize()
        {
            YamlHelpers.SerializeFile( FileName, this, serializeAsJson: false, emitDefaultValues: true );
        }

        public static SynapseServerConfig Deserialze(string fileName = null)
        {
            if( !string.IsNullOrWhiteSpace( fileName ) )
                FileName = fileName;

            if( !File.Exists( FileName ) )
                throw new FileNotFoundException( $"Could not find {FileName}" );

            return YamlHelpers.DeserializeFile<SynapseServerConfig>( FileName );
        }

        public static SynapseServerConfig DeserializeOrNew(ServerRole role, string fileName = null)
        {
            SynapseServerConfig config = null;

            int port = ((role & ServerRole.Controller) == ServerRole.Controller) ? 20000 : 20001;

            if( !string.IsNullOrWhiteSpace( fileName ) )
                FileName = fileName;

            if( !File.Exists( FileName ) )
            {
                config = new SynapseServerConfig();
                config.WebApi.Port = port;
                config.Service.Name = $"Synapse.{role}";
                config.Service.DisplayName = $"Synapse {role}";
                config.Service.Role = role;

                if( config.Service.IsRoleController )
                {
                    WebApiConfig node = new Services.WebApiConfig() { Host = config.WebApi.Host, IsSecure = config.WebApi.IsSecure };
                    node.Port = config.Service.IsRoleServer ? 20000 : 20001;
                    config.Controller.Configure( node.ToUri( Environment.UserInteractive ) );
                }
                if( !config.Service.IsRoleController )
                    config.Controller = null;
                if( !config.Service.IsRoleNode )
                    config.Node = null;

                config.Serialize();
            }
            else
                config = YamlHelpers.DeserializeFile<SynapseServerConfig>( FileName );

            return config;
        }
    }

    public class ServiceConfig
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public ServerRole Role { get; set; }

        internal bool IsRoleController { get { return (Role & ServerRole.Controller) == ServerRole.Controller; } }
        internal bool IsRoleNode { get { return (Role & ServerRole.Node) == ServerRole.Node; } }
        internal bool IsRoleServer { get { return (Role & ServerRole.Server) == ServerRole.Server; } }
    }

    public class WebApiConfig
    {
        static string localhost = "localhost";
        public string Host { get; set; } = localhost;
        public int Port { get; set; }
        public bool IsSecure { get; set; } = false;
        public bool UseImpersonation { get; set; } = false;

        public AuthenticationConfig Authentication { get; set; } = new AuthenticationConfig();


        public string GetHost(bool isUserInteractive)
        {
            string host = Host.ToLower();
            if( host == localhost || host == "*" || string.IsNullOrWhiteSpace( host ) )
                Host = isUserInteractive ? localhost : "*";
            return Host;
        }

        public string ToUri(bool isUserInteractive)
        {
            string scheme = IsSecure ? "https" : "http";
            string host = GetHost( isUserInteractive );
            return $"{scheme}://{host}:{Port}";
        }
    }

    public class AuthenticationConfig
    {
        public AuthenticationSchemes Scheme { get; set; } = AuthenticationSchemes.IntegratedWindowsAuthentication;

        public object Config { get; set; }
    }

    public class SignatureConfig
    {
        public string KeyUri { get; set; }

        public string KeyContainerName { get; set; }

        public CspProviderFlags CspProviderFlags { get; set; } = CspProviderFlags.NoFlags;
    }
}