namespace BRM_2;
internal class GpxHandler
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="GpxHandler" /> class.
    /// Tries the location as aGPX filename or if not, if it is folder containing a .gpx file.
    /// If a GPX file is found loads the GPX data as an XDocument.
    /// </summary>
    /// <param name="gpxFileLocation">
    ///     The location.
    /// </param>
    public GpxHandler(string gpxFileLocation)
    {
        var filename = "";
        _gpxFileExists = false;
        if (string.IsNullOrWhiteSpace(gpxFileLocation)) return;
        //GPXData = new XDocument();
        //GPXData.Add(XElement.Parse("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\" ?>"));
        if (gpxFileLocation.ToUpper().EndsWith(".GPX"))
        {
            if (File.Exists(gpxFileLocation))
            {
                filename = gpxFileLocation;
                _gpxFileExists = true;
                //GPXData.Add(XElement.Load(Location));
            }
        }
        else
        {
            if (Directory.Exists(gpxFileLocation))
            {
                var gpxFileList = Directory.EnumerateFiles(Path.GetDirectoryName(gpxFileLocation), "*.gpx");
                //var GPXFileList= Directory.EnumerateFiles(Location, "*.GPX");
                //gpxFileList = gpxFileList.Concat<string>(GPXFileList);
                if (!gpxFileList.IsNullOrEmpty())
                {
                    filename = gpxFileList.FirstOrDefault();
                    foreach (var fname in gpxFileList)
                        if ((new FileInfo(fname)?.Length ?? 0) > (new FileInfo(filename)?.Length ?? 0))
                            filename = fname;

                    _gpxFileExists = true;
                    //GPXData.Add(XElement.Load(gpxFileList.FirstOrDefault()));
                }
                else
                {
                    //no gpx file, but we may have a gps.csv file which is formatted in lines of
                    // date, time, lat, [NS], long, [EW], folder\file, ID
                    var gpsFileName = Path.Combine(Path.GetDirectoryName(gpxFileLocation), "gps.csv");
                    if (File.Exists(gpsFileName))
                    {
                        var lines = File.ReadAllLines(gpsFileName);
                        if (lines?.Any() ?? false)
                        {
                            _gpxData = new XDocument(new XDeclaration("1.0", "UTF-8", "no"));
                            XElement trkpts = new XElement("trk", new XElement("name", "unknown location"));
                            XElement trkseg = new XElement("trkseg", "");
                            foreach (var line in lines)
                            {
                                if (line.StartsWith("DATE")) continue; // skip the header line
                                var parts = line.Split(',');
                                string date = parts[0];
                                string time = parts[1];
                                string lat = parts[2];
                                string northing = parts[3];
                                if (northing.Trim() == "S")
                                {
                                    lat = "-" + lat.Trim();
                                }
                                string longit = parts[4];
                                string westing = parts[5];
                                if (westing.Trim() == "W")
                                {
                                    longit = "-" + longit.Trim();
                                }
                                if (!DateTime.TryParse(date, out DateTime dateTime)) continue;
                                if (!TimeSpan.TryParse(time, out TimeSpan time2)) continue;
                                dateTime = dateTime + time2;
                                dateTime = dateTime.ToUniversalTime();

                                XElement trkpt = new XElement("trkpt", "");
                                trkpt.SetAttributeValue("lat",$"{lat:#0.00000}");
                                trkpt.SetAttributeValue("lon",$"{longit:0.00000}");
                                XElement ele = new XElement("ele", "00");
                                XElement xtim = new XElement("time", $"{dateTime:s}Z");
                                XElement spd = new XElement("speed", "0.00");
                                trkpt.Add(ele);
                                trkpt.Add(xtim);
                                trkpt.Add(spd);
                                trkseg.Add(trkpt);

                            }
                            trkpts.Add(trkseg);
                            _gpxData.Add(trkpts);
                            _gpxData.Save(Path.Combine(Path.GetDirectoryName(gpxFileLocation), "track.gpx"));
                            _gpxFileExists = true;
                            _gpxNamespace = GetGpxNameSpace();
                            return;
                        }
                    }
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(filename) && File.Exists(filename))
            try
            {
                _gpxData = new XDocument(
                    new XDeclaration("1.0", "UTF-8", "no"),
                    XElement.Load(filename)
                );
            }
            catch (Exception ex)
            {
                Tools.ErrorLog(ex.Message);
                _gpxFileExists = false;
            }

        if (_gpxFileExists) _gpxNamespace = GetGpxNameSpace();
    }

    public bool gpxFileExists
    {
        get
        {
            return (_gpxFileExists);
        }
    }

    public List<ValueTuple<decimal, decimal>> getAllTrackPoints(DateTime start = new DateTime(), DateTime end = new DateTime())
    {
        List<ValueTuple<decimal, decimal>> trackPointList = new List<ValueTuple<decimal, decimal>>();
        if (_gpxFileExists && _gpxData != null)
        {
            //XElement previous = null;
            var all = _gpxData.Descendants();

            // var trackPoints = GPXData.Descendants(gpxNamespace + "trkpt");
            var trackPoints = _gpxData.Descendants().Where(x => x.ToString().StartsWith("<trkpt"));

            //var trackPoints =
            //    from tp in GPXData.Descendants("trk")
            //   select (tp.Value);
            if (!trackPoints.IsNullOrEmpty())
                foreach (var trkpt in trackPoints)
                {
                    DateTime trkptTime = GetTrackPointTime(trkpt);
                    if (start.Ticks > 0L) // check for correct date
                    {
                        if (trkptTime < start || trkptTime >= end) continue;
                    }

                    var coords = GetGpsCoordinates(trkpt);
                    var coord = (latitude: coords[0], longitude: coords[1]);
                    trackPointList.Add(coord);
                }
        }

        return (trackPointList);
    }

    /// <summary>
    ///     Gets the location.
    /// </summary>
    /// <param name="time">
    ///     The time.
    /// </param>
    /// <returns>
    /// </returns>
    public ObservableCollection<decimal> GetLocation(DateTime time)
    {
        var result = new ObservableCollection<decimal>();
        if (_gpxFileExists && _gpxData != null)
        {
            if (time.Ticks == 0L) return new ObservableCollection<decimal>();

            var utcTime = time.ToUniversalTime();

            XElement? previous = null;
            var all = _gpxData.Descendants();

            // var trackPoints = GPXData.Descendants(gpxNamespace + "trkpt");
            var trackPoints = _gpxData.Descendants().Where(x => x.ToString().StartsWith("<trkpt"));
            var tps = trackPoints.Count();
            if (tps > 0)
            {
                ////Debug.WriteLine(tps + " trackpoints");

                ////Debug.WriteLine(trackPoints.First().Value);
            }

            //var trackPoints =
            //    from tp in GPXData.Descendants("trk")
            //   select (tp.Value);
            if (!trackPoints.IsNullOrEmpty())
                foreach (var trkpt in trackPoints)
                {
                    if (TrackPointIsEarlier(utcTime, trkpt))
                    {
                        previous = trkpt;
                        continue;
                    }

                    if (previous == null)
                    {
                        result = GetGpsCoordinates(trkpt);
                        break;
                    }

                    var offsetToPrevious = GetOffset(previous, utcTime);
                    var offsetToNext = GetOffset(trkpt, utcTime);
                    result = GetGpsCoordinates(offsetToNext <= offsetToPrevious ? trkpt : previous);
                    break;
                }
        }

        return result;
    }

    internal static bool IsValidLocation(decimal Latitude, decimal Longitude)
    {
        return (IsValidLocation((double)Latitude, (double)Longitude));
    }

    internal static bool IsValidLocation(double Latitude, double Longitude)
    {
        if (Latitude == 0.0d && Longitude == 0.0d) return (false);
        if (Math.Abs(Latitude) > 90.0d) return (false);
        if (Math.Abs(Longitude) > 180.0d) return (false);
        return (true);
    }

    internal static bool IsValidLocation(GPSLocation m_Location)
    {
        if (m_Location == null) return (false);
        return (IsValidLocation(m_Location.m_Latitude, m_Location.m_Longitude));
    }

    /// <summary>
    ///     The GPX data
    /// </summary>
    private readonly XDocument _gpxData;

    /// <summary>
    ///     The GPX file exists
    /// </summary>
    private readonly bool _gpxFileExists;

    /// <summary>
    ///     The GPX namespace
    /// </summary>
    private readonly XNamespace _gpxNamespace;

    /// <summary>
    ///     Gets the GPS coordinates.
    /// </summary>
    /// <param name="trkpt">
    ///     The TRKPT.
    /// </param>
    /// <returns>
    /// </returns>
    private ObservableCollection<decimal> GetGpsCoordinates(XElement trkpt)
    {
        var strLat = trkpt?.Attribute("lat")?.Value??"";
        var strLong = trkpt?.Attribute("lon")?.Value ?? "";
        decimal.TryParse(strLat, out var dLat);
        decimal.TryParse(strLong, out var dLong);
        var result = new ObservableCollection<decimal>
        {
            dLat,
            dLong
        };
        return result;
    }

    /// <summary>
    ///     Load the namespace for a standard GPX document
    /// </summary>
    /// <returns>
    /// </returns>
    private XNamespace GetGpxNameSpace()
    {
        var gpx = XNamespace.Get("http://www.topografix.com/GPX/1/0");
        if (_gpxData != null)
        {
            var pattern = @"(xmlns=)(.http://\S+)\s";
            var result = Regex.Match(_gpxData.ToString(), pattern);
            if (result.Success && result.Groups.Count > 2)
            {
                var xmls = result.Groups[2].Value.Trim();
                gpx = XNamespace.Get(xmls);
            }
        }

        return gpx;
    }

    /// <summary>
    ///     Gets the offset.
    /// </summary>
    /// <param name="trackPoint">
    ///     The track point.
    /// </param>
    /// <param name="utcTime">
    ///     The UTC time.
    /// </param>
    /// <returns>
    /// </returns>
    private TimeSpan GetOffset(XElement trackPoint, DateTime utcTime)
    {
        var trackPointTime = GetTrackPointTime(trackPoint);
        return (trackPointTime - utcTime).Duration();
    }

    /// <summary>
    ///     Gets the track point time.
    /// </summary>
    /// <param name="trackPoint">
    ///     The track point.
    /// </param>
    /// <returns>
    /// </returns>
    private DateTime GetTrackPointTime(XElement trackPoint)
    {
        var strDateTimeElement =
            trackPoint.Descendants().First(x => x.ToString().StartsWith("<time")).Value;
        var tpTime = DateTime.Parse(strDateTimeElement);
        return tpTime;
    }

    /// <summary>
    ///     Tracks the point is earlier.
    /// </summary>
    /// <param name="utcTime">
    ///     The UTC time.
    /// </param>
    /// <param name="trkpt">
    ///     The TRKPT.
    /// </param>
    /// <returns>
    /// </returns>
    private bool TrackPointIsEarlier(DateTime utcTime, XElement trkpt)
    {
        var trackPointTime = GetTrackPointTime(trkpt).ToUniversalTime();
        if (trackPointTime < utcTime) return true;
        return false;
    }
}
