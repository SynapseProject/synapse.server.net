using System;

using Synapse.Core;

namespace Synapse.ControllerService.Dal
{
    public class PlanUpdateItem
    {
        public Plan Plan { get; set; }
        public int RetryAttempts { get; set; }
    }
}