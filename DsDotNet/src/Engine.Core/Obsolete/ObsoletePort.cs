//namespace Engine.Core.Obsolete;
//#if false

//[DebuggerDisplay("{QualifiedName}")]
//[Obsolete("PortInfo 으로 대체 예정")]
//public abstract class Port : Bit, IBitReadWritable
//{
//    public Segment OwnerSegment { get; set; }
//    public Port(Segment ownerSegment, string name)
//        : base(ownerSegment.Cpu, $"{ownerSegment.QualifiedName}_{name}")
//    {
//        OwnerSegment = ownerSegment;
//    }
//    public string QualifiedName => $"{OwnerSegment.QualifiedName}.{GetType().Name}";
//    public override string ToString() => $"{QualifiedName}[{this.GetType().Name}]@{Cpu.Name}={Value}";
//}
//public class PortS : Port
//{
//    public PortS(Segment ownerSegment) : base(ownerSegment, "PortS") { }
//}
//public class PortR : Port
//{
//    public PortR(Segment ownerSegment) : base(ownerSegment, "PortR") { }
//}
//public class PortE : Port
//{
//    public PortE(Segment ownerSegment) : base(ownerSegment, "PortE") { }
//    public override bool Value
//    {
//        get => _value;
//        set
//        {
//            if (_value != value)
//            {
//                _value = value;
//                OwnerSegment.TagsEnd.Iter(et => et.Value = value);
//            }
//        }
//    }
//}

//#endif