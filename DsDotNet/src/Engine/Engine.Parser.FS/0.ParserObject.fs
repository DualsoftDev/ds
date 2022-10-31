type ParserOptions = {
        ActiveCpuName:string
        IsSimulationMode:bool           // { get; set; } = true
        AllowSkipExternalSegment:bool // { get; set; } = true
} with
    let defaultOption = {
        ActiveCpuName = ""
        IsSimulationMode = true
        AllowSkipExternalSegment = false
    }
    static member Create4Runtime(activeCpuName:string) = {
        ActiveCpuName = activeCpuName
        IsSimulationMode = false
        AllowSkipExternalSegment = false
    }
    static member Create4Simulation(activeCpuName:string) = { defaultOption with ActiveCpuName = activeCpuName}
    static member Verify() = IsSimulationMode || (ActiveCpuName != null && !AllowSkipExternalSegment)

//public enum GraphVertexType
//{
//    None           = 0,
//    System         = 1 << 0,
//    Flow           = 1 << 1,
//    Segment        = 1 << 2,  // not child
//    Parenting      = 1 << 6,
//    Child          = 1 << 10, // not Segment
//    Call           = 1 << 11,
//    AliaseKey      = 1 << 15, // not direct call
//    AliaseMnemonic = 1 << 16, // not direct call
//    ApiKey         = 1 << 19,
//    ApiSER         = 1 << 20, // S ~ E ~ R
//}

