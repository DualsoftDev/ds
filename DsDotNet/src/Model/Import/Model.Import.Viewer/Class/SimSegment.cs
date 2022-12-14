using DocumentFormat.OpenXml.Wordprocessing;
using Engine.Common;
using Engine.Core;
using Microsoft.Msagl.Core.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Engine.Core.CoreModule;
using static Engine.Cpu.RunTime;
using static Model.Import.Office.InterfaceClass;
using static Model.Import.Office.ViewModule;

namespace Dual.Model.Import
{
    public static class SimSeg
    {

        internal static async Task TestStart(DsCPU cpu, CancellationTokenSource cts)
        {
            await Task.Run(async () =>
               {
                   while (!cts.IsCancellationRequested)
                   {
                       cpu.Update();
                       await Task.Delay(100);
                   }
               });
        }

        //private static void Update(ViewNode viewNode, UCView ucView)
        //{
        //    ucView.Update(viewNode);
        //}
        //sys.Flows.ForEach(f =>
        //{
        //    var ucView = _DicMyUI[f].Tag as UCView;
        //    f.Graph.Vertices.ForEach(v =>
        //    {
        //        Update(v, ucView);
        //        if (v is Real)
        //            ((Real)v).Graph.Vertices.ForEach(c => Update(c, ucView));

        //    });
        //});
        //private static async Task Test(IEnumerable<MSeg> rootSegs, Status4 status, List<NodeType> showList)
        //{
        //    foreach (var seg in rootSegs)
        //    {
        //        await Task.Run(async () =>
        //         {
        //             if (showList.Contains(seg.NodeType))
        //             {
        //                 seg.SetStatus(status);
        //                 await Task.Delay(1);
        //             }
        //         });
        //    }
        //}

        //public static async Task TestORG(ImportModel model)
        //{
        //    if (model == null) return;

        //    var segs = model.AllFlows.SelectMany(s => s.Nodes).Cast<MSeg>();

        //    await Task.Run(async () =>
        //    {
        //        await Test(segs, Status4.Homing, AllSeg);
        //        await Task.Delay(5);

        //        await Test(segs, Status4.Ready, AllSeg);
        //        await Task.Delay(5);
        //        await Test(segs, Status4.Ready, notMys);
        //        await Task.Delay(5);
        //        await Test(segs, Status4.Ready, mys);
        //    });

        //    org = true;
        //}

        //public static async Task TestStart(ImportModel model)
        //{
        //    if (model == null) return;
        //    if (!org) await TestORG(model);

        //    var dicSeg = model.AllFlows.SelectMany(s => s.Nodes).Distinct().Cast<MSeg>().ToDictionary(d => d);

        //    await Task.Run(async () =>
        //    {
        //        foreach (var cont in dicSeg.Values)
        //        {
        //            if (dicSeg.Count() == 0) break;

        //            //List<MSeg> heads = cont.ChildFlow.HeadNodes.Cast<MSeg>().ToList();
        //            //await Test(heads, Status4.Going, AllSeg);
        //            await Task.Delay(5);

        //            //await runSeg(cont, heads, cont.ChildFlow);
        //            //await Test(heads, Status4.Finish, AllSeg);
        //        }
        //    });

        //    org = false;
        //}

        //private static async Task runSeg(MSeg parent, List<MSeg> heads, MFlow flow)
        //{
        //    if (parent != null)
        //        await GoingFinish(new List<MSeg>() { parent });

        //    await GoingFinish(heads);
        //    foreach (var curr in heads)
        //    {
        //        //List<MSeg> nextHeads = childFlow.NextNodes(curr).Cast<MSeg>().ToList();
        //        //await runSeg(null, nextHeads, childFlow);
        //    }

        //}

        //private static async Task GoingFinish(List<MSeg> heads)
        //{
        //    await Task.Run(async () =>
        //    {
        //        await Test(heads, Status4.Going, AllSeg);
        //        await Task.Delay(5);
        //        await Test(heads, Status4.Finish, AllSeg);
        //    });
        //}
    }

}

