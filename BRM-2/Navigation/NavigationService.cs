namespace BRM_2.Navigation;
public class NavigationService:INavigationService
{
    public async Task Navigate(string pageName, Dictionary<string, object> parameters)
    {
        await Shell.Current.GoToAsync(pageName);
    }

    public Task GotoMapSelectionPage() => Navigate("mapSelectionPage",new Dictionary<string, object>());

    public Task GotoSessionsPage() => Navigate("sessPage", new Dictionary<string, object>());

    public Task GotoSessionDetailsPage() => Navigate("sessDetailsPage", new Dictionary<string, object>());

    public Task GotoRecordingsPage() => Navigate("recordingsPage", new Dictionary<string, object>());

    public Task GotoBatDetailsPage() => Navigate("batDetailsPage", new Dictionary<string, object>());

    public Task GoBack() => Shell.Current.GoToAsync("..");
}
