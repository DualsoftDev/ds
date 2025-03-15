namespace Dsu.PLCConverter.FS

open System
open System.IO
open System.Collections.Generic

[<AutoOpen>]
module XgiMappingTable =
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

    let mappingCSV = 
        let path = Directory.GetCurrentDirectory()+"/Config/CommandMapping.csv"
        if File.Exists(path) 
        then path 
        else  __SOURCE_DIRECTORY__ + @"/../Config/CommandMapping.csv" //유닛테스트용

    let internal readCSV (file:string) =
        let filePath = if(file = "") then mappingCSV else file
        let data = parseCsv(mappingCSV, 1)
        seq {
            for row in data.Rows do
                yield {
                    Part     = row.["Part"]
                    Category = row.["Category"]
                    Melsec   = row.["Melsec"]
                    Xgi      = row.["Xgi"]
                    EtcXgi   = row.["EtcXgi"]
                    Etc      = row.["Etc"] }
        }

    let createDictionary file =
        readCSV file
        |> Seq.onDuplicate (fun e -> e.Melsec) (fun e -> printfn "[%s] duplicated!" e.Melsec)
        |> Seq.distinctBy (fun e -> e.Melsec)
        |> Seq.map (fun e -> e.Melsec, e)
        |> dict
        |> Dictionary
