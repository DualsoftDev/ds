using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DevExpress.XtraEditors.Repository;
using DevExpress.XtraExport.Helpers;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;

using Dual.Common.Base.FS;

namespace Dual.Common.Winform.DevX
{
    public static partial class GridControlExtension
    {
        public static T AddRepositoryItemOnDemand<T>(this GridControl gridControl) where T : RepositoryItem, new()
        {
            // RepositoryItemCheckEdit가 이미 존재하는지 확인
            var existingRepositoryItem = gridControl.RepositoryItems
                .OfType<T>()
                .FirstOrDefault()
                ;
            return
                (existingRepositoryItem == null)
                ? new T().Tee(r => gridControl.RepositoryItems.Add(r))
                : existingRepositoryItem
                ;
        }
    }
}
