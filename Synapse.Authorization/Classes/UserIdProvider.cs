using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Synapse.Core.Utilities;
using Synapse.Services;

using YamlDotNet.Serialization;

public class UserIdProvider : IAuthorizationProvider
{
    static UserIdProvider _listSource = null;
    static DateTime _listLastWriteTime = DateTime.MinValue;


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
                DateTime lastWriteTime = File.GetLastWriteTimeUtc( ListSourcePath );
                if( _listSource == null || !lastWriteTime.Equals( _listLastWriteTime ) )
                {
                    _listLastWriteTime = lastWriteTime;
                    _listSource = YamlHelpers.DeserializeFile<UserIdProvider>( ListSourcePath );
                }

                if( _listSource.HasAllowed )
                {
                    if( Allowed == null )
                        Allowed = new List<string>();
                    Allowed.AddRange( _listSource.Allowed );
                }

                if( _listSource.HasDenied )
                {
                    if( Denied == null )
                        Denied = new List<string>();
                    Denied.AddRange( _listSource.Denied );
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