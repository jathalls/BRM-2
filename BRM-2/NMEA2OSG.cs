// *  Copyright 2016 Justin A T Halls
//  *
//  *  This file is part of the Bat Recording Manager Project
// 
//         Licensed under the Apache License, Version 2.0 (the "License");
//         you may not use this file except in compliance with the License.
//         You may obtain a copy of the License at
// 
//             http://www.apache.org/licenses/LICENSE-2.0
// 
//         Unless required by applicable law or agreed to in writing, software
//         distributed under the License is distributed on an "AS IS" BASIS,
//         WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//         See the License for the specific language governing permissions and
//         limitations under the License.
using Convert = System.Convert;


namespace BRM_2;

public class NMEA2OSG
{
    private static readonly double deg2rad = Math.PI / 180;
    private static readonly double rad2deg = 180.0 / Math.PI;
    private const double ArcSecondsToRadians = Math.PI / (180.0 * 3600.0);
    private const double Airy1830SemiMajorAxis = 6377563.396;
    private const double Airy1830SemiMinorAxis = 6356256.909;
    private const double Wgs84SemiMajorAxis = 6378137.0;
    private const double Wgs84SemiMinorAxis = 6356752.3141;
    public double deciLat;
    public double deciLon;
    public string ngr { get; set; }

    private string[][] prefixes = new string[][] {
        new string[]{"SV", "SW", "SX", "SY", "SZ", "TV", "TW" },
         new string[]{"SQ", "SR", "SS", "ST", "SU", "TQ", "TR" },
         new string[]{"SL", "SM", "SN", "SO", "SP", "TL", "TM" },
         new string[]{"SF", "SG", "SH", "SJ", "SK", "TF", "TG" },
         new string[]{"SA", "SB", "SC", "SD", "SE", "TA", "TB" },
         new string[]{"NV", "NW", "NX", "NY", "NZ", "OV", "OW" },
         new string[]{"NQ", "NR", "NS", "NT", "NU", "OQ", "OR" },
         new string[]{"NL", "NM", "NN", "NO", "NP", "OL", "OM" },
         new string[]{"NF", "NG", "NH", "NJ", "NK", "OF", "OG" },
         new string[]{"NA", "NB", "NC", "ND", "NE", "OA", "OB" },
         new string[]{"HV", "HW", "HX", "HY", "HZ", "JV", "JW" },
         new string[]{"HQ", "HR", "HS", "HT", "HU", "JQ", "JR" },
         new string[]{"HL", "HM", "HN", "HO", "HP", "JL", "JM" }
    };

    private Tuple<double,double> MapRef2EN(string mapRef)
    {
        mapRef = mapRef.Trim().ToUpper().Replace(" ", "");
        var test = _validateGridRef(mapRef);
        if (!test)
        {
            Debug.WriteLine("Invalid map reference "+mapRef);
            return null;
        }
        var gridLetters = "VWXYZQRSTULMNOPFGHJKABCDE";

        
        
        var majorEasting = gridLetters.IndexOf(mapRef [0]) % 5 * 500000 - 1000000;
        var majorNorthing = Math.Floor(gridLetters.IndexOf(mapRef [0]) / 5.0d) * 500000 - 500000;

        var minorEasting = gridLetters.IndexOf(mapRef [1]) % 5 * 100000;
        var minorNorthing = Math.Floor(gridLetters.IndexOf(mapRef[1]) / 5.0d) * 100000;

        var i = (mapRef.Length - 2) / 2;
        var m = Math.Pow(10, 5 - i);

        int easting;
        if(!int.TryParse(mapRef.Substring(2, i), out easting)) easting = -1;
        int northing;
        if (!int.TryParse(mapRef.Substring(i + 2, i), out northing)) northing = -1;
        if(easting<0 || northing<0) return null;

        var e = majorEasting + minorEasting + (easting * m);
        var n = majorNorthing + minorNorthing + (northing * m);

        return (new Tuple<double,double >(e,n));
    }

    /**
     * Test whether a standard grid reference with a valid format has been provided.
     * param {string} gridref - The grid reference to be validated.
     */
    private bool _validateGridRef(string gridref)
    {
        if (string.IsNullOrEmpty(gridref)) return false;
        if (gridref.Length < 4) return false;

        const string validFirstLetters = "THJONS";
        const string validSecondLetters = "VWXYZQRSTULMNOPFGHJKABCDE";

        if (!validFirstLetters.Contains(gridref[0])) return false;
        if (!validSecondLetters.Contains(gridref[1])) return false;

        var digits = gridref.Substring(2);
        if (digits.Length < 2 || digits.Length > 10 || digits.Length % 2 != 0) return false;

        return digits.All(char.IsDigit);
    }


    // Processes WGS84 lat and lon in NMEA form 
    // 52�09.1461"N         002�33.3717"W
    public bool ParseNMEA(string nlat, string nlon, double height)
    {
        //grab the bit up to the �
        deciLat = Convert.ToDouble(nlat.Substring(0, nlat.IndexOf("�")));
        deciLon = Convert.ToDouble(nlon.Substring(0, nlon.IndexOf("�")));

        //remove that bit from the string now we've used it and the � symbol
        nlat = nlat.Substring(nlat.IndexOf("�") + 1);
        nlon = nlon.Substring(nlon.IndexOf("�") + 1);

        //grab the bit up to the " - divide by 60 to convert to degrees and add it to our double value
        deciLat += Convert.ToDouble(nlat.Substring(0, nlat.IndexOf("\""))) / 60;
        deciLon += Convert.ToDouble(nlon.Substring(0, nlat.IndexOf("\""))) / 60;

        //ok remove that now and just leave the compass direction
        nlat = nlat.Substring(nlat.IndexOf("\"") + 1);
        nlon = nlon.Substring(nlon.IndexOf("\"") + 1);

        // check for negative directions
        if (nlat == "S") deciLat = 0 - deciLat;
        if (nlon == "W") deciLon = 0 - deciLon;

        //now we can parse them
        return Transform(deciLat, deciLon, height);
    }

    // Processes WGS84 lat and lon in decimal form with S and W as -ve
    public bool Transform(double WGlat, double wGlon, double height)
    {
        //first off convert to radians
        var radWGlat = WGlat * deg2rad;
        var radWGlon = wGlon * deg2rad;

        /* these calculations were derived from the work of
         * Roger Muggleton (http://www.carabus.co.uk/) */

        /* quoting Roger Muggleton :-
         * There are many ways to convert data from one system to another, the most accurate 
         * being the most complex! For this example I shall use a 7 parameter Helmert 
         * transformation.
         * The process is in three parts: 
         * (1) Convert latitude and longitude to Cartesian coordinates (these also include height 
         * data, and have three parameters, X, Y and Z). 
         * (2) Transform to the new system by applying the 7 parameters and using a little maths.
         * (3) Convert back to latitude and longitude.
         * For the example we shall transform a GRS80 location to Airy, e.g. a GPS reading to 
         * the Airy spheroid.
         * The following code converts latitude and longitude to Cartesian coordinates. The 
         * input parameters are: WGS84 latitude and longitude, axis is the GRS80/WGS84 major 
         * axis in metres, ecc is the eccentricity, and height is the height above the 
         *  ellipsoid.
         *  v = axis / (Math.sqrt (1 - ecc * (Math.pow (Math.sin(lat), 2))));
         *  x = (v + height) * Math.cos(lat) * Math.cos(lon);
         * y = (v + height) * Math.cos(lat) * Math.sin(lon);
         * z = ((1 - ecc) * v + height) * Math.sin(lat);
         * The transformation requires the 7 parameters: xp, yp and zp correct the coordinate 
         * origin, xr, yr and zr correct the orientation of the axes, and sf deals with the 
         * changing scale factors. */

        //these are the values for WGS86(GRS80) to OSGB36(Airy)
        double a = 6378137; // WGS84_AXIS
        var e = 0.00669438037928458; // WGS84_ECCENTRIC
        var h = height; // height above datum  (from GPS GGA sentence)
        var a2 = 6377563.396; //OSGB_AXIS
        var e2 = 0.0066705397616; // OSGB_ECCENTRIC 
        var xp = -446.448;
        var yp = 125.157;
        var zp = -542.06;
        var xr = -0.1502;
        var yr = -0.247;
        var zr = -0.8421;
        var s = 20.4894;

        // convert to cartesian; lat, lon are radians
        var sf = s * 0.000001;
        var v = a / Math.Sqrt(1 - e * (Math.Sin(radWGlat) * Math.Sin(radWGlat)));
        var x = (v + h) * Math.Cos(radWGlat) * Math.Cos(radWGlon);
        var y = (v + h) * Math.Cos(radWGlat) * Math.Sin(radWGlon);
        var z = ((1 - e) * v + h) * Math.Sin(radWGlat);

        // transform cartesian
        var xrot = xr / 3600 * deg2rad;
        var yrot = yr / 3600 * deg2rad;
        var zrot = zr / 3600 * deg2rad;
        var hx = x + x * sf - y * zrot + z * yrot + xp;
        var hy = x * zrot + y + y * sf - z * xrot + yp;
        var hz = -1 * x * yrot + y * xrot + z + z * sf + zp;

        // Convert back to lat, lon
        var newLon = Math.Atan(hy / hx);
        var p = Math.Sqrt(hx * hx + hy * hy);
        var newLat = Math.Atan(hz / (p * (1 - e2)));
        v = a2 / Math.Sqrt(1 - e2 * (Math.Sin(newLat) * Math.Sin(newLat)));
        var errvalue = 1.0;
        double lat0 = 0;
        while (errvalue > 0.001)
        {
            lat0 = Math.Atan((hz + e2 * v * Math.Sin(newLat)) / p);
            errvalue = Math.Abs(lat0 - newLat);
            newLat = lat0;
        }

        //convert back to degrees
        newLat = newLat * rad2deg;
        newLon = newLon * rad2deg;

        //convert lat and lon (OSGB36)  to OS 6 figure northing and easting
        return LLtoNE(newLat, newLon);
    }

    //converts lat and lon (OSGB36)  to OS 6 figure northing and easting
    private bool LLtoNE(double lat, double lon)
    {
        var phi = lat * deg2rad; // convert latitude to radians
        var lam = lon * deg2rad; // convert longitude to radians
        var a = 6377563.396; // OSGB semi-major axis
        var b = 6356256.91; // OSGB semi-minor axis
        double e0 = 400000; // easting of false origin
        double n0 = -100000; // northing of false origin
        var f0 = 0.9996012717; // OSGB scale factor on central meridian
        var e2 = 0.0066705397616; // OSGB eccentricity squared
        var lam0 = -0.034906585039886591; // OSGB false east
        var phi0 = 0.85521133347722145; // OSGB false north
        var af0 = a * f0;
        var bf0 = b * f0;

        // easting
        var slat2 = Math.Sin(phi) * Math.Sin(phi);
        var nu = af0 / Math.Sqrt(1 - e2 * slat2);
        var rho = nu * (1 - e2) / (1 - e2 * slat2);
        var eta2 = nu / rho - 1;
        var p = lam - lam0;
        var IV = nu * Math.Cos(phi);
        var clat3 = Math.Pow(Math.Cos(phi), 3);
        var tlat2 = Math.Tan(phi) * Math.Tan(phi);
        var V = nu / 6 * clat3 * (nu / rho - tlat2);
        var clat5 = Math.Pow(Math.Cos(phi), 5);
        var tlat4 = Math.Pow(Math.Tan(phi), 4);
        var VI = nu / 120 * clat5 * (5 - 18 * tlat2 + tlat4 + 14 * eta2 - 58 * tlat2 * eta2);
        var east = e0 + p * IV + Math.Pow(p, 3) * V + Math.Pow(p, 5) * VI;

        // northing
        var n = (af0 - bf0) / (af0 + bf0);
        var M = Marc(bf0, n, phi0, phi);
        var I = M + n0;
        var II = nu / 2 * Math.Sin(phi) * Math.Cos(phi);
        var III = nu / 24 * Math.Sin(phi) * Math.Pow(Math.Cos(phi), 3) *
                  (5 - Math.Pow(Math.Tan(phi), 2) + 9 * eta2);
        var IIIA = nu / 720 * Math.Sin(phi) * clat5 * (61 - 58 * tlat2 + tlat4);
        var north = I + p * p * II + Math.Pow(p, 4) * III + Math.Pow(p, 6) * IIIA;

        // make whole number values
        east = Math.Round(east); // round to whole number
        north = Math.Round(north); // round to whole number

        // Notify the calling application of the change
        NorthingEastingReceived?.Invoke(north, east);

        // convert to nat grid ref
        return NE2NGR(east, north);
    }

    // a function used in LLtoNE  - that's all I know about it
    private static double Marc(double bf0, double n, double phi0, double phi)
    {
        return bf0 * ((1 + n + 5 / 4 * (n * n) + 5 / 4 * (n * n * n)) * (phi - phi0)
                      - (3 * n + 3 * (n * n) + 21 / 8 * (n * n * n)) * Math.Sin(phi - phi0) * Math.Cos(phi + phi0)
                      + (15 / 8 * (n * n) + 15 / 8 * (n * n * n)) * Math.Sin(2 * (phi - phi0)) *
                      Math.Cos(2 * (phi + phi0))
                      - 35 / 24 * (n * n * n) * Math.Sin(3 * (phi - phi0)) * Math.Cos(3 * (phi + phi0)));
    }

    //convert 12 (6e & 6n) figure numeric to letter and number grid system
    private bool NE2NGR(double east, double north)
    {
        var eX = east / 500000;
        var nX = north / 500000;
        var tmp = Math.Floor(eX) - 5.0 * Math.Floor(nX) +
                  17.0; //Math.Floor Returns the largest integer less than or equal to the specified number.
        nX = 5 * (nX - Math.Floor(nX));
        eX = 20 - 5.0 * Math.Floor(nX) + Math.Floor(5.0 * (eX - Math.Floor(eX)));
        if (eX > 7.5) eX = eX + 1;
        if (tmp > 7.5) tmp = tmp + 1;
        var eing = Convert.ToString(east);
        var ning = Convert.ToString(north);
        var lnth = eing.Length;
        eing = eing.Substring(lnth - 5);
        lnth = ning.Length;
        ning = ning.Substring(lnth - 5);
        ngr = Convert.ToString((char)(tmp + 65)) + Convert.ToString((char)(eX + 65)) + " " + eing + " " + ning;
        if (!string.IsNullOrWhiteSpace(ngr)) return true;
        return false;
        // Notify the calling application of the change
        //if (NatGridRefReceived != null) NatGridRefReceived(ngr);
    }

    public static string GPS2UKMapRef(double latitude, double longitude, double height)
    {
        string ngr = "";
        var nmea2osg = new NMEA2OSG();
        var transformed=nmea2osg.Transform(latitude, longitude, height);
        ngr = nmea2osg.ngr;

        return (ngr);
    }

    public static (double latitude,double longitude)? ConvertmapReferenceToWGS4(string mapref)
    {
        NMEA2OSG nMEA2OSG = new NMEA2OSG();
        Tuple<double, double>? eastNorth = nMEA2OSG.MapRef2EN(mapref);
        if(eastNorth==null) return null;
        return EastingNorthingToWgs84(eastNorth.Item1, eastNorth.Item2);
    }

    private static (double latitude, double longitude) EastingNorthingToWgs84(double easting, double northing)
    {
        var (osgbLatitude, osgbLongitude) = EastingNorthingToOsgb36(easting, northing);
        var airyCartesian = LatLonToCartesian(osgbLatitude, osgbLongitude, 0.0d, Airy1830SemiMajorAxis, Airy1830SemiMinorAxis);
        var wgs84Cartesian = HelmertTransform(
            airyCartesian,
            tx: 446.448,
            ty: -125.157,
            tz: 542.060,
            rxArcSeconds: 0.1502,
            ryArcSeconds: 0.2470,
            rzArcSeconds: 0.8421,
            scalePpm: -20.4894);

        return CartesianToLatLon(wgs84Cartesian, Wgs84SemiMajorAxis, Wgs84SemiMinorAxis);
    }

    private static (double latitude, double longitude) EastingNorthingToOsgb36(double easting, double northing)
    {
        const double scaleFactor = 0.9996012717;
        var falseOriginLatitude = 49.0 * deg2rad;
        var falseOriginLongitude = -2.0 * deg2rad;
        const double falseOriginNorthing = -100000.0;
        const double falseOriginEasting = 400000.0;

        var eccentricitySquared = 1 - (Airy1830SemiMinorAxis * Airy1830SemiMinorAxis) / (Airy1830SemiMajorAxis * Airy1830SemiMajorAxis);
        var n = (Airy1830SemiMajorAxis - Airy1830SemiMinorAxis) / (Airy1830SemiMajorAxis + Airy1830SemiMinorAxis);
        var lat = falseOriginLatitude;
        var meridionalArc = 0.0d;

        while (northing - falseOriginNorthing - meridionalArc >= 0.00001)
        {
            lat = (northing - falseOriginNorthing - meridionalArc) / (Airy1830SemiMajorAxis * scaleFactor) + lat;
            meridionalArc = Marc(Airy1830SemiMinorAxis * scaleFactor, n, falseOriginLatitude, lat);
        }

        var sinLat = Math.Sin(lat);
        var cosLat = Math.Cos(lat);
        var tanLat = Math.Tan(lat);
        var nu = Airy1830SemiMajorAxis * scaleFactor / Math.Sqrt(1 - eccentricitySquared * sinLat * sinLat);
        var rho = Airy1830SemiMajorAxis * scaleFactor * (1 - eccentricitySquared) /
                  Math.Pow(1 - eccentricitySquared * sinLat * sinLat, 1.5);
        var etaSquared = nu / rho - 1;
        var tan2Lat = tanLat * tanLat;
        var tan4Lat = tan2Lat * tan2Lat;
        var tan6Lat = tan4Lat * tan2Lat;
        var secLat = 1.0d / cosLat;
        var deltaEast = easting - falseOriginEasting;

        var vii = tanLat / (2 * rho * nu);
        var viii = tanLat / (24 * rho * Math.Pow(nu, 3)) * (5 + 3 * tan2Lat + etaSquared - 9 * tan2Lat * etaSquared);
        var ix = tanLat / (720 * rho * Math.Pow(nu, 5)) * (61 + 90 * tan2Lat + 45 * tan4Lat);
        var x = secLat / nu;
        var xi = secLat / (6 * Math.Pow(nu, 3)) * (nu / rho + 2 * tan2Lat);
        var xii = secLat / (120 * Math.Pow(nu, 5)) * (5 + 28 * tan2Lat + 24 * tan4Lat);
        var xiia = secLat / (5040 * Math.Pow(nu, 7)) * (61 + 662 * tan2Lat + 1320 * tan4Lat + 720 * tan6Lat);

        var latitude = lat - vii * deltaEast * deltaEast
                           + viii * Math.Pow(deltaEast, 4)
                           - ix * Math.Pow(deltaEast, 6);
        var longitude = falseOriginLongitude + x * deltaEast
                                              - xi * Math.Pow(deltaEast, 3)
                                              + xii * Math.Pow(deltaEast, 5)
                                              - xiia * Math.Pow(deltaEast, 7);

        return (latitude * rad2deg, longitude * rad2deg);
    }

    private static (double x, double y, double z) LatLonToCartesian(double latitude, double longitude, double height, double semiMajorAxis, double semiMinorAxis)
    {
        var latitudeRadians = latitude * deg2rad;
        var longitudeRadians = longitude * deg2rad;
        var eccentricitySquared = 1 - (semiMinorAxis * semiMinorAxis) / (semiMajorAxis * semiMajorAxis);
        var sinLatitude = Math.Sin(latitudeRadians);
        var nu = semiMajorAxis / Math.Sqrt(1 - eccentricitySquared * sinLatitude * sinLatitude);

        var x = (nu + height) * Math.Cos(latitudeRadians) * Math.Cos(longitudeRadians);
        var y = (nu + height) * Math.Cos(latitudeRadians) * Math.Sin(longitudeRadians);
        var z = ((1 - eccentricitySquared) * nu + height) * sinLatitude;

        return (x, y, z);
    }

    private static (double x, double y, double z) HelmertTransform(
        (double x, double y, double z) point,
        double tx,
        double ty,
        double tz,
        double rxArcSeconds,
        double ryArcSeconds,
        double rzArcSeconds,
        double scalePpm)
    {
        var scale = 1 + (scalePpm * 1e-6);
        var rx = rxArcSeconds * ArcSecondsToRadians;
        var ry = ryArcSeconds * ArcSecondsToRadians;
        var rz = rzArcSeconds * ArcSecondsToRadians;

        var x = tx + point.x * scale - point.y * rz + point.z * ry;
        var y = ty + point.x * rz + point.y * scale - point.z * rx;
        var z = tz - point.x * ry + point.y * rx + point.z * scale;

        return (x, y, z);
    }

    private static (double latitude, double longitude) CartesianToLatLon((double x, double y, double z) point, double semiMajorAxis, double semiMinorAxis)
    {
        var eccentricitySquared = 1 - (semiMinorAxis * semiMinorAxis) / (semiMajorAxis * semiMajorAxis);
        var p = Math.Sqrt(point.x * point.x + point.y * point.y);
        var latitude = Math.Atan2(point.z, p * (1 - eccentricitySquared));
        double previousLatitude;

        do
        {
            previousLatitude = latitude;
            var sinLatitude = Math.Sin(latitude);
            var nu = semiMajorAxis / Math.Sqrt(1 - eccentricitySquared * sinLatitude * sinLatitude);
            latitude = Math.Atan2(point.z + eccentricitySquared * nu * sinLatitude, p);
        } while (Math.Abs(latitude - previousLatitude) > 1e-12);

        var longitude = Math.Atan2(point.y, point.x);
        return (latitude * rad2deg, longitude * rad2deg);
    }

    #region Delegates

    public delegate void NorthingEastingReceivedEventHandler(double northing, double easting);

    public delegate void NatGridRefReceivedEventHandler(string ngr);

    #endregion

    #region Events

    public event NorthingEastingReceivedEventHandler NorthingEastingReceived;
    public event NatGridRefReceivedEventHandler NatGridRefReceived;

    #endregion
}
