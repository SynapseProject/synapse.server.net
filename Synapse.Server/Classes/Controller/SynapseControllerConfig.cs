using System;
using System.Collections.Generic;


namespace Synapse.Services
{
    /// <summary>
    /// Hold the startup config for Synapse.Controller; written as an independent class (not using .NET config) for cross-platform compatibility.
    /// </summary>
    public class SynapseControllerConfig
    {
        public SynapseControllerConfig()
        {
        }


        public string NodeUrl { get; set; } = "http://localhost:20001/synapse/node";
        internal bool HasNodeUrl { get { return !string.IsNullOrWhiteSpace( NodeUrl ); } }

        public bool SignPlan { get; set; } = true;
        internal string SignPlanString { get; set; } = "true";
        internal bool TestSetSignPlanString
        {
            get
            {
                bool v = SignPlan;
                bool ok = bool.TryParse( SignPlanString, out v );
                if( ok )
                    SignPlan = v;
                return ok;
            }
        }

        public SynapseDalConfig Dal { get; set; } = new SynapseDalConfig();


        public static Dictionary<string, string> GetConfigDefaultValues()
        {
            Dictionary<string, string> values = new Dictionary<string, string>();

            SynapseControllerConfig c = new SynapseControllerConfig();

            string n = "c_";
            values[n + nameof( c.NodeUrl )] = c.NodeUrl;
            values[n + nameof( c.SignPlan )] = c.SignPlan.ToString();

            foreach( KeyValuePair<string, string> kvp in SynapseDalConfig.GetConfigDefaultValues() )
                values[kvp.Key] = kvp.Value;

            return values;
        }

        public void Configure(Dictionary<string, string> values)
        {
            SynapseControllerConfig c = new SynapseControllerConfig();

            string n = "c_";
            if( values.ContainsKey( n + nameof( c.NodeUrl ).ToLower() ) )
                c.NodeUrl = values[n + nameof( c.NodeUrl ).ToLower()];

            if( values.ContainsKey( n + nameof( c.SignPlan ).ToLower() ) )
                c.SignPlanString = values[n + nameof( c.SignPlan ).ToLower()];

            c.Dal.Configure( values );

            Configure( c );
        }

        public void Configure(SynapseControllerConfig value)
        {
            //configure with anything provided
            if( value.HasNodeUrl )
                NodeUrl = value.NodeUrl;

            if( value.TestSetSignPlanString )
                SignPlan = value.SignPlan;

            //xxx.Dal.Configure( value.Dal );
        }
    }
}