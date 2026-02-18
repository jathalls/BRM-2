using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BRM_2.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
	/// <summary>
	/// Initializes the singleton application object.  This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
	{
		try
		{
			this.InitializeComponent();
		}catch(Exception ex)	
		{
			System.Diagnostics.Debug.WriteLine($"App constructor exception: {ex}");
			if(ex.InnerException != null)
			{
				System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException}");
            }
            throw;
		}
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

