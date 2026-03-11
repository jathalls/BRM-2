using BPASpectrogramM.Interfaces;

namespace BPASpectrogramM.Views;

public partial class AudioPlayer
{
    private partial IAudioPlaybackService? CreatePlatformAudioPlaybackService()
    {
        // Windows uses MediaElement with WAV header speed adjustment.
        // Return null to fallback to MediaElement, which will use the speed-adjusted segment file.
        return null;
    }
}
