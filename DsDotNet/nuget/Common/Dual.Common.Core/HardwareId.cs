//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Management;
//using System.Net.NetworkInformation;
//using Dual.Common.Core.ExtensionMethods;

//namespace Dual.Common.Core
//{
//    public class HardDriveInfo
//    {
//        public string Model { get; set; }
//        public string Type { get; set; }
//        public string SerialNo { get; set; }
//    }


//    /// <summary>
//    /// Hardware id
//    ///
//    /// </summary>
//    public static class HardwareId
//    {
//        /// <summary>
//        /// Get CPU id.  http://www.vcskicks.com/hardware_id.php.  e.g "BFEBFBFF000506E3"
//        /// </summary>
//        /// <returns></returns>
//        public static string GetCpuId()
//        {
//            string cpuInfo = string.Empty;
//            ManagementClass mc = new ManagementClass("win32_processor");
//            ManagementObjectCollection moc = mc.GetInstances();

//            foreach (ManagementObject mo in moc)
//            {
//                if (cpuInfo == "")
//                {
//                    //Get only the first CPU's ID
//                    cpuInfo = mo.Properties["processorID"].Value.ToString();
//                    break;
//                }
//            }
//            return cpuInfo;
//        }


//        public static string GetHddVolumnSerial(string drive = "C")
//        {
//            drive = drive.Replace("\\", "").Replace(":", "");
//            ManagementObject dsk = new ManagementObject($"win32_logicaldisk.deviceid=\"{drive}:\"");
//            dsk.Get();
//            string volumeSerial = dsk["VolumeSerialNumber"].ToString();
//            return volumeSerial;
//        }

//        /// <summary>
//        /// MAC address.  e.g "D05099A31ABD"
//        /// </summary>
//        /// <returns></returns>
//        public static string GetMacAddress()
//        {
//            string macAddresses = "";

//            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
//            {
//                if (nic.OperationalStatus == OperationalStatus.Up)
//                {
//                    macAddresses += nic.GetPhysicalAddress().ToString();
//                    break;
//                }
//            }
//            return macAddresses;
//        }



//        /// <summary>
//        /// https://www.codeproject.com/articles/6077/how-to-retrieve-the-real-hard-drive-serial-number
//        /// E.g
//        ///     Model		: Samsun SSD 850 EVO 250G SCSI Disk Device
//        ///     Type		: IDE
//        ///     Serial No.  : S2R8NX0H401487R
//        /// </summary>
//        public static IEnumerable<HardDriveInfo> GetHddInfos()
//        {
//            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");

//            foreach (ManagementObject wmi_HD in searcher.Get())
//            {
//                var hd = new HardDriveInfo()
//                {
//                    Model = wmi_HD["Model"].ToString(),
//                    Type = wmi_HD["InterfaceType"].ToString(),
//                    SerialNo = wmi_HD["SerialNumber"].ToString(),
//                };

//                yield return hd;
//            }
//        }

//        private static void WriteLine(string msg)
//        {
//            Trace.WriteLine(msg);
//            Console.WriteLine(msg);
//        }

//        public static void ShowHardwareInformation()
//        {
//            WriteLine("CPU id : " + HardwareId.GetCpuId());
//            System.IO.DriveInfo.GetDrives()
//                .Where(di => di.DriveType == System.IO.DriveType.Fixed && di.IsReady)       // fixed disk 에 format 되어 사용가능한 drive
//                .Select(di => di.Name)
//                .Select(d => Tuple.Create(d, HardwareId.GetHddVolumnSerial(d)))
//                .ForEach(tpl =>
//                {
//                    var volumn = tpl.Item1;
//                    var serial = tpl.Item2;
//                    WriteLine($"HDD Serial {volumn}: = {serial}");
//                });
//            WriteLine("MAC Address : " + HardwareId.GetMacAddress());

//            GetHddInfos().ForEach(hd =>
//            {
//                WriteLine("Model\t\t: " + hd.Model);
//                WriteLine("Type\t\t: " + hd.Type);
//                WriteLine("Serial No.\t: " + hd.SerialNo);
//                WriteLine("");
//            });
//        }
//    }
//}
