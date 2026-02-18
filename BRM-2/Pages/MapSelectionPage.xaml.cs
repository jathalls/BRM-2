namespace BRM_2.Pages;
public partial class MapSelectionPage : ContentPage
{
	
	

	
	public MapSelectionPage(MapSelectionVM viewModel)
	{
		InitializeComponent();

		BindingContext = viewModel;
	}

	public void SetInitialPosition(MapLatLng mapLatLng)
	{
		if (BindingContext is MapSelectionVM viewModel)
		{
			viewModel.SelectedPagePosition = mapLatLng;
			mapControl.DesiredPosition = mapLatLng;
			mapControl.InvalidateMeasure();
        }
	}

	/// <summary>
	/// Forced close if the parent is shutting down while this is still open
	/// </summary>
    internal async void Close()
    {
        if (BindingContext is MapSelectionVM viewModel)
        {
			await viewModel.Close(); 
            
        }
    }

    internal MapLatLng? GetFinalPosition()
    {
        if (BindingContext is MapSelectionVM viewModel)
        {
            return(viewModel.FinalPosition);
        }
		return null;
    }
}
