using System;
using System.Collections.Generic;

namespace Dual.Common.Core
{
    /*
     * Tools : Type Conversion part
     */
    public partial class Tools
    {
        static public object[] ToObjects(string[] astr)
        {
            object[] arr = astr;
            return arr;
        }

        static public object[] ToObjects(Array arr)
        {
            if ( arr == null )
                return new object[0];

            object[] o = new object[arr.Length];
            arr.CopyTo(o, 0);
            return o;
        }

        static public T[] ToObjects<T>(object[] arr)
        {
            T[] objs = new T[arr.Length];

            int i = 0;
            foreach (object o in arr)
                objs[i++] = (T)o;

            return objs;
        }

        static public T[] ToObjects<T>(object oArr)
        {
            return ToObjects<T>(oArr as object[]);
        }



        static public List<T> ToList<T>(Array arr)
        {
            List<T> lst = new List<T>();
            if (arr != null)
            {
                foreach (object o in arr)
                    lst.Add((T)o);
            }

            return lst;
        }

        static public List<T> ToList<T>(object[] arr)
        {
            List<T> lst = new List<T>();
            if (arr != null)
            {
                foreach (object o in arr)
                    lst.Add((T)o);
            }

            return lst;
        }

        static public List<T> ToList<F, T>(List<F> lstSrc) where T : class { return ToList<F, T>(lstSrc, true);  }

        // <F>rom type 의 list 를 <T>o type 의 list 로 변환
        static public List<T> ToList<F, T>(List<F> lstSrc, bool bRemoveNotConvertable/*=true*/) where T : class
        {
            List<T> lst = new List<T>();
            foreach (F f in lstSrc)
            {
                T t = f as T;
                if (bRemoveNotConvertable && t == null)
                    continue;

                lst.Add(t);
            }

            return lst;
        }

        static public bool ForceToBool(object o)
        {
            if (o == null)
                return false;

            if (o is bool)
                return (bool)o;
            else if (o is string)
            {
                string s = (string)o;
                if (String.IsNullOrEmpty(s) || s == "-1")
                    return false;
                if (s.ToLower() == "true")
                    return true;
                if (s.ToLower() == "false")
                    return false;

                return Convert.ToInt32(s) != 0;
            }

            try
            {
                return (int)o != 0;
            }
            catch (System.Exception ex)
            {
                ShowMessage("RUNTIME ERROR: failed to convert {0} to boolean!!\r\n{1}", o, ex.Message);
                return false;
            }
        }

        static public double ForceToDouble(object o)
        {
            if (o == null)
                return 0;

            if (o is double)
                return (double)o;
            else
            {
                try
                {
                    string s = o.ToString();
                    if (String.IsNullOrEmpty(s))
                        return 0;
                    return Convert.ToDouble(s);
                }
                catch (System.Exception ex)
                {
                    ShowMessage("RUNTIME ERROR: failed to convert {0} to double!!\r\n{1}", o, ex.Message);
                    return 0;
                }
            }
        }

        static public int ForceToInt(object o)
        {
            if (o == null)
                return 0;

            if (o is int)
                return (int)o;
            else
            {
                try
                {
                    string s = o.ToString();
                    if (String.IsNullOrEmpty(s))
                        return 0;
                    return Convert.ToInt32(s);
                }
                catch (System.Exception ex)
                {
                    ShowMessage("RUNTIME ERROR: failed to convert {0} to int!!\r\n{1}", o, ex.Message);
                    return 0;
                }
            }
        }

        static public long ForceToLong(object o)
        {
            if (o == null)
                return 0;

            if (o is long)
                return (long)o;
            else
            {
                try
                {
                    string s = o.ToString();
                    if (String.IsNullOrEmpty(s))
                        return 0;
                    return Convert.ToInt64(s);
                }
                catch (System.Exception ex)
                {
                    ShowMessage("RUNTIME ERROR: failed to convert {0} to long!!\r\n{1}", o, ex.Message);
                    return 0;
                }
            }
        }

        // TIPS : IEnumerable<T> -> List<T> 로 변환
        static public List<T> EnumerableToList<T>(IEnumerable<T> src)
        {
            List<T> list = new List<T>();
            foreach (T ele in src)
                list.Add(ele);
            return list;
        }


        // http://stackoverflow.com/questions/1029612/is-there-a-built-in-way-to-convert-ienumerator-to-ienumerable
        static public IEnumerable<T> EnumeratorToEnumerable<T>(System.Collections.IEnumerator iterator)
        {
            while (iterator.MoveNext())
            {
                yield return (T)iterator.Current;
            }
        }

        static public List<T> EnumeratorToList<T>(IEnumerator<T> enumerator)
        {
            return EnumerableToList<T>(EnumeratorToEnumerable<T>(enumerator));
        }


        // http://stackoverflow.com/questions/828342/what-is-the-best-way-to-convert-an-ienumerator-to-a-generic-ienumerator
        static public System.Collections.Generic.IEnumerator<T> EnumeratorToGenericEnumerator<T>(System.Collections.IEnumerator iterator)
        {
            while (iterator.MoveNext())
            {
                yield return (T)iterator.Current;
            }
        }
    }
}
