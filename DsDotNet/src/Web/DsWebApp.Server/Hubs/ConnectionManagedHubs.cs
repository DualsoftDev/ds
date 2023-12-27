using IO.Core;

namespace DsWebApp.Server.Hubs;

using ClientId = string;

public class ModelHub() : ConnectionManagedHub(Name)
{
    public static string Name => "ModelHub";
    public static string HubPath => "/hub/model";
    public static HashSet<ClientId> ConnectedClients => ConnectionManagedHub.ConnectedClientMap[Name];
}

public class FieldIoHub() : ConnectionManagedHub(Name)
{
    public static string Name => "FiledIoHub";
    public static string HubPath => "/hub/io";
    public static HashSet<ClientId> ConnectedClients => ConnectionManagedHub.ConnectedClientMap[Name];
}

public class HmiTagHub() : ConnectionManagedHub(Name)
{
    public static string Name => "HmiTagHub";
    public static string HubPath => "/hub/hmi/tag";
    public static HashSet<ClientId> ConnectedClients => ConnectionManagedHub.ConnectedClientMap[Name];
}
public class InfoHub() : ConnectionManagedHub(Name)
{
    public static string Name => "InfoHub";
    public static string HubPath => "/hub/info";
    public static HashSet<ClientId> ConnectedClients => ConnectionManagedHub.ConnectedClientMap[Name];
}
