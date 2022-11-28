namespace Engine.Cpu

open System.Diagnostics
open System
open System.Text.RegularExpressions
open Engine.Core

[<AutoOpen>]
module  TagModule = 

    [<AbstractClass>]
    [<DebuggerDisplay("{Name}")>]
    type Tag<'T>(name) as this =
        interface ITag with
            member _.ToText() = $"({name}={(this.Data.ToString())})"
            member _.Name: string = name
            member _.Data  = this.Data 
          
        member x.ToText() = (x :> ITag).ToText()
        member x.Name     = (x :> ITag).Name
        abstract Data:IData with get, set


    /// (ActionTAG) 시스템 매개체 TAG 정의 class
    [<AbstractClass>]
    type ActionTag<'T> (name, data:IData) =
        inherit Tag<'T>(name)
        let mutable data = data 
        member x.SetValue(v:obj) = data <- v |> ToData
        override x.Data    with get()        = data
                           and  set(v:IData) = data <- v

    /// (PlanTAG) 시스템 인과 계획정보 TAG 정의 class
    [<AbstractClass>]
    type PlanTag<'T> (name, memory:IData) =
        inherit Tag<'T>(name)
        let mutable memory:Memory = memory :?> Memory

        member x.Memory = memory
        override x.Data     with get()        = memory.Value|> ToData
                            and  set(v:IData) = memory.Value <- (byte)(v|>ToValue)
        
                           
    /// PLC tag (ActionTAG) class
    type PlcTag<'T> private (name, data:IData) =
         inherit ActionTag<'T>(name, data)
         member val Address = "" with get, set
         static member Create(name:string, value:obj)  = PlcTag(name, ToData value)
    /// PC tag (ActionTAG) class
    type PcTag<'T> private (name, data:IData) =
         inherit ActionTag<'T>(name, data)
         static member Create(name:string, value:obj)  = PcTag(name, ToData value)
    
    /// DsStatusTag tag (PlanTag) class
    type DsStatusTag<'T>  (name, m:IData, monitor:Monitor) =
         inherit PlanTag<'T>(name, m)
         member x.Monitor = monitor
         override x.Data   
            with get() = 
                match monitor with
                |Monitor.R|Monitor.G|Monitor.F| Monitor.H  
                        -> x.Memory.Status = monitor |> ToData
                |Origin -> x.Memory.Origin    |> ToData
                |Pause  -> x.Memory.Pause     |> ToData
                |ErrorTx  -> x.Memory.ErrorTx |> ToData
                |ErrorRx  -> x.Memory.ErrorRx |> ToData

            and  set(v:IData) = failwith "error"

    //name[Index] 규격 ex : R203[3]  
    type DsDotBit<'T> private (name, m:IData, index:int) =
         inherit PlanTag<'T>(name, m)
         static let getIndex(name) = 
            //대괄호 안에 내용의 index 가져오기
            let matches = Regex.Matches(name, "(?<=\[).*?(?=\])")
            matches.[matches.Count-1].Value |> Convert.ToInt32

         member x.Index = index

         static member Create(name:string, m:IData)  = 
            if (name.EndsWith("]") && name.Contains("[")) |> not
            then failwith $"{name} DsDotBit name type is name[Index]"

            DsDotBit(name, m, getIndex(name))

         member x.On()  = x.Data <- (true  |>ToData)
         member x.Off() = x.Data <- (false |>ToData)
         override x.Data   
            with get() = 
                match index with 
                | EndIndex -> x.Memory.End |> ToData
                | ResetIndex -> x.Memory.Reset |> ToData
                | StartIndex -> x.Memory.Start |> ToData
                | RelayIndex -> x.Memory.Relay |> ToData
                |_ -> failwith "error"
        
            and  set(v:IData) =
                let IsOn = Convert.ToBoolean(v|>ToValue|>box)
                match index with 
                | EndIndex -> if IsOn then x.Memory.EndOn() else x.Memory.EndOff()
                | ResetIndex -> if IsOn then x.Memory.ResetOn() else x.Memory.ResetOff()
                | StartIndex -> if IsOn then x.Memory.StartOn() else x.Memory.StartOff()
                | RelayIndex -> if IsOn then x.Memory.RelayOn() else x.Memory.RelayOff()
                |_ -> failwith "error"
        
    type DsTag = SegmentTag<IData>
    /// DsTag (PlanTAG) 행위 Memory 정보
    and SegmentTag<'T> private (name:string, data:IData) as this =
        inherit PlanTag<'T>(name, Memory(data |> ToValue))

        let startTag   = DsDotBit.Create($"{name}[{StartIndex}]"  ,this.Memory)
        let resetTag   = DsDotBit.Create($"{name}[{ResetIndex}]"  ,this.Memory)
        let endTag     = DsDotBit.Create($"{name}[{EndIndex}]"    ,this.Memory)
        let relayTag   = DsDotBit.Create($"{name}[{RelayIndex}]"  ,this.Memory)

        let readyTag   = DsStatusTag($"{name}(R)", this.Memory, Monitor.R)
        let goingTag   = DsStatusTag($"{name}(G)", this.Memory, Monitor.G)
        let finishTag  = DsStatusTag($"{name}(F)", this.Memory, Monitor.F)
        let homingTag  = DsStatusTag($"{name}(H)", this.Memory, Monitor.H)
        let originTag  = DsStatusTag($"{name}(4)", this.Memory, Monitor.Origin)
        let pauseTag   = DsStatusTag($"{name}(5)", this.Memory, Monitor.Pause)
        let errorTxTag = DsStatusTag($"{name}(6)", this.Memory, Monitor.ErrorTx)
        let errorRxTag = DsStatusTag($"{name}(7)", this.Memory, Monitor.ErrorRx)

        do 
            if (data|>box) :? Data<byte> |>not 
            then  failwith $"{name} DsTag data type is byte"
        
        ////초기값이 항상 byte (0)인 데이터 생성
        static member Create(name:string, data:IData) = SegmentTag(name, data)
        static member Create(name:string)  = SegmentTag(name, 0uy |> ToData)
       
        member x.Byte  = this.Memory.Value
        member x.Start = startTag
        member x.Reset = resetTag
        member x.End = endTag
        member x.Relay = relayTag
        
        member x.Ready  = readyTag
        member x.Going  = goingTag
        member x.Finish = finishTag
        member x.Homing = homingTag

        member x.Origin  =  originTag 
        member x.Pause   =  pauseTag  
        member x.ErrorTx =  errorTxTag
        member x.ErrorRx =  errorRxTag

    