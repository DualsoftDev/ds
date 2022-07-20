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
            Cpu = Model.Cpus.First(cpu => cpu.Name == activeCpuName);
            Cpu.Engine = this;

            Model.BuidGraphInfo();
            this.InitializeAllFlows(Opc);


            Model.Epilogue();


            Debug.Assert(Opc._opcTags.All(t => t.OriginalTag.IsExternal()));
            Opc.Print();

            Model.Cpus.Iter(cpu => readTagsFromOpc(cpu));

            void readTagsFromOpc(Cpu cpu)
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
        }
    }


    // Engine Initializer
    partial class Engine
    {
        public void InitializeAllFlows(OpcBroker opc)
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
            }


            TagGenInfo[] tgisActive = CreateTags4Child(Cpu, activeFlows);
            TagGenInfo[] tgisFake   = CreateTags4Child(FakeCpu, otherFlows);
            tgisActive.Select(tgi => tgi.GeneratedTag).Iter(Cpu.AddTag);
            tgisFake.Select(  tgi => tgi.GeneratedTag).Iter(FakeCpu.AddTag);

            foreach (var f in otherFlows)
            {
                f.Cpu = FakeCpu;
                f.RootSegments.SelectMany(s => s.AllPorts).Iter(p => p.OwnerCpu = FakeCpu);
                InitializeRootFlow(f, false, opc);
            }


            foreach (var f in activeFlows)
                InitializeRootFlow(f, true, opc);

            Cpu.BuildBackwardDependency();
            FakeCpu?.BuildBackwardDependency();

            opc._cpus.Add(Cpu);
            if (FakeCpu != null)
                opc._cpus.Add(FakeCpu);

            // debugging
            Debug.Assert(Cpu.CollectBits().All(b => b.OwnerCpu == Cpu));
            Debug.Assert(FakeCpu == null || FakeCpu.CollectBits().All(b => b.OwnerCpu == FakeCpu));
        }

        /// <summary> Root flow 에서 타 시스템을 호출하기 위한 interface tag 를 생성한다. </summary>

        void InitializeRootFlow(RootFlow rootFlow, bool isActiveCpu, OpcBroker opc)
        {
            // my flow 상의 root segment 들에 대한 HMI s/r/e tags
            var hmiTags = rootFlow.GenereateHmiTags4Segments().ToArray();
            var cpu = rootFlow.Cpu;

            hmiTags.Iter(t => t.Type = t.Type.Add(TagType.External));
            opc.AddTags(hmiTags);

            if (isActiveCpu)
            {

                //var subCalls = vertices.OfType<Segment>().SelectMany(seg => seg.CallChildren);
                //var rootCalls = vertices.OfType<Call>();
                //var calls = rootCalls.Concat(subCalls).Distinct();
                //foreach (var call in calls)
                //{
                //    call.OwnerCpu = cpu;
                //    var txs = call.TXs.OfType<Segment>().Distinct().ToArray();
                //    var rxs = call.RXs.OfType<Segment>().Distinct().ToArray();

                //    // Call prototype 의 tag 로부터 Call instance 에 사용할 tag 생성

                //    var startTags = txs.Select(s => Tag.CreateCallTx(call, s.TagS)).ToArray();
                //    opc.AddTags(startTags);
                //    call.TxTags = startTags;
                //    cpu.TxRxTags.AddRange(startTags);

                //    var endTags = rxs.Select(s => Tag.CreateCallRx(call, s.TagE)).ToArray();
                //    opc.AddTags(endTags);
                //    call.RxTags = endTags;
                //    cpu.TxRxTags.AddRange(endTags);

                //    call.OwnerCpu = flow.Cpu;
                //}

            }

            var tags = rootFlow.Cpu.TagsMap;
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
