namespace Engine.Cpu

open System.Collections.Concurrent
open System.Diagnostics
open System
open Engine.Core
open System.Collections
open System.Text.RegularExpressions

[<AutoOpen>]
module CpuUtilModule =
        //core 병합시 삭제
    let internal verifyM (message:string) condition =
        if not condition then
            failwith message
    
    let internal getIndex (name:string)  =
        if (name.EndsWith("]") && name.Contains("[")) |> not
        then failwith $"{name} DsDotBit name type is name[Index]"
        else 
            let matches = Regex.Matches(name, "(?<=\[).*?(?=\])")
            matches.[matches.Count-1].Value |> Convert.ToInt32
