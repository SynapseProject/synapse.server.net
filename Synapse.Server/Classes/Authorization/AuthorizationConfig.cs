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

        public bool IsAuthorized(string id, ServerRole serverRole)
        {
            if( string.IsNullOrWhiteSpace( id ) && !AllowAnonymous )
                return false;

            bool? isAuthorized = null;
            bool ok = false;

            IEnumerable<AuthorizationProvider> providers = Providers.Where( p => (p.ServerRole & serverRole) == serverRole );
            foreach( AuthorizationProvider provider in providers )
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


            //process denies
            //if( !denied )
              //process allows, break on first match

            return ok;
        }
    }
}