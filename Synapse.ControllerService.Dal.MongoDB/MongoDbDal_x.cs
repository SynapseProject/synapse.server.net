using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Synapse.Core;
using Synapse.Core.Utilities;

namespace mongoTest
{
    class Program
    {
        static void Main(string[] args)
        {
            UpdateActions();
        }

        static void UpdateActions()
        {
            MongoClient client = new MongoClient();
            IMongoDatabase synapseDb = client.GetDatabase( "synapse" );

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


            IMongoCollection<Plan> plans = synapseDb.GetCollection<Plan>( "plans" );
            IMongoCollection<ActionPath> paths = synapseDb.GetCollection<ActionPath>( "paths" );

            long ticks = DateTime.Now.Ticks;
            Plan plan = new Plan()
            {
                Name = $"Plan_{ticks}",
                InstanceId = ticks
            };
            plans.InsertOne( plan );


            loop( ticks, 1, 0, new ExecuteResult() { Status = StatusType.New } );
            loop( ticks, 2, 1, new ExecuteResult() { Status = StatusType.Running } );
            loop( ticks, 2, 1, new ExecuteResult() { Status = StatusType.New } );
            loop( ticks, 3, 2, new ExecuteResult() { Status = StatusType.New } );
            loop( ticks, 4, 3, new ExecuteResult() { Status = StatusType.New } );
            loop( ticks, 3, 2, new ExecuteResult() { Status = StatusType.Running } );
            loop( ticks, 4, 3, new ExecuteResult() { Status = StatusType.Running } );
        }

        static void loop(long planInstanceId, long actionInstanceId, long actionParentInstanceId, ExecuteResult result)
        {
            MongoClient client = new MongoClient();
            IMongoDatabase synapseDb = client.GetDatabase( "synapse" );
            IMongoCollection<Plan> plans = synapseDb.GetCollection<Plan>( "plans" );
            IMongoCollection<ActionPath> paths = synapseDb.GetCollection<ActionPath>( "paths" );

            string key = $"{planInstanceId}_{actionInstanceId}";

            FilterDefinition<ActionPath> pathFilter =
                Builders<ActionPath>.Filter.Where( p => p.Key == key );
            List<ActionPath> pathList = paths.Find( pathFilter ).ToList();
            string nodePath = pathList.Count > 0 ? pathList[0].Path : null;

            if( nodePath == null )
            {
                ActionItem action = new ActionItem()
                {
                    InstanceId = actionInstanceId,
                    Result = result
                };

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
                plans.FindOneAndUpdate( pf,
                    Builders<Plan>.Update.Push( updatePath, action ),
                    new FindOneAndUpdateOptions<Plan, object>() { IsUpsert = true } );

                Plan thisPlan = plans.Find( pf ).ToList()[0];

                string path = GetMaterialzedPath( actionInstanceId, thisPlan.Actions, "" );
                paths.InsertOne( new ActionPath() { Key = key, Path = path } );

                //UpdateDefinition<Plan> upd =
                //    Builders<Plan>.Update.Set( $"{updatePath}.$.Result", result );
                //plans.FindOneAndUpdate( pf, upd, new FindOneAndUpdateOptions<Plan, object>() { IsUpsert = true } );
            }
            else
            {
                plans.FindOneAndUpdate( Builders<Plan>.Filter.And(
                    Builders<Plan>.Filter.Where( p => p.InstanceId == planInstanceId ),
                    Builders<Plan>.Filter.Eq( $"{nodePath}.InstanceId", actionInstanceId ),
                    Builders<Plan>.Filter.Lt( $"{nodePath}.Result.Status", result.Status ) ),
                    Builders<Plan>.Update.Set( $"{nodePath}.Result", result ),
                    new FindOneAndUpdateOptions<Plan, object>() { IsUpsert = false } );
            }
        }

        static string GetMaterialzedPath(long id, List<ActionItem> actions, string path)
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

        static void UpdateActions_1()
        {
            MongoClient client = new MongoClient();
            IMongoDatabase synapseDb = client.GetDatabase( "synapse" );

            //Plan plan =
            //    YamlHelpers.DeserializeFile<Plan>( @"executeCase.result.yaml" );


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


            IMongoCollection<BsonDocument> plans = synapseDb.GetCollection<BsonDocument>( "plans" );
            IMongoCollection<BsonDocument> paths = synapseDb.GetCollection<BsonDocument>( "paths" );

            long ticks = DateTime.Now.Ticks;
            Plan p = new Plan()
            {
                Name = $"Plan_{ticks}",
                Description = "foo",
                InstanceId = ticks,
                Actions = new List<ActionItem>()
            };
            p.Actions.Add( new ActionItem() { Name = "action_00" } );

            BsonDocument filter = new BsonDocument( "InstanceId", p.InstanceId );
            List<BsonDocument> list = plans.Find( filter ).ToList();
            if( list.Count == 0 )
            {
                UpdateResult planResult = plans.UpdateOne( p.ToBsonDocument( typeof( Plan ) ),
                    Builders<BsonDocument>.Update.Set( "Description", "foo" ),
                    new UpdateOptions() { IsUpsert = true } );
                p._id = planResult.UpsertedId;
            }
            else
            {
                BsonDocument x = list[0];
            }

            IMongoCollection<Plan> strongPlans = synapseDb.GetCollection<Plan>( "plans" );

            ActionItem action = new ActionItem()
            {
                Name = "action_01",
                Result = new ExecuteResult() { Status = StatusType.Running }
            };

            //FilterDefinition<Plan> af = Builders<Plan>.Filter.And(
            //    Builders<Plan>.Filter.Where( x => x._id == p._id ),
            //    Builders<Plan>.Filter.Eq( "Actions.Name", action.Name ) );
            FilterDefinition<Plan> af = Builders<Plan>.Filter.And(
                Builders<Plan>.Filter.Where( x => x.Name == p.Name ),
                Builders<Plan>.Filter.Eq( "Actions.Name", action.Name ) );
            List<Plan> f = strongPlans.Find( af ).ToList();
            if( f.Count == 0 )
            {
                dynamic foo =
                    strongPlans.FindOneAndUpdate( Builders<Plan>.Filter.Where( x => x.Name == p.Name ),
                    Builders<Plan>.Update.Push( "Actions", action ),
                    new FindOneAndUpdateOptions<Plan, object>() { IsUpsert = true } );

                List<Plan> ppp = strongPlans.Find( Builders<Plan>.Filter.Eq( "Name", action.Name ) ).ToList();
                //recurse down and find the thing


                List<object> ai = foo.Actions;
            }

            UpdateDefinition<Plan> upd =
                Builders<Plan>.Update.Set( "Actions.$.Result", new ExecuteResult() { Status = StatusType.Complete } );
            strongPlans.FindOneAndUpdate( af, upd, new FindOneAndUpdateOptions<Plan, object>() { IsUpsert = true } );



            list = plans.Find( filter ).ToList();
            BsonDocument found = list[0];

            UpdateResult result = plans.UpdateOne( found,
                Builders<BsonDocument>.Update.CurrentDate( "lastModified" ),
                new UpdateOptions() { IsUpsert = true } );
            p._id = result.UpsertedId;
        }

        static void UpdateActions_2()
        {
            MongoClient client = new MongoClient();
            IMongoDatabase synapseDb = client.GetDatabase( "synapse" );

            //Plan plan =
            //    YamlHelpers.DeserializeFile<Plan>( @"executeCase.result.yaml" );


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


            IMongoCollection<BsonDocument> plans = synapseDb.GetCollection<BsonDocument>( "plans" );
            IMongoCollection<BsonDocument> paths = synapseDb.GetCollection<BsonDocument>( "paths" );

            long ticks = DateTime.Now.Ticks;
            Plan p = new Plan()
            {
                Name = $"Plan_{ticks}",
                Description = "foo",
                InstanceId = ticks,
                Actions = new List<ActionItem>()
            };
            p.Actions.Add( new ActionItem() { Name = "action_00" } );

            BsonDocument filter = new BsonDocument( "InstanceId", p.InstanceId );
            List<BsonDocument> list = plans.Find( filter ).ToList();
            if( list.Count == 0 )
            {
                UpdateResult planResult = plans.UpdateOne( p.ToBsonDocument( typeof( Plan ) ),
                    Builders<BsonDocument>.Update.Set( "Description", "foo" ),
                    new UpdateOptions() { IsUpsert = true } );
                p._id = planResult.UpsertedId;
            }
            else
            {
                BsonDocument x = list[0];
            }

            IMongoCollection<Plan> strongPlans = synapseDb.GetCollection<Plan>( "plans" );

            ActionItem action = new ActionItem()
            {
                Name = "action_0",
                Result = new ExecuteResult() { Status = StatusType.Running }
            };

            //FilterDefinition<Plan> af = Builders<Plan>.Filter.And(
            //    Builders<Plan>.Filter.Where( x => x._id == p._id ),
            //    Builders<Plan>.Filter.Eq( "Actions.Name", action.Name ) );
            FilterDefinition<Plan> af = Builders<Plan>.Filter.And(
                Builders<Plan>.Filter.Where( x => x.Name == p.Name ),
                Builders<Plan>.Filter.Eq( "Actions.Name", action.Name ) );
            List<Plan> f = strongPlans.Find( af ).ToList();
            if( f.Count == 0 )
            {
                UpdateDefinition<Plan> upd =
                    Builders<Plan>.Update.Push( "Actions", action );
                object foo =
                    strongPlans.FindOneAndUpdate( af, upd, new FindOneAndUpdateOptions<Plan, object>() { IsUpsert = true } );
            }
            else
            {
                UpdateDefinition<Plan> upd =
                    Builders<Plan>.Update.Set( "Actions.$.Result", new ExecuteResult() { Status = StatusType.Complete } );
                strongPlans.FindOneAndUpdate( af, upd, new FindOneAndUpdateOptions<Plan, object>() { IsUpsert = true } );
            }



            list = plans.Find( filter ).ToList();
            BsonDocument found = list[0];

            UpdateResult result = plans.UpdateOne( found,
                Builders<BsonDocument>.Update.CurrentDate( "lastModified" ),
                new UpdateOptions() { IsUpsert = true } );
            p._id = result.UpsertedId;
        }

        static void Test()
        {
            MongoClient client = new MongoClient();
            IMongoDatabase synapseDb = client.GetDatabase( "synapse" );
            IMongoCollection<Plan> plans = synapseDb.GetCollection<Plan>( "plans" );

            //Plan plan =
            //    YamlHelpers.DeserializeFile<Plan>( @"executeCase.result.yaml" );
            //plan.ParentPath = plan.InstanceId.ToString();
            //UpdateMaterialzedPaths( ","+plan.ParentPath, plan.Actions );
            //plans.InsertOne( plan );

            //// strongly-typed, not yet working
            //FilterDefinitionBuilder<Plan> builder = Builders<Plan>.Filter;
            //FilterDefinition<Plan> pf = builder.Eq( plan => plan.Name, "executeCase" );
            //IFindFluent<Plan, Plan> found = plans.Find( pf );
            //List<Plan> f = found.ToList();
            //f[0].Actions[0].Description = $"changed: {DateTime.Now}";
            //UpdateDefinitionBuilder<Plan> u = Builders<Plan>.Update;
            //plans.UpdateOne( pf, u.Set() );

            // Bson approach, almost works, but I don't know how to traverse to child yet (might need recursive search)
            IMongoCollection<BsonDocument> collection = synapseDb.GetCollection<BsonDocument>( "plans" );
            BsonDocument filter = new BsonDocument( "Actions.Actions.ParentPath", ",9,ac0" );
            List<BsonDocument> list = collection.Find( filter ).ToList();
            //finds the right node, but I don't know how to update it yet
            BsonDocument p = list[0];
            UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update
                .Set( "Description", $"changed: {DateTime.Now}" )
                .CurrentDate( "lastModified" );
            collection.UpdateOne( filter, update );


            int count = 0;
            IAsyncCursor<BsonDocument> cursor = collection.Find( filter ).ToCursor();
            while( cursor.MoveNext() )
            {
                var batch = cursor.Current;
                foreach( var document in batch )
                {
                    // process document
                    count++;
                    Console.WriteLine( count );
                }
            }
        }

        static void UpdateMaterialzedPaths(string parentPath, List<ActionItem> actions)
        {
            foreach( ActionItem a in actions )
            {
                a.ParentPath = parentPath;
                if( a.HasActionGroup )
                {
                    a.ActionGroup.ParentPath = parentPath;
                    if( a.ActionGroup.HasActions )
                        UpdateMaterialzedPaths( $"{a.ActionGroup.ParentPath},{a.ActionGroup.Name}", a.ActionGroup.Actions );
                }
                if( a.HasActions )
                    UpdateMaterialzedPaths( $"{a.ParentPath},{a.Name}", a.Actions );
            }
        }
    }

    public class ActionPath
    {
        public object _id { get; set; }
        public string Key { get; set; }
        public string Path { get; set; }
    }
}