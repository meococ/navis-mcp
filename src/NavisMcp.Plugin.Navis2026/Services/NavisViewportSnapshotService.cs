using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Autodesk.Navisworks.Api;
using NavisMcp.Contracts;
using NavisMcp.Plugin.Navis2026.Bridge;

namespace NavisMcp.Plugin.Navis2026.Services
{
    internal sealed class NavisViewportSnapshotService
    {
        public ViewportSnapshotResult Capture(Document document, ViewportSnapshotParams parameters)
        {
            if (document == null)
            {
                throw new BridgeRpcException("no_document", "No active Navisworks document is open.");
            }

            parameters = parameters ?? new ViewportSnapshotParams();

            var filePath = Path.GetFullPath(parameters.FilePath ?? string.Empty);
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new BridgeRpcException("invalid_params", "Snapshot filePath is required.");
            }

            var width = Clamp(parameters.Width, 64, 4096);
            var height = Clamp(parameters.Height, 64, 4096);
            var format = NormalizeFormat(parameters.Format);
            var styleName = NormalizeStyleName(parameters.Style);
            var style = ParseStyle(styleName);
            var framing = NormalizeFraming(parameters.Framing);
            var maxTimeHint = Clamp(parameters.MaxTimeHintSeconds, 0.0, 60.0);
            var view = document.ActiveView;
            Viewpoint originalViewpoint = null;
            var framingApplied = "current";
            CurrentViewpointResult capturedViewpoint = null;

            if (view == null && framing != "current")
            {
                throw new BridgeRpcException("no_active_view", "No active Navisworks view is available for viewport framing.");
            }

            if (view != null && parameters.RestoreView && framing != "current")
            {
                originalViewpoint = view.CreateViewpointCopy();
            }

            try
            {
                if (view != null && framing != "current")
                {
                    framingApplied = ApplyFraming(document, view, framing);
                }

                capturedViewpoint = ReadCurrentViewpoint(document);

                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var bitmap = GenerateBitmap(document, view, style, width, height, maxTimeHint, parameters.EnableSectioning))
                {
                    if (bitmap == null)
                    {
                        throw new BridgeRpcException("snapshot_failed", "Navisworks did not return a bitmap for the active view.");
                    }

                    bitmap.Save(filePath, GetImageFormat(format));
                }

                return new ViewportSnapshotResult
                {
                    FilePath = filePath,
                    RelativePath = parameters.RelativePath ?? string.Empty,
                    Width = width,
                    Height = height,
                    Format = format,
                    Length = File.Exists(filePath) ? new FileInfo(filePath).Length : (long?)null,
                    Style = styleName,
                    FramingApplied = framingApplied,
                    DocumentTitle = document.Title ?? string.Empty,
                    SelectionCount = document.CurrentSelection.SelectedItems.Count,
                    Viewpoint = capturedViewpoint,
                    GeneratedUtc = DateTimeOffset.UtcNow
                };
            }
            catch (BridgeRpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new BridgeRpcException("snapshot_failed", "Failed to capture Navisworks viewport: " + ex.Message);
            }
            finally
            {
                if (view != null && originalViewpoint != null)
                {
                    TryRestoreView(view, originalViewpoint);
                }
            }
        }

        private static Bitmap GenerateBitmap(Document document, View view, ImageGenerationStyle style, int width, int height, double maxTimeHint, bool enableSectioning)
        {
            return view != null
                ? view.GenerateImage(style, width, height, maxTimeHint, enableSectioning)
                : document.GenerateImage(style, width, height, maxTimeHint, enableSectioning);
        }

        private static string ApplyFraming(Document document, View view, string framing)
        {
            switch (framing)
            {
                case "auto":
                    if (document.CurrentSelection.SelectedItems.Count > 0)
                    {
                        view.FocusOnCurrentSelection();
                        return "selection";
                    }

                    FitModel(document, view, orientFrontRightTop: true);
                    return "model";
                case "selection":
                    if (document.CurrentSelection.SelectedItems.Count == 0)
                    {
                        throw new BridgeRpcException("invalid_snapshot_framing", "Cannot apply selection framing because no items are selected.");
                    }

                    view.FocusOnCurrentSelection();
                    return "selection";
                case "model":
                    FitModel(document, view, orientFrontRightTop: false);
                    return "model";
                case "front_right_top":
                    FitModel(document, view, orientFrontRightTop: true);
                    return "front_right_top";
                case "iso":
                case "route_overview":
                    FitModel(document, view, orientFrontRightTop: true);
                    return framing;
                case "top":
                    FitModelFromDirection(document, view, document.UpVector.Negate(), document.FrontVector);
                    return "top";
                case "front":
                    FitModelFromDirection(document, view, document.FrontVector, document.UpVector);
                    return "front";
                case "back":
                    FitModelFromDirection(document, view, document.FrontVector.Negate(), document.UpVector);
                    return "back";
                case "right":
                    FitModelFromDirection(document, view, document.RightVector, document.UpVector);
                    return "right";
                case "left":
                    FitModelFromDirection(document, view, document.RightVector.Negate(), document.UpVector);
                    return "left";
                case "current":
                    return "current";
                default:
                    throw new BridgeRpcException("invalid_snapshot_framing", "Snapshot framing must be current, auto, selection, model, or front_right_top.");
            }
        }

        private static void FitModel(Document document, View view, bool orientFrontRightTop)
        {
            if (orientFrontRightTop)
            {
                view.LookFromFrontRightTop();
            }

            var viewpoint = view.CreateViewpointCopy();
            viewpoint.ZoomBox(document.GetBoundingBox(ignoreHidden: true));
            view.CopyViewpointFrom(viewpoint, ViewChange.JumpCut);
        }

        private static void FitModelFromDirection(Document document, View view, Vector3D direction, Vector3D up)
        {
            var viewpoint = view.CreateViewpointCopy();
            viewpoint.AlignDirection(direction);
            viewpoint.AlignUp(up);
            viewpoint.ZoomBox(document.GetBoundingBox(ignoreHidden: true));
            view.CopyViewpointFrom(viewpoint, ViewChange.JumpCut);
        }

        private static CurrentViewpointResult ReadCurrentViewpoint(Document document)
        {
            var viewpoint = document.CurrentViewpoint.Value;
            return new CurrentViewpointResult
            {
                Projection = SafeString(() => viewpoint.Projection.ToString()),
                Position = SafeString(() => viewpoint.Position.ToString()),
                Rotation = SafeString(() => viewpoint.Rotation.ToString())
            };
        }

        private static void TryRestoreView(View view, Viewpoint originalViewpoint)
        {
            try
            {
                view.CopyViewpointFrom(originalViewpoint, ViewChange.JumpCut);
            }
            catch
            {
            }
        }

        private static ImageFormat GetImageFormat(string format)
        {
            return string.Equals(format, "png", StringComparison.OrdinalIgnoreCase)
                ? ImageFormat.Png
                : ImageFormat.Jpeg;
        }

        private static ImageGenerationStyle ParseStyle(string style)
        {
            switch (style)
            {
                case "scene":
                    return ImageGenerationStyle.Scene;
                case "scene_using_ray_trace":
                    return ImageGenerationStyle.SceneUsingRayTrace;
                case "scene_plus_overlay":
                    return ImageGenerationStyle.ScenePlusOverlay;
                default:
                    throw new BridgeRpcException("invalid_snapshot_style", "Snapshot style must be scene, scene_plus_overlay, or scene_using_ray_trace.");
            }
        }

        private static string NormalizeStyleName(string style)
        {
            var value = NormalizeToken(style, "scene_plus_overlay");
            switch (value)
            {
                case "scene":
                case "scene_plus_overlay":
                case "sceneplusoverlay":
                    return value == "sceneplusoverlay" ? "scene_plus_overlay" : value;
                case "scene_using_ray_trace":
                case "sceneusingraytrace":
                case "raytrace":
                case "ray_trace":
                    return "scene_using_ray_trace";
                default:
                    throw new BridgeRpcException("invalid_snapshot_style", "Snapshot style must be scene, scene_plus_overlay, or scene_using_ray_trace.");
            }
        }

        private static string NormalizeFraming(string framing)
        {
            var value = NormalizeToken(framing, "auto");
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
                    throw new BridgeRpcException("invalid_snapshot_framing", "Snapshot framing must be current, auto, selection, model, or front_right_top.");
            }
        }

        private static string NormalizeFormat(string format)
        {
            var value = NormalizeToken(format, "png").TrimStart('.');
            switch (value)
            {
                case "png":
                    return "png";
                case "jpg":
                case "jpeg":
                    return "jpg";
                default:
                    throw new BridgeRpcException("invalid_snapshot_format", "Snapshot format must be png, jpg, or jpeg.");
            }
        }

        private static string NormalizeToken(string value, string defaultValue)
        {
            value = (value ?? string.Empty).Trim().ToLowerInvariant().Replace("-", "_").Replace(" ", "_");
            return value.Length == 0 ? defaultValue : value;
        }

        private static string SafeString(Func<string> getter)
        {
            try
            {
                return getter();
            }
            catch
            {
                return null;
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return min;
            }

            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
