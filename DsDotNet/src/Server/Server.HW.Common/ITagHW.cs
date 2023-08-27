using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.HW.Common
{
    public enum TagType
    {
        Bool,
        Byte,
        Double,
        Int16,
        Int32,
        Int64,
        Sbyte,
        Single,
        Uint16,
        Uint32,
        Uint64,
        String,
    }

    /// <summary>
    /// IO 접점.  
    /// </summary>
    public interface ITagHW
    {
        object Value { get; set; }
        string Name { get; set; }     
        TagType Type { get; }
    }
}
