using Engine.Graph;

namespace Engine;

public static class HmiTagGenerator
{
    static ILog Logger => Program.Logger;

    ///// <summary> flow 의 init, last segment 에 대해서 auto start, auto reset tag 생성 </summary>
    //static Tag[] GenerateHmiAutoTagForRootSegment(RootFlow flow)
    //{
    //    var cpu = flow.Cpu;
    //    var midName = $"{flow.System.Name}_{flow.Name}";

    //    flow.Auto = new Tag(cpu, null, $"Auto_{midName}", TagType.Auto | TagType.Flow);

    //    var autoStart = Tag.CreateAutoStart(cpu, null, $"AutoStart_{midName}", $"{midName}.AutoStart");
    //    flow.AutoStart = autoStart;

    //    var autoReset = Tag.CreateAutoReset(cpu, null, $"AutoReset_{midName}", $"{midName}.AutoReset");
    //    flow.AutoReset = autoReset;

    //    foreach(var rs in flow.RootSegments)
    //    {
    //        rs.AddStartTags(autoStart);
    //        rs.AddResetTags(autoReset);
    //    }

    //    return new[] {autoStart, autoReset} ;
    //}

    ///// <summary>
    ///// Flow 에 속한 root segment 에 대해서 S/R/E tag 생성
    ///// - init 에 대해서 auto start,
    ///// - last segment에 대해서 auto reset tag 생성
    ///// <para>Side effect : 해당 cpu 에 dependancy 설정됨. </para>
    ///// to do: flow 에 속한 call 에 대한 HMI tag 생성
    ///// <para> - 생성된 tag 는 CPU 에 저장된다.</para>
    ///// </summary>
    //public static Tag[] GenereateHmiTags4Segments(this RootFlow flow)
    //{
    //    var cpu = flow.Cpu;

    //    var segments = flow.RootSegments;

    //    // 모든 root segment 에 대해서 S/R/E tag 생성
    //    var sre = segments.SelectMany(s => GenerateHmiTag(s)).ToArray();
    //    var autoSegTags = GenerateHmiAutoTagForRootSegment(flow);

    //    var hmiTags = sre.Concat(autoSegTags).ToArray();
    //    hmiTags.Iter(t => t.Type = t.Type.Add(TagType.External));

    //    return hmiTags;
    //}
}
