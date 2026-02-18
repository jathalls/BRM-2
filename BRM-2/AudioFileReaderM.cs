using SoundFlow.Backends.MiniAudio;
using SoundFlow.Metadata.Models;
using SoundFlow.Providers;
using System;
using System.Collections.Generic;
using System.Text;

namespace BRM_2
{
    /// <summary>
    /// a .NET Maui replacement for NAudio's AudioFileReader, which is not compatible with .NET Maui. This class will handle reading audio files and providing audio data for playback and visualization.
    /// 
    /// </summary>
    internal class AudioFileReaderM
    {
        public SoundFormatInfo? FormatInfo { get; private set; }
        public MiniAudioEngine Engine { get; private set; }

        public StreamDataProvider Provider { get; private set; }

        public int maxLength { get; private set; }

        public int SampleRate => FormatInfo?.SampleRate ?? 0;

        public AudioFileReaderM(string filePath)
        {
            try
            {
                var engine = new MiniAudioEngine();
                Engine = engine;
                using var fs = File.OpenRead(@"Resources\raw\Test.wav");
                //var stream = new StreamReader(@"Resources\raw\Test.wav");
                using var provider = new StreamDataProvider(engine, fs);
                Provider = provider;
                var format = provider.FormatInfo;
                FormatInfo = format;
                maxLength = provider.Length;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                if (ex.InnerException != null)
                {
                    Debug.WriteLine(ex.InnerException);
                }


            }
        }

        /// <summary>
        /// Seek to a specific position in the audio file. The position is specified in samples, and the method will convert it to bytes before seeking in the provider. This allows for accurate seeking based on the audio format (e.g., sample rate, bit depth, number of channels).
        /// </summary>
        /// <param name="position"></param>
        public void Seek(long position)
        {
            Provider.Seek((int)(position)); // position is in samples, but we need to convert it to bytes (assuming 16-bit audio, which is 2 bytes per sample)
        }

        public void Seek(TimeSpan time)
        {
            // Convert the TimeSpan to bytes based on the audio format
            if (FormatInfo == null)
                throw new InvalidOperationException("FormatInfo is not available.");
            long bytesPerSecond = FormatInfo.SampleRate *  FormatInfo.ChannelCount;
            long bytePosition = (long)(time.TotalSeconds * bytesPerSecond);
            Provider.Seek((int)bytePosition);
        }



        public void Take(int count)
        {
            maxLength=Provider.Position + count;
        }

        public int Read(ref float[] buffer)
        {
            var count = buffer.Length;
            if (Provider.Position >= maxLength)
                return 0; // No more data to read
            int samplesToRead = (int)Math.Min(count, maxLength - Provider.Position);
            Span<float> spBuffer = new Span<float>(buffer);
            var samplesRead= Provider.ReadBytes(spBuffer);
            buffer=spBuffer.ToArray();
            return samplesRead;
        }


    }
}
