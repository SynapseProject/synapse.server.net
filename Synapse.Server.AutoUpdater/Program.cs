using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using HappyBin.AutoUpdater;

namespace Synapse.Server.AutoUpdater
{
    class Program
    {
        static Updater _updater = null;

        static void Main(string[] args)
        {
            UpdateInfo updateInfo = UpdateInfo.Deserialize();

            foreach( string config in updateInfo.ConfigFiles )
            {
                ServiceConfig c = ServiceConfig.Deserialize( config );

                ServiceController sc = new ServiceController( c.Name );
                SetLogMessage( $"The {c.Name} service status is currently set to {sc.Status}." );

                if( !((sc.Status.Equals( ServiceControllerStatus.Stopped )) || (sc.Status.Equals( ServiceControllerStatus.StopPending ))) )
                {
                    SetLogMessage( $"Stopping the {c.Name} service..." );
                    sc.Stop();
                    sc.WaitForStatus( ServiceControllerStatus.Stopped, new TimeSpan( 0, 0, 0, 0, updateInfo.WaitForExitMillseconds ) );
                    SetLogMessage( $"The {c.Name} service status is currently set to {sc.Status}." );
                }
            }

            _updater = new Updater( updateInfo );
            _updater.PropertyChanged += new PropertyChangedEventHandler( updater_PropertyChanged );

            _updater.InitializePatchStatus();

            if( _updater.Status.PatchIsValid )
                Task.Run( () =>
                {
                    _updater.InstallExistingPatches( _updater.Status.ExeInfo.Name, _updater.Status.ExeInfo.FolderPath );
                } ).Wait();
        }

        private static void SetLogMessage(string v)
        {
            throw new NotImplementedException();
        }

        static void updater_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if( e.PropertyName == "LogMessage" )
                Console.WriteLine( "{0}\t{1}", _updater.LogMessage.TimeStamp, _updater.LogMessage.Message );
        }
    }
}