namespace DSModeler.ViewDiagram
{
    public static class ViewDraw
    {
        /// <summary>
        /// DicStatusNValue
        /// </summary>
        public static Dictionary<Vertex, Tuple<Status4, bool>> DicSV;


        public static Dictionary<DsTask, IEnumerable<Vertex>> DicTask;
        public static Subject<EventVertex> StatusChangeSubject = new();
        public static Subject<Tuple<Vertex, object>> ActionChangeSubject = new();

        public static void DrawInitStatus(TabbedView tv, Dictionary<DsSystem, CpuLoader.PouGen> dicCpu)
        {
            DicSV = new Dictionary<Vertex, Tuple<Status4, bool>>();
            foreach (KeyValuePair<DsSystem, CpuLoader.PouGen> item in dicCpu)
            {
                DsSystem sys = item.Key;
                IEnumerable<Vertex> reals = sys.GetVertices().OfType<Vertex>();
                foreach (Vertex r in reals)
                {
                    DicSV.Add(r, Tuple.Create(Status4.Homing, false));
                }
            }
        }


        public static void DrawInitActionTask(FormMain formMain, Dictionary<DsSystem, CpuLoader.PouGen> dicCpu)
        {
            DicTask = new Dictionary<DsTask, IEnumerable<Vertex>>();
            foreach (KeyValuePair<DsSystem, CpuLoader.PouGen> item in dicCpu)
            {
                DsSystem sys = item.Key;
                IEnumerable<Call> calls = sys.GetVertices().OfType<Call>();
                _ = calls.SelectMany(s => s.CallTargetJob.DeviceDefs)
                     .Distinct()
                     .Iter(d =>
                     {
                         IEnumerable<Call> finds = calls.Where(w => w.CallTargetJob.DeviceDefs.Contains(d));
                         DicTask.Add(d, finds);
                     });
            }
        }


        public static void DrawStatusNValue(ViewNode v, FormDocView view)
        {
            IEnumerable<ViewNode> viewNodes = v.UsedViewNodes.Where(w => w.CoreVertex != null);
            foreach (ViewNode f in viewNodes)
            {
                if (DicSV.ContainsKey(f.CoreVertex.Value))
                {
                    f.Status4 = DicSV[f.CoreVertex.Value].Item1;
                    var value = DicSV[f.CoreVertex.Value].Item2;
                    view.UcView.UpdateStatus(f);
                    view.UcView.UpdateValue(f, value);
                }
            }
        }
    }
}





