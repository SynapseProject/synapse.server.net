using System;
using System.Collections.Generic;
using System.Linq;

using Synapse.Authorization;

using YamlDotNet.Serialization;

namespace Synapse.Services
{
    public class AuthorizationConfig
    {
        public AuthorizationConfig() { }


        public bool AllowAnonymous { get; set; } = true;

        public List<AuthorizationProvider> Providers { get; set; } = new List<AuthorizationProvider>();
        [YamlIgnore]
        public bool HasProviders { get { return Providers != null && Providers.Count > 0; } }

        public bool IsAuthorized(string id, ServerRole serverRole, string topic = null)
        {
            if( string.IsNullOrWhiteSpace( id ) && !AllowAnonymous )
                return false;


            IEnumerable<AuthorizationProvider> roleProviders = null;
            if( string.IsNullOrWhiteSpace( topic ) )
                roleProviders = Providers.Where( p => p.ContainsServerRole( serverRole ) && p.ContainsNoTopics() );
            else
                roleProviders = Providers.Where( p => p.ContainsServerRole( serverRole ) && p.ContainsTopic( topic ) );


            AuthorizationType isAuthorized = AuthorizationType.None;
            foreach( AuthorizationProvider provider in roleProviders )
                isAuthorized |= provider.IsAuthorized( id );


            bool ok = false;
            if( (isAuthorized & AuthorizationType.ExplicitDeny) == AuthorizationType.ExplicitDeny )
                ok = false;
            else if( (isAuthorized & AuthorizationType.ExplicitAllow) == AuthorizationType.ExplicitAllow )
                ok = true;
            else if( (isAuthorized & AuthorizationType.ImplicitDeny) == AuthorizationType.ImplicitDeny )
                ok = false;
            else if( (isAuthorized & AuthorizationType.ImplicitAllow) == AuthorizationType.ImplicitAllow )
                ok = true;
            else if( isAuthorized == AuthorizationType.None )
                ok = true;


            if( !ok )
                ServerGlobal.Logger.Debug( $"Access Denied!  User: [{id}], Role: [{serverRole}], Topic: [{topic}]." );


            return ok;
        }
    }
}