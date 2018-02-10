using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Synapse.Core.Utilities;
using Synapse.Services;

using YamlDotNet.Serialization;

public class UserIdProvider : IAuthorizationProvider
{
    static Dictionary<int, UserIdProvider> _cache = new Dictionary<int, UserIdProvider>();

    UserIdProvider _inner = null;


    [YamlIgnore]
    internal DateTime ListLastWriteTime { get; set; } = DateTime.MinValue;


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
            int hash = conifg.Config.GetHashCode();
            if( !_cache.ContainsKey( hash ) || _cache[hash] == null )
            {
                string s = YamlHelpers.Serialize( conifg.Config );
                _cache[hash] = _inner = YamlHelpers.Deserialize<UserIdProvider>( s );
            }
            else
                _inner = _cache[hash];

            //if external source declared, merge contents
            if( !string.IsNullOrWhiteSpace( _inner.ListSourcePath ) && File.Exists( _inner.ListSourcePath ) )
            {
                DateTime lastWriteTime = File.GetLastWriteTimeUtc( _inner.ListSourcePath );
                if( !lastWriteTime.Equals( _inner.ListLastWriteTime ) )
                {
                    string s = YamlHelpers.Serialize( conifg.Config );
                    _inner = YamlHelpers.Deserialize<UserIdProvider>( s );

                    _inner.ListLastWriteTime = lastWriteTime;

                    UserIdProvider listSource = YamlHelpers.DeserializeFile<UserIdProvider>( _inner.ListSourcePath );

                    if( listSource.HasAllowed )
                    {
                        if( _inner.Allowed == null )
                            _inner.Allowed = new List<string>();
                        _inner.Allowed.AddRange( listSource.Allowed );
                    }

                    if( listSource.HasDenied )
                    {
                        if( _inner.Denied == null )
                            _inner.Denied = new List<string>();
                        _inner.Denied.AddRange( listSource.Denied );
                    }

                    _cache[hash] = _inner;
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

        if( _inner.HasDenied )
            found = _inner.Denied.Contains( id, StringComparer.OrdinalIgnoreCase );
        if( found.HasValue && found.Value )
            return false;

        if( _inner.HasAllowed )
            found = _inner.Allowed.Contains( id, StringComparer.OrdinalIgnoreCase );
        if( found.HasValue && found.Value )
            return true;

        return found;
    }
}