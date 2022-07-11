using Dsu.Common.Utilities.ExtensionMethods;

using log4net;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Engine.Core
{
    public abstract class CpuBase : Named, ICpu
    {
        public IEngine Engine { get; set; }
        public Model Model { get; }
        public RootFlow[] RootFlows { get; }

        public ConcurrentQueue<BitChange> Queue { get; } = new ConcurrentQueue<BitChange>();

        protected CpuBase(string name, RootFlow[] rootFlows, Model model) : base(name) {
            RootFlows = rootFlows;
            Model = model;
            rootFlows.Iter(f => f.Cpu = this);
        }


        public Dictionary<IBit, HashSet<IBit>> ForwardDependancyMap { get; } = new Dictionary<IBit, HashSet<IBit>>();
        public Dictionary<IBit, HashSet<IBit>> BackwardDependancyMap { get; private set; }
        public Dictionary<string, Tag> Tags { get; private set; }
        public List<Tag> TxRxTags { get; } = new List<Tag>();
        public void AddBitDependancy(IBit source, IBit target)
        {
            if (!ForwardDependancyMap.ContainsKey(source))
                ForwardDependancyMap[source] = new HashSet<IBit>();

            ForwardDependancyMap[source].Add(target);
        }

        public void BuildBackwardDependency()
        {
            BackwardDependancyMap = new Dictionary<IBit, HashSet<IBit>>();

            foreach (var tpl in ForwardDependancyMap)
            {
                (var source, var targets) = (tpl.Key, tpl.Value);

                foreach(var t in targets)
                {
                    if (!BackwardDependancyMap.ContainsKey(t))
                        BackwardDependancyMap[t] = new HashSet<IBit>();

                    BackwardDependancyMap[t].Add(source);
                }
            }

            Tags = this.CollectTags().Distinct().ToDictionary(t => t.Name, t => t);
        }

        /// <summary> 외부에서 tag 가 변경된 경우 </summary>
        public void OnOpcTagChanged(string tagName, bool value)
        {
            if (Tags.ContainsKey(tagName))
            {
                var tag = Tags[tagName];
                tag.Value = value;
                OnBitChanged(new BitChange(tag, value, true));
            }
        }

        public void OnBitChanged(BitChange bitChange)
        {
            Queue.Enqueue(bitChange);
            ProcessQueue();
        }

        public void ProcessQueue()
        {
            while (Queue.Count > 0)
            {
                BitChange bc;
                while(Queue.TryDequeue(out bc))
                {
                    var bit = bc.Bit;
                    if (bc.NewValue != bit.Value)
                    {
                        Debug.Assert(!bc.Applied);
                        bit.Value = bc.NewValue;
                    }

                    foreach ( var forward in ForwardDependancyMap[bit])
                        forward.Evaluate();
                }
            }
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
            var externalTagNames = string.Join("\r\n\t", tags.Where(t => t.IsExternal).Select(t => t.Name));
            var internalTagNames = string.Join("\r\n\t", tags.Where(t => !t.IsExternal).Select(t => t.Name));
            Logger.Debug($"-- Tags for {cpu.Name}");
            Logger.Debug($"  External:\r\n\t{externalTagNames}");
            Logger.Debug($"  Internal:\r\n\t{internalTagNames}");
        }
    }
}
