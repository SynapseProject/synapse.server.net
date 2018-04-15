using System;
using System.Collections.Generic;
using System.Net;


namespace Synapse.Services
{
    /// <summary>
    /// Hold the startup config for Synapse.Controller; written as an independent class (not using .NET config) for cross-platform compatibility.
    /// </summary>
    public class SynapseControllerConfig
    {
        public SynapseControllerConfig() { }


        public string NodeUrl { get; set; }
        public AuthenticationSchemes NodeAuthenticationScheme { get; set; } = AuthenticationSchemes.None;
        public bool SignPlan { get; set; } = false;

        public Dictionary<string, object> Assemblies { get; set; }
        internal bool HasAssemblies { get { return Assemblies != null && Assemblies.Count > 0; } }

        public SynapseDalConfig Dal { get; set; } = new SynapseDalConfig();


        internal void Configure(string nodeUriRoot)
        {
            NodeUrl = $"{nodeUriRoot}/synapse/node";
            Dal.Configure( null );
        }
    }
}