namespace MauiLib1
{
    // All the code in this file is included in all platforms.
    public static class Bookmarks
    {
        public static void Save(string preferenceKey, NSUrl folderUrl)
        {
#if MACCATALYST || MACOS || IOS
            SecurityScopedBookmarks.SaveFolderBookmark(preferenceKey, folderUrl);
#elif WINDOWS
            SecurityScopedBookmarks.SaveFolderBookmark(preferenceKey, folderUrl);
#else
            // On platforms without security-scoped bookmarks, just return the URL directly.
#endif
        }

        public static NSUrl? Restore(string preferenceKey)
        {
#if MACCATALYST || MACOS || IOS
            return SecurityScopedBookmarks.TryRestoreFolderFromBookmark(preferenceKey);
#elif WINDOWS
            return SecurityScopedBookmarks.TryRestoreFolderFromBookmark(preferenceKey);
#else
            return preferenceKey;
#endif
        }

        public static string RestorePath(string preferenceKey)
        {
#if MACCATALYST || MACOS || IOS
            NSUrl? url=Restore(preferenceKey);
            if (url == null) { return preferenceKey; }
            return url.Path;
#else
            return preferenceKey;
#endif
        }

        public static void Clear(string preferenceKey)
        {
#if MACCATALYST || MACOS || IOS
            SecurityScopedBookmarks.ClearBookmark(preferenceKey);
#else
            // No-op on platforms without security-scoped bookmarks.
#endif
        }
    }
}
