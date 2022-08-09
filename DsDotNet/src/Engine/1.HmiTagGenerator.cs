using Engine.Graph;

namespace Engine;

public static class HmiTagGenerator
{
    static ILog Logger => Program.Logger;

    /// <summary> flow 의 모든 root segment 에 대해서 S/R/E tag 생성 </summary>
    static Tag[] GenerateHmiTag(Segment segment)
    {
        var flow = segment.ContainerFlow;
        var cpu = flow.Cpu;
        var name = $"{flow.System.Name}_{flow.Name}_{segment.Name}";
        var s = new Tag(cpu, segment, $"Start_{name}") { Type = TagType.Q };
        var r = new Tag(cpu, segment, $"Reset_{name}") { Type = TagType.Q };
        var e = new Tag(cpu, segment, $"End_{name}") { Type = TagType.I };

        segment.AddStartTags(s);
        segment.AddResetTags(r);
        segment.AddEndTags(e);

        //cpu.AddBitDependancy(s, segment.PortInfoS);
        //cpu.AddBitDependancy(r, segment.PortInfoR);
        //cpu.AddBitDependancy(segment.PortInfoE, e);
        return new[] { s, r, e };
    }

    /// <summary> flow 의 init, last segment 에 대해서 auto start, auto reset tag 생성 </summary>
    static Tag[] GenerateHmiAutoTagForRootSegment(RootFlow flow)
    {
        var cpu = flow.Cpu;
        var midName = $"{flow.System.Name}_{flow.Name}";

        // graph 분석
        var graphInfo = GraphUtil.analyzeFlows(new[] { flow }, true);
        List<Tag> tags = new();

        foreach (var init_ in graphInfo.Inits)
        {
            var init = init_ as Segment;
            if (init == null)
            {
                Debug.Assert(init_ is Call);
                // do nothing for call
            }
            else
            {
                var s = Tag.CreateAutoStart(cpu, init, $"AutoStart_{midName}_{init.Name}");
                init.AddStartTags(s);
                tags.Add(s);
            }
        }

        foreach (var last_ in graphInfo.Lasts)
        {
            var last = last_ as Segment;
            if (last == null)
            {
                Debug.Assert(last_ is Call);
                // do nothing for call
            }
            else
            {
                var r = Tag.CreateAutoReset(cpu, last, $"AutoReset_{midName}_{last.Name}");
                last.AddResetTags(r);
                tags.Add(r);
            }
        }

        return tags.ToArray();
    }

    /// <summary>
    /// Flow 에 속한 root segment 에 대해서 S/R/E tag 생성
    /// - init 에 대해서 auto start,
    /// - last segment에 대해서 auto reset tag 생성
    /// <para>Side effect : 해당 cpu 에 dependancy 설정됨. </para>
    /// todo: flow 에 속한 call 에 대한 HMI tag 생성
    /// <para> - 생성된 tag 는 CPU 에 저장된다.</para>
    /// </summary>
    public static Tag[] GenereateHmiTags4Segments(this RootFlow flow)
    {
        var cpu = flow.Cpu;

        var segments = flow.RootSegments;

        // 모든 root segment 에 대해서 S/R/E tag 생성
        var sre = segments.SelectMany(s => GenerateHmiTag(s)).ToArray();
        var autoSegTags = GenerateHmiAutoTagForRootSegment(flow);

        var hmiTags = sre.Concat(autoSegTags).ToArray();
        hmiTags.Iter(t => t.Type = t.Type.Add(TagType.External));

        return hmiTags;
    }
}
