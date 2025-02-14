// http://www.codeproject.com/Articles/17450/Marshal-an-Array-of-Zero-Terminated-Strings-or-Str

using System;
using System.Runtime.InteropServices;

namespace Dual.Common.Core
{
    public static class Marshaler
    {
        public static IntPtr StringArrayToIntPtr<GenChar>(string[] InputStrArray) where GenChar : struct
        {
            int size = InputStrArray.Length;

            //build array of pointers to string
            IntPtr[] InPointers = new IntPtr[size];
            int dim = IntPtr.Size * size;
            IntPtr rRoot = Marshal.AllocCoTaskMem(dim);
            for (int i = 0; i < size; i++)
            {
                if (typeof(GenChar) == typeof(char))
                    InPointers[i] = Marshal.StringToCoTaskMemUni(InputStrArray[i]);
                else if (typeof(GenChar) == typeof(byte))
                    InPointers[i] = Marshal.StringToCoTaskMemAnsi(InputStrArray[i]);
                else if (typeof(GenChar) == typeof(IntPtr))     //assume BSTR for IntPtr param
                    InPointers[i] = Marshal.StringToBSTR(InputStrArray[i]);
            }

            //copy the array of pointers
            Marshal.Copy(InPointers, 0, rRoot, size);
            return rRoot;
        } 

        public static string[] IntPtrToStringArray<GenChar>(int size, IntPtr rRoot) where GenChar : struct
        {

            //get the output array of pointers
            IntPtr[] OutPointers = new IntPtr[size];
            Marshal.Copy(rRoot, OutPointers, 0, size);
            string[] OutputStrArray = new string[size];
            for (int i = 0; i < size; i++)
            {
                if (typeof(GenChar) == typeof(char))
                    OutputStrArray[i] = Marshal.PtrToStringUni(OutPointers[i]);
                else if (typeof(GenChar) == typeof(byte))
                    OutputStrArray[i] = Marshal.PtrToStringAnsi(OutPointers[i]);
                else if (typeof(GenChar) == typeof(IntPtr))//assune BSTR for IntPtr param
                    OutputStrArray[i] = Marshal.PtrToStringBSTR(OutPointers[i]);

                //dispose of unneeded memory
                Marshal.FreeCoTaskMem(OutPointers[i]);
            }

            //dispose of the pointers array
            Marshal.FreeCoTaskMem(rRoot);
            return OutputStrArray;
        }


        public static IntPtr IntPtrFromStuctArray<T>(T[] InputArray) where T : new()
        {
            int size = InputArray.Length;
            T[] resArray = new T[size];
            int dim = IntPtr.Size * size;
            IntPtr rRoot = Marshal.AllocCoTaskMem(Marshal.SizeOf(InputArray[0])* size);

            for (int i = 0; i < size; i++)
            {
                Marshal.StructureToPtr(InputArray[i], (IntPtr)(rRoot.ToInt32() + i* Marshal.SizeOf(InputArray[i])), false);
            }

            return rRoot;
        }

        public static T[] StructArrayFromIntPtr<T>(IntPtr outArray, int size) where T : new()
        {

            T[] resArray = new T[size];
            IntPtr current = outArray;
            for (int i = 0; i < size; i++)
            {
                resArray[i] = new T();
                Marshal.PtrToStructure(current, resArray[i]);
                Marshal.DestroyStructure(current, typeof(T));
                int structsize = Marshal.SizeOf(resArray[i]);
                current = (IntPtr)((long)current + structsize);
            }

            Marshal.FreeCoTaskMem(outArray);
            return resArray;
        }      
    }
}
