﻿using System;
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

        internal string DefaultType { get { return "Synapse.Controller.Dal.FileSystem:FileSystemDal"; } }


        public string Type { get; set; } = "Synapse.Controller.Dal.FileSystem:FileSystemDal";
        internal bool HasType { get { return !string.IsNullOrWhiteSpace( Type ); } }

        public string LdapRoot { get; set; }
        internal bool HasLdapRoot { get { return !string.IsNullOrWhiteSpace( LdapRoot ); } }

        public object Config { get; set; }
        internal bool HasConfig { get { return Config != null; } }


        public Dictionary<string, string> GetDefaultValues()
        {
            return GetConfigDefaultValues();
        }

        public static Dictionary<string, string> GetConfigDefaultValues()
        {
            Dictionary<string, string> values = new Dictionary<string, string>();

            SynapseDalConfig c = new SynapseDalConfig();

            string n = "c_";
            values[n + nameof( c.LdapRoot )] = c.LdapRoot;
            values[n + nameof( c.Type )] = c.Type;
            values[n + nameof( c.Config )] = c.Config?.ToString();

            return values;
        }

        public void Configure(Dictionary<string, string> values)
        {
            SynapseDalConfig c = new SynapseDalConfig();

            string n = "c_";
            if( values.ContainsKey( n + nameof( c.LdapRoot ).ToLower() ) )
                c.LdapRoot = values[n + nameof( c.LdapRoot ).ToLower()];

            if( values.ContainsKey( n + nameof( c.Type ).ToLower() ) )
                c.Type = values[n + nameof( c.Type ).ToLower()];

            //if( values.ContainsKey( n + nameof( c.Config ).ToLower() ) )
                //c.SignPlanString = values[n + nameof( c.Config ).ToLower()];

            Configure( c );
        }

        public void Configure(IConfigurationProvider configProvider)
        {
            Configure( configProvider as SynapseDalConfig );
        }

        public void Configure(SynapseDalConfig value)
        {
            if( value == null )
                throw new ArgumentNullException( nameof( value ), "DalConfig value is required." );

            //configure with anything provided
            if( value.HasLdapRoot )
                LdapRoot = value.LdapRoot;

            string type = DefaultType;
            if( value.HasType )
                Type = type = value.Type;

            try
            {
                IControllerDal dal = AssemblyLoader.Load<IControllerDal>( type, DefaultType );
                Config = dal.GetDefaultConfig();
            }
            catch { }


            //if( value.TestSetConfigString )
            //    Config = value.Config;
        }
    }
}