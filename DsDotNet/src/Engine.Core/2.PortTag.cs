using System.Threading.Tasks;

namespace Engine.Core;


/// <summary> 정보(Plan) + 물리(Actual) </summary>
public abstract class PortExpression : BitReEvaluatable
{
    protected PortExpression(Cpu cpu, Segment segment, string name, IBit plan, Tag actual)
        : base(cpu, name, plan, actual)
    {
        Plan = plan;
        Actual = actual;
        Segment = segment;
    }

    public Segment Segment { get; set; }

    IBit _plan;
    Tag _actual;
    public IBit Plan {
        get => _plan;
        set {
            _plan = value;
            _monitoringBits = new[] { _plan, _actual };
            //ReSubscribe();
        }
    }
    /// <summary> Allow null </summary>
    public Tag Actual {
        get => _actual;
        set
        {
            _actual = value;
            _monitoringBits = new[] { _plan, _actual };
            //ReSubscribe();
        }
    }
}


/// <summary> 지시(Start or Reset) 용 정보(Plan) + 물리(Actual) </summary>
public abstract class PortExpressionCommand : PortExpression
{
    protected PortExpressionCommand(Cpu cpu, Segment segment, string name, IBit plan, Tag actual)
        : base(cpu, segment, name, plan, actual)
    {
    }
    public override bool Evaluate() => Plan.Value;

    //protected override void ReEvaluate(IBit causeBit)
    //{
    //    if (causeBit == Plan)
    //    {
    //        var val = causeBit.Value;
    //        if (Actual != null)
    //            Actual.Value = val;
    //        Debug.Assert(Plan.Value == val);
    //        BitChange.Publish(this, val, true, causeBit);
    //    }
    //}
}
/// <summary> Start 명령용 정보(Plan) + 물리(Actual) </summary>
public class PortExpressionStart : PortExpressionCommand
{
    public PortExpressionStart(Cpu cpu, Segment segment, string name, IBit plan, Tag actual)
        : base(cpu, segment, name, plan, actual)
    {
    }
}
/// <summary> Reset 명령용 정보(Plan) + 물리(Actual) </summary>
public class PortExpressionReset : PortExpressionCommand
{
    public PortExpressionReset(Cpu cpu, Segment segment, string name, IBit plan, Tag actual)
        : base(cpu, segment, name, plan, actual)
    {
    }
}

/// <summary> 관찰용 정보(Plan) + 물리(Actual) </summary>
public class PortExpressionEnd : PortExpression
{
    private PortExpressionEnd(Cpu cpu, Segment segment, string name, IBitWritable plan, Tag actual)
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
    public static PortExpressionEnd Create(Cpu cpu, Segment segment, string name, Tag actual)
    {
        var plan = new Flag(cpu, $"{name}_Plan");
        return new PortExpressionEnd(cpu, segment, name, plan, actual);
    }


    public override bool Evaluate() => Plan.Value && (Actual == null || Actual.Value);

    public override bool Value
    {
        get => Evaluate();
        // PortExpressionEnd 에 한해, setter 를 허용한다.
        set
        {
            if (Plan.Value != value)
            {
                CheckMatch(value);

                //! 호출 순서 매우 민감 + 병렬화 불가 영역
                Plan.Value = value;
                Debug.Assert(Plan.Value == value);
                if (Actual == null)
                    BitChange.Publish(this, value, true);
            }
        }
    }

    void CheckMatch(bool newPlanValue)
    {
        // Plan 설정 이후에, Actual 센서가 이미 Plan 설정하려는 값과 동일한 상태로 먼저 바뀌어 있는 상태
        if (Actual != null && newPlanValue == Actual.Value)
            throw new DsException($"Spatial Error: Plan[{Plan}={newPlanValue}] <> Actual[{Actual.Value}]");

    }


    //protected override void ReEvaluate(IBit causeBit)
    //{
    //    if (causeBit == Actual)
    //    {
    //        var val = Actual.Value;
    //        if (Actual != null && Plan.Value != val)
    //            throw new DsException($"Spatial Error: Plan[{causeBit}={val}] <> Actual[{Actual.Value}]");
    //        Debug.Assert(this.Value == val);
    //        BitChange.Publish(this, val, true, causeBit);
    //    }
    //}
}
