using System.Collections.Generic;

namespace Synapse.Services.Controller.Dal
{
    public interface IControllerDalConfig
    {
        object GetDefaultConfig();
        Dictionary<string, string> Configure(ISynapseDalConfig conifg);
    }
}