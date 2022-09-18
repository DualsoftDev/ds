using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Engine.Core.DsType;
using static Model.Import.Office.Object;

namespace Dual.Model.Import
{
    public static class SimSeg
    {
        static readonly List<NodeCausal> mys = new List<NodeCausal>() { NodeCausal.MY };
        static readonly List<NodeCausal> notMys = new List<NodeCausal>() { NodeCausal.EX, NodeCausal.TR, NodeCausal.TX, NodeCausal.RX };
        static bool org = false;
        static List<NodeCausal> AllSeg
        {
            get
            {
                var obj = new List<NodeCausal>();
                obj.AddRange(mys); obj.AddRange(notMys);
                return obj;
            }
        }


        private static async Task Test(IEnumerable<Seg> rootSegs, Status4 status, List<NodeCausal> showList)
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

            var rootSegs = model.ActiveSys.RootSegs();
            var notRootSegs = model.ActiveSys.NotRootSegs();
            await Task.Run(async () =>
            {
                await Test(rootSegs, Status4.Homing, AllSeg);
                await Test(notRootSegs, Status4.Homing, AllSeg);
                await Task.Delay(10);

                await Test(notRootSegs, Status4.Ready, AllSeg);
                await Task.Delay(10);
                await Test(rootSegs, Status4.Ready, notMys);
                await Task.Delay(10);
                await Test(rootSegs, Status4.Ready, mys);
            });

            org = true;
        }
        private static List<Seg> getHeads(Dictionary<Seg, Seg> dic, Dictionary<MEdge, MEdge> dicEdge)
        {
            List<Seg> tgts = dicEdge.Values.Where(w => w.Causal.IsStart).Select(edge => edge.Target).Distinct().ToList();
            List<Seg> heads = dic.Values.Where(s => !tgts.Contains(s)).ToList();
            List<MEdge> findEdges = dicEdge.Values
                .Where(edge => edge.Causal.IsStart)
                .Where(edge => heads.Contains(edge.Source)).ToList();

            foreach (var seg in heads) dic.Remove(seg);
            foreach (var edge in findEdges) dicEdge.Remove(edge);

            return heads;
        }


        private static async Task runSeg(IEnumerable<Seg> segs, IEnumerable<MEdge> runEdges)
        {
            var dicSeg = segs.ToDictionary(d => d);
            var dicEdge = runEdges.ToDictionary(d => d);

            await Task.Run(async () =>
            {
                foreach (var seg in runEdges.SelectMany(s => s.Nodes).Distinct())
                {
                    if (dicSeg.Count() == 0) break;
                    List<Seg> heads = getHeads(dicSeg, dicEdge);

                    await Test(heads, Status4.Going, AllSeg);
                    await Task.Delay(50);
                    await Test(heads, Status4.Finish, AllSeg);
                }
            });
        }

        public static async Task TestStart(DsModel model)
        {
            if (model == null) return;
            if (!org) await TestORG(model);

            var dicSeg = model.ActiveSys.RootSegs().ToDictionary(d => d);
            var dicEdge = model.ActiveSys.RootEdges().ToDictionary(d => d);

            await Task.Run(async () =>
            {
                List<MEdge> edges = model.ActiveSys.RootEdges().ToList();
                var segs = edges.SelectMany(s => s.Nodes).ToList();
                segs.AddRange(dicSeg.Values);

                foreach (var cont in segs.Distinct())
                {
                    if (dicSeg.Count() == 0) break;

                    List<Seg> heads = getHeads(dicSeg, dicEdge);
                    await Test(heads, Status4.Going, AllSeg);
                    await Task.Delay(50);

                    foreach (var seg in heads)
                    {
                        await runSeg(seg.ChildSegs, seg.MEdges);
                    }

                    await Test(heads, Status4.Finish, AllSeg);
                }
            });

            org = false;
        }




    }

}

