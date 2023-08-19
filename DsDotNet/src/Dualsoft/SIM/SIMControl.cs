using DevExpress.Utils.Extensions;
using DevExpress.XtraBars.Navigation;
using DSModeler.Tree;
using Dual.Common.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using static Engine.Core.CoreModule;
using static Engine.Cpu.RunTime;

namespace DSModeler
{
    public static class SIMControl
    {

        public static Dictionary<DsSystem, DsCPU> DicCpu = new Dictionary<DsSystem, DsCPU>();
        public static List<DsCPU> RunCpus = new List<DsCPU>();

        /// <summary>
        /// ThreadPool.GetMinThreads 제한으로 멀티로 동시에 돌리는게 한계(thread 스위칭으로 더느림)
        /// Active 1 - Passive n개로 돌림  PC의 절반 CPU 활용
        /// </summary>
        /// <returns></returns>
        public static List<DsCPU> GetRunCpuSingle(Dictionary<DsSystem, DsCPU> dicCpu)
        {
            List<DsCPU> runCpus = new List<DsCPU>();

            var passiveCPU =
           new DsCPU(
           dicCpu.Values.SelectMany(d => d.CommentedStatements)
         , dicCpu.Values.SelectMany(d => d.Systems)
         , Global.CpuRunMode);

            runCpus.Add(passiveCPU);
            return runCpus;
        }
        public static List<DsCPU> GetRunCpus(Dictionary<DsSystem, DsCPU> dicCpu)
        {
            List<DsCPU> runCpus = new List<DsCPU>();
            //Global.ActiveSys 제외한  PC의 절반 CPU 활용
            var ableCpuCnt = (Environment.ProcessorCount - 1) / 2;

            var devices = dicCpu.Values.Where(d => !d.Systems.Contains( Global.ActiveSys)).ToList();
            if (devices.Any()) //1개이상은 외부 Device 존재
            {
                Dictionary<int, List<DsCPU>> cpus = new Dictionary<int, List<DsCPU>>();
                for (int i = 0; i < devices.Count(); i++)
                {
                    var index = i % ableCpuCnt; //cpu 개수 만큼 만듬
                    if (!cpus.ContainsKey(index))
                        cpus.Add(index, new List<DsCPU> { devices[i] });
                    else
                        cpus[index].Add(devices[i]);
                }

                foreach (var cpu in cpus)
                {
                    var passiveCPU =
                   new DsCPU(
                   cpu.Value.SelectMany(d => d.CommentedStatements)
                 , cpu.Value.SelectMany(d => d.Systems)
                 , Global.CpuRunMode);

                    runCpus.Add(passiveCPU);
                }

            }

            var activeCPU = dicCpu[Global.ActiveSys];
            runCpus.Add(activeCPU);

            return runCpus;
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
            var activeCpu = DicCpu[Global.ActiveSys];

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

        public static void ReadySim()
        {
            Task.WhenAll(RunCpus.Select(s =>
                          Task.Run(() => s.ReadySim()))
              );
        }
    }
}


