using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
#if ARCH_INTEL
using System.Windows.Forms;
#endif

namespace Dual.Common.Core
{
    public static partial class Tools
    {
        static public void VERIFY(bool b) { Debug.Assert(b, "Verify failed!!"); }

        static public void ShowMessage(string format, params object[] args)
        {
#if ARCH_INTEL
            MessageBox.Show(String.Format(format, args));
#else
            throw new NotImplementedException();
#endif
        }

        static private HashSet<string> m_setstrPrintedMessages = new HashSet<string>();
        static public void ShowMessageOnce(string format, params object[] args)
        {
            string strMessage = String.Format(format, args);
            ShowMessageOnceHelper(strMessage, strMessage);
        }

        static public void ShowMessageOnceWithKey(string key, string format, params object[] args)
        {
            ShowMessageOnceHelper(key, String.Format(format, args));
        }

        static private void ShowMessageOnceHelper(string key, string msg)
        {
            if (!m_setstrPrintedMessages.Contains(key))
            {
                m_setstrPrintedMessages.Add(key);
                ShowMessage(msg + "\r\n\r\nThis message will be displayed just once!");
            }
        }

        static public bool Swap<T>(ref T x, ref T y)
        {
            try
            {
                T t = y;
                y = x;
                x = t;
                return true;
            }
            catch
            {
                return false;
            }
        }

        static public string InputBox(string Prompt, string Title, string DefaultResponse, int XPos, int YPos)
        {
#if ARCH_INTEL
            return Microsoft.VisualBasic.Interaction.InputBox(Prompt, Title, DefaultResponse, XPos, YPos);
#else
            return "";
#endif
        }
        static public string InputBox(string Prompt, string Title, string DefaultResponse)
        {
            return InputBox(Prompt, Title, DefaultResponse, 80, 80);
        }

        static public bool IsNullOrEmpty(object o)
        {
            if (o == null)
                return true;

            string[] astrCheckProp = { "Count", "Length", };
            foreach( string strCheckProp in astrCheckProp )
            {
                if ( RTTI.HasProperty_p(o.GetType(), strCheckProp) )
                {
                    object oValue = o.GetType().GetProperty(strCheckProp).GetValue(o, null);
                    return (int)oValue == 0;
                }
            }

            throw new System.ArgumentException(String.Format("IsNullOrEmpty(): Parameter {0} supports neither \"Count\" nor \"Length\" properties.", o), o.ToString());
        }


        static public bool NoOverlap_p(IComparable a1, IComparable a2, IComparable b1, IComparable b2)
        {
            return max(a1, a2).CompareTo(min(b1, b2)) <= 0 || min(a1, a2).CompareTo(max(b1, b2)) >= 0;
        }

        static public bool Overlap_p(IComparable a1, IComparable a2, IComparable b1, IComparable b2)
        {
            return !NoOverlap_p(a1, a2, b1, b2);
        }

        // a 의 range 가 b 의 range 에 대해서 어떤 상태인지를 return
        static public EMOverlap GetOverlapStatus<T>(T a1, T a2, T b1, T b2) where T : IComparable<T>
        {
            Debug.Assert( a1.CompareTo(a2) <= 0 && b1.CompareTo(b2) <= 0);

            if (NoOverlap_p((IComparable)a1, (IComparable)a2, (IComparable)b1, (IComparable)b2)) return EMOverlap.NoOverlap;

            bool bSameStart = a1.CompareTo(b1) == 0;
            bool bSameEnd = a2.CompareTo(b2) == 0;
            if (bSameStart && bSameEnd)
                return EMOverlap.Equal;
            else if (bSameStart)
                return a2.CompareTo(b2) > 0 ? EMOverlap.Nesting : EMOverlap.Nested;
            else if ( bSameEnd )
                return a1.CompareTo(b1) < 0 ? EMOverlap.Nesting : EMOverlap.Nested;

            if (a1.InClosedRange(b1, b2))
            {
                if (a2.InClosedRange(b1, b2))
                {
                    if ( a1.InOpenRange(b1, b2) && a2.InOpenRange(b1, b2))
                        return EMOverlap.FullyNested;

                    return EMOverlap.Nested;
                }
                else
                    return EMOverlap.Partial;
            }
            else
            {
                Debug.Assert(b1.InClosedRange(a1, a2));
                if (b2.InClosedRange(a1, a2))
                {
                    if (b1.InOpenRange(a1, a2) && b2.InOpenRange(a1, a2))
                        return EMOverlap.FullyNesting;

                    return EMOverlap.Nesting;
                }
                else
                    return EMOverlap.Partial;
            }
        }

        static public Array GetEnumerationArray<T>() { return Enum.GetValues(typeof(T)); }
        static public List<T> GetEnumerationList<T>() { return ToList<T>(GetEnumerationArray<T>()); }
        static public T ParseEnum<T>(string strEnum) { return (T)Enum.Parse(typeof(T), strEnum); }

        static public bool X86_p()  { return !X64_p(); }
        static public bool X64_p() { return Environment.Is64BitProcess; }


        /// <summary>
        /// Expands environment variables and, if unqualified, locates the exe in the working directory
        /// or the evironment's path.
        /// </summary>
        /// <param name="exe">The name of the executable file</param>
        /// <returns>The fully-qualified path to the file</returns>
        /// http://csharptest.net/526/how-to-search-the-environments-path-for-an-exe-or-dll/
        public static string FindExePath(string exe)
        {
            exe = Environment.ExpandEnvironmentVariables(exe);
            if (!File.Exists(exe))
            {
                if (Path.GetDirectoryName(exe) == String.Empty)
                {
                    foreach (string test in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
                    {
                        string path = test.Trim();
                        if (!String.IsNullOrEmpty(path) && File.Exists(path = Path.Combine(path, exe)))
                            return Path.GetFullPath(path);
                    }
                }
                return null;
            }
            return Path.GetFullPath(exe);
        }
    }

    public enum EMOverlap
    {
        NoOverlap = 0,
        Partial = 1,
        FullyNested = 2,        // 완전 부분집합
        Nested = 3,             // 경계면을 공유하면서
        FullyNesting = 4,
        Nesting = 5,
        Equal = 6
    }

    // TIPS : restricting template parameter
    public static class ToolsEx
    {
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }
    }
}
