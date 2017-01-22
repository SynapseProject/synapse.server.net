using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Synapse.ControllerService.Dal
{
    public sealed class PlanItemSingletonProcessor
    {
        private static readonly Lazy<PlanItemSingletonProcessor> lazy =
            new Lazy<PlanItemSingletonProcessor>( () => new PlanItemSingletonProcessor() );

        public static PlanItemSingletonProcessor Instance { get { return lazy.Value; } }

        private PlanItemSingletonProcessor()
        {
            Queue = new ConcurrentQueue<PlanUpdateItem>();
            Exceptions = new ConcurrentQueue<Exception>();
            Fatal = new ConcurrentQueue<Exception>();
        }

        public ConcurrentQueue<PlanUpdateItem> Queue { get; }
        public ConcurrentQueue<Exception> Exceptions { get; }
        public ConcurrentQueue<Exception> Fatal { get; }

        IControllerDal _dal = null;

        public void StartQueueWatcher(IControllerDal controllerDalInstance)
        {
            _dal = controllerDalInstance;

            Thread thread = new Thread( () => Instance.DrainQueue() );
            thread.IsBackground = true;
            thread.Name = "PlanItemThread";
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

                PlanUpdateItem item = null;
                while( Instance.Queue.TryDequeue( out item ) )
                    _dal.UpdatePlanStatus( item );
            }
        }
    }
}