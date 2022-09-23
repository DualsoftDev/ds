using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Base.Obsolete
{
    partial class PortInfo
    {
        //public abstract void SetValue(bool newValue);
        public abstract bool PlanValueChanged(bool newValue);
        public abstract bool ActualValueChanged(bool newValue);
    }


    partial class PortInfoEnd
    {
        public override void SetValue(bool newValue)    // PortInfoEnd
        {
            Assert(Plan.Value == _value);

            if (Plan.Value != newValue)
            {
                CheckMatch(newValue);

                if (Plan is IBitWritable wPlan)
                    wPlan.SetValue(newValue);
                else
                    Assert(Plan.Value == newValue);

                _value = newValue;
            }
        }

        public override bool PlanValueChanged(bool newValue)    // PortInfoEnd
        {
            Assert(Plan.Value == newValue);
            CheckMatch(newValue);
            return Actual == null || Actual.Value == newValue;
        }
        public override bool ActualValueChanged(bool newValue)
        {
            Assert(Actual.Value == newValue);
            if (Plan.Value != newValue)
                throw new DsException($"Spatial Error: Plan[{Plan}={Plan.Value}] <> Actual[{Actual.Value}]");

            _value = newValue;
            return true;
        }

    }

    partial class PortInfoCommand
    {
        //public override void SetValue(bool newValue)    // PortInfoCommand
        //{
        //    if (Plan is IBitWritable w)
        //        w.SetValue(newValue);
        //    else
        //        Assert(Evaluate() == newValue);

        //    _value = newValue;
        //    Actual?.SetValue(newValue);
        //}

        public override bool PlanValueChanged(bool newValue)    // PortInfoCommand
        {
            Assert(Plan.Value == newValue);
            Actual?.SetValue(newValue);
            //SetValue(newValue);
            return true;
        }
        public override bool ActualValueChanged(bool newValue) => false;
    }

}
