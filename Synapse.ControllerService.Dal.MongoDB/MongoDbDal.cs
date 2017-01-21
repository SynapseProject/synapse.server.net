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
                plan.Actions.Add( a1 );

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
            Environment.Exit( 0 );
        }


        IMongoDatabase _db = null;
        internal static readonly string _plans = "plans";
        internal static readonly string _paths = "paths";
        internal static readonly string _hist = "history";

        public MongoDBDal()
        {
            _db = new MongoClient().GetDatabase( "synapse" );

            if( !BsonClassMap.IsClassMapRegistered( typeof( Plan ) ) )
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
            }
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
            ActionUpdateItem item = new ActionUpdateItem()
            {
                PlanUniqueName = planUniqueName,
                PlanInstanceId = planInstanceId,
                ActionItem = actionItem
            };

            //UpdatePlanActionStatusInternal( item );
            ActionItemSingletonProcessor.Instance.Queue.Enqueue( item );
        }

        internal void UpdatePlanActionStatusInternal(ActionUpdateItem item)
        {
            IMongoCollection<Plan> hist = _db.GetCollection<Plan>( _hist );
            IMongoCollection<ActionPath> paths = _db.GetCollection<ActionPath>( _paths );

            long planInstanceId = item.PlanInstanceId;
            ActionItem actionItem = item.ActionItem;
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
                    //default: actionParentInstanceId == 0
                    string updatePath = "Actions";

                    if( actionParentInstanceId != 0 )
                    {
                        FilterDefinition<ActionPath> parFilter =
                            Builders<ActionPath>.Filter.Where( p => p.Key == $"{planInstanceId}_{actionParentInstanceId}" );
                        List<ActionPath> parList = paths.Find( parFilter ).ToList();

                        if( parList.Count > 0 )
                            updatePath = parList[0].Path + ".Actions";
                        else
                            throw new Exception( $"Could not find Plan.InstanceId = [{planInstanceId}], Action:{actionItem.Name}.ParentInstanceId = [{actionItem.ParentInstanceId}] in paths table." );
                    }

                    FilterDefinition<Plan> pf = Builders<Plan>.Filter.Where( p => p.InstanceId == planInstanceId );
                    hist.FindOneAndUpdate( pf,
                        Builders<Plan>.Update.Push( updatePath, actionItem ),
                        new FindOneAndUpdateOptions<Plan, object>() { IsUpsert = true } );

                    Plan plan = null;
                    List<Plan> plans = hist.Find( pf ).ToList();
                    if( plans.Count > 0 )
                        plan = plans[0];
                    else
                        throw new Exception( $"Could not find Plan.InstanceId = [{planInstanceId}] in history table." );

                    string path = GetMaterialzedPath( actionInstanceId, plan.Actions, "" );
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
            catch( Exception ex )
            {
                Exception e = ex;
                ActionItemSingletonProcessor.Instance.Exceptions.Enqueue( ex );

                item.RetryAttempts++;
                if( item.RetryAttempts < 5 )
                    ActionItemSingletonProcessor.Instance.Queue.Enqueue( item );
                else
                    ActionItemSingletonProcessor.Instance.Fatal.Enqueue( ex );
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

    public class ActionUpdateItem
    {
        public string PlanUniqueName { get; set; }
        public long PlanInstanceId { get; set; }
        public ActionItem ActionItem { get; set; }
        public int RetryAttempts { get; set; }
    }

    public sealed class ActionItemSingletonProcessor
    {
        private static readonly Lazy<ActionItemSingletonProcessor> lazy =
            new Lazy<ActionItemSingletonProcessor>( () => new ActionItemSingletonProcessor() );

        public static ActionItemSingletonProcessor Instance { get { return lazy.Value; } }

        private ActionItemSingletonProcessor()
        {
            Queue = new ConcurrentQueue<ActionUpdateItem>();
            Exceptions = new ConcurrentQueue<Exception>();
            Fatal = new ConcurrentQueue<Exception>();
        }

        public ConcurrentQueue<ActionUpdateItem> Queue { get; }
        public ConcurrentQueue<Exception> Exceptions { get; }
        public ConcurrentQueue<Exception> Fatal { get; }

        MongoDBDal _dal = null;

        public void StartQueueWatcher()
        {
            _dal = new Dal.MongoDBDal();

            Thread thread = new Thread( () => Instance.DrainQueue() );
            thread.IsBackground = true;
            thread.Name = "ActionItemThread";
            thread.Start();
        }

        bool _allowExit = false;
        public bool ReadyToExit = false;
        void DrainQueue()
        {
            while( true )
            {
                if( Instance.Queue.Count == 0 )
                {
                    Thread.Sleep( 500 ); //no pending actions available, pause
                    if( _allowExit )
                        ReadyToExit = true;
                    continue;
                }
                _allowExit = true;

                ActionUpdateItem item = null;
                while( Instance.Queue.TryDequeue( out item ) )
                    _dal.UpdatePlanActionStatusInternal( item );
            }
        }
    }
}