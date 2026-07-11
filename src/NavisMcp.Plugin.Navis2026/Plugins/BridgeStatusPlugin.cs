using System;
using System.Windows.Forms;
using Autodesk.Navisworks.Api.Plugins;
using NavisMcp.Plugin.Navis2026.Bridge;

namespace NavisMcp.Plugin.Navis2026.Plugins
{
    [Plugin("NavisMcp.BridgeStatus", "NMCP", DisplayName = "Navis MCP Bridge Status", ToolTip = "Show or restart the local Navis MCP bridge.")]
    [AddInPlugin(AddInLocation.AddIn)]
    public sealed class BridgeStatusPlugin : AddInPlugin
    {
        public override int Execute(params string[] parameters)
        {
            try
            {
                var restarted = false;
                if (parameters != null && parameters.Length > 0 && string.Equals(parameters[0], "restart", StringComparison.OrdinalIgnoreCase))
                {
                    BridgeRuntime.Restart();
                    restarted = true;
                }
                else if (!BridgeRuntime.IsRunning)
                {
                    BridgeRuntime.Start();
                }

                if (restarted)
                {
                    MessageBox.Show(BridgeRuntime.Status, "Navis MCP Bridge", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return 0;
                }

                var status = BridgeRuntime.Status + Environment.NewLine + Environment.NewLine + "Restart bridge?";
                var restart = MessageBox.Show(status, "Navis MCP Bridge", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (restart == DialogResult.Yes)
                {
                    BridgeRuntime.Restart();
                    MessageBox.Show(BridgeRuntime.Status, "Navis MCP Bridge", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Navis MCP Bridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
        }
    }
}
