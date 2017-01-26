using System;
using System.Collections.Generic;


namespace Synapse.Services.Controller.Cli
{
    class Program : Synapse.Common.CmdLine.HttpApiCliBase
    {
        static void Main(string[] args)
        {
            new Program().ProcessArgs( args );
        }


        Dictionary<string, string> _methods = new Dictionary<string, string>();
        string _service = "service";

        void ProcessArgs(string[] args)
        {
            if( args.Length == 0 )
            {
                WriteHelpAndExit();
            }
            else
            {
                _methods.Add( "getplanlist", "GetPlanList" );
                _methods.Add( "-getplanlist", "GetPlanList" );
                _methods.Add( "/getplanlist", "GetPlanList" );
                _methods.Add( "listplans", "GetPlanList" );
                _methods.Add( "getplaninstanceidlist", "GetPlanInstanceIdList" );
                _methods.Add( "-getplaninstanceidlist", "GetPlanInstanceIdList" );
                _methods.Add( "/getplaninstanceidlist", "GetPlanInstanceIdList" );
                _methods.Add( "listplaninstances", "GetPlanInstanceIdList" );

                string arg0 = args[0].ToLower();

                if( _methods.ContainsKey( arg0 ) )
                {
                    ControllerServiceHttpApiClient c = new ControllerServiceHttpApiClient( "" );
                    RunMethod( c, _methods[arg0], args );
                }
                else if( arg0.StartsWith( _service ) )
                {
                    RunServiceAction( args );
                }
                else
                {
                    WriteHelpAndExit( "Unknown action." );
                }
            }
        }


        protected virtual void RunServiceAction(string[] args)
        {
            if( args.Length < 1 )
                WriteHelpAndExit( "Not enough arguments specified." );

            Dictionary<string, string> options = ParseCmdLine( args, 0 );

            switch( options[_service] )
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
            Console_WriteLine( "  synapse.cli.exe /plan:{0}filePath{1}|{0}encodedPlanString{1}", ConsoleColor.Cyan, "{", "}" );
            Console.WriteLine( "    [/resultPlan:{0}filePath{1}|true] [/dryRun:true|false]", "{", "}" );
            Console.WriteLine( "    [/taskModel:inProc|external] [/render:encode|decode] [dynamic parameters]\r\n" );
            Console_WriteLine( "  /plan{0,-8}- filePath: Valid path to plan file.", ConsoleColor.Green, "" );
            Console.WriteLine( "{0,-15}- [or] encodedPlanString: Inline base64 encoded plan string.", "" );
            Console.WriteLine( "  /resultPlan{0,-2}- filePath: Valid path to write ResultPlan output file.", "" );
            Console.WriteLine( "{0,-15}- [or]: 'true' will write to same path as /plan as *.result.*", "" );
            Console.WriteLine( "  /dryRun{0,-6}Specifies whether to execute the plan as a DryRun only.", "" );
            Console.WriteLine( "{0,-15}  Default is false.", "" );
            Console.WriteLine( "  /taskModel{0,-3}Specifies whether to execute the plan on an internal", "" );
            Console.WriteLine( "{0,-15}  thread or shell process.  Default is InProc.", "" );
            Console.WriteLine( "  /render{0,-6}- encode: Returns the base64 encoded value of the", "" );
            Console.WriteLine( "{0,-15}  specifed plan file.", "" );
            Console.WriteLine( "{0,-15}- decode: Returns the base64 decoded value of the specified", "" );
            Console.WriteLine( "{0,-15}  encodedPlanString.", "" );
            Console.WriteLine( "  dynamic{0,-6}Any remaining /arg:value pairs will passed to the plan", "" );
            Console.WriteLine( "{0,-15}  as dynamic parms.", "" );

            if( haveError )
                Console_WriteLine( $"\r\n\r\n*** Last error:\r\n{errorMessage}\r\n", ConsoleColor.Red );

            Console.ForegroundColor = defaultColor;

            Environment.Exit( haveError ? 1 : 0 );
        }
        #endregion
    }
}