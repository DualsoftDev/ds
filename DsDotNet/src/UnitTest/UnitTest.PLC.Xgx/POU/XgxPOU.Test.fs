namespace T.POU
open T

open Dual.UnitTest.Common.FS

open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.LS
open PLC.CodeGen.Common

type XgxPOUTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)

    do
        (* base class 초기화 이전에 let 구문들이 실행되어 오류 발생하는 것을 막기 위해 강제 do 구문 수행 *)
        PLC.CodeGen.LS.ModuleInitializer.Initialize()
        setRuntimeTarget XGI |> ignore

    let globalStorages = Storages()
    let pou11 = lazy (
        let localStorages = Storages()
        let code = """
            bool xx0 = false;
            bool xx1 = false;
            $xx1 = $xx0;
"""
        let statements = parseCodeForWindows localStorages code |> map withNoComment
        {
            TaskName = "Scan Program"
            POUName = "POU1"
            Comment = "POU1"
            LocalStorages = localStorages
            GlobalStorages = globalStorages
            CommentedStatements = statements
        }
    )
    let pou12 = lazy (
        let storages = Storages()
        let code = """
            bool yy0 = false;
            bool yy1 = false;
            $yy1 = $yy0;
"""
        let statements = parseCodeForWindows storages code|> map withNoComment
        {
            TaskName = "Scan Program"
            POUName = "POU2"
            Comment = "POU2"
            LocalStorages = storages
            GlobalStorages = globalStorages
            CommentedStatements = statements
        }
    )

    let pou21 = lazy (
        let storages = Storages()
        let code = """
            bool zz0 = false;
            bool zz1 = false;
            $zz1 = $zz0;
"""
        let statements = parseCodeForWindows storages code|> map withNoComment
        {
            TaskName = "ZZ Program"
            POUName = "POU1"
            Comment = "POU1"
            LocalStorages = storages
            GlobalStorages = globalStorages
            CommentedStatements = statements
        }
    )

    let createProjectParams(projName):XgxProjectParams = {
        getXgxProjectParams xgx projName with
            EnableXmlComment = true
            POUs = [pou11.Value; pou12.Value; pou21.Value]
            MemoryAllocatorSpec = AllocatorFunctions(createMemoryAllocator "M" (0, 640 * 1024) [] xgx) // 유닛테스트 연속호출시 누적되므로 새로 호출
    }

    //pou만으로는 xg5000에서 열수 없음
    member x.``POU1 test`` () =
        let dummyPrjParams = createProjectParams "dummy"
        let xml = pou11.Value.GenerateXmlString(dummyPrjParams, None)
        x.saveTestResult (getFuncName()) xml

    member x.``Project test`` () =
        let f = getFuncName()
        let projComment = "This is project comment."
        let xml = { createProjectParams(f) with ProjectComment=projComment}.GenerateXmlString()
        x.saveTestResult f xml


    member x.``Project with global test`` () =
        let globalStorages = Storages()
        let code = """
            bool gg0 = createTag("%IX0.0.1", false);
            bool gg1 = false;
"""
        let f = getFuncName()
        parseCodeForWindows globalStorages code |> ignore
        let projectParams = { createProjectParams(f) with GlobalStorages = globalStorages }
        let xml = projectParams.GenerateXmlString()
        x.saveTestResult f xml


    member x.``Project with existing project test`` () =
        let myTemplate = $"{__SOURCE_DIRECTORY__}/../Xgi/Xmls/Templates/myTemplate.xml"


        let globalStorages = Storages()
        let code = """
            bool gg0 = createTag("%IX0.0.1", false);
            bool gg1 = false;
"""
        let f = getFuncName()
        parseCodeForWindows globalStorages code |> ignore
        let projectParams = { createProjectParams(f) with GlobalStorages = globalStorages; ExistingLSISprj = Some myTemplate }
        let xml = projectParams.GenerateXmlString()
        x.saveTestResult f xml


    member x.``Validation= Existing project global variable memory test`` () =
        (* existing project 에 이미 global 변수들이 선언되어 있고,
         * 새로 선언되는 자동 할당 변수들이 미리 선언된 메모리 영역을 피해서 생성되는지 검사한다.
         *)

        let myTemplate = $"{__SOURCE_DIRECTORY__}/../../../PLC/PLC.CodeGen.LS/Documents/XmlSamples/multiProgramSample.xml"
        let xdoc = DualXmlDocument.loadFromFile myTemplate
        let usedMemoryByteIndices = collectUsedMermoryByteIndicesInGlobalSymbols xdoc xgx
        usedMemoryByteIndices |> SeqEq [ 0; 1; 2; 4; 8; 9; 10; 11; 12; 13; 14; 15; 17; ]


        let globalStorages = Storages()
        let code = """
            bool gg0 = createTag("%IX1.0.1", false);
            bool gg1 = false;
            bool xm0 = false;
            bool xm1 = false;
            bool xm2 = false;
            bool xm3 = false;

            int8 y0 = 0y;
            int8 y1 = 0y;
            int8 y2 = 0y;
            int8 y3 = 0y;

            int nm0 = 0;
            int nm1 = 0;
            int nm2 = 0;
            int nm3 = 0;
"""
        let f = getFuncName()
        parseCodeForWindows globalStorages code |> ignore
        for n in ["xm0"; "xm1"; "xm2"; "xm3"; "y0"; "y1"; "y2"; "y3"; "nm0"; "nm1"; "nm2"; "nm3"] do
            globalStorages[n].Address <- TextAddrEmpty       // force to allocate Memory

        let projectParams = {
            createProjectParams(f) with
                GlobalStorages = globalStorages
                ExistingLSISprj = Some myTemplate
                MemoryAllocatorSpec = AllocatorFunctions (createMemoryAllocator "M" (0, 640*1024) usedMemoryByteIndices xgx)    // 640K M memory 영역
        }
        let xml = projectParams.GenerateXmlString()


        //주소 없는 auto 변수 사용
        globalStorages["gg1"].Address === ""
        globalStorages["xm0"].Address === "%MX320"

        x.saveTestResult f xml


    member x.``Validation= Existing project global variable name collide exactly test`` () =
        (* existing project 에 이미 global 변수들이 선언되어 있고,
         * 새로 선언되는 자동 할당 변수 이름이 미리 선언된 gloal 변수와 동일할 때 fail 해야 한다..
         *)

        let myTemplate = $"{__SOURCE_DIRECTORY__}/../../../PLC/PLC.CodeGen.LS/Documents/XmlSamples/multiProgramSample.xml"
        let xdoc = DualXmlDocument.loadFromFile myTemplate
        let usedMemoryByteIndices = collectUsedMermoryByteIndicesInGlobalSymbols xdoc xgx
        let existingGlobals = collectGlobalSymbols xdoc |> map name

        existingGlobals |> List.contains "MMX0" === true

        let globalStorages = Storages()
        let code = """
            bool MMX0 = false;
"""
        let f = getFuncName()
        parseCodeForWindows globalStorages code |> ignore
        globalStorages["MMX0"].Address <- ""       // force to allocate Memory

        let projectParams = {
            createProjectParams(f) with
                GlobalStorages = globalStorages
                ExistingLSISprj = Some myTemplate
                MemoryAllocatorSpec = AllocatorFunctions (createMemoryAllocator "M" (0, 640*1024) usedMemoryByteIndices xgx)    // 640K M memory 영역
        }
        ( fun () ->
            let xml = projectParams.GenerateXmlString()
            x.saveTestResult f xml
        ) |> ShouldFailWithSubstringT "ERROR: Duplicated global variable name : MMX0"

    member x.``Validation= Existing project global variable name collide ignoring case test`` () =
        (* existing project 에 이미 global 변수들이 선언되어 있고,
         * 새로 선언되는 자동 할당 변수 이름이 대소문자를 가리지 않고 미리 선언된 gloal 변수와 동일할 때 fail 해야 한다..
         *)

        let myTemplate = $"{__SOURCE_DIRECTORY__}/../../../PLC/PLC.CodeGen.LS/Documents/XmlSamples/multiProgramSample.xml"
        let xdoc = DualXmlDocument.loadFromFile myTemplate
        let usedMemoryByteIndices = collectUsedMermoryByteIndicesInGlobalSymbols xdoc xgx
        let existingGlobals = collectGlobalSymbols xdoc |> map name

        existingGlobals |> List.contains "MMX1" === true

        let globalStorages = Storages()
        let code = """
            bool mmx1 = false;      // MMX1 과 대소문자를 구분하지 않아야 한다.
"""
        let f = getFuncName()
        parseCodeForWindows globalStorages code |> ignore
        globalStorages["MMX1"].Address <- ""       // force to allocate Memory

        let projectParams = {
            createProjectParams(f) with
                GlobalStorages = globalStorages
                ExistingLSISprj = Some myTemplate
                MemoryAllocatorSpec = AllocatorFunctions (createMemoryAllocator "M" (0, 640*1024) usedMemoryByteIndices xgx)    // 640K M memory 영역
        }
        ( fun () ->
            let xml = projectParams.GenerateXmlString()
            x.saveTestResult f xml
        ) |> ShouldFailWithSubstringT "ERROR: Duplicated global variable name : MMX1"

    member x.``Validation= Existing project duplicated POU name test`` () =
        let myTemplate = $"{__SOURCE_DIRECTORY__}/../../../PLC/PLC.CodeGen.LS/Documents/XmlSamples/multiProgramSample.xml"
        let collidingPou = {
            pou11.Value with
                TaskName = "스캔 프로그램"
                POUName = "DsLogic"
        }
        let projectParams = {
            createProjectParams("POU 이름 충돌 test 중..") with
                GlobalStorages = globalStorages
                ExistingLSISprj = Some myTemplate
                POUs = [collidingPou]
        }
        ( fun () ->
            let _xml = projectParams.GenerateXmlString()
            ()
        ) |> ShouldFailWithSubstringT "ERROR: Duplicated POU name"




type XgiPOUTest() =
    inherit XgxPOUTest(XGI)

    [<Test>] member __.``POU1 test`` () = base.``POU1 test``()
    [<Test>] member __.``Project test`` () = base.``Project test``()
    [<Test>] member __.``Project with global test`` () = base.``Project with global test``()
    [<Test>] member __.``Project with existing project test`` () = base.``Project with existing project test``()
    [<Test>] member __.``Validation= Existing project global variable memory test`` () = base.``Validation= Existing project global variable memory test``()
    [<Test>] member __.``Validation= Existing project global variable name collide exactly test`` () = base.``Validation= Existing project global variable name collide exactly test``()
    [<Test>] member __.``Validation= Existing project global variable name collide ignoring case test`` () = base.``Validation= Existing project global variable name collide ignoring case test``()
    [<Test>] member __.``Validation= Existing project duplicated POU name test`` () = base.``Validation= Existing project duplicated POU name test``()
