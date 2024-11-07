using System;
using System.Windows.Forms;
using System.IO;
using Dual.Common.Core;
using Engine.Core;
using Engine.Runtime;
using static Engine.Runtime.DsPropertyTreeModule;
using static Engine.Runtime.DsPropertyTreeExt;
using static Engine.Core.RuntimeGeneratorModule;
using static Engine.Runtime.DsPropertyModule;
using System.Diagnostics;
using static Engine.Runtime.DsPropertyBaseModule;
using System.ComponentModel;
using System.Threading.Channels;
using System.Collections;
using System.Reflection;

namespace DSModeler
{
    public partial class FormProperties : Form
    {
        private DsTreeNode? _RootTree;

        public FormProperties()
        {
            InitializeComponent();
            InitializeEvents();
        }

        private void InitializeEvents()
        {
            treeView1.NodeMouseClick += (s, e) =>
            {
                propertyGrid1.SelectedObject = e.Node.Tag;
            
            };
            treeView1.AfterSelect += (s, e) =>
            {
                if (e.Node != null)
                    propertyGrid1.SelectedObject = e.Node.Tag;

            };
            KeyPreview = true;
            this.KeyDown += FormProperties_KeyDown;
        }

        private void FormProperties_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F4 && sender != null)
            {
                OpenToolStripMenuItem_Click(sender, e);
                e.Handled = true;
            }
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var filePath = GetFilePath("DSZ files (*.dsz)|*.dsz|All files (*.*)|*.*", "Open DSZ 파일");
            if (IsValidDszFile(filePath))
            {
                LoadRuntimeModel(filePath);
            }
            else
            {
                ShowMessage("올바른 DSZ 파일을 선택해 주세요.", "오류");
            }
        }

        private void exportJsonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_RootTree == null)
            {
                ShowMessage("파일을 열어주세요.");
                return;
            }

            var savePath = GetFilePath("JSON files (*.json)|*.json|All files (*.*)|*.*", "Export JSON 파일 저장", true);
            if (!string.IsNullOrEmpty(savePath))
            {
                DsPropertyExt.ExportPropertyToJson(savePath, _RootTree);
                ShowMessage($"파일이 {savePath}에 저장되었습니다.", "저장 완료");
            }
        }

        private void importJsonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var filePath = GetFilePath("JSON files (*.json)|*.json|All files (*.*)|*.*", "Import JSON 파일 선택");
            if (!string.IsNullOrEmpty(filePath))
            {
                ImportJsonFile(filePath);
            }
        }

        private void LoadRuntimeModel(string filePath)
        {
            try
            {
                var runtimeModel = new RuntimeModel(filePath, PlatformTarget.WINDOWS);
                _RootTree = DsPropertyTreeExt.GetPropertyTree(runtimeModel);
                PopulateTreeView(_RootTree);
                ShowMessage("DSZ 파일이 성공적으로 열렸습니다.", "파일 열기 완료");
            }
            catch (Exception ex)
            {
                ShowMessage($"파일을 읽는 중 오류가 발생했습니다: {ex.Message}", "오류");
            }
        }

        private void ImportJsonFile(string filePath)
        {
            var importedTree = DsPropertyExt.ImportPropertyFromJson<DsTreeNode>(filePath);
            if (importedTree != null)
            {
                _RootTree = importedTree;
                PopulateTreeView(_RootTree);
                ShowMessage("파일이 성공적으로 불러와졌습니다.", "불러오기 완료");
            }
            else
            {
                ShowMessage("파일 형식이 잘못되었거나 파일을 읽을 수 없습니다.", "오류");
            }
        }

        private bool IsValidDszFile(string filePath)
        {
            return !string.IsNullOrEmpty(filePath) && Path.GetExtension(filePath).ToLower() == ".dsz";
        }

        private void PopulateTreeView(DsTreeNode rootTree)
        {
            treeView1.Nodes.Clear();
            treeView1.Nodes.Add(ConvertToTreeViewNode(rootTree));
            var lst = ConvertToINotifyProperty(rootTree, new List<INotifyPropertyChanged>());   
            foreach (var item in lst)
            {
                item.PropertyChanged += (s, e) =>
                {
                    Console.WriteLine($"Changed{e.PropertyName} ({s})");

                    if (s != null && e.PropertyName != null)
                    {
                        var property = s.GetType().GetProperty(e.PropertyName);

                        if (property != null)
                        {
                            var value = property.GetValue(s);
                            // value가 ICollection 타입인지 확인
                            if (value is ICollection)
                            {
                                // Collection 타입일 경우 propertyGrid1을 새로고침합니다.
                                propertyGrid1.Refresh();
                            }
                        }
                    }
                };
            }
            treeView1.ExpandAll();
        }

        private List<INotifyPropertyChanged> ConvertToINotifyProperty(DsTreeNode tree, List<INotifyPropertyChanged> lst)
        {
            lst.Add(tree.Node as INotifyPropertyChanged);   
            foreach (var child in tree.Children)
            {
                ConvertToINotifyProperty(child, lst);
            }
            return lst;
        }

        private TreeNode ConvertToTreeViewNode(DsTreeNode tree)
        {
            var node = new TreeNode(tree.Node.Name) { Tag = tree.Node };
            foreach (var child in tree.Children)
            {
                node.Nodes.Add(ConvertToTreeViewNode(child));
            }
            return node;
        }


        private string GetFilePath(string filter, string title, bool isSaveDialog = false)
        {
            FileDialog dialog = isSaveDialog ? (FileDialog)new SaveFileDialog() : new OpenFileDialog();
            dialog.Filter = filter;
            dialog.Title = title;
            dialog.FilterIndex = 1;
            dialog.RestoreDirectory = true;
            return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : string.Empty;
        }

        private void ShowMessage(string message, string caption = "알림")
        {
            MessageBox.Show(message, caption);
        }
    }
}
