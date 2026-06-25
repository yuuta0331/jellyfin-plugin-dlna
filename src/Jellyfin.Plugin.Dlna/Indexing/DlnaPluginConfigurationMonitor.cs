using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Dlna.ContentDirectory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.Indexing;

/// <summary>
/// Invalidates caches and rebuilds indexes when DLNA plugin configuration changes.
/// </summary>
public sealed class DlnaPluginConfigurationMonitor : IHostedService
{
    private readonly IContentInvalidationService _invalidationService;
    private readonly DlnaDebugLoggingState _debugState;
    private readonly ILogger<DlnaPluginConfigurationMonitor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DlnaPluginConfigurationMonitor"/> class.
    /// </summary>
    public DlnaPluginConfigurationMonitor(
        IContentInvalidationService invalidationService,
        DlnaDebugLoggingState debugState,
        ILogger<DlnaPluginConfigurationMonitor> logger)
    {
        _invalidationService = invalidationService;
        _debugState = debugState;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        DlnaPlugin.Instance.ConfigurationUpdated += OnConfigurationUpdated;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        DlnaPlugin.Instance.ConfigurationUpdated -= OnConfigurationUpdated;
        return Task.CompletedTask;
    }

    private void OnConfigurationUpdated(object? sender, EventArgs e)
    {
        _debugState.SyncFrom(DlnaPlugin.Instance.Configuration);
        _logger.LogInformation("DLNA plugin configuration updated; invalidating caches and indexes");
        _invalidationService.InvalidateAll("plugin configuration updated");
    }
}
