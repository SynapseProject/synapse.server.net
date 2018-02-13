using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Synapse.Authorization;
using Synapse.Core.Utilities;
using Synapse.Services;

using YamlDotNet.Serialization;


public class WindowsPrincipalProvider : IAuthorizationProvider
{
    static Dictionary<int, WindowsPrincipalProvider> _cache = new Dictionary<int, WindowsPrincipalProvider>();

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

    public string LdapRoot { get; set; }
    [YamlIgnore]
    internal bool HasLdapRoot { get { return !string.IsNullOrWhiteSpace( LdapRoot ); } }

    private bool HasContent { get { return HasUsers || HasGroups; } }


    public void Configure(IAuthorizationProviderConfig conifg)
    {
        if( conifg != null )
        {
            int hash = conifg.Config.GetHashCode();
            if( !_cache.ContainsKey( hash ) || _cache[hash] == null )
            {
                string s = YamlHelpers.Serialize( conifg.Config );
                _cache[hash] = YamlHelpers.Deserialize<WindowsPrincipalProvider>( s );
            }

            Configure( _cache[hash] );

            //if external source declared, merge contents
            if( !string.IsNullOrWhiteSpace( ListSourcePath ) && File.Exists( ListSourcePath ) )
            {
                DateTime lastWriteTime = File.GetLastWriteTimeUtc( ListSourcePath );
                if( !lastWriteTime.Equals( ListSourceLastWriteTime ) )
                {
                    string s = YamlHelpers.Serialize( conifg.Config );
                    WindowsPrincipalProvider p = YamlHelpers.Deserialize<WindowsPrincipalProvider>( s );
                    Configure( p, lastWriteTime );
                    

                    WindowsPrincipalProvider listSource = YamlHelpers.DeserializeFile<WindowsPrincipalProvider>( ListSourcePath );

                    if( listSource.HasUsers )
                    {
                        EnsureUsersGroups( isUsers: true );

                        if( listSource.Users.HasAllowed )
                            Users.Allowed.AddRange( listSource.Users.Allowed );

                        if( listSource.Users.HasDenied )
                            Users.Denied.AddRange( listSource.Users.Denied );
                    }
                    if( listSource.HasGroups )
                    {
                        EnsureUsersGroups( isUsers: false );

                        if( listSource.Groups.HasAllowed )
                            Groups.Allowed.AddRange( listSource.Groups.Allowed );

                        if( listSource.Groups.HasDenied )
                            Groups.Denied.AddRange( listSource.Groups.Denied );
                    }

                    _cache[hash] = this;
                }
            }
        }
    }

    private void Configure(WindowsPrincipalProvider p, DateTime? listSourceLastWriteTime = null)
    {
        Users = p.Users;
        Groups = p.Groups;
        LdapRoot = p.LdapRoot;
        ListSourcePath = p.ListSourcePath;
        ListSourceLastWriteTime = listSourceLastWriteTime ?? p.ListSourceLastWriteTime;
    }

    private void EnsureUsersGroups(bool isUsers)
    {
        if( isUsers )
        {
            if( Users == null )
                Users = new PrincipalList();
            if( Users.Allowed == null )
                Users.Allowed = new List<string>();
            if( Users.Denied == null )
                Users.Denied = new List<string>();
        }

        if( !isUsers )
        {
            if( Groups == null )
                Groups = new PrincipalList();
            if( Groups.Allowed == null )
                Groups.Allowed = new List<string>();
            if( Groups.Denied == null )
                Groups.Denied = new List<string>();
        }
    }

    public object GetDefaultConfig()
    {
        return new WindowsPrincipalProvider();
    }

    public bool? IsAuthorized_(string id)
    {
        if( !HasContent )
            return true;

        bool? found = null;

        List<string> groupMembership = null;
        if( HasGroups && HasLdapRoot )
            groupMembership = Synapse.Services.Authorization.Utilities.GetNtGroupMembership( userName: id, ldapRoot: LdapRoot );
        bool haveGroupMembership = groupMembership != null && groupMembership.Count > 0;

        //process Denies
        if( HasUsersDenied )
        {
            found = Users.Denied.Contains( id, StringComparer.OrdinalIgnoreCase );
            if( found.HasValue && found.Value )
                return false;
        }

        if( HasGroupsDenied )
        {
            if( haveGroupMembership )
            {
                IEnumerable<string> denied = from member in groupMembership
                                             join grp in Groups.Denied
                                             on member.ToLower() equals grp.ToLower()
                                             select member;
                found = denied.Count() > 0;
            }
            else
                found = true;  //no groupMembership == implied Deny

            if( found.HasValue && found.Value )
                return false;
        }

        //process Allows
        if( HasUsersAllowed )
        {
            found = Users.Allowed.Contains( id, StringComparer.OrdinalIgnoreCase );
            if( found.HasValue && found.Value )
                return true;
        }

        if( HasGroupsAllowed )
        {
            if( haveGroupMembership )
            {
                IEnumerable<string> allowed = from member in groupMembership
                                              join grp in Groups.Allowed
                                              on member.ToLower() equals grp.ToLower()
                                              select member;
                found = allowed.Count() > 0;
            }
            else
                found = false;  //no groupMembership == implied Deny

            if( found.HasValue && found.Value )
                return true;
        }

        //if we got here, the user id wasn't specified in Denied and wasn't specifically Allowed
        //if either of these is true (HasUsers/HasGroups), we take omission as implied Deny
        if( HasUsersAllowed || HasGroupsAllowed )
            return false;

        //if we got here, the user id wasn't specified in Denied and Allowed wasn't declared at all
        //in this case omission translates to Allow
        return found;
    }

    public AuthorizationType IsAuthorized(string id)
    {
        if( !HasContent )
            return AuthorizationType.ImplicitAllow;

        bool? found = null;
        AuthorizationType result = AuthorizationType.None;

        List<string> groupMembership = null;
        if( HasGroups && HasLdapRoot )
            groupMembership = Synapse.Services.Authorization.Utilities.GetNtGroupMembership( userName: id, ldapRoot: LdapRoot );
        bool haveGroupMembership = groupMembership != null && groupMembership.Count > 0;

        //process Denies
        if( HasUsersDenied )
        {
            found = Users.Denied.Contains( id, StringComparer.OrdinalIgnoreCase );
            if( found.HasValue && found.Value )
                return AuthorizationType.ExplicitDeny; //explicit deny is final, return now
        }

        if( HasGroupsDenied )
        {
            if( haveGroupMembership )
            {
                IEnumerable<string> denied = from member in groupMembership
                                             join grp in Groups.Denied
                                             on member.ToLower() equals grp.ToLower()
                                             select member;
                found = denied.Count() > 0;
                if( found.Value )
                    return AuthorizationType.ExplicitDeny;  //explicit deny is final, return now
            }
            else
                result |= AuthorizationType.ImplicitAllow;  //no groupMembership == implied Allow (user not specifically denied)
        }

        //process Allows
        if( HasUsersAllowed )
        {
            found = Users.Allowed.Contains( id, StringComparer.OrdinalIgnoreCase );
            if( found.HasValue && found.Value )
                result |= AuthorizationType.ExplicitAllow;
        }

        if( HasGroupsAllowed )
        {
            if( haveGroupMembership )
            {
                IEnumerable<string> allowed = from member in groupMembership
                                              join grp in Groups.Allowed
                                              on member.ToLower() equals grp.ToLower()
                                              select member;
                found = allowed.Count() > 0;
                if( found.Value )
                    result |= AuthorizationType.ExplicitAllow;
            }
            else
                result |= AuthorizationType.ImplicitDeny;  //no groupMembership == implied Deny
        }

        //if we got here, the user id wasn't specified in Denied and wasn't specifically Allowed
        //if either of these is true (HasUsers/HasGroups), we take omission as implied Deny
        if( (HasUsersAllowed || HasGroupsAllowed) && (result & AuthorizationType.GeneralAllow) != AuthorizationType.GeneralAllow )
            result |= AuthorizationType.ImplicitDeny;

        return result;
    }
}