using System;
using Foundation;
using Microsoft.Maui.Storage;

namespace MauiLib1
{
#if IOS && !MACCATALYST
    public class SecurityScopedBookmarks
    {
        // On iOS (but not MacCatalyst), use the same bookmark approach as macOS/MacCatalyst
        // (iOS does support security-scoped bookmarks)

        public static void SaveFolderBookmark(string preferenceKey, NSUrl folderUrl)
        {
            if (string.IsNullOrWhiteSpace(preferenceKey))
                throw new ArgumentException("Preference key is required.", nameof(preferenceKey));
            if (folderUrl is null)
                throw new ArgumentNullException(nameof(folderUrl));

            NSError? error;

            var existing = TryRestoreFolderFromBookmark(preferenceKey);
            if (!(existing is null)) return; //we already have a bookmark for that Path

            // Create a security-scoped bookmark that can be resolved later.
            var bookmarkData = folderUrl.CreateBookmarkData(
                NSUrlBookmarkCreationOptions.WithSecurityScope,
                null,
                null,
                out error);

            if (bookmarkData is null || error != null)
                throw new InvalidOperationException($"Failed to create bookmark. {error?.LocalizedDescription}");

            var base64 = bookmarkData.GetBase64EncodedString(NSDataBase64EncodingOptions.None);
            Preferences.Set(preferenceKey, base64);
        }

        public static NSUrl? TryRestoreFolderFromBookmark(string preferenceKey)
        {
            if (string.IsNullOrWhiteSpace(preferenceKey))
                return null;

            var base64 = Preferences.Get(preferenceKey, null);
            if (string.IsNullOrWhiteSpace(base64))
                return null;

            NSData? bookmarkData;
            try
            {
                bookmarkData = new NSData(base64, NSDataBase64DecodingOptions.None);
            }
            catch
            {
                return null;
            }

            bool isStale;
            NSError? error;

            // Resolve the bookmark back into an NSUrl.
            var restoredUrl = NSUrl.FromBookmarkData(
                bookmarkData,
                NSUrlBookmarkResolutionOptions.WithSecurityScope,
                null,
                out isStale,
                out error);

            if (restoredUrl is null || error != null)
                return null;

            // If stale, recreate the bookmark (Apple recommends this).
            if (isStale)
            {
                try
                {
                    SaveFolderBookmark(preferenceKey, restoredUrl);
                }
                catch
                {
                    // Ignore errors while updating
                }
            }

            if (restoredUrl is not null)
                restoredUrl.StartAccessingSecurityScopedResource();

            return restoredUrl;
        }

        public static void ClearBookmark(string preferenceKey)
        {
            Preferences.Remove(preferenceKey);
        }
    }
#endif
}
