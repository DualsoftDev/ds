namespace Engine.CodeGen

open Engine.Core
open System.Collections.Generic
open System.Collections.Concurrent

[<AutoOpen>]
module GateModule =

    [<AbstractClass>]
    type Gate(out:ICpuBit) as this = 
        interface IGate with
            override x.Update() = 
                        let outValue = this.EvaluateGate()                      
                        if outValue <> out.Value 
                        then out.Value <- outValue
        
        abstract EvaluateGate: unit -> bool
        member x.Out = out
        member private x.UsedUpdate(bit:Bit, bAdd:bool) = 
            if bAdd then bit.AddGate(this) else bit.RemoveGate(this)
            |> ignore

        member x.AddBit(exps: ConcurrentDictionary<Bit, bool>, bit:Bit, ?not:bool) =  
                        if exps.TryAdd(bit, not.IsSome && not.Value)
                        then this.UsedUpdate(bit, true)
        member x.RemoveBit(exps: ConcurrentDictionary<Bit, bool>, bit:Bit) =  
                        if (exps.TryRemove(bit) |> fun (removed, _) -> removed)
                        then this.UsedUpdate(bit, false)
       
    type GateOr(out:Bit) as this = 
        inherit Gate(out)
        let ors = ConcurrentDictionary<Bit, bool>()
        override x.EvaluateGate() =
            ors |> Seq.map(fun orBit ->orBit.Key, orBit.Value)
                |> Evaluation  
        //ors bit add 
        member x.Add(bit:Bit, ?not:bool) = 
            this.AddBit(ors, bit, not.IsSome && not.Value)
        //ors bit remove
        member x.Remove(bit:Bit) = 
            this.RemoveBit(ors, bit)
                   

    type GateSR(out:Bit) as this = 
        inherit Gate(out)
        //set   bit의 and 조합(not value 포함)
        let sets = ConcurrentDictionary<Bit, bool>()
        //reset bit의 and 조합(not value 포함)
        let rsts = ConcurrentDictionary<Bit, bool>()
        
        // start reset 둘다 성립시 reset 우선
        let evaluateGateSR() =
            let setOn = sets |> Seq.map(fun set ->set.Key, set.Value) |> Evaluation  
            let rstOn = rsts |> Seq.map(fun rst ->rst.Key, rst.Value) |> Evaluation
            match rstOn, setOn, out.Value with
            |true , _, _ -> false         //reset 우선
            |false, true , _ -> true      //set true
            |false, false, _ -> out.Value //pass

        //Set bit add 
        member x.AddSet(bit:Bit, ?not:bool) = 
            this.AddBit(sets, bit, not.IsSome && not.Value)
        //Set bit remove
        member x.RemoveSet(bit:Bit) =
            this.RemoveBit(sets, bit)
        
        //Reset bit add
        member x.AddRst(bit:Bit, ?not:bool) = 
            this.AddBit(rsts, bit, not.IsSome && not.Value)
        //Reset bit remove
        member x.RemoveRst(bit:Bit) =
            this.RemoveBit(rsts, bit)

        override x.EvaluateGate() = evaluateGateSR() 
