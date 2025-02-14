using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dual.PLC.Common
{
    public enum TagType
    {
        Bit,
        I1,
        I2,
        I4,
        I8,
        F4,
    }

    /// <summary>
    /// PLC 접점.  AB 의 tag or MELSEC 의 device 에 해당하는 개념
    /// </summary>
    public interface ITag
    {
        object Value { get; set; }
        string Name { get; set; }       // address.  e.g "Y2", "I3.1", "Array1[0,0,0]", ...
        TagType Type { get; }
        DateTime Timestamp { get; }
    }
}
