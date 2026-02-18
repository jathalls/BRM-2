namespace BRM_2;

public partial class App : Application
{
	public App()
	{
		System.Diagnostics.Debug.WriteLine("[App] Constructor starting");
		InitializeComponent();
		System.Diagnostics.Debug.WriteLine("[App] Constructor completed");
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		try
		{
			System.Diagnostics.Debug.WriteLine("[App.CreateWindow] Starting");
			
			// Get the MauiApp services from the current application
			var services = this.Handler?.MauiContext?.Services;
			System.Diagnostics.Debug.WriteLine($"[App.CreateWindow] Services available: {services != null}");
			
			if (services == null)
			{
				throw new InvalidOperationException("Services are not available in MauiContext");
			}
			
			var tabVm = services.GetService<BRM_2.ViewModels.TabViewPageVM>();
			System.Diagnostics.Debug.WriteLine($"[App.CreateWindow] TabViewPageVM retrieved: {tabVm != null}");
			
			if (tabVm == null)
			{
				throw new InvalidOperationException("TabViewPageVM could not be resolved from DI container");
			}
			
			System.Diagnostics.Debug.WriteLine("[App.CreateWindow] Creating AppShell");
			var shell = new AppShell(tabVm);
			var window = new Window(shell);
			
			System.Diagnostics.Debug.WriteLine("[App.CreateWindow] Window created successfully");
			return window;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[App.CreateWindow] CRITICAL ERROR: {ex.GetType().Name}");
			System.Diagnostics.Debug.WriteLine($"[App.CreateWindow] Message: {ex.Message}");
			System.Diagnostics.Debug.WriteLine($"[App.CreateWindow] StackTrace: {ex.StackTrace}");
			throw;
		}
	}
}