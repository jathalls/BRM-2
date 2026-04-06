using System;
using Microsoft.Maui.Storage;

namespace MauiLib1
{
#if WINDOWS
    public class SecurityScopedBookmarks
    {
        // Persist bookmark as Base64 in Preferences.
        // If you prefer encryption, use SecureStorage, but note it can be finicky on macOS depending on keychain state.
        public static void SaveFolderBookmark(string preferenceKey, string folderUrl)
        {

        }

        public static string? TryRestoreFolderFromBookmark(string preferenceKey)
        {
            return preferenceKey;
        }

        public static void ClearBookmark(string preferenceKey)
        {

        }
    }
#endif
}