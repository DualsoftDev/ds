using DevExpress.Blazor;

namespace GridWithContextMenu.Data {
    public enum GridContextMenuItemType {
        FullExpand, FullCollapse,
        SortAscending, SortDescending, ClearSorting,
        GroupByColumn, UngroupColumn, ClearGrouping, ShowGroupPanel,
        HideColumn, ShowColumnChooser,
        ClearFilter,
        ShowFilterRow, ShowFooter,

        ExpandRow, CollapseRow,
        ExpandDetailRow, CollapseDetailRow,
        NewRow, EditRow, DeleteRow,

        FixColumnToRight, FixColumnToLeft, Unfix,

        SaveUpdates, CancelUpdates
    }

    public class ContextMenuItem {
        public GridContextMenuItemType ItemType { get; set; }
        public string Text { get; set; }
        public bool Enabled { get; set; }
        public bool Visible { get; set; }
        public bool BeginGroup { get; set; }
        public string CssClass { get; set; }
        public string IconCssClass { get; set; }
    }

    public class GridContextMenuHelper {
        static List<ContextMenuItem> CreateColumnContextMenuItems() {
            return new List<ContextMenuItem> {
                new ContextMenuItem { ItemType = GridContextMenuItemType.FullExpand, Text = "Expand All", IconCssClass="grid-context-menu-item-full-expand" },
                new ContextMenuItem { ItemType = GridContextMenuItemType.FullCollapse, Text = "Collapse All", IconCssClass="grid-context-menu-item-full-collapse" },
                new ContextMenuItem { ItemType = GridContextMenuItemType.SortAscending, Text = "Sort Ascending", BeginGroup = true, IconCssClass="grid-context-menu-item-sort-ascending" },
                new ContextMenuItem { ItemType = GridContextMenuItemType.SortDescending, Text = "Sort Descending", IconCssClass="grid-context-menu-item-sort-descending" },
                new ContextMenuItem { ItemType = GridContextMenuItemType.ClearSorting, Text = "Clear Sorting" },
                new ContextMenuItem { ItemType = GridContextMenuItemType.GroupByColumn, Text = "Group By This Column", BeginGroup = true, IconCssClass="grid-context-menu-item-group-by-column" },
                new ContextMenuItem { ItemType = GridContextMenuItemType.UngroupColumn, Text = "Ungroup", IconCssClass="grid-context-menu-item-ungroup-column" },
                new ContextMenuItem { ItemType = GridContextMenuItemType.ClearGrouping, Text = "Clear Grouping", IconCssClass="grid-context-menu-item-clear-grouping" },
                new ContextMenuItem { ItemType = GridContextMenuItemType.ShowGroupPanel, Text = "Group Panel", IconCssClass="grid-context-menu-item-show-group-panel" },
                new ContextMenuItem { ItemType = GridContextMenuItemType.HideColumn, Text = "Hide Column", BeginGroup = true, IconCssClass="grid-context-menu-item-hide-column" },
                new ContextMenuItem { ItemType = GridContextMenuItemType.ShowColumnChooser, Text = "Column Chooser", IconCssClass="grid-context-menu-item-column-chooser" },
                new ContextMenuItem { ItemType = GridContextMenuItemType.FixColumnToLeft, Text = "Fix Column to the Left", BeginGroup = true, IconCssClass="grid-context-menu-item-fix-column-left" },
                new ContextMenuItem { ItemType = GridContextMenuItemType.FixColumnToRight, Text = "Fix Column to the Right", IconCssClass="grid-context-menu-item-fix-column-right" },
                new ContextMenuItem { ItemType = GridContextMenuItemType.Unfix, Text = "Unfix Column", IconCssClass="grid-context-menu-item-unfix-column" },
                new ContextMenuItem { ItemType = GridContextMenuItemType.ClearFilter, Text = "Clear Filter", BeginGroup = true, IconCssClass="grid-context-menu-item-clear-filter" },
                new ContextMenuItem { ItemType = GridContextMenuItemType.ShowFilterRow, Text = "Filter Row", IconCssClass="grid-context-menu-item-filter-row" },
                new ContextMenuItem { ItemType = GridContextMenuItemType.ShowFooter, Text = "Footer", IconCssClass="grid-context-menu-item-footer" }
            };
        }
        static List<ContextMenuItem> CreateRowContextMenuItems() {
            return new List<ContextMenuItem> {
                new ContextMenuItem { ItemType = GridContextMenuItemType.SaveUpdates, Text = "Save", IconCssClass="grid-context-menu-item-edit-row" },
                new ContextMenuItem { ItemType = GridContextMenuItemType.CancelUpdates, Text = "Cancel", IconCssClass="grid-context-menu-item-delete-row" },
                new ContextMenuItem { ItemType = GridContextMenuItemType.ExpandRow, Text = "Expand", IconCssClass="grid-context-menu-item-expand-row" },
                new ContextMenuItem { ItemType = GridContextMenuItemType.CollapseRow, Text = "Collapse", IconCssClass="grid-context-menu-item-collapse-row" },
                new ContextMenuItem { ItemType = GridContextMenuItemType.ExpandDetailRow, Text = "Expand Detail", BeginGroup = true, IconCssClass="grid-context-menu-item-expand-detail-row" },
                new ContextMenuItem { ItemType = GridContextMenuItemType.CollapseDetailRow, Text = "Collapse Detail", IconCssClass="grid-context-menu-item-collapse-detail-row" },
                new ContextMenuItem { ItemType = GridContextMenuItemType.NewRow, Text = "New", BeginGroup = true, IconCssClass="grid-context-menu-item-new-row" },
                new ContextMenuItem { ItemType = GridContextMenuItemType.EditRow, Text = "Edit", IconCssClass="grid-context-menu-item-edit-row" },
                new ContextMenuItem { ItemType = GridContextMenuItemType.DeleteRow, Text = "Delete", IconCssClass="grid-context-menu-item-delete-row" }
            };
        }

        public static bool IsContextMenuElement(GridElementType elementType) {
            return IsColumnContextMenuElement(elementType) || IsRowContextMenuElement(elementType);
        }
        public static bool IsColumnContextMenuElement(GridElementType elementType) {
            switch(elementType) {
                case GridElementType.HeaderCell:
                case GridElementType.HeaderCommandCell:
                case GridElementType.HeaderSelectionCell:
                case GridElementType.GroupPanelHeader:
                    return true;
            }
            return false;
        }
        public static bool IsRowContextMenuElement(GridElementType elementType) {
            switch(elementType) {
                case GridElementType.DataRow:
                case GridElementType.GroupRow:
                case GridElementType.EditRow:
                    return true;
            }
            return false;
        }

        public static void ProcessColumnMenuItemClick(ContextMenuItem item, IGridColumn column, IGrid grid) {
            var dataColumn = column as IGridDataColumn;
            grid.BeginUpdate();
            switch(item.ItemType) {
                case GridContextMenuItemType.FullExpand:
                    grid.ExpandAllGroupRows();
                    break;
                case GridContextMenuItemType.FullCollapse:
                    grid.CollapseAllGroupRows();
                    break;
                case GridContextMenuItemType.SortAscending:
                    if(dataColumn.SortOrder != GridColumnSortOrder.Ascending) {
                        var newSortIndex = dataColumn.SortIndex > -1 ? dataColumn.SortIndex : grid.GetSortedColumns().Count;
                        grid.SortBy(dataColumn.FieldName, GridColumnSortOrder.Ascending, newSortIndex);
                    }
                    break;
                case GridContextMenuItemType.SortDescending:
                    if(dataColumn.SortOrder != GridColumnSortOrder.Descending) {
                        var newSortIndex = dataColumn.SortIndex > -1 ? dataColumn.SortIndex : grid.GetSortedColumns().Count;
                        grid.SortBy(dataColumn.FieldName, GridColumnSortOrder.Descending, newSortIndex);
                    }
                    break;
                case GridContextMenuItemType.ClearSorting:
                    grid.SortBy(dataColumn.FieldName, GridColumnSortOrder.Descending, -1);
                    break;
                case GridContextMenuItemType.GroupByColumn:
                    grid.GroupBy(dataColumn.FieldName, grid.GetGroupCount());
                    break;
                case GridContextMenuItemType.UngroupColumn:
                    grid.GroupBy(dataColumn.FieldName, -1);
                    break;
                case GridContextMenuItemType.ClearGrouping:
                    grid.ClearSort();
                    break;
                case GridContextMenuItemType.ShowGroupPanel:
                    grid.ShowGroupPanel = !grid.ShowGroupPanel;
                    break;
                case GridContextMenuItemType.ShowFilterRow:
                    grid.ShowFilterRow = !grid.ShowFilterRow;
                    break;
                case GridContextMenuItemType.ShowFooter:
                    var isFooterVisible = grid.FooterDisplayMode == GridFooterDisplayMode.Always
                        || grid.FooterDisplayMode == GridFooterDisplayMode.Auto && grid.GetTotalSummaryItems().Count > 0;
                    grid.FooterDisplayMode = isFooterVisible ? GridFooterDisplayMode.Never : GridFooterDisplayMode.Always;
                    break;
                case GridContextMenuItemType.HideColumn:
                    column.Visible = false;
                    break;               
                case GridContextMenuItemType.ShowColumnChooser:
                    grid.ShowColumnChooser();
                    break;
                case GridContextMenuItemType.FixColumnToLeft:
                    column.FixedPosition = GridColumnFixedPosition.Left;
                    break;
                case GridContextMenuItemType.FixColumnToRight:
                    column.FixedPosition = GridColumnFixedPosition.Right;
                    break;
                case GridContextMenuItemType.Unfix:
                    column.FixedPosition = GridColumnFixedPosition.None;
                    break;
                case GridContextMenuItemType.ClearFilter:
                    grid.ClearFilter();
                    break;
            }
            grid.EndUpdate();
        }
        public static async Task ProcessRowMenuItemClickAsync(ContextMenuItem item, int visibleIndex, IGrid grid) {
            switch(item.ItemType) {
                case GridContextMenuItemType.ExpandRow:
                    grid.ExpandGroupRow(visibleIndex);
                    break;
                case GridContextMenuItemType.CollapseRow:
                    grid.CollapseGroupRow(visibleIndex);
                    break;
                case GridContextMenuItemType.ExpandDetailRow:
                    grid.ExpandDetailRow(visibleIndex);
                    break;
                case GridContextMenuItemType.CollapseDetailRow:
                    grid.CollapseDetailRow(visibleIndex);
                    break;
                case GridContextMenuItemType.NewRow:
                    await grid.StartEditNewRowAsync();
                    break;
                case GridContextMenuItemType.EditRow:
                    await grid.StartEditRowAsync(visibleIndex);
                    break;
                case GridContextMenuItemType.DeleteRow:
                    grid.ShowRowDeleteConfirmation(visibleIndex);
                    break;
                case GridContextMenuItemType.SaveUpdates:
                    await grid.SaveChangesAsync();
                    break;
                case GridContextMenuItemType.CancelUpdates:
                    await grid.CancelEditAsync();
                    break;
            }
        }
        public static List<ContextMenuItem> GetColumnItems(GridCustomizeElementEventArgs e) {
            var items = CreateColumnContextMenuItems();
            var applyBeginGroupForNextVisibleItem = false;
            foreach(var item in items) {
                item.Visible = IsColumnMenuItemVisible(e, item.ItemType);
                if(!item.Visible && item.BeginGroup)
                    applyBeginGroupForNextVisibleItem = true;
                if(item.Visible && applyBeginGroupForNextVisibleItem) {
                    item.BeginGroup = true;
                    applyBeginGroupForNextVisibleItem = false;
                }
                item.Enabled = IsColumnMenuItemEnabled(e, item.ItemType);
                var isSelected = IsColumnMenuItemSelected(e, item.ItemType);
                if(item.Enabled && isSelected)
                    item.CssClass = "menu-item-selected";
            }
            return items;
        }
        public static List<ContextMenuItem> GetRowItems(GridCustomizeElementEventArgs e) {
            var items = CreateRowContextMenuItems();
            foreach(var item in items) {
                item.Visible = IsRowMenuItemVisible(e, item.ItemType);
                item.Enabled = IsRowMenuItemEnabled(e, item.ItemType);
            }
            return items;
        }

        static bool IsColumnMenuItemVisible(GridCustomizeElementEventArgs e, GridContextMenuItemType itemType) {
            var dataColumn = e.Column as IGridDataColumn;
            var allowSort = GetAllowSort(e.Column, e.Grid);
            var allowGroup = GetAllowGroup(e.Column, e.Grid);            
            switch(itemType) {
                case GridContextMenuItemType.FullExpand:
                case GridContextMenuItemType.FullCollapse:
                    return e.ElementType == GridElementType.GroupPanelHeader;
                case GridContextMenuItemType.SortAscending:
                case GridContextMenuItemType.SortDescending:
                    return allowSort;
                case GridContextMenuItemType.ClearSorting:
                    return allowSort && dataColumn.GroupIndex < 0;
                case GridContextMenuItemType.GroupByColumn:
                    return allowGroup && dataColumn.GroupIndex < 0;
                case GridContextMenuItemType.UngroupColumn:
                    return allowGroup && dataColumn.GroupIndex > -1;
                case GridContextMenuItemType.ClearGrouping:
                    return e.Grid.AllowGroup;               
                case GridContextMenuItemType.ShowGroupPanel:
                case GridContextMenuItemType.ShowFilterRow:
                case GridContextMenuItemType.ShowFooter:
                case GridContextMenuItemType.HideColumn:
                case GridContextMenuItemType.ShowColumnChooser:
                case GridContextMenuItemType.FixColumnToLeft:
                case GridContextMenuItemType.FixColumnToRight:
                case GridContextMenuItemType.Unfix:
                case GridContextMenuItemType.ClearFilter:
                    return true;
            }
            return false;
        }
        static bool IsColumnMenuItemSelected(GridCustomizeElementEventArgs e, GridContextMenuItemType itemType) {
            var dataColumn = e.Column as IGridDataColumn;
            if (dataColumn == null)
                return false;

            var isSorted = dataColumn != null && dataColumn.SortIndex > -1;
            var isGrouped = dataColumn != null && dataColumn.GroupIndex > -1;
            var sortOrder = GridColumnSortOrder.None;
            var fixedPosition = dataColumn.FixedPosition;
            if (isSorted || isGrouped) {
                sortOrder = dataColumn.SortOrder;
                if (sortOrder == GridColumnSortOrder.None)
                    sortOrder = GridColumnSortOrder.Ascending;
            }
            switch(itemType) {
                case GridContextMenuItemType.SortAscending:
                    return sortOrder == GridColumnSortOrder.Ascending;
                case GridContextMenuItemType.SortDescending:
                    return sortOrder == GridColumnSortOrder.Descending;
                case GridContextMenuItemType.FixColumnToLeft:
                    return fixedPosition == GridColumnFixedPosition.Left;
                case GridContextMenuItemType.FixColumnToRight:
                    return fixedPosition == GridColumnFixedPosition.Right;
                case GridContextMenuItemType.ShowGroupPanel:
                    return e.Grid.ShowGroupPanel;
                case GridContextMenuItemType.ShowFilterRow:
                    return e.Grid.ShowFilterRow;
                case GridContextMenuItemType.ShowFooter:
                    return e.Grid.FooterDisplayMode == GridFooterDisplayMode.Always
                        || e.Grid.FooterDisplayMode == GridFooterDisplayMode.Auto && e.Grid.GetTotalSummaryItems().Count > 0;
            }
            return false;
        }
        static bool IsColumnMenuItemEnabled(GridCustomizeElementEventArgs e, GridContextMenuItemType itemType) {
            var dataColumn = e.Column as IGridDataColumn;
            var allowSort = GetAllowSort(e.Column, e.Grid);
            var allowGroup = GetAllowGroup(e.Column, e.Grid);
            switch(itemType) {
                case GridContextMenuItemType.FullExpand:
                case GridContextMenuItemType.FullCollapse:
                case GridContextMenuItemType.SortAscending:
                case GridContextMenuItemType.SortDescending:
                case GridContextMenuItemType.GroupByColumn:
                case GridContextMenuItemType.UngroupColumn:
                case GridContextMenuItemType.ShowGroupPanel:
                case GridContextMenuItemType.ShowFilterRow:
                case GridContextMenuItemType.ShowFooter:
                case GridContextMenuItemType.HideColumn:
                case GridContextMenuItemType.ShowColumnChooser:
                case GridContextMenuItemType.FixColumnToLeft:
                case GridContextMenuItemType.FixColumnToRight:
                    return true;
                case GridContextMenuItemType.ClearSorting:
                    return allowSort && (dataColumn.SortIndex > -1 || dataColumn.GroupIndex > -1);
                case GridContextMenuItemType.ClearGrouping:
                    return e.Grid.AllowGroup && e.Grid.GetGroupCount() > 1;
                case GridContextMenuItemType.ClearFilter:
                    return e.Grid.GetDataColumns().Any(i => i.FilterRowValue != null);
                case GridContextMenuItemType.Unfix:
                    return e.Column.FixedPosition != GridColumnFixedPosition.None;
            }
            return false;
        }

        static bool IsRowMenuItemVisible(GridCustomizeElementEventArgs e, GridContextMenuItemType itemType) {
            var isGroupRow = e.Grid.IsGroupRow(e.VisibleIndex);
            var hasDetailButton = !isGroupRow
                        && e.Grid.DetailRowTemplate != null
                        && e.Grid.DetailRowDisplayMode == GridDetailRowDisplayMode.Auto
                        && e.Grid.DetailExpandButtonDisplayMode == GridDetailExpandButtonDisplayMode.Auto;
            switch(itemType) {
                case GridContextMenuItemType.ExpandRow:
                case GridContextMenuItemType.CollapseRow:
                    return isGroupRow;
                case GridContextMenuItemType.ExpandDetailRow:
                case GridContextMenuItemType.CollapseDetailRow:
                    return hasDetailButton;
                case GridContextMenuItemType.NewRow:
                    return true;
                case GridContextMenuItemType.EditRow:
                case GridContextMenuItemType.DeleteRow:
                    return !isGroupRow;
                case GridContextMenuItemType.SaveUpdates:
                case GridContextMenuItemType.CancelUpdates:
                    return e.Grid.IsEditing();
            }
            return false;
        }
        static bool IsRowMenuItemEnabled(GridCustomizeElementEventArgs e, GridContextMenuItemType itemType) {
            var isGroupRow = e.Grid.IsGroupRow(e.VisibleIndex);
            var isGroupRowExpanded = e.Grid.IsGroupRowExpanded(e.VisibleIndex);
            var hasDetailButton = !isGroupRow
                        && e.Grid.DetailRowTemplate != null
                        && e.Grid.DetailRowDisplayMode == GridDetailRowDisplayMode.Auto
                        && e.Grid.DetailExpandButtonDisplayMode == GridDetailExpandButtonDisplayMode.Auto;
            var isDetailRowExpanded = e.Grid.IsDetailRowExpanded(e.VisibleIndex);
            switch(itemType) {
                case GridContextMenuItemType.ExpandRow:
                    return isGroupRow && !isGroupRowExpanded;
                case GridContextMenuItemType.CollapseRow:
                    return isGroupRow && isGroupRowExpanded;
                case GridContextMenuItemType.ExpandDetailRow:
                    return hasDetailButton && !isDetailRowExpanded;
                case GridContextMenuItemType.CollapseDetailRow:
                    return hasDetailButton && isDetailRowExpanded;
                case GridContextMenuItemType.NewRow:
                case GridContextMenuItemType.EditRow:
                case GridContextMenuItemType.DeleteRow:
                    return !e.Grid.IsEditing();
                case GridContextMenuItemType.SaveUpdates:
                case GridContextMenuItemType.CancelUpdates:
                    return e.Grid.IsEditing();
            }
            return false;
        }

        static bool GetAllowSort(IGridColumn column, IGrid grid) {
            if(column is IGridDataColumn dataColumn)
                return dataColumn.AllowSort ?? grid.AllowSort;
            return false;
        }
        static bool GetAllowGroup(IGridColumn column, IGrid grid) {
            if(column is IGridDataColumn dataColumn)
                return dataColumn.AllowGroup ?? grid.AllowGroup;
            return false;
        }
    }
}
