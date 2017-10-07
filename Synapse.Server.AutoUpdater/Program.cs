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
            if( args.Length > 0 && args[0].ToLower() == "genconfig" )
            {
                UpdateInfo.SerializeSample();
                return;
            }

            UpdateInfo updateInfo = UpdateInfo.Deserialize();

            if( StopServices( updateInfo.ConfigFiles, updateInfo.WaitForExitMillseconds ) )
            {
                _updater = new Updater( updateInfo );
                _updater.PropertyChanged += Updater_PropertyChanged;
                _updater.InitializePatchStatus();
                if( _updater.Status.PatchIsValid )
                    Task.Run( () =>
                    {
                        _updater.InstallExistingPatches( _updater.Status.ExeInfo.Name, _updater.Status.ExeInfo.FolderPath );
                    } ).Wait();

                StartServices( updateInfo.ConfigFiles, updateInfo.WaitForExitMillseconds );
            }
        }

        static bool StopServices(List<string> configs, int timeout)
        {
            bool ok = false;
            try
            {
                foreach( string config in configs )
                {
                    ServiceConfig c = ServiceConfig.Deserialize( config );

                    ServiceController sc = new ServiceController( c.Name );
                    LogMessage( $"The {c.Name} service status is currently set to {sc.Status}." );

                    if( !((sc.Status.Equals( ServiceControllerStatus.Stopped )) || (sc.Status.Equals( ServiceControllerStatus.StopPending ))) )
                    {
                        LogMessage( $"Stopping the {c.Name} service..." );
                        sc.Stop();
                        sc.WaitForStatus( ServiceControllerStatus.Stopped, new TimeSpan( 0, 0, 0, 0, timeout ) );
                        LogMessage( $"The {c.Name} service status is currently set to {sc.Status}." );
                    }
                }

                ok = true;
            }
            catch( Exception ex )
            {
                LogMessage( ex.Message );
                StartServices( configs, timeout );
            }

            return ok;
        }

        static bool StartServices(List<string> configs, int timeout)
        {
            bool ok = false;
            try
            {
                foreach( string config in configs )
                {
                    ServiceConfig c = ServiceConfig.Deserialize( config );

                    ServiceController sc = new ServiceController( c.Name );
                    LogMessage( $"The {c.Name} service status is currently set to {sc.Status}." );

                    if( !((sc.Status.Equals( ServiceControllerStatus.StartPending )) || (sc.Status.Equals( ServiceControllerStatus.Running ))) )
                    {
                        LogMessage( $"Stopping the {c.Name} service..." );
                        sc.Start();
                        sc.WaitForStatus( ServiceControllerStatus.Stopped, new TimeSpan( 0, 0, 0, 0, timeout ) );
                        LogMessage( $"The {c.Name} service status is currently set to {sc.Status}." );
                    }
                }

                ok = true;
            }
            catch( Exception ex )
            {
                LogMessage( ex.Message );
            }

            return ok;
        }

        static void Updater_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if( e.PropertyName == "LogMessage" )
                LogMessage( $"{_updater.LogMessage.TimeStamp}\t{_updater.LogMessage.Message}" );
        }

        static void LogMessage(string v)
        {
            Console.WriteLine( v );
        }
    }
}