using DevExpress.Accessibility;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using Dual.Common.Core;

using Engine.Core;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Subjects;
using System.Reflection;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;

namespace DSModeler
{

    public static class LogicLog
    {
        
        public static List<ValueLog> ValueLogs { get; set; }  = new List<ValueLog>();

        private static object _lock = new object();
        public static void TryAdd(ValueLog v)
        {
            lock (_lock)
            {
                ValueLogs.Add(v);
            }
        }
        public static void InitControl(GridLookUpEdit gle, GridView gv)
        {
            gle.Properties.DisplayMember = "Name";

            gv.PreviewLineCount = 20;
            gv.OptionsView.ShowAutoFilterRow = true;
            gv.OptionsView.ShowGroupPanel = false;
        }
    }

    public class ValueLog
    {
        public ValueLog() { }   
        private DateTime time  = DateTime.Now;
        public string Time  => time.ToString("HH:mm:ss.fff");
        public string Name { get; set; }
        public string Value { get; set; }
        public string System { get; set; }
        public string TagKind { get; set; }
    }

  

}


