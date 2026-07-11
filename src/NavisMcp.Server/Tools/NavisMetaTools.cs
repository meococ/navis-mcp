using System.ComponentModel;
using System.Globalization;
using System.Text;
using ModelContextProtocol.Server;
using NavisMcp.Contracts;
using NavisMcp.Server.Infrastructure;
using NavisMcp.Server.Playbooks;

namespace NavisMcp.Server.Tools;

[McpServerToolType]
[Description("Meta tools for targets, health, document info, federation, playbooks, and toolsets.")]
public static class NavisMetaTools
{
    [McpServerTool]
    [Description("List active local Navisworks Manage 2026 bridge targets discovered from session descriptors.")]
    public static object nwd_list_targets(SessionRegistry sessions)
    {
        return ToolInvocation.ToolEnvelope.Success(new TargetListResult { Targets = sessions.ListPublicTargets().ToList() });
    }

    [McpServerTool]
    [Description("Check bridge reachability, active document metadata, and whether write tools are enabled on this MCP server.")]
    public static Task<object> nwd_health_check(
        PipeRpcClient client,
        WriteGate writeGate,
        [Description("Optional Navisworks targetId from nwd_list_targets. Defaults to latest reachable target.")] string? targetId = null)
    {
        return ToolInvocation.Read<HealthResult>(client, "nwd_health_check", null, targetId, NavisMcpDefaults.DefaultTimeoutMs, result =>
        {
            result.WritesEnabled = writeGate.AllowWrites;
            if (result.Target is not null)
            {
                result.Target = SessionRegistry.ToPublicDescriptor(result.Target);
            }

            var serverVersion = typeof(NavisMetaTools).Assembly.GetName().Version?.ToString() ?? "0.0.0";
            var pluginVersion = result.PluginVersion
                ?? result.Target?.PluginVersion
                ?? string.Empty;
            result.ServerVersion = serverVersion;
            result.PluginVersion = string.IsNullOrWhiteSpace(pluginVersion) ? null : pluginVersion;
            result.VersionMismatch = !string.IsNullOrWhiteSpace(pluginVersion)
                && !string.Equals(ToolInvocation.NormalizeVersion(serverVersion), ToolInvocation.NormalizeVersion(pluginVersion), StringComparison.OrdinalIgnoreCase);
            result.VersionNote = result.VersionMismatch
                ? $"Server {serverVersion} and plugin {pluginVersion} differ; prefer matching NavisMcp builds."
                : string.IsNullOrWhiteSpace(pluginVersion)
                    ? "Plugin version was not reported by the bridge target."
                    : "Server and plugin versions match.";

            return result;
        });
    }

    [McpServerTool]
    [Description("Return title, file path, model count, units, and current selection count for the active document.")]
    public static Task<object> nwd_get_document_info(
        PipeRpcClient client,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        return ToolInvocation.Read<DocumentInfo>(client, "nwd_get_document_info", null, targetId);
    }

    [McpServerTool]
    [Description("Inventory linked/federated models from the active document (name, path, item count estimate, units).")]
    public static Task<object> nwd_get_federation_map(
        PipeRpcClient client,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        return ToolInvocation.Read<FederationMapResult>(client, "nwd_get_federation_map", null, targetId, NavisMcpDefaults.LongTimeoutMs);
    }

    [McpServerTool]
    [Description("Return built-in VN/linear infrastructure domain ontology search packs with false-positive notes.")]
    public static object nwd_list_domain_ontology()
    {
        return ToolInvocation.ToolEnvelope.Success(DomainOntology.ListPacks());
    }

    [McpServerTool]
    [Description("Write a federated preflight QA pack (document + model inventory + units + default clash tolerance record) under reports/. Requires --allow-writes.")]
    public static Task<object> nwd_run_federated_preflight(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        ServerOptions options,
        [Description("Default clash tolerance in meters to record in the report (project default 0.100).")] double defaultToleranceMeters = 0.1,
        [Description("Markdown file name under reports/.")] string fileName = "",
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        return ToolInvocation.Run(async () =>
        {
            writeGate.EnsureAllowed();
            var mdPath = ReportPathGuard.ResolveReportPath(options, fileName, "md", "federated-preflight");
            var jsonPath = ReportPathGuard.ResolveReportPath(options, Path.GetFileNameWithoutExtension(mdPath.FilePath) + ".json", "json", "federated-preflight");

            var document = await client.InvokeAsync<DocumentInfo>("nwd_get_document_info", null, targetId).ConfigureAwait(false);
            var federation = await client.InvokeAsync<FederationMapResult>("nwd_get_federation_map", null, targetId).ConfigureAwait(false);
            var warnings = new List<string>();
            if (federation.ModelCount == 0)
            {
                warnings.Add("No federated models reported.");
            }

            if (string.IsNullOrWhiteSpace(document.Units) && string.IsNullOrWhiteSpace(federation.Models.FirstOrDefault()?.Units))
            {
                warnings.Add("Document units were not reported; confirm meters before clash runs.");
            }

            if (Math.Abs(defaultToleranceMeters - 0.1) > 0.0001)
            {
                warnings.Add("Non-default clash tolerance recorded: " + defaultToleranceMeters.ToString("0.###", CultureInfo.InvariantCulture) + " m.");
            }

            var result = new FederatedPreflightResult
            {
                FilePath = mdPath.FilePath,
                RelativePath = mdPath.RelativePath,
                JsonFilePath = jsonPath.FilePath,
                JsonRelativePath = jsonPath.RelativePath,
                DocumentTitle = document.Title ?? federation.DocumentTitle,
                DocumentPath = document.FilePath ?? federation.DocumentPath,
                Units = document.Units ?? federation.Models.FirstOrDefault()?.Units,
                ModelCount = federation.ModelCount,
                DefaultToleranceMeters = defaultToleranceMeters,
                Warnings = warnings,
                Models = federation.Models,
                GeneratedUtc = DateTimeOffset.UtcNow,
                Message = "Federated preflight written under reports/."
            };

            var jsonPayload = new
            {
                result.DocumentTitle,
                result.DocumentPath,
                result.Units,
                result.ModelCount,
                result.DefaultToleranceMeters,
                result.Warnings,
                result.Models,
                result.GeneratedUtc
            };
            Directory.CreateDirectory(Path.GetDirectoryName(jsonPath.FilePath)!);
            File.WriteAllText(jsonPath.FilePath, System.Text.Json.JsonSerializer.Serialize(jsonPayload, JsonDefaults.Options));

            var sb = new StringBuilder();
            sb.AppendLine("# Federated preflight");
            sb.AppendLine();
            sb.AppendLine("- Document: " + result.DocumentTitle);
            sb.AppendLine("- Path: " + (result.DocumentPath ?? "(unknown)"));
            sb.AppendLine("- Units: " + (result.Units ?? "(unknown)"));
            sb.AppendLine("- Model count: " + result.ModelCount.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("- Default clash tolerance: " + defaultToleranceMeters.ToString("0.###", CultureInfo.InvariantCulture) + " m");
            sb.AppendLine();
            sb.AppendLine("## Models");
            sb.AppendLine();
            foreach (var model in federation.Models)
            {
                sb.AppendLine("- " + (model.DisplayName ?? model.SourceFileName ?? "(unnamed)") + " | " + (model.Units ?? "?") + " | items≈" + model.ItemCountEstimate.ToString(CultureInfo.InvariantCulture));
            }

            if (warnings.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("## Warnings");
                sb.AppendLine();
                foreach (var warning in warnings)
                {
                    sb.AppendLine("- " + warning);
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(mdPath.FilePath)!);
            File.WriteAllText(mdPath.FilePath, sb.ToString(), Encoding.UTF8);
            result.Written = File.Exists(mdPath.FilePath) && File.Exists(jsonPath.FilePath);

            await audit.WriteAsync("nwd_run_federated_preflight", new { result.RelativePath, result.ModelCount, defaultToleranceMeters }, CancellationToken.None).ConfigureAwait(false);
            return ToolInvocation.ToolEnvelope.Success(result);
        });
    }

    [McpServerTool]
    [Description("List coordination/safety playbook ids available on the MCP server.")]
    public static object nwd_list_playbooks()
    {
        return ToolInvocation.ToolEnvelope.Success(PlaybookCatalog.List());
    }

    [McpServerTool]
    [Description("Return the markdown content of a coordination/safety playbook.")]
    public static object nwd_get_playbook(
        [Description("Playbook id, for example coord.phase0_selection_qa or safety.preflight.")] string id)
    {
        return ToolInvocation.RunSync(() => ToolInvocation.ToolEnvelope.Success(PlaybookCatalog.Get(id)));
    }

    [McpServerTool]
    [Description("Document toolset groups (core, search, clash, report, write) and current --toolsets/--brief server options.")]
    public static object nwd_list_toolsets(ServerOptions options)
    {
        return ToolInvocation.ToolEnvelope.Success(ToolsetCatalog.List(options));
    }
}
