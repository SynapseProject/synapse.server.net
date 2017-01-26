using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using Newtonsoft.Json;


namespace Synapse.Services.Controller.Cli
{
    class Program
    {
        static Dictionary<string, string> _methods = new Dictionary<string, string>();

        static void Main(string[] args)
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
                string methodName = null;
                if( _methods.ContainsKey( arg0 ) )
                {
                    methodName = _methods[arg0];
                    arg0 = "method";
                }

                switch( arg0 )
                {
                    case "run":
                    case "-run":
                    case "/run":
                    {
                        SynapseControllerService.RunConsole();
                        break;
                    }
                    case "install":
                    case "-install":
                    case "/install":
                    {
                        string message = string.Empty;
                        if( !InstallUtility.InstallService( install: true, message: out message ) )
                            Console.WriteLine( message );
                        break;
                    }
                    case "uninstall":
                    case "-uninstall":
                    case "/uninstall":
                    {
                        string message = string.Empty;
                        if( !InstallUtility.InstallService( install: false, message: out message ) )
                            Console.WriteLine( message );
                        break;
                    }
                    case "method":
                    {
                        RunMethod( methodName, args );
                        break;
                    }
                    default:
                    {
                        WriteHelpAndExit( "Unknown action." );
                        break;
                    }
                }
            }
        }


        static void RunMethod(string methodName, string[] args)
        {
            bool needHelp = args.Length == 2 && args[1].ToLower().Contains( "help" );

            switch( args[0].ToLower() )
            {
                case "getplanlist":
                {
                    break;
                }
                case "getplaninstanceidlist":
                {
                    break;
                }
                case "startplan":
                {
                    break;
                }
            }


            ControllerServiceHttpApiClient syntoApi = new ControllerServiceHttpApiClient( "http://localhost:8008/synapse/execute" );
            MethodInfo mi = typeof( ControllerServiceHttpApiClient ).GetMethod( methodName );
            ParameterInfo[] parms = mi.GetParameters();

            if( needHelp )
            {
                Console.WriteLine( $"Parameter options for {methodName}:\r\n" );
                WriteMethodParametersHelp( parms );
            }
            else
            {
                List<object> parameters = new List<object>();
                if( parms.Length > 0 )
                    parameters = GetMethodParameters( args, 1, parms, 0 );

                try
                {
                    object result = mi.Invoke( syntoApi, parameters.ToArray() );
                    if( result is IList && ((IList)result).Count == 1 )
                    {
                        result = ((IList)result)[0];
                    }

                    string jsonString = JsonConvert.SerializeObject( result, Formatting.Indented );
                    Console.WriteLine( jsonString );
                }
                catch( Exception ex )
                {
                    WriteException( ex );
                }
            }
        }


        #region utility methods
        static List<object> GetMethodParameters(string[] args, int cmdlineStartIndex, ParameterInfo[] parms, int parmsStartIndex)
        {
            if( args.Length < (cmdlineStartIndex + 1) )
                WriteHelpAndExit( "Not enough arguments specified." );

            Dictionary<string, string> options = ParseCmdLine( args, cmdlineStartIndex );

            List<object> parameters = new List<object>();
            for( int i = parmsStartIndex; i < parms.Length; i++ )
            {
                ParameterInfo parm = parms[i];
                if( options.Keys.Contains( parm.Name.ToLower() ) )
                    parameters.Add( ParseInput( options[parm.Name.ToLower()], parm.ParameterType ) );
                else
                    parameters.Add( null );
            }
            return parameters;
        }

        static Dictionary<string, string> ParseCmdLine(string[] args, int startIndex)
        {
            Dictionary<string, string> options = new Dictionary<string, string>();

            string pattern = @"(?<argname>\w+):(?<argvalue>.*)";
            for( int i = startIndex; i < args.Length; i++ )
            {
                Match match = Regex.Match( args[i], pattern );

                // If match not found, command line args are improperly formed.
                if( match.Success )
                    options[match.Groups["argname"].Value.ToLower()] = match.Groups["argvalue"].Value.ToLower();
                else
                    WriteHelpAndExit( "The command line arguments are not valid or are improperly formed. Use 'argname:argvalue' for extended arguments." );
            }

            return options;
        }

        static object ParseInput(string input, Type type)
        {
            if( type == typeof( List<Guid> ) )
            {
                return null; // input.CsvToList<Guid>();
            }
            else if( type == typeof( Guid? ) || type == typeof( Guid ) )
            {
                return Guid.Parse( input );
            }
            else if( type == typeof( int? ) || type == typeof( int ) )
            {
                return int.Parse( input );
            }
            else if( type == typeof( bool? ) || type == typeof( bool ) )
            {
                return bool.Parse( input );
            }
            else if( type == typeof( DateTime? ) || type == typeof( DateTime ) )
            {
                return bool.Parse( input );
            }
            else if( type.IsEnum )
            {
                return Enum.Parse( type, input, true );
            }
            else
            {
                return input;
            }
        }

        static void WriteMethodParametersHelp(ParameterInfo[] parms, int startIndex = 0)
        {
            int count = 0;
            for( int i = startIndex; i < parms.Length; i++ )
            {
                count++;
                ParameterInfo parm = parms[i];
                //Console.WriteLine( $"\t{}\t{}" );

                if( parm.Name.ToLower() == "syntosarecordtype" )
                    Console.WriteLine( "\t{0,-30}{1}", parm.Name, "{see applicable types}" );
                else
                    Console.WriteLine( "\t{0,-30}{1}", parm.Name, GetTypeFriendlyName( parm.ParameterType ) );
            }
            if( count == 0 )
                Console.WriteLine( $"\tNo additional parameter options." );
        }

        static string GetTypeFriendlyName(Type type)
        {
            string typeName = type.ToString().ToLower();
            if( typeName.Contains( "guid" ) )
            {
                if( typeName.Contains( "generic.list" ) )
                    return "Csv list of Guids or JSON list of Guids";
                else
                    return "Guid";
            }
            else if( typeName.Contains( "int" ) )
            {
                return "int";
            }
            else if( typeName.Contains( "bool" ) )
            {
                return "bool";
            }
            else if( typeName.Contains( "string" ) )
            {
                return "string";
            }
            else if( typeName.Contains( "datetime" ) )
            {
                return "DateTime";
            }
            else if( type.IsEnum )
            {
                return GetEnumValuesCsv( type );
            }
            else
            {
                return type.ToString().Replace( "Syntosa.Core.ObjectModel.", "" );
            }
        }

        static string GetEnumValuesCsv(Type enumType)
        {
            Array values = Enum.GetValues( enumType );
            List<object> av = new List<object>();
            foreach( object v in values ) av.Add( v );
            return string.Join( ",", av );
        }

        static void WriteException(Exception ex)
        {
            ConsoleColor currentForeground = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine( $"\r\n*** An error occurred:\r\n" );
            string[] msgs = Synapse.Common.WebApi.Utilities.UnwindException( ex ).Split( new string[] { @"\r\n" }, StringSplitOptions.RemoveEmptyEntries );
            foreach( string msg in msgs )
                Console.WriteLine( msg );
            Console.WriteLine( $"\r\n" );
            Console.ForegroundColor = currentForeground;
        }
        #endregion


        #region Help
        static void WriteHelpAndExit(string errorMessage = null)
        {
            bool haveError = !string.IsNullOrWhiteSpace( errorMessage );

            ConsoleColor defaultColor = Console.ForegroundColor;

            Console_WriteLine( $"synapse..controller.cli.exe, Version: {typeof( Program ).Assembly.GetName().Version}\r\n", ConsoleColor.Green );
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

        static void Console_WriteLine(string s, ConsoleColor color, params object[] args)
        {
            Console.ForegroundColor = color;
            Console.WriteLine( s, args );
        }
        #endregion
    }

    internal class Arguments
    {
        const string __plan = "plan";
        const string __data = "data";
        const string __run = "run";
        const string __taskmodel = "taskmodel";
        const string __render = "render";
        const string __resultplan = "resultplan";

        public Arguments(string[] args)
        {
            IsParsed = true;

            if( args.Length == 0 )
            {
                IsParsed = false;
            }
            else if( args.Length == 1 )
            {
                string p = args[0].ToLower();
                if( p.Equals( "/?" ) || p.Equals( "/help" ) )
                    IsParsed = false;
            }

            if( IsParsed )
            {
                Args = ParseCmdLine( args );

                #region Plan
                if( Args.Keys.Contains( __plan ) )
                {
                    if( File.Exists( Args[__plan] ) )
                    {
                        Plan = File.ReadAllText( Args[__plan] );
                        PlanFilePath = Args[__plan];
                    }
                    else
                    {
                        //string plan = null;
                        //if( CryptoHelpers.TryDecode( Args[__plan], out plan ) )
                        //    Plan = plan;
                        //else
                        //    Message = "  * Unable to resolve Plan as path or encoded string.\r\n";
                    }

                    Args.Remove( __plan );
                }
                else
                {
                    //Message = "No plan specified.";
                }
                #endregion

                #region Data
                if( Args.Keys.Contains( __data ) )
                {
                    Data = Args[__run];
                    Args.Remove( __run );
                }
                else
                {
                    Data = string.Empty;
                }
                #endregion

                #region Run
                if( Args.Keys.Contains( __run ) )
                {
                    Run = true;

                    Args.Remove( __run );
                }
                else
                {
                    Run = false;
                }
                #endregion

                #region ResultPlan
                if( Args.Keys.Contains( __resultplan ) )
                {
                    ResultPlan = Args[__resultplan];
                    Args.Remove( __resultplan );
                }
                else
                {
                    ResultPlan = string.Empty;
                }
                #endregion
            }

            IsParsed &= string.IsNullOrWhiteSpace( Message );
        }

        public Dictionary<string, string> Args { get; internal set; }
        public string Plan { get; set; }
        public string PlanFilePath { get; set; }
        public string Data { get; set; }
        public bool Run { get; set; }
        public string ResultPlan { get; set; }
        public string Message { get; internal set; }
        public bool IsParsed { get; internal set; }

        Dictionary<string, string> ParseCmdLine(string[] args)
        {
            IsParsed = true;
            Dictionary<string, string> options = new Dictionary<string, string>();

            string pattern = @"(?<argname>/\w+):(?<argvalue>.*)";
            for( int i = 0; i < args.Length; i++ )
            {
                if( args[i].ToLower() == "/run" )
                    args[i] += ":true"; //hack, need to update regex

                Match match = Regex.Match( args[i], pattern );

                // If match not found, command line args are improperly formed.
                if( match.Success )
                {
                    options[match.Groups["argname"].Value.ToLower().TrimStart( '/' )] =
                        match.Groups["argvalue"].Value;
                }
                else
                {
                    Message = "The command line arguments are not valid or are improperly formed. Use 'argname:argvalue' for extended arguments.\r\n";
                    IsParsed = false;
                    break;
                }
            }

            return options;
        }
    }
}