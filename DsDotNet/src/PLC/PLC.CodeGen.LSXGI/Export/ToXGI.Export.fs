namespace PLC.CodeGen.LSXGI

open Engine.Common.FS
open PLC.CodeGen.Common.QGraph
open System.Collections.Generic
open Engine.Core

module LsXGI =

    //<kwak>
    //let generateXGIXmlFromLadderInfo (opt:CodeGenerationOption) (ladderInfo:LadderInfo) (tags) (unusedTags) (existingLSISprj:string option) =
    //    let existTagdict =
    //        existingLSISprj
    //        |> map (DsXml.load >> XGIXml.createUsedVariableMap)
    //        |> Option.defaultValue (new Dictionary<string, string>())

    //    let statements =
    //        ladderInfo.Rungs
    //        |> RungGenerator.replaceDuplicateTags tags existTagdict
    //        |> Seq.groupBy(fun ri -> ri.GetCoilTerminal())
    //        |> Seq.map (rungInfoToStatement opt)

    //    let plctags = statementToTag statements |> Seq.append tags |> Seq.distinct

    //    File.generateXGIXmlFromStatement ladderInfo.PrologComments statements plctags unusedTags existingLSISprj

    //let generateXGIXmlFromLadderInfoAndStatus (opt:CodeGenerationOption) (ladderInfo:LadderInfo) status (tags) (unusedTags) (existingLSISprj:string option) =
    //    let existTagdict =
    //        existingLSISprj
    //        |> map (DsXml.load >> XGIXml.createUsedVariableMap)
    //        |> Option.defaultValue (new Dictionary<string, string>())

    //    let statements = ladderInfo.Rungs @ status |> RungGenerator.replaceDuplicateTags tags existTagdict |> Seq.groupBy(fun ri -> ri.GetCoilTerminal()) |> Seq.map (rungInfoToStatement opt)

    //    let plctags = statementToTag statements |> Seq.append tags |> Seq.distinct
    //    File.generateXGIXmlFromStatement ladderInfo.PrologComments statements plctags unusedTags existingLSISprj

    let generateXml (opt:CodeGenerationOption) (storages:Storages) (commentedStatements:CommentedStatement list) : string =
        let prologComments = [""]
        let tags = storages.Values |> Seq.ofType<ITagWithAddress>
        let unusedTags:ITagWithAddress list = []
        let existingLSISprj = None

        let xml = generateXGIXmlFromStatement prologComments commentedStatements tags unusedTags existingLSISprj
        xml
