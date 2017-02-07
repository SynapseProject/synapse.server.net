using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Synapse.Core.Utilities;


namespace Synapse.Services
{
    /// <summary>
    /// Hold the startup config for Synapse.Node; written as an independent class (not using .NET config) for cross-platform compatibility.
    /// </summary>
    public class SynapseControllerConfig : SynapseServerConfig
    {
        public SynapseControllerConfig()
        {
        }


        public string NodeServiceUrl { get; set; } = "http://localhost:8000/synapse/node";
        internal bool HasNodeServiceUrl { get { return !string.IsNullOrWhiteSpace( NodeServiceUrl ); } }

        public string DalProvider { get; set; } = "Synapse.Controller.Dal.FileSystem:FileSystemDal";
        internal bool HasDalProvider { get { return !string.IsNullOrWhiteSpace( DalProvider ); } }



        override public void Serialize()
        {
            YamlHelpers.SerializeFile( FileName, this, serializeAsJson: false, emitDefaultValues: true );
        }

        new public static SynapseControllerConfig Deserialze()
        {
            if( !File.Exists( FileName ) )
                new SynapseControllerConfig().Serialize();

            return YamlHelpers.DeserializeFile<SynapseControllerConfig>( FileName );
        }

        override public Dictionary<string, string> GetConfigDefaultValues()
        {
            Dictionary<string, string> values = base.GetConfigDefaultValues();

            SynapseControllerConfig c = new SynapseControllerConfig();
            values[nameof( c.ServiceName )] = c.ServiceName;
            values[nameof( c.ServiceDisplayName )] = c.ServiceDisplayName;
            values[nameof( c.WebApiPort )] = c.WebApiPort.ToString();
            values[nameof( c.AuthenticationScheme )] = c.AuthenticationScheme.ToString();
            values[nameof( c.NodeServiceUrl )] = c.NodeServiceUrl;
            values[nameof( c.DalProvider )] = c.DalProvider;

            return values;
        }

        override public SynapseServerConfig Configure(Dictionary<string, string> values)
        {
            SynapseControllerConfig c = new SynapseControllerConfig();

            if( values.ContainsKey( nameof( c.ServiceName ).ToLower() ) )
                c.ServiceName = values[nameof( c.ServiceName ).ToLower()];

            if( values.ContainsKey( nameof( c.ServiceDisplayName ).ToLower() ) )
                c.ServiceDisplayName = values[nameof( c.ServiceDisplayName ).ToLower()];

            if( values.ContainsKey( nameof( c.WebApiPort ).ToLower() ) )
                c.WebApiPortString = values[nameof( c.WebApiPort ).ToLower()];

            if( values.ContainsKey( nameof( c.AuthenticationScheme ).ToLower() ) )
                c.AuthenticationSchemeString = values[nameof( c.AuthenticationScheme ).ToLower()];

            if( values.ContainsKey( nameof( c.NodeServiceUrl ).ToLower() ) )
                c.NodeServiceUrl = values[nameof( c.NodeServiceUrl ).ToLower()];

            if( values.ContainsKey( nameof( c.DalProvider ).ToLower() ) )
                c.DalProvider = values[nameof( c.DalProvider ).ToLower()];

            return Configure( c );
        }

        public static SynapseControllerConfig Configure(SynapseControllerConfig value)
        {
            //initialize with defaults
            SynapseControllerConfig config = new SynapseControllerConfig();
            //ovrride defaults with file values
            if( File.Exists( FileName ) )
                config = YamlHelpers.DeserializeFile<SynapseControllerConfig>( FileName );

            //configure with anything provided
            if( value.HasServiceName && !(value.ServiceName == config.ServiceName) )
                config.ServiceName = value.ServiceName;

            if( value.HasServiceDisplayName && !(value.ServiceDisplayName == config.ServiceDisplayName) )
                config.ServiceDisplayName = value.ServiceDisplayName;

            if( value.TestSetAuthenticationSchemeString && !(value.AuthenticationScheme == config.AuthenticationScheme) )
                config.AuthenticationScheme = value.AuthenticationScheme;

            if( value.TestSetWebApiPortString && !(value.WebApiPort == config.WebApiPort) )
                config.WebApiPort = value.WebApiPort;

            if( value.HasNodeServiceUrl && !(value.NodeServiceUrl == config.NodeServiceUrl) )
                config.NodeServiceUrl = value.NodeServiceUrl;

            if( value.HasDalProvider && !(value.DalProvider == config.DalProvider) )
                config.DalProvider = value.DalProvider;

            config.Serialize();

            return config;
        }
    }
}