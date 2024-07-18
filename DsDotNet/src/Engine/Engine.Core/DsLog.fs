namespace Engine.Core

[<AutoOpen>]
module DsLogModule =
    
    type DsLog(time, stg:IStorage) =
        member x.Time: System.DateTime = time
        member x.Storage = stg
        
        //여기 boxedValue는 참조라 History log로는 의미없음
        //member val Value: obj = stg.BoxedValue
