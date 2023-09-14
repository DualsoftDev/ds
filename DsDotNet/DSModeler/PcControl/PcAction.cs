
namespace DSModeler.PcControl;
[SupportedOSPlatform("windows")]
public static class PcAction
{
    public static void Play(AccordionControlElement ace_Play)
    {
        if (!Global.IsLoadedPPT())
        {
            return;
        }

        SimTree.PlayUI(ace_Play, true);

        if (RuntimeDS.Package.IsSimulation || RuntimeDS.Package.IsPackagePC())
        {
            Global.SimReset = false;


            _ = Task.WhenAll(PcContr.RunCpus.Select(s =>
                            Task.Run(() => s.Run()))
                );

            //FormMain.formMain.connection.On<Tuple<string, int>>("S2CSet", tpl =>  //<<shin>>
            //{
            //    MessageBox.Show($"Got value change notification from server: {tpl.Item1} = {tpl.Item2}", "get change value");
            //    //MessageBox.Show($"Got value change notification from server: {tag}", "get change value");
            //});

            Global.Logger.Info("Push Run");
        }
        else
        {
            _ = MBox.Warn("설정 H/W 에서 Simution 타입을 선택하세요");
        }
    }


    public static void Step(AccordionControlElement ace_Play)
    {
        if (!Global.IsLoadedPPT())
        {
            return;
        }

        SimTree.PlayUI(ace_Play, false);
        if (RuntimeDS.Package.IsPackagePLC())
        {
            _ = MBox.Warn("설정 H/W 에서 Simution or PC 타입을 선택하세요");
            return;
        }
        Global.SimReset = false;

        _ = Task.WhenAll(PcContr.RunCpus.Select(s =>
                      Task.Run(() => s.Step()))
          );
        Global.Logger.Info("Push Step");
    }
    public static void Stop(AccordionControlElement ace_Play)
    {
        if (!Global.IsLoadedPPT())
        {
            return;
        }

        SimTree.PlayUI(ace_Play, false);

        if (RuntimeDS.Package.IsSimulation || RuntimeDS.Package.IsPackagePC())
        {
            Global.SimReset = false;

            _ = Task.WhenAll(PcContr.RunCpus.Select(s =>
                          Task.Run(() => s.Stop()))
              );

            if (RuntimeDS.Package.IsStandardPC)
            {
                Global.DsDriver.Stop();
            }

            Global.Logger.Info("Push Stop");
        }
        else
        {
            _ = MBox.Warn("설정 H/W 에서 Simution 타입을 선택하세요");
        }
    }
    public static void Reset(
          AccordionControlElement ace_Play
        , AccordionControlElement ace_HMI
        , GridLookUpEdit gDevice)
    {
        if (!Global.IsLoadedPPT()) return;

        SimTree.PlayUI(ace_Play, false);
        Global.SimReset = true;
        HMITree.OffHMIBtn(ace_HMI);
        Engine.Cpu.RunTime.DsCPU activeCpu = PcContr.RunCpus.First(w => w.Systems.Contains(Global.ActiveSys));

        if (RuntimeDS.Package.IsStandardPC && Global.DsDriver != null)
        {
            Task.Run(async () =>
            {
                var tags = PcContr.DicActionOut.Values;
                if (Global.DSHW.Company == Company.LSE)
                {
                    tags.Cast<XG5KTag>().Iter(t => t.XgPLCTag.WriteValue = false);
                    while (tags.Cast<XG5KTag>().Where(t => t.XgPLCTag.WriteValue != null).Any())
                        await Task.Delay(1);
                }
                else
                {
                    tags.Iter(t => t.WriteRequestValue = false);
                    while (tags.Where(t => t.WriteRequestValue != null).Any()) 
                        await Task.Delay(1);
                }

                while (tags.Where(t => t.WriteRequestValue != null).Any())
                    await Task.Delay(1);

                Global.DsDriver.Stop();
                PcContr.CreatePcControl(gDevice);
            });
        }

        _ = Task.Run(() =>
        {
            Task.Run(() => activeCpu.ResetActive()).Wait();

            _ = Task.WhenAll(PcContr.RunCpus.Where(w => w != activeCpu).Select(s =>
                Task.Run(() => s.Reset()))
            );
        });

        Global.Logger.Info("Push Reset");
    }

    public static void Disconnect()
    {
        _ = Task.WhenAll(PcContr.RunCpus.Select(s =>
                      Task.Run(() => s.Dispose()))
          );
    }

    public static void SetBit(TagHW tag, bool value)
    {
        if (tag == null) return;
        if (RuntimeDS.Package.IsPackagePC())
        {
            if (Global.DSHW.Company == Company.LSE)
            {
                var tagHW = PcContr.DicActionOut.Values.First(f => f == tag);
                ((XG5KTag)tagHW).XgPLCTag.WriteValue = value;
            }
            else
                tag.WriteRequestValue = value;
        }
        else
        {
            _ = MBox.Warn("설정 H/W 에서 PC 타입을 선택하세요");
        }
    }

    internal static void CreateConnect()
    {
        _ = (Global.DsDriver?.Conn.Disconnect());

        if (Global.RunCountIn + Global.RunCountOut == 0)
        {
            Global.Logger.Error($"IO Slot 개수가 0입니다. IO통신이 불가능합니다.");
        }

        Global.DsDriver = new DsDriver(Global.DSHW, Global.RunHWIP, Global.RunCountIn, Global.RunCountOut);
        if (Global.DsDriver.Open())
        {
            Global.Logger.Info($"{Global.DSHW} {Global.RunHWIP} 연결에 성공 하였습니다.");
        }
        else
        {
            Global.Logger.Warn($"{Global.DSHW} {Global.RunHWIP} 연결에 실패 하였습니다. 통신 연결을 확인하세요");
        }
    }
}