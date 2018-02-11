using System;
using System.Collections.Generic;
using System.Linq;

using Synapse.Core.Utilities;

using YamlDotNet.Serialization;

namespace Synapse.Services
{
    public class AuthorizationProvider : IAuthorizationProviderConfig
    {
        public string Type { get; set; } = "Synapse.Authorization:UserIdProvider";
        internal bool HasType { get { return !string.IsNullOrWhiteSpace( Type ); } }

        public ServerRole ServerRole { get; set; } = ServerRole.Admin;

        public List<string> Topics { get; set; } = null;
        [YamlIgnore]
        public bool HasTopics { get { return Topics != null && Topics.Count > 0; } }
        public bool ContainsTopic(string topic)
        {
            return HasTopics ? Topics.Contains( topic, StringComparer.OrdinalIgnoreCase ) : false;
        }

        public object Config { get; set; }
        [YamlIgnore]
        public bool HasConfig { get { return Config != null; } }

        public bool? IsAuthorized(string id)
        {
            if( HasType && HasConfig )
            {
                IAuthorizationProvider auth = AssemblyLoader.Load<IAuthorizationProvider>( Type, Type );
                auth.Configure( this );
                return auth.IsAuthorized( id );
            }
            else
                return true;
        }
    }
}