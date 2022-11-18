namespace Engine.Parser.FS

open System.IO
open Antlr4.Runtime.Tree

open Engine.Common.FS
open Engine.Parser
open Engine.Core
open type Engine.Parser.dsParser
open type Engine.Parser.FS.DsParser

module ModelParser =
    let mutable initialized = false
    let Walk(parser:dsParser, helper:ParserHelper) =
        let sListener = new SkeletonListener(parser, helper)
        ParseTreeWalker.Default.Walk(sListener, parser.system())
        tracefn("--- End of skeleton listener")

        //let eleListener = new ElementListener(parser, helper)
        //ParseTreeWalker.Default.Walk(eleListener, parser.system())
        //tracefn("--- End of element listener")


        let edgeListener = new EdgeListener(parser, helper)
        ParseTreeWalker.Default.Walk(edgeListener, parser.system())
        tracefn("--- End of edge listener")

        let etcListener = new EtcListener(parser, helper)
        ParseTreeWalker.Default.Walk(etcListener, parser.system())
        tracefn("--- End of etc listener")

    let ParseFromString2(text:string, options:ParserOptions):ParserHelper =
        assert(initialized)
        let (parser, errors) = DsParser.FromDocument(text)
        let helper = new ParserHelper(options)

        Walk(parser, helper)

        let system = helper.TheSystem.Value
        system.CreateMRIEdgesTransitiveClosure()
        system.Validate() |> ignore

        helper


    let ParseFromString(text:string, options:ParserOptions):DsSystem = ParseFromString2(text, options).TheSystem.Value

    //let ParsePartial(text:string, helper:ParserHelper, predExtract:RuleExtractor option) =
    //    //let predExtract = defaultArg predExtract (fun (parser:dsParser) -> parser.system() :> RuleContext)
    //    let (parser, ast, parserErrors) = FromDocument(text, predExtract)
    //    Walk(parser, helper)

    let Initialize() =
        tracefn "Initializing"
        initialized <- true
        let loadSystemFromDsFile (dsFilePath, loadedName) =
            let text = File.ReadAllText(dsFilePath)
            let dir = Path.GetDirectoryName(dsFilePath)
            let option = ParserOptions.Create4Runtime(dir, "ActiveCpuName")
            option.IsSubSystemParsing <- true
            option.LoadedSystemName <- Some loadedName
            let system = ParseFromString(text, option)
            system
        let loadDevice (constainerSystem:DsSystem) (absoluteFilePath, simpleFilePath) loadedName =
            let device =
                let system = loadSystemFromDsFile (absoluteFilePath, loadedName)
                system.Name <- loadedName
                Device(system, constainerSystem)
            device.AbsoluteFilePath <- absoluteFilePath
            device.UserSpecifiedFilePath <- simpleFilePath
            device

        let loadExternalSystem (constainerSystem:DsSystem) (absoluteFilePath, simpleFilePath) loadedName =
            let externalSystem =
                let system = loadSystemFromDsFile (absoluteFilePath, loadedName)
                ExternalSystem(loadedName, system, constainerSystem)
            externalSystem.AbsoluteFilePath <- absoluteFilePath
            externalSystem.UserSpecifiedFilePath <- simpleFilePath
            externalSystem

        fwdLoadDevice <- loadDevice
        fwdLoadExternalSystem <- loadExternalSystem
