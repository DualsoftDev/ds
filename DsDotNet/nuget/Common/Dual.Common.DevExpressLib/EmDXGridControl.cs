using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.BandedGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;

using Images = Dual.Common.Resources.Images;

namespace Dual.Common.DevExpressLib
{
    public class GridViewUpdator : IDisposable
    {
        private GridView _gridView;

        public GridViewUpdator(GridView gridView)
        {
            _gridView = gridView;
            _gridView.BeginUpdate();
        }
        public void Dispose()
        {
            _gridView.EndUpdate();
        }
    }
    public static partial class EmDXGridControl
    {
        public static GridViewUpdator UpdateBeginer(this GridView gridView)
        {
            return new GridViewUpdator(gridView);
        }

        /// <summary> Grid view 의 row index 로부터 row item 을 얻는다. </summary>
        /// <typeparam name="T"> row 에 할당된 item 의 type </typeparam>
        public static T GetItemOnRow<T>(this GridView gridView, int r)
        {
            return (T)gridView.GetRow(r);
        }

        /// <summary> Grid view 의 선택된 row 들에 대해서 row items 를 얻는다. </summary>
        public static IEnumerable<T> GetSelectedItems<T>(this GridView gridView)
        {
            return gridView.GetSelectedRows().Select(rh => gridView.GetRow(rh)).Cast<T>();
        }

        public static GridHitInfo GetHitInfo(this GridView gridView, Point pt) => gridView.CalcHitInfo(pt);
        public static GridHitInfo GetHitInfo(this GridView gridView, MouseEventArgs e) => gridView.CalcHitInfo(e.Location);

        /// Cell 영역을 정확히 click 했을 때, 해당 hit info 반환.  다른 영역 click 시 null
        public static GridHitInfo GetCellHitInfo(this GridView gridView, Point pt)
        {
            GridHitInfo hi = gridView.CalcHitInfo(pt);
            if (hi.InRowCell && hi.InRowCell /*&& gridView.IsCellSelected(hi.RowHandle, hi.Column)*/)
                return hi;
            else
                return null;
        }

        public static (int, GridColumn) GetRowColumn(this GridHitInfo hi) => (hi.RowHandle, hi.Column);




        #region Context menu
        public static ContextMenuStrip CreateDefaultContextMenu(this GridView gridView, bool appendSeparator=false)
        {
            // https://documentation.devexpress.com/#WindowsForms/DevExpressXtraGridGridControl_ContextMenuStriptopic
            gridView.GridControl.ContextMenuStrip = new ContextMenuStrip();

            AppendDefaultContextMenu(gridView, appendSeparator);
            return gridView.GridControl.ContextMenuStrip;
        }


        [Flags]
        public enum DXGridViewContextMenuFlag
        {
            AutoFilterRow = 1 << 0,
            GroupPanel = 1 << 1,
            FullRowSelectionMode = 1 << 2,
            CellSelectionMode = 1 << 3,
            BestFitColumn = 1 << 4,
            EnableSingleClickCheckBox = 1 << 5,
            DisableSingleClickCheckBox = 1 << 6,
            EnableShowAllColumns = 1 << 7,
            All = AutoFilterRow | GroupPanel | FullRowSelectionMode | CellSelectionMode | BestFitColumn
                | EnableSingleClickCheckBox | DisableSingleClickCheckBox | EnableShowAllColumns,
        }
        public static void AppendDefaultContextMenu(this GridView gridView, bool appendSeparator=false, DXGridViewContextMenuFlag mode=DXGridViewContextMenuFlag.All)
        {
            var separator = new ToolStripSeparator();
            if (gridView.GridControl.ContextMenuStrip == null)
                gridView.GridControl.ContextMenuStrip = new ContextMenuStrip();

            ContextMenuStrip menu = gridView.GridControl.ContextMenuStrip;
            if (menu.Items.Count > 0)
                menu.Items.Add(separator);

            ToolStripMenuItem miShowAutoFilterRow=null, miShowGroupPanel=null, miFullRowSelectionMode=null;
            ToolStripMenuItem miCellSelectionMode=null, miBestFitColumns=null, miEnableSingleClickCheck=null;
            ToolStripMenuItem miDisableSingleClickCheck=null, miEnableShowAllColumn=null;

            if (mode.HasFlag(DXGridViewContextMenuFlag.AutoFilterRow))
            {
                miShowAutoFilterRow = new ToolStripMenuItem("show auto filter row", Images.Filter, (o, args) =>
                {
                    gridView.OptionsView.ShowAutoFilterRow = !gridView.OptionsView.ShowAutoFilterRow;
                });
                menu.Items.Add(miShowAutoFilterRow);
            }

            if (mode.HasFlag(DXGridViewContextMenuFlag.GroupPanel))
            {
                miShowGroupPanel = new ToolStripMenuItem("Show group panel", Images.UserGroup, (o, args) =>
                {
                    gridView.OptionsView.ShowGroupPanel = !gridView.OptionsView.ShowGroupPanel;
                });
                menu.Items.Add(miShowGroupPanel);
            }

            if (mode.HasFlag(DXGridViewContextMenuFlag.FullRowSelectionMode))
            {
                miFullRowSelectionMode = new ToolStripMenuItem("Full row selection mode", Images.TableRow, (o, args) =>
                {
                    gridView.OptionsSelection.MultiSelectMode = GridMultiSelectMode.RowSelect;
                });
                menu.Items.Add(miFullRowSelectionMode);
            }


            if (mode.HasFlag(DXGridViewContextMenuFlag.CellSelectionMode))
            {
                miCellSelectionMode = new ToolStripMenuItem("Cell selection mode", Images.TableCell, (o, args) =>
                {
                    gridView.OptionsSelection.MultiSelectMode = GridMultiSelectMode.CellSelect;
                });
                menu.Items.Add(miCellSelectionMode);
            }

            if (mode.HasFlag(DXGridViewContextMenuFlag.BestFitColumn))
            {
                miBestFitColumns = new ToolStripMenuItem("Best fit columns", null, (o, args) =>
                {
                    gridView.BestFitColumns();
                });
               menu.Items.Add(miBestFitColumns);
            }

            if (mode.HasFlag(DXGridViewContextMenuFlag.EnableSingleClickCheckBox))
            {
                miEnableSingleClickCheck = new ToolStripMenuItem("Enable single click check box", null, (o, args) =>
                {
                    gridView.EnableSingleClickCheckBox();
                });
                menu.Items.Add(miEnableSingleClickCheck);
            }


            if (mode.HasFlag(DXGridViewContextMenuFlag.DisableSingleClickCheckBox))
            {
                miDisableSingleClickCheck = new ToolStripMenuItem("Disable single click check box", null, (o, args) =>
                {
                    gridView.DisableSingleClickCheckBox();
                });
                menu.Items.Add(miDisableSingleClickCheck);
            }

            if (mode.HasFlag(DXGridViewContextMenuFlag.EnableShowAllColumns))
            {
                miEnableShowAllColumn = new ToolStripMenuItem("show all columns", null, (o, args) =>
                {
                    gridView.Columns.ForEach(c => c.Visible = true);
                    if ( gridView is BandedGridView )
                    ((BandedGridView)gridView).Bands.ForEach(b => b.Visible = true);
                });
                menu.Items.Add(miEnableShowAllColumn);
            }



            if ( appendSeparator )
                menu.Items.Add(separator);

            /* 추후 checked 상태를 update 할 수 있는 method 등록 */
            menu.Tag = new Action(() =>
            {
                if ( miShowGroupPanel != null )
                    miShowGroupPanel.Checked = gridView.OptionsView.ShowGroupPanel;

                if (miShowAutoFilterRow != null)
                    miShowAutoFilterRow.Checked = gridView.OptionsView.ShowAutoFilterRow;

                if (miFullRowSelectionMode != null)
                    miFullRowSelectionMode.Checked = gridView.OptionsSelection.MultiSelectMode == GridMultiSelectMode.RowSelect;

                if (miCellSelectionMode != null)
                    miCellSelectionMode.Checked = gridView.OptionsSelection.MultiSelectMode == GridMultiSelectMode.CellSelect;

                if ( miEnableShowAllColumn != null )
                    miEnableShowAllColumn.Visible = gridView.Columns.Count > gridView.Columns.Where(c => c.Visible).Count();
            });
        }


        public static void EnableDefaultContextMenu(this GridView gridView, DXGridViewContextMenuFlag mode = DXGridViewContextMenuFlag.All)
        {
            gridView.AppendDefaultContextMenu(false, mode);
            gridView.MouseDown += (sender, e) =>
            {
                // https://www.devexpress.com/Support/Center/Question/Details/Q257821
                if (e.Button == MouseButtons.Right)
                {
                    GridHitInfo hitInfo = gridView.CalcHitInfo(e.Location);
                    if (hitInfo != null && hitInfo.InRow)
                    {
                        ((DXMouseEventArgs)e).Handled = true;

                        /* 이미 만들어진 메뉴 항목의 checked 속성을, 현재 시점에 맞게 update 한다. */
                        var action = gridView.GridControl.ContextMenuStrip.Tag as Action;
                        if (action != null)
                            action();
                    }
                }
            };
        }
        #endregion



        public static object GetClickedItem(this GridView gridView, GridControl gridControl)
        {
            Point clickPoint = gridControl.PointToClient(Control.MousePosition);
            var hitInfo = gridView.CalcHitInfo(clickPoint);
            if (!hitInfo.InRowCell)
                return null;

            return gridView.GetRow(hitInfo.RowHandle);
        }


        /// <summary>
        /// https://www.devexpress.com/Support/Center/Question/Details/A2761
        /// </summary>
        /// <param name="gridView"></param>
        public static void ChangeFontStyle(this GridView gridView, FontStyle style)
        {
            foreach (AppearanceObject ap in gridView.Appearance)
                ap.Font = new Font(ap.Font, style);
        }



        /// <summary>
        /// DevXpress gridview 상에서의 checkbox click 시, column 선택 후 toggle 이 이루어지므로,
        /// 반드시 2번 이상의 click 을 해야만 check box 를 toggle 할 수 있다.
        /// 이를 한번 click 으로 toggle 할 수 있도록 한다.
        /// </summary>
        /// <param name="gridView"></param>
        public static void EnableSingleClickCheckBox(this GridView gridView)
        {
            gridView.MouseDown += GridViewOnMouseDownSingleClickEnabler;
        }
        public static void DisableSingleClickCheckBox(this GridView gridView)
        {
            gridView.MouseDown -= GridViewOnMouseDownSingleClickEnabler;
        }

        public static void EnableSingleClickCheckBox(this GridView gridView, RepositoryItemCheckEdit repositoryItemCheckEdit)
        {
            repositoryItemCheckEdit.EditValueChanged += (sender, args) => { gridView.PostEditor(); };
        }

        private static void GridViewOnMouseDownSingleClickEnabler(object sender, MouseEventArgs e)
        {
            // https://www.devexpress.com/Support/Center/Question/Details/K18380

            if (e.Button != MouseButtons.Left)
                return;

            var gridView1 = sender as GridView;
            GridHitInfo hitInfo = gridView1.CalcHitInfo(e.Location);
            if (hitInfo.InRowCell)
            {
                if (hitInfo.Column.RealColumnEdit is RepositoryItemCheckEdit
                    || hitInfo.Column.ColumnType == typeof(object)     // <-- object type 의 symbol table 의 value column check box 를 위한 special case.
                    )
                {
                    gridView1.FocusedColumn = hitInfo.Column;
                    gridView1.FocusedRowHandle = hitInfo.RowHandle;

                    /*
                     * Single click check box 에서는 복수개의 선택을 허용하면,
                     * check box 아닌 cell 을 클릭하고 check box 를 클릭할 때에, 두 cell 모두 boolean 으로 toggle 하려고 해서 문제 발생...
                     * ==> 마지막 click 한 cell 을 제외하고 나머지는 선택 해제
                     */
                    gridView1.ClearSelection();
                    gridView1.SelectCell(hitInfo.RowHandle, hitInfo.Column);

                    gridView1.ShowEditor();
                    CheckEdit edit = gridView1.ActiveEditor as CheckEdit;
                    if (edit == null)
                        return;

                    edit.Toggle();
                    //gridView1.PostEditor();
                    DXMouseEventArgs.GetMouseArgs(e).Handled = true;
                }
            }
        }




        private static void HideCascading(GridBand band)
        {
            if (band == null)
                return;
            band.Visible = false;

            if (band.ParentBand == null)
                return;

            if ( band.ParentBand.Children.FirstVisibleBand == null )
                HideCascading(band.ParentBand);
        }

        public static void HideCascading(this GridColumn column)
        {
            column.Visible = false;
            var bandColumn = column as BandedGridColumn;
            if (bandColumn != null)
            {
                if (bandColumn.OwnerBand == null)
                    return;
                if ( bandColumn.OwnerBand.Columns.VisibleColumnCount == 0)
                    HideCascading(bandColumn.OwnerBand);
            }
        }

        public static void MakeEditable(this GridView gv, bool editable)
        {
            gv.OptionsBehavior.Editable = editable;
        }

        public static void MakeReadOnly(this GridView gv, bool readOnly=true)
        {
            gv.OptionsBehavior.ReadOnly = readOnly;
        }

        public static void MakeSelectableReadOnly(this GridView gv, bool readOnly = true)
        {
            using (gv.UpdateBeginer())
            {
                gv.Columns.ForEach(c => c.MakeReadOnly(readOnly));
            }
        }

        /// <summary>
        /// Column 에 대해서 readonly 로 만들면서도, text 를 선택해서 복사할 수 있도록 하기 위함
        /// https://www.devexpress.com/Support/Center/Question/Details/Q256225
        /// </summary>
        /// <param name="col"></param>
        /// <param name="readOnly"></param>
        /// <param name="allowEdit"></param>
        public static void MakeReadOnly(this GridColumn col, bool readOnly = true, bool allowEdit = true)
        {
            col.OptionsColumn.ReadOnly = readOnly;
            col.OptionsColumn.AllowEdit = allowEdit;
        }



        public static void ShowHideAllColumns(this GridView gv, bool show)
        {
            gv.Columns.ForEach(c => c.Visible = show);
        }
        public static void ShowHideAllBands(this BandedGridView gv, bool show)
        {
            gv.Bands.ForEach(b => b.Visible = show);
        }
    }
}
