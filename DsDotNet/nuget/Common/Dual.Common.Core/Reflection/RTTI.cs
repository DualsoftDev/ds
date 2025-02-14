using System;
using System.Collections.Generic;
using System.Reflection;

namespace Dual.Common.Core
{
    public class RTTI
    {
        /*
         * TIPS : Getting Type
         * - From instance : Debug.Assert(RTTI.HasProperty_p(lststr.GetType(), "Count"));
         * - From type : Debug.Assert(!RTTI.HasMethod_p(typeof(List<string>), "SomeNonExistingMethod"));
         */
        static public bool HasProperty_p(Type t, string propName)
        {
            if ( String.IsNullOrEmpty(propName) )
                return false;

            PropertyInfo[] pis = t.GetProperties();
            return Array.Find<PropertyInfo>(pis, element => element.Name == propName) != null;
        }

        static public List<string> GetProperties(Type t) { return GetProperties(t, false);  }
        static public List<string> GetProperties(Type t, bool bSort/*=false*/)
        {
            List<string> lstprop = new List<string>();
            PropertyInfo[] pis = t.GetProperties();
            Array.Sort(pis, (element1, element2) => element1.Name.CompareTo(element2.Name));
            foreach (PropertyInfo p in pis)
                lstprop.Add(p.Name);

            return lstprop;
        }



        static public bool HasMethod_p(Type t, string propName)
        {
            if (String.IsNullOrEmpty(propName))
                return false;

            MethodInfo[] pis = t.GetMethods();
            return Array.Find<MethodInfo>(pis, element => element.Name == propName) != null;
        }

        static public List<string> GetMethods(Type t) { return GetMethods(t, false); }
        static public List<string> GetMethods(Type t, bool bSort/*=false*/)
        {
            List<string> lstprop = new List<string>();
            MethodInfo[] pis = t.GetMethods();
            Array.Sort(pis, (element1, element2) => element1.Name.CompareTo(element2.Name));
            foreach (MethodInfo p in pis)
                lstprop.Add(p.Name);

            return lstprop;
        }

    }
}
