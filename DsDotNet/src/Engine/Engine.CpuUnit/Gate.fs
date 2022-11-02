namespace Engine.Cpu

open System.Collections.Generic
open System.Collections.Concurrent

[<AutoOpen>]
module GateModule =

    [<AbstractClass>]
    type Gate(out:ICpuBit) as this = 
        interface IGate with
            member _.AddBit(exps: ConcurrentDictionary<ICpuBit,bool>, bit: ICpuBit, negative: bool): unit = 
                if exps.TryAdd(bit, negative)
                then this.UsedUpdate(bit:?>Bit, true)
            member _.RemoveBit(exps: ConcurrentDictionary<ICpuBit,bool>, bit: ICpuBit): unit = 
                if (exps.TryRemove(bit) |> fun (removed, _) -> removed)
                then this.UsedUpdate(bit:?>Bit, false)
            member _.Update() = 
                let outValue = this.EvaluateGate()                      
                if outValue <> out.Value 
                then out.Value <- outValue
        
        abstract EvaluateGate: unit -> bool
        member x.Out = out
        member private x.UsedUpdate(bit:Bit, bAdd:bool) = 
            if bAdd then bit.AddGate(this) else bit.RemoveGate(this)
            |> ignore

       
    type GateOR(out:ICpuBit) as this = 
        inherit Gate(out)
        let ors = ConcurrentDictionary<ICpuBit, bool>()
        override x.EvaluateGate() =
            ors |> Seq.map(fun orBit ->orBit.Key, orBit.Value)
                |> EvaluationOr  
        //ors bit add 
        member x.Add(bit:Bit, ?negative:bool) = 
            (this :> IGate).AddBit(ors, bit, negative.IsSome && negative.Value)
        //ors bit remove
        member x.Remove(bit:Bit) = 
            (this :> IGate).RemoveBit(ors, bit)

    type GateAND(out:ICpuBit) as this = 
        inherit Gate(out)
        let ands = ConcurrentDictionary<ICpuBit, bool>()
        override x.EvaluateGate() =
            ands |> Seq.map(fun andBit ->andBit.Key, andBit.Value)
                 |> EvaluationAnd  
        //ands bit add 
        member x.Add(bit:Bit, ?negative:bool) = 
            (this :> IGate).AddBit(ands, bit, negative.IsSome && negative.Value)
        //ands bit remove
        member x.Remove(bit:Bit) = 
            (this :> IGate).RemoveBit(ands, bit)
             
    type GateSR(out:ICpuBit) as this = 
        inherit Gate(out)
        //set   bit의 and 조합(not value 포함)
        let sets = ConcurrentDictionary<ICpuBit, bool>()
        //reset bit의 and 조합(not value 포함)
        let rsts = ConcurrentDictionary<ICpuBit, bool>()
        
        // start reset 둘다 성립시 reset 우선
        let evaluateGateSR() =
            let setOn = sets |> Seq.map(fun set ->set.Key, set.Value) |> EvaluationAnd  
            let rstOn = rsts |> Seq.map(fun rst ->rst.Key, rst.Value) |> EvaluationAnd
            match rstOn, setOn, out.Value with
            |true , _, _ -> false         //reset 우선
            |false, true , _ -> true      //set true
            |false, false, _ -> out.Value //pass

        //Set bit add 
        member x.AddSet(bit:Bit, ?negative:bool) =
            (this :> IGate).AddBit(sets, bit, negative.IsSome && negative.Value)
        //Set bit remove
        member x.RemoveSet(bit:Bit) = 
            (this :> IGate).RemoveBit(sets, bit)
        
        //Reset bit add
        member x.AddRst(bit:Bit, ?negative:bool) = 
            (this :> IGate).AddBit(rsts, bit, negative.IsSome && negative.Value)
        //Reset bit remove
        member x.RemoveRst(bit:Bit) = 
            (this :> IGate).RemoveBit(rsts, bit)

        override x.EvaluateGate() = evaluateGateSR() 
