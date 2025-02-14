using System.Reflection;

namespace Dual.Common.Core
{
    public static class EmReflection
    {
        /// <summary>
        /// instance 가 가진 private/protected/internal field 의 값을 reflection 을 이용해서 가져온다.
        /// </summary>
        public static object GetNonPublicField<T>(this T instance, string fieldName)
        {
            var field = typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
            var value = field.GetValue(instance);
            return value;
        }
    }
}
