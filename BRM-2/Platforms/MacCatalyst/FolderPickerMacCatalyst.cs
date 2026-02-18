using System;
using System.Threading.Tasks;
using AppKit;
using UniformTypeIdentifiers;
using UIKit;

namespace BRM_2.Platforms.MacCatalyst
{
    public class FolderPickerMacCatalyst : IFolderPicker
    {
        UTType folderType =
            UTType.CreateFromIdentifier("public.folder")
            ?? UTType.CreateFromIdentifier("public.directory");
        public Task<string> PickFolderAsync()
        {
            var tcs = new TaskCompletionSource<string>();

            var picker = new UIDocumentPickerViewController(
                new[] { folderType },
                false);

            picker.AllowsMultipleSelection = false;

            picker.DidPickDocumentAtUrls += (sender, e) =>
            {
                var url = e.Urls?.FirstOrDefault();
                tcs.TrySetResult(url?.Path ?? string.Empty);
            };

            picker.WasCancelled += (sender, e) =>
            {
                tcs.TrySetResult(string.Empty);
            };

            var vc = GetTopViewController();
            vc.PresentViewController(picker, true, null);

            return tcs.Task;
        }

        private static UIViewController GetTopViewController()
        {
            var window = UIApplication.SharedApplication
                .ConnectedScenes
                .OfType<UIWindowScene>()
                .SelectMany(s => s.Windows)
                .FirstOrDefault(w => w.IsKeyWindow);

            var vc = window?.RootViewController;
            while (vc?.PresentedViewController != null)
                vc = vc.PresentedViewController;

            return vc ?? throw new InvalidOperationException("No active UIViewController to present document picker.");
        }
    }
}
