using System.ComponentModel;
using ModelContextProtocol.Server;
using NavisMcp.Contracts;
using NavisMcp.Server.Infrastructure;

namespace NavisMcp.Server.Tools;

[McpServerToolType]
[Description("Report tools for exports, issue packs, BCF, QA, viewport capture, and visual QA.")]
public static class NavisReportTools
{
    [McpServerTool]
    [Description("Capture the Navisworks viewport using native Navisworks rendering and save it under the project snapshots folder.")]
    public static Task<object> nwd_capture_viewport(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        ServerOptions options,
        [Description("Output image width in pixels. Clamped by the Navisworks bridge to 64..4096.")] int width = 1920,
        [Description("Output image height in pixels. Clamped by the Navisworks bridge to 64..4096.")] int height = 1080,
        [Description("Image format: png, jpg, or jpeg.")] string format = "png",
        [Description("File name or path under the project snapshots folder. Extension is appended when omitted.")] string fileName = "",
        [Description("Image style: scene, scene_plus_overlay, or scene_using_ray_trace.")] string style = "scene_plus_overlay",
        [Description("Framing mode: current, auto, selection, model, front_right_top, iso, route_overview, top, front, back, right, left. Non-current modes require --allow-writes.")] string framing = "auto",
        [Description("Whether sectioning should affect the generated image.")] bool enableSectioning = true,
        [Description("Rendering time hint in seconds. Clamped by the Navisworks bridge to 0..60.")] double maxTimeHintSeconds = 2.0,
        [Description("Restore the original active view after applying a framing mode.")] bool restoreView = false,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        return ToolInvocation.Run(async () =>
        {
            var normalizedFraming = ToolInvocation.NormalizeSnapshotFraming(framing);
            if (ToolInvocation.SnapshotRequiresWrites(normalizedFraming))
            {
                writeGate.EnsureAllowed();
            }

            var snapshotPath = SnapshotPathGuard.ResolveSnapshotPath(options, fileName, format);
            var parameters = new ViewportSnapshotParams
            {
                FilePath = snapshotPath.FilePath,
                RelativePath = snapshotPath.RelativePath,
                Width = width,
                Height = height,
                Format = snapshotPath.Format,
                Style = style,
                Framing = normalizedFraming,
                EnableSectioning = enableSectioning,
                MaxTimeHintSeconds = maxTimeHintSeconds,
                RestoreView = restoreView
            };

            if (ToolInvocation.SnapshotRequiresWrites(normalizedFraming))
            {
                await audit.WriteAsync("nwd_capture_viewport", new { targetId, parameters }, CancellationToken.None).ConfigureAwait(false);
            }

            var data = await client.InvokeAsync<ViewportSnapshotResult>("nwd_capture_viewport", parameters, targetId, NavisMcpDefaults.LongTimeoutMs).ConfigureAwait(false);
            return ToolInvocation.ToolEnvelope.Success(data);
        });
    }

    [McpServerTool]
    [Description("Export selected/search item properties as CSV/JSON/HTML under reports/. Requires --allow-writes.")]
    public static Task<object> nwd_export_items_table(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        ServerOptions options,
        string fileName = "",
        string format = "csv",
        string query = "",
        string[]? itemIds = null,
        bool useCurrentSelection = false,
        int maxItems = 500,
        string? scopeItemId = null,
        string? sourceModelContains = null,
        string? classContains = null,
        string? categoryContains = null,
        string? propertyCategoryContains = null,
        string? propertyNameContains = null,
        string? propertyValueContains = null,
        string? materialContains = null,
        string? tagContains = null,
        string? targetId = null)
    {
        return ToolInvocation.Run(async () =>
        {
            writeGate.EnsureAllowed();
            var path = ReportPathGuard.ResolveReportPath(options, fileName, format, "items-table");
            var parameters = new ItemsTableExportParams
            {
                FilePath = path.FilePath,
                RelativePath = path.RelativePath,
                Format = path.Format,
                Query = query,
                ItemIds = itemIds?.ToList() ?? new List<string>(),
                UseCurrentSelection = useCurrentSelection,
                MaxItems = maxItems,
                ScopeItemId = scopeItemId,
                Filter = ToolInvocation.BuildSearchFilter(sourceModelContains, classContains, categoryContains, propertyCategoryContains, propertyNameContains, propertyValueContains, materialContains, tagContains)
            };
            await audit.WriteAsync("nwd_export_items_table", new { targetId, parameters }, CancellationToken.None).ConfigureAwait(false);
            var data = await client.InvokeAsync<ReportResult>("nwd_export_items_table", parameters, targetId, NavisMcpDefaults.LongTimeoutMs).ConfigureAwait(false);
            return ToolInvocation.ToolEnvelope.Success(data);
        });
    }

    [McpServerTool]
    [Description("Create a local issue summary bundle under reports/. Requires --allow-writes.")]
    public static Task<object> nwd_create_issue_summary(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        ServerOptions options,
        string fileName = "",
        string format = "json",
        string title = "",
        string query = "",
        string? clashTestId = null,
        int maxItems = 100,
        string? scopeItemId = null,
        string? sourceModelContains = null,
        string? classContains = null,
        string? categoryContains = null,
        string? propertyCategoryContains = null,
        string? propertyNameContains = null,
        string? propertyValueContains = null,
        string? materialContains = null,
        string? tagContains = null,
        string? targetId = null)
    {
        return ToolInvocation.Run(async () =>
        {
            writeGate.EnsureAllowed();
            var path = ReportPathGuard.ResolveReportPath(options, fileName, format, "issue-summary");
            var parameters = new IssueSummaryParams
            {
                FilePath = path.FilePath,
                RelativePath = path.RelativePath,
                Format = path.Format,
                Title = title,
                Query = query,
                ClashTestId = clashTestId,
                MaxItems = maxItems,
                ScopeItemId = scopeItemId,
                Filter = ToolInvocation.BuildSearchFilter(sourceModelContains, classContains, categoryContains, propertyCategoryContains, propertyNameContains, propertyValueContains, materialContains, tagContains)
            };
            await audit.WriteAsync("nwd_create_issue_summary", new { targetId, parameters }, CancellationToken.None).ConfigureAwait(false);
            var data = await client.InvokeAsync<ReportResult>("nwd_create_issue_summary", parameters, targetId, NavisMcpDefaults.LongTimeoutMs).ConfigureAwait(false);
            return ToolInvocation.ToolEnvelope.Success(data);
        });
    }

    [McpServerTool]
    [Description("Run local QA checks for missing properties and duplicate tags. Writes a report only when fileName is provided.")]
    public static Task<object> nwd_run_qa_checks(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        ServerOptions options,
        string query = "",
        string[]? requiredProperties = null,
        bool checkDuplicateTags = true,
        int maxItems = 1000,
        string? scopeItemId = null,
        string fileName = "",
        string format = "json",
        string? sourceModelContains = null,
        string? classContains = null,
        string? categoryContains = null,
        string? propertyCategoryContains = null,
        string? propertyNameContains = null,
        string? propertyValueContains = null,
        string? materialContains = null,
        string? tagContains = null,
        string? targetId = null)
    {
        return ToolInvocation.Run(async () =>
        {
            GuardedPath? path = null;
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                writeGate.EnsureAllowed();
                path = ReportPathGuard.ResolveReportPath(options, fileName, format, "qa-checks");
            }

            var parameters = new QaCheckParams
            {
                Query = query,
                RequiredProperties = requiredProperties?.ToList() ?? new List<string>(),
                CheckDuplicateTags = checkDuplicateTags,
                MaxItems = maxItems,
                ScopeItemId = scopeItemId,
                FilePath = path?.FilePath ?? string.Empty,
                RelativePath = path?.RelativePath ?? string.Empty,
                Format = path?.Format ?? format,
                Filter = ToolInvocation.BuildSearchFilter(sourceModelContains, classContains, categoryContains, propertyCategoryContains, propertyNameContains, propertyValueContains, materialContains, tagContains)
            };
            if (path is not null)
            {
                await audit.WriteAsync("nwd_run_qa_checks", new { targetId, parameters }, CancellationToken.None).ConfigureAwait(false);
            }

            var data = await client.InvokeAsync<QaCheckResult>("nwd_run_qa_checks", parameters, targetId, NavisMcpDefaults.LongTimeoutMs).ConfigureAwait(false);
            return ToolInvocation.ToolEnvelope.Success(data);
        });
    }

    [McpServerTool]
    [Description("Write a JSON+markdown issue evidence pack under reports/. Requires --allow-writes.")]
    public static Task<object> nwd_create_issue_pack(
        WriteGate writeGate,
        AuditLogger audit,
        ServerOptions options,
        [Description("Issue title.")] string title = "",
        [Description("Status field, for example open, reviewed, approved.")] string status = "open",
        [Description("Optional assignee.")] string? assignee = null,
        [Description("Optional priority.")] string? priority = null,
        [Description("Optional description.")] string? description = null,
        [Description("Item ids to include.")] string[]? itemIds = null,
        [Description("Optional snapshot path under project root.")] string? snapshotPath = null,
        [Description("Optional clash test id.")] string? clashTestId = null,
        [Description("Optional clash result id.")] string? clashResultId = null,
        [Description("Optional clash point.")] string? clashPoint = null,
        [Description("Optional clash distance.")] string? distance = null,
        [Description("File name under reports/ for the JSON pack.")] string fileName = "")
    {
        return ToolInvocation.Run(async () =>
        {
            writeGate.EnsureAllowed();
            var jsonPath = ReportPathGuard.ResolveReportPath(options, string.IsNullOrWhiteSpace(fileName) ? string.Empty : fileName, "json", "issue-pack");
            var mdFileName = Path.GetFileNameWithoutExtension(jsonPath.FilePath) + ".md";
            var mdPath = ReportPathGuard.ResolveReportPath(options, mdFileName, "md", "issue-pack");
            string? snapshotRelative = null;
            string? snapshotAbsolute = null;
            if (!string.IsNullOrWhiteSpace(snapshotPath))
            {
                snapshotAbsolute = Path.IsPathRooted(snapshotPath)
                    ? Path.GetFullPath(snapshotPath)
                    : Path.GetFullPath(Path.Combine(options.ProjectRoot, snapshotPath));
                snapshotRelative = Path.GetRelativePath(options.ProjectRoot, snapshotAbsolute);
            }

            var parameters = new IssuePackParams
            {
                FilePath = jsonPath.FilePath,
                RelativePath = jsonPath.RelativePath,
                MarkdownFilePath = mdPath.FilePath,
                MarkdownRelativePath = mdPath.RelativePath,
                Title = title,
                Status = status,
                Assignee = assignee,
                Priority = priority,
                Description = description,
                ItemIds = itemIds?.ToList() ?? new List<string>(),
                SnapshotPath = snapshotAbsolute,
                SnapshotRelativePath = snapshotRelative,
                ClashTestId = clashTestId,
                ClashResultId = clashResultId,
                ClashPoint = clashPoint,
                Distance = distance
            };
            await audit.WriteAsync("nwd_create_issue_pack", new { parameters.Title, parameters.Status, parameters.RelativePath }, CancellationToken.None).ConfigureAwait(false);
            var data = IssuePackWriter.Write(parameters);
            return ToolInvocation.ToolEnvelope.Success(data);
        });
    }

    [McpServerTool]
    [Description("Alias for nwd_create_issue_pack: write an evidence pack (snapshot + clash ids + item ids + markdown) under reports/. Requires --allow-writes.")]
    public static Task<object> nwd_create_evidence_pack(
        WriteGate writeGate,
        AuditLogger audit,
        ServerOptions options,
        [Description("Evidence pack title.")] string title = "",
        [Description("Status field, for example open, reviewed, approved.")] string status = "open",
        [Description("Optional assignee.")] string? assignee = null,
        [Description("Optional priority.")] string? priority = null,
        [Description("Optional description.")] string? description = null,
        [Description("Item ids to include.")] string[]? itemIds = null,
        [Description("Optional snapshot path under project root.")] string? snapshotPath = null,
        [Description("Optional clash test id.")] string? clashTestId = null,
        [Description("Optional clash result id.")] string? clashResultId = null,
        [Description("Optional clash point.")] string? clashPoint = null,
        [Description("Optional clash distance.")] string? distance = null,
        [Description("File name under reports/ for the JSON pack.")] string fileName = "")
    {
        return nwd_create_issue_pack(
            writeGate,
            audit,
            options,
            title,
            status,
            assignee,
            priority,
            description,
            itemIds,
            snapshotPath,
            clashTestId,
            clashResultId,
            clashPoint,
            distance,
            fileName);
    }

    [McpServerTool]
    [Description("Create a minimal open BCF 2.1 zip from an issue pack JSON under reports/. Requires --allow-writes.")]
    public static Task<object> nwd_export_bcf(
        WriteGate writeGate,
        AuditLogger audit,
        ServerOptions options,
        [Description("Issue pack JSON path under reports/ or absolute under project reports root.")] string issuePackPath,
        [Description("Output BCF zip file name under reports/.")] string fileName = "",
        [Description("Optional title override for the BCF topic.")] string title = "")
    {
        return ToolInvocation.Run(async () =>
        {
            writeGate.EnsureAllowed();
            var packPath = ToolInvocation.ResolveReportsInputPath(options, issuePackPath);
            var output = ReportPathGuard.ResolveReportPath(
                options,
                string.IsNullOrWhiteSpace(fileName) ? Path.GetFileNameWithoutExtension(packPath) + ".bcfzip" : fileName,
                "bcfzip",
                "issue-bcf");
            await audit.WriteAsync("nwd_export_bcf", new { issuePackPath = packPath, output = output.RelativePath }, CancellationToken.None).ConfigureAwait(false);
            var data = BcfExport.CreateFromIssuePack(packPath, output.FilePath, output.RelativePath, title);
            return ToolInvocation.ToolEnvelope.Success(data);
        });
    }

    [McpServerTool]
    [Description("Write Phase 0 selection QA markdown/json under reports/ summarizing current selection or search samples. Requires --allow-writes.")]
    public static Task<object> nwd_run_phase0_selection_qa(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        ServerOptions options,
        [Description("Optional search query when not using current selection.")] string query = "",
        [Description("Use current Navisworks selection.")] bool useCurrentSelection = true,
        [Description("Maximum sample names to include.")] int maxSamples = 50,
        [Description("Optional scope item id.")] string? scopeItemId = null,
        [Description("Markdown file name under reports/.")] string fileName = "",
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        return ToolInvocation.Run(async () =>
        {
            writeGate.EnsureAllowed();
            var mdPath = ReportPathGuard.ResolveReportPath(options, fileName, "md", "phase0-selection-qa");
            var jsonPath = ReportPathGuard.ResolveReportPath(options, Path.GetFileNameWithoutExtension(mdPath.FilePath) + ".json", "json", "phase0-selection-qa");

            DocumentInfo? document = null;
            try
            {
                document = await client.InvokeAsync<DocumentInfo>("nwd_get_document_info", null, targetId).ConfigureAwait(false);
            }
            catch (NavisRpcException)
            {
                document = null;
            }

            var samples = new List<ItemRef>();
            if (useCurrentSelection)
            {
                var selection = await client.InvokeAsync<SelectionResult>("nwd_get_current_selection", null, targetId).ConfigureAwait(false);
                samples = selection.Items.Take(Math.Max(1, maxSamples)).ToList();
            }
            else if (!string.IsNullOrWhiteSpace(query))
            {
                var found = await client.InvokeAsync<ItemSearchResult>("nwd_find_items", new FindItemsParams
                {
                    Query = query,
                    MaxResults = Math.Max(1, maxSamples),
                    ScopeItemId = scopeItemId,
                    AccentInsensitive = true
                }, targetId).ConfigureAwait(false);
                samples = found.Items;
            }

            var payload = new Phase0SelectionQaResult
            {
                Written = false,
                FilePath = mdPath.FilePath,
                RelativePath = mdPath.RelativePath,
                JsonFilePath = jsonPath.FilePath,
                JsonRelativePath = jsonPath.RelativePath,
                SelectionCount = samples.Count,
                SampleCount = samples.Count,
                SampleNames = samples.Select(s => s.DisplayName).Where(n => !string.IsNullOrWhiteSpace(n)).Take(maxSamples).ToList(),
                Must = new List<string> { "(fill) discipline Must terms" },
                Should = new List<string> { "(fill) Should terms" },
                Qa = new List<string> { "(fill) QA checks" },
                List = new List<string> { "(fill) inventory List terms" },
                Exclude = new List<string> { "(fill) Exclude / false positives" },
                Message = "Phase 0 selection QA written."
            };

            var jsonBody = new
            {
                documentTitle = document?.Title,
                documentPath = document?.FilePath,
                modelCount = document?.ModelCount,
                units = document?.Units,
                query,
                useCurrentSelection,
                scopeItemId,
                generatedUtc = DateTimeOffset.UtcNow,
                selectionCount = payload.SelectionCount,
                sampleNames = payload.SampleNames,
                sampleItems = samples,
                taxonomy = new
                {
                    must = payload.Must,
                    should = payload.Should,
                    qa = payload.Qa,
                    list = payload.List,
                    exclude = payload.Exclude
                }
            };
            File.WriteAllText(jsonPath.FilePath, System.Text.Json.JsonSerializer.Serialize(jsonBody, JsonDefaults.Options));

            var md = new System.Text.StringBuilder();
            md.AppendLine("# Phase 0 Selection QA");
            md.AppendLine();
            md.AppendLine("- Document: " + (document?.Title ?? "(unknown)"));
            md.AppendLine("- Models: " + (document?.ModelCount.ToString() ?? "?"));
            md.AppendLine("- Units: " + (document?.Units ?? "?"));
            md.AppendLine("- Selection/sample count: " + payload.SelectionCount);
            md.AppendLine("- Query: " + (string.IsNullOrWhiteSpace(query) ? "(current selection)" : query));
            md.AppendLine();
            md.AppendLine("## Sample names");
            md.AppendLine();
            foreach (var name in payload.SampleNames)
            {
                md.AppendLine("- " + name);
            }

            md.AppendLine();
            md.AppendLine("## Taxonomy placeholders");
            md.AppendLine();
            md.AppendLine("| Bucket | Notes |");
            md.AppendLine("| --- | --- |");
            md.AppendLine("| Must | " + string.Join("; ", payload.Must) + " |");
            md.AppendLine("| Should | " + string.Join("; ", payload.Should) + " |");
            md.AppendLine("| QA | " + string.Join("; ", payload.Qa) + " |");
            md.AppendLine("| List | " + string.Join("; ", payload.List) + " |");
            md.AppendLine("| Exclude | " + string.Join("; ", payload.Exclude) + " |");
            File.WriteAllText(mdPath.FilePath, md.ToString());

            payload.Written = File.Exists(mdPath.FilePath) && File.Exists(jsonPath.FilePath);
            await audit.WriteAsync("nwd_run_phase0_selection_qa", new { markdown = mdPath.RelativePath, json = jsonPath.RelativePath, payload.SelectionCount }, CancellationToken.None).ConfigureAwait(false);
            return ToolInvocation.ToolEnvelope.Success(payload);
        });
    }

    [McpServerTool]
    [Description("Capture multiple viewport framings to snapshots/ and write a visual QA checklist scaffold under reports/. Requires --allow-writes.")]
    public static Task<object> nwd_visual_qa_batch(
        PipeRpcClient client,
        WriteGate writeGate,
        AuditLogger audit,
        ServerOptions options,
        [Description("Comma-separated framings, for example auto,iso,top,selection.")] string framings = "auto,iso,top,selection",
        [Description("Snapshot width.")] int width = 1600,
        [Description("Snapshot height.")] int height = 900,
        [Description("Image format.")] string format = "png",
        [Description("File name prefix under snapshots/.")] string fileNamePrefix = "visual-qa",
        [Description("Checklist JSON file name under reports/.")] string checklistFileName = "",
        [Description("Restore view after each framing.")] bool restoreView = true,
        [Description("Optional Navisworks targetId.")] string? targetId = null)
    {
        return ToolInvocation.Run(async () =>
        {
            writeGate.EnsureAllowed();
            var framingList = (framings ?? string.Empty)
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ToolInvocation.NormalizeSnapshotFraming)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (framingList.Count == 0)
            {
                framingList = new List<string> { "auto", "iso", "top", "selection" };
            }

            var checklistPath = ReportPathGuard.ResolveReportPath(options, checklistFileName, "json", "visual-qa-checklist");
            var result = new VisualQaBatchResult
            {
                ChecklistFilePath = checklistPath.FilePath,
                ChecklistRelativePath = checklistPath.RelativePath,
                ChecklistItems = new List<string>
                {
                    "Confirm federated models are visible and correctly colored",
                    "Confirm selection framing isolates intended discipline",
                    "Confirm top/iso views show expected corridor extents",
                    "Note any hidden/section artifacts for follow-up"
                }
            };

            var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
            foreach (var framing in framingList)
            {
                var entry = new VisualQaSnapshotEntry { Framing = framing };
                try
                {
                    if (ToolInvocation.SnapshotRequiresWrites(framing))
                    {
                        writeGate.EnsureAllowed();
                    }

                    var snapshotPath = SnapshotPathGuard.ResolveSnapshotPath(
                        options,
                        $"{fileNamePrefix}-{stamp}-{framing}.{format}",
                        format);
                    var parameters = new ViewportSnapshotParams
                    {
                        FilePath = snapshotPath.FilePath,
                        RelativePath = snapshotPath.RelativePath,
                        Width = width,
                        Height = height,
                        Format = snapshotPath.Format,
                        Style = "scene_plus_overlay",
                        Framing = framing,
                        EnableSectioning = true,
                        MaxTimeHintSeconds = 2.0,
                        RestoreView = restoreView
                    };
                    var snap = await client.InvokeAsync<ViewportSnapshotResult>("nwd_capture_viewport", parameters, targetId, NavisMcpDefaults.LongTimeoutMs).ConfigureAwait(false);
                    entry.Ok = true;
                    entry.FilePath = snap.FilePath;
                    entry.RelativePath = snap.RelativePath;
                }
                catch (Exception ex)
                {
                    entry.Ok = false;
                    entry.Error = ex.Message;
                }

                result.Snapshots.Add(entry);
            }

            var checklistPayload = new
            {
                generatedUtc = DateTimeOffset.UtcNow,
                framings = framingList,
                snapshots = result.Snapshots,
                checklist = result.ChecklistItems.Select(item => new { text = item, status = "pending" }).ToList()
            };
            File.WriteAllText(checklistPath.FilePath, System.Text.Json.JsonSerializer.Serialize(checklistPayload, JsonDefaults.Options));
            result.Written = File.Exists(checklistPath.FilePath);
            result.Message = "Visual QA batch complete.";
            await audit.WriteAsync("nwd_visual_qa_batch", new { checklistPath.RelativePath, framingCount = framingList.Count }, CancellationToken.None).ConfigureAwait(false);
            return ToolInvocation.ToolEnvelope.Success(result);
        });
    }
}
