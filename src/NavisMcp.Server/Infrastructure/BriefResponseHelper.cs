using NavisMcp.Contracts;

namespace NavisMcp.Server.Infrastructure;

public static class BriefResponseHelper
{
    public const int DefaultBriefItemLimit = 20;
    public const int DefaultBriefClashLimit = 25;
    public const int DefaultBriefClusterLimit = 15;

    public static ItemSearchResult MaybeTruncate(ItemSearchResult result, bool brief, int limit = DefaultBriefItemLimit)
    {
        if (!brief || result.Items.Count <= limit)
        {
            return result;
        }

        result.Items = result.Items.Take(limit).ToList();
        result.ReturnedCount = result.Items.Count;
        result.Truncated = true;
        if (string.IsNullOrWhiteSpace(result.NextCursor))
        {
            result.NextCursor = "brief";
        }

        return result;
    }

    public static ClashResultsResult MaybeTruncate(ClashResultsResult result, bool brief, int limit = DefaultBriefClashLimit)
    {
        if (!brief || result.Results.Count <= limit)
        {
            return result;
        }

        result.Results = result.Results.Take(limit).ToList();
        result.Truncated = true;
        if (string.IsNullOrWhiteSpace(result.NextCursor))
        {
            result.NextCursor = "brief";
        }

        return result;
    }

    public static ClashClusterResult MaybeTruncate(ClashClusterResult result, bool brief, int limit = DefaultBriefClusterLimit)
    {
        if (!brief || result.Clusters.Count <= limit)
        {
            return result;
        }

        result.Clusters = result.Clusters.Take(limit).ToList();
        result.Truncated = true;
        return result;
    }

    public static SelectionResult MaybeTruncate(SelectionResult result, bool brief, int limit = DefaultBriefItemLimit)
    {
        if (!brief || result.Items.Count <= limit)
        {
            return result;
        }

        result.Items = result.Items.Take(limit).ToList();
        result.Count = result.Items.Count;
        return result;
    }
}
