using DocumentFormat.OpenXml.Spreadsheet;
using Engine.Common.FS;
using Engine.Core;
using Microsoft.Msagl.Core.DataStructures;
using Model.Import.Office;
using System;
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

            var segs = model.AllFlows.SelectMany(s => s.UsedSegs).Cast<MSeg>();
         
            await Task.Run(async () =>
            {
                await Test(segs, Status4.Homing, AllSeg);
                await Task.Delay(10);

                await Test(segs, Status4.Ready, AllSeg);
                await Task.Delay(10);
                await Test(segs, Status4.Ready, notMys);
                await Task.Delay(10);
                await Test(segs, Status4.Ready, mys);
            });

            org = true;
        }

        public static async Task TestStart(ImportModel model)
        {
            if (model == null) return;
            if (!org) await TestORG(model);

            var dicSeg  = model.AllFlows.SelectMany(s=>s.Nodes).Distinct().Cast<MSeg>().ToDictionary(d => d);

            await Task.Run(async () =>
            {
                //List<MEdge> edges = model.AllFlows.SelectMany(s => s.Edges).Cast<MEdge>().ToList();
                //var segs = edges.SelectMany(s => s.Nodes).ToList();
                //segs.AddRange(dicSeg.Values);



                foreach (var cont in dicSeg.Values)
                {
                    if (dicSeg.Count() == 0) break;

                    List<MSeg> heads = cont.ChildFlow.HeadNodes.Cast<MSeg>().ToList();
                    await Test(heads, Status4.Going, AllSeg);
                    await Task.Delay(50);

                    await runSeg(cont, heads, cont.ChildFlow);
                    await Test(heads, Status4.Finish, AllSeg);
                }
            });

            org = false;
        }

        private static async Task runSeg(MSeg parent, List<MSeg> heads, CoreFlow.ChildFlow childFlow)
        {
            if (parent != null)
                await GoingFinish(new List<MSeg>() { parent });

            await GoingFinish(heads);
            foreach (var curr in heads)
            {
                List<MSeg> nextHeads = childFlow.NextNodes(curr).Cast<MSeg>().ToList();
                await runSeg(null, nextHeads, childFlow);
            }

        }

        private static async Task GoingFinish(List<MSeg> heads)
        {
            await Task.Run(async () =>
            {
                await Test(heads, Status4.Going, AllSeg);
                await Task.Delay(50);
                await Test(heads, Status4.Finish, AllSeg);
            });
        }
    }

}

