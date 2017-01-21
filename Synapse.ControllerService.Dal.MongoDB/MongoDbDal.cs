using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using MongoDB.Driver;
using MongoDB.Bson.Serialization;

using Synapse.Core;
using Synapse.Core.Utilities;
using Synapse.ControllerService.Common;
using System.Threading.Tasks;

namespace Synapse.ControllerService.Dal
{
    public class MongoDBDal : IControllerDal
    {
        static readonly string CurrentPath = $"{System.IO.Path.GetDirectoryName( typeof( MongoDBDal ).Assembly.Location )}";

        static void Main(string[] args)
        {
            BsonClassMap.RegisterClassMap<Plan>( x =>
            {
                x.AutoMap();
                x.GetMemberMap( m => m._id ).SetIgnoreIfDefault( true );
            } );

            BsonClassMap.RegisterClassMap<ActionItem>( x =>
            {
                x.AutoMap();
                x.GetMemberMap( m => m._id ).SetIgnoreIfDefault( true );
            } );

            BsonClassMap.RegisterClassMap<ActionPath>( x =>
            {
                x.AutoMap();
                x.GetMemberMap( m => m._id ).SetIgnoreIfDefault( true );
            } );

            MongoDBDal dal = new MongoDBDal();

            long ticks = DateTime.Now.Ticks;
            List<Tuple<Plan, ActionItem>> msgs = new List<Tuple<Plan, ActionItem>>();
            for( int i = 0; i < 10; i++ )
            {
                ActionItem a1 = new ActionItem() { Name = "1", InstanceId = 1, ParentInstanceId = 0, Result = new ExecuteResult() { Status = StatusType.New } };
                ActionItem a2 = new ActionItem() { Name = "2", InstanceId = 2, ParentInstanceId = 1, Result = new ExecuteResult() { Status = StatusType.New } };
                ActionItem a3 = new ActionItem() { Name = "3", InstanceId = 3, ParentInstanceId = 2, Result = new ExecuteResult() { Status = StatusType.New } };
                ActionItem a4 = new ActionItem() { Name = "4", InstanceId = 4, ParentInstanceId = 2, Result = new ExecuteResult() { Status = StatusType.New } };
                ActionItem a5 = new ActionItem() { Name = "5", InstanceId = 5, ParentInstanceId = 4, Result = new ExecuteResult() { Status = StatusType.New } };
                //a1.Actions.Add( a2 ); //a2.Actions.Add( a3 ); //a2.Actions.Add( a4 ); //a4.Actions.Add( a5 );

                Plan plan = new Plan()
                {
                    Name = $"Plan_{ticks}",
                    UniqueName = $"Plan_{ticks}",
                    InstanceId = ticks++
                };
                plan.Actions.Add( a1 );

                Tuple<Plan, ActionItem> t1 = new Tuple<Plan, ActionItem>( plan, a1 );
                Tuple<Plan, ActionItem> t2 = new Tuple<Plan, ActionItem>( plan, a2 );
                Tuple<Plan, ActionItem> t3 = new Tuple<Plan, ActionItem>( plan, a3 );
                Tuple<Plan, ActionItem> t4 = new Tuple<Plan, ActionItem>( plan, a4 );
                Tuple<Plan, ActionItem> t5 = new Tuple<Plan, ActionItem>( plan, a5 );

                msgs.Add( t1 );
                msgs.Add( t2 );
                msgs.Add( t3 );
                msgs.Add( t4 );
                msgs.Add( t5 );
            }

            Parallel.ForEach( msgs, m => {
                dal.UpdatePlanStatus( m.Item1 );
            } );

            ActionItemSingletonProcessor.Instance.StartQueueWatcher();

            dal.UpdatePlanActionStatus( msgs[0].Item1.UniqueName, msgs[0].Item1.InstanceId, msgs[0].Item2 );
            dal.UpdatePlanActionStatus( msgs[1].Item1.UniqueName, msgs[1].Item1.InstanceId, msgs[1].Item2 );
            dal.UpdatePlanActionStatus( msgs[2].Item1.UniqueName, msgs[2].Item1.InstanceId, msgs[2].Item2 );
            dal.UpdatePlanActionStatus( msgs[3].Item1.UniqueName, msgs[3].Item1.InstanceId, msgs[3].Item2 );
            dal.UpdatePlanActionStatus( msgs[4].Item1.UniqueName, msgs[4].Item1.InstanceId, msgs[4].Item2 );

            //Parallel.ForEach( msgs, m => {
            //    dal.UpdatePlanActionStatus( m.Item1.UniqueName, m.Item1.InstanceId, m.Item2 );
            //} );

            Thread.Sleep( Timeout.Infinite );
        }


        IMongoDatabase _db = null;
        internal static readonly string _plans = "plans";
        internal static readonly string _paths = "paths";
        internal static readonly string _hist = "history";

        public MongoDBDal()
        {
            _db = new MongoClient().GetDatabase( "synapse" );
        }

        public Plan GetPlan(string planUniqueName)
        {
            //todo: this is temporary only, switch to mongo impl
            string planFile = $"{CurrentPath}\\Plans\\{planUniqueName}.yaml";
            return YamlHelpers.DeserializeFile<Plan>( planFile );
        }

        public Plan GetPlanStatus(string planUniqueName, long planInstanceId)
        {
            FilterDefinition<Plan> pf = GetPlanInstanceFilter( planUniqueName, planInstanceId );
            List<Plan> plans = _db.GetCollection<Plan>( _hist ).Find( pf ).ToList();
            return plans.Count > 0 ? plans[0] : null;
        }

        public void UpdatePlanStatus(Plan plan)
        {
            //_db.GetCollection<Plan>( _hist ).InsertOne( plan );

            FilterDefinition<Plan> pf = GetPlanInstanceFilter( plan.UniqueName, plan.InstanceId );
            _db.GetCollection<Plan>( _hist ).FindOneAndUpdate( pf,
                Builders<Plan>.Update.CurrentDate( "LastModified" ),
                new FindOneAndUpdateOptions<Plan, object>() { IsUpsert = true } );
        }

        public void UpdatePlanActionStatus(string planUniqueName, long planInstanceId, ActionItem actionItem)
        {
            QueuedActionItem item = new QueuedActionItem()
            {
                PlanUniqueName = planUniqueName,
                PlanInstanceId = planInstanceId,
                ActionItem = actionItem
            };

            ActionItemSingletonProcessor.Instance.Queue.Enqueue( item );
        }

        FilterDefinition<Plan> GetPlanInstanceFilter(string planUniqueName, long planInstanceId)
        {
            return Builders<Plan>.Filter.Where( p => p.UniqueName == planUniqueName && p.InstanceId == planInstanceId );
        }
    }


    class ActionPath
    {
        public object _id { get; set; }
        public string Key { get; set; }
        public string Path { get; set; }
    }

    public class QueuedActionItem
    {
        public string PlanUniqueName { get; set; }
        public long PlanInstanceId { get; set; }
        public ActionItem ActionItem { get; set; }
    }

    public sealed class ActionItemSingletonProcessor
    {
        private static readonly Lazy<ActionItemSingletonProcessor> lazy =
            new Lazy<ActionItemSingletonProcessor>( () => new ActionItemSingletonProcessor() );

        public static ActionItemSingletonProcessor Instance { get { return lazy.Value; } }

        private ActionItemSingletonProcessor()
        {
            Queue = new ConcurrentQueue<QueuedActionItem>();
        }

        public ConcurrentQueue<QueuedActionItem> Queue { get; }

        public void StartQueueWatcher()
        {
            Thread thread = new Thread( () => Instance.DrainQueue() );
            thread.IsBackground = true;
            thread.Name = "ActionItemThread";
            thread.Start();
        }


        void DrainQueue()
        {
            while( true )
            {
                if( Instance.Queue.Count == 0 )
                {
                    Thread.Sleep( 500 ); //no pending actions available, pause
                    continue;
                }

                QueuedActionItem item = null;
                while( Instance.Queue.TryDequeue( out item ) )
                {
                    UpdatePlanActionStatusInternal( item.PlanUniqueName, item.PlanInstanceId, item.ActionItem );
                    item = null;
                }
            }
        }

        void UpdatePlanActionStatusInternal(string planUniqueName, long planInstanceId, ActionItem actionItem)
        {
            IMongoDatabase db = new MongoClient().GetDatabase( "synapse" );
            IMongoCollection<Plan> hist = db.GetCollection<Plan>( MongoDBDal._hist );
            IMongoCollection<ActionPath> paths = db.GetCollection<ActionPath>( MongoDBDal._paths );

            long actionInstanceId = actionItem.InstanceId;
            long actionParentInstanceId = actionItem.ParentInstanceId;
            string key = $"{planInstanceId}_{actionInstanceId}";

            FilterDefinition<ActionPath> pathFilter =
                Builders<ActionPath>.Filter.Where( p => p.Key == key );
            List<ActionPath> pathList = paths.Find( pathFilter ).ToList();
            string nodePath = pathList.Count > 0 ? pathList[0].Path : null;

            try
            {
                if( nodePath == null )
                {
                    //actionParentInstanceId == 0
                    string updatePath = "Actions";

                    if( actionParentInstanceId != 0 )
                    {
                        FilterDefinition<ActionPath> parFilter =
                            Builders<ActionPath>.Filter.Where( p => p.Key == $"{planInstanceId}_{actionParentInstanceId}" );
                        List<ActionPath> parList = paths.Find( parFilter ).ToList();

                        updatePath = parList[0].Path + ".Actions";
                    }

                    FilterDefinition<Plan> pf = Builders<Plan>.Filter.Where( p => p.InstanceId == planInstanceId );
                    hist.FindOneAndUpdate( pf,
                        Builders<Plan>.Update.Push( updatePath, actionItem ),
                        new FindOneAndUpdateOptions<Plan, object>() { IsUpsert = true } );

                    Plan thisPlan = hist.Find( pf ).ToList()[0];

                    string path = GetMaterialzedPath( actionInstanceId, thisPlan.Actions, "" );
                    paths.InsertOne( new ActionPath() { Key = key, Path = path } );
                }
                else
                {
                    hist.FindOneAndUpdate( Builders<Plan>.Filter.And(
                        Builders<Plan>.Filter.Where( p => p.InstanceId == planInstanceId ),
                        Builders<Plan>.Filter.Eq( $"{nodePath}.InstanceId", actionInstanceId ),
                        Builders<Plan>.Filter.Lt( $"{nodePath}.Result.Status", actionItem.Result.Status ) ),
                        //end of filter above, set starts here:
                        Builders<Plan>.Update.Set( $"{nodePath}.Result", actionItem.Result ),
                        new FindOneAndUpdateOptions<Plan, object>() { IsUpsert = false } );
                }
            }
            catch(Exception ex)
            {
                QueuedActionItem item = new QueuedActionItem()
                {
                    PlanUniqueName = planUniqueName,
                    PlanInstanceId = planInstanceId,
                    ActionItem = actionItem
                };
                Exception e = ex;
                //Instance.Queue.Enqueue( item );
            }
        }

        string GetMaterialzedPath(long id, List<ActionItem> actions, string path)
        {
            int i = 0;
            foreach( ActionItem a in actions )
            {
                if( a.InstanceId == id )
                {
                    path = $"{path}.Actions.{i}";
                }
                else
                {
                    if( a.HasActionGroup )
                    {
                        if( a.ActionGroup.InstanceId == id )
                            path += "ActionGroup";
                        else if( a.ActionGroup.HasActions )
                            path = GetMaterialzedPath( id, a.ActionGroup.Actions, path );
                    }

                    if( a.HasActions )
                        path = GetMaterialzedPath( id, a.Actions, $"{path}.Actions.{i}" );
                }

                i++;
            }

            return path.TrimStart( '.' );
        }
    }
}