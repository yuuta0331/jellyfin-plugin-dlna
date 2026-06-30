using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.Indexing;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.Dlna.Tasks;

/// <summary>
/// Scheduled task that prewarms DLNA Browse responses during off-peak hours.
/// </summary>
public sealed class DlnaScheduledPrewarmTask : IScheduledTask, IConfigurableScheduledTask
{
    private readonly IDlnaBrowsePrewarmService _prewarmService;
    private readonly DlnaServerLoadGuard _loadGuard;

    /// <summary>
    /// Initializes a new instance of the <see cref="DlnaScheduledPrewarmTask"/> class.
    /// </summary>
    public DlnaScheduledPrewarmTask(
        IDlnaBrowsePrewarmService prewarmService,
        DlnaServerLoadGuard loadGuard)
    {
        _prewarmService = prewarmService;
        _loadGuard = loadGuard;
    }

    /// <inheritdoc />
    public string Name => "DLNA Scheduled Browse Prewarm";

    /// <inheritdoc />
    public string Key => "DlnaScheduledPrewarm";

    /// <inheritdoc />
    public string Description => "Pre-generates DLNA Browse XML for key folders when the server is idle.";

    /// <inheritdoc />
    public string Category => "DLNA";

    /// <inheritdoc />
    public bool IsEnabled => DlnaConfigurationAccessor.Current.EnableScheduledPrewarm;

    /// <inheritdoc />
    public bool IsHidden => false;

    /// <inheritdoc />
    public bool IsLogged => true;

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        if (!DlnaConfigurationAccessor.Current.EnableScheduledPrewarm)
        {
            return [];
        }

        return
        [
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.DailyTrigger,
                TimeOfDayTicks = TimeSpan.FromHours(3).Ticks,
                MaxRuntimeTicks = TimeSpan.FromHours(2).Ticks
            }
        ];
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        if (!DlnaConfigurationAccessor.Current.EnableScheduledPrewarm)
        {
            return;
        }

        if (_loadGuard.IsServerBusy())
        {
            return;
        }

        await _prewarmService.PrewarmAsync(null, cancellationToken).ConfigureAwait(false);
        progress?.Report(100);
    }
}
