using Gizmo.Client.UI.Services;
using Gizmo.UI;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace Gizmo.Client.UI.Host.WPF
{
    /// <summary>
    /// Interaction logic for HostWindow.xaml
    /// </summary>
    public partial class HostWindow : Window, IHostWindow
    {
        public HostWindow(DesktopUICompositionService componentDiscoveryService)
        {
            InitializeComponent();

            componentDiscoveryService.Initialized += ComponentDiscoveryService_Initialized;
            UpdateValues(componentDiscoveryService);
        }

        private void ComponentDiscoveryService_Initialized(object sender, System.EventArgs e)
        {
            UpdateValues((DesktopUICompositionService)sender);
        }

        private void UpdateValues(DesktopUICompositionService componentDiscoveryService)
        {
            var rootComponent = componentDiscoveryService.RootComponentType;
            if (rootComponent != null)
            {
                //set component type based on the settings found by discovery service
                _ROOT_COMPONENT.ComponentType = rootComponent;

                _BLAZOR_WEB_VIEW.HostPage = Path.Combine(componentDiscoveryService.BasePath, @"wwwroot\index.html");
            }
        }

        private void BlazorWebViewInit(object sender, BlazorWebViewInitializedEventArgs e)
        {
            var wv2 = _BLAZOR_WEB_VIEW.WebView.CoreWebView2;

            var staticFiles = Path.Combine(Environment.CurrentDirectory, "static");
            if (Directory.Exists(staticFiles))
                wv2.SetVirtualHostNameToFolderMapping("static", staticFiles, CoreWebView2HostResourceAccessKind.Allow);

            wv2.WebMessageReceived += OnWebMessage;
        }

        private static void OnWebMessage(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                using var doc  = System.Text.Json.JsonDocument.Parse(e.WebMessageAsJson);
                var root = doc.RootElement;
                if (!root.TryGetProperty("__gglog", out _)) return;

                var level = root.GetProperty("level").GetString() ?? "info";
                var msg   = root.GetProperty("msg").GetString()   ?? "";

                switch (level)
                {
                    case "error": GGModDebugLog.Error(msg); break;
                    case "warn":  GGModDebugLog.Warn(msg);  break;
                    default:      GGModDebugLog.Info(msg);  break;
                }
            }
            catch { }
        }

        /// <summary>
        /// Gets web view process ids.
        /// </summary>
        /// <returns>Process ids, empty list if web view is not initialized.</returns>
        public IEnumerable<int> GetWebViewProcessIds() => Enumerable.Empty<int>();

        /// <summary>
        /// Gets window handle.
        /// </summary>
        public IntPtr Handle { get; } = IntPtr.Zero;
    }
}
