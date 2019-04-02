using System;
using System.IO;
using System.Net;

using YamlDotNet.Serialization;

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
        public string AuditLogRootPath { get; set; } = @".\Logs";
        [YamlIgnore]
        public bool HasAuditLogRootPath { get { return !string.IsNullOrWhiteSpace( AuditLogRootPath ); } }

        public string Log4NetConversionPattern { get; set; } = "%d{ISO8601}|%-5p|(%t)|%m%n";
        [YamlIgnore]
        public bool HasLog4NetConversionPattern { get { return !string.IsNullOrWhiteSpace( Log4NetConversionPattern ); } }

        public bool SerializeResultPlan { get; set; } = true;
        public bool ValidatePlanSignature { get; set; } = false;
        public string ControllerUrl { get; set; }
        public AuthenticationSchemes ControllerAuthenticationScheme { get; set; } = AuthenticationSchemes.None;
        [YamlIgnore]
        public bool HasControllerUrl { get { return !string.IsNullOrWhiteSpace( ControllerUrl ); } }


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
    }
}