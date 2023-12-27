using IO.Core;

namespace DsWebApp.Server.Hubs;

using ClientId = string;

public class ModelHub() : ConnectionManagedHub("ModelHub")
{
    public static HashSet<ClientId> ConnectedClients => ConnectionManagedHub.ConnectedClientMap["ModelHub"];
}

public class FieldIoHub() : ConnectionManagedHub("FiledIoHub")
{
    public static HashSet<ClientId> ConnectedClients => ConnectionManagedHub.ConnectedClientMap["FiledIoHub"];
}

public class HmiTagHub() : ConnectionManagedHub("HmiTagHub")
{
    public static HashSet<ClientId> ConnectedClients => ConnectionManagedHub.ConnectedClientMap["HmiTagHub"];
}
public class InfoHub() : ConnectionManagedHub("InfoHub")
{
    public static HashSet<ClientId> ConnectedClients => ConnectionManagedHub.ConnectedClientMap["InfoHub"];
}
