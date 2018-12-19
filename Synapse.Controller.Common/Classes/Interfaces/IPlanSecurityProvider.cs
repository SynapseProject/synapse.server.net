using System;

using Suplex.Security.AclModel;

namespace Synapse.Services.Controller.Dal
{
    public interface IPlanSecurityProvider : IControllerDalConfig
    {
        bool HasAccess(string securityContext, string planUniqueName, FileSystemRight right = FileSystemRight.Execute);

        void HasAccessOrException(string securityContext, string planUniqueName, FileSystemRight right = FileSystemRight.Execute);
    }
}