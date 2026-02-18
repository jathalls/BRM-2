namespace BRM_2;

public partial class AppShell : Shell
{
    private TabViewPageVM tabViewPageVM;

    public AppShell(TabViewPageVM tabViewPageVM)
    {
        System.Diagnostics.Debug.WriteLine("[AppShell] Constructor: Starting");
        this.tabViewPageVM = tabViewPageVM;

        InitializeComponent();
        System.Diagnostics.Debug.WriteLine("[AppShell] Constructor: XAML initialized");
        
        this.BindingContext = tabViewPageVM;
        System.Diagnostics.Debug.WriteLine("[AppShell] Constructor: BindingContext set");
    }

    public enum TABS { Sessions = 0, Details = 1, Recordings = 2, Bats = 3, Spectrogram = 4, AudioPlayer = 5 }

    protected override async void OnAppearing()
    {
        System.Diagnostics.Debug.WriteLine("[AppShell] OnAppearing: Starting");
        base.OnAppearing();

        var readstatus = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
        if (readstatus != PermissionStatus.Granted)
        {
            readstatus = await Permissions.RequestAsync<Permissions.StorageRead>();
        }
        if (readstatus != PermissionStatus.Granted)
        {
            System.Diagnostics.Debug.WriteLine("[AppShell] Permissions not granted - READ");
            return;
        }
        
        var writestatus = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
        if (writestatus != PermissionStatus.Granted)
        {
            writestatus = await Permissions.RequestAsync<Permissions.StorageWrite>();
            if (writestatus != PermissionStatus.Granted)
            {
                System.Diagnostics.Debug.WriteLine("[AppShell] Permissions not granted - WRITE");
                return;
            }
        }
        
        System.Diagnostics.Debug.WriteLine("[AppShell] OnAppearing: Permissions granted");
    }

    public void SwitchToTab(TABS tab, object parameter)
    {
        tabViewPageVM.SwitchToTab(tab, parameter);
    }

    public void SwitchToTab(TABS tab)
    {
        tabViewPageVM.SwitchToTab(tab);
    }
}
