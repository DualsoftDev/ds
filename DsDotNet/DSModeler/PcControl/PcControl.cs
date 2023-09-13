
using DocumentFormat.OpenXml.Wordprocessing;
using static Engine.Core.CoreModule;

namespace DSModeler.PcControl;
[SupportedOSPlatform("windows")]
public static class PcContr
{
    public static List<DsCPU> RunCpus = new();
    public static Dictionary<TagHW, IEnumerable<ITag>> DicActionIn;
    public static Dictionary<ITag, TagHW> DicActionOut;
    public static Dictionary<TagHW, IEnumerable<ITag>> GetActionInputs(DsSystem sys)
    {
        Dictionary<TagHW, IEnumerable<ITag>> actions = new();
        IEnumerable<ITag> inTags
             = sys.Jobs
                  .SelectMany(j => j.DeviceDefs.Select(s => s.InTag))
                  .Where(w => w != null)
                  .Where(w => !w.Address.Trim().IsNullOrEmpty());

        _ = inTags
          .GroupBy(g => g.Address)
          .Iter(g =>
          {
              string names = string.Join(", ", g.Select(s => s.Name));
              TagHW hwTag = getTagHW(names, g.Key);
              actions.Add(hwTag, g.Select(s => s));
          });

        return actions;
    }
    public static Dictionary<ITag, TagHW> GetActionOutputs(DsSystem sys)
    {
        Dictionary<ITag, TagHW> actions = new();
        IEnumerable<ITag> outTags
             = sys.Jobs
                  .SelectMany(j => j.DeviceDefs.Select(s => s.OutTag))
                  .Where(w => w != null)
                  .Where(w => !w.Address.Trim().IsNullOrEmpty());

        _ = outTags
          .GroupBy(g => g.Address)
          .Iter(g =>
          {
              string names = string.Join(", ", g.Select(s => s.Name));
              TagHW hwTag = getTagHW(names, g.Key);
              _ = g.Iter(s => actions.Add(s, hwTag));
          });

        return actions;
    }

    private static TagHW getTagHW(string name, string address)
    {
        if (address.IsNullOrEmpty())
            _ = MBox.Error($"주소가 없습니다. {name}");

        TagHW tag = null;
        if (HwModels.GetListByCompany(Company.LSE).Contains(Global.DSHW))
        {
            var xgTag = AddressConvert.tryParseTagByCpu(address, Global.DSHW.ModelId);
            if (xgTag != null)
            {
                TagIOType ioType = TagIOType.Memory;

                if (xgTag.Value.GetIOM() == "I") ioType = TagIOType.Input;
                else if (xgTag.Value.GetIOM() == "O") ioType = TagIOType.Output;
                else if (xgTag.Value.GetIOM() == "M") ioType = TagIOType.Memory;

                tag = new XG5KTag(Global.DsDriver.Conn as XG5KConnection, name);
                tag.SetAddress(address, tag.BitOffset, ioType);
            }
        }
        else if (HwModels.GetListByCompany(Company.PAIX).Contains(Global.DSHW))
        {
            ///...
        }
        if(tag == null)
            _ = MBox.Error($"TAG 등록실패 name:{name}, address:{address}");


        return tag;
    }


    private static void CreatePcControl(GridLookUpEdit gDevice)
    {
        PcAction.CreateConnect();
        DicActionIn = GetActionInputs(Global.ActiveSys);
        DicActionOut = GetActionOutputs(Global.ActiveSys);
        _ = Global.DsDriver.Conn.AddMonitoringTags(DicActionIn.Keys.Distinct());
        _ = Global.DsDriver.Conn.AddMonitoringTags(DicActionOut.Values.Distinct());

      
        gDevice.Do(() =>
        {
            List<TagHW> tags = DicActionIn.Keys.Cast<TagHW>().ToList();
            tags.AddRange(DicActionOut.Values.Cast<TagHW>());
            gDevice.Properties.DataSource = tags;
            gDevice.Properties.DisplayMember = "Name";
        });
    }

   

    public static async Task CreateRunCpuSingle(Dictionary<DsSystem, PouGen> DicPou, GridLookUpEdit gDevice)
    {
        if (Global.CpuRunMode.IsPackagePC())
        {
            CreatePcControl(gDevice);
        }

        List<DsCPU> runCpus = new();
        List<CommentedStatement> css = new();
        int cnt = 0;
        foreach (PouGen cpu in DicPou.Values)
        {
            DsProcessEvent.DoWork(Convert.ToInt32((cnt++ * 1.0 / DicPou.Values.Count() * 50) + 50));
            css.AddRange(cpu.CommentedStatements().ToList());
            await Task.Delay(1);
        }

        DsCPU passiveCPU =
           new(
           css
         , DicPou.Values.Select(s => s.ToSystem())
         , Global.CpuRunMode);

        runCpus.Add(passiveCPU);
        RunCpus = runCpus;
        DsProcessEvent.DoWork(100);
    }


    /// <summary>
    /// ThreadPool.GetMinThreads 제한으로 멀티로 동시에 돌리는게 한계(thread 스위칭으로 더느림)
    /// Active 1 - Passive n개로 돌림  PC의 절반 CPU 활용
    /// </summary>
    /// <returns></returns>
    //public static async Task GetRunCpus(Dictionary<DsSystem, PouGen> DicPou, GridLookUpEdit gDevice)
    //{
    //    await Task.Yield();

    //    if (Global.CpuRunMode.IsPackagePC())
    //    {
    //        CreatePcControl(gDevice);
    //    }

    //    List<DsCPU> runCpus = new();
    //    //Global.ActiveSys 제외한  PC의 절반 CPU 활용
    //    int ableCpuCnt = (Environment.ProcessorCount - 1) / 2;

    //    List<PouGen> devices = DicPou.Values.Where(d => d.ToSystem() != Global.ActiveSys).ToList();
    //    if (devices.Any()) //1개이상은 외부 Device 존재
    //    {
    //        Dictionary<int, List<PouGen>> pous = new();
    //        for (int i = 0; i < devices.Count(); i++)
    //        {
    //            int index = i % ableCpuCnt; //cpu 개수 만큼 만듬
    //            if (!pous.ContainsKey(index))
    //            {
    //                pous.Add(index, new List<PouGen> { devices[i] });
    //            }
    //            else
    //            {
    //                pous[index].Add(devices[i]);
    //            }
    //        }

    //        foreach (List<PouGen> pouSet in pous.Values)
    //        {
    //            DsCPU passiveCPU =
    //           new(
    //            pouSet.SelectMany(s => s.CommentedStatements())
    //         , pouSet.Select(d => d.ToSystem())
    //         , Global.CpuRunMode);

    //            runCpus.Add(passiveCPU);
    //        }

    //    }

    //    DsCPU activeCPU = CreateCpu(DicPou[Global.ActiveSys]);
    //    runCpus.Add(activeCPU);

    //    RunCpus = runCpus;
    //}

    public static DsCPU CreateCpu(PouGen pou)
    {
        DsCPU cpu = new(
            pou.CommentedStatements(),
            new List<DsSystem>() { pou.ToSystem() },
            Global.CpuRunMode);

        return cpu;
    }

    internal static void Stop()
    {
        var con = Global.DsDriver.Conn as XG5KConnection;
        con.Stop();

        PcContr.DicActionIn = null;
        PcContr.DicActionOut = null;

    }
}


