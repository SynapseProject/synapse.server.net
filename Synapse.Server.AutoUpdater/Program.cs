using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ServiceProcess;
using System.Threading.Tasks;

using HappyBin.AutoUpdater;


namespace Synapse.Server.AutoUpdater
{
    class Program
    {
        static Updater _updater = null;

        static void Main(string[] args)
        {
            if( args.Length > 0 )
            {
                string arg0 = args[0].ToLower();
                switch( arg0 )
                {
                    case "genconfig":
                    {
                        SynapseUpdaterSettings.SerializeSample();
                        break;
                    }
                    case "update":
                    {
                        ExecuteUpdate();
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

        static void ExecuteUpdate()
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
                        LogMessage( $"The {service} service status is currently set to {sc.Status}." );

                        bool working = false;
                        if( desiredStatus == ServiceControllerStatus.Stopped )
                        {
                            if( !((sc.Status.Equals( ServiceControllerStatus.StopPending )) || (sc.Status.Equals( ServiceControllerStatus.Stopped ))) )
                            {
                                working = true;
                                LogMessage( $"Stopping the {service} service..." );
                                sc.Stop();
                            }
                        }
                        else
                        {
                            if( !((sc.Status.Equals( ServiceControllerStatus.StartPending )) || (sc.Status.Equals( ServiceControllerStatus.Running ))) )
                            {
                                working = true;
                                LogMessage( $"Stopping the {service} service..." );
                                sc.Stop();
                            }
                        }

                        if( working )
                        {
                            sc.WaitForStatus( desiredStatus, new TimeSpan( 0, 0, 0, 0, timeout ) );
                            LogMessage( $"The {service} service status is currently set to {sc.Status}." );
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
                LogMessage( $"{_updater.LogMessage.TimeStamp}|{_updater.LogMessage.Message}" );
        }

        static void LogMessage(string v)
        {
            Console.WriteLine( v );
        }

        #region Help
        static void WriteHelpAndExit(string errorMessage = null)
        {
            bool haveError = !string.IsNullOrWhiteSpace( errorMessage );

            ConsoleColor defaultColor = Console.ForegroundColor;

            Console_WriteLine( $"synapse.server.autoupdater.exe, Version: {typeof( Program ).Assembly.GetName().Version}\r\n", ConsoleColor.Green );
            Console.WriteLine( "Syntax:" );
            Console_WriteLine( "  synapse.server.autoupdater.exe [update|genconfig]\r\n", ConsoleColor.Cyan, "{", "}" );
            Console_WriteLine( "  update:{0,-6}Runs updater, sources yaml config.", ConsoleColor.Green, "" );
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