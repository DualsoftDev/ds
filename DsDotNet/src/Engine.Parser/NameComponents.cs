using Engine.Common;
using System.Globalization;
using static Engine.Core.CoreClass;
using static Engine.Core.CoreFlow;
using static Engine.Core.CoreStruct;

namespace Engine.Parser;

public record ParserOptions
{
    public string ActiveCpuName { get; set; } = null;
    public bool IsSimulationMode { get; set; } = true;
    public bool AllowSkipExternalParserSegment { get; set; } = true;
    public static ParserOptions Create4Runtime(string activeCpuName) =>
        new ParserOptions
        {
            ActiveCpuName = activeCpuName,
            IsSimulationMode = false,
            AllowSkipExternalParserSegment = false
        };

    public static ParserOptions Create4Simulation(string activeCpuName=null) =>
        new ParserOptions { ActiveCpuName = activeCpuName, };
    public static ParserOptions Create4SimulationWhileIgnoringExtSegCall() =>
        new ParserOptions { AllowSkipExternalParserSegment  = false, };

    public bool Verify() => IsSimulationMode || (ActiveCpuName != null && !AllowSkipExternalParserSegment);
}

public static class ParserExtension
{
    /// <summary>
    /// DS 문법에서 사용하는 identifier (ParserSegment, flow, call 등의 이름)가 적법한지 검사.
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

    static string _doubleQuote = @"""";
    /// <summary>이름 구성 요소에서 맨 앞, 맨 뒤의 double quote 가 있으면 이를 제거한 문자열 반환 </summary>
    public static string DeQuoteOnDemand(this string compo) =>
        compo.StartsWith(_doubleQuote) && compo.EndsWith(_doubleQuote)
        ? compo.Substring(1, compo.Length - 2)
        : compo
        ;
    /// <summary>이름 구성 요소에 특수 문자가 포함된 경우, 맨 앞, 맨 뒤에 double quote 를 추가한 문자열 반환 </summary>
    public static string QuoteOnDemand(this string compo) =>
        compo.IsQuotationRequired()
        ? $"{_doubleQuote}{compo}{_doubleQuote}"
        : compo
        ;


    /// <summary> path 구성 요소 array 를 '.' 으로 combine </summary>
    public static string Combine(this string[] nameComponents, string separator=".") =>
        string.Join(separator, nameComponents.Select(n => n.IsQuotationRequired() ? $"\"{n}\"" : n));
    public static string[] Divide(this string qualifiedName) => qualifiedName.Split(new[] { '.' }).ToArray();
    public static DsSystem FindParserSystem(this Model model, string[] nameComponents) =>
        model.Systems.FirstOrDefault(sys => sys.Name == nameComponents[0]);
    public static RootFlow FindFlow(this Model model, string[] nameComponents)
    {
        var system = model.FindParserSystem(nameComponents);
        var flow = system?.RootFlows().FirstOrDefault(rf => rf.Name == nameComponents[1]);
        return flow;
    }

    public static ParserSegment FindParenting(this Model model, string[] nameComponents)
    {
        Assert(nameComponents.Length >= 3);
        var flow = model.FindFlow(nameComponents);
        var seg = flow?.InstanceMap[nameComponents[2]] as ParserSegment;
        return seg;
    }


    public static object Find(this Model model, string[] fqdn)
    {
        var n = fqdn.Length;
        Assert(n >= 3);
        if (n == 4)
        {
            var parenting = model.FindParenting(fqdn);
            if (parenting == null)
                return null;

            if (parenting.InstanceMap.ContainsKey(fqdn[3]))
                return parenting.InstanceMap[fqdn[3]];

            var aliasMap = parenting.ContainerFlow.AliasNameMaps;
            var aliasKey = fqdn[3];
            if (aliasMap.ContainsKey(aliasKey))
                return model.Find(aliasMap[aliasKey]);
            return null;
        }

        var flow = model.FindFlow(fqdn);
        if (flow == null)
            return null;

        var name = fqdn[2];
        if (flow.InstanceMap.ContainsKey(name))
            return flow.InstanceMap[name];

        return flow.CallPrototypes.FirstOrDefault(cp => cp.Name == name);
    }

    public static T Find<T>(this Model model, string[] fqdn) where T : class => model.Find(fqdn) as T;

    public class NameComponentsComparer : IEqualityComparer<string[]>
    {
        public bool Equals(string[] x, string[] y) => x.Length == y.Length && x.SequenceEqual(y);

        public int GetHashCode(string[] obj) => (int)obj.Average(ob => ob.GetHashCode());
        public static NameComponentsComparer Instance { get; } = new NameComponentsComparer();
    }
}
