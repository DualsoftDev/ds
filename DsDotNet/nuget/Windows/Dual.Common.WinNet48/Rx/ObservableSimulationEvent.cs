using System.Runtime.InteropServices;

namespace Dsu.Common.Utilities
{

    /// <summary> Observable Simulation Event 에 대한 base interface </summary>
    [ComVisible(false)]
    public interface IObservableSimulationEvent : IObservableEvent { }

    public enum SimulationEventType
    {
        Starting,
        Started,
        Stopping,
        Stopped,
        Paused,
        Resumming,
        Resumed,
        Finished
    }

    /// <summary> Simulation event </summary>
    [ComVisible(false)]
    public class ObservableSimulationEvent : IObservableSimulationEvent
    {
        public SimulationEventType Type { get; private set; }
        public ObservableSimulationEvent(SimulationEventType type) { Type = type; }
    }
}
