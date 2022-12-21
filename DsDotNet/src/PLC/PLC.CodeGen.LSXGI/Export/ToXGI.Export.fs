namespace Dual.ConvertPLC.FS.LsXGI

open Engine.Common.FS
open Dual.Core.QGraph
open Dual.Core
open FSharpPlus
open System.Collections.Generic

module LsXGI =

    let generateXGIXmlFromLadderInfo (opt:CodeGenerationOption) (ladderInfo:LadderInfo) (tags) (unusedTags) (existingLSISprj:string option) = 
        let existTagdict = existingLSISprj |> map (DsXml.load >> XGIXml.createUsedVariableMap) |> Option.defaultValue (new Dictionary<string, string>())

        let statements = ladderInfo.Rungs |> RungGenerator.replaceDuplicateTags tags existTagdict |> Seq.groupBy(fun ri -> ri.GetCoilTerminal()) |> Seq.map (rungInfoToStatement opt)
        let plctags = statementToTag statements |> Seq.append tags |> Seq.distinct

        File.generateXGIXmlFromStatement ladderInfo.PrologComments statements plctags unusedTags existingLSISprj

    let generateXGIXmlFromLadderInfoAndStatus (opt:CodeGenerationOption) (ladderInfo:LadderInfo) status (tags) (unusedTags) (existingLSISprj:string option) = 
        let existTagdict = existingLSISprj |> map (DsXml.load >> XGIXml.createUsedVariableMap) |> Option.defaultValue (new Dictionary<string, string>())
        
        let statements = ladderInfo.Rungs @@ status |> RungGenerator.replaceDuplicateTags tags existTagdict |> Seq.groupBy(fun ri -> ri.GetCoilTerminal()) |> Seq.map (rungInfoToStatement opt)
        
        let plctags = statementToTag statements |> Seq.append tags |> Seq.distinct
        File.generateXGIXmlFromStatement ladderInfo.PrologComments statements plctags unusedTags existingLSISprj
