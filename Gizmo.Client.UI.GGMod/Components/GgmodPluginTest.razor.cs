using Microsoft.AspNetCore.Components;

namespace Gizmo.Client.UI.GGMod.Components
{
    public partial class GgmodPluginTest : ComponentBase
    {
        private int _clicks;

        private void OnClick()
        {
            _clicks++;
        }
    }
}
