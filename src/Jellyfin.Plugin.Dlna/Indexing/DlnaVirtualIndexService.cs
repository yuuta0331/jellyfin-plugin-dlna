using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.Model;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.Indexing;

/// <summary>
/// Orchestrates DLNA virtual folder index builds.
/// </summary>
public sealed class DlnaVirtualIndexService : IDlnaVirtualIndexService, IDisposable
{
    private readonly ILibraryManager _libraryManager;
    private readonly IVirtualIndexStore _store;
    private readonly DlnaIndexBuilder _builder;
    private readonly DlnaIndexGeneration _generation;
    private readonly ILogger<DlnaVirtualIndexService> _logger;
    private readonly SemaphoreSlim _buildLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="DlnaVirtualIndexService"/> class.
    /// </summary>
    public DlnaVirtualIndexService(
        ILibraryManager libraryManager,
        IVirtualIndexStore store,
        IImageProcessor imageProcessor,
        IMediaSourceManager mediaSourceManager,
        IMediaEncoder mediaEncoder,
        IDlnaManager dlnaManager,
        DlnaIndexGeneration generation,
        ILogger<DlnaVirtualIndexService> logger,
        ILoggerFactory loggerFactory)
    {
        _libraryManager = libraryManager;
        _store = store;
        _builder = new DlnaIndexBuilder(
            libraryManager,
            store,
            imageProcessor,
            mediaSourceManager,
            mediaEncoder,
            dlnaManager,
            loggerFactory.CreateLogger<DlnaIndexBuilder>());
        _generation = generation;
        _logger = logger;
    }

    /// <inheritdoc />
    public DlnaIndexGeneration Generation => _generation;

    /// <inheritdoc />
    public bool IsReady(Guid libraryId)
    {
        if (!DlnaPlugin.Instance.Configuration.EnableVirtualFolderIndex)
        {
            return false;
        }

        return _store.IsLibraryIndexed(libraryId);
    }

    /// <inheritdoc />
    public async Task RebuildAllAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        var libraries = GetDlnaLibraries();
        for (var i = 0; i < libraries.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RebuildLibraryAsync(libraries[i].Id, cancellationToken).ConfigureAwait(false);
            progress?.Report((i + 1.0) / libraries.Count);
        }
    }

    /// <inheritdoc />
    public async Task RebuildLibraryAsync(Guid libraryId, CancellationToken cancellationToken)
    {
        await _buildLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await RebuildLibraryCoreAsync(libraryId, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _buildLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> TryRebuildLibrariesAsync(
        IReadOnlyList<Guid> libraryIds,
        CancellationToken cancellationToken)
    {
        if (libraryIds.Count == 0)
        {
            return [];
        }

        if (!await _buildLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            return [];
        }

        try
        {
            var rebuilt = new List<Guid>(libraryIds.Count);
            foreach (var libraryId in libraryIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (await RebuildLibraryCoreAsync(libraryId, cancellationToken).ConfigureAwait(false))
                {
                    rebuilt.Add(libraryId);
                }
            }

            return rebuilt;
        }
        finally
        {
            _buildLock.Release();
        }
    }

    private async Task<bool> RebuildLibraryCoreAsync(Guid libraryId, CancellationToken cancellationToken)
    {
        var library = _libraryManager.GetItemById(libraryId);
        if (library is null || !LibraryBrowseQueryHelper.IsDlnaLibraryView(library))
        {
            return false;
        }

        var config = DlnaPlugin.Instance.Configuration;
        await _builder.BuildLibraryAsync(library, config, cancellationToken).ConfigureAwait(false);
        _generation.Increment();
        return true;
    }

    /// <inheritdoc />
    public void InvalidateAll()
    {
        _store.ClearAll();
        _generation.Increment();
        _logger.LogInformation("DLNA virtual index fully invalidated");
    }

    /// <inheritdoc />
    public void InvalidateLibrary(Guid libraryId)
    {
        _store.ClearLibrary(libraryId);
        _generation.Increment();
        _logger.LogInformation("DLNA virtual index invalidated for library {LibraryId}", libraryId);
    }

    /// <inheritdoc />
    public void Dispose() => _buildLock.Dispose();

    private IReadOnlyList<BaseItem> GetDlnaLibraries()
    {
        return _libraryManager.GetUserRootFolder().Children
            .Where(LibraryBrowseQueryHelper.IsDlnaLibraryView)
            .ToList();
    }
}
