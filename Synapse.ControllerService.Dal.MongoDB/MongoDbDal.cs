using System;
using System.Collections.Generic;
using System.Linq;

using MongoDB.Driver;
using MongoDB.Bson.Serialization;

using Synapse.Core;
using Synapse.Core.Utilities;
using Synapse.ControllerService.Common;

namespace Synapse.ControllerService.Dal
{
    public partial class MongoDBDal : IControllerDal
    {
        static readonly string CurrentPath = $"{System.IO.Path.GetDirectoryName( typeof( MongoDBDal ).Assembly.Location )}";

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
}