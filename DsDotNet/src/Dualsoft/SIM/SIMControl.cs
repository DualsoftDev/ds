using DevExpress.Data.Extensions;
using DevExpress.XtraBars.Navigation;
using DevExpress.XtraEditors.Filtering.Repository;
using DSModeler.Tree;
using Dual.Common.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;

using static Engine.Core.CoreModule;
using static Engine.Cpu.RunTime;
using static Engine.Cpu.RunTimeUtil;

namespace DSModeler
{
    public static class SIMControl
    {

        public static Dictionary<DsSystem, DsCPU> DicCpu = new Dictionary<DsSystem, DsCPU>();
        public static List<DsCPU> RunCpus = new List<DsCPU>();

        /// <summary>
        /// ThreadPool.GetMinThreads 제한으로 멀티로 동시에 돌리는게 한계(thread 스위칭으로 더느림)
        /// Active - Passive 2개로 돌림
        /// </summary>
        /// <returns></returns>
        public static List<DsCPU> GetRunCpus(Dictionary<DsSystem, DsCPU> dicCpu)
        {
            List<DsCPU> runCpus = new List<DsCPU>();

            var passiveStatements =
                    dicCpu.Where(d => d.Key != Global.ActiveSys)
                          .SelectMany(d => d.Value.CommentedStatements);

            if (passiveStatements.Any())
            {
                var passiveCPU =
                    new DsCPU(
                    passiveStatements
                  , dicCpu.Keys.First(w=> w != Global.ActiveSys)  // 가장 처음 디바이스
                  , Global.CpuRunMode);
                runCpus.Add(passiveCPU);
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


            //var procCnt = Environment.ProcessorCount;
            //Task.Factory.StartNew(() => {
            //    Parallel.ForEach(dic.Values, new ParallelOptions { MaxDegreeOfParallelism = procCnt },
            //        (cpu) =>
            //        {
            //            cpu.Run();  
            //        });
            //});
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
        public static void Stop( AccordionControlElement ace_Play)
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

            Task.WhenAll(RunCpus.Select(s =>
                           Task.Run(() => s.Reset()))
               );
            Global.Logger.Info("시뮬레이션 : Reset");
        }

        public static void Disconnect(Dictionary<DsSystem, DsCPU> dic)
        {
            Task.WhenAll(RunCpus.Select(s =>
                          Task.Run(() => s.Dispose()))
              );
        }
    }
}


