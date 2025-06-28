using System.Configuration;
using System.Data;
using System.Windows;
using System.Runtime.InteropServices;
using System;

namespace ToyControlApp // Changed from WpfApp2 to match your namespace
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Apply dark mode to any window when it's loaded
            EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent, new RoutedEventHandler(OnWindowLoaded));
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is Window window)
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    UseImmersiveDarkMode(hwnd, true);
                }
                else
                {
                    // If handle isn't ready yet, wait for SourceInitialized
                    window.SourceInitialized += (s, args) =>
                    {
                        var handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
                        UseImmersiveDarkMode(handle, true);
                    };
                }
            }
        }

        private static bool UseImmersiveDarkMode(IntPtr handle, bool enabled)
        {
            if (IsWindows10OrGreater(17763))
            {
                var attribute = DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;
                if (IsWindows10OrGreater(18985))
                {
                    attribute = DWMWA_USE_IMMERSIVE_DARK_MODE;
                }

                int useImmersiveDarkMode = enabled ? 1 : 0;
                return DwmSetWindowAttribute(handle, attribute, ref useImmersiveDarkMode, sizeof(int)) == 0;
            }

            return false;
        }

        private static bool IsWindows10OrGreater(int build = -1)
        {
            return Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= build;
        }
    }
}