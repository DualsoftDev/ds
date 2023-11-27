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
    let directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
    let testPath = @$"{directoryPath}/HelloDS.pptx"
    
    [<Fact>]
    let ``HMIPackage Create Test`` () = 
        
        let dsPPT = ImportPPT.GetDSFromPPTWithLib testPath    
        let dsCPU = DsCpuExt.GetDsCPU(dsPPT.System, RuntimePackage.StandardPC);
        
        let hmiPackage = dsCPU.GetHMIPackage()

        hmiPackage.Devices.Length > 0  |> Assert.True
  