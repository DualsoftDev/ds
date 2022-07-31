/**/    namespace Engine.Core;
/**/

public static class RisingFalling
{
    public static Subject<IBit> RisingFallingSourceSubject { get; } = new();
    public static Subject<IBit> RisingFallingChangedSubject { get; } = new();
}


/**/
[Obsolete("Not supported rising/falling")]
/**/    public abstract class RisingFallingBase : BitReEvaluatable
/**/    {
/**/        public override bool Value
/**/        {
/**/            get => _value;
/**/            set => throw new DsException("Not Supported.");
/**/        }
/**/
/**/        protected void SetValue(bool value, IBit cause=null)
/**/        {
/**/            if (_value != value)
/**/            {
/**/                _value = value;

                    List<IBit> changes = new();
                    using var _subscription =
                        RisingFalling.RisingFallingChangedSubject
                            .Subscribe(bit => changes.Add(bit))
                            ;
                    RisingFalling.RisingFallingSourceSubject.OnNext(this);
/**/                BitChange.Publish(this, value, true, cause);
/**/            }
/**/        }
/**/
/**/        protected RisingFallingBase(Cpu cpu, string name, IBit target)
/**/            : base(cpu, name, target)
/**/        {
/**/            Debug.Assert(target != null);
/**/        }
/**/
/**/    }
/**/
/**/    [Obsolete("Not supported rising/falling")]
/**/    public class Rising : RisingFallingBase
/**/    {
/**/        public Rising(Cpu cpu, string name, IBit target)
/**/            : base(cpu, name, target)
/**/        {
/**/        }
    /**/
    protected override bool NeedChange(IBit causeBit) => throw new Exception("ERROR");
/**/        protected override void ReEvaulate(IBit causeBit)
/**/        {
/**/            SetValue(_monitoringBits[0].Value, causeBit);
/**/
/**/            // end of rising
/**/            SetValue(false);
/**/
/**/        }
/**/    }
/**/    [Obsolete("Not supported rising/falling")]
/**/    public class Falling : RisingFallingBase
/**/    {
/**/        public Falling(Cpu cpu, string name, IBit target)
/**/            : base(cpu, name, target)
/**/        {
/**/        }
/**/
    protected override bool NeedChange(IBit causeBit) => throw new Exception("ERROR");

/**/        protected override void ReEvaulate(IBit causeBit)
/**/        {
/**/            SetValue(!_monitoringBits[0].Value, causeBit);
/**/
/**/            // end of falling
/**/            SetValue(false);
/**/        }
/**/    }
/**/


