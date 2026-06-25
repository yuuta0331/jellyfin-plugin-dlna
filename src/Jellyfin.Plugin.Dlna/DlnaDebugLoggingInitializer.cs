using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Jellyfin.Plugin.Dlna;

/// <summary>
/// Synchronizes debug logging state when the plugin starts.
/// </summary>
public sealed class DlnaDebugLoggingInitializer : IHostedService
{
    private readonly DlnaDebugLoggingState _debugState;

    /// <summary>
    /// Initializes a new instance of the <see cref="DlnaDebugLoggingInitializer"/> class.
    /// </summary>
    /// <param name="debugState">The debug logging state.</param>
    public DlnaDebugLoggingInitializer(DlnaDebugLoggingState debugState)
    {
        _debugState = debugState;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _debugState.SyncFrom(DlnaPlugin.Instance.Configuration);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
