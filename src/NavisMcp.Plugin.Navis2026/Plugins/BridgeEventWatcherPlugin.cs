using Autodesk.Navisworks.Api.Plugins;
using NavisMcp.Plugin.Navis2026.Bridge;

namespace NavisMcp.Plugin.Navis2026.Plugins
{
    [Plugin("NavisMcp.BridgeEventWatcher", "NMCP", DisplayName = "Navis MCP Bridge", ToolTip = "Starts the local Navis MCP bridge.")]
    public sealed class BridgeEventWatcherPlugin : EventWatcherPlugin
    {
        public override void OnLoaded()
        {
            BridgeAssemblyResolver.Install();
            BridgeRuntime.Start();
        }

        public override void OnUnloading()
        {
            BridgeRuntime.Stop();
        }
    }
}
