using BPASpectrogramM.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace BPASpectrogramM.Views;

public partial class SpectrogramToolbar : ContentView
{
    public SpectrogramToolbarVM viewModel;

    public SpectrogramToolbar()
    {
        // Fix: Use MauiProgram.ServiceProvider to resolve the view model
        viewModel = new SpectrogramToolbarVM( );

        InitializeComponent();
        BindingContext = viewModel;
    }

    public void SetParentPage(SpectrogramPageAsControl parentPage)
    {
        viewModel.spPage = parentPage;
    }   


    internal void DisableFileButtons()
    {
        viewModel.DisableFileButtons();

    }

    internal void NextFile(bool IsModified=false)
    {
        viewModel.Next(IsModified);
    }
}
