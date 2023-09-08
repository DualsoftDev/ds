using DevExpress.XtraBars.Navigation;
using DSModeler.Tree;
using Microsoft.AspNetCore.SignalR.Client;
using Server.HW.XG5K;
using System;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows;
using static Engine.Core.RuntimeGeneratorModule;

namespace DSModeler;
[SupportedOSPlatform("windows")]
public static class PcAction
{
    public static void Play(AccordionControlElement ace_Play)
    {
        if (!Global.IsLoadedPPT()) return;
        SimTree.PlayUI(ace_Play, true);

        if (RuntimeDS.Package.IsSimulation || RuntimeDS.Package.IsPackagePC())
        {
            Global.SimReset = false;
            if (RuntimeDS.Package.IsStandardPC)
                Global.PaixDriver.Start();

            Task.WhenAll(PcControl.RunCpus.Select(s =>
                            Task.Run(() => s.Run()))
                );

            FormMain.formMain.connection.On<Tuple<string, int>>("S2CSet", tpl =>
            {
                MessageBox.Show($"Got value change notification from server: {tpl.Item1} = {tpl.Item2}", "get change value");
                //MessageBox.Show($"Got value change notification from server: {tag}", "get change value");
            });

            Global.Logger.Info("시뮬레이션 : Run");
        }
        else
            MBox.Warn("설정 H/W 에서 Simution 타입을 선택하세요");
    }


    public static void Step(AccordionControlElement ace_Play)
    {
        if (!Global.IsLoadedPPT()) return;
        SimTree.PlayUI(ace_Play, false);
        if (!RuntimeDS.Package.IsSimulation)
        {
            MBox.Warn("설젱 H/W 에서 Simution 타입을 선택하세요");
            return;
        }
        Global.SimReset = false;

        Task.WhenAll(PcControl.RunCpus.Select(s =>
                      Task.Run(() => s.Step()))
          );
        Global.Logger.Info("시뮬레이션 : Step");
    }
    public static void Stop(AccordionControlElement ace_Play)
    {
        if (!Global.IsLoadedPPT()) return;
        SimTree.PlayUI(ace_Play, false);

        if (RuntimeDS.Package.IsSimulation || RuntimeDS.Package.IsPackagePC())
        {
            Global.SimReset = false;

            Task.WhenAll(PcControl.RunCpus.Select(s =>
                          Task.Run(() => s.Stop()))
              );

            if (RuntimeDS.Package.IsStandardPC)
                Global.PaixDriver.Stop();

            Global.Logger.Info("시뮬레이션 : Stop");
        }
        else
            MBox.Warn("설정 H/W 에서 Simution 타입을 선택하세요");
    }
    public static void Reset(
          AccordionControlElement ace_Play
        , AccordionControlElement ace_HMI)
    {
        if (!Global.IsLoadedPPT()) return;
        SimTree.PlayUI(ace_Play, false);
        Global.SimReset = true;
        HMITree.OffHMIBtn(ace_HMI);
        var activeCpu = PcControl.RunCpus.First(w => w.Systems.Contains(Global.ActiveSys));

        if (RuntimeDS.Package.IsStandardPC)
            Global.PaixDriver?.Stop();

        Task.Run(() =>
        {
            Task.Run(() => activeCpu.ResetActive()).Wait();

            Task.WhenAll(PcControl.RunCpus.Where(w => w != activeCpu).Select(s =>
                Task.Run(() => s.Reset()))
            );
        });

        Global.Logger.Info("시뮬레이션 : Reset");
    }

    public static void Disconnect()
    {
        Task.WhenAll(PcControl.RunCpus.Select(s =>
                      Task.Run(() => s.Dispose()))
          );
    }

    public static void SetBit(XG5KTag tag, bool value)
    {
        if (RuntimeDS.Package.IsPackagePC())
            tag.WriteRequestValue = value;
        else
            MBox.Warn("설정 H/W 에서 PC 타입을 선택하세요");
    }

    internal static void CreateConnect()
    {
        if (Global.PaixDriver != null) Global.PaixDriver.Conn.Disconnect();

        if (Global.RunCountIn + Global.RunCountOut == 0)
            Global.Logger.Error($"IO Slot 개수가 0입니다. IO통신이 불가능합니다.");

        Global.PaixDriver = new PaixDriver(Global.PaixHW, Global.RunHWIP, Global.RunCountIn, Global.RunCountOut);
        if (Global.PaixDriver.Open())
            Global.Logger.Info($"{Global.PaixHW} {Global.RunHWIP} 연결에 성공 하였습니다.");
        else
            Global.Logger.Warn($"{Global.PaixHW} {Global.RunHWIP} 연결에 실패 하였습니다. 통신 연결을 확인하세요");
    }
}