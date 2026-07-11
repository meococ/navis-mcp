using System;
using System.IO;
using NavisMcp.Contracts;

namespace NavisMcp.Plugin.Navis2026.Bridge
{
    internal static class BridgeRequestPolicy
    {
        private static readonly object Gate = new object();
        private static string _exportsRoot = string.Empty;
        private static string _snapshotsRoot = string.Empty;
        private static string _reportsRoot = string.Empty;
        private static string _modelInputRoot = string.Empty;

        public static void EnsureTrusted(RequestContext context, SessionDescriptor descriptor)
        {
            if (context == null)
            {
                throw new BridgeRpcException("unauthorized", "Missing Navis MCP request context.");
            }

            if (!TokenComparer.EqualsConstantTime(descriptor.AuthToken, context.AuthToken))
            {
                throw new BridgeRpcException("unauthorized", "Invalid Navis MCP bridge token.");
            }

            EnsurePinnedRoots(context);
        }

        public static void EnsureWrites(RequestContext context, SessionDescriptor descriptor)
        {
            EnsureTrusted(context, descriptor);
            if (!context.AllowWrites)
            {
                throw new BridgeRpcException("writes_disabled", "This MCP server was started without --allow-writes.");
            }
        }

        public static void EnsurePathUnderRoot(string filePath, string root, string errorCode, string label)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new BridgeRpcException("invalid_params", label + " file path is required.");
            }

            if (string.IsNullOrWhiteSpace(root))
            {
                throw new BridgeRpcException("invalid_params", label + " root is missing from request context.");
            }

            var fullPath = Path.GetFullPath(filePath);
            var fullRoot = NormalizeRoot(root) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new BridgeRpcException(errorCode, label + " paths are restricted to '" + Path.GetFullPath(root) + "'.");
            }

            EnsureNoReparsePointInPath(fullRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), fullPath, errorCode, label);
        }

        private static void EnsurePinnedRoots(RequestContext context)
        {
            lock (Gate)
            {
                _exportsRoot = PinOrCompare(_exportsRoot, context.ExportsRoot, "exportsRoot");
                _snapshotsRoot = PinOrCompare(_snapshotsRoot, context.SnapshotsRoot, "snapshotsRoot");
                _reportsRoot = PinOrCompare(_reportsRoot, context.ReportsRoot, "reportsRoot");
                _modelInputRoot = PinOrCompare(_modelInputRoot, context.ModelInputRoot, "modelInputRoot");
            }
        }

        private static string PinOrCompare(string pinned, string supplied, string name)
        {
            if (string.IsNullOrWhiteSpace(supplied))
            {
                throw new BridgeRpcException("invalid_context", "Missing guarded root in request context: " + name + ".");
            }

            var normalized = NormalizeRoot(supplied);
            if (string.IsNullOrWhiteSpace(pinned))
            {
                return normalized;
            }

            if (!string.Equals(pinned, normalized, StringComparison.OrdinalIgnoreCase))
            {
                throw new BridgeRpcException("context_roots_changed", "Guarded roots changed after bridge context was established. Restart the Navis MCP bridge to switch project roots.");
            }

            return pinned;
        }

        private static string NormalizeRoot(string root)
        {
            return Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static void EnsureNoReparsePointInPath(string rootFull, string candidateFull, string errorCode, string label)
        {
            var directory = Directory.Exists(candidateFull) ? candidateFull : Path.GetDirectoryName(candidateFull);
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            CheckDirectory(rootFull, errorCode, label);
            var relative = directory.Length > rootFull.Length
                ? directory.Substring(rootFull.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                : string.Empty;
            if (relative.Length == 0)
            {
                return;
            }

            var current = rootFull;
            foreach (var segment in relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
            {
                current = Path.Combine(current, segment);
                if (Directory.Exists(current))
                {
                    CheckDirectory(current, errorCode, label);
                }
            }
        }

        private static void CheckDirectory(string path, string errorCode, string label)
        {
            var attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
            {
                throw new BridgeRpcException(errorCode, label + " paths cannot pass through a junction or symbolic link.");
            }
        }
    }
}
