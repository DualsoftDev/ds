using Model.Import.Office;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Model.Import.Office.Model;
using static Model.Import.Office.Object;
using static Model.Import.Office.Type;

namespace Dual.Model.Import
{
    public static class SimSegment
    {
        static readonly List<NodeCausal> mys = new List<NodeCausal>() { NodeCausal.MY };
        static readonly List<NodeCausal> notMys = new List<NodeCausal>() { NodeCausal.EX, NodeCausal.TR, NodeCausal.TX, NodeCausal.RX, NodeCausal.DUMMY };
        static bool org = false;
        static List<NodeCausal> AllSeg {
            get
            {
                var obj = new List<NodeCausal>();
                obj.AddRange(mys); obj.AddRange(notMys);
                return obj;
            }}


        private static async Task Test(IEnumerable<Segment> rootSegs, Status status, List<NodeCausal> showList)
        {
            foreach (var seg in rootSegs)
            {
                await Task.Run(async () =>
                 {
                     if (showList.Contains(seg.NodeCausal))
                     {
                         seg.SetStatus(status);
                         await Task.Delay(1);
                     }
                 });
            }
        }

        public static async Task TestORG(DsModel model)
        {
            if (model == null) return;

            var rootSegs = model.ActiveSys.RootSegments();
            var notRootSegs = model.ActiveSys.NotRootSegments();
            await Task.Run(async () =>
            {
                await Test(rootSegs, Status.H, AllSeg);
                await Test(notRootSegs, Status.H, AllSeg);
                await Task.Delay(10);

                await Test(notRootSegs, Status.R, AllSeg);
                await Task.Delay(10);
                await Test(rootSegs, Status.R, notMys);
                await Task.Delay(10);
                await Test(rootSegs, Status.R, mys);
            });

            org = true;
        }
        private static List<Segment> getHeads(Dictionary<Segment, Segment> dic, Dictionary<MEdge, MEdge> dicEdge)
        {
            List<Segment> tgts = dicEdge.Values.Select(edge => edge.Target).Distinct().ToList();
            List<Segment> heads = dic.Values.Where(s => !tgts.Contains(s)).ToList();
            List<MEdge> findEdges = dicEdge.Values
              //  .Where(edge => edge.Causal.IsStart)
                .Where(edge => heads.Contains(edge.Source)).ToList();

            foreach (var seg in heads) dic.Remove(seg);
            foreach (var edge in findEdges) dicEdge.Remove(edge);

            return heads;
        }


        private static async Task runSegment(IEnumerable<Segment> segs, IEnumerable<MEdge> runEdges)
        {
            var dicSeg = segs.ToDictionary(d => d);
            var dicEdge = runEdges.ToDictionary(d => d);

            await Task.Run(async () =>
            {
                foreach (var seg in runEdges.SelectMany(s=>s.Nodes).Distinct())
                {
                    if (dicSeg.Count() == 0) break;
                    List<Segment> heads = getHeads(dicSeg, dicEdge);

                    await Test(heads, Status.G, AllSeg);
                    await Task.Delay(50);
                    await Test(heads, Status.F, AllSeg);
                }
            });
        }

        public static async Task TestStart(DsModel model)
        {
            if (model == null) return;
            if (!org) await TestORG(model);

            var dicSeg  = model.ActiveSys.RootSegments().ToDictionary(d => d);
            var dicEdge = model.ActiveSys.RootEdges().ToDictionary(d => d);

            await Task.Run(async () =>
            {
                List<MEdge> edges = model.ActiveSys.RootEdges().ToList();
                foreach (var cont in edges.SelectMany(s=>s.Nodes).Distinct())
                {
                    if (dicSeg.Count() == 0) break;
                  
                    List<Segment> heads = getHeads(dicSeg, dicEdge);
                    await Test(heads, Status.G, AllSeg);
                    await Task.Delay(50);

                    foreach (var seg in heads)
                    {
                        await runSegment(seg.ChildSegs, seg.MEdges);
                    }

                    await Test(heads, Status.F, AllSeg);
                }
            });

            org = false;
        }




    }

}

