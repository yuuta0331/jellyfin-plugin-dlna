using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Configuration;
using Microsoft.Data.Sqlite;

namespace Jellyfin.Plugin.Dlna.Indexing;

/// <summary>
/// SQLite-backed virtual index storage.
/// </summary>
public sealed class VirtualIndexStore : IVirtualIndexStore, IDisposable
{
    private readonly string _connectionString;
    private readonly string _dbPath;
    private readonly Lock _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualIndexStore"/> class.
    /// </summary>
    /// <param name="applicationPaths">Application paths.</param>
    public VirtualIndexStore(IApplicationPaths applicationPaths)
    {
        var dbDir = Path.Combine(applicationPaths.PluginConfigurationsPath, "dlna");
        _dbPath = Path.Combine(dbDir, "dlna-index.db");
        Directory.CreateDirectory(dbDir);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        InitializeSchema();
    }

    /// <inheritdoc />
    public string DatabasePath => _dbPath;

    /// <inheritdoc />
    public IndexStoreStatistics GetStatistics()
    {
        var fileSize = File.Exists(_dbPath) ? new FileInfo(_dbPath).Length : 0L;
        lock (_lock)
        {
            using var connection = OpenConnection();
            var indexedLibraries = new List<Guid>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT library_id FROM library_indexed";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (Guid.TryParse(reader.GetString(0), out var libraryId))
                    {
                        indexedLibraries.Add(libraryId);
                    }
                }
            }

            return new IndexStoreStatistics(
                _dbPath,
                fileSize,
                CountRows(connection, "library_indexed"),
                CountRows(connection, "item_summary"),
                CountRows(connection, "virtual_list"),
                CountRows(connection, "kana_row"),
                CountRows(connection, "facet_index"),
                indexedLibraries);
        }
    }

    /// <inheritdoc />
    public void ClearAll()
    {
        lock (_lock)
        {
            using var connection = OpenConnection();
            ExecuteNonQuery(connection, "DELETE FROM library_indexed");
            ExecuteNonQuery(connection, "DELETE FROM item_summary");
            ExecuteNonQuery(connection, "DELETE FROM virtual_list");
            ExecuteNonQuery(connection, "DELETE FROM kana_row");
            ExecuteNonQuery(connection, "DELETE FROM facet_index");
        }
    }

    /// <inheritdoc />
    public void ClearLibrary(Guid libraryId)
    {
        var library = libraryId.ToString("N", CultureInfo.InvariantCulture);
        lock (_lock)
        {
            using var connection = OpenConnection();
            ExecuteNonQuery(connection, "DELETE FROM library_indexed WHERE library_id = $id", ("$id", library));
            ExecuteNonQuery(connection, "DELETE FROM item_summary WHERE library_id = $id", ("$id", library));
            ExecuteNonQuery(connection, "DELETE FROM virtual_list WHERE library_id = $id", ("$id", library));
            ExecuteNonQuery(connection, "DELETE FROM kana_row WHERE library_id = $id", ("$id", library));
            ExecuteNonQuery(connection, "DELETE FROM facet_index WHERE library_id = $id", ("$id", library));
        }
    }

    /// <inheritdoc />
    public bool IsLibraryIndexed(Guid libraryId)
    {
        var library = libraryId.ToString("N", CultureInfo.InvariantCulture);
        lock (_lock)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM library_indexed WHERE library_id = $id LIMIT 1";
            command.Parameters.AddWithValue("$id", library);
            return command.ExecuteScalar() is not null;
        }
    }

    /// <inheritdoc />
    public void MarkLibraryIndexed(Guid libraryId)
    {
        var library = libraryId.ToString("N", CultureInfo.InvariantCulture);
        lock (_lock)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT OR REPLACE INTO library_indexed(library_id, indexed_utc) VALUES ($id, $utc)";
            command.Parameters.AddWithValue("$id", library);
            command.Parameters.AddWithValue("$utc", DateTime.UtcNow.Ticks);
            command.ExecuteNonQuery();
        }
    }

    /// <inheritdoc />
    public void ReplaceItemSummaries(Guid libraryId, IReadOnlyList<ItemSummaryRecord> summaries)
    {
        var library = libraryId.ToString("N", CultureInfo.InvariantCulture);
        lock (_lock)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            ExecuteNonQuery(connection, "DELETE FROM item_summary WHERE library_id = $id", ("$id", library));
            foreach (var summary in summaries)
            {
                using var command = connection.CreateCommand();
                command.CommandText = """
                    INSERT INTO item_summary(library_id, item_id, item_type, name, sort_name, parent_id, production_year, date_created_ticks, premiere_date_ticks, index_number, is_folder, date_modified_ticks, primary_image_item_id, primary_image_tag, primary_width, primary_height, thumb_image_item_id, thumb_image_tag, thumb_width, thumb_height, runtime_ticks, file_size, container, video_width, video_height, total_bitrate, video_codec, audio_codec, media_source_id, media_source_tag, supports_direct_play)
                    VALUES ($library_id, $item_id, $item_type, $name, $sort_name, $parent_id, $production_year, $date_created_ticks, $premiere_date_ticks, $index_number, $is_folder, $date_modified_ticks, $primary_image_item_id, $primary_image_tag, $primary_width, $primary_height, $thumb_image_item_id, $thumb_image_tag, $thumb_width, $thumb_height, $runtime_ticks, $file_size, $container, $video_width, $video_height, $total_bitrate, $video_codec, $audio_codec, $media_source_id, $media_source_tag, $supports_direct_play)
                    """;
                command.Parameters.AddWithValue("$library_id", library);
                command.Parameters.AddWithValue("$item_id", summary.ItemId.ToString("N", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$item_type", summary.ItemType.ToString());
                command.Parameters.AddWithValue("$name", summary.Name);
                command.Parameters.AddWithValue("$sort_name", summary.SortName);
                command.Parameters.AddWithValue("$parent_id", summary.ParentId.ToString("N", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$production_year", summary.ProductionYear ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$date_created_ticks", summary.DateCreatedTicks);
                command.Parameters.AddWithValue("$premiere_date_ticks", summary.PremiereDateTicks ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$index_number", summary.IndexNumber ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$is_folder", summary.IsFolder ? 1 : 0);
                command.Parameters.AddWithValue("$date_modified_ticks", summary.DateModifiedTicks ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$primary_image_item_id", summary.PrimaryImageItemId?.ToString("N", CultureInfo.InvariantCulture) ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$primary_image_tag", summary.PrimaryImageTag ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$primary_width", summary.PrimaryWidth ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$primary_height", summary.PrimaryHeight ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$thumb_image_item_id", summary.ThumbImageItemId?.ToString("N", CultureInfo.InvariantCulture) ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$thumb_image_tag", summary.ThumbImageTag ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$thumb_width", summary.ThumbWidth ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$thumb_height", summary.ThumbHeight ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$runtime_ticks", summary.RunTimeTicks ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$file_size", summary.FileSize ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$container", summary.Container ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$video_width", summary.VideoWidth ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$video_height", summary.VideoHeight ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$total_bitrate", summary.TotalBitrate ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$video_codec", summary.VideoCodec ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$audio_codec", summary.AudioCodec ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$media_source_id", summary.MediaSourceId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$media_source_tag", summary.MediaSourceTag ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$supports_direct_play", summary.SupportsDirectPlay ? 1 : 0);
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<Guid, ItemSummaryRecord> GetItemSummaries(Guid libraryId, IReadOnlyList<Guid> itemIds)
    {
        if (itemIds.Count == 0)
        {
            return new Dictionary<Guid, ItemSummaryRecord>();
        }

        var library = libraryId.ToString("N", CultureInfo.InvariantCulture);
        var results = new Dictionary<Guid, ItemSummaryRecord>(itemIds.Count);
        lock (_lock)
        {
            using var connection = OpenConnection();
            foreach (var chunk in itemIds.Chunk(500))
            {
                using var command = connection.CreateCommand();
                var placeholders = new List<string>();
                var index = 0;
                command.Parameters.AddWithValue("$library_id", library);
                foreach (var itemId in chunk)
                {
                    var param = "$id" + index.ToString(CultureInfo.InvariantCulture);
                    placeholders.Add(param);
                    command.Parameters.AddWithValue(param, itemId.ToString("N", CultureInfo.InvariantCulture));
                    index++;
                }

#pragma warning disable CA2100
                command.CommandText = $"""
                    SELECT item_id, item_type, name, sort_name, parent_id, production_year, date_created_ticks, premiere_date_ticks, index_number, is_folder, date_modified_ticks, primary_image_item_id, primary_image_tag, primary_width, primary_height, thumb_image_item_id, thumb_image_tag, thumb_width, thumb_height, runtime_ticks, file_size, container, video_width, video_height, total_bitrate, video_codec, audio_codec, media_source_id, media_source_tag, supports_direct_play
                    FROM item_summary
                    WHERE library_id = $library_id AND item_id IN ({string.Join(',', placeholders)})
                    """;
#pragma warning restore CA2100
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (!Guid.TryParse(reader.GetString(0), out var itemGuid))
                    {
                        continue;
                    }

                    if (!Enum.TryParse(reader.GetString(1), out BaseItemKind itemType))
                    {
                        continue;
                    }
                    results[itemGuid] = new ItemSummaryRecord
                    {
                        ItemId = itemGuid,
                        ItemType = itemType,
                        Name = reader.GetString(2),
                        SortName = reader.GetString(3),
                        ParentId = Guid.TryParse(reader.GetString(4), out var parentId) ? parentId : Guid.Empty,
                        ProductionYear = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                        DateCreatedTicks = reader.GetInt64(6),
                        PremiereDateTicks = reader.IsDBNull(7) ? null : reader.GetInt64(7),
                        IndexNumber = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                        IsFolder = !reader.IsDBNull(9) && reader.GetInt32(9) != 0,
                        DateModifiedTicks = reader.IsDBNull(10) ? null : reader.GetInt64(10),
                        PrimaryImageItemId = ReadOptionalGuid(reader, 11),
                        PrimaryImageTag = reader.IsDBNull(12) ? null : reader.GetString(12),
                        PrimaryWidth = reader.IsDBNull(13) ? null : reader.GetInt32(13),
                        PrimaryHeight = reader.IsDBNull(14) ? null : reader.GetInt32(14),
                        ThumbImageItemId = ReadOptionalGuid(reader, 15),
                        ThumbImageTag = reader.IsDBNull(16) ? null : reader.GetString(16),
                        ThumbWidth = reader.IsDBNull(17) ? null : reader.GetInt32(17),
                        ThumbHeight = reader.IsDBNull(18) ? null : reader.GetInt32(18),
                        RunTimeTicks = reader.IsDBNull(19) ? null : reader.GetInt64(19),
                        FileSize = reader.IsDBNull(20) ? null : reader.GetInt64(20),
                        Container = reader.IsDBNull(21) ? null : reader.GetString(21),
                        VideoWidth = reader.IsDBNull(22) ? null : reader.GetInt32(22),
                        VideoHeight = reader.IsDBNull(23) ? null : reader.GetInt32(23),
                        TotalBitrate = reader.IsDBNull(24) ? null : reader.GetInt32(24),
                        VideoCodec = reader.IsDBNull(25) ? null : reader.GetString(25),
                        AudioCodec = reader.IsDBNull(26) ? null : reader.GetString(26),
                        MediaSourceId = reader.IsDBNull(27) ? null : reader.GetString(27),
                        MediaSourceTag = reader.IsDBNull(28) ? null : reader.GetString(28),
                        SupportsDirectPlay = !reader.IsDBNull(29) && reader.GetInt32(29) != 0
                    };
                }
            }
        }

        return results;
    }

    /// <inheritdoc />
    public void ReplaceVirtualList(Guid libraryId, VirtualListType listType, IReadOnlyList<Guid> itemIds)
    {
        ReplaceOrderedList("virtual_list", libraryId, listType.ToString(), null, null, itemIds);
    }

    /// <inheritdoc />
    public IReadOnlyList<Guid> GetVirtualList(Guid libraryId, VirtualListType listType)
        => GetOrderedList("virtual_list", libraryId, listType.ToString(), null, null);

    /// <inheritdoc />
    public void ReplaceTitleBrowseGroup(Guid libraryId, BaseItemKind itemType, string groupId, IReadOnlyList<Guid> itemIds)
        => ReplaceOrderedList("kana_row", libraryId, itemType.ToString(), groupId, null, itemIds);

    /// <inheritdoc />
    public IReadOnlyList<Guid> GetTitleBrowseGroup(Guid libraryId, BaseItemKind itemType, string groupId)
        => GetOrderedList("kana_row", libraryId, itemType.ToString(), groupId, null);

    /// <inheritdoc />
    public IReadOnlyList<TitleBrowseGroupCount> GetTitleBrowseGroupCounts(Guid libraryId, BaseItemKind itemType)
    {
        var library = libraryId.ToString("N", CultureInfo.InvariantCulture);
        var results = new List<TitleBrowseGroupCount>();
        lock (_lock)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT row_index, COUNT(*) AS item_count
                FROM kana_row
                WHERE library_id = $library_id AND item_type = $item_type
                GROUP BY row_index
                ORDER BY row_index
                """;
            command.Parameters.AddWithValue("$library_id", library);
            command.Parameters.AddWithValue("$item_type", itemType.ToString());
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new TitleBrowseGroupCount(reader.GetString(0), reader.GetInt32(1)));
            }
        }

        return results;
    }

    /// <inheritdoc />
    public int GetVirtualListCount(Guid libraryId, VirtualListType listType)
        => GetVirtualList(libraryId, listType).Count;

    /// <inheritdoc />
    [Obsolete("Use ReplaceTitleBrowseGroup instead.")]
    public void ReplaceKanaRow(Guid libraryId, BaseItemKind itemType, int rowIndex, IReadOnlyList<Guid> itemIds)
        => ReplaceTitleBrowseGroup(libraryId, itemType, rowIndex.ToString(CultureInfo.InvariantCulture), itemIds);

    /// <inheritdoc />
    [Obsolete("Use GetTitleBrowseGroup instead.")]
    public IReadOnlyList<Guid> GetKanaRow(Guid libraryId, BaseItemKind itemType, int rowIndex)
        => GetTitleBrowseGroup(libraryId, itemType, rowIndex.ToString(CultureInfo.InvariantCulture));

    /// <inheritdoc />
    public void ReplaceFacets(Guid libraryId, FacetType facetType, IReadOnlyDictionary<string, IReadOnlyList<Guid>> entries)
    {
        var library = libraryId.ToString("N", CultureInfo.InvariantCulture);
        var facet = facetType.ToString();
        lock (_lock)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            using (var delete = connection.CreateCommand())
            {
                delete.CommandText = "DELETE FROM facet_index WHERE library_id = $library_id AND facet_type = $facet_type";
                delete.Parameters.AddWithValue("$library_id", library);
                delete.Parameters.AddWithValue("$facet_type", facet);
                delete.ExecuteNonQuery();
            }

            foreach (var entry in entries)
            {
                var position = 0;
                foreach (var itemId in entry.Value)
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = """
                        INSERT INTO facet_index(library_id, facet_type, facet_key, item_id, sort_position)
                        VALUES ($library_id, $facet_type, $facet_key, $item_id, $sort_position)
                        """;
                    command.Parameters.AddWithValue("$library_id", library);
                    command.Parameters.AddWithValue("$facet_type", facet);
                    command.Parameters.AddWithValue("$facet_key", entry.Key);
                    command.Parameters.AddWithValue("$item_id", itemId.ToString("N", CultureInfo.InvariantCulture));
                    command.Parameters.AddWithValue("$sort_position", position++);
                    command.ExecuteNonQuery();
                }
            }

            transaction.Commit();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<FacetKeyCount> GetFacetKeys(Guid libraryId, FacetType facetType)
    {
        var library = libraryId.ToString("N", CultureInfo.InvariantCulture);
        var results = new List<FacetKeyCount>();
        lock (_lock)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT facet_key, COUNT(*) AS item_count
                FROM facet_index
                WHERE library_id = $library_id AND facet_type = $facet_type
                GROUP BY facet_key
                ORDER BY facet_key
                """;
            command.Parameters.AddWithValue("$library_id", library);
            command.Parameters.AddWithValue("$facet_type", facetType.ToString());
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new FacetKeyCount(reader.GetString(0), reader.GetInt32(1)));
            }
        }

        return results;
    }

    /// <inheritdoc />
    public IReadOnlyList<Guid> GetFacetItems(Guid libraryId, FacetType facetType, string facetKey)
        => GetOrderedList("facet_index", libraryId, facetType.ToString(), facetKey, "facet_key");

    /// <inheritdoc />
    public int GetSeriesCount(Guid libraryId)
        => GetVirtualList(libraryId, VirtualListType.SeriesAll).Count;

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private void InitializeSchema()
    {
        lock (_lock)
        {
            using var connection = OpenConnection();
            ExecuteNonQuery(connection, """
                CREATE TABLE IF NOT EXISTS library_indexed(
                    library_id TEXT NOT NULL PRIMARY KEY,
                    indexed_utc INTEGER NOT NULL)
                """);
            ExecuteNonQuery(connection, """
                CREATE TABLE IF NOT EXISTS item_summary(
                    library_id TEXT NOT NULL,
                    item_id TEXT NOT NULL,
                    item_type TEXT NOT NULL,
                    name TEXT NOT NULL,
                    sort_name TEXT NOT NULL,
                    parent_id TEXT NOT NULL,
                    production_year INTEGER NULL,
                    date_created_ticks INTEGER NOT NULL,
                    premiere_date_ticks INTEGER NULL,
                    index_number INTEGER NULL,
                    is_folder INTEGER NOT NULL DEFAULT 0,
                    date_modified_ticks INTEGER NULL,
                    PRIMARY KEY(library_id, item_id))
                """);
            EnsureColumn(connection, "item_summary", "index_number", "INTEGER NULL");
            EnsureColumn(connection, "item_summary", "is_folder", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumn(connection, "item_summary", "date_modified_ticks", "INTEGER NULL");
            EnsureColumn(connection, "item_summary", "primary_image_item_id", "TEXT NULL");
            EnsureColumn(connection, "item_summary", "primary_image_tag", "TEXT NULL");
            EnsureColumn(connection, "item_summary", "primary_width", "INTEGER NULL");
            EnsureColumn(connection, "item_summary", "primary_height", "INTEGER NULL");
            EnsureColumn(connection, "item_summary", "thumb_image_item_id", "TEXT NULL");
            EnsureColumn(connection, "item_summary", "thumb_image_tag", "TEXT NULL");
            EnsureColumn(connection, "item_summary", "thumb_width", "INTEGER NULL");
            EnsureColumn(connection, "item_summary", "thumb_height", "INTEGER NULL");
            EnsureColumn(connection, "item_summary", "runtime_ticks", "INTEGER NULL");
            EnsureColumn(connection, "item_summary", "file_size", "INTEGER NULL");
            EnsureColumn(connection, "item_summary", "container", "TEXT NULL");
            EnsureColumn(connection, "item_summary", "video_width", "INTEGER NULL");
            EnsureColumn(connection, "item_summary", "video_height", "INTEGER NULL");
            EnsureColumn(connection, "item_summary", "total_bitrate", "INTEGER NULL");
            EnsureColumn(connection, "item_summary", "video_codec", "TEXT NULL");
            EnsureColumn(connection, "item_summary", "audio_codec", "TEXT NULL");
            EnsureColumn(connection, "item_summary", "media_source_id", "TEXT NULL");
            EnsureColumn(connection, "item_summary", "media_source_tag", "TEXT NULL");
            EnsureColumn(connection, "item_summary", "supports_direct_play", "INTEGER NOT NULL DEFAULT 0");
            ExecuteNonQuery(connection, """
                CREATE TABLE IF NOT EXISTS virtual_list(
                    library_id TEXT NOT NULL,
                    list_type TEXT NOT NULL,
                    item_id TEXT NOT NULL,
                    sort_position INTEGER NOT NULL,
                    PRIMARY KEY(library_id, list_type, item_id))
                """);
            ExecuteNonQuery(connection, """
                CREATE TABLE IF NOT EXISTS kana_row(
                    library_id TEXT NOT NULL,
                    item_type TEXT NOT NULL,
                    row_index TEXT NOT NULL,
                    item_id TEXT NOT NULL,
                    sort_position INTEGER NOT NULL,
                    PRIMARY KEY(library_id, item_type, row_index, item_id))
                """);
            ExecuteNonQuery(connection, """
                CREATE TABLE IF NOT EXISTS facet_index(
                    library_id TEXT NOT NULL,
                    facet_type TEXT NOT NULL,
                    facet_key TEXT NOT NULL,
                    item_id TEXT NOT NULL,
                    sort_position INTEGER NOT NULL,
                    PRIMARY KEY(library_id, facet_type, facet_key, item_id))
                """);
        }
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static void ExecuteNonQuery(SqliteConnection connection, string sql, params (string Name, object Value)[] parameters)
    {
        using var command = connection.CreateCommand();
#pragma warning disable CA2100 // SQL is built from fixed table/column names in this class
        command.CommandText = sql;
#pragma warning restore CA2100
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        command.ExecuteNonQuery();
    }

    private void ReplaceOrderedList(string table, Guid libraryId, string key1, string? key2, string? key2Column, IReadOnlyList<Guid> itemIds)
    {
        var library = libraryId.ToString("N", CultureInfo.InvariantCulture);
        lock (_lock)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            using (var delete = connection.CreateCommand())
            {
#pragma warning disable CA2100
                if (table == "virtual_list")
                {
                    delete.CommandText = $"DELETE FROM {table} WHERE library_id = $library_id AND list_type = $key1";
                    delete.Parameters.AddWithValue("$library_id", library);
                    delete.Parameters.AddWithValue("$key1", key1);
                }
                else if (table == "kana_row")
                {
                    delete.CommandText = $"DELETE FROM {table} WHERE library_id = $library_id AND item_type = $key1 AND row_index = $key2";
                    delete.Parameters.AddWithValue("$library_id", library);
                    delete.Parameters.AddWithValue("$key1", key1);
                    delete.Parameters.AddWithValue("$key2", key2!);
                }
                else
                {
                    delete.CommandText = $"DELETE FROM {table} WHERE library_id = $library_id AND facet_type = $key1 AND facet_key = $key2";
                    delete.Parameters.AddWithValue("$library_id", library);
                    delete.Parameters.AddWithValue("$key1", key1);
                    delete.Parameters.AddWithValue("$key2", key2!);
                }
#pragma warning restore CA2100

                delete.ExecuteNonQuery();
            }

            var position = 0;
            foreach (var itemId in itemIds)
            {
                using var command = connection.CreateCommand();
                if (table == "virtual_list")
                {
                    command.CommandText = "INSERT INTO virtual_list(library_id, list_type, item_id, sort_position) VALUES ($library_id, $list_type, $item_id, $sort_position)";
                    command.Parameters.AddWithValue("$library_id", library);
                    command.Parameters.AddWithValue("$list_type", key1);
                }
                else if (table == "kana_row")
                {
                    command.CommandText = "INSERT INTO kana_row(library_id, item_type, row_index, item_id, sort_position) VALUES ($library_id, $item_type, $row_index, $item_id, $sort_position)";
                    command.Parameters.AddWithValue("$library_id", library);
                    command.Parameters.AddWithValue("$item_type", key1);
                    command.Parameters.AddWithValue("$row_index", key2!);
                }
                else
                {
                    command.CommandText = "INSERT INTO facet_index(library_id, facet_type, facet_key, item_id, sort_position) VALUES ($library_id, $facet_type, $facet_key, $item_id, $sort_position)";
                    command.Parameters.AddWithValue("$library_id", library);
                    command.Parameters.AddWithValue("$facet_type", key1);
                    command.Parameters.AddWithValue("$facet_key", key2!);
                }

                command.Parameters.AddWithValue("$item_id", itemId.ToString("N", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$sort_position", position++);
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
    }

    private IReadOnlyList<Guid> GetOrderedList(string table, Guid libraryId, string key1, string? key2, string? key2Column)
    {
        var library = libraryId.ToString("N", CultureInfo.InvariantCulture);
        var results = new List<Guid>();
        lock (_lock)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            if (table == "virtual_list")
            {
                command.CommandText = "SELECT item_id FROM virtual_list WHERE library_id = $library_id AND list_type = $key1 ORDER BY sort_position";
                command.Parameters.AddWithValue("$library_id", library);
                command.Parameters.AddWithValue("$key1", key1);
            }
            else if (table == "kana_row")
            {
                command.CommandText = "SELECT item_id FROM kana_row WHERE library_id = $library_id AND item_type = $key1 AND row_index = $key2 ORDER BY sort_position";
                command.Parameters.AddWithValue("$library_id", library);
                command.Parameters.AddWithValue("$key1", key1);
                command.Parameters.AddWithValue("$key2", key2!);
            }
            else
            {
                command.CommandText = "SELECT item_id FROM facet_index WHERE library_id = $library_id AND facet_type = $key1 AND facet_key = $key2 ORDER BY sort_position";
                command.Parameters.AddWithValue("$library_id", library);
                command.Parameters.AddWithValue("$key1", key1);
                command.Parameters.AddWithValue("$key2", key2!);
            }

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (Guid.TryParse(reader.GetString(0), out var itemId))
                {
                    results.Add(itemId);
                }
            }
        }

        return results;
    }

    private static int CountRows(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
#pragma warning disable CA2100
        command.CommandText = $"SELECT COUNT(*) FROM {tableName}";
#pragma warning restore CA2100
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static Guid? ReadOptionalGuid(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        return Guid.TryParse(reader.GetString(ordinal), out var guid) ? guid : null;
    }

    private static void EnsureColumn(SqliteConnection connection, string table, string column, string definition)
    {
        using var check = connection.CreateCommand();
#pragma warning disable CA2100
        check.CommandText = $"PRAGMA table_info({table})";
#pragma warning restore CA2100
        using var reader = check.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        ExecuteNonQuery(connection, $"ALTER TABLE {table} ADD COLUMN {column} {definition}");
    }
}
