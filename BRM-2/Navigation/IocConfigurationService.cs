namespace BRM_2.Navigation;
internal class IocConfigurationService : IMauiInitializeService
{
    public void Initialize(IServiceProvider services)
    {
        Ioc.Default.ConfigureServices(services);
        
    }
}
