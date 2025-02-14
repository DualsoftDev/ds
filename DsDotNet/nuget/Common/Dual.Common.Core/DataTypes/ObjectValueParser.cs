using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;

namespace Dual.Common.Core
{
    /// <summary>
    /// 문자열을 주어진 객체의 type 에 맞게 parsing 해서 type 에 맞는 객체를 반환하기 위한 class
    /// see DataValueType in IVariable.DataValueType.cs
    /// </summary>
    public static class ObjectValueParser
    {
        /// <summary>
        /// 문자열 value 을 refereceObject 의 type 에 맞게 parsing 해서 type 객체를 반환
        /// </summary>
        /// <param name="referenceObject"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object Parse(object referenceObject, string value)
        {
            return Parse(referenceObject.GetType(), value);
        }

        /// <summary>
        /// 문자열 value 을 type 에 맞게 parsing 해서 type 객체를 반환
        /// </summary>
        /// <param name="type">문자열 value 의 'Type' </param>
        /// <param name="value">문자열</param>
        /// <returns></returns>
        public static object Parse(Type type, string value)
        {
            if (type == typeof (string))
                return value;

            /*
             * http://stackoverflow.com/questions/2380467/c-dynamic-parse-from-system-type
             */
            var converter = TypeDescriptor.GetConverter(type);
            return converter.ConvertFrom(value);
        }


        public static bool IsZero<T>(T value)
        {
            return value.Equals(Activator.CreateInstance(typeof(T)));
        }
        public static bool IsZero(Type type, object value)
        {
            return value.Equals(Activator.CreateInstance(type));
        }

        public static bool IsNumericType(this Type type)
        {
            return type.IsOneOf(typeof (int), typeof (uint), typeof (short), typeof (ushort),
                typeof (long), typeof (ulong), typeof (float), typeof (double), typeof (byte));
        }

        /// <summary>
        /// value 를 type 에 맞게 객체 생성해서 반환
        /// </summary>
        public static object Convert(Type targetType, object value)
        {
            Contract.Requires(value != null);
            if (value == null)
            {
                throw new UnexpectedCaseOccurredException("Null argument for ObjectValueParser.Convert() call.");
                //return Activator.CreateInstance(targetType);
            }

            Type sourceType = value.GetType();
            if ( targetType == sourceType )
                return value;

            if (targetType.IsNumericType())
                return Parse(targetType, value.ToString());

            if (targetType == typeof (bool))
            {
                if (sourceType.IsNumericType())
                    return ! IsZero(sourceType, value);
            }

            throw new UnexpectedCaseOccurredException(String.Format("Failed to convert {0}({1}) to type {2}", value, sourceType.ToString(), targetType.ToString()));
        }
    }
}
