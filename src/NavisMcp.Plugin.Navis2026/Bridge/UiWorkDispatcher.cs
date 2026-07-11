using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NavisMcp.Plugin.Navis2026.Bridge
{
    internal sealed class UiWorkDispatcher : IDisposable
    {
        private readonly ConcurrentQueue<WorkItem> _queue = new ConcurrentQueue<WorkItem>();
        private readonly Timer _timer;
        private bool _disposed;

        public UiWorkDispatcher()
        {
            _timer = new Timer { Interval = 25 };
            _timer.Tick += OnTick;
            _timer.Start();
        }

        public Task<object> InvokeAsync(Func<object> work)
        {
            if (work == null)
            {
                throw new ArgumentNullException(nameof(work));
            }

            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue.Enqueue(new WorkItem(work, tcs));
            return tcs.Task;
        }

        private void OnTick(object sender, EventArgs e)
        {
            while (_queue.TryDequeue(out var item))
            {
                try
                {
                    item.Completion.SetResult(item.Work());
                }
                catch (Exception ex)
                {
                    item.Completion.SetException(ex);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _timer.Stop();
            _timer.Tick -= OnTick;
            _timer.Dispose();

            while (_queue.TryDequeue(out var item))
            {
                item.Completion.TrySetCanceled();
            }
        }

        private sealed class WorkItem
        {
            public WorkItem(Func<object> work, TaskCompletionSource<object> completion)
            {
                Work = work;
                Completion = completion;
            }

            public Func<object> Work { get; }
            public TaskCompletionSource<object> Completion { get; }
        }
    }
}
