using System.Threading.Tasks;

namespace Engine.Core;


/// <summary> 정보(Plan) + 물리(Actual) </summary>
public abstract class PortInfo : BitReEvaluatable, IBitWritable
{
    protected PortInfo(Cpu cpu, Segment segment, string name, IBit plan, Tag actual)
        : base(cpu, name, plan, actual)
    {
        Plan = plan;
        Actual = actual;
        Segment = segment;
    }

    public Segment Segment { get; set; }
    public string QualifiedName => $"{Segment.QualifiedName}.{GetType().Name}";
    public abstract void SetValue(bool newValue);
    public abstract bool PlanValueChanged(bool newValue);
    public abstract bool ActualValueChanged(bool newValue);

    IBit _plan;
    Tag _actual;
    public IBit Plan {
        get => _plan;
        set {
            _plan = value;
            _monitoringBits = new[] { _plan, _actual };
        }
    }
    /// <summary> Allow null </summary>
    public Tag Actual {
        get => _actual;
        set
        {
            _actual = value;
            _monitoringBits = new[] { _plan, _actual };
        }
    }
}


/// <summary> 지시(Start or Reset) 용 정보(Plan) + 물리(Actual) </summary>
public abstract class PortInfoCommand : PortInfo
{
    protected PortInfoCommand(Cpu cpu, Segment segment, string name, IBit plan, Tag actual)
        : base(cpu, segment, name, plan, actual)
    {
    }
    public override bool Evaluate() => Plan.Value;
    public override void SetValue(bool newValue)
    {
        switch(Plan)
        {
            case IBitWritable w:
                w.SetValue(newValue);
                break;
            default:
                var eval = Evaluate();
                Debug.Assert(eval == newValue);
                break;
        }
        _value = newValue;
        Actual.SetValue(newValue);
    }

    public override bool PlanValueChanged(bool newValue)
    {
        Debug.Assert(Plan.Value == newValue);
        Actual?.SetValue(newValue);
        SetValue(newValue);
        return true;
    }
    public override bool ActualValueChanged(bool newValue) => false;

}
/// <summary> Start 명령용 정보(Plan) + 물리(Actual) </summary>
public class PortInfoStart : PortInfoCommand
{
    public PortInfoStart(Cpu cpu, Segment segment, string name, IBit plan, Tag actual)
        : base(cpu, segment, name, plan, actual)
    {
    }
}
/// <summary> Reset 명령용 정보(Plan) + 물리(Actual) </summary>
public class PortInfoReset : PortInfoCommand
{
    public PortInfoReset(Cpu cpu, Segment segment, string name, IBit plan, Tag actual)
        : base(cpu, segment, name, plan, actual)
    {
    }
}

/// <summary> 관찰용 정보(Plan) + 물리(Actual) </summary>
public class PortInfoEnd : PortInfo
{
    private PortInfoEnd(Cpu cpu, Segment segment, string name, IBitWritable plan, Tag actual)
        : base(cpu, segment, name, plan, actual)
    {
        if (Actual == null || (!Plan.Value && !Actual.Value))
            Global.NoOp();
        else
            CheckMatch(Plan.Value);
    }


    // End port expression 은 plan 으로 외부에서 따로 지정할 수 없고,
    // 내부에서 해당 값을 설정할 수 있어야 하므로 (Segment Finish 나 Homing 완료시 ON/OFF 시켜야 함)
    // 외부에서 받지 않고, 내부에서 생성해서 관리한다.
    public static PortInfoEnd Create(Cpu cpu, Segment segment, string name, Tag actual)
    {
        var plan = new Flag(cpu, $"{name}_Plan");
        return new PortInfoEnd(cpu, segment, name, plan, actual);
    }


    public override bool Evaluate() => Plan.Value && (Actual == null || Actual.Value);

    public override bool Value => Evaluate();

    void CheckMatch(bool newPlanValue)
    {
        // Plan 설정 이후에, Actual 센서가 이미 Plan 설정하려는 값과 동일한 상태로 먼저 바뀌어 있는 상태
        if (Actual != null && newPlanValue == Actual.Value)
            throw new DsException($"Spatial Error: Plan[{Plan}={newPlanValue}] <> Actual[{Actual.Value}]");

    }
    public override void SetValue(bool newValue)
    {
        Debug.Assert(Plan.Value == _value);

        if (Plan.Value != newValue)
        {
            CheckMatch(newValue);

            var wPlan = Plan as IBitWritable;
            if (wPlan == null)
                Debug.Assert(Plan.Value == newValue);
            else
                wPlan.SetValue(newValue);

            _value = newValue;
        }
    }

    public override bool PlanValueChanged(bool newValue)
    {
        Debug.Assert(Plan.Value == newValue);
        CheckMatch(newValue);
        return false;
    }
    public override bool ActualValueChanged(bool newValue)
    {
        Debug.Assert(Actual.Value == newValue);
        if (Plan.Value != newValue)
            throw new DsException($"Spatial Error: Plan[{Plan}={Plan.Value}] <> Actual[{Actual.Value}]");

        _value = newValue;
        return true;
    }

}
