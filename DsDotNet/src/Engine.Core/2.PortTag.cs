namespace Engine.Core;


/// <summary> 정보(Plan) + 물리(Actual) </summary>
public abstract class PortExpression : BitReEvaluatable
{
    protected PortExpression(Cpu cpu, string name, IBit plan, Tag actual)
        : base(cpu, name, plan, actual)
    {
        Plan = plan;
        Actual = actual;
    }


    public IBit Plan { get; }
    /// <summary> Allow null </summary>
    public Tag Actual { get; }
}


/// <summary> 지시(Start or Reset) 용 정보(Plan) + 물리(Actual) </summary>
public abstract class PortExpressionCommand : PortExpression
{
    protected PortExpressionCommand(Cpu cpu, string name, IBit plan, Tag actual)
        : base(cpu, name, plan, actual)
    {
    }
    public override bool Value => Plan.Value;

    protected override void ReEvaulate(BitChange bitChange)
    {
        if (bitChange.Bit == Plan)
        {
            var val = bitChange.Bit.Value;
            if (Actual != null)
                Actual.Value = val;
            Global.RawBitChangedSubject.OnNext(new BitChange(this, val, true));
        }
    }
}
/// <summary> Start 명령용 정보(Plan) + 물리(Actual) </summary>
public class PortExpressionStart : PortExpressionCommand
{
    public PortExpressionStart(Cpu cpu, string name, IBit plan, Tag actual)
        : base(cpu, name, plan, actual)
    {
    }
}
/// <summary> Reset 명령용 정보(Plan) + 물리(Actual) </summary>
public class PortExpressionReset : PortExpressionCommand
{
    public PortExpressionReset(Cpu cpu, string name, IBit plan, Tag actual)
        : base(cpu, name, plan, actual)
    {
    }
}

/// <summary> 관찰용 정보(Plan) + 물리(Actual) </summary>
public class PortExpressionEnd : PortExpression
{
    private PortExpressionEnd(Cpu cpu, string name, IBit plan, Tag actual)
        : base(cpu, name, plan, actual)
    {
        if (Actual == null || (!Plan.Value && !Actual.Value))
            Global.NoOp();
        else
            CheckMatch(Plan.Value);
    }


    // End port expression 은 plan 으로 외부에서 따로 지정할 수 없고,
    // 내부에서 해당 값을 설정할 수 있어야 하므로 (Segment Finish 나 Homing 완료시 ON/OFF 시켜야 함)
    // 외부에서 받지 않고, 내부에서 생성해서 관리한다.
    public static PortExpressionEnd Create(Cpu cpu, string name, Tag actual)
    {
        var plan = new Flag(cpu, $"{name}_Plan");
        return new PortExpressionEnd(cpu, name, plan, actual);
    }


    public override bool Value
    {
        get => Plan.Value && (Actual == null || Actual.Value);
        // PortExpressionEnd 에 한해, setter 를 허용한다.
        set
        {
            if (Plan.Value != value)
            {
                CheckMatch(value);
                Plan.Value = value;
                Global.RawBitChangedSubject.OnNext(new BitChange(this, value, true));
            }
        }
    }

    void CheckMatch(bool newPlanValue)
    {
        // Plan 설정 이후에, Actual 센서가 이미 Plan 설정하려는 값과 동일한 상태로 먼저 바뀌어 있는 상태
        if (Actual != null && newPlanValue == Actual.Value)
            throw new DsException($"Spatial Error: Plan[{Plan}={newPlanValue}] <> Actual[{Actual.Value}]");

    }


    protected override void ReEvaulate(BitChange bitChange)
    {
        var val = bitChange.Bit.Value;
        if (bitChange.Bit == Actual)
        {
            if (Actual != null && Plan.Value != val)
                throw new DsException($"Spatial Error: Plan[{bitChange.Bit}={val}] <> Actual[{Actual.Value}]");
            Debug.Assert(this.Value == val);
        }
    }
}
