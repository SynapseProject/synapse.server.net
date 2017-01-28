using System;
using System.Collections.Generic;


namespace Synapse.Services.Controller.Cli
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

                string input = Console.ReadLine();
                while( input.ToLower() != "exit" )
                {
                    p.ProcessArgs( input.Split( ' ' ) );
                    input = Console.ReadLine();
                }
            }
            else
            {
                new Program().ProcessArgs( args );
            }
        }


        Dictionary<string, string> _methods = new Dictionary<string, string>();
        string _service = "service";

        public Program()
        {
            _methods.Add( "hello", "Hello" );
            _methods.Add( "hi", "Hello" );
            _methods.Add( "whoami", "WhoAmI" );
            _methods.Add( "who", "WhoAmI" );
            _methods.Add( "list", "GetPlanList" );
            _methods.Add( "l", "GetPlanList" );
            _methods.Add( "listinstances", "GetPlanInstanceIdList" );
            _methods.Add( "li", "GetPlanInstanceIdList" );
            _methods.Add( "start", "StartPlan" );
            _methods.Add( "s", "StartPlan" );
            _methods.Add( "getstatus", "GetPlanStatus" );
            _methods.Add( "gs", "GetPlanStatus" );
            _methods.Add( "setstatus", "SetPlanStatus" );
            _methods.Add( "ss", "SetPlanStatus" );
            _methods.Add( "cancel", "CancelPlan" );
            _methods.Add( "c", "CancelPlan" );

            SynapseControllerConfig config = SynapseControllerConfig.Deserialze();
            BaseUrl = $"http://localhost:{config.WebApiPort}/synapse/execute";
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
                string arg0 = args[0].ToLower();

                if( _methods.ContainsKey( arg0 ) )
                {
                    if( args.Length > 1 )
                    {
                        bool error = false;
                        Dictionary<string, string> parms = ParseCmdLine( args, 1, ref error, suppressErrorMessages: true );
                        if( parms.ContainsKey( "url" ) )
                            BaseUrl = parms["url"];
                    }
                    Console.WriteLine( $"Calling {_methods[arg0]} on {BaseUrl}" );
                    RunMethod( new ControllerServiceHttpApiClient( BaseUrl ), _methods[arg0], args );
                }
                else if( arg0.StartsWith( _service ) )
                    RunServiceAction( args );
                else
                    WriteHelpAndExit( "Unknown action." );
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
                    SynapseControllerService.RunConsole();
                    break;
                }
                case "install":
                {
                    string message = string.Empty;
                    if( !InstallUtility.InstallService( install: true, message: out message ) )
                        Console.WriteLine( message );
                    break;
                }
                case "uninstall":
                {
                    string message = string.Empty;
                    if( !InstallUtility.InstallService( install: false, message: out message ) )
                        Console.WriteLine( message );
                    break;
                }
                default:
                {
                    WriteHelpAndExit( "Unknown service action." );
                    break;
                }
            }
        }


        #region Help
        protected override void WriteHelpAndExit(string errorMessage = null)
        {
            bool haveError = !string.IsNullOrWhiteSpace( errorMessage );

            ConsoleColor defaultColor = Console.ForegroundColor;

            Console_WriteLine( $"synapse.controller.cli.exe, Version: {typeof( Program ).Assembly.GetName().Version}\r\n", ConsoleColor.Green );
            Console.WriteLine( "Syntax:" );
            Console_WriteLine( "  synapse.controller.cli.exe service {0}command{1} | {0}httpAction parm:value{1} |", ConsoleColor.Cyan, "{", "}" );
            Console.WriteLine( "       interactive|i [url:http://{1}host:port{2}/synapse/execute]\r\n", "", "{", "}" );
            Console_WriteLine( "  interactive{0,-2}Run this CLI in interactive mode. Optionally specify URL.", ConsoleColor.Green, "" );
            Console.WriteLine( "{0,-15}All commands below work in standard or interactive modes.\r\n", "" );
            Console.WriteLine( "  service{0,-6}Install/Uninstall the Windows Service, or Run the Service", "" );
            Console.WriteLine( "{0,-15}as a cmdline-hosted daemon.", "" );
            Console.WriteLine( "{0,-15}- Commands: install|uninstall|run", "" );
            Console.WriteLine( "{0,-15}- Example:  synapse.controller.cli service run\r\n", "" );
            Console.WriteLine( "  httpAction{0,-3}Execute a command, optionally include URL", "" );
            Console.WriteLine( "{0,-15}Parm help: synapse.controller.cli {1}httpAction{2} help.", "", "{", "}" );
            Console.WriteLine( "{0,-15}URL: url:http://{1}host:port{2}/synapse/execute\r\n", "", "{", "}" );
            Console.WriteLine( "  - httpActions:", "" );
            Console.WriteLine( "    - Hello|hi           Returns 'Hello World'.", "" );
            Console.WriteLine( "    - WhoAmI|who         Returns ControllerServer User Context.", "" );
            Console.WriteLine( "    - List|l             Get a list of Plans.", "" );
            Console.WriteLine( "    - ListInstances|li   Get a list of Plans Instances.", "" );
            Console.WriteLine( "    - Start|s            Start a new Plan Instance.", "" );
            Console.WriteLine( "    - GetStatus|gs       Get the Status for a Plan Instance.", "" );
            Console.WriteLine( "    - SetStatus|ss       Set the Status for a Plan Instance.", "" );
            Console.WriteLine( "    - Cancel|c           Cancel a Plan Instance.\r\n", "" );
            Console.WriteLine( "  Examples:", "" );
            Console.WriteLine( "    synapse.controller.cli l url:http://somehost/synapse/execute", "" );
            Console.WriteLine( "    synapse.controller.cli li help", "" );
            Console.WriteLine( "    synapse.controller.cli li planName:foo url:http://somehost/synapse/execute", "" );
            Console.WriteLine( "    synapse.controller.cli li planName:foo", "" );
            Console.WriteLine( "    synapse.controller.cli i url:http://somehost/synapse/execute", "" );
            Console.WriteLine( "    synapse.controller.cli i", "" );

            if( haveError )
                Console_WriteLine( $"\r\n\r\n*** Last error:\r\n{errorMessage}\r\n", ConsoleColor.Red );

            Console.ForegroundColor = defaultColor;

            if( !IsInteractive )
                Environment.Exit( haveError ? 1 : 0 );
        }
        #endregion
    }
}