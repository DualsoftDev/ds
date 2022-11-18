namespace Engine.Parser.FS

open System

type ParserOptions(referencePath, activeCpuName, isSimulationMode, allowSkipExternalSegment) =
    member _.ActiveCpuName:string = activeCpuName
    member _.IsSimulationMode:bool = isSimulationMode           // { get; set; } = true
    member _.AllowSkipExternalSegment:bool = allowSkipExternalSegment // { get; set; } = true

    /// [device or external system] 정의에서의 file path 속성값
    member val ReferencePath:string = referencePath with get, set

    /// [device or external system] 으로 새로 loading 된 system name.  외부 ds file 을 parsing 중일 때에만 Some 값을 가짐
    member val LoadedSystemName:string option = None with get, set

    static member Create4Runtime(referencePath, activeCpuName) = ParserOptions(referencePath, activeCpuName, false, false)
    static member Create4Simulation(referencePath, activeCpuName) = ParserOptions(referencePath, activeCpuName, true, false)
    member x.Verify() = x.IsSimulationMode || (x.ActiveCpuName <> null && not x.AllowSkipExternalSegment)


