using System;
using System.Runtime.InteropServices;

namespace Gizmo.Client.UI.Host.WPF.Services
{
    internal static class WindowsTaskbarHelper
    {
        [DllImport("user32.dll")] static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        public static void SetVisible(bool visible)
        {
            int cmd = visible ? SW_SHOW : SW_HIDE;

            // Main taskbar (Windows 10 & 11)
            var taskbar = FindWindow("Shell_TrayWnd", null);
            if (taskbar != IntPtr.Zero)
                ShowWindow(taskbar, cmd);

            // Secondary taskbars on other monitors
            var secondaryTaskbar = FindWindow("Shell_SecondaryTrayWnd", null);
            if (secondaryTaskbar != IntPtr.Zero)
                ShowWindow(secondaryTaskbar, cmd);

            // Windows 11: Start menu button lives in a separate process window
            var startHost = FindWindow("Windows.UI.Core.CoreWindow", null);
            if (startHost != IntPtr.Zero)
                ShowWindow(startHost, cmd);
        }
    }
}
