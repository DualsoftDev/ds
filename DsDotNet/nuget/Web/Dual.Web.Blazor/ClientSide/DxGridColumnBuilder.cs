using DevExpress.Blazor;

using Dual.Common.Core;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

using System.Reflection;

namespace Dual.Web.Blazor.ClientSide;

// https://supportcenter.devexpress.com/ticket/details/t1079647/grid-for-blazor-how-to-create-columns-dynamically-for-dxgrid
public static class DxGridColumnBuilder
{
    /// <summary>
    /// Data type 에 맞게 gird column 을 생성한다.
    /// </summary>
    /// <param name="type">생성할 data 의 type</param>
    /// <param name="excludes">column 생성에서 제외할 column names</param>
    /// <param name="columnRenames">column header 를 원래 type 속성 이름 대신 새로운 이름으로 대체할 목록</param>
    /// <param name="displayOrders">column 배열 순서</param>
    /// <returns></returns>
    public static RenderFragment BuildGridColumns(this Type type
        , IEnumerable<string> predefinedColumns = null
        , IEnumerable<(string, string)> columnRenames=null
        , IEnumerable<string> displayOrders = null
    )
    {
        PropertyInfo[] props = type.GetProperties();
        string[] pdcs = (predefinedColumns ?? Enumerable.Empty<string>()).ToArray();
        string[] dos = (displayOrders ?? pdcs.Concat(props.Select(p => p.Name))).ToArray();
        Dictionary<string, string> renameMap = columnRenames?.ToDictionary(map => map.Item1, map => map.Item2) ?? new Dictionary<string, string>();


        Console.WriteLine($"predefinedColumns={predefinedColumns?.JoinString(", ")}");
        Console.WriteLine($"DisplayOrders={dos.JoinString(", ")}");
        Console.WriteLine($"Props={props.Select(p => p.Name).JoinString(", ")}");
        // column display 순서 재 정렬.  display order 로 지정한 것을 순서대로 맨 앞에 표시하고, 나머지는 있는 그대로 순서대로 표시한다.
        IEnumerable<PropertyInfo> getReordered()
        {
            foreach(var cn in dos)
            {
                var prop = props.FirstOrDefault(p => p.Name == cn);
                if (prop is null)
                {
                    if (renameMap.TryGetValue(cn, out var value))
                        yield return props.FirstOrDefault(p => p.Name == value);
                    else
                        Console.WriteLine($"Unknown column name: {cn}");
                }
                else
                    yield return prop;
            }
        }
        List<PropertyInfo> reOrdered = getReordered().ToList();
        Console.WriteLine($"reOrdered={reOrdered.Select(pr => pr.Name).JoinString(", ")}");

        HashSet<string> pdcsSet = new(pdcs);
        RenderFragment columns = (RenderTreeBuilder b) =>
        {
            int i = pdcsSet.Count;
            foreach (var prop in reOrdered)
            {
                var name = prop.Name;
                if (! pdcsSet.Contains(name))
                {
                    b.OpenComponent(0, typeof(DxGridDataColumn));
                    b.AddAttribute(0, "FieldName", name);
                    b.AddAttribute(0, "Caption", name);
                    b.AddAttribute(0, "Name", name);
                    b.AddAttribute(0, "VisibleIndex", i++);
                    b.CloseComponent();

                    Console.WriteLine($"COLUMN {name} [{i}]");
                }

            }
        };
        return columns;
    }

    public static string GetFieldName(this IGridColumn gridColumn) =>
        gridColumn switch
        {
            DxGridDataColumn c => c.FieldName,
            DxGridCommandColumn c => c.Caption,
            _ => throw new Exception($"Not DevExpress Grid Column: {gridColumn?.GetType().Name}"),
        };
}
