namespace BRM_2;
#region GPSLocation

/// <summary>
///     A class to hold details of a particular location
/// </summary>
public class GPSLocation : INotifyPropertyChanged
{

    public event PropertyChangedEventHandler? PropertyChanged;

    protected  void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    //public static Location defaultLocation = new Location(51.79603, -0.10754);// somwhere in Panshanger Park car park


    /// <summary>
    ///     Constructor for a Location class.  Paraeters are GPS co-ordinates for
    ///     Latitude and Longitude as doubles, and an optional name and 3 or 4 letter identification code
    /// </summary>
    /// <param name="latitude"></param>
    /// <param name="longitude"></param>
    /// <param name="name"></param>
    /// <param name="ID"></param>
    public GPSLocation(double latitude, double longitude, string name = "", string ID = ""):base()
    {
        m_Name = name;
        m_ID = ID;
        LatLong = (latitude, longitude);
        //m_GridRef = ConvertGPStoGridRef(latitude, longitude);
       
    }

    public GPSLocation((double latitude,double longitude) latlong,string name="",string ID = ""):base()
    {
        m_Name = name;
        m_ID = ID;
        this.LatLong = latlong;
        
        
    }

    /// <summary>
    /// Given a start date and time, and the fully qualified name of a .wav or .zc (or other) file,
    /// looks for a .GPS file in the same location and interpolates between the trackpoint just before
    /// the given datetime and the trackpoint immediately after the given date time.  If the datetime is not
    /// represented in the gps file then other gps files will also be examined (by default the largest is used)
    /// and if there is no valid result a new GPSLocation of 0,0 will be returned.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="filename"></param>
    public GPSLocation(DateTime start,string filename) : base()
    {
        string? folder=Path.GetDirectoryName(filename);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            List<FileInfo> fileData = new List<FileInfo>();
            
            var gpxFiles = Directory.EnumerateFiles(folder, "*.gpx");
            if(gpxFiles!=null && gpxFiles.Count() > 0)
            {
                foreach (var file in gpxFiles)
                {
                    fileData.Add(new FileInfo(file));
                }

                gpxFiles = from fileInfo in fileData
                           orderby fileInfo.Length
                           select (fileInfo.FullName);
                foreach(var file in gpxFiles)
                {
                    var handler = new GpxHandler(file);
                    var location = handler.GetLocation(start);
                    if (location!=null && location.Count()>1 && GPSLocation.IsValidLocation(location[0], location[1]))
                    {
                        m_Latitude = (double)location[0];
                        m_Longitude = (double)location[1];
                        GetWhat3Words();
                        break;
                    }
                }

            }
        }
    }

    public GPSLocation()
    {
        m_Latitude = 51.0;
        m_Longitude = -0.2;
        //m_GridRef = "";
        m_Name = "";
        m_ID = "";
        What3Words = "";
    }

    /// <summary>
    ///     Alternative constructoer for a Location class object.
    ///     The parameters are a strig defining the WGS84 location, and
    ///     an optional name and 3 or 4 letter identification code.
    ///     The string should be in the format:-
    ///     nn.nnnnn,N,mmm.mmmmm,W[,alt]
    /// </summary>
    /// <param name="WGS84AsciiLocation"></param>
    /// <param name="name"></param>
    /// <param name="id"></param>
    public GPSLocation(string WGS84AsciiLocation, string name = "", string id = ""):base()
    {
        if (ConvertToLatLong(WGS84AsciiLocation, out var latitude, out var longitude))
        {
            m_Name = name;
            m_ID = id;
            LatLong =(latitude,longitude);
           
            
        }
    }

    //private string _m_GridRef = "";
    /// <summary>
    ///     The UK grid reference for the location if possible, calculated from the
    ///     latitude and longitude fields
    /// </summary>
    /*public string m_GridRef 
    {
        get { return _m_GridRef; }
        set
        {
            _m_GridRef= value;
            
            OnPropertyChanged(nameof(m_GridRef));

        }
    }*/
    /*
    private static (double lat, double longit) ConvertGridRef2GPS(string value)
    {
        (double lat, double longit) result = (AvMap.defaultLocation.Lat,AvMap.defaultLocation.Lng);
        if (string.IsNullOrWhiteSpace(value)) return result;
        try
        {
            OSRef osRef = new OSRef(value);
            LatLng latLng = osRef.ToLatLng();
            latLng.ToWGS84();
            result = ((latLng.Latitude) , (latLng.Longitude) );
        }
        catch (Exception)
        {
            return result;
        }

        return (result);
    }*/

    private string _m_ID = "";
    /// <summary>
    ///     A three or four letter ID for the location.  May be null or empty
    /// </summary>
    public string m_ID
    {
        get { return _m_ID; }
        set
        {
            _m_ID= value;
            OnPropertyChanged(nameof(m_ID));
        }
    }

    private double _m_Latitude = AvMap.defaultLocation.Lat;
    /// <summary>
    ///     GPS latitude as a double
    /// </summary>
    public double m_Latitude {
        get { return _m_Latitude; }
        set {
            if (value != _m_Latitude)
            {
                _m_Latitude = value;
                OnPropertyChanged(nameof(m_Latitude));
                //m_GridRef = ConvertGPStoGridRef(m_Latitude, m_Longitude);
                //GetWhat3Words();
                //OnPropertyChanged(nameof(m_GridRef));

            }
        }
    }

    private double _m_Longitude = AvMap.defaultLocation.Lng;
    /// <summary>
    ///     GPS longitude as a double
    /// </summary>
    public double m_Longitude 
    {
        get { return _m_Longitude; }
        set {
            if (value != _m_Longitude)
            {
                _m_Longitude = value;
                
                OnPropertyChanged(nameof(m_Longitude));
                //m_GridRef = ConvertGPStoGridRef(m_Latitude, m_Longitude);
                //GetWhat3Words();
                //OnPropertyChanged(nameof(m_GridRef));
            }
        }
    }

    
    public (double latitude, double longitude) LatLong
    {
        get { return ((m_Latitude, m_Longitude)); }
        set
        {
            if (value.latitude != _m_Latitude || value.longitude != _m_Longitude)
            {
                
                _m_Latitude = value.latitude;
                _m_Longitude = value.longitude;
                OnPropertyChanged(nameof(m_Latitude));
                OnPropertyChanged(nameof(m_Longitude));
                if (isValidLocation)
                {
                    //_m_GridRef = ConvertGPStoGridRef(m_Latitude, m_Longitude);
                    //GetWhat3Words();
                    //OnPropertyChanged(nameof(m_GridRef));
                    

                }
            }
        }
    }

    private string _what3Words = "";
    public string What3Words
    {
        get { return _what3Words; }
        set
        {
            _what3Words = value;
            OnPropertyChanged(nameof(What3Words));
        }
    }

    /// <summary>
    ///     The common name for the location.  May be null or empty.
    /// </summary>
    private string _m_Name = "";
    public string m_Name
    {
        get { return _m_Name; }
        set
        {
            _m_Name = value;
            OnPropertyChanged(nameof(m_Name));
        }
    }
    /*
    /// <summary>
    ///     Converts a GPS position in the form of latitude and longitude into a UK grid reference.
    ///     May not be precise because altitude is not take into account in the conversion, but is
    ///     generally close enough.
    /// </summary>
    /// <param name="latitude"></param>
    /// <param name="longitude"></param>
    /// <returns></returns>
    public static string ConvertGPStoGridRef(double latitude, double longitude)
    {
        var result = "";

        if (!IsValidLocation(latitude,longitude)) return (result); // not valid for this location

        var nmea2OSG = new NMEA2OSG();
        if (latitude >= -48.0d && latitude <= 63.0d && longitude >= -12.0d && longitude <= 3.0d) // generous limits for UK and Ireland
            // we have valid latitudes and longitudes
            // for now just assume they are in the OS grid ref acceptable area

            if (nmea2OSG.Transform(latitude, longitude, 0.0d))
                result = nmea2OSG.ngr;

        return result;
    }*/

    internal bool isValidLocation
    {
        get
        {
            return (GPSLocation.IsValidLocation((decimal?)m_Latitude, (decimal?)m_Longitude));
        }
    }

    internal static bool IsValidLocation(double? latitude, double? longitude)
    {
        if (double.IsNaN(latitude??double.NaN) || double.IsNaN(longitude??double.NaN)) return false;
        return (IsValidLocation((decimal?)latitude,(decimal?)longitude));
    }

    /// <summary>
    /// Given a pair of decimal? checks to see if these represent a valid GPS location
    /// which is not 0,0
    /// </summary>
    /// <param name="locationGPSLatitude"></param>
    /// <param name="locationGPSLongitude"></param>
    /// <returns></returns>
    internal static bool IsValidLocation(decimal? locationGPSLatitude, decimal? locationGPSLongitude)
    {
        
        if (locationGPSLatitude == null || locationGPSLongitude == null) return false;
        if((double)(locationGPSLatitude??0.0m)==AvMap.defaultLocation.Lat && (double)(locationGPSLongitude??0.0m)==AvMap.defaultLocation.Lng) return false;
        
        
        if (Math.Abs(locationGPSLatitude.Value) > 90.0m) return false;
        if (Math.Abs(locationGPSLongitude.Value) > 180.0m) return false;

        if (locationGPSLatitude.Value == 0.0m && locationGPSLongitude.Value == 0.0m) return false;

        return (true);
    }

    internal static bool IsValidLocation(Location? location)
    {
        if(location== null) return false;
        return(IsValidLocation(location?.Lat??double.NaN, location?.Lng??double.NaN));
    }

    internal static bool IsValidLocation(string strLat, string strLong)
    {
        if (!string.IsNullOrWhiteSpace(strLat) && !string.IsNullOrWhiteSpace(strLong))
        {
            if (double.TryParse(strLat, out var dLat) && double.TryParse(strLong, out var dlong))
            {
                return (IsValidLocation(dLat, dlong));
            }
        }
        return (false);
    }

    /// <summary>
    ///     Valids the coordinates as GPS lat and long in text format and returns those
    ///     coordinates as a Location or null if they are not valid
    /// </summary>
    /// <param name="latit">
    ///     The latitude
    /// </param>
    /// <param name="longit">
    ///     The longitude
    /// </param>
    /// <returns>
    /// </returns>
    internal static Location? ValidCoordinates(string latit, string longit)
    {
        Location? result = null;
        if (!string.IsNullOrWhiteSpace(latit) && !string.IsNullOrWhiteSpace(longit))
        {
            if (double.TryParse(latit, out var dLat) && double.TryParse(longit, out var dlong)){
                result = ValidCoordinates(new Location(dLat, dlong));
            }
        }

        return result;
    }

    /// <summary>
    ///     Valids the coordinates in the location as valid GPS coordinates and returns the valid
    ///     Location or null if they are not valid.
    /// </summary>
    /// <param name="location">
    ///     The last selected location.
    /// </param>
    /// <returns>
    /// </returns>
    internal static Location? ValidCoordinates(Location? location)
    {
        Location? result = null;
        if (location != null && IsValidLocation(location))
           result = location;
        return result;
    }

    /// <summary>
    ///     Converts a string in the format "blah nn.nnnnn,N,mmm.mmmmm,W[,alt]
    ///     into a latitude and longitude pair in the form of doubles
    /// </summary>
    /// <param name="wGS84AsciiLocation"></param>
    /// <param name="latitude"></param>
    /// <param name="longitude"></param>
    /// <returns></returns>
    private bool ConvertToLatLong(string WGS84AsciiLocation, out double latitude, out double longitude)
    {
        var result = false;
        latitude = 200.0d;
        longitude = 200.0d;
        var pattern = @"WGS84,([0-9.-]*),?([NS]?),([0-9.-]*),?([WE]?)"; // e.g. WGS84,51.74607,N,0.26183,W
        var match = Regex.Match(WGS84AsciiLocation, pattern);
        if (match.Success && match.Groups.Count >= 5)
        {
            if (double.TryParse(match.Groups[1].Value, out var dd)) latitude = dd;
            dd = -1.0d;
            if (double.TryParse(match.Groups[3].Value, out dd)) longitude = dd;
            if (match.Groups[2].Value.Contains("S")) latitude = 0.0d - latitude;
            if (match.Groups[4].Value.Contains("W")) longitude = 0.0d - longitude;
        }

        if (latitude < 200.0d && longitude < 200.0d) result = true;
        return result;
    }

    public void GetWhat3Words()
    {/*
        if (!string.IsNullOrWhiteSpace(What3Words)) return;
        string words = "";
        string place = "";
        var wrapper = new What3WordsV3(APIKeys.What3WordsApiKey);

        var result = await wrapper.ConvertTo3WA(new what3words.dotnet.wrapper.models.Coordinates(m_Latitude, m_Longitude)).RequestAsync();
        if (result.IsSuccessful)
        {
            //Debug.WriteLine($"W3W:-({m_Latitude},{m_Longitude})->{result.IsSuccessful}: {result.Data.Words}/{result.Data.NearestPlace}");

            

            words = result.Data.Words;
            place = result.Data.NearestPlace;

            //getDataFromJson(result.Data, out  words, out place);

        }
        //headerFile.what3words = words;
        What3Words = words;
        OnPropertyChanged(nameof(What3Words));
        if (string.IsNullOrWhiteSpace(m_Name))
        {
            _m_Name = place;
            OnPropertyChanged(nameof(m_Name));
            if (!string.IsNullOrWhiteSpace(place))
            {
                if (place.Length >= 3)
                {
                    m_ID = m_Name.Substring(0, 3).ToUpper();
                }
                else
                {
                    m_ID = place.ToUpper();
                    while (m_ID.Length < 3) m_ID += "_";
                }
            }
            else { m_ID = "UNK"; }
        }
        */


    }
    /*
    internal static Location? displayMapWindow(double lat, double longit)
    {
        AvMap map = new AvMap();
        map.isDialog = true;
        Location? location = null;
        //var map = new MapWindow(true);
        using (new WaitCursor())
        {



            if (IsValidLocation(lat, longit))
            {
                location = new Location(lat, longit);
            }
            else
            {
                location = new Location(AvMap.defaultLocation.Lat,AvMap.defaultLocation.Lng);
            }

            if (location != null)
            {
                map.Position = new GMap.NET.PointLatLng(location.Latitude, location.Longitude);
                map.SetRoute(new List<RecordingLocation>() { new RecordingLocation(location.Latitude,location.Longitude) });
                //map.Coordinates = location;
                //map.SetPushPin(location);
            }
        }
        Location? newLocation = null;

        if (map.ShowDialog() ?? false)
        {

            newLocation = new Location((map.RouteOrigin?.Lat)??AvMap.defaultLocation.Lat, (map.RouteOrigin?.Lng)??AvMap.defaultLocation.Lng);
            
        }

        if (IsValidLocation(newLocation))
        {
            location = newLocation;
        }
        
        return (location);
    }*/
    /*
    internal static GPSLocation? GetLocation(string thermalVideoLocation)
    {
        var result = DBAccess.GetLocations();
       Dictionary<string, double> evaluated = new System.Collections.Generic.Dictionary<string,double>();
        string bestLocation = thermalVideoLocation;
        double bestMatch = 0.0d;
        foreach(var loc in result)
        {
            var engine=new F23.StringSimilarity.NormalizedLevenshtein();
            var similarity = engine.Similarity(loc, thermalVideoLocation);
            if (similarity > bestMatch)
            {
                bestMatch = similarity;
                bestLocation = loc;
            }

        }
        if (bestMatch > 0.5)
        {
            var gpsLocation=DBAccess.GetGPSForLocation(bestLocation);
            if (gpsLocation != null)
            {
                return(gpsLocation);
            }
        }

        return (null);
    }*/
    /*
    internal static GPSLocation? GetLocationFromMapRef(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            var place = GPSLocation.ConvertGridRef2GPS(text);

            GPSLocation result = new GPSLocation(place);
            if (result.isValidLocation)
            {
                return (result);
            }

        }
        return (null);
    }*/

    private static bool inWhat3Words = false;
    internal static GPSLocation? GetLocationFrom3Words(string text)
    {
        if (!inWhat3Words)
        {
            inWhat3Words = true;
            if (!String.IsNullOrWhiteSpace(text))
            {
                var place = GetLocationFrom3Words(text);
                if (place?.isValidLocation??false)
                {
                    inWhat3Words = false;
                    return (place);
                }
            }
           
        }
        inWhat3Words = false;
        return (null);
    }

    internal static bool CloseTo(GPSLocation? gPSLocation, decimal latDec, decimal longDec)
    {
        bool result = false;
        if (gPSLocation == null || !GPSLocation.IsValidLocation(latDec, longDec)) return result;

        result = true;
        var lowLim = (decimal)gPSLocation.m_Latitude - 0.01m;
        var hiLim = (decimal)gPSLocation.m_Latitude + 0.01m;
        if (latDec < lowLim || latDec > hiLim) result = false;

        lowLim = (decimal)gPSLocation.m_Longitude - 0.01m;
        hiLim=(decimal)gPSLocation.m_Longitude+0.01m;
        if (latDec < lowLim || latDec > hiLim) result = false;


        return result;
    }
}

#endregion GPSLocation
