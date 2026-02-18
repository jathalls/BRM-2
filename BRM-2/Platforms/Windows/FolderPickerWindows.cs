using System;
using System.Threading.Tasks;
using WinRT.Interop;

namespace BRM_2.Platforms.Windows
{
    public class FolderPickerWindows : IFolderPicker
    {
        public async Task<string> PickFolderAsync()
        {
            try
            {
                var folderPicker = new global::Windows.Storage.Pickers.FolderPicker();
                
                // Get the window handle for the current MAUI window
                var window = Microsoft.Maui.Controls.Application.Current?.Windows?[0];
                if (window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window xamlWindow)
                {
                    var hwnd = WindowNative.GetWindowHandle(xamlWindow);
                    InitializeWithWindow.Initialize(folderPicker, hwnd);
                }
                
                folderPicker.SuggestedStartLocation = global::Windows.Storage.Pickers.PickerLocationId.Desktop;
                folderPicker.FileTypeFilter.Add("*");

                var folder = await folderPicker.PickSingleFolderAsync();
                return folder?.Path ?? "";
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to pick folder on Windows: {ex.Message}", ex);
            }
        }
    }
}
