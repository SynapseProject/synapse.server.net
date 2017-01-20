using System;
using System.IO;
using System.Net;
using Synapse.Core.Utilities;


namespace Synapse.Services
{
    /// <summary>
    /// Hold the startup config for Synapse.Node; written as an independent class (not using .NET config) for cross-platform compatibility.
    /// </summary>
    public class SynapseControllerConfig
    {
        public SynapseControllerConfig()
        {
            AuthenticationScheme = AuthenticationSchemes.IntegratedWindowsAuthentication;
            NodeServiceUrl = "http://localhost:8000/synapse/node";
        }

        public static readonly string CurrentPath = $"{Path.GetDirectoryName( typeof( SynapseControllerConfig ).Assembly.Location )}";
        public static readonly string FileName = $"{Path.GetDirectoryName( typeof( SynapseControllerConfig ).Assembly.Location )}\\Synapse.Controller.config.yaml";

        public int MaxServerThreads { get; set; }
        public AuthenticationSchemes AuthenticationScheme { get; set; }
        public string WebApiPort { get; set; }
        public string NodeServiceUrl { get; set; }


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

        public static SynapseControllerConfig Deserialze()
        {
            return YamlHelpers.DeserializeFile<SynapseControllerConfig>( FileName );
        }
    }
}