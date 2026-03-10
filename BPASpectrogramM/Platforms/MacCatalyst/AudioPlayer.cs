using BPASpectrogramM.Interfaces;

namespace BPASpectrogramM.Views;

public partial class AudioPlayer
{
    private partial IAudioPlaybackService? CreatePlatformAudioPlaybackService()
    {
        return new Platforms.MacCatalyst.AudioPlaybackService();
    }
}
