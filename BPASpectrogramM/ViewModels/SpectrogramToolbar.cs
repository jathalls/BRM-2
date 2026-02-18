using BPASpectrogramM.Views;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Syncfusion.Maui.Charts;
using System.ComponentModel;
using System.Diagnostics;
using Color = Microsoft.Maui.Graphics.Color;

namespace BPASpectrogramM.ViewModels
{
    public partial class SpectrogramToolbarVM: ObservableObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string PropertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        public SpectrogramPageAsControl spPage;

        private string _currentFile = "";
        public string CurrentFile { get => _currentFile;  set { _currentFile = value; OnPropertyChanged(); } }
        public string currentFolder { get;  set; }
        public List<string> FileNames { get;  set; }
        public List<string> WavFileNames { get;  set; }

        public List<bool> FilesModified { get; set; }=new List<bool>();
        public int currentFileIndex { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether segment mode is enabled.
        /// Set for triggered mode recordings where a file represents a single segment
        /// </summary>
        [ObservableProperty]
        private bool _isSegmentMode = false;

        /// <summary>
        /// Gets or sets a value indicating whether multi-mode operation is enabled.
        /// Set for long recordings with multiple passes in a single file
        /// </summary>
        [ObservableProperty]
        private bool _isMultiMode = true;


        private bool _nextButtonEnabled = true;
        public bool NextButtonEnabled
        {
            get => _nextButtonEnabled;
            set
            {
                if (_nextButtonEnabled != value)
                {
                    _nextButtonEnabled = value;
                    if (value)
                    {
                        NextBackGround = Colors.Wheat;
                    }
                    else
                    {
                        NextBackGround = Colors.LightGray;
                    }
                    OnPropertyChanged();
                }
            }
        }

        [ObservableProperty]
        private Color _nextBackGround = Colors.Wheat;

        
        private bool _prevButtonEnabled = true;
        public bool PrevButtonEnabled
        {
            get => _prevButtonEnabled;
            set
            {
                if (_prevButtonEnabled != value)
                {
                    _prevButtonEnabled = value;
                    if (value)
                    {
                        PrevBackGround = Colors.Wheat;
                    }
                    else
                    {
                        PrevBackGround = Colors.LightGray;
                    }
                    OnPropertyChanged();
                }
            }
        }

        [ObservableProperty]
        private Color _prevBackGround = Colors.Wheat;

        public SpectrogramToolbarVM() { //this.spPage = spPage;
                                        }



        [RelayCommand]
        public async void OpenFile()
        {
            await spPage.SaveTextFile();
            var file = await PickFile(new CancellationToken());
            CurrentFile = Path.GetFileName(file).Trim();
            if(!CurrentFile.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                // pick the corresponding wav file if it exists
                var wavFile = Path.ChangeExtension(file, ".wav");
                if(File.Exists(wavFile))
                {
                    file = wavFile;
                    CurrentFile = wavFile;
                }
                else { 
                    await spPage.DisplayAlertSP("Error", $"No corresponding .WAV file found for {file}", "OK");
                    CurrentFile = "";
                    currentFileIndex = -1;
                    return;
                }
            }
            WavFileNames = new List<string>();

            WavFileNames?.Add(CurrentFile);
            FilesModified = new List<bool>();
            FilesModified?.Add(false);
            currentFileIndex = 0;
            Debug.WriteLine("Open File");
            
            await spPage.ReadFile(file);
            //DisableFileButtons();
            
        }


        private static async Task<string?> PickFile(CancellationToken cancellationToken)
        {
            //var folderResult = await FolderPicker.Default.PickAsync(cancellationToken);
            var files = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Pick first .wav or sidecar .txt file"


            });

            string? file = files.FullPath;
            if (!string.IsNullOrWhiteSpace(file))
            {

                
                return file;
            }
            return "";
        }

        private  async Task<string?> PickFolder(CancellationToken cancellationToken)
        {
            //var folderResult = await FolderPicker.Default.PickAsync(cancellationToken);
            var files = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Pick first .wav or sidecar .txt file"

                
            });
            
            string? file = files?.FullPath; // gets the full path and filename
            if (!string.IsNullOrWhiteSpace(file))
            {

                string folder = Path.GetDirectoryName(file);
                if (Directory.Exists(folder))
                {

                    return file;
                }
               
            }
            return "";
        }

        [RelayCommand]
        private async Task Prev()
        {

            await spPage.SaveTextFile();
            if (spPage.segmentLoaded) return; // do not allow prev if a segment is loaded
            if (currentFileIndex > 0)
            {
                currentFileIndex--;
                if (currentFileIndex < WavFileNames.Count)
                {
                    CurrentFile = Path.GetFileName(WavFileNames[currentFileIndex]).Trim();
                    currentFolder = Path.GetDirectoryName(WavFileNames[currentFileIndex]);
                    await spPage.ReadFile(WavFileNames[currentFileIndex]);
                    NextButtonEnabled = true;
                }
                else
                {
                    currentFileIndex++;
                }
                if (currentFileIndex >= WavFileNames.Count)
                {
                    NextButtonEnabled = false;
                    PrevButtonEnabled = true;
                }
                if (currentFileIndex <= 0)
                {
                    PrevButtonEnabled = false;
                }
            }
        }

        [RelayCommand]
        public async Task Next(bool IsModified = false)
        {
            await spPage.SaveTextFile(IsModified);
            if (spPage.segmentLoaded) return; // do not allow next if a segment is loaded
            if (currentFileIndex >= 0)
            {
                currentFileIndex++;
                if (currentFileIndex >= WavFileNames.Count)
                {
                    // we have run out of files to analyse
                    currentFileIndex--; // restore the index
                    spPage.AnalysisCompleted();
                    return; // do nothing
                }
                // move on to the next file
                CurrentFile = Path.GetFileName(WavFileNames[currentFileIndex]).Trim();
                currentFolder = Path.GetDirectoryName(WavFileNames[currentFileIndex]);
                await spPage.ReadFile(WavFileNames[currentFileIndex]);
                PrevButtonEnabled = true;
                if (currentFileIndex <= 0) PrevButtonEnabled = false;
            }
        }


        [RelayCommand]
        private async Task OpenFolder()
        {
            await spPage.SaveTextFile();
            await OpenFolderAsync();
        }

        /// <summary>
        /// Gets a folder from the user and opens the first file in it
        /// </summary>
        /// <returns></returns>
        private async Task OpenFolderAsync() 
        {
            CancellationToken cancellationToken = default;
            Debug.WriteLine("Open Folder");
            try
            {

                string file = await PickFolder(cancellationToken);
                if (!string.IsNullOrWhiteSpace(file))
                {
                    Debug.WriteLine($"Folder picked {file}");

                }
                else
                {
                    Debug.WriteLine("No folder picked");
                    await Toast.Make($"Folder is not picked").Show(cancellationToken);
                    return;
                }
                currentFolder = Path.GetDirectoryName(file);
                FileNames = Directory.EnumerateFiles(currentFolder, "*.*").ToList();
                WavFileNames = Directory.EnumerateFiles(currentFolder, "*.wav").OrderBy(f => new FileInfo(f).CreationTime).ToList();
                FilesModified = new bool[WavFileNames.Count].ToList();
                for(int i=0;i<FilesModified.Count;i++) { FilesModified[i] = false; }
                if (WavFileNames?.Any() ?? false)
                {
                    string selectedWavFile = Path.ChangeExtension(file, ".wav");
                    currentFileIndex = WavFileNames.FindIndex(f => f.Equals(selectedWavFile, StringComparison.OrdinalIgnoreCase));
                    CurrentFile = Path.GetFileName(WavFileNames[currentFileIndex]).Trim();
                    await spPage.ReadFile(WavFileNames[currentFileIndex]);
                    if (WavFileNames.Count > 1) NextButtonEnabled = true;
                }
                else
                {
                    currentFileIndex = -1;
                    CurrentFile = "";
                    await spPage.DisplayAlertSP("Error", $"No .WAV files found in folder {currentFolder}", "OK");
                    //await Toast.Make($"No .WAV files found in folder {currentFolder}").Show(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                CurrentFile = "";
                currentFileIndex = -1;
                FileNames?.Clear();
                WavFileNames?.Clear();
                FilesModified?.Clear();
                await spPage.DisplayAlertSP("Error", $"Error picking folder {ex.Message}", "OK");
            }
        }

        internal bool IsModified()
        {
            int index= currentFileIndex;
            if (index>=0 && index<(FilesModified?.Count??-1))
            {
                return FilesModified[index];
            }
            return false;
        }

        internal void SetUnmodified()
        {
            int index = currentFileIndex;
            if ((index >= 0 && index < FilesModified.Count))
            {
                FilesModified[index] = false;
            }
        }

        internal void SetModified()
        {
            int index = currentFileIndex;
            if ((index >= 0 && index < FilesModified.Count))
            {
                FilesModified[index] = true;
            }

        }

        internal void DisableFileButtons()
        {
            NextButtonEnabled = false;
            PrevButtonEnabled = false;
        }
    }
}
