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
        public static readonly string CurrentPath = $"{Path.GetDirectoryName( typeof( SynapseNodeConfig ).Assembly.Location )}";

        public SynapseNodeConfig()
        {
        }


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


        public string GetResolvedAuditLogRootPath()
        {
            if( Path.IsPathRooted( AuditLogRootPath ) )
                return AuditLogRootPath;
            else
                return PathCombine( CurrentPath, AuditLogRootPath );
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


        public static Dictionary<string, string> GetConfigDefaultValues()
        {
            Dictionary<string, string> values = new Dictionary<string, string>();

            SynapseNodeConfig c = new SynapseNodeConfig();

            values[nameof( c.MaxServerThreads )] = c.MaxServerThreads.ToString();
            values[nameof( c.AuditLogRootPath )] = c.AuditLogRootPath;
            values[nameof( c.Log4NetConversionPattern )] = c.Log4NetConversionPattern;
            values[nameof( c.SerializeResultPlan )] = c.SerializeResultPlan.ToString();
            values[nameof( c.ValidatePlanSignature )] = c.ValidatePlanSignature.ToString();
            values[nameof( c.ControllerServiceUrl )] = c.ControllerServiceUrl;

            return values;
        }

        public void Configure(Dictionary<string, string> values)
        {
            SynapseNodeConfig c = new SynapseNodeConfig();

            if( values.ContainsKey( nameof( c.MaxServerThreads ).ToLower() ) )
                c.MaxServerThreadsString = values[nameof( c.MaxServerThreads ).ToLower()];

            if( values.ContainsKey( nameof( c.AuditLogRootPath ).ToLower() ) )
                c.AuditLogRootPath = values[nameof( c.AuditLogRootPath ).ToLower()];

            if( values.ContainsKey( nameof( c.Log4NetConversionPattern ).ToLower() ) )
                c.Log4NetConversionPattern = values[nameof( c.Log4NetConversionPattern ).ToLower()];

            if( values.ContainsKey( nameof( c.SerializeResultPlan ).ToLower() ) )
                c.SerializeResultPlanString = values[nameof( c.SerializeResultPlan ).ToLower()];

            if( values.ContainsKey( nameof( c.ValidatePlanSignature ).ToLower() ) )
                c.ValidatePlanSignatureString = values[nameof( c.ValidatePlanSignature ).ToLower()];

            if( values.ContainsKey( nameof( c.ControllerServiceUrl ).ToLower() ) )
                c.ControllerServiceUrl = values[nameof( c.ControllerServiceUrl ).ToLower()];

            Configure( c );
        }

        public void Configure(SynapseNodeConfig value)
        {
            if( value.TestSetMaxServerThreadsString )
                MaxServerThreads = value.MaxServerThreads;

            if( value.HasAuditLogRootPath )
                AuditLogRootPath = value.AuditLogRootPath;

            if( value.HasLog4NetConversionPattern )
                Log4NetConversionPattern = value.Log4NetConversionPattern;

            if( value.TestSetSerializeResultPlanString )
                SerializeResultPlan = value.SerializeResultPlan;

            if( value.TestSetValidatePlanSignatureString )
                ValidatePlanSignature = value.ValidatePlanSignature;

            if( value.HasControllerServiceUrl )
                ControllerServiceUrl = value.ControllerServiceUrl;
        }
    }
}