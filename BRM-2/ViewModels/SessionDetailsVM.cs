namespace BRM_2.ViewModels;
public class SessionDetailsVM
{
    public string sessionTag { get; set; } = "session tag";

    public DateTime startDate { get; set; } = DateTime.Now;

    public TimeSpan startTime { get; set; } = DateTime.Now.TimeOfDay;

    public DateTime endDate { get; set; }=DateTime.Now;

    public TimeSpan endTime { get; set; } = DateTime.Now.TimeOfDay;

    public string location { get; set; } = "Unknown Location";

    public string Latitude { get; set; } = "";

    public string Longitude { get; set; } = "";



    public RecordingSessionTable recordingSession { get; set; } = new RecordingSessionTable();
    public ObservableCollection<string> microphoneList { get; internal set; }=new ObservableCollection<string>();

    public void DebugListing()
    {
        //Debug.WriteLine($"Tag={sessionTag}");
        //Debug.WriteLine($"startDate={startDate.ToString()}");
        //Debug.WriteLine($"startTime={startTime.ToString()}");
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
    public void UpdateSession()
    {
        recordingSession.SessionTag = sessionTag;
        recordingSession.SessionStart = startDate;
        recordingSession.SessionEnd = endDate;
    }
}
