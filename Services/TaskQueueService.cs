using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTool.Services
{
    public class TaskQueueService
    {
        private readonly SemaphoreSlim _semaphore;

        public TaskQueueService(int maxParallel = 10)
        {
            _semaphore = new SemaphoreSlim(maxParallel);
        }

        public async Task RunAsync(Func<Task> task, CancellationToken token = default)
        {
            await _semaphore.WaitAsync(token);
            try
            {
                if (!token.IsCancellationRequested)
                    await task();
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}