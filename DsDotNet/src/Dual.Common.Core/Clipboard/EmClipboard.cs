using System;
using System.Runtime.Serialization.Formatters.Binary;

namespace Dsu.Common.Utilities
{
    public static class EmClipboard
    {
        /// <summary>
        /// Check whether given object is serializable or not.
        /// http://www.codeproject.com/Articles/8102/Saving-and-obtaining-custom-objects-to-from-Window
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool IsSerializable(this object obj)
        {
            System.IO.MemoryStream mem = new System.IO.MemoryStream();
            BinaryFormatter bin = new BinaryFormatter();
            try
            {
                bin.Serialize(mem, obj);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Your object cannot be serialized." +
                                 " The reason is: " + ex.ToString());
                return false;
            }
        }
    }
}
