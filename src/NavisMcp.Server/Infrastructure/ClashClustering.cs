using System.Globalization;
using System.Text.RegularExpressions;
using NavisMcp.Contracts;

namespace NavisMcp.Server.Infrastructure;

public static class ClashClustering
{
    private static readonly Regex PointRegex = new(
        @"[-+]?\d*\.?\d+(?:[eE][-+]?\d+)?",
        RegexOptions.Compiled);

    public static ClashClusterResult Cluster(ClashClusterParams parameters, IReadOnlyList<ClashResultInfo> results)
    {
        var precision = parameters.PointPrecision > 0 ? parameters.PointPrecision : 0.1;
        var buckets = (parameters.DistanceBuckets == null || parameters.DistanceBuckets.Count == 0)
            ? new List<double> { 0.01, 0.05, 0.1, 0.5, 1.0 }
            : parameters.DistanceBuckets.OrderBy(x => x).ToList();

        var groups = new Dictionary<string, ClashClusterGroup>(StringComparer.OrdinalIgnoreCase);
        foreach (var result in results)
        {
            var roundedPoint = RoundPoint(result.ClashPoint, precision);
            var modelPair = BuildModelPair(result);
            var distanceBucket = BucketDistance(ParseDistance(result.Distance), buckets);
            var key = string.Join("|", roundedPoint ?? "unknown", modelPair, distanceBucket);

            if (!groups.TryGetValue(key, out var group))
            {
                group = new ClashClusterGroup
                {
                    ClusterKey = key,
                    RoundedClashPoint = roundedPoint,
                    ModelPair = modelPair,
                    DistanceBucket = distanceBucket
                };
                groups[key] = group;
            }

            group.Count++;
            group.ResultIds.Add(result.Id);
            if (group.SampleResults.Count < 3)
            {
                group.SampleResults.Add(result);
            }
        }

        var clusters = groups.Values
            .OrderByDescending(g => g.Count)
            .ThenBy(g => g.ClusterKey, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ClashClusterResult
        {
            TestId = parameters.TestId ?? string.Empty,
            SourceResultCount = results.Count,
            ClusterCount = clusters.Count,
            Clusters = clusters,
            Truncated = false
        };
    }

    public static string? RoundPoint(string? clashPoint, double precision)
    {
        if (string.IsNullOrWhiteSpace(clashPoint))
        {
            return null;
        }

        var matches = PointRegex.Matches(clashPoint);
        if (matches.Count < 3)
        {
            return clashPoint.Trim();
        }

        var coords = matches
            .Cast<Match>()
            .Take(3)
            .Select(m => RoundValue(double.Parse(m.Value, CultureInfo.InvariantCulture), precision))
            .ToArray();

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0:0.###},{1:0.###},{2:0.###}",
            coords[0],
            coords[1],
            coords[2]);
    }

    public static string BuildModelPair(ClashResultInfo result)
    {
        var a = NormalizeModelName(result.Item1?.ModelName ?? result.Item1Detail?.Layer);
        var b = NormalizeModelName(result.Item2?.ModelName ?? result.Item2Detail?.Layer);
        var pair = new[] { a, b }.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        return pair[0] + " x " + pair[1];
    }

    public static string BucketDistance(double? distance, IReadOnlyList<double> buckets)
    {
        if (distance is null)
        {
            return "unknown";
        }

        var value = Math.Abs(distance.Value);
        foreach (var edge in buckets)
        {
            if (value <= edge)
            {
                return "<=" + edge.ToString("0.###", CultureInfo.InvariantCulture);
            }
        }

        return ">" + buckets[buckets.Count - 1].ToString("0.###", CultureInfo.InvariantCulture);
    }

    public static double? ParseDistance(string? distance)
    {
        if (string.IsNullOrWhiteSpace(distance))
        {
            return null;
        }

        var match = PointRegex.Match(distance);
        if (!match.Success)
        {
            return null;
        }

        return double.Parse(match.Value, CultureInfo.InvariantCulture);
    }

    private static double RoundValue(double value, double precision)
    {
        if (precision <= 0)
        {
            return value;
        }

        // Floor onto the precision grid so nearby clash points stay in one cluster.
        return Math.Floor(value / precision) * precision;
    }

    private static string NormalizeModelName(string? name)
    {
        return string.IsNullOrWhiteSpace(name) ? "(unknown)" : name.Trim();
    }
}
