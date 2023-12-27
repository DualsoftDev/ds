namespace UnitTest.Info

open System
open Xunit
open System.Reflection
open System.IO
open System.Linq
open Engine.Import.Office
open Engine.Cpu
open Engine.Core
open Engine.CodeGenCPU
open Engine.Info

module InfoPackageTEST = 
    let directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
    let dsPPT = ImportPPT.GetDSFromPPTWithLib @$"{directoryPath}/HelloDS.pptx"  

    let sys = dsPPT.System
    let _ = CpuLoaderExt.LoadStatements(sys, new Storages())

    let querySet = QuerySet()
    let configPath = @$"{directoryPath}../../../../../../../apps/CommonAppSettings.json"
    querySet.CommonAppSettings <- DSCommonAppSettings.Load(configPath) 
    DBLogger.InitializeLogReaderOnDemandAsync(querySet, [sys]).Result |> ignore
    
    [<Fact>]
    let ``Test System GetInfo`` () = 
        let info =  sys.GetInfo()
        info.Name = sys.Name |> Assert.True

    [<Fact>]
    let ``Test Multi Devices GetInfo  `` () = 
        sys.Devices.GetInfos().First().Name = sys.Devices.First().Name |> Assert.True
