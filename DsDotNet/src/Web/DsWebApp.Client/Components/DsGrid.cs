using DevExpress.Blazor;

using Microsoft.AspNetCore.Components;

namespace DsWebApp.Client.Components;

/// <summary>
/// 사용자 환경 설정을 반영해서 DxGrid 의 속성 값을 설정.
/// </summary>
public class DsGrid : DxGrid
{
    [Inject] ClientGlobal ClientGlobal { get; set; }
    //[Parameter] public Action<GridCustomizeElementEventArgs> AdditionalCustomizeElement { get; set; }

    Action<GridCustomizeElementEventArgs> _originalCustomizeElement;
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        var cs = ClientGlobal?.ClientSettings;
        if (cs != null)
        {
            ShowGroupPanel = cs.ShowGroupPanel;
            ShowFilterRow = cs.ShowFilterRow;

            TextWrapEnabled = cs.TextWrapEnabled;
            PageSizeSelectorVisible = cs.PageSizeSelectorVisible;
            ColumnResizeMode = cs.ColumnResizeMode;
        }

        _originalCustomizeElement = CustomizeElement;
        CustomizeElement = new ( e => customizeElement(e));
    }

    void customizeElement(GridCustomizeElementEventArgs e)
    {
        if (e.ElementType == GridElementType.DataRow)
            e.CssClass = "ds-gridrow-highlighted-item";

        _originalCustomizeElement?.Invoke(e);
    }

}

/*
 * 동일한 방식으로 component 로 상속받아서 구현시, Grid 가 보이지 않음.
 * 
@inherits DxGrid

@code {
    protected override Task OnInitializedAsync()
    {
        ColumnResizeMode = GridColumnResizeMode.NextColumn;
        return base.OnInitializedAsync();
    }
}


 */