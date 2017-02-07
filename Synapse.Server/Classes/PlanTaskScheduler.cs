using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Synapse.Core.Runtime
{
    public class PlanScheduler : IDisposable
    {
        public event EventHandler<PlanCompletedEventArgs> PlanCompleted;
        protected void OnPlanCompleted(IPlanRuntimeContainer planContainer)
        {
            PlanCompleted?.Invoke( this, new PlanCompletedEventArgs( planContainer ) );
        }

        // default TaskFactory for starting new Tasks
        TaskFactory _tf = null;

        // list of current tasks
        List<Task> _tasks = new List<Task>();
        Dictionary<long, InProcPlanInfo> _plans = new Dictionary<long, InProcPlanInfo>();

        // handles max threading
        LimitedConcurrencyLevelTaskScheduler _limitedConcurTaskSched = null;

        bool _isDrainstopped = false;
        bool _isDrainstopComplete = false;

        // default ctor
        public PlanScheduler()
            : this( 0 )
        { }

        /// <summary>
        /// Initializes a new PlanScheduler with optional MaxThreads
        /// </summary>
        /// <param name="maxThreads">Specifies the maximum count for processing threads</param>
        public PlanScheduler(int maxThreads = 0)
        {
            if( maxThreads == 0 )
            {
                _tf = new TaskFactory();
            }
            else
            {
                _limitedConcurTaskSched = new LimitedConcurrencyLevelTaskScheduler( maxThreads );
                _tf = new TaskFactory( _limitedConcurTaskSched );
            }
        }

        /// <summary>
        /// Queues a new Task onto the TaskFactory
        /// </summary>
        /// <param name="planContainer"></param>
        /// <returns>Success/Fail for whether the Task is queued.</returns>
        public virtual bool StartPlan(IPlanRuntimeContainer planContainer)
        {
            if( !_isDrainstopped )
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                InProcPlanInfo info = new InProcPlanInfo()
                {
                    PlanPod = planContainer,
                    CancellationToken = cts
                };
                _plans[planContainer.PlanInstanceId] = info;

                _tasks.Add( _tf.StartNew( () => { planContainer.Start( cts.Token, PlanComplete ); }, cts.Token ) );
            }

            return !_isDrainstopped;
        }

        public virtual bool CancelPlan(int planInstanceId)
        {
            bool found = _plans.ContainsKey( planInstanceId );
            if( found )
                _plans[planInstanceId].CancellationToken.Cancel();
            return found;
        }

        protected virtual void PlanComplete(IPlanRuntimeContainer planContainer)
        {
            _plans.Remove( planContainer.PlanInstanceId );
            OnPlanCompleted( planContainer );
        }


        public virtual void Drainstop()
        {
            if( !_isDrainstopped )
            {
                _isDrainstopped = true;
                _isDrainstopComplete = false;
                Task.WaitAll( _tasks.ToArray() );
                _isDrainstopComplete = true;
            }
        }
        public virtual void Undrainstop()
        {
            _isDrainstopped = false;
            _isDrainstopComplete = false;
        }

        public virtual int TasksQueuedOrRunning { get { return _limitedConcurTaskSched != null ? _limitedConcurTaskSched.DelegatesQueuedOrRunning : -1; } }

        public virtual bool IsDrainstopped { get { return _isDrainstopped; } }
        public virtual bool IsDrainstopComplete { get { return _isDrainstopComplete; } }

        public virtual int CurrentQueueDepth { get { return _plans != null ? _plans.Keys.Count : -1; } }

        public virtual List<string> CurrentQueue
        {
            get
            {
                List<string> list = new List<string>();
                foreach( InProcPlanInfo info in _plans.Values )
                    list.Add( info.PlanPod.Plan.Name );

                return list;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            //_cancellationTokenSrc.Dispose();
            GC.SuppressFinalize( this );
        }

        #endregion
    }

    // https://msdn.microsoft.com/en-us/library/system.threading.tasks.taskscheduler(v=vs.110).aspx
    internal class LimitedConcurrencyLevelTaskScheduler : TaskScheduler
    {
        // Indicates whether the current thread is processing work items.
        [ThreadStatic]
        private static bool _currentThreadIsProcessingItems;

        // The list of tasks to be executed 
        private readonly LinkedList<Task> _tasks = new LinkedList<Task>(); // protected by lock(_tasks)

        // The maximum concurrency level allowed by this scheduler. 
        private readonly int _maxDegreeOfParallelism;

        // Indicates whether the scheduler is currently processing work items. 
        private int _delegatesQueuedOrRunning = 0;

        // Creates a new instance with the specified degree of parallelism. 
        public LimitedConcurrencyLevelTaskScheduler(int maxDegreeOfParallelism)
        {
            if( maxDegreeOfParallelism < 1 ) throw new ArgumentOutOfRangeException( "maxDegreeOfParallelism" );
            _maxDegreeOfParallelism = maxDegreeOfParallelism;
        }

        // Queues a task to the scheduler. 
        protected sealed override void QueueTask(Task task)
        {
            // Add the task to the list of tasks to be processed.  If there aren't enough 
            // delegates currently queued or running to process tasks, schedule another. 
            lock( _tasks )
            {
                _tasks.AddLast( task );
                if( _delegatesQueuedOrRunning < _maxDegreeOfParallelism )
                {
                    ++_delegatesQueuedOrRunning;
                    NotifyThreadPoolOfPendingWork();
                }
            }
        }

        // Inform the ThreadPool that there's work to be executed for this scheduler. 
        private void NotifyThreadPoolOfPendingWork()
        {
            ThreadPool.UnsafeQueueUserWorkItem( _ =>
            {
                // Note that the current thread is now processing work items.
                // This is necessary to enable inlining of tasks into this thread.
                _currentThreadIsProcessingItems = true;
                try
                {
                    // Process all available items in the queue.
                    while( true )
                    {
                        Task item;
                        lock( _tasks )
                        {
                            // When there are no more items to be processed,
                            // note that we're done processing, and get out.
                            if( _tasks.Count == 0 )
                            {
                                --_delegatesQueuedOrRunning;
                                break;
                            }

                            // Get the next item from the queue
                            item = _tasks.First.Value;
                            _tasks.RemoveFirst();
                        }

                        // Execute the task we pulled out of the queue
                        base.TryExecuteTask( item );
                    }
                }
                // We're done processing items on the current thread
                finally { _currentThreadIsProcessingItems = false; }
            }, null );
        }

        // Attempts to execute the specified task on the current thread. 
        protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // If this thread isn't already processing a task, we don't support inlining
            if( !_currentThreadIsProcessingItems ) return false;

            // If the task was previously queued, remove it from the queue
            if( taskWasPreviouslyQueued )
                // Try to run the task. 
                if( TryDequeue( task ) )
                    return base.TryExecuteTask( task );
                else
                    return false;
            else
                return base.TryExecuteTask( task );
        }

        // Attempt to remove a previously scheduled task from the scheduler. 
        protected sealed override bool TryDequeue(Task task)
        {
            lock( _tasks ) return _tasks.Remove( task );
        }

        // Gets the maximum concurrency level supported by this scheduler. 
        public sealed override int MaximumConcurrencyLevel { get { return _maxDegreeOfParallelism; } }
        public int DelegatesQueuedOrRunning { get { return _delegatesQueuedOrRunning; } }

        // Gets an enumerable of the tasks currently scheduled on this scheduler. 
        protected sealed override IEnumerable<Task> GetScheduledTasks()
        {
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter( _tasks, ref lockTaken );
                if( lockTaken ) return _tasks;
                else throw new NotSupportedException();
            }
            finally
            {
                if( lockTaken ) Monitor.Exit( _tasks );
            }
        }
    }

    public class InProcPlanInfo
    {
        public IPlanRuntimeContainer PlanPod { get; set; }
        public CancellationTokenSource CancellationToken { get; set; }
    }

    public class PlanCompletedEventArgs : EventArgs
    {
        PlanCompletedEventArgs() { }
        public PlanCompletedEventArgs(IPlanRuntimeContainer planContainer)
        {
            PlanContainer = planContainer;
            TimeCompleted = DateTime.Now;
        }

        public IPlanRuntimeContainer PlanContainer { get; private set; }
        public DateTime TimeCompleted { get; private set; }
    }
}


///// <summary>
///// Queues a new Task onto the TaskFactory
///// </summary>
///// <param name="planInstanceId"></param>
///// <param name="dryRun"></param>
///// <param name="plan"></param>
//public bool StartPlan(string planInstanceId, bool dryRun, Plan plan)
//{
//    if( !_isDrainstopped )
//    {
//        CancellationTokenSource cts = new CancellationTokenSource();
//        _tasks.Add( _tf.StartNew( () => { plan.Start( null, dryRun ); }, cts.Token ) );
//    }

//    return !_isDrainstopped;
//}