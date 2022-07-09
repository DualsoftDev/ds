using Dsu.Common.Utilities.ExtensionMethods;

using System.Collections.Generic;

namespace Engine.Core
{
    public class Cpu : Named
    {
        public Model Model;
        public RootFlow[] RootFlows;
        public Cpu(string name, RootFlow[] rootFlows, Model model) : base(name) {
            RootFlows = rootFlows;
            Model = model;
            model.Cpus.Add(this);
            rootFlows.Iter(f => f.Cpu = this);
        }


        public Dictionary<IBit, HashSet<IBit>> ForwardDependancyMap { get; } = new Dictionary<IBit, HashSet<IBit>>();
        public Dictionary<IBit, HashSet<IBit>> BackwardDependancyMap { get; private set; }
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
        }
    }
}
