using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using MongoDB.Driver;

using Synapse.Core;
using Synapse.Core.Utilities;
using Synapse.ControllerService.Common;


namespace Synapse.ControllerService.Dal
{
    public class MongoDBDal : IControllerDal
    {
        static readonly string CurrentPath = $"{System.IO.Path.GetDirectoryName( typeof( MongoDBDal ).Assembly.Location )}";

        //static ConcurrentQueue<QueuedActionItem> ActionItemQueue = new ConcurrentQueue<QueuedActionItem>();

        IMongoDatabase _db = null;
        internal static readonly string _plans = "plans";
        internal static readonly string _paths = "paths";
        internal static readonly string _hist = "history";

        public MongoDBDal()
        {
            _db = new MongoClient().GetDatabase( "synapse" );
            Singleton.Instance.StartQueueWatcher();
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

            Singleton.Instance.ActionItemQueue.Enqueue( item );
        }

        FilterDefinition<Plan> GetPlanInstanceFilter(string planUniqueName, long planInstanceId)
        {
            return Builders<Plan>.Filter.Where( p => p.UniqueName == planUniqueName && p.InstanceId == planInstanceId );
        }
    }


    class QueuedActionItem
    {
        public string PlanUniqueName { get; set; }
        public long PlanInstanceId { get; set; }
        public ActionItem ActionItem { get; set; }
    }
    class ActionPath
    {
        public object _id { get; set; }
        public string Key { get; set; }
        public string Path { get; set; }
    }

    sealed class Singleton
    {
        private static readonly Lazy<Singleton> lazy =
            new Lazy<Singleton>( () => new Singleton() );

        public static Singleton Instance { get { return lazy.Value; } }

        private Singleton()
        {
            ActionItemQueue = new ConcurrentQueue<QueuedActionItem>();
        }

        public ConcurrentQueue<QueuedActionItem> ActionItemQueue { get; }

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
                if( Instance.ActionItemQueue.Count == 0 )
                {
                    Thread.Sleep( 500 ); //no pending actions available, pause
                    continue;
                }

                QueuedActionItem item = null;
                while( Instance.ActionItemQueue.TryDequeue( out item ) )
                    UpdatePlanActionStatusInternal( item.PlanUniqueName, item.PlanInstanceId, item.ActionItem );
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
            catch
            {
                QueuedActionItem item = new QueuedActionItem()
                {
                    PlanUniqueName = planUniqueName,
                    PlanInstanceId = planInstanceId,
                    ActionItem = actionItem
                };
                Instance.ActionItemQueue.Enqueue( item );
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