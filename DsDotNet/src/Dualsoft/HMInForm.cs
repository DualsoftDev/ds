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
using DevExpress.XtraBars.Docking2010;
using DevExpress.Utils;
using System.Media;
using static Engine.CodeGenCPU.SystemManagerModule;
using static Engine.CodeGenCPU.TagManagerModule;

namespace DSModeler
{
    public partial class HMIForm : XtraForm
    {

        WindowsUIButton _btnMode = new WindowsUIButton("모드", true, null, ButtonStyle.CheckButton, "운전모드", 0, true, null, true, false, true, null, 0, true);
        WindowsUIButton _btnState = new WindowsUIButton("상태", true, null, ButtonStyle.CheckButton, "운전상태", 0, true, null, true, false, true, null, 0, true);
        WindowsUIButton _btnStop = new WindowsUIButton("정지", true, null, ButtonStyle.CheckButton, "정지모드", 0, true, null, true, false, true, null, 0, true);


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

            _btnMode.Appearance.BackColor = Color.Transparent;
            _btnMode.Appearance.Options.UseBackColor = true;

            _btnState.Appearance.BackColor = Color.Transparent;
            _btnState.Appearance.Options.UseBackColor = true;

            _btnStop.Appearance.BackColor = Color.Transparent;
            _btnStop.Appearance.Options.UseBackColor = true;
        }

        private DsHMIDataSource CreateHMIData(DsSystem sys)
        {
            this.windowsUIView.Caption = $"{sys.Name} 시스템 조작 화면\t\t\t          ";
            DsHMIDataModel hmi = new DsHMIDataModel();
            hmi.CreateSystem(sys);
            foreach (var flow in sys.Flows)
            {
                hmi.CreateFlow(flow, flow.Name, flow.Name, "subtitle", "img", "descr");

                foreach (var real in flow.Graph.Vertices.OfType<Real>())
                {
                    var rm = (real.TagManager as VertexManager);
                    var realData = new DsHMIDataReal(rm.SF, real.Name, real.Name, "description", "content", flow.Name);
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

                //$"{group.Title} - 각 Work 수동 시작 버튼";
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


                    if (!(item is DsHMIDataBtn))
                    {
                        tile.Appearances.Pressed.BackColor = Color.LimeGreen;
                        tile.Appearances.Hovered.BackColor = Color.LimeGreen;
                        tile.Properties.ItemSize = DevExpress.XtraEditors.TileItemSize.Medium;
                        tile.Appearances.Normal.Font = new System.Drawing.Font("Segoe UI Light", 22);
                    }
                }
            }

            tileContainerDS.Buttons.Add(_btnMode);
            tileContainerDS.Buttons.Add(_btnState);
            tileContainerDS.Buttons.Add(_btnStop);
           
            //windowsUIButtonPanel1.Buttons.AddRange(new WindowsUIButton[] { checkBtn1, checkBtn2, checkBtn3, pushBtn1 });
            //windowsUIButtonPanel1.Buttons.Insert(3, new WindowsUISeparator());
            //tileContainerDS.Buttons[pg.Caption].Properties.Tag = pg;


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
            

            //(tileContainerFlow as ITileControl).OnItemClick(s=>s);

            tile.Press += (s, e) =>
            {
                if (e.Tile.Tag is DsHMIDataBtn)
                {
                    SystemSounds.Beep.Play();

                    var btn = (e.Tile.Tag as DsHMIDataBtn);
                    if (btn.Description != "ON")
                        btn.Description = "ON";
                    else
                        btn.Description = "OFF";

                    switch (btn.Title)
                    {
                        case "Auto": e.Tile.BackgroundImage = btn.Description == "OFF" ? Resources.btn_OffAuto : Resources.btn_OnAuto;        
                            if (btn.Description == "ON") { _btnMode.Caption = "자동모드"; _btnMode.Checked = true;} break;
                        case "Manual": e.Tile.BackgroundImage = btn.Description == "OFF" ? Resources.btn_OffManual : Resources.btn_OnManual;    
                            if (btn.Description == "ON") {_btnMode.Caption = "수동모드";  _btnMode.Checked = false; }  break;
                        case "Drive": e.Tile.BackgroundImage = btn.Description == "OFF" ? Resources.btn_OffDrive : Resources.btn_OnDrive;      
                            if (btn.Description == "ON") {_btnState.Caption = "운전중"; _btnState.Checked = true; } break;
                        case "Test": e.Tile.BackgroundImage = btn.Description == "OFF" ? Resources.btn_OffTest : Resources.btn_OnTest;          
                            if (btn.Description == "ON") {_btnState.Caption = "시운전중"; _btnState.Checked = false; }   break;
                        case "Home": e.Tile.BackgroundImage = btn.Description == "OFF" ? Resources.btn_OffHome : Resources.btn_OnHome;          
                            if (btn.Description == "ON") {_btnState.Caption = "원위치"; _btnState.Checked = false; } break;
                        case "Ready": e.Tile.BackgroundImage = btn.Description == "OFF" ? Resources.btn_OffReady : Resources.btn_OnReady;      
                            if (btn.Description == "ON") {_btnState.Caption = "준비상태"; _btnState.Checked = false; }   break;
                        case "Clear": e.Tile.BackgroundImage = btn.Description == "OFF" ? Resources.btn_OffClear : Resources.btn_OnClear;      
                            if (btn.Description == "ON") {_btnState.Caption = "해지상태"; _btnState.Checked = false; }   break;
                        case "Emg": e.Tile.BackgroundImage = btn.Description == "OFF" ? Resources.btn_OffEmg : Resources.btn_OnEmg;             
                            if (btn.Description == "ON") {_btnStop.Caption = "비상상태"; _btnStop.Checked = true; }  break;
                        case "Stop": e.Tile.BackgroundImage = btn.Description == "OFF" ? Resources.btn_OffStop : Resources.btn_OnStop;         
                            if (btn.Description == "ON") {_btnStop.Caption = "스톱상태"; _btnStop.Checked = true; } break;
                        default: break;
                    };
                }
             
            };

            tile.Click += (s, e) =>
            {
                if (e.Tile.Tag is DsHMIDataBtn)
                {
                    var btn = (e.Tile.Tag as DsHMIDataBtn);
                    bool bOn = btn.Description == "ON";
                    btn.Storage.BoxedValue = bOn;   
                }
                if (e.Tile.Tag is DsHMIDataReal)
                {
                    var real = (e.Tile.Tag as DsHMIDataReal);

                    real.Storage.BoxedValue = !Convert.ToBoolean(real.Storage.BoxedValue);
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
