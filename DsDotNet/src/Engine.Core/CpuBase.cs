using Dsu.Common.Utilities.ExtensionMethods;

using log4net;

using System.Collections.Generic;
using System.Linq;

namespace Engine.Core
{
    public abstract class CpuBase : Named, ICpu
    {
        public IEngine Engine { get; set; }
        public Model Model { get; }
        public RootFlow[] RootFlows { get; }

        protected CpuBase(string name, RootFlow[] rootFlows, Model model) : base(name) {
            RootFlows = rootFlows;
            Model = model;
            rootFlows.Iter(f => f.Cpu = this);
        }


        public Dictionary<IBit, HashSet<IBit>> ForwardDependancyMap { get; } = new Dictionary<IBit, HashSet<IBit>>();
        public Dictionary<IBit, HashSet<IBit>> BackwardDependancyMap { get; private set; }
        public Dictionary<string, Tag> Tags { get; private set; }
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

            Tags = this.CollectTags().ToDictionary(t => t.Name, t => t);
        }

        public void OnOpcTagChanged(string tagName, bool value)
        {
            var tag = Tags[tagName];
            OnBitChanged(tag, value);
        }

        public void OnBitChanged(IBit bit, bool value)
        {
            bit.SetOrReset(value);
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

    public static class CpuHelper
    {
        static ILog Logger => Global.Logger;
        public static IEnumerable<IBit> CollectBits(this CpuBase cpu)
        {
            IEnumerable<IBit> Helper()
            {
                foreach (var map in new[] { cpu.ForwardDependancyMap, cpu.BackwardDependancyMap })
                {
                    foreach (var tpl in map)
                    {
                        yield return tpl.Key;
                        foreach (var v in tpl.Value)
                            yield return v;
                    }
                }
            }

            return Helper().Distinct();
        }
        public static IEnumerable<Tag> CollectTags(this CpuBase cpu) => cpu.CollectBits().OfType<Tag>();


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
