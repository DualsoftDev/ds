using Dual.Common.Base.CS;

using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Dual.Common.Core
{
    public static class EmDataTable
    {
        /// 테이블을 구성하는 row 들을 반환 (DataRow type)
#if NET
        public static IEnumerable<DataRow> GetRows(this DataTable table) => table.AsEnumerable();
#else
        public static IEnumerable<DataRow> GetRows(this DataTable table) => table.Rows.ToEnumerable<DataRow>();
#endif
        /// 테이블을 구성하는 column 들을 반환 (DataColumn type)
        public static IEnumerable<DataColumn> GetColumns(this DataTable table) => table.Columns.Cast<DataColumn>();
        /// 테이블을 구성하는 컬럼명을 모두 추출
        public static IEnumerable<string> GetColumnNames(this DataTable table) => table.GetColumns().Select(x => x.ColumnName);
    }
}
