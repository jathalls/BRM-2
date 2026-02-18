using BPASpectrogramM;

public class AudioStreamProcessor
{
    private AudioFileReaderM reader;
    private HetrodyneModifier modifier;
    private const int BUFFER_SIZE = 4096;
    private float[] buffer;
    private float[] processedBuffer;

    public AudioStreamProcessor(string filePath, WavFormatInfo format, double heterodyneFrequency)
    {
        reader = new AudioFileReaderM(filePath);
        modifier = new HetrodyneModifier(format, 5000f, (float)heterodyneFrequency * 1000f);
        buffer = new float[BUFFER_SIZE];
        processedBuffer = new float[BUFFER_SIZE];
    }

    public float[] GetNextProcessedSamples()
    {
        int samplesRead = reader.Read(buffer);
        if (samplesRead <= 0) return null;

        Array.Copy(buffer, processedBuffer, samplesRead);
        modifier.Process(processedBuffer, samplesRead);

        return processedBuffer.Take(samplesRead).ToArray();
    }

    public void Seek(TimeSpan position)
    {
        reader.Seek(position);
    }

    public void Dispose()
    {
        reader?.Dispose();
    }
}