using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public static ParserOptions Create4Simulation(string activeCpuName) =>
        new ParserOptions { ActiveCpuName = activeCpuName, };
    public static ParserOptions Create4SimulationWhileIgnoringExtSegCall() => new ParserOptions {};

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

    public static string Combine(this string[] nameComponents) =>
        string.Join(".", nameComponents.Select(n => n.IsQuotationRequired() ? $"\"{n}\"" : n));
}

//public class NameComponents
//{
//    public List<string> Names = new();
//    public string QualifiedName =>
//        string.Join(".", Names.Select(n => n.IsQuotationRequired() ? $"\"{n}\"" : n));
//}
