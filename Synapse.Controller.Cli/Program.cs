using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Synapse.Core;
using Synapse.Core.Utilities;

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
            else if( args.Length > 0 && (args[0].ToLower() == "test" || args[0].ToLower() == "t") )
            {
                new Program().RediculousExcuseForUnitTest( args );
            }
            else
            {
                new Program().ProcessArgs( args );
            }
        }


        Dictionary<string, string> _methods = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
        string _service = "service";
        string _keygen = "keygen";

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
            _methods.Add( "startwait", "StartPlanWait" );
            _methods.Add( "sw", "StartPlanWait" );
            _methods.Add( "getelement", "GetPlanElement" );
            _methods.Add( "ge", "GetPlanElement" );
            _methods.Add( "getstatus", "GetPlanStatus" );
            _methods.Add( "gs", "GetPlanStatus" );
            _methods.Add( "setstatus", "SetPlanStatus" );
            _methods.Add( "ss", "SetPlanStatus" );
            _methods.Add( "cancel", "CancelPlan" );
            _methods.Add( "c", "CancelPlan" );

            SynapseServerConfig config = SynapseServerConfig.DeserializeOrNew( ServerRole.Controller );
            BaseUrl = $"{config.WebApi.ToUri( isUserInteractive: true )}/synapse/execute";
        }


        //todo: delete this and create actual unit tests
        void RediculousExcuseForUnitTest(string[] args)
        {
            string[] a = new string[] { "s", $"planName:{args[1]}", "dryRun:true" };
            System.Threading.Tasks.Parallel.For( 0, Int32.Parse( args[2] ), ctr => { ProcessArgs( a ); } );
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
                    Dictionary<string, string> parms = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
                    if( args.Length > 1 )
                    {
                        bool error = false;
                        parms = ParseCmdLine( args, 1, ref error, suppressErrorMessages: true );
                        if( parms.ContainsKey( "url" ) )
                        {
                            BaseUrl = parms["url"];
                            parms.Remove( "url" );
                        }
                    }
                    Console.WriteLine( $"Calling {_methods[arg0]} on {BaseUrl}" );

                    if( _methods[arg0] == "StartPlan" || _methods[arg0] == "StartPlanWait" )
                        RunStartPlanMethod( args, parms, _methods[arg0] == "StartPlanWait" );
                    else
                        RunMethod( new ControllerServiceHttpApiClient( BaseUrl ), _methods[arg0], args );
                }
                else if( arg0.StartsWith( _service ) )
                    RunServiceAction( args );
                else if( arg0.StartsWith( _keygen ) )
                    RunKeyGenerator( args );
                else
                    WriteHelpAndExit( "Unknown action." );
            }
        }

        protected virtual void RunStartPlanMethod(string[] args, Dictionary<string, string> parameters, bool isSync)
        {
            string methodName = "StartPlan";
            ControllerServiceHttpApiClient instance = new ControllerServiceHttpApiClient( BaseUrl );
            bool needHelp = args.Length == 2 && args[1].ToLower().Contains( "help" );

            if( needHelp )
            {
                Dictionary<string, Type> parms = new Dictionary<string, Type>();
                parms.Add( "planName", typeof( string ) );
                parms.Add( "dryRun", typeof( bool ) );
                parms.Add( "requestNumber", typeof( string ) );
                if( isSync )
                {
                    parms.Add( "path", typeof( string ) );
                    parms.Add( "serializationType", typeof( SerializationType ) );
                    parms.Add( "setContentType", typeof( bool ) );
                    parms.Add( "pollingIntervalSeconds", typeof( int ) );
                    parms.Add( "timeoutSeconds", typeof( int ) );
                }
                parms.Add( "nodeRootUrl", typeof( string ) );
                parms.Add( "asPost", typeof( string ) );
                Console.WriteLine( $"Parameter options for {methodName}:\r\n" );
                WriteMethodParametersHelp( parms );
                Console.WriteLine( "\r\nRemaining argname:argvalue pairs will be passed as dynamic parameters, unless" );
                Console.WriteLine( "'asPost' is specified, in which case a file of {key: value} pairs is posted" );
                Console.WriteLine( "and remaining parameters are ignored.  Layout of 'asPost' file is:\r\n" );
                Console.WriteLine( "DynamicParameters:\r\n  key0: value0\r\n  key1: value1\r\n  key2: value2\r\n" );
            }
            else
            {
                string planName = null;
                string pn = nameof( planName );
                if( parameters.ContainsKey( pn ) )
                {
                    planName = parameters[pn];
                    parameters.Remove( pn );
                }
                else
                    throw new Exception( "PlanName is required." );

                bool dryRun = false;
                string dr = nameof( dryRun );
                if( parameters.ContainsKey( dr ) )
                {
                    bool.TryParse( parameters[dr], out dryRun );
                    parameters.Remove( dr );
                }

                string requestNumber = null;
                string rn = nameof( requestNumber );
                if( parameters.ContainsKey( rn ) )
                {
                    requestNumber = parameters[rn];
                    parameters.Remove( rn );
                }

                string path = "Actions[0]:Result:ExitData";
                SerializationType serializationType = SerializationType.Json;
                bool setContentType = true;
                int pollingIntervalSeconds = 1;
                int timeoutSeconds = 120;
                if( isSync )
                {
                    string pa = nameof( path );
                    if( parameters.ContainsKey( pa ) )
                    {
                        path = parameters[pa];
                        parameters.Remove( pa );
                    }

                    string st = nameof( serializationType );
                    if( parameters.ContainsKey( st ) )
                    {
                        Enum.TryParse( parameters[st], true, out serializationType );
                        parameters.Remove( st );
                    }

                    string sct = nameof( setContentType );
                    if( parameters.ContainsKey( sct ) )
                    {
                        bool.TryParse( parameters[sct], out setContentType );
                        parameters.Remove( sct );
                    }

                    string pi = nameof( pollingIntervalSeconds );
                    if( parameters.ContainsKey( pi ) )
                    {
                        int.TryParse( parameters[pi], out pollingIntervalSeconds );
                        parameters.Remove( pi );
                    }

                    string to = nameof( timeoutSeconds );
                    if( parameters.ContainsKey( to ) )
                    {
                        int.TryParse( parameters[to], out timeoutSeconds );
                        parameters.Remove( to );
                    }
                }

                string nodeRootUrl = null;
                string nu = nameof( nodeRootUrl );
                if( parameters.ContainsKey( nu ) )
                {
                    nodeRootUrl = parameters[nu];
                    parameters.Remove( nu );
                }

                bool postDynamicParameters = false;
                string asPost = null;
                string ap = nameof( asPost );
                if( parameters.ContainsKey( ap ) )
                {
                    string fileName = parameters[ap];
                    parameters.Remove( ap );

                    try
                    {
                        if( !File.Exists( fileName ) )
                            throw new FileNotFoundException( $"Could not find DynamicParameters file: {fileName}.", fileName );

                        string yaml = File.ReadAllText( fileName );
                        StartPlanEnvelope planEnvelope = StartPlanEnvelope.FromYaml( yaml );

                        if( planEnvelope == null )
                            throw new Exception( $"Could not deserialize DynamicParameters file: {fileName}." );

                        parameters = planEnvelope.DynamicParameters;

                        postDynamicParameters = true;
                    }
                    catch( Exception ex )
                    {
                        WriteException( ex );
                        Environment.Exit( 1 );
                    }
                }


                try
                {
                    if( isSync )
                    {
                        object result = instance.StartPlanWait( planName, dryRun, requestNumber, parameters, postDynamicParameters,
                            path, serializationType, setContentType,
                            pollingIntervalSeconds, timeoutSeconds, nodeRootUrl );
                        Console.WriteLine( result );
                    }
                    else
                    {
                        long result = instance.StartPlan( planName, dryRun, requestNumber, parameters, postDynamicParameters, nodeRootUrl );
                        Console.WriteLine( result );
                    }
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
                    SynapseServer.RunConsole();
                    break;
                }
                case "install":
                {
                    string message = string.Empty;
                    bool error = false;
                    Dictionary<string, string> values = ParseCmdLine( args, 2, ref error, true );
                    if( !InstallUtility.InstallAndStartService( serverRole: ServerRole.Controller, installOptions: values, message: out message ) )
                    {
                        Console.WriteLine( message );
                        Environment.Exit( 1 );
                    }

                    break;
                }
                case "uninstall":
                {
                    string message = string.Empty;
                    if( !InstallUtility.StopAndUninstallService( out message ) )
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


        private void RunKeyGenerator(string[] args)
        {
            Console.WriteLine( $"Calling {nameof( GenerateRsaKeys )}." );
            RunMethod( this, nameof( GenerateRsaKeys ), args );
        }

        public void GenerateRsaKeys(string keyContainerName, string filePath)
        {
            CryptoHelpers.GenerateRsaKeys( keyContainerName, $"{filePath}.pubPriv", $"{filePath}.pubOnly" );
        }


        #region Help
        protected override void WriteHelpAndExit(string errorMessage = null)
        {
            StringBuilder df = new StringBuilder();
            ////Dictionary<string, string> cdf = SynapseServerConfig.GetConfigDefaultValues( serverRole: ServerRole.Controller );
            ////df.AppendFormat( "{0,-15}- Optional install args, use argname:value.  Defaults shown.\r\n", "" );
            ////foreach( string key in cdf.Keys )
            ////    df.AppendLine( $"                 - {key}:{cdf[key]}" );
            df.AppendLine( $"                 - Run:true  (Optionally Starts the Windows Service)\r\n" );

            bool haveError = !string.IsNullOrWhiteSpace( errorMessage );

            ConsoleColor defaultColor = Console.ForegroundColor;

            Console_WriteLine( $"synapse.controller.cli.exe, Version: {typeof( Program ).Assembly.GetName().Version}\r\n", ConsoleColor.Green );
            Console.WriteLine( "Syntax:" );
            Console_WriteLine( "  synapse.controller.cli.exe service {0}command{1} | {0}httpAction parm:value{1} |", ConsoleColor.Cyan, "{", "}" );
            Console.WriteLine( "       interactive|i [url:http://{1}host:port{2}/synapse/execute]\r\n", "", "{", "}" );
            Console_WriteLine( "  About URLs:{0,-2}URL is an optional parameter on all commands except 'service'", ConsoleColor.Green, "" );
            Console.WriteLine( "{0,-15}commands. Specify as [url:http://{1}host:port{2}/synapse/execute].", "", "{", "}" );
            Console.WriteLine( "{0,-15}URL default is localhost:{1}port{2} (See WebApiPort in config.yaml)\r\n", "", "{", "}" );
            Console.WriteLine( "  interactive{0,-2}Run this CLI in interactive mode, optionally specify URL.", "" );
            Console.WriteLine( "{0,-15}All commands below work in standard or interactive modes.\r\n", "" );
            Console.WriteLine( "  service{0,-6}Install/Uninstall the Windows Service, or Run the Service", "" );
            Console.WriteLine( "{0,-15}as a cmdline-hosted daemon.", "" );
            Console.WriteLine( "{0,-15}- Commands: install [run:true|false] | uninstall | run", "" );
            Console.WriteLine( "{0,-15}- Example:  synapse.controller.cli service install run:false", "" );
            Console.WriteLine( "{0,-15}            synapse.controller.cli service run\r\n", "" );
            Console.WriteLine( "  keygen{0,-7}Generate RSA key for signing Plans.", "" );
            Console.WriteLine( "{0,-15}- keyContainerName:  Key values storage Container.", "" );
            Console.WriteLine( "{0,-15}- filePath:          Path and filename to store key values.\r\n", "" );
            Console.WriteLine( "  httpAction{0,-3}Execute a command, optionally specify URL.", "" );
            Console.WriteLine( "{0,-15}Parm help: synapse.controller.cli {1}httpAction{2} help.\r\n", "", "{", "}" );
            Console.WriteLine( "  - httpActions:", "" );
            Console.WriteLine( "    - Hello|hi           Returns 'Hello, World!'.", "" );
            Console.WriteLine( "    - WhoAmI|who         Returns ControllerServer User Context.", "" );
            Console.WriteLine( "    - List|l             Get a list of Plans.", "" );
            Console.WriteLine( "    - ListInstances|li   Get a list of Plans Instances.", "" );
            Console.WriteLine( "    - Start|s            Start a new Plan Instance.", "" );
            Console.WriteLine( "    - StartWait|sw       Run a new Plan Instance at Controller.", "" );
            Console.WriteLine( "    - StartWait|sw       Run a new Plan Instance at Controller.", "" );
            Console.WriteLine( "    - GetPlanElement|ge  Get an element part from a ResultPlan.", "" );
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