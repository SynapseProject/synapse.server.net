using System;
using System.Threading;
using System.Threading.Tasks;

using Synapse.Core;
using Synapse.Core.Utilities;

namespace Synapse.Services
{
    public class SyncExecuteHelper
    {
        public static object WaitForTerminalStatusOrTimeout(IExecuteController ec, string planName, long id,
            string path = "Actions[0]:Result:ExitData", SerializationType serializationType = SerializationType.Json,
            int pollingIntervalSeconds = 1, int timeoutSeconds = 120)
        {
            StatusType status = Task.Run( () => GetStatus( ec, planName, id, pollingIntervalSeconds, timeoutSeconds ) ).Result;
            if( status == StatusType.Success )
                return ec.GetPlanElements( planName, id, path, serializationType, setContentType: false );
            else
                return status;
        }
        public static StatusType GetStatus(IExecuteController ec, string planName, long id, int pollingIntervalSeconds = 1, int timeoutSeconds = 120)
        {
            //ensure valid values
            pollingIntervalSeconds = pollingIntervalSeconds < 1 ? 1 : pollingIntervalSeconds;
            timeoutSeconds = timeoutSeconds < 1 ? 120 : timeoutSeconds;

            int c = 0;
            StatusType status = StatusType.New;
            while( c < timeoutSeconds )
            {
                Thread.Sleep( pollingIntervalSeconds * 1000 );
                try { Enum.TryParse( ec.GetPlanElements( planName, id, "Result:Status", setContentType: false ).ToString(), out status ); } catch { }
                c = status < StatusType.Success ? c + 1 : int.MaxValue;
            }
            return status;
        }


        //[Obsolete( "not in use" )]
        //public static T Execute<T>(IExecuteController ec, string planName, StartPlanEnvelope pe, string path = "Actions[0]:Result:ExitData")
        //{
        //    long id = ec.StartPlan( pe, planName );
        //    StatusType status = Task.Run( () => GetStatus( ec, planName, id ) ).Result;
        //    if( status == StatusType.Success )
        //        return ec.GetPlanElements( planName, id, path, setContentType: false );
        //    else
        //        return default( T );
        //}
        //[Obsolete( "not in use" )]
        //public static Task<StatusType> Execute(IExecuteController ec, string planName, StartPlanEnvelope pe, out long id)
        //{
        //    long pid = id = ec.StartPlan( pe, planName );
        //    return Task.Run( () => GetStatus( ec, planName, pid ) );
        //}
        //[Obsolete( "not in use" )]
        //public static Task<StatusType> GetStatusAsync(IExecuteController ec, string planName, long id)
        //{
        //    return Task.Run( () => GetStatus( ec, planName, id ) );
        //}
    }
}