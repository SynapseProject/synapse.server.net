using System;
//using System.Collections.Generic;

//using Suplex.Security;

//using Synapse.Core;

namespace Synapse.Services.Controller.Dal
{
    public interface IControllerDal : IControllerDalConfig, IPlanSecurityProvider, IPlanExecuteReader, IPlanHistoryWriter
    {
        //object GetDefaultConfig();
        //Dictionary<string, string> Configure(ISynapseDalConfig conifg);


        //bool HasAccess(string securityContext, string planUniqueName, FileSystemRight right = FileSystemRight.Execute);

        //bool HasAccess(string securityContext, string planUniqueName, AceType aceType, object right);

        //void HasAccessOrException(string securityContext, string planUniqueName, FileSystemRight right = FileSystemRight.Execute);

        //void HasAccessOrException(string securityContext, string planUniqueName, AceType aceType, object right);


        //IEnumerable<string> GetPlanList(string filter = null, bool isRegexFilter = true);

        //IEnumerable<long> GetPlanInstanceIdList(string planUniqueName);

        //Plan GetPlan(string planUniqueName);

        //Plan CreatePlanInstance(string planUniqueName);

        //Plan GetPlanStatus(string planUniqueName, long planInstanceId);

        //void UpdatePlanStatus(Plan plan);

        //void UpdatePlanStatus(PlanUpdateItem item);

        //void UpdatePlanActionStatus(string planUniqueName, long planInstanceId, ActionItem actionItem);

        //void UpdatePlanActionStatus(ActionUpdateItem item);
    }
}