using Microsoft.AspNetCore.Components;

using System.Net.Http.Json;

using static Engine.Core.HmiPackageModule;

namespace DsWebApp.Client.Pages.Hmis
{
    public class CompHmiLoader : ComponentBase
    {
        [Inject] protected HttpClient Http { get; set; }
        [Inject] protected ClientGlobal ClientGlobal { get; set; }

        protected HMIPackage _hmiPackage;
        protected HMISystem _system => _hmiPackage.System;
        protected HMIDevice[] _devices => _hmiPackage.Devices;

        protected override async Task OnInitializedAsync()
        {
            _hmiPackage = ClientGlobal.HmiPackage;
            if (_hmiPackage == null)
                ClientGlobal.HmiPackage = _hmiPackage = await Http.GetFromJsonAsync<HMIPackage>($"api/hmitag");
            
            await base.OnInitializedAsync();
        }
    }
}
