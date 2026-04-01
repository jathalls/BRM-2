using System.Diagnostics;

namespace BPASpectrogramM
{
    internal class HeterodyneModifier 
    {
        private readonly float[] _previousOutput;
        private float _cutoffFrequency;
        private readonly WavFormatInfo _format;
        private float _heterodyneFrequency;

        public float HeterodyneFrequency
        {
            get => _heterodyneFrequency;
            set
            {
                _heterodyneFrequency = value;
                if(HeterodyneOscillator != null)
                    HeterodyneOscillator.Frequency = _heterodyneFrequency;
            }
        }
        

        public float CutoffFrequency
        {
            get => _cutoffFrequency;
            set => _cutoffFrequency = Math.Max(1000,value);
        }

        public Oscillator HeterodyneOscillator { get; set; }

        public HeterodyneModifier(WavFormatInfo format, float cutoffFrequency = 5000f, float heterodyneFrequency = 50000)
        {
            _format = format;
            _cutoffFrequency = cutoffFrequency;
            _previousOutput = new float[format.ChannelCount];
            Debug.WriteLine($"Initializing HeterodyneModifier with cutoff frequency: {_cutoffFrequency} Hz and heterodyne frequency: {heterodyneFrequency} Hz SR={_format.SampleRate}");
            HeterodyneFrequency = heterodyneFrequency;
            if (_format.SampleRate == 0)
            {
                Debug.WriteLine("Sample rate is zero, cannot initialize oscillator.");
                
            }
            var osc=new BPASpectrogramM.Oscillator( format,heterodyneFrequency);
            osc.Frequency = HeterodyneFrequency;
            osc.Amplitude = 1.0f;
            if (heterodyneFrequency <= 10_000f)
            {
                osc.Type = BPASpectrogramM.Oscillator.WaveformType.Pulse;
            }
            else
            {
                osc.Type = BPASpectrogramM.Oscillator.WaveformType.Sine;
            }
                
            HeterodyneOscillator = osc;

        }

        public float ProcessSample(float sample, int channel)
        {
            float alpha = 0.0f;
            if (_cutoffFrequency == 0)
            {
                Debug.WriteLine("Cutoff frequency is zero, skipping processing.");
                return sample;
            }
            try
            {
                sample = sample * HeterodyneOscillator.GenerateSample();
                var dt = 1.0f / (float)_format.SampleRate;
                var rc = 1.0f / (2.0f * (float)Math.PI * _cutoffFrequency);
                if (rc + dt == 0)
                {
                    Debug.WriteLine("RC + DT is zero, skipping processing.");
                    return sample;
                }
                alpha = dt / (rc + dt);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing sample: {ex.Message}");
                return sample; // Return unprocessed sample on error
            }
            _previousOutput[channel] += alpha * (sample - _previousOutput[channel]);
            return _previousOutput[channel];
        }

        internal void Process(float[] processedBuffer, int samplesRead)
        {
            for(int i=0;i<processedBuffer.Length && i<samplesRead;i++)
            {
                processedBuffer[i]= ProcessSample(processedBuffer[i],0);
            }
        }
    }
}
