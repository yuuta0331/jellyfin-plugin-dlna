using System;
using System.Diagnostics;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Collects timing metrics for a single DLNA Browse operation.
/// </summary>
public sealed class BrowseTimingScope : IDisposable
{
    private readonly Stopwatch _total = Stopwatch.StartNew();
    private long _queryMs;
    private long _indexMs;
    private long _summaryMs;
    private long _dtoMs;
    private long _didlMs;

    /// <summary>
    /// Gets or sets the browsed object id.
    /// </summary>
    public string ObjectId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stub type name, if any.
    /// </summary>
    public string? StubTypeName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the browse response cache hit.
    /// </summary>
    public bool CacheHit { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a virtual index was used.
    /// </summary>
    public bool IndexHit { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether item summaries were used.
    /// </summary>
    public bool SummaryHit { get; set; }

    /// <summary>
    /// Gets or sets the number of returned items.
    /// </summary>
    public int Items { get; set; }

    /// <summary>
    /// Gets or sets the generated XML byte length.
    /// </summary>
    public int XmlBytes { get; set; }

    /// <summary>
    /// Records query elapsed milliseconds.
    /// </summary>
    /// <param name="elapsedMs">Elapsed milliseconds.</param>
    public void AddQueryMs(long elapsedMs) => _queryMs += elapsedMs;

    /// <summary>
    /// Records index elapsed milliseconds.
    /// </summary>
    /// <param name="elapsedMs">Elapsed milliseconds.</param>
    public void AddIndexMs(long elapsedMs) => _indexMs += elapsedMs;

    /// <summary>
    /// Records summary elapsed milliseconds.
    /// </summary>
    /// <param name="elapsedMs">Elapsed milliseconds.</param>
    public void AddSummaryMs(long elapsedMs) => _summaryMs += elapsedMs;

    /// <summary>
    /// Records DTO elapsed milliseconds.
    /// </summary>
    /// <param name="elapsedMs">Elapsed milliseconds.</param>
    public void AddDtoMs(long elapsedMs) => _dtoMs += elapsedMs;

    /// <summary>
    /// Records DIDL elapsed milliseconds.
    /// </summary>
    /// <param name="elapsedMs">Elapsed milliseconds.</param>
    public void AddDidlMs(long elapsedMs) => _didlMs += elapsedMs;

    /// <inheritdoc />
    public void Dispose()
    {
        _total.Stop();
    }

    /// <summary>
    /// Builds a summary string for structured logging.
    /// </summary>
    /// <returns>The summary.</returns>
    public string ToLogSummary()
        => $"ObjectId={ObjectId} StubType={StubTypeName ?? "none"} CacheHit={CacheHit} IndexHit={IndexHit} SummaryHit={SummaryHit} " +
           $"QueryMs={_queryMs} IndexMs={_indexMs} SummaryMs={_summaryMs} DtoMs={_dtoMs} DidlMs={_didlMs} TotalMs={_total.ElapsedMilliseconds} " +
           $"Items={Items} XmlBytes={XmlBytes}";
}
