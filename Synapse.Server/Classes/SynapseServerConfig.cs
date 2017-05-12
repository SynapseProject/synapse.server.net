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
        public static readonly string FileName = $"{Path.GetDirectoryName( typeof( SynapseServerConfig ).Assembly.Location )}\\Synapse.Server.config.yaml";


        public ServiceConfig Service { get; set; } = new ServiceConfig();
        public WebApiConfig WebApi { get; set; } = new WebApiConfig();
        public SignatureConfig Signature { get; set; } = new SignatureConfig();
        public SynapseControllerConfig Controller { get; set; } = new SynapseControllerConfig();
        public SynapseNodeConfig Node { get; set; } = new SynapseNodeConfig();


        public void Serialize()
        {
            YamlHelpers.SerializeFile( FileName, this, serializeAsJson: false, emitDefaultValues: true );
        }

        public static SynapseServerConfig Deserialze(ServerRole serverRole = ServerRole.Controller)
        {
            SynapseServerConfig config = null;

            int port = serverRole == ServerRole.Controller ? 20000 : 20001;

            if( !File.Exists( FileName ) )
            {
                config = new SynapseServerConfig();
                YamlHelpers.SerializeFile( FileName, config, emitDefaultValues: true );
            }
            else
                config = YamlHelpers.DeserializeFile<SynapseServerConfig>( FileName );

            return config;
        }

        //public static Dictionary<string, string> GetConfigDefaultValues(ServerRole serverRole)
        //{
        //    return new SynapseServerConfig();
        //}
    }

    public class ServiceConfig
    {
        internal static readonly string defaultName = "Synapse.[Controller/Node]";
        internal static readonly string defaultDisplayName = "Synapse [Controller/Node]";

        public string Name { get; set; } = defaultName;
        public string DisplayName { get; set; } = defaultDisplayName;

        internal bool HasServiceNameDefaults { get { return Name == defaultName || DisplayName == defaultDisplayName; } }

        public ServerRole Role { get; set; }
        internal bool ServerIsController { get { return Role == ServerRole.Controller; } }
    }

    public class WebApiConfig
    {
        internal static string localhost = "localhost";
        public string Host { get; set; } = localhost;
        public string GetHost(bool isUserInteractive)
        {
            string host = Host.ToLower();
            if( host == localhost || host == "*" )
                Host = isUserInteractive ? localhost : "*";
            return Host;
        }

        public int Port { get; set; } = 20000;
        public bool IsSecure { get; set; } = false;

        public AuthenticationConfig Authentication { get; set; }


        public string ToUri(bool isUserInteractive)
        {
            string protocol = IsSecure ? "https" : "http";
            string host = GetHost( isUserInteractive );
            return $"{protocol}://{host}:{Port}";
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

        public string KeyContainerName { get; set; } = "DefaultContainerName";

        public CspProviderFlags CspProviderFlags { get; set; } = CspProviderFlags.NoFlags;
    }
}