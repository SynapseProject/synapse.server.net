using System;
using System.Collections.Generic;
using System.Linq;


namespace Synapse.Services
{
    public class AuthorizationConfig
    {
        public AuthorizationConfig() { }


        public bool AllowAnonymous { get; set; } = true;

        public List<AuthorizationProvider> Providers { get; set; } = new List<AuthorizationProvider>();
        internal bool HasProviders { get { return Providers != null && Providers.Count > 0; } }

        public bool IsAuthorized(string id, ServerRole serverRole, string topic = null)
        {
            if( string.IsNullOrWhiteSpace( id ) && !AllowAnonymous )
                return false;

            bool? isAuthorized = null;
            bool ok = false;


            IEnumerable<AuthorizationProvider> roleProviders = null;

            if( string.IsNullOrWhiteSpace( topic ) )
                roleProviders = Providers.Where( p => p.ContainsServerRole( serverRole ) && p.ContainsNoTopics() );
            else
                roleProviders = Providers.Where( p => p.ContainsServerRole( serverRole ) && p.ContainsTopic( topic ) );


            foreach( AuthorizationProvider provider in roleProviders )
            {
                isAuthorized = provider.IsAuthorized( id );

                if( isAuthorized.HasValue )
                {
                    ok = isAuthorized.Value;
                    break;
                }
            }

            if( isAuthorized == null )
                ok = true;


            return ok;
        }
    }
}