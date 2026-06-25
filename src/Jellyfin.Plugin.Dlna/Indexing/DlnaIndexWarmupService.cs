using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Dlna.Indexing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.Indexing;

/// <summary>
/// Warms up DLNA virtual indexes on server startup.
/// </summary>
public sealed class DlnaIndexWarmupService : IHostedService
{
    private readonly IDlnaVirtualIndexService _indexService;
    private readonly IDlnaBrowsePrewarmService _prewarmService;
    private readonly ILogger<DlnaIndexWarmupService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DlnaIndexWarmupService"/> class.
    /// </summary>
    public DlnaIndexWarmupService(
        IDlnaVirtualIndexService indexService,
        IDlnaBrowsePrewarmService prewarmService,
        ILogger<DlnaIndexWarmupService> logger)
    {
        _indexService = indexService;
        _prewarmService = prewarmService;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var config = DlnaPlugin.Instance.Configuration;
        if (!config.WarmupIndexOnStartup)
        {
            return Task.CompletedTask;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("DLNA index warmup started");
                await _indexService.RebuildAllAsync(null, cancellationToken).ConfigureAwait(false);
                await _prewarmService.PrewarmAsync(null, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("DLNA index warmup completed");
            }
            catch (OperationCanceledException)
            {
                // shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DLNA index warmup failed");
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
