using Gizmo.Client.UI.View.States;
using Gizmo.Web.Components;
using Microsoft.AspNetCore.Components;

namespace Gizmo.Client.UI
{
    public partial class TopBarInfoCluster : CustomDOMComponentBase
    {
        private bool _hideBalance;

        [Inject]
        public UserBalanceViewState UserBalanceViewState { get; set; }

        private void ToggleBalance()
        {
            _hideBalance = !_hideBalance;
            StateHasChanged();
        }

        protected override void OnInitialized()
        {
            this.SubscribeChange(UserBalanceViewState);
            base.OnInitialized();
        }

        public override void Dispose()
        {
            this.UnsubscribeChange(UserBalanceViewState);
            base.Dispose();
        }
    }
}
