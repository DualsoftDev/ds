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
        let pptParms:PPTParams = {TargetType = WINDOWS; AutoIOM = true}
        
        clearNFullSlotHwSlotDataTypes()
        let dsPPT = ImportPPT.GetDSFromPPTWithLib (testPath, false, pptParms)
        assignAutoAddress (dsPPT.System, 0 , 0)  WINDOWS

        RuntimeDS.Package <- RuntimePackage.PC
        let dsCPU, hmiPackage, _ = DsCpuExt.GetDsCPU(dsPPT.System) pptParms.TargetType;

        hmiPackage.Devices.Length > 0  |> Assert.True
  