namespace UnitTest.PowerPointAddIn

open System
open Xunit
open System.Reflection
open System.IO

module HMIPackageTEST = 
    let directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
    let testPath = @$"{directoryPath}/HelloDS.pptx"
    
    [<Fact>]
    let ``HMIPackage Create Test`` () = true |> Assert.True
  