using BPASpectrogramM.Navigation;

namespace BPASpectrogramM.Views;

public partial class SpectrogramButtons : ContentView
{
    public event EventHandler<EventArgs> SpectrogramButtonClicked;
    protected virtual void OnSpectrogramButtonClicked(EventArgs e) { SpectrogramButtonClicked?.Invoke(this, e); }
    public string Text { get; set; } = "";

    public string[][] batNames =
    {
        new string[] {"NoBats", "PIP", "SLN","FM", "Barbastelle", "SP and CP", "PIP and FM", "PIP and Daubentons" },
        new string[] {"Bat", "SP", "Serotine", "Myotis", "Daubentons", "SP and Noctule", "CP and Noctule", "CP and Daubentons" },
        new string[] {"Nathusius", "CP","Leislers","BLE","Natterers","SP and Barbastelle","CP and Barbastelle","SP and Daubentons" },
        new string[] {"P40","P50","Noctule","??","Barbastelle?","SP and Leislers","CP and Leislers","CP and Leislers","CP and SP and Daubentons" },
        new string[] {"Clear()" }
    };
	
	
    public SpectrogramButtons()
    {
		
        InitializeComponent();

        var numRows=ButtonGrid.RowDefinitions.Count;
        var numCols=ButtonGrid.ColumnDefinitions.Count;

        for (int i = 0; i < numRows; i++)
        {
            for (int j = 0; j < numCols; j++)
            {
                Button b = new Button();
                b.HeightRequest = 30;
                b.BorderColor = Colors.Black;
                b.BorderWidth = 2;
                if (i<batNames.Length && j < batNames[i].Length)
                {
                    b.Text = batNames[i][j];
                }
                b.Clicked += (sender, e) =>
                {
                    if (sender is Button thisButton)
                    {
                        buttonClicked(thisButton);
                    }
                };
                ButtonGrid.Add(b, j,i);

            }
        }
		
    }

    private void buttonClicked(Button button)
    {
        if (button != null)
        {
            BPAServiceProvider.GetService<BPASpectrogramM.ViewModels.SpectrogramToolbarVM>()?.SetModified();
            Text =button.Text;
            OnSpectrogramButtonClicked(EventArgs.Empty);
			
        }
    }
}
