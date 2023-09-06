using DevExpress.Utils.Behaviors.Common;
using DevExpress.XtraBars.Docking2010.Views.WindowsUI;
using DSModeler.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using static Engine.CodeGenCPU.FlowManagerModule;
using static Engine.CodeGenCPU.SystemManagerModule;
using static Engine.CodeGenCPU.TagManagerModule;
using static Engine.Core.CoreModule;

// The data model defined by this file serves as a representative example of a strongly-typed
// model that supports notification when members are added, removed, or modified.  The property
// names chosen coincide with data bindings in the standard item WindowsUIViewApplications.
//
// Applications may use this model as a starting point and build on it, or discard it entirely and
// replace it with something appropriate to their needs.

namespace DSModeler
{
    /// <summary>
    /// Base class for <see cref="DsHMIDataReal"/> and <see cref="DsHMIDataFlow"/> that
    /// defines properties common to both.
    /// </summary>
    public class DsHMIDataCommon
    {
        string subtitleCore, descriptionCore, titleCore, groupName;
        Bitmap imageCore;
        Engine.Core.Interface.IStorage storage; 
        public string Title { get { return titleCore; } }
        public string GroupName { get { return groupName; } }
        public string Subtitle { get { return subtitleCore; } }
        public Bitmap Image { get { return imageCore; } }
        public Engine.Core.Interface.IStorage Storage { get { return storage; } }
        public string Description { get { return descriptionCore; } set { descriptionCore = value; }}
        public DsHMIDataCommon(Engine.Core.Interface.IStorage stg, string title, string subtitle, Bitmap image, string description, string group)
        {
            titleCore = title;
            subtitleCore = subtitle;
            imageCore = image;
            descriptionCore = description;
            groupName = group;
            storage = stg;
        }
        public DsHMIDataCommon() { }
    }
    /// <summary>
    /// Generic item data model.
    /// </summary>
    public class DsHMIDataReal : DsHMIDataCommon
    {
        string contentCore;
        public DsHMIDataReal(Engine.Core.Interface.IStorage stg,  string title, string subtitle, string description, string content, string flowName)
            : base(stg, title, subtitle, null, description, flowName)
        {
            contentCore = content;
        }
        public string Content { get { return contentCore; } }
    }
    /// <summary>
    /// Generic item data model.
    /// </summary>
    public class DsHMIDataBtn : DsHMIDataCommon
    {

        public DsHMIDataBtn(Engine.Core.Interface.IStorage storage, string title, string subtitle, Bitmap image, string description, string group)
            : base(storage, title, subtitle, image, description, group)
        {
        }
      
    }
    /// <summary>
    /// Generic flow data model.
    /// </summary>
    public class DsHMIDataFlow : DsHMIDataCommon
    {
        string nameCore;
        Collection<DsHMIDataCommon> itemsCore;
        public DsHMIDataFlow(string name)
            : base()
        {
            this.nameCore = name;
            itemsCore = new Collection<DsHMIDataCommon>();
        }
        public DsHMIDataFlow(Engine.Core.Interface.IStorage stg, string name, string title, string subtitle,  string description)
            : base(stg,  title, subtitle, null, description, "")
        {
            this.nameCore = name;
            itemsCore = new Collection<DsHMIDataCommon>();
        }
        public string Name { get { return nameCore; } }
        public Collection<DsHMIDataCommon> Items { get { return itemsCore; } }
        public bool AddItem(DsHMIDataReal tile)
        {
            if (!itemsCore.Contains(tile))
            {
                itemsCore.Add(tile);
                return true;
            }
            return false;
        }
        public bool AddItem(DsHMIDataBtn tile)
        {
            if (!itemsCore.Contains(tile))
            {
                itemsCore.Add(tile);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Generic data model.
    /// </summary>
    public class DsHMIDataModel
    {
        Collection<DsHMIDataFlow> flowsCore;
        public DsHMIDataModel()
        {
            flowsCore = new Collection<DsHMIDataFlow>();
        }
        public Collection<DsHMIDataFlow> Flows { get { return flowsCore; } }
        DsHMIDataFlow GetFlow(string name)
        {
            foreach (var flow in flowsCore)
                if (flow.Name == name) return flow;
            return null;
        }
        public bool AddRealItem(DsHMIDataReal tile)
        {
            if (tile == null) return false;
            string flowName = tile.GroupName == null ? "" : tile.GroupName;
            DsHMIDataFlow thisFlow = GetFlow(flowName);
            if (thisFlow == null)
            {
                thisFlow = new DsHMIDataFlow(flowName);
                flowsCore.Add(thisFlow);
            }
            return thisFlow.AddItem(tile);
        }
        bool ContainsFlow(string name)
        {
            return GetFlow(name) != null;
        }
        public void CreateFlow(Flow flow, string name, string title, string subtitle, string imagePath, string description)
        {
            if (ContainsFlow(name)) return;
            var fm = flow.TagManager as FlowManager;
            var fEmg = fm.GetFlowTag(Engine.Core.TagKindModule.FlowTag.emergency_op);

            DsHMIDataFlow dsflow = new DsHMIDataFlow(fEmg, name, title, subtitle, description);
            flowsCore.Add(dsflow);
        }
        public void CreateSystem(DsSystem sys)
        {

            var sm = (sys.TagManager as SystemManager);
            var sysEmg = sm.GetSystemTag(Engine.Core.TagKindModule.SystemTag.emg);
            DsHMIDataFlow flow = new DsHMIDataFlow(sysEmg ,"전체 조작반", "System", "", sys.HostIp);

            flowsCore.Add(flow);
            flow.AddItem(new DsHMIDataBtn(sm.GetSystemTag(Engine.Core.TagKindModule.SystemTag.auto), "Auto", "", Resources.btn_OffAuto, "", flow.Name));
            flow.AddItem(new DsHMIDataBtn(sm.GetSystemTag(Engine.Core.TagKindModule.SystemTag.manual), "Manual", "", Resources.btn_OffManual, "", flow.Name));
            flow.AddItem(new DsHMIDataBtn(sm.GetSystemTag(Engine.Core.TagKindModule.SystemTag.clear), "Clear", "", Resources.btn_OffClear, "", flow.Name));
            flow.AddItem(new DsHMIDataBtn(sm.GetSystemTag(Engine.Core.TagKindModule.SystemTag.ready), "Ready", "", Resources.btn_OffReady, "", flow.Name));
            flow.AddItem(new DsHMIDataBtn(sm.GetSystemTag(Engine.Core.TagKindModule.SystemTag.stop), "Stop", "", Resources.btn_OffStop, "", flow.Name));
            flow.AddItem(new DsHMIDataBtn(sm.GetSystemTag(Engine.Core.TagKindModule.SystemTag.drive), "Drive", "", Resources.btn_OffDrive, "", flow.Name));
            flow.AddItem(new DsHMIDataBtn(sm.GetSystemTag(Engine.Core.TagKindModule.SystemTag.test), "Test", "", Resources.btn_OffTest, "", flow.Name));
            flow.AddItem(new DsHMIDataBtn(sm.GetSystemTag(Engine.Core.TagKindModule.SystemTag.home), "Home", "", Resources.btn_OffHome, "", flow.Name));
            flow.AddItem(new DsHMIDataBtn(sm.GetSystemTag(Engine.Core.TagKindModule.SystemTag.emg), "Emg", "", Resources.btn_OffEmg, "", flow.Name));
        }
    }

    /// <summary>
    /// Creates a collection of flows and items with hard-coded content.
    /// 
    /// DsHMIDataSource initializes with placeholder data rather than live production
    /// data so that sample data is provided at both design-time and run-time.
    /// </summary>
    public class DsHMIDataSource
    {
        DsHMIDataModel _dataCore;
        public DsHMIDataSource(DsHMIDataModel dataCore)
        {
            _dataCore = dataCore;
            
        }
        public DsHMIDataModel Data { get { return _dataCore; } }
    }
}
