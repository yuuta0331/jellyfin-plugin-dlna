using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Dlna.Indexing;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.Dlna.Tasks;

/// <summary>
/// Scheduled task that rebuilds DLNA virtual folder indexes.
/// </summary>
public sealed class RebuildDlnaQuestIndexTask : IScheduledTask, IConfigurableScheduledTask
{
    private readonly IDlnaVirtualIndexService _indexService;
    private readonly IDlnaBrowsePrewarmService _prewarmService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RebuildDlnaQuestIndexTask"/> class.
    /// </summary>
    /// <param name="indexService">The virtual index service.</param>
    /// <param name="prewarmService">The browse prewarm service.</param>
    public RebuildDlnaQuestIndexTask(
        IDlnaVirtualIndexService indexService,
        IDlnaBrowsePrewarmService prewarmService)
    {
        _indexService = indexService;
        _prewarmService = prewarmService;
    }

    /// <inheritdoc />
    public string Name => "Rebuild DLNA Quest Index";

    /// <inheritdoc />
    public string Key => "RebuildDlnaQuestIndex";

    /// <inheritdoc />
    public string Description => "Rebuilds DLNA virtual folder indexes for faster Browse responses.";

    /// <inheritdoc />
    public string Category => "DLNA";

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public bool IsHidden => false;

    /// <inheritdoc />
    public bool IsLogged => true;

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => [];

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        await _indexService.RebuildAllAsync(progress, cancellationToken).ConfigureAwait(false);
        await _prewarmService.PrewarmAsync(null, cancellationToken).ConfigureAwait(false);
    }
}
