using System;
using System.Linq;


namespace Dual.Common.Core
{
    public partial class Tools
    {
        //static public IComparable max(params IComparable[] args)
        //{
        //    //List<string> lststrProp = RTTI.GetProperties(args.GetType());
        //    //Debug.Assert(RTTI.HasProperty_p(args.GetType(), "Count"));
        //    IComparable max = args[0];
        //    for (int i = 1; i < args.Length; i++)
        //    {
        //        if (max.CompareTo(args[i]) < 0)
        //            max = args[i];
        //    }

        //    return max;
        //}

        static public double max(params double[] args)
        {
            double max = args[0];
            for (int i = 1; i < args.Length; i++)
            {
                if (max.CompareTo(args[i]) < 0)
                    max = args[i];
            }

            return max;
        }

        static public T max<T>(Array arr)
        {
            if (arr == null || arr.Length == 0)
                throw new System.ArgumentException("max(): Parameter cannot be null or empty");

            System.Collections.IEnumerator it = arr.GetEnumerator();
            it.MoveNext();
            IComparable max = it.Current as IComparable;
            while ((it.MoveNext()) && (it.Current != null))
            {
                IComparable val = it.Current as IComparable;
                if (max.CompareTo(val) < 0)
                    max = val;
            }

            return (T)max;
        }

        // TIPS : template paramterer restriction
        static public T max<T>(params T[] args) where T : IComparable
        {
            T max = args[0];
            for (int i = 1; i < args.Length; i++)
            {
                if (max.CompareTo(args[i]) < 0)
                    max = args[i];
            }

            return max;
        }


        //static public IComparable min(params IComparable[] args)
        //{
        //    if (args == null || args.Length == 0)
        //        throw new System.ArgumentException("min(): Parameter cannot be null or empty");

        //    IComparable min = args[0];
        //    for (int i = 1; i < args.Count(); i++)
        //    {
        //        if (min.CompareTo(args[i]) > 0)
        //            min = args[i];
        //    }

        //    return min;
        //}

        static public double min(params double[] args)
        {
            double min = args[0];
            for (int i = 1; i < args.Length; i++)
            {
                if (min.CompareTo(args[i]) > 0)
                    min = args[i];
            }

            return min;
        }

        static public T min<T>(Array arr)
        {
            if (arr == null || arr.Length == 0)
                throw new System.ArgumentException("min(): Parameter cannot be null or empty");

            System.Collections.IEnumerator it = arr.GetEnumerator();
            it.MoveNext();
            IComparable min = it.Current as IComparable;
            while ((it.MoveNext()) && (it.Current != null))
            {
                IComparable val = it.Current as IComparable;
                if (min.CompareTo(val) > 0)
                    min = val;
            }

            return (T)min;
        }

        static public T min<T>(params T[] args) where T : IComparable
        {
            T min = args[0];
            for (int i = 1; i < args.Length; i++)
            {
                if (min.CompareTo(args[i]) > 0)
                    min = args[i];
            }

            return min;
        }


        static public double mid3(double a, double b, double c)
        {
            return (a + b + c) - (double)max(a, b, c) - (double)min(a, b, c);
        }



        static public double avg(params double[] args)
        {
            double dSum = 0;
            foreach (double d in args )
            {
                dSum += d;
            }

            return dSum / args.Count<double>();
        }


        static public bool AllEqual_p(params IComparable[] args)
        {
            if ( Tools.IsNullOrEmpty(args) )
                throw new System.ArgumentException("AllEqual_p(): Parameter cannot be null or empty");

            IComparable nBase = args[0];
            for (int i = 1; i < args.Length; i++)
            {
                if (nBase.CompareTo(args[i]) != 0)
                    return false;
            }

            return true;
        }

        static public bool NonNullEqual_p(params IComparable[] args)
        {
            return args != null && args.Length >= 2 && args[0] != null && AllEqual_p(args);
        }


        static public bool HasAnyNull_p(params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if ( args[i] == null)
                    return true;
            }

            return false;
        }

        static public bool HasAnyNullString_p(params string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (String.IsNullOrEmpty(args[i]))
                    return true;
            }

            return false;
        }

        static public bool AllNull_p(params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] != null)
                    return false;
            }

            return true;
        }


        static public double Sq(double x) { return x * x; }


    }
}
