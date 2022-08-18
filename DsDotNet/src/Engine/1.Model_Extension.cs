using Engine.Graph;

namespace Engine;

public static class ModelExtension
{
    public static void BuidGraphInfo(this Model model)
    {
        var rootFlows = model.CollectRootFlows();
        foreach (var flow in rootFlows)
            flow.GraphInfo = FsGraphInfo.AnalyzeFlows(new[] { flow }, true);

        foreach (var cpu in model.Cpus)
            cpu.GraphInfo = FsGraphInfo.AnalyzeFlows(cpu.RootFlows, true);

        foreach (var segment in model.CollectSegments())
        {
            segment.GraphInfo = FsGraphInfo.AnalyzeFlows(new[] { segment }, false);
            //var pi = new GraphProgressSupportUtil.ProgressInfo(segment.GraphInfo);
            //segment.ChildrenOrigin = pi.ChildOrigin.ToArray();
        }
    }
    public static void Epilogue(this Model model)
    {
        foreach (var segment in model.CollectSegments())
            segment.Epilogue();
    }


    public static IEnumerable<RootFlow> CollectRootFlows(this Model model) => model.Systems.SelectMany(sys => sys.RootFlows);

    public static IEnumerable<Flow> CollectFlows(this Model model)
    {
        var rootFlows = model.CollectRootFlows().ToArray();
        var subFlows = rootFlows.SelectMany(rf => rf.RootSegments);
        var allFlows = rootFlows.Cast<Flow>().Concat(subFlows);
        return allFlows;
    }
    public static IEnumerable<SegmentBase> CollectSegments(this Model model) =>
        model.CollectRootFlows().SelectMany(rf => rf.RootSegments)
        ;
}
