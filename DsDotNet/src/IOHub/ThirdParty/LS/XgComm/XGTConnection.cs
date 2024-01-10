﻿using XGTComm;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using XGCommLib;
using System.Collections.Generic;
using System.Drawing;
namespace XGTComm
{
	/// <summary>
	/// The XGTConnection class manages the connection to XGT devices, providing functionalities to connect, disconnect, read, and handle device data.
	/// </summary>
	public class XGTConnection
    {

        private readonly int tryCnt = 1;

		/// <summary>
		/// Tries to execute a given function with a specified number of attempts. It's a generic method used for retry logic.
		/// </summary>
		/// <param name="budget">The number of attempts to try the function.</param>
		/// <param name="f">The function to try.</param>
		/// <param name="arg">The argument to pass to the function.</param>
		/// <param name="name">The name of the operation for logging purposes.</param>
		/// <returns>True if the function succeeds within the given attempts, else throws an exception.</returns>
		private bool tryFunction<T>(int budget, Func<T, int> f, T arg, string name)
        {
            if (budget == 0)
            {
                throw new Exception($"connection failed: {name}");
            }
            else
            {
                int result = f(arg);
                if (result != 1)
                {
                    Console.WriteLine($"Retrying {name} with remaining {budget}");
                    Thread.Sleep(200);
                    return tryFunction(budget - 1, f, arg, name);
                }
                else
                {
                    return true;
                }
            }
        }
        /// <summary>
        /// Constructor for XGTConnection. Initializes the connection with the specified connection string.
        /// </summary>
        /// <param name="connStr">The connection string for the XGT device.</param>
        public XGTConnection(string connStr, bool autoConn = false)
        {
            ConnStr = connStr;

            if (autoConn) { Connect(); }
        }

        public string ConnStr { get; }

        public CommObject20 CommObject { get; private set; }
        public CommObjectFactory20 Factory { get; private set; }
        public DeviceInfo CreateDevice(char device, char memType, int size, int offset) => CreateDeviceWapper( device,  memType,  size,  offset);

        private const int longSize = 8; // 8 bytes
        private const int MAX_RANDOM_READ_POINTS = 64;
        private const int MAX_RANDOM_WRITE_POINTS = 64;
		/// <summary>
		/// Establishes a connection to the XGT device.
		/// </summary>
		/// <returns>True if the connection is successful, false otherwise.</returns>
		public bool Connect()
        {
            Type t = Type.GetTypeFromCLSID(new Guid("7BBF93C0-7C64-4205-A2B0-45D4BD1F51DC")); // CommObjectFactory
            Factory = Activator.CreateInstance(t) as CommObjectFactory20;
            CommObject = Factory.GetMLDPCommObject20(ConnStr);
            bool isConnected = tryFunction(tryCnt, x => CommObject.Connect(""), "", ConnStr);
            Thread.Sleep(100);
            return isConnected;
        }
        public bool IsConnected => CommObject != null && CommObject.IsConnected() == 1;
		/// <summary>
		/// Checks and re-establishes the connection to the XGT device if it's not connected.
		/// </summary>
		/// <returns>True if the connection is successfully re-established, false otherwise.</returns>
		public bool CheckConnect()
        {
            bool isCn = false;
            if (CommObject.IsConnected() != 1)
            {
                isCn = tryFunction(tryCnt, x => CommObject.Connect(""), "", "Connecting");
            }
            return isCn;
        }
		/// <summary>
		/// Disconnects the current connection to the XGT device.
		/// </summary>
		public void Disconnect()
        {
            _ = CommObject.Disconnect();
        }
		/// <summary>
		/// Re-establishes the connection to the XGT device.
		/// </summary>
		public void ReConnect()
        {
            Disconnect();
            _ = Connect();
        }

		/// <summary>
		/// Creates a device information object for communication with the XGT device.
		/// </summary>
		/// <param name="device">The device type character.</param>
		/// <param name="memType">The memory type character.</param>
		/// <param name="size">The size of the device.</param>
		/// <param name="offset">The offset for the device.</param>
		/// <returns>A DeviceInfo object representing the device.</returns>
		private DeviceInfo CreateDeviceWapper(char device, char memType, int size, int offset)
        {
            //if (size < 1)
            //    throw new Exception($"device {device} is size error : current {size}");
            DeviceInfo di = Factory.CreateDevice();
            di.ucDeviceType = (byte)device;
            di.ucDataType = (byte)memType;
            di.lSize = size;
            di.lOffset = offset;
            return di;
        }
		/// <summary>
		/// Reads the values from the specified XGT devices.
		/// </summary>
		/// <param name="xgtDevices">An array of XGTDevice objects to read.</param>
		/// <returns>True if the read operation is successful, false otherwise.</returns>
		public bool ReadRandomDevice(XGTDevice[] xgtDevices)
        {
            if (xgtDevices.Count() > MAX_RANDOM_READ_POINTS)
                throw new Exception($"MAX_RANDOM_READ_POINTS is {MAX_RANDOM_READ_POINTS} : current {xgtDevices.Count()}");

            var devInfos = xgtDevices.Select(s => CreateDevice(s.Device, s.MemType, s.Size, s.Offset)).ToList();

            devInfos.ForEach(f => CommObject.AddDeviceInfo(f));

            byte[] buf = new byte[MAX_RANDOM_READ_POINTS * longSize];

            if (ReadRandomDevice(buf, xgtDevices.Select(s => s.ToText())))
            {
                int i = 0;
                foreach (var item in xgtDevices)
                {
                    switch (item)
                    {
                        case XGTDeviceBit xgtBit:
                            xgtBit.Value = Convert.ToBoolean(buf[i++]);
                            break;
                        case XGTDeviceByte xgtByte:
                            xgtByte.Value = buf[i];
                            i += 1;
                            break;
                        case XGTDeviceWord xgtWord:
                            xgtWord.Value = BitConverter.ToUInt16(buf, i);
                            i += 2;
                            break;
                        case XGTDeviceDWord xgtDWord:
                            xgtDWord.Value = BitConverter.ToUInt32(buf, i);
                            i += 4;
                            break;
                        case XGTDeviceLWord xgtLDWord:
                            xgtLDWord.Value = BitConverter.ToUInt64(buf, i);
                            i += 8;
                            break;
                        default:
                            // Handle unknown device type or raise an exception if needed
                            break;
                    }
                }

                return true;
            }
            else
                return false;
        }

		/// <summary>
		/// Reads random devices' data into a buffer.
		/// </summary>
		/// <param name="buf">The buffer to store the read data.</param>
		/// <param name="names">The names of the devices to be read.</param>
		/// <returns>True if the read operation is successful, false otherwise.</returns>
		private bool ReadRandomDevice(byte[] buf, IEnumerable<string> names)
        {
            if (CommObject.ReadRandomDevice(buf) != 1)
            {
                throw new Exception($"ReadRandomDevice ERROR {String.Join(", ", names)}");
            }
            CommObject.RemoveAll();
            return true;
        }
        private bool WriteRandomDevice(byte[] buf, IEnumerable<string> names)
        {
            if (CommObject.WriteRandomDevice(buf) != 1)
            {
                throw new Exception($"WriteRandomDevice ERROR {String.Join(", ", names)}");
            }
            CommObject.RemoveAll();
            return true;
        }

        /// <summary>
        /// Reads the values from the specified XGT devices.
        /// </summary>
        /// <param name="xgtDevices">An array of XGTDevice objects to read.</param>
        /// <returns>True if the read operation is successful, false otherwise.</returns>
        public bool WriteRandomDevice(XGTDevice[] xgtDevices)
        {
            if (xgtDevices.Count() > MAX_RANDOM_WRITE_POINTS)
                throw new Exception($"MAX_RANDOM_WRITE_POINTS is {MAX_RANDOM_WRITE_POINTS} : current {xgtDevices.Count()}");

            var devInfos = xgtDevices.Select(s => CreateDevice(s.Device, s.MemType, s.Size, s.Offset)).ToList();
            devInfos.ForEach(f => CommObject.AddDeviceInfo(f));


            byte[] buf = new byte[MAX_RANDOM_WRITE_POINTS * longSize];
            int iWrite = 0;
            foreach (var item in xgtDevices)
            {
                switch (item)
                {
                    case XGTDeviceBit xgtBit:
                        buf[iWrite++] = Convert.ToByte(xgtBit.Value);
                        break;
                    case XGTDeviceByte xgtByte:
                        buf[iWrite] = xgtByte.Value;
                        iWrite += 1;
                        break;
                    case XGTDeviceWord xgtWord:
                        var wordSize = 2;
                        for (int iOffset = iWrite; iOffset < iWrite + wordSize; iOffset++)
                            buf[iOffset] = BitConverter.GetBytes(xgtWord.Value)[iOffset - iWrite];

                        iWrite += wordSize;
                        break;
                    case XGTDeviceDWord xgtDWord:
                        var dwordSize = 4;
                        for (int iOffset = iWrite; iOffset < iWrite + dwordSize; iOffset++)
                            buf[iOffset] = BitConverter.GetBytes(xgtDWord.Value)[iOffset- iWrite];

                        iWrite += dwordSize;
                        break;
                    case XGTDeviceLWord xgtLDWord:
                        var lwordSize = 8;
                        for (int iOffset = iWrite; iOffset < iWrite + lwordSize; iOffset++)
                            buf[iOffset] = BitConverter.GetBytes(xgtLDWord.Value)[iOffset- iWrite];

                        iWrite += lwordSize;
                        break;
                    default:
                        // Handle unknown device type or raise an exception if needed
                        break;
                }
            }


            return WriteRandomDevice(buf.Take(iWrite).ToArray(), xgtDevices.Select(s => s.ToText()));
        }
    }
}