using System;

namespace Synapse.Services
{
    [Flags]
    public enum ServerRole
    {
        Controller = 1,
        Node = 2,
        Admin = 4,
        Server = 7,
        Enterprise = 8,
        Universal = 15
    }
}