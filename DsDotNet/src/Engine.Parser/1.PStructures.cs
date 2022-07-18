using Engine.Core;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DsParser
{

    public static class PModelUtil
    {
        static ICoin FindCoin(this Model model, string systemName, string flowOrTaskName, string segmentOrCallName, bool isSegment)
        {
            var system = model.Systems.First(s => s.Name == systemName);
            if (isSegment)
            {
                var flow = system.RootFlows.FirstOrDefault(f => f.Name == flowOrTaskName);
                if (flow == null)
                    return null;

                return flow.ChildVertices.OfType<Segment>()
                    .FirstOrDefault(s => s.Name == segmentOrCallName)
                    ;
            }

            var task = system.Tasks.FirstOrDefault(t => t.Name == flowOrTaskName);
            if (task == null)
                return null;

            return task.CallPrototypes.FirstOrDefault(c => c.Name == segmentOrCallName);
        }
        public static Segment FindSegment(this Model model, string systemName, string flowName, string segmentName) =>
            model.FindCoin(systemName, flowName, segmentName, true) as Segment;

        public static CallPrototype FindCall(this Model model, string systemName, string taskName, string callName) =>
            model.FindCoin(systemName, taskName, callName, false) as CallPrototype;

        public static Segment FindSegment(this Model model, string fqSegmentName)
        {
            if (fqSegmentName == "_")
                return null;

            var names = fqSegmentName.Split(new[] { '.' });
            Debug.Assert(names.Length == 3);
            (var sysName, var flowName, var segmentName) = (names[0], names[1], names[2]);
            return model.FindSegment(sysName, flowName, segmentName);
        }

        public static ICoin FindCoin(this Model model, string fqSegmentName)
        {
            var seg = model.FindSegment(fqSegmentName);
            if (seg != null)
                return seg;

            return model.FindCall(fqSegmentName);
        }

        public static Segment[] FindSegments(this Model model, string fqSegmentNames)
        {
            return
                fqSegmentNames
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(segName => FindSegment(model, segName))
                    .ToArray()
                    ;
        }

        public static CallPrototype FindCall(this Model model, string fqCallName)
        {
            var names = fqCallName.Split(new[] { '.' });
            (var sysName, var taskName, var callName) = (names[0], names[1], names[2]);
            return model.FindCall(sysName, taskName, callName);
        }
    }
}
