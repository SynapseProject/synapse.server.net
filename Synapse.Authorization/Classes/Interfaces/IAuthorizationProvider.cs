using System.Collections.Generic;

namespace Synapse.Services
{
    public interface IAuthorizationProvider
    {
        object GetDefaultConfig();
        void Configure(IAuthorizationProviderConfig conifg);

        bool? IsAuthorized(string id);
    }
}