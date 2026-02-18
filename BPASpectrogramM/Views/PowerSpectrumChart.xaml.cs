using BPASpectrogramM.Navigation;
using BPASpectrogramM.ViewModels;
using System.Diagnostics;

namespace BPASpectrogramM.Views;
public partial class PowerSpectrumChart : ContentView
{
	
    public PowerSpectrumChart()
    {
		
        InitializeComponent();
        //BindingContext = BPAServiceProvider.GetService<PowerSpectrumVM>(); 
    }

    internal void Init()
    {/*
        if (BindingContext is PowerSpectrumVM vm)
        {
            ChartLineSeries.ItemsSource = vm.PowerSpectrumSeries;
        }*/
    }
}
