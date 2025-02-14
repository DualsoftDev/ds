using Dual.Common.Base.CS;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace Dual.Common.Core
{
    /// <summary>
    /// XML attribute 의 Value 값을 가져 오기 편하도록 하는 helper
    /// </summary>
    public static class EmXml
    {
        public static double? GetDouble(this XmlAttribute att)
        {
            if (att == null)
                return null;
            double value = 0;
            if (!Double.TryParse(att.Value, out value))
                return null;

            return value;
        }

        public static double GetDoubleValue(this XmlAttribute att, double defaultValue=0.0)
        {
            var result = GetDouble(att);
            return result.HasValue ? result.Value : defaultValue;
        }


        public static int? GetInt(this XmlAttribute att)
        {
            if (att == null)
                return null;
            int value = 0;
            if (!Int32.TryParse(att.Value, out value))
                return null;

            return value;
        }

        public static int GetIntValue(this XmlAttribute att, int defaultValue=0)
        {
            var result = GetInt(att);
            return result.HasValue ? result.Value : defaultValue;
        }

        public static int? GetHex(this XmlAttribute att)
        {
            if (att == null)
                return null;
            int value = 0;
            if (!Int32.TryParse(att.Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
                return null;

            return value;
        }

        public static int GetHexValue(this XmlAttribute att, int defaultValue=0)
        {
            var result = GetHex(att);
            return result.HasValue ? result.Value : defaultValue;
        }

        public static long? GetLong(this XmlAttribute att)
        {
            if (att == null)
                return null;
            long value = 0;
            if (!Int64.TryParse(att.Value, out value))
                return null;

            return value;
        }

        public static long GetLongValue(this XmlAttribute att, long defaultValue=0L)
        {
            var result = GetLong(att);
            return result.HasValue ? result.Value : defaultValue;
        }



        public static string GetString(this XmlAttribute att)
        {
            if (att == null)
                return null;

            return att.Value;
        }

        public static string GetStringValue(this XmlAttribute att, string defaultValue="")
        {
            var result = GetString(att);
            return result == null ? defaultValue : result;
        }


        public static bool? GetBoolean(this XmlAttribute att)
        {
            if (att == null)
                return null;
            bool value = false;
            if (!Boolean.TryParse(att.Value, out value))
                return null;

            return value;
        }

        public static bool GetBooleanValue(this XmlAttribute att, bool defaultValue=false)
        {
            var result = GetBoolean(att);
            return result.HasValue ? result.Value : defaultValue;
        }


        /// <summary>
        /// 범용 type 에 대한 value getter.  가급적 위의 specialized version 을 사용하기 바람.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="att"></param>
        /// <returns></returns>
        public static T ParseGetValueOrDefault<T>(this XmlAttribute att)
        {
            if (att == null)
                return (T)Activator.CreateInstance(typeof(T));
            return (T)ObjectValueParser.Parse(typeof(T), att.Value);
        }


        public static string ToXmlString(this XmlNode node, int indentation=2)
        {
            using (var sw = new System.IO.StringWriter())
            {
                using (var xw = new XmlTextWriter(sw))
                {
                    xw.Formatting = Formatting.Indented;
                    xw.Indentation = indentation;
                    //node.WriteContentTo(xw);
                    node.WriteTo(xw);
                }
                return sw.ToString();
            }
        }


        /// <summary>
        /// node 의 children 이 주어진 possibleChildrenTags 이외의 tag 를 갖고 있으면 오류 반환
        /// </summary>
        /// <param name="node"></param>
        /// <param name="possibleChildrenTags"></param>
        /// <returns></returns>
        public static IEnumerable<string> FindInvalidChildrenTags(this XmlNode node, params string[] validTags)
        {
            var childTags = node.ChildNodes
                .SelectEx<XmlNode, string>(c => c.Name)
                .Distinct()
                ;

            var diff = childTags.Except(validTags);
            return diff;
        }

        public static IEnumerable<string> FindInvalidChildrenTags(this XmlNode node, IEnumerable<string> validTags)
        {
            return FindInvalidChildrenTags(node, validTags.ToArray());
        }

        public static bool CheckXmlChildrenTags(this XmlNode node, params string[] possibleChildrenTags)
        {
            return FindInvalidChildrenTags(node, possibleChildrenTags).IsNullOrEmpty();
        }
    }
}
