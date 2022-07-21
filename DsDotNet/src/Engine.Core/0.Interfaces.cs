global using System;
global using System.Collections.Generic;
global using System.Reactive.Subjects;
global using System.Diagnostics;
global using System.Linq;

global using log4net;
global using TagDic = System.Collections.Generic.Dictionary<string, Engine.Core.Tag>;
global using Engine.Common;



namespace Engine.Core;

public enum Status4
{
    Ready = 0,
    Going,
    Finished,
    Homing
}

//public interface IWithRGFH
//{
//    Status4 RGFH { get; set; }
//    bool ChangeR();
//    bool ChangeG();
//    bool ChangeF();
//    bool ChangeH();
//}

public interface IWithSREPorts
{
    PortS PortS { get; set; }
    PortR PortR { get; set; }
    PortE PortE { get; set; }
}


public interface IVertex : IBit { }
public interface IEdge : IBit { }

/// <summary> Segment or Call Base </summary>
public interface ICoin : IVertex {
    //IWallet Wallet { get; }
}
/// <summary> Coin container.  Segment or Flow base interface </summary>
public interface IWallet {
    //IEnumerable<ICoin> Coins { get; }
}
public interface IAlias : INamed { }

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
public interface IStrongEdge : IEdge { }
public interface IWeakEdge : IEdge { }
public interface ISetEdge : IEdge { }
public interface IResetEdge : IEdge { }

public interface ICpu {}
public interface IEngine {}

public static class Global
{
    public static ILog Logger { get; set; }
    public static Subject<BitChange> BitChangedSubject { get; } = new Subject<BitChange>();
}