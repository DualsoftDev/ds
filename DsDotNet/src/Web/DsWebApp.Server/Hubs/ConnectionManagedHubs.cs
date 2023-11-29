namespace DsWebApp.Server.Hubs;


public class ModelHub() : ConnectionManagedHub("ModelHub")
{
}

public class FieldIoHub() : ConnectionManagedHub("FiledIoHub")
{
}

public class HmiTagHub() : ConnectionManagedHub("HmiTagHub")
{
}
