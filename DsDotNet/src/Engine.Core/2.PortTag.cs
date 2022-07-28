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
    public PortExpressionEnd(Cpu cpu, string name, IBit plan, Tag actual)
        : base(cpu, name, plan, actual)
    {
        if (Actual == null || (!Plan.Value && !Actual.Value))
            Global.NoOp();
        else
            CheckMatch(Plan.Value);
    }

    public override bool Value => Plan.Value && (Actual == null || Actual.Value);

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
