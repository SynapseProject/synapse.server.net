using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

using HappyBin.AutoUpdater;


namespace Synapse.Server.AutoUpdater
{
    class Program
    {
        static Updater _updater = null;
        static StringBuilder _log = new StringBuilder();

        static void Main(string[] args)
        {
            if( args.Length > 0 )
            {
                string currentPath = $@"{Path.GetDirectoryName( typeof( Program ).Assembly.Location )}\AutoUpdater";
                string logfile = $"{DateTime.Now.Ticks}_{Path.GetFileNameWithoutExtension( Path.GetTempFileName() )}.log";
                string logPath = Path.Combine( currentPath, logfile );
                if( args.Length > 1 )
                    logPath = args[1];

                string arg0 = args[0].ToLower();
                switch( arg0 )
                {
                    case "genconfig":
                    {
                        SynapseUpdaterSettings.SerializeSample();
                        break;
                    }
                    case "shadowcopy":
                    {
                        ShadowCopy( logPath );
                        break;
                    }
                    case "update":
                    {
                        ExecuteUpdate( logPath );
                        break;
                    }
                    default:
                    {
                        WriteHelpAndExit();
                        break;
                    }
                }

                return;
            }

            WriteHelpAndExit();
        }

        static void ExecuteUpdate(string logPath)
        {
            SynapseUpdaterSettings settings = SynapseUpdaterSettings.Deserialize();

            if( ManageServices( settings.ServiceConfigs, ServiceControllerStatus.Stopped, settings.WaitForExitMillseconds ) )
            {
                try
                {
                    _updater = new Updater( settings );
                    _updater.PropertyChanged += Updater_PropertyChanged;
                    _updater.InitializePatchStatus();
                    if( _updater.Status.PatchIsValid )
                        Task.Run( () =>
                        {
                            _updater.InstallExistingPatches( _updater.Status.ExeInfo.Name, _updater.Status.ExeInfo.FolderPath );
                        } ).Wait();
                }
                catch( Exception ex )
                {
                    LogMessage( ex.Message );
                }

                if( settings.StartServicesAfterInstall )
                    ManageServices( settings.ServiceConfigs, ServiceControllerStatus.Running, settings.WaitForExitMillseconds );
            }

            WriteLogToFile( logPath );
        }

        static bool ManageServices(List<string> configs, ServiceControllerStatus desiredStatus, int timeout)
        {
            bool ok = false;
            try
            {
                if( configs?.Count > 0 )
                    foreach( string config in configs )
                    {
                        SynapseServerConfig c = SynapseServerConfig.Deserialize( config );
                        string service = c.Service.Name;

                        ServiceController sc = new ServiceController( service );
                        LogMessage( $"[{service}] service status is [{sc.Status}]." );

                        bool working = false;
                        if( desiredStatus == ServiceControllerStatus.Stopped )
                        {
                            if( !((sc.Status.Equals( ServiceControllerStatus.StopPending )) || (sc.Status.Equals( ServiceControllerStatus.Stopped ))) )
                            {
                                working = true;
                                LogMessage( $"Stopping [{service}] service..." );
                                sc.Stop();
                            }
                        }
                        else
                        {
                            if( !((sc.Status.Equals( ServiceControllerStatus.StartPending )) || (sc.Status.Equals( ServiceControllerStatus.Running ))) )
                            {
                                working = true;
                                LogMessage( $"Starting [{service}] service..." );
                                sc.Start();
                            }
                        }

                        if( working )
                        {
                            sc.WaitForStatus( desiredStatus, new TimeSpan( 0, 0, 0, 0, timeout ) );
                            LogMessage( $"[{service}] service status is [{sc.Status}]." );
                        }
                    }

                ok = true;
            }
            catch( Exception ex )
            {
                LogMessage( ex.Message );

                if( desiredStatus == ServiceControllerStatus.Stopped )
                    ManageServices( configs, ServiceControllerStatus.Running, timeout );
            }

            return ok;
        }

        static void Updater_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if( e.PropertyName == "LogMessage" )
                LogMessageString( $"{_updater.LogMessage.TimeStamp}|{_updater.LogMessage.Message}" );
        }

        #region logging
        static void LogMessage(string v)
        {
            LogMessageString( $"{DateTime.Now}|{v}" );
        }

        static void LogMessageString(string v)
        {
            Console.WriteLine( v );
            _log.AppendLine( v );
        }

        static void WriteLogToFile(string logPath)
        {
            try
            {
                File.WriteAllText( logPath, _log.ToString() );
            }
            catch { }//eat the error
        }
        #endregion

        #region ShadowCopy
        static void ShadowCopy(string logPath)
        {
            Assembly assm = Assembly.GetExecutingAssembly();
            string currentPath = Path.GetDirectoryName( assm.Location );
            string shadowDirPath = Path.Combine( currentPath, ".shadow" );
            string shadowLogPath = logPath.Replace( ".log", ".shadow.log" );


            if( !Directory.Exists( shadowDirPath ) )
                Directory.CreateDirectory( shadowDirPath );

            LogMessageString( "Updating shadow copy of AutoUpdater." );
            foreach( FileInfo file in new DirectoryInfo( currentPath ).GetFiles() )
                if( file.Extension.ToLower() != ".log" )
                    file.CopyTo( Path.Combine( shadowDirPath, file.Name ), true );

            Process.Start( new ProcessStartInfo()
            {
                FileName = Path.Combine( shadowDirPath, Path.GetFileName( assm.Location ) ),
                WorkingDirectory = shadowDirPath,
                Arguments = $"update {logPath}",
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            } );

            LogMessageString( $"Starting update, logging to [{logPath}]." );
            WriteLogToFile( shadowLogPath );
        }
        #endregion

        #region Help
        static void WriteHelpAndExit(string errorMessage = null)
        {
            bool haveError = !string.IsNullOrWhiteSpace( errorMessage );

            ConsoleColor defaultColor = Console.ForegroundColor;

            Console_WriteLine( $"synapse.server.autoupdater.exe, Version: {typeof( Program ).Assembly.GetName().Version}\r\n", ConsoleColor.Green );
            Console.WriteLine( "Syntax:" );
            Console_WriteLine( "  synapse.server.autoupdater.exe [update|shadowcopy [logfile] | genconfig]\r\n", ConsoleColor.Cyan, "{", "}" );
            Console_WriteLine( "  update:{0,-6}Runs updater, sources yaml config.", ConsoleColor.Green, "" );
            Console.WriteLine( "  shadowcopy:{0,-2}Creates a shadow copy of the AutoUpdater and runs update.", "", "{", "}" );
            Console.WriteLine( "  genconfig:{0,-3}Creates a new autoupdater yaml config file.", "", "{", "}" );
            Console.WriteLine( "{0,-15}- List every server.config.yaml file in autoupdater yaml that", "", "{", "}" );
            Console.WriteLine( "{0,-15}  sources the local synapse.server.exe.\r\n", "", "{", "}" );

            if( haveError )
                Console_WriteLine( $"\r\n\r\n*** Last error:\r\n{errorMessage}\r\n", ConsoleColor.Red );

            Console.ForegroundColor = defaultColor;

            Environment.Exit( haveError ? 1 : 0 );
        }

        static void Console_WriteLine(string s, ConsoleColor color, params object[] args)
        {
            Console.ForegroundColor = color;
            Console.WriteLine( s, args );
        }
        #endregion
    }
}