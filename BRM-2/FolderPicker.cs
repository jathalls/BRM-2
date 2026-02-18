using System;
using System.Threading.Tasks;

namespace BRM_2
{
    public static class FolderPicker
    {
        public static async Task<string> PickFolderAsync()
        {
            try
            {
                var services = App.Current?.Handler?.MauiContext?.Services;
                var picker = services?.GetService<IFolderPicker>();
                if (picker != null)
                {
                    return await picker.PickFolderAsync();
                }
                
                // Fallback implementation per platform
#if WINDOWS
                var windowsPicker = new BRM_2.Platforms.Windows.FolderPickerWindows();
                return await windowsPicker.PickFolderAsync();
#elif MACCATALYST
                var macPicker = new BRM_2.Platforms.MacCatalyst.FolderPickerMacCatalyst();
                return await macPicker.PickFolderAsync();
#else
                throw new PlatformNotSupportedException($"Folder picker is not supported on {DeviceInfo.Platform}");
#endif
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to pick folder: {ex.Message}");
            }
        }
    }
}
