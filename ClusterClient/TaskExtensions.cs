using System;
using System.Threading;
using System.Threading.Tasks;

namespace ClusterClient
{
    public static class TaskExtensions
    {
        public static async Task<ResultOrTimeout<TResult>> LimitByTimeout<TResult>(
            this Task<TResult> task, TimeSpan timeout,
            CancellationTokenSource cts = null)
        {
            if (cts == null)
                cts = new CancellationTokenSource();

            await Task.WhenAny(task, Task.Delay(timeout, cts.Token));
            cts.Cancel();
            return !task.IsCompleted ? ResultOrTimeout<TResult>.Timeout : task.Result;
        }
    }
}
