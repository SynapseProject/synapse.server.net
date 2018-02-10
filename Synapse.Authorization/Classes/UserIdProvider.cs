using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Synapse.Core.Utilities;
using Synapse.Services;

using YamlDotNet.Serialization;

public class UserIdProvider : IAuthorizationProvider
{
    public List<string> Allowed { get; set; }
    [YamlIgnore]
    public bool HasAllowed { get { return Allowed != null && Allowed.Count > 0; } }

    public List<string> Denied { get; set; }
    [YamlIgnore]
    public bool HasDenied { get { return Denied != null && Denied.Count > 0; } }

    public string ListSourcePath { get; set; }


    public Dictionary<string, string> Configure(IAuthorizationProviderConfig conifg)
    {
        if( conifg != null )
        {
            string s = YamlHelpers.Serialize( conifg.Config );
            UserIdProvider uidp = YamlHelpers.Deserialize<UserIdProvider>( s );

            //initialize with whatever is declared in synapse.server.config.yaml
            Allowed = uidp.Allowed;
            Denied = uidp.Denied;
            ListSourcePath = uidp.ListSourcePath;

            //if external source declared, merge contents
            if( !string.IsNullOrWhiteSpace( ListSourcePath ) && File.Exists( ListSourcePath ) )
            {
                UserIdProvider listSource = YamlHelpers.DeserializeFile<UserIdProvider>( ListSourcePath );
                if( listSource.HasAllowed )
                {
                    if( Allowed == null )
                        Allowed = new List<string>();
                    Allowed.AddRange( listSource.Allowed );
                }

                if( listSource.HasDenied )
                {
                    if( Denied == null )
                        Denied = new List<string>();
                    Denied.AddRange( listSource.Denied );
                }
            }
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

        if( HasDenied )
            found = Denied.Contains( id, StringComparer.OrdinalIgnoreCase );
        if( found.HasValue && found.Value )
            return false;

        if( HasAllowed )
            found = Allowed.Contains( id, StringComparer.OrdinalIgnoreCase );
        if( found.HasValue && found.Value )
            return true;

        return found;
    }
}