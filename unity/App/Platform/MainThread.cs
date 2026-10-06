using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedMoon.UnityApp.Platform
{
    /// <summary>
    /// Kører kode på Unitys hovedtråd.
    /// RedMoon.Core arbejder med async/await og kan fortsætte på baggrundstråde. Kald til Android (JNI)
    /// må kun ske fra en tråd Java kender, så de sendes altid til hovedtråden via Unitys SynchronizationContext.
    /// </summary>
    internal static class MainThread
    {
        private static SynchronizationContext? _context;
        private static int _mainThreadId = -1;

        /// <summary>Skal kaldes én gang fra hovedtråden ved opstart.</summary>
        public static void Capture()
        {
            _context = SynchronizationContext.Current;
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>Sand hvis koden allerede kører på hovedtråden.</summary>
        public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == _mainThreadId;

        /// <summary>Kører <paramref name="func"/> på hovedtråden og returnerer resultatet.</summary>
        public static Task<T> RunAsync<T>(Func<T> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            if (IsMainThread || _context == null)
            {
                try
                {
                    return Task.FromResult(func());
                }
                catch (Exception ex)
                {
                    return Task.FromException<T>(ex);
                }
            }

            var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            _context.Post(_ =>
            {
                try
                {
                    completion.SetResult(func());
                }
                catch (Exception ex)
                {
                    completion.SetException(ex);
                }
            }, null);
            return completion.Task;
        }

        /// <summary>Kører <paramref name="action"/> på hovedtråden.</summary>
        public static Task RunAsync(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return RunAsync(() =>
            {
                action();
                return true;
            });
        }
    }
}
