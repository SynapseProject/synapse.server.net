namespace Synapse.Common
{
    public interface IAuthorizationProvider
    {
        bool HasAccess(string id);
    }
}