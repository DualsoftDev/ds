using Engine.Core.Obsolete;
using Engine.Util;

namespace Engine.Core;


/// <summary> 정보(Plan) + 물리(Actual) </summary>
public abstract class PortInfo : BitReEvaluatable//, IBitWritable
{
    protected PortInfo(Cpu cpu, SegmentBase segment, string name, IBit plan, Tag actual)
        : base(cpu, name, plan, actual)
    {
        Plan = plan;
        Actual = actual;
        Segment = segment;
    }

    public SegmentBase Segment { get; set; }
    public string QualifiedName => $"{Segment.QualifiedName}_{GetType().Name}";
    /// <summary> 내부 추적용 Tag 이름 : QualifiedName + 기능명.  e.g "L.F.Main.AutoStart.  사용자가 지정하는 이름과는 별개 </summary>
    public string InternalName { get; set; }

    IBit _plan;
    Tag _actual;
    public IBit Plan
    {
        get => _plan;
        set
        {
            _plan = value;
            _monitoringBits = new[] { _plan, _actual };
        }
    }
    /// <summary> Allow null </summary>
    public Tag Actual
    {
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
    protected PortInfoCommand(Cpu cpu, SegmentBase segment, string name, IBit plan, Tag actual)
        : base(cpu, segment, name, plan, actual)
    {
    }
    public override bool Evaluate() => Plan.Value;

    public override bool Value
    {
        get
        {
            _value = Evaluate();
            return _value;
        }
    }

}
/// <summary> Start 명령용 정보(Plan) + 물리(Actual) </summary>
public class PortInfoStart : PortInfoCommand
{
    public PortInfoStart(Cpu cpu, SegmentBase segment, string name, IBit plan, Tag actual)
        : base(cpu, segment, name, plan, actual)
    {
    }
}
/// <summary> Reset 명령용 정보(Plan) + 물리(Actual) </summary>
public class PortInfoReset : PortInfoCommand
{
    public PortInfoReset(Cpu cpu, SegmentBase segment, string name, IBit plan, Tag actual)
        : base(cpu, segment, name, plan, actual)
    {
    }
}

/// <summary> 관찰용 정보(Plan) + 물리(Actual) </summary>
public class PortInfoEnd : PortInfo
{
    public PortInfoEnd(Cpu cpu, SegmentBase segment, string name, IBitWritable plan, Tag actual)
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
    public static PortInfoEnd Create(Cpu cpu, SegmentBase segment, string name, Tag actual)
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
            throw new Common.DsException($"Spatial Error: Plan[{Plan}={newPlanValue}] <> Actual[{Actual.Value}]");
    }
}
