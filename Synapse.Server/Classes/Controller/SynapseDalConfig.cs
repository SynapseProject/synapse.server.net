using System;
using System.Collections.Generic;
using Synapse.Core.Utilities;
using Synapse.Services.Controller.Dal;

namespace Synapse.Services
{
    /// <summary>
    /// Hold the startup config for Synapse.Node; written as an independent class (not using .NET config) for cross-platform compatibility.
    /// </summary>
    public class SynapseDalConfig : ISynapseDalConfig, IConfigurationProvider
    {
        public SynapseDalConfig()
        {
        }

        internal string DefaultType { get { return "Synapse.Controller.Dal.Componentized:ComponentizedDal"; } }


        public string Type { get; set; } = "Synapse.Controller.Dal.Componentized:ComponentizedDal";
        internal bool HasType { get { return !string.IsNullOrWhiteSpace( Type ); } }

        public string LdapRoot { get; set; }
        internal bool HasLdapRoot { get { return !string.IsNullOrWhiteSpace( LdapRoot ); } }

        public object Config { get; set; }
        internal bool HasConfig { get { return Config != null; } }


        public object GetDefaultConfig()
        {
            return GetConfigDefaultValues();
        }

        public static object GetConfigDefaultValues()
        {
            SynapseDalConfig c = new SynapseDalConfig();
            c.ConfigureDalProvider();

            return c;
        }

        public void Configure(IConfigurationProvider config)
        {
            if( config != null )
            {
                SynapseDalConfig c = config as SynapseDalConfig;
                Type = c.Type;
                LdapRoot = c.LdapRoot;
                Config = c.Config;
            }
            ConfigureDalProvider();
        }

        void ConfigureDalProvider()
        {
            try
            {
                IControllerDal dal = AssemblyLoader.Load<IControllerDal>( Type, DefaultType );
                Config = dal?.GetDefaultConfig();
            }
            catch { }
        }
    }
}