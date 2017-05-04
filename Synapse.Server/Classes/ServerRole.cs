using System;

namespace Synapse.Services
{
    [Flags]
    public enum ServerRole
    {
        Controller = 1,
        Node = 2,
        MiddleTier = 3,
        Enterprise = 4,
        Universal = 7
    }
}