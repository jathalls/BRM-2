using BPASpectrogramM.Interfaces;

namespace BPASpectrogramM.Views;

public partial class AudioPlayer
{
    private partial IAudioPlaybackService? CreatePlatformAudioPlaybackService()
    {
        // Android implementation not yet available
        return null;
    }
}
