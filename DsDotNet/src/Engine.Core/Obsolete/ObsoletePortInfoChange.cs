namespace Engine.Core;

//public abstract class PortInfoChange : BitChange
//{
//    public PortInfo PortInfo { get; }
//    //public PortInfoChange(PortInfo bit, bool newValue, object cause = null, ExceptionHandler onError =null)
//    //    : base(bit, newValue, cause, onError)
//    //{
//    //    PortInfo = bit;
//    //}
//    public PortInfoChange(BitChange bc)
//        : base(bc.Bit, bc.NewValue, $"PortInfo change by {bc.Cause}", bc.OnError)
//    {
//        PortInfo = (PortInfo)bc.Bit;
//    }
//}
//public class PortInfoPlanChange : PortInfoChange
//{
//    public bool Applied { get; set; }
//    public PortInfoPlanChange(BitChange bc) : base(bc) { }
//    //public PortInfoPlanChange(PortInfo bit, bool newValue, object cause = null, ExceptionHandler onError = null)
//    //    : base(bit, newValue, cause, onError)
//    //{}
//}
//public class PortInfoActualChange : PortInfoChange
//{
//    public PortInfoActualChange(BitChange bc) : base(bc) { }
//    //public PortInfoActualChange(PortInfo bit, bool newValue, object cause = null, ExceptionHandler onError = null)
//    //    : base(bit, newValue, cause, onError)
//    //{}
//}
