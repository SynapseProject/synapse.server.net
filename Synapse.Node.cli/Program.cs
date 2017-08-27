using System;
using System.Collections.Generic;
using System.Text;


namespace Synapse.Services.NodeService.Cli
{
    class Program : Synapse.Common.CmdLine.HttpApiCliBase
    {
        static void Main(string[] args)
        {
            if( args.Length > 0 && (args[0].ToLower() == "interactive" || args[0].ToLower() == "i") )
            {
                Program p = new Program()
                {
                    IsInteractive = true,
                };
                if( args.Length > 1 )
                {
                    string[] s = args[1].Split( new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries );
                    if( s.Length == 2 )
                        p.BaseUrl = s[1];
                }

				Console.Write( "synapse> " );
				string input = Console.ReadLine();
                while( input.ToLower() != "exit" )
                {
                    p.ProcessArgs( input.Split( ' ' ) );
					Console.Write( "synapse> " );
					input = Console.ReadLine();
                }
            }
            else
            {
                new Program().ProcessArgs( args );
            }
        }


        Dictionary<string, string> _methods = new Dictionary<string, string>();
        readonly string _service = "service";
        readonly string _genconfig = "genconfig";

        public Program()
        {
            _methods.Add( "hello", "Hello" );
            _methods.Add( "hi", "Hello" );
            _methods.Add( "whoami", "WhoAmI" );
            _methods.Add( "who", "WhoAmI" );
            _methods.Add( "start", "StartPlanFile" );
            _methods.Add( "s", "StartPlanFile" );
            _methods.Add( "cancel", "CancelPlan" );
            _methods.Add( "c", "CancelPlan" );
            _methods.Add( "drainstop", "Drainstop" );
            _methods.Add( "dst", "Drainstop" );
            _methods.Add( "undrainstop", "Undrainstop" );
            _methods.Add( "ust", "Undrainstop" );
            _methods.Add( "DrainStatus", "GetIsDrainstopComplete" );
            _methods.Add( "dss", "GetIsDrainstopComplete" );
            _methods.Add( "QueueDepth", "GetCurrentQueueDepth" );
            _methods.Add( "qd", "GetCurrentQueueDepth" );
            _methods.Add( "QueueItems", "GetCurrentQueueItems" );
            _methods.Add( "qi", "GetCurrentQueueItems" );
        }

        public bool IsInteractive { get; set; }
        public string BaseUrl { get; set; }

        void ProcessArgs(string[] args)
        {
            if( args.Length == 0 )
            {
                WriteHelpAndExit();
            }
            else
            {
                try
                {
                    bool error = false;
                    Dictionary<string, string> parms = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
                    parms = ParseCmdLine( args, 0, ref error, suppressErrorMessages: true );
                    string configFile = null;
                    if( parms.ContainsKey( InstallUtility.SynapseConfigParm ) )
                    {
                        configFile = parms[InstallUtility.SynapseConfigParm];
                        parms.Remove( InstallUtility.SynapseConfigParm );
                    }

                    SynapseServerConfig config = SynapseServerConfig.DeserializeOrNew( ServerRole.Node, configFile );
                    if( string.IsNullOrWhiteSpace( BaseUrl ) )
                        BaseUrl = $"{config.WebApi.ToUri( isUserInteractive: true )}/synapse/node";

                    if( parms.ContainsKey( "url" ) )
                    {
                        BaseUrl = parms["url"];
                        parms.Remove( "url" );
                    }


                    string arg0 = args[0].ToLower();

                    if( _methods.ContainsKey( arg0 ) )
                    {
                        Console.WriteLine( $"Calling {_methods[arg0]} on {BaseUrl}" );

                        if( _methods[arg0] == "StartPlanFile" )
                            RunStartPlanMethod( args, parms );
                        else
                            RunMethod( new NodeServiceHttpApiClient( BaseUrl ), _methods[arg0], args );
                    }
                    else if( arg0.StartsWith( _service ) )
                        RunServiceAction( args );
                    else if( arg0.StartsWith( _genconfig ) )
                        RunConfigGenerator( args );
                    else
                        WriteHelpAndExit( "Unknown action." );
                }
                catch( Exception ex )
                {
                    WriteHelpAndExit( Synapse.Common.WebApi.Utilities.UnwindException( ex ) );
                }
            }
        }

        protected virtual void RunStartPlanMethod(string[] args, Dictionary<string, string> parameters)
        {
            string methodName = "StartPlanFile";
            NodeServiceHttpApiClient instance = new NodeServiceHttpApiClient( BaseUrl );
            bool needHelp = args.Length == 2 && args[1].ToLower().Contains( "help" );

            if( needHelp )
            {
                Dictionary<string, Type> parms = new Dictionary<string, Type>();
                parms.Add( "filePath", typeof( string ) );
                parms.Add( "planInstanceId", typeof( long ) );
                parms.Add( "dryRun", typeof( bool ) );
                Console.WriteLine( $"Parameter options for {methodName}:\r\n" );
                WriteMethodParametersHelp( parms );
                Console.WriteLine( $"Remaining argname:argvalue pairs will be passed as dynamic parameters.\r\n" );
            }
            else
            {
                string filePath = null;
                string fp = nameof( filePath ).ToLower();
                if( parameters.ContainsKey( fp ) )
                {
                    filePath = parameters[fp];
                    parameters.Remove( fp );
                }
                else
                    throw new Exception( "filePath is required." );

                long planInstanceId = 0;
                string piid = nameof( planInstanceId ).ToLower();
                if( parameters.ContainsKey( piid ) )
                {
                    long.TryParse( parameters[piid], out planInstanceId );
                    parameters.Remove( piid );
                }

                bool dryRun = false;
                string dr = nameof( dryRun ).ToLower();
                if( parameters.ContainsKey( dr ) )
                {
                    bool.TryParse( parameters[dr], out dryRun );
                    parameters.Remove( dr );
                }


                try
                {
                    Core.ExecuteResult result = instance.StartPlanFile( filePath, planInstanceId, dryRun, parameters );
                    Console.WriteLine( result );
                }
                catch( Exception ex )
                {
                    WriteException( ex );
                }
            }
        }


        protected virtual void RunServiceAction(string[] args)
        {
            if( args.Length < 2 )
                WriteHelpAndExit( "Not enough arguments specified." );

            string option = args[1].ToLower();

            switch( option )
            {
                case "run":
                {
                    string[] arguments = new string[] { };
                    if( args.Length > 2 )
                    {
                        arguments = new string[args.Length - 2];
                        for( int i = 2; i < args.Length; i++ )
                            arguments[i - 2] = args[i];
                    }
                    SynapseServer.RunConsole( arguments );
                    break;
                }
                case "install":
                {
                    string message = string.Empty;
                    bool error = false;
                    Dictionary<string, string> values = ParseCmdLine( args, 2, ref error, true );
                    if( !InstallUtility.InstallAndStartService( serverRole: ServerRole.Node, installOptions: values, message: out message ) )
                    {
                        Console.WriteLine( message );
                        Environment.Exit( 1 );
                    }

                    break;
                }
                case "uninstall":
                {
                    string message = string.Empty;
                    bool error = false;
                    Dictionary<string, string> values = ParseCmdLine( args, 2, ref error, true );
                    if( !InstallUtility.StopAndUninstallService( installOptions: values, message: out message ) )
                    {
                        Console.WriteLine( message );
                        Environment.Exit( 1 );
                    }

                    break;
                }
                default:
                {
                    WriteHelpAndExit( "Unknown service action." );
                    Environment.Exit( 1 );

                    break;
                }
            }
        }


        #region Generate ServerConfigs
        private void RunConfigGenerator(string[] args)
        {
            Console.WriteLine( $"Calling {nameof( GenerateConfig )}." );
            RunMethod( this, nameof( GenerateConfig ), args );
        }

        public void GenerateConfig(string filePath)
        {
            if( string.IsNullOrWhiteSpace( filePath ) )
                throw new ArgumentNullException( nameof( filePath ), "FilePath is required." );

            SynapseServerConfig.DeserializeOrNew( ServerRole.Node, filePath ); ;
        }
        #endregion


        #region Help
        protected override void WriteHelpAndExit(string errorMessage = null)
        {
            bool haveError = !string.IsNullOrWhiteSpace( errorMessage );

            ConsoleColor defaultColor = Console.ForegroundColor;

            Console_WriteLine( $"synapse.node.cli.exe, Version: {typeof( Program ).Assembly.GetName().Version}\r\n", ConsoleColor.Green );
            Console.WriteLine( "Syntax:" );
            Console_WriteLine( "  synapse.node.cli.exe service {0}command{1} | {0}httpAction parm:value{1} |", ConsoleColor.Cyan, "{", "}" );
            Console.WriteLine( "       interactive|i [url:http://{1}host:port{2}/synapse/node] |", "", "{", "}" );
            Console.WriteLine( "       genconfig filePath:{1}path{2}\r\n", "", "{", "}" );
            Console_WriteLine( "  About URLs:{0,-2}URL is an optional parameter on all commands except 'service'", ConsoleColor.Green, "" );
            Console.WriteLine( "{0,-15}commands. Specify as [url:http://{1}host:port{2}/synapse/node].", "", "{", "}" );
            Console.WriteLine( "{0,-15}URL default is localhost:{1}port{2} (See WebApiPort in config.yaml)\r\n", "", "{", "}" );
            Console.WriteLine( "  interactive{0,-2}Run this CLI in interactive mode, optionally specify URL.", "" );
            Console.WriteLine( "{0,-15}All commands below work in standard or interactive modes.\r\n", "" );
            Console.WriteLine( "  service{0,-6}Install/Uninstall the Windows Service, or Run the Service", "" );
            Console.WriteLine( "{0,-15}as a cmdline-hosted daemon.", "" );
            Console.WriteLine( "{0,-15}- Commands: install [run:true|false] | uninstall | run", "" );
            Console.WriteLine( "{0,-15}- Example:  synapse.node.cli service install run:false", "" );
            Console.WriteLine( "{0,-15}            synapse.node.cli service run\r\n", "" );
            Console.WriteLine( "  {1}{0,-4}Generate a Synapse Node config file.", "", _genconfig );
            Console.WriteLine( "{0,-15}- filePath: Path and filename for the config file.", "" );
			Console.WriteLine( "{0,-15}- Example: synapse.node.cli genconfig filepath:custom.yaml\r\n", "" );
			Console.WriteLine( "{0,-15}Note: Running synapse.node.cli with no parameters will", "" );
            Console.WriteLine( "{0,-15}      generate a default config if none exists.  Use this option", "" );
            Console.WriteLine( "{0,-15}      to generate a named config file.\r\n", "" );
            Console.WriteLine( "  httpAction{0,-3}Execute a command, optionally specify URL.", "" );
            Console.WriteLine( "{0,-15}Parm help: synapse.node.cli {1}httpAction{2} help.\r\n", "", "{", "}" );
            Console.WriteLine( "  - httpActions:", "" );
            Console.WriteLine( "    - Hello|hi           Returns 'Hello, World!'.", "" );
            Console.WriteLine( "    - WhoAmI|who         Returns NodeServer User Context.", "" );
            Console.WriteLine( "    - Start|s            Start a new Plan Instance.", "" );
            Console.WriteLine( "    - Cancel|c           Cancel a Plan Instance.", "" );
            Console.WriteLine( "    - Drainstop|dst      Prevents the node from receiving incoming requests;", "" );
            Console.WriteLine( "                         allows existing threads to complete. Optionally stops", "" );
            Console.WriteLine( "                         the Service when queue is fully drained.", "" );
            Console.WriteLine( "    - DrainStatus|dss    Returns true/false on whether queue is fully drained.", "" );
            Console.WriteLine( "    - QueueDepth|qd      Returns the number of items remaining in the queue.", "" );
            Console.WriteLine( "    - QueueItems|qi      Returns the list of items remaining in the queue.", "" );
            Console.WriteLine( "    - Undrainstop|ust    Resumes normal request processing.\r\n", "" );
            Console.WriteLine( "  Examples:", "" );
            Console.WriteLine( "    synapse.node.cli hi url:http://somehost/synapse/node", "" );
            Console.WriteLine( "    synapse.node.cli s help", "" );
            Console.WriteLine( "    synapse.node.cli s planInstanceId:0 dryRun:false filePath:C:\\planFile.yaml", "" );
            Console.WriteLine( "    synapse.node.cli dst url:http://somehost/synapse/node", "" );
            Console.WriteLine( "    synapse.node.cli i url:http://somehost/synapse/node", "" );
            Console.WriteLine( "    synapse.node.cli i", "" );

            if( haveError )
                Console_WriteLine( $"\r\n\r\n*** Last error:\r\n{errorMessage}\r\n", ConsoleColor.Red );

            Console.ForegroundColor = defaultColor;

            if( !IsInteractive )
                Environment.Exit( haveError ? 1 : 0 );
        }
        #endregion
    }
}