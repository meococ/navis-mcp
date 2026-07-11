using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Autodesk.Navisworks.Api;
using NavisMcp.Contracts;
using NavisMcp.Plugin.Navis2026.Bridge;
using ContractDocumentInfo = NavisMcp.Contracts.DocumentInfo;
using NwApplication = Autodesk.Navisworks.Api.Application;

namespace NavisMcp.Plugin.Navis2026.Services
{
    internal sealed partial class NavisDocumentService
    {
        public Document GetRequiredDocument()
        {
            var document = NwApplication.ActiveDocument;
            if (document == null || document.IsClear)
            {
                throw new BridgeRpcException("no_document", "No active Navisworks document is open.");
            }

            return document;
        }

        public HealthResult Health(SessionDescriptor descriptor)
        {
            ContractDocumentInfo documentInfo = null;
            var status = "ok";

            try
            {
                documentInfo = GetDocumentInfo();
            }
            catch (BridgeRpcException ex) when (ex.Code == "no_document")
            {
                status = "no_document";
            }

            return new HealthResult
            {
                Status = status,
                Target = descriptor,
                Document = documentInfo,
                PluginVersion = descriptor == null ? null : descriptor.PluginVersion
            };
        }

        public ContractDocumentInfo GetDocumentInfo()
        {
            var document = GetRequiredDocument();
            return new ContractDocumentInfo
            {
                Title = document.Title ?? string.Empty,
                FilePath = EmptyAsNull(document.CurrentFileName),
                ModelCount = document.Models.Count,
                CurrentSelectionCount = document.CurrentSelection.SelectedItems.Count,
                Units = GetDocumentUnits(document)
            };
        }

        public FederationMapResult GetFederationMap()
        {
            var document = GetRequiredDocument();
            var result = new FederationMapResult
            {
                DocumentTitle = document.Title ?? string.Empty,
                DocumentPath = EmptyAsNull(document.CurrentFileName)
            };

            const int itemCountCap = 100000;
            foreach (Model model in document.Models)
            {
                var root = model.RootItem;
                var estimate = 0;
                foreach (var _ in EnumerateModelItem(root))
                {
                    estimate++;
                    if (estimate >= itemCountCap)
                    {
                        break;
                    }
                }

                string units = null;
                try
                {
                    units = model.Units.ToString();
                }
                catch
                {
                    units = null;
                }

                string guid = null;
                try
                {
                    guid = model.Guid == Guid.Empty ? null : model.Guid.ToString("D");
                }
                catch
                {
                    guid = null;
                }

                var sourcePath = EmptyAsNull(model.FileName) ?? EmptyAsNull(model.SourceFileName);
                result.Models.Add(new FederationModelInfo
                {
                    DisplayName = GetModelName(model) ?? (root == null ? "(unnamed)" : (root.DisplayName ?? "(unnamed)")),
                    SourcePath = sourcePath,
                    SourceFileName = sourcePath == null ? null : Path.GetFileName(sourcePath),
                    Guid = guid,
                    ItemCountEstimate = estimate,
                    Units = units,
                    RootItem = root == null ? null : ToItemRef(document, root)
                });
            }

            result.ModelCount = result.Models.Count;
            return result;
        }

        public ModelTreeResult GetModelTree(ModelTreeParams parameters)
        {
            var document = GetRequiredDocument();
            var maxDepth = Clamp(parameters.MaxDepth, 0, 20);
            var maxNodes = Clamp(parameters.MaxNodes, 1, 10000);
            var offset = ParseCursor(parameters.Cursor, 0);
            var result = new ModelTreeResult();
            var visited = 0;
            var returned = 0;
            var truncated = false;

            if (!string.IsNullOrWhiteSpace(parameters.ScopeItemId))
            {
                var scopedRoot = ResolveItem(document, new ItemLookupParams { ItemId = parameters.ScopeItemId });
                var scopedNode = BuildTreeNodePage(document, scopedRoot, 0, maxDepth, offset, maxNodes, ref visited, ref returned, ref truncated, result.Roots);
                if (scopedNode != null)
                {
                    result.Roots.Add(scopedNode);
                }

                result.ReturnedNodeCount = returned;
                result.Truncated = truncated;
                result.NextCursor = truncated ? visited.ToString(CultureInfo.InvariantCulture) : null;
                return result;
            }

            foreach (Model model in document.Models)
            {
                if (returned >= maxNodes)
                {
                    truncated = true;
                    break;
                }

                var node = BuildTreeNodePage(document, model.RootItem, 0, maxDepth, offset, maxNodes, ref visited, ref returned, ref truncated, result.Roots);
                if (node != null)
                {
                    result.Roots.Add(node);
                }

                if (truncated)
                {
                    break;
                }
            }

            result.ReturnedNodeCount = returned;
            result.Truncated = truncated;
            result.NextCursor = truncated ? visited.ToString(CultureInfo.InvariantCulture) : null;
            return result;
        }

        public ItemSearchResult FindItems(FindItemsParams parameters)
        {
            var document = GetRequiredDocument();
            var query = (parameters.Query ?? string.Empty).Trim();
            if (query.Length == 0)
            {
                throw new BridgeRpcException("invalid_params", "query is required.");
            }

            var maxResults = Clamp(parameters.MaxResults, 1, 5000);
            var offset = ParseCursor(parameters.Cursor, parameters.Offset);
            var result = new ItemSearchResult();
            var matchedCount = 0;

            foreach (var item in EnumerateSearchScope(document, parameters.ScopeItemId))
            {
                if (MatchesItem(item, query, parameters.IncludeProperties, parameters))
                {
                    if (matchedCount++ < offset)
                    {
                        continue;
                    }

                    result.Items.Add(ToItemRef(document, item));
                    if (result.Items.Count >= maxResults)
                    {
                        result.Truncated = true;
                        result.NextCursor = matchedCount.ToString(CultureInfo.InvariantCulture);
                        break;
                    }
                }
            }

            result.ReturnedCount = result.Items.Count;
            return result;
        }

        public ItemPropertiesResult GetItemProperties(ItemLookupParams parameters)
        {
            var document = GetRequiredDocument();
            var item = ResolveItem(document, parameters);
            var result = new ItemPropertiesResult
            {
                ItemRef = ToItemRef(document, item)
            };

            foreach (PropertyCategory category in item.PropertyCategories)
            {
                var categoryDto = new PropertyCategoryDto
                {
                    DisplayName = SafeString(() => category.DisplayName),
                    Name = SafeString(() => category.Name)
                };

                foreach (DataProperty property in category.Properties)
                {
                    categoryDto.Properties.Add(new PropertyDto
                    {
                        DisplayName = SafeString(() => property.DisplayName),
                        Name = SafeString(() => property.Name),
                        Value = FormatVariant(property.Value),
                        Type = SafeString(() => property.Value.DataType.ToString())
                    });
                }

                result.Categories.Add(categoryDto);
            }

            return result;
        }

        public PropertySchemaResult ListPropertySchema(PropertySchemaParams parameters)
        {
            var document = GetRequiredDocument();
            var maxItems = Clamp(parameters.MaxItems, 1, 50000);
            var maxCategories = Clamp(parameters.MaxCategories, 1, 1000);
            var maxProperties = Clamp(parameters.MaxPropertiesPerCategory, 1, 1000);
            var result = new PropertySchemaResult();
            var categories = new Dictionary<string, PropertySchemaCategory>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in EnumerateModelItems(document))
            {
                if (result.SampledItemCount++ >= maxItems)
                {
                    result.Truncated = true;
                    break;
                }

                foreach (PropertyCategory category in item.PropertyCategories)
                {
                    var categoryName = SafeString(() => category.DisplayName) ?? SafeString(() => category.Name) ?? "(unnamed)";
                    if (!categories.TryGetValue(categoryName, out var categoryDto))
                    {
                        if (categories.Count >= maxCategories)
                        {
                            result.Truncated = true;
                            continue;
                        }

                        categoryDto = new PropertySchemaCategory
                        {
                            DisplayName = categoryName,
                            Name = SafeString(() => category.Name)
                        };
                        categories.Add(categoryName, categoryDto);
                    }

                    foreach (DataProperty property in category.Properties)
                    {
                        if (categoryDto.Properties.Count >= maxProperties)
                        {
                            result.Truncated = true;
                            break;
                        }

                        var propertyName = SafeString(() => property.DisplayName) ?? SafeString(() => property.Name) ?? "(unnamed)";
                        if (categoryDto.Properties.Any(x => string.Equals(x.DisplayName, propertyName, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }

                        categoryDto.Properties.Add(new PropertySchemaProperty
                        {
                            DisplayName = propertyName,
                            Name = SafeString(() => property.Name),
                            Type = SafeString(() => property.Value.DataType.ToString())
                        });
                    }
                }
            }

            result.Categories.AddRange(categories.Values.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase));
            return result;
        }

        public SelectionResult GetCurrentSelection()
        {
            var document = GetRequiredDocument();
            return ToSelectionResult(document, document.CurrentSelection.SelectedItems, int.MaxValue);
        }

        public SavedItemListResult ListSelectionSets()
        {
            var document = GetRequiredDocument();
            var result = new SavedItemListResult();
            foreach (var item in FlattenSavedItems(document.SelectionSets.Value))
            {
                result.Items.Add(ToSavedItemInfo(document.SelectionSets, item, "ss"));
            }

            return result;
        }

        public SelectionResult GetSelectionSetItems(SelectionSetLookupParams parameters)
        {
            var document = GetRequiredDocument();
            var path = ParseScopedId(parameters.SelectionSetId, "ss");
            var item = document.SelectionSets.ResolveIndexPath(path);
            var set = item as SelectionSet;
            if (set == null)
            {
                throw new BridgeRpcException("not_found", "Selection set '" + parameters.SelectionSetId + "' was not found or is not a selection set.");
            }

            var maxItems = Clamp(parameters.MaxItems, 1, 10000);
            return ToSelectionResult(document, set.GetSelectedItems(document), maxItems);
        }

        public ViewpointListResult ListViewpoints()
        {
            var document = GetRequiredDocument();
            var result = new ViewpointListResult();
            foreach (var item in FlattenSavedItems(document.SavedViewpoints.Value))
            {
                result.Items.Add(ToViewpointInfo(document, item));
            }

            return result;
        }

        public CurrentViewpointResult GetCurrentViewpoint()
        {
            var document = GetRequiredDocument();
            var viewpoint = document.CurrentViewpoint.Value;
            return new CurrentViewpointResult
            {
                Projection = SafeString(() => viewpoint.Projection.ToString()),
                Position = SafeString(() => viewpoint.Position.ToString()),
                Rotation = SafeString(() => viewpoint.Rotation.ToString())
            };
        }

        public SelectionResult SelectItems(ItemsLookupParams parameters)
        {
            var document = GetRequiredDocument();
            var items = ResolveItems(document, parameters.ItemIds);

            if (parameters.PreserveExistingSelection)
            {
                var merged = new List<ModelItem>();
                foreach (ModelItem item in document.CurrentSelection.SelectedItems)
                {
                    merged.Add(item);
                }

                foreach (var item in items)
                {
                    if (!merged.Any(existing => existing.Equals(item)))
                    {
                        merged.Add(item);
                    }
                }

                document.CurrentSelection.CopyFrom(merged);
            }
            else
            {
                document.CurrentSelection.CopyFrom(items);
            }

            return GetCurrentSelection();
        }

        public SelectionResult SelectBySearch(SelectBySearchParams parameters)
        {
            var document = GetRequiredDocument();
            var query = (parameters.Query ?? string.Empty).Trim();
            if (query.Length == 0)
            {
                throw new BridgeRpcException("invalid_params", "query is required.");
            }

            var maxResults = Clamp(parameters.MaxResults, 1, 5000);
            var matches = new List<ModelItem>();
            foreach (var item in EnumerateSearchScope(document, parameters.ScopeItemId))
            {
                if (MatchesItem(item, query, includeProperties: false, new FindItemsParams { Query = query, MaxResults = maxResults, ScopeItemId = parameters.ScopeItemId }))
                {
                    matches.Add(item);
                    if (matches.Count >= maxResults)
                    {
                        break;
                    }
                }
            }

            document.CurrentSelection.CopyFrom(matches);
            return GetCurrentSelection();
        }

        public WriteResult ClearSelection()
        {
            var document = GetRequiredDocument();
            var count = document.CurrentSelection.SelectedItems.Count;
            document.CurrentSelection.Clear();
            return new WriteResult { Applied = true, AffectedCount = count, Message = "Selection cleared." };
        }

        public WriteResult GoToViewpoint(ViewpointLookupParams parameters)
        {
            var document = GetRequiredDocument();
            var path = ParseScopedId(parameters.ViewpointId, "vp");
            var item = document.SavedViewpoints.ResolveIndexPath(path);
            var saved = item as SavedViewpoint;
            if (saved == null)
            {
                throw new BridgeRpcException("not_found", "Viewpoint '" + parameters.ViewpointId + "' was not found or is not a saved viewpoint.");
            }

            document.SavedViewpoints.CurrentSavedViewpoint = saved;
            document.CurrentViewpoint.CopyFrom(saved.Viewpoint);
            return new WriteResult { Applied = true, AffectedCount = 1, Message = "Viewpoint applied." };
        }

        public ViewpointInfo SaveViewpoint(SaveViewpointParams parameters)
        {
            var document = GetRequiredDocument();
            var displayName = (parameters.DisplayName ?? string.Empty).Trim();
            if (displayName.Length == 0)
            {
                displayName = "MCP Viewpoint " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }

            var saved = new SavedViewpoint(document.CurrentViewpoint.CreateCopy())
            {
                DisplayName = displayName
            };

            document.SavedViewpoints.AddCopy(saved);

            SavedItem added = null;
            foreach (var item in FlattenSavedItems(document.SavedViewpoints.Value))
            {
                if (item is SavedViewpoint && string.Equals(item.DisplayName, displayName, StringComparison.Ordinal))
                {
                    added = item;
                }
            }

            return added == null
                ? new ViewpointInfo { DisplayName = displayName, Kind = "viewpoint" }
                : ToViewpointInfo(document, added);
        }

        public WriteResult UpdateViewpoint(SavedItemMutationParams parameters)
        {
            var document = GetRequiredDocument();
            var path = ParseScopedId(parameters.Id, "vp");
            var item = document.SavedViewpoints.ResolveIndexPath(path);
            var saved = item as SavedViewpoint;
            if (saved == null)
            {
                throw new BridgeRpcException("not_found", "Viewpoint '" + parameters.Id + "' was not found or is not a saved viewpoint.");
            }

            var copy = (SavedViewpoint)saved.CreateCopy();
            copy.DisplayName = string.IsNullOrWhiteSpace(parameters.DisplayName) ? saved.DisplayName : parameters.DisplayName.Trim();
            ReplaceSavedItem(document.SavedViewpoints, path, copy);
            return new WriteResult { Applied = true, AffectedCount = 1, Message = "Viewpoint updated." };
        }

        public WriteResult DeleteViewpoint(SavedItemMutationParams parameters)
        {
            var document = GetRequiredDocument();
            var path = ParseScopedId(parameters.Id, "vp");
            var item = document.SavedViewpoints.ResolveIndexPath(path);
            if (!(item is SavedViewpoint))
            {
                throw new BridgeRpcException("not_found", "Viewpoint '" + parameters.Id + "' was not found or is not a saved viewpoint.");
            }

            var removed = document.SavedViewpoints.Remove(item);
            return new WriteResult { Applied = removed, AffectedCount = removed ? 1 : 0, Message = removed ? "Viewpoint deleted." : "Viewpoint was not deleted." };
        }

        public SavedItemInfo CreateSelectionSet(SelectionSetMutationParams parameters)
        {
            var document = GetRequiredDocument();
            var displayName = (parameters.DisplayName ?? string.Empty).Trim();
            if (displayName.Length == 0)
            {
                throw new BridgeRpcException("invalid_params", "displayName is required.");
            }

            ModelItemCollection items;
            if (parameters.UseCurrentSelection)
            {
                items = document.CurrentSelection.SelectedItems;
            }
            else
            {
                items = new ModelItemCollection();
                items.AddRange(ResolveItems(document, parameters.ItemIds));
            }

            if (items.Count == 0)
            {
                throw new BridgeRpcException("invalid_params", "At least one item is required to create a selection set.");
            }

            var set = new SelectionSet(items) { DisplayName = displayName };
            document.SelectionSets.AddCopy(set);

            SavedItem added = null;
            foreach (var item in FlattenSavedItems(document.SelectionSets.Value))
            {
                if (item is SelectionSet && string.Equals(item.DisplayName, displayName, StringComparison.Ordinal))
                {
                    added = item;
                }
            }

            return added == null
                ? new SavedItemInfo { DisplayName = displayName, Kind = "selection_set" }
                : ToSavedItemInfo(document.SelectionSets, added, "ss");
        }

        public WriteResult DeleteSelectionSet(SavedItemMutationParams parameters)
        {
            var document = GetRequiredDocument();
            var path = ParseScopedId(parameters.Id, "ss");
            var item = document.SelectionSets.ResolveIndexPath(path);
            if (!(item is SelectionSet))
            {
                throw new BridgeRpcException("not_found", "Selection set '" + parameters.Id + "' was not found or is not a selection set.");
            }

            var removed = document.SelectionSets.Remove(item);
            return new WriteResult { Applied = removed, AffectedCount = removed ? 1 : 0, Message = removed ? "Selection set deleted." : "Selection set was not deleted." };
        }

        public SavedItemInfo CreateSearchSet(SearchSetDefinition parameters)
        {
            var document = GetRequiredDocument();
            var displayName = (parameters.DisplayName ?? string.Empty).Trim();
            if (displayName.Length == 0)
            {
                throw new BridgeRpcException("invalid_params", "displayName is required.");
            }

            var search = BuildSearch(parameters);
            var set = new SelectionSet(search) { DisplayName = displayName };
            document.SelectionSets.AddCopy(set);

            SavedItem added = null;
            foreach (var item in FlattenSavedItems(document.SelectionSets.Value))
            {
                if (item is SelectionSet selectionSet &&
                    selectionSet.HasSearch &&
                    string.Equals(item.DisplayName, displayName, StringComparison.Ordinal))
                {
                    added = item;
                }
            }

            return added == null
                ? new SavedItemInfo { DisplayName = displayName, Kind = "search_set" }
                : ToSavedItemInfo(document.SelectionSets, added, "ss");
        }

        public ReportResult ExportSearchSets(SearchSetImportExportParams parameters)
        {
            var document = GetRequiredDocument();
            if (string.IsNullOrWhiteSpace(parameters.FilePath))
            {
                throw new BridgeRpcException("invalid_params", "filePath is required.");
            }

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<exchange xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:noNamespaceSchemaLocation=\"http://download.autodesk.com/us/navisworks/schemas/nw-exchange-12.0.xsd\" units=\"m\" filename=\"navis-mcp-search-sets\" filepath=\"\">");
            sb.AppendLine("  <selectionsets>");

            var exported = 0;
            foreach (var item in FlattenSavedItems(document.SelectionSets.Value))
            {
                var set = item as SelectionSet;
                if (set == null || !set.HasSearch)
                {
                    continue;
                }

                exported++;
                sb.Append("    <selectionset name=\"");
                sb.Append(XmlEscape(set.DisplayName));
                sb.AppendLine("\" guid=\"\">");
                sb.AppendLine("      <findspec mode=\"all\" disjoint=\"0\">");
                sb.AppendLine("        <conditions>");
                sb.AppendLine("          <condition test=\"exported\" flags=\"0\"><note>Dynamic search set exported by NavisMcp; re-import via nwd_create_search_set.</note></condition>");
                sb.AppendLine("        </conditions>");
                sb.AppendLine("        <locator>/</locator>");
                sb.AppendLine("      </findspec>");
                sb.AppendLine("    </selectionset>");
            }

            sb.AppendLine("  </selectionsets>");
            sb.AppendLine("</exchange>");

            var directory = Path.GetDirectoryName(parameters.FilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(parameters.FilePath, sb.ToString(), new UTF8Encoding(false));
            return new ReportResult
            {
                Written = File.Exists(parameters.FilePath),
                FilePath = parameters.FilePath,
                RelativePath = parameters.RelativePath ?? string.Empty,
                Format = "xml",
                Length = File.Exists(parameters.FilePath) ? new FileInfo(parameters.FilePath).Length : (long?)null,
                DocumentTitle = document.Title ?? string.Empty,
                ItemCount = exported,
                GeneratedUtc = DateTimeOffset.UtcNow,
                Message = "Exported " + exported.ToString(CultureInfo.InvariantCulture) + " search set(s)."
            };
        }

        private static Search BuildSearch(SearchSetDefinition parameters)
        {
            var search = new Search();
            search.Selection.SelectAll();
            search.Locations = SearchLocations.DescendantsAndSelf;

            var conditions = parameters.Conditions ?? new List<SearchConditionDefinition>();
            if (conditions.Count == 0)
            {
                // Default: Item Name contains the display name (useful for domain presets).
                var fallback = SearchCondition.HasPropertyByDisplayName("Item", "Name")
                    .DisplayStringContains(parameters.DisplayName.Trim());
                search.SearchConditions.Add(fallback);
                return search;
            }

            foreach (var definition in conditions)
            {
                search.SearchConditions.Add(BuildSearchCondition(definition));
            }

            return search;
        }

        private static SearchCondition BuildSearchCondition(SearchConditionDefinition definition)
        {
            var category = string.IsNullOrWhiteSpace(definition.Category) ? "Item" : definition.Category.Trim();
            var property = string.IsNullOrWhiteSpace(definition.Property) ? "Name" : definition.Property.Trim();
            var op = (definition.Operator ?? "contains").Trim().ToLowerInvariant();
            var value = definition.Value ?? string.Empty;

            SearchCondition condition = SearchCondition.HasPropertyByDisplayName(category, property);
            switch (op)
            {
                case "equals":
                case "equal":
                case "=":
                    condition = condition.EqualValue(VariantData.FromDisplayString(value));
                    break;
                case "contains":
                case "displaynamecontains":
                case "displaystringcontains":
                    condition = condition.DisplayStringContains(value);
                    break;
                case "wildcard":
                case "displaynamewildcard":
                case "displaystringwildcard":
                    condition = condition.DisplayStringWildcard(value);
                    break;
                default:
                    throw new BridgeRpcException(
                        "invalid_params",
                        "Unsupported search operator '" + definition.Operator + "'. Use equals, contains, or wildcard.");
            }

            if (definition.Negate)
            {
                condition = condition.Negate();
            }

            return condition;
        }

        private static string XmlEscape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        public WriteResult SetHidden(SetHiddenParams parameters)
        {
            var document = GetRequiredDocument();
            var items = ResolveItems(document, parameters.ItemIds);
            document.Models.SetHidden(items, parameters.Hidden);
            return new WriteResult
            {
                Applied = true,
                AffectedCount = items.Count,
                Message = parameters.Hidden ? "Items hidden." : "Items unhidden."
            };
        }

        public WriteResult UnhideAll()
        {
            var document = GetRequiredDocument();
            document.Models.ResetAllHidden();
            return new WriteResult { Applied = true, Message = "All hidden state cleared." };
        }

        public ExportResult ExportNwd(ExportNwdParams parameters)
        {
            var document = GetRequiredDocument();
            var filePath = Path.GetFullPath(parameters.FilePath ?? string.Empty);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new NwdExportOptions
            {
                ExcludeHiddenItems = parameters.ExcludeHiddenItems,
                EmbedXrefs = parameters.EmbedXrefs,
                FileVersion = (int)DocumentFileVersion.Navisworks2026
            };

            var exported = document.TryExportToNwd(filePath, options);
            return new ExportResult
            {
                Exported = exported,
                FilePath = filePath,
                Length = File.Exists(filePath) ? new FileInfo(filePath).Length : (long?)null
            };
        }

        public WriteResult AppendModel(AppendModelParams parameters)
        {
            var document = GetRequiredDocument();
            var filePath = Path.GetFullPath(parameters.FilePath ?? string.Empty);
            var before = document.Models.Count;
            var appended = document.TryAppendFile(filePath);
            var after = document.Models.Count;
            return new WriteResult
            {
                Applied = appended,
                AffectedCount = Math.Max(0, after - before),
                Message = appended ? "Model appended." : "Navisworks did not append the model."
            };
        }

        public ReportResult ExportItemsTable(ItemsTableExportParams parameters)
        {
            var document = GetRequiredDocument();
            var items = ResolveReportItems(document, parameters.Query, parameters.ItemIds, parameters.UseCurrentSelection, parameters.MaxItems, parameters.Filter, parameters.ScopeItemId);
            var rows = items.Select(item => new ReportRow
            {
                Item = ToItemRef(document, item),
                Properties = parameters.IncludeProperties ? ReadPropertyMap(item) : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            }).ToList();

            WriteItemsReport(parameters.FilePath, parameters.Format, document, rows);
            return BuildReportResult(document, parameters.FilePath, parameters.RelativePath, parameters.Format, rows.Count, "Items table exported.");
        }

        public ReportResult CreateIssueSummary(IssueSummaryParams parameters)
        {
            var document = GetRequiredDocument();
            var items = ResolveReportItems(document, parameters.Query, new List<string>(), useCurrentSelection: false, parameters.MaxItems, parameters.Filter, parameters.ScopeItemId)
                .Select(item => ToItemRef(document, item))
                .ToList();
            var payload = new
            {
                title = string.IsNullOrWhiteSpace(parameters.Title) ? "Navis MCP Issue Summary" : parameters.Title,
                documentTitle = document.Title ?? string.Empty,
                documentPath = EmptyAsNull(document.CurrentFileName),
                generatedUtc = DateTimeOffset.UtcNow,
                query = parameters.Query ?? string.Empty,
                clashTestId = parameters.ClashTestId,
                items,
                note = "Cloud issue upload is intentionally not implemented in this local-first bridge."
            };

            WriteStructuredReport(parameters.FilePath, parameters.Format, payload, "Issue Summary");
            return BuildReportResult(document, parameters.FilePath, parameters.RelativePath, parameters.Format, items.Count, "Issue summary exported.");
        }

        public QaCheckResult RunQaChecks(QaCheckParams parameters)
        {
            var document = GetRequiredDocument();
            var items = ResolveReportItems(document, parameters.Query, new List<string>(), useCurrentSelection: false, parameters.MaxItems, parameters.Filter, parameters.ScopeItemId);
            var result = new QaCheckResult { CheckedItemCount = items.Count };
            var required = parameters.RequiredProperties ?? new List<string>();

            foreach (var item in items)
            {
                var properties = ReadPropertyMap(item);
                foreach (var property in required)
                {
                    if (!properties.Keys.Any(key => ContainsComparable(key, property, "contains", accentInsensitive: true, caseSensitive: false)))
                    {
                        result.Issues.Add(new QaIssue
                        {
                            Code = "missing_property",
                            Message = "Required property '" + property + "' is missing.",
                            Item = ToItemRef(document, item)
                        });
                    }
                }
            }

            if (parameters.CheckDuplicateTags)
            {
                foreach (var group in items.Select(item => new { Item = item, Tag = FindPropertyValue(item, "tag") })
                    .Where(x => !string.IsNullOrWhiteSpace(x.Tag))
                    .GroupBy(x => x.Tag, StringComparer.OrdinalIgnoreCase)
                    .Where(group => group.Count() > 1))
                {
                    foreach (var duplicate in group)
                    {
                        result.Issues.Add(new QaIssue
                        {
                            Code = "duplicate_tag",
                            Message = "Duplicate tag '" + group.Key + "'.",
                            Item = ToItemRef(document, duplicate.Item)
                        });
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(parameters.FilePath))
            {
                var payload = new
                {
                    documentTitle = document.Title ?? string.Empty,
                    documentPath = EmptyAsNull(document.CurrentFileName),
                    generatedUtc = DateTimeOffset.UtcNow,
                    checkedItemCount = result.CheckedItemCount,
                    issues = result.Issues
                };
                WriteStructuredReport(parameters.FilePath, parameters.Format, payload, "QA Checks");
                result.Report = BuildReportResult(document, parameters.FilePath, parameters.RelativePath, parameters.Format, result.CheckedItemCount, "QA report exported.");
            }

            return result;
        }

        public MeasureResult MeasureItems(MeasureParams parameters)
        {
            var document = GetRequiredDocument();
            var items = parameters.UseCurrentSelection
                ? document.CurrentSelection.SelectedItems.ToList()
                : ResolveItems(document, parameters.ItemIds);

            if (items.Count == 0)
            {
                throw new BridgeRpcException("invalid_params", "At least one item or a current selection is required.");
            }

            var boxes = items.Select(item => SafeString(() => item.BoundingBox(true).ToString())).Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
            return new MeasureResult
            {
                ItemCount = items.Count,
                BoundingBox = boxes.Count == 1 ? boxes[0] : string.Join("; ", boxes.Take(20).ToArray()),
                Message = boxes.Count == 0 ? "No bounding boxes were available." : "Bounding boxes measured."
            };
        }

        private static ModelTreeNode? BuildTreeNodePage(Document document, ModelItem item, int depth, int maxDepth, int offset, int maxNodes, ref int visited, ref int returned, ref bool truncated, List<ModelTreeNode> orphanRoots)
        {
            if (returned >= maxNodes)
            {
                truncated = true;
                return null;
            }

            var includeSelf = visited >= offset;
            visited++;
            ModelTreeNode? node = null;
            if (includeSelf)
            {
                node = new ModelTreeNode
                {
                    ItemRef = ToItemRef(document, item),
                    HasChildren = HasChildren(item)
                };
                returned++;
            }

            if (depth >= maxDepth)
            {
                return node;
            }

            foreach (ModelItem child in item.Children)
            {
                if (returned >= maxNodes)
                {
                    truncated = true;
                    break;
                }

                var childNode = BuildTreeNodePage(document, child, depth + 1, maxDepth, offset, maxNodes, ref visited, ref returned, ref truncated, orphanRoots);
                if (childNode != null)
                {
                    if (node != null)
                    {
                        node.Children.Add(childNode);
                    }
                    else
                    {
                        orphanRoots.Add(childNode);
                    }
                }

                if (truncated)
                {
                    break;
                }
            }

            return node;
        }

        private static IEnumerable<ModelItem> EnumerateModelItems(Document document)
        {
            foreach (Model model in document.Models)
            {
                foreach (var item in EnumerateModelItem(model.RootItem))
                {
                    yield return item;
                }
            }
        }

        private static IEnumerable<ModelItem> EnumerateSearchScope(Document document, string scopeItemId)
        {
            if (!string.IsNullOrWhiteSpace(scopeItemId))
            {
                foreach (var item in EnumerateModelItem(ResolveItem(document, new ItemLookupParams { ItemId = scopeItemId })))
                {
                    yield return item;
                }

                yield break;
            }

            foreach (var item in EnumerateModelItems(document))
            {
                yield return item;
            }
        }

        private static IEnumerable<ModelItem> EnumerateModelItem(ModelItem root)
        {
            yield return root;
            foreach (ModelItem child in root.Children)
            {
                foreach (var item in EnumerateModelItem(child))
                {
                    yield return item;
                }
            }
        }

        private static bool MatchesItem(ModelItem item, string query, bool includeProperties, FindItemsParams parameters)
        {
            var filter = parameters.Filter;
            if (filter != null)
            {
                if (!MatchesOptional(GetModelName(item.Model), filter.SourceModelContains, parameters)) return false;
                if (!MatchesOptional(item.ClassDisplayName, filter.ClassContains, parameters)) return false;
                if (!MatchesOptional(item.ClassDisplayName, filter.CategoryContains, parameters) &&
                    !PropertyValueMatches(item, null, "category", filter.CategoryContains, parameters)) return false;
                if (!PropertyValueMatches(item, filter.PropertyCategoryContains, filter.PropertyNameContains, filter.PropertyValueContains, parameters)) return false;
                if (!PropertyValueMatches(item, null, "material", filter.MaterialContains, parameters)) return false;
                if (!PropertyValueMatches(item, null, "tag", filter.TagContains, parameters)) return false;
            }

            if (ContainsComparable(item.DisplayName, query, parameters.MatchMode, parameters.AccentInsensitive, parameters.CaseSensitive) ||
                ContainsComparable(item.ClassDisplayName, query, parameters.MatchMode, parameters.AccentInsensitive, parameters.CaseSensitive))
            {
                return true;
            }

            if (!includeProperties)
            {
                return false;
            }

            foreach (PropertyCategory category in item.PropertyCategories)
            {
                if (ContainsComparable(category.DisplayName, query, parameters.MatchMode, parameters.AccentInsensitive, parameters.CaseSensitive) ||
                    ContainsComparable(category.Name, query, parameters.MatchMode, parameters.AccentInsensitive, parameters.CaseSensitive))
                {
                    return true;
                }

                foreach (DataProperty property in category.Properties)
                {
                    if (ContainsComparable(property.DisplayName, query, parameters.MatchMode, parameters.AccentInsensitive, parameters.CaseSensitive) ||
                        ContainsComparable(property.Name, query, parameters.MatchMode, parameters.AccentInsensitive, parameters.CaseSensitive) ||
                        ContainsComparable(FormatVariant(property.Value), query, parameters.MatchMode, parameters.AccentInsensitive, parameters.CaseSensitive))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool PropertyValueMatches(ModelItem item, string categoryQuery, string propertyQuery, string valueQuery, FindItemsParams parameters)
        {
            if (string.IsNullOrWhiteSpace(categoryQuery) && string.IsNullOrWhiteSpace(propertyQuery) && string.IsNullOrWhiteSpace(valueQuery))
            {
                return true;
            }

            foreach (PropertyCategory category in item.PropertyCategories)
            {
                if (!MatchesOptional(category.DisplayName, categoryQuery, parameters) && !MatchesOptional(category.Name, categoryQuery, parameters))
                {
                    continue;
                }

                foreach (DataProperty property in category.Properties)
                {
                    if (!MatchesOptional(property.DisplayName, propertyQuery, parameters) && !MatchesOptional(property.Name, propertyQuery, parameters))
                    {
                        continue;
                    }

                    if (MatchesOptional(FormatVariant(property.Value), valueQuery, parameters))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool MatchesOptional(string source, string query, FindItemsParams parameters)
        {
            return string.IsNullOrWhiteSpace(query) ||
                ContainsComparable(source, query, "contains", parameters.AccentInsensitive, parameters.CaseSensitive);
        }

        private static SelectionResult ToSelectionResult(Document document, IEnumerable<ModelItem> items, int maxItems)
        {
            var result = new SelectionResult();
            foreach (var item in items)
            {
                if (result.Items.Count >= maxItems)
                {
                    break;
                }

                result.Items.Add(ToItemRef(document, item));
            }

            result.Count = result.Items.Count;
            return result;
        }

        private static ItemRef ToItemRef(Document document, ModelItem item)
        {
            var path = document.Models.CreateIndexPath(item).ToList();
            return new ItemRef
            {
                Id = FormatScopedId("mi", path),
                IndexPath = path,
                DisplayName = item.DisplayName ?? item.ClassDisplayName ?? "(unnamed)",
                ModelName = GetModelName(item.Model)
            };
        }

        private static ModelItem ResolveItem(Document document, ItemLookupParams parameters)
        {
            var path = parameters.IndexPath;
            if ((path == null || path.Count == 0) && !string.IsNullOrWhiteSpace(parameters.ItemId))
            {
                path = ParseScopedId(parameters.ItemId, "mi");
            }

            if (path == null)
            {
                throw new BridgeRpcException("invalid_params", "itemId or indexPath is required.");
            }

            var item = document.Models.ResolveIndexPath(path);
            if (item == null)
            {
                throw new BridgeRpcException("invalid_item_id", "The model item reference could not be resolved in the active document.");
            }

            return item;
        }

        private static List<ModelItem> ResolveItems(Document document, IList<string> itemIds)
        {
            if (itemIds == null || itemIds.Count == 0)
            {
                throw new BridgeRpcException("invalid_params", "At least one item id is required.");
            }

            var items = new List<ModelItem>();
            foreach (var id in itemIds)
            {
                items.Add(ResolveItem(document, new ItemLookupParams { ItemId = id }));
            }

            return items;
        }

        private static List<ModelItem> ResolveReportItems(Document document, string query, IList<string> itemIds, bool useCurrentSelection, int maxItems, SearchFilter? filter, string scopeItemId)
        {
            maxItems = Clamp(maxItems, 1, 10000);
            if (itemIds != null && itemIds.Count > 0)
            {
                return ResolveItems(document, itemIds).Take(maxItems).ToList();
            }

            if (useCurrentSelection)
            {
                return document.CurrentSelection.SelectedItems.Take(maxItems).ToList();
            }

            var parameters = new FindItemsParams
            {
                Query = string.IsNullOrWhiteSpace(query) ? "*" : query,
                MaxResults = maxItems,
                IncludeProperties = true,
                Filter = filter,
                MatchMode = string.IsNullOrWhiteSpace(query) ? "all" : "contains"
            };

            var result = new List<ModelItem>();
            foreach (var item in EnumerateSearchScope(document, scopeItemId))
            {
                var matches = parameters.MatchMode == "all"
                    ? MatchesItem(item, string.Empty, includeProperties: true, parameters)
                    : MatchesItem(item, parameters.Query, includeProperties: true, parameters);
                if (matches)
                {
                    result.Add(item);
                    if (result.Count >= maxItems)
                    {
                        break;
                    }
                }
            }

            return result;
        }

        private static Dictionary<string, string> ReadPropertyMap(ModelItem item)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (PropertyCategory category in item.PropertyCategories)
            {
                var categoryName = SafeString(() => category.DisplayName) ?? SafeString(() => category.Name) ?? "Property";
                foreach (DataProperty property in category.Properties)
                {
                    var propertyName = SafeString(() => property.DisplayName) ?? SafeString(() => property.Name) ?? "Value";
                    var key = categoryName + "." + propertyName;
                    if (!map.ContainsKey(key))
                    {
                        map.Add(key, FormatVariant(property.Value));
                    }
                }
            }

            return map;
        }

        private static string FindPropertyValue(ModelItem item, string nameContains)
        {
            foreach (PropertyCategory category in item.PropertyCategories)
            {
                foreach (DataProperty property in category.Properties)
                {
                    if (ContainsComparable(property.DisplayName, nameContains, "contains", accentInsensitive: true, caseSensitive: false) ||
                        ContainsComparable(property.Name, nameContains, "contains", accentInsensitive: true, caseSensitive: false))
                    {
                        return FormatVariant(property.Value);
                    }
                }
            }

            return null;
        }

        private static void WriteItemsReport(string filePath, string format, Document document, IEnumerable<ReportRow> rows)
        {
            format = NormalizeReportFormat(format);
            if (format == "json")
            {
                WriteStructuredReport(filePath, format, new { documentTitle = document.Title ?? string.Empty, generatedUtc = DateTimeOffset.UtcNow, rows }, "Items Table");
                return;
            }

            var rowList = rows.ToList();
            if (format == "html")
            {
                var html = new StringBuilder();
                html.AppendLine("<!doctype html><html><head><meta charset=\"utf-8\"><title>Navis Items Table</title></head><body>");
                html.AppendLine("<h1>Navis Items Table</h1>");
                html.AppendLine("<table border=\"1\" cellspacing=\"0\" cellpadding=\"4\"><thead><tr><th>Id</th><th>Name</th><th>Model</th><th>Properties</th></tr></thead><tbody>");
                foreach (var row in rowList)
                {
                    html.Append("<tr><td>").Append(Html(row.Item.Id)).Append("</td><td>").Append(Html(row.Item.DisplayName)).Append("</td><td>").Append(Html(row.Item.ModelName)).Append("</td><td>");
                    foreach (var kv in row.Properties)
                    {
                        html.Append(Html(kv.Key)).Append(": ").Append(Html(kv.Value)).Append("<br>");
                    }
                    html.AppendLine("</td></tr>");
                }
                html.AppendLine("</tbody></table></body></html>");
                File.WriteAllText(filePath, html.ToString(), Encoding.UTF8);
                return;
            }

            var propertyKeys = rowList.SelectMany(row => row.Properties.Keys).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
            var csv = new StringBuilder();
            csv.Append("id,displayName,modelName");
            foreach (var key in propertyKeys)
            {
                csv.Append(',').Append(Csv(key));
            }
            csv.AppendLine();

            foreach (var row in rowList)
            {
                csv.Append(Csv(row.Item.Id)).Append(',').Append(Csv(row.Item.DisplayName)).Append(',').Append(Csv(row.Item.ModelName));
                var properties = row.Properties;
                foreach (var key in propertyKeys)
                {
                    properties.TryGetValue(key, out var value);
                    csv.Append(',').Append(Csv(value));
                }
                csv.AppendLine();
            }

            File.WriteAllText(filePath, csv.ToString(), Encoding.UTF8);
        }

        private static void WriteStructuredReport(string filePath, string format, object payload, string title)
        {
            format = NormalizeReportFormat(format);
            if (format == "html")
            {
                var json = JsonSerializer.Serialize(payload, JsonDefaults.Options);
                File.WriteAllText(filePath, "<!doctype html><html><head><meta charset=\"utf-8\"><title>" + Html(title) + "</title></head><body><h1>" + Html(title) + "</h1><pre>" + Html(json) + "</pre></body></html>", Encoding.UTF8);
            }
            else if (format == "md")
            {
                File.WriteAllText(filePath, "# " + title + Environment.NewLine + Environment.NewLine + "```json" + Environment.NewLine + JsonSerializer.Serialize(payload, JsonDefaults.Options) + Environment.NewLine + "```" + Environment.NewLine, Encoding.UTF8);
            }
            else
            {
                File.WriteAllText(filePath, JsonSerializer.Serialize(payload, JsonDefaults.Options), Encoding.UTF8);
            }
        }

        private static ReportResult BuildReportResult(Document document, string filePath, string relativePath, string format, int itemCount, string message)
        {
            return new ReportResult
            {
                Written = File.Exists(filePath),
                FilePath = filePath,
                RelativePath = relativePath ?? string.Empty,
                Format = NormalizeReportFormat(format),
                Length = File.Exists(filePath) ? new FileInfo(filePath).Length : (long?)null,
                DocumentTitle = document.Title ?? string.Empty,
                ItemCount = itemCount,
                GeneratedUtc = DateTimeOffset.UtcNow,
                Message = message
            };
        }

        private static void ReplaceSavedItem(Autodesk.Navisworks.Api.DocumentParts.DocumentSavedViewpoints part, IList<int> path, SavedItem item)
        {
            if (path == null || path.Count == 0)
            {
                throw new BridgeRpcException("invalid_params", "Cannot replace root saved item.");
            }

            part.ReplaceWithCopy(path[path.Count - 1], item);
        }

        private static SavedItemInfo ToSavedItemInfo(Autodesk.Navisworks.Api.DocumentParts.DocumentSelectionSets part, SavedItem item, string prefix)
        {
            var path = part.CreateIndexPath(item).ToList();
            var group = item as GroupItem;
            var selectionSet = item as SelectionSet;
            var kind = selectionSet != null
                ? (selectionSet.HasSearch ? "search_set" : "selection_set")
                : group != null ? "folder" : item.GetType().Name;
            return new SavedItemInfo
            {
                Id = prefix + ":" + string.Join(".", path),
                IndexPath = path,
                DisplayName = item.DisplayName ?? "(unnamed)",
                Kind = kind,
                ChildCount = group == null ? 0 : group.Children.Count
            };
        }

        private static ViewpointInfo ToViewpointInfo(Document document, SavedItem item)
        {
            var path = document.SavedViewpoints.CreateIndexPath(item).ToList();
            var group = item as GroupItem;
            return new ViewpointInfo
            {
                Id = "vp:" + string.Join(".", path),
                IndexPath = path,
                DisplayName = item.DisplayName ?? "(unnamed)",
                Kind = item is SavedViewpoint ? "viewpoint" : group != null ? "folder" : item.GetType().Name
            };
        }

        private static IEnumerable<SavedItem> FlattenSavedItems(SavedItemCollection items)
        {
            foreach (SavedItem item in items)
            {
                yield return item;
                var group = item as GroupItem;
                if (group == null)
                {
                    continue;
                }

                foreach (var child in FlattenSavedItems(group.Children))
                {
                    yield return child;
                }
            }
        }

        private static List<int> ParseScopedId(string id, string expectedPrefix)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new BridgeRpcException("invalid_params", expectedPrefix + " id is required.");
            }

            var value = id.Trim();
            var rootId = expectedPrefix + ":root";
            if (string.Equals(value, rootId, StringComparison.OrdinalIgnoreCase))
            {
                return new List<int>();
            }

            var prefix = expectedPrefix + ":";
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(prefix.Length);
            }

            if (value.Length == 0)
            {
                return new List<int>();
            }

            var path = new List<int>();
            foreach (var part in value.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!int.TryParse(part, out var index))
                {
                    throw new BridgeRpcException("invalid_params", "Invalid scoped id '" + id + "'.");
                }

                path.Add(index);
            }

            if (path.Count == 0)
            {
                throw new BridgeRpcException("invalid_params", "Invalid scoped id '" + id + "'.");
            }

            return path;
        }

        private static string FormatScopedId(string prefix, IList<int> path)
        {
            return path == null || path.Count == 0 ? prefix + ":root" : prefix + ":" + string.Join(".", path);
        }

        private static bool HasChildren(ModelItem item)
        {
            foreach (ModelItem _ in item.Children)
            {
                return true;
            }

            return false;
        }

        private static string GetDocumentUnits(Document document)
        {
            try
            {
                var first = document.Models.First;
                return first == null ? null : first.Units.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static string GetModelName(Model model)
        {
            if (model == null)
            {
                return null;
            }

            var name = EmptyAsNull(model.FileName) ?? EmptyAsNull(model.SourceFileName);
            return name == null ? null : Path.GetFileName(name);
        }

        private static string FormatVariant(VariantData value)
        {
            if (value == null)
            {
                return null;
            }

            try
            {
                return value.ToDisplayString();
            }
            catch
            {
                return value.ToString();
            }
        }

        private static int ParseCursor(string cursor, int offset)
        {
            if (!string.IsNullOrWhiteSpace(cursor) && int.TryParse(cursor, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return Math.Max(0, parsed);
            }

            return Math.Max(0, offset);
        }

        private static bool Contains(string source, string query)
        {
            return !string.IsNullOrEmpty(source) && source.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ContainsComparable(string source, string query, string matchMode, bool accentInsensitive, bool caseSensitive)
        {
            if (string.IsNullOrEmpty(query) || query == "*")
            {
                return true;
            }

            if (string.IsNullOrEmpty(source))
            {
                return false;
            }

            var left = accentInsensitive ? RemoveDiacritics(source) : source;
            var right = accentInsensitive ? RemoveDiacritics(query) : query;
            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            return string.Equals(matchMode, "equals", StringComparison.OrdinalIgnoreCase)
                ? string.Equals(left, right, comparison)
                : left.IndexOf(right, comparison) >= 0;
        }

        private static string RemoveDiacritics(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(ch);
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC)
                .Replace('\u0111', 'd')
                .Replace('\u0110', 'D')
                .Replace('đ', 'd')
                .Replace('Đ', 'D');
        }

        private static string NormalizeReportFormat(string format)
        {
            var value = (format ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant();
            if (value.Length == 0)
            {
                value = "json";
            }

            if (value == "htm")
            {
                value = "html";
            }

            return value;
        }

        private static string Csv(string value)
        {
            value = value ?? string.Empty;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string Html(string value)
        {
            value = value ?? string.Empty;
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
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

        private static string EmptyAsNull(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        private sealed class ReportRow
        {
            public ItemRef Item { get; set; }
            public Dictionary<string, string> Properties { get; set; }
        }
    }
}
