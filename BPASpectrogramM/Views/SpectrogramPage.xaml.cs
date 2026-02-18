using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Syncfusion.Maui.Core;
using AppoMobi.Specials;
using Microsoft.Extensions.Options;

namespace BPASpectrogramM.Views;

public partial class SpectrogramPage :  ContentPage, INotifyPropertyChanged
{
    public new event PropertyChangedEventHandler PropertyChanged;
    protected override void OnPropertyChanged([CallerMemberName] string PropertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName)); }

    internal void ImportThisSession()
    {
        throw new NotImplementedException();
    }

    private bool _isBusy = false;
    public bool IsBusyRunning 
    { 
        get=>_isBusy; 
        set 
        { 
            _isBusy = value; 
            OnPropertyChanged();
        } 
    }
    public SpectrogramPage()
    {
		
        InitializeComponent();
        BindingContext = this;
        
        
        
    }

}
