using System;

namespace Synapse.Services
{
    [Flags]
    public enum ServerRole
    {
        Controller = 1,
        Node = 2,
        Server = 3,
        Enterprise = 4,
        Universal = 7
    }
}