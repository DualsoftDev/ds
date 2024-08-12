using System;
using System.Linq;
using System.Text.RegularExpressions;
using static XGTComm.XGTDevice;

namespace XGTComm
{
    /// <summary>
    /// The XGTParserUtil class provides utility functions for parsing and handling XGT device data.
    /// </summary>
    public static class XGTParserUtil
    {
        /// <summary>
        /// Creates and returns tag information for an XGT device.
        /// </summary>
        /// <param name="tag">The tag name of the device.</param>
        /// <param name="device">The device identifier.</param>
        /// <param name="dataType">The data type of the device (Bit, Byte, Word, etc.).</param>
        /// <param name="totalBitOffset">The total bit offset for the device.</param>
        /// <returns>A tuple containing the device, data type, and total bit offset.</returns>
        public static Tuple<string, XGTDeviceSize, int> CreateTagInfo(string tag, string device, XGTDeviceSize dataType, int totalBitOffset)
        {
            return Tuple.Create(device, dataType, totalBitOffset);
        }

        /// <summary>
        /// Gets the byte length of a given data type.
        /// </summary>
        /// <param name="dataType">The XGT device data type.</param>
        /// <returns>The byte length corresponding to the data type.</returns>
        public static int GetByteLength(XGTDeviceSize dataType)
        {
            switch (dataType)
            {
                case XGTDeviceSize.Bit:
                    return 1;
                default:
                    return GetBitLength(dataType) / 8;
            }
        }
        /// <summary>
        /// Gets the bit length of a given data type.
        /// </summary>
        /// <param name="dataType">The XGT device data type.</param>
        /// <returns>The bit length corresponding to the data type.</returns>
        public static int GetBitLength(XGTDeviceSize dataType)
        {
            switch (dataType)
            {
                case XGTDeviceSize.Bit:
                    return 1;
                case XGTDeviceSize.Byte:
                    return 8;
                case XGTDeviceSize.Word:
                    return 16;
                case XGTDeviceSize.DWord:
                    return 32;
                case XGTDeviceSize.LWord:
                    return 64;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }
        }

        public static XGTDevice CreateXGTDevice(string devHead, XGTDeviceSize dataType, int bitOffset)
        {
            var head = devHead.ToCharArray().First();
            switch (dataType)
            {
                case XGTDeviceSize.Bit:
                    return new XGTDeviceBit(head, bitOffset);
                case XGTDeviceSize.Byte:
                    return new XGTDeviceByte(head, bitOffset);
                case XGTDeviceSize.Word:
                    return new XGTDeviceWord(head, bitOffset); 
                case XGTDeviceSize.DWord:
                    return new XGTDeviceDWord(head, bitOffset);
                case XGTDeviceSize.LWord:
                    return new XGTDeviceLWord(head, bitOffset);
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }
        }


        public static object GetValueByType(Type dataType, object value)
        {
            if (dataType == typeof(bool))
                return value;
            if (dataType == typeof(byte))
                return Convert.ToByte(value);
            if (dataType == typeof(ushort))
                return Convert.ToUInt16(value);
            if (dataType == typeof(uint))
                return Convert.ToUInt32(value);
            if (dataType == typeof(ulong))
                return Convert.ToUInt64(value);
            if (dataType == typeof(sbyte))
                return Convert.ToSByte(value);
            if (dataType == typeof(short))
                return Convert.ToInt16(value);
            if (dataType == typeof(int))
                return Convert.ToInt32(value);
            if (dataType == typeof(long))
                return Convert.ToInt64(value);
            if (dataType == typeof(float))
                return Convert.ToSingle(value);
            if (dataType == typeof(double))
                return Convert.ToDouble(value);
            if (dataType == typeof(char))
                return Convert.ToChar(value);
            else
                return Convert.ToString(value);
        }

    }
}
