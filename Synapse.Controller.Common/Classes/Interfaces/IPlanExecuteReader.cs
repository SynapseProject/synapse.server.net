using System;
using System.Collections.Generic;

using Synapse.Core;

namespace Synapse.Services.Controller.Dal
{
    public interface IPlanExecuteReader : IControllerDalConfig
    {
        IEnumerable<string> GetPlanList(string filter = null, bool isRegexFilter = true);

        Plan GetPlan(string planUniqueName);
    }
}