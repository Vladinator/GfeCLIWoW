#if WINDOWS

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GfeCLIWoW
{
    class ConsoleUtils
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetParent(IntPtr handle);
        public static nint GetWindow()
        {
            var handle = GetConsoleWindow();
            if (handle == IntPtr.Zero)
            {
                return handle;
            }
            var visited = new HashSet<IntPtr>();
            while (handle != IntPtr.Zero)
            {
                if (!visited.Add(handle))
                {
                    return IntPtr.Zero;
                }
                var parent = GetParent(handle);
                if (parent == IntPtr.Zero)
                {
                    return handle;
                }
                handle = parent;
            }
            return handle;
        }
    }

    class WindowUtils
    {
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr handle);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr handle, int command);
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr handle);
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowLong(IntPtr handle, int index);
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLong(IntPtr handle, int index, IntPtr newLong);
        public static bool? IsShown(nint handle)
        {
            if (handle == IntPtr.Zero)
            {
                return null;
            }
            var visible = IsWindowVisible(handle);
#if DEBUG
            Console.WriteLine($"[Debug] Window handle {handle:X} is {(visible ? "shown" : "hidden")}");
#endif
            return visible;
        }
        public static bool? SetShown(nint handle, bool shown)
        {
            if (handle == IntPtr.Zero)
            {
                return null;
            }
            var changed = ShowWindow(handle, shown ? SW_SHOW : SW_HIDE);
#if DEBUG
            Console.WriteLine($"[Debug] Window handle {handle:X} set as {(shown ? "shown" : "hidden")}");
#endif
            if (!changed)
            {
                return false;
            }
            var currentStyle = GetWindowLong(handle, GWL_EXSTYLE);
            var newStyle = shown ? (currentStyle | WS_EX_TOOLWINDOW) : (currentStyle & ~WS_EX_TOOLWINDOW);
            changed = SetWindowLong(handle, GWL_EXSTYLE, newStyle) == newStyle;
            if (!changed)
            {
                return false;
            }
            changed = SetForegroundWindow(handle);
            return changed;
        }
    }

    class Tray : IDisposable
    {
        private static Tray? trayInstance;
        private static Task? trayInstanceWorker;
        private static CancellationTokenSource? trayInstanceWorkerCancellation;

        public static void Start()
        {
            var handle = ConsoleUtils.GetWindow();
            if (handle == IntPtr.Zero)
            {
                return;
            }
            trayInstanceWorkerCancellation ??= new();
            trayInstanceWorker ??= Task.Run(() => {
                trayInstance = new Tray(handle);
                while (!trayInstanceWorkerCancellation.IsCancellationRequested)
                {
                    Application.DoEvents();
                    Thread.Sleep(250);
                }
                trayInstance.Dispose();
            }, trayInstanceWorkerCancellation.Token);
            Task.Run(() =>
            {
                while (!trayInstanceWorkerCancellation.IsCancellationRequested && trayInstance == null)
                {
                    Thread.Sleep(25);
                }
            }).Wait();
        }

        public static void Stop()
        {
            if (trayInstanceWorkerCancellation == null || trayInstanceWorker == null)
            {
                return;
            }
            trayInstanceWorkerCancellation.Cancel();
            trayInstanceWorker.Wait();
        }

        public static void HideTo()
        {
            if (trayInstance == null)
            {
                return;
            }
            trayInstance.CloseWindow();
        }

        private readonly nint handle;
        private readonly NotifyIcon icon;

        private Tray(nint windowHandle)
        {
            handle = windowHandle;
            icon = new NotifyIcon
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? System.Drawing.SystemIcons.Application,
                Text = Process.GetCurrentProcess().ProcessName,
                Visible = true,
            };
            icon.MouseClick += OnTrayClick;
        }

        private void OnTrayClick(object? sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                case MouseButtons.Right:
                    ToggleWindow();
                    break;
            }
        }

        public void Dispose()
        {
            icon.MouseClick -= OnTrayClick;
            icon.Dispose();
        }

        private bool IsWindowShown()
        {
            return WindowUtils.IsShown(handle) ?? false;
        }

        private void OpenWindow()
        {
            WindowUtils.SetShown(handle, true);
        }

        public void CloseWindow()
        {
            WindowUtils.SetShown(handle, false);
        }

        private void ToggleWindow()
        {
            if (IsWindowShown())
            {
                CloseWindow();
            }
            else
            {
                OpenWindow();
            }
        }
    }
}

#endif
