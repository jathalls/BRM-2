namespace BRM_2.Controls;
public partial class MapControl : ContentView, INotifyPropertyChanged
{
    public new event PropertyChangedEventHandler? PropertyChanged;

    protected override void OnPropertyChanged([CallerMemberName] string PropertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
    }

    public static readonly BindableProperty SelectedPositionProperty =
        BindableProperty.Create(
            nameof(SelectedPosition),
            typeof(MapLatLng),
            typeof(MapControl),
            default(MapLatLng?));

    public MapLatLng SelectedPosition 
    {
        get { return (MapLatLng)GetValue(SelectedPositionProperty); }
        set 
        {  
            if(value.Latitude==0 && value.Longitude == 0)
            {
                value.Latitude = 50.0;
                value.Longitude = 1.0;
            }

            SetValue(SelectedPositionProperty,value); 
            Debug.WriteLine($"\tTo Lat={value?.Latitude}, Long={value?.Longitude}");
            OnPropertyChanged(nameof(SelectedPositionProperty));
        }
    }

    public MapLatLng DesiredPosition { get; set; }= new MapLatLng() { Latitude = 51, Longitude = -0.2 };

    //private MapLatLng? _selectedLatLng = new MapLatLng() { Latitude = 51, Longitude = -0.2 };
	public MapControl()
	{
        SelectedPosition = new MapLatLng();
        SelectedPosition.Latitude = 51.0;
        SelectedPosition.Longitude = -0.2;
		InitializeComponent();
        //this.mapTile.UrlTemplate= "https://mt0.google.com/vt/lyrs=y&x={x}&y={y}&z={z}";
        //this.mapTile.UrlTemplate= "https://www.google.com/maps/@?api=1&map_action=map&center=-33.712206%2C150.311941&zoom=12&basemap=terrain";
        //this.mapTile.UrlTemplate= "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
        this.mapTile.UrlTemplate= "https://mts0.google.com/vt/lyrs=y&x={x}&y={y}&z={z}";
    }


    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        mapTile.Tapped += MapTile_Tapped;
        
        //mapTile.Center = SelectedPosition;
        mapTile.Center = DesiredPosition;
        SetSingleMarker(DesiredPosition);
        SelectedPosition = DesiredPosition;
        

    }

    private void MapTile_Tapped(object? sender, Syncfusion.Maui.Maps.TappedEventArgs e)
    {
        var pos = mapTile.GetLatLngFromPoint(new Point(e.Position.X, e.Position.Y));
        mapTile.Center = pos;
        SelectedPosition = pos;
        
        SetSingleMarker(pos);
        //Debug.WriteLine($"Map {mapTile.Center.Latitude}, {mapTile.Center.Longitude}");
        //Debug.WriteLine($"Pos={pos.Latitude}, {pos.Longitude}");
    }

    private void SetSingleMarker(MapLatLng pos)
    {
        mapTile.Markers = Enumerable.Empty<MapMarker>();
        MapMarker marker = new MapMarker();
        marker.Latitude = pos.Latitude;
        marker.Longitude = pos.Longitude;
        marker.IconHeight = 15;
        marker.IconWidth = 15;
        marker.IconType = MapIconType.Diamond;
        marker.IconFill = Color.FromRgb(255, 0, 0);
        MapMarkerCollection markerCollection = new MapMarkerCollection();
        markerCollection.Add(marker);

        mapTile.Markers = markerCollection;
    }

    internal void SetInitialPosition(MapLatLng pos)
    {
        if (mapTile != null)
        {
            
            DesiredPosition = pos;
        }
    }
}