namespace BRM_2.Pages;

public partial class TabViewPage : ContentPage
{
    public TabViewPageVM ViewModel;
    public SpectrogramPageAsControl spectrogramTab;

    public TabViewPage(TabViewPageVM viewModel)
    {
        System.Diagnostics.Debug.WriteLine("[TabViewPage] Constructor: Starting");
        this.ViewModel = viewModel;
        
        // SET BINDING CONTEXT BEFORE InitializeComponent() so XAML bindings resolve
        this.BindingContext = ViewModel;
        System.Diagnostics.Debug.WriteLine("[TabViewPage] Constructor: BindingContext set (before InitializeComponent)");
        
        try
        {
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("[TabViewPage] Constructor: InitializeComponent completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TabViewPage] InitializeComponent ERROR: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[TabViewPage] Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[TabViewPage] StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"[TabViewPage] Inner Exception: {ex.InnerException.Message}");
            }
            throw;
        }
    }

    protected override void OnAppearing()
    {
        System.Diagnostics.Debug.WriteLine("[TabViewPage] OnAppearing: Starting");
        base.OnAppearing();
        //spectrogramTab = spectrogramTabControl;
        
        if (tabView != null)
        {
            tabView.SelectionChanging += TabView_SelectionChanging;
            System.Diagnostics.Debug.WriteLine("[TabViewPage] OnAppearing: Event handler attached");
        }
        
        //spectrogramTab.AnalysisCompletedEvent += spectrogramTabControl_AnalysisCompletedEvent;  
    }

    protected override void OnDisappearing()
    {
        if (tabView != null)
        {
            tabView.SelectionChanging -= TabView_SelectionChanging;
        }
        base.OnDisappearing();
    }


    private async void TabView_SelectionChanging(object sender, SelectionChangingEventArgs e)
    {
        if (e.Index == 4)
        {
            ViewModel.BusyRunning = true;
            try
            {
                System.Diagnostics.Debug.WriteLine("[TabViewPage] TabView_SelectionChanging: Spectrogram tab selected");
                if (ViewModel.spectrogramPage == null)
                {
                    Debug.WriteLine("[TabViewPage] TabView_SelectionChanging: Creating SpectrogramPage");
                    await ViewModel.CreateSpectrogramPage();
                    Debug.WriteLine("[TabViewPage] TabView_SelectionChanging: SpectrogramPage created");
                    
                    if (ViewModel.spectrogramPage != null)
                    {
                        spectrogramGrid.Children.Add(ViewModel.spectrogramPage);
                        await ViewModel.spectrogramPage.ReadDefaults();
                        Debug.WriteLine("[TabViewPage] TabView_SelectionChanging: SpectrogramPage defaults read");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally { ViewModel.BusyRunning = false; }
        }
        
    }


    private void spectrogramTabControl_AnalysisCompletedEvent(object sender, BPASpectrogramM.Views.FileEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e?.folder))
        {
            ViewModel.ImportFromFolder(e.folder);
        }
        else
        {
            DisplayAlert("Import Session Failed", $"Unable to Import from {e.folder}", "OK");
        }
    }
}