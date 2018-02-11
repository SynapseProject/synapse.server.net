using System;
using System.Collections.Generic;
using System.DirectoryServices;

namespace Synapse.Services.Authorization
{
    public class Utilities
    {
        public static List<string> GetNtGroupMembership(string userName, string ldapRoot, string authUser = null, string authPswd = null)
        {
            List<string> result = null;

            if( !string.IsNullOrWhiteSpace( ldapRoot ) )
            {
                string[] user = userName.Split( '\\' );
                string name = user[0];
                if( user.Length > 1 )
                {
                    name = user[1];
                }

                DirectoryEntry root = new DirectoryEntry( ldapRoot );
                if( !string.IsNullOrWhiteSpace( authUser ) && string.IsNullOrWhiteSpace( authPswd ) )
                {
                    root.Username = authUser;
                    root.Password = authPswd;
                }
                DirectorySearcher groups = new DirectorySearcher( root );
                groups.Filter = "sAMAccountName=" + name;
                groups.PropertiesToLoad.Add( "memberOf" );

                SearchResult sr = groups.FindOne();
                List<string> list = new List<string>();

                if( sr != null )
                {
                    for( int i = 0; i <= sr.Properties["memberOf"].Count - 1; i++ )
                    {
                        string group = sr.Properties["memberOf"][i].ToString();
                        list.Add( group.Split( ',' )[0].Replace( "CN=", "" ) );
                    }
                }

                result = list;
            }

            return result;
        }
    }
}