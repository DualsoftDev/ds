using DevExpress.XtraBars.Navigation;
using Dual.Common.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static Engine.CodeGenCPU.ConvertCoreExt;
using static Engine.CodeGenCPU.TagManagerModule;
using static Engine.Core.CoreModule;
using static Engine.Core.Interface;
using static Engine.Core.TagKindModule;
using static Engine.Core.TagModule;
using static Engine.Cpu.RunTime;

namespace DSModeler
{
    public static class SIM
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

        public static void Play(Dictionary<DsSystem, DsCPU> dic)
        {
            dic.ForEach(f =>
            {
                var system = f.Key;
                var cpu = f.Value;

                cpu.Run();
                SIM.RunSimMode(system);
            });
        }
        public static void Step(Dictionary<DsSystem, DsCPU> dic)
        {
        }
        public static void Stop(Dictionary<DsSystem, DsCPU> dic)
        {
            dic.ForEach(f =>
            {
                var cpu = f.Value;
                cpu.Stop();
            });
        }
        public static void Reset(Dictionary<DsSystem, DsCPU> dic)
        {
        }
    }

}


