namespace PLC.Mapper.FS

open System
open System.Drawing
open System.Collections.Concurrent
open System.Text.RegularExpressions
open ColorUtilModule
open Engine.Core.MapperDataModule
open PrefixTrie
open System.Collections.Generic

[<AutoOpen>]
module MappingUtils =


    // ========== 유틸리티 함수 ==========

    let groupByPrefixLength (tags: MapperTag[]) (prefixLen: int) : (string * MapperTag[])[] =
        tags
        |> Array.groupBy (fun t ->
            if t.Name.Length >= prefixLen
            then t.Name.Substring(0, prefixLen)
            else t.Name
            )

    let defaultValueIfEmpty (txt: string) (fallback: string) =
        if String.IsNullOrWhiteSpace(txt) then fallback else txt

    let findCommonPrefix (items: string list) : string =
        let delimiters = [| ' '; '_'; '-' |]

        match items with
        | [] -> ""
        | first :: rest ->
            // 모든 문자열에서 공통 접두어 문자 단위로 탐색
            let prefix =
                rest
                |> List.fold (fun (acc:string) s ->
                    let minLen = min acc.Length s.Length
                    let mutable i = 0
                    while i < minLen && acc.[i] = s.[i] do
                        i <- i + 1
                    acc.Substring(0, i)
                ) first

            // 공통 접두어에서 가장 마지막 구분자 이전까지만 유지
            let cutPoint = prefix.LastIndexOfAny(delimiters)
            if cutPoint > 0 then prefix.Substring(0, cutPoint)
            else prefix
