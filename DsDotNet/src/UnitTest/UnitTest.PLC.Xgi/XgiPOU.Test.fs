namespace T


open Xunit
open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Engine.Common.FS
open PLC.CodeGen.LSXGI


type XgiPOUTest() =
    inherit XgiTestBaseClass()

    do
        (* base class 초기화 이전에 let 구문들이 실행되어 오류 발생하는 것을 막기 위해 강제 do 구문 수행 *)
        PLC.CodeGen.LSXGI.ModuleInitializer.Initialize()
        setRuntimeTarget XGI |> ignore

    let globalStorages = Storages()
    let pou11 =
        let localStorages = Storages()
        let code = """
            bool xx0 = false;
            bool xx1 = false;
            $xx1 := $xx0;
"""
        let statements = parseCode localStorages code |> map withNoComment
        {
            TaskName = "Scan Program"
            POUName = "POU1"
            Comment = "POU1"
            LocalStorages = localStorages
            GlobalStorages = globalStorages
            CommentedStatements = statements
        }
    let pou12 =
        let storages = Storages()
        let code = """
            bool yy0 = false;
            bool yy1 = false;
            $yy1 := $yy0;
"""
        let statements = parseCode storages code |> map withNoComment
        {
            TaskName = "Scan Program"
            POUName = "POU2"
            Comment = "POU2"
            LocalStorages = storages
            GlobalStorages = globalStorages
            CommentedStatements = statements
        }

    let pou21 =
        let storages = Storages()
        let code = """
            bool zz0 = false;
            bool zz1 = false;
            $zz1 := $zz0;
"""
        let statements = parseCode storages code |> map withNoComment
        {
            TaskName = "ZZ Program"
            POUName = "POU1"
            Comment = "POU1"
            LocalStorages = storages
            GlobalStorages = globalStorages
            CommentedStatements = statements
        }

    let createProjectParams(projName):XgiProjectParams = {
        ProjectName = projName
        ProjectComment = ""
        GlobalStorages = Storages()
        ExistingLSISprj = None
        POUs = [pou11; pou12; pou21]
    }
    [<Test>]
    member __.``POU1 test`` () =
        let dummyPrjParams = createProjectParams "dummy"
        let xml = pou11.GenerateXmlString(dummyPrjParams)
        saveTestResult (get_current_function_name()) xml

    [<Test>]
    member __.``Project test`` () =
        let f = get_current_function_name()
        let projComment = "This is project comment."
        let xml = { createProjectParams(f) with ProjectComment=projComment}.GenerateXmlString()
        saveTestResult f xml


    [<Test>]
    member __.``Project with global test`` () =
        let globalStorages = Storages()
        let code = """
            bool gg0 = createTag("%IX0.0.1", false);
            bool gg1 = false;
"""
        let f = get_current_function_name()
        parseCode globalStorages code |> ignore
        let projectParams = { createProjectParams(f) with GlobalStorages = globalStorages }
        let xml = projectParams.GenerateXmlString()
        saveTestResult f xml


    [<Test>]
    member __.``Project with existing project test`` () =
        let myTemplate = $"{__SOURCE_DIRECTORY__}/XgiXmls/Templates/myTemplate.xml"


        let globalStorages = Storages()
        let code = """
            bool gg0 = createTag("%IX0.0.1", false);
            bool gg1 = false;
"""
        let f = get_current_function_name()
        parseCode globalStorages code |> ignore
        let projectParams = { createProjectParams(f) with GlobalStorages = globalStorages; ExistingLSISprj = Some myTemplate }
        let xml = projectParams.GenerateXmlString()
        saveTestResult f xml
