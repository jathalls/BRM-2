#if MACCATALYST

using Foundation;
using UIKit;
namespace BRM_2;


public static class MacFolderPicker
{
    public static Task<NSUrl?> PickFolderAsync()
    {
        var tcs = new TaskCompletionSource<NSUrl?>();

        var picker = new UIDocumentPickerViewController(
            new[] { "public.folder" }, // folder UTI
            UIDocumentPickerMode.Open);

        picker.AllowsMultipleSelection = false;

        picker.DidPickDocumentAtUrls += (_, e) =>
        {
            var url = e?.Urls?.FirstOrDefault();
            tcs.TrySetResult(url);
        };

        picker.WasCancelled += (_, __) => tcs.TrySetResult(null);

        // Present
        var vc = UIApplication.SharedApplication.KeyWindow?.RootViewController;
        while (vc?.PresentedViewController != null)
            vc = vc.PresentedViewController;

        vc?.PresentViewController(picker, true, null);

        return tcs.Task;
    }
}
#endif
