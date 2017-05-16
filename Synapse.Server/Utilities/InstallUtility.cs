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
        public static bool InstallAndStartService(ServerRole serverRole, Dictionary<string, string> installOptions, out string message)
        {
            message = null;
            bool startService = true;

            if( installOptions != null )
            {
                const string run = "run";
                if( installOptions.ContainsKey( run ) )
                {
                    bool.TryParse( installOptions[run], out startService );
                    installOptions.Remove( run );
                }
            }

            SynapseServerConfig.DeserializeOrNew( serverRole );
            bool ok = InstallOrUninstallService( install: true, message: out message );

            if( ok && startService )
                try
                {
                    string sn = SynapseServerConfig.Deserialze().Service.Name;
                    Console.Write( $"\r\nStarting {sn}... " );
                    ServiceController sc = new ServiceController( sn );
                    sc.Start();
                    sc.WaitForStatus( ServiceControllerStatus.Running, TimeSpan.FromMinutes( 2 ) );
                    Console.WriteLine( sc.Status );
                }
                catch( Exception ex )
                {
                    Console.WriteLine();
                    message = ex.Message;
                    ok = false;
                }

            return ok;
        }

        public static bool StopAndUninstallService(out string message)
        {
            bool ok = true;
            message = null;

            try
            {
                string sn = SynapseServerConfig.Deserialze().Service.Name;
                ServiceController sc = new ServiceController( sn );
                if( sc.Status == ServiceControllerStatus.Running )
                {
                    Console.WriteLine( $"\r\nStopping {sn}..." );
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
                ok = InstallOrUninstallService( install: false, message: out message );

            return ok;
        }

        public static bool InstallOrUninstallService(bool install, out string message)
        {
            string fullFilePath = typeof( SynapseServerServiceInstaller ).Assembly.Location;
            string logFile = $"Synapse.Server.InstallLog.txt";

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
    public class SynapseServerServiceInstaller : Installer
    {
        public SynapseServerServiceInstaller()
        {
            ServiceProcessInstaller processInstaller = new ServiceProcessInstaller();
            ServiceInstaller serviceInstaller = new ServiceInstaller();

            SynapseServerConfig config = SynapseServerConfig.Deserialze();
            ////if( config.HasServiceNameDefaults )
            ////{
            ////    config.ServiceName = config.ServiceNameValue;
            ////    config.ServiceDisplayName = config.ServiceDisplayNameValue;
            ////    config.Serialize();
            ////}

            //set the privileges
            processInstaller.Account = ServiceAccount.LocalSystem;

            serviceInstaller.DisplayName = config.Service.DisplayName;
            string desc = config.Service.IsRoleController ?
                "Serves Plan commands to and receives Plan status from Synapse Nodes." : "Runs Plans, proxies to other Synapse Nodes.";
            serviceInstaller.Description = $"{desc}  Use 'Synapse.Server /uninstall' to remove.  Information at http://synapse.readthedocs.io/en/latest/.";
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            //must be the same as what was set in Program's constructor
            serviceInstaller.ServiceName = config.Service.Name;
            this.Installers.Add( processInstaller );
            this.Installers.Add( serviceInstaller );
        }
    }
}