using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.Indexing;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Title browse virtual folder helpers.
/// </summary>
internal static class TitleBrowseHelper
{
    /// <summary>
    /// Legacy row index for alphanumeric titles.
    /// </summary>
    public const int AlphanumericRowIndex = KanaTitleClassifier.AlphanumericRowIndex;

    /// <summary>
    /// Legacy row index for unclassified titles.
    /// </summary>
    public const int OtherRowIndex = KanaTitleClassifier.OtherRowIndex;

    /// <summary>
    /// Classifies a title into a browse group id.
    /// </summary>
    public static string Classify(string? sortName, string? name, TitleBrowseOptions options)
        => TitleBrowseClassifier.Classify(sortName, name, options);

    /// <summary>
    /// Returns true when the title belongs to the given browse group.
    /// </summary>
    public static bool MatchesGroup(string? sortName, string? name, string groupId, TitleBrowseOptions options)
        => TitleBrowseClassifier.MatchesGroup(sortName, name, groupId, options);

    /// <summary>
    /// Gets the display label for a browse group.
    /// </summary>
    public static string GetGroupLabel(TitleBrowseGroup group, bool isJapanese)
        => isJapanese ? group.LabelJa : group.LabelEn;

    /// <summary>
    /// Gets the display label for a group id within a preset.
    /// </summary>
    public static string GetGroupLabel(TitleBrowsePreset preset, string groupId, bool isJapanese)
    {
        var group = preset.Groups.FirstOrDefault(g => string.Equals(g.Id, groupId, StringComparison.Ordinal));
        if (group is null)
        {
            return groupId;
        }

        return GetGroupLabel(group, isJapanese);
    }

    /// <summary>
    /// Builds title browse group virtual folders for a library.
    /// </summary>
    public static ServerItem[] CreateGroupFolders(
        BaseItem library,
        TitleBrowsePreset preset,
        bool hideEmpty,
        IVirtualIndexStore? store,
        BaseItemKind? itemTypeForCounts = null)
    {
        var groups = preset.Groups ?? Array.Empty<TitleBrowseGroup>();
        if (groups.Length == 0)
        {
            return [];
        }

        Dictionary<string, int>? counts = null;
        if (hideEmpty && store is not null && library.Id != Guid.Empty)
        {
            counts = GetCombinedGroupCounts(store, library.Id);
        }

        var items = new List<ServerItem>(groups.Length);
        foreach (var group in groups)
        {
            if (hideEmpty && counts is not null)
            {
                var count = counts.TryGetValue(group.Id, out var value) ? value : 0;
                if (count == 0 && group.MatchMode != TitleBrowseMatchMode.Other)
                {
                    continue;
                }
            }

            items.Add(new ServerItem(library, StubType.BrowseByKanaRow, library.Id, titleBrowseGroupId: group.Id));
        }

        return items.ToArray();
    }

    /// <summary>
    /// Resolves a legacy kana row index to a group id using the japanese-kana preset layout.
    /// </summary>
    public static string LegacyRowIndexToGroupId(int rowIndex)
        => TitleBrowsePresetDefaults.LegacyRowIndexToGroupId(rowIndex);

    /// <summary>
    /// Resolves a group id from a legacy kana row client id index.
    /// </summary>
    public static string ResolveGroupIdFromLegacyRowIndex(int rowIndex, TitleBrowsePreset preset)
    {
        var legacyId = LegacyRowIndexToGroupId(rowIndex);
        if (preset.Groups.Any(g => string.Equals(g.Id, legacyId, StringComparison.Ordinal)))
        {
            return legacyId;
        }

        if (rowIndex >= 0 && rowIndex < preset.Groups.Length)
        {
            return preset.Groups[rowIndex].Id;
        }

        return TitleBrowsePresetDefaults.OtherGroupId;
    }

    private static Dictionary<string, int> GetCombinedGroupCounts(IVirtualIndexStore store, Guid libraryId)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        AddCounts(store.GetTitleBrowseGroupCounts(libraryId, BaseItemKind.Series), counts);
        AddCounts(store.GetTitleBrowseGroupCounts(libraryId, BaseItemKind.Movie), counts);
        return counts;
    }

    private static void AddCounts(IReadOnlyList<TitleBrowseGroupCount> source, Dictionary<string, int> target)
    {
        foreach (var entry in source)
        {
            target.TryGetValue(entry.GroupId, out var existing);
            target[entry.GroupId] = existing + entry.Count;
        }
    }
}
