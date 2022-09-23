//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Engine.Base.Obsolete;

//public static class CpuExtension
//{
//    public static void AddTag(this Cpu cpu, Tag tag)
//    {
//        Assert(tag.Cpu == cpu);
//        if (cpu.BitsMap.ContainsKey(tag.Name))
//        {
//            Assert(cpu.BitsMap[tag.Name] == tag);
//            return;
//        }
//        cpu.BitsMap.Add(tag.Name, tag);
//    }
//    public static IEnumerable<IBit> CollectBits(this Cpu cpu)
//    {
//        IEnumerable<IBit> helper()
//        {
//            foreach (var map in new[] { cpu.ForwardDependancyMap, cpu.BackwardDependancyMap })
//            {
//                if (map == null)
//                    continue;

//                foreach (var tpl in map)
//                {
//                    yield return tpl.Key;
//                    foreach (var v in tpl.Value)
//                        yield return v;
//                }
//            }
//        }

//        return helper().Distinct();
//    }
//    //public static void PrintTags(this Cpu cpu)
//    //{
//    //    var tags = cpu.Tags.ToArray();
//    //    var externalTagNames = string.Join("\r\n\t", tags.Where(t => t.IsExternal()).Select(t => t.Name));
//    //    var internalTagNames = string.Join("\r\n\t", tags.Where(t => !t.IsExternal()).Select(t => t.Name));
//    //    Logger.Debug($"-- Tags for {cpu.Name}");
//    //    Logger.Debug($"  External:\r\n\t{externalTagNames}");
//    //    Logger.Debug($"  Internal:\r\n\t{internalTagNames}");
//    //}

//}



//public static class CpuExtensionBitChange
//{
//    [Obsolete("Old version")]
//    public static void AddBitDependancy(this Cpu cpu, IBit source, IBit target)
//    {
//        Assert(source is not null && target is not null);

//        var fwdMap = cpu.ForwardDependancyMap;

//        if (!fwdMap.ContainsKey(source))
//        {
//            var srcTag = source as Tag;
//            if (srcTag != null)
//            {
//                var xxx = fwdMap.Keys.OfType<Tag>().FirstOrDefault(k => k.Name == srcTag.Name);
//                Assert(!fwdMap.Keys.OfType<Tag>().Any(k => k.Name == srcTag.Name));
//            }


//            fwdMap[source] = new HashSet<IBit>();
//        }

//        fwdMap[source].Add(target);
//    }

//    public static void BuildTagsMap(this Cpu cpu)
//    {
//        cpu.BitsMap
//            .Where(kv => kv.Value is Tag && !cpu.TagsMap.ContainsKey(kv.Key))
//            .Iter(kv => cpu.TagsMap.Add(kv.Key, kv.Value as Tag))
//            ;
//    }

//    [Obsolete("Old version")]
//    public static void BuildBackwardDependency(this Cpu cpu)
//    {
//        cpu.BackwardDependancyMap = new Dictionary<IBit, HashSet<IBit>>();
//        var bwdMap = cpu.BackwardDependancyMap;

//        foreach (var tpl in cpu.ForwardDependancyMap)
//        {
//            (var source, var targets) = (tpl.Key, tpl.Value);

//            foreach (var t in targets)
//            {
//                if (!bwdMap.ContainsKey(t))
//                    bwdMap[t] = new HashSet<IBit>();

//                bwdMap[t].Add(source);
//            }
//        }
//    }
//}
