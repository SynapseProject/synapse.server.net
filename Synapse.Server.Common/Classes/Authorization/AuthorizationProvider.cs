using System;
using System.Collections.Generic;
using System.Linq;

using Synapse.Authorization;
using Synapse.Core.Utilities;

using YamlDotNet.Serialization;

namespace Synapse.Services
{
    public class AuthorizationProvider : IAuthorizationProviderConfig
    {
        public string Type { get; set; } = "Synapse.Authorization:UserIdProvider";
        [YamlIgnore]
        public bool HasType { get { return !string.IsNullOrWhiteSpace( Type ); } }

        public AuthorizationProviderFilter AppliesTo { get; set; } = new AuthorizationProviderFilter();

        public bool ContainsServerRole(ServerRole serverRole)
        {
            if( AppliesTo == null )
                AppliesTo = new AuthorizationProviderFilter();
            return (AppliesTo.ServerRole & serverRole) == serverRole;
        }

        public bool ContainsNoTopics()
        {
            if( AppliesTo == null )
                AppliesTo = new AuthorizationProviderFilter();
            return AppliesTo.HasTopics == false;
        }

        public bool ContainsTopic(string topic)
        {
            if( AppliesTo == null )
                AppliesTo = new AuthorizationProviderFilter();
            return AppliesTo.HasTopics ? AppliesTo.Topics.Contains( topic, StringComparer.OrdinalIgnoreCase ) : false;
        }

        public object Config { get; set; }
        [YamlIgnore]
        public bool HasConfig { get { return Config != null; } }

        public AuthorizationType IsAuthorized(string id)
        {
            if( HasType && HasConfig )
            {
                IAuthorizationProvider auth = AssemblyLoader.Load<IAuthorizationProvider>( Type, Type );
                auth.Configure( this );
                return auth.IsAuthorized( id );
            }
            else
                return AuthorizationType.ImplicitAllow;
        }
    }

    public class AuthorizationProviderFilter
    {
        public ServerRole ServerRole { get; set; } = ServerRole.Universal;

        public List<string> Topics { get; set; } = null;
        [YamlIgnore]
        public bool HasTopics { get { return Topics != null && Topics.Count > 0; } }
    }
}