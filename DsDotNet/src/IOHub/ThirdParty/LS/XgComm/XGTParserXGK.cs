using Dual.PLC.TagParser.FS;

using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text.RegularExpressions;
using static XGTComm.XGTDevice;

namespace XGTComm
{
    /// <summary>
    /// The XGTParserXGK class provides utility functions for parsing XGK tags and extracting information about XGT devices specific to the XGK series.
    /// </summary>
    public static class XGTParserXGK
    {
        /// <summary>
        /// Parses the given XGK tag string and returns a tuple containing device, size, and bit offset information.
        /// The method matches the tag against various regex patterns to extract the relevant information.
        /// </summary>
        /// <param name="tag">The tag name to parse.</param>
        /// <param name="isBit">A boolean indicating if the tag refers to a bit or not.</param>
        /// <returns>A tuple with the device identifier, the data type size, and the bit offset. Returns null if parsing fails.</returns>
        public static Tuple<string, XGTDeviceSize, int> LsTagXGKPattern(string tag, bool isBit)
        {
            tag = tag.ToUpper().Split('[').First(); //ZR123321[Z01]
            var result = LsXgkTagParser.Parse(tag, isBit);
            if (result == null)
            {
                Console.WriteLine($"Failed to XGK parse tag : {tag}");
                return null;
            }

            (string device, int dataSize, int totalBitOffset) = result;
            return XGTParserUtil.CreateTagInfo(tag, device, isBit ? XGTDeviceSize.Bit : XGTDeviceSize.Word, totalBitOffset);
        }
    }
}
