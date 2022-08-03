using System.Threading.Tasks;

namespace Engine.Core;

[DebuggerDisplay("{ToText()}")]
public abstract class Bit : Named, IBit
{
    protected bool _value;
    public virtual bool Value
    {
        get => _value;
        set
        {
            Debug.Assert(this is IBitWritable);
            if (_value != value)
            {
                _value = value;
                BitChange.Publish(this, value, true);
            }

        }
    }

    public Cpu Cpu { get; set; }
    public Bit(Cpu cpu, string name, bool bit = false) : base(name)
    {
        Debug.Assert(cpu != null);

        Value = bit;
        Cpu = cpu;
        cpu.BitsMap.Add(name, this);
    }

    /// <summary>  Bit 생성 이전에 동일 이름이 존재하는지 check 하기 위한 용도. </summary>
    public static T GetExistingBit<T>(Cpu cpu, string name) where T: Bit
    {
        if (cpu.BitsMap.ContainsKey(name))
        {
            var existing = cpu.BitsMap[name];
            if (existing is T)
                return existing as T;
            else
                throw new Exception($"ERROR: duplicate name {name} exists with other type {existing.GetType().Name}");
        }
        return null;
    }

    // Value setter 를 수행하지 않기 위한 생성자
    protected Bit(string name, Cpu cpu) : base(name)
    {
        Debug.Assert(cpu != null);
        Cpu = cpu;
        cpu.BitsMap.Add(name, this);
    }
    // null cpu 를 허용하기 위한 생성자.  OpcTag 만 cpu null 허용
    internal Bit(string name, bool bit = false) : base(name)
    {
        Value = bit;
        Debug.Assert(GetType().Name.Contains("OpcTag"));
    }


    public override string ToText() => BitExtension.ToText(this);
}


/// <summary> 다른 bit 요소(monitoringBits)에 의해서 값이 변경될 수 있는 bit 에 대한 추상 class </summary>
public abstract class BitReEvaluatable : Bit, IBitReadable
{
    internal IBit[] _monitoringBits;
    protected abstract void ReEvaulate(IBit causeBit);
    public override bool Value { set => throw new DsException("Not Supported."); }
    IDisposable _subscription;
    protected BitReEvaluatable(Cpu cpu, string name, params IBit[] monitoringBits)
        : base(name, cpu)
    {
        // PortExpression 의 경우, plan 대비 actual 에 null 을 허용
        _monitoringBits = monitoringBits.Where(b => b is not null).ToArray();

        ReSubscribe();
    }

    protected void ReSubscribe()
    {
        _subscription?.Dispose();
        _subscription =
            Global.RawBitChangedSubject
                .Select(bc => bc.Bit)
                .Where(bit => _monitoringBits.Contains(bit))
                .Subscribe(ReEvaulate)
                ;
    }
}



public class Flag : Bit, IBitReadWritable
{
    public Flag(Cpu cpu, string name, bool bit = false) : base(cpu, name, bit) { }
}



[DebuggerDisplay("{QualifiedName}")]
[Obsolete("PortExpression 으로 대체 예정")]
public abstract class Port : Bit, IBitReadWritable
{
    public Segment OwnerSegment { get; set; }
    public Port(Segment ownerSegment, string name)
        : base(ownerSegment.Cpu, $"{ownerSegment.QualifiedName}_{name}")
    {
        OwnerSegment = ownerSegment;
    }
    public string QualifiedName => $"{OwnerSegment.QualifiedName}.{GetType().Name}";
    public override string ToString() => $"{QualifiedName}[{this.GetType().Name}]@{Cpu.Name}={Value}";
}
public class PortS : Port
{
    public PortS(Segment ownerSegment) : base(ownerSegment, "PortS") {}
}
public class PortR : Port
{
    public PortR(Segment ownerSegment) : base(ownerSegment, "PortR") {}
}
public class PortE : Port
{
    public PortE(Segment ownerSegment) : base(ownerSegment, "PortE") {}
    public override bool Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                OwnerSegment.TagsEnd.Iter(et => et.Value = value);
            }
        }
    }
}


//public class Relay : Bit { }
//public class WeakRelay : Relay { }
//public class StrongRelay : Relay { }


// class or record?
public class BitChange
{
    public IBit Bit { get; }
    public bool NewValue { get; }
    public IBit Cause { get; }
    public bool Applied { get; internal set; }
    public DateTime Time { get; }
    public BitChange(IBit bit, bool newValue, bool applied= false, IBit cause = null)
    {
        Bit = bit;
        NewValue = newValue;
        Applied = applied;
        Time = DateTime.Now;
        Cause = cause;
    }

    public static ConcurrentHashSet<Task> PendingTasks = new();

    public static void Publish(IBit bit, bool newValue, bool applied, IBit cause = null)
    {
        (new BitChange(bit, newValue, applied, cause)).Publish();
    }
    public void Publish()
    {
        if (Global.IsSupportParallel)
        {
            //! 현재값 publish 를 threading 으로 처리...
            var capturedThis = this;
            var task = new Task(() =>
            {
                Global.RawBitChangedSubject.OnNext(capturedThis);
            });
            PendingTasks.Add(task);
            task.ContinueWith(t => PendingTasks.TryRemove(t, out Task _task));
            task.Start();
        }
        else
            Global.RawBitChangedSubject.OnNext(this);
    }

    public override string ToString() => $"{Bit}={NewValue}";
}

public record OpcTagChange
{
    public string TagName { get; }
    public bool Value { get; }
    public OpcTagChange(string tagName, bool value)
    {
        TagName = tagName;
        Value = value;
    }
}
