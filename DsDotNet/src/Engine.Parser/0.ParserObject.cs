using Engine.CpuUnit;

namespace Engine.Parser;


//public class ParserRootFlow : RootFlow
//{
//    public ParserRootFlow(string name, ParserSystem system)
//        : base(name, system)
//    {
//    }


//    // alias : ppt 도형으로 modeling 하면 문제가 되지 않으나, text grammar 로 서술할 경우, 
//    // 동일 이름의 call 등이 중복 사용되면, line 을 나누어서 기술할 때, unique 하게 결정할 수 없어서 도입.
//    // e.g Ap = { Ap1; Ap2;}
//    /// <summary> mnemonic -> target : "Ap1" -> "My.F.Ap", "My.F.Ap2" -> "My.F.Ap" </summary>
//    public Dictionary<string, string[]> AliasNameMaps = new();
//    /// <summary>target -> mnemonics : "My.F.Ap" -> ["Ap1"; "Ap2"] </summary>
//    public Dictionary<string[], string[]> BackwardAliasMaps = new(ParserExtension.NameComponentsComparer.Instance);
//    public virtual Cpu Cpu { get; }
//    public List<CallPrototype> CallPrototypes = new();


//}


//public class ParserChildFlow : ChildFlow
//{
//    public ParserChildFlow(string name, ParserRootFlow rootFlow)
//        : base(name, rootFlow)
//    {
//    }

//}



//public class ParserSystem : DsSystem
//{
//    public ParserSystem(string name, Model model)
//        : base(name, model)
//    {
//    }

//    public List<ParserRootFlow> ParserRootFlows = new();
//}

//public class ParserModel : Model
//{
//    public ParserModel()
//        : base()
//    {
//    }
//    public List<ParserSystem> ParserSystems = new();
//}

//public class ParserCpu : Cpu.CpuUnit.Cpu
//{
//    public ParserCpu(string name, Model model)
//        : base(name)
//    {
//    }
//    public TagDic TagsMap { get; } = new();

//    public List<ParserRootFlow> ParserRootFlows = new();
//}

//public class ParserSegment : SegmentBase
//{
//    public ParserSegment(string name, ParserChildFlow childFlow)
//        : base(name, childFlow)
//    {
//    }

//    public List<ParserRootFlow> ParserRootFlows = new();
//}

