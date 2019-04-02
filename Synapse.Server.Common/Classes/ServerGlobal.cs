using System;
using System.Collections.Generic;
using System.Text;
using log4net;

namespace Synapse.Services
{
    public partial class ServerGlobal
    {
        public static ILog Logger = null;
        public static SynapseServerConfig Config = null;
    }
}