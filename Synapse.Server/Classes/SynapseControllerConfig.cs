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


        public string NodeUrl { get; set; } = "http://localhost:8000/synapse/node";
        internal bool HasNodeUrl { get { return !string.IsNullOrWhiteSpace( NodeUrl ); } }

        public string Dal { get; set; } = "Synapse.Controller.Dal.FileSystem:FileSystemDal";
        internal bool HasDal { get { return !string.IsNullOrWhiteSpace( Dal ); } }


        public static Dictionary<string, string> GetConfigDefaultValues()
        {
            Dictionary<string, string> values = new Dictionary<string, string>();

            SynapseControllerConfig c = new SynapseControllerConfig();

            string n = "c.";
            values[n + nameof( c.NodeUrl )] = c.NodeUrl;
            values[n + nameof( c.Dal )] = c.Dal;

            return values;
        }

        public void Configure(Dictionary<string, string> values)
        {
            SynapseControllerConfig c = new SynapseControllerConfig();

            string n = "c.";
            if( values.ContainsKey( n + nameof( c.NodeUrl ).ToLower() ) )
                c.NodeUrl = values[n + nameof( c.NodeUrl ).ToLower()];

            if( values.ContainsKey( n + nameof( c.Dal ).ToLower() ) )
                c.Dal = values[n + nameof( c.Dal ).ToLower()];

            Configure( c );
        }

        public void Configure(SynapseControllerConfig value)
        {
            //configure with anything provided
            if( value.HasNodeUrl )
                NodeUrl = value.NodeUrl;

            if( value.HasDal )
                Dal = value.Dal;
        }
    }
}