using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Synapse.ControllerService.Dal
{
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
            _dal = new MongoDBDal();

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