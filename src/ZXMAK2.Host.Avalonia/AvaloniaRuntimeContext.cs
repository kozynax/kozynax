using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;

namespace ZXMAK2.Host.AvaloniaBackend
{
    /// <summary>
    /// Shared host state for dialogs and services: the main window (dialog owner)
    /// and hooks to quiesce emulator input while a modal UI is up.
    /// </summary>
    public sealed class AvaloniaRuntimeContext
    {
        public Window MainWindow { get; set; }

        /// <summary>Release Spectrum keys / mouse capture before a modal opens.</summary>
        public Action PrepareUiInput { get; set; }

        /// <summary>Restore state after a modal closes.</summary>
        public Action EndUiInput { get; set; }

        /// <summary>
        /// Blocks the caller until <paramref name="task"/> completes. On the UI thread a
        /// nested dispatcher frame keeps the window responsive; elsewhere it just waits.
        /// Lets the synchronous IUserQuery / IOpenFileDialog contracts sit on async Avalonia APIs.
        /// </summary>
        public static T RunSync<T>(Func<Task<T>> start)
        {
            if (!Dispatcher.UIThread.CheckAccess())
                return Dispatcher.UIThread.Invoke(() => RunSync(start));

            var task = start();
            if (!task.IsCompleted)
            {
                var frame = new DispatcherFrame();
                task.ContinueWith(
                    _ => Dispatcher.UIThread.Post(() => frame.Continue = false),
                    TaskScheduler.Default);
                Dispatcher.UIThread.PushFrame(frame);
            }
            return task.GetAwaiter().GetResult();
        }

        public static void RunSync(Func<Task> start)
        {
            RunSync<object>(async () =>
            {
                await start().ConfigureAwait(true);
                return null;
            });
        }
    }
}
