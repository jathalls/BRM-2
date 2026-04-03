namespace BPASpectrogramM.Views;

public partial class AudioPlayer
{
    private partial IAudioPlaybackService? CreatePlatformAudioPlaybackService()
        => new BPASpectrogramM.Platforms.Windows.AudioPlaybackService();
}
