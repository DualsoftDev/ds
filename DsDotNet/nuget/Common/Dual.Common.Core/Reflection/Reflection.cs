using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.CompilerServices;


namespace Dual.Common.Core
{
    public static partial class Reflection
    {
        public static IEnumerable<string> GetMethodInfo(MethodInfo m)
        {
            // Get return value. 
            yield return m.ReturnType.FullName + " " + m.Name;

            // Get params. 
            foreach (ParameterInfo pi in m.GetParameters())
                yield return pi.ParameterType + " " + pi.Name;
        }

        public static IEnumerable<string> GetMethods(Type t) { return GetMethods(t, null); }
        public static IEnumerable<string> GetMethods(Type t, Nullable<BindingFlags> flag/*=null*/)
        {
            MethodInfo[] mi;
            if (flag.HasValue)
                mi = t.GetMethods(flag.Value);
            else
                mi = t.GetMethods();

            foreach (MethodInfo m in mi)
                yield return string.Join(" ", GetMethodInfo(m).ToArray());
        }

        public static IEnumerable<string> GetMembers(Type t) { return GetMembers(t, null); }
        public static IEnumerable<string> GetMembers(Type t, Nullable<BindingFlags> flag/*=null*/)
        {
            MemberInfo[] mi;
            if (flag.HasValue)
                mi = t.GetMembers(flag.Value);
            else
                mi = t.GetMembers();

            foreach (MemberInfo m in mi)
                yield return String.Format("{0}\t{1}\t{2}", m.DeclaringType, m.MemberType, m.Name);
        }

        public static IEnumerable<string> GetFields(Type t) { return GetFields(t, null); }
        public static IEnumerable<string> GetFields(Type t, Nullable<BindingFlags> flag/*=null*/)
        {
            FieldInfo[] fi;
            if (flag.HasValue)
                fi = t.GetFields(flag.Value);
            else
                fi = t.GetFields();

            foreach (FieldInfo f in fi)
                yield return f.Name;
        }

        public static IEnumerable<string> GetProperties(Type t) { return GetProperties(t, null); }
        public static IEnumerable<string> GetProperties(Type t, Nullable<BindingFlags> flag/*=null*/)
        {
            PropertyInfo[] pi;
            if (flag.HasValue)
                pi = t.GetProperties(flag.Value);
            else
                pi = t.GetProperties();

            foreach (PropertyInfo p in pi)
                yield return p.Name;
        }


        public static IEnumerable<string> GetInterfaces(Type t)
        {
            Type[] ii = t.GetInterfaces();

            foreach (Type i in ii)
                yield return i.Name;
        }

        public static IEnumerable<string> GetConstructors(Type t) { return GetConstructors(t, null); }
        public static IEnumerable<string> GetConstructors(Type t, Nullable<BindingFlags> flag/*=null*/)
        {
            ConstructorInfo[] ci;
            if (flag.HasValue)
                ci = t.GetConstructors(flag.Value);
            else
                ci = t.GetConstructors();

            foreach (ConstructorInfo c in ci)
                yield return c.ToString();
        }


        public static string GetTypeInfo(Type t)
        {
            StringBuilder OutputText = new StringBuilder();

            //properties retrieve the strings 
            OutputText.AppendLine("Analysis of type " + t.Name);
            OutputText.AppendLine("Type Name: " + t.Name);
            OutputText.AppendLine("Full Name: " + t.FullName);
            OutputText.AppendLine("Namespace: " + t.Namespace);

            //properties retrieve references        
            Type tBase = t.BaseType;

            if (tBase != null)
                OutputText.AppendLine("Base Type: " + tBase.Name);

            Type tUnderlyingSystem = t.UnderlyingSystemType;

            if (tUnderlyingSystem != null)
            {
                OutputText.AppendLine("UnderlyingSystem Type: " + tUnderlyingSystem.Name);
            }

            //properties retrieve boolean         
            OutputText.AppendLine("Is Abstract Class: " + t.IsAbstract);
            OutputText.AppendLine("Is an Arry: " + t.IsArray);
            OutputText.AppendLine("Is a Class: " + t.IsClass);
            OutputText.AppendLine("Is a COM Object : " + t.IsCOMObject);

            return OutputText.ToString();
        }


        /// <summary>
        /// http://stackoverflow.com/questions/20350397/how-can-i-tell-if-a-c-sharp-method-is-async-await-via-reflection
        /// </summary>
        /// <param name="classType"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public static bool IsAsyncMethod(this Type classType, string methodName)
        {
            // Obtain the method with the specified name.
            MethodInfo method = classType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            Type attType = typeof(AsyncStateMachineAttribute);

            // Obtain the custom attribute for the method. 
            // The value returned contains the StateMachineType property. 
            // Null is returned if the attribute isn't present for the method. 
            var attrib = (AsyncStateMachineAttribute)method.GetCustomAttribute(attType);

            return (attrib != null);
        }


        static public void Check()
        {
            Type t = Type.GetType("Dual.Common.Core.Reflection");
            Debug.Assert(t.FullName == "Dual.Common.Core.Reflection");

            t = typeof(Reflection);
            Debug.Assert(t.FullName == "Dual.Common.Core.Reflection");

            //t = typeof (Form);

            //Debug.WriteLine(Reflection.GetTypeInfo(t));

            //Debug.WriteLine("------ Methods ------");
            //foreach (string mi in Reflection.GetMethods(t, BindingFlags.Public | BindingFlags.Static))
            //    Debug.WriteLine(mi);

            //Debug.WriteLine("------ Members ------");
            //foreach (string mi in Reflection.GetMembers(t))
            //    Debug.WriteLine(mi);

            //Debug.WriteLine("------ Fields ------");
            //foreach (string mi in Reflection.GetFields(t))
            //    Debug.WriteLine(mi);

            //Debug.WriteLine("------ Properties ------");
            //foreach (string mi in Reflection.GetProperties(t))
            //    Debug.WriteLine(mi);

            //Debug.WriteLine("------ Interfaces ------");
            //foreach (string mi in Reflection.GetInterfaces(t))
            //    Debug.WriteLine(mi);

            //Debug.WriteLine("------ Constructors ------");
            //foreach (string mi in Reflection.GetConstructors(t))
            //    Debug.WriteLine(mi);


            ///*
            // * Assembly
            // */
            //// You must supply a valid fully qualified assembly name here.
            ////Assembly objAssembly = Assembly.Load("mscorlib,2.0.0.0,Neutral,b77a5c561934e089");

            //// Loads an assembly using its file name   
            ////Assembly objAssembly = Assembly.LoadFrom(@"C:\Windows\Microsoft.NET\Framework\v1.1.4322\caspol.exe");

            ////this loads currnly running process assembly 
            //Assembly objAssembly = Assembly.GetExecutingAssembly();

            //DEBUG.WriteLine("------ GetCustomAttributes in Assembly ------");
            //foreach (string mi in Reflection.GetCustomAttributes(objAssembly))
            //    DEBUG.WriteLine(mi);

            //try
            //{
            //    DEBUG.WriteLine("------ Types in Assembly ------");
            //    foreach (string mi in Reflection.GetTypes(objAssembly))
            //    {
            //        Debug.Assert(!String.IsNullOrEmpty(mi));
            //        if (mi == "<GetIncomingNodes>d__3b")
            //            DEBUG.WriteLine("");
            //        DEBUG.WriteLine(mi);
            //    }
            //}
            //catch (System.Exception ex)
            //{
            //    DEBUG.WriteLine("Exception : {0}\n{1}", ex.Message, ex.StackTrace);            	
            //}


            //DEBUG.WriteLine("");
        }
    }
}