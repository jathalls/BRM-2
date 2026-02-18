using System;
using Foundation;
using Microsoft.Maui.Storage;

namespace BRM_2;

public class SecurityScopedBookmarks
{
    // Persist bookmark as Base64 in Preferences.
    // If you prefer encryption, use SecureStorage, but note it can be finicky on macOS depending on keychain state.
    public static void SaveFolderBookmark(string preferenceKey, NSUrl folderUrl)
    {
        if (string.IsNullOrWhiteSpace(preferenceKey))
            throw new ArgumentException("Preference key is required.", nameof(preferenceKey));
        if (folderUrl is null)
            throw new ArgumentNullException(nameof(folderUrl));

        NSError? error;
        
        var existing=TryRestoreFolderFromBookmark(preferenceKey);
        if (!(existing is null)) return; //we already have a bookmark for that Path

        // Create a security-scoped bookmark that can be resolved later.
        // For folders, this is the typical approach.
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
                // If refresh fails, still return the URL; it may work for current session.
            }
        }

        return restoredUrl;
    }

    public static void ClearBookmark(string preferenceKey)
    {
        if (!string.IsNullOrWhiteSpace(preferenceKey))
            Preferences.Remove(preferenceKey);
    }
}