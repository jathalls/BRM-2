namespace BRM_2.Controls;
public partial class SessionsPage : ContentView, INotifyPropertyChanged
{


    public SessionsPageVM viewModel;

    public SessionsPage()
    {
        viewModel = BRM_2.Navigation.ServiceProvider.GetService<SessionsPageVM>();
        InitializeComponent();
        this.BindingContext = this.viewModel;



        sfSessionDataGrid.SelectionChanged += SfSessionDataGrid_SelectionChanged;
        Loaded += SessionsPage_Loaded;
    }

    public SessionsPage(SessionsPageVM viewModel)
	{
		//Displays = new ObservableCollection<SessionDisplay>(DBAccess.GetDisplaySessionsAsync().Result);
		this.viewModel = viewModel;
        InitializeComponent();
		this.BindingContext = this.viewModel;
        
        
        sfSessionDataGrid.SelectionChanged += SfSessionDataGrid_SelectionChanged;
        Loaded += SessionsPage_Loaded;
    }

    private async void SfSessionDataGrid_SelectionChanged(object? sender, DataGridSelectionChangedEventArgs e)
    {
        await viewModel.SfSessionDataGrid_SelectionChanged(sender, e);
        this.InvalidateMeasure();
    }

    private async void SessionsPage_Loaded(object? sender, EventArgs e)
    {
        
        try
        {
            if (BindingContext is SessionsPageVM viewModel)
            {
                await viewModel.RefreshAsync();
            }
			
		}
        catch (Exception ex)
        {
            Debug.WriteLine($"ERR- {ex.Message} @ {ex.StackTrace}");
        }

        

    }
}