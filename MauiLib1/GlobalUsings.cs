#if WINDOWS
global using NSUrl = System.String;

#endif
#if MACCATALYST || MACOS || IOS

global using NSUrl = Foundation.NSUrl;
#endif
#if ANDROID
global using NSUrl = System.String;
#endif
