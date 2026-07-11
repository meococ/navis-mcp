using System;
using System.Collections.Generic;
using System.Text.Json;

namespace NavisMcp.Contracts
{
    public static class NavisMcpDefaults
    {
        public const string AppName = "NavisMcp";
        public const string SessionFolderName = "sessions";
        public const string LogFolderName = "logs";
        public const string PipePrefix = "NavisMcp.Navis2026.";
        public const int DefaultTimeoutMs = 30000;
        public const int LongTimeoutMs = 300000;
        public const string SessionTokenFileSuffix = ".token";
    }

    public sealed class NavisRpcRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Method { get; set; } = string.Empty;
        public string? TargetId { get; set; }
        public int? TimeoutMs { get; set; }
        public RequestContext? Context { get; set; }
        public JsonElement Params { get; set; }
    }

    public sealed class RequestContext
    {
        public string AuthToken { get; set; } = string.Empty;
        public bool AllowWrites { get; set; }
        public string ExportsRoot { get; set; } = string.Empty;
        public string SnapshotsRoot { get; set; } = string.Empty;
        public string ReportsRoot { get; set; } = string.Empty;
        public string ModelInputRoot { get; set; } = string.Empty;
    }

    public sealed class NavisRpcResponse
    {
        public string Id { get; set; } = string.Empty;
        public bool Ok { get; set; }
        public object? Data { get; set; }
        public RpcError? Error { get; set; }
        public long ElapsedMs { get; set; }

        public static NavisRpcResponse Success(string id, object? data, long elapsedMs)
        {
            return new NavisRpcResponse
            {
                Id = id,
                Ok = true,
                Data = data,
                ElapsedMs = elapsedMs
            };
        }

        public static NavisRpcResponse Failure(string id, string code, string message, long elapsedMs, object? details = null)
        {
            return new NavisRpcResponse
            {
                Id = id,
                Ok = false,
                Error = new RpcError { Code = code, Message = message, Details = details },
                ElapsedMs = elapsedMs
            };
        }
    }

    public sealed class RpcError
    {
        public string Code { get; set; } = "error";
        public string Message { get; set; } = string.Empty;
        public object? Details { get; set; }
    }

    public sealed class SessionDescriptor
    {
        public string TargetId { get; set; } = string.Empty;
        public string PipeName { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = "Navisworks";
        public string NavisworksVersion { get; set; } = string.Empty;
        public string PluginVersion { get; set; } = string.Empty;
        public string MachineName { get; set; } = Environment.MachineName;
        public string UserName { get; set; } = Environment.UserName;
        public string? DocumentTitle { get; set; }
        public string? DocumentPath { get; set; }
        public string? AuthToken { get; set; }
        public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset LastSeenUtc { get; set; } = DateTimeOffset.UtcNow;
    }

    public sealed class TargetListResult
    {
        public List<SessionDescriptor> Targets { get; set; } = new List<SessionDescriptor>();
    }

    public sealed class HealthResult
    {
        public string Status { get; set; } = "ok";
        public SessionDescriptor? Target { get; set; }
        public DocumentInfo? Document { get; set; }
        public bool WritesEnabled { get; set; }
        public string? ServerVersion { get; set; }
        public string? PluginVersion { get; set; }
        public bool VersionMismatch { get; set; }
        public string? VersionNote { get; set; }
    }

    public sealed class DocumentInfo
    {
        public string Title { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public int ModelCount { get; set; }
        public int CurrentSelectionCount { get; set; }
        public string? Units { get; set; }
    }

    public sealed class ModelInfo
    {
        public string DisplayName { get; set; } = string.Empty;
        public string? SourceFileName { get; set; }
        public string? Guid { get; set; }
        public ItemRef? RootItem { get; set; }
    }

    public sealed class ModelTreeResult
    {
        public List<ModelTreeNode> Roots { get; set; } = new List<ModelTreeNode>();
        public int ReturnedNodeCount { get; set; }
        public bool Truncated { get; set; }
        public string? NextCursor { get; set; }
    }

    public sealed class ModelTreeNode
    {
        public ItemRef ItemRef { get; set; } = new ItemRef();
        public bool HasChildren { get; set; }
        public List<ModelTreeNode> Children { get; set; } = new List<ModelTreeNode>();
    }

    public sealed class ItemRef
    {
        public string Id { get; set; } = string.Empty;
        public List<int> IndexPath { get; set; } = new List<int>();
        public string DisplayName { get; set; } = string.Empty;
        public string? ModelName { get; set; }
    }

    public sealed class ItemSearchResult
    {
        public List<ItemRef> Items { get; set; } = new List<ItemRef>();
        public int ReturnedCount { get; set; }
        public bool Truncated { get; set; }
        public string? NextCursor { get; set; }
    }

    public sealed class ItemPropertiesResult
    {
        public ItemRef ItemRef { get; set; } = new ItemRef();
        public List<PropertyCategoryDto> Categories { get; set; } = new List<PropertyCategoryDto>();
    }

    public sealed class PropertyCategoryDto
    {
        public string DisplayName { get; set; } = string.Empty;
        public string? Name { get; set; }
        public List<PropertyDto> Properties { get; set; } = new List<PropertyDto>();
    }

    public sealed class PropertyDto
    {
        public string DisplayName { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Value { get; set; }
        public string? Type { get; set; }
    }

    public sealed class SelectionResult
    {
        public List<ItemRef> Items { get; set; } = new List<ItemRef>();
        public int Count { get; set; }
    }

    public sealed class SavedItemInfo
    {
        public string Id { get; set; } = string.Empty;
        public List<int> IndexPath { get; set; } = new List<int>();
        public string DisplayName { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public int ChildCount { get; set; }
    }

    public sealed class SavedItemListResult
    {
        public List<SavedItemInfo> Items { get; set; } = new List<SavedItemInfo>();
    }

    public sealed class ViewpointInfo
    {
        public string? Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public List<int> IndexPath { get; set; } = new List<int>();
        public string Kind { get; set; } = "viewpoint";
    }

    public sealed class ViewpointListResult
    {
        public List<ViewpointInfo> Items { get; set; } = new List<ViewpointInfo>();
    }

    public sealed class CurrentViewpointResult
    {
        public string? DisplayName { get; set; }
        public string? Projection { get; set; }
        public string? Position { get; set; }
        public string? Rotation { get; set; }
    }

    public sealed class ClashTestInfo
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ResultCount { get; set; }
    }

    public sealed class ClashTestListResult
    {
        public List<ClashTestInfo> Tests { get; set; } = new List<ClashTestInfo>();
    }

    public sealed class ClashResultInfo
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public ItemRef? Item1 { get; set; }
        public ItemRef? Item2 { get; set; }
        public ClashItemDetail? Item1Detail { get; set; }
        public ClashItemDetail? Item2Detail { get; set; }
        public string? Distance { get; set; }
        public string? Description { get; set; }
        public string? ClashPoint { get; set; }
        public string? ImagePath { get; set; }
        public string? ImageRelativePath { get; set; }
    }

    public sealed class ClashItemDetail
    {
        public string ItemId { get; set; } = string.Empty;
        public string Layer { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
    }

    public sealed class ClashResultsResult
    {
        public string TestId { get; set; } = string.Empty;
        public List<ClashResultInfo> Results { get; set; } = new List<ClashResultInfo>();
        public bool Truncated { get; set; }
        public string? NextCursor { get; set; }
    }

    public sealed class WriteResult
    {
        public bool Applied { get; set; }
        public string Message { get; set; } = string.Empty;
        public int AffectedCount { get; set; }
    }

    public sealed class ExportResult
    {
        public bool Exported { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public long? Length { get; set; }
    }

    public sealed class ViewportSnapshotResult
    {
        public string FilePath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public string Format { get; set; } = "png";
        public long? Length { get; set; }
        public string Style { get; set; } = "scene_plus_overlay";
        public string FramingApplied { get; set; } = "current";
        public string DocumentTitle { get; set; } = string.Empty;
        public int SelectionCount { get; set; }
        public CurrentViewpointResult? Viewpoint { get; set; }
        public DateTimeOffset GeneratedUtc { get; set; } = DateTimeOffset.UtcNow;
    }

    public sealed class ModelTreeParams
    {
        public int MaxDepth { get; set; } = 3;
        public int MaxNodes { get; set; } = 500;
        public string? Cursor { get; set; }
        public string? ScopeItemId { get; set; }
    }

    public sealed class FindItemsParams
    {
        public string Query { get; set; } = string.Empty;
        public int MaxResults { get; set; } = 50;
        public bool IncludeProperties { get; set; }
        public string MatchMode { get; set; } = "contains";
        public bool AccentInsensitive { get; set; } = true;
        public bool CaseSensitive { get; set; }
        public int Offset { get; set; }
        public string? Cursor { get; set; }
        public string? ScopeItemId { get; set; }
        public SearchFilter? Filter { get; set; }
    }

    public sealed class SearchFilter
    {
        public string? SourceModelContains { get; set; }
        public string? ClassContains { get; set; }
        public string? CategoryContains { get; set; }
        public string? PropertyCategoryContains { get; set; }
        public string? PropertyNameContains { get; set; }
        public string? PropertyValueContains { get; set; }
        public string? MaterialContains { get; set; }
        public string? TagContains { get; set; }
    }

    public sealed class PropertySchemaParams
    {
        public int MaxItems { get; set; } = 2000;
        public int MaxCategories { get; set; } = 200;
        public int MaxPropertiesPerCategory { get; set; } = 200;
    }

    public sealed class PropertySchemaResult
    {
        public List<PropertySchemaCategory> Categories { get; set; } = new List<PropertySchemaCategory>();
        public int SampledItemCount { get; set; }
        public bool Truncated { get; set; }
    }

    public sealed class PropertySchemaCategory
    {
        public string DisplayName { get; set; } = string.Empty;
        public string? Name { get; set; }
        public List<PropertySchemaProperty> Properties { get; set; } = new List<PropertySchemaProperty>();
    }

    public sealed class PropertySchemaProperty
    {
        public string DisplayName { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Type { get; set; }
    }

    public sealed class ItemLookupParams
    {
        public string? ItemId { get; set; }
        public List<int>? IndexPath { get; set; }
    }

    public sealed class ItemsLookupParams
    {
        public List<string> ItemIds { get; set; } = new List<string>();
        public bool PreserveExistingSelection { get; set; }
    }

    public sealed class SelectionSetLookupParams
    {
        public string SelectionSetId { get; set; } = string.Empty;
        public int MaxItems { get; set; } = 1000;
    }

    public sealed class ViewpointLookupParams
    {
        public string ViewpointId { get; set; } = string.Empty;
    }

    public sealed class SaveViewpointParams
    {
        public string DisplayName { get; set; } = string.Empty;
    }

    public sealed class SetHiddenParams
    {
        public List<string> ItemIds { get; set; } = new List<string>();
        public bool Hidden { get; set; } = true;
    }

    public sealed class SelectBySearchParams
    {
        public string Query { get; set; } = string.Empty;
        public int MaxResults { get; set; } = 100;
        public string? ScopeItemId { get; set; }
    }

    public sealed class ExportNwdParams
    {
        public string FilePath { get; set; } = string.Empty;
        public bool ExcludeHiddenItems { get; set; } = true;
        public bool EmbedXrefs { get; set; } = true;
    }

    public sealed class ViewportSnapshotParams
    {
        public string FilePath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public int Width { get; set; } = 1920;
        public int Height { get; set; } = 1080;
        public string Format { get; set; } = "png";
        public string Style { get; set; } = "scene_plus_overlay";
        public string Framing { get; set; } = "auto";
        public bool EnableSectioning { get; set; } = true;
        public double MaxTimeHintSeconds { get; set; } = 2.0;
        public bool RestoreView { get; set; }
    }

    public sealed class ClashTestLookupParams
    {
        public string TestId { get; set; } = string.Empty;
        public int MaxResults { get; set; } = 100;
        public int Offset { get; set; }
        public string? Cursor { get; set; }
    }

    public sealed class ReportResult
    {
        public bool Written { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string Format { get; set; } = "json";
        public long? Length { get; set; }
        public string DocumentTitle { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public DateTimeOffset GeneratedUtc { get; set; } = DateTimeOffset.UtcNow;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class ItemsTableExportParams
    {
        public string FilePath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string Format { get; set; } = "csv";
        public string Query { get; set; } = string.Empty;
        public List<string> ItemIds { get; set; } = new List<string>();
        public bool UseCurrentSelection { get; set; }
        public bool IncludeProperties { get; set; } = true;
        public int MaxItems { get; set; } = 500;
        public string? ScopeItemId { get; set; }
        public SearchFilter? Filter { get; set; }
    }

    public sealed class IssueSummaryParams
    {
        public string FilePath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string Format { get; set; } = "json";
        public string Title { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public string? ClashTestId { get; set; }
        public int MaxItems { get; set; } = 100;
        public int MaxClashResults { get; set; } = 100;
        public string? ScopeItemId { get; set; }
        public SearchFilter? Filter { get; set; }
    }

    public sealed class ClashReportExportParams
    {
        public string TestId { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string Format { get; set; } = "html";
        public int MaxResults { get; set; } = 500;
        public int Offset { get; set; }
        public bool IncludeImages { get; set; } = true;
        public int ImageWidth { get; set; } = 220;
        public int ImageHeight { get; set; } = 140;
    }

    public sealed class QaCheckParams
    {
        public string FilePath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string Format { get; set; } = "json";
        public string Query { get; set; } = string.Empty;
        public List<string> RequiredProperties { get; set; } = new List<string>();
        public bool CheckDuplicateTags { get; set; } = true;
        public int MaxItems { get; set; } = 1000;
        public string? ScopeItemId { get; set; }
        public SearchFilter? Filter { get; set; }
    }

    public sealed class QaCheckResult
    {
        public List<QaIssue> Issues { get; set; } = new List<QaIssue>();
        public int CheckedItemCount { get; set; }
        public bool Truncated { get; set; }
        public ReportResult? Report { get; set; }
    }

    public sealed class QaIssue
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public ItemRef? Item { get; set; }
    }

    public sealed class SelectionSetMutationParams
    {
        public string SelectionSetId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public List<string> ItemIds { get; set; } = new List<string>();
        public bool UseCurrentSelection { get; set; }
    }

    public sealed class SavedItemMutationParams
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    public sealed class SearchSetDefinition
    {
        public string DisplayName { get; set; } = string.Empty;
        public List<SearchConditionDefinition> Conditions { get; set; } = new List<SearchConditionDefinition>();
        public string? Folder { get; set; }
    }

    public sealed class SearchConditionDefinition
    {
        public string Category { get; set; } = string.Empty;
        public string Property { get; set; } = string.Empty;
        public string Operator { get; set; } = "contains";
        public string Value { get; set; } = string.Empty;
        public bool Negate { get; set; }
        public string Logic { get; set; } = "and";
    }

    public sealed class SearchSetImportExportParams
    {
        public string FilePath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
    }

    public sealed class AppearanceOverride
    {
        public List<string> ItemIds { get; set; } = new List<string>();
        public string? Color { get; set; }
        public double? Transparency { get; set; }
    }

    public sealed class SectionBoxParams
    {
        public string Mode { get; set; } = "clear";
        public List<string> ItemIds { get; set; } = new List<string>();
    }

    public sealed class ClashTestDefinition
    {
        public string TestId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string SelectionA { get; set; } = string.Empty;
        public string SelectionB { get; set; } = string.Empty;
        public string ClashType { get; set; } = "hard";
        public double Tolerance { get; set; }
        public bool CompositeObjectClashing { get; set; } = true;
    }

    public sealed class ClashResultUpdate
    {
        public string TestId { get; set; } = string.Empty;
        public string ResultId { get; set; } = string.Empty;
        public string? Status { get; set; }
        public string? Comment { get; set; }
        public string? AssignedTo { get; set; }
    }

    public sealed class AppendModelParams
    {
        public string FilePath { get; set; } = string.Empty;
    }

    public sealed class TimeLinerTaskDto
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Start { get; set; }
        public string? End { get; set; }
        public int AttachedItemCount { get; set; }
    }

    public sealed class TimeLinerTaskListResult
    {
        public List<TimeLinerTaskDto> Tasks { get; set; } = new List<TimeLinerTaskDto>();
        public bool Truncated { get; set; }
    }

    public sealed class TimeLinerLinkParams
    {
        public string TaskId { get; set; } = string.Empty;
        public List<string> ItemIds { get; set; } = new List<string>();
        public string? SearchSetId { get; set; }
    }

    public sealed class QuantificationSummary
    {
        public List<PropertyDto> Metrics { get; set; } = new List<PropertyDto>();
        public ReportResult? Report { get; set; }
    }

    public sealed class MeasureParams
    {
        public List<string> ItemIds { get; set; } = new List<string>();
        public bool UseCurrentSelection { get; set; }
    }

    public sealed class MeasureResult
    {
        public int ItemCount { get; set; }
        public string? BoundingBox { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public sealed class NotSupportedResult
    {
        public bool Supported { get; set; }
        public string ErrorCode { get; set; } = "not_supported_by_api";
        public string Capability { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class FederationModelInfo
    {
        public string DisplayName { get; set; } = string.Empty;
        public string? SourcePath { get; set; }
        public string? SourceFileName { get; set; }
        public string? Guid { get; set; }
        public int ItemCountEstimate { get; set; }
        public string? Units { get; set; }
        public ItemRef? RootItem { get; set; }
    }

    public sealed class FederationMapResult
    {
        public string DocumentTitle { get; set; } = string.Empty;
        public string? DocumentPath { get; set; }
        public List<FederationModelInfo> Models { get; set; } = new List<FederationModelInfo>();
        public int ModelCount { get; set; }
    }

    public sealed class FederatedPreflightResult
    {
        public bool Written { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string JsonFilePath { get; set; } = string.Empty;
        public string JsonRelativePath { get; set; } = string.Empty;
        public string DocumentTitle { get; set; } = string.Empty;
        public string? DocumentPath { get; set; }
        public string? Units { get; set; }
        public int ModelCount { get; set; }
        public double DefaultToleranceMeters { get; set; } = 0.1;
        public List<string> Warnings { get; set; } = new List<string>();
        public List<FederationModelInfo> Models { get; set; } = new List<FederationModelInfo>();
        public DateTimeOffset GeneratedUtc { get; set; } = DateTimeOffset.UtcNow;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class DomainOntologyTerm
    {
        public string Term { get; set; } = string.Empty;
        public string Kind { get; set; } = "must";
        public string? Note { get; set; }
    }

    public sealed class DomainOntologyPack
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Discipline { get; set; } = string.Empty;
        public string Locale { get; set; } = "vi";
        public List<string> SearchQueries { get; set; } = new List<string>();
        public List<DomainOntologyTerm> Terms { get; set; } = new List<DomainOntologyTerm>();
        public List<string> FalsePositiveNotes { get; set; } = new List<string>();
    }

    public sealed class DomainOntologyResult
    {
        public List<DomainOntologyPack> Packs { get; set; } = new List<DomainOntologyPack>();
    }

    public sealed class ClashClusterParams
    {
        public string TestId { get; set; } = string.Empty;
        public int MaxResults { get; set; } = 500;
        public double PointPrecision { get; set; } = 0.1;
        public List<double> DistanceBuckets { get; set; } = new List<double> { 0.01, 0.05, 0.1, 0.5, 1.0 };
        public List<ClashResultInfo> Results { get; set; } = new List<ClashResultInfo>();
    }

    public sealed class ClashClusterGroup
    {
        public string ClusterKey { get; set; } = string.Empty;
        public string? RoundedClashPoint { get; set; }
        public string? ModelPair { get; set; }
        public string? DistanceBucket { get; set; }
        public int Count { get; set; }
        public List<string> ResultIds { get; set; } = new List<string>();
        public List<ClashResultInfo> SampleResults { get; set; } = new List<ClashResultInfo>();
    }

    public sealed class ClashClusterResult
    {
        public string TestId { get; set; } = string.Empty;
        public int SourceResultCount { get; set; }
        public int ClusterCount { get; set; }
        public List<ClashClusterGroup> Clusters { get; set; } = new List<ClashClusterGroup>();
        public bool Truncated { get; set; }
    }

    public sealed class IssuePackParams
    {
        public string FilePath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string MarkdownFilePath { get; set; } = string.Empty;
        public string MarkdownRelativePath { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = "open";
        public string? Assignee { get; set; }
        public string? Priority { get; set; }
        public string? Description { get; set; }
        public List<string> ItemIds { get; set; } = new List<string>();
        public List<ItemRef> Items { get; set; } = new List<ItemRef>();
        public string? SnapshotPath { get; set; }
        public string? SnapshotRelativePath { get; set; }
        public string? ClashTestId { get; set; }
        public string? ClashResultId { get; set; }
        public string? ClashPoint { get; set; }
        public string? Distance { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    public sealed class IssuePackResult
    {
        public bool Written { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string MarkdownFilePath { get; set; } = string.Empty;
        public string MarkdownRelativePath { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = "open";
        public int ItemCount { get; set; }
        public DateTimeOffset GeneratedUtc { get; set; } = DateTimeOffset.UtcNow;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class BcfExportParams
    {
        public string IssuePackPath { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    public sealed class BcfExportResult
    {
        public bool Written { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string IssuePackPath { get; set; } = string.Empty;
        public long? Length { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public sealed class Phase0SelectionQaParams
    {
        public string FilePath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string JsonFilePath { get; set; } = string.Empty;
        public string JsonRelativePath { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public int MaxSamples { get; set; } = 50;
        public bool UseCurrentSelection { get; set; } = true;
        public string? ScopeItemId { get; set; }
    }

    public sealed class Phase0SelectionQaResult
    {
        public bool Written { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string JsonFilePath { get; set; } = string.Empty;
        public string JsonRelativePath { get; set; } = string.Empty;
        public int SelectionCount { get; set; }
        public int SampleCount { get; set; }
        public List<string> SampleNames { get; set; } = new List<string>();
        public List<string> Must { get; set; } = new List<string>();
        public List<string> Should { get; set; } = new List<string>();
        public List<string> Qa { get; set; } = new List<string>();
        public List<string> List { get; set; } = new List<string>();
        public List<string> Exclude { get; set; } = new List<string>();
        public string Message { get; set; } = string.Empty;
    }

    public sealed class VisualQaBatchParams
    {
        public List<string> Framings { get; set; } = new List<string>();
        public int Width { get; set; } = 1600;
        public int Height { get; set; } = 900;
        public string Format { get; set; } = "png";
        public string Style { get; set; } = "scene_plus_overlay";
        public bool RestoreView { get; set; } = true;
        public string ChecklistFilePath { get; set; } = string.Empty;
        public string ChecklistRelativePath { get; set; } = string.Empty;
        public string FileNamePrefix { get; set; } = "visual-qa";
    }

    public sealed class VisualQaSnapshotEntry
    {
        public string Framing { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public bool Ok { get; set; }
        public string? Error { get; set; }
    }

    public sealed class VisualQaBatchResult
    {
        public bool Written { get; set; }
        public string ChecklistFilePath { get; set; } = string.Empty;
        public string ChecklistRelativePath { get; set; } = string.Empty;
        public List<VisualQaSnapshotEntry> Snapshots { get; set; } = new List<VisualQaSnapshotEntry>();
        public List<string> ChecklistItems { get; set; } = new List<string>();
        public string Message { get; set; } = string.Empty;
    }

    public sealed class PlaybookInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
    }

    public sealed class PlaybookListResult
    {
        public List<PlaybookInfo> Playbooks { get; set; } = new List<PlaybookInfo>();
    }

    public sealed class PlaybookContentResult
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string Markdown { get; set; } = string.Empty;
    }

    public sealed class ToolsetInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Tools { get; set; } = new List<string>();
    }

    public sealed class ToolsetListResult
    {
        public List<string> Requested { get; set; } = new List<string>();
        public bool Brief { get; set; }
        public List<ToolsetInfo> Toolsets { get; set; } = new List<ToolsetInfo>();
        public string Note { get; set; } = "Tool filtering at registration is not applied; use this catalog to scope agent tool use.";
    }
}
