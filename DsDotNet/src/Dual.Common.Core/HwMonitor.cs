using Dsu.Common.Utilities.ExtensionMethods;

using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dual.Common.Core
{
    public class HwMonitor
    {
        // https://stackoverflow.com/questions/105031/how-do-you-get-total-amount-of-ram-the-computer-has
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

        static Lazy<PerformanceCounter[]> _pcs;
        public static async Task<float[]> GetCpuUsagesAsync()
        {
            await Task.Yield();
            if (_pcs == null)
                _pcs = new Lazy<PerformanceCounter[]>(() =>
                {
                    // core 갯수를 구하는데, 상당히 오랜 시간이 걸림... => Lazy 로 저장해 둠.
                    //
                    // https://stackoverflow.com/questions/5537286/how-to-get-cpu-usage-for-more-than-2-cores
                    int coreCount = 0;
                    foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
                    {
                        coreCount += int.Parse(item["NumberOfCores"].ToString());
                    }

                    var pcs =
                        Enumerable.Range(0, coreCount)
                        .Select(n => new PerformanceCounter("Processor", "% Processor Time", n.ToString()))
                        .ToArray()
                        ;
                    pcs.Iter(pc => pc.NextValue());
                    return pcs;
                });

            var cpuPerformaces = _pcs.Value;

            //cpuPerformaces.Iter(pc => pc.NextValue());
            //await Task.Delay(10);
            var usages =
                cpuPerformaces
                .Select(pc => pc.NextValue()).ToArray();

            return usages;
        }

        /// <summary>
        /// MB 단위
        /// </summary>
        public static long GetTotalRAM()
        {
            GetPhysicallyInstalledSystemMemory(out long memKb);
            return memKb / 1024;
        }

        /// <summary>
        /// MB 단위
        /// </summary>
        public static long GetAvaialbleRAM()
        {
            var pc = new PerformanceCounter("Memory", "Available MBytes");
            return (long)pc.NextValue();     //.ToString() + "MB";
        }

        /// <summary>
        /// MHz 단위
        /// </summary>
        public static uint GetCpuClockSpeed()
        {
            var searcher = new ManagementObjectSearcher("select MaxClockSpeed from Win32_Processor").Get();
            return (uint)searcher.ToEnumerable<ManagementBaseObject>().First()["MaxClockSpeed"];
        }



        public static async Task<HardwarePerformance> GetHardwarePerformanceAsync()
        {
            var cpuUsages    = await GetCpuUsagesAsync();
            var cpuSpeed     = GetCpuClockSpeed();
            var ramTotal     = GetTotalRAM();
            var ramAvailable = GetAvaialbleRAM();
            var score        = (int) (cpuUsages.Select(u => 100 - u).Sum() * cpuSpeed / 1000 * ramAvailable / 1024);
            return new HardwarePerformance(cpuSpeed, cpuUsages, ramTotal, ramAvailable, score);
        }

        public static async Task<int> GetTotalScoreAsync()
        {
            var pc = await GetHardwarePerformanceAsync();
            return pc.TotalScore;
        }
    }

    public class HardwarePerformance
    {
        public HardwarePerformance(uint clockSpeed, float[] coreUsages, long rAMTotal, long rAMAvailable, int totalScore)
        {
            Debug.Assert(coreUsages != null);
            TimeStamp      = DateTime.Now;
            ClockSpeedMHz  = clockSpeed;
            CoreUsages     = coreUsages;
            RAMTotalMB     = rAMTotal;
            RAMAvailableMB = rAMAvailable;
            TotalScore     = totalScore;
        }

        /// <summary>
        /// MHz 단위
        /// </summary>
        public uint ClockSpeedMHz { get; set; }
        public float[] CoreUsages { get; set; }
        public float CpuUsage => CoreUsages.Average();
        public long RAMTotalMB { get; set; }
        public long RAMAvailableMB { get; set; }
        public int TotalScore { get; set; }
        public DateTime TimeStamp { get; set; }

        private HardwarePerformance() { }
    }
}
