using System;
using System.Windows.Forms;
using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Nodes;

using Images = Dual.Common.Resources.Images;

namespace Dual.Common.DevExpressLib
{
    partial class EmDXTreeControl
    {
        public static void EnableDefaultContextMenu(this TreeList treeList, DXTreeListContextMenuFlag mode = DXTreeListContextMenuFlag.All)
        {
            treeList.AppendDefaultContextMenu(false, mode);
            treeList.MouseDown += (sender, e) =>
            {
                // https://www.devexpress.com/Support/Center/Question/Details/Q257821
                if (e.Button == MouseButtons.Right)
                {
                    TreeListHitInfo hitInfo = treeList.CalcHitInfo(e.Location);
                    if (hitInfo != null && hitInfo.Node != null)
                    {
                        /* 이미 만들어진 메뉴 항목의 checked 속성을, 현재 시점에 맞게 update 한다. */
                        var action = treeList.ContextMenuStrip.Tag as Action;
                        if (action != null)
                            action();
                    }
                }
            };
        }

        private class TreeListCustomInfo
        {
            public bool VisibleTreeLine { get; set; }
            public bool EnabledSingleClickCheckBox { get; set; }
        }

        private static TreeListCustomInfo GetCustomInfo(this TreeList treeList)
        {
            var info = treeList.Tag as TreeListCustomInfo;
            return info ?? new TreeListCustomInfo();
        }

        [Flags]
        public enum DXTreeListContextMenuFlag
        {
            AutoFilterRow = 1 << 0,
            FullRowSelectionMode = 1 << 1,
            MultiRowSelectionMode = 1 << 2,
            BestFitColumn = 1 << 3,
            EnableSingleClickCheckBox = 1 << 4,
            Expand = 1 << 5,
            Collapse = 1 << 6,
            ExpandAll = 1 << 7,
            CollapseAll = 1 << 8,
            ShowHideTreeLine = 1 << 9,
            All = AutoFilterRow | FullRowSelectionMode | MultiRowSelectionMode | BestFitColumn | EnableSingleClickCheckBox
                | Expand | Collapse | ExpandAll | CollapseAll | ShowHideTreeLine,
        }


        public static void AppendDefaultContextMenu(this TreeList treeList, bool appendSeparator = false, DXTreeListContextMenuFlag mode = DXTreeListContextMenuFlag.All)
        {
            var separator = new ToolStripSeparator();
            if (treeList.ContextMenuStrip == null)
                treeList.ContextMenuStrip = new ContextMenuStrip();

            ContextMenuStrip menu = treeList.ContextMenuStrip;
            if (menu.Items.Count > 0)
                menu.Items.Add(separator);

            ToolStripMenuItem miShowAutoFilterRow = null, miFullRowSelectionMode = null, miMultiRowSelect = null, miBestFitColumns = null;
            ToolStripMenuItem miEnableSingleClickCheck = null, miDisableSingleClickCheck = null;
            ToolStripMenuItem miShowTreeLine = null;

            if (mode.HasFlag(DXTreeListContextMenuFlag.AutoFilterRow))
            {
                miShowAutoFilterRow = new ToolStripMenuItem("show auto filter row", Images.Filter, (o, args) =>
                {
                    treeList.OptionsView.ShowAutoFilterRow = !treeList.OptionsView.ShowAutoFilterRow;
                });
                menu.Items.Add(miShowAutoFilterRow);
            }

            if (mode.HasFlag(DXTreeListContextMenuFlag.MultiRowSelectionMode))
            {
                miMultiRowSelect = new ToolStripMenuItem("Multi row selection mode", Images.TableCell, (o, args) =>
                {
                    treeList.OptionsSelection.MultiSelect = !treeList.OptionsSelection.MultiSelect;
                });
                menu.Items.Add(miMultiRowSelect);
            }

            if (mode.HasFlag(DXTreeListContextMenuFlag.FullRowSelectionMode))
            {
                miFullRowSelectionMode = new ToolStripMenuItem("Full row selection mode", Images.TableRow, (o, args) =>
                {
                    treeList.OptionsSelection.EnableAppearanceFocusedCell = ! treeList.OptionsSelection.EnableAppearanceFocusedCell;
                });
                menu.Items.Add(miFullRowSelectionMode);
            }


            if (mode.HasFlag(DXTreeListContextMenuFlag.BestFitColumn))
            {
                miBestFitColumns = new ToolStripMenuItem("Best fit columns", null, (o, args) =>
                {
                    treeList.BestFitColumns();
                });
                menu.Items.Add(miBestFitColumns);
            }

            if (mode.HasFlag(DXTreeListContextMenuFlag.EnableSingleClickCheckBox))
            {
                miEnableSingleClickCheck = new ToolStripMenuItem("Enable single click check box", null, (o, args) =>
                {
                    treeList.EnableSingleClickCheckBox();
                });
                menu.Items.Add(miEnableSingleClickCheck);

                miDisableSingleClickCheck = new ToolStripMenuItem("Disable single click check box", null, (o, args) =>
                {
                    treeList.DisableSingleClickCheckBox();
                });
                menu.Items.Add(miDisableSingleClickCheck);
            }



            if (mode.HasFlag(DXTreeListContextMenuFlag.ExpandAll))
            {
                menu.Items.Add(new ToolStripMenuItem("Expand all", null, (o, args) =>
                {
                    treeList.ExpandAll();
                }));
            }

            if (mode.HasFlag(DXTreeListContextMenuFlag.CollapseAll))
            {
                menu.Items.Add(new ToolStripMenuItem("Collapse all", null, (o, args) =>
                {
                    treeList.CollapseAll();
                }));
            }

            if (mode.HasFlag(DXTreeListContextMenuFlag.Expand))
            {
                menu.Items.Add(new ToolStripMenuItem("Expand selected", null, (o, args) =>
                {
                    foreach (var n in treeList.Selection)
                        treeList.ExpandNode((TreeListNode)n);//   ((TreeListNode)n).Expanded = true;
                }));
            }
            if (mode.HasFlag(DXTreeListContextMenuFlag.Collapse))
            {
                menu.Items.Add(new ToolStripMenuItem("Collapse selected", null, (o, args) =>
                {
                    foreach (var n in treeList.Selection)
                        ((TreeListNode)n).Expanded = false;
                }));
            }

            if (mode.HasFlag(DXTreeListContextMenuFlag.ShowHideTreeLine))
            {
                miShowTreeLine = new ToolStripMenuItem("Show tree line", null, (o, args) =>
                {
                    treeList.ShowHideTreeLine(!treeList.GetCustomInfo().VisibleTreeLine);
                });

                menu.Items.Add(miShowTreeLine);
            }


            if (appendSeparator)
                menu.Items.Add(separator);

            /* 추후 checked 상태를 update 할 수 있는 method 등록 */
            menu.Tag = new Action(() =>
            {
                if (miShowAutoFilterRow != null)
                    miShowAutoFilterRow.Checked = treeList.OptionsView.ShowAutoFilterRow;

                if (miFullRowSelectionMode != null)
                    miFullRowSelectionMode.Checked = ! treeList.OptionsSelection.EnableAppearanceFocusedCell;

                if (miMultiRowSelect != null)
                    miMultiRowSelect.Checked = treeList.OptionsSelection.MultiSelect;

                if (miShowTreeLine != null)
                {
                    var visible = treeList.GetCustomInfo().VisibleTreeLine;
                    miShowTreeLine.Checked = visible;
                }
            });
        }
    }
}
