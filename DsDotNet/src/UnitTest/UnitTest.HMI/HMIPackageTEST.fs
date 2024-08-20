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
    let testPath = @$"{__SOURCE_DIRECTORY__}../../../../bin/net7.0-windows/HelloDS.pptx";
    [<Fact>]
    let ``HMIPackage Create Test`` () =
        let pptParms:PptParams = defaultPptParams()

        clearNFullSlotHwSlotDataTypes()
        let dsPpt = ImportPpt.GetDSFromPptWithLib (testPath, false, pptParms)
        assignAutoAddress (dsPpt.System, 0 , 0)  (pptParms.TargetType, pptParms.DriverIO)

        RuntimeDS.Package <- RuntimePackage.PC
        let dsCPU, hmiPackage, _ = DsCpuExt.CreateRuntime(dsPpt.System) (pptParms.TargetType, pptParms.DriverIO)

        hmiPackage.Devices.Length > 0  |> Assert.True
