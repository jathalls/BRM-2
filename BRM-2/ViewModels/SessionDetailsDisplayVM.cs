using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Syncfusion.Maui.Maps;
using ServiceProvider = BRM_2.Navigation.ServiceProvider;

namespace BRM_2.ViewModels;
public partial class SessionDetailsDisplayVM : ObservableObject
{
    private readonly NavigationService navigationService;

    public SessionDetailsDisplayVM(NavigationService navigationService)
    {
        this.navigationService = navigationService;
    }

    [ObservableProperty]
    private string _sessionTag = "session tag";

    [ObservableProperty]
    private DateTime _startDate = DateTime.Now;


    [ObservableProperty]
    private DateTime _endDate = DateTime.Now;


    [ObservableProperty]
    private string _location = "Unknown Location";

    [ObservableProperty]
    private string _operator = "Operator";

    [ObservableProperty]
    private string _equipment = "Equipment";

    [ObservableProperty]
    private string _microphone = "Microphone";

    [ObservableProperty]
    private string _fileLocation = "";

    [ObservableProperty]
    private Microsoft.Maui.Graphics.Color _fileLocationColor = Microsoft.Maui.Graphics.Colors.Red;

    public string Latitude
    {
        get { return _latitude.ToString(); }
        set
        {
            if (double.TryParse(value, out double lat)) _latitude = lat;
            else _latitude = 200;
            OnPropertyChanged();
        }
    }
    private double _latitude = 200;

    public string Longitude
    {
        get { return _longitude.ToString(); }
        set
        {
            if (double.TryParse(value, out double longit)) _longitude = longit;
            else _longitude = 200;
            OnPropertyChanged();
        }
    }
    private double _longitude = 200;


    private RecordingSessionTable _recordingSession = new RecordingSessionTable();

    public RecordingSessionTable recordingSession
    {
        get { return _recordingSession; }
        set
        {
            _recordingSession = value;
            if (value != null)
            {
                //Debug.WriteLine($"form gets new {value.SessionTag}");
                SessionTag = value.SessionTag;
                StartDate = value.SessionStart;

                EndDate = value.SessionEnd;
                Location = value.Location;
                Operator = value.Operator;
                Equipment = value.Equipment;
                Microphone = value.microphone;
                Latitude = value.LocationGPSLatitude.ToString();
                Longitude = value.LocationGPSLongitude.ToString();
                WeatherText = (value.Weather) ?? "";
                SessionNotes = value.SessionNotes;
                FileLocation = value.OriginalFilePath;
                if (Directory.Exists(Path.GetDirectoryName(FileLocation) ?? ""))
                {
                    FileLocationColor = Colors.Green;
                }
                else
                {
                    FileLocationColor = Colors.Red;
                }
            }




            OnPropertyChanged();
        }
    }
    public MapLatLng? selectedPosition
    {
        get { return _selectedPosition; }
        set
        {
            _selectedPosition = value;
            Latitude = value?.Latitude.ToString() ?? "";
            Longitude = value?.Longitude.ToString() ?? "";
            OnPropertyChanged();
        }
    }
    private MapLatLng? _selectedPosition = new MapLatLng();


    [ObservableProperty]
    private string _sessionNotes = "";

    [ObservableProperty]
    private string _weatherText = "";

    public MapLatLng selectedMapPosition
    {
        get { return _selectedPosition; }
        set { _selectedPosition = value; OnPropertyChanged(); }
    }

    [RelayCommand]
    public async void MapButton()
    {
        //googleMap();

        var mapSelectionPage = new MapSelectionPage(ServiceProvider.GetService<MapSelectionVM>());

        MapLatLng? pos = new MapLatLng();
        if (double.TryParse(Latitude, out var latitudeValue))
        {

            pos.Latitude = latitudeValue;
        }
        else
        {
            pos.Latitude = (double)recordingSession.LocationGPSLatitude;
        }


        if (double.TryParse(Longitude, out var longitudeValue))
        {
            pos.Longitude = longitudeValue;
        }
        else
        {
            pos.Longitude = (double)recordingSession.LocationGPSLongitude;
        }

        selectedPosition = pos;
        selectedMapPosition = pos;

        try
        {// need to set up Services and GetService to retrieve the NavigationService
            mapSelectionPage.SetInitialPosition(pos);

            await Shell.Current.Navigation.PushModalAsync(mapSelectionPage);

        }
        catch (HttpRequestException hex)
        {
            //Debug.WriteLine(hex.Message);
        }
    }

    public string text { get; set; }


    /// <summary>
    /// "data/3.0/onecall/timemachine?lat={locationGPSLatitude}&lon={locationGPSLongitude}" +
    ///                $"&dt={dt}&appid={APIKeys.OpenWeatherApiKey}&units=metric"
    /// </summary>
    private async void googleMap()
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://www.google.com/maps/");
                var callResult = await client.GetAsync($@"?api=1&query=51.2%2C-0.21");
                //Debug.WriteLine(callResult.ToString());
                if (callResult.IsSuccessStatusCode)
                {
                    var result = await callResult.Content.ReadAsStringAsync();
                }

            }

        }
        catch (Exception ex)
        {
            //Debug.WriteLine($"GWH:- {ex}");
        }
    }
}
