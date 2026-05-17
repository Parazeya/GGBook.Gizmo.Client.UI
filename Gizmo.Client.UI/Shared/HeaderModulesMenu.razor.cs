using System;
using Gizmo.Client.UI.Services;
using Gizmo.Client.UI.View.States;
using Microsoft.AspNetCore.Components;

namespace Gizmo.Client.UI
{
    public partial class HeaderModulesMenu : ComponentBase, IDisposable
    {
        [Inject()]
        private PageModulesViewState ViewState
        {
            get;set;
        }

        protected override void OnInitialized()
            => GGModConfig.ConfigResolved += OnConfigResolved;

        private async void OnConfigResolved(object? sender, EventArgs e)
            => await InvokeAsync(StateHasChanged);

        public void Dispose()
            => GGModConfig.ConfigResolved -= OnConfigResolved;

        // GGMod — hide nav items for disabled features; hidden while config is not yet resolved
        private static bool ShouldShowModule(string guid) => guid switch
        {
            "A1B2C3D4-E5F6-7890-ABCD-EF1234567890" => GGModConfig.Cases,
            "B2C3D4E5-F6A7-8901-BCDE-F12345678901" => GGModConfig.Tasks,
            _ => true
        };
    }
}
