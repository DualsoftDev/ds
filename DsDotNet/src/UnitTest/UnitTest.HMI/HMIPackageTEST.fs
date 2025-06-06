namespace UnitTest.HMI

open System
open Xunit
open System.Reflection
open System.IO
open Engine.Import.Office
open Engine.CodeGenHMI
open Engine.Cpu
open Engine.Core
open Engine.CodeGenCPU
open Engine.Core.MapperDataModule

module HMIPackageTEST =
    let testPath = @$"{__SOURCE_DIRECTORY__}../../../../bin/net8.0-windows/HelloDS.pptx";
    [<Fact>]
    let ``HMIPackage Create Test`` () =
        let pptParms:PptParams = defaultPptParams()

        let dsPpt = ImportPpt.GetDSFromPptWithLib (testPath, false, pptParms)
        let dsCPU, hmiPackage, _ = DsCpuExt.CreateRuntime(dsPpt.System) dsPpt.ModelConfig  

        hmiPackage.Devices.Length > 0  |> Assert.True
