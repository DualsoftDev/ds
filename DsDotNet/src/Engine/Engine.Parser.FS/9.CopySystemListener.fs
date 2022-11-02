namespace Engine.Parser.FS

open System.Linq

open Engine.Parser
open type Engine.Parser.FS.DsParser
open type Engine.Parser.dsParser


//type CopySystemListener(parser:dsParser, helper:ParserHelper) =
//    inherit dsParserBaseListener()   //ListenerBase

//    let mutable _parser = parser
//    member val ParserHelper = helper with get, set

//    member x.CopySystemListener(parser:dsParser, helper:ParserHelper) =
//        x.ParserHelper <- helper
//        parser.Reset()
//        _parser <- parser

//    override x.EnterSystem(ctx:SystemContext) =
//        let sysCopyCtx = findFirstChild<SysCopySpecContext>(ctx)
//        match sysCopyCtx with
//        | Some sysCopyCtx ->
//            let srcSysName = findFirstChild<SourceSystemNameContext>(sysCopyCtx).Value.GetText()
//            let newSysName = findFirstChild<SystemNameContext>(ctx).Value.GetText()

//            let sysCtxs = enumerateChildren<SystemContext>(_parser.model()).ToArray()
//            let srcSysCtx =
//                enumerateChildren<SystemContext>(_parser.model())
//                    .FirstOrDefault(fun sysctx -> findFirstChild<SystemNameContext>(sysctx).Value.GetText() = srcSysName)


//            let srcSys = x.ParserHelper.Model.Systems.First(fun sys -> sys.Name = srcSysName)
//            ()
//        | None -> ()

