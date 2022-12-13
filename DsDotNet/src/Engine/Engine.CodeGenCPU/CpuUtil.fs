namespace Engine.CodeGenCPU

open System.Collections.Concurrent
open System.Diagnostics
open System
open Engine.Core
open System.Collections
open System.Text.RegularExpressions

[<AutoOpen>]
module CpuUtil =
  
    let internal getIndex (name:string)  =
        if (name.EndsWith("]") && name.Contains("[")) |> not
        then failwith $"{name} DsDotBit name type is name[Index]"
        else 
            let matches = Regex.Matches(name, "(?<=\[).*?(?=\])")
            matches.[matches.Count-1].Value |> Convert.ToInt32
