using DevExpress.Utils.Extensions;
using DevExpress.Utils.Filtering.Internal;
using DevExpress.Utils.Serializing.Helpers;
using DevExpress.XtraBars.Navigation;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DSModeler.Tree;
using Dual.Common.Core;
using Dual.Common.Winform;
using Engine.Core;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;
using Server.HW.Common;
using Server.HW.WMX3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.DesignerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Engine.CodeGenCPU.CpuLoader;
using static Engine.Core.CoreModule;
using static Engine.Core.ExpressionForwardDeclModule;
using static Engine.Core.ExpressionModule;
using static Engine.Core.RuntimeGeneratorModule;
using static Engine.Core.TagModule;
using static Engine.Cpu.RunTime;
using static Engine.Cpu.RunTimeUtil;

namespace DSModeler
{
    public static class PcControl
    {

        public static Dictionary<DsSystem, PouGen> DicPou = new Dictionary<DsSystem, PouGen>();
        public static List<DsCPU> RunCpus = new List<DsCPU>();
        public static Dictionary<TagHW, ITag> DicActionIn = new Dictionary<TagHW, ITag>();
        public static Dictionary<ITag, TagHW> DicActionOut = new Dictionary<ITag, TagHW>();

        public static Dictionary<TagHW, ITag> GetActionInputs(DsSystem sys)
        {
            var actions = new Dictionary<TagHW, ITag>();

            sys.Jobs.Iter(j => j.DeviceDefs
                                .Where(w => !w.InAddress.IsNullOrEmpty())
                                .Iter(d => actions.Add(getTagHW(d.InTag, true), d.InTag)));

            return actions;
        }
        public static Dictionary<ITag, TagHW> GetActionOutputs(DsSystem sys)
        {
            var actions = new Dictionary<ITag, TagHW>();

            sys.Jobs.Iter(j => j.DeviceDefs
                                .Where(w => !w.OutAddress.IsNullOrEmpty())
                                .Iter(d => actions.Add(d.OutTag, getTagHW(d.OutTag, false))));

            return actions;
        }

        private static TagHW getTagHW(ITag dsTag, bool bInput)
        {
            string name = dsTag.Name;
            string address = dsTag.Address;

            if (address.IsNullOrEmpty() || dsTag == null)
                MBox.Error($"{dsTag}");

            var tag = new WMXTag(Global.PaixDriver.Conn as WMXConnection, name);
            tag.SetAddress(address);
            tag.IOType = bInput? TagIOType.Input : TagIOType.Output;        

            return tag;
        }


        private static void CreatePcControl()
        {
            if (Global.CpuRunMode.IsPackagePC())
            {
                PcControl.CreateConnect();
                DicActionIn = GetActionInputs(Global.ActiveSys);
                DicActionOut = GetActionOutputs(Global.ActiveSys);
                Global.PaixDriver.Conn.AddMonitoringTags(DicActionIn.Keys);
                Global.PaixDriver.Conn.AddMonitoringTags(DicActionOut.Values);
            }
        }
        public static void UpdateDevice(GridLookUpEdit gDevice)
        {
       

            gDevice.Do(() =>
            {
                var tags = DicActionIn.Keys.Cast<WMXTag>().ToList();
                tags.AddRange(DicActionOut.Values.Cast<WMXTag>());
                gDevice.Properties.DataSource = tags;
                gDevice.Properties.DisplayMember = "Name";
            });
        }
        
        public static async Task CreateRunCpuSingle()
        {
            CreatePcControl();

            List<DsCPU> runCpus = new List<DsCPU>();
            List<CommentedStatement> css = new List<CommentedStatement>();
            int cnt = 0;
            foreach (var cpu in DicPou.Values)
            {
                DsProcessEvent.DoWork(Convert.ToInt32(((cnt++ * 1.0) / DicPou.Values.Count()) * 50 + 50));
                css.AddRange(cpu.CommentedStatements().ToList());
                await Task.Delay(1);
            }

            var passiveCPU =
               new DsCPU(
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
        public static async Task GetRunCpus()
        {
            await Task.Yield();

            CreatePcControl();

            List<DsCPU> runCpus = new List<DsCPU>();
            //Global.ActiveSys 제외한  PC의 절반 CPU 활용
            var ableCpuCnt = (Environment.ProcessorCount - 1) / 2;

            var devices = DicPou.Values.Where(d => d.ToSystem() != Global.ActiveSys).ToList();
            if (devices.Any()) //1개이상은 외부 Device 존재
            {
                Dictionary<int, List<PouGen>> pous = new Dictionary<int, List<PouGen>>();
                for (int i = 0; i < devices.Count(); i++)
                {
                    var index = i % ableCpuCnt; //cpu 개수 만큼 만듬
                    if (!pous.ContainsKey(index))
                        pous.Add(index, new List<PouGen> { devices[i] });
                    else
                        pous[index].Add(devices[i]);
                }

                foreach (var pouSet in pous.Values)
                {
                    var passiveCPU =
                   new DsCPU(
                    pouSet.SelectMany(s => s.CommentedStatements())
                 , pouSet.Select(d => d.ToSystem())
                 , Global.CpuRunMode);

                    runCpus.Add(passiveCPU);
                }

            }

            var activeCPU = CreateCpu(DicPou[Global.ActiveSys]);
            runCpus.Add(activeCPU);

            RunCpus = runCpus;
        }

        public static DsCPU CreateCpu(PouGen pou)
        {
            var cpu = new DsCPU(
                pou.CommentedStatements(),
                new List<DsSystem>() { pou.ToSystem() },
                Global.CpuRunMode);

            return cpu ;
        }

        public static void Play(AccordionControlElement ace_Play)
        {
            if (!Global.IsLoadedPPT()) return;
            SimTree.PlayUI(ace_Play, true);

            if (RuntimeDS.Package.IsSimulation || RuntimeDS.Package.IsPackagePC())
            {
                Global.SimReset = false;
                if (RuntimeDS.Package.IsStandardPC)
                    Global.PaixDriver.Start();

                Task.WhenAll(RunCpus.Select(s =>
                                Task.Run(() => s.Run()))
                    );
               
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

            Task.WhenAll(RunCpus.Select(s =>
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
               
                Task.WhenAll(RunCpus.Select(s =>
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
            var activeCpu = RunCpus.First(w => w.Systems.Contains(Global.ActiveSys));

            if (RuntimeDS.Package.IsStandardPC)
                Global.PaixDriver.Stop();
            Task.Run(() =>
            {
                Task.Run(() => activeCpu.ResetActive()).Wait();

                Task.WhenAll(RunCpus.Where(w => w != activeCpu).Select(s =>
                    Task.Run(() => s.Reset()))
                );
            });
        
            Global.Logger.Info("시뮬레이션 : Reset");
        }

        public static void Disconnect()
        {
            Task.WhenAll(RunCpus.Select(s =>
                          Task.Run(() => s.Dispose()))
              );
        }

        public static void SetBit(WMXTag tag, bool value)
        {
            if (RuntimeDS.Package.IsPackagePC())
                tag.WriteRequestValue = value;
            else
                MBox.Warn("설정 H/W 에서 PC 타입을 선택하세요");
        }

        internal static void CreateConnect()
        {
            if(Global.PaixDriver != null) Global.PaixDriver.Conn.Disconnect();  

            if(Global.RunCountIn + Global.RunCountOut == 0)
                Global.Logger.Error($"IO Slot 개수가 0입니다. IO통신이 불가능합니다.");

            Global.PaixDriver = new PaixDriver(Global.PaixHW, Global.RunHWIP, Global.RunCountIn, Global.RunCountOut);
            if (Global.PaixDriver.Open())
                Global.Logger.Info($"{Global.PaixHW} {Global.RunHWIP} 연결에 성공 하였습니다.");
            else
                Global.Logger.Warn($"{Global.PaixHW} {Global.RunHWIP} 연결에 실패 하였습니다. 통신 연결을 확인하세요");
        }
    }
}


