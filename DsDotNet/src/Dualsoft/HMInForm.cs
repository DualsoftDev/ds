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
                tileContainerFlow.Items.Iter(tile => tile.BackgroundImage = Resources.btn_OffHome); ;
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
                tileContainerFlow.Buttons.Add(new DevExpress.XtraBars.Docking2010.WindowsUIButton(group.Title, null, -1, DevExpress.XtraBars.Docking2010.ImageLocation.AboveText, DevExpress.XtraBars.Docking2010.ButtonStyle.PushButton, null, true, -1, true, null, false, false, true, null, group, -1, false, false));
                PageGroup pageGroup = new PageGroup();
                pageGroup.Parent = tileContainerFlow;
                pageGroup.Caption = group.Title;
                windowsUIView.ContentContainers.Add(pageGroup);
                groupsItemDetailPage.Add(group, CreateGroupItemDetailPage(group, pageGroup));
                foreach (DsHMIDataCommon item in group.Items)
                {
                    ItemDetailPage itemDetailPage = new ItemDetailPage(item);
                    itemDetailPage.Dock = System.Windows.Forms.DockStyle.Fill;
                    BaseDocument document = windowsUIView.AddDocument(itemDetailPage);
                    document.Caption = item.Title;
                    pageGroup.Items.Add(document as Document);
                    CreateTile(document as Document, item).ActivationTarget = pageGroup;
                }
            }
            windowsUIView.ActivateContainer(tileContainerFlow);
            tileContainerFlow.ButtonClick += new DevExpress.XtraBars.Docking2010.ButtonEventHandler(buttonClick);
        }

        
        Tile CreateTile(Document document, DsHMIDataCommon item)
        {
            Tile tile = new Tile();
            //tile.Document = document;
            tile.Group = item.GroupName;
            tile.BackgroundImage = item.Image;
            tile.Properties.BackgroundImageScaleMode = TileItemImageScaleMode.Stretch;

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


            tile.Press += (s, e) =>
            {

                switch ((e.Tile.Tag as DsHMIDataBtn).Title)
                {
                    case "Auto": e.Tile.BackgroundImage = Resources.btn_PushAuto; break;
                    case "Manual": e.Tile.BackgroundImage = Resources.btn_PushManual; break;

                    default: break;
                };
             
            };
          

            windowsUIView.Tiles.Add(tile);
            tileContainerFlow.Items.Add(tile);
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
                page.Parent = tileContainerFlow;
                page.SetSelected((e.Tile as Tile).Document);
            }
        }
        PageGroup CreateGroupItemDetailPage(DsHMIDataFlow group, PageGroup child)
        {
            GroupDetailPage page = new GroupDetailPage(group, child);
            PageGroup pageGroup = page.PageGroup;
            BaseDocument document = windowsUIView.AddDocument(page);
            pageGroup.Parent = tileContainerFlow;
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
                windowsUIView.ActivateContainer(groupsItemDetailPage[tileGroup]);
            }
        }
    }
}
