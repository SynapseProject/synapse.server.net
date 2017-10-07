using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Synapse.Server.AutoUpdater
{
    [Flags]
    public enum ServerRole
    {
        Controller = 1,
        Node = 2,
        Server = 3,
        Enterprise = 4,
        Universal = 7
    }

    public class ServiceConfig
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public ServerRole Role { get; set; }

        internal bool IsRoleController { get { return (Role & ServerRole.Controller) == ServerRole.Controller; } }
        internal bool IsRoleNode { get { return (Role & ServerRole.Node) == ServerRole.Node; } }
        internal bool IsRoleServer { get { return (Role & ServerRole.Server) == ServerRole.Server; } }
    }

    public class SynapseServerConfig
    {
        public ServiceConfig Service { get; set; } = new ServiceConfig();

        public static SynapseServerConfig Deserialize(string fileName)
        {
            if( !File.Exists( fileName ) )
                throw new FileNotFoundException( $"Could not find {fileName}" );

            SynapseServerConfig ssc = null;
            using( StreamReader reader = new StreamReader( fileName ) )
                ssc = new DeserializerBuilder().IgnoreUnmatchedProperties().Build().Deserialize<SynapseServerConfig>( reader );
            return ssc;
        }
    }
}