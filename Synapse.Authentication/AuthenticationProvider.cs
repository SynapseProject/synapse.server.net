using System;
using System.DirectoryServices;
using System.Web.Http;

using Synapse.Services;

using Thinktecture.IdentityModel.WebApi.Authentication.Handler;

namespace Synapse.Authentication
{
    public class AuthenticationProvider : IAuthenticationProvider
    {
        HttpConfiguration _config = null;
        public AuthenticationProvider(HttpConfiguration config)
        {
            _config = config;
        }

        public void ConfigureBasicAuthentication(string ldapRoot, string domain, bool requireSsl = true)
        {
            AuthenticationConfiguration authConfig = new AuthenticationConfiguration
            {
                RequireSsl = requireSsl
            };

            authConfig.AddBasicAuthentication( (username, password) => IsAuthenticated( ldapRoot, domain, username, password ) );

            _config.MessageHandlers.Add( new AuthenticationHandler( authConfig ) );
        }

        public bool IsAuthenticated(string ldapRoot, string domain, string username, string password)
        {
            string domainAndUsername = username.StartsWith( domain + @"\" ) ? username : domain + @"\" + username;
            DirectoryEntry entry = new DirectoryEntry( ldapRoot, domainAndUsername, password );

            try
            {
                // Bind to the native AdsObject to force authentication.
                Object obj = entry.NativeObject;
                DirectorySearcher search = new DirectorySearcher( entry )
                {
                    Filter = "(sAMAccountName=" + username + ")"
                };
                search.PropertiesToLoad.Add( "cn" );
                SearchResult result = search.FindOne();
                if( null == result )
                {
                    return false;
                }
                // Update the new path to the user in the directory
                ldapRoot = result.Path;
                string filterAttribute = (String)result.Properties["cn"][0];
            }
            catch( Exception ex )
            {
                throw new Exception( "Error authenticating user. " + ex.Message );
            }

            return true;
        }
    }
}