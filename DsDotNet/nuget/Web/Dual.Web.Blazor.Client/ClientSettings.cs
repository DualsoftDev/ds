using Blazored.LocalStorage;
using DevExpress.Blazor;

namespace Dual.Web.Blazor.Client;

public class ClientSettings
{
    public bool ShowAllRows { get; set; }
    public bool ShowGroupPanel { get; set; }
    public bool ShowFilterRow { get; set; }
    public bool TextWrapEnabled { get; set; } = false;
    public bool PageSizeSelectorVisible { get; set; } = true;
    public bool ShowSectionSidebar { get; set; } = true;

    public GridColumnResizeMode ColumnResizeMode { get; set; } = GridColumnResizeMode.NextColumn;
    public GridFooterDisplayMode FooterDisplayMode { get; set; } = GridFooterDisplayMode.Auto;

    public virtual async Task SaveAsync(ILocalStorageService localStorage) =>
        await localStorage.SetItemAsync("ClientSettings", this);

    public static async Task<ClientSettings> ReadAsync(ILocalStorageService localStorage) =>
        await localStorage.GetItemAsync<ClientSettings>("ClientSettings") ?? new ClientSettings();

    // theme 은 clientsettings 에서 관리 안함.  참조 project 에서 읽고 쓰므로 여기서는 무시.
    // 여기서 구현하려면, getter/setter 를 구현하고, localStorage 에서 읽고 쓰는 코드를 추가해야 함.
}
