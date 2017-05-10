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
            //note: we found that ldap seems to require upper-case protocol declaration.  don't know why.
            //      this will correct whatever comes in to allowable casing.
            ldapRoot = ldapRoot.ToLower().Replace( "ldap://", "LDAP://" );

            string domainAndUsername = username.ToLower().StartsWith( domain.ToLower() + @"\" ) ?
                username : domain + @"\" + username;
            DirectoryEntry entry = new DirectoryEntry( ldapRoot, domainAndUsername, password );

            bool isAuthenticated = false;
            try
            {
                // Bind to the native AdsObject to force authentication.
                Object obj = entry.NativeObject;

                DirectorySearcher search = new DirectorySearcher( entry );
                search.Filter = "(sAMAccountName=" + username + ")";
                search.PropertiesToLoad.Add( "cn" );
                SearchResult result = search.FindOne();

                isAuthenticated = result != null;
            }
            catch( Exception ex )
            {
                throw new Exception( "Error authenticating user. " + ex.Message );
            }

            return isAuthenticated;
        }
    }
}