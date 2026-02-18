


using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace BPASpectrogramM.Views;

public partial class SpectrogramControls : ContentView, INotifyPropertyChanged
{
	public event PropertyChangedEventHandler PropertyChanged;
	protected virtual void OnPropertyChanged([CallerMemberName] string PropertyName = "")
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
	}

	public SpectrogramControls()
	{

		InitializeComponent();
		
        CommentEntry.Focused += CommentEntry_Focused;
		CommentEntry.Unfocused += CommentEntry_Unfocused;


	}


	private void CommentEntry_Unfocused(object? sender, FocusEventArgs e)
	{
		Debug.WriteLine("Comment Entry Lost Focus");

	}

	private void CommentEntry_Focused(object? sender, FocusEventArgs e)
	{
		Debug.WriteLine($"Comment Entry has focus");

	}


	public enum AUTOADVANCEMODE { OFF = 0, BUTTON = 1, TEXT = 2, BOTH = 3 }
	public AUTOADVANCEMODE CurrentAutoAdvanceState = AUTOADVANCEMODE.OFF;
	public AUTOADVANCEMODE CurrentAutoAdvanceMode = AUTOADVANCEMODE.TEXT;
	public bool AutoAdvanceOn { get; set; } = false;

	internal async Task SetEntryFocus()
	{
		try
		{
			await Task.Run(() =>
			{
				Thread.Sleep(500);
				MainThread.BeginInvokeOnMainThread(() =>
				{

					var cf = CommentEntry.Focus();
				});
			});
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex.ToString());
		}


	}

	internal void ToggleAutoAdvance()
	{
		AutoAdvanceOn = !AutoAdvanceOn;
		if (AutoAdvanceOn)
		{
			AutoAdvanceButton.Background = new SolidColorBrush(Colors.LightGreen);
			CurrentAutoAdvanceState = CurrentAutoAdvanceMode;
		}
		else
		{
			AutoAdvanceButton.Background = new SolidColorBrush(Colors.Wheat);
			CurrentAutoAdvanceState = AUTOADVANCEMODE.OFF;
		}
	}

	private void mfiAutoOff_Clicked(object sender, EventArgs e)
	{
		AutoAdvanceOn = false;
		AutoAdvanceButton.Background = new SolidColorBrush(Colors.Wheat);
		CurrentAutoAdvanceState = AUTOADVANCEMODE.OFF;
	}

	private void mfiAutoButton_Clicked(object sender, EventArgs e)
	{
		AutoAdvanceOn = true;
		AutoAdvanceButton.Background = new SolidColorBrush(Colors.LightGreen);
		CurrentAutoAdvanceState = AUTOADVANCEMODE.BUTTON;
	}

	private void mfiAutoText_Clicked(object sender, EventArgs e)
	{
		AutoAdvanceOn = true;
		AutoAdvanceButton.Background = new SolidColorBrush(Colors.LightGreen);
		CurrentAutoAdvanceState = AUTOADVANCEMODE.TEXT;
	}

	private void mfiAutoBoth_Clicked(object sender, EventArgs e)
	{
		AutoAdvanceOn = true;
		AutoAdvanceButton.Background = new SolidColorBrush(Colors.LightGreen);
		CurrentAutoAdvanceState = AUTOADVANCEMODE.BOTH;
	}
}
