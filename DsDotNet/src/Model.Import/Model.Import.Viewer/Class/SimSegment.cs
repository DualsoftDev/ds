using Model.Import.Office;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Engine.Core.DsType;
using static Model.Import.Office.Object;
using ImportModel = Model.Import.Office.Object.ImportModel;

namespace Dual.Model.Import
{
    public static class SimSeg
    {
        static readonly List<NodeType> mys = new List<NodeType>() { NodeType.MY };
        static readonly List<NodeType> notMys = new List<NodeType>() { NodeType.TR, NodeType.TX, NodeType.RX };
        static bool org = false;
        static List<NodeType> AllSeg
        {
            get
            {
                var obj = new List<NodeType>();
                obj.AddRange(mys); obj.AddRange(notMys);
                return obj;
            }
        }


        private static async Task Test(IEnumerable<MSeg> rootSegs, Status4 status, List<NodeType> showList)
        {
            foreach (var seg in rootSegs)
            {
                await Task.Run(async () =>
                 {
                     if (showList.Contains(seg.NodeType))
                     {
                         seg.SetStatus(status);
                         await Task.Delay(1);
                     }
                 });
            }
        }

        public static async Task TestORG(ImportModel model)
        {
            if (model == null) return;

            var rootSegs = model.AllFlows.SelectMany(s => s.Nodes).Cast<MSeg>();

         
            await Task.Run(async () =>
            {
                await Test(rootSegs, Status4.Homing, AllSeg);
                await Task.Delay(10);

                await Test(rootSegs, Status4.Ready, AllSeg);
                await Task.Delay(10);
                await Test(rootSegs, Status4.Ready, notMys);
                await Task.Delay(10);
                await Test(rootSegs, Status4.Ready, mys);
            });

            org = true;
        }
        private static List<MSeg> getHeads(Dictionary<MSeg, MSeg> dic, Dictionary<MEdge, MEdge> dicEdge)
        {
            List<MSeg> tgts = dicEdge.Values.Where(w => w.Causal.IsStart).Select(edge => edge.Target).Distinct().ToList();
            List<MSeg> heads = dic.Values.Where(s => !tgts.Contains(s)).ToList();
            List<MEdge> findEdges = dicEdge.Values
                .Where(edge => edge.Causal.IsStart)
                .Where(edge => heads.Contains(edge.Source)).ToList();

            foreach (var seg in heads) dic.Remove(seg);
            foreach (var edge in findEdges) dicEdge.Remove(edge);

            return heads;
        }


        private static async Task runSeg(IEnumerable<MSeg> segs, IEnumerable<MEdge> runEdges)
        {
            var dicSeg = segs.Distinct().ToDictionary(d => d);
            var dicEdge = runEdges.ToDictionary(d => d);

            await Task.Run(async () =>
            {
                foreach (var seg in runEdges.SelectMany(s => s.Nodes).Distinct())
                {
                    if (dicSeg.Count() == 0) break;
                    List<MSeg> heads = getHeads(dicSeg, dicEdge);

                    await Test(heads, Status4.Going, AllSeg);
                    await Task.Delay(50);
                    await Test(heads, Status4.Finish, AllSeg);
                }
            });
        }

        public static async Task TestStart(ImportModel model)
        {
            if (model == null) return;
            if (!org) await TestORG(model);

            var dicSeg  = model.AllFlows.SelectMany(s=>s.Nodes).Distinct().Cast<MSeg>().ToDictionary(d => d);
            var dicEdge = model.AllFlows.SelectMany(s=>s.Edges).Cast<MEdge>().ToDictionary(d => d);

            await Task.Run(async () =>
            {
                List<MEdge> edges = model.AllFlows.SelectMany(s => s.Edges).Cast<MEdge>().ToList();
                var segs = edges.SelectMany(s => s.Nodes).ToList();
                segs.AddRange(dicSeg.Values);

                foreach (var cont in segs.Distinct())
                {
                    if (dicSeg.Count() == 0) break;

                    List<MSeg> heads = getHeads(dicSeg, dicEdge);
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

