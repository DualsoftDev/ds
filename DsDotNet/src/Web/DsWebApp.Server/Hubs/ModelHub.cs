namespace DsWebApp.Server.Hubs
{
    public class ModelHub : ConnectionManagedHub
    {
        public ModelHub(IConfiguration configuration)
        {
            //GlobalCounter.TheGlobalCounter ??= configuration.GetSection("GlobalCounter").Get<GlobalCounter>();
        }


    }

}
