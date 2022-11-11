namespace Engine.Parser.FS

open System

type ParserOptions(activeCpuName, isSimulationMode, allowSkipExternalSegment) =
    member _.ActiveCpuName:string = activeCpuName
    member _.IsSimulationMode:bool = isSimulationMode           // { get; set; } = true
    member _.AllowSkipExternalSegment:bool = allowSkipExternalSegment // { get; set; } = true

    static member Create4Runtime(activeCpuName:string) = ParserOptions(activeCpuName, false, false)
    static member Create4Simulation(activeCpuName:string) = ParserOptions(activeCpuName, true, false)
    member x.Verify() = x.IsSimulationMode || (x.ActiveCpuName <> null && not x.AllowSkipExternalSegment)


[<Flags>]
type GraphVertexType =
    | None           = 0b0000000000000000
    | System         = 0b0000000000000010
    | Flow           = 0b0000000000000100
    | Segment        = 0b0000000000001000   // not child
    | Parenting      = 0b0000000000010000
    | Child          = 0b0000000000100000   // not Segment
    | Call           = 0b0000000001000000
    | AliaseKey      = 0b0000000010000000   // not direct call
    | AliaseMnemonic = 0b0000000100000000   // not direct call
    | ApiKey         = 0b0000001000000000
    | ApiSER         = 0b0000010000000000   // S ~ E ~ R
    | CausalToken    = 0b0000100000000000   // S ~ E ~ R

type GVT = GraphVertexType