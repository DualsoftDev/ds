namespace IOMapApi

open System
open System.IO


module MemoryUtilImpl = 


    let mutable TestMode = false

    let BasePath =
            Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Dualsoft", "MemoryFile")

    let LogDirectory = Path.Combine(BasePath, "Log")

    //경로를 호출이름으로 사용
    let getMMFName (namePath:string) = 
        let nameKey = namePath.Replace(@"\", "_") 
        if TestMode 
        then
            nameKey
        else 
            @$"Global\{nameKey}" 

    //해당 파일 쓰기 락 관리키
    let getSemaphoreKey (namePath:string) = 
        let nameKey = namePath.Replace(@"\", "_") 
        @$"Semaphore{nameKey}" 

    //자신의 쓰기 시도 타임아웃  //쓰기 대기후 강제 semaphore 릴리즈  타임
    let WriteTimeout, SemaphoreReleaseTime = 10000, 20000


    let GetAllRelativeFiles()  =
        Directory.GetFiles(BasePath, "*.*", SearchOption.AllDirectories)
        |> Array.map (fun fullPath -> 
            if fullPath.StartsWith(BasePath, StringComparison.OrdinalIgnoreCase) then 
                fullPath.Substring(BasePath.Length).TrimStart(Path.DirectorySeparatorChar) 
            else 
                fullPath)
        |> Array.filter (fun fullPath -> not (fullPath.Contains(".")  )) //Log.txt 제외


