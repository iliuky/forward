using System;
using System.Threading;
using System.Threading.Tasks;

namespace Forward.Server.Common
{
    public static class TaskExtensions
    {
        /// <summary>
        /// 有返回值
        /// </summary>
        /// <param name="task"></param>
        /// <param name="timeout"></param>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout, CancellationToken token)
        {
            using (var timeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task)
                {
                    timeoutCancellationTokenSource.Cancel();
                    return await task;
                }
                else
                {
                    throw new TimeoutException("The operation has timed out.");
                }
            }
        }

        /// <summary>
        /// 无返回值
        /// </summary>
        /// <param name="task"></param>
        /// <param name="timeout"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task TimeoutAfter(this Task task, TimeSpan timeout, CancellationToken token)
        {
            using (var timeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task)
                {
                    timeoutCancellationTokenSource.Cancel();
                    await task;
                }
                else
                {
                    throw new TimeoutException("The operation has timed out.");
                }
            }
        }
    }
}