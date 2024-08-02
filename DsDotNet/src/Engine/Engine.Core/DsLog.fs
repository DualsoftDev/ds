namespace Engine.Core

open System

[<AutoOpen>]
module DsLogModule =
    
    type DsLog(time, stg:IStorage, token:Nullable<uint32>) =
        member x.Time: System.DateTime = time
        member x.Storage = stg
        member x.Token = token
        
        //여기 boxedValue는 참조라 History log로는 의미없음
        //member val Value: obj = stg.BoxedValue
