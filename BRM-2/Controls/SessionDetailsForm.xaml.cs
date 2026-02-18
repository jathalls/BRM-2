using System.Windows.Input;

namespace BRM_2.Controls;
public partial class SessionDetailsForm : ContentView, INotifyPropertyChanged
{
    public new event PropertyChangedEventHandler? PropertyChanged;

    protected override void OnPropertyChanged([CallerMemberName] string PropertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
    }

    public string sessionTag { get { return _sessionTag; } set { _sessionTag = value; OnPropertyChanged(); } }
    private string _sessionTag = "session tag";

    public DateTime startDate { get { return _startDate; } set { _startDate = value; OnPropertyChanged(); } }
    private DateTime _startDate = DateTime.Now;


    public DateTime endDate { get { return _endDate; } set { _endDate = value; OnPropertyChanged(); } }
    private DateTime _endDate = DateTime.Now;


    public string Location { get { return _location; } set { _location = value; OnPropertyChanged(); } }
    private string _location = "Unknown Location";

    public String Operator { get { return _operator; } set { _operator = value; OnPropertyChanged(); } }
    private string _operator = "Operator";

    public string Equipment { get { return _equipment; } set { _equipment = value; OnPropertyChanged(); } }
    private string _equipment = "Equipment";

    public string Microphone { get{return _microphone;} set { _microphone = value; OnPropertyChanged(); } }
        private string _microphone = "Microphone";

    public string Latitude
    {
        get { return _latitude.ToString(); }
        set
        {if(double.TryParse(value, out double lat)) _latitude = lat;
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
    private double _longitude= 200;

    private RecordingSessionEx _recordingSession = new RecordingSessionEx();

    public RecordingSessionEx recordingSession 
    {
        get {  return _recordingSession; }
        set {  _recordingSession = value;
            if (value != null)
            {
                //Debug.WriteLine($"form gets new {value.SessionTag}");
                sessionTag = value.SessionTag;
                startDate = value.SessionStart;

                endDate = value.SessionEnd;
                Location = value.Location;
                Operator = value.Operator;
                Equipment = value.Equipment;
                Microphone = value.microphone;
                Latitude = value.LocationGPSLatitude.ToString();
                Longitude = value.LocationGPSLongitude.ToString();
                weatherText = (value.Weather)??"";
                sessionNotes = value.SessionNotes;
            }


            
            
            OnPropertyChanged(); }
    }
    public ObservableCollection<string> microphoneList { get; internal set; }

    public ICommand PopupAcceptCommand { get; set; }
    public ICommand PopupDeclineCommand { get; set; }

    public MapLatLng? selectedPosition
    {
        get { return _selectedPosition; }
        set { 
            _selectedPosition = value; 
            Latitude=value?.Latitude.ToString()??"";
            Longitude=value?.Longitude.ToString()??"";
            OnPropertyChanged(); 
        }
    }
    private MapLatLng? _selectedPosition=new MapLatLng();

    public string sessionNotes { get { return _sessionNotes; }  set { _sessionNotes = value; OnPropertyChanged(); } }

    private string _sessionNotes = "";

    public string weatherText 
    {
        get { return _weatherText; }
        set { _weatherText = value; OnPropertyChanged(); }
    }

    private string _weatherText = "";
   
    public void DebugListing()
    {
        //Debug.WriteLine($"Tag={sessionTag}");
        //Debug.WriteLine($"startDate={startDate.ToString()}");
        
        //Debug.WriteLine($"microphones in list={microphoneList.Count}");
        foreach (string phone in microphoneList)
        {
            //Debug.WriteLine($"    <{phone}>");
        }
    }

    /// <summary>
    /// Uses the data in the bound elements to populate the RecordingSession instance which was
    /// originally used to initialise the form
    /// </summary>
    public RecordingSessionEx UpdateSession()
    {
        recordingSession.SessionTag = sessionTag;
        recordingSession.SessionStart = startDate;
        recordingSession.SessionEnd = endDate;
        recordingSession.Location = Location;
        recordingSession.Operator = Operator;
        recordingSession.Equipment = Equipment;
        recordingSession.microphone = Microphone;
        recordingSession.Weather = weatherText;
        recordingSession.SessionNotes = sessionNotes;
        recordingSession.LocationGPSLatitude = dLatitude;
        recordingSession.LocationGPSLongitude = dLongitude;

        return recordingSession;
    }

    private decimal dLatitude
    {
        get
        {
            decimal lat = 0;
            decimal.TryParse(Latitude,out lat);
            return lat;
        }
    }

    private decimal dLongitude
    {
        get
        {
            decimal lon = 0;
            decimal.TryParse(Longitude, out lon);
            return lon;
        }
    }

    private void GetWeatherButton_Clicked(object sender,EventArgs e)
    {
        GetWeather();
    }

    private void GetWeather()
    {
        if(double.TryParse(Latitude,out double Lat) && double.TryParse(Longitude,out double Longit))
        {
            string report=VCWeather.GetWeatherHistory(Lat, Longit, startDate)??"";
            weatherText = report;
        }
    }


    public List<string> items { get; set; } = new List<string>(new String[] { "one", "two", "three" });


    public string text { get; set; }
	public SessionDetailsForm()
	{
		InitializeComponent();
        
        Task task = populateLists();
        BindingContext = this;
        
        
	}

    public async Task  populateLists()
    {
        sfLocationCombo.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<string>(await DBAccess.GetAllLocationsListAsync());
        sfOperatorCombo.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<string>(await DBAccess.GetAllOperatorsListAsync());
        sfEquipmentCombo.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<string> ( await DBAccess.GetAllEquipmentListAsync());
        
        
        sfMicrophoneCombo.ItemsSource = new ObservableCollection<string>((await DBAccess.GetAllMicrophonesListAsync()).Distinct());

        
    }

    private MapSelectionPage mapPage;

    
    private async void MapButton_Clicked(Object sender, EventArgs e)
    {
        

        var mapVM = BRM_2.Navigation.ServiceProvider.GetService<MapSelectionVM>();
        mapPage =new MapSelectionPage(mapVM);
        mapVM.MapClosing += MapPage_MapClosing;
        
        MapLatLng? pos=new MapLatLng();
        if (double.TryParse(Latitude, out var latitudeValue))
        {
            
            pos.Latitude = latitudeValue;
        }
        else
        {
            pos.Latitude = (double)recordingSession.LocationGPSLatitude;
        }


        if(double.TryParse(Longitude, out var longitudeValue))
        {
            pos.Longitude = longitudeValue;
        }
        else
        {
            pos.Longitude = (double)recordingSession.LocationGPSLongitude;
        }

        selectedPosition = pos;

        mapPage.SetInitialPosition(selectedPosition);
        try
        {
            await Navigation.PushAsync(mapPage);
        }
        catch (Exception ex)
        {
            //Debug.WriteLine($"Failed to open map page {ex}");
        }

    }
    

    private void MapPage_MapClosing(object? sender, EventArgs e)
    {
        var pos=mapPage.GetFinalPosition();
        selectedPosition = pos;

    }

    private void StartDateButton_Clicked(object sender, EventArgs e)
    {
        sfStartDatePicker.IsOpen = true;
    }

    private void EndDateButton_Clicked(object obj, EventArgs e)
    {
        sfEndDatePicker.IsOpen = true;
    }

    public void Close()
    {
        mapPage?.Close();
    }

}
