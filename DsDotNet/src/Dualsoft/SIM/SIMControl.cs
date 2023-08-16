using DevExpress.XtraBars.Navigation;
using DSModeler.Tree;
using Dual.Common.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using static Engine.Core.CoreModule;
using static Engine.Cpu.RunTime;

namespace DSModeler
{
    public static class SIMControl
    {

      

        public static void Play(Dictionary<DsSystem, DsCPU> dic, AccordionControlElement ace_Play)
        {
            if (!Global.IsLoadedPPT()) return;
            Global.SimReset = false;
            SimTree.SimPlayUI(ace_Play, true);
            
            Task.WhenAll(dic.Values.Select(s => 
                            Task.Run(() => s.Run()))
                );
            Global.Logger.Info("시뮬레이션 : Run");
        }


        public static void Step(Dictionary<DsSystem, DsCPU> dic, AccordionControlElement ace_Play)
        {
            if (!Global.IsLoadedPPT()) return;
            Global.SimReset = false;
            SimTree.SimPlayUI(ace_Play, false);

            Task.WhenAll(dic.Values.Select(s =>
                          Task.Run(() => s.Step()))
              );
            Global.Logger.Info("시뮬레이션 : Step");
        }
        public static void Stop(Dictionary<DsSystem, DsCPU> dic, AccordionControlElement ace_Play)
        {
            if (!Global.IsLoadedPPT()) return;
            Global.SimReset = false;
            SimTree.SimPlayUI(ace_Play, false);

            Task.WhenAll(dic.Values.Select(s =>
                          Task.Run(() => s.Stop()))
              );
            Global.Logger.Info("시뮬레이션 : Stop");
        }
        public static void Reset(Dictionary<DsSystem, DsCPU> dic
            , AccordionControlElement ace_Play
            , AccordionControlElement ace_HMI)
        {
            if (!Global.IsLoadedPPT()) return;
            Global.SimReset = true;
            SimTree.SimPlayUI(ace_Play, false);
            HMITree.OffHMIBtn(ace_HMI);

            Task.WhenAll(dic.Values.Select(s =>
                           Task.Run(() => s.Reset()))
               );
            Global.Logger.Info("시뮬레이션 : Reset");
        }

        public static void Disconnect(Dictionary<DsSystem, DsCPU> dic)
        {
            Task.WhenAll(dic.Values.Select(s =>
                          Task.Run(() => s.Dispose()))
              );
        }
    }
}


