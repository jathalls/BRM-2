global using CommunityToolkit.Mvvm.ComponentModel;
global using Syncfusion.Maui.Core;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using System.Threading.Tasks;

#if WINDOWS
global using NSUrl = System.String;

#endif
#if MACCATALYST || MACOS || IOS

global using NSUrl = Foundation.NSUrl;
#endif
#if ANDROID
global using NSUrl = System.String;
#endif
