using Dsu.Common.Utilities.ExtensionMethods;

using System.Collections.Generic;

namespace Engine.Core
{
    public class Cpu : Named
    {
        public Model Model;
        public Flow[] Flows;
        public Cpu(string name, Flow[] flows, Model model) : base(name) {
            Flows = flows;
            Model = model;
            model.Cpus.Add(this);
            flows.Iter(f => f.Cpu = this);
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
