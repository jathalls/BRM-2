namespace BRM_2.ViewModels;
    public partial class RecordingsPageVM : ObservableObject
    {
        public RecordingsPageVM() 
        {
#if WINDOWS || MACCATALYST
            IsBatDetect2Available = true;
#endif
        }

        [ObservableProperty]
        private RecordingSessionEx _session;

        [ObservableProperty]
        private RecordingEx _selectedRecording = new RecordingEx();

        [ObservableProperty]
        private bool _isBusyRunning=false;

        [ObservableProperty]
        private bool _isBatDetect2Available = false;

        [ObservableProperty]
        private bool _isPlaySegmentAvailable = true;



        public ObservableCollection<RecordingEx> _recordings = new ObservableCollection<RecordingEx>();

        public ObservableCollection<RecordingEx> Recordings
        {
            get => _recordings;
            set
            {
                _recordings = value;
                _recordingsSetCollection = new ObservableCollection<RecordingsSet>();
                foreach(var rec in value) _recordingsSetCollection.Add(new RecordingsSet(rec));
                OnPropertyChanged(nameof(Recordings));
                OnPropertyChanged(nameof(RecordingsSetCollection));
            }
        }

        [ObservableProperty]
        private LabelledSegmentEx _selectedSegment = new LabelledSegmentEx();

        [ObservableProperty]
        private LabelledSegmentEx? _segment = null;

        [ObservableProperty]
        private ObservableCollection<RecordingsSet> _recordingsSetCollection = new ObservableCollection<RecordingsSet>(); 




       public void RecordingsCollectionView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            
            if ( e.CurrentSelection?.Any()??false)
            {
                SelectedRecording = e.CurrentSelection[0] as RecordingEx;
                SelectedSegment = new LabelledSegmentEx();
            }
            else
            {
                if (e.PreviousSelection != null && e.PreviousSelection.Any())
                {
                    SelectedRecording = e.PreviousSelection[0] as RecordingEx;
                    SelectedSegment = new LabelledSegmentEx();
                }
            }
        }

    public bool isUpdating { get; set; } = false;
    
    public async Task<int> Update(bool cancelUpdate = false)
    {
        

        if (!isUpdating)
        {
            isUpdating = true;
            try
            {
                IsBusyRunning = true;
                Debug.WriteLine("Recordings Update");
                if (Session == null)
                {
                    Recordings.Clear();
                    //Debug.WriteLine("No Session, so no List");
                    return -1;
                }
                if (Session.ID < 0)
                {
                    if (((Session.recordings?.Count) ?? 0) > 0)
                    {
                        Recordings = new ObservableCollection<RecordingEx>(Session?.recordings ?? new List<RecordingEx>());
                        //Debug.WriteLine($"Unsaved session so list is contents {Recordings.Count}");
                        return 1;
                    }
                }
                var recs = await DBAccess.GetRecordingsForSessionAsync(Session.ID);
                if(cancelUpdate)
                {
                    Debug.WriteLine("Recordings Update cancelled");
                    
                    return-2;
                }
                //Debug.WriteLine($"\tRecordings: {recs.Count}");
                foreach (RecordingEx recording in recs)
                {
                    var segs = await DBAccess.GetSegmentsForRecordingAsync(recording.ID);
                    if (cancelUpdate)
                    {
                        Debug.WriteLine("Recordings Update cancelled");
                        
                        return -2;
                    }
                    recording.LabelledSegments = segs;
                    //Debug.WriteLine($"\t\tSegments {recording.LabelledSegments.Count}");
                    //Debug.WriteLine($"rec has {segs.Count} segments");
                    foreach (var seg in recording.LabelledSegments)
                    {
                        var bats = await DBAccess.GetIdedBatsForSegmentAsync(seg.ID);
                        if (cancelUpdate)
                        {
                            Debug.WriteLine("Recordings Update cancelled");
                           
                            return -2;
                        }
                        //Debug.WriteLine($"\t\t\tided bats {bats.Count}");
                        seg.IdedBats = bats;
                        //Debug.WriteLine($"\t\t\tsummaries {seg.BatSummaryList.Count}");
                        seg.BatSummaryList = await seg.GetSegBatSummaryAsync();
                    }

                    var metas = await DBAccess.GetMetasForRecordingAsync(recording.ID);
                    if (cancelUpdate)
                    {
                        Debug.WriteLine("Recordings Update cancelled");
                        
                        return -2;
                    }
                    recording.Metas = metas;
                    recording.BatSummaryString = await recording.GetRecBatSummaryAsync();
                    if (cancelUpdate)
                    {
                        Debug.WriteLine("Recordings Update cancelled");
                        
                        return -2;
                    }
                    //Debug.WriteLine($"Rec Update:- {recording.LabelledSegments.Count}segs, summary={recording.BatSummaryString}");
                }
                Recordings = new ObservableCollection<RecordingEx>(recs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return -3;
            }
            finally
            {
                isUpdating = false;
                cancelUpdate = false;
                IsBusyRunning = false;
            }
        }
        return 0;
    }

        [RelayCommand]
        public void miAudacityClicked()
        {

            if (SelectedSegment!=null && SelectedSegment.ID>0)
            {
                SelectedRecording = Recordings.Where(rec => rec.ID == SelectedSegment.RecordingID).FirstOrDefault();
                var rec = SelectedRecording;
                //AudacityRecording(rec);
                if(Shell.Current is AppShell appShell)
                {
                    appShell.SwitchToTab(AppShell.TABS.Spectrogram,SelectedSegment);
                }
            }

        }

        [RelayCommand]
        public async Task PlaySegment()
        {
            
        }

        [RelayCommand]
        public void SegmentSelectionChanged(LabelledSegmentTable segment)
        {
            if (SelectedSegment == null)
            {
                return;
            }
        }

        [RelayCommand]
        public async void miSaveSegmentClicked()
        {
            if (SelectedSegment != null && SelectedSegment.ID > 0)
            {
                if ((SelectedRecording?.ID ?? -1) <= 0)
                {
                    SelectedRecording = await DBAccess.GetRecordingAsync(SelectedSegment.RecordingID);
                }
                Debug.WriteLine($"Saving segment {SelectedSegment.ID} for recording {SelectedRecording.ID}");
                _ = await SelectedSegment.Save(SelectedRecording);
            }
        }

        [RelayCommand]
        public async void miBatCallAnalyserClicked()
        {
            if (SelectedSegment != null && SelectedSegment.ID > 0)
            {
                if ((SelectedRecording?.ID ?? -1) <= 0)
                {
                    SelectedRecording = await DBAccess.GetRecordingAsync(SelectedSegment.RecordingID);
                }
                Debug.WriteLine($"Saving segment {SelectedSegment.ID} for recording {SelectedRecording.ID}");
                string fqFileName = await SelectedSegment.Save(SelectedRecording);
                string text = $"[BRMFile]\n{fqFileName}\n";
                var clipboard = Clipboard.Default;
                await clipboard.SetTextAsync(text);
                Application.Current?.MainPage?.DisplayAlert("Bat Recording Manager", "Call data copied to clipboard - you can now paste into Bat Call Analyser", "OK");
            }
        }

        [RelayCommand]
        public async void miBatDetect2Clicked()
        {
            Debug.WriteLine("miBatDetect2Clicked");
            if (SelectedSegment != null && SelectedSegment.ID > 0)
            {
                Debug.WriteLine($"{SelectedSegment.ID}");
                if ((SelectedRecording?.ID ?? -1) <= 0)
                {
                    SelectedRecording = await DBAccess.GetRecordingAsync(SelectedSegment.RecordingID);
                }
                var destination = await SelectedSegment.Save(SelectedRecording);
                Debug.WriteLine($"Process {destination}");
                var result = await RunBatDetect2(destination);
                Debug.WriteLine(result);
                await SelectedSegment.InsertSummary(result);
                await Update();
            }
            else
            {
                Debug.WriteLine("Ignored");
            }
        }

        private async Task<string> RunBatDetect2(string destination)
        {
            string result = "";
            if (File.Exists(destination))
            {


                try
                {
                    Debug.WriteLine($"[RecordingsPageVM] ===== PROCESS FILE STARTING =====");
                    IsBusyRunning = true;
                    var bd2 = BatDetect2.Instance;
                    Debug.WriteLine($"[RecordingsPageVM] BatDetect2.Instance obtained: {bd2 != null}");

                    //await bd2.InstallPython(); // does Python initialisations as well as installation if needed

                    Debug.WriteLine($"[RecordingsPageVM] Calling ProcessFile with destination: {destination}");
                    result = await BatDetect2.Instance.ProcessFile(destination);
                    Debug.WriteLine($"[RecordingsPageVM] ProcessFile returned: {result}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[RecordingsPageVM] EXCEPTION in ProcessFile:");
                    Debug.WriteLine($"[RecordingsPageVM] Exception Type: {ex.GetType().Name}");
                    Debug.WriteLine($"[RecordingsPageVM] Message: {ex.Message}");
                    Debug.WriteLine($"[RecordingsPageVM] StackTrace: {ex.StackTrace}");
                    Debug.WriteLine(ex.Message);
                }
                finally
                {
                    IsBusyRunning = false;
                }


            }
            return result;
        }


        private async void AudacityRecording(RecordingTable rec)
        {
            var file = rec.RecordingName;
            RecordingSessionTable session = await getSession(rec.SessionID);
            var path = session.OriginalFilePath;
        /*#if MACCATALYST
        var url=SecurityScopedBookmarks.TryRestoreFolderFromBookmark(path);
        if(url!=null) path=url.Path;
        #endif*/
        path = MauiLib1.Bookmarks.RestorePath(path);
            var qFile = Path.Combine(path, file);
            if (File.Exists(qFile))
            {
                var canOpen = await Launcher.CanOpenAsync(qFile);
                if (canOpen)
                {
                    await Launcher.OpenAsync(qFile);
                }
            }
        }

        private async Task<RecordingSessionTable> getSession(int sessionID)
        {
            var session = await DBAccess.GetSessionAsync(sessionID);
            return session;
        }

        internal int GetNumberOfSegmentsForRecording(int rowIndex)
        {
            if(rowIndex>=0 && rowIndex < Recordings.Count)
            {
                return Recordings[rowIndex].LabelledSegments.Count;
            }
            return 0;
        }
    }

    public partial class RecordingsSet : ObservableObject
    {
        [ObservableProperty]
        private RecordingTable _recording;

        public RecordingsSet(RecordingTable recording)
        {
            this.Recording = recording;
        }
    }
