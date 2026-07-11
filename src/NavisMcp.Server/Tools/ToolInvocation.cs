using NavisMcp.Contracts;
using NavisMcp.Server.Infrastructure;

namespace NavisMcp.Server.Tools;

internal static class ToolInvocation
{
    internal static SearchFilter? BuildSearchFilter(
        string? sourceModelContains,
        string? classContains,
        string? categoryContains,
        string? propertyCategoryContains,
        string? propertyNameContains,
        string? propertyValueContains,
        string? materialContains,
        string? tagContains)
    {
        if (new[] { sourceModelContains, classContains, categoryContains, propertyCategoryContains, propertyNameContains, propertyValueContains, materialContains, tagContains }
            .All(string.IsNullOrWhiteSpace))
        {
            return null;
        }

        return new SearchFilter
        {
            SourceModelContains = sourceModelContains,
            ClassContains = classContains,
            CategoryContains = categoryContains,
            PropertyCategoryContains = propertyCategoryContains,
            PropertyNameContains = propertyNameContains,
            PropertyValueContains = propertyValueContains,
            MaterialContains = materialContains,
            TagContains = tagContains
        };
    }

    internal static string ResolveReportsInputPath(ServerOptions options, string fileNameOrPath)
    {
        if (string.IsNullOrWhiteSpace(fileNameOrPath))
        {
            throw new NavisRpcException("invalid_params", "issuePackPath is required.");
        }

        var reportsRoot = Path.GetFullPath(options.ReportsRoot);
        var candidate = Path.IsPathRooted(fileNameOrPath)
            ? Path.GetFullPath(fileNameOrPath)
            : Path.GetFullPath(Path.Combine(reportsRoot, fileNameOrPath));

        var normalizedRoot = reportsRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new NavisRpcException("report_path_denied", $"Issue packs must be under '{reportsRoot}'.");
        }

        PathGuardPolicy.EnsureNoReparsePointInPath(reportsRoot, candidate, "report_path_denied", "Report");
        return candidate;
    }

    internal static string NormalizeVersion(string? version)
    {
        var value = (version ?? string.Empty).Trim();
        if (Version.TryParse(value, out var parsed))
        {
            return $"{parsed.Major}.{parsed.Minor}.{Math.Max(parsed.Build, 0)}";
        }

        return value;
    }

    internal static object RunSync(Func<ToolEnvelope> action)
    {
        try
        {
            return action();
        }
        catch (NavisRpcException ex)
        {
            return ToolEnvelope.Fail(ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return ToolEnvelope.Fail("server_error", ex.Message);
        }
    }

    internal static Task<object> Read<T>(PipeRpcClient client, string method, object? parameters, string? targetId, int timeoutMs = NavisMcpDefaults.DefaultTimeoutMs)
    {
        return Read(client, method, parameters, targetId, timeoutMs, (Func<T, object>)(x => x!));
    }

    internal static Task<object> Read<T>(PipeRpcClient client, string method, object? parameters, string? targetId, Func<T, object> transform)
    {
        return Read(client, method, parameters, targetId, NavisMcpDefaults.DefaultTimeoutMs, transform);
    }

    internal static Task<object> Read<T>(PipeRpcClient client, string method, object? parameters, string? targetId, int timeoutMs, Func<T, object> transform)
    {
        return Run(async () =>
        {
            var data = await client.InvokeAsync<T>(method, parameters, targetId, timeoutMs).ConfigureAwait(false);
            return ToolEnvelope.Success(transform(data));
        });
    }

    internal static Task<object> Write<T>(PipeRpcClient client, WriteGate writeGate, AuditLogger audit, string method, object? parameters, string? targetId, int timeoutMs = NavisMcpDefaults.DefaultTimeoutMs)
    {
        return Run(async () =>
        {
            writeGate.EnsureAllowed();
            await audit.WriteAsync(method, new { targetId, parameters }, CancellationToken.None).ConfigureAwait(false);
            var data = await client.InvokeAsync<T>(method, parameters, targetId, timeoutMs).ConfigureAwait(false);
            return ToolEnvelope.Success(data);
        });
    }

    internal static async Task<object> Run(Func<Task<ToolEnvelope>> action)
    {
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (NavisRpcException ex)
        {
            return ToolEnvelope.Fail(ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return ToolEnvelope.Fail("server_error", ex.Message);
        }
    }

    internal static bool SnapshotRequiresWrites(string? framing)
    {
        return !string.Equals(NormalizeSnapshotFraming(framing), "current", StringComparison.OrdinalIgnoreCase);
    }

    internal static string NormalizeSnapshotFraming(string? framing)
    {
        var value = (framing ?? string.Empty).Trim().ToLowerInvariant().Replace("-", "_");
        if (value.Length == 0)
        {
            value = "auto";
        }

        switch (value)
        {
            case "current":
            case "auto":
            case "selection":
            case "model":
            case "front_right_top":
            case "iso":
            case "route_overview":
            case "top":
            case "front":
            case "back":
            case "right":
            case "left":
                return value;
            case "frontrighttop":
            case "front_right":
                return "front_right_top";
            default:
                throw new NavisRpcException("invalid_snapshot_framing", "Snapshot framing must be current, auto, selection, model, front_right_top, iso, route_overview, top, front, back, right, or left.");
        }
    }

    internal sealed class ToolEnvelope
    {
        public bool Ok { get; private set; }
        public object? Data { get; private set; }
        public RpcError? Error { get; private set; }
        public string? ErrorCode => Error?.Code;

        public static ToolEnvelope Success(object? data)
        {
            return new ToolEnvelope { Ok = true, Data = data };
        }

        public static ToolEnvelope Fail(string code, string message)
        {
            return new ToolEnvelope
            {
                Ok = false,
                Error = new RpcError { Code = code, Message = message },
                Data = new { ok = false, errorCode = code, message }
            };
        }
    }
}
