

using BPASpectrogramM.Views;
using CommunityToolkit.Mvvm.Input;
using Spectrogram;
using System.Diagnostics;

namespace BPASpectrogramM.ViewModels
{
    public partial class PowerSpectrumVM : ObservableObject
    {
        [ObservableProperty]
        double _cursorFrequency = 40.0;

        [ObservableProperty]
        private double _peakFrequency = 40.0;

        [ObservableProperty]
        private double _cursorAmplitude = 0.0;

        [ObservableProperty]
        private double _peakAmplitude = 0.0;

       
        private double _minDB = 0;

        public double MinDB
        {
            get
            {
                
                    return _minDB;
                
            }
            set
            {
                _minDB = value; OnPropertyChanged(nameof(MinDB));
            }
        }

            
        private double _maxDB = 95;

        public double MaxDB
        {
            get
            { 
                
                    return _maxDB;
                
            }
            set { _maxDB = value; OnPropertyChanged(nameof(MaxDB)); }
        }

        [ObservableProperty]
        private double _maxFrequency = 100;

        
        public List<PowerSpectrumPoint> PowerSpectrumSeries { get; set; } = new List<PowerSpectrumPoint>();

        private SpectrogramGenerator? spectrogramGenerator = null;

        public PowerSpectrumPage parent { get; set; }

        

        public PowerSpectrumVM() 
        {

            

        }

        public bool popping=false;

        [RelayCommand]
        internal async Task Close()
        {
            popping = true;
            //await parent?.Navigation.PopModalAsync();
            await Shell.Current.Navigation.PopModalAsync(); 
        }

       

        internal void Init(SpectrogramGenerator sg, int startFFTs, int endFFTs)
        {
            spectrogramGenerator = sg;
            if (sg == null) return;
            popping = false;
            
            var ffts=sg.GetFFTs().Skip(startFFTs).Take(endFFTs-startFFTs);
            var firstFFT = ffts.FirstOrDefault();
            var spectrum = new double[firstFFT.Length];
            var sampleRate = sg.SampleRate;
            var MaxFreqkHz = (double)sampleRate / 2000.0d;
            var kHzPerPoint = MaxFreqkHz / (firstFFT.Length);
            PowerSpectrumSeries=new List<PowerSpectrumPoint>();
            MinDB = double.MaxValue;
            MaxDB = double.MinValue;
            for (int i = 10; i < firstFFT.Length; i++)
            {
                spectrum[i] = ffts.Average(fft => fft[i]);
                spectrum[i] = spectrum[i] * spectrum[i];
                spectrum[i] = 10.0d * Math.Log(spectrum[i]);
                var frequency = i * kHzPerPoint;
                PowerSpectrumSeries.Add(new PowerSpectrumPoint(frequency, spectrum[i]));
                

            }
            MinDB = PowerSpectrumSeries.Select(Point => Point.Amplitude).Min();
            MaxDB = PowerSpectrumSeries.Select(Point => Point.Amplitude).Max();
            
            OnPropertyChanged(nameof(PowerSpectrumSeries));
            Debug.WriteLine($"min={MinDB}, Max={MaxDB}");
            
        }

        

        internal void LoadSegment(string currentFile, TimeSpan timeSpan1, TimeSpan timeSpan2)
        {
            Debug.WriteLine($"Loaded segment: {currentFile}, from {timeSpan1} to {timeSpan2}");
            float[] buffer = new float[1024];
            double[] totalPsd = new double[513];
            double[] freq = null;
            var fftBuffer = new List<double>();
            int start = (int)Math.Floor(timeSpan1.TotalSeconds);
            int end = (int)Math.Ceiling(timeSpan2.TotalSeconds);
            int duration = end - start;
            if (duration <= 0) duration = 1;
            for (int i = 0; i < totalPsd.Length; i++) totalPsd[i] = 0.0d;
            try
            {
                using (var afr = new AudioFileReaderM(currentFile))
                {
                    int samplesRead;
                    //afr.Seek(timeSpan1);
                    //var segment = afr?.Take(timeSpan2 - timeSpan1);
                    afr.Select(timeSpan1, timeSpan2);
                    int numberOfFFTs = 0;
                    
                    while ((samplesRead = afr.Read(buffer)) > 0)
                    {
                        
                        if (samplesRead == buffer.Length)
                        {
                            fftBuffer = new List<double>();
                            foreach (var sample in buffer) fftBuffer.Add((double)sample);

                            System.Numerics.Complex[] spectrum = FftSharp.FFT.Forward(fftBuffer.ToArray());
                            double[] psd = FftSharp.FFT.Power(spectrum);
                            psd[0] = psd[1];
                            Debug.WriteLine($"PSDMax={psd.Max()} of {psd.Length} at {psd.IndexOf(psd.Max())}");
                            freq = FftSharp.FFT.FrequencyScale(psd.Length, afr.SampleRate);
                            numberOfFFTs++;
                            
                            for (int i = 0; i < psd.Length; i++)
                            {
                                totalPsd[i] += psd[i];
                                
                            }

                        }
                    }
                    Debug.WriteLine($"TotalMax={totalPsd.Max()} in {numberOfFFTs} at {totalPsd.IndexOf(totalPsd.Max())}");
                    totalPsd = totalPsd.Select(ps => ps / numberOfFFTs).ToArray();
                    Debug.WriteLine($"Max Avg={totalPsd.Max()}");

                    PowerSpectrumSeries = new List<PowerSpectrumPoint>();
                    
                    if (freq != null && totalPsd != null)
                    {
                        for (int i = 0; i < totalPsd.Length && i<freq.Length; i++)
                        {
                            var pt = new PowerSpectrumPoint(freq[i]/1000.0d, totalPsd[i]);
                            PowerSpectrumSeries.Add(pt);
                            
                        }
                        MinDB = totalPsd.Min();
                        MaxDB=totalPsd.Max();
                        MaxFrequency=freq.Max()/1000.0d;
                        OnPropertyChanged(nameof(PowerSpectrumSeries));
                        Debug.WriteLine($"min={MinDB}, Max={MaxDB}");
                    }
                }
            }catch(Exception ex)
            {
                Debug.WriteLine($"ERR - {ex.Message}");
            }
        }
    }

    public partial  class PowerSpectrumPoint : ObservableObject
    {
        [ObservableProperty]
        private double _amplitude = 0.0; // amplitude in decibels

        //public double Amplitude { get => _amplitude; set { _amplitude = value; } }

        [ObservableProperty]
        private double _frequency = 40.0; // frequency in kHz

        public String Text { get => this.ToString(); }

        //public double Frequency { get => _frequency; set { _frequency = value; } }

        public PowerSpectrumPoint(double Frequency, double Amplitude)
        {
            this.Amplitude = Amplitude;
            this.Frequency = Frequency;

        }

        public string ToString()
        {
            return $"{Amplitude:F2}dB: {Frequency:F2}kHz";
        }
    }
}
