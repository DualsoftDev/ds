using DevExpress.XtraBars.Docking2010.Views;
using DevExpress.XtraBars.Docking2010.Views.WindowsUI;
using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static DevExpress.Skins.SolidColorHelper;
using System.Web.UI.WebControls;
using static Engine.Core.CoreModule;
using DSModeler.Properties;
using System.Runtime.CompilerServices;
using Dual.Common.Core;

namespace DSModeler
{
    public partial class HMIForm : XtraForm
    {
        DsHMIDataSource _dataSource;
        Dictionary<DsHMIDataFlow, PageGroup> groupsItemDetailPage;
        public HMIForm(DsSystem sys)
        {
            InitializeComponent();
            windowsUIView.AddTileWhenCreatingDocument = DevExpress.Utils.DefaultBoolean.False;
            groupsItemDetailPage = new Dictionary<DsHMIDataFlow, PageGroup>();
            _dataSource = CreateHMIData(sys);
            CreateLayout();
            this.KeyPreview = true;
            this.MouseUp += (s, e) =>
            {
                tileContainerDS.Items.Iter(tile => tile.BackgroundImage = Resources.btn_OffHome); ;
            };
            
        }

        private DsHMIDataSource CreateHMIData(DsSystem sys)
        {
            this.windowsUIView.Caption = sys.Name;
            DsHMIDataModel hmi = new DsHMIDataModel();
            hmi.CreateSystem(sys);
            foreach (var flow in sys.Flows)
            {
                hmi.CreateFlow(flow.Name, flow.Name, "subtitle", "img", "descr");

                foreach (var real in flow.Graph.Vertices.OfType<Real>())
                {
                    var realData = new DsHMIDataReal(real.Name, real.Name, "description", "content", flow.Name);
                    hmi.AddRealItem(realData);
                }
            }

            return new DsHMIDataSource(hmi);
        }

        void CreateLayout()
        {
            foreach (DsHMIDataFlow group in _dataSource.Data.Flows)
            {
                tileContainerDS.Buttons.Add(new DevExpress.XtraBars.Docking2010.WindowsUIButton(group.Title, null, -1, DevExpress.XtraBars.Docking2010.ImageLocation.AboveText, DevExpress.XtraBars.Docking2010.ButtonStyle.PushButton, null, true, -1, true, null, false, false, true, null, group, -1, false, false));
                PageGroup pageGroup = new PageGroup();
                pageGroup.Parent = tileContainerDS;
                pageGroup.Caption = group.Title;
                windowsUIView.ContentContainers.Add(pageGroup);
                groupsItemDetailPage.Add(group, CreateGroupItemDetailPage(group, pageGroup));
                foreach (DsHMIDataCommon item in group.Items)
                {
                    ItemDetailPage itemDetailPage = new ItemDetailPage(item);
                    itemDetailPage.Dock = System.Windows.Forms.DockStyle.Fill;
                    //BaseDocument document = windowsUIView.AddDocument(itemDetailPage);
                    //document.Caption = item.Title;
                    //pageGroup.Items.Add(document as Document);
                    var tile = CreateTile(item);//


                    if(!(item is DsHMIDataBtn))
                    {
                        tile.ActivationTarget = pageGroup;
                    }

                }
            }
            windowsUIView.ActivateContainer(tileContainerDS);
            tileContainerDS.ButtonClick += new DevExpress.XtraBars.Docking2010.ButtonEventHandler(buttonClick);
        }

        
        Tile CreateTile(DsHMIDataCommon item)
        {
            Tile tile = new Tile();
            //tile.Document = document;
            tile.Group = item.GroupName;
            tile.BackgroundImage = item.Image;
            tile.Properties.BackgroundImageScaleMode = TileItemImageScaleMode.Stretch;
            //tile.Enabled =  false;
            tile.Tag = item;
            tile.Elements.Add(CreateTileItemElement(item.Subtitle, TileItemContentAlignment.MiddleCenter, Point.Empty, 20));
            tile.Appearances.Selected.BackColor = tile.Appearances.Hovered.BackColor = tile.Appearances.Normal.BackColor = Color.FromArgb(140, 140, 140);
            tile.Appearances.Selected.BorderColor = tile.Appearances.Hovered.BorderColor = tile.Appearances.Normal.BorderColor = Color.FromArgb(140, 140, 140);
            //tile.Click += new TileClickEventHandler(tile_Click);
            //tile.Click += (s, e) =>
            //{
            //    e.Tile.BackgroundImage = Resources.btn_PushAuto;
            //};

            //(tileContainerFlow as ITileControl).OnItemClick(s=>s);

            tile.Click += (s, e) =>
            {
                
            };

            



            tile.Press += (s, e) =>
            {
                if (e.Tile.Tag is DsHMIDataBtn)
                {
                    var btn = (e.Tile.Tag as DsHMIDataBtn);
                    if (btn.Description != "ON")
                        btn.Description = "ON";
                    else
                        btn.Description = "OFF";

                    switch (btn.Title)
                    {
                        case "Auto": e.Tile.BackgroundImage = btn.Description == "OFF" ? Resources.btn_OffAuto : Resources.btn_OnAuto; break;
                        case "Manual": e.Tile.BackgroundImage = btn.Description == "OFF" ? Resources.btn_OffManual : Resources.btn_OnManual; break;
                        case "Drive": e.Tile.BackgroundImage = btn.Description == "OFF" ? Resources.btn_OffDrive : Resources.btn_OnDrive; break;
                        case "Test": e.Tile.BackgroundImage = btn.Description == "OFF" ? Resources.btn_OffTest : Resources.btn_OnTest; break;
                        case "Home": e.Tile.BackgroundImage = btn.Description == "OFF" ? Resources.btn_OffHome : Resources.btn_OnHome; break;
                        case "Ready": e.Tile.BackgroundImage = btn.Description == "OFF" ? Resources.btn_OffReady : Resources.btn_OnReady; break;
                        case "Clear": e.Tile.BackgroundImage = btn.Description == "OFF" ? Resources.btn_OffClear : Resources.btn_OnClear; break;
                        case "Emg": e.Tile.BackgroundImage = btn.Description == "OFF" ? Resources.btn_OffEmg : Resources.btn_OnEmg; break;
                        case "Stop": e.Tile.BackgroundImage = btn.Description == "OFF" ? Resources.btn_OffStop : Resources.btn_OnStop; break;
                        default: break;
                    };
                }
             
            };
            windowsUIView.Tiles.Add(tile);
            tileContainerDS.Items.Add(tile);
            return tile;
        }
        TileItemElement CreateTileItemElement(string text, TileItemContentAlignment alignment, Point location, float fontSize)
        {
            TileItemElement element = new TileItemElement();
            element.TextAlignment = alignment;
            if (!location.IsEmpty) element.TextLocation = location;
            element.Text = text;
            return element;
        }
        void tile_Click(object sender, TileClickEventArgs e)
        {
            PageGroup page = ((e.Tile as Tile).ActivationTarget as PageGroup);
            if (page != null)
            {
                page.Parent = tileContainerDS;
                page.SetSelected((e.Tile as Tile).Document);
            }
        }
        PageGroup CreateGroupItemDetailPage(DsHMIDataFlow group, PageGroup child)
        {
            GroupDetailPage page = new GroupDetailPage(group, child);
            PageGroup pageGroup = page.PageGroup;
            BaseDocument document = windowsUIView.AddDocument(page);
            pageGroup.Parent = tileContainerDS;
            pageGroup.Properties.ShowPageHeaders = DevExpress.Utils.DefaultBoolean.False;
            pageGroup.Items.Add(document as Document);
            windowsUIView.ContentContainers.Add(pageGroup);
            windowsUIView.ActivateContainer(pageGroup);
            return pageGroup;
        }
        void buttonClick(object sender, DevExpress.XtraBars.Docking2010.ButtonEventArgs e)
        {
            DsHMIDataFlow tileGroup = (e.Button.Properties.Tag as DsHMIDataFlow);
            if (tileGroup != null)
            {
                if(groupsItemDetailPage.ContainsKey(tileGroup))
                    windowsUIView.ActivateContainer(groupsItemDetailPage[tileGroup]);
            }
        }

        private void HMIForm_Load(object sender, EventArgs e)
        {

        }

        private void HMIForm_Shown(object sender, EventArgs e)
        {
            //tileContainerDS.
        }
    }
}
