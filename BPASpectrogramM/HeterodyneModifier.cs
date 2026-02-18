namespace BPASpectrogramM
{
    internal class HetrodyneModifier 
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

        public HetrodyneModifier(WavFormatInfo format, float cutoffFrequency = 5000f, float heterodyneFrequency = 50000)
        {
            _format = format;
            _cutoffFrequency = cutoffFrequency;
            _previousOutput = new float[format.ChannelCount];
            
            HeterodyneFrequency = heterodyneFrequency;
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

        public override float ProcessSample(float sample, int channel)
        {
            sample = sample * HeterodyneOscillator.GenerateSample();
            var dt = 1/_format.SampleRate;
            var rc = 1.0f / (2.0f * (float)Math.PI * _cutoffFrequency);
            var alpha = dt / (rc + dt);
            _previousOutput[channel] +=  alpha * (sample - _previousOutput[channel]);
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
