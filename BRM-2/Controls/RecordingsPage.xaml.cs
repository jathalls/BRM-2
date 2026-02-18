namespace BRM_2.Controls;
public partial class RecordingsPage : ContentView, INotifyPropertyChanged
{
    public new event PropertyChangedEventHandler? PropertyChanged;

    protected override void OnPropertyChanged([CallerMemberName] string PropertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
    }

	public RecordingsPageVM viewModel { get; set; }
    

	public RecordingsPage()
    {
        this.viewModel=BRM_2.Navigation.ServiceProvider.GetService<RecordingsPageVM>();
        InitializeComponent();
        BindingContext = this.viewModel;
        
    }
	
	public RecordingsPage(RecordingsPageVM viewModel)
	{
		this.viewModel = viewModel;
		InitializeComponent();
		BindingContext = this.viewModel;
        //recordingsCollectionView.SelectionChanged += RecordingsCollectionView_SelectionChanged;
		
	}

    private void RecordingsCollectionView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
		viewModel.RecordingsCollectionView_SelectionChanged(sender, e);
    }

    public async Task Update()
	{
		await viewModel.Update();
	}

    private void recordingsCollectionView_QueryItemSize(object sender, Syncfusion.Maui.ListView.QueryItemSizeEventArgs e)
    {
        if (e.ItemIndex > 0)
        {
            e.ItemSize = 500;
            e.Handled = true;
        }
    }

    private void segmentsCollectionView_SelectionChanged(object sender, Syncfusion.Maui.ListView.ItemSelectionChangedEventArgs e)
    {
        Debug.WriteLine("segment selected");
        if (e.AddedItems?.Any() ?? false)
        {
            viewModel.SelectedSegment = (e.AddedItems as LabelledSegmentEx)??new LabelledSegmentEx();
        }
    }


    private void TapGestureRecognizer_Tapped(object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        Debug.WriteLine("Tapped");
        var lv = sender as Label;
        if (lv != null)
        {
            if(lv.BindingContext is LabelledSegmentEx segment){
                viewModel.SelectedSegment = segment;
            }
        }
    }
}