namespace UnitTest.HMI

open System
open Xunit
open System.Reflection
open System.IO
open Engine.Import.Office
open Engine.CodeGenHMI
open Engine.Cpu
open Engine.Core

module HMIPackageTEST = 
    let testPath = @$"{__SOURCE_DIRECTORY__}../../../../bin/net7.0-windows/HelloDS.pptx";
    [<Fact>]
    let ``HMIPackage Create Test`` () = 
        
        let dsPPT = ImportPPT.GetDSFromPPTWithLib (testPath, false)
        RuntimeDS.Package <- RuntimePackage.PC
        let dsCPU, hmiPackage, _ = DsCpuExt.GetDsCPU(dsPPT.System);

        hmiPackage.Devices.Length > 0  |> Assert.True
  