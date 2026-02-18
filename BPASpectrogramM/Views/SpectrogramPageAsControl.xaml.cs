using AppoMobi.Specials;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BPASpectrogramM.Views;

public partial class SpectrogramPageAsControl : ContentView, INotifyPropertyChanged, IDisposable
{
    public new event PropertyChangedEventHandler PropertyChanged;
    protected override void OnPropertyChanged([CallerMemberName] string PropertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName)); }

    public event EventHandler<FileEventArgs> AnalysisCompletedEvent;

    protected virtual void OnAnalysisCompleted(FileEventArgs e)
    {
        AnalysisCompletedEvent?.Invoke(this, e);
    }

    private bool _isBusy = false;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();
        }
    }
    public SpectrogramPageAsControl()
    {
        Debug.WriteLine("[SpectrogramPageAsControl] Constructor: Starting");
        InitializeComponent();
        Debug.WriteLine("[SpectrogramPageAsControl] Initialize completed");
        BindingContext = this;
        SpButtons.SpectrogramButtonClicked += SpButtons_SpectrogramButtonClicked;
        //spectrogram.SelectColour += Spectrogram_SelectColour;
        //spectrogram.BracketAdded += Spectrogram_BracketAdded;

        spectrogram.SelectionChanged += Spectrogram_SelectionChanged;
        Debug.WriteLine("[SpectrogramPageAsControl] Events configured");
        ButtonVisibility = false;
        SpToolbar.SetParentPage(this);
        Debug.WriteLine("[SpectrogramPageAsControl] About to ReadDefaults");
        ReadDefaults().ConfigureAwait(false);
        Debug.WriteLine("[SpectrogramPageAsControl] Defaults read");
        Unfocused += SpectrogramPageAsControl_Unfocused;
        Debug.WriteLine("[SpectrogramPageAsControl] Unfocused; Constructor Completed");
    }

    private void SpectrogramPageAsControl_Unfocused(object? sender, FocusEventArgs e)
    {
        Debug.WriteLine("SpectrogramPageAsControl Unfocused - stopping spectrogram");
        spectrogram.Stop();
    }

    private async Task ReadDefaults()
    {
        try
        {
            string wavFilePath = await SPTools.CopyFileToAppDataDirectory("BatRef.wav");
            string txtFilePath = await SPTools.CopyFileToAppDataDirectory("BatRef.txt");

            Debug.WriteLine($"[ReadDefaults] Wav file path is {wavFilePath}");
            if (!string.IsNullOrEmpty(wavFilePath) && File.Exists(wavFilePath))
            {
                try
                {
                    Debug.WriteLine($"[ReadDefaults] About to call ReadFile");
                    await ReadFile(wavFilePath);
                    Debug.WriteLine($"[ReadDefaults] ReadFile completed successfully");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SpectrogramPageAsControl] Error reading file: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }

    }

    private double selectionStart { get; set; } = 0;
    private double selectionEnd { get; set; } = 0;

    private string CurrentFile { get; set; } = "";
    private async void Spectrogram_SelectionChanged(object? sender, SpectrogramSelectionChangedEventArgs e)
    {

        if (e.StartSecs != 0 || e.EndSecs != 0)
        {
            selectionStart = e.StartSecs;
            selectionEnd = e.EndSecs;
            CurrentFile = e.fqFileName;
            ButtonVisibility = true;
            await SpControls.SetEntryFocus();
        }
        else
        {
            ButtonVisibility = false;
            selectionStart = 0;
            selectionEnd = 0;
        }
        if (SpControls.CurrentAutoAdvanceState == SpectrogramControls.AUTOADVANCEMODE.BUTTON)
        {
            ButtonVisibility = true;
        }
    }


    /// <summary>
    /// When a label bracket has been added, default the focus tot he text entry box
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <exception cref="NotImplementedException"></exception>
    private async void Spectrogram_BracketAdded(object? sender, EventArgs e)
    {

        await SpControls.SetEntryFocus();


    }

    private async void Spectrogram_SelectColour(object? sender, EventArgs e)
    {
        await SelectColourScheme();
    }

    //protected override void OnDisappearing()
    //{
    //    Save();
    //}

    public async Task Save()
    {
        await SaveTextFile();
    }

    private bool _nextButtonEnabled = false;
    public bool NextButtonEnabled { get => _nextButtonEnabled; set { _nextButtonEnabled = value; OnPropertyChanged(); } }

    private bool _prevButtonEnabled = false;
    public bool PrevButtonEnabled { get => _prevButtonEnabled; set { _prevButtonEnabled = value; OnPropertyChanged(); } }

    private async void SpButtons_SpectrogramButtonClicked(object? sender, EventArgs e)
    {
        string text = SpButtons.Text;
        //spectrogram.ButtonClicked(text);
        bool isModified = false;
        if (!string.IsNullOrWhiteSpace(text) && (selectionStart != 0 || selectionEnd != 0))
        {
            spectrogram.AddLabel(selectionStart, selectionEnd, text); // add the provided text as a label fro the current selection
            isModified = spectrogram.IsModified;
        }
        else if (!string.IsNullOrWhiteSpace(text))
        {
            if (SpControls.CurrentAutoAdvanceState == SpectrogramControls.AUTOADVANCEMODE.BUTTON ||
                SpControls.CurrentAutoAdvanceState == SpectrogramControls.AUTOADVANCEMODE.BOTH)
                spectrogram.AddLabel(0, 0, text); // add the provided text as a label for the entire displayed spectrogram
            isModified = spectrogram.IsModified;
        }
        await SpControls.SetEntryFocus(); // set the default focus to the text entry box
        AutoAdvance(SpectrogramControls.AUTOADVANCEMODE.BUTTON, isModified); // advance to the next file if required
    }


    public bool WaveformVisibility { get => _waveformVisibility; set { _waveformVisibility = value; OnPropertyChanged(); } }
    private bool _waveformVisibility = false;

    public double WaveformHeight { get => _waveformHeight; set { _waveformHeight = value; OnPropertyChanged(); } }
    private double _waveformHeight = 100;

    public bool ButtonVisibility
    {
        get => _buttonVisibility;
        set
        {
            _buttonVisibility = value;

            OnPropertyChanged();
        }
    }
    private bool _buttonVisibility = true;

    public double ButtonViewHeight { get => _buttonViewHeight; set { _buttonViewHeight = value; OnPropertyChanged(); } }
    private double _buttonViewHeight = 200;



    private async Task SelectColourScheme()
    {
        var colours = ScottPlot.Colormap.GetColormaps();
        var colourMapNames = new List<string>();
        foreach (var colour in colours)
        {
            string name = colour.Name;
            colourMapNames.Add(name);
        }
        string action = await Shell.Current.DisplayActionSheet("Select Scheme:-", "Cancel", null, colourMapNames.ToArray());
        //spectrogram.SetColourScheme(action);
    }

    //private SpectrogramGenerator? SpecGen { get; set; } = null;

    public async Task ReadFile()
    {
        var fileResult = await PickAndShow(PickOptions.Default);
        if (fileResult is null) return;
        string wavFilePath = fileResult.FullPath;

        if (wavFilePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            wavFilePath = Path.ChangeExtension(wavFilePath, ".wav");
        }
        await ReadFile(wavFilePath);
    }

    public async Task ReadFile(string file, double startSecs = 0.0d, double endSecs = 5.0d, bool AddLabels = true)
    {
        if (string.IsNullOrWhiteSpace(file)) return;
        if (!File.Exists(file)) return;

        IsBusy = true;
        try
        {
            segmentLoaded = false;
            TimeSpan start = DateTime.Now.TimeOfDay;
            Debug.WriteLine($"[ReadFile] Busy Set True {start}");
            Debug.WriteLine($"[ReadFile] Loading file: {file}");

            SpToolbar.viewModel.currentFolder = Path.GetDirectoryName(file) ?? "";
            SpToolbar.viewModel.CurrentFile = Path.GetFileName(file);

            Debug.WriteLine($"[ReadFile] About to call spectrogram.LoadFile");
            await spectrogram.LoadFile(file);

            Debug.WriteLine($"[ReadFile] spectrogram.LoadFile completed");
            if (!spectrogram.Success())
            {
                Debug.WriteLine($"[ReadFile] Spectrogram.Success() returned false");
                return;
            }

            Debug.WriteLine($"[ReadFile] File loaded successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReadFile] Error: {ex.GetType().Name}");
            Debug.WriteLine($"[ReadFile] Message: {ex.Message}");
            Debug.WriteLine($"[ReadFile] StackTrace: {ex.StackTrace}");
            await Toast.Make($"Error loading file: {ex.Message}").Show();
            return;
        }
        finally
        {

            IsBusy = false;
        }

        Debug.WriteLine($"Busy False; File is {Path.Combine(SpToolbar.viewModel.currentFolder, SpToolbar.viewModel.CurrentFile)}");
        if (AddLabels)
        {
            AddExistingLabels(file);
        }

    }

    /// <summary>
    /// Looks for .txt sidecar file and if found reads any label data, adding appropiate brackets to
    /// the current spectrogram
    /// </summary>
    /// <param name="file"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void AddExistingLabels(string file)
    {
        string textFile = Path.ChangeExtension(file, ".txt");
        if (File.Exists(textFile))
        {
            List<string> labelLines = GetLabelLines(textFile);
            foreach (string line in labelLines ?? new List<string>())
            {
                var parts = line.Split(new char[] { '\t', ' ', ',' }, 3);
                if (parts.Length >= 3)
                    if (double.TryParse(parts[0], out double start))
                        if (double.TryParse(parts[1], out double end))
                            spectrogram.AddLabel(start, end, parts[2]);

            }
        }
        else
        {
            Debug.WriteLine($"No label file at {textFile}");
        }
    }

    /// <summary>
    /// Reads lines from a text file and adds any in form 'double double string' to a list
    /// </summary>
    /// <param name="textFile"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private List<string> GetLabelLines(string textFile)
    {

        var lines = new List<string>();
        var allLines = File.ReadAllLines(textFile).ToList();
        foreach (string line in allLines ?? new List<string>())
        {
            if (line.StartsWith("/") || line.StartsWith("\\")) continue;
            if (line[0].IsNumber() && line.Contains('\t'))
                lines.Add(line);
        }
        return (lines);
    }



    /// <summary>
    /// Replaces or creates a sidecar text file for the current wavfile, using the current label data
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task SaveTextFile(bool IsModified = false)
    {
        if (SpToolbar.viewModel.IsModified() || IsModified)
        {
            if (segmentLoaded)
            {
                await DisplayAlertSP("Info", "Segment loaded - no save performed", "OK");
                SpToolbar.viewModel.SetUnmodified();
                return;
            }
            if (!string.IsNullOrWhiteSpace(SpToolbar.viewModel.CurrentFile) && !string.IsNullOrWhiteSpace(SpToolbar.viewModel.currentFolder))
            {
                string file = Path.Combine(SpToolbar.viewModel.currentFolder, SpToolbar.viewModel.CurrentFile);
                string textFile = Path.ChangeExtension(file, ".txt");
                if (File.Exists(textFile))
                {
                    string bakfile = Path.ChangeExtension(file, ".bak");
                    if (File.Exists(bakfile))
                    {
                        File.Delete(bakfile);
                    }
                    File.Copy(textFile, bakfile);
                    File.Delete(textFile);
                }
                string text = spectrogram.GetLabelText();
                File.WriteAllText(textFile, file + "\n");
                File.AppendAllText(textFile, text);
                Debug.WriteLine($"send to {file}\n{text}\n");
                SpToolbar.viewModel.SetUnmodified();
            }
        }
    }

    [RelayCommand]
    private void ToggleWaveform()
    {
        WaveformVisibility = !WaveformVisibility;
        if (WaveformVisibility) { WaveformHeight = 100; }
        else { WaveformHeight = 0; }

    }

    [RelayCommand]
    private void ToggleButtonView()
    {
        ButtonVisibility = !ButtonVisibility;
        if (ButtonVisibility)
        {
            ButtonViewHeight = 200;
        }
        else { ButtonViewHeight = 0; }
    }

    [RelayCommand]
    private void GotoStart()
    {
        spectrogram.PanToStart();
    }

    [RelayCommand]
    private void GotoEnd()
    {
        spectrogram.PanToEnd();
    }


    [RelayCommand]
    private void PageBack()
    {
        spectrogram.PageBack();

    }

    [RelayCommand]
    private void PageForward()
    {
        spectrogram.PageForward();
    }

    [RelayCommand]
    private void ToggleAutoAdvance()
    {
        SpControls.ToggleAutoAdvance();
    }

    [RelayCommand]
    private void CommentReturn()
    {
        if (selectionStart != 0 || selectionEnd != 0)
        {
            spectrogram.AddLabel(selectionStart, selectionEnd, comment);
            AutoAdvance(SpectrogramControls.AUTOADVANCEMODE.TEXT);
        }
    }

    /// <summary>
    /// Advances to the next file based on the specified auto-advance mode.
    /// </summary>
    /// <remarks>This method checks the current auto-advance state and mode before performing the operation. 
    /// If the current auto-advance state is <see cref="SpectrogramControls.AUTOADVANCEMODE.OFF"/>,  the method does
    /// nothing. Otherwise, it advances to the next file if the specified mode matches  the current auto-advance mode or
    /// is compatible with it.</remarks>
    /// <param name="mode">The auto-advance mode that triggers the operation. Valid values are  <see
    /// cref="SpectrogramControls.AUTOADVANCEMODE.TEXT"/>,  <see cref="SpectrogramControls.AUTOADVANCEMODE.BUTTON"/>, or
    /// <see cref="SpectrogramControls.AUTOADVANCEMODE.BOTH"/>.</param>
    private void AutoAdvance(SpectrogramControls.AUTOADVANCEMODE mode, bool IsModified = false)
    {
        if (SpControls.CurrentAutoAdvanceState != SpectrogramControls.AUTOADVANCEMODE.OFF)
        {
            switch (SpControls.CurrentAutoAdvanceState)
            {
                case SpectrogramControls.AUTOADVANCEMODE.TEXT:
                    if (mode == SpectrogramControls.AUTOADVANCEMODE.TEXT)
                    {
                        NextFile(IsModified);
                    }
                    break;
                case SpectrogramControls.AUTOADVANCEMODE.BUTTON:
                    if (mode == SpectrogramControls.AUTOADVANCEMODE.BUTTON)
                    {
                        NextFile(IsModified);
                    }
                    break;
                case SpectrogramControls.AUTOADVANCEMODE.BOTH:
                    if (mode == SpectrogramControls.AUTOADVANCEMODE.BUTTON || mode == SpectrogramControls.AUTOADVANCEMODE.TEXT)
                    {
                        NextFile(IsModified);
                    }
                    break;
                default: break;
            }
        }
    }

    private void NextFile(bool IsModified = false)
    {
        SpToolbar.NextFile(IsModified);
    }

    private bool _commentryHasFocus = false;
    public bool CommentryHasFocus { get => _commentryHasFocus; set { _commentryHasFocus = value; OnPropertyChanged(); } }


    public string comment { get; set; } = "";

    public async Task<FileResult> PickAndShow(PickOptions options)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(options);


            return result;
        }
        catch (Exception ex)
        {
            // The user canceled or something went wrong
        }

        return null;
    }

    internal bool segmentLoaded = false;

    /// <summary>
    /// Given a fully qualified, but unchecked, filename and start and end offsets,
    /// opens the file and zooms to the specified extents
    /// </summary>
    /// <param name="file"></param>
    /// <param name="startOffsetTimeSpan"></param>
    /// <param name="endOffsetTimeSpan"></param>
    /// <exception cref="NotImplementedException"></exception>
    public async Task LoadSegment(string file, TimeSpan startOffsetTimeSpan, TimeSpan endOffsetTimeSpan, List<LabelItem> labelList)
    {
        if (Validate(file))
        {

            NextButtonEnabled = false;
            PrevButtonEnabled = false;
            //await Save();


            //await ReadFile(file, startOffsetTimeSpan.TotalSeconds, endOffsetTimeSpan.TotalSeconds);
            await ReadFile(file, AddLabels: (labelList is null || !labelList.Any()));
            segmentLoaded = true;
            foreach (var item in labelList ?? new List<LabelItem>())
            {
                spectrogram.AddLabel(item.startOffset, item.endOffset, item.idedBats);
            }
            spectrogram.ZoomToSecs(startOffsetTimeSpan.TotalSeconds, endOffsetTimeSpan.TotalSeconds);
            SpToolbar.DisableFileButtons();
            SpControls.CurrentAutoAdvanceMode = SpectrogramControls.AUTOADVANCEMODE.OFF;
            SpControls.CurrentAutoAdvanceState = SpectrogramControls.AUTOADVANCEMODE.OFF;


        }
    }

    private void Zoom(double start, double end)
    {
        spectrogram.Zoom(start, end);
    }

    /// <summary>
    /// Checks  to see if a fully qualified filename exists
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private bool Validate(string file)
    {
        if (string.IsNullOrWhiteSpace(file)) return false;
        if (File.Exists(file)) return true;
        return (false);
    }

    internal async Task DisplayAlertSP(string v1, string v2, string v3)
    {
        // Use the Application.Current.MainPage to display the alert, since ContentView does not have DisplayAlert
        if (Application.Current?.MainPage != null)
        {
            await Application.Current.MainPage.DisplayAlert(v1, v2, v3);
        }
    }

    /// <summary>
    /// Called when the next button is clicked and there are no more files in the file list
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    internal async Task AnalysisCompleted()
    {
        await Save();
        bool answer = await Application.Current.MainPage.DisplayAlert("End of files", "Import This Session?", "Yes", "No");
        if (answer)
        {
            OnAnalysisCompleted(new FileEventArgs(Path.Combine(SpToolbar.viewModel.currentFolder, SpToolbar.viewModel.CurrentFile)));


        }
    }

    public void Stop()
    {
        try
        {
            Debug.WriteLine("Stopping spectrogram");
            spectrogram?.Stop();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error stopping spectrogram: {ex.Message}");
        }
    }

    public void Close()
    {
        spectrogram?.Stop();
        spectrogram?.Dispose();
        spectrogram = null;
    }

    public void Dispose()
    {

        Close();
    }
}

/// <summary>
/// Used to return a folder/file on completyion of analysis
/// </summary>
public class FileEventArgs
{
    public string folder { get; set; } = "";
    public string file { get; set; } = "";

    public FileEventArgs(string FQFileName)
    {
        if (!string.IsNullOrWhiteSpace(FQFileName))
        {
            this.folder = Path.GetDirectoryName(FQFileName) ?? "";
            this.file = Path.GetFileName(FQFileName);
        }
    }
}

