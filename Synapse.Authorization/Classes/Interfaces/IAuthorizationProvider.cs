using System.Collections.Generic;

namespace Synapse.Services
{
    public interface IAuthorizationProvider
    {
        object GetDefaultConfig();
        Dictionary<string, string> Configure(IAuthorizationProviderConfig conifg);

        bool? IsAuthorized(string id);
    }
}