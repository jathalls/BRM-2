//using Foundation;

namespace BRM_2.ViewModels;
public partial class SessionsPageVM : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<RecordingSessionEx> _sessions = new ObservableCollection<RecordingSessionEx>();

    [ObservableProperty]
    private RecordingSessionEx _selectedSession = new RecordingSessionEx();

    [ObservableProperty]
    private List<BatSummary> _batSummaryList=new List<BatSummary>();


    
    private  bool _busyRunning = false;

    public bool BusyRunning
    {
        get => _busyRunning;
        set
        {
            if (value)
            {
                _busyCount++;
                if (_busyRunning)
                {
                    Debug.WriteLine($"BusyRunning already true, incrementing busy count {_busyCount}");
                    
                    return;
                }
            }
            else
            {
                _busyCount--;
                Debug.WriteLine($"BusyRunning set to false, decrementing busy count {_busyCount}");
                
                if (_busyCount < 0) { _busyCount = 0; }
                if (_busyCount > 0) { return; }

            }
            if (SetProperty(ref _busyRunning, value))
            {
                Debug.WriteLine($"BusyRunning set to {value}, notifying command can execute changed");
                ImportCommand.NotifyCanExecuteChanged();
                DeleteSessionCommand.NotifyCanExecuteChanged();
                ViewDetailsCommand.NotifyCanExecuteChanged();
                ViewRecordingsCommand.NotifyCanExecuteChanged();
            }
        }
    }

    private int _busyCount = 0;

    [RelayCommand]
    public async void DeleteSession()
    {
        await DeleteSession(SelectedSession);
    }

    private async Task DeleteSession(RecordingSessionEx selectedSession)
    {
        await DBAccess.DeleteSessionAsync(selectedSession);
        await RefreshAsync();
    }

    [RelayCommand]
    public void ViewRecordings()
    {
        if(Shell.Current is AppShell appShell)
        {
            appShell.SwitchToTab(AppShell.TABS.Recordings);
        }
    }

    [RelayCommand]
    public void ViewDetails()
    {
        if ((Shell.Current is AppShell appShell))
        {
            appShell.SwitchToTab(AppShell.TABS.Details);
        }
    }

    public async Task SfSessionDataGrid_SelectionChanged(object? sender, Syncfusion.Maui.DataGrid.DataGridSelectionChangedEventArgs e)
    {
        RecordingSessionEx? selection = null;
        if ((e.AddedRows?.Count ?? 0) > 0)
        {
            selection = e.AddedRows?[0] as RecordingSessionEx;
        }
        else if ((e.RemovedRows?.Count ?? 0) > 0)
        {
            selection = e.RemovedRows?[0] as RecordingSessionEx;
        }
        if (selection != null && selection.ID >= 0)
        {
            SelectedSession = selection;
            await GetBatSummaryForSession(selection);
            
            
            

            var detailsVM = BRM_2.Navigation.ServiceProvider.GetService<SessionDetailsDisplayVM>();
            if (detailsVM != null)
            {
                detailsVM.recordingSession = selection;


            }
        }
    }

    private async Task UpdateRecordingsPage(RecordingSessionEx selection)
    {
        var recordingsVM = BRM_2.Navigation.ServiceProvider.GetService<RecordingsPageVM>();
        if (recordingsVM != null)
        {
            recordingsVM.Session = selection;

            var errorCode = await recordingsVM.Update();
            if (errorCode != 0)
            {
                var toast = Toast.Make($"Error loading recordings for session: {errorCode}");
                toast?.Show();
                return;
            }
        }
    }

    /// <summary>
    /// for the selected session, generates a list of BatSummary objects and assigns it to BatSummaryList.
    /// </summary>
    /// <param name="selection"></param>
    /// <exception cref="NotImplementedException"></exception>
    private async Task GetBatSummaryForSession(RecordingSessionEx selection)
    {
        
        var batSummaryList = new List<BatSummary>();
        var recordings = await DBAccess.GetRecordingsForSessionAsync(selection.ID);
        if (recordings != null)
        {
            foreach (var rec in recordings)
            {
                var reclist = await rec.GetRecBatSummariesAsync(force : true);
               batSummaryList.AddRange(reclist);
            }
        }
        BatSummaryList = RecordingEx.CondenseBatSummaries(batSummaryList);
    }

    private List<BatSummary> CondenseBatSummaryList(List<BatSummary> batSummaryList)
    {
        throw new NotImplementedException();
    }

    public async Task RefreshAsync()
    {

        var sessions = await DBAccess.GetSessionsAsync();
        Sessions = new ObservableCollection<RecordingSessionEx>(sessions);
        //Debug.WriteLine($"Loaded {Sessions.Count} Sessions");


    }

    /// <summary>
    /// Generates report for the session as a .csv file in the original folder in HertsAtlas format.
    /// </summary>
    [RelayCommand]
    public async void Report()
    {
        List<HertsAtlasItem> reportItems = new List<HertsAtlasItem>();
        var session= SelectedSession;
        var recordings = await DBAccess.GetRecordingsForSessionAsync(session.ID);
        foreach(var rec in recordings)
        {
            string gridRef=await rec.GetGridRef();
            var summaries = await rec.GetRecBatSummariesAsync(force: true);
            foreach(var summary in summaries)
            {
                HertsAtlasItem item = new HertsAtlasItem(
                    file: rec.RecordingName,
                    date: rec.RecordingDate,
                    location: session.Location,                    
                    latitude: rec.RecordingGPSLatitude,
                    longitude: rec.RecordingGPSLongitude,
                    gridRef: gridRef,
                    species: summary.BatName,
                    passes: summary.Passes,
                    equipment: session.Equipment,
                    comment: rec.RecordingNotes,
                    observer: session.Operator

                );
                reportItems.Add(item);
            }
        }
        var fileName = $"{session.SessionTag}_Report.csv";
        var filePath=session.OriginalFilePath;
        if(!Directory.Exists(filePath)){ Directory.CreateDirectory(filePath); }
        var fqFileName=Path.Combine(filePath,fileName);
        if(File.Exists(fqFileName))
        {
            var backupName = fqFileName + ".bak";
            if(File.Exists(backupName))
            {
                File.Delete(backupName);
            }
            File.Move(fqFileName, backupName);

        }
        File.WriteAllText(fqFileName, HertsAtlasItem.Headers() + Environment.NewLine);
        foreach(var item in reportItems)
        {
            File.AppendAllText(fqFileName, item.ToString() + Environment.NewLine);
        }
        await BatDetect2.SafeDisplayAlertAsync("Report Generated", $"Report saved as {fqFileName}", "OK");


    }

    private bool inImport = false;

    /// <summary>
    /// Initiates the import process by allowing the user to select a folder containing WAV files.
    /// </summary>
    /// <remarks>This method displays a folder picker dialog for the user to select a folder. If no
    /// folder is selected,  a toast notification is displayed to inform the user. The method ensures that the
    /// import process  cannot be initiated multiple times concurrently.</remarks>
    [RelayCommand]
    public async void Import()
    {

        Debug.WriteLine("Import Clicked");
        try
        {
            if (!inImport)
            {
               
                inImport = true;
#if MACCATALYST
                var folderUrl = await MacFolderPicker.PickFolderAsync();
                if (folderUrl == null || folderUrl.Path==null)
                {
                    var toast=Toast.Make($"No Folder selected");
                    toast?.Show();
                    return;
                }
                
                Import(folderUrl);
#else
                var result = await Tools.GetWavFileFolderAsync();
                if (string.IsNullOrWhiteSpace(result))
                {
                    var toast=Toast.Make($"No Folder selected");
                    toast?.Show();
                    return;
                }
                Debug.WriteLine($"Selected folder for Import: {result}");
                await Import(result??"");
                BusyRunning = false;
                
#endif
                //var folder = await _folderPicker.PickFolder();
                //if (folder == null) { return; }


            }
        }catch(Exception ex)
        {
            BusyRunning = false;
            inImport = false;
            Debug.WriteLine($"Error during import: {ex}");
            var toast=Toast.Make($"Error during import: {ex.Message}");
            toast?.Show();
        }
        finally
        {
            
        }
    }

    /// <summary>
    /// Imports a session from the specified WAV file and navigates to the session details dialog.
    /// </summary>
    /// <remarks>This method initiates the import process using the provided WAV file. If the import
    /// is successful, a session is created, and the user is navigated to a session details dialog. If the import
    /// fails, a toast notification is displayed to inform the user.  The method sets the <c>BusyRunning</c> flag to
    /// indicate that the import process is in progress. This flag is reset when the operation completes, regardless
    /// of success or failure.</remarks>
    /// <param name="file">The path to the WAV file to import. The file must exist and be accessible.</param>
    public async Task Import(string file)
    {
        if(string.IsNullOrWhiteSpace(file) || !(File.Exists(file) || Directory.Exists(file)))
        {
            var toast=Toast.Make($"Invalid file path: {file}");
            toast?.Show();
            return;
        }
        try
        {
            Debug.WriteLine($"Starting import for file: {file}");
            BusyRunning = true;
            Importer importer = new Importer();
            var session = await importer.ImportFromWav(file);
            BusyRunning = false;
            if (session == null)
            {
                await Toast.Make($"Failed to Create New Session").Show();
                return;
            }
            var dlg = new SessionDetailsDialog();


            dlg.session = session;
            dlg.FormClosed += Dlg_FormClosed;
            var page = new NavigationPage(dlg);
            await Shell.Current.Navigation.PushModalAsync(page);
        }
        finally
        {
            inImport = false;
            BusyRunning = false;
        }
    }

#if MACCATALYST
    public async void Import(NSUrl folderUrl)
    {
        try
        {
            BusyRunning = true;
            Importer importer = new Importer();
            //SecurityScopedBookmarks.SaveFolderBookmark(folderUrl.Path, folderUrl);
            MauiLib1.Bookmarks.Save(folderUrl?.Path??"",folderUrl);
            var session = await importer.ImportFromWav(folderUrl.Path);
            BusyRunning = false;
            if (session == null)
            {
                await Toast.Make($"Failed to Create New Session").Show();
                return;
            }
            var dlg = new SessionDetailsDialog();


            dlg.session = session;
            dlg.FormClosed += Dlg_FormClosed;
            var page = new NavigationPage(dlg);
            await Shell.Current.Navigation.PushModalAsync(page);
        }
        finally
        {
            inImport=false;
            BusyRunning = false;
        }
    }
#endif



    private async void Dlg_FormClosed(object? sender, EventArgs e)
    {
        if (sender is SessionDetailsDialog dlg)
        {
            RecordingSessionEx session = dlg.session;

            await SaveSession(session);
            BusyRunning = false;
            BusyRunning = false;
            BusyRunning = false; // to force BusyRunning to clear
        }



    }

    private async Task SaveSession(RecordingSessionEx session)
    {
        if(session==null) { return; }
        BusyRunning = true;
        try
        {
            if (!string.IsNullOrWhiteSpace(session.SessionTag))
            {
                _ = await DBAccess.InsertSessionAsync(session);
            }
            await RefreshAsync();
            (Shell.Current as AppShell)?.SwitchToTab(AppShell.TABS.Details);
            (Shell.Current as AppShell)?.SwitchToTab(AppShell.TABS.Sessions);
            //SwitchToTab(1);
            //SwitchToTab(0);
        }
        finally
        {
            BusyRunning = false;
        }
    }


    public event EventHandler<DataGridSelectionChangedEventArgs> SelectionChanged;

    protected virtual void OnSelectionChanged(DataGridSelectionChangedEventArgs e) =>
        SelectionChanged?.Invoke(this, e);

    internal async void Update()
    {
        await RefreshAsync();
    } 
}
