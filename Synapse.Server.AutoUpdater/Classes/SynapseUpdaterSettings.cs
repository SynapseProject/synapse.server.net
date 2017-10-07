using System;
using System.Collections.Generic;
using System.IO;

using YamlDotNet.Serialization;

namespace Synapse.Server.AutoUpdater
{
    public class SynapseUpdaterSettings : HappyBin.AutoUpdater.UpdaterSettings
    {
        public static string FileName { get; private set; } = $"{Path.GetDirectoryName( typeof( SynapseUpdaterSettings ).Assembly.Location )}\\Synapse.Server.AutoUpdater.yaml";
        public List<string> ServiceConfigs { get; set; } = new List<string>();

        [YamlIgnore]
        new public string ProcessToStop { get { return base.ProcessToStop; } }


        public static void SerializeSample()
        {
            new SynapseUpdaterSettings()
            {
                ServiceConfigs = new List<string>() { { @"..\Synapse.Server.config.yaml" } },
                UpdateConfigUri = "http://host:port/updates/updateconfig.xml",
                RuntimeExe = @"..\Synapse.Server.exe",
                DownloadFolder = "patches",
                WaitForExitMillseconds = 30000,
                StartProcessAfterInstall = true,
                ServiceName = null,
            }.Serialize();
        }

        public void Serialize()
        {
            using( StreamWriter writer = new StreamWriter( FileName ) )
                new SerializerBuilder().Build().Serialize( writer, this );
        }

        public static SynapseUpdaterSettings Deserialize()
        {
            if( !File.Exists( FileName ) )
                throw new FileNotFoundException( $"Could not find {FileName}" );

            SynapseUpdaterSettings ssc = null;
            using( StreamReader reader = new StreamReader( FileName ) )
                ssc = new DeserializerBuilder().IgnoreUnmatchedProperties().Build().Deserialize<SynapseUpdaterSettings>( reader );
            return ssc;
        }
    }
}