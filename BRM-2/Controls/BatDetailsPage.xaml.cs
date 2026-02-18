using System.Collections;

namespace BRM_2.Controls;
public partial class BatDetailsPage : ContentView,INotifyPropertyChanged
{
	public new event PropertyChangedEventHandler? PropertyChanged;
	protected override void OnPropertyChanged([CallerMemberName] string PropertyName = "") { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName)); }

	private ObservableCollection<BatEx> _bats = new System.Collections.ObjectModel.ObservableCollection<BatEx>();
	public ObservableCollection<BatEx> Bats { get => _bats; set { _bats = value; OnPropertyChanged(); } }

	public string BatTag { get; set; } = "";
	public BatDetailsPage()
	{
		InitializeComponent();

		this.BindingContext = this;
        Loaded += BatDetailsPage_Loaded;
    }

    private void BatDetailsPage_Loaded(object? sender, EventArgs e)
    {

		ListBats();
	}

	private async void ListBats()
	{
        var bats = await DBAccess.GetAllBatsAsync();
        Bats = new ObservableCollection<BatEx>(bats);
        
		
    }
}

public class ItemSourceSelector : IItemsSourceSelector
{


    IEnumerable IItemsSourceSelector.GetItemsSource(object record, object dataContext)
    {
        if (record == null) { return new List<BatTag>(); }

        var batEx = record as BatEx;
        if(batEx == null) { return new List<BatTag>(); }
        var context = dataContext as BatDetailsPage;
        if (context != null)
        {
            var tags = (from b in context.Bats
                        where b.Name == batEx.Name
                        select b.BatTags).FirstOrDefault()?.ToList();

            var tagList = from tag in (tags ?? Enumerable.Empty<BatTag>())
                          select tag.Tag;
            if(tagList?.Any() ?? false)
            {
                batEx.Tag = new BatTag() { Tag = batEx.Label };
            }
            return tags??new List<BatTag>();
        }
        /*
        var label = batEx.Label;
        var context = dataContext as BatDetailsPage;
        var list = (from batInList in context.Bats
                    where batInList.Name == batEx.Name
                    select batInList.BatTags).FirstOrDefault().ToList();
        if (list?.Any() ?? false)
        {
            return list;
        }*/
        return new List<BatTag>()	;
    }
}
