using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Synapse.Core.Utilities;
using Synapse.Services;

using YamlDotNet.Serialization;

public class WindowsUserProvider : IAuthorizationProvider
{
    static Dictionary<int, WindowsUserProvider> _cache = new Dictionary<int, WindowsUserProvider>();

    WindowsUserProvider _inner = null;


    [YamlIgnore]
    internal DateTime ListLastWriteTime { get; set; } = DateTime.MinValue;


    public List<string> Allowed { get; set; }
    [YamlIgnore]
    public bool HasAllowed { get { return Allowed != null && Allowed.Count > 0; } }

    public List<string> Denied { get; set; }
    [YamlIgnore]
    public bool HasDenied { get { return Denied != null && Denied.Count > 0; } }

    public string ListSourcePath { get; set; }


    public void Configure(IAuthorizationProviderConfig conifg)
    {
        if( conifg != null )
        {
            int hash = conifg.Config.GetHashCode();
            if( !_cache.ContainsKey( hash ) || _cache[hash] == null )
            {
                string s = YamlHelpers.Serialize( conifg.Config );
                _cache[hash] = _inner = YamlHelpers.Deserialize<WindowsUserProvider>( s );
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
                    _inner = YamlHelpers.Deserialize<WindowsUserProvider>( s );

                    _inner.ListLastWriteTime = lastWriteTime;

                    WindowsUserProvider listSource = YamlHelpers.DeserializeFile<WindowsUserProvider>( _inner.ListSourcePath );

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
    }

    public object GetDefaultConfig()
    {
        return new WindowsUserProvider();
    }

    public bool? IsAuthorized(string id)
    {
        if( _inner == null )
            return true;

        bool? found = null;

        if( _inner.HasDenied )
            found = _inner.Denied.Contains( id, StringComparer.OrdinalIgnoreCase );
        if( found.HasValue && found.Value )
            return false;

        if( _inner.HasAllowed )
            found = _inner.Allowed.Contains( id, StringComparer.OrdinalIgnoreCase );
        if( found.HasValue )
            return found.Value;

        return found;
    }
}