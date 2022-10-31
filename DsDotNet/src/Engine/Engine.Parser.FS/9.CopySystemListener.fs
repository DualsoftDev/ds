namespace Engine.Parser.FS
open Engine.Parser
open System
open System.Linq
open System.Collections.Generic
open System.Diagnostics
open System.Reactive.Linq
open Antlr4.Runtime
open Antlr4.Runtime.Tree
open Antlr4.Runtime.Misc
open Engine.Common
open Engine.Core
open Engine.Core.CoreModule
open Engine.Core.Interface
open Engine.Core.SpitModuleHelper

//open Antlr4.Runtime
open type Engine.Parser.dsParser
//open Engine.Parser.dsParser
//open Engine.Parser.DsParser
//open Engine.Parser.Global


type CopySystemListener(parser:dsParser, helper:ParserHelper) =
    inherit dsParserBaseListener()   //ListenerBase

    let mutable _parser = parser
    member val ParserHelper = helper with get, set

    member x.CopySystemListener(parser:dsParser, helper:ParserHelper) =
        x.ParserHelper <- helper
        parser.Reset()
        _parser <- parser


    //override x.EnterModel(ctx:ModelContext) =
    //    let sysCtxs = enumerateChildren<SystemContext>(ctx).ToArray()


    override x.EnterSystem(ctx:SystemContext) =
        let sysCopyCtx = findFirstChild<SysCopySpecContext>(ctx)
        if sysCopyCtx <> null then
            let srcSysName = findFirstChild<SourceSystemNameContext>(sysCopyCtx).GetText()
            let newSysName = findFirstChild<SystemNameContext>(ctx).GetText()

            let sysCtxs = enumerateChildren<SystemContext>(_parser.model()).ToArray()
            let srcSysCtx =
                enumerateChildren<SystemContext>(_parser.model())
                    .FirstOrDefault(sysctx => findFirstChild<SystemNameContext>(sysctx).GetText() == srcSysName)
                    

            let srcSys = ParserHelper.Model.Systems.First(sys => sys.Name == srcSysName)
            ()

