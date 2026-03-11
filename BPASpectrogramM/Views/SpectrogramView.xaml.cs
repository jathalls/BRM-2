using BPASpectrogramM.Navigation;
using CommunityToolkit.Mvvm.Input;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Syncfusion.Maui.Gauges;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;


namespace BPASpectrogramM.Views;

public partial class SpectrogramView : ContentView, INotifyPropertyChanged,IDisposable
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string PropertyName = "")
    {
        PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(PropertyName));
    }

    public event EventHandler<SpectrogramSelectionChangedEventArgs> SelectionChanged;
    protected virtual void OnSelectionChanged(SpectrogramSelectionChangedEventArgs e) => SelectionChanged?.Invoke(this, e);

    public event EventHandler<EventArgs> PlaySelection;
    protected virtual void OnPlaySelection(EventArgs e) => PlaySelection?.Invoke(this, e);

    private double _frequencyScaleStart = 0;
    private double _frequencyScaleEnd = 192;
    public double FrequencyScaleStart { get => _frequencyScaleStart; set { _frequencyScaleStart = value; OnPropertyChanged(); } }
    public double FrequencyScaleEnd { get => _frequencyScaleEnd; set { _frequencyScaleEnd= value; OnPropertyChanged(); } } 

    private double _timeScaleStart = 0;
    private double _timeScaleEnd = 5.0d;
    public double TimeScaleStart { get => StartOfSpectrogramInFFTs/FFTsPerSec; set { _timeScaleStart = value; OnPropertyChanged(); } } 
    public double TimeScaleEnd { get => EndOfSpectrogramInFFTs/FFTsPerSec; set { _timeScaleEnd= value; OnPropertyChanged(); } }

    private bool _isBusyRunning = false;
    public bool IsBusyRunning { get=>_isBusyRunning;set { _isBusyRunning = value; OnPropertyChanged(); } }

    private double _intesityValue = 16;
    public double IntensityValue { get => _intesityValue; set { _intesityValue= value; OnPropertyChanged(); } }

    private double _maxIntensity = 30;
    public double MaxIntensity { get => _maxIntensity; set { _maxIntensity = value; OnPropertyChanged(); } }

    private double _minIntensity = 0;
    public double MinIntensity { get => _minIntensity; set { _minIntensity = value; OnPropertyChanged(); } }

    private string CurrentFile = "";

    private double PlayHeadPositionSecs = 0.0;

    
    private bool _audioPlayerVisibility = false;

    public bool AudioPlayerVisibility 
    { 
        get => _audioPlayerVisibility; 
        set { _audioPlayerVisibility = value; OnPropertyChanged(); } 
    }


    private SKBitmap _bitmap = new SKBitmap(1000,512);

    private SKSurface? _skSurface = null;

    public bool IsModified=false;

    public SpectrogramView()
	{
        Setup();
		InitializeComponent();
        BindingContext = this;

         CanvasView.Touch += DoTouch;
        audioPlayer.PlayBackUpdated += AudioPlayer_PlayBackUpdated;

        /*
        
        string imagePath = System.IO.Path.Combine(@".", "BatRef.wav");
        if (!File.Exists(imagePath))
        {
            imagePath= System.IO.Path.Combine(@"..", "BatRef.wav");
        }
        if (File.Exists(imagePath))
        {
            try
            {
                _bitmap = GetSpectrogram(imagePath);
                CanvasView.InvalidateSurface();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }


        else
        {
            // Handle missing file scenario
            _bitmap =new SKBitmap(1000,512);
            CanvasView.InvalidateSurface();
        }*/
        _bitmap = new SKBitmap(1000, 512);
        IsModified = false;
    }

    private void AudioPlayer_PlayBackUpdated(object? sender, FileEventArgs e)
    {
        SetPlayhead(audioPlayer.GetPosition());
    }

    private void Setup()
    {
        //Preferences.Default.Clear();
        FFTSize=(int)GetSetPreference(nameof(FFTSize), (int)1024);
        FFTStepSize = (int)GetSetPreference(nameof(FFTStepSize), 512);
        PrefferredViewLengthSecs = (float)GetSetPreference(nameof(PrefferredViewLengthSecs), 5.0f);
        CurrentColorMap = (string)GetSetPreference(nameof(CurrentColorMap), "Grayscale Reversed");
        Intensity = (double)GetSetPreference(nameof(Intensity), 10.0d);
        IntensityValue = Intensity;
        /*foreach(var cm in Spectrogram.Colormap.GetColormapNames())
        {
            Debug.WriteLine($"Colormap: {cm}");
        }*/
        
    }

    private object GetSetPreference(string key, object defaultValue)
    {
        if (Preferences.Default.ContainsKey(key)) 
        {
            var result = defaultValue;
            string resultStr= Preferences.Default.Get(key, defaultValue).ToString();
            if (!String.IsNullOrWhiteSpace(resultStr))
            {
                 result = Resolve(resultStr, defaultValue);
            }
            return result;
        }
        else if (Preferences.Default.ContainsKey($"Default_{key}"))
        {
            var result = defaultValue;
            string resultStr= Preferences.Default.Get($"Default_{key}", defaultValue).ToString();
            if (!String.IsNullOrWhiteSpace(resultStr))
            {
                 result = Resolve(resultStr, defaultValue);
            }
            return result;
        }
        else
        {

            if (defaultValue is Boolean boolVal) Preferences.Set($"Default_{key}", boolVal);
            else if (defaultValue is Double dblVal) Preferences.Set($"Default_{key}", dblVal);
            else if (defaultValue is Int32 intVal) Preferences.Set($"Default_{key}", intVal);
            else if (defaultValue is Single snglVal) Preferences.Set($"Default_{key}", snglVal);
            else if (defaultValue is Int64 lngVal) Preferences.Set($"Default_{key}", lngVal);
            else if (defaultValue is String strVal) Preferences.Set($"Default_{key}", strVal);
            else if (defaultValue is DateTime dtVal) Preferences.Set($"Default_{key}", dtVal);
            else Preferences.Set($"Default_{key}", defaultValue.ToString());



        }
        return defaultValue;

    }

    private object Resolve(string resultStr,object defaultValue)
    {
        if (defaultValue is Boolean boolVal) return (bool.Parse(resultStr));
        else if (defaultValue is Double dblVal) return Double.Parse(resultStr);
        else if (defaultValue is Int32 intVal) return int.Parse(resultStr);
        else if (defaultValue is Single snglVal) return float.Parse(resultStr);
        else if (defaultValue is Int64 lngVal) return long.Parse(resultStr);
        else if (defaultValue is String strVal) return resultStr;
        else if (defaultValue is DateTime dtVal) return DateTime.Parse(resultStr);
        else return resultStr;
    }

    private SKPoint? mouseRightDownLocation = null;
    private SKPoint? mouseLeftDownLocation = null;
    private SKPoint? mouseLeftUpLocation = null;
    private SKPoint? mouseMiddleDownLocation = null;
    private SKPoint? mouseCurrentLocation = null;

    private void DoTouch(object? sender, SKTouchEventArgs e)
    {
        if (e.MouseButton == SKMouseButton.Left) // select a region to add a label
        {
            HandleLeftMouseButton(e);
            e.Handled = true;
            CanvasView.InvalidateSurface();
            return;

        }
        if (e.MouseButton == SKMouseButton.Middle) // Zoom to a selected region or if clciked zoom extents
        {
            

            if (e.ActionType == SKTouchAction.Pressed)
            {
                mouseMiddleDownLocation = e.Location;
                Debug.WriteLine($"Middle pressed at {mouseMiddleDownLocation?.X}");
            } else if(e.ActionType == SKTouchAction.Released)
            {
                Debug.WriteLine($"Relesaed at {e.Location.X}");
                if (e.Location.X == (mouseMiddleDownLocation?.X ?? e.Location.X + 1))
                {
                    ZoomOut();
                }
                else
                {
                    Zoom(e.Location.X, mouseMiddleDownLocation?.X ?? e.Location.X);
                }
                mouseMiddleDownLocation = null;
            }
            e.Handled = true;
            return;
        }

        if(e.MouseButton == SKMouseButton.Right) // Drag the spectrogram left and right
        {
            var position = e.Location;
            if (mouseRightDownLocation != null) 
            {
                Pan(position);
                mouseRightDownLocation = position;
            }

            if (e.ActionType == SKTouchAction.Pressed) {
                mouseRightDownLocation = position;
            }else if (e.ActionType == SKTouchAction.Released)
            {
                
                mouseRightDownLocation = null;
            }
            e.Handled = true;
            return;
            
        }

        if (e.WheelDelta < 0)
        {
            float midpoint = StartOfSpectrogramInFFTs + (SpectrogramLengthInFFTs / 2.0f);
            /*
            SpectrogramLengthInFFTs = SpectrogramLengthInFFTs * 1.2f;
            EndOfSpectrogramInFFTs = midpoint + (SpectrogramLengthInFFTs / 2.0f);
            StartOfSpectrogramInFFTs = EndOfSpectrogramInFFTs - SpectrogramLengthInFFTs;*/
            Debug.WriteLine($"start < {StartOfSpectrogramInFFTs}-{EndOfSpectrogramInFFTs}={SpectrogramLengthInFFTs}");
            var delta = SpectrogramLengthInFFTs * 0.2f;
            EndOfSpectrogramInFFTs += delta / 2;
            StartOfSpectrogramInFFTs -= delta / 2;
            SpectrogramLengthInFFTs = EndOfSpectrogramInFFTs - StartOfSpectrogramInFFTs;
            Debug.WriteLine($"Delta {delta}=> {StartOfSpectrogramInFFTs}-{EndOfSpectrogramInFFTs}={SpectrogramLengthInFFTs}");
            if (SpectrogramLengthInFFTs < 0)
            {
                Debug.WriteLine("Error");
            }

            NormalizeSpectrogram();
            //if (EndOfSpectrogramInFFTs > _bitmap.Width) EndOfSpectrogramInFFTs = _bitmap.Width;
            //StartOfSpectrogramInFFTs = EndOfSpectrogramInFFTs - SpectrogramLengthInFFTs;
            //if (StartOfSpectrogramInFFTs < 0) StartOfSpectrogramInFFTs = 0;
            //SpectrogramLengthInFFTs = EndOfSpectrogramInFFTs - StartOfSpectrogramInFFTs;
            e.Handled = true;
            CanvasView.InvalidateSurface();
            return;
        }

        if (e.WheelDelta > 0)
        {
            float midpoint = StartOfSpectrogramInFFTs + (SpectrogramLengthInFFTs / 2.0f);
            /*SpectrogramLengthInFFTs = SpectrogramLengthInFFTs / 1.2f;
            EndOfSpectrogramInFFTs = midpoint + (SpectrogramLengthInFFTs / 2.0f);
            StartOfSpectrogramInFFTs = EndOfSpectrogramInFFTs - SpectrogramLengthInFFTs;*/
            Debug.WriteLine($"start > {StartOfSpectrogramInFFTs}-{EndOfSpectrogramInFFTs}={SpectrogramLengthInFFTs}");
            var delta = SpectrogramLengthInFFTs * 0.2f;
            EndOfSpectrogramInFFTs-= delta / 2;
            StartOfSpectrogramInFFTs += delta / 2;
            SpectrogramLengthInFFTs = EndOfSpectrogramInFFTs - StartOfSpectrogramInFFTs;
            Debug.WriteLine($"Delta {delta}=> {StartOfSpectrogramInFFTs}-{EndOfSpectrogramInFFTs}={SpectrogramLengthInFFTs}");
            if (SpectrogramLengthInFFTs < 0)
            {
                Debug.WriteLine("Error");
            }
            NormalizeSpectrogram();
            //if (EndOfSpectrogramInFFTs > _bitmap.Width) EndOfSpectrogramInFFTs = _bitmap.Width;
            //StartOfSpectrogramInFFTs = EndOfSpectrogramInFFTs - SpectrogramLengthInFFTs;
            //if (StartOfSpectrogramInFFTs < 0) StartOfSpectrogramInFFTs = 0;
            //SpectrogramLengthInFFTs = EndOfSpectrogramInFFTs - StartOfSpectrogramInFFTs;
            e.Handled = true;
            CanvasView.InvalidateSurface();
            return;
        }
    }

    /// <summary>
    /// Deals with the left mouse button, dragging for a selection,
    /// click to clear the current selection
    /// </summary>
    /// <param name="e"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void HandleLeftMouseButton(SKTouchEventArgs e)
    {
        try
        {
            if (e.ActionType == SKTouchAction.Pressed)
            { // start a new selection
                selection = new Selection(e.Location, CanvasView,
                    new SKRect(StartOfSpectrogramInFFTs, (float)FrequencyRangeEnd, EndOfSpectrogramInFFTs, (float)FrequencyRangeStart));

            }
            else if (e.ActionType == SKTouchAction.Released)
            { // if a click, cancel the selection, otherwise just update it
                if (e.Location == (selection?.startPosition ?? new SKPoint()))
                {
                    selection = null;
                    AudioPlayerVisibility = false;
                    audioPlayer.IsVisible = false;
                    audioPlayer.Stop();
                }
                else
                {
                    selection?.Update(e.Location, CanvasView,
                        new SKRect(StartOfSpectrogramInFFTs, (float)FrequencyRangeEnd, EndOfSpectrogramInFFTs, (float)FrequencyRangeStart));
                    //AudioPlayerVisibility = true;
                    //audioPlayer.IsVisible = true;

                    audioPlayer.LoadSegment(CurrentFile,
                        TimeSpan.FromSeconds(selection?.startFFTs??StartOfSpectrogramInFFTs / FFTsPerSec),
                        TimeSpan.FromSeconds(selection?.endFFTs??EndOfSpectrogramInFFTs / FFTsPerSec));
                }
                OnSelectionChanged(new SpectrogramSelectionChangedEventArgs(
                        CurrentFile, (selection?.startFFTs ?? 0) / FFTsPerSec, (selection?.endFFTs ?? 0) / FFTsPerSec));
            }
            else
            {
                selection?.Update(e.Location, CanvasView,
                   new SKRect(StartOfSpectrogramInFFTs, (float)FrequencyRangeEnd, EndOfSpectrogramInFFTs, (float)FrequencyRangeStart));
                if (e.Location.X > CanvasView.Width)
                {
                    PanScrollLeft();
                }
                else if (e.Location.X < 0)
                {
                    PanScrollRight();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"HandleLeftMouseButton:- {ex.Message}");
        }
    }

    /// <summary>
    /// Moves the bitmap image to the right in the canvasview by adjusting the spectrogram start and end
    /// parameters
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void PanScrollRight()
    {
        var distanceToMove = SpectrogramLengthInFFTs / 20;
        if (distanceToMove > 0)
        {
            StartOfSpectrogramInFFTs-= distanceToMove;
            EndOfSpectrogramInFFTs -= distanceToMove;
            NormalizeSpectrogram();
        }
    }

    /// <summary>
    /// Moves the bitmap image to the left in the canvasview by adjustin the spectrogram start and end parameters
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void PanScrollLeft()
    {
        var distanceToMove = SpectrogramLengthInFFTs / 20;
        if(distanceToMove > 0)
        {
            StartOfSpectrogramInFFTs+= distanceToMove;
            EndOfSpectrogramInFFTs += distanceToMove;
            NormalizeSpectrogram();
        }
    }

    public class Selection
    {
        private float _lowFreq=0;
        private float _highFreq=0;
        private float _startFFTs=0;
        private float _endFFTs=0;

        public SKPoint? startPosition { get; set; }

        public float lowFreq { get => (float)Math.Min(_lowFreq, _highFreq); set => _lowFreq = value; }
    
        public float highFreq { get => (float)Math.Max(_lowFreq, _highFreq); set => _highFreq = value; }
        public float startFFTs { get => (float)Math.Min(_startFFTs, _endFFTs); set=> _startFFTs = value; }
        public float endFFTs { get => (float)Math.Max(_startFFTs, _endFFTs); set=> _endFFTs = value; }

        public Selection(SKPoint? startPosition,SKCanvasView canvas,SKRect dimensions)
        {
            this.startPosition = startPosition;
            var FrequencyRangeEnd = dimensions.Top;
            var FrequencyRangeStart = dimensions.Bottom;
            var SpectrogramStart=dimensions.Left;
            var SpectrogramEnd=dimensions.Right;

            var kHzPerPixel = Math.Abs(FrequencyRangeEnd - FrequencyRangeStart) / canvas.CanvasSize.Height;
            var FFTsPerPixel = Math.Abs(SpectrogramEnd-SpectrogramStart) / canvas.CanvasSize.Width;

            var localPos= (startPosition?.X ?? 0) * FFTsPerPixel;
            startFFTs = SpectrogramStart + localPos;
            endFFTs = SpectrogramStart + localPos;

            var fPos = canvas.CanvasSize.Height - (startPosition?.Y ?? 0);
            highFreq = fPos * kHzPerPixel;
            lowFreq = fPos * kHzPerPixel;

            Debug.WriteLine($"Start Selection at {startFFTs} {highFreq} from pixel {startPosition?.Y??-1}");  
        }

        public void Update(SKPoint? currentPosition,SKCanvasView canvas,SKRect dimensions)
        {
            var FrequencyRangeEnd = dimensions.Bottom;
            var FrequencyRangeStart = dimensions.Top;
            var SpectrogramStart = dimensions.Left;
            var SpectrogramEnd = dimensions.Right;

            var kHzPerPixel = Math.Abs(FrequencyRangeEnd - FrequencyRangeStart) / canvas.CanvasSize.Height;
            var FFTsPerPixel = Math.Abs(SpectrogramEnd - SpectrogramStart) / canvas.CanvasSize.Width;
            var localXpos= (currentPosition?.X ?? 0) * FFTsPerPixel;
            var fPos = canvas.CanvasSize.Height - (currentPosition?.Y ?? 0);
            endFFTs = localXpos + SpectrogramStart;
             lowFreq=fPos * kHzPerPixel;
            Debug.WriteLine($"Updated Frequency={lowFreq}-{highFreq}");
        }
    }

    private bool Panned = false;

    public Selection? selection { get; set; } = null;


    private void Zoom(float now, float then)
    {
        if(now==then) { 
        StartOfSpectrogramInFFTs = 0;
            EndOfSpectrogramInFFTs = TotalFFTs;
            SpectrogramLengthInFFTs = EndOfSpectrogramInFFTs - StartOfSpectrogramInFFTs;
            NormalizeSpectrogram() ;
            CanvasView.InvalidateSurface();
            return;
        }
        var meanPos = (then+now) / 2;
        var zoomScale = Math.Abs(then-now)/CanvasWidth;
        if (then > now) zoomScale = 1 / zoomScale; // drag to left = zoom out
        var newLengthInFFTs = SpectrogramLengthInFFTs * zoomScale;
        var newCentre = ((meanPos / CanvasWidth) * SpectrogramLengthInFFTs) + StartOfSpectrogramInFFTs;
        //SpectrogramLengthInFFTs = SpectrogramLengthInFFTs * zoomScale;
        StartOfSpectrogramInFFTs = newCentre - newLengthInFFTs / 2;
        if(StartOfSpectrogramInFFTs<0)StartOfSpectrogramInFFTs=0;
        EndOfSpectrogramInFFTs = StartOfSpectrogramInFFTs + newLengthInFFTs;
        if (EndOfSpectrogramInFFTs > _bitmap.Width)
        {
            EndOfSpectrogramInFFTs=_bitmap.Width;
            StartOfSpectrogramInFFTs = EndOfSpectrogramInFFTs - newLengthInFFTs;
        }
        SpectrogramLengthInFFTs = EndOfSpectrogramInFFTs - StartOfSpectrogramInFFTs;
        CanvasView.InvalidateSurface();
    }

    private void Pan(SKPoint position)
    {
        
        var Pixeldistance =  (mouseRightDownLocation?.X??position.X)- position.X ;
        PanPixelDistance(Pixeldistance);
    }

    private void PanPixelDistance(float Pixeldistance)
    {
        var proportion = Pixeldistance / CanvasWidth;
        var distance = SpectrogramLengthInFFTs * proportion;
        var currentLength = SpectrogramLengthInFFTs;
        
        //Debug.WriteLine($"from {mouseRightDownLocation?.X} to {position.X} => {distance}");
        if (distance != 0)
        {
            //Debug.WriteLine($"\tmove {StartOfSpectrogramInFFTs} to {StartOfSpectrogramInFFTs + distance}");
            StartOfSpectrogramInFFTs += distance;
            EndOfSpectrogramInFFTs = StartOfSpectrogramInFFTs + currentLength;
            NormalizeSpectrogram();
            CanvasView.InvalidateSurface();
        }
    }

    


    private SKBitmap? GetSpectrogram(string file)
    {
        sg = ReadMono(file);
        SKBitmap? SDbmp = null;
        if (sg!=null)
        {
            try
            {
                sg.Colormap = Spectrogram.Colormap.GetColormap(CurrentColorMap);
                Debug.WriteLine($"[GetSpectrogram] Calling GetBitmap for {sg.Width} x {sg.Height} frames…");
                var sw = System.Diagnostics.Stopwatch.StartNew();
                SDbmp = sg?.GetBitmap(dB: true, intensity: Intensity);
                sw.Stop();
                Debug.WriteLine($"[GetSpectrogram] GetBitmap completed in {sw.ElapsedMilliseconds} ms. Bitmap: {SDbmp?.Width}x{SDbmp?.Height}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetSpectrogram:- {ex.Message}");
                sg = null;
                SDbmp?.Dispose();
                SDbmp = new SKBitmap(1000, 512);
                IsModified = false;
            }
        }

        return SDbmp;
    }

    private double DurationSecs;
    private double SampleRate;

    private int _totalFFTs=0;
    public int TotalFFTs
    {
        get =>_totalFFTs;
        set
        {
            _totalFFTs = value;
            OnPropertyChanged();
        }
    }
    private double FFTsPerSec;
    private double _fMax;
    private double zoomedfMax;
    private double fMin;
    private double zoomedfMin;
    private int FFTSize;
    private int FFTStepSize;
    private double PrefferredViewLengthSecs;
    private string CurrentColorMap;
    private double Intensity;
    public double MaxFrequency { get => _fMax; set { _fMax = value; OnPropertyChanged(); } }

    private double _frequencyRangeStart=0;
        private double _frequencyRangeEnd=100;

    public double FrequencyRangeStart 
    { 
        get => _frequencyRangeStart; 
        set 
        { 
            _frequencyRangeStart = value; 
            OnPropertyChanged(); 
            
        } 
    }
    public double FrequencyRangeEnd 
    { 
        get => _frequencyRangeEnd; 
        set 
        { 
            _frequencyRangeEnd = value; 
            OnPropertyChanged(); 
            
        }
    }

    /// <summary>
    /// Maximum number of seconds of audio to process into FFT frames on initial load.
    /// Prevents startup hang on very long files (e.g. 60-second reference files at 384 kHz
    /// which would produce ~46,000 FFT columns and a ~94 MB bitmap that blocks the thread pool).
    /// Full-file metadata (TotalFFTs, DurationSecs) is still calculated from the WAV header so
    /// the time-axis navigation remains accurate.
    /// </summary>
    private const double MaxInitialLoadDurationSecs = 300.0;

    private Spectrogram.SpectrogramGenerator sg = null;

    Spectrogram.SpectrogramGenerator? ReadMono(string filePath, double multiplier = 16000,
        double maxLoadDurationSecs = MaxInitialLoadDurationSecs)
    {
        if(!File.Exists(filePath))
        {
            return null;
        }
        CurrentFile = filePath;
        IsBusyRunning = true;
        try
        {
            using (var afr = new AudioFileReaderM(filePath))
            {
                int sampleRate = afr.SampleRate;
                int bytesPerSample = afr.FormatInfo.BitsPerSample / 8;
                int bytesPerFrame = bytesPerSample * afr.Channels;
                int sampleCount = afr.FormatInfo.AudioDataSize / bytesPerFrame;
                SampleRate = sampleRate;
                DurationSecs = sampleCount / SampleRate;
                TotalFFTs = (int)(sampleCount / FFTStepSize); // at an advance of 512 samples per FFT
                FFTsPerSec = TotalFFTs / DurationSecs;
                SpectrogramLengthInFFTs = (float)(PrefferredViewLengthSecs * FFTsPerSec);
                EndOfSpectrogramInFFTs = SpectrogramLengthInFFTs;
                StartOfSpectrogramInFFTs = 0;
                fMin = 0;

                MaxFrequency = sampleRate / 2000;
                FrequencyRangeStart = 0;
                FrequencyRangeEnd = Math.Max(100,MaxFrequency/2);
                int channelCount = afr.FormatInfo.ChannelCount;
                sg = new Spectrogram.SpectrogramGenerator(sampleRate, fftSize: FFTSize, stepSize: FFTStepSize, maxFreq: sampleRate / 2);

                if (sg != null)
                {
                    // Limit how many samples we feed into the spectrogram generator on first load.
                    // For a 384 kHz file, every second produces 750 FFT columns (384000/512).
                    // Loading the entire 61-second reference file upfront creates ~46 k columns,
                    // a ~94 MB bitmap, and causes the thread pool to hang.
                    long maxSamplesToLoad = maxLoadDurationSecs > 0
                        ? (long)(maxLoadDurationSecs * sampleRate * channelCount)
                        : long.MaxValue;

                    if (DurationSecs > maxLoadDurationSecs && maxLoadDurationSecs > 0)
                        Debug.WriteLine($"[ReadMono] File is {DurationSecs:F1}s — loading first {maxLoadDurationSecs:F0}s to avoid startup hang.");

                    var buffer = new float[sampleRate * channelCount];
                    int samplesRead = 0;
                    long totalSamplesAdded = 0;
                    while ((samplesRead = afr.Read(buffer)) > 0)
                    {
                        int samplesToAdd = (int)Math.Min(samplesRead, maxSamplesToLoad - totalSamplesAdded);
                        if (samplesToAdd <= 0) break;
                        sg.Add(buffer.Take(samplesToAdd).Select(x => (double)(x * multiplier)));
                        totalSamplesAdded += samplesToAdd;
                        if (totalSamplesAdded >= maxSamplesToLoad) break;
                    }

                    Debug.WriteLine($"[ReadMono] Loaded {sg.Width} FFT frames ({sg.Width / FFTsPerSec:F1}s of audio).");
                }
            }
        }
        catch (Exception ex) { Debug.WriteLine($"ReadMono:- {ex.Message}"); sg = null; }
        finally
        {
            IsBusyRunning = false;
        }
        return (sg);
    }

    // Redraw the canvas when the bitmap is loaded
    private float _startInFFTs = 0;
    private float _endInFFTs = 1000;
    public float StartOfSpectrogramInFFTs 
    { 
        get => _startInFFTs; 
        set 
        {  
            _startInFFTs = value; 
            OnPropertyChanged(nameof(TimeScaleStart));
            OnPropertyChanged();
            
        } 
    } 
    public float EndOfSpectrogramInFFTs 
    { 
        get => _endInFFTs; 
        set 
        { 
            _endInFFTs = value; 
            OnPropertyChanged(nameof(TimeScaleEnd));

            OnPropertyChanged();
        } 
    }
    public float SpectrogramLengthInFFTs { get; set; }
    int CanvasWidth = 1000;


    private void  OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        if(sg is null || _bitmap is null)
        {
            
            return;
        }
        var canvas = e.Surface.Canvas;
        CanvasWidth = e.Info.Width;
        // Clear the canvas
        canvas.Clear(SKColors.Bisque);

        if (_bitmap != null)
        {
            // Draw the bitmap centered on the canvas
            float x = (e.Info.Width - _bitmap.Width) / 2f;
            float y = (e.Info.Height - _bitmap.Height) / 2f;

            var frequencyChunkSize = _bitmap.Height / MaxFrequency; // Rows/kHz
            var fStart = _bitmap.Height-(FrequencyRangeStart * frequencyChunkSize);
            var fEnd= _bitmap.Height-(FrequencyRangeEnd * frequencyChunkSize);

            //Debug.WriteLine($"for {FrequencyRangeStart}-{FrequencyRangeEnd}kHz, top={fEnd} bottom={fStart} out of {_bitmap.Height}");
            
            SKRect dst = new SKRect(0, 0, e.Info.Width, e.Info.Height);
            SKRect src = new SKRect(StartOfSpectrogramInFFTs, (float)fEnd, EndOfSpectrogramInFFTs, (float)fStart);
            
            var sampling=new SKSamplingOptions(SKFilterMode.Linear,SKMipmapMode.Linear);

            //canvas.DrawBitmap(_bitmap, src, dst);
            if (_skSurface != null)
            {
                canvas.DrawImage(_skSurface.Snapshot(), src, dst, sampling);
            }
            //canvas.DrawBitmap(_bitmap, x, y);
            //Debug.WriteLine($"Origin is {x}, {y}");
            if (selection != null)
            {
                var paint = new SKPaint
                {

                    IsAntialias = true,
                    Color = new SKColor(100, 100, 100, 50),
                    Style = SKPaintStyle.Fill
                };

                var xMin = Math.Max(0, selection.startFFTs-StartOfSpectrogramInFFTs );
                var xMax = Math.Min(SpectrogramLengthInFFTs, selection.endFFTs-StartOfSpectrogramInFFTs);
                var fMin = Math.Max(FrequencyRangeStart, selection.lowFreq-FrequencyRangeStart);
                var fMax=Math.Min(FrequencyRangeEnd, selection.highFreq-FrequencyRangeStart);
                //Debug.WriteLine($"Selected {selection.lowFreq}-{selection.highFreq}kHz");
                //Debug.WriteLine($"Shade kHz {fMin}-{fMax}");

                var fScale = e.Info.Height / (FrequencyRangeEnd - FrequencyRangeStart);
                var pixelsPerFFT=e.Info.Width/SpectrogramLengthInFFTs;

                var xMinC = xMin * pixelsPerFFT;
                var xMaxC = xMax * pixelsPerFFT;
                var yMinC = CanvasView.CanvasSize.Height-(fMin * fScale);
                var yMaxC = CanvasView.CanvasSize.Height-(fMax * fScale);

                //Debug.WriteLine($"Shade Pixels {yMinC}-{yMaxC}");

                //Debug.WriteLine($"Rect {xMaxC - xMinC} x {yMaxC - yMinC}");
                if (xMaxC > xMinC)
                {
                    SKRect shadow = new SKRect(xMinC, (float)yMaxC, xMaxC, (float)yMinC);
                    canvas.DrawRect(shadow, paint);
                }
               

                
            }
            
            DrawGrid(canvas,e.Info.Width,e.Info.Height);
        }
        else
        {
            // Draw a placeholder message if the bitmap is null
            using var paint = new SKPaint
            {
                Color = SKColors.Red,
                TextSize = 40,
                IsAntialias = true
            };
            canvas.DrawText("Image not found", 50, 100, paint);
        }
    }

    /// <summary>
    /// Draws a light red grid over the spectrogram
    /// </summary>
    /// <param name="canvas"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void DrawGrid(SKCanvas canvas,int width,int height)
    {
        
        FrequencyScaleStart = FrequencyRangeStart;
        FrequencyScaleEnd = FrequencyRangeEnd;
        var FrequencyRange=FrequencyScaleEnd - FrequencyScaleStart;
        var scale = height/FrequencyRange;
        var step = (float)(scale * 10.0f);
        var redPaint= new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(255, 0, 0, 255),
            Style = SKPaintStyle.StrokeAndFill
        };
        var paint = new SKPaint
        {

            IsAntialias = true,
            Color = new SKColor(100, 0, 0, 50),
            Style = SKPaintStyle.StrokeAndFill
        };
        
        for (float y=0;y< height; y+=step)
        {
            canvas.DrawLine(0,height-y,width,height-y,paint);
            //Debug.WriteLine($"h={height}, y={y} gap={step}");
        }

        canvas.DrawLine(width/2,0,width/2,height,paint);
        //Debug.WriteLine($"PlayheadPositionSecs={PlayHeadPositionSecs}, FFTsPerSec={FFTsPerSec}, StartOfSpectrogramInFFTs={StartOfSpectrogramInFFTs}");
        if (PlayHeadPositionSecs>0 && selection!=null)
        {
            
            var xPos = (float)((PlayHeadPositionSecs * FFTsPerSec) - StartOfSpectrogramInFFTs);
            var pixelsPerFFT = width / SpectrogramLengthInFFTs;
            xPos = xPos * pixelsPerFFT;
            canvas.DrawLine(xPos, 0, xPos, height, redPaint);
            //Debug.WriteLine($"Draw playhead at {PlayHeadPositionSecs}s, xpos={xPos}/{width}");
        }

    }

    List<LabelItem> currentLabels=new List<LabelItem>();
    /// <summary>
    /// Adds pointers to the time scale at the start and end of the label, and a content pointer
    /// holding the text in the middle
    /// </summary>
    /// <param name="startSecs"></param>
    /// <param name="endSecs"></param>
    /// <param name="label"></param>
    public void AddLabel(double startSecs,double endSecs,string label,int level=-1)
    {
        IsModified = true;
        if (startSecs==0 && endSecs == 0)
        {
            startSecs = 0;
            endSecs = DurationSecs;
        }
        int overlaps = 0;
        if(startSecs<0 || endSecs>DurationSecs || startSecs==endSecs) { return; }
        if (level < 0)
        {
            overlaps=getOverlaps(startSecs, endSecs);
            if (overlaps < 0)
            {
                MergeLabels(startSecs, endSecs,label);
                return;
            }
        }

        SfLinearGauge currentTrack = TimeScale;
        int NumberOfExistingTracks = TimeStack.Children.Count;
        Debug.WriteLine($"overlaps={level}, number of tracks={NumberOfExistingTracks}");

        var scalelevel = NumberOfExistingTracks - 1;
        var desiredLevel = scalelevel - 1 - overlaps;


        if (desiredLevel >=0 && desiredLevel<NumberOfExistingTracks)
        {
            if (TimeStack.Children[desiredLevel] is SfLinearGauge g)
            {
                currentTrack = g;
                Debug.WriteLine($"use existing track [{desiredLevel}]");
            }
        }
        else
        {
            currentTrack=MakeNewTimeScale();
            TimeStack.Children.Insert(0, currentTrack);
            //TimeStack.Children.Add(currentTrack);
            Debug.WriteLine($"Insert new track [0]");
        }
        currentLabels.Add(new LabelItem(label, startSecs,endSecs));
            //TimeScale.MarkerPointers.Clear();
            
         
        //Debug.WriteLine($"Add Label {startSecs}-{endSecs}-{label}");
        
        
        if (startSecs != endSecs)
        {
            if (label.Contains(@"Clear()"))
            {
                                currentTrack.Ranges.Clear();
                currentTrack.MarkerPointers.Clear();
                currentLabels.Clear();
                return;
            }
            currentTrack.Ranges.Add(new LinearRange()
            {
                StartValue = startSecs,
                EndValue=endSecs,
                Fill=Colors.Blue,
                
            });
            //g2.MarkerPointers.Add(new LinearShapePointer() { Value = startSecs });
            //g2.MarkerPointers.Add(new LinearShapePointer() { Value = endSecs });
            LinearContentPointer contentPointer = new LinearContentPointer();
            contentPointer.Value = (startSecs + endSecs) / 2;
            Label cplabel = new Label();
            cplabel.Text = label;
            
            cplabel.FontSize = 8;
            contentPointer.Content = cplabel;
            contentPointer.OffsetY = -3;
            currentTrack.MarkerPointers.Add(contentPointer);
        }
        


    }

    private void MergeLabels(double startSecs, double endSecs, string label)
    {

        for(int t=1;t<TimeStack.Children.Count-1;t++) 
        {
            
            if (TimeStack.Children[t] is SfLinearGauge g)
            {
                foreach (var range in g.Ranges)
                {
                    if (range.StartValue == startSecs && range.EndValue == endSecs)
                    {
                        int index = g.Ranges.IndexOf(range);
                        if (index >= 0 && index < (g.MarkerPointers?.Count() ?? 0))
                        {


                            var contentPointerObject = (LinearContentPointer)g.MarkerPointers[index];
                            g.MarkerPointers.RemoveAt(index);
                            if (contentPointerObject is LinearContentPointer cp)
                            {
                                if (cp.Content is Label l)
                                {
                                    l.Text = $"{l.Text}, {label}";
                                    cp.Content = l;
                                    (TimeStack.Children[t] as SfLinearGauge)?.MarkerPointers.Insert(index, cp);
                                }
                            }
                        }

                    }


                }
            }
        }
    }

    /// <summary>
    /// counts the number of overlaps with existing label markers
    /// </summary>
    /// <param name="startSecs"></param>
    /// <param name="endSecs"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private int getOverlaps(double startSecs, double endSecs)
    {
        int overlaps = 0;
        foreach (var label in currentLabels)
        {
            if(label.Matches(startSecs, endSecs))
            {
                return -1;
            }
            if(label.Overlaps(startSecs, endSecs)){
                overlaps++;
            }
        }
        return overlaps;
    }

    private SfLinearGauge MakeNewTimeScale()
    {
       var gauge=new SfLinearGauge();
        var b1 = new Binding();
        var b2= new Binding();
        gauge.Orientation=GaugeOrientation.Horizontal;
        gauge.Margin=0;
        gauge.SetBinding(SfLinearGauge.MinimumProperty, nameof(TimeScaleStart));
        gauge.SetBinding(SfLinearGauge.MaximumProperty,nameof(TimeScaleEnd));
        gauge.LabelFormat = "F1";
        gauge.ShowLine = false;
        gauge.ShowLabels = false;
        gauge.ShowTicks = false;
        return gauge;
        
    }

    private void TimeRangeSlider_ValueChanged(object sender, Syncfusion.Maui.Sliders.RangeSliderValueChangedEventArgs e)
    {
        NormalizeSpectrogram();
        CanvasView.InvalidateSurface();
    }

    internal async Task LoadFile(string file)
    {
        ClearLabels();
        if (Path.GetExtension(file).EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await Task.Run(() =>

                    {
                        _bitmap = GetSpectrogram(file);
                        if(_bitmap==null)
                        {
                            _bitmap=new SKBitmap(1000,512);
                        }
                        IsModified = false;
                        _skSurface =SKSurface.Create(new SKImageInfo(_bitmap.Width, _bitmap.Height));
                        using var canvas= _skSurface.Canvas;
                        canvas.Clear(SKColors.Transparent);
                        SKRect rect=new SKRect(0,0,_bitmap.Width,_bitmap.Height);
                        canvas.DrawBitmap(_bitmap,rect);
                    });
                CanvasView.InvalidateSurface();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

        }
    }

    private void ClearLabels()
    {
        for (int i = TimeStack.Children.Count - 1; i >= 0; i--)
        {
            var child = TimeStack.Children[i];
            if (child is SfLinearGauge scale)
            {
                Debug.WriteLine($"Deleteing stack item with {scale.Ranges.Count}");
                if(scale.Ranges.Count > 0)
                {
                    TimeStack.Children.RemoveAt(i);
                }
            }
            
        }
        currentLabels = new List<LabelItem>();
    }

    public void ZoomToSecs(double start,double end)
    {
        SpectrogramLengthInFFTs = (float)((end - start) * FFTsPerSec);
        StartOfSpectrogramInFFTs = (float)(start * FFTsPerSec);
        EndOfSpectrogramInFFTs = (float)(end * FFTsPerSec);
        NormalizeSpectrogram();
        CanvasView.InvalidateSurface();

    }

    private void FreqRangeSlider_ValueChanged(object sender, Syncfusion.Maui.Sliders.RangeSliderValueChangedEventArgs e)
    {
        FrequencyScale.ShowTicks = true;
        
        CanvasView.InvalidateSurface();
    }


    private void MenuFlyoutItem_Clicked(object sender, EventArgs e)
    {
        FrequencyRangeStart = 0;
        FrequencyRangeEnd = Math.Max(100,MaxFrequency / 2);
        CanvasView.InvalidateSurface();
    }

    private void MenuFlyoutItem_Clicked_1(object sender, EventArgs e)
    {
        FrequencyRangeStart= 0;
        FrequencyRangeEnd = MaxFrequency;
        CanvasView.InvalidateSurface();
    }

    internal void Zoom(double start, double end)
    {
        StartOfSpectrogramInFFTs = (float)(start * FFTsPerSec);
        EndOfSpectrogramInFFTs =(float)(end * FFTsPerSec);
        SpectrogramLengthInFFTs=EndOfSpectrogramInFFTs-StartOfSpectrogramInFFTs;
        NormalizeSpectrogram();
        CanvasView.InvalidateSurface();

    }

    internal void PageForward()
    {
        var distanceToMove= SpectrogramLengthInFFTs;
        StartOfSpectrogramInFFTs += distanceToMove;
        EndOfSpectrogramInFFTs += distanceToMove;
        NormalizeSpectrogram();
        CanvasView.InvalidateSurface();
    }

    private void NormalizeSpectrogram()
    {
        if (_bitmap is null) return;
        if (EndOfSpectrogramInFFTs > _bitmap.Width)
        {
            var shift=EndOfSpectrogramInFFTs-_bitmap.Width;
            EndOfSpectrogramInFFTs -= shift;
            StartOfSpectrogramInFFTs -= shift;
            
        }
        if (StartOfSpectrogramInFFTs < 0)
        {
            var shift = -StartOfSpectrogramInFFTs;
            StartOfSpectrogramInFFTs = 0;
            if (EndOfSpectrogramInFFTs + shift > _bitmap.Width)
            {
                SpectrogramLengthInFFTs = EndOfSpectrogramInFFTs;
            }
            else
            {
                EndOfSpectrogramInFFTs += shift;
            }
            
        }
        SpectrogramLengthInFFTs = EndOfSpectrogramInFFTs - StartOfSpectrogramInFFTs;
    }

    internal void PageBack()
    {
        var distanceToMove = SpectrogramLengthInFFTs;
        StartOfSpectrogramInFFTs -= distanceToMove;
        EndOfSpectrogramInFFTs -= distanceToMove;
        if (StartOfSpectrogramInFFTs < 0)
        {
            StartOfSpectrogramInFFTs = 0;
            EndOfSpectrogramInFFTs = SpectrogramLengthInFFTs;
        }
        NormalizeSpectrogram();
        CanvasView.InvalidateSurface();
    }

    internal void PanToEnd()
    {
        var currentLength = SpectrogramLengthInFFTs;
        EndOfSpectrogramInFFTs = TotalFFTs;
        StartOfSpectrogramInFFTs = EndOfSpectrogramInFFTs - currentLength;
        NormalizeSpectrogram();
        CanvasView.InvalidateSurface();
    }

    internal void PanToStart()
    {
        var currentLength = SpectrogramLengthInFFTs;
        StartOfSpectrogramInFFTs = 0;
        EndOfSpectrogramInFFTs = currentLength;
        NormalizeSpectrogram();
        CanvasView.InvalidateSurface();
    }

    internal bool Success()
    {
        if(sg!=null && _bitmap!=null) return true;
        return false;
    }

    private double startOfPan = 0;

    /// <summary>
    /// Called in reponse to a Pan gesture on the CanvasView
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        var dist= e.TotalX;
        if(e.StatusType == GestureStatus.Started)
        {
            startOfPan = 0;
        }
        else  if (e.StatusType == GestureStatus.Running)
        {
            Pan(dist);
        }
        else if (e.StatusType == GestureStatus.Completed)
        {
            // nothing to do
        }

    }

    /// <summary>
    /// Pans a distance based on the distance given in pixels from startOfPan
    /// </summary>
    /// <param name="dist"></param>
    private void Pan(double dist)
    {
        var distanceToMove = (float)(dist-startOfPan);
        PanPixelDistance(-distanceToMove);
    }

    private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        if(e.Status == GestureStatus.Started)
        {
            // nothing to do
        }
        else if (e.Status == GestureStatus.Running)
        {
            var scale = e.Scale;
            if (scale != 1.0)
            {
                IsBusyRunning = true;
                var midpoint = StartOfSpectrogramInFFTs + (SpectrogramLengthInFFTs / 2.0f);
                SpectrogramLengthInFFTs = SpectrogramLengthInFFTs *(float) scale;
                StartOfSpectrogramInFFTs = midpoint - (SpectrogramLengthInFFTs / 2.0f);
                EndOfSpectrogramInFFTs = StartOfSpectrogramInFFTs + SpectrogramLengthInFFTs;
                NormalizeSpectrogram();
                CanvasView.InvalidateSurface();
                IsBusyRunning = false;
            }
        }
        else if (e.Status == GestureStatus.Completed)
        {
            // nothing to do
        }
    }

    private void fmiZoomIn_Clicked(object sender, EventArgs e)
    {
        IsBusyRunning = true;
        var scale = 0.5;
        var midpoint =  StartOfSpectrogramInFFTs+ (SpectrogramLengthInFFTs / 2.0f);
        SpectrogramLengthInFFTs= (float)(SpectrogramLengthInFFTs * scale);
        StartOfSpectrogramInFFTs= midpoint - (SpectrogramLengthInFFTs / 2.0f);
        EndOfSpectrogramInFFTs= StartOfSpectrogramInFFTs + SpectrogramLengthInFFTs;
        NormalizeSpectrogram();
        CanvasView.InvalidateSurface();
        IsBusyRunning = false;

    }

    [RelayCommand]
    public void ZoomIn()
    {
        fmiZoomIn_Clicked(this, EventArgs.Empty);
    }

    [RelayCommand]
    public void ZoomOut()
    {
        fmiZoomOut_Clicked(this, EventArgs.Empty);
    }

    private void fmiZoomOut_Clicked(object sender, EventArgs e)
    {
        IsBusyRunning = true;
        var scale = 2.0;
        var midpoint = StartOfSpectrogramInFFTs + (SpectrogramLengthInFFTs / 2.0f);
        SpectrogramLengthInFFTs =(float)( SpectrogramLengthInFFTs * scale);
        StartOfSpectrogramInFFTs = midpoint - (SpectrogramLengthInFFTs / 2.0f);
        EndOfSpectrogramInFFTs = StartOfSpectrogramInFFTs + SpectrogramLengthInFFTs;
        NormalizeSpectrogram();
        CanvasView.InvalidateSurface();
        IsBusyRunning = false;
    }

    private void fmiZoomAll_Clicked(object sender, EventArgs e)
    {
        IsBusyRunning = true;
        StartOfSpectrogramInFFTs = 0;
        EndOfSpectrogramInFFTs = TotalFFTs;
        SpectrogramLengthInFFTs = EndOfSpectrogramInFFTs - StartOfSpectrogramInFFTs;
        NormalizeSpectrogram();
        CanvasView.InvalidateSurface();
        IsBusyRunning = false;
    }

    private void fmiZoomSelection_Clicked(object sender, EventArgs e)
    {
        IsBusyRunning = true;
        if (selection == null) return;
        StartOfSpectrogramInFFTs = selection?.startFFTs ?? 0;
        EndOfSpectrogramInFFTs = selection?.endFFTs ?? 0;
        SpectrogramLengthInFFTs = EndOfSpectrogramInFFTs - StartOfSpectrogramInFFTs;
        NormalizeSpectrogram();
        
        CanvasView.InvalidateSurface();
        IsBusyRunning = false;

    }

    private void fmiZoomDefault_Clicked(object sender, EventArgs e)
    {
        IsBusyRunning = true;
        var midPoint=StartOfSpectrogramInFFTs + (SpectrogramLengthInFFTs / 2.0f);
        SpectrogramLengthInFFTs = (float)(PrefferredViewLengthSecs * FFTsPerSec);
        StartOfSpectrogramInFFTs = midPoint - (SpectrogramLengthInFFTs / 2.0f);
        EndOfSpectrogramInFFTs = StartOfSpectrogramInFFTs + SpectrogramLengthInFFTs;
        NormalizeSpectrogram();
        CanvasView.InvalidateSurface();
        IsBusyRunning = false;
    }

    /// <summary>
    /// Retrieves all the labels for the current file and returns them as 'start end comment' on
    /// a new line for each
    /// </summary>
    /// <returns></returns>
    internal string GetLabelText()
    {
        if(currentLabels?.Any()??false)
        {
            StringBuilder sb = new StringBuilder();
            foreach(var label in currentLabels)
            {
                sb.AppendLine($"{label.startOffset}\t{label.endOffset}\t{label.idedBats}");
            }
            return sb.ToString();
        }
        return "start\tend\tNo Bats\n";
    }


    private async void mfiColorChoice_Clicked(object sender, EventArgs e)
    {
        var action = await Application.Current.MainPage.DisplayActionSheet("Select Color Map", "Cancel", null, Spectrogram.Colormap.GetColormapNames().ToArray());
        Debug.WriteLine($"Color map choice {action}");
        if ((action?.ToLower()??"") != "cancel" && !string.IsNullOrWhiteSpace(action))
        {
            IsBusyRunning = true;
            var cmap = Spectrogram.Colormap.GetColormapNames().FirstOrDefault(x => x == action);
            CurrentColorMap = cmap ?? "Grayscale Reversed";
            sg.Colormap = Spectrogram.Colormap.GetColormap(CurrentColorMap);
            _bitmap = sg?.GetBitmap(dB: true, intensity: Intensity);
            _skSurface = SKSurface.Create(new SKImageInfo(_bitmap.Width, _bitmap.Height));
            using var canvas = _skSurface.Canvas;
            canvas.Clear(SKColors.Transparent);
            SKRect rect = new SKRect(0, 0, _bitmap.Width, _bitmap.Height);
            canvas.DrawBitmap(_bitmap, rect);
            CanvasView.InvalidateSurface();
            IsBusyRunning = false;
        }
    }

    private void CanvasView_SizeChanged(object sender, EventArgs e)
    {
        CanvasView.InvalidateSurface();
    }



    private void IntensitySlider_ValueChanged(object sender, Syncfusion.Maui.Sliders.SliderValueChangedEventArgs e)
    {
        if (sg != null)
        {
            IsBusyRunning = true;
            try { 


                Debug.WriteLine($"Intensity was {e.OldValue} now {e.NewValue} as {IntensityValue}");
                _bitmap = sg?.GetBitmap(dB: true, intensity: IntensityValue);
                _skSurface = SKSurface.Create(new SKImageInfo(_bitmap.Width, _bitmap.Height));
                using var canvas = _skSurface.Canvas;
                canvas.Clear(SKColors.Transparent);
                SKRect rect = new SKRect(0, 0, _bitmap.Width, _bitmap.Height);
                canvas.DrawBitmap(_bitmap, rect);
                CanvasView.InvalidateSurface();
            }
            finally { IsBusyRunning = false; }
        }
    }

    private void mfiPlaySelectionClicked(object sender, EventArgs e)
    {
        audioPlayer?.LoadSegment(CurrentFile, TimeSpan.FromSeconds((selection?.startFFTs ?? 0) / FFTsPerSec), TimeSpan.FromSeconds((selection?.endFFTs ?? 0) / FFTsPerSec));
        audioPlayer.IsVisible = true;
    }

    internal void SetPlayhead(double totalSeconds)
    {
        PlayHeadPositionSecs = totalSeconds;
        CanvasView.InvalidateSurface();
        Debug.WriteLine($"Set playhead to {totalSeconds}s");
    }

    internal void Stop()
    {
        Debug.WriteLine("SpectrogramView Stop called - stopping audio player");
        audioPlayer.Stop();
        
    }

    public void Dispose()
    {
        Stop();
        _bitmap?.Dispose();
        _bitmap = null;
        audioPlayer?.Dispose();
        audioPlayer = null;
    }

    private async void mfiPowerSpectrumClicked(object sender, EventArgs e)
    {
        if (selection != null && selection.startFFTs != selection.endFFTs)
        {
            PowerSpectrumPage powerSpectrum = BPAServiceProvider.GetService<PowerSpectrumPage>();
            //new PowerSpectrumPage(BPAServiceProvider.GetService<BPASpectrogramM.ViewModels.PowerSpectrumVM>());

            powerSpectrum.Init(sg, (int)(selection?.startFFTs ?? 0), (int)(selection?.endFFTs ?? 0));
            //powerSpectrum.LoadSegment(CurrentFile, TimeSpan.FromSeconds((selection?.startFFTs ?? 0) / FFTsPerSec), TimeSpan.FromSeconds((selection?.endFFTs ?? 0) / FFTsPerSec));


            //await Navigation.PushModalAsync(powerSpectrum); 
            //powerSpectrum.Display();
            await Shell.Current.Navigation.PushModalAsync(powerSpectrum);
        }

    }
}

public class SpectrogramSelectionChangedEventArgs : EventArgs
{
    public double StartSecs { get; set; } = 0;
    public double EndSecs { get; set; } = 0;
    public double FreqMin { get; set; } = 0;
    public double FreqMax { get; set; } = 0;

    public string fqFileName { get; set; } = "";

    public SpectrogramSelectionChangedEventArgs(string file,double StartSecs = 0, double EndSecs = 0, double FreqMin= 0,double FreqMax= 0)
    {
        this.fqFileName = file;
        this.StartSecs = StartSecs;
        this.EndSecs = EndSecs;
        this.FreqMin = FreqMin;
        this.FreqMax = FreqMax;
    }
}
