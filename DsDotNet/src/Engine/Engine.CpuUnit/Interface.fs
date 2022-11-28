namespace Engine.Cpu

open System.Collections.Concurrent

[<AutoOpen>]
module Interface =

    //22.11.16 일 기준 지원 Data type = { bool, int, single, double, string }
    type IData      = interface end

    type ITag    = 
        abstract Name   : string
        abstract Data:IData // with get, set
        //abstract GetData  : unit -> IData
        //abstract SetData  : IData  -> unit
        abstract ToText   : unit -> string  

    type IExpression = 
        abstract Evaluate : unit -> IData  
        abstract ToText   : unit -> string  
