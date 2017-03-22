using System;
using System.Collections.Generic;
using System.Linq;

using MongoDB.Driver;
using MongoDB.Bson.Serialization;

using Synapse.Core;
using Synapse.Core.Utilities;
using Suplex.Security;

namespace Synapse.Services.Controller.Dal
{
    public partial class MongoDBDal : IControllerDal
    {
        static readonly string CurrentPath = $"{System.IO.Path.GetDirectoryName( typeof( MongoDBDal ).Assembly.Location )}";

        IMongoDatabase _db = null;
        internal static readonly string _plans = "synapsePlans";
        internal static readonly string _paths = "synapsePaths";
        internal static readonly string _hist = "synapseHistory";

        //this is a stub feature
        static long PlanInstanceIdCounter = DateTime.Now.Ticks;

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

            ProcessPlansOnSingleton = false;
            ProcessActionsOnSingleton = true;
        }

        public MongoDBDal(bool processPlansOnSingleton = false, bool processActionsOnSingleton = true) : this()
        {
            ProcessPlansOnSingleton = processPlansOnSingleton;
            ProcessActionsOnSingleton = processActionsOnSingleton;
        }


        public bool ProcessPlansOnSingleton { get; set; }
        public bool ProcessActionsOnSingleton { get; set; }


        public bool HasAccess(string securityContext, string planUniqueName, FileSystemRight right = FileSystemRight.Execute)
        {
            throw new NotImplementedException();

            bool ok = false;
            try
            {
                //_splxDal?.TrySecurityOrException( securityContext, planUniqueName, AceType.FileSystem, right, "Plan" );
                ok = true;
            }
            catch { }

            return ok;
        }

        public bool HasAccess(string securityContext, string planUniqueName, AceType aceType, object right)
        {
            throw new NotImplementedException();

            bool ok = false;
            try
            {
                //_splxDal?.TrySecurityOrException( securityContext, planUniqueName, aceType, right, "Plan" );
                ok = true;
            }
            catch { }

            return ok;
        }

        public void HasAccessOrException(string securityContext, string planUniqueName, FileSystemRight right = FileSystemRight.Execute)
        {
            throw new NotImplementedException();

            //_splxDal?.TrySecurityOrException( securityContext, planUniqueName, AceType.FileSystem, right, "Plan" );
        }

        public void HasAccessOrException(string securityContext, string planUniqueName, AceType aceType, object right)
        {
            throw new NotImplementedException();

            //_splxDal?.TrySecurityOrException( securityContext, planUniqueName, aceType, right, "Plan" );
        }


        public IEnumerable<string> GetPlanList()
        {
            return new string[] { "Hello,", "World,", "from", "MongoDBDal!" };
        }

        public IEnumerable<long> GetPlanInstanceIdList(string planUniqueName)
        {
            return new long[] { 1, 2, 3 };
        }

        public Plan GetPlan(string planUniqueName)
        {
            //todo: this is temporary only, switch to mongo impl
            string planFile = $"{CurrentPath}\\Plans\\{planUniqueName}.yaml";
            return YamlHelpers.DeserializeFile<Plan>( planFile );
        }

        public Plan CreatePlanInstance(string planUniqueName)
        {
            //todo: this is temporary only, switch to mongo impl
            string planFile = $"{CurrentPath}\\Plans\\{planUniqueName}.yaml";
            Plan plan = YamlHelpers.DeserializeFile<Plan>( planFile );

            if( string.IsNullOrWhiteSpace( plan.UniqueName ) )
                plan.UniqueName = planUniqueName;
            plan.InstanceId = PlanInstanceIdCounter++;

            return plan;
        }

        public Plan GetPlanStatus(string planUniqueName, long planInstanceId)
        {
            FilterDefinition<Plan> pf = GetPlanInstanceFilter( planUniqueName, planInstanceId );
            List<Plan> plans = _db.GetCollection<Plan>( _hist ).Find( pf ).ToList();
            return plans.Count > 0 ? plans[0] : null;
        }

        public void UpdatePlanStatus(Plan plan)
        {
            PlanUpdateItem item = new PlanUpdateItem() { Plan = plan };

            if( ProcessPlansOnSingleton )
                PlanItemSingletonProcessor.Instance.Queue.Enqueue( item );
            else
                UpdatePlanStatus( item );
        }

        public void UpdatePlanStatus(PlanUpdateItem item)
        {
            try
            {
                FilterDefinition<Plan> pf = GetPlanInstanceFilter( item.Plan.UniqueName, item.Plan.InstanceId );

                item.Plan.LastModified = DateTime.Now.ToString();
                _db.GetCollection<Plan>( _hist ).FindOneAndReplace( pf, item.Plan,
                    new FindOneAndReplaceOptions<Plan, object>() { IsUpsert = true } );
            }
            catch( Exception ex )
            {
                PlanItemSingletonProcessor.Instance.Exceptions.Enqueue( ex );

                if( item.RetryAttempts++ < 5 )
                    PlanItemSingletonProcessor.Instance.Queue.Enqueue( item );
                else
                    PlanItemSingletonProcessor.Instance.Fatal.Enqueue( ex );
            }
        }

        public void UpdatePlanActionStatus(string planUniqueName, long planInstanceId, ActionItem actionItem)
        {
            ActionUpdateItem item = new ActionUpdateItem()
            {
                PlanUniqueName = planUniqueName,
                PlanInstanceId = planInstanceId,
                ActionItem = actionItem
            };

            if( ProcessActionsOnSingleton )
                ActionItemSingletonProcessor.Instance.Queue.Enqueue( item );
            else
                UpdatePlanActionStatus( item );
        }

        public void UpdatePlanActionStatus(ActionUpdateItem item)
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

                    string path = DalUtilities.GetActionInstanceMaterialzedPath( actionInstanceId, plan.Actions );
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
                ActionItemSingletonProcessor.Instance.Exceptions.Enqueue( ex );

                if( item.RetryAttempts++ < 5 )
                    ActionItemSingletonProcessor.Instance.Queue.Enqueue( item );
                else
                    ActionItemSingletonProcessor.Instance.Fatal.Enqueue( ex );
            }
        }

        FilterDefinition<Plan> GetPlanInstanceFilter(string planUniqueName, long planInstanceId)
        {
            return Builders<Plan>.Filter.Where( p => p.UniqueName == planUniqueName && p.InstanceId == planInstanceId );
        }
    }
}