using DsWebApp.Server.Controllers;
using DsWebApp.Shared;


using Microsoft.AspNetCore.SignalR;

namespace DsWebApp.Server.Hubs
{
    public class FieldIoHub(IConfiguration configuration) : ConnectionManagedHub
    {
    }
}
