namespace BRM_2.Interfaces;
public interface INavigationService
{
    Task GotoMapSelectionPage();

    Task GotoRecordingsPage();

    Task GotoBatDetailsPage();

    

    Task GotoSessionsPage();

    Task GoBack();
}
