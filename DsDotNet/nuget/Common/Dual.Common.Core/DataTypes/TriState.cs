using System;
using System.Runtime.InteropServices;

/*
 * http://dotnetframework.org/default.aspx/DotNET/DotNET/8@0/untmp/whidbey/REDBITS/ndp/fx/src/CommonUI/System/Drawing/Printing/TriState@cs/1/TriState@cs
 */
namespace Dual.Common.Core
{
    [Serializable]
    public struct TriState
    {
        private byte value; // 0 is "default", not false

        public static readonly TriState Default = new TriState(0);
        public static readonly TriState False = new TriState(1);
        public static readonly TriState True = new TriState(2);

        private TriState(byte value)
        {
            this.value = value;
        }

        public bool IsDefault
        {
            get { return this == Default; }
        }

        public bool IsFalse
        {
            get { return this == False; }
        }

        public bool IsNotDefault
        {
            get { return this != Default; }
        }

        public bool IsTrue
        {
            get { return this == True; }
        }

        public static bool operator ==(TriState left, TriState right)
        {
            return left.value == right.value;
        }

        public static bool operator !=(TriState left, TriState right)
        {
            return !(left == right);
        }

        public override bool Equals(object o)
        {
            TriState state = (TriState)o;
            return this.value == state.value;
        }

        public override int GetHashCode()
        {
            return value;
        }

        public static implicit operator TriState(bool value)
        {
            return (value) ? True : False;
        }

        public static explicit operator bool(TriState value)
        {
            if (value.IsDefault)
                throw new InvalidCastException("TriState Compare Error");
            else
                return (value == True);
        }

        /// <internalonly>
        /// <devdoc>
        ///    <para>
        ///       Provides some interesting information about the TriState in
        ///       String form.
        ///    </para>
        /// </devdoc>
        /// </internalonly>
        public override string ToString()
        {
            if (this == Default)
                return "Default";

            if (this == False)
                return "False";

            return "True";
        }
    }
}
