using System;
using System.Collections.Generic;
using System.Globalization;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Clash;
using NavisMcp.Contracts;
using NavisMcp.Plugin.Navis2026.Bridge;

namespace NavisMcp.Plugin.Navis2026.Services
{
    internal sealed class NavisClashService
    {
        public ClashTestListResult ListTests(Document document)
        {
            var testsData = GetTestsData(document);
            var result = new ClashTestListResult();
            foreach (var test in EnumerateTests(testsData.Tests))
            {
                result.Tests.Add(ToTestInfo(test));
            }

            return result;
        }

        public ClashResultsResult GetResults(Document document, ClashTestLookupParams parameters)
        {
            var test = ResolveTest(document, parameters.TestId);
            return ToResults(document, test, parameters.MaxResults, ParseCursor(parameters.Cursor, parameters.Offset));
        }

        public ClashResultsResult RunTest(Document document, ClashTestLookupParams parameters)
        {
            var testsData = GetTestsData(document);
            var test = ResolveTest(testsData, parameters.TestId);
            testsData.TestsRunTest(test);
            return ToResults(document, test, parameters.MaxResults, ParseCursor(parameters.Cursor, parameters.Offset));
        }

        public ClashTestInfo CreateTest(Document document, ClashTestDefinition parameters)
        {
            if (parameters == null)
            {
                throw new BridgeRpcException("invalid_params", "Clash test definition is required.");
            }

            var displayName = (parameters.DisplayName ?? string.Empty).Trim();
            if (displayName.Length == 0)
            {
                throw new BridgeRpcException("invalid_params", "displayName is required.");
            }

            var testsData = GetTestsData(document);
            var selectionA = ResolveClashSelection(document, parameters.SelectionA, "selectionA");
            var selectionB = ResolveClashSelection(document, parameters.SelectionB, "selectionB");
            var test = new ClashTest
            {
                DisplayName = displayName,
                CustomTestName = displayName,
                Guid = Guid.NewGuid(),
                TestType = ParseClashType(parameters.ClashType),
                Tolerance = Math.Max(0, parameters.Tolerance),
                MergeComposites = parameters.CompositeObjectClashing
            };

            test.SelectionA.Selection.CopyFrom(selectionA);
            test.SelectionB.Selection.CopyFrom(selectionB);
            testsData.TestsAddCopy(test);

            var added = EnumerateTests(testsData.Tests)
                .FirstOrDefault(x => x.Guid == test.Guid)
                ?? EnumerateTests(testsData.Tests).LastOrDefault(x => string.Equals(x.DisplayName, displayName, StringComparison.Ordinal));

            return ToTestInfo(added ?? test);
        }

        public ClashResultInfo UpdateResult(Document document, ClashResultUpdate parameters)
        {
            if (parameters == null)
            {
                throw new BridgeRpcException("invalid_params", "Clash result update parameters are required.");
            }

            var testsData = GetTestsData(document);
            var test = ResolveTest(testsData, parameters.TestId);
            var clashResult = ResolveResult(test, parameters.ResultId);
            var changed = false;

            if (!string.IsNullOrWhiteSpace(parameters.Status))
            {
                ClashResultStatus status;
                if (!TryParseClashResultStatus(parameters.Status, out status))
                {
                    throw new BridgeRpcException(
                        "invalid_params",
                        "status must be one of: New, Active, Reviewed, Approved, Resolved.");
                }

                var assignee = clashResult.AssignedTo ?? new Assignee();
                testsData.TestsEditResultStatus(clashResult, status, assignee);
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(parameters.AssignedTo))
            {
                testsData.TestsEditResultAssignedTo(clashResult, new Assignee(parameters.AssignedTo.Trim()));
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(parameters.Comment))
            {
                var comments = new CommentCollection();
                comments.CopyFrom(clashResult.Comments);
                comments.Add(new Comment(parameters.Comment.Trim(), CommentStatus.New));
                testsData.TestsEditResultComments(clashResult, comments);
                changed = true;
            }

            if (!changed)
            {
                throw new BridgeRpcException("invalid_params", "Provide at least one of status, comment, or assignedTo.");
            }

            // Re-resolve after edit APIs (handles copy-on-write semantics).
            clashResult = ResolveResult(ResolveTest(testsData, parameters.TestId), parameters.ResultId);
            return ToResultInfo(document, clashResult);
        }

        public ReportResult ExportReport(Document document, ClashReportExportParams parameters)
        {
            var testsData = GetTestsData(document);
            var test = ResolveTest(testsData, parameters.TestId);
            var results = BuildExportResults(document, testsData, test, parameters);
            var format = NormalizeReportFormat(parameters.Format);
            if (format == "html")
            {
                WriteHtmlReport(parameters.FilePath, document, test, results.Results);
            }
            else if (format == "csv")
            {
                WriteCsvReport(parameters.FilePath, results.Results);
            }
            else
            {
                var payload = new
                {
                    documentTitle = document.Title ?? string.Empty,
                    documentPath = document.CurrentFileName,
                    generatedUtc = DateTimeOffset.UtcNow,
                    testId = results.TestId,
                    testName = test.DisplayName ?? "(unnamed)",
                    results = results.Results
                };
                File.WriteAllText(parameters.FilePath, JsonSerializer.Serialize(payload, JsonDefaults.Options), Encoding.UTF8);
            }

            return new ReportResult
            {
                Written = File.Exists(parameters.FilePath),
                FilePath = parameters.FilePath,
                RelativePath = parameters.RelativePath ?? string.Empty,
                Format = format,
                Length = File.Exists(parameters.FilePath) ? new FileInfo(parameters.FilePath).Length : (long?)null,
                DocumentTitle = document.Title ?? string.Empty,
                ItemCount = results.Results.Count,
                GeneratedUtc = DateTimeOffset.UtcNow,
                Message = "Clash report exported."
            };
        }

        private static DocumentClashTests GetTestsData(Document document)
        {
            var clash = DocumentClash.ClashInstance(document);
            if (clash == null || clash.TestsData == null)
            {
                throw new BridgeRpcException("not_found", "Clash Detective data is not available in this Navisworks session.");
            }

            return clash.TestsData;
        }

        private static ClashTest ResolveTest(Document document, string testId)
        {
            return ResolveTest(GetTestsData(document), testId);
        }

        private static ClashTest ResolveTest(DocumentClashTests testsData, string testId)
        {
            if (string.IsNullOrWhiteSpace(testId))
            {
                throw new BridgeRpcException("invalid_params", "testId is required.");
            }

            SavedItem item = null;
            Guid guid;
            if (Guid.TryParse(testId, out guid))
            {
                item = testsData.ResolveGuid(guid);
            }

            if (item == null)
            {
                item = EnumerateTests(testsData.Tests)
                    .FirstOrDefault(x => string.Equals(x.DisplayName, testId, StringComparison.OrdinalIgnoreCase));
            }

            var test = item as ClashTest;
            if (test == null)
            {
                throw new BridgeRpcException("not_found", "Clash test '" + testId + "' was not found.");
            }

            return test;
        }

        private static ModelItemCollection ResolveClashSelection(Document document, string selectionSpec, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(selectionSpec))
            {
                throw new BridgeRpcException("invalid_params", parameterName + " is required. Use a saved selection set id/name or comma-separated model item ids.");
            }

            var value = selectionSpec.Trim();
            if (string.Equals(value, "current", StringComparison.OrdinalIgnoreCase))
            {
                var selected = document.CurrentSelection.SelectedItems;
                if (selected.Count == 0)
                {
                    throw new BridgeRpcException("invalid_params", parameterName + " requested current selection, but nothing is selected.");
                }

                return selected;
            }

            var set = ResolveSelectionSet(document, value);
            if (set != null)
            {
                var selected = set.GetSelectedItems(document);
                if (selected.Count == 0)
                {
                    throw new BridgeRpcException("invalid_params", "Selection set '" + value + "' is empty.");
                }

                return selected;
            }

            var itemIds = value.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToList();
            if (itemIds.Count == 0 && value.StartsWith("mi:", StringComparison.OrdinalIgnoreCase))
            {
                itemIds.Add(value);
            }

            if (itemIds.Count == 0 || itemIds.Any(x => !x.StartsWith("mi:", StringComparison.OrdinalIgnoreCase)))
            {
                throw new BridgeRpcException("not_found", parameterName + " '" + value + "' was not found as a selection set and is not a model item id list.");
            }

            var collection = new ModelItemCollection();
            foreach (var itemId in itemIds)
            {
                collection.Add(ResolveModelItem(document, itemId));
            }

            return collection;
        }

        private static SelectionSet ResolveSelectionSet(Document document, string value)
        {
            if (value.StartsWith("ss:", StringComparison.OrdinalIgnoreCase))
            {
                var item = document.SelectionSets.ResolveIndexPath(ParseScopedId(value, "ss"));
                return item as SelectionSet;
            }

            return FlattenSavedItems(document.SelectionSets.Value)
                .OfType<SelectionSet>()
                .FirstOrDefault(x => string.Equals(x.DisplayName, value, StringComparison.OrdinalIgnoreCase));
        }

        private static ModelItem ResolveModelItem(Document document, string itemId)
        {
            var item = document.Models.ResolveIndexPath(ParseScopedId(itemId, "mi"));
            if (item == null)
            {
                throw new BridgeRpcException("invalid_item_id", "The model item reference '" + itemId + "' could not be resolved in the active document.");
            }

            return item;
        }

        private static List<int> ParseScopedId(string id, string expectedPrefix)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new BridgeRpcException("invalid_params", expectedPrefix + " id is required.");
            }

            var value = id.Trim();
            var prefix = expectedPrefix + ":";
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(prefix.Length);
            }

            var path = new List<int>();
            foreach (var part in value.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int index;
                if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out index))
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

        private static ClashTestType ParseClashType(string clashType)
        {
            var value = (clashType ?? string.Empty).Trim().Replace("-", string.Empty).Replace("_", string.Empty);
            if (value.Length == 0 || string.Equals(value, "hard", StringComparison.OrdinalIgnoreCase))
            {
                return ClashTestType.Hard;
            }

            if (string.Equals(value, "hardconservative", StringComparison.OrdinalIgnoreCase))
            {
                return ClashTestType.HardConservative;
            }

            if (string.Equals(value, "clearance", StringComparison.OrdinalIgnoreCase))
            {
                return ClashTestType.Clearance;
            }

            if (string.Equals(value, "duplicate", StringComparison.OrdinalIgnoreCase))
            {
                return ClashTestType.Duplicate;
            }

            throw new BridgeRpcException("invalid_params", "clashType must be hard, hard_conservative, clearance, or duplicate.");
        }

        private static ClashTestInfo ToTestInfo(ClashTest test)
        {
            return new ClashTestInfo
            {
                Id = test.Guid.ToString("N"),
                DisplayName = test.DisplayName ?? "(unnamed)",
                Status = test.Status.ToString(),
                ResultCount = EnumerateResults(test.Children).Count()
            };
        }

        private static ClashResultsResult ToResults(Document document, ClashTest test, int maxResults)
        {
            return ToResults(document, test, maxResults, 0);
        }

        private static ClashResultsResult ToResults(Document document, ClashTest test, int maxResults, int offset)
        {
            maxResults = Clamp(maxResults, 1, 5000);
            offset = Math.Max(0, offset);
            var result = new ClashResultsResult { TestId = test.Guid.ToString("N") };
            var matched = 0;
            foreach (var clashResult in EnumerateResults(test.Children))
            {
                if (matched++ < offset)
                {
                    continue;
                }

                if (result.Results.Count >= maxResults)
                {
                    result.Truncated = true;
                    result.NextCursor = matched.ToString(CultureInfo.InvariantCulture);
                    break;
                }

                result.Results.Add(ToResultInfo(document, clashResult));
            }

            return result;
        }

        private static ClashResultsResult BuildExportResults(Document document, DocumentClashTests testsData, ClashTest test, ClashReportExportParams parameters)
        {
            var maxResults = Clamp(parameters.MaxResults, 1, 5000);
            var offset = Math.Max(0, parameters.Offset);
            var result = new ClashResultsResult { TestId = test.Guid.ToString("N") };
            var rawResults = EnumerateResults(test.Children).Skip(offset).Take(maxResults + 1).ToList();
            result.Truncated = rawResults.Count > maxResults;
            if (result.Truncated)
            {
                rawResults = rawResults.Take(maxResults).ToList();
                result.NextCursor = (offset + maxResults).ToString(CultureInfo.InvariantCulture);
            }

            for (var i = 0; i < rawResults.Count; i++)
            {
                var info = ToResultInfo(document, rawResults[i]);
                if (parameters.IncludeImages)
                {
                    TryAttachImage(testsData, rawResults[i], info, parameters, offset + i + 1);
                }

                result.Results.Add(info);
            }

            return result;
        }

        private static void TryAttachImage(DocumentClashTests testsData, IClashResult clashResult, ClashResultInfo info, ClashReportExportParams parameters, int sequence)
        {
            try
            {
                var reportDirectory = Path.GetDirectoryName(parameters.FilePath);
                if (string.IsNullOrWhiteSpace(reportDirectory))
                {
                    return;
                }

                var imageFolderName = Path.GetFileNameWithoutExtension(parameters.FilePath) + "_images";
                var imageFolder = Path.Combine(reportDirectory, imageFolderName);
                Directory.CreateDirectory(imageFolder);

                var imageFileName = "clash-" + sequence.ToString("0000", CultureInfo.InvariantCulture) + ".png";
                var imagePath = Path.Combine(imageFolder, imageFileName);
                var width = Clamp(parameters.ImageWidth, 64, 1600);
                var height = Clamp(parameters.ImageHeight, 64, 1200);
                using (var bitmap = testsData.TestsImageForResult(clashResult, ImageGenerationStyle.ScenePlusOverlay, width, height))
                {
                    bitmap.Save(imagePath, ImageFormat.Png);
                }

                info.ImagePath = imagePath;
                info.ImageRelativePath = Path.Combine(imageFolderName, imageFileName);
            }
            catch
            {
                info.ImagePath = string.Empty;
                info.ImageRelativePath = string.Empty;
            }
        }

        private static void WriteHtmlReport(string filePath, Document document, ClashTest test, IEnumerable<ClashResultInfo> results)
        {
            var html = new StringBuilder();
            html.AppendLine("<!doctype html><html><head><meta charset=\"utf-8\"><title>Navis Clash Report</title><style>body{font-family:Arial,sans-serif;font-size:12px}table{border-collapse:collapse;width:100%}th,td{border:1px solid #333;padding:4px;vertical-align:top}th{background:#f2f2f2}img{max-width:220px;height:auto}</style></head><body>");
            html.Append("<h1>").Append(Html(test.DisplayName ?? "Clash Report")).AppendLine("</h1>");
            html.Append("<p>Document: ").Append(Html(document.Title ?? string.Empty)).Append("</p>");
            html.AppendLine("<table><thead><tr><th rowspan=\"2\">Image</th><th rowspan=\"2\">Clash Name</th><th rowspan=\"2\">Status</th><th rowspan=\"2\">Distance</th><th rowspan=\"2\">Description</th><th rowspan=\"2\">Clash Point</th><th colspan=\"4\">Item 1</th><th colspan=\"4\">Item 2</th></tr>");
            html.AppendLine("<tr><th>Item ID</th><th>Layer</th><th>Item Name</th><th>Item Type</th><th>Item ID</th><th>Layer</th><th>Item Name</th><th>Item Type</th></tr></thead><tbody>");
            foreach (var result in results)
            {
                html.Append("<tr><td>");
                if (!string.IsNullOrWhiteSpace(result.ImageRelativePath))
                {
                    html.Append("<img src=\"").Append(Html(result.ImageRelativePath.Replace("\\", "/"))).Append("\" alt=\"clash image\">");
                }

                html.Append("</td><td>").Append(Html(result.DisplayName)).Append("</td><td>").Append(Html(result.Status)).Append("</td><td>")
                    .Append(Html(result.Distance)).Append("</td><td>").Append(Html(result.Description)).Append("</td><td>").Append(Html(result.ClashPoint)).Append("</td><td>")
                    .Append(Html(result.Item1Detail?.ItemId)).Append("</td><td>").Append(Html(result.Item1Detail?.Layer)).Append("</td><td>")
                    .Append(Html(result.Item1Detail?.ItemName)).Append("</td><td>").Append(Html(result.Item1Detail?.ItemType)).Append("</td><td>")
                    .Append(Html(result.Item2Detail?.ItemId)).Append("</td><td>").Append(Html(result.Item2Detail?.Layer)).Append("</td><td>")
                    .Append(Html(result.Item2Detail?.ItemName)).Append("</td><td>").Append(Html(result.Item2Detail?.ItemType)).AppendLine("</td></tr>");
            }
            html.AppendLine("</tbody></table></body></html>");
            File.WriteAllText(filePath, html.ToString(), Encoding.UTF8);
        }

        private static void WriteCsvReport(string filePath, IEnumerable<ClashResultInfo> results)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Image,Clash Name,Status,Distance,Description,Clash Point,Item 1 Item ID,Item 1 Layer,Item 1 Item Name,Item 1 Item Type,Item 2 Item ID,Item 2 Layer,Item 2 Item Name,Item 2 Item Type");
            foreach (var result in results)
            {
                csv.Append(Csv(result.ImageRelativePath)).Append(',').Append(Csv(result.DisplayName)).Append(',').Append(Csv(result.Status)).Append(',')
                    .Append(Csv(result.Distance)).Append(',').Append(Csv(result.Description)).Append(',').Append(Csv(result.ClashPoint)).Append(',')
                    .Append(Csv(result.Item1Detail?.ItemId)).Append(',').Append(Csv(result.Item1Detail?.Layer)).Append(',')
                    .Append(Csv(result.Item1Detail?.ItemName)).Append(',').Append(Csv(result.Item1Detail?.ItemType)).Append(',')
                    .Append(Csv(result.Item2Detail?.ItemId)).Append(',').Append(Csv(result.Item2Detail?.Layer)).Append(',')
                    .Append(Csv(result.Item2Detail?.ItemName)).Append(',').Append(Csv(result.Item2Detail?.ItemType)).AppendLine();
            }
            File.WriteAllText(filePath, csv.ToString(), Encoding.UTF8);
        }

        private static string NormalizeReportFormat(string format)
        {
            var value = (format ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant();
            if (value.Length == 0)
            {
                value = "html";
            }

            return value == "htm" ? "html" : value;
        }

        private static int ParseCursor(string cursor, int offset)
        {
            if (!string.IsNullOrWhiteSpace(cursor) && int.TryParse(cursor, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return Math.Max(0, parsed);
            }

            return Math.Max(0, offset);
        }

        private static string Csv(string value)
        {
            value = value ?? string.Empty;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string Html(string value)
        {
            value = value ?? string.Empty;
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private static ClashResultInfo ToResultInfo(Document document, IClashResult result)
        {
            var item1 = FirstItem(result.Selection1);
            var item2 = FirstItem(result.Selection2);
            return new ClashResultInfo
            {
                Id = GetGuid(result),
                DisplayName = result.DisplayName ?? "(unnamed)",
                Status = result.Status.ToString(),
                Item1 = item1 == null ? null : ToItemRef(document, item1),
                Item2 = item2 == null ? null : ToItemRef(document, item2),
                Item1Detail = item1 == null ? null : ToItemDetail(document, item1),
                Item2Detail = item2 == null ? null : ToItemDetail(document, item2),
                Distance = result.Distance.ToString("0.###", CultureInfo.InvariantCulture),
                Description = string.IsNullOrWhiteSpace(result.Description) ? result.TestType.ToString() : result.Description,
                ClashPoint = FormatPoint(result.Center)
            };
        }

        private static IEnumerable<ClashTest> EnumerateTests(SavedItemCollection items)
        {
            foreach (SavedItem item in items)
            {
                var test = item as ClashTest;
                if (test != null)
                {
                    yield return test;
                }

                var group = item as GroupItem;
                if (group == null)
                {
                    continue;
                }

                foreach (var child in EnumerateTests(group.Children))
                {
                    yield return child;
                }
            }
        }

        private static IEnumerable<IClashResult> EnumerateResults(SavedItemCollection items)
        {
            foreach (SavedItem item in items)
            {
                var result = item as IClashResult;
                if (result != null)
                {
                    yield return result;
                }

                var group = item as ClashResultGroup;
                if (group == null)
                {
                    continue;
                }

                foreach (var child in EnumerateResults(group.Children))
                {
                    yield return child;
                }
            }
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

        private static ModelItem FirstItem(ModelItemCollection items)
        {
            if (items == null || items.Count == 0)
            {
                return null;
            }

            foreach (ModelItem item in items)
            {
                return item;
            }

            return null;
        }

        private static ClashItemDetail ToItemDetail(Document document, ModelItem item)
        {
            var itemRef = ToItemRef(document, item);
            var elementId = FindPropertyValue(item, "Element ID", "ElementId", "Entity Handle", "IfcGUID", "GUID", "UniqueId");
            var layer = FindPropertyValue(item, "Layer");
            var itemName = FindPropertyValue(item, "Item Name", "Name");
            var itemType = FindPropertyValue(item, "Item Type", "Type Name", "Type", "Class");

            return new ClashItemDetail
            {
                ItemId = string.IsNullOrWhiteSpace(elementId) ? itemRef.Id : elementId,
                Layer = layer ?? string.Empty,
                ItemName = string.IsNullOrWhiteSpace(itemName) ? itemRef.DisplayName : itemName,
                ItemType = string.IsNullOrWhiteSpace(itemType) ? (SafeString(() => item.ClassDisplayName) ?? string.Empty) : itemType
            };
        }

        private static string FindPropertyValue(ModelItem item, params string[] nameContains)
        {
            IEnumerable<PropertyCategory> categories;
            try
            {
                categories = item.PropertyCategories.Cast<PropertyCategory>().ToList();
            }
            catch
            {
                return string.Empty;
            }

            foreach (PropertyCategory category in categories)
            {
                IEnumerable<DataProperty> properties;
                try
                {
                    properties = category.Properties.Cast<DataProperty>().ToList();
                }
                catch
                {
                    continue;
                }

                foreach (DataProperty property in properties)
                {
                    var propertyName = (SafeString(() => property.DisplayName) ?? string.Empty) + " " + (SafeString(() => property.Name) ?? string.Empty);
                    foreach (var name in nameContains)
                    {
                        if (Contains(propertyName, name))
                        {
                            var value = FormatVariant(property.Value);
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                return value;
                            }
                        }
                    }
                }
            }

            return string.Empty;
        }

        private static string FormatPoint(Point3D point)
        {
            if (point == null)
            {
                return string.Empty;
            }

            return "x:" + point.X.ToString("0.###", CultureInfo.InvariantCulture) +
                ", y:" + point.Y.ToString("0.###", CultureInfo.InvariantCulture) +
                ", z:" + point.Z.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static ItemRef ToItemRef(Document document, ModelItem item)
        {
            var path = new List<int>();
            var id = "mi:unresolved";
            try
            {
                path = document.Models.CreateIndexPath(item).ToList();
                id = path.Count == 0 ? "mi:root" : "mi:" + string.Join(".", path);
            }
            catch
            {
                var elementId = FindPropertyValue(item, "Element ID", "ElementId", "Entity Handle", "IfcGUID", "GUID", "UniqueId");
                if (!string.IsNullOrWhiteSpace(elementId))
                {
                    id = elementId;
                }
            }

            return new ItemRef
            {
                Id = id,
                IndexPath = path,
                DisplayName = SafeString(() => item.DisplayName) ?? SafeString(() => item.ClassDisplayName) ?? "(unnamed)",
                ModelName = SafeModelName(item)
            };
        }

        private static string FormatVariant(VariantData value)
        {
            if (value == null)
            {
                return string.Empty;
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

        private static bool Contains(string source, string query)
        {
            return !string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(query) && source.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static IClashResult ResolveResult(ClashTest test, string resultId)
        {
            if (string.IsNullOrWhiteSpace(resultId))
            {
                throw new BridgeRpcException("invalid_params", "resultId is required.");
            }

            var needle = resultId.Trim();
            Guid guid;
            var hasGuid = Guid.TryParse(needle, out guid);
            foreach (var result in EnumerateResults(test.Children))
            {
                if (hasGuid)
                {
                    var saved = result as SavedItem;
                    if (saved != null && saved.Guid == guid)
                    {
                        return result;
                    }
                }

                if (string.Equals(GetGuid(result), needle, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(result.DisplayName, needle, StringComparison.OrdinalIgnoreCase))
                {
                    return result;
                }
            }

            throw new BridgeRpcException("not_found", "Clash result '" + resultId + "' was not found in test '" + (test.DisplayName ?? test.Guid.ToString("N")) + "'.");
        }

        private static bool TryParseClashResultStatus(string value, out ClashResultStatus status)
        {
            status = ClashResultStatus.New;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var normalized = value.Trim();
            return Enum.TryParse(normalized, true, out status);
        }

        private static string GetGuid(IClashResult result)
        {
            var savedItem = result as SavedItem;
            if (savedItem == null)
            {
                return Guid.NewGuid().ToString("N");
            }

            try
            {
                return savedItem.Guid.ToString("N");
            }
            catch
            {
                return Guid.NewGuid().ToString("N");
            }
        }

        private static string SafeModelName(ModelItem item)
        {
            try
            {
                if (item.Model == null)
                {
                    return null;
                }

                return System.IO.Path.GetFileName(item.Model.FileName ?? item.Model.SourceFileName);
            }
            catch
            {
                return null;
            }
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
    }
}
