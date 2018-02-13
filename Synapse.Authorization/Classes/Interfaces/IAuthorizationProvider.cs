using Synapse.Authorization;

namespace Synapse.Services
{
    public interface IAuthorizationProvider
    {
        object GetDefaultConfig();
        void Configure(IAuthorizationProviderConfig conifg);

        AuthorizationType IsAuthorized(string id);
    }
}