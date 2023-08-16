using DevExpress.XtraBars.Navigation;
using DSModeler.Tree;
using Dual.Common.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Engine.CodeGenCPU.ConvertCoreExt;
using static Engine.Core.CoreModule;
using static Engine.Core.Interface;
using static Engine.Core.TagKindModule;
using static Engine.Core.TagModule;
using static Engine.Cpu.RunTime;

namespace DSModeler
{
    public static class SIMControl
    {

        public static void RunSimMode(DsSystem sys)
        {
            var sysBits = Enum.GetValues(typeof(SystemTag)).Cast<SystemTag>();
            sysBits
                .Select(f => TagInfoType.GetTagSys(sys, f))
                .OfType<PlanVar<bool>>()
                .ForEach(tag =>
                {
                    int kind = ((IStorage)tag).TagKind;
                    if (
                       kind == (int)SystemTag.auto
                        || kind == (int)SystemTag.drive
                        || kind == (int)SystemTag.ready
                        || kind == (int)SystemTag.sim
                        )
                        tag.Value = true;
                });
        }

        public static void Play(Dictionary<DsSystem, DsCPU> dic, AccordionControlElement ace_Play)
        {
            if (!Global.IsLoadedPPT()) return;
            SimTree.SimPlayUI(ace_Play, true);
            RunSimMode(Global.ActiveSys);

            Task.WhenAll(dic.Values.Select(s => 
                            Task.Run(() => s.Run()))
                );
        }


        public static void Step(Dictionary<DsSystem, DsCPU> dic, AccordionControlElement ace_Play)
        {
            if (!Global.IsLoadedPPT()) return;
            SimTree.SimPlayUI(ace_Play, false);
            RunSimMode(Global.ActiveSys);
            Task.WhenAll(dic.Values.Select(s =>
                          Task.Run(() => s.Step()))
              );
        }
        public static void Stop(Dictionary<DsSystem, DsCPU> dic, AccordionControlElement ace_Play)
        {
            if (!Global.IsLoadedPPT()) return;
            SimTree.SimPlayUI(ace_Play, false);
            Task.WhenAll(dic.Values.Select(s =>
                          Task.Run(() => s.Stop()))
              );
        }
        public static void Reset(Dictionary<DsSystem, DsCPU> dic
            , AccordionControlElement ace_Play
            , AccordionControlElement ace_HMI)
        {
            if (!Global.IsLoadedPPT()) return;
            SimTree.SimPlayUI(ace_Play, false);
            HMITree.OffHMIBtn(ace_HMI);
            Task.WhenAll(dic.Values.Select(s =>
                           Task.Run(() => s.Reset()))
               );
        }

        public static void Disconnect(Dictionary<DsSystem, DsCPU> dic)
        {
            Task.WhenAll(dic.Values.Select(s =>
                          Task.Run(() => s.Dispose()))
              );
        }
    }
}


