namespace BPASpectrogramM.Navigation
{
    public static class BPAServiceProvider
    {
        public static TService GetService<TService>() => Current.GetService<TService>();

        public static IServiceProvider Current =>
#if WINDOWS10_0_17763_0_OR_GREATER
            MauiWinUIApplication.Current.Services;
#elif ANDROID
            IPlatformApplication.Current.Services;
#elif IOS || MACCATALYST
            IPlatformApplication.Current.Services;
#else
            null;
#endif


    }
}
