using System;
using System.Collections.Generic;
using System.IO;

using Synapse.Core.Utilities;


namespace Synapse.Services
{
    /// <summary>
    /// Hold the startup config for Synapse.Node; written as an independent class (not using .NET config) for cross-platform compatibility.
    /// </summary>
    public class SynapseNodeConfig
    {
        public SynapseNodeConfig()
        {
        }

        public static readonly string CurrentPath = $"{Path.GetDirectoryName( typeof( SynapseNodeConfig ).Assembly.Location )}";
        public static readonly string FileName = $"{Path.GetDirectoryName( typeof( SynapseNodeConfig ).Assembly.Location )}\\Synapse.Node.config.yaml";


        public string ServiceName { get; set; } = "Synapse.Node";
        internal bool HasServiceName { get { return !string.IsNullOrWhiteSpace( ServiceName ); } }

        public string ServiceDisplayName { get; set; } = "Synapse Node";
        internal bool HasServiceDisplayName { get { return !string.IsNullOrWhiteSpace( ServiceDisplayName ); } }

        public int MaxServerThreads { get; set; } = 0;
        internal string MaxServerThreadsString { get; set; } = "0";
        internal bool TestSetMaxServerThreadsString
        {
            get
            {
                int threads = MaxServerThreads;
                bool ok = int.TryParse( MaxServerThreadsString, out threads );
                if( ok )
                    MaxServerThreads = threads;
                return ok;
            }
        }

        public string AuditLogRootPath { get; set; } = @".\Logs";
        internal bool HasAuditLogRootPath { get { return !string.IsNullOrWhiteSpace( AuditLogRootPath ); } }

        public string ServiceLogRootPath { get; set; } = @".\Logs";
        internal bool HasServiceLogRootPath { get { return !string.IsNullOrWhiteSpace( ServiceLogRootPath ); } }

        public string Log4NetConversionPattern { get; set; } = "%d{ISO8601}|%-5p|(%t)|%m%n";
        internal bool HasLog4NetConversionPattern { get { return !string.IsNullOrWhiteSpace( Log4NetConversionPattern ); } }

        public bool SerializeResultPlan { get; set; } = true;
        internal string SerializeResultPlanString { get; set; } = "true";
        internal bool TestSetSerializeResultPlanString
        {
            get
            {
                bool v = SerializeResultPlan;
                bool ok = bool.TryParse( SerializeResultPlanString, out v );
                if( ok )
                    SerializeResultPlan = v;
                return ok;
            }
        }

        public bool ValidatePlanSignature { get; set; } = true;
        internal string ValidatePlanSignatureString { get; set; } = "true";
        internal bool TestSetValidatePlanSignatureString
        {
            get
            {
                bool v = ValidatePlanSignature;
                bool ok = bool.TryParse( ValidatePlanSignatureString, out v );
                if( ok )
                    ValidatePlanSignature = v;
                return ok;
            }
        }

        public string ControllerServiceUrl { get; set; } = "http://localhost:8008/synapse/execute";
        internal bool HasControllerServiceUrl { get { return !string.IsNullOrWhiteSpace( ControllerServiceUrl ); } }

        public int WebApiPort { get; set; } = 8000;
        internal string WebApiPortString { get; set; } = "8000";
        internal bool TestSetWebApiPortString
        {
            get
            {
                int port = WebApiPort;
                bool ok = int.TryParse( WebApiPortString, out port );
                if( ok )
                    WebApiPort = port;
                return ok;
            }
        }


        public string GetResolvedAuditLogRootPath()
        {
            if( Path.IsPathRooted( AuditLogRootPath ) )
                return AuditLogRootPath;
            else
                return PathCombine( CurrentPath, AuditLogRootPath );
        }

        public string GetResolvedServiceLogRootPath()
        {
            if( Path.IsPathRooted( ServiceLogRootPath ) )
                return ServiceLogRootPath;
            else
                return PathCombine( CurrentPath, ServiceLogRootPath );
        }

        /// <summary>
        /// A wrapper on Path.Combine to correct for fronting/trailing backslashes that otherwise fail in Path.Combine.
        /// </summary>
        /// <param name="paths">An array of parts of the path.</param>
        /// <returns>The combined path</returns>
        public static string PathCombine(params string[] paths)
        {
            if( paths.Length > 0 )
            {
                int last = paths.Length - 1;
                for( int c = 0; c <= last; c++ )
                {
                    if( c != 0 )
                    {
                        paths[c] = paths[c].Trim( Path.DirectorySeparatorChar );
                    }
                    if( c != last )
                    {
                        paths[c] = string.Format( "{0}\\", paths[c] );
                    }
                }
            }
            else
            {
                return string.Empty;
            }

            return Path.Combine( paths );
        }


        public void Serialize()
        {
            YamlHelpers.SerializeFile( FileName, this, serializeAsJson: false, emitDefaultValues: true );
        }

        public static SynapseNodeConfig Deserialze()
        {
            if( !File.Exists( FileName ) )
                new SynapseNodeConfig().Serialize();

            return YamlHelpers.DeserializeFile<SynapseNodeConfig>( FileName );
        }

        public static Dictionary<string, string> GetConfigDefaultValues()
        {
            Dictionary<string, string> values = new Dictionary<string, string>();

            SynapseNodeConfig c = new SynapseNodeConfig();
            values[nameof( c.ServiceName )] = c.ServiceName;
            values[nameof( c.ServiceDisplayName )] = c.ServiceDisplayName;
            values[nameof( c.MaxServerThreads )] = c.MaxServerThreads.ToString();
            values[nameof( c.AuditLogRootPath )] = c.AuditLogRootPath;
            values[nameof( c.ServiceLogRootPath )] = c.ServiceLogRootPath;
            values[nameof( c.Log4NetConversionPattern )] = c.Log4NetConversionPattern;
            values[nameof( c.SerializeResultPlan )] = c.SerializeResultPlan.ToString();
            values[nameof( c.ValidatePlanSignature )] = c.ValidatePlanSignature.ToString();
            values[nameof( c.ControllerServiceUrl )] = c.ControllerServiceUrl;
            values[nameof( c.WebApiPort )] = c.WebApiPort.ToString();

            return values;
        }

        public static SynapseNodeConfig Configure(Dictionary<string, string> values)
        {
            SynapseNodeConfig c = new SynapseNodeConfig();

            if( values.ContainsKey( nameof( c.ServiceName ).ToLower() ) )
                c.ServiceName = values[nameof( c.ServiceName ).ToLower()];

            if( values.ContainsKey( nameof( c.ServiceDisplayName ).ToLower() ) )
                c.ServiceDisplayName = values[nameof( c.ServiceDisplayName ).ToLower()];

            if( values.ContainsKey( nameof( c.MaxServerThreads ).ToLower() ) )
                c.MaxServerThreadsString = values[nameof( c.MaxServerThreads ).ToLower()];

            if( values.ContainsKey( nameof( c.AuditLogRootPath ).ToLower() ) )
                c.AuditLogRootPath = values[nameof( c.AuditLogRootPath ).ToLower()];

            if( values.ContainsKey( nameof( c.ServiceLogRootPath ).ToLower() ) )
                c.ServiceLogRootPath = values[nameof( c.ServiceLogRootPath ).ToLower()];

            if( values.ContainsKey( nameof( c.Log4NetConversionPattern ).ToLower() ) )
                c.Log4NetConversionPattern = values[nameof( c.Log4NetConversionPattern ).ToLower()];

            if( values.ContainsKey( nameof( c.SerializeResultPlan ).ToLower() ) )
                c.SerializeResultPlanString = values[nameof( c.SerializeResultPlan ).ToLower()];

            if( values.ContainsKey( nameof( c.ValidatePlanSignature ).ToLower() ) )
                c.ValidatePlanSignatureString = values[nameof( c.ValidatePlanSignature ).ToLower()];

            if( values.ContainsKey( nameof( c.ControllerServiceUrl ).ToLower() ) )
                c.ControllerServiceUrl = values[nameof( c.ControllerServiceUrl ).ToLower()];

            if( values.ContainsKey( nameof( c.WebApiPort ).ToLower() ) )
                c.WebApiPortString = values[nameof( c.WebApiPort ).ToLower()];

            return Configure( c );
        }

        public static SynapseNodeConfig Configure(SynapseNodeConfig value)
        {
            //initialize with defaults
            SynapseNodeConfig config = new SynapseNodeConfig();
            //ovrride defaults with file values
            if( File.Exists( FileName ) )
                config = YamlHelpers.DeserializeFile<SynapseNodeConfig>( FileName );

            //configure with anything provided
            if( value.HasServiceName && !(value.ServiceName == config.ServiceName) )
                config.ServiceName = value.ServiceName;

            if( value.HasServiceDisplayName && !(value.ServiceDisplayName == config.ServiceDisplayName) )
                config.ServiceDisplayName = value.ServiceDisplayName;

            if( value.TestSetMaxServerThreadsString && !(value.MaxServerThreads == config.MaxServerThreads) )
                config.MaxServerThreads = value.MaxServerThreads;

            if( value.HasAuditLogRootPath && !(value.AuditLogRootPath == config.AuditLogRootPath) )
                config.AuditLogRootPath = value.AuditLogRootPath;

            if( value.HasServiceLogRootPath && !(value.ServiceLogRootPath == config.ServiceLogRootPath) )
                config.ServiceLogRootPath = value.ServiceLogRootPath;

            if( value.HasLog4NetConversionPattern && !(value.Log4NetConversionPattern == config.Log4NetConversionPattern) )
                config.Log4NetConversionPattern = value.Log4NetConversionPattern;

            if( value.TestSetSerializeResultPlanString && !(value.SerializeResultPlan == config.SerializeResultPlan) )
                config.SerializeResultPlan = value.SerializeResultPlan;

            if( value.TestSetValidatePlanSignatureString && !(value.ValidatePlanSignature == config.ValidatePlanSignature) )
                config.ValidatePlanSignature = value.ValidatePlanSignature;

            if( value.HasControllerServiceUrl && !(value.ControllerServiceUrl == config.ControllerServiceUrl) )
                config.ControllerServiceUrl = value.ControllerServiceUrl;

            if( value.TestSetWebApiPortString && !(value.WebApiPort == config.WebApiPort) )
                config.WebApiPort = value.WebApiPort;

            config.Serialize();

            return config;
        }
    }
}