namespace BRM_2.Controls;
public partial class SessionDetailsDisplay :  ContentView
{
   

    public SessionDetailsDisplay()
    {
        var viewModel = BRM_2.Navigation.ServiceProvider.GetService<SessionDetailsDisplayVM>();
        InitializeComponent();

        //populateLists();
        BindingContext = viewModel;
    }


    


}
