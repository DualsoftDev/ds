namespace T

open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.LS
open PLC.CodeGen.Common


type AutoMemoryAllocTest(xgx:RuntimeTargetType) =
    inherit XgxTestBaseClass(xgx)

    member x.``Auto memory allocation test`` () =
        let globalStorages = Storages()
        let pouIQMap =
            let code =
                [ for i in [ 0..128] -> $"bool ax{i} = false;"
                  for i in [ 0..10] -> $"byte ab{i} = 0uy;"
                  for i in [ 0..10] -> $"uint16 aw{i} = 0us;"
                  for i in [ 0..10] -> $"uint32 ad{i} = 0u;"
                  for i in [ 0..10] -> $"uint64 al{i} = 0UL;"
                ] |> String.concat "\n"

            let statements = parseCode globalStorages code |> map withNoComment
            for t in globalStorages.Values do
                t.Address <- TextAddrEmpty

            {
                TaskName = "Scan Program"
                POUName = "POU1"
                Comment = "POU1"
                LocalStorages = Storages()
                GlobalStorages = globalStorages
                CommentedStatements = statements
            }

        let usedByteIndices = [0..9] @ [25..31] @[39;42] @ [120..130]
        let prjParam = {
            defaultXgxProjectParams with
                TargetType = TestRuntimeTargetType
                ProjectName = "Dummy IQ Map test"
                GlobalStorages = globalStorages
                MemoryAllocatorSpec = AllocatorFunctions (createMemoryAllocator "M" (0, 640*1024) usedByteIndices)    // 640K M memory 영역
                POUs = [pouIQMap]
                RungCounter = counterGenerator 0 |> Some
        }

        let xml = prjParam.GenerateXmlString()
        let f = getFuncName()
        x.saveTestResult f xml


type XgiAutoMemoryAllocTest() =
    inherit AutoMemoryAllocTest(XGI)
    [<Test>]
    member x.``Auto memory allocation test`` () = base.``Auto memory allocation test``()
