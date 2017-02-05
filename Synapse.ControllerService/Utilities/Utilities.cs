using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.ServiceProcess;

namespace Synapse.Services
{
    public class InstallUtility
    {
        public static bool StopAndUninstall(bool install, out string message)
        {
            bool ok = false;
            message = null;

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
                ok = InstallService( install: false, configValues: null, message: out message );

            return ok;
        }

        public static bool InstallService(bool install, Dictionary<string, string> configValues, out string message)
        {
            if( configValues != null )
                SynapseControllerConfig.Configure( configValues );

            string fullFilePath = typeof( SynapseControllerServiceInstaller ).Assembly.Location;
            string logFile = $"Synapse.Node.InstallLog.txt";

            List<string> args = new List<string>();

            args.Add( $"/logfile={logFile}" );
            args.Add( "/LogToConsole=true" );
            args.Add( "/ShowCallStack=true" );
            args.Add( fullFilePath );

            if( !install )
                args.Add( "/u" );

            try
            {
                ManagedInstallerClass.InstallHelper( args.ToArray() );
                message = "ok";
                return true;
            }
            catch( Exception ex )
            {
                string path = Path.GetDirectoryName( fullFilePath );
                File.AppendAllText( $"{path}\\{logFile}", ex.Message );
                message = ex.Message;
                return false;
            }
        }
    }

    [RunInstaller( true )]
    public class SynapseControllerServiceInstaller : Installer
    {
        public SynapseControllerServiceInstaller()
        {
            ServiceProcessInstaller processInstaller = new ServiceProcessInstaller();
            ServiceInstaller serviceInstaller = new ServiceInstaller();
            SynapseControllerConfig config = SynapseControllerConfig.Deserialze();

            //set the privileges
            processInstaller.Account = ServiceAccount.LocalSystem;

            serviceInstaller.DisplayName = config.ServiceDisplayName;
            serviceInstaller.Description = "Serves Plan commands to and receives Plan status from Synapse Nodes.  Use 'Synapse.Controller /uninstall' to remove.  Information at http://synapse.readthedocs.io/en/latest/.";
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            //must be the same as what was set in Program's constructor
            serviceInstaller.ServiceName = config.ServiceName;
            this.Installers.Add( processInstaller );
            this.Installers.Add( serviceInstaller );
        }
    }
}