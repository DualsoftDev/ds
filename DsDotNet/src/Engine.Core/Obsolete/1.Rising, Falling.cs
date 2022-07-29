/**/    namespace Engine.Core;
/**/
/**/    [Obsolete("Not supported rising/falling")]
/**/    public abstract class RisingFallingBase : BitReEvaluatable
/**/    {
/**/        bool _value;
/**/        public override bool Value
/**/        {
/**/            get => _value;
/**/            set => throw new DsException("Not Supported.");
/**/        }
/**/
/**/        protected void SetValue(bool value)
/**/        {
/**/            if (_value != value)
/**/            {
/**/                _value = value;
/**/                Global.RawBitChangedSubject.OnNext(new BitChange(this, value, true));
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
/**/        protected override void ReEvaulate(BitChange bitChange)
/**/        {
/**/            SetValue(_monitoringBits[0].Value);
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
/**/        protected override void ReEvaulate(BitChange bitChange)
/**/        {
/**/            SetValue(!_monitoringBits[0].Value);
/**/
/**/            // end of falling
/**/            SetValue(false);
/**/        }
/**/    }
/**/


