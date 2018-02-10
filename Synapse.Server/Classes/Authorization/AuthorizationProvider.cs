using System.Collections.Generic;
using Synapse.Core.Utilities;

namespace Synapse.Services
{
    public class AuthorizationProvider : IAuthorizationProviderConfig
    {
        public string Type { get; set; } = "Synapse.Authorization:UserIdProvider";
        internal bool HasType { get { return !string.IsNullOrWhiteSpace( Type ); } }

        public ServerRole ServerRole { get; set; } = ServerRole.Admin;

        public object Config { get; set; }
        internal bool HasConfig { get { return Config != null; } }

        public bool? IsAuthorized(string id)
        {
            if( HasType && HasConfig )
            {
                IAuthorizationProvider auth = AssemblyLoader.Load<IAuthorizationProvider>( Type, Type );
                Dictionary<string, string> props = auth.Configure( this );
                return auth.IsAuthorized( id );
            }
            else
                return true;
        }
    }
}