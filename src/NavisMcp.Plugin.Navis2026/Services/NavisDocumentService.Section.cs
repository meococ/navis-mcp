using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Navisworks.Api;
using NavisMcp.Contracts;
using NavisMcp.Plugin.Navis2026.Bridge;

namespace NavisMcp.Plugin.Navis2026.Services
{
    internal sealed partial class NavisDocumentService
    {
        public WriteResult SetSectionBox(SectionBoxParams parameters)
        {
            var document = GetRequiredDocument();
            var mode = (parameters?.Mode ?? "clear").Trim().ToLowerInvariant();
            var viewpoint = document.CurrentViewpoint.CreateCopy();
            if (viewpoint == null)
            {
                throw new BridgeRpcException("not_found", "Current viewpoint is not available.");
            }

            if (mode == "clear" || mode == "off" || mode == "disable")
            {
                if (viewpoint.ClipPlanes != null)
                {
                    viewpoint.ClipPlanes.Enabled = false;
                }

                document.CurrentViewpoint.CopyFrom(viewpoint);
                return new WriteResult { Applied = true, AffectedCount = 0, Message = "Section box cleared." };
            }

            if (mode != "fit" && mode != "box" && mode != "selection" && mode != "items")
            {
                throw new BridgeRpcException("invalid_params", "mode must be clear, fit, box, selection, or items.");
            }

            List<ModelItem> items;
            if (parameters.ItemIds != null && parameters.ItemIds.Count > 0)
            {
                items = ResolveItems(document, parameters.ItemIds);
            }
            else
            {
                items = document.CurrentSelection.SelectedItems.Cast<ModelItem>().ToList();
            }

            if (items.Count == 0)
            {
                throw new BridgeRpcException("invalid_params", "Provide itemIds or select items before fitting a section box.");
            }

            BoundingBox3D union = null;
            foreach (var item in items)
            {
                BoundingBox3D box;
                try
                {
                    box = item.BoundingBox(true);
                }
                catch
                {
                    continue;
                }

                if (box == null || box.IsEmpty)
                {
                    continue;
                }

                union = union == null ? box : union.Extend(box);
            }

            if (union == null || union.IsEmpty)
            {
                throw new BridgeRpcException("not_found", "Could not compute a bounding box for the requested items.");
            }

            var pad = Math.Max(0.05, union.Size.Length * 0.02);
            var padded = new BoundingBox3D(
                new Point3D(union.Min.X - pad, union.Min.Y - pad, union.Min.Z - pad),
                new Point3D(union.Max.X + pad, union.Max.Y + pad, union.Max.Z + pad));

            if (viewpoint.ClipPlanes == null)
            {
                throw new BridgeRpcException("not_supported_by_api", "Current viewpoint does not expose clip planes.");
            }

            viewpoint.ClipPlanes.Mode = ClipPlaneSetMode.Box;
            viewpoint.ClipPlanes.Enabled = true;
            viewpoint.ClipPlanes.FitToBox(padded);
            document.CurrentViewpoint.CopyFrom(viewpoint);

            return new WriteResult
            {
                Applied = true,
                AffectedCount = items.Count,
                Message = "Section box fitted to " + items.Count + " item(s)."
            };
        }
    }
}
