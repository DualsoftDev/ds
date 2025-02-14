using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Dual.Common.Core
{
    /// <summary>
    /// http://romacode.com/blog/c-helper-functions-to-map-a-datatable-or-datarow-to-a-class-object
    /// see UnitTestDataRow
    /// </summary>
    public static class EmDataRow
    {
        /// <summary>
        /// function that set the given object from the given data row
        /// </summary>
        public static void SetItem<T>(this DataRow row, T item)
            where T : new()
        {
            // go through each column
            foreach (DataColumn c in row.Table.Columns)
            {
                // find the property for the column
                PropertyInfo p = item.GetType().GetProperty(c.ColumnName);

                // if exists, set the value
                if (p != null && row[c] != DBNull.Value)
                {
                    p.SetValue(item, row[c], null);
                }
            }
        }

        /// <summary>
        /// function that creates an object from the given data row
        /// </summary>
        public static T CreateItem<T>(this DataRow row)
            where T : new()
        {
            // create a new object
            T item = new T();

            // set the item
            row.SetItem(item);

            // return
            return item;
        }

        /// <summary>
        /// function that creates a list of an object from the given data table
        /// </summary>
        public static List<T> CreateList<T>(this DataTable tbl)
            where T : new()
        {
            // define return list
            List<T> lst = new List<T>();

            // go through each row
            foreach (DataRow r in tbl.Rows)
            {
                // add to the list
                lst.Add(r.CreateItem<T>());
            }

            // return the list
            return lst;
        }


        public static IEnumerable<DataColumn> SelectColumns(this DataTable tbl) => tbl.Columns.Cast<DataColumn>();
        public static IEnumerable<DataRow> SelectAddedRows(this DataTable tbl) => tbl.GetRows().Where(r => r.RowState == DataRowState.Added);
        public static IEnumerable<DataRow> SelectModifiedRows(this DataTable tbl) => tbl.GetRows().Where(r => r.RowState == DataRowState.Modified);

        /// <summary>
        /// DataTable tbl 에 포함된 DataRow 의 ItemArray 를 table 의 개별 column 과 맵핑한 결과를 반환한다.
        /// </summary>
        /// <param name="tbl"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        public static IEnumerable<Tuple<DataColumn, object>> MapRowWithColumns(this DataTable tbl, DataRow row)
        {
            var columns = tbl.Columns.Cast<DataColumn>();
            var zipped = columns.Zip(row.ItemArray, (c, it) => Tuple.Create(c, it));
            return zipped;
        }
    }
}
