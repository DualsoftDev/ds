using System.Globalization;

namespace Engine.Core;

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

    public static ParserOptions Create4Simulation(string activeCpuName=null) =>
        new ParserOptions { ActiveCpuName = activeCpuName, };
    public static ParserOptions Create4SimulationWhileIgnoringExtSegCall() =>
        new ParserOptions { AllowSkipExternalSegment  = false, };

    public bool Verify() => IsSimulationMode || (ActiveCpuName != null && !AllowSkipExternalSegment);
}

public static class ParserExtension
{
    /// <summary>
    /// DS 문법에서 사용하는 identifier (Segment, flow, call 등의 이름)가 적법한지 검사.
    /// 적법하지 않으면 double quote 로 감싸주어야 한다.
    /// </summary>
    public static bool IsValidIdentifier(this string identifier)
    {
        if (identifier.IsNullOrEmpty())
            throw new ArgumentNullException(nameof(identifier));
        if (identifier == "_")
            return true;

        bool isHangul(char ch) => char.GetUnicodeCategory(ch) == UnicodeCategory.OtherLetter;
        bool isValidStart(char ch) => ch == '_' || char.IsLetter(ch) || isHangul(ch);
        bool isValid(char ch) => isValidStart(ch) || char.IsDigit(ch);

        var chars = identifier.ToCharArray();
        var first = chars[0];

        return isValidStart(first) && chars.Skip(1).ForAll(isValid);
    }
    public static bool IsQuotationRequired(this string identifier) => !IsValidIdentifier(identifier);

    /// <summary> path 구성 요소 array 를 '.' 으로 combine </summary>
    public static string Combine(this string[] nameComponents, string separator=".") =>
        string.Join(separator, nameComponents.Select(n => n.IsQuotationRequired() ? $"\"{n}\"" : n));
    public static string[] Divide(this string qualifiedName) => qualifiedName.Split(new[] { '.' }).ToArray();
    public static DsSystem FindSystem(this Model model, string[] nameComponents) =>
        model.Systems.FirstOrDefault(sys => sys.Name == nameComponents[0]);
    public static RootFlow FindFlow(this Model model, string[] nameComponents)
    {
        var system = model.FindSystem(nameComponents);
        var flow = system?.RootFlows.FirstOrDefault(rf => rf.Name == nameComponents[1]);
        return flow;
    }

    public static SegmentBase FindParenting(this Model model, string[] nameComponents)
    {
        Assert(nameComponents.Length >= 3);
        var flow = model.FindFlow(nameComponents);
        var seg = flow?.InstanceMap[nameComponents[2]] as SegmentBase;
        return seg;
    }


    public static object Find(this Model model, string[] fqdn)
    {
        var n = fqdn.Length;
        Assert(n >= 3);
        if (n == 4)
        {
            var parenting = model.FindParenting(fqdn);
            if (parenting != null && parenting.InstanceMap.ContainsKey(fqdn[3]))
                return parenting.InstanceMap[fqdn[3]];
            return null;
        }

        var flow = model.FindFlow(fqdn);
        return flow?.InstanceMap[fqdn[2]];
    }
}
