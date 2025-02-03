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

module HMIPackageTEST =
    let testPath = @$"{__SOURCE_DIRECTORY__}../../../../bin/net8.0-windows/HelloDS.pptx";
    [<Fact>]
    let ``HMIPackage Create Test`` () =
        let pptParms:PptParams = defaultPptParams()
        let modelConfig = createDefaultModelConfig();

        let dsPpt = ImportPpt.GetDSFromPptWithLib (testPath, false, pptParms, modelConfig)
        assignAutoAddress (dsPpt.System, 0 , 0, pptParms.HwTarget)

        RuntimeDS.ChangeRuntimePackage(RuntimePackage.PC)
        let dsCPU, hmiPackage, _ = DsCpuExt.CreateRuntime(dsPpt.System) (pptParms.HwTarget.Platform) modelConfig

        hmiPackage.Devices.Length > 0  |> Assert.True
