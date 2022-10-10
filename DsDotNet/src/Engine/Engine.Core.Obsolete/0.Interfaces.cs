namespace Engine.Core.Obsolete;


public interface IVertex : IBit { }
public interface IEdge : IBit { }

/// <summary> Segment or Call Base </summary>
public interface ICoin : IVertex
{
    //IWallet Wallet { get; }
}
/// <summary> Coin container.  Segment or Flow base interface </summary>
public interface IWallet
{
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
    bool Value { get; }
    Cpu Cpu { get; set; }
}

public interface IBitReadable : IBit { }
public interface IBitWritable : IBit
{
    void SetValue(bool newValue);
}
public interface IBitReadWritable : IBitReadable, IBitWritable { }



public interface IAutoTag { }
public interface IStrongEdge : IEdge { }
public interface IWeakEdge : IEdge { }
public interface ISetEdge : IEdge { }
public interface IResetEdge : IEdge { }

public interface ICpu { }
public interface IEngine { }
public interface IParserObject
{
    IEnumerable<IParserObject> SpitParserObjects();
    string[] NameComponents { get; }
}
