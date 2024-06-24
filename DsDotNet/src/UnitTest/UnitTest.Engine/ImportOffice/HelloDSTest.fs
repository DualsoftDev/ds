namespace T.PPT
open Dual.UnitTest.Common.FS

open T

open Engine.Core
open Dual.Common.Core.FS
open NUnit.Framework
open Engine.Parser.FS
open Engine.Import.Office
open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS



[<AutoOpen>]
module HelloDSTestModule =

    type HelloDSTest() =
        inherit EngineTestBaseClass()
        do
            RuntimeDS.Package <- PCSIM

        let helloDSPath = @$"{__SOURCE_DIRECTORY__}/../../../../Apps/OfficeAddIn/PowerPointAddInHelper/Utils/HelloDS.pptx"
        let getSystem() =
            let result = ImportPPT.GetDSFromPPTWithLib (helloDSPath, false, pptParms)
            let { 
                System = system
                ActivePath =  exportPath 
                LoadingPaths = loadingPaths 
                LayoutImgPaths = layoutImgPaths 
            } = result
            system


        [<Test>]
        member __.``HelloDS vertices read test``() =
            let system = getSystem()

            let flowNames = system.Flows |> map (fun f -> f.Name) |> toArray
            SeqEq flowNames ["STN1"]          // 숨김 페이지: "STN2"; "STN3"; "Flow1"; "KIT"
            let stn1 = system.Flows |> Seq.exactlyOne
            let reals = stn1.Graph.Vertices.OfType<Real>() |> toArray
            let realNames = reals |> map (fun r -> r.Name) |> toArray
            SeqEq realNames [|"Work1"; "Work2"|]

            let callsInReal1 = reals[0].Graph.Vertices.OfType<Call>() |> toArray
            let callInReal1Names = callsInReal1 |> map (fun c -> c.Name) |> toArray
            SeqEq callInReal1Names [|
                "STN1__Device1_ADV"
                "STN1__Device2_ADV"
                "STN1__Device3_ADV"
                "STN1__Device4_ADV"
                "STN1__Device1_RET"
                "STN1__Device2_RET"
                "STN1__Device3_RET"
                "STN1__Device4_RET"
                |]

            let callsInReal2 = reals[1].Graph.Vertices.OfType<Call>() |> toArray
            let callInReal2Names = callsInReal2 |> map (fun c -> c.Name) |> toArray
            SeqEq callInReal2Names [||]

            ()

