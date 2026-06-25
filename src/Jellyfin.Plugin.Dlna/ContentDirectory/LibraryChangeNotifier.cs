using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Dlna.Indexing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Hosting;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Tracks library changes and invalidates browse caches selectively.
/// </summary>
public sealed class LibraryChangeNotifier : IHostedService
{
    private readonly ILibraryManager _libraryManager;
    private readonly IContentInvalidationService _invalidationService;
    private int _libraryGeneration;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryChangeNotifier"/> class.
    /// </summary>
    public LibraryChangeNotifier(
        ILibraryManager libraryManager,
        IContentInvalidationService invalidationService)
    {
        _libraryManager = libraryManager;
        _invalidationService = invalidationService;
    }

    /// <summary>
    /// Gets the current library generation used for cache keys and UpdateID.
    /// </summary>
    public int LibraryGeneration => _libraryGeneration;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemAdded += OnLibraryItemChanged;
        _libraryManager.ItemUpdated += OnLibraryItemChanged;
        _libraryManager.ItemRemoved += OnLibraryItemChanged;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemAdded -= OnLibraryItemChanged;
        _libraryManager.ItemUpdated -= OnLibraryItemChanged;
        _libraryManager.ItemRemoved -= OnLibraryItemChanged;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invalidates all caches and indexes (e.g. after configuration change).
    /// </summary>
    public void InvalidateAll(string reason)
    {
        Interlocked.Increment(ref _libraryGeneration);
        _invalidationService.InvalidateAll(reason);
    }

    private void OnLibraryItemChanged(object? sender, ItemChangeEventArgs e)
    {
        Interlocked.Increment(ref _libraryGeneration);
        var config = DlnaPlugin.Instance.Configuration;

        if (!config.InvalidateByLibraryScope)
        {
            _invalidationService.InvalidateAll("library-item-changed");
            return;
        }

        var libraryId = LibraryBrowseQueryHelper.ResolveLibraryId(_libraryManager, e.Item);
        if (libraryId == Guid.Empty)
        {
            _invalidationService.InvalidateAll("library-item-changed-unscoped");
            return;
        }

        _invalidationService.ScheduleLibraryInvalidation(libraryId, "library-item-changed");
    }
}
