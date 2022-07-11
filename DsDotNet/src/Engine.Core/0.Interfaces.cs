using log4net;

using System;

namespace Engine.Core
{
    public enum Status4
    {
        Ready = 0,
        Going,
        Finished,
        Homing
    }

    public interface IWithRGFH
    {
        Status4 RGFH { get; set; }
        bool ChangeR();
        bool ChangeG();
        bool ChangeF();
        bool ChangeH();
    }

    public interface IWithSREPorts
    {
        PortS PortS { get; set; }
        PortR PortR { get; set; }
        PortE PortE { get; set; }
    }


    public interface IVertex { }
    public interface IEdge { }

    public interface ISegmentOrCall : IVertex, IBit { }
    public interface ISegmentOrFlow { }

    /// <summary> Call TX or RX </summary>
    public interface ITxRx { }

    public interface INamed
    {
        string Name { get; set; }
    }

    public interface IBit
    {
        bool Value { get; set; }
        CpuBase OwnerCpu { get; set; }
    }

    public interface IAutoTag { }
    public interface IStrong { }
    public interface IReset { }

    public interface ICpu {}
    public interface IEngine {}

    public static class Global
    {
        public static ILog Logger { get; set; }

    }
}