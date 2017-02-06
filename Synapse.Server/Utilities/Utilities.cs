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
        public static bool InstallAndStartService(ServerRole role, Dictionary<string, string> configValues, out string message)
        {
            message = null;

            SynapseServerConfig config = new SynapseServerConfig() { ServerRole = role };
            SynapseServerConfig.Configure( config );
            if( configValues != null )
                SynapseServerConfig.Configure( configValues );


            bool ok = InstallService( role: role, install: true, configValues: configValues, message: out message );

            if( ok && !(configValues.ContainsKey( "run" ) && configValues["run"] == "false") )
                try
                {
                    string sn = SynapseServerConfig.Deserialze().ServiceName;
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

            SynapseServerConfig config = SynapseServerConfig.Deserialze();

            try
            {
                string sn = config.ServiceName;
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
                ok = InstallService( config.ServerRole, install: false, configValues: null, message: out message );

            return ok;
        }

        public static bool InstallService(ServerRole role, bool install, Dictionary<string, string> configValues, out string message)
        {
            if( configValues != null )
            {
                if( role == ServerRole.Controller )
                    SynapseServerConfig.Configure( configValues );
                else
                    SynapseNodeConfig.Configure( configValues );
            }

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

            //set the privileges
            processInstaller.Account = ServiceAccount.LocalSystem;

            serviceInstaller.DisplayName = config.ServiceDisplayName;
            string desc = config.ServerIsController ?
                "Serves Plan commands to and receives Plan status from Synapse Nodes." : "Runs Plans, proxies to other Synapse Nodes.";
            serviceInstaller.Description = $"{desc}  Use 'Synapse.Server /uninstall' to remove.  Information at http://synapse.readthedocs.io/en/latest/.";
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            //must be the same as what was set in Program's constructor
            serviceInstaller.ServiceName = config.ServiceName;
            this.Installers.Add( processInstaller );
            this.Installers.Add( serviceInstaller );
        }
    }
}