using System;
using System.Threading;
using System.Threading.Tasks;

namespace CLIF.Tests.Utilities
{
    /// <summary>
    /// Provides helpers to enforce timeouts for async Tasks in tests.
    /// </summary>
    public static class TaskTimeoutExtensions
    {
        /// <summary>
        /// Await a Task with a timeout. Throws TimeoutException if the timeout elapses first.
        /// </summary>
        public static async Task WithTimeout(this Task task, TimeSpan timeout, string? operationName = null)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            using var cts = new CancellationTokenSource();
            var delayTask = Task.Delay(timeout, cts.Token);
            var completed = await Task.WhenAny(task, delayTask).ConfigureAwait(false);

            if (completed == delayTask)
            {
                throw new TimeoutException($"Operation {operationName ?? "task"} timed out after {timeout}.");
            }

            cts.Cancel(); // cancel the delay to free the timer
            await task.ConfigureAwait(false); // propagate any exceptions
        }

        /// <summary>
        /// Await a Task<T> with a timeout. Throws TimeoutException if the timeout elapses first.
        /// </summary>
        public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout, string? operationName = null)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            using var cts = new CancellationTokenSource();
            var delayTask = Task.Delay(timeout, cts.Token);
            var completed = await Task.WhenAny(task, delayTask).ConfigureAwait(false);

            if (completed == delayTask)
            {
                throw new TimeoutException($"Operation {operationName ?? "task"} timed out after {timeout}.");
            }

            cts.Cancel(); // cancel the delay to free the timer
            return await task.ConfigureAwait(false); // propagate result/exceptions
        }
    }
}
