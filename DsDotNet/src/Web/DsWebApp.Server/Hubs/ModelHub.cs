namespace DsWebApp.Server.Hubs
{
    public class ModelHub(IConfiguration configuration) : ConnectionManagedHub("ModelHub")
    {
    }
}
