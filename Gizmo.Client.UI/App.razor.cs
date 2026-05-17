using System;
using System.Threading.Tasks;
using Gizmo.Client.UI.Services;
using Gizmo.Client.UI.View.States;
using Gizmo.UI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Gizmo.Client.UI;

public partial class App : ComponentBase, IDisposable
{
    #region PROPERTIES

    [Inject] public IUICompositionService ComponentDiscoveryService { get; protected set; }
    [Inject] private NavigationManager NavigationManager { get; set; }
    [Inject] private IJSRuntime JSRuntime { get; set; }
    [Inject] private JSRuntimeService JSRuntimeService { get; set; }
    [Inject] private NavigationService NavigationService { get; set; }
    [Inject] private JSInteropService JSInteropService { get; set; }
    [Inject] private UserViewState UserViewState { get; set; }

    #endregion

    #region LIFECYCLE

    protected override void OnInitialized()
    {
        JSRuntimeService.AssociateJSRuntime(JSRuntime);
        NavigationService.AssociateNavigationManager(NavigationManager);
        UserViewState.OnChange += OnUserViewStateChanged; // GGMod
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

    public void Dispose()
    {
        UserViewState.OnChange -= OnUserViewStateChanged; // GGMod
    }

    #endregion
}
