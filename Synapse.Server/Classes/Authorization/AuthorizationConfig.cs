using System;
using System.Collections.Generic;
using System.Net;


namespace Synapse.Services
{
    /// <summary>
    /// Hold the startup config for Synapse.Controller; written as an independent class (not using .NET config) for cross-platform compatibility.
    /// </summary>
    public class AuthorizationConfig : IConfigurationProvider
    {
        public AuthorizationConfig() { }


        public bool AllowAnonymous { get; set; } = true;

        public List<AuthorizationProviderInfo> Providers { get; set; } = new List<AuthorizationProviderInfo>();
        internal bool HasProviders { get { return Providers != null && Providers.Count > 0; } }

        public bool HasAccess(string id)
        {
            if( string.IsNullOrWhiteSpace( id ) && !AllowAnonymous )
                return false;

            //process denies
            //if( !denied )
              //process allows, break on first match

            return true;
        }


        public object GetDefaultConfig()
        {
            AuthorizationConfig synapseAdminConfig = new AuthorizationConfig();
            synapseAdminConfig.Providers.Add( new AuthorizationProviderInfo() );
            return synapseAdminConfig;
        }

        public void Configure(IConfigurationProvider configProvider)
        {
        }
    }

    public class AuthorizationProviderInfo
    {
        public string Type { get; set; } = "Synapse.Common:Synapse.Common.UserIdProvider";
        internal bool HasType { get { return !string.IsNullOrWhiteSpace( Type ); } }

        public ServerRole ServerRole { get; set; } = ServerRole.Admin;


        public object Config { get; set; }
        internal bool HasConfig { get { return Config != null; } }
    }
}