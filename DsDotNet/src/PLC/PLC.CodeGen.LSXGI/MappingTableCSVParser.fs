namespace Dsu.PLCConverter.FS

open System
open System.IO
open System.Collections.Generic
open FSharp.Data
open Dual.Common

[<AutoOpen>]
module MappingTableParser =
    ///CommandMapping 정보를 테이블 형태로 저장
    type MappingEntry = {
        Part    : string
        Category: string
        Melsec  : string
        Xgi     : string
        EtcXgi  : string
        Etc     : string
    }

    type MappingDictionary = Dictionary<string, MappingEntry>
    type internal MappingTableCSV = CsvProvider<"../bin/Config/CommandMapping.csv">
    let mappingCSV =  __SOURCE_DIRECTORY__ + @"/../bin/Config/CommandMapping.csv"

    let internal readCSV (file:string) =
        let filePath = if(file = "") then mappingCSV else file
        let data = MappingTableCSV.Load(filePath)
        seq {
            for row in data.Rows do
                yield {
                    Part     = row.Part
                    Category = row.Category
                    Melsec   = row.Melsec
                    Xgi      = row.Xgi
                    EtcXgi   = row.EtcXgi
                    Etc      = row.Etc }
        }

    let createDictionary file =
        readCSV file
        |> Seq.onDuplicate (fun e -> e.Melsec) (fun e -> printfn "[%s] duplicated!" e.Melsec)
        |> Seq.distinctBy (fun e -> e.Melsec)
        |> Seq.map (fun e -> e.Melsec, e)
        |> dict
        |> Dictionary
