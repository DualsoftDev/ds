namespace DsMemoryService

open System.IO
open System.Reactive
open System.IO.MemoryMappedFiles
open System.Security.AccessControl
open System.Security.Principal
open IOMapApi
open System
open System.ServiceProcess
open System.Threading
open System.Collections.Generic
open IOMapApi.MemoryIOEventImpl
open IOMapApi.MemoryUtilImpl
open System.Diagnostics

type MMF = MemoryMappedFile

module ServiceImpl =

    let getMMFSecurity() =  //MemoryMappedFileSecurity 이것때문에 net48로 구동중
        let mmfSec = MemoryMappedFileSecurity()
        mmfSec.AddAccessRule(
            AccessRule<MemoryMappedFileRights>(
                SecurityIdentifier(WellKnownSidType.WorldSid, null), 
                MemoryMappedFileRights.ReadWrite, 
                AccessControlType.Allow)
        )
        mmfSec

    
    type IOMapService() =
        inherit ServiceBase(ServiceName = "IOMapService")
        let logPath = Path.Combine(LogDirectory, "Log.txt")
        do
            if not (Directory.Exists(LogDirectory)) then 
                Directory.CreateDirectory(LogDirectory) |> ignore

        let log msg = File.AppendAllText(logPath,$"{System.DateTime.Now} : {msg}\n")
        let load(device: string) =
            let filePath = Path.Combine(MemoryUtilImpl.BasePath, $@"{device}")
            use fs = new FileStream(filePath, FileMode.Open)
           
            //MMF.CreateFromFile(filePath, FileMode.Open, getMMFName device)  //권한 생략
            // 메모리 맵 파일 생성
            MMF.CreateFromFile(
                fs,
                getMMFName device,
                fs.Length,
                MemoryMappedFileAccess.ReadWrite, 
                getMMFSecurity(),
                HandleInheritability.None, 
                leaveOpen = true)  


        let cts = new CancellationTokenSource()
        
        let loaded = new Dictionary<string, MemoryMappedFile>()
        let loadFiles xs  =
            xs |> Array.map (fun f-> f, load f)
               |> Array.iter(fun (f,m) -> loaded.Add(f,m))

        let checkFunction  =
            async {
                try
                    while not cts.IsCancellationRequested
                        do
                        let handles = String.Join(", ", loaded |>Seq.map(fun d->d.Key))
                        log $"LoadedFiles: {handles}"
                        log "IOMapService is running."
                        do! Async.Sleep(60000) //  60초 간격으로 로그 출력

                with ex -> log $"MemoryIOManager checkFunction 예외 {ex.Message}"
                }

        let loadFunction  =
            async {
                try
                    ()
                    while not cts.IsCancellationRequested
                        do
                        let newFiles = MemoryUtilImpl.GetAllRelativeFiles()
                                        |> Array.filter(fun f-> not(loaded.Keys |> Seq.contains(f)))

                        if newFiles.Length > 0 then
                            loadFiles newFiles 
                            let handles = String.Join(", ", newFiles)
                            log $"CreateFromNewFileHandles: {handles}"
                        do! Async.Sleep(1000) //  1초 간격으로 신규 file 체크

                with ex -> log $"MemoryIOManager loadFunction 예외 {ex.Message}"
                }

 
        override this.OnStart(args: string[]) =
            loadFiles (MemoryUtilImpl.GetAllRelativeFiles())
            
            Async.StartImmediate (loadFunction, cts.Token) |> ignore
            Async.StartImmediate (checkFunction, cts.Token) |> ignore


            base.OnStart(args)     

        override this.OnStop() =
            base.OnStop()          
            
        
        member this.Load(dev) = load dev
            
    