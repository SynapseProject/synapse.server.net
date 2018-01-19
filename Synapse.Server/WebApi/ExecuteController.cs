using System;
using System.Collections.Generic;
using System.IO;
using netHttp = System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Web.Http;
using System.Web.Http.Routing;
using System.Net.Http.Headers;

using Synapse.Core;
using Synapse.Common.WebApi;
using Synapse.Core.Utilities;
using Synapse.Common;

namespace Synapse.Services
{
    [RoutePrefix( "synapse/execute" )]
    public class ExecuteController : ApiController, IExecuteController
    {
        PlanServer _server = null;

        [HttpGet]
        [Route( "hello" )]
        public string Hello(bool log = true)
        {
            string context = GetContext( nameof( Hello ) );

            try
            {
                if( log )
                    SynapseServer.Logger.Debug( context );
                return "Hello from SynapseController, World!";
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

        [HttpGet]
        [Route( "{planUniqueName}/item" )]
        public Plan GetPlan(string planUniqueName)
        {
            InitPlanServer();

            string context = GetContext( nameof( GetPlanList ), nameof( planUniqueName ), planUniqueName );

            try
            {
                SynapseServer.Logger.Debug( context );
                return _server.GetPlan( planUniqueName );
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [HttpGet]
        [Route( "" )]
        public IEnumerable<string> GetPlanList(string filter = null, bool isRegexFilter = true)
        {
            InitPlanServer();

            string context = GetContext( nameof( GetPlanList ), nameof( filter ), filter, nameof( isRegexFilter ), isRegexFilter );

            try
            {
                SynapseServer.Logger.Debug( context );
                return _server.GetPlanList( filter, isRegexFilter );
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planUniqueName}/" )]
        [HttpGet]
        public IEnumerable<long> GetPlanInstanceIdList(string planUniqueName)
        {
            InitPlanServer();

            string context = GetContext( nameof( GetPlanInstanceIdList ), nameof( planUniqueName ), planUniqueName );

            try
            {
                SynapseServer.Logger.Debug( context );
                return _server.GetPlanInstanceIdList( planUniqueName );
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planUniqueName}/start/" )]
        [HttpGet]
        public long StartPlan(string planUniqueName, bool dryRun = false, string requestNumber = null, string nodeRootUrl = null)
        {
            InitPlanServer();

            if( !string.IsNullOrWhiteSpace( requestNumber ) ) { requestNumber = System.Web.HttpUtility.UrlDecode( requestNumber ); }
            if( !string.IsNullOrWhiteSpace( nodeRootUrl ) ) { requestNumber = System.Web.HttpUtility.UrlDecode( nodeRootUrl ); }

            Uri uri = CurrentUrl.Request.RequestUri;
            string context = GetContext( nameof( StartPlan ), nameof( CurrentUserName ), CurrentUserName,
                nameof( planUniqueName ), planUniqueName, nameof( dryRun ), dryRun,
                nameof( requestNumber ), requestNumber, nameof( nodeRootUrl ), nodeRootUrl, "QueryString", uri.Query );

            Impersonator runAsUser = null;
            if( SynapseServer.UseImpersonation( CurrentUser?.Identity ) )
            {
                if( Request?.Headers?.Authorization?.Scheme?.ToLower() == "basic" )
                {
                    runAsUser = new Impersonator( this.AuthenticationHeader );
                }
                else
                    runAsUser = new Impersonator( (WindowsIdentity)(CurrentUser?.Identity) );
                runAsUser.Start( SynapseServer.Logger );
            }

            try
            {
                SynapseServer.Logger.Debug( context );
                Dictionary<string, string> dynamicParameters = uri.ParseQueryString();
                if( dynamicParameters.ContainsKey( nameof( dryRun ) ) ) dynamicParameters.Remove( nameof( dryRun ) );
                return _server.StartPlan( CurrentUserName, planUniqueName, dryRun, requestNumber, dynamicParameters, nodeRootUrl: nodeRootUrl,
                    referrer: CurrentUrl.Request.RequestUri, authHeader: this.AuthenticationHeader );
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
            finally
            {
                runAsUser?.Stop( SynapseServer.Logger );
            }
        }

        [Route( "{planUniqueName}/start/" )]
        [HttpPost]
        public long StartPlan([FromBody]StartPlanEnvelope planEnvelope, string planUniqueName, bool dryRun = false, string requestNumber = null, string nodeRootUrl = null)
        {
            InitPlanServer();

            if( !string.IsNullOrWhiteSpace( requestNumber ) ) { requestNumber = System.Web.HttpUtility.UrlDecode( requestNumber ); }
            if( !string.IsNullOrWhiteSpace( nodeRootUrl ) ) { requestNumber = System.Web.HttpUtility.UrlDecode( nodeRootUrl ); }

            bool failedToDeserialize = false;
            Dictionary<string, string> dynamicParameters = planEnvelope?.DynamicParameters;

            StringBuilder parms = new StringBuilder();
            if( dynamicParameters != null )
            {
                string s = string.Empty;
                foreach( string key in dynamicParameters.Keys )
                {
                    parms.Append( $"{s}{key}: {dynamicParameters[key]}" );
                    s = ", ";
                }
            }
            else
            {
                string rawBody = CurrentUrl.Request.Properties["body"].ToString();
                failedToDeserialize = !string.IsNullOrWhiteSpace( rawBody );
                if( failedToDeserialize )
                    parms.Append( rawBody );
            }

            string context = GetContext( nameof( StartPlan ), nameof( CurrentUserName ), CurrentUserName,
                nameof( planUniqueName ), planUniqueName, nameof( dryRun ), dryRun,
                nameof( requestNumber ), requestNumber, nameof( nodeRootUrl ), nodeRootUrl, "planParameters", parms.ToString() );

            Impersonator runAsUser = null;
            if( SynapseServer.UseImpersonation( CurrentUser?.Identity ) )
            {
                if( Request?.Headers?.Authorization?.Scheme?.ToLower() == "basic" )
                {
                    runAsUser = new Impersonator( this.AuthenticationHeader );
                }
                else
                    runAsUser = new Impersonator( (WindowsIdentity)(CurrentUser?.Identity) );
                runAsUser.Start( SynapseServer.Logger );
            }

            try
            {
                SynapseServer.Logger.Info( context );

                if( failedToDeserialize )
                    throw new Exception( $"Failed to deserialize message body:\r\n{parms.ToString()}" );

                return _server.StartPlan( CurrentUserName, planUniqueName, dryRun, requestNumber, dynamicParameters,
                    postDynamicParameters: true, nodeRootUrl: nodeRootUrl, referrer: CurrentUrl.Request.RequestUri, authHeader: this.AuthenticationHeader );
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
            finally
            {
                runAsUser?.Stop( SynapseServer.Logger );
            }
        }

        #region StartPlanSync
        [Route( "{planUniqueName}/start/sync/" )]
        [HttpGet]
        public object StartPlanSync(string planUniqueName, bool dryRun = false, string requestNumber = null,
            string path = "Actions[0]:Result:ExitData", SerializationType serializationType = SerializationType.Json,
            bool setContentType = true, int pollingIntervalSeconds = 1, int timeoutSeconds = 120, string nodeRootUrl = null)
        {
            if( !string.IsNullOrWhiteSpace( requestNumber ) ) { requestNumber = System.Web.HttpUtility.UrlDecode( requestNumber ); }
            if( !string.IsNullOrWhiteSpace( nodeRootUrl ) ) { requestNumber = System.Web.HttpUtility.UrlDecode( nodeRootUrl ); }

            Uri uri = CurrentUrl.Request.RequestUri;
            string context = GetContext( nameof( StartPlanSync ), nameof( CurrentUserName ), CurrentUserName,
                nameof( planUniqueName ), planUniqueName, nameof( dryRun ), dryRun, nameof( requestNumber ), requestNumber,
                nameof( path ), path, nameof( serializationType ), serializationType, nameof( setContentType ), setContentType,
                nameof( pollingIntervalSeconds ), pollingIntervalSeconds, nameof( timeoutSeconds ), timeoutSeconds,
                nameof( nodeRootUrl ), nodeRootUrl, "QueryString", uri.Query );
            SynapseServer.Logger.Debug( context );

            long instanceId = StartPlan( planUniqueName, dryRun, requestNumber, nodeRootUrl );

            return WaitForTerminalStatusOrTimeout( instanceId, planUniqueName, path, serializationType,
                pollingIntervalSeconds, timeoutSeconds, setContentType );
        }

        [Route( "{planUniqueName}/start/sync/" )]
        [HttpPost]
        public object StartPlanSync([FromBody]StartPlanEnvelope planEnvelope, string planUniqueName, bool dryRun = false, string requestNumber = null,
            string path = "Actions[0]:Result:ExitData", SerializationType serializationType = SerializationType.Json,
            bool setContentType = true, int pollingIntervalSeconds = 1, int timeoutSeconds = 120, string nodeRootUrl = null)
        {
            if( !string.IsNullOrWhiteSpace( requestNumber ) ) { requestNumber = System.Web.HttpUtility.UrlDecode( requestNumber ); }
            if( !string.IsNullOrWhiteSpace( nodeRootUrl ) ) { requestNumber = System.Web.HttpUtility.UrlDecode( nodeRootUrl ); }

            string context = GetContext( nameof( StartPlanSync ), nameof( CurrentUserName ), CurrentUserName,
                nameof( planUniqueName ), planUniqueName, nameof( dryRun ), dryRun, nameof( requestNumber ), requestNumber,
                nameof( path ), path, nameof( serializationType ), serializationType, nameof( setContentType ), setContentType,
                nameof( pollingIntervalSeconds ), pollingIntervalSeconds, nameof( timeoutSeconds ), timeoutSeconds,
                nameof( nodeRootUrl ), nodeRootUrl );
            SynapseServer.Logger.Debug( context );

            long instanceId = StartPlan( planEnvelope, planUniqueName, dryRun, requestNumber, nodeRootUrl );

            return WaitForTerminalStatusOrTimeout( instanceId, planUniqueName, path, serializationType,
                pollingIntervalSeconds, timeoutSeconds, setContentType );
        }

        object WaitForTerminalStatusOrTimeout(long instanceId, string planUniqueName, string path, SerializationType serializationType,
            int pollingIntervalSeconds, int timeoutSeconds, bool setContentType)
        {
            object result = SyncExecuteHelper.WaitForTerminalStatusOrTimeout(
                this, planUniqueName, instanceId, path, serializationType, pollingIntervalSeconds, timeoutSeconds );

            if( setContentType )
            {
                Encoding encoding = serializationType == SerializationType.Xml ? Encoding.Unicode : Encoding.UTF8;
                netHttp.HttpResponseMessage response = new netHttp.HttpResponseMessage( System.Net.HttpStatusCode.OK );
                response.Content = new netHttp.StringContent( GetStringContent( result, serializationType ),
                    encoding, SerializationContentType.GetContentType( serializationType ) );
                return response;
            }
            else
            {
                return result;
            }
        }

        string GetStringContent(object content, SerializationType serializationType)
        {
            switch( serializationType )
            {
                case SerializationType.Xml: { return XmlHelpers.Serialize<string>( content, omitXmlDeclaration: false, omitXmlNamespace: true ); }
                default: { return content.ToString(); }
            }
        }
        #endregion


        [Route( "{planUniqueName}/{planInstanceId:long}/" )]
        [HttpGet]
        public Plan GetPlanStatus(string planUniqueName, long planInstanceId)
        {
            InitPlanServer();

            string context = GetContext( nameof( GetPlanStatus ),
                nameof( planUniqueName ), planUniqueName, nameof( planInstanceId ), planInstanceId );

            try
            {
                SynapseServer.Logger.Debug( context );
                return _server.GetPlanStatus( planUniqueName, planInstanceId );
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planUniqueName}/{planInstanceId:long}/" )]
        [HttpPost]
        public void SetStatus(string planUniqueName, long planInstanceId, [FromBody]string planString)
        {
            InitPlanServer();

            string context = GetContext( nameof( SetStatus ),
                nameof( planUniqueName ), planUniqueName, nameof( planInstanceId ), planInstanceId,
                nameof( planString ), planString );

            planString = CryptoHelpers.Decode( planString );
            Plan plan = Plan.FromYaml( new StringReader( planString ) );

            try
            {
                SynapseServer.Logger.Debug( context );
                _server.UpdatePlanStatus( plan );
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planUniqueName}/{planInstanceId:long}/action/" )]
        [HttpPost]
        public void SetStatus(string planUniqueName, long planInstanceId, [FromBody]ActionItem actionItem)
        {
            InitPlanServer();

            string context = GetContext( nameof( SetStatus ),
                nameof( planUniqueName ), planUniqueName, nameof( planInstanceId ), planInstanceId,
                nameof( actionItem ), actionItem );

            try
            {
                SynapseServer.Logger.Debug( context );
                _server.UpdatePlanActionStatus( planUniqueName, planInstanceId, actionItem );
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planUniqueName}/{planInstanceId:long}/" )]
        [HttpDelete]
        public void CancelPlan(string planUniqueName, long planInstanceId, string nodeRootUrl = null)
        {
            InitPlanServer();

            string context = GetContext( nameof( GetPlanStatus ),
                nameof( planUniqueName ), planUniqueName, nameof( planInstanceId ), planInstanceId,
                nameof( nodeRootUrl ), nodeRootUrl );

            try
            {
                SynapseServer.Logger.Debug( context );
                _server.CancelPlan( planInstanceId, nodeRootUrl, referrer: CurrentUrl.Request.RequestUri );
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planUniqueName}/{planInstanceId:long}/part/" )]
        [HttpGet]
        public object GetPlanElements(string planUniqueName, long planInstanceId, string elementPath, SerializationType serializationType = SerializationType.Json, bool setContentType = true)
        {
            InitPlanServer();

            string context = GetContext( nameof( GetPlanStatus ),
                nameof( planUniqueName ), planUniqueName, nameof( planInstanceId ), planInstanceId,
                nameof( elementPath ), elementPath, nameof( serializationType ), serializationType );

            try
            {
                SynapseServer.Logger.Debug( context );

                PlanElementParms pep = new PlanElementParms();
                pep.Type = serializationType;
                pep.ElementPaths.Add( elementPath );

                object result = _server.GetPlanElements( planUniqueName, planInstanceId, pep );

                if( setContentType )
                {
                    Encoding encoding = serializationType == SerializationType.Xml ? Encoding.Unicode : Encoding.UTF8;
                    netHttp.HttpResponseMessage response = new netHttp.HttpResponseMessage( System.Net.HttpStatusCode.OK );
                    response.Content = new netHttp.StringContent( GetStringContent( result, serializationType ),
                        encoding, SerializationContentType.GetContentType( serializationType ) );
                    return response;
                }
                else
                {
                    return result;
                }
            }
            catch( Exception ex )
            {
                SynapseServer.Logger.Error(
                    Utilities.UnwindException( context, ex, asSingleLine: true ) );
                throw;
            }
        }

        [Route( "{planUniqueName}/{planInstanceId:long}/part/" )]
        [HttpPost]
        public object GetPlanElements(string planUniqueName, long planInstanceId, [FromBody]PlanElementParms elementParms)
        {
            InitPlanServer();

            string context = GetContext( nameof( GetPlanStatus ),
                nameof( planUniqueName ), planUniqueName, nameof( planInstanceId ), planInstanceId );

            try
            {
                SynapseServer.Logger.Debug( context );
                return _server.GetPlanElements( planUniqueName, planInstanceId, elementParms );
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
        public List<AutoUpdaterMessage> AutoUpdate()
        {
            string context = GetContext( nameof( AutoUpdate ) );

            try
            {
                SynapseServer.Logger.Info( context );
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

        #region utility methods
        void InitPlanServer()
        {
            if( _server == null )
                _server = new PlanServer();
        }

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