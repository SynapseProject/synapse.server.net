using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.ServiceProcess;
using System.Text;

namespace Synapse.Services
{
    public class InstallUtility
    {
        public static readonly string SynapseConfigParm = "synapseConfig";

        public static bool InstallAndStartService(ServerRole serverRole, Dictionary<string, string> installOptions, out string message)
        {
            message = null;
            bool startService = true;

            string configFile = null;

            if( installOptions != null )
            {
                const string run = "run";
                if( installOptions.ContainsKey( run ) )
                {
                    bool.TryParse( installOptions[run], out startService );
                    installOptions.Remove( run );
                }
                if( installOptions.ContainsKey( SynapseConfigParm ) )
                {
                    configFile = installOptions[SynapseConfigParm];
                    installOptions.Remove( SynapseConfigParm );
                }
            }

            SynapseServerConfig config = SynapseServerConfig.DeserializeOrNew( serverRole, configFile );
            bool ok = InstallOrUninstallService( install: true, configFile: configFile, message: out message );

            if( ok && startService )
                try
                {
                    Console.Write( $"\r\nStarting {config.Service.Name}... " );
                    ServiceController sc = new ServiceController( config.Service.Name );
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

        public static bool StopAndUninstallService(Dictionary<string, string> installOptions, out string message)
        {
            bool ok = true;
            message = null;

            string configFile = null;
            if( installOptions != null && installOptions.ContainsKey( SynapseConfigParm ) )
            {
                configFile = installOptions[SynapseConfigParm];
                installOptions.Remove( SynapseConfigParm );
            }

            try
            {
                string sn = SynapseServerConfig.Deserialze( configFile ).Service.Name;
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
                ok = InstallOrUninstallService( install: false, configFile: configFile, message: out message );

            return ok;
        }

        static bool InstallOrUninstallService(bool install, string configFile, out string message)
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

    public class SynapseServiceInstaller : ServiceInstaller
    {
        public override void Install(IDictionary stateSaver)
        {
            //each element of the assemblyPath needs to individually encapsulated in double-quotes
            StringBuilder path = new StringBuilder( "\"" );
            path.Append( Context.Parameters["assemblypath"] );
            path.Append( "\" \"" );
            path.Append( Path.GetFullPath( SynapseServerConfig.FileName ) );
            path.Append( "\"" );
            Context.Parameters["assemblypath"] = path.ToString();
            base.Install( stateSaver );
        }
    }

    [RunInstaller( true )]
    public class SynapseServerServiceInstaller : Installer
    {
        public SynapseServerServiceInstaller()
        {
            ServiceProcessInstaller processInstaller = new ServiceProcessInstaller();
            ServiceInstaller serviceInstaller = new SynapseServiceInstaller();

            //SynapseServerConfig.FileName is a static, so this will deserialize a custom config file if specified
            //  since it was alreadt deserialized above at [SynapseServerConfig.DeserializeOrNew( serverRole, configFile );] (line 37)
            SynapseServerConfig config = SynapseServerConfig.Deserialze();

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