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

        static ConcurrentQueue<QueuedActionItem> ActionItemQueue = new ConcurrentQueue<QueuedActionItem>();

        IMongoDatabase _db = null;
        const string _plans = "plans";
        const string _paths = "paths";
        const string _hist = "history";

        public MongoDBDal()
        {
            _db = new MongoClient().GetDatabase( "synapse" );

            Thread thread = new Thread( () => DrainQueue() );
            thread.IsBackground = true;
            thread.Name = "ActionItemThread";
            thread.Start();
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
            ActionItemQueue.Enqueue( item );
        }

        private void DrainQueue()
        {
            while( true )
            {
                if( ActionItemQueue.Count == 0 )
                {
                    Thread.Sleep( 500 ); //no pending actions available, pause
                    continue;
                }

                QueuedActionItem item = null;
                while( ActionItemQueue.TryDequeue( out item ) )
                    UpdatePlanActionStatusInternal( item.PlanUniqueName, item.PlanInstanceId, item.ActionItem );
            }
        }

        void UpdatePlanActionStatusInternal(string planUniqueName, long planInstanceId, ActionItem actionItem)
        {
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
}