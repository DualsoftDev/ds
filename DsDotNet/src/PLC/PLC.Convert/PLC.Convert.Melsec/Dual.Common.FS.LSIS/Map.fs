namespace Dual.Common.FS.LSIS

open System.Linq
open System.Collections.Generic


module MapExt =
    /// Chained dictionary (다중 dictionary) 에서 항목 찾기
    let findAll maps key =
        maps |> Seq.map (Map.tryFind key) |> Seq.choose id

    let tryFindFirst maps key =
        findAll maps key |> Seq.tryHead

    #if INTERACTIVE
    let private doTest() =
        let boxSnd(k, v) = k, box(v)
        let dic1 = [ (3, 3); (1, 1); (2, 2); ]               |> Seq.map boxSnd |> Map.ofSeq
        let dic2 = [ (3, "Three"); (4, "Four"); (5, "Five")] |> Seq.map boxSnd |> Map.ofSeq

        findAll [dic1; dic2] 3
        findAll [dic1; dic2] 3
    #endif

    // 다중 중복 키 허용 dictionary 는 MultiValueDictionary 를 이용
    // Microsoft.Collections.Extensions.MultiValueDictionary