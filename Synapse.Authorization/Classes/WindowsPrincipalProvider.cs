using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Synapse.Core.Utilities;
using Synapse.Services;

using YamlDotNet.Serialization;


public class WindowsPrincipalProvider : IAuthorizationProvider
{
    static Dictionary<int, WindowsPrincipalProvider> _cache = new Dictionary<int, WindowsPrincipalProvider>();

    WindowsPrincipalProvider _inner = null;

    public PrincipalList Users { get; set; }
    [YamlIgnore]
    public bool HasUsers { get { return Users != null && Users.HasContent; } }
    [YamlIgnore]
    public bool HasUsersAllowed { get { return Users != null && Users.HasAllowed; } }
    [YamlIgnore]
    public bool HasUsersDenied { get { return Users != null && Users.HasDenied; } }

    public PrincipalList Groups { get; set; }
    [YamlIgnore]
    public bool HasGroups { get { return Groups != null && Groups.HasContent; } }
    [YamlIgnore]
    public bool HasGroupsAllowed { get { return Groups != null && Groups.HasAllowed; } }
    [YamlIgnore]
    public bool HasGroupsDenied { get { return Groups != null && Groups.HasDenied; } }

    public string ListSourcePath { get; set; }
    [YamlIgnore]
    internal DateTime ListSourceLastWriteTime { get; set; } = DateTime.MinValue;

    public string LdapPath { get; set; }
    [YamlIgnore]
    internal bool HasLdapPath { get { return !string.IsNullOrWhiteSpace( LdapPath ); } }


    public void Configure(IAuthorizationProviderConfig conifg)
    {
        if( conifg != null )
        {
            int hash = conifg.Config.GetHashCode();
            if( !_cache.ContainsKey( hash ) || _cache[hash] == null )
            {
                string s = YamlHelpers.Serialize( conifg.Config );
                _cache[hash] = _inner = YamlHelpers.Deserialize<WindowsPrincipalProvider>( s );
            }
            else
                _inner = _cache[hash];

            //if external source declared, merge contents
            if( !string.IsNullOrWhiteSpace( _inner.ListSourcePath ) && File.Exists( _inner.ListSourcePath ) )
            {
                DateTime lastWriteTime = File.GetLastWriteTimeUtc( _inner.ListSourcePath );
                if( !lastWriteTime.Equals( _inner.ListSourceLastWriteTime ) )
                {
                    string s = YamlHelpers.Serialize( conifg.Config );
                    _inner = YamlHelpers.Deserialize<WindowsPrincipalProvider>( s );

                    _inner.ListSourceLastWriteTime = lastWriteTime;

                    WindowsPrincipalProvider listSource = YamlHelpers.DeserializeFile<WindowsPrincipalProvider>( _inner.ListSourcePath );

                    if( listSource.HasUsers )
                    {
                        if( listSource.Users.HasAllowed )
                        {
                            if( _inner.Users.Allowed == null )
                                _inner.Users.Allowed = new List<string>();
                            _inner.Users.Allowed.AddRange( listSource.Users.Allowed );
                        }

                        if( listSource.Users.HasDenied )
                        {
                            if( _inner.Users.Denied == null )
                                _inner.Users.Denied = new List<string>();
                            _inner.Users.Denied.AddRange( listSource.Users.Denied );
                        }
                    }
                    if( listSource.HasGroups )
                    {
                        if( listSource.Groups.HasAllowed )
                        {
                            if( _inner.Groups.Allowed == null )
                                _inner.Groups.Allowed = new List<string>();
                            _inner.Groups.Allowed.AddRange( listSource.Groups.Allowed );
                        }

                        if( listSource.Groups.HasDenied )
                        {
                            if( _inner.Groups.Denied == null )
                                _inner.Groups.Denied = new List<string>();
                            _inner.Groups.Denied.AddRange( listSource.Groups.Denied );
                        }
                    }

                    _cache[hash] = _inner;
                }
            }
        }
    }

    public object GetDefaultConfig()
    {
        return new WindowsPrincipalProvider();
    }

    public bool? IsAuthorized(string id)
    {
        if( _inner == null )
            return true;

        bool? found = null;

        //process Denies
        if( _inner.HasUsersDenied )
            found = _inner.Users.Denied.Contains( id, StringComparer.OrdinalIgnoreCase );
        if( found.HasValue && found.Value )
            return false;

        if( _inner.HasGroupsDenied )
            found = _inner.Groups.Denied.Contains( id, StringComparer.OrdinalIgnoreCase );
        if( found.HasValue && found.Value )
            return false;

        //process Allows
        if( _inner.HasUsersAllowed )
            found = _inner.Users.Allowed.Contains( id, StringComparer.OrdinalIgnoreCase );
        if( found.HasValue && found.Value )
            return true;

        if( _inner.HasGroupsAllowed )
            found = _inner.Groups.Allowed.Contains( id, StringComparer.OrdinalIgnoreCase );
        if( found.HasValue && found.Value )
            return true;

        //if we got here, the user id wasn't specified in Denied and wasn't specifically Allowed
        //if either of these is true (HasUsers/HasGroups), we take omission as implied Deny
        if( _inner.HasUsersAllowed || _inner.HasGroupsAllowed )
            return false;

        //if we got here, the user id wasn't specified in Denied and Allowed wasn't declared at all
        //in this case omission translates to Allow
        return found;
    }
}