using System.Threading.Tasks;
using Gizmo.Client.UI.Services;
using Gizmo.UI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Gizmo.Client.UI;

public partial class App : ComponentBase
{
    [Inject] public IUICompositionService ComponentDiscoveryService { get; protected set; }
    [Inject] private NavigationManager NavigationManager { get; set; }
    [Inject] private IJSRuntime JSRuntime { get; set; }
    [Inject] private JSRuntimeService JSRuntimeService { get; set; }
    [Inject] private NavigationService NavigationService { get; set; }
    [Inject] private JSInteropService JSInteropService { get; set; }

    protected override void OnInitialized()
    {
        JSRuntimeService.AssociateJSRuntime(JSRuntime);
        NavigationService.AssociateNavigationManager(NavigationManager);
        GGModSubscribeConfigResolved(); // GGMod
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JSInteropService.InitializeAsync(default);
            await FetchGGBookConfigAsync(); // GGMod
        }

        await base.OnAfterRenderAsync(firstRender);
    }
}
