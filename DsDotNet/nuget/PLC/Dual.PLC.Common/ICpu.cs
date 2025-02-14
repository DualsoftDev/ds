namespace Dual.PLC.Common
{
    public interface ICpu
    {
        /// <summary> PLC cpu model </summary>
        string Model { get; }

        /// <summary> True if PLC is RUNNING state. </summary>
        bool IsRunning { get; }

        /// <summary> Stop the PLC </summary>
        void Stop();

        /// <summary> Start the PLC </summary>
        void Run();
    }

    public class UnidentifiedCpu : ICpu
    {
        public string Model => "Unidentified";

        public bool IsRunning => false;

        public void Run()
        {
            throw new System.NotImplementedException();
        }

        public void Stop()
        {
            throw new System.NotImplementedException();
        }

        static private UnidentifiedCpu _instance = new UnidentifiedCpu();
        public static UnidentifiedCpu Instance = _instance;
    }
}
