namespace BRM_2;
internal class Location
{
    public double Lat;
    public double Lng;

    public static Location defaultLocation = new Location() { Lat = 51.2, Lng = -0.1 };

    public Location() { }

    public Location(double lat, double lng) { Lat = lat; Lng = lng; }
}
