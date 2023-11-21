namespace IOMapDSApi

open System
open System.IO
open System.Text


module Global = 

    let BasePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Dualsoft", "MemoryFile")

    let LogDirectory = Path.Combine(BasePath, "Log")

    //경로를 호출이름으로 사용
    let getMMFName (namePath:string) = 
        let nameKey = namePath.Replace(@"\", "_") 
        @$"Global\{nameKey}" 

    //해당 파일 쓰기 락 관리키
    let getSemaphoreKey (namePath:string) = 
        let nameKey = namePath.Replace(@"\", "_") 
        @$"Semaphore{nameKey}" 

    //자신의 쓰기 시도 타임아웃  //쓰기 대기후 강제 semaphore 릴리즈  타임
    let WriteTimeout, SemaphoreReleaseTime = 5000, 10000
