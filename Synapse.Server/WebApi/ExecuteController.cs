using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Web.Http;
using System.Web.Http.Routing;
using System.Net.Http.Headers;
using netHttp = System.Net.Http;

using Synapse.Core;
using Synapse.Common.WebApi;
using Synapse.Core.Utilities;
using Synapse.Common;


namespace Synapse.Services
{
    [SynapseAuthorize( serverRole: ServerRole.Controller )]
    [RoutePrefix( "synapse/execute" )]
    public class ExecuteController : ApiController, IExecuteController
    {
        PlanServer _server = null;

        public ExecuteController()
        {
            IsControllerOrException();
        }

        void IsControllerOrException()
        {
            Exception ex = null;

            if( !SynapseServer.Config.Service.IsRoleController )
                ex = new NotSupportedException( $"This instance of Synapse is not configured as a Controller.  Check the settings at {SynapseServerConfig.FileName}." );
            else if( SynapseServer.Config.Controller == null )
                ex = new Exception( $"This instance of Synapse is missing required configuration to execute as a Controller.  Check the settings at {SynapseServerConfig.FileName}." );

            if( ex != null )
            {
                SynapseServer.Logger.Fatal( ex.Message, ex );
                throw ex;
            }
        }

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

        //[HttpGet]
        //[Route( "hello/about" )]
        //public ServerAboutData About(bool asCsv = false)
        //{
        //    string context = GetContext( nameof( About ) );

        //    try
        //    {
        //        SynapseServer.Logger.Debug( context );

        //        ServerAboutData about = new ServerAboutData() { Config = SynapseServer.Config };
        //        about.GetFiles( asCsv );

        //        return about;
        //    }
        //    catch( Exception ex )
        //    {
        //        SynapseServer.Logger.Error(
        //            Utilities.UnwindException( context, ex, asSingleLine: true ) );
        //        throw;
        //    }
        //}

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
        [Route( "{planUniqueName}/help" )]
        public List<DynamicValue> GetPlanDynamicValues(string planUniqueName, bool simplify = true)
        {
            InitPlanServer();

            string context = GetContext( nameof( GetPlanList ), nameof( planUniqueName ), planUniqueName, nameof( simplify ), simplify );

            try
            {
                SynapseServer.Logger.Debug( context );
                Plan plan = _server.GetPlan( planUniqueName );
                return plan.GetDynamicValues( simplify );
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


        #region StartPlanAsync
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


            GetPlanEnvelopeFromRawBody( ref planEnvelope );

            bool failedToDeserialize = false;
            Dictionary<string, string> dynamicParameters = planEnvelope?.TryGetCaseInsensitiveDynamicParameters();
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
                failedToDeserialize = !string.IsNullOrWhiteSpace( RawBody );
                if( failedToDeserialize )
                    parms.Append( RawBody );
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

        #region StartPlan, Async with ContentType
        [Route( "{planUniqueName}/start/async/" )]
        [HttpGet]
        public object StartPlanAsync(string planUniqueName, bool dryRun = false, string requestNumber = null, string nodeRootUrl = null)
        {
            SerializationType serializationType = IsMediaTypeApplicationXml ? SerializationType.Xml : SerializationType.Json;
            try
            {
                long pid = StartPlan( planUniqueName, dryRun, requestNumber, nodeRootUrl );
                return GetHttpResponse( new StartPlanResponse { PlanInstanceId = pid }, serializationType: serializationType );
            }
            catch( Exception ex )
            {
                string exc = Utilities.UnwindException( "StartPlanAsync", ex, asSingleLine: true );
                return GetHttpResponse( new ExceptionWrapper { Exception = exc }, serializationType: serializationType );
            }
        }

        [Route( "{planUniqueName}/start/async/" )]
        [HttpPost]
        public object StartPlanAsync([FromBody]StartPlanEnvelope planEnvelope, string planUniqueName, bool dryRun = false, string requestNumber = null, string nodeRootUrl = null)
        {
            SerializationType serializationType = IsMediaTypeApplicationXml ? SerializationType.Xml : SerializationType.Json;
            try
            {
                long pid = StartPlan( planEnvelope, planUniqueName, dryRun, requestNumber, nodeRootUrl );
                return GetHttpResponse( new StartPlanResponse { PlanInstanceId = pid }, serializationType: serializationType );
            }
            catch( Exception ex )
            {
                string exc = Utilities.UnwindException( "StartPlanAsync", ex, asSingleLine: true );
                return GetHttpResponse( new ExceptionWrapper { Exception = exc }, serializationType: serializationType );
            }
        }
        #endregion
        #endregion

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
                return GetHttpResponse( result, serializationType );
            else
                return result;
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

                PlanElementParms pep = new PlanElementParms { Type = serializationType };
                pep.ElementPaths.Add( elementPath );

                object result = _server.GetPlanElements( planUniqueName, planInstanceId, pep );

                if( setContentType )
                    return GetHttpResponse( result, serializationType );
                else
                    return result;
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


        //#region AutoUpdate
        //[HttpGet]
        //[Route( "admin/update" )]
        //public List<AutoUpdaterMessage> AutoUpdate()
        //{
        //    string context = GetContext( nameof( AutoUpdate ) );

        //    try
        //    {
        //        SynapseServer.Logger.Info( context );
        //        return AutoUpdater.Update();
        //    }
        //    catch( Exception ex )
        //    {
        //        SynapseServer.Logger.Error(
        //            Utilities.UnwindException( context, ex, asSingleLine: true ) );
        //        throw;
        //    }
        //}

        //[HttpGet]
        //[Route( "admin/update/logs" )]
        //public List<AutoUpdaterMessage> FetchAutoUpdateLogList()
        //{
        //    string context = GetContext( nameof( FetchAutoUpdateLogList ) );

        //    try
        //    {
        //        SynapseServer.Logger.Debug( context );
        //        return AutoUpdater.FetchLogList();
        //    }
        //    catch( Exception ex )
        //    {
        //        SynapseServer.Logger.Error(
        //            Utilities.UnwindException( context, ex, asSingleLine: true ) );
        //        throw;
        //    }
        //}

        //[HttpGet]
        //[Route( "admin/update/logs/{name}" )]
        //public List<AutoUpdaterMessage> FetchAutoUpdateLog(string name = null)
        //{
        //    string context = GetContext( nameof( FetchAutoUpdateLog ), nameof( name ), name );

        //    try
        //    {
        //        SynapseServer.Logger.Debug( context );
        //        return AutoUpdater.FetchLog( name );
        //    }
        //    catch( Exception ex )
        //    {
        //        SynapseServer.Logger.Error(
        //            Utilities.UnwindException( context, ex, asSingleLine: true ) );
        //        throw;
        //    }
        //}
        //#endregion


        //#region logs
        //[HttpGet]
        //[Route( "admin/logs" )]
        //public List<AutoUpdaterMessage> FetchSynapseLogList()
        //{
        //    string context = GetContext( nameof( FetchSynapseLogList ) );

        //    try
        //    {
        //        SynapseServer.Logger.Debug( context );
        //        return Log4netUtil.FetchLogList();
        //    }
        //    catch( Exception ex )
        //    {
        //        SynapseServer.Logger.Error(
        //            Utilities.UnwindException( context, ex, asSingleLine: true ) );
        //        throw;
        //    }
        //}

        //[HttpGet]
        //[Route( "admin/logs/{name}" )]
        //public netHttp.HttpResponseMessage FetchSynapseLog(string name)
        //{
        //    string context = GetContext( nameof( FetchSynapseLogList ) );

        //    try
        //    {
        //        string path = Log4netUtil.GetLogfilePath( name );
        //        FileStream stream = new FileStream( path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite );

        //        netHttp.HttpResponseMessage msg = new netHttp.HttpResponseMessage( System.Net.HttpStatusCode.OK )
        //        {
        //            Content = new netHttp.StreamContent( stream )
        //        };
        //        msg.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue( "attachment" )
        //        {
        //            FileName = name
        //        };
        //        msg.Content.Headers.ContentType = new MediaTypeHeaderValue( "application/octet-stream" );

        //        return msg;
        //    }
        //    catch( Exception ex )
        //    {
        //        SynapseServer.Logger.Error(
        //            Utilities.UnwindException( context, ex, asSingleLine: true ) );
        //        throw;
        //    }
        //}
        //#endregion


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

        public object GetCustomAssemblyConfig(string name)
        {
            CustomAssemblyConfig customAssmConfig =
                SynapseServer.Config.Controller.Assemblies.Find( ca => ca.Name.Equals( name, StringComparison.OrdinalIgnoreCase ) );

            return customAssmConfig?.Config;
        }


        netHttp.HttpResponseMessage GetHttpResponse(object content, SerializationType serializationType)
        {
            string s = GetStringContent( content, serializationType );
            Encoding encoding = serializationType == SerializationType.Xml ? Encoding.Unicode : Encoding.UTF8;
            return new netHttp.HttpResponseMessage( System.Net.HttpStatusCode.OK )
            {
                Content = new netHttp.StringContent( s, encoding, SerializationContentType.GetContentType( serializationType ) )
            };
        }

        string GetStringContent(object content, SerializationType serializationType)
        {
            switch( serializationType )
            {
                case SerializationType.Json:
                {
                    return YamlHelpers.Serialize( content, serializeAsJson: true, formatJson: true, emitDefaultValues: false );
                }
                case SerializationType.Xml:
                {
                    try
                    {
                        return XmlHelpers.Serialize<string>( content, omitXmlDeclaration: false, omitXmlNamespace: true );
                    }
                    catch
                    {
                        if( content is IDictionary<object, object> kvps )
                            content = new RootNode { KeyValuePairs = kvps };

                        string serializedData = YamlHelpers.Serialize( content, serializeAsJson: true, formatJson: true, emitDefaultValues: false );
                        System.Xml.XmlDocument doc = Newtonsoft.Json.JsonConvert.DeserializeXmlNode( serializedData );
                        return XmlHelpers.Serialize<string>( doc );
                    }
                }
                default: { return content.ToString(); }
            }
        }

        bool IsMediaTypeApplicationXml { get { return SerializationContentType.IsApplicationXml( Request.Content.Headers.ContentType.MediaType ); } }

        string RawBody { get { return CurrentUrl.Request.Properties["body"].ToString(); } }

        void GetPlanEnvelopeFromRawBody(ref StartPlanEnvelope planEnvelope)
        {
            if( planEnvelope == null && !string.IsNullOrWhiteSpace( RawBody ) )
            {
                try { planEnvelope = StartPlanEnvelope.FromXml( RawBody ); }
                catch
                {
                    try { planEnvelope = StartPlanEnvelope.FromYaml( RawBody ); }
                    catch { }
                }
            }
        }
        #endregion
    }
}