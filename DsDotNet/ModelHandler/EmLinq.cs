using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelHandler;

public static class EmLinq
{
    public static bool NonNullAny<TSource>(this IEnumerable<TSource> source)
    {
        if (source == null)
            return false;
        return source.Any();
    }
    public static bool NonNullAny<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        if (source == null)
            return false;
        return source.Any(predicate);
    }
    public static bool IsNullOrEmpty(this IEnumerable enumerable)
    {
        return (enumerable == null || !enumerable.Cast<object>().Any());
    }
    public static void ForEach<T>(this IEnumerable source, Action<T> action)
    {
        foreach (T item in source)
            action(item);
    }

    public static void Iter<T>(this IEnumerable<T> source, Action<T> action) => ForEach(source, action);
    public static void Iter<T>(this IEnumerable<T> source, Action<T, int> action)
    {
        int i = 0;
        foreach (T item in source)
            action(item, i++);
    }

    public static int GetCount(this IEnumerable xs)
    {
        if (xs == null)
            return 0;

        int n = 0;
        foreach (var x in xs)
            n++;
        return n;
    }

    public static T Singleton<T>(this IEnumerable<T> xs)
    {
        var xs_ = xs.ToArray();
        if (xs_.Length == 1)
            return xs_[0];
        throw new Exception("Not a singleton");
    }
    public static T Singleton<T>(this IEnumerable<T> xs, Func<T, bool> pred)
    {
        var xs_ = xs.Where(pred).ToArray();
        if (xs_.Length == 1)
            return xs_[0];
        throw new Exception("Not a singleton");
    }
    public static bool IsSingleton<T>(this IEnumerable<T> xs) => xs?.Count() == 1;
    public static bool IsSingleton<T>(this IEnumerable<T> xs, Func<T, bool> pred) => xs?.Where(pred).Count() == 1;

    public static bool IsOneOf(this IComparable key, params IComparable[] set) => set.Any(e => e.CompareTo(key) == 0);
    public static bool IsOneOf(this object key, params object[] set) => set.Any(e => e == key);
    public static bool IsOneOf(this Type type, params Type[] set) => set.Any(t => t.IsAssignableFrom(type));
}
