namespace Engine.Cpu

open System.Diagnostics
open System.Collections.Generic
open Engine.Core

[<AutoOpen>]
module Cpu =

  
    [<DebuggerDisplay("{name}")>]
    type DsCpu(name:string, model:DsModel)  =
        let assignFlows = HashSet<IFlow>() 
        interface ICpu 

        member x.Name = name
        member x.AssignFlows = assignFlows
        member x.Model = model
        member val IsActive = false with get, set
        member x.SubscribeStatusEvent(onoff:bool) = ()



        
//public class Cpu : Named, ICpu
//{
//    public IEngine Engine { get; set; }
//    public Model Model { get; }

//    /// <summary> My System 의 Cpu 인지 여부</summary>
//    public bool IsActive { get; set; }



//    /// <summary> this Cpu 가 관장하는 root flows </summary>
//    public List<RootFlow> RootFlows { get; } = new();



//    /// <summary> Bit change event queue </summary>
//    public ObservableConcurrentQueue<BitChange> Queue { get; } = new();
//    //public ConcurrentQueue<BitChange[]> Queue { get; } = new();

//    /// <summary>CPU queue 에 더 처리할 내용이 있음을 외부에 알리기 위한 flag</summary>
//    public bool ProcessingQueue { get; internal set; }
//    /// <summary>외부에서 CPU 를 멈추거나 가동하기 위한 flag</summary>
//    public Action<BitChange, bool> Apply { get; internal set; }

//    public bool Running { get; set; } = true;
//    public bool NeedWait => Running && (ProcessingQueue || Queue.Count > 0);

//    internal Dictionary<ICpuBit, FlipFlop[]> FFSetterMap;
//    internal Dictionary<ICpuBit, FlipFlop[]> FFResetterMap;
//    internal int DbgNestingLevel { get; set; }
//    public int DbgThreadId { get; internal set; }

//    public GraphInfo GraphInfo { get; set; }

//    /// <summary> bit 간 순방향 의존성 map </summary>
//    public Dictionary<ICpuBit, HashSet<ICpuBit>> ForwardDependancyMap { get; } = new();
//    /// <summary> bit 간 역방향 의존성 map </summary>
//    public Dictionary<ICpuBit, HashSet<ICpuBit>> BackwardDependancyMap { get; internal set; }
//    /// <summary> this Cpu 관련 tags.  Root segment 의 S/R/E 및 call 의 Tx, Rx </summary>
//    public BitDic BitsMap { get; } = new();
//    public TagDic TagsMap { get; } = new();

//    public Cpu(string name, Model model) : base(name)
//    {
//        Model = model;
//        model.Cpus.Add(this);
//    }

//    public override string ToText() => $"Cpu [{Name}={DbgThreadId}]";
//}