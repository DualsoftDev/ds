using System.Globalization;

namespace Engine.Util;

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
}
