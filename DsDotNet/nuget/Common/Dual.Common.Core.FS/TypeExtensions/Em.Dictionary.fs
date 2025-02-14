namespace Dual.Common.Core.FS

open System.Runtime.CompilerServices
open System.Collections.Generic

type DictionaryExt =

    /// collection 과 keySelector, valueSelector 로부터 dictionary 생성
    [<Extension>]
    static member ToDictionary<'x, 'k, 'v when 'k : equality>(
        xs: 'x seq,
        keySelector: 'x -> 'k,
        valueSelector: 'x -> 'v
    ) =
        xs
        |> map (fun x -> keySelector x, valueSelector x)
        |> Tuple.toDictionary

    /// (Key, Value) tuple 로부터 dictionary 생성
    [<Extension>] static member ToDictionary<'k, 'v when 'k : equality>(kvs:('k*'v) seq) = Tuple.toDictionary kvs
    /// KeyValuePair 로부터 dictionary 생성
    [<Extension>] static member ToDictionary<'k, 'v when 'k : equality>(kvs:KeyValuePair<'k, 'v> seq) = kvs |> map (fun kv ->  kv.Key, kv.Value) |> Tuple.toDictionary

