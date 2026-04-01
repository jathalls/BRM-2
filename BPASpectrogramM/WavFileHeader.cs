using System;
using System.Collections.Generic;
using System.Text;

namespace BPASpectrogramM
{
    /// <summary>
    /// Class to hold and manipulate a .wav file header chunk
    /// </summary>
    public class WavFileHeader
    {
        public char[] chunkID =new char[4]; // "RIFF"
        public Int32 chunkSize; // Size of the entire file minus 8 bytes
        public char[] format = new char[4]; // "WAVE"
        public char[] formatchunk1ID = new char[4]; // "fmt "
        public Int32 formatchunk1Size; // Size of the fmt chunk (16 for PCM)
        public Int16 audioFormat; // Audio format (PCM = 1)
        public Int16 numChannels; // Number of channels
        public Int32 sampleRate; // Sample rate
        public Int32 byteRate; // Byte rate
        public Int16 blockAlign; // Block align
        public Int16 bitsPerSample; // Bits per sample 
        public char[] dataChunkID = new char[4]; // "data"
        public Int32 dataChunkSize; // Size of the data chunk

        public WavFileHeader()
        {
            // Initialize with default values for a standard PCM WAV file
            chunkID = "RIFF".ToCharArray();
            format = "WAVE".ToCharArray();
            formatchunk1ID = "fmt ".ToCharArray();
            formatchunk1Size = 16; // PCM format
            audioFormat = 1; // PCM
            numChannels = 1; // Mono
            sampleRate = 384000; // Standard CD quality
            bitsPerSample = 16; // Standard CD quality
            byteRate = sampleRate * numChannels * bitsPerSample / 8;
            blockAlign = (short)(numChannels * bitsPerSample / 8);
            dataChunkID = "data".ToCharArray();
            dataChunkSize = 0; // Will be set when writing the file
        }

        internal int Write(FileStream dest)
        {
            dest.Write(System.Text.Encoding.ASCII.GetBytes(new string(chunkID)), 0, chunkID.Length);
            dest.Write(BitConverter.GetBytes(chunkSize), 0, 4);
            dest.Write(System.Text.Encoding.ASCII.GetBytes(new string(format)), 0, format.Length);
            dest.Write(System.Text.Encoding.ASCII.GetBytes(new string(formatchunk1ID)), 0, formatchunk1ID.Length);
            dest.Write(BitConverter.GetBytes(formatchunk1Size), 0, 4);
            dest.Write(BitConverter.GetBytes(audioFormat), 0, 2);
            dest.Write(BitConverter.GetBytes(numChannels), 0, 2);
            dest.Write(BitConverter.GetBytes(sampleRate), 0, 4);
            dest.Write(BitConverter.GetBytes(byteRate), 0, 4);
            dest.Write(BitConverter.GetBytes(blockAlign), 0, 2);
            dest.Write(BitConverter.GetBytes(bitsPerSample), 0, 2);
            dest.Write(System.Text.Encoding.ASCII.GetBytes(new string(dataChunkID)), 0, dataChunkID.Length);
            dest.Write(BitConverter.GetBytes(dataChunkSize), 0, 4);
            return 44; // Standard PCM WAV header size
        }
    }
}
