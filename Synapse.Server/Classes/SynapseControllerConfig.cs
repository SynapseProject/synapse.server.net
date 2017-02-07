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
    public class SynapseControllerConfig
    {
        public SynapseControllerConfig()
        {
        }


        public string NodeServiceUrl { get; set; } = "http://localhost:8000/synapse/node";
        internal bool HasNodeServiceUrl { get { return !string.IsNullOrWhiteSpace( NodeServiceUrl ); } }

        public string DalProvider { get; set; } = "Synapse.Controller.Dal.FileSystem:FileSystemDal";
        internal bool HasDalProvider { get { return !string.IsNullOrWhiteSpace( DalProvider ); } }


        public static Dictionary<string, string> GetConfigDefaultValues()
        {
            Dictionary<string, string> values = new Dictionary<string, string>();

            SynapseControllerConfig c = new SynapseControllerConfig();

            values[nameof( c.NodeServiceUrl )] = c.NodeServiceUrl;
            values[nameof( c.DalProvider )] = c.DalProvider;

            return values;
        }

        public void Configure(Dictionary<string, string> values)
        {
            SynapseControllerConfig c = new SynapseControllerConfig();

            if( values.ContainsKey( nameof( c.NodeServiceUrl ).ToLower() ) )
                c.NodeServiceUrl = values[nameof( c.NodeServiceUrl ).ToLower()];

            if( values.ContainsKey( nameof( c.DalProvider ).ToLower() ) )
                c.DalProvider = values[nameof( c.DalProvider ).ToLower()];

            Configure( c );
        }

        public void Configure(SynapseControllerConfig value)
        {
            //configure with anything provided
            if( value.HasNodeServiceUrl )
                NodeServiceUrl = value.NodeServiceUrl;

            if( value.HasDalProvider )
                DalProvider = value.DalProvider;
        }
    }
}