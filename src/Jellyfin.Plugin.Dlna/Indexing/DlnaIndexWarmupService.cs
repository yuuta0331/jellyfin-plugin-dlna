using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Dlna.Indexing;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.Indexing;

/// <summary>
/// Warms up DLNA virtual indexes on server startup for libraries that are not yet indexed.
/// </summary>
public sealed class DlnaIndexWarmupService : IHostedService
{
    private readonly ILibraryManager _libraryManager;
    private readonly IDlnaVirtualIndexService _indexService;
    private readonly DlnaServerLoadGuard _loadGuard;
    private readonly ILogger<DlnaIndexWarmupService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DlnaIndexWarmupService"/> class.
    /// </summary>
    public DlnaIndexWarmupService(
        ILibraryManager libraryManager,
        IDlnaVirtualIndexService indexService,
        DlnaServerLoadGuard loadGuard,
        ILogger<DlnaIndexWarmupService> logger)
    {
        _libraryManager = libraryManager;
        _indexService = indexService;
        _loadGuard = loadGuard;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var config = DlnaConfigurationAccessor.Current;
        if (!config.WarmupIndexOnStartup)
        {
            return Task.CompletedTask;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                if (!_loadGuard.CanRunIndexWork())
                {
                    _logger.LogInformation("DLNA index warmup deferred: server busy or minimum interval not elapsed");
                    return;
                }

                var libraries = _libraryManager.GetUserRootFolder().Children
                    .Where(LibraryBrowseQueryHelper.IsDlnaLibraryView)
                    .Where(library => !_indexService.IsReady(library.Id))
                    .Select(library => library.Id)
                    .ToList();

                if (libraries.Count == 0)
                {
                    _logger.LogInformation("DLNA index warmup skipped: all libraries already indexed");
                    return;
                }

                _logger.LogInformation("DLNA index warmup started for {LibraryCount} libraries", libraries.Count);
                var rebuilt = await _indexService.TryRebuildLibrariesAsync(libraries, cancellationToken).ConfigureAwait(false);
                if (rebuilt.Count > 0)
                {
                    _loadGuard.RecordIndexWorkCompleted();
                }

                _logger.LogInformation("DLNA index warmup completed Libraries={LibraryCount}", rebuilt.Count);
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
