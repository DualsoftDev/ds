namespace Engine.Core;

//public abstract class ExpressionBase : BitReEvaluatable
//{
//    public override bool Value { get => base.Value; set => throw new Exception("ERROR"); }
//}
//public class And : ExpressionBase
//{
//    public override bool Value { get { return Bits.All(b => b.Value); } set { throw new Exception("ERROR"); } }
//    public List<IBit> Bits;
//    public And(IBit[] bits)
//    {
//        Bits = bits.ToList();
//    }
//}
//public class Or : ExpressionBase
//{
//    public override bool Value => Bits.Any(b => b.Value);
//    public List<IBit> Bits;
//    public Or(IBit[] bits)
//    {
//        Bits = bits.ToList();
//    }
//}

//public class Not : ExpressionBase
//{
//    public override bool Value => !Bit.Value;
//    public IBit Bit;
//    public Not(IBit bit) => Bit = bit;
//}

