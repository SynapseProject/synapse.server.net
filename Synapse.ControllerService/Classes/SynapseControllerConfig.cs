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
        }

        public static readonly string CurrentPath = $"{Path.GetDirectoryName( typeof( SynapseControllerConfig ).Assembly.Location )}";
        public static readonly string FileName = $"{Path.GetDirectoryName( typeof( SynapseControllerConfig ).Assembly.Location )}\\Synapse.Controller.config.yaml";

        //public int MaxServerThreads { get; set; } = 0;
        public AuthenticationSchemes AuthenticationScheme { get; set; } = AuthenticationSchemes.IntegratedWindowsAuthentication;
        public int WebApiPort { get; set; } = 8008;
        public string NodeServiceUrl { get; set; } = "http://localhost:8000/synapse/node";
        public string DalProvider { get; set; } = "Synapse.Controller.Dal.FileSystem:Synapse.Services.Controller.Dal.FileSystemDal";


        public void Serialize()
        {
            YamlHelpers.SerializeFile( FileName, this, serializeAsJson: false, emitDefaultValues: true );
        }

        public static SynapseControllerConfig Deserialze()
        {
            if( !File.Exists( FileName ) )
                new SynapseControllerConfig().Serialize();

            return YamlHelpers.DeserializeFile<SynapseControllerConfig>( FileName );
        }
    }
}