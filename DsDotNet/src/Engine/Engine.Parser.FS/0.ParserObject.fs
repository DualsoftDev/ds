public record ParserOptions
{
    public string ActiveCpuName { get; set; } = null;
    public bool IsSimulationMode { get; set; } = true;
    public bool AllowSkipExternalSegment { get; set; } = true;
    public static ParserOptions Create4Runtime(string activeCpuName) =>
        new ParserOptions
        {
            ActiveCpuName = activeCpuName,
            IsSimulationMode = false,
            AllowSkipExternalSegment = false
        };

    public static ParserOptions Create4Simulation(string activeCpuName = null) =>
        new ParserOptions { ActiveCpuName = activeCpuName, };
    public static ParserOptions Create4SimulationWhileIgnoringExtSegCall() =>
        new ParserOptions { AllowSkipExternalSegment = false, };

    public bool Verify() => IsSimulationMode || (ActiveCpuName != null && !AllowSkipExternalSegment);
}

public enum GraphVertexType
{
    None           = 0,
    System         = 1 << 0,
    Flow           = 1 << 1,
    Segment        = 1 << 2,  // not child
    Parenting      = 1 << 6,
    Child          = 1 << 10, // not Segment
    Call           = 1 << 11,
    AliaseKey      = 1 << 15, // not direct call
    AliaseMnemonic = 1 << 16, // not direct call
    ApiKey         = 1 << 19,
    ApiSER         = 1 << 20, // S ~ E ~ R
}

