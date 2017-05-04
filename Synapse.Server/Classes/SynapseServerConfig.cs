using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
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


        internal static readonly string defaultServiceName = "Synapse.[Controller/Node]";
        public string ServiceName { get; set; } = defaultServiceName;
        internal bool HasServiceName { get { return !string.IsNullOrWhiteSpace( ServiceName ); } }
        internal string ServiceNameValue
        {
            get
            {
                if( ServiceName == defaultServiceName )
                    return ServerIsController ? "Synapse.Controller" : "Synapse.Node";
                else
                    return ServiceName;
            }
        }


        internal static readonly string defaultServiceDisplayName = "Synapse [Controller/Node]";
        public string ServiceDisplayName { get; set; } = defaultServiceDisplayName;
        internal bool HasServiceDisplayName { get { return !string.IsNullOrWhiteSpace( ServiceDisplayName ); } }
        internal string ServiceDisplayNameValue
        {
            get
            {
                if( ServiceDisplayName == defaultServiceDisplayName )
                    return ServerIsController ? "Synapse Controller" : "Synapse Node";
                else
                    return ServiceDisplayName;
            }
        }

        internal bool HasServiceNameDefaults {get { return ServiceName == defaultServiceName || ServiceDisplayName == defaultServiceDisplayName; } }


        ServerRole _serverRole= ServerRole.Controller;
        public ServerRole ServerRole { get { return _serverRole; } set { _serverRole = value; ServerRoleString = _serverRole.ToString(); } } 
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
        internal bool ServerIsController { get { return ServerRole == ServerRole.Controller; } }

        public int WebApiPort { get; set; } = 20000;
        internal string WebApiPortString { get; set; } = "20000";
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

        public bool WebApiIsSecure { get; set; }
        internal string WebApiIsSecureString { get; set; } = "false";
        internal bool TestSetWebApiIsSecureString
        {
            get
            {
                bool v = WebApiIsSecure;
                bool ok = bool.TryParse( WebApiIsSecureString, out v );
                if( ok )
                    WebApiIsSecure = v;
                return ok;
            }
        }


        public AuthenticationSchemes AuthenticationScheme { get; set; } = AuthenticationSchemes.IntegratedWindowsAuthentication;
        internal string AuthenticationSchemeString { get; set; } = AuthenticationSchemes.IntegratedWindowsAuthentication.ToString();
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

        public string SignatureKeyFile { get; set; }
        internal bool HasSignatureKeyFile { get { return !string.IsNullOrWhiteSpace( SignatureKeyFile ); } }

        public string SignatureKeyContainerName { get; set; } = "DefaultContainerName";
        internal bool HasSignatureKeyContainerName { get { return !string.IsNullOrWhiteSpace( SignatureKeyContainerName ); } }

        public CspProviderFlags SignatureCspProviderFlags { get; set; }
        internal string SignatureCspProviderFlagsString { get; set; } = CspProviderFlags.NoFlags.ToString();
        internal bool TestSignatureCspProviderFlagsString
        {
            get
            {
                CspProviderFlags flags = SignatureCspProviderFlags;
                bool ok = Enum.TryParse( SignatureCspProviderFlagsString, true, out flags );
                if( ok )
                    SignatureCspProviderFlags = flags;
                return ok;
            }
        }



        public SynapseControllerConfig Controller { get; set; } = new SynapseControllerConfig();
        public SynapseNodeConfig Node { get; set; } = new SynapseNodeConfig();


        public void Serialize()
        {
            YamlHelpers.SerializeFile( FileName, this, serializeAsJson: false, emitDefaultValues: true );
        }

        public static SynapseServerConfig Deserialze(ServerRole serverRole = ServerRole.Controller)
        {
            SynapseServerConfig config = null;

            if( !File.Exists( FileName ) )
                config = Configure( new SynapseServerConfig() { ServerRole = serverRole } );
            else
                config = YamlHelpers.DeserializeFile<SynapseServerConfig>( FileName );

            return config;
        }

        public static Dictionary<string, string> GetConfigDefaultValues(ServerRole serverRole)
        {
            Dictionary<string, string> values = new Dictionary<string, string>();

            SynapseServerConfig c = new SynapseServerConfig();
            c.ServerRole = serverRole;
            c.ServiceName = $"Synapse.{serverRole}";
            c.ServiceDisplayName = $"Synapse {serverRole}";
            if( !c.ServerIsController )
                c.WebApiPort = 20001;

            values[nameof( c.ServiceName )] = c.ServiceName;
            values[nameof( c.ServiceDisplayName )] = c.ServiceDisplayName;
            values[nameof( c.ServerRole )] = c.ServerRole.ToString();
            values[nameof( c.WebApiPort )] = c.WebApiPort.ToString();
            values[nameof( c.WebApiIsSecure )] = c.WebApiIsSecure.ToString();
            values[nameof( c.AuthenticationScheme )] = c.AuthenticationScheme.ToString();
            values[nameof( c.SignatureKeyFile )] = c.SignatureKeyFile;
            values[nameof( c.SignatureKeyContainerName )] = c.SignatureKeyContainerName;
            values[nameof( c.SignatureCspProviderFlags )] = c.SignatureCspProviderFlags.ToString();

            foreach( KeyValuePair<string, string> kvp in SynapseControllerConfig.GetConfigDefaultValues() )
                values[kvp.Key] = kvp.Value;

            foreach( KeyValuePair<string, string> kvp in SynapseNodeConfig.GetConfigDefaultValues() )
                values[kvp.Key] = kvp.Value;

            return values;
        }

        public static SynapseServerConfig Configure(ServerRole serverRole, Dictionary<string, string> values)
        {
            Dictionary<string, string> defaults = GetConfigDefaultValues( serverRole );
            foreach( string key in defaults.Keys )
                if( !values.ContainsKey( key.ToLower() ) )
                    values.Add( key.ToLower(), defaults[key] );

            SynapseServerConfig c = new SynapseServerConfig();

            if( values.ContainsKey( nameof( c.ServiceName ).ToLower() ) )
                c.ServiceName = values[nameof( c.ServiceName ).ToLower()];

            if( values.ContainsKey( nameof( c.ServiceDisplayName ).ToLower() ) )
                c.ServiceDisplayName = values[nameof( c.ServiceDisplayName ).ToLower()];

            if( values.ContainsKey( nameof( c.ServerRole ).ToLower() ) )
                c.ServerRoleString = values[nameof( c.ServerRole ).ToLower()];

            if( values.ContainsKey( nameof( c.WebApiPort ).ToLower() ) )
                c.WebApiPortString = values[nameof( c.WebApiPort ).ToLower()];

            if( values.ContainsKey( nameof( c.WebApiIsSecure ).ToLower() ) )
                c.WebApiIsSecureString = values[nameof( c.WebApiIsSecure ).ToLower()];

            if( values.ContainsKey( nameof( c.AuthenticationScheme ).ToLower() ) )
                c.AuthenticationSchemeString = values[nameof( c.AuthenticationScheme ).ToLower()];

            if( values.ContainsKey( nameof( c.SignatureKeyFile ).ToLower() ) )
                c.SignatureKeyFile = values[nameof( c.SignatureKeyFile ).ToLower()];

            if( values.ContainsKey( nameof( c.SignatureKeyContainerName ).ToLower() ) )
                c.SignatureKeyContainerName = values[nameof( c.SignatureKeyContainerName ).ToLower()];

            if( values.ContainsKey( nameof( c.SignatureCspProviderFlags ).ToLower() ) )
                c.SignatureCspProviderFlagsString = values[nameof( c.SignatureCspProviderFlags ).ToLower()];


            if( (serverRole & ServerRole.Controller) == ServerRole.Controller )
                c.Controller.Configure( values );
            else
                c.Controller = null;

            if( (serverRole & ServerRole.Node) == ServerRole.Node )
                c.Node.Configure( values );
            else
                c.Node = null;

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

            if( value.TestSetWebApiIsSecureString && !(value.WebApiIsSecure == config.WebApiIsSecure) )
                config.WebApiIsSecure = value.WebApiIsSecure;

            if( value.TestSetAuthenticationSchemeString && !(value.AuthenticationScheme == config.AuthenticationScheme) )
                config.AuthenticationScheme = value.AuthenticationScheme;

            if( value.HasSignatureKeyFile && !(value.SignatureKeyFile == config.SignatureKeyFile) )
                config.SignatureKeyFile = value.SignatureKeyFile;

            if( value.HasSignatureKeyContainerName && !(value.SignatureKeyContainerName == config.SignatureKeyContainerName) )
                config.SignatureKeyContainerName = value.SignatureKeyContainerName;

            if( value.TestSignatureCspProviderFlagsString && !(value.SignatureCspProviderFlags == config.SignatureCspProviderFlags) )
                config.SignatureCspProviderFlags = value.SignatureCspProviderFlags;


            if( (value.ServerRole & ServerRole.Controller) == ServerRole.Controller )
                config.Controller.Configure( value.Controller );
            else
                config.Controller = null;

            if( (value.ServerRole & ServerRole.Node) == ServerRole.Node )
                config.Node.Configure( value.Node );
            else
                config.Node = null;


            config.Serialize();

            return config;
        }
    }
}