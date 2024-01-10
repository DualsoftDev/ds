using Engine.Custom;

using System.Diagnostics;

namespace EngineCustom;

public class Cylinder : IBitObject
{
    public DsApi DsApi { get; }

    public Cylinder(DsApi dsApi)
    {
        DsApi = dsApi;
    }

    (string, string) getIOTags(string objectName) =>
        objectName switch {
            "cyl1Adv" => ("%ix11", "%ox11"),
            "cyl1Ret" => ("%ix12", "%ox12"),
            _ => throw new Exception($"Unknown object: {objectName}"),
        };
public async Task SetAsync(string objectName)
    {
        Debug.WriteLine($"Setting {objectName}");

        var (tagI, tagO) = getIOTags(objectName);

        DsApi.WriteTag(tagO, true);

        while(true)
        {
            var done = (bool)DsApi.ReadTag(tagI);
            if (done)
            {
                // 출력 끊기
                DsApi.WriteTag(tagO, false);
                break;
            }

            await Task.Delay(30);
        }
    }

    public async Task ResetAsync(string objectName)
    {
        Debug.WriteLine($"Resetting {objectName}");
        // simulate long-running task
        await Task.Yield();
        var (tagI, tagO) = getIOTags(objectName);
        var reverse = objectName switch
        {
            "cyl1Adv" => "cyl1Ret",
            "cyl1Ret" => "cyl1Adv",
            _ => throw new Exception($"Unknown object: {objectName}"),
        };
        var (rTagI, rTagO) = getIOTags(reverse);

        if (!DsApi.ReadBit(rTagI))
        {
            DsApi.WriteTag(rTagO, true);
            while (!DsApi.ReadBit(rTagI))
                await Task.Delay(30);
            DsApi.WriteTag(rTagO, false);
        }
    }
}
public class CustomExtension : IEngineExtension
{
    public Dictionary<string, IDsObject> Initialize(DsApi dsApi)
    {
        Cylinder cylinder = new (dsApi);
        return new Dictionary<string, IDsObject>
        {
            { "cyl1Adv",  cylinder},
            { "cyl1Ret",  cylinder},
        };
    }
}
