using Dual.Web.Blazor.Auth;
using Microsoft.AspNetCore.Components.Authorization;

using System.Reactive.Subjects;

namespace Dual.Web.Blazor.Client;

public class ClientGlobalBase
{
    public static ClientGlobalBase TheInstance { get; private set; }

    public UserSession UserSession { get; set; }
    public AuthenticationState AuthenticationState { get; set; }
    public ClientSettings ClientSettings { get; protected set; }

    /// <summary>
    /// SPA 에서 현재 location 을 추적.  
    /// MainLayout.razor 의 OnLocationChanged 에서 페이지 변경시마다 update 해 주어야 한다.
    /// </summary>
    public string CurrentLocation { get; set; }// = "/";
    public string PreviousLocation { get; set; }// = "/";
    public string UserAgent { get; set; }
    public string UserName => AuthenticationState?.User.Identity?.Name;
    public bool IsInRole(string role) => AuthenticationState?.User.IsInRole(role) ?? false;

    /// <summary>
    /// MainLayout.razor 의 IsMobileLayout 값 변경시마다 update 해 주어야 한다.
    /// </summary>
    public bool IsMobileLayout { get; set; }
    public bool ShowSectionSidebar { get; set; } = true;

    /// <summary>
    /// DevExpress Blazor 의 side bar open/close 상태 변경시 호출됨.
    /// <br/>
    /// - true 이면 expanded, false 이면 collapsed
    /// </summary>
    public Subject<bool> SidebarStatusChangedSubject = new();

    /*
     * MainLayout.razor
        protected override async Task OnInitializedAsync()
        {
            NavigationManager.LocationChanged += OnLocationChanged;
            ClientGlobal.UserAgent = await JsDual.GetUserAgent();
        }
        async void OnLocationChanged(object sender, LocationChangedEventArgs args)
        {
            ClientGlobal.PreviousLocation = ClientGlobal.CurrentLocation;
            ClientGlobal.CurrentLocation = args.Location;
            ....
     */

    public ClientGlobalBase()
    {
        TheInstance = this;
    }
}
