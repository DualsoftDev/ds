namespace T
open NUnit.Framework
open Dual.Common.Core.FS
open Engine.Custom


[<AutoOpen>]
module CustomEngineTestModule =
    type CustomEngineTest() =
        inherit EngineTestBaseClass()
        let dllPath = @$"{__SOURCE_DIRECTORY__}/../Engine.Custom.Sample/bin/Debug/net8.0/Engine.Custom.Sample.dll"

        [<Test>]
        member __.``Load Dlls`` () =
            let dic = Loader.LoadFromDll dllPath
            let bitDevices =
                dic
                |> filter(fun tpl -> tpl.Value :? IBitObject)
                |> map (fun (KeyValue(k, v)) -> (k, v :?> IBitObject))
                |> Tuple.toDictionary

            let cyl1Adv = bitDevices["cyl1Adv"]
            cyl1Adv.SetAsync("cyl1Adv").Wait()
            cyl1Adv.ResetAsync("cyl1Adv").Wait()


            let cyl1Ret = bitDevices["cyl1Ret"]
            cyl1Ret.SetAsync("cyl1Ret").Wait()
            cyl1Ret.ResetAsync("cyl1Ret").Wait()
