// http://www.codeproject.com/Tips/550160/Getting-enum-value-from-another-class-via-Reflecti

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Dual.Common.Core
{
    [System.Reflection.Obfuscation(Exclude = true, Feature = "renaming")]
    public static class EnumerationConverter
    {
        public static EnumType GetEnumValue<EnumType>(this string enumValue)
        {
            return (EnumType)Enum.Parse(typeof(EnumType), enumValue);
        }

        public static string[] GetStrings<EnumType>()
        {
            return GetStrings(typeof(EnumType));
        }

        public static string[] GetStrings(Type typeEnums)
        {
            return Enum.GetNames(typeEnums);
        }

        public static string GetStringValue<EnumType>(EnumType e)
        {
            return e.ToString();
        }

        public static int GetOffset<EnumType>(string enumValue)
        {
            string[] strEnums = GetStrings<EnumType>();
            for (int i = 0; i < strEnums.Count(); i++)
            {
                if (strEnums[i] == enumValue)
                    return i;
            }

            return -1;
        }

        public static int GetOffset<EnumType>(EnumType enumValue)
        {
            return Tools.ToList<EnumType>(Enum.GetValues(typeof (EnumType))).IndexOf(enumValue);
        }
    }

    public static class EnumerationExtractor
    {
        public static IEnumerable<Type> GetInnerEnumTypes(object dynamicObject) { return GetInnerEnumTypes(dynamicObject.GetType()); }
        public static IEnumerable<Type> GetInnerEnumTypes(Type dynamicType)
        {
            //get member info
            MemberInfo[] memberInfos = dynamicType.GetMembers(BindingFlags.Public | BindingFlags.Static);
            foreach (MemberInfo t in memberInfos)
            {
                System.Console.WriteLine(t.Name);
                string enumName = t.Name;
                string enumValue = enumName;
                var typeEnums = Type.GetType(dynamicType.Namespace + "." + dynamicType.Name + "+" + enumValue);
                yield return typeEnums;
            }
        }

        [Obsolete("Use Enum.GetNames() instead.")]
        public static IEnumerable<string> GetEnumeration(Type typeEnums)
        {
            if (typeEnums != null && (typeEnums.BaseType != null && (typeEnums.BaseType.FullName == "System.Enum")))
            {
                var fieldsArray = typeEnums.GetFields(BindingFlags.Public | BindingFlags.Static);
                foreach (var fInfo in fieldsArray)
                {
                    var ulValue = (ulong)Convert.ChangeType(fInfo.GetValue(null), typeof(ulong));
                    yield return fInfo.Name.ToString(CultureInfo.InvariantCulture) + ":" + ulValue.ToString(CultureInfo.InvariantCulture);
                }
            }
        }
    }
}
