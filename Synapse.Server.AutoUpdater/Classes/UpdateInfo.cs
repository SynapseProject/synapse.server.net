using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Synapse.Server.AutoUpdater
{
    public class UpdateInfo : HappyBin.AutoUpdater.UpdaterSettings
    {
        public static string FileName { get; private set; } = $"{Path.GetDirectoryName( typeof( UpdateInfo ).Assembly.Location )}\\Synapse.Server.AutoUpdater.yaml";
        public List<string> ConfigFiles { get; set; } = new List<string>();

        public static void SerializeSample()
        {
            new UpdateInfo()
            {
                DownloadFolder = "patches",
                RuntimeExe = "Synapse.Server.exe",
                StartProcessAfterInstall = true,
                WaitForExitMillseconds = 30000,
                ConfigFiles = new List<string>() { { "Synapse.Server.config.yaml" } },
                ServiceName = null,
                UpdateConfigUri = "http://xxx"
            }.Serialize();
        }

        public void Serialize()
        {
            using( StreamWriter writer = new StreamWriter( FileName ) )
                new SerializerBuilder().Build().Serialize( writer, this );
        }

        public static UpdateInfo Deserialize()
        {
            if( !File.Exists( FileName ) )
                throw new FileNotFoundException( $"Could not find {FileName}" );

            UpdateInfo ssc = null;
            using( StreamReader reader = new StreamReader( FileName ) )
                ssc = new DeserializerBuilder().IgnoreUnmatchedProperties().Build().Deserialize<UpdateInfo>( reader );
            return ssc;
        }
    }
}