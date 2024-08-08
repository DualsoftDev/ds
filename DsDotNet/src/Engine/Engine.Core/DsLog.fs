namespace Engine.Core

open System

type TokenIdType = Nullable<int64>

[<AutoOpen>]
module DsLogModule =

    type DsLog(time, stg:IStorage, tokenId:TokenIdType) =
        member x.Time: System.DateTime = time
        member x.Storage = stg
        member x.TokenId = tokenId

        //여기 boxedValue는 참조라 History log로는 의미없음
        //member val Value: obj = stg.BoxedValue
