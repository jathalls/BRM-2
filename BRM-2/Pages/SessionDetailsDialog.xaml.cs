namespace BRM_2.Pages;
public partial class SessionDetailsDialog : ContentPage
{
    public RecordingSessionEx result { get; set; }
    public RecordingSessionEx session
    {
        get { return sessionDetailsForm.recordingSession; }
        set { sessionDetailsForm.recordingSession = value; result = value; }
    }

	public SessionDetailsDialog()
	{
		InitializeComponent();
	}


    private async Task CloseDialog()
    {
        await Navigation.PopModalAsync();

        OnFormClosed(EventArgs.Empty);
        //sessionDetailsForm.Close();
    }

    private async void OKButton_Clicked(object sender, EventArgs e)
    {

        result=sessionDetailsForm.UpdateSession();
        
        await CloseDialog();
    }

    private async void CancelButton_Clicked(object sender, EventArgs e)
    {
        await this.CloseDialog();
    }

    #region event

    public event EventHandler<EventArgs>? FormClosed;

    protected virtual void OnFormClosed(EventArgs e)=> FormClosed?.Invoke(this, e);
    #endregion

}