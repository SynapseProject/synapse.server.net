using System;

namespace Synapse.Services.Controller.Dal
{
    public interface IControllerDal : IControllerDalConfig, IPlanSecurityProvider, IPlanExecuteReader, IPlanHistoryWriter
    {
    }
}