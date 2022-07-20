using Engine.Common;
using Engine.Core;
using Engine.OPC;
using Engine.Parser;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

[assembly: DebuggerDisplay("[{Key}={Value}]", Target = typeof(KeyValuePair<,>))]

namespace Engine
{
    public partial class Engine : IEngine
    {
        public OpcBroker Opc { get; }
        public Cpu Cpu { get; }
        public FakeCpu FakeCpu { get; set; }
        public Model Model { get; }

        public Engine(string modelText, string activeCpuName)
        {
            Model = ModelParser.ParseFromString(modelText);

            Opc = new OpcBroker();
            Cpu = Model.Cpus.OfType<Cpu>().First(cpu => cpu.Name == activeCpuName);
            Cpu.Engine = this;

            Model.BuidGraphInfo();
            InitializeAllFlows();


            Model.Epilogue();


            Opc.Print();

            Model.Cpus.Iter(cpu => readTagsFromOpc(cpu));
            Model.Cpus.Iter(cpu => cpu.PrintTags());

            void readTagsFromOpc(CpuBase cpu)
            {
                var tpls = Opc.ReadTags(cpu.TagsMap.Select(t => t.Key)).ToArray();
                foreach ((var tName, var value) in tpls)
                {
                    var tag = cpu.TagsMap[tName];
                    if (tag.Value != value)
                        cpu.OnOpcTagChanged(tName, value);
                }
            }
        }

        public void Run()
        {
            //Cpu.Run();
            //FakeCpu.Run();
            Global.Logger.Info("Engine started!");
        }
    }


    // Engine Initializer
    partial class Engine
    {
        public void InitializeAllFlows()
        {
            var allRootFlows = Model.Systems.SelectMany(s => s.RootFlows);
            var flowsGrps =
                from flow in allRootFlows
                group flow by Cpu.RootFlows.Contains(flow) into g
                select new { Active = g.Key, Flows = g.ToList() };
            ;
            var activeFlows = flowsGrps.Where(gr => gr.Active).SelectMany(gr => gr.Flows).ToArray();
            var otherFlows = flowsGrps.Where(gr => !gr.Active).SelectMany(gr => gr.Flows).ToArray();
            Debug.Assert(activeFlows.All(f => f.Cpu == Cpu));

            if (otherFlows.Any())
            {
                // generate fake cpu's for other flows
                FakeCpu = new FakeCpu("FakeCpu", otherFlows, Model) { Engine = this };
                Model.Cpus.Add(FakeCpu);
            }


            TagGenInfo[] tgisActive = CreateTags4Child(Cpu, activeFlows);
            TagGenInfo[] tgisFake   = CreateTags4Child(FakeCpu, otherFlows);
            var tagsActive = tgisActive.Select(tgi => tgi.GeneratedTag).ToArray();
            var tagsFake = tgisFake.Select(tgi => tgi.GeneratedTag).ToArray();
            tagsActive.Iter(Cpu.AddTag);
            tagsFake.Iter(FakeCpu.AddTag);

            var allTgis = tgisActive.Concat(tgisFake).ToArray();
            allTgis.Iter(tgi => copyChildSRETagsToSegment(tgi));

            Opc.AddTags(tagsActive);
            Opc.AddTags(tagsFake);


            foreach (var f in otherFlows)
            {
                f.Cpu = FakeCpu;
                f.RootSegments.SelectMany(s => s.AllPorts).Iter(p => p.OwnerCpu = FakeCpu);
                InitializeRootFlow(f, false);
            }


            foreach (var f in activeFlows)
                InitializeRootFlow(f, true);

            Cpu.BuildBackwardDependency();
            FakeCpu?.BuildBackwardDependency();

            Opc._cpus.Add(Cpu);
            if (FakeCpu != null)
                Opc._cpus.Add(FakeCpu);

            // debugging
            Debug.Assert(Cpu.CollectBits().All(b => b.OwnerCpu == Cpu));
            Debug.Assert(FakeCpu == null || FakeCpu.CollectBits().All(b => b.OwnerCpu == FakeCpu));



            /// 'Child' 의 Tags{Start,Reset,End} tag 들을 Child 가 실제 가리키는 segment 의 S/R/E Tags 에도 반영한다. 
            void copyChildSRETagsToSegment(TagGenInfo tgi)
            {
                //tgi.Child.
            }
        }

        /// <summary> Root flow 의 root segment 를 타 시스템에서 호출하기 위한 interface tag 를 생성한다. </summary>

        void InitializeRootFlow(RootFlow rootFlow, bool isActiveCpu)
        {
            // my flow 상의 root segment 들에 대한 HMI s/r/e tags
            var hmiTags = rootFlow.GenereateHmiTags4Segments().ToArray();
            var cpu = rootFlow.Cpu;

            hmiTags.Iter(t => t.Type = t.Type.Add(TagType.External));
            hmiTags.Iter(cpu.AddTag);
            Opc.AddTags(hmiTags);

            var tags = cpu.TagsMap;
            // Edge 를 Bit 로 보고
            // A -> B 연결을 A -> Edge -> B 연결 정보로 변환
            foreach (var e in rootFlow.Edges)
            {
                var deps = e.CollectForwardDependancy().ToArray();
                foreach ((var src_, var tgt_) in deps)
                {
                    (var src, var tgt) = (src_, tgt_);
                    if (cpu != src.OwnerCpu)
                        src = tags[((Named)src).Name];
                    if (cpu != tgt.OwnerCpu)
                        tgt = tags[((Named)tgt).Name];

                    Debug.Assert(cpu == src.OwnerCpu);
                    Debug.Assert(cpu == tgt.OwnerCpu);
                    rootFlow.Cpu.AddBitDependancy(src, tgt);
                }
            }


            rootFlow.PrintFlow(isActiveCpu);

            //cpu.PrintTags();
        }
    }

    public static class EngineExtension
    {

    }
}
