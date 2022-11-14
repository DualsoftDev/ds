namespace Engine.Parser.FS

open System

type ParserOptions(referencePath, activeCpuName, isSimulationMode, allowSkipExternalSegment) =
    member _.ActiveCpuName:string = activeCpuName
    member _.IsSimulationMode:bool = isSimulationMode           // { get; set; } = true
    member _.AllowSkipExternalSegment:bool = allowSkipExternalSegment // { get; set; } = true

    /// [device or external system] 정의에서의 file path 속성값
    member val ReferencePath:string = referencePath with get, set

    /// [device or external system] 에 정의된 ds file 을 parsing 중일 때에만 true
    member val IsSubSystemParsing = false with get, set

    /// [device or external system] 으로 새로 loading 된 system name
    member val LoadedSystemName:string option = None with get, set

    static member Create4Runtime(referencePath, activeCpuName) = ParserOptions(referencePath, activeCpuName, false, false)
    static member Create4Simulation(referencePath, activeCpuName) = ParserOptions(referencePath, activeCpuName, true, false)
    member x.Verify() = x.IsSimulationMode || (x.ActiveCpuName <> null && not x.AllowSkipExternalSegment)


[<Flags>]
type GraphVertexType =
    | None           = 0b0000000000000000
    | System         = 0b0000000000000010
    | Flow           = 0b0000000000000100
    | Segment        = 0b0000000000001000   // not child
    | Parenting      = 0b0000000000010000
    | Child          = 0b0000000000100000   // not Segment
    | CallAliasKey   = 0b0000000001000000
    | CallFlowReal   = 0b0000000010000000
    | CallApi        = 0b0000000100000000
    | AliaseKey      = 0b0000001000000000   // not direct call
    | AliaseMnemonic = 0b0000010000000000   // not direct call
    | ApiKey         = 0b0000100000000000
    | ApiSER         = 0b0001000000000000   // S ~ E ~ R
    | CausalToken    = 0b0010000000000000   // S ~ E ~ R

type GVT = GraphVertexType

(*
    0b0000000000000000
    0b0000000000000010
    0b0000000000000100
    0b0000000000001000
    0b0000000000010000
    0b0000000000100000
    0b0000000001000000
    0b0000000010000000
    0b0000000100000000
    0b0000001000000000
    0b0000010000000000
    0b0000100000000000
*)
