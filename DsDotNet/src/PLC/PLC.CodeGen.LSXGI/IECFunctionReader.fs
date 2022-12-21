namespace Dsu.PLCConverter.FS

open System
open System.IO
open System.Collections.Generic
open Dual.Common
open Dual.Common.ActivePattern

[<AutoOpen>]
/// IEC FunctionReader 규격을 읽음
module IECFunctionReader =
    /// IEC FunctionReader 규격을 읽음
    let pathFBtxt =  __SOURCE_DIRECTORY__ + @"/../bin/Config/XG5000_IEC_FUNC.txt"

    let createReader =
        let filePath = if(XgiBaseXML.XgiOpt.PathFBList = "") then pathFBtxt else XgiBaseXML.XgiOpt.PathFBList
        let dic =
            File.ReadAllLines(filePath)
            |> Seq.filter (fun s -> not <| s.StartsWith("#"))
            |> Seq.map (fun s -> s.Trim())
            |> Seq.splitOn (fun s1 s2 -> s1.isEmpty() && not <| s2.isEmpty())
            |> Seq.map (Seq.filter (String.IsNullOrEmpty >> not) >> Array.ofSeq)
            |> Seq.map (fun fb ->
                let fName =
                    let fnameLine = fb |> Array.find (fun l -> l.StartsWith("FNAME:"))
                    match fnameLine with
                    | RegexPattern @"FNAME: (\w+)$" [fn] ->
                        fn
                    | _ -> failwith "ERROR"
                fName, fb )
            |> dict |> Dictionary
        dic

    /// getFBXML FB 이름 기준으로 XML 저장 파라메터를 읽음
    let getFBXML (fnameSource, fnameTarget, instance) =
        let fbXml = 
            createReader.[fnameSource] 
            |> Array.map (function
                | StartsWith "FNAME: " -> sprintf "FNAME: %s" fnameTarget
                | StartsWith "INSTANCE: " -> sprintf "INSTANCE: %s" instance
                | str -> str)
            |> String.concat "&#xA;" 
        fbXml + " &#xA;"

    /// getFBXML FB 이름 기준으로 Input para 갯수를 읽어옴
    let getFBInCount (fnameSource:string) =
        let lstIn = 
            createReader.[fnameSource]
            |> Array.filter (function f -> not (f.StartsWith("VAR_IN: EN")))
            |> Array.filter (function f -> f.StartsWith("VAR_IN: ") || f.StartsWith("VAR_IN_OUT: "))
            |> Array.toSeq
        let onlyIn = 
            lstIn 
            |> Seq.filter (function f -> f.StartsWith("VAR_IN: ")) 

        onlyIn.length(), lstIn.length()
            



