using System;
using System.IO;
using System.Linq;
using Jellyfin.Plugin.Dlna.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.Dlna.Indexing;

/// <summary>
/// Guards DLNA index and prewarm work against server load and unavailable library paths.
/// </summary>
public sealed class DlnaServerLoadGuard
{
    private static readonly string[] BusyTaskNameFragments =
    [
        "Scan",
        "Database",
        "Trickplay",
        "Chapter",
        "Intro",
        "Keyframe",
        "Segment"
    ];

    private readonly ITaskManager _taskManager;
    private readonly ILibraryManager _libraryManager;
    private DateTimeOffset _lastIndexWorkUtc = DateTimeOffset.MinValue;
    private DateTimeOffset _lastPrewarmUtc = DateTimeOffset.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="DlnaServerLoadGuard"/> class.
    /// </summary>
    public DlnaServerLoadGuard(ITaskManager taskManager, ILibraryManager libraryManager)
    {
        _taskManager = taskManager;
        _libraryManager = libraryManager;
    }

    /// <summary>
    /// Returns whether Jellyfin scheduled tasks indicate the server is busy.
    /// </summary>
    public bool IsServerBusy()
    {
        var config = DlnaConfigurationAccessor.Current;
        if (!config.SkipIndexWhileServerBusy)
        {
            return false;
        }

        return _taskManager.ScheduledTasks
            .Where(task => task.State == TaskState.Running)
            .Select(task => task.Name)
            .Any(IsBusyTaskName);
    }

    /// <summary>
    /// Returns whether a scheduled task name indicates the server is busy.
    /// </summary>
    /// <param name="taskName">The task name.</param>
    internal static bool IsBusyTaskName(string taskName)
        => BusyTaskNameFragments.Any(fragment =>
            taskName.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Returns whether all configured paths for a library are reachable.
    /// </summary>
    /// <param name="libraryId">The library id.</param>
    public bool IsLibraryPathAvailable(Guid libraryId)
    {
        var library = _libraryManager.GetItemById(libraryId);
        if (library is not CollectionFolder folder)
        {
            return true;
        }

        var paths = folder.PhysicalLocations;
        if (paths is null || paths.Length == 0)
        {
            return true;
        }

        return paths.All(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path));
    }

    /// <summary>
    /// Returns whether automatic index work may run now.
    /// </summary>
    public bool CanRunIndexWork()
    {
        if (IsServerBusy())
        {
            return false;
        }

        var config = DlnaConfigurationAccessor.Current;
        var minInterval = TimeSpan.FromMinutes(Math.Max(1, config.MinIndexIntervalMinutes));
        return DateTimeOffset.UtcNow - _lastIndexWorkUtc >= minInterval;
    }

    /// <summary>
    /// Returns whether prewarm work may run now.
    /// </summary>
    public bool CanRunPrewarm()
    {
        if (IsServerBusy())
        {
            return false;
        }

        var config = DlnaConfigurationAccessor.Current;
        var minInterval = TimeSpan.FromMinutes(Math.Max(1, config.PrewarmMinIntervalMinutes));
        return DateTimeOffset.UtcNow - _lastPrewarmUtc >= minInterval;
    }

    /// <summary>
    /// Records that index work completed.
    /// </summary>
    public void RecordIndexWorkCompleted()
        => _lastIndexWorkUtc = DateTimeOffset.UtcNow;

    /// <summary>
    /// Records that prewarm work completed.
    /// </summary>
    public void RecordPrewarmCompleted()
        => _lastPrewarmUtc = DateTimeOffset.UtcNow;
}
