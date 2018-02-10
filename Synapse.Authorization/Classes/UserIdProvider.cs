using System;
using System.Collections.Generic;
using System.Linq;

using Synapse.Core.Utilities;
using Synapse.Services;


public class UserIdProvider : IAuthorizationProvider
{
    public List<string> Allowed { get; set; }
    public List<string> Denied { get; set; }

    public string ListSourcePath { get; set; }

    public Dictionary<string, string> Configure(IAuthorizationProviderConfig conifg)
    {
        if( conifg != null )
        {
            string s = YamlHelpers.Serialize( conifg.Config );
            UserIdProvider uidp = YamlHelpers.Deserialize<UserIdProvider>( s );

            Allowed = uidp.Allowed;
            Denied = uidp.Denied;
            ListSourcePath = uidp.ListSourcePath;
        }

        return null;
    }

    public object GetDefaultConfig()
    {
        return new UserIdProvider();
    }

    public bool? IsAuthorized(string id)
    {
        bool? found = null;

        if( Denied != null && Denied.Count > 0 )
            found = Denied.Contains( id, StringComparer.OrdinalIgnoreCase );
        if( found.HasValue && found.Value )
            return false;

        if( Allowed != null && Allowed.Count > 0 )
            found = Allowed.Contains( id, StringComparer.OrdinalIgnoreCase );
        if( found.HasValue && found.Value )
            return true;

        return found;
    }
}