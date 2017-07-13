using System;
using System.Collections.Generic;


namespace Synapse.Services
{
    /// <summary>
    /// Hold the startup config for Synapse.Controller; written as an independent class (not using .NET config) for cross-platform compatibility.
    /// </summary>
    public class SynapseControllerConfig
    {
        public SynapseControllerConfig() { }


        public string NodeUrl { get; set; }
        public bool SignPlan { get; set; } = false;

        public List<string> Assemblies { get; set; }
        internal bool HasAssemblies { get { return Assemblies != null && Assemblies.Count > 0; } }

        public SynapseDalConfig Dal { get; set; } = new SynapseDalConfig();


        internal void Configure(string nodeUriRoot)
        {
            NodeUrl = $"{nodeUriRoot}/synapse/node";
            Dal.Configure( null );
        }
    }
}