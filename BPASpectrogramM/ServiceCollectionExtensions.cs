using Microsoft.Extensions.DependencyInjection;
using BPASpectrogramM.Views;
using BPASpectrogramM.ViewModels;

namespace BPASpectrogramM.Navigation
{
    public static class ServiceCollectionExtensions
    {
        public static void AddSpectrogramServices(this IServiceCollection services)
        {
            _ = services.AddSingleton<BPASpectrogramM.Views.SpectrogramButtons>();
            services.AddSingleton<BPASpectrogramM.Views.SpectrogramView>();
            services.AddSingleton<BPASpectrogramM.Views.SpectrogramControls>();
            services.AddSingleton<BPASpectrogramM.Views.SpectrogramPageAsControl>();
            
            services.AddSingleton<BPASpectrogramM.Views.SpectrogramToolbar>();
            services.AddSingleton<BPASpectrogramM.ViewModels.SpectrogramToolbarVM>();
            services.AddSingleton<BPASpectrogramM.Views.SpectrogramWaveform>();

            services.AddSingleton<BPASpectrogramM.Views.PowerSpectrumPage>();
            services.AddSingleton<BPASpectrogramM.ViewModels.PowerSpectrumVM>();

            services.AddSingleton<BPASpectrogramM.Views.AudioPlayer>();

        }
    }
}
