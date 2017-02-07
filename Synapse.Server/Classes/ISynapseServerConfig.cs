using System.Collections.Generic;
using System.Net;

namespace Synapse.Services
{
    public interface ISynapseServerConfig
    {
        AuthenticationSchemes AuthenticationScheme { get; set; }
        bool ServerIsController { get; }
        ServerRole ServerRole { get; set; }
        string ServiceDisplayName { get; set; }
        string ServiceName { get; set; }
        int WebApiPort { get; set; }

        SynapseServerConfig Configure(SynapseServerConfig value);
        SynapseServerConfig Configure(Dictionary<string, string> values);
        Dictionary<string, string> GetConfigDefaultValues();
        void Serialize();
    }
}