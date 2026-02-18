using System.Runtime.InteropServices;
using WinRT.Interop;

namespace BPASpectrogramM.Platforms.Windows
{

    public static class WindowExtensions
    {
        // Win32 constants
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x00080000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public static void DisableCloseButton(this Microsoft.Maui.Controls.Window window)
        {
            var nativeWindow = window.Handler.PlatformView as Microsoft.UI.Xaml.Window;
            var hwnd = WindowNative.GetWindowHandle(nativeWindow);

            // Remove the system menu (which contains the Close button)
            int style = GetWindowLong(hwnd, GWL_STYLE);
            SetWindowLong(hwnd, GWL_STYLE, style & ~WS_SYSMENU);
        }
    }
}
