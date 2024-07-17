using Dual.Web.Blazor.ClientSide;

using Engine.Core;

using static System.Net.WebRequestMethods;

namespace DsWebApp.Client.Pages.Hmis;

/// <summary>
/// TagWeb 을 DevExpress Grid 에서 사용하기 위한 ORM
/// </summary>
public class TagWebORM(TagWeb tagWeb)
{
    public string Name => tagWeb.Name;
    public object Value {
        get => tagWeb.Value;
        set => tagWeb.SetValue(value);
    }
    public int Kind => tagWeb.Kind;
    public string KindDescription => tagWeb.KindDescription;
}

