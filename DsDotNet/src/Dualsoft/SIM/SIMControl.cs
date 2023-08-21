using DevExpress.Utils.Extensions;
using DevExpress.Utils.Filtering.Internal;
using DevExpress.Utils.Serializing.Helpers;
using DevExpress.XtraBars.Navigation;
using DSModeler.Tree;
using Dual.Common.Core;
using Engine.Core;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Engine.CodeGenCPU.CpuLoader;
using static Engine.Core.CoreModule;
using static Engine.Core.ExpressionForwardDeclModule;
using static Engine.Core.ExpressionModule;
using static Engine.Core.TagModule;
using static Engine.Cpu.RunTime;
using static Engine.Cpu.RunTimeUtil;

namespace DSModeler
{
    public static class SIMControl
    {

        public static Dictionary<DsSystem, PouGen> DicPou = new Dictionary<DsSystem, PouGen>();
        public static List<DsCPU> RunCpus = new List<DsCPU>();
        public static Dictionary<string, ITag> DicActionInput = new Dictionary<string, ITag>();

        public static Dictionary<string, ITag> GetActionInputs(DsSystem sys)
        {
            var actionInputs = new Dictionary<string, ITag>();

            sys.Jobs.Iter(j => j.DeviceDefs
                                .Where(w => !w.InAddress.IsNullOrEmpty())
                                .Iter(d => actionInputs.Add(d.InAddress, d.InTag))) ;

            return actionInputs;
        }

        public static async Task CreateRunCpuSingle()
        {
            DicActionInput = GetActionInputs(Global.ActiveSys);
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
        }
        /// <summary>
        /// ThreadPool.GetMinThreads 제한으로 멀티로 동시에 돌리는게 한계(thread 스위칭으로 더느림)
        /// Active 1 - Passive n개로 돌림  PC의 절반 CPU 활용
        /// </summary>
        /// <returns></returns>
        public static async Task GetRunCpus()
        {
            await Task.Yield();

            DicActionInput = GetActionInputs(Global.ActiveSys);

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
            Global.SimReset = false;
            SimTree.SimPlayUI(ace_Play, true);

            Task.WhenAll(RunCpus.Select(s =>
                            Task.Run(() => s.Run()))
                );

            Global.Logger.Info("시뮬레이션 : Run");
        }


        public static void Step(AccordionControlElement ace_Play)
        {
            if (!Global.IsLoadedPPT()) return;
            Global.SimReset = false;
            SimTree.SimPlayUI(ace_Play, false);

            Task.WhenAll(RunCpus.Select(s =>
                          Task.Run(() => s.Step()))
              );
            Global.Logger.Info("시뮬레이션 : Step");
        }
        public static void Stop(AccordionControlElement ace_Play)
        {
            if (!Global.IsLoadedPPT()) return;
            Global.SimReset = false;
            SimTree.SimPlayUI(ace_Play, false);

            Task.WhenAll(RunCpus.Select(s =>
                          Task.Run(() => s.Stop()))
              );
            Global.Logger.Info("시뮬레이션 : Stop");
        }
        public static void Reset(
              AccordionControlElement ace_Play
            , AccordionControlElement ace_HMI)
        {
            if (!Global.IsLoadedPPT()) return;
            Global.SimReset = true;
            SimTree.SimPlayUI(ace_Play, false);
            HMITree.OffHMIBtn(ace_HMI);
            var activeCpu = RunCpus.First(w => w.Systems.Contains(Global.ActiveSys));

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

        //public static void ReadyMode()
        //{
        //    Task.WhenAll(RunCpus.Select(s =>
        //                  Task.Run(() => s.ReadyMode()))
        //      );
        //}
      
    }
}


