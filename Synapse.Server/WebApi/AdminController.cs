using System;
using System.Collections.Generic;
using System.IO;
using netHttp = System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Web.Http;
using System.Web.Http.Routing;
using System.Net.Http.Headers;

using Synapse.Common.WebApi;


namespace Synapse.Services
{
    [RoutePrefix( "synapse/admin" )]
    public class AdminController : ApiController
    {
        /// <summary>
        /// Returns 'Hello World'.
        /// </summary>
        /// <param name="log">Option to create entry in logs.</param>
        /// <returns></returns>
        [AdminAuthorizer]
        [HttpGet]
        [Route( "hello" )]
        public string Hello(bool log = true)
        {
            string context = GetContext( nameof( Hello ) );

            try
            {
                if( log )
                    SynapseServer.Logger.Debug( context );
                return "Hello from AdminController, World!";
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [HttpGet]
        [Route( "hello/whoami" )]
        public string WhoAmI()
        {
            string context = GetContext( nameof( WhoAmI ) );

            try
            {
                SynapseServer.Logger.Debug( context );
                return CurrentUserName;
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [HttpGet]
        [Route( "hello/about" )]
        public ServerAboutData About(bool asCsv = false)
        {
            string context = GetContext( nameof( About ) );

            try
            {
                SynapseServer.Logger.Debug( context );

                ServerAboutData about = new ServerAboutData() { Config = SynapseServer.Config };
                about.GetFiles( asCsv );

                return about;
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        #region AutoUpdate
        [HttpGet]
        [Route( "update" )]
        [Authorize]
        public List<AutoUpdaterMessage> AutoUpdate(bool drainstopNode = true)
        {
            string context = GetContext( nameof( AutoUpdate ), "Role", SynapseServer.Config.Service.Role, nameof( drainstopNode ), drainstopNode );

            try
            {
                SynapseServer.Logger.Info( context );

                if( SynapseServer.Config.Service.IsRoleNode && drainstopNode )
                    new NodeController().Drainstop();

                return AutoUpdater.Update();
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [HttpGet]
        [Route( "update/logs" )]
        public List<AutoUpdaterMessage> FetchAutoUpdateLogList()
        {
            string context = GetContext( nameof( FetchAutoUpdateLogList ) );

            try
            {
                SynapseServer.Logger.Debug( context );
                return AutoUpdater.FetchLogList();
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [HttpGet]
        [Route( "update/logs/{name}" )]
        public List<AutoUpdaterMessage> FetchAutoUpdateLog(string name = null)
        {
            string context = GetContext( nameof( FetchAutoUpdateLog ), nameof( name ), name );

            try
            {
                SynapseServer.Logger.Debug( context );
                return AutoUpdater.FetchLog( name );
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }
        #endregion


        #region logs
        [HttpGet]
        [Route( "logs" )]
        public List<AutoUpdaterMessage> FetchSynapseLogList()
        {
            string context = GetContext( nameof( FetchSynapseLogList ) );

            try
            {
                SynapseServer.Logger.Debug( context );
                return Log4netUtil.FetchLogList();
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [HttpGet]
        [Route( "logs/{name}" )]
        public netHttp.HttpResponseMessage FetchSynapseLog(string name)
        {
            string context = GetContext( nameof( FetchSynapseLogList ) );

            try
            {
                if( string.IsNullOrWhiteSpace( name ) )
                    throw new ArgumentNullException( nameof( name ), $"Logfile name is required." );

                string path = Log4netUtil.GetLogfilePath( name );

                if( !File.Exists( path ) )
                    throw new FileNotFoundException( $"Log [{name}] not found.", name );

                FileStream stream = new FileStream( path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite );
                netHttp.HttpResponseMessage msg = new netHttp.HttpResponseMessage( System.Net.HttpStatusCode.OK )
                {
                    Content = new netHttp.StreamContent( stream )
                };
                msg.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue( "attachment" )
                {
                    FileName = name
                };
                msg.Content.Headers.ContentType = new MediaTypeHeaderValue( "application/octet-stream" );

                return msg;
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }
        #endregion


        #region utility methods
        string GetContext(string context, params object[] parms)
        {
            StringBuilder c = new StringBuilder();
            c.Append( $"{context}(" );
            for( int i = 0; i < parms.Length; i += 2 )
                c.Append( $"{parms[i]}: {parms[i + 1]}, " );

            return $"{c.ToString().TrimEnd( ',', ' ' )})";
        }

        string GetContext(string context, Dictionary<string, object> d)
        {
            StringBuilder c = new StringBuilder();
            c.Append( $"{context}(" );
            foreach( string key in d.Keys )
                c.Append( $"{key}: {d[key]}, " );

            return $"{c.ToString().TrimEnd( ',', ' ' )})";
        }

        string CurrentUserName
        {
            get
            {
                return CurrentUser?.Identity?.Name ?? "Anonymous";
            }
        }

        UrlHelper _currentUrl = null;
        public UrlHelper CurrentUrl
        {
            get { return _currentUrl ?? this.Url; }
            set { _currentUrl = value; }
        }

        IPrincipal _currentUser = null;
        public IPrincipal CurrentUser
        {
            get { return _currentUser ?? this.User; }
            set { _currentUser = value; }
        }

        AuthenticationHeaderValue _authenticationHeader = null;
        public AuthenticationHeaderValue AuthenticationHeader
        {
            get { return _authenticationHeader ?? this?.Request?.Headers?.Authorization; }
            set { _authenticationHeader = value; }
        }
        #endregion
    }
}