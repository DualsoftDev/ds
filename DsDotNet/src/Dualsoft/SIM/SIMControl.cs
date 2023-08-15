using DevExpress.XtraBars.Navigation;
using DSModeler.Tree;
using Dual.Common.Core;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public static async Task Play(Dictionary<DsSystem, DsCPU> dic, AccordionControlElement ace_Play)
        {
            SimTree.SimPlayUI(ace_Play, true);
            await Task.Run(() =>
            {
                dic.ForEach(f =>
                {
                    var system = f.Key;
                    var cpu = f.Value;

                    SIMControl.RunSimMode(system);
                    cpu.Run();
                });
            });
        }


        public static async Task Step(Dictionary<DsSystem, DsCPU> dic, AccordionControlElement ace_Play)
        {
            SimTree.SimPlayUI(ace_Play, false);
            await Task.Run(() =>
            {
                dic.ForEach(f =>
                {
                    var system = f.Key;
                    var cpu = f.Value;

                    SIMControl.RunSimMode(system);
                    cpu.Step();
                });
            });
        }
        public static async Task Stop(Dictionary<DsSystem, DsCPU> dic, AccordionControlElement ace_Play)
        {
            SimTree.SimPlayUI(ace_Play, false);
            await Task.Run(() =>
            {
                dic.ForEach(f =>
                {
                    var cpu = f.Value;
                    cpu.Stop();
                });
            });
        }
        public static async Task Reset(Dictionary<DsSystem, DsCPU> dic
            , AccordionControlElement ace_Play
            , AccordionControlElement ace_HMI)
        {
            SimTree.SimPlayUI(ace_Play, false);
            HMITree.OffHMIBtn(ace_HMI);
            await Task.Run(() =>
            {
                dic.ForEach(f =>
                {
                    var cpu = f.Value;
                    cpu.Reset();
                });
            });
        }
    }

}


