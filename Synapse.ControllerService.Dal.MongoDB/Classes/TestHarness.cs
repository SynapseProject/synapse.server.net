using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Synapse.Core;
using Synapse.ControllerService.Common;

namespace Synapse.ControllerService.Dal
{
    public partial class MongoDBDal : IControllerDal
    {
        static void Main(string[] args)
        {
            MongoDBDal dal = new MongoDBDal();

            long ticks = DateTime.Now.Ticks;
            List<Tuple<Plan, ActionItem>> msgs = new List<Tuple<Plan, ActionItem>>();
            for( int i = 0; i < 100; i++ )
            {
                ActionItem a1 = new ActionItem() { Name = "01", InstanceId = 1, ParentInstanceId = 0, Result = new ExecuteResult() { Status = StatusType.New } };
                ActionItem a2 = new ActionItem() { Name = "02", InstanceId = 2, ParentInstanceId = 1, Result = new ExecuteResult() { Status = StatusType.New } };
                ActionItem a3 = new ActionItem() { Name = "03", InstanceId = 3, ParentInstanceId = 2, Result = new ExecuteResult() { Status = StatusType.New } };
                ActionItem a4 = new ActionItem() { Name = "04", InstanceId = 4, ParentInstanceId = 2, Result = new ExecuteResult() { Status = StatusType.New } };
                ActionItem a5 = new ActionItem() { Name = "05", InstanceId = 5, ParentInstanceId = 4, Result = new ExecuteResult() { Status = StatusType.New } };
                //a1.Actions.Add( a2 );
                //a2.Actions.Add( a3 );
                //a2.Actions.Add( a4 );
                //a4.Actions.Add( a5 );

                Plan plan = new Plan()
                {
                    Name = $"Plan_{ticks}",
                    UniqueName = $"Plan_{ticks}",
                    InstanceId = ticks++
                };
                //plan.Actions.Add( a1 );

                Tuple<Plan, ActionItem> t01 = new Tuple<Plan, ActionItem>( plan, a1 );
                Tuple<Plan, ActionItem> t02 = new Tuple<Plan, ActionItem>( plan, a2 );
                Tuple<Plan, ActionItem> t03 = new Tuple<Plan, ActionItem>( plan, a3 );
                Tuple<Plan, ActionItem> t04 = new Tuple<Plan, ActionItem>( plan, a4 );
                Tuple<Plan, ActionItem> t05 = new Tuple<Plan, ActionItem>( plan, a5 );
                Tuple<Plan, ActionItem> t06 = new Tuple<Plan, ActionItem>( plan, new ActionItem() { Name = "01", InstanceId = 1, ParentInstanceId = 0, Result = new ExecuteResult() { Status = StatusType.Complete } } );
                Tuple<Plan, ActionItem> t07 = new Tuple<Plan, ActionItem>( plan, new ActionItem() { Name = "02", InstanceId = 2, ParentInstanceId = 1, Result = new ExecuteResult() { Status = StatusType.Complete } } );
                Tuple<Plan, ActionItem> t08 = new Tuple<Plan, ActionItem>( plan, new ActionItem() { Name = "03", InstanceId = 3, ParentInstanceId = 2, Result = new ExecuteResult() { Status = StatusType.Complete } } );
                Tuple<Plan, ActionItem> t09 = new Tuple<Plan, ActionItem>( plan, new ActionItem() { Name = "04", InstanceId = 4, ParentInstanceId = 2, Result = new ExecuteResult() { Status = StatusType.Complete } } );
                Tuple<Plan, ActionItem> t10 = new Tuple<Plan, ActionItem>( plan, new ActionItem() { Name = "05", InstanceId = 5, ParentInstanceId = 4, Result = new ExecuteResult() { Status = StatusType.Complete } } );

                msgs.Add( t10 );
                msgs.Add( t09 );
                msgs.Add( t08 );
                msgs.Add( t07 );
                msgs.Add( t06 );
                msgs.Add( t05 );
                msgs.Add( t04 );
                msgs.Add( t03 );
                msgs.Add( t02 );
                msgs.Add( t01 );
            }

            Parallel.ForEach( msgs, m => {
                dal.UpdatePlanStatus( m.Item1 );
            } );

            ActionItemSingletonProcessor.Instance.StartQueueWatcher();

            //foreach( Tuple<Plan, ActionItem> m in msgs )
            //    dal.UpdatePlanActionStatus( m.Item1.UniqueName, m.Item1.InstanceId, m.Item2 );

            Parallel.ForEach( msgs, m => {
                dal.UpdatePlanActionStatus( m.Item1.UniqueName, m.Item1.InstanceId, m.Item2 );
            } );


            while( !ActionItemSingletonProcessor.Instance.ReadyToExit )
                Thread.Sleep( 500 );

            if( ActionItemSingletonProcessor.Instance.Fatal.Count > 0 )
                Console.WriteLine( $"Fatal errors: {ActionItemSingletonProcessor.Instance.Fatal.Count}" );

            Plan p = dal.GetPlanStatus( msgs[0].Item1.UniqueName, msgs[0].Item1.InstanceId );
            p.Actions[0].Result.Status = StatusType.Failed;
            p.Actions[0].Result.BranchStatus = StatusType.Cancelled;
            dal.UpdatePlanStatus( p );

            p = dal.GetPlanStatus( msgs[0].Item1.UniqueName, msgs[0].Item1.InstanceId );

            Environment.Exit( 0 );
        }
    }
}