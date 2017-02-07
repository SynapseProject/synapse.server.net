using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Synapse.Core.Utilities;


namespace Synapse.Services
{
    /// <summary>
    /// Hold the startup config for Synapse.Server; written as an independent class (not using .NET config) for cross-platform compatibility.
    /// </summary>
    public class SynapseServerConfig
    {
        public SynapseServerConfig() { }

        public static readonly string CurrentPath = $"{Path.GetDirectoryName( typeof( SynapseServerConfig ).Assembly.Location )}";
        public static readonly string FileName = $"{Path.GetDirectoryName( typeof( SynapseServerConfig ).Assembly.Location )}\\Synapse.Server.config.yaml";


        public string ServiceName { get; set; } = "Synapse.Server";
        internal bool HasServiceName { get { return !string.IsNullOrWhiteSpace( ServiceName ); } }

        public string ServiceDisplayName { get; set; } = "Synapse Server";
        internal bool HasServiceDisplayName { get { return !string.IsNullOrWhiteSpace( ServiceDisplayName ); } }

        public ServerRole ServerRole { get; set; } = ServerRole.Controller;
        internal string ServerRoleString { get; set; } = "Controller";
        internal bool TestSetServerRoleString
        {
            get
            {
                ServerRole v = ServerRole;
                bool ok = Enum.TryParse( ServerRoleString, true, out v );
                if( ok )
                    ServerRole = v;
                return ok;
            }
        }
        public bool ServerIsController { get { return ServerRole == ServerRole.Controller; } }

        public int WebApiPort { get; set; } = 8008;
        internal string WebApiPortString { get; set; } = "8008";
        internal bool TestSetWebApiPortString
        {
            get
            {
                int port = WebApiPort;
                bool ok = int.TryParse( WebApiPortString, out port );
                if( ok )
                    WebApiPort = port;
                return ok;
            }
        }

        public AuthenticationSchemes AuthenticationScheme { get; set; } = AuthenticationSchemes.IntegratedWindowsAuthentication;
        internal string AuthenticationSchemeString { get; set; } = "IntegratedWindowsAuthentication";
        internal bool TestSetAuthenticationSchemeString
        {
            get
            {
                AuthenticationSchemes scheme = AuthenticationScheme;
                bool ok = Enum.TryParse( AuthenticationSchemeString, true, out scheme );
                if( ok )
                    AuthenticationScheme = scheme;
                return ok;
            }
        }


        public SynapseControllerConfig ControllerConfig { get; set; }
        public SynapseNodeConfig NodeConfig { get; set; }


        public void Serialize()
        {
            YamlHelpers.SerializeFile( FileName, this, serializeAsJson: false, emitDefaultValues: true );
        }

        public static SynapseServerConfig Deserialze()
        {
            if( !File.Exists( FileName ) )
                new SynapseServerConfig().Serialize();

            return YamlHelpers.DeserializeFile<SynapseServerConfig>( FileName );
        }

        public static Dictionary<string, string> GetConfigDefaultValues()
        {
            Dictionary<string, string> values = new Dictionary<string, string>();

            SynapseServerConfig c = new SynapseServerConfig();

            values[nameof( c.ServiceName )] = c.ServiceName;
            values[nameof( c.ServiceDisplayName )] = c.ServiceDisplayName;
            values[nameof( c.ServerRole )] = c.ServerRole.ToString();
            values[nameof( c.WebApiPort )] = c.WebApiPort.ToString();
            values[nameof( c.AuthenticationScheme )] = c.AuthenticationScheme.ToString();

            foreach( KeyValuePair<string, string> kvp in SynapseControllerConfig.GetConfigDefaultValues() )
                values[kvp.Key] = kvp.Value;

            foreach( KeyValuePair<string, string> kvp in SynapseNodeConfig.GetConfigDefaultValues() )
                values[kvp.Key] = kvp.Value;

            return values;
        }

        public static SynapseServerConfig Configure(Dictionary<string, string> values)
        {
            SynapseServerConfig c = new SynapseServerConfig();

            if( values.ContainsKey( nameof( c.ServiceName ).ToLower() ) )
                c.ServiceName = values[nameof( c.ServiceName ).ToLower()];

            if( values.ContainsKey( nameof( c.ServiceDisplayName ).ToLower() ) )
                c.ServiceDisplayName = values[nameof( c.ServiceDisplayName ).ToLower()];

            if( values.ContainsKey( nameof( c.ServerRole ).ToLower() ) )
                c.ServerRoleString = values[nameof( c.ServerRole ).ToLower()];

            if( values.ContainsKey( nameof( c.WebApiPort ).ToLower() ) )
                c.WebApiPortString = values[nameof( c.WebApiPort ).ToLower()];

            if( values.ContainsKey( nameof( c.AuthenticationScheme ).ToLower() ) )
                c.AuthenticationSchemeString = values[nameof( c.AuthenticationScheme ).ToLower()];

            c.ControllerConfig.Configure( values );

            c.NodeConfig.Configure( values );

            return Configure( c );
        }

        public static SynapseServerConfig Configure(SynapseServerConfig value)
        {
            //initialize with defaults
            SynapseServerConfig config = new SynapseServerConfig();
            //ovrride defaults with file values
            if( File.Exists( FileName ) )
                config = YamlHelpers.DeserializeFile<SynapseServerConfig>( FileName );

            //configure with anything provided
            if( value.HasServiceName && !(value.ServiceName == config.ServiceName) )
                config.ServiceName = value.ServiceName;

            if( value.HasServiceDisplayName && !(value.ServiceDisplayName == config.ServiceDisplayName) )
                config.ServiceDisplayName = value.ServiceDisplayName;

            if( value.TestSetServerRoleString && !(value.ServerRole == config.ServerRole) )
                config.ServerRole = value.ServerRole;

            if( value.TestSetWebApiPortString && !(value.WebApiPort == config.WebApiPort) )
                config.WebApiPort = value.WebApiPort;

            if( value.TestSetAuthenticationSchemeString && !(value.AuthenticationScheme == config.AuthenticationScheme) )
                config.AuthenticationScheme = value.AuthenticationScheme;

            config.ControllerConfig.Configure( value.ControllerConfig );

            config.NodeConfig.Configure( value.NodeConfig );

            config.Serialize();

            return config;
        }
    }
}