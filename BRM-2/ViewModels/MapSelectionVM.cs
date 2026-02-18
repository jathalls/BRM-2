namespace BRM_2.ViewModels;
public partial class MapSelectionVM:ObservableObject
{
    public event EventHandler<EventArgs>? MapClosing;

    internal virtual void OnMapClosing(EventArgs e) => MapClosing?.Invoke(this, e);



    [ObservableProperty]
    private MapLatLng _selectedPagePosition;
    

    private  NavigationService NavigationService { get; set; }
    public MapSelectionVM( NavigationService navigationService)
    {
       
        this.NavigationService = navigationService;
    }

    public MapLatLng FinalPosition = new MapLatLng();

    [RelayCommand]
    public async void OKButton()
    {
        FinalPosition = SelectedPagePosition;
        await Close();
    }

    [RelayCommand]
    public async void CancelButton()
    {
        await Close();
    }

    public async Task Close()
    {
        await NavigationService.GoBack();
        OnMapClosing(EventArgs.Empty);
        
    }

    internal void SetInitialPosition(MapLatLng pos)
    {
        SelectedPagePosition = pos;
        FinalPosition = pos;
    }
}
