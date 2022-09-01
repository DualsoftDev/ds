using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Obsolete
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
            DAssert(Plan.Value == _value);

            if (Plan.Value != newValue)
            {
                CheckMatch(newValue);

                if (Plan is IBitWritable wPlan)
                    wPlan.SetValue(newValue);
                else
                    DAssert(Plan.Value == newValue);

                _value = newValue;
            }
        }

        public override bool PlanValueChanged(bool newValue)    // PortInfoEnd
        {
            DAssert(Plan.Value == newValue);
            CheckMatch(newValue);
            return Actual == null || Actual.Value == newValue;
        }
        public override bool ActualValueChanged(bool newValue)
        {
            DAssert(Actual.Value == newValue);
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
        //        DAssert(Evaluate() == newValue);

        //    _value = newValue;
        //    Actual?.SetValue(newValue);
        //}

        public override bool PlanValueChanged(bool newValue)    // PortInfoCommand
        {
            DAssert(Plan.Value == newValue);
            Actual?.SetValue(newValue);
            //SetValue(newValue);
            return true;
        }
        public override bool ActualValueChanged(bool newValue) => false;
    }

}
