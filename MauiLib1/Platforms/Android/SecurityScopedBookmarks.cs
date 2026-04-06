using System;
using Microsoft.Maui.Storage;

namespace MauiLib1
{
#if ANDROID
    public class SecurityScopedBookmarks
    {
        // Android doesn't have the same security-scoped bookmark concept as iOS/macOS
        // This is a stub implementation that just stores the path string

        public static void SaveFolderBookmark(string preferenceKey, string folderUrl)
        {
            if (string.IsNullOrWhiteSpace(preferenceKey))
                throw new ArgumentException("Preference key is required.", nameof(preferenceKey));

            Preferences.Set(preferenceKey, folderUrl);
        }

        public static string? TryRestoreFolderFromBookmark(string preferenceKey)
        {
            return Preferences.Get(preferenceKey, null);
        }

        public static void ClearBookmark(string preferenceKey)
        {
            Preferences.Remove(preferenceKey);
        }
    }
#endif
}
