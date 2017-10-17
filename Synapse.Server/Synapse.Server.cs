using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Security.Principal;

using log4net;

using Microsoft.Owin.Hosting;

using Synapse.Common.CmdLine;
using Synapse.Services.Common;
using Synapse.Common;
using System.Linq;
using System.IO;

namespace Synapse.Services
{
    public partial class SynapseServer : ServiceBase
    {
        public static ILog Logger = null;
        public static SynapseServerConfig Config = null;

        ServiceHost _serviceHost = null;
        private IDisposable _webapp;


        public SynapseServer()
        {
            InitializeComponent();

            this.ServiceName = Config.Service.Name;
        }

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

            DeserialzeConfig( args );

            //can't setup the Logger until after deserializing Config
            SetupLogger();

#if DEBUG
            RunConsole( args );
#endif

            InstallService( args ); //only runs RELEASE
            RunService(); //only runs RELEASE
        }

        public static void DeserialzeConfig(string[] args)
        {
            string configFile = SynapseServerConfig.FileName;

            if( args != null && args.Length > 0 )
            {
                string arg0 = args[0].ToLower();
                List<string> parms = (new string[] { "install", "i", "uninstall", "u", "genconfig", "gc" }).ToList();
                if( !parms.Contains( arg0 ) )
                    if( File.Exists( arg0 ) )
                        configFile = args[0];  //this line supports "synapse.server xx.config.yaml" for running as service
                    else
                    {
                        FileNotFoundException ex = new FileNotFoundException( $"Could not find startup config file [{args[0]}].", args[0] );
                        if( Environment.UserInteractive )
                            WriteHelpAndExit( ex.Message );
                        else
                            throw ex;
                    }
            }

            Config = SynapseServerConfig.DeserializeOrNew( ServerRole.Server, configFile );
        }

        public static void SetupLogger()
        {
            log4net.GlobalContext.Properties["LogName"] = $"{Config.Service.Name}.{Environment.MachineName.ToLower()}";
            Logger = LogManager.GetLogger( "SynapseServer" );

            //can't log the Config path until after Logger is setup
            Logger.Info( $"Using SynapseServerConfig from [{SynapseServerConfig.FileName}]" );
        }

        /// <summary>
        /// Install/Uninstall the service.
        /// Only works for Release build, as Debug will timeout on service start anyway (Thread.Sleep( Timeout.Infinite );).
        /// </summary>
        /// <param name="args"></param>
        [Conditional( "RELEASE" )]
        public static void InstallService(string[] args)
        {
            if( Environment.UserInteractive )
                if( args.Length > 0 )
                {
                    bool ok = false;
                    string message = string.Empty;

                    bool cliParseError = false;
                    Dictionary<string, string> options = args.Length > 1 ? CmdLineUtilities.ParseCmdLine( args, 1, ref cliParseError, ref message, null, true ) : null;

                    bool quiet = false;
                    if( !cliParseError )
                        if( options != null && options.ContainsKey( "quiet" ) )
                        {
                            bool.TryParse( options["quiet"], out quiet );
                            options.Remove( "quiet" );
                        }


                    string arg0 = args[0].ToLower();
                    if( arg0 == "install" || arg0 == "i" )
                    {
                        if( !cliParseError )
                            ok = InstallUtility.InstallAndStartService( serverRole: ServerRole.Server, installOptions: options, message: out message );
                    }
                    else if( arg0 == "uninstall" || arg0 == "u" )
                    {
                        ok = InstallUtility.StopAndUninstallService( installOptions: options, message: out message );
                    }
                    else if( arg0 == "genconfig" || arg0 == "gc" )
                    {
                        string configFile = null;
                        if( !cliParseError )
                            if( options != null && options.ContainsKey( "filepath" ) )
                                configFile = options["filepath"];

                        SynapseServerConfig.DeserializeOrNew( ServerRole.Server, configFile );

                        ok = true;
                    }


                    if( !ok )
                        WriteHelpAndExit( message, quiet );
                    else
                        Environment.Exit( 0 );
                }
                else
                {
                    WriteHelpAndExit();
                }
        }

        [Conditional( "RELEASE" )]
        public static void RunService()
        {
            ServiceBase.Run( new SynapseServer() );
        }

        public static void RunConsole(string[] args = null)
        {
            if( !Environment.UserInteractive )
            {
                string msg = "This is a Debug build of SynapseServer and will not run as Service.";
                Logger.Fatal( msg );
                new SynapseServer().WriteEventLog( msg );

                Environment.Exit( 1 );
            }

            if( Config == null )
                DeserialzeConfig( args );

            ConsoleColor current = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine( $"Starting Synapse.Server as {Config?.Service.Role}: Press Ctrl-C/Ctrl-Break to stop." );
            Console.ForegroundColor = current;

            //can't setup the Logger until after deserializing Config
            SetupLogger();

            using( SynapseServer s = new SynapseServer() )
            {
                s.OnStart( null );
                Thread.Sleep( Timeout.Infinite );
                Console.WriteLine( "Terminating Synapse.Server." );
                s.OnStop();
            }
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                Logger.Info( ServiceStatus.Starting );

                if( _serviceHost != null )
                    _serviceHost.Close();

                if( !Config.Service.IsRoleController )
                {
                    NodeController.InitPlanScheduler();
                    NodeController.DrainstopCallback = () => StopCallback();
                }

                string url = Config.WebApi.ToUri( Environment.UserInteractive );
                _webapp = WebApp.Start<WebServerConfig>( url );
                Logger.Info( $"Listening on {url}" );
                Logger.Info( $"Authentication Scheme = [{Config.WebApi.Authentication.Scheme}]" );

                _serviceHost = Config.Service.IsRoleController ?
                    new ServiceHost( typeof( ExecuteController ) ) : new ServiceHost( typeof( NodeController ) );
                _serviceHost.Open();

                Logger.Info( ServiceStatus.Running );
            }
            catch( Exception ex )
            {
                string msg = ex.Message;

                //_log.Write( Synapse.Common.LogLevel.Fatal, msg );
                Logger.Fatal( ex );
                WriteEventLog( ex.ToString() );

                Stop();
                Environment.Exit( 1 );
            }
        }

        void StopCallback()
        {
            this.Stop();
            Environment.Exit( 0 );
        }

        protected override void OnStop()
        {
            Logger.Info( ServiceStatus.Stopping );

            try
            {
                _webapp?.Dispose();

                if( _serviceHost != null )
                    _serviceHost.Close();
            }
            catch( Exception ex )
            {
                Logger.Fatal( ex.Message );
                WriteEventLog( ex.Message );
            }

            Logger.Info( ServiceStatus.Stopped );
        }


        public static bool UseImpersonation(IIdentity user)
        {
            bool rc = true;

            if( SynapseServer.Config.WebApi.UseImpersonation == false )
                rc = false;
            else if( SynapseServer.Config.WebApi.Authentication.Scheme == System.Net.AuthenticationSchemes.Anonymous )
                rc = false;
            else
            {
                string currentUser = user?.Name;
                string runningAsUser = Impersonator.WhoAmI()?.Name;

                if( currentUser == null )
                    rc = false;
                else if( currentUser.ToLower() == runningAsUser.ToLower() )
                    rc = false;
            }

            return rc;
        }

        #region exception handling
        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string source = "SynapseServer";
            string log = "Application";

            string msg = ((Exception)e.ExceptionObject).Message + ((Exception)e.ExceptionObject).InnerException.Message;

            Logger.Error( ((Exception)e.ExceptionObject).Message );
            Logger.Error( msg );

            try
            {
                if( !EventLog.SourceExists( source ) )
                    EventLog.CreateEventSource( source, log );

                EventLog.WriteEntry( source, msg, EventLogEntryType.Error );
            }
            catch { }

            try
            {
                string logRootPath = System.IO.Directory.CreateDirectory(
                    SynapseServerConfig.CurrentPath ).FullName;
                string logFilePath = $"{logRootPath}\\UnhandledException_{DateTime.Now.Ticks}.log";
                Exception ex = (Exception)e.ExceptionObject;
                string innerMsg = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                System.IO.File.AppendAllText( logFilePath, $"{ex.Message}\r\n\r\n{innerMsg}" );
            }
            catch { }
        }

        void WriteEventLog(string msg, EventLogEntryType entryType = EventLogEntryType.Error)
        {
            string source = "SynapseServer";
            string log = "Application";

            try
            {
                if( !EventLog.SourceExists( source ) )
                    EventLog.CreateEventSource( source, log );

                EventLog.WriteEntry( source, msg, entryType );
            }
            catch { }
        }
        #endregion

        #region Help
        static void WriteHelpAndExit(string errorMessage = null, bool quiet = false)
        {
            bool haveError = !string.IsNullOrWhiteSpace( errorMessage );

            MessageBoxIcon icon = MessageBoxIcon.Information;

            string msg = $"synapse.server.exe, Version: {typeof( SynapseServer ).Assembly.GetName().Version}\r\n\r\nSyntax:";
            msg += @"
synapse.server [install|uninstall|genconfig] [synapseConfig:{file}] [quiet:true|false]

  install: Installs the service, optionally runs it.
             - [run:*true*|false]
             - [synapseConfig:{file}]
             - [quiet:true|*false*]

  uninstall: Uninstalls the service.
             - [synapseConfig:{file}]
             - [quiet:true|*false*]

  genconfig: Generate a synapse.server config, requires synapseConfig.
             - synapseConfig:{file}
             - [quiet:true|*false*]

  Parameter options:
  - synapseConfig: Path to synapse.server config file.
  - quiet: Optionally suppress this dialog.";


            if( haveError )
            {
                msg += $"\r\n\r\n* Last error:\r\n{errorMessage}\r\nSee logs for details.";
                icon = MessageBoxIcon.Error;
            }

            if( !quiet && Environment.UserInteractive )
                MessageBox.Show( msg, "Synapse Server", MessageBoxButtons.OK, icon );

            Environment.Exit( haveError ? 1 : 0 );
        }
        #endregion
    }
}