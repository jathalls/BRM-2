namespace BRM_2.Controls;
public partial class SessionDetailsPage : ContentView
{
	private RecordingSessionTable? _session = null;

	private readonly SessionDetailsDisplayVM displayVM;
	public RecordingSessionTable session 
	{
		get { return _session??new RecordingSessionTable(); }
		set {  _session = value; this.displayVM.recordingSession = value;	}
	}

	public SessionDetailsPage()
	{
		this.displayVM = BRM_2.Navigation.ServiceProvider.GetService<SessionDetailsDisplayVM>();
        InitializeComponent();
		this.BindingContext = displayVM;
    }
    public SessionDetailsPage(SessionDetailsDisplayVM displayVM)
	{
		this.displayVM = displayVM;
		InitializeComponent();
		this.BindingContext = displayVM;
	}
}
