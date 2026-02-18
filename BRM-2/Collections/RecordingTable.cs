namespace BRM_2.Collections;

[Table("RecordingTable")]
public class RecordingTable
{
    [PrimaryKey, AutoIncrement]
    public int ID { get; set; } = 0;


    [XmlAttribute("RecordingName")]
    public string RecordingName { get; set; } = string.Empty;

    public DateTime RecordingStartTime { get; set; }

    public DateTime RecordingEndTime { get; set; }

    [XmlAttribute("RecordingGPSLongitude")]
    public double RecordingGPSLongitude { get; set; } = 200.0d;

    [XmlAttribute("RecordingGPSLatitude")]
    public double RecordingGPSLatitude { get; set; } = 200.0d;

    [XmlAttribute("RecordingNotes")]
    public string RecordingNotes { get; set; } = string.Empty;

    [XmlAttribute("RecordingDate")]
    public DateTime RecordingDate
    {
        get { return _recordingDate; }
        set { _recordingDate = value; }
    }

    protected DateTime _recordingDate = new DateTime();

    public int SessionID { get; set; } = 0;

}
