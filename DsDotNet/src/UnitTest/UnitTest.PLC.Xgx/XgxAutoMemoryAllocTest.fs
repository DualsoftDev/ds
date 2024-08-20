namespace T

open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.LS
open PLC.CodeGen.Common


type AutoMemoryAllocTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)

    member x.``Auto memory allocation test`` () =
        let globalStorages = Storages()
        let pouIQMap =

            let code =
                let data =
                    [   for i in [ 0..128] -> $"bool ax{i} = false;"
                        for i in [ 0..10] -> $"uint16 aw{i} = 0us;"
                        for i in [ 0..10] -> $"uint32 ad{i} = 0u;"
                    ] @ match xgx with
                        | XGI ->
                            [
                                for i in [ 0..10] -> $"byte ab{i} = 0uy;"
                                for i in [ 0..10] -> $"uint64 al{i} = 0UL;"
                            ]
                        | XGK ->
                            []
                        | _ ->
                            failwithf $"not support {xgx}"

                data |> String.concat "\n"
            let statements = parseCodeForTarget globalStorages code  xgx |> map withNoComment
            for t in globalStorages.Values do
                t.Address <- TextAddrEmpty

            {
                POUName = "POU1"
                Comment = "POU1"
                LocalStorages = Storages()
                GlobalStorages = globalStorages
                CommentedStatements = statements
            }

        let usedByteIndices = [0..9] @ [25..31] @[39;42] @ [120..130]
        let prjParam = {
            getXgxProjectParams xgx (getFuncName()) with
                GlobalStorages = globalStorages
                EnableXmlComment = true
                MemoryAllocatorSpec = AllocatorFunctions (createMemoryAllocator "M" (0, 640*1024) usedByteIndices xgx)    // 640K M memory 영역
                POUs = [pouIQMap]
        }

        let xml = prjParam.GenerateXmlString()
        let f = getFuncName()
        x.saveTestResult f xml


type XgiAutoMemoryAllocTest() =
    inherit AutoMemoryAllocTest(XGI)
    [<Test>] member __.``Auto memory allocation test`` () = base.``Auto memory allocation test``()

type XgkAutoMemoryAllocTest() =
    inherit AutoMemoryAllocTest(XGK)
    [<Test>] member __.``Auto memory allocation test`` () = base.``Auto memory allocation test``()

