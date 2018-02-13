using System;

namespace Synapse.Authorization
{
    [Flags]
    public enum AuthorizationType
    {
        None = 0,
        GeneralAllow = 1,
        ExplicitAllow = 2 | GeneralAllow,
        ImplicitAllow = 4 | GeneralAllow,
        GeneralDeny = 8,
        ImplicitDeny = 16 | GeneralDeny,
        ExplicitDeny = 32 | GeneralDeny
    }
}