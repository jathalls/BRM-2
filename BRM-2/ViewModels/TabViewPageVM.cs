using SessionsPage = BRM_2.Controls.SessionsPage;

namespace BRM_2.ViewModels;

public partial class TabViewPageVM:ObservableObject
{
    private SessionsPage sessPage;
    private SessionDetailsPage sessDetailsPage;
    private RecordingsPage recordingsPage;
    private BatDetailsPage batDetailsPage;
    internal BPASpectrogramM.Views.SpectrogramPageAsControl? spectrogramPage = null;
    private TabViewPage parentPage;

    [ObservableProperty]
    private List<SfTabItem> _tabContentsList = new List<SfTabItem>();

    [ObservableProperty]
    public bool _busyRunning = false;

    [ObservableProperty]
    public int _selectedTabIndex = 0;

    public TabViewPageVM(TabViewPage parent = null)
    {
        parentPage = parent;
    }
    

    

    private void SessPage_SelectionChanged(object? sender, DataGridSelectionChangedEventArgs e)
    {
        if (sessDetailsPage != null)
        {
            try
            {
                BusyRunning = true;
                RecordingSessionEx? sessionEx;
                if ((e.AddedRows?.Count ?? 0) > 0)
                {

                    sessionEx = (RecordingSessionEx)(e?.AddedRows[0] ?? new RecordingSessionEx());

                }
                else if ((e.RemovedRows?.Count ?? 0) > 0)
                {
                    sessionEx = e.RemovedRows?[0] as RecordingSessionEx;
                }
                else
                {
                    return;
                }
                if (sessionEx != null)
                {
                    sessDetailsPage.session = sessionEx;
                    recordingsPage.viewModel.Session = sessionEx;
                }
            }
            finally
            {
                BusyRunning = false;
            }
        }
    }

    [RelayCommand]
    public async void Import()
    {
        Debug.WriteLine("Import Clicked");
        try
        {
            if (!inImport)
            {
                inImport = true;

                var result = await Tools.GetWavFileFolderAsync();
                BusyRunning = true;
                Importer importer = new Importer();
                var session = await importer.ImportFromWav(result);
                BusyRunning = false;
                var dlg = new SessionDetailsDialog();


                dlg.session = session;
                dlg.FormClosed += Dlg_FormClosed;
                var page = new NavigationPage(dlg);
                await Shell.Current.Navigation.PushModalAsync(page);

            }
        }
        finally
        {
            BusyRunning = false;
            inImport = false;
        }
    }

    public async void ImportFromFolder(string folder)
    {
        Debug.WriteLine("Import From Folder");
        try
        {
            if (!inImport)
            {
                inImport = true;
                BusyRunning = true;
                Importer importer = new Importer();
                var session = await importer.ImportFromWav(folder);
                BusyRunning = false;
                var dlg = new SessionDetailsDialog();



                    dlg.session = session;
                dlg.FormClosed += Dlg_FormClosed;
                var page = new NavigationPage(dlg);
                await Shell.Current.Navigation.PushModalAsync(page);

            }
        }
        finally
        {
            BusyRunning = false;
            inImport = false;
        }
    }

    private bool inImport = false;


    private async void Dlg_FormClosed(object? sender, EventArgs e)
    {
        if (sender is SessionDetailsDialog dlg)
        {
            RecordingSessionEx session = dlg.session;

            await SaveSession(session);
        }



    }

    private async Task SaveSession(RecordingSessionEx session)
    {
        BusyRunning = true;
        try
        {
            if (!string.IsNullOrWhiteSpace(session.SessionTag))
            {
                _ = await DBAccess.InsertSessionAsync(session);
            }

            SwitchToTab(AppShell.TABS.Details);
            Thread.Sleep(10);
            SwitchToTab(AppShell.TABS.Sessions);
        }
        finally
        {
            BusyRunning = false;
            var sessionsVM = BRM_2.Navigation.ServiceProvider.GetService<SessionsPageVM>();
            sessionsVM.Update();
        }
    }

    private bool InExit = false;

    [RelayCommand]
    public async void Exit()
    {
        try
        {
            var page = BRM_2.Navigation.ServiceProvider.GetService<TabViewPage>();
            var spectrogramPageAsControl = page.spectrogramTab;
            if (spectrogramPageAsControl != null)
            {
                spectrogramPageAsControl?.Close();
                //spectrogramPageAsControl?.Dispose();
                //spectrogramPageAsControl = null;
                //spectrogramPageAsControl = new BPASpectrogramM.Views.SpectrogramPageAsControl();
            }
            Application.Current.Quit();
        }
        finally
        {
            InExit = false;
        }
    }

    

    public void SwitchToTab(AppShell.TABS tab)
    {
        _ = Shell.Current.Dispatcher?.Dispatch(() =>
        {
            try
            {
                if(tab== AppShell.TABS.Spectrogram) 
                {
                    
                }
                lastTab = SelectedTabIndex;
                SelectedTabIndex = (int)tab;
                
            }
            catch (Exception ex)
            {
                //Debug.WriteLine(ex.ToString());
            }
        });
    }

    internal async Task CreateSpectrogramPage()
    {
        if (spectrogramPage == null)
        {
            {
                spectrogramPage = new BPASpectrogramM.Views.SpectrogramPageAsControl(true);
                

                spectrogramPage.AnalysisCompletedEvent += SpectrogramPage_AnalysisCompletedEvent;

                
            }
        }
    }


    private int tabCount = 5;

    public int lastTab = -1;

    [RelayCommand]
    public void GoBack()
    {
       if(lastTab >= 0 && lastTab<tabCount)
        {
            var tmp = SelectedTabIndex;
            SelectedTabIndex = lastTab;
            lastTab = tmp;
        }
    }

    [RelayCommand]
    public void GoLeft()
    {
        if (SelectedTabIndex > 0)
        {
            lastTab= SelectedTabIndex;
            SelectedTabIndex--;
        }
    }

    [RelayCommand]
    public void GoRight()
    {
        if (SelectedTabIndex < tabCount-1)
        {
            lastTab = SelectedTabIndex;
            SelectedTabIndex++;
        }
    }

    [RelayCommand]
    public async Task OpenHelp()
    {
        try
        {
            var helpFile = Path.Combine(".", "BRMLiteM.pdf");
            if (File.Exists(helpFile))
            {
                Process.Start(new ProcessStartInfo(helpFile) { UseShellExecute = true });
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Help File Not Found", $"The help file {helpFile} was not found.", "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }


    [RelayCommand]
    public void OpenSettings()
    {
        //spPage.OpenSettings();
        Debug.WriteLine("OpenSettings command executed.");
    }


    public async void SwitchToTab(AppShell.TABS tab, object parameter)
    {
        try
        {
            if (tab == AppShell.TABS.Spectrogram && parameter is LabelledSegmentEx seg)
            {
                BusyRunning = true;
                string file = await seg.GetFile();
                //var spectrogramPageAsControl = BRMLiteM.Navigation.ServiceProvider.GetService<BPASpectrogramM.Views.SpectrogramPageAsControl>();
                var page = BRM_2.Navigation.ServiceProvider.GetService<TabViewPage>();

                if (spectrogramPage == null)
                { spectrogramPage = new BPASpectrogramM.Views.SpectrogramPageAsControl();
                    SfTabItem spectrogramTabItem = new SfTabItem();
                    spectrogramTabItem.Header = "Spectrogram";
                    spectrogramTabItem.Content = spectrogramPage;
                    spectrogramPage.AnalysisCompletedEvent += SpectrogramPage_AnalysisCompletedEvent;
                    TabContentsList.Add(spectrogramTabItem);
                    OnPropertyChanged(nameof(TabContentsList));
                }

                if (spectrogramPage != null) {
                    {
                        var rec = await DBAccess.GetRecordingAsync(seg.RecordingID);
                        List<LabelItem> LabelList = await rec.GetLabelList();
                        List<BPASpectrogramM.LabelItem> spectroLabelList = LabelList.Select(li => new BPASpectrogramM.LabelItem(
                            li.idedBats, li.startOffset, li.endOffset)).ToList();
                        await spectrogramPage.LoadSegment(file, seg.StartOffsetTimeSpan, seg.EndOffsetTimeSpan, spectroLabelList ?? new List<BPASpectrogramM.LabelItem>());
                    }



                    SwitchToTab(tab);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
        finally { BusyRunning = false; }
    }

    public void Appearing()
    {
        TabContentsList.Clear();
        sessPage = new SessionsPage();
        SfTabItem sessTabItem= new SfTabItem();
        sessTabItem.Header = "Sessions";
        sessTabItem.Content = sessPage;
        TabContentsList.Add(sessTabItem);

        sessDetailsPage = new SessionDetailsPage();
        SfTabItem detailsTabItem = new SfTabItem();
        detailsTabItem.Header = "Session Details";
        detailsTabItem.Content = sessDetailsPage;
        TabContentsList.Add(detailsTabItem);

        recordingsPage = new RecordingsPage();
        SfTabItem recTabItem = new SfTabItem();
        recTabItem.Header = "Recordings";
        recTabItem.Content = recordingsPage;
        TabContentsList.Add(recTabItem);

        batDetailsPage = new BatDetailsPage();
        SfTabItem batsTabItem = new SfTabItem();
        batsTabItem.Header = "Bats";
        batsTabItem.Content = batDetailsPage;
        TabContentsList.Add(batsTabItem);

        spectrogramPage = new BPASpectrogramM.Views.SpectrogramPageAsControl();
        SfTabItem spectrogramTabItem = new SfTabItem();
        spectrogramTabItem.Header = "Spectrogram";
        spectrogramTabItem.Content = spectrogramPage;
        spectrogramPage.AnalysisCompletedEvent += SpectrogramPage_AnalysisCompletedEvent;
        TabContentsList.Add(spectrogramTabItem);

        OnPropertyChanged(nameof(TabContentsList));

    }

    private void SpectrogramPage_AnalysisCompletedEvent(object? sender, BPASpectrogramM.Views.FileEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e?.folder))
        {
            ImportFromFolder(e.folder);
        }
        else
        {
            Debug.WriteLine("Import Session Failed", $"Unable to Import from {e.folder}", "OK");
        }
    }
}
