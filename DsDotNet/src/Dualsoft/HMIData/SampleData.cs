using DevExpress.XtraBars.Docking2010.Views.WindowsUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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
        string subtitleCore, imagePathCore, descriptionCore, titleCore;
        public string Title { get { return titleCore; } }
        public string Subtitle { get { return subtitleCore; } }
        public string ImagePath { get { return imagePathCore; } }
        public string Description { get { return descriptionCore; } }
        public DsHMIDataCommon(string title, string subtitle, string imagePath, string description)
        {
            titleCore = title;
            subtitleCore = subtitle;
            imagePathCore = imagePath;
            descriptionCore = description;
        }
        public DsHMIDataCommon() { }
    }
    /// <summary>
    /// Generic item data model.
    /// </summary>
    public class DsHMIDataReal : DsHMIDataCommon
    {
        string contentCore, flowNameCore;
        public DsHMIDataReal(string title, string subtitle, string imagePath, string description, string content, string flowName)
            : base(title, subtitle, imagePath, description)
        {
            contentCore = content;
            flowNameCore = flowName;
        }
        public string Content { get { return contentCore; } }
        public string FlowName { get { return flowNameCore; } }
    }
    /// <summary>
    /// Generic item data model.
    /// </summary>
    public class DsHMIDataBtn : DsHMIDataCommon
    {
        public DsHMIDataBtn(string title, string subtitle, string imagePath, string description)
            : base(title, subtitle, imagePath, description)
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
        public DsHMIDataFlow(string name, string title, string subtitle, string imagePath, string description)
            : base(title, subtitle, imagePath, description)
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
            string flowName = tile.FlowName == null ? "" : tile.FlowName;
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
        public void CreateFlow(string name, string title, string subtitle, string imagePath, string description)
        {
            if (ContainsFlow(name)) return;
            DsHMIDataFlow flow = new DsHMIDataFlow(name, title, subtitle, imagePath, description);
            flowsCore.Add(flow);
        }
        public void CreateSystem(DsSystem sys)
        {
            DsHMIDataFlow flow = new DsHMIDataFlow(sys.Name, sys.Name, sys.Name, "", sys.HostIp);
            flow.AddItem(new DsHMIDataBtn("Auto", "A", "", ""));
            flow.AddItem(new DsHMIDataBtn("Maunual", "A", "", ""));
            flow.AddItem(new DsHMIDataBtn("Drive", "A", "", ""));
            flow.AddItem(new DsHMIDataBtn("Stop", "A", "", ""));
            flow.AddItem(new DsHMIDataBtn("Clear", "A", "", ""));
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
            //String ITEM_CONTENT = String.Format("Item Content: {0}\n\n{0}\n\n{0}\n\n{0}\n\n{0}\n\n{0}",
            //            "Curabitur class aliquam vestibulum nam curae maecenas sed integer cras phasellus suspendisse quisque donec dis praesent accumsan bibendum pellentesque condimentum adipiscing etiam consequat vivamus dictumst aliquam duis convallis scelerisque est parturient ullamcorper aliquet fusce suspendisse nunc hac eleifend amet blandit facilisi condimentum commodo scelerisque faucibus aenean ullamcorper ante mauris dignissim consectetuer nullam lorem vestibulum habitant conubia elementum pellentesque morbi facilisis arcu sollicitudin diam cubilia aptent vestibulum auctor eget dapibus pellentesque inceptos leo egestas interdum nulla consectetuer suspendisse adipiscing pellentesque proin lobortis sollicitudin augue elit mus congue fermentum parturient fringilla euismod feugiat");
            //dataCore.CreateFlow("Flow-1",
            //        "Flow Title: 1",
            //        "Flow Subtitle: 1",
            //        typeof(HMIForm).Namespace + ".Assets.DarkGray.png",
            //        "Flow Description: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus tempor scelerisque lorem in vehicula. Aliquam tincidunt, lacus ut sagittis tristique, turpis massa volutpat augue, eu rutrum ligula ante a ante");
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 1",
            //        "Item Subtitle: 1",
            //        typeof(HMIForm).Namespace + ".Assets.LightGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        "Flow-1"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 2",
            //        "Item Subtitle: 2",
            //        typeof(HMIForm).Namespace + ".Assets.DarkGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        "Flow-1"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 3",
            //        "Item Subtitle: 3",
            //        typeof(HMIForm).Namespace + ".Assets.MediumGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        "Flow-1"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 4",
            //        "Item Subtitle: 4",
            //        typeof(HMIForm).Namespace + ".Assets.DarkGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        "Flow-1"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 5",
            //        "Item Subtitle: 5",
            //        typeof(HMIForm).Namespace + ".Assets.MediumGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        "Flow-1"));

            //dataCore.CreateFlow("Flow-2",
            //        "Flow Title: 2",
            //        "Flow Subtitle: 2",
            //        typeof(HMIForm).Namespace + ".Assets.LightGray.png",
            //        "Flow Description: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus tempor scelerisque lorem in vehicula. Aliquam tincidunt, lacus ut sagittis tristique, turpis massa volutpat augue, eu rutrum ligula ante a ante");
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 1",
            //        "Item Subtitle: 1",
            //        typeof(HMIForm).Namespace + ".Assets.DarkGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        "Flow-2"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 2",
            //        "Item Subtitle: 2",
            //        typeof(HMIForm).Namespace + ".Assets.MediumGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        "Flow-2"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 3",
            //        "Item Subtitle: 3",
            //        typeof(HMIForm).Namespace + ".Assets.LightGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        "Flow-2"));

            //dataCore.CreateFlow("Flow-3",
            //        "Flow Title: 3",
            //        "Flow Subtitle: 3",
            //        typeof(HMIForm).Namespace + ".Assets.MediumGray.png",
            //        "Flow Description: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus tempor scelerisque lorem in vehicula. Aliquam tincidunt, lacus ut sagittis tristique, turpis massa volutpat augue, eu rutrum ligula ante a ante");
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 1",
            //        "Item Subtitle: 1",
            //        typeof(HMIForm).Namespace + ".Assets.MediumGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        "Flow-3"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 2",
            //        "Item Subtitle: 2",
            //        typeof(HMIForm).Namespace + ".Assets.LightGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        "Flow-3"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 3",
            //        "Item Subtitle: 3",
            //        typeof(HMIForm).Namespace + ".Assets.DarkGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //        "Flow-3"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 4",
            //        "Item Subtitle: 4",
            //        typeof(HMIForm).Namespace + ".Assets.LightGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //           "Flow-3"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 5",
            //        "Item Subtitle: 5",
            //        typeof(HMIForm).Namespace + ".Assets.MediumGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //           "Flow-3"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 6",
            //        "Item Subtitle: 6",
            //        typeof(HMIForm).Namespace + ".Assets.DarkGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //            "Flow-3"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 7",
            //        "Item Subtitle: 7",
            //        typeof(HMIForm).Namespace + ".Assets.MediumGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //             "Flow-3"));

            //dataCore.CreateFlow("Flow-4",
            //        "Flow Title: 4",
            //        "Flow Subtitle: 4",
            //        typeof(HMIForm).Namespace + ".Assets.LightGray.png",
            //        "Flow Description: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus tempor scelerisque lorem in vehicula. Aliquam tincidunt, lacus ut sagittis tristique, turpis massa volutpat augue, eu rutrum ligula ante a ante");
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 1",
            //        "Item Subtitle: 1",
            //        typeof(HMIForm).Namespace + ".Assets.DarkGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //              "Flow-4"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 2",
            //        "Item Subtitle: 2",
            //        typeof(HMIForm).Namespace + ".Assets.LightGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //              "Flow-4"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 3",
            //        "Item Subtitle: 3",
            //        typeof(HMIForm).Namespace + ".Assets.DarkGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //              "Flow-4"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 4",
            //        "Item Subtitle: 4",
            //        typeof(HMIForm).Namespace + ".Assets.LightGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //              "Flow-4"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 5",
            //        "Item Subtitle: 5",
            //        typeof(HMIForm).Namespace + ".Assets.MediumGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //              "Flow-4"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 6",
            //        "Item Subtitle: 6",
            //        typeof(HMIForm).Namespace + ".Assets.LightGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //              "Flow-4"));

            //dataCore.CreateFlow("Flow-5",
            //        "Flow Title: 5",
            //        "Flow Subtitle: 5",
            //        typeof(HMIForm).Namespace + ".Assets.MediumGray.png",
            //        "Flow Description: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus tempor scelerisque lorem in vehicula. Aliquam tincidunt, lacus ut sagittis tristique, turpis massa volutpat augue, eu rutrum ligula ante a ante");
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 1",
            //        "Item Subtitle: 1",
            //        typeof(HMIForm).Namespace + ".Assets.LightGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //              "Flow-5"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 2",
            //        "Item Subtitle: 2",
            //        typeof(HMIForm).Namespace + ".Assets.DarkGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //              "Flow-5"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 3",
            //        "Item Subtitle: 3",
            //        typeof(HMIForm).Namespace + ".Assets.LightGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //              "Flow-5"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 4",
            //        "Item Subtitle: 4",
            //        typeof(HMIForm).Namespace + ".Assets.MediumGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //              "Flow-5"));

            //dataCore.CreateFlow("Flow-6",
            //        "Flow Title: 6",
            //        "Flow Subtitle: 6",
            //        typeof(HMIForm).Namespace + ".Assets.DarkGray.png",
            //        "Flow Description: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus tempor scelerisque lorem in vehicula. Aliquam tincidunt, lacus ut sagittis tristique, turpis massa volutpat augue, eu rutrum ligula ante a ante");
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 1",
            //        "Item Subtitle: 1",
            //        typeof(HMIForm).Namespace + ".Assets.LightGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //              "Flow-6"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 2",
            //        "Item Subtitle: 2",
            //        typeof(HMIForm).Namespace + ".Assets.DarkGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //              "Flow-6"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 3",
            //        "Item Subtitle: 3",
            //        typeof(HMIForm).Namespace + ".Assets.MediumGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //              "Flow-6"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 4",
            //        "Item Subtitle: 4",
            //        typeof(HMIForm).Namespace + ".Assets.DarkGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //              "Flow-6"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 5",
            //        "Item Subtitle: 5",
            //        typeof(HMIForm).Namespace + ".Assets.LightGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //               "Flow-6"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 6",
            //        "Item Subtitle: 6",
            //        typeof(HMIForm).Namespace + ".Assets.MediumGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //              "Flow-6"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 7",
            //        "Item Subtitle: 7",
            //        typeof(HMIForm).Namespace + ".Assets.DarkGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //              "Flow-6"));
            //dataCore.AddItem(new DsHMIDataReal("Item Title: 8",
            //        "Item Subtitle: 8",
            //        typeof(HMIForm).Namespace + ".Assets.LightGray.png",
            //        "Item Description: Pellentesque porta, mauris quis interdum vehicula, urna sapien ultrices velit, nec venenatis dui odio in augue. Cras posuere, enim a cursus convallis, neque turpis malesuada erat, ut adipiscing neque tortor ac erat.",
            //        ITEM_CONTENT,
            //              "Flow-6"));
        }
        public DsHMIDataModel Data { get { return _dataCore; } }
    }
}
