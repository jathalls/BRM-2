using BPASpectrogramM.ViewModels;
using Spectrogram;
using Microsoft.Maui.Controls;
namespace BPASpectrogramM.Views;

public partial class PowerSpectrumPage : ContentPage
{
    private PowerSpectrumVM ViewModel;
    public PowerSpectrumPage(PowerSpectrumVM viewModel)
    {
        this.ViewModel = viewModel;
        viewModel.parent = this;
        InitializeComponent();
        BindingContext = viewModel;

        powerSpectrumChart.BindingContext = viewModel;
    }


    internal void Init(SpectrogramGenerator sg, int startFFTs, int endFFTs)
    {
        ViewModel.Init(sg, startFFTs, endFFTs);
        powerSpectrumChart.Init();
    }

}
