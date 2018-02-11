using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;


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

            IEnumerable<AuthorizationProvider> providers = Providers.Where( p => (p.ServerRole & serverRole) == serverRole );
            foreach( AuthorizationProvider provider in providers )
            {
                if( provider.HasTopics )
                {
                    isAuthorized = false;
                    if( !string.IsNullOrWhiteSpace( topic ) && provider.Topics.Contains( topic, StringComparer.OrdinalIgnoreCase ) )
                        isAuthorized = provider.IsAuthorized( id );
                }
                else
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