using System;
using Synapse.Common.Utilities;

namespace Synapse.Services
{
    public class ServerAboutData : AboutData
    {
        public new SynapseServerConfig Config { get; set; }
    }
}