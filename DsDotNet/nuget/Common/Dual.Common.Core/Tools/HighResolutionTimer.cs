using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dual.Common.Core
{
    public class HighResolutionTimer
    {
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        private long startTime, stopTime;
        private long freq;

        // Constructor
        public HighResolutionTimer()
        {
            startTime = 0;
            stopTime = 0;

            if (QueryPerformanceFrequency(out freq) == false)
            {
                // high-performance counter not supported
                throw new Exception("HighResolutionTimer not supported.");
            }
        }

        // Start the timer
        public void Start()
        {
            // lets do the waiting threads there work
            System.Threading.Thread.Sleep(0);

            QueryPerformanceCounter(out startTime);
        }

        // Stop the timer
        public void Stop()
        {
            QueryPerformanceCounter(out stopTime);
        }

        // Returns the duration of the timer (in seconds)
        public double Duration => (stopTime - startTime) / (double)freq;
        public double TotalMilliseconds => Duration * 1000.0;
        public double TotalMicroseconds => TotalMilliseconds * 1000.0;
        public double TotalNanoseconds => TotalMicroseconds * 1000.0;

        public double ElapsedMilliseconds => TotalMilliseconds;
        public double ElapsedMicroseconds => TotalMicroseconds;
        public double ElapsedNanoseconds => TotalNanoseconds;
    }
}
