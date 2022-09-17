using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Common;

// https://stackoverflow.com/questions/93744/most-common-c-sharp-bitwise-operations-on-enums
/*
    SomeType value = SomeType.Grapes;
    bool isGrapes = value.Is(SomeType.Grapes); //true
    bool hasGrapes = value.Has(SomeType.Grapes); //true

    value = value.Add(SomeType.Oranges);
    value = value.Add(SomeType.Apples);
    value = value.Remove(SomeType.Grapes);

    bool hasOranges = value.Has(SomeType.Oranges); //true
    bool isApples = value.Is(SomeType.Apples); //false
    bool hasGrapes = value.Has(SomeType.Grapes); //false
*/
public static class EmEnum
{
    public static bool Has<T>(this System.Enum type, T value)
    {
        try
        {
            return (((int)(object)type & (int)(object)value) == (int)(object)value);
        }
        catch
        {
            return false;
        }
    }

    public static bool Is<T>(this System.Enum type, T value)
    {
        try
        {
            return (int)(object)type == (int)(object)value;
        }
        catch
        {
            return false;
        }
    }


    public static T Add<T>(this System.Enum type, T value)
    {
        try
        {
            return (T)(object)(((int)(object)type | (int)(object)value));
        }
        catch (Exception ex)
        {
            throw new ArgumentException(
                string.Format(
                    "Could not append value from enumerated type '{0}'.",
                    typeof(T).Name
                    ), ex);
        }
    }


    public static T Remove<T>(this System.Enum type, T value)
    {
        try
        {
            return (T)(object)(((int)(object)type & ~(int)(object)value));
        }
        catch (Exception ex)
        {
            throw new ArgumentException(
                string.Format(
                    "Could not remove value from enumerated type '{0}'.",
                    typeof(T).Name
                    ), ex);
        }
    }
}


public static class EmEnuerable
{
    public static IEnumerable<T> Do<T>(this IEnumerable<T> xs, Action<T> action)
    {
        var xxs = xs.ToArray();
        foreach(var x in xxs)
            action(x);
        return xxs;
    }
}
