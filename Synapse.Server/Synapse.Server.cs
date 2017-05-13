using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using log4net;

using Microsoft.Owin.Hosting;

using Synapse.Common.CmdLine;
using Synapse.Services.Common;


namespace Synapse.Services
{
    public partial class SynapseServer : ServiceBase
    {
        public static ILog Logger = LogManager.GetLogger( "SynapseServer" );
        public static SynapseServerConfig Config = null;

        ServiceHost _serviceHost = null;
        private IDisposable _webapp;


        public SynapseServer()
        {
            if( Config == null )
                Config = SynapseServerConfig.Deserialze();

            InitializeComponent();

            this.ServiceName = Config.Service.Name;
        }

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

#if DEBUG
            RunConsole();
#endif

            InstallService( args ); //only runs RELEASE
            RunService(); //only runs RELEASE
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

                    string arg0 = args[0].ToLower();
                    if( arg0 == "install" || arg0 == "i" )
                    {
                        bool error = false;
                        Dictionary<string, string> options = args.Length > 1 ? CmdLineUtilities.ParseCmdLine( args, 1, ref error, ref message, null, true ) : null;
                        if( !error )
                            ok = InstallUtility.InstallAndStartService( serverRole: ServerRole.Server, installOptions: options, message: out message );
                    }
                    else if( arg0 == "uninstall" || arg0 == "u" )
                    {
                        ok = InstallUtility.StopAndUninstallService( out message );
                    }
                    else if( arg0 == "config" || arg0 == "c" )
                    {
                        SynapseServerConfig.DeserialzeOrNew( ServerRole.Server );
                        ok = true;
                    }

                    if( !ok )
                        WriteHelpAndExit( message );
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

        public static void RunConsole()
        {
            if( !Environment.UserInteractive )
            {
                string msg = "This is a Debug build of SynapseServer and will not run as Service.";
                Logger.Fatal( msg );
                new SynapseServer().WriteEventLog( msg );

                Environment.Exit( 1 );
            }

            Config = SynapseServerConfig.Deserialze();
            ConsoleColor current = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine( $"Starting Synapse.Server as {Config?.Service.Role}: Press Ctrl-C/Ctrl-Break to stop." );
            Console.ForegroundColor = current;

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
        static void WriteHelpAndExit(string errorMessage = null)
        {
            bool haveError = !string.IsNullOrWhiteSpace( errorMessage );

            MessageBoxIcon icon = MessageBoxIcon.Information;

            string msg = $"synapse.server.exe, Version: {typeof( SynapseServer ).Assembly.GetName().Version}\r\nSyntax:\r\n  synapse.server.exe install [run:true|false] | uninstall | config";

            if( haveError )
            {
                msg += $"\r\n\r\n* Last error:\r\n{errorMessage}\r\nSee logs for details.";
                icon = MessageBoxIcon.Error;
            }

            MessageBox.Show( msg, "Synapse Server", MessageBoxButtons.OK, icon );

            Environment.Exit( haveError ? 1 : 0 );
        }
        #endregion
    }
}