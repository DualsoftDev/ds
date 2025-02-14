using DevExpress.Skins;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Columns;
using DevExpress.XtraTreeList.Nodes;

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;


namespace Dual.Common.DevExpressLib
{
    /// <summary>
    /// DevXpress TreeList extension methods.
    /// <br/> Sibling classes : EmDXGridControl
    /// </summary>
    public static partial class EmDXTreeControl
    {
        public abstract class TreeBase : IDisposable
        {
            protected TreeList _treeList;

            public abstract void Dispose();

            protected TreeBase(TreeList treeList)
            {
                _treeList = treeList;
            }
        }

        public class TreeUpdator : TreeBase
        {
            public TreeUpdator(TreeList treeList)
                : base(treeList)
            {
                treeList.BeginUpdate();
            }

            public override void Dispose()
            {
                _treeList.EndUpdate();
            }
        }

        /// <summary>
        /// In unbound mode, prevents updates of the tree structure due to adding, deleting
        /// and modifying nodes, until the DevExpress.XtraTreeList.TreeList.EndUnboundLoad()
        /// method is called.
        /// </summary>
        public class TreeUnboundLoader : TreeBase
        {
            public TreeUnboundLoader(TreeList treeList)
                : base(treeList)
            {
                _treeList.BeginUnboundLoad();
            }

            public override void Dispose()
            {
                _treeList.EndUnboundLoad();
            }
        }

        public static TreeUpdator UpdateBeginer(this TreeList treeList)
        {
            return new TreeUpdator(treeList);
        }

        public static TreeUnboundLoader UnboundLoadBeginer(this TreeList treeList)
        {
            return new TreeUnboundLoader(treeList);
        }

        public static void AddColumns(this TreeList treeList, IEnumerable<string> columnNames)
        {
            var n = treeList.Columns.Count;
            foreach (var c in columnNames)
            {
                var column = new TreeListColumn() { Caption = c, VisibleIndex = n++ };
                treeList.Columns.Add(column);
            }
        }

        //public static void ChangeToPlusMinusExpandButton(this TreeList treeList)
        //{
        //    // https://www.devexpress.com/Support/Center/Question/Details/Q496916
        //    Skin skin = GridSkins.GetSkin(treeList.LookAndFeel);
        //    SkinElement element = skin[GridSkins.SkinPlusMinus];
        //    element.Image.SetImage(Properties.Resources.PlusMinus, Color.Gray);
        //    LookAndFeelHelper.ForceDefaultLookAndFeelChanged();
        //}

        public static void ShowHideTreeLine(this TreeList treeList, bool show)
        {
            treeList.GetCustomInfo().VisibleTreeLine = show;

            //https://www.devexpress.com/Support/Center/Question/Details/T107795
            Skin skin = GridSkins.GetSkin(treeList.LookAndFeel);
            skin.Properties[GridSkins.OptShowTreeLine] = show;
            treeList.LookAndFeel.UpdateStyleSettings();
        }

        public static void EnableMultiSelect(this TreeList treeList, bool enable = true)
        {
            // https://www.devexpress.com/Support/Center/Question/Details/Q528188
            treeList.OptionsSelection.MultiSelect = true;
        }

        public static void EnableFullRowSelect(this TreeList treeList, bool enable = true)
        {
            treeList.OptionsSelection.EnableAppearanceFocusedCell = !enable;
        }

        public static void ExpandNode(this TreeList treeList, TreeListNode node)
        {
            using (treeList.UpdateBeginer())

                treeList.ExpandNodeHelper(node);
        }

        private static void ExpandNodeHelper(this TreeList treeList, TreeListNode node)
        {
            if (node.HasChildren)
            {
                node.Expanded = true;

                foreach (TreeListNode child in node.Nodes)
                {
                    treeList.ExpandNodeHelper(child);
                }
            }
        }


        public static IEnumerable<TreeListNode> PopulateNodes(this TreeList treeList, TreeListNode start, Func<TreeListNode, bool> predicate=null)
        {
            if ( predicate != null && predicate(start))
                yield return start;

            foreach (var c in start.Nodes.Cast<TreeListNode>())
            {
                foreach (var p in PopulateNodes(treeList, c, predicate))
                    yield return p;
            }
        }

        public static void ApplyFavoriteSetting(this TreeList treeList)
        {
            Contract.Requires(treeList != null && treeList.Tag == null);
            treeList.Tag = new TreeListCustomInfo();

            treeList.EnableMultiSelect();
            treeList.EnableFullRowSelect();

            //treeList.ChangeToPlusMinusExpandButton();
            treeList.ShowHideTreeLine(show: true);

            treeList.EnableDefaultContextMenu();
        }

        #region SingleClickCheckBox // 필요한가???

        /// <summary>
        /// DevXpress gridview 상에서의 checkbox click 시, column 선택 후 toggle 이 이루어지므로,
        /// 반드시 2번 이상의 click 을 해야만 check box 를 toggle 할 수 있다.
        /// 이를 한번 click 으로 toggle 할 수 있도록 한다.
        /// </summary>
        /// <param name="gridView"></param>
        public static void EnableSingleClickCheckBox(this TreeList treeList)
        {
            treeList.GetCustomInfo().EnabledSingleClickCheckBox = true;

            treeList.MouseDown += TreeListOnMouseDownSingleClickEnabler;
        }

        public static void DisableSingleClickCheckBox(this TreeList treeList)
        {
            treeList.GetCustomInfo().EnabledSingleClickCheckBox = false;
            treeList.MouseDown -= TreeListOnMouseDownSingleClickEnabler;
        }


        public static TreeListNode GetHitNode(this TreeList treeList, Point location)
        {
            TreeListHitInfo hitInfo = treeList.CalcHitInfo(location);
            if (hitInfo == null)
                return null;
            return hitInfo.Node;
        }

        private static void TreeListOnMouseDownSingleClickEnabler(object sender, MouseEventArgs e)
        {
            // https://www.devexpress.com/Support/Center/Question/Details/K18380

            if (e.Button != MouseButtons.Left)
                return;

            var treeList = sender as TreeList;
            TreeListHitInfo hitInfo = treeList.CalcHitInfo(e.Location);
            if (hitInfo.Node != null)
            {
                if (hitInfo.Column.RealColumnEdit is RepositoryItemCheckEdit
                    || hitInfo.Column.ColumnType == typeof(object)     // <-- object type 의 symbol table 의 value column check box 를 위한 special case.
                    )
                {
                    treeList.FocusedColumn = hitInfo.Column;
                    treeList.FocusedNode = hitInfo.Node;

                    ///*
                    // * Single click check box 에서는 복수개의 선택을 허용하면,
                    // * check box 아닌 cell 을 클릭하고 check box 를 클릭할 때에, 두 cell 모두 boolean 으로 toggle 하려고 해서 문제 발생...
                    // * ==> 마지막 click 한 cell 을 제외하고 나머지는 선택 해제
                    // */
                    //treeList.ClearSelection();
                    //treeList.SelectCell(hitInfo.RowHandle, hitInfo.Column);

                    treeList.ShowEditor();
                    CheckEdit edit = treeList.ActiveEditor as CheckEdit;
                    if (edit == null) return;
                    edit.Toggle();
                    DXMouseEventArgs.GetMouseArgs(e).Handled = true;
                }
            }
        }

        #endregion SingleClickCheckBox // 필요한가???
    }
}