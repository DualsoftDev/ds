using Dual.PLC.TagParser.FS;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static XGTComm.XGTDevice;
namespace XGTComm
{
    /// <summary>
    /// The XGTParserXGI class provides utility functions for parsing XGI tags and extracting information about XGT devices.
    /// </summary>
    public static class XGTParserXGI
    {
        static Dictionary<int, XGTDeviceSize> dicDeviceSize = new()
        {
            { 1,  XGTDeviceSize.Bit },
            { 8,  XGTDeviceSize.Byte },
            { 16, XGTDeviceSize.Word },
            { 32, XGTDeviceSize.DWord },
            { 64, XGTDeviceSize.LWord },
        };

        /// <summary>
        /// Parses the given tag string and returns a tuple containing device, size, and bit offset information.
        /// </summary>
        /// <param name="name">The tag name to parse.</param>
        /// <returns>A tuple with the device identifier, the data type size, and the bit offset.</returns>
        public static Tuple<string, XGTDeviceSize, int> LsTagXGIPattern(string tag)
        {
            var result = LsXgiTagParser.Parse(tag);
            if (result == null)
            {
                Console.WriteLine($"Failed to XGK parse tag : {tag}");
                return null;
            }

            (string device, int dataSize, int totalBitOffset) = result;
            return XGTParserUtil.CreateTagInfo(tag, device, dicDeviceSize[dataSize], totalBitOffset);
        }
    }
}