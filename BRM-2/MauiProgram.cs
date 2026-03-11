using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Core;
using ScottPlot.Maui;
using Syncfusion.Maui.Core.Hosting;
using Microsoft.Maui.LifecycleEvents;
using CommunityToolkit.Maui;

namespace BRM_2;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		try
		{
			System.Diagnostics.Debug.WriteLine("[MauiProgram] Starting CreateMauiApp");
			
			Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JGaF5cXGpCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdlWX1ednRcR2VZWURzXEJWYEs=");
			System.Diagnostics.Debug.WriteLine("[MauiProgram] License registered");

			var builder = MauiApp.CreateBuilder();
			System.Diagnostics.Debug.WriteLine("[MauiProgram] Builder created");
			
			builder
				.UseMauiApp<App>()
				.UseMauiCommunityToolkitMediaElement(false)
				.UseMauiCommunityToolkit()
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
					fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				})
				
				.ConfigureSyncfusionCore()
				.UseScottPlot()
				.ConfigureLifecycleEvents(events =>
				{
#if WINDOWS
					events.AddWindows(windowsLifecycleBuilder =>
					{
						windowsLifecycleBuilder.OnWindowCreated(window =>
						{
							window.ExtendsContentIntoTitleBar = false;
							var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
							var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
							var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);
						});
					});
#endif
				});

			System.Diagnostics.Debug.WriteLine("[MauiProgram] Registering services");
			
			// Register services
			builder.Services.AddSingleton<SessionDetailsDisplay>();
			builder.Services.AddSingleton<SessionDetailsDisplayVM>();
			builder.Services.AddSingleton<RecordingsPage>();
			builder.Services.AddSingleton<RecordingsPageVM>();
			builder.Services.AddSingleton<ViewModels.SessionDetailsVM>();
			builder.Services.AddTransient<MapSelectionPage>();
			builder.Services.AddTransient<MapSelectionVM>();
			builder.Services.AddSingleton<SessionsPageVM>();
			builder.Services.AddSingleton<TabViewPageVM>(sp => new ViewModels.TabViewPageVM());
			builder.Services.AddSingleton<TabViewPage>(sp => 
{
	var vm = sp.GetRequiredService<TabViewPageVM>();
	return new Pages.TabViewPage(vm);
});

#if WINDOWS
			builder.Services.AddSingleton<IFolderPicker, BRM_2.Platforms.Windows.FolderPickerWindows>();
#elif MACCATALYST
			builder.Services.AddSingleton<IFolderPicker, BRM_2.Platforms.MacCatalyst.FolderPickerMacCatalyst>();
#else
			//builder.Services.AddSingleton<IFolderPicker>(sp => new FolderPicker());
#endif

			builder.Services.AddSingleton<NavigationService, NavigationService>();
			
			System.Diagnostics.Debug.WriteLine("[MauiProgram] Services registered, building app");
			
#if DEBUG
			builder.Logging.AddDebug();
#endif

			BPASpectrogramM.Navigation.ServiceCollectionExtensions.AddSpectrogramServices(builder.Services);
			
			System.Diagnostics.Debug.WriteLine("[MauiProgram] Building MauiApp");
			var app = builder.Build();
			System.Diagnostics.Debug.WriteLine("[MauiProgram] MauiApp built successfully");
			return app;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[MauiProgram] CRITICAL ERROR: {ex.GetType().Name}");
			System.Diagnostics.Debug.WriteLine($"[MauiProgram] Message: {ex.Message}");
			System.Diagnostics.Debug.WriteLine($"[MauiProgram] StackTrace: {ex.StackTrace}");
			throw;
		}
	}
}
