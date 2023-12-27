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
    let ``Test Flow GetInfo  `` () = 
        sys.Flows.ToList().ForEach(fun f->
            let info =  f.GetInfo()
            info.Name = f.Name |> Assert.True
        )        

    [<Fact>]
    let ``Test Real GetInfo  `` () = 
        sys.Flows.ToList().ForEach(fun f->
            f.GetVerticesOfFlow().OfType<Real>().ToList().ForEach(fun r->
            let info =  r.GetInfo()
            info.Name = r.Name |> Assert.True
            )     
        )
    
    [<Fact>]
    let ``Test Call GetInfo  `` () = 
        sys.Flows.ToList().ForEach(fun f->
            f.GetVerticesOfFlow().OfType<Call>().ToList().ForEach(fun c->
            let info =  c.GetInfo()
            info.Name = c.Name |> Assert.True
            )     
        )
    [<Fact>]
    let ``Test Device GetInfo  `` () = 
        sys.Devices.ToList().ForEach(fun d->
            let info =  d.GetInfo()
            info.Name = d.Name |> Assert.True
            )     
    [<Fact>]
    let ``Test Multi Devices GetInfo  `` () = 
        sys.Devices.GetInfos().First().Name = sys.Devices.First().Name |> Assert.True
