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
    public partial class SynapseControllerService : ServiceBase
    {
        public static ILog Logger = LogManager.GetLogger( "SynapseControllerServer" );
        public static SynapseControllerConfig Config = null;

        ServiceHost _serviceHost = null;
        private IDisposable _webapp;


        public SynapseControllerService()
        {
            Config = SynapseControllerConfig.Deserialze();

            InitializeComponent();

            this.ServiceName = Config.ServiceName;
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
                    if( arg0 == "/install" || arg0 == "/i" )
                    {
                        Dictionary<string, string> values = CmdLineUtilities.ParseCmdLine( args, 1, ref ok, null );
                        if( ok )
                            ok = InstallUtility.InstallService( install: true, configValues: values, message: out message );
                        if( ok && !(values.ContainsKey( "run" ) && values["run"] == "false") )
                            try
                            {
                                ServiceController sc = new ServiceController( SynapseControllerConfig.Deserialze().ServiceName );
                                sc.Start();
                                sc.WaitForStatus( ServiceControllerStatus.Running, TimeSpan.FromMinutes( 2 ) );
                            }
                            catch( Exception ex )
                            {
                                message = ex.Message;
                                ok = false;
                            }
                    }
                    else if( arg0 == "/uninstall" || arg0 == "/u" )
                    {
                        try
                        {
                            ServiceController sc = new ServiceController( SynapseControllerConfig.Deserialze().ServiceName );
                            if( sc.Status == ServiceControllerStatus.Running )
                            {
                                sc.Stop();
                                sc.WaitForStatus( ServiceControllerStatus.Stopped, TimeSpan.FromMinutes( 2 ) );
                            }
                        }
                        catch( Exception ex )
                        {
                            message = ex.Message;
                            ok = false;
                        }
                        if( ok )
                            ok = InstallUtility.InstallService( install: false, configValues: null, message: out message );
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
            ServiceBase.Run( new SynapseControllerService() );
        }

        public static void RunConsole()
        {
            ConsoleColor current = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine( "Starting Synapse.Controller: Press Ctrl-C/Ctrl-Break to stop." );
            Console.ForegroundColor = current;

            using( SynapseControllerService s = new SynapseControllerService() )
            {
                s.OnStart( null );
                Thread.Sleep( Timeout.Infinite );
                s.OnStop();
            }
            Console.WriteLine( "Terminating Synapse.Controller." );
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                Logger.Info( ServiceStatus.Starting );

                if( _serviceHost != null )
                    _serviceHost.Close();

                string url = Environment.UserInteractive ?
                    $"http://localhost:{Config.WebApiPort}" :
                    $"http://*:{Config.WebApiPort}";
                _webapp = WebApp.Start<WebServerConfig>( url );
                Logger.Info( $"Listening on {url}" );

                _serviceHost = new ServiceHost( typeof( ExecuteController ) );
                _serviceHost.Open();

                Logger.Info( ServiceStatus.Running );
            }
            catch( Exception ex )
            {
                string msg = ex.Message;

                //_log.Write( Synapse.Common.LogLevel.Fatal, msg );
                Logger.Fatal( msg );
                WriteEventLog( msg );

                Stop();
                Environment.Exit( 1 );
            }
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
            string source = "SynapseControllerService";
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
                    SynapseControllerConfig.CurrentPath ).FullName;
                string logFilePath = $"{logRootPath}\\UnhandledException_{DateTime.Now.Ticks}.log";
                Exception ex = (Exception)e.ExceptionObject;
                string innerMsg = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                System.IO.File.AppendAllText( logFilePath, $"{ex.Message}\r\n\r\n{innerMsg}" );
            }
            catch { }
        }

        void WriteEventLog(string msg, EventLogEntryType entryType = EventLogEntryType.Error)
        {
            string source = "SynapseControllerService";
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
            Dictionary<string, string> cdf = SynapseControllerConfig.GetConfigDefaultValues();
            StringBuilder df = new StringBuilder();
            df.AppendLine( $"Optional args for configuring /install, use argname:value.  Defaults shown." );
            foreach( string key in cdf.Keys )
                df.AppendLine( $" - {key}:{cdf[key]}" );
            df.AppendLine( $" - Run:true  (Optionally Starts the Windows Service)" );

            string msg = $"synapse.controller.exe, Version: {typeof( SynapseControllerService ).Assembly.GetName().Version}\r\nSyntax:\r\n  synapse.controller.exe /install | /uninstall\r\n\r\n{df.ToString()}";

            if( haveError )
            {
                msg += $"\r\n\r\n* Last error:\r\n{errorMessage}\r\nSee logs for details.";
                icon = MessageBoxIcon.Error;
            }

            MessageBox.Show( msg, "Synapse Controller Service", MessageBoxButtons.OK, icon );

            Environment.Exit( haveError ? 1 : 0 );
        }
        #endregion
    }
}