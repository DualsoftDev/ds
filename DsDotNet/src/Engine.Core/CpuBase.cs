using log4net;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Engine.Common;

namespace Engine.Core
{
    public abstract class CpuBase : Named, ICpu
    {
        public IEngine Engine { get; set; }
        public Model Model { get; }
        /// <summary> this Cpu 가 관장하는 root flows </summary>
        public RootFlow[] RootFlows { get; }

        /// <summary> Bit change event queue </summary>
        public ConcurrentQueue<BitChange> Queue { get; } = new ConcurrentQueue<BitChange>();
        public CsGraphInfo GraphInfo { get; set; }

        /// <summary> bit 간 순방향 의존성 map </summary>
        public Dictionary<IBit, HashSet<IBit>> ForwardDependancyMap { get; } = new Dictionary<IBit, HashSet<IBit>>();
        /// <summary> bit 간 역방향 의존성 map </summary>
        public Dictionary<IBit, HashSet<IBit>> BackwardDependancyMap { get; internal set; }
        /// <summary> this Cpu 관련 tags.  Root segment 의 S/R/E 및 call 의 Tx, Rx </summary>
        public Dictionary<string, Tag> Tags { get; internal set; }
        /// <summary> Call 의 TX RX 에 사용된 tag 목록 </summary>
        public List<Tag> TxRxTags { get; } = new List<Tag>();

        protected CpuBase(string name, RootFlow[] rootFlows, Model model) : base(name)
        {
            RootFlows = rootFlows;
            Model = model;
            rootFlows.Iter(f => f.Cpu = this);
        }
    }

    public class Cpu: CpuBase
    {
        public Cpu(string name, RootFlow[] rootFlows, Model model)
            : base(name, rootFlows, model)
        {
            model.Cpus.Add(this);
        }
    }

    public class FakeCpu : CpuBase
    {
        public FakeCpu(string name, RootFlow[] rootFlows, Model model)
            : base(name, rootFlows, model)
        {
        }
    }

    public static class CpuExtension
    {
        static ILog Logger => Global.Logger;
        public static IEnumerable<IBit> CollectBits(this CpuBase cpu)
        {
            IEnumerable<IBit> helper()
            {
                foreach (var map in new[] { cpu.ForwardDependancyMap, cpu.BackwardDependancyMap })
                {
                    if (map == null)
                        continue;

                    foreach (var tpl in map)
                    {
                        yield return tpl.Key;
                        foreach (var v in tpl.Value)
                            yield return v;
                    }
                }
            }

            return helper().Distinct();
        }
        public static IEnumerable<Tag> CollectTags(this CpuBase cpu) => cpu.TxRxTags.Concat(cpu.CollectBits()).OfType<Tag>();


        public static void PrintTags(this CpuBase cpu)
        {
            var tags = cpu.CollectTags().ToArray();
            var externalTagNames = string.Join("\r\n\t", tags.Where(t => t.IsExternal()).Select(t => t.Name));
            var internalTagNames = string.Join("\r\n\t", tags.Where(t => !t.IsExternal()).Select(t => t.Name));
            Logger.Debug($"-- Tags for {cpu.Name}");
            Logger.Debug($"  External:\r\n\t{externalTagNames}");
            Logger.Debug($"  Internal:\r\n\t{internalTagNames}");
        }
    }


    public static class CpuExtensionBitChange
    {
        public static void AddBitDependancy(this CpuBase cpu, IBit source, IBit target)
        {
            var fwdMap = cpu.ForwardDependancyMap;
            if (!fwdMap.ContainsKey(source))
                fwdMap[source] = new HashSet<IBit>();

            fwdMap[source].Add(target);
        }

        public static void BuildBackwardDependency(this CpuBase cpu)
        {
            cpu.BackwardDependancyMap = new Dictionary<IBit, HashSet<IBit>>();
            var bwdMap = cpu.BackwardDependancyMap;

            foreach (var tpl in cpu.ForwardDependancyMap)
            {
                (var source, var targets) = (tpl.Key, tpl.Value);

                foreach (var t in targets)
                {
                    if (!bwdMap.ContainsKey(t))
                        bwdMap[t] = new HashSet<IBit>();

                    bwdMap[t].Add(source);
                }
            }

            cpu.Tags = cpu.CollectTags().Distinct().ToDictionary(t => t.Name, t => t);
        }

        public static void OnTagChanged(this CpuBase cpu, Tag tag, bool value)
        {

        }
        /// <summary> 외부에서 tag 가 변경된 경우 </summary>
        public static void OnOpcTagChanged(this CpuBase cpu, string tagName, bool value)
        {
            if (cpu.Tags.ContainsKey(tagName))
            {
                var tag = cpu.Tags[tagName];
                tag.Value = value;
                cpu.OnBitChanged(new BitChange(tag, value, true));
            }
        }

        public static void OnBitChanged(this CpuBase cpu, BitChange bitChange)
        {
            cpu.Queue.Enqueue(bitChange);
            cpu.ProcessQueue();
        }

        public static void ProcessQueue(this CpuBase cpu)
        {
            BitChange bc;
            while (cpu.Queue.Count > 0)
            {
                while (cpu.Queue.TryDequeue(out bc))
                {
                    var bit = bc.Bit;
                    if (bc.NewValue != bit.Value)
                    {
                        Debug.Assert(!bc.Applied);
                        bit.Value = bc.NewValue;
                    }

                    // 변경 이벤트 공지
                    Global.BitChangedSubject.OnNext(bc);

                    foreach (var forward in cpu.ForwardDependancyMap[bit])
                        forward.Evaluate();
                }

                Thread.Sleep(10);
            }
        }
    }
}
