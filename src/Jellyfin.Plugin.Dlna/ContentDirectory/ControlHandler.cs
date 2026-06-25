using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Xml;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Plugin.Dlna.Configuration;
using Jellyfin.Plugin.Dlna.Didl;
using Jellyfin.Plugin.Dlna.Indexing;
using Jellyfin.Plugin.Dlna.Model;
using Jellyfin.Plugin.Dlna.Service;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;
using DeviceProfile = MediaBrowser.Model.Dlna.DeviceProfile;
using Genre = MediaBrowser.Controller.Entities.Genre;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Defines the <see cref="ControlHandler" />.
/// </summary>
public class ControlHandler : BaseControlHandler
{
    private const string NsDc = "http://purl.org/dc/elements/1.1/";
    private const string NsDidl = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/";
    private const string NsDlna = "urn:schemas-dlna-org:metadata-1-0/";
    private const string NsUpnp = "urn:schemas-upnp-org:metadata-1-0/upnp/";

    private readonly ILibraryManager _libraryManager;
    private readonly IUserDataManager _userDataManager;
    private readonly User? _user;
    private readonly ITVSeriesManager _tvSeriesManager;

    private readonly IBrowseResponseCache _browseResponseCache;
    private readonly IBrowseNodeCache _browseNodeCache;
    private readonly ChildCountCache _childCountCache;
    private readonly LibraryChangeNotifier _libraryChangeNotifier;
    private readonly IVirtualIndexStore _indexStore;
    private readonly IDlnaVirtualIndexService _indexService;
    private readonly IBrowseMetrics _browseMetrics;

    private readonly int _systemUpdateId;

    private readonly DidlBuilder _didlBuilder;

    private readonly DlnaDeviceProfile _profile;

    private readonly string _serverAddress;

    private BrowsePagingContext? _browsePaging;

    /// <summary>
    /// Initializes a new instance of the <see cref="ControlHandler"/> class.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="profile">The <see cref="DeviceProfile"/>.</param>
    /// <param name="serverAddress">The server address.</param>
    /// <param name="accessToken">The access token.</param>
    /// <param name="imageProcessor">Instance of the <see cref="IImageProcessor"/> interface.</param>
    /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
    /// <param name="user">The <see cref="User"/>.</param>
    /// <param name="systemUpdateId">The system id.</param>
    /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
    /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
    /// <param name="userViewManager">Instance of the <see cref="IUserViewManager"/> interface.</param>
    /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
    /// <param name="tvSeriesManager">Instance of the <see cref="ITVSeriesManager"/> interface.</param>
    /// <param name="browseResponseCache">The browse response cache.</param>
    /// <param name="browseNodeCache">The browse node cache.</param>
    /// <param name="childCountCache">The child count cache.</param>
    /// <param name="libraryChangeNotifier">The library change notifier.</param>
    /// <param name="indexStore">The virtual index store.</param>
    /// <param name="indexService">The virtual index service.</param>
    /// <param name="browseMetrics">The browse metrics collector.</param>
    public ControlHandler(
        ILogger logger,
        ILibraryManager libraryManager,
        DlnaDeviceProfile profile,
        string serverAddress,
        string? accessToken,
        IImageProcessor imageProcessor,
        IUserDataManager userDataManager,
        User? user,
        int systemUpdateId,
        ILocalizationManager localization,
        IMediaSourceManager mediaSourceManager,
        IUserViewManager userViewManager,
        IMediaEncoder mediaEncoder,
        ITVSeriesManager tvSeriesManager,
        IBrowseResponseCache browseResponseCache,
        IBrowseNodeCache browseNodeCache,
        ChildCountCache childCountCache,
        LibraryChangeNotifier libraryChangeNotifier,
        IVirtualIndexStore indexStore,
        IDlnaVirtualIndexService indexService,
        IBrowseMetrics browseMetrics)
        : base(logger)
    {
        _libraryManager = libraryManager;
        _userDataManager = userDataManager;
        _user = user;
        _systemUpdateId = systemUpdateId;
        _ = userViewManager;
        _tvSeriesManager = tvSeriesManager;
        _browseResponseCache = browseResponseCache;
        _browseNodeCache = browseNodeCache;
        _childCountCache = childCountCache;
        _libraryChangeNotifier = libraryChangeNotifier;
        _indexStore = indexStore;
        _indexService = indexService;
        _browseMetrics = browseMetrics;
        _profile = profile;
        _serverAddress = serverAddress;

        _didlBuilder = new DidlBuilder(
            profile,
            user,
            imageProcessor,
            serverAddress,
            accessToken,
            userDataManager,
            localization,
            mediaSourceManager,
            Logger,
            mediaEncoder,
            libraryManager);
    }

    /// <inheritdoc />
    protected override void WriteResult(string methodName, IReadOnlyDictionary<string, string> methodParams, XmlWriter xmlWriter)
    {
        ArgumentNullException.ThrowIfNull(xmlWriter);
        ArgumentNullException.ThrowIfNull(methodParams);

        const string DeviceId = "test";

        if (string.Equals(methodName, "GetSearchCapabilities", StringComparison.OrdinalIgnoreCase))
        {
            HandleGetSearchCapabilities(xmlWriter);
            return;
        }

        if (string.Equals(methodName, "GetSortCapabilities", StringComparison.OrdinalIgnoreCase))
        {
            HandleGetSortCapabilities(xmlWriter);
            return;
        }

        if (string.Equals(methodName, "GetSortExtensionCapabilities", StringComparison.OrdinalIgnoreCase))
        {
            HandleGetSortExtensionCapabilities(xmlWriter);
            return;
        }

        if (string.Equals(methodName, "GetSystemUpdateID", StringComparison.OrdinalIgnoreCase))
        {
            HandleGetSystemUpdateID(xmlWriter);
            return;
        }

        if (string.Equals(methodName, "Browse", StringComparison.OrdinalIgnoreCase))
        {
            HandleBrowse(xmlWriter, methodParams, DeviceId);
            return;
        }

        if (string.Equals(methodName, "X_GetFeatureList", StringComparison.OrdinalIgnoreCase))
        {
            HandleXGetFeatureList(xmlWriter);
            return;
        }

        if (string.Equals(methodName, "GetFeatureList", StringComparison.OrdinalIgnoreCase))
        {
            HandleGetFeatureList(xmlWriter);
            return;
        }

        if (string.Equals(methodName, "X_SetBookmark", StringComparison.OrdinalIgnoreCase))
        {
            HandleXSetBookmark(methodParams);
            return;
        }

        if (string.Equals(methodName, "Search", StringComparison.OrdinalIgnoreCase))
        {
            HandleSearch(xmlWriter, methodParams, DeviceId);
            return;
        }

        if (string.Equals(methodName, "X_BrowseByLetter", StringComparison.OrdinalIgnoreCase))
        {
            HandleXBrowseByLetter(xmlWriter, methodParams, DeviceId);
            return;
        }

        throw new ResourceNotFoundException("Unexpected control request name: " + methodName);
    }

    /// <summary>
    /// Adds a "XSetBookmark" element to the xml document.
    /// </summary>
    /// <param name="sparams">The method parameters.</param>
    private void HandleXSetBookmark(IReadOnlyDictionary<string, string> sparams)
    {
        if (_user is null)
        {
            return;
        }

        var id = sparams["ObjectID"];

        var serverItem = GetItemFromObjectId(id);

        var item = serverItem.Item;

        var newbookmark = int.Parse(sparams["PosSecond"], CultureInfo.InvariantCulture);

        var userdata = _userDataManager.GetUserData(_user, item)!;

        userdata.PlaybackPositionTicks = TimeSpan.FromSeconds(newbookmark).Ticks;

        _userDataManager.SaveUserData(
            _user,
            item,
            userdata,
            UserDataSaveReason.TogglePlayed,
            CancellationToken.None);
    }

    /// <summary>
    /// Adds the "SearchCaps" element to the xml document.
    /// </summary>
    /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
    private static void HandleGetSearchCapabilities(XmlWriter xmlWriter)
    {
        xmlWriter.WriteElementString(
            "SearchCaps",
            "res@resolution,res@size,res@duration,dc:title,dc:creator,upnp:actor,upnp:artist,upnp:genre,upnp:album,dc:date,upnp:class,@id,@refID,@protocolInfo,upnp:author,dc:description,pv:avKeywords");
    }

    /// <summary>
    /// Adds the "SortCaps" element to the xml document.
    /// </summary>
    /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
    private static void HandleGetSortCapabilities(XmlWriter xmlWriter)
    {
        xmlWriter.WriteElementString(
            "SortCaps",
            "res@duration,res@size,res@bitrate,dc:date,dc:title,dc:size,upnp:album,upnp:artist,upnp:albumArtist,upnp:episodeNumber,upnp:genre,upnp:originalTrackNumber,upnp:rating");
    }

    /// <summary>
    /// Adds the "SortExtensionCaps" element to the xml document.
    /// </summary>
    /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
    private static void HandleGetSortExtensionCapabilities(XmlWriter xmlWriter)
    {
        xmlWriter.WriteElementString(
            "SortExtensionCaps",
            "res@duration,res@size,res@bitrate,dc:date,dc:title,dc:size,upnp:album,upnp:artist,upnp:albumArtist,upnp:episodeNumber,upnp:genre,upnp:originalTrackNumber,upnp:rating");
    }

    /// <summary>
    /// Adds the "Id" element to the xml document.
    /// </summary>
    /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
    private void HandleGetSystemUpdateID(XmlWriter xmlWriter)
    {
        xmlWriter.WriteElementString("Id", _systemUpdateId.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Adds the "FeatureList" element to the xml document.
    /// </summary>
    /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
    private static void HandleGetFeatureList(XmlWriter xmlWriter)
    {
        xmlWriter.WriteElementString("FeatureList", WriteFeatureListXml());
    }

    /// <summary>
    /// Adds the "FeatureList" element to the xml document.
    /// </summary>
    /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
    private static void HandleXGetFeatureList(XmlWriter xmlWriter)
        => HandleGetFeatureList(xmlWriter);

    /// <summary>
    /// Builds a static feature list.
    /// </summary>
    /// <returns>The xml feature list.</returns>
    private static string WriteFeatureListXml()
    {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
               + "<Features xmlns=\"urn:schemas-upnp-org:av:avs\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"urn:schemas-upnp-org:av:avs http://www.upnp.org/schemas/av/avs.xsd\">"
               + "<Feature name=\"samsung.com_BASICVIEW\" version=\"1\">"
               + "<container id=\"0\" type=\"object.item.imageItem\"/>"
               + "<container id=\"0\" type=\"object.item.audioItem\"/>"
               + "<container id=\"0\" type=\"object.item.videoItem\"/>"
               + "</Feature>"
               + "</Features>";
    }

    /// <summary>
    /// Builds the "Browse" xml response.
    /// </summary>
    /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
    /// <param name="sparams">The method parameters.</param>
    /// <param name="deviceId">The device Id to use.</param>
    private void HandleBrowse(XmlWriter xmlWriter, IReadOnlyDictionary<string, string> sparams, string deviceId)
    {
        var id = sparams["ObjectID"];
        var flag = sparams["BrowseFlag"];
        var filter = new Filter(sparams.GetValueOrDefault("Filter", "*"));
        var sortCriteriaString = sparams.GetValueOrDefault("SortCriteria", string.Empty);
        var sortCriteria = new SortCriteria(sortCriteriaString);

        var provided = 0;

        int? requestedCount = null;
        int? start = null;

        if (sparams.ContainsKey("RequestedCount") && int.TryParse(sparams["RequestedCount"], out var requestedVal) && requestedVal > 0)
        {
            requestedCount = requestedVal;
        }

        if (sparams.ContainsKey("StartingIndex") && int.TryParse(sparams["StartingIndex"], out var startVal) && startVal >= 0)
        {
            start = startVal;
        }

        _browsePaging = ResolveBrowsePaging(requestedCount, start);

        var cacheKey = BuildBrowseCacheKey(
            id,
            flag,
            sortCriteriaString,
            sparams.GetValueOrDefault("Filter", "*"),
            deviceId,
            _browsePaging);
        var isDirectChildren = string.Equals(flag, "BrowseDirectChildren", StringComparison.Ordinal);
        using var timing = new BrowseTimingScope { ObjectId = id };
        if (_browseResponseCache.TryGet(cacheKey, out var cachedEntry))
        {
            timing.CacheHit = true;
            WriteCachedBrowseResponse(xmlWriter, cachedEntry);
            DlnaPluginLog.BrowsePerformance(Logger, timing.ToLogSummary());
            _browseMetrics.RecordBrowse(BrowseCacheHitKind.Response, indexHit: false, totalMs: 0);
            return;
        }

        if (isDirectChildren
            && GetPluginConfiguration().EnableBrowseNodeCache
            && _browseNodeCache.TryGet(cacheKey, out var nodeCacheEntry))
        {
            var nodeDidl = BuildDidlFromCachedNodes(nodeCacheEntry, id, filter, deviceId);
            var nodeResponse = new BrowseCacheEntry(
                nodeDidl,
                nodeCacheEntry.Nodes.Count,
                nodeCacheEntry.TotalMatches,
                _systemUpdateId);
            _browseResponseCache.Set(cacheKey, nodeResponse);
            timing.CacheHit = true;
            WriteCachedBrowseResponse(xmlWriter, nodeResponse);
            CacheChildCountFromBrowse(id, sortCriteriaString, nodeCacheEntry.TotalMatches);
            DlnaPluginLog.BrowsePerformance(Logger, timing.ToLogSummary());
            _browseMetrics.RecordBrowse(BrowseCacheHitKind.Node, indexHit: false, totalMs: 0);
            return;
        }

        int totalCount;
        string didlXml;

        try
        {
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                CloseOutput = false,
                OmitXmlDeclaration = true,
                ConformanceLevel = ConformanceLevel.Fragment
            };

            using (StringWriter builder = new StringWriterWithEncoding(Encoding.UTF8))
            using (var writer = XmlWriter.Create(builder, settings))
            {
                writer.WriteStartElement(string.Empty, "DIDL-Lite", NsDidl);

                writer.WriteAttributeString("xmlns", "dc", null, NsDc);
                writer.WriteAttributeString("xmlns", "dlna", null, NsDlna);
                writer.WriteAttributeString("xmlns", "upnp", null, NsUpnp);

                DidlBuilder.WriteXmlRootAttributes(_profile, writer);

                var serverItem = GetItemFromObjectId(id);
                timing.StubTypeName = serverItem.StubType?.ToString();
                var item = serverItem.Item;

                if (string.Equals(flag, "BrowseMetadata", StringComparison.Ordinal))
                {
                    totalCount = 1;

                    if (item.IsDisplayedAsFolder || serverItem.StubType.HasValue)
                    {
                        var childCount = ResolveChildCount(serverItem, sortCriteria, id);
                        _didlBuilder.WriteFolderElement(writer, serverItem, null, childCount, filter, id);
                    }
                    else
                    {
                        _didlBuilder.WriteItemElement(writer, item, _user, null, null, deviceId, filter);
                    }

                    provided++;
                }
                else
                {
                    var querySw = Stopwatch.StartNew();
                    var childrenResult = ApplyBrowsePaging(GetUserItems(serverItem, _user, sortCriteria, timing));
                    querySw.Stop();
                    timing.AddQueryMs(querySw.ElapsedMilliseconds);
                    totalCount = childrenResult.TotalRecordCount;

                    provided = childrenResult.Items.Count;
                    timing.Items = provided;

                    List<BrowseNodeRecord>? nodeRecords = isDirectChildren && GetPluginConfiguration().EnableBrowseNodeCache
                        ? new List<BrowseNodeRecord>(provided)
                        : null;
                    var parentClientId = DidlBuilder.GetClientId(serverItem);
                    var imageContext = DlnaImageBrowseContextMapper.FromParent(serverItem.StubType, item);

                    foreach (var childServerItem in childrenResult.Items)
                    {
                        if (childServerItem.IsSummaryBacked)
                        {
                            var summaryChildCount = childServerItem.Summary!.IsFolder
                                ? ResolveChildCount(childServerItem, sortCriteria)
                                : null;
                            _didlBuilder.WriteSummaryElement(writer, childServerItem, item, summaryChildCount, filter, imageContext);
                            nodeRecords?.Add(_didlBuilder.CreateBrowseNodeRecord(childServerItem, item, parentClientId, summaryChildCount, imageContext));
                            continue;
                        }

                        var childItem = childServerItem.Item;
                        var displayStubType = childServerItem.StubType;

                        if (childItem.IsDisplayedAsFolder || displayStubType.HasValue)
                        {
                            var childCount = ResolveChildCount(childServerItem, sortCriteria);
                            _didlBuilder.WriteFolderElement(writer, childServerItem, item, childCount, filter, imageContext: imageContext);
                            nodeRecords?.Add(_didlBuilder.CreateBrowseNodeRecord(childServerItem, item, parentClientId, childCount, imageContext));
                        }
                        else
                        {
                            _didlBuilder.WriteItemElement(writer, childItem, _user, item, serverItem.StubType, deviceId, filter, imageContext: imageContext);
                            nodeRecords?.Add(_didlBuilder.CreateBrowseNodeRecord(childServerItem, item, parentClientId, null, imageContext));
                        }
                    }

                    if (nodeRecords is not null && timing.IndexHit && nodeRecords.Count > 0)
                    {
                        _browseNodeCache.Set(cacheKey, new BrowseNodeCacheEntry(nodeRecords, totalCount));
                    }
                }

                writer.WriteFullEndElement();
                writer.Flush();
                didlXml = builder.ToString();
                timing.XmlBytes = Encoding.UTF8.GetByteCount(didlXml);
            }
        }
        finally
        {
            _browsePaging = null;
        }

        var responseEntry = new BrowseCacheEntry(didlXml, provided, totalCount, _systemUpdateId);
        _browseResponseCache.Set(cacheKey, responseEntry);
        WriteCachedBrowseResponse(xmlWriter, responseEntry);

        if (isDirectChildren)
        {
            CacheChildCountFromBrowse(id, sortCriteriaString, totalCount);
        }

        DlnaPluginLog.BrowsePerformance(Logger, timing.ToLogSummary());
        _browseMetrics.RecordBrowse(BrowseCacheHitKind.None, indexHit: timing.IndexHit, totalMs: 0);
    }

    private BrowseCacheKey BuildBrowseCacheKey(
        string objectId,
        string browseFlag,
        string sortCriteria,
        string filter,
        string deviceId,
        BrowsePagingContext paging)
    {
        var config = GetPluginConfiguration();
        return new BrowseCacheKey(
            _user?.Id,
            objectId,
            browseFlag,
            BrowseCacheKeyNormalizer.NormalizeSortCriteria(sortCriteria),
            BrowseCacheKeyNormalizer.NormalizeFilter(filter),
            _profile.Id?.ToString() ?? deviceId,
            BrowseCacheKeyNormalizer.NormalizeServerBase(_serverAddress),
            BrowseConfigFingerprint.Compute(config),
            paging.StartIndex,
            paging.Limit);
    }

    private static void WriteCachedBrowseResponse(XmlWriter xmlWriter, BrowseCacheEntry entry)
    {
        xmlWriter.WriteElementString("Result", entry.DidlXml);
        xmlWriter.WriteElementString("NumberReturned", entry.NumberReturned.ToString(CultureInfo.InvariantCulture));
        xmlWriter.WriteElementString("TotalMatches", entry.TotalMatches.ToString(CultureInfo.InvariantCulture));
        xmlWriter.WriteElementString("UpdateID", entry.UpdateId.ToString(CultureInfo.InvariantCulture));
    }

    private string BuildDidlFromCachedNodes(
        BrowseNodeCacheEntry entry,
        string parentObjectId,
        Filter filter,
        string deviceId)
    {
        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            CloseOutput = false,
            OmitXmlDeclaration = true,
            ConformanceLevel = ConformanceLevel.Fragment
        };

        using var builder = new StringWriterWithEncoding(Encoding.UTF8);
        using (var writer = XmlWriter.Create(builder, settings))
        {
            writer.WriteStartElement(string.Empty, "DIDL-Lite", NsDidl);

            writer.WriteAttributeString("xmlns", "dc", null, NsDc);
            writer.WriteAttributeString("xmlns", "dlna", null, NsDlna);
            writer.WriteAttributeString("xmlns", "upnp", null, NsUpnp);

            DidlBuilder.WriteXmlRootAttributes(_profile, writer);

            var parentServerItem = GetItemFromObjectId(parentObjectId);
            var parentItem = parentServerItem.Item;
            var imageContext = DlnaImageBrowseContextMapper.FromParent(parentServerItem.StubType, parentItem);
            var childServerItems = ResolveCachedChildItems(entry.Nodes, parentServerItem);

            for (var i = 0; i < entry.Nodes.Count; i++)
            {
                var node = entry.Nodes[i];
                var childServerItem = childServerItems[i];

                if (childServerItem.IsSummaryBacked)
                {
                    _didlBuilder.WriteSummaryElement(writer, childServerItem, parentItem, node.ChildCount, filter, imageContext);
                    continue;
                }

                var childItem = childServerItem.Item;
                if (node.IsFolder || childItem.IsDisplayedAsFolder || childServerItem.StubType.HasValue)
                {
                    _didlBuilder.WriteFolderElement(writer, childServerItem, parentItem, node.ChildCount, filter, imageContext: imageContext);
                }
                else
                {
                    _didlBuilder.WriteItemElement(writer, childItem, _user, parentItem, parentServerItem.StubType, deviceId, filter, imageContext: imageContext);
                }
            }

            writer.WriteFullEndElement();
            writer.Flush();
        }

        return builder.ToString();
    }

    private IReadOnlyList<ServerItem> ResolveCachedChildItems(
        IReadOnlyList<BrowseNodeRecord> nodes,
        ServerItem parentServerItem)
    {
        var config = GetPluginConfiguration();
        var libraryId = LibraryBrowseQueryHelper.ResolveLibraryId(_libraryManager, parentServerItem.Item);
        var summaryIds = new List<Guid>(nodes.Count);

        foreach (var node in nodes)
        {
            if (Guid.TryParse(node.ClientId, out var itemId))
            {
                summaryIds.Add(itemId);
            }
        }

        IReadOnlyDictionary<Guid, ItemSummaryRecord>? summaries = null;
        if (config.EnableItemSummaryBrowse && libraryId != Guid.Empty && summaryIds.Count > 0)
        {
            summaries = _indexStore.GetItemSummaries(libraryId, summaryIds);
        }

        var results = new List<ServerItem>(nodes.Count);
        foreach (var node in nodes)
        {
            if (summaries is not null
                && Guid.TryParse(node.ClientId, out var itemId)
                && summaries.TryGetValue(itemId, out var summary))
            {
                results.Add(new ServerItem(summary));
            }
            else
            {
                results.Add(GetItemFromObjectId(node.ClientId));
            }
        }

        return results;
    }

    private int? ResolveChildCount(ServerItem child, SortCriteria sort, string? objectId = null)
    {
        if (child.IsSummaryBacked)
        {
            if (!child.Summary!.IsFolder)
            {
                return null;
            }

            var summaryConfig = GetPluginConfiguration();
            var summaryMode = ChildCountResolution.GetEffectiveChildCountMode(summaryConfig);
            if (summaryMode != ChildCountMode.Accurate)
            {
                var summaryClientId = objectId ?? DidlBuilder.GetClientId(child);
                var summarySortKey = sort.SortOrder.ToString();
                int? summaryCached = null;
                if (summaryConfig.EnableChildCountCache)
                {
                    summaryCached = _childCountCache.TryGet(_user?.Id, summaryClientId, summarySortKey);
                }

                return ChildCountResolution.ResolveWithoutQuery(
                    summaryConfig,
                    child.StubType.HasValue,
                    summaryCached);
            }

            var folderItem = _libraryManager.GetItemById(child.Summary.ItemId);
            if (folderItem is null)
            {
                return null;
            }

            return ResolveChildCount(new ServerItem(folderItem, child.StubType), sort, objectId);
        }

        var config = GetPluginConfiguration();
        var clientId = objectId ?? DidlBuilder.GetClientId(child);
        var sortKey = sort.SortOrder.ToString();
        int? cached = null;

        if (config.EnableChildCountCache)
        {
            cached = _childCountCache.TryGet(_user?.Id, clientId, sortKey);
        }

        var mode = ChildCountResolution.GetEffectiveChildCountMode(config);
        if (mode == ChildCountMode.Accurate && config.EnableChildCountCache && cached.HasValue)
        {
            return cached;
        }

        var withoutQuery = ChildCountResolution.ResolveWithoutQuery(
            config,
            child.StubType.HasValue,
            cached);

        if (withoutQuery.HasValue || !ChildCountResolution.RequiresAccurateQuery(config, child.StubType.HasValue))
        {
            return withoutQuery;
        }

        var count = ApplyBrowsePaging(GetUserItems(child, _user, sort)).TotalRecordCount;
        if (config.EnableChildCountCache)
        {
            _childCountCache.Set(_user?.Id, clientId, sortKey, count);
        }

        return count;
    }

    private void CacheChildCountFromBrowse(string objectId, string sortCriteria, int count)
    {
        var config = GetPluginConfiguration();
        if (!config.EnableChildCountCache)
        {
            return;
        }

        if (ChildCountResolution.GetEffectiveChildCountMode(config) == ChildCountMode.Disabled)
        {
            return;
        }

        _childCountCache.Set(_user?.Id, objectId, sortCriteria, count);
    }

    /// <summary>
    /// Builds the response to the "X_BrowseByLetter request.
    /// </summary>
    /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
    /// <param name="sparams">The method parameters.</param>
    /// <param name="deviceId">The device id.</param>
    private void HandleXBrowseByLetter(XmlWriter xmlWriter, IReadOnlyDictionary<string, string> sparams, string deviceId)
    {
        // TODO: Implement this method
        HandleSearch(xmlWriter, sparams, deviceId);
    }

    /// <summary>
    /// Builds a response to the "Search" request.
    /// </summary>
    /// <param name="xmlWriter">The xmlWriter<see cref="XmlWriter"/>.</param>
    /// <param name="sparams">The method parameters.</param>
    /// <param name="deviceId">The deviceId<see cref="string"/>.</param>
    private void HandleSearch(XmlWriter xmlWriter, IReadOnlyDictionary<string, string> sparams, string deviceId)
    {
        var searchCriteria = new SearchCriteria(sparams.GetValueOrDefault("SearchCriteria", string.Empty));
        var sortCriteria = new SortCriteria(sparams.GetValueOrDefault("SortCriteria", string.Empty));
        var filter = new Filter(sparams.GetValueOrDefault("Filter", "*"));

        // sort example: dc:title, dc:date

        // Default to null instead of 0
        // Upnp inspector sends 0 as requestedCount when it wants everything
        int? requestedCount = null;
        int? start = 0;

        if (sparams.ContainsKey("RequestedCount") && int.TryParse(sparams["RequestedCount"], out var requestedVal) && requestedVal > 0)
        {
            requestedCount = requestedVal;
        }

        if (sparams.ContainsKey("StartingIndex") && int.TryParse(sparams["StartingIndex"], out var startVal) && startVal > 0)
        {
            start = startVal;
        }

        QueryResult<BaseItem> childrenResult;
        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            CloseOutput = false,
            OmitXmlDeclaration = true,
            ConformanceLevel = ConformanceLevel.Fragment
        };

        using (StringWriter builder = new StringWriterWithEncoding(Encoding.UTF8))
        using (var writer = XmlWriter.Create(builder, settings))
        {
            writer.WriteStartElement(string.Empty, "DIDL-Lite", NsDidl);
            writer.WriteAttributeString("xmlns", "dc", null, NsDc);
            writer.WriteAttributeString("xmlns", "dlna", null, NsDlna);
            writer.WriteAttributeString("xmlns", "upnp", null, NsUpnp);

            DidlBuilder.WriteXmlRootAttributes(_profile, writer);

            var serverItem = GetItemFromObjectId(sparams["ContainerID"]);

            var item = serverItem.Item;

            childrenResult = GetChildrenSorted(item, _user, searchCriteria, sortCriteria, start, requestedCount);
            var searchImageContext = DlnaImageBrowseContext.Search;
            foreach (var i in childrenResult.Items)
            {
                if (i.IsDisplayedAsFolder)
                {
                    int? childCount = null;
                    var config = GetPluginConfiguration();
                    if (ChildCountResolution.RequiresAccurateQuery(config, isStubFolder: false))
                    {
                        childCount = GetChildrenSorted(i, _user, searchCriteria, sortCriteria, null, 0)
                            .TotalRecordCount;
                    }
                    else
                    {
                        childCount = ChildCountResolution.ResolveWithoutQuery(config, isStubFolder: false, cachedCount: null);
                    }

                    _didlBuilder.WriteFolderElement(writer, i, null, item, childCount, filter, imageContext: searchImageContext);
                }
                else
                {
                    _didlBuilder.WriteItemElement(writer, i, _user, item, serverItem.StubType, deviceId, filter, imageContext: searchImageContext);
                }
            }

            writer.WriteFullEndElement();
            writer.Flush();
            xmlWriter.WriteElementString("Result", builder.ToString());
        }

        xmlWriter.WriteElementString("NumberReturned", childrenResult.Items.Count.ToString(CultureInfo.InvariantCulture));
        xmlWriter.WriteElementString("TotalMatches", childrenResult.TotalRecordCount.ToString(CultureInfo.InvariantCulture));
        xmlWriter.WriteElementString("UpdateID", _systemUpdateId.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Returns the child items meeting the criteria.
    /// </summary>
    /// <param name="item">The <see cref="BaseItem"/>.</param>
    /// <param name="user">The <see cref="User"/>.</param>
    /// <param name="search">The <see cref="SearchCriteria"/>.</param>
    /// <param name="sort">The <see cref="SortCriteria"/>.</param>
    /// <param name="startIndex">The start index.</param>
    /// <param name="limit">The maximum number to return.</param>
    /// <returns>The <see cref="QueryResult{BaseItem}"/>.</returns>
    private static QueryResult<BaseItem> GetChildrenSorted(BaseItem item, User? user, SearchCriteria search, SortCriteria sort, int? startIndex, int? limit)
    {
        var folder = (Folder)item;

        MediaType[] mediaTypes = [];
        bool? isFolder = null;

        switch (search.SearchType)
        {
            case SearchType.Audio:
                mediaTypes = [MediaType.Audio];
                isFolder = false;
                break;
            case SearchType.Video:
                mediaTypes = [MediaType.Video];
                isFolder = false;
                break;
            case SearchType.Image:
                mediaTypes = [MediaType.Photo];
                isFolder = false;
                break;
            case SearchType.Playlist:
            case SearchType.MusicAlbum:
                isFolder = true;
                break;
        }

        return folder.GetItems(new InternalItemsQuery
        {
            Limit = limit,
            StartIndex = startIndex,
            OrderBy = GetOrderBy(sort, folder.IsPreSorted),
            User = user,
            Recursive = true,
            IsMissing = false,
            ExcludeItemTypes = [BaseItemKind.Book],
            IsFolder = isFolder,
            MediaTypes = mediaTypes,
            DtoOptions = GetBrowseListDtoOptions()
        });
    }

    /// <summary>
    /// Returns lightweight DtoOptions for browse list queries.
    /// </summary>
    /// <returns>The <see cref="DtoOptions"/>.</returns>
    private static DtoOptions GetBrowseListDtoOptions()
    {
        return new DtoOptions(false);
    }

    /// <summary>
    /// Returns full DtoOptions for playback-oriented queries.
    /// </summary>
    /// <returns>The <see cref="DtoOptions"/>.</returns>
    private static DtoOptions GetPlaybackDtoOptions()
    {
        return new DtoOptions(true);
    }

    /// <summary>
    /// Returns the User items meeting the criteria.
    /// </summary>
    /// <param name="serverItem">The browsed <see cref="ServerItem"/>.</param>
    /// <param name="user">The <see cref="User"/>.</param>
    /// <param name="sort">The <see cref="SortCriteria"/>.</param>
    /// <param name="timing">Optional browse timing scope.</param>
    /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
    private QueryResult<ServerItem> GetUserItems(ServerItem serverItem, User? user, SortCriteria sort, BrowseTimingScope? timing = null)
    {
        if (serverItem.IsSummaryBacked)
        {
            return new QueryResult<ServerItem>();
        }

        var item = serverItem.Item;
        var stubType = serverItem.StubType;

        if (user is not null)
        {
            if (stubType == StubType.Extras)
            {
                return GetExtrasItems(item, user, timing);
            }

            switch (item)
            {
                case MusicGenre:
                    return GetMusicGenreItems(item, user, sort, serverItem.LibraryScopeId, timing);
                case MusicArtist:
                    return GetMusicArtistItems(item, user, sort);
                case Genre:
                    return GetGenreItems(item, user, sort, serverItem.LibraryScopeId, timing);
            }

            if (LibraryBrowseQueryHelper.ShouldRouteLibraryByCollectionType(item, stubType))
            {
                if (LibraryBrowseQueryHelper.IsMixedLibrary(item))
                {
                    return GetMixedFolders(serverItem, user, sort, timing);
                }

                if (item is not IHasCollectionType collectionFolder)
                {
                    return new QueryResult<ServerItem>();
                }

                switch (collectionFolder.CollectionType)
                {
                    case CollectionType.music:
                        return GetMusicFolders(item, user, stubType, sort, timing);
                    case CollectionType.movies:
                        return GetMovieFolders(serverItem, user, sort, timing);
                    case CollectionType.tvshows:
                        return GetTvFolders(serverItem, user, sort, timing);
                    case CollectionType.homevideos:
                        return GetHomeVideoFolders(serverItem, user, sort, timing);
                    case CollectionType.musicvideos:
                        return GetMusicVideoFolders(serverItem, user, sort, timing);
                    case CollectionType.folders:
                        return GetFolders(user);
                    case CollectionType.livetv:
                        return GetLiveTvChannels(user, sort);
                }
            }
        }

        if (stubType.HasValue && stubType.Value != StubType.Folder)
        {
            // TODO should this be doing something?
            return new QueryResult<ServerItem>();
        }

        if (LibraryBrowseQueryHelper.IsDlnaLibraryView(item) && item is IHasCollectionType libraryType)
        {
            Logger.LogInformation(
                "DLNA library browse using physical children (no virtual folder route): {LibraryName} CollectionType={CollectionType} StubType={StubType}",
                item.Name,
                libraryType.CollectionType?.ToString() ?? "null",
                stubType?.ToString() ?? "none");
        }

        var folder = (Folder)item;

        var query = new InternalItemsQuery(user)
        {
            IsVirtualItem = false,
            ExcludeItemTypes = [BaseItemKind.Book],
            IsPlaceHolder = false,
            DtoOptions = GetBrowseListDtoOptions(),
            OrderBy = GetOrderBy(sort, folder.IsPreSorted)
        };

        var indexedFolderResult = TryGetIndexedSeriesOrSeasonChildren(item, user, query, timing);
        if (indexedFolderResult is not null)
        {
            return indexedFolderResult;
        }

        var queryResult = folder.GetItems(query);
        var result = ToResult(null, queryResult);

        return AppendSeriesSeasonExtrasStub(item, user, result);
    }

    /// <summary>
    /// Returns the Live Tv Channels meeting the criteria.
    /// </summary>
    /// <param name="user">The <see cref="User"/>.</param>
    /// <param name="sort">The <see cref="SortCriteria"/>.</param>
    /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
    private QueryResult<ServerItem> GetLiveTvChannels(User user, SortCriteria sort)
    {
        var query = new InternalItemsQuery(user)
        {
            IncludeItemTypes = [BaseItemKind.LiveTvChannel],
            OrderBy = GetOrderBy(sort, false)
        };

        var result = _libraryManager.GetItemsResult(query);

        return ToResult(null, result);
    }

    /// <summary>
    /// Returns the music folders meeting the criteria.
    /// </summary>
    /// <param name="item">The <see cref="BaseItem"/>.</param>
    /// <param name="user">The <see cref="User"/>.</param>
    /// <param name="stubType">The <see cref="StubType"/>.</param>
    /// <param name="sort">The <see cref="SortCriteria"/>.</param>
    /// <param name="timing">Optional browse timing scope.</param>
    /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
    private QueryResult<ServerItem> GetMusicFolders(BaseItem item, User user, StubType? stubType, SortCriteria sort, BrowseTimingScope? timing = null)
    {
        var query = new InternalItemsQuery(user)
        {
            OrderBy = GetOrderBy(sort, false)
        };

        switch (stubType)
        {
            case StubType.Latest:
                return GetRecentlyAdded(item, query, BaseItemKind.Audio);
            case StubType.Playlists:
                return GetMusicPlaylists(query);
            case StubType.Albums:
                return GetChildrenOfItem(item, query, BaseItemKind.MusicAlbum);
            case StubType.Artists:
                return GetMusicArtists(item, query);
            case StubType.AlbumArtists:
                return GetMusicAlbumArtists(item, query);
            case StubType.FavoriteAlbums:
                return GetChildrenOfItem(item, query, BaseItemKind.MusicAlbum, true);
            case StubType.FavoriteArtists:
                return GetFavoriteArtists(item, query);
            case StubType.FavoriteSongs:
                return GetChildrenOfItem(item, query, BaseItemKind.Audio, true);
            case StubType.Songs:
                return GetChildrenOfItem(item, query, BaseItemKind.Audio);
            case StubType.Genres:
                return GetMusicGenres(item, query, timing);
        }

        var serverItems = new ServerItem[]
        {
            new(item, StubType.Latest),
            new(item, StubType.Playlists),
            new(item, StubType.Albums),
            new(item, StubType.AlbumArtists),
            new(item, StubType.Artists),
            new(item, StubType.Songs),
            new(item, StubType.Genres),
            new(item, StubType.FavoriteArtists),
            new(item, StubType.FavoriteAlbums),
            new(item, StubType.FavoriteSongs)
        };

        var musicFolderCount = serverItems.Length;
        return new QueryResult<ServerItem>(
            null,
            musicFolderCount,
            serverItems);
    }

    /// <summary>
    /// Returns the movie folders meeting the criteria.
    /// </summary>
    /// <param name="serverItem">The browsed <see cref="ServerItem"/>.</param>
    /// <param name="user">The <see cref="User"/>.</param>
    /// <param name="sort">The <see cref="SortCriteria"/>.</param>
    /// <param name="timing">Optional browse timing scope.</param>
    /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
    private QueryResult<ServerItem> GetMovieFolders(ServerItem serverItem, User user, SortCriteria sort, BrowseTimingScope? timing = null)
    {
        var item = serverItem.Item;
        var stubType = serverItem.StubType;
        var query = new InternalItemsQuery(user)
        {
            OrderBy = GetOrderBy(sort, false),
            DtoOptions = GetBrowseListDtoOptions()
        };

        switch (stubType)
        {
            case StubType.ContinueWatching:
                return GetMovieContinueWatching(item, query);
            case StubType.Latest:
                return ApplyRecentlyAddedLimit(GetRecentlyAdded(item, query, BaseItemKind.Movie, timing));
            case StubType.Movies:
                return GetMoviesWithOptionalExtras(item, query, timing);
            case StubType.RecentlyAddedMovies:
                return ApplyRecentlyAddedLimit(GetRecentlyAdded(item, query, BaseItemKind.Movie, timing));
            case StubType.RecentlyReleasedMovies:
                return ApplyRecentlyAddedLimit(GetRecentlyReleased(item, query, BaseItemKind.Movie, timing));
            case StubType.BrowseByKana:
                return GetBrowseByKanaRows(item);
            case StubType.BrowseByKanaRow:
                return GetBrowseByKanaRowItems(item, query, BaseItemKind.Movie, serverItem.KanaRowIndex ?? 0, timing);
            case StubType.BrowseByYear:
                return GetBrowseByYearFolders(item, BaseItemKind.Movie, timing);
            case StubType.BrowseByYearItem:
                return GetBrowseByYearItems(item, query, BaseItemKind.Movie, serverItem.ProductionYear, timing);
            case StubType.BrowseByStudio:
                return GetFacetFolders(item, FacetType.Studio, StubType.StudioItem, timing);
            case StubType.StudioItem:
                return GetFacetItems(item, query, FacetType.Studio, serverItem.FacetKey, timing);
            case StubType.BrowseByTag:
                return GetFacetFolders(item, FacetType.Tag, StubType.TagItem, timing);
            case StubType.TagItem:
                return GetFacetItems(item, query, FacetType.Tag, serverItem.FacetKey, timing);
            case StubType.BrowseByRating:
                return GetFacetFolders(item, FacetType.Rating, StubType.RatingItem, timing);
            case StubType.RatingItem:
                return GetFacetItems(item, query, FacetType.Rating, serverItem.FacetKey, timing);
            case StubType.BrowseByPerson:
                return GetFacetFolders(item, FacetType.Person, StubType.PersonItem, timing);
            case StubType.PersonItem:
                return GetFacetItems(item, query, FacetType.Person, serverItem.FacetKey, timing);
            case StubType.RecentlyModifiedMovies:
                return GetRecentlyModified(item, query, VirtualListType.RecentlyModifiedMovies, BaseItemKind.Movie, timing);
            case StubType.Genres:
                return GetGenres(item, query, BaseItemKind.Movie, timing);
            case StubType.Collections:
                return GetMovieCollections(query);
            case StubType.Favorites:
                return GetChildrenOfItem(item, query, BaseItemKind.Movie, true);
            case StubType.ThreeDMovies:
                return GetThreeDMovies(item, query);
            case StubType.FourKMovies:
                return GetFourKMovies(item, query);
            case StubType.EightKMovies:
                return GetEightKMovies(item, query);
            case StubType.VrMovies:
                return GetVrMovies(item, query);
            case StubType.EightKVrMovies:
                return GetEightKVrMovies(item, query);
        }

        var config = GetPluginConfiguration();
        var serverItemsList = new List<ServerItem>
        {
            new(item, StubType.ContinueWatching)
        };

        if (config.EnableRecentlyAddedMovies)
        {
            serverItemsList.Add(new(item, StubType.RecentlyAddedMovies));
        }
        else
        {
            serverItemsList.Add(new(item, StubType.Latest));
        }

        serverItemsList.Add(new(item, StubType.Movies));

        if (config.EnableThreeDMovies)
        {
            serverItemsList.Add(new(item, StubType.ThreeDMovies));
        }

        if (config.EnableFourKMovies)
        {
            serverItemsList.Add(new(item, StubType.FourKMovies));
        }

        if (config.EnableEightKMovies)
        {
            serverItemsList.Add(new(item, StubType.EightKMovies));
        }

        if (config.EnableVrMovies)
        {
            serverItemsList.Add(new(item, StubType.VrMovies));
        }

        if (config.EnableEightKVrMovies)
        {
            serverItemsList.Add(new(item, StubType.EightKVrMovies));
        }

        if (config.EnableRecentlyReleasedMovies)
        {
            serverItemsList.Add(new(item, StubType.RecentlyReleasedMovies));
        }

        serverItemsList.Add(new(item, StubType.Collections));
        serverItemsList.Add(new(item, StubType.Favorites));

        if (config.EnableBrowseByKana)
        {
            serverItemsList.Add(new(item, StubType.BrowseByKana));
        }

        if (config.EnableBrowseByStudio)
        {
            serverItemsList.Add(new(item, StubType.BrowseByStudio));
        }

        if (config.EnableBrowseByTag)
        {
            serverItemsList.Add(new(item, StubType.BrowseByTag));
        }

        if (config.EnableBrowseByRating)
        {
            serverItemsList.Add(new(item, StubType.BrowseByRating));
        }

        if (config.EnableBrowseByPerson)
        {
            serverItemsList.Add(new(item, StubType.BrowseByPerson));
        }

        if (config.EnableRecentlyModifiedMovies)
        {
            serverItemsList.Add(new(item, StubType.RecentlyModifiedMovies));
        }

        if (config.EnableBrowseByYear)
        {
            serverItemsList.Add(new(item, StubType.BrowseByYear));
        }

        serverItemsList.Add(new(item, StubType.Genres));

        var array = serverItemsList.ToArray();
        var movieFolderCount = array.Length;
        return new QueryResult<ServerItem>(
            null,
            movieFolderCount,
            array);
    }

    /// <summary>
    /// Returns the folders meeting the criteria.
    /// </summary>
    /// <param name="user">The <see cref="User"/>.</param>
    /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
    private QueryResult<ServerItem> GetFolders(User user)
    {
        var folders = _libraryManager.GetUserRootFolder().GetChildren(user, true);
        var items = folders
            .OrderBy(i => i.SortName)
            .Select(i => new ServerItem(i, StubType.Folder))
            .ToArray();

        return new QueryResult<ServerItem>(
            null,
            items.Length,
            items);
    }

    /// <summary>
    /// Returns the TV folders meeting the criteria.
    /// </summary>
    /// <param name="serverItem">The browsed <see cref="ServerItem"/>.</param>
    /// <param name="user">The <see cref="User"/>.</param>
    /// <param name="sort">The <see cref="SortCriteria"/>.</param>
    /// <param name="timing">Optional browse timing scope.</param>
    /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
    private QueryResult<ServerItem> GetTvFolders(ServerItem serverItem, User user, SortCriteria sort, BrowseTimingScope? timing = null)
    {
        var item = serverItem.Item;
        var stubType = serverItem.StubType;
        var query = new InternalItemsQuery(user)
        {
            OrderBy = GetOrderBy(sort, false),
            DtoOptions = GetBrowseListDtoOptions()
        };

        switch (stubType)
        {
            case StubType.ContinueWatching:
                return GetMovieContinueWatching(item, query);
            case StubType.NextUp:
                return GetNextUp(item, query);
            case StubType.Latest:
                return ApplyRecentlyAddedLimit(GetRecentlyAdded(item, query, BaseItemKind.Episode, timing));
            case StubType.RecentlyAddedEpisodes:
                return ApplyRecentlyAddedLimit(GetRecentlyAdded(item, query, BaseItemKind.Episode, timing));
            case StubType.RecentlyAddedSeries:
                return ApplyRecentlyAddedLimit(GetRecentlyAdded(item, query, BaseItemKind.Series, timing));
            case StubType.RecentlyReleasedEpisodes:
                return ApplyRecentlyAddedLimit(GetRecentlyReleased(item, query, BaseItemKind.Episode, timing));
            case StubType.RecentlyReleasedSeries:
                return ApplyRecentlyAddedLimit(GetRecentlyReleasedSeries(item, query, timing));
            case StubType.RecentlyUpdatedSeries:
                return ApplyRecentlyAddedLimit(GetRecentlyUpdatedSeries(item, query, timing));
            case StubType.CurrentlyAiring:
                return GetCurrentlyAiringSeries(item, query);
            case StubType.BrowseByKana:
                return GetBrowseByKanaRows(item);
            case StubType.BrowseByKanaRow:
                return GetBrowseByKanaRowItems(item, query, BaseItemKind.Series, serverItem.KanaRowIndex ?? 0, timing);
            case StubType.BrowseByYear:
                return GetBrowseByYearFolders(item, BaseItemKind.Series, timing);
            case StubType.BrowseByYearItem:
                return GetBrowseByYearItems(item, query, BaseItemKind.Series, serverItem.ProductionYear, timing);
            case StubType.BrowseByStudio:
                return GetFacetFolders(item, FacetType.Studio, StubType.StudioItem, timing);
            case StubType.StudioItem:
                return GetFacetItems(item, query, FacetType.Studio, serverItem.FacetKey, timing);
            case StubType.BrowseByTag:
                return GetFacetFolders(item, FacetType.Tag, StubType.TagItem, timing);
            case StubType.TagItem:
                return GetFacetItems(item, query, FacetType.Tag, serverItem.FacetKey, timing);
            case StubType.BrowseByRating:
                return GetFacetFolders(item, FacetType.Rating, StubType.RatingItem, timing);
            case StubType.RatingItem:
                return GetFacetItems(item, query, FacetType.Rating, serverItem.FacetKey, timing);
            case StubType.BrowseByPerson:
                return GetFacetFolders(item, FacetType.Person, StubType.PersonItem, timing);
            case StubType.PersonItem:
                return GetFacetItems(item, query, FacetType.Person, serverItem.FacetKey, timing);
            case StubType.RecentlyModifiedSeries:
                return GetRecentlyModified(item, query, VirtualListType.RecentlyModifiedSeries, BaseItemKind.Series, timing);
            case StubType.RecentlyModifiedEpisodes:
                return GetRecentlyModified(item, query, VirtualListType.RecentlyModifiedEpisodes, BaseItemKind.Episode, timing);
            case StubType.SeriesRange:
                return GetSeriesRangeItems(item, query, serverItem.RangeStart ?? 0, serverItem.RangeEnd ?? 0, timing);
            case StubType.Series:
                return ApplySeriesListLimit(GetChildrenOfItem(item, query, BaseItemKind.Series, timing: timing));
            case StubType.FavoriteSeries:
                return GetChildrenOfItem(item, query, BaseItemKind.Series, true);
            case StubType.FavoriteEpisodes:
                return GetChildrenOfItem(item, query, BaseItemKind.Episode, true);
            case StubType.Genres:
                return GetGenres(item, query, BaseItemKind.Series, timing);
        }

        var config = GetPluginConfiguration();
        var serverItemsList = new List<ServerItem>
        {
            new(item, StubType.ContinueWatching),
            new(item, StubType.NextUp)
        };

        if (config.EnableRecentlyAddedEpisodes)
        {
            serverItemsList.Add(new(item, StubType.RecentlyAddedEpisodes));
        }
        else
        {
            serverItemsList.Add(new(item, StubType.Latest));
        }

        serverItemsList.Add(new(item, StubType.Series));

        if (config.EnableRecentlyUpdatedSeries)
        {
            serverItemsList.Add(new(item, StubType.RecentlyUpdatedSeries));
        }

        if (config.EnableRecentlyAddedSeries)
        {
            serverItemsList.Add(new(item, StubType.RecentlyAddedSeries));
        }

        if (config.EnableRecentlyReleasedEpisodes)
        {
            serverItemsList.Add(new(item, StubType.RecentlyReleasedEpisodes));
        }

        if (config.EnableRecentlyReleasedSeries)
        {
            serverItemsList.Add(new(item, StubType.RecentlyReleasedSeries));
        }

        if (config.EnableCurrentlyAiring)
        {
            serverItemsList.Add(new(item, StubType.CurrentlyAiring));
        }

        serverItemsList.Add(new(item, StubType.FavoriteSeries));
        serverItemsList.Add(new(item, StubType.FavoriteEpisodes));

        if (config.EnableBrowseByKana)
        {
            serverItemsList.Add(new(item, StubType.BrowseByKana));
        }

        if (config.EnableBrowseByStudio)
        {
            serverItemsList.Add(new(item, StubType.BrowseByStudio));
        }

        if (config.EnableBrowseByTag)
        {
            serverItemsList.Add(new(item, StubType.BrowseByTag));
        }

        if (config.EnableBrowseByRating)
        {
            serverItemsList.Add(new(item, StubType.BrowseByRating));
        }

        if (config.EnableBrowseByPerson)
        {
            serverItemsList.Add(new(item, StubType.BrowseByPerson));
        }

        if (config.EnableRecentlyModifiedSeries)
        {
            serverItemsList.Add(new(item, StubType.RecentlyModifiedSeries));
        }

        if (config.EnableRecentlyModifiedEpisodes)
        {
            serverItemsList.Add(new(item, StubType.RecentlyModifiedEpisodes));
        }

        if (config.EnableBrowseByYear)
        {
            serverItemsList.Add(new(item, StubType.BrowseByYear));
        }

        serverItemsList.Add(new(item, StubType.Genres));
        serverItemsList.AddRange(IndexBrowseHelper.GetSeriesRangeFolders(_indexStore, _indexService, config, item));

        var serverItems = serverItemsList.ToArray();
        var tvFolderCount = serverItems.Length;
        return new QueryResult<ServerItem>(
            null,
            tvFolderCount,
            serverItems);
    }

    /// <summary>
    /// Returns virtual folders for mixed (TV + movies) libraries.
    /// </summary>
    private QueryResult<ServerItem> GetMixedFolders(ServerItem serverItem, User user, SortCriteria sort, BrowseTimingScope? timing = null)
    {
        var item = serverItem.Item;
        var stubType = serverItem.StubType;

        if (stubType is StubType stub)
        {
            if (MixedLibraryBrowseHelper.IsTvExclusiveStub(stub))
            {
                return GetTvFolders(serverItem, user, sort, timing);
            }

            if (MixedLibraryBrowseHelper.IsMovieExclusiveStub(stub))
            {
                return GetMovieFolders(serverItem, user, sort, timing);
            }
        }

        var query = new InternalItemsQuery(user)
        {
            OrderBy = GetOrderBy(sort, false),
            DtoOptions = GetBrowseListDtoOptions()
        };

        switch (stubType)
        {
            case StubType.Genres:
                return GetGenres(item, query, BaseItemKind.Series, timing);
            case StubType.BrowseByKana:
                return GetBrowseByKanaRows(item);
            case StubType.BrowseByKanaRow:
                return GetMixedBrowseByKanaRowItems(item, query, serverItem.KanaRowIndex ?? 0, timing);
            case StubType.BrowseByYear:
                return GetMixedBrowseByYearFolders(item, timing);
            case StubType.BrowseByYearItem:
                return GetMixedBrowseByYearItems(item, query, serverItem.ProductionYear, timing);
            case StubType.BrowseByStudio:
                return GetFacetFolders(item, FacetType.Studio, StubType.StudioItem, timing);
            case StubType.StudioItem:
                return GetFacetItems(item, query, FacetType.Studio, serverItem.FacetKey, timing);
            case StubType.BrowseByTag:
                return GetFacetFolders(item, FacetType.Tag, StubType.TagItem, timing);
            case StubType.TagItem:
                return GetFacetItems(item, query, FacetType.Tag, serverItem.FacetKey, timing);
            case StubType.BrowseByRating:
                return GetFacetFolders(item, FacetType.Rating, StubType.RatingItem, timing);
            case StubType.RatingItem:
                return GetFacetItems(item, query, FacetType.Rating, serverItem.FacetKey, timing);
            case StubType.BrowseByPerson:
                return GetFacetFolders(item, FacetType.Person, StubType.PersonItem, timing);
            case StubType.PersonItem:
                return GetFacetItems(item, query, FacetType.Person, serverItem.FacetKey, timing);
        }

        var config = GetPluginConfiguration();
        var array = MixedLibraryBrowseHelper.BuildMixedRootFolderList(config, item, _indexStore, _indexService);
        return new QueryResult<ServerItem>(null, array.Length, array);
    }

    /// <summary>
    /// Returns virtual folders for home videos and photos libraries.
    /// </summary>
    private QueryResult<ServerItem> GetHomeVideoFolders(ServerItem serverItem, User user, SortCriteria sort, BrowseTimingScope? timing = null)
    {
        var item = serverItem.Item;
        var stubType = serverItem.StubType;
        var query = new InternalItemsQuery(user)
        {
            OrderBy = GetOrderBy(sort, false),
            DtoOptions = GetBrowseListDtoOptions()
        };

        switch (stubType)
        {
            case StubType.Latest:
                return ApplyRecentlyAddedLimit(GetRecentlyAdded(item, query, BaseItemKind.Video, timing));
            case StubType.Videos:
                return GetChildrenOfItem(item, query, BaseItemKind.Video);
            case StubType.Photos:
                return GetChildrenOfItem(item, query, BaseItemKind.Photo);
            case StubType.BrowseByYear:
                return GetBrowseByYearFolders(item, BaseItemKind.Video, timing);
            case StubType.BrowseByYearItem:
                return GetBrowseByYearItems(item, query, BaseItemKind.Video, serverItem.ProductionYear, timing);
            case StubType.Favorites:
                return GetChildrenOfItem(item, query, BaseItemKind.Video, true);
        }

        var config = GetPluginConfiguration();
        var serverItemsList = new List<ServerItem>
        {
            new(item, StubType.Latest),
            new(item, StubType.Videos),
            new(item, StubType.Photos)
        };

        if (config.EnableBrowseByYear)
        {
            serverItemsList.Add(new(item, StubType.BrowseByYear));
        }

        serverItemsList.Add(new(item, StubType.Favorites));

        var array = serverItemsList.ToArray();
        return new QueryResult<ServerItem>(null, array.Length, array);
    }

    /// <summary>
    /// Returns virtual folders for music videos libraries.
    /// </summary>
    private QueryResult<ServerItem> GetMusicVideoFolders(ServerItem serverItem, User user, SortCriteria sort, BrowseTimingScope? timing = null)
    {
        var item = serverItem.Item;
        var stubType = serverItem.StubType;
        var query = new InternalItemsQuery(user)
        {
            OrderBy = GetOrderBy(sort, false),
            DtoOptions = GetBrowseListDtoOptions()
        };

        switch (stubType)
        {
            case StubType.Latest:
                return ApplyRecentlyAddedLimit(GetRecentlyAdded(item, query, BaseItemKind.MusicVideo, timing));
            case StubType.MusicVideos:
                return GetChildrenOfItem(item, query, BaseItemKind.MusicVideo);
            case StubType.Artists:
                return GetMusicVideoArtists(item, query);
            case StubType.Genres:
                return GetMusicVideoGenres(item, query, timing);
        }

        var serverItems = new ServerItem[]
        {
            new(item, StubType.Latest),
            new(item, StubType.MusicVideos),
            new(item, StubType.Artists),
            new(item, StubType.Genres)
        };

        return new QueryResult<ServerItem>(null, serverItems.Length, serverItems);
    }

    /// <summary>
    /// Returns the 3D Movies meeting the criteria.
    /// </summary>
    /// <param name="parent">The <see cref="BaseItem"/>.</param>
    /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
    /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
    private QueryResult<ServerItem> GetThreeDMovies(BaseItem parent, InternalItemsQuery query)
    {
        ApplyLibraryQueryScope(query, parent);
        query.IncludeItemTypes = [BaseItemKind.Movie];
        query.Is3D = true;

        var result = _libraryManager.GetItemsResult(query);

        return ToResult(null, result);
    }

    /// <summary>
    /// Returns the 4K Movies meeting the criteria.
    /// </summary>
    private QueryResult<ServerItem> GetFourKMovies(BaseItem parent, InternalItemsQuery query)
    {
        return GetFilteredMovies(parent, query, video => video.Width >= 3800 || video.Height >= 2000);
    }

    /// <summary>
    /// Returns the 8K Movies meeting the criteria.
    /// </summary>
    private QueryResult<ServerItem> GetEightKMovies(BaseItem parent, InternalItemsQuery query)
    {
        return GetFilteredMovies(parent, query, video => video.Width >= 7000 || video.Height >= 4000);
    }

    /// <summary>
    /// Returns the VR Movies meeting the criteria.
    /// </summary>
    private QueryResult<ServerItem> GetVrMovies(BaseItem parent, InternalItemsQuery query)
    {
        return GetFilteredMovies(parent, query, video => IsVrVideo(video));
    }

    /// <summary>
    /// Returns the 8K VR Movies meeting the criteria.
    /// </summary>
    private QueryResult<ServerItem> GetEightKVrMovies(BaseItem parent, InternalItemsQuery query)
    {
        return GetFilteredMovies(parent, query, video => IsVrVideo(video) && (video.Width >= 7000 || video.Height >= 4000));
    }

    /// <summary>
    /// Helper method to return filtered movies using in-memory filtering and manual paging.
    /// </summary>
    private QueryResult<ServerItem> GetFilteredMovies(BaseItem parent, InternalItemsQuery query, Func<Video, bool> filter)
    {
        var fullQuery = new InternalItemsQuery(query.User)
        {
            OrderBy = query.OrderBy,
            IsPlaceHolder = false,
            DtoOptions = GetBrowseListDtoOptions()
        };
        ApplyLibraryQueryScope(fullQuery, parent);
        fullQuery.IncludeItemTypes = [BaseItemKind.Movie];

        var result = _libraryManager.GetItemsResult(fullQuery);
        var filtered = result.Items
            .OfType<Video>()
            .Where(filter)
            .Cast<BaseItem>()
            .ToList();

        var totalCount = filtered.Count;
        var serverItems = filtered
            .Select(i => new ServerItem(i, null))
            .ToArray();

        return new QueryResult<ServerItem>(null, totalCount, serverItems);
    }

    /// <summary>
    /// Helper to detect if a video is VR format.
    /// </summary>
    private static bool IsVrVideo(Video video)
    {
        if (video.Tags != null && video.Tags.Any(t => t.Contains("vr", StringComparison.OrdinalIgnoreCase) 
                                                     || t.Contains("180", StringComparison.OrdinalIgnoreCase) 
                                                     || t.Contains("360", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var path = video.Path;
        if (!string.IsNullOrEmpty(path))
        {
            var fileName = System.IO.Path.GetFileName(path);
            if (fileName.Contains("vr180", StringComparison.OrdinalIgnoreCase)
                || fileName.Contains("vr360", StringComparison.OrdinalIgnoreCase)
                || fileName.Contains("180_sbs", StringComparison.OrdinalIgnoreCase)
                || fileName.Contains("360_sbs", StringComparison.OrdinalIgnoreCase)
                || fileName.Contains("_180", StringComparison.OrdinalIgnoreCase)
                || fileName.Contains("_360", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the Movies that are part watched that meet the criteria.
    /// </summary>
    /// <param name="parent">The <see cref="BaseItem"/>.</param>
    /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
    /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
    private QueryResult<ServerItem> GetMovieContinueWatching(BaseItem parent, InternalItemsQuery query)
    {
        var listQuery = new InternalItemsQuery(query.User)
        {
            OrderBy =
            [
                (ItemSortBy.DatePlayed, SortOrder.Descending),
                (ItemSortBy.SortName, SortOrder.Ascending)
            ],
            IsResumable = true,
            IsPlaceHolder = false,
            DtoOptions = GetBrowseListDtoOptions()
        };
        ApplyLibraryQueryScope(listQuery, parent);
        PrepareItemsQuery(listQuery);

        var allItems = _libraryManager.GetItemsResult(listQuery);
        var totalCount = Math.Max(allItems.TotalRecordCount, allItems.Items.Count);

        return ToResult(null, totalCount, allItems.Items);
    }

    /// <summary>
    /// Returns the Movie collections meeting the criteria.
    /// </summary>
    /// <param name="query">The see cref="InternalItemsQuery"/>.</param>
    /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
    private QueryResult<ServerItem> GetMovieCollections(InternalItemsQuery query)
    {
        query.Recursive = true;
        query.IncludeItemTypes = [BaseItemKind.BoxSet];

        var result = _libraryManager.GetItemsResult(query);

        return ToResult(null, result);
    }

    /// <summary>
    /// Returns the children that meet the criteria.
    /// </summary>
    /// <param name="parent">The <see cref="BaseItem"/>.</param>
    /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
    /// <param name="itemType">The item type.</param>
    /// <param name="isFavorite">A value indicating whether to only fetch favorite items.</param>
    /// <param name="timing">Optional browse timing scope.</param>
    /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
    private QueryResult<ServerItem> GetChildrenOfItem(BaseItem parent, InternalItemsQuery query, BaseItemKind itemType, bool isFavorite = false, BrowseTimingScope? timing = null)
    {
        if (!isFavorite)
        {
            var indexed = TryGetIndexedLibraryChildren(parent, query, itemType, timing);
            if (indexed is not null)
            {
                return indexed;
            }
        }

        if (LibraryBrowseQueryHelper.IsDlnaLibraryView(parent))
        {
            var listQuery = new InternalItemsQuery(query.User)
            {
                OrderBy = query.OrderBy,
                IncludeItemTypes = [itemType],
                IsPlaceHolder = false,
                DtoOptions = GetBrowseListDtoOptions()
            };
            if (isFavorite)
            {
                listQuery.IsFavorite = true;
            }

            ApplyLibraryQueryScope(listQuery, parent);
            PrepareItemsQuery(listQuery);

            var allItems = _libraryManager.GetItemsResult(listQuery);
            var totalCount = Math.Max(allItems.TotalRecordCount, allItems.Items.Count);

            return ToResult(null, totalCount, allItems.Items);
        }

        var fullQuery = new InternalItemsQuery(query.User)
        {
            OrderBy = query.OrderBy,
            IncludeItemTypes = [itemType],
            IsPlaceHolder = false,
            DtoOptions = GetBrowseListDtoOptions()
        };
        if (isFavorite)
        {
            fullQuery.IsFavorite = true;
        }

        ApplyLibraryQueryScope(fullQuery, parent);
        PrepareItemsQuery(fullQuery);

        var allResult = _libraryManager.GetItemsResult(fullQuery);
        var total = Math.Max(allResult.TotalRecordCount, allResult.Items.Count);

        return ToResult(null, total, allResult.Items);
    }

    /// <summary>
    /// Returns movies for the Movies stub, optionally inserting Extras folders after each movie.
    /// </summary>
    private QueryResult<ServerItem> GetMoviesWithOptionalExtras(BaseItem parent, InternalItemsQuery query, BrowseTimingScope? timing = null)
    {
        var config = GetPluginConfiguration();
        var indexed = IndexBrowseHelper.TryGetVirtualList(
            _indexStore,
            _libraryManager,
            _indexService,
            config,
            parent,
            query,
            VirtualListType.MoviesAll,
            timing);
        if (indexed is not null)
        {
            if (config.EnableExtras != true)
            {
                return ApplyIndexedResult(indexed, timing);
            }

            var itemsWithExtras = new List<ServerItem>(indexed.Items.Count * 2);
            foreach (var movieItem in indexed.Items)
            {
                itemsWithExtras.Add(movieItem);
                if (MovieHasExtras(movieItem, parent.Id))
                {
                    var extrasParent = movieItem.IsSummaryBacked
                        ? _libraryManager.GetItemById(movieItem.Summary!.ItemId)
                        : movieItem.Item;
                    if (extrasParent is not null)
                    {
                        itemsWithExtras.Add(new ServerItem(extrasParent, StubType.Extras));
                    }
                }
            }

            return ApplyIndexedResult(new BrowsableQueryResult(itemsWithExtras, itemsWithExtras.Count, indexed.SummaryHit), timing);
        }

        if (DlnaPlugin.Instance?.Configuration.EnableExtras != true)
        {
            return GetChildrenOfItem(parent, query, BaseItemKind.Movie, timing: timing);
        }

        var allMoviesQuery = new InternalItemsQuery(query.User)
        {
            OrderBy = query.OrderBy,
            IsPlaceHolder = false,
            DtoOptions = GetBrowseListDtoOptions()
        };
        ApplyLibraryQueryScope(allMoviesQuery, parent);
        allMoviesQuery.IncludeItemTypes = [BaseItemKind.Movie];
        PrepareItemsQuery(allMoviesQuery);

        var allMoviesResult = _libraryManager.GetItemsResult(allMoviesQuery);
        var fallbackItemsWithExtras = new List<ServerItem>(allMoviesResult.Items.Count * 2);
        foreach (var movieItem in allMoviesResult.Items)
        {
            fallbackItemsWithExtras.Add(new ServerItem(movieItem, null));
            if (movieItem.GetExtras().Any())
            {
                fallbackItemsWithExtras.Add(new ServerItem(movieItem, StubType.Extras));
            }
        }

        return new QueryResult<ServerItem>(null, fallbackItemsWithExtras.Count, fallbackItemsWithExtras.ToArray());
    }

    /// <summary>
    /// Scopes a library query to items under the given library folder or user view.
    /// LibraryManager converts CollectionFolder/UserView parents to TopParentIds (physical folder ids).
    /// </summary>
    private static void ApplyLibraryQueryScope(InternalItemsQuery query, BaseItem parent)
    {
        query.Parent = parent;
        query.ParentId = parent.Id;
        query.Recursive = true;
    }

    private static void PrepareItemsQuery(InternalItemsQuery query)
    {
        if (query.User is not null)
        {
            query.SetUser(query.User);
        }
    }

    /// <summary>
    /// Returns the genres meeting the criteria, scoped to items in the given library.
    /// Mirrors Jellyfin UserViewBuilder.GetTvGenres / GetMovieGenres.
    /// </summary>
    private QueryResult<ServerItem> GetGenres(BaseItem parent, InternalItemsQuery query, BaseItemKind itemType, BrowseTimingScope? timing = null)
    {
        var indexed = IndexBrowseHelper.TryGetGenreFolders(
            _libraryManager,
            _indexStore,
            _indexService,
            GetPluginConfiguration(),
            parent,
            Logger,
            timing);
        if (indexed is not null)
        {
            MarkIndexHit(timing);
            return new QueryResult<ServerItem>(null, indexed.Count, indexed.ToArray());
        }

        var listQuery = new InternalItemsQuery(query.User)
        {
            IncludeItemTypes = LibraryBrowseQueryHelper.GetGenreBrowseItemTypes(parent).ToArray(),
            IsPlaceHolder = false,
            IsVirtualItem = false,
            EnableTotalRecordCount = false,
            DtoOptions = new DtoOptions(false)
        };
        ApplyLibraryQueryScope(listQuery, parent);
        PrepareItemsQuery(listQuery);

        var genreNames = _libraryManager.GetItemsResult(listQuery).Items
            .SelectMany(i => i.Genres)
            .DistinctNames();

        var genreItems = new List<BaseItem>();
        foreach (var name in genreNames)
        {
            try
            {
                var genre = _libraryManager.GetGenre(name);
                if (genre is not null)
                {
                    genreItems.Add(genre);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error getting genre {GenreName}", name);
            }
        }

        var serverItems = genreItems
            .OrderBy(g => g.SortName, StringComparer.OrdinalIgnoreCase)
            .Select(g => new ServerItem(g, null, parent.Id))
            .ToArray();

        return new QueryResult<ServerItem>(null, serverItems.Length, serverItems);
    }

    /// <summary>
    /// Returns music video artists scoped to a music videos library.
    /// </summary>
    private QueryResult<ServerItem> GetMusicVideoArtists(BaseItem parent, InternalItemsQuery query)
    {
        query.OrderBy = [];
        query.AncestorIds = [parent.Id];
        query.IncludeItemTypes = [BaseItemKind.MusicVideo];
        var artists = _libraryManager.GetArtists(query);
        return ToResult(null, artists);
    }

    /// <summary>
    /// Returns music video genres scoped to a music videos library.
    /// </summary>
    private QueryResult<ServerItem> GetMusicVideoGenres(BaseItem parent, InternalItemsQuery query, BrowseTimingScope? timing = null)
    {
        var listQuery = new InternalItemsQuery(query.User)
        {
            IncludeItemTypes = [BaseItemKind.MusicVideo],
            IsPlaceHolder = false,
            IsVirtualItem = false,
            EnableTotalRecordCount = false,
            DtoOptions = new DtoOptions(false)
        };
        ApplyLibraryQueryScope(listQuery, parent);
        PrepareItemsQuery(listQuery);

        var genreNames = _libraryManager.GetItemsResult(listQuery).Items
            .SelectMany(i => i.Genres)
            .DistinctNames();

        var genreItems = new List<BaseItem>();
        foreach (var name in genreNames)
        {
            try
            {
                var genre = _libraryManager.GetMusicGenre(name);
                if (genre is not null)
                {
                    genreItems.Add(genre);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error getting music genre {GenreName}", name);
            }
        }

        var serverItems = genreItems
            .OrderBy(g => g.SortName, StringComparer.OrdinalIgnoreCase)
            .Select(g => new ServerItem(g, null, parent.Id))
            .ToArray();

        return new QueryResult<ServerItem>(null, serverItems.Length, serverItems);
    }

    /// <summary>
    /// Returns the music genres meeting the criteria, scoped to items in the given library.
    /// </summary>
    private QueryResult<ServerItem> GetMusicGenres(BaseItem parent, InternalItemsQuery query, BrowseTimingScope? timing = null)
    {
        var indexed = IndexBrowseHelper.TryGetMusicGenreFolders(
            _libraryManager,
            _indexStore,
            _indexService,
            GetPluginConfiguration(),
            parent,
            Logger,
            timing);
        if (indexed is not null)
        {
            MarkIndexHit(timing);
            return new QueryResult<ServerItem>(null, indexed.Count, indexed.ToArray());
        }

        var listQuery = new InternalItemsQuery(query.User)
        {
            IncludeItemTypes = [BaseItemKind.Audio, BaseItemKind.MusicAlbum],
            IsPlaceHolder = false,
            IsVirtualItem = false,
            EnableTotalRecordCount = false,
            DtoOptions = new DtoOptions(false)
        };
        ApplyLibraryQueryScope(listQuery, parent);
        PrepareItemsQuery(listQuery);

        var genreNames = _libraryManager.GetItemsResult(listQuery).Items
            .SelectMany(i => i.Genres)
            .DistinctNames();

        var genreItems = new List<BaseItem>();
        foreach (var name in genreNames)
        {
            try
            {
                var genre = _libraryManager.GetMusicGenre(name);
                if (genre is not null)
                {
                    genreItems.Add(genre);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error getting music genre {GenreName}", name);
            }
        }

        var serverItems = genreItems
            .OrderBy(g => g.SortName, StringComparer.OrdinalIgnoreCase)
            .Select(g => new ServerItem(g, null, parent.Id))
            .ToArray();

        return new QueryResult<ServerItem>(null, serverItems.Length, serverItems);
    }

    /// <summary>
    /// Returns the music albums by artist that meet the criteria.
    /// </summary>
    /// <param name="parent">The <see cref="BaseItem"/>.</param>
    /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
    /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
    private QueryResult<ServerItem> GetMusicAlbumArtists(BaseItem parent, InternalItemsQuery query)
    {
        // Don't sort
        query.OrderBy = [];
        query.AncestorIds = [parent.Id];
        var artists = _libraryManager.GetAlbumArtists(query);

        return ToResult(null, artists);
    }

    /// <summary>
    /// Returns the music artists meeting the criteria.
    /// </summary>
    /// <param name="parent">The <see cref="BaseItem"/>.</param>
    /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
    /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
    private QueryResult<ServerItem> GetMusicArtists(BaseItem parent, InternalItemsQuery query)
    {
        // Don't sort
        query.OrderBy = [];
        query.AncestorIds = [parent.Id];
        var artists = _libraryManager.GetArtists(query);
        return ToResult(null, artists);
    }

    /// <summary>
    /// Returns the artists tagged as favourite that meet the criteria.
    /// </summary>
    /// <param name="parent">The <see cref="BaseItem"/>.</param>
    /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
    /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
    private QueryResult<ServerItem> GetFavoriteArtists(BaseItem parent, InternalItemsQuery query)
    {
        // Don't sort
        query.OrderBy = [];
        query.AncestorIds = [parent.Id];
        query.IsFavorite = true;
        var artists = _libraryManager.GetArtists(query);
        return ToResult(null, artists);
    }

    /// <summary>
    /// Returns the music playlists meeting the criteria.
    /// </summary>
    /// <param name="query">The query<see cref="InternalItemsQuery"/>.</param>
    /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
    private QueryResult<ServerItem> GetMusicPlaylists(InternalItemsQuery query)
    {
        query.Parent = null;
        query.IncludeItemTypes = [BaseItemKind.Playlist];
        query.Recursive = true;

        var result = _libraryManager.GetItemsResult(query);

        return ToResult(null, result);
    }

    /// <summary>
    /// Returns the next up item meeting the criteria.
    /// </summary>
    /// <param name="parent">The <see cref="BaseItem"/>.</param>
    /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
    /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
    private QueryResult<ServerItem> GetNextUp(BaseItem parent, InternalItemsQuery query)
    {
        query.OrderBy = [];

        var result = _tvSeriesManager.GetNextUp(
            new NextUpQuery
            {
                Limit = int.MaxValue,
                StartIndex = 0,
                // User cannot be null here as the caller has set it
                User = query.User!
            },
            [parent],
            query.DtoOptions);

        var totalCount = Math.Max(result.TotalRecordCount, result.Items.Count);

        return ToResult(null, totalCount, result.Items);
    }

    /// <summary>
    /// Returns recently added items of [itemType] within the library, sorted by date.
    /// </summary>
    private QueryResult<ServerItem> GetRecentlyAdded(BaseItem parent, InternalItemsQuery query, BaseItemKind itemType, BrowseTimingScope? timing = null)
    {
        var config = GetPluginConfiguration();
        var listType = itemType switch
        {
            BaseItemKind.Episode => VirtualListType.RecentlyAddedEpisodes,
            BaseItemKind.Series => VirtualListType.RecentlyAddedSeries,
            BaseItemKind.Movie => VirtualListType.RecentlyAddedMovies,
            _ => (VirtualListType?)null
        };

        if (listType is VirtualListType virtualListType)
        {
            var indexed = IndexBrowseHelper.TryGetVirtualList(
                _indexStore,
                _libraryManager,
                _indexService,
                config,
                parent,
                query,
                virtualListType,
                timing);
            if (indexed is not null)
            {
                return ApplyIndexedResult(indexed, timing);
            }
        }

        var sortField = itemType == BaseItemKind.Series
            ? ItemSortBy.DateLastContentAdded
            : ItemSortBy.DateCreated;

        var listQuery = new InternalItemsQuery(query.User)
        {
            OrderBy =
            [
                (sortField, SortOrder.Descending),
                (ItemSortBy.SortName, SortOrder.Ascending)
            ],
            IncludeItemTypes = [itemType],
            IsPlaceHolder = false,
            IsVirtualItem = false,
            DtoOptions = GetBrowseListDtoOptions()
        };
        ApplyLibraryQueryScope(listQuery, parent);
        PrepareItemsQuery(listQuery);

        var allItems = _libraryManager.GetItemsResult(listQuery);
        var totalCount = Math.Max(allItems.TotalRecordCount, allItems.Items.Count);

        return ToResult(null, totalCount, allItems.Items);
    }

    /// <summary>
    /// Returns music artist items that meet the criteria.
    /// </summary>
    /// <param name="item">The <see cref="BaseItem"/>.</param>
    /// <param name="user">The <see cref="User"/>.</param>
    /// <param name="sort">The <see cref="SortCriteria"/>.</param>
    /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
    private QueryResult<ServerItem> GetMusicArtistItems(BaseItem item, User user, SortCriteria sort)
    {
        var query = new InternalItemsQuery(user)
        {
            Recursive = true,
            ArtistIds = [item.Id],
            IncludeItemTypes = [BaseItemKind.MusicAlbum],
            DtoOptions = GetBrowseListDtoOptions(),
            OrderBy = GetOrderBy(sort, false)
        };

        var result = _libraryManager.GetItemsResult(query);

        return ToResult(null, result);
    }

    /// <summary>
    /// Returns the genre items meeting the criteria.
    /// </summary>
    /// <param name="item">The <see cref="BaseItem"/>.</param>
    /// <param name="user">The <see cref="User"/>.</param>
    /// <param name="sort">The <see cref="SortCriteria"/>.</param>
    /// <param name="libraryScopeId">The library folder id when the genre was browsed from a library.</param>
    /// <param name="timing">Optional browse timing scope.</param>
    /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
    private QueryResult<ServerItem> GetGenreItems(BaseItem item, User user, SortCriteria sort, Guid? libraryScopeId, BrowseTimingScope? timing = null)
    {
        if (libraryScopeId is Guid libraryId && !string.IsNullOrWhiteSpace(item.Name))
        {
            var parent = _libraryManager.GetItemById(libraryId);
            if (parent is not null)
            {
                var indexedQuery = new InternalItemsQuery(user)
                {
                    DtoOptions = GetBrowseListDtoOptions(),
                    OrderBy = GetOrderBy(sort, false),
                    IncludeItemTypes = LibraryBrowseQueryHelper.GetGenreBrowseItemTypes(parent).ToArray()
                };

                var indexed = IndexBrowseHelper.TryGetFacetItems(
                    _indexStore,
                    _libraryManager,
                    _indexService,
                    GetPluginConfiguration(),
                    parent,
                    indexedQuery,
                    FacetType.Genre,
                    item.Name,
                    timing);
                if (indexed is not null)
                {
                    return ApplyIndexedResult(indexed, timing);
                }
            }
        }

        var query = new InternalItemsQuery(user)
        {
            GenreIds = [item.Id],
            DtoOptions = GetBrowseListDtoOptions(),
            OrderBy = GetOrderBy(sort, false)
        };

        if (libraryScopeId is Guid scopedLibraryId)
        {
            var parent = _libraryManager.GetItemById(scopedLibraryId);
            if (parent is not null)
            {
                ApplyLibraryQueryScope(query, parent);
                query.IncludeItemTypes = LibraryBrowseQueryHelper.GetGenreBrowseItemTypes(parent).ToArray();
            }
        }
        else
        {
            query.Recursive = true;
            query.IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Series];
        }

        PrepareItemsQuery(query);

        var result = _libraryManager.GetItemsResult(query);

        return ToResult(null, result);
    }

    /// <summary>
    /// Returns the music genre items meeting the criteria.
    /// </summary>
    /// <param name="item">The <see cref="BaseItem"/>.</param>
    /// <param name="user">The <see cref="User"/>.</param>
    /// <param name="sort">The <see cref="SortCriteria"/>.</param>
    /// <param name="libraryScopeId">The library folder id when the genre was browsed from a library.</param>
    /// <param name="timing">Optional browse timing scope.</param>
    /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
    private QueryResult<ServerItem> GetMusicGenreItems(BaseItem item, User user, SortCriteria sort, Guid? libraryScopeId, BrowseTimingScope? timing = null)
    {
        if (libraryScopeId is Guid libraryId && !string.IsNullOrWhiteSpace(item.Name))
        {
            var parent = _libraryManager.GetItemById(libraryId);
            if (parent is not null)
            {
                var indexedQuery = new InternalItemsQuery(user)
                {
                    IncludeItemTypes = [BaseItemKind.MusicAlbum],
                    DtoOptions = GetBrowseListDtoOptions(),
                    OrderBy = GetOrderBy(sort, false)
                };

                var indexed = IndexBrowseHelper.TryGetFacetItems(
                    _indexStore,
                    _libraryManager,
                    _indexService,
                    GetPluginConfiguration(),
                    parent,
                    indexedQuery,
                    FacetType.MusicGenre,
                    item.Name,
                    timing);
                if (indexed is not null)
                {
                    return ApplyIndexedResult(indexed, timing);
                }
            }
        }

        var query = new InternalItemsQuery(user)
        {
            GenreIds = [item.Id],
            IncludeItemTypes = [BaseItemKind.MusicAlbum],
            DtoOptions = GetBrowseListDtoOptions(),
            OrderBy = GetOrderBy(sort, false)
        };

        if (libraryScopeId is Guid scopedMusicLibraryId)
        {
            var parent = _libraryManager.GetItemById(scopedMusicLibraryId);
            if (parent is not null)
            {
                ApplyLibraryQueryScope(query, parent);
            }
        }
        else
        {
            query.Recursive = true;
        }

        PrepareItemsQuery(query);

        var result = _libraryManager.GetItemsResult(query);

        return ToResult(null, result);
    }

    /// <summary>
    /// Converts <see cref="IReadOnlyCollection{BaseItem}"/> into a <see cref="QueryResult{ServerItem}"/>.
    /// </summary>
    /// <param name="startIndex">The start index.</param>
    /// <param name="result">An array of <see cref="BaseItem"/>.</param>
    /// <returns>A <see cref="QueryResult{ServerItem}"/>.</returns>
    private static QueryResult<ServerItem> ToResult(int? startIndex, BaseItem[]? result)
    {
        var serverItems = result?
            .Select(i => new ServerItem(i, null))
            .ToArray();

        return new QueryResult<ServerItem>(
            startIndex,
            result?.Length ?? 0,
            serverItems ?? []);
    }

    /// <summary>
    /// Converts a <see cref="QueryResult{BaseItem}"/> to a <see cref="QueryResult{ServerItem}"/>.
    /// </summary>
    /// <param name="startIndex">The index the result started at.</param>
    /// <param name="result">A <see cref="QueryResult{BaseItem}"/>.</param>
    /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
    private static QueryResult<ServerItem> ToResult(int? startIndex, QueryResult<BaseItem> result)
        => ToResult(startIndex, result.TotalRecordCount, result);

    private static QueryResult<ServerItem> ToResult(int? startIndex, int totalRecordCount, IReadOnlyList<BaseItem> items)
    {
        var length = items.Count;
        var serverItems = new ServerItem[length];
        for (var i = 0; i < length; i++)
        {
            serverItems[i] = new ServerItem(items[i], null);
        }

        return new QueryResult<ServerItem>(
            startIndex,
            totalRecordCount,
            serverItems);
    }

    private static QueryResult<ServerItem> ToResult(int? startIndex, int totalRecordCount, QueryResult<BaseItem> result)
    {
        var length = result.Items.Count;
        var serverItems = new ServerItem[length];
        for (var i = 0; i < length; i++)
        {
            serverItems[i] = new ServerItem(result.Items[i], null);
        }

        return new QueryResult<ServerItem>(
            startIndex,
            totalRecordCount,
            serverItems);
    }

    /// <summary>
    /// Converts a query result to a <see cref="QueryResult{ServerItem}"/>.
    /// </summary>
    /// <param name="startIndex">The start index.</param>
    /// <param name="result">A <see cref="QueryResult{BaseItem}"/>.</param>
    /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
    private static QueryResult<ServerItem> ToResult(int? startIndex, QueryResult<(BaseItem Item, ItemCounts ItemCounts)> result)
    {
        var length = result.Items.Count;
        var serverItems = new ServerItem[length];
        for (var i = 0; i < length; i++)
        {
            serverItems[i] = new ServerItem(result.Items[i].Item, null);
        }

        return new QueryResult<ServerItem>(
            startIndex,
            result.TotalRecordCount,
            serverItems);
    }

    /// <summary>
    /// Gets the sorting method on a query.
    /// </summary>
    /// <param name="sort">The <see cref="SortCriteria"/>.</param>
    /// <param name="isPreSorted">True if pre-sorted.</param>
    private static (ItemSortBy SortName, SortOrder SortOrder)[] GetOrderBy(SortCriteria sort, bool isPreSorted)
    {
        return isPreSorted ? Array.Empty<(ItemSortBy, SortOrder)>() : [(ItemSortBy.SortName, sort.SortOrder)];
    }

    /// <summary>
    /// Retrieves the ServerItem id.
    /// </summary>
    /// <param name="id">The id<see cref="string"/>.</param>
    /// <returns>The <see cref="ServerItem"/>.</returns>
    private ServerItem GetItemFromObjectId(string id)
    {
        return DidlBuilder.IsIdRoot(id)
            ? new ServerItem(_libraryManager.GetUserRootFolder(), null)
            : ParseItemId(id);
    }

    /// <summary>
    /// Parses the item id into a <see cref="ServerItem"/>.
    /// </summary>
    /// <param name="id">The <see cref="string"/>.</param>
    /// <returns>The corresponding <see cref="ServerItem"/>.</returns>
    private ServerItem ParseItemId(string id)
    {
        StubType? stubType = null;

        // After using PlayTo, MediaMonkey sends a request to the server trying to get item info
        const string ParamsSrch = "Params=";
        var paramsIndex = id.IndexOf(ParamsSrch, StringComparison.OrdinalIgnoreCase);
        if (paramsIndex != -1)
        {
            id = id[(paramsIndex + ParamsSrch.Length)..];

            var parts = id.Split(';');
            id = parts[23];
        }

        if (DidlBuilder.TryParseLibraryScopedGenreClientId(id, out var libraryId, out var genreId))
        {
            var genre = _libraryManager.GetItemById(genreId);
            if (genre is not null)
            {
                return new ServerItem(genre, null, libraryId);
            }
        }

        if (DidlBuilder.TryParseLibraryScopedMusicGenreClientId(id, out libraryId, out var musicGenreId))
        {
            var musicGenre = _libraryManager.GetItemById(musicGenreId);
            if (musicGenre is not null)
            {
                return new ServerItem(musicGenre, null, libraryId);
            }
        }

        if (DidlBuilder.TryParseKanaRowClientId(id, out libraryId, out var kanaRowIndex))
        {
            var library = _libraryManager.GetItemById(libraryId);
            if (library is not null)
            {
                return new ServerItem(library, StubType.BrowseByKanaRow, libraryId, kanaRowIndex);
            }
        }

        if (DidlBuilder.TryParseFacetClientId(id, out libraryId, out var facetStubType, out var facetKey))
        {
            var library = _libraryManager.GetItemById(libraryId);
            if (library is not null)
            {
                return new ServerItem(library, facetStubType, libraryId, facetKey: facetKey);
            }
        }

        if (DidlBuilder.TryParseSeriesRangeClientId(id, out libraryId, out var rangeStart, out var rangeEnd))
        {
            var library = _libraryManager.GetItemById(libraryId);
            if (library is not null)
            {
                return new ServerItem(library, StubType.SeriesRange, libraryId, rangeStart: rangeStart, rangeEnd: rangeEnd);
            }
        }

        if (DidlBuilder.TryParseYearClientId(id, out libraryId, out var productionYear))
        {
            var library = _libraryManager.GetItemById(libraryId);
            if (library is not null)
            {
                return new ServerItem(library, StubType.BrowseByYearItem, libraryId, productionYear: productionYear);
            }
        }

        var dividerIndex = id.IndexOf('_', StringComparison.Ordinal);
        if (dividerIndex != -1 && Enum.TryParse<StubType>(id.AsSpan(0, dividerIndex), true, out var parsedStubType))
        {
            id = id[(dividerIndex + 1)..];
            stubType = parsedStubType;
        }

        if (Guid.TryParse(id, out var itemId))
        {
            var item = _libraryManager.GetItemById(itemId);
            if (item is not null)
            {
                return new ServerItem(item, stubType);
            }
        }

        Logger.LogError("Error parsing item Id: {Id}. Returning user root folder.", id);

        return new ServerItem(_libraryManager.GetUserRootFolder(), null);
    }

    /// <summary>
    /// Returns recently released items of a specific type.
    /// </summary>
    /// <param name="parent">The parent item.</param>
    /// <param name="query">The query parameters.</param>
    /// <param name="itemType">The type of item to retrieve.</param>
    /// <param name="timing">Optional browse timing scope.</param>
    /// <returns>QueryResult of ServerItems.</returns>
    private QueryResult<ServerItem> GetRecentlyReleased(BaseItem parent, InternalItemsQuery query, BaseItemKind itemType, BrowseTimingScope? timing = null)
    {
        var config = GetPluginConfiguration();
        VirtualListType? listType = itemType switch
        {
            BaseItemKind.Episode when config.EnableIndexRecentlyReleasedEpisodes => VirtualListType.RecentlyReleasedEpisodes,
            BaseItemKind.Movie when config.EnableIndexRecentlyReleasedMovies => VirtualListType.RecentlyReleasedMovies,
            _ => null
        };

        if (listType is VirtualListType virtualListType)
        {
            var indexed = IndexBrowseHelper.TryGetVirtualList(
                _indexStore,
                _libraryManager,
                _indexService,
                config,
                parent,
                query,
                virtualListType,
                timing);
            if (indexed is not null)
            {
                return ApplyIndexedResult(indexed, timing);
            }
        }

        var orderBy = new (ItemSortBy, SortOrder)[]
        {
            (ItemSortBy.PremiereDate, SortOrder.Descending),
            (ItemSortBy.SortName, SortOrder.Ascending)
        };

        if (LibraryBrowseQueryHelper.IsDlnaLibraryView(parent))
        {
            var listQuery = new InternalItemsQuery(query.User)
            {
                OrderBy = orderBy,
                IncludeItemTypes = [itemType],
                IsPlaceHolder = false,
                DtoOptions = GetBrowseListDtoOptions()
            };
            ApplyLibraryQueryScope(listQuery, parent);
            PrepareItemsQuery(listQuery);

            var allItems = _libraryManager.GetItemsResult(listQuery);
            var totalCount = Math.Max(allItems.TotalRecordCount, allItems.Items.Count);

            return ToResult(null, totalCount, allItems.Items);
        }

        var fullQuery = new InternalItemsQuery(query.User)
        {
            OrderBy = orderBy,
            IncludeItemTypes = [itemType],
            IsPlaceHolder = false,
            DtoOptions = GetBrowseListDtoOptions()
        };
        ApplyLibraryQueryScope(fullQuery, parent);
        PrepareItemsQuery(fullQuery);

        var allResult = _libraryManager.GetItemsResult(fullQuery);
        var total = Math.Max(allResult.TotalRecordCount, allResult.Items.Count);

        return ToResult(null, total, allResult.Items);
    }

    /// <summary>
    /// Returns the extras items associated with a parent item.
    /// </summary>
    /// <param name="item">The parent item.</param>
    /// <param name="user">The user requesting items.</param>
    /// <param name="timing">Optional browse timing scope.</param>
    /// <returns>QueryResult of ServerItems.</returns>
    private QueryResult<ServerItem> GetExtrasItems(BaseItem item, User user, BrowseTimingScope? timing = null)
    {
        var query = new InternalItemsQuery(user)
        {
            DtoOptions = GetBrowseListDtoOptions()
        };
        var indexed = IndexBrowseHelper.TryGetExtras(
            _indexStore,
            _libraryManager,
            _indexService,
            GetPluginConfiguration(),
            item,
            query,
            timing);
        if (indexed is not null)
        {
            return ApplyIndexedResult(indexed, timing);
        }

        var extras = item.GetExtras().ToList();

        if (extras.Count == 0 && item is MediaBrowser.Controller.Entities.TV.Season season)
        {
            var extrasQuery = new InternalItemsQuery(user)
            {
                Parent = season,
                Recursive = true,
                IsPlaceHolder = false,
                IsVirtualItem = false,
                DtoOptions = GetBrowseListDtoOptions()
            };
            extras = _libraryManager.GetItemsResult(extrasQuery).Items
                .Where(i => i.ExtraType.HasValue)
                .ToList();
        }

        var serverItems = extras
            .Select(i => new ServerItem(i, null))
            .ToArray();

        return new QueryResult<ServerItem>(null, serverItems.Length, serverItems);
    }

    private BrowsePagingContext ResolveBrowsePaging(int? requestedCount, int? startIndex)
        => BrowsePagingResolver.Resolve(GetPluginConfiguration(), requestedCount, startIndex);

    private QueryResult<ServerItem> ApplyBrowsePaging(QueryResult<ServerItem> result)
    {
        if (_browsePaging is null)
        {
            return result;
        }

        var total = result.TotalRecordCount;
        var items = result.Items;

        if (_browsePaging.StartIndex > 0)
        {
            items = items.Skip(_browsePaging.StartIndex).ToArray();
        }

        if (_browsePaging.Limit.HasValue)
        {
            items = items.Take(_browsePaging.Limit.Value).ToArray();
        }

        if (!_browsePaging.StrictTotalMatches)
        {
            total = Math.Max(total, items.Count);
        }

        return new QueryResult<ServerItem>(_browsePaging.StartIndex, total, items);
    }

    private QueryResult<ServerItem> ApplyRecentlyAddedLimit(QueryResult<ServerItem> result)
    {
        var max = GetPluginConfiguration().MaxRecentlyAddedItems;
        if (max <= 0 || result.Items.Count <= max)
        {
            return result;
        }

        var items = result.Items.Take(max).ToArray();
        return new QueryResult<ServerItem>(result.StartIndex, items.Length, items);
    }

    private QueryResult<ServerItem> ApplySeriesListLimit(QueryResult<ServerItem> result)
    {
        var max = GetPluginConfiguration().MaxSeriesListItems;
        if (max <= 0 || result.Items.Count <= max)
        {
            return result;
        }

        var items = result.Items.Take(max).ToArray();
        return new QueryResult<ServerItem>(result.StartIndex, items.Length, items);
    }

    private QueryResult<ServerItem> GetRecentlyReleasedSeries(BaseItem parent, InternalItemsQuery query, BrowseTimingScope? timing = null)
    {
        var config = GetPluginConfiguration();
        if (config.EnableIndexRecentlyReleasedSeries)
        {
            var indexed = IndexBrowseHelper.TryGetVirtualList(
                _indexStore,
                _libraryManager,
                _indexService,
                config,
                parent,
                query,
                VirtualListType.RecentlyReleasedSeries,
                timing);
            if (indexed is not null)
            {
                return ApplyIndexedResult(indexed, timing);
            }
        }

        var listQuery = new InternalItemsQuery(query.User)
        {
            OrderBy =
            [
                (ItemSortBy.PremiereDate, SortOrder.Descending),
                (ItemSortBy.SortName, SortOrder.Ascending)
            ],
            IncludeItemTypes = [BaseItemKind.Episode],
            IsPlaceHolder = false,
            IsVirtualItem = false,
            DtoOptions = GetBrowseListDtoOptions()
        };
        ApplyLibraryQueryScope(listQuery, parent);
        PrepareItemsQuery(listQuery);

        var episodes = _libraryManager.GetItemsResult(listQuery).Items;
        var seenSeries = new HashSet<Guid>();
        var seriesList = new List<BaseItem>();

        foreach (var item in episodes)
        {
            if (item is not MediaBrowser.Controller.Entities.TV.Episode episode)
            {
                continue;
            }

            var seriesId = episode.SeriesId;
            if (seriesId == Guid.Empty || !seenSeries.Add(seriesId))
            {
                continue;
            }

            var series = _libraryManager.GetItemById(seriesId);
            if (series is not null)
            {
                seriesList.Add(series);
            }
        }

        return ToResult(null, seriesList.Count, seriesList);
    }

    private QueryResult<ServerItem> GetRecentlyModified(
        BaseItem parent,
        InternalItemsQuery query,
        VirtualListType listType,
        BaseItemKind itemType,
        BrowseTimingScope? timing = null)
    {
        var config = GetPluginConfiguration();
        if (IsVirtualListBrowseEnabled(config, listType))
        {
            var indexed = IndexBrowseHelper.TryGetVirtualList(
                _indexStore,
                _libraryManager,
                _indexService,
                config,
                parent,
                query,
                listType,
                timing);
            if (indexed is not null)
            {
                return ApplyIndexedResult(indexed, timing);
            }
        }

        var listQuery = new InternalItemsQuery(query.User)
        {
            OrderBy =
            [
                (ItemSortBy.DateCreated, SortOrder.Descending),
                (ItemSortBy.SortName, SortOrder.Ascending)
            ],
            IncludeItemTypes = [itemType],
            IsPlaceHolder = false,
            IsVirtualItem = false,
            DtoOptions = GetBrowseListDtoOptions()
        };
        ApplyLibraryQueryScope(listQuery, parent);
        PrepareItemsQuery(listQuery);

        return ApplyRecentlyAddedLimit(ToResult(null, _libraryManager.GetItemsResult(listQuery)));
    }

    private static bool IsVirtualListBrowseEnabled(DlnaPluginConfiguration config, VirtualListType listType)
        => listType switch
        {
            VirtualListType.RecentlyModifiedEpisodes => config.EnableIndexRecentlyModifiedEpisodes,
            VirtualListType.RecentlyModifiedMovies => config.EnableIndexRecentlyModifiedMovies,
            VirtualListType.RecentlyModifiedSeries => config.EnableIndexRecentlyModifiedSeries,
            _ => true
        };

    private QueryResult<ServerItem> GetRecentlyUpdatedSeries(BaseItem parent, InternalItemsQuery query, BrowseTimingScope? timing = null)
    {
        var indexed = IndexBrowseHelper.TryGetVirtualList(
            _indexStore,
            _libraryManager,
            _indexService,
            GetPluginConfiguration(),
            parent,
            query,
            VirtualListType.RecentlyUpdatedSeries,
            timing);
        if (indexed is not null)
        {
            return ApplyIndexedResult(indexed, timing);
        }

        return ApplyRecentlyAddedLimit(GetRecentlyAdded(parent, query, BaseItemKind.Series, timing));
    }

    private QueryResult<ServerItem> GetFacetFolders(BaseItem library, FacetType facetType, StubType folderStubType, BrowseTimingScope? timing)
    {
        var folders = IndexBrowseHelper.GetFacetFolders(
            _indexStore,
            _indexService,
            GetPluginConfiguration(),
            library,
            facetType,
            folderStubType);
        if (folders.Count > 0)
        {
            MarkIndexHit(timing);
        }

        return new QueryResult<ServerItem>(null, folders.Count, folders.ToArray());
    }

    private QueryResult<ServerItem> GetFacetItems(
        BaseItem library,
        InternalItemsQuery query,
        FacetType facetType,
        string? facetKey,
        BrowseTimingScope? timing)
    {
        if (string.IsNullOrWhiteSpace(facetKey))
        {
            return new QueryResult<ServerItem>();
        }

        var indexed = IndexBrowseHelper.TryGetFacetItems(
            _indexStore,
            _libraryManager,
            _indexService,
            GetPluginConfiguration(),
            library,
            query,
            facetType,
            facetKey,
            timing);
        if (indexed is not null)
        {
            return ApplyIndexedResult(indexed, timing);
        }

        return new QueryResult<ServerItem>();
    }

    private QueryResult<ServerItem> GetSeriesRangeItems(
        BaseItem library,
        InternalItemsQuery query,
        int rangeStart,
        int rangeEnd,
        BrowseTimingScope? timing)
    {
        var indexed = IndexBrowseHelper.TryGetSeriesRange(
            _indexStore,
            _libraryManager,
            _indexService,
            GetPluginConfiguration(),
            library,
            query,
            rangeStart,
            rangeEnd,
            timing);
        if (indexed is not null)
        {
            return ApplySeriesListLimit(ApplyIndexedResult(indexed, timing));
        }

        return ApplySeriesListLimit(GetChildrenOfItem(library, query, BaseItemKind.Series));
    }

    private QueryResult<ServerItem> GetCurrentlyAiringSeries(BaseItem parent, InternalItemsQuery query)
    {
        var listQuery = new InternalItemsQuery(query.User)
        {
            OrderBy = query.OrderBy,
            IncludeItemTypes = [BaseItemKind.Series],
            SeriesStatuses = [SeriesStatus.Continuing],
            IsPlaceHolder = false,
            IsVirtualItem = false,
            DtoOptions = GetBrowseListDtoOptions()
        };
        ApplyLibraryQueryScope(listQuery, parent);
        PrepareItemsQuery(listQuery);

        var allItems = _libraryManager.GetItemsResult(listQuery);
        var totalCount = Math.Max(allItems.TotalRecordCount, allItems.Items.Count);

        return ToResult(null, totalCount, allItems.Items);
    }

    private static QueryResult<ServerItem> GetBrowseByKanaRows(BaseItem library)
    {
        var rows = KanaRowHelper.CreateRowFolders(library);
        return new QueryResult<ServerItem>(null, rows.Length, rows);
    }

    private QueryResult<ServerItem> GetBrowseByKanaRowItems(BaseItem parent, InternalItemsQuery query, BaseItemKind itemType, int rowIndex, BrowseTimingScope? timing = null)
    {
        var indexed = IndexBrowseHelper.TryGetKanaRow(
            _indexStore,
            _libraryManager,
            _indexService,
            GetPluginConfiguration(),
            parent,
            query,
            itemType,
            rowIndex,
            timing);
        if (indexed is not null)
        {
            return ApplyIndexedResult(indexed, timing);
        }

        var kanaOptions = KanaClassificationOptions.FromConfiguration(GetPluginConfiguration());
        var listQuery = new InternalItemsQuery(query.User)
        {
            OrderBy = [(ItemSortBy.SortName, SortOrder.Ascending)],
            IncludeItemTypes = [itemType],
            IsPlaceHolder = false,
            IsVirtualItem = false,
            DtoOptions = GetBrowseListDtoOptions()
        };
        ApplyLibraryQueryScope(listQuery, parent);
        PrepareItemsQuery(listQuery);

        var filtered = _libraryManager.GetItemsResult(listQuery).Items
            .Where(i => KanaRowHelper.MatchesRow(i.SortName, i.Name, rowIndex, kanaOptions))
            .ToList();

        return ToResult(null, filtered.Count, filtered);
    }

    private QueryResult<ServerItem> GetMixedBrowseByKanaRowItems(
        BaseItem parent,
        InternalItemsQuery query,
        int rowIndex,
        BrowseTimingScope? timing = null)
    {
        var indexed = IndexBrowseHelper.TryGetMixedKanaRow(
            _indexStore,
            _libraryManager,
            _indexService,
            GetPluginConfiguration(),
            parent,
            query,
            rowIndex,
            timing);
        if (indexed is not null)
        {
            return ApplyIndexedResult(indexed, timing);
        }

        var kanaOptions = KanaClassificationOptions.FromConfiguration(GetPluginConfiguration());
        var listQuery = new InternalItemsQuery(query.User)
        {
            OrderBy = [(ItemSortBy.SortName, SortOrder.Ascending)],
            IncludeItemTypes = [BaseItemKind.Series, BaseItemKind.Movie],
            IsPlaceHolder = false,
            IsVirtualItem = false,
            DtoOptions = GetBrowseListDtoOptions()
        };
        ApplyLibraryQueryScope(listQuery, parent);
        PrepareItemsQuery(listQuery);

        var filtered = _libraryManager.GetItemsResult(listQuery).Items
            .Where(i => KanaRowHelper.MatchesRow(i.SortName, i.Name, rowIndex, kanaOptions))
            .ToList();

        return ToResult(null, filtered.Count, filtered);
    }

    private QueryResult<ServerItem> GetBrowseByYearFolders(BaseItem parent, BaseItemKind itemType, BrowseTimingScope? timing = null)
    {
        var indexed = IndexBrowseHelper.TryGetYearFolders(
            _indexStore,
            _indexService,
            GetPluginConfiguration(),
            parent,
            timing);
        if (indexed is not null)
        {
            MarkIndexHit(timing);
            return new QueryResult<ServerItem>(null, indexed.Count, indexed.ToArray());
        }

        var listQuery = new InternalItemsQuery(_user)
        {
            IncludeItemTypes = [itemType],
            IsPlaceHolder = false,
            IsVirtualItem = false,
            EnableTotalRecordCount = false,
            DtoOptions = new DtoOptions(false)
        };
        ApplyLibraryQueryScope(listQuery, parent);
        PrepareItemsQuery(listQuery);

        var years = _libraryManager.GetItemsResult(listQuery).Items
            .Where(i => i.ProductionYear.HasValue)
            .Select(i => i.ProductionYear!.Value)
            .Distinct()
            .OrderByDescending(y => y)
            .Select(y => new ServerItem(parent, StubType.BrowseByYearItem, parent.Id, productionYear: y))
            .ToArray();

        return new QueryResult<ServerItem>(null, years.Length, years);
    }

    private QueryResult<ServerItem> GetMixedBrowseByYearFolders(BaseItem parent, BrowseTimingScope? timing = null)
    {
        var indexed = IndexBrowseHelper.TryGetYearFolders(
            _indexStore,
            _indexService,
            GetPluginConfiguration(),
            parent,
            timing);
        if (indexed is not null)
        {
            MarkIndexHit(timing);
            return new QueryResult<ServerItem>(null, indexed.Count, indexed.ToArray());
        }

        var listQuery = new InternalItemsQuery(_user)
        {
            IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Series],
            IsPlaceHolder = false,
            IsVirtualItem = false,
            EnableTotalRecordCount = false,
            DtoOptions = new DtoOptions(false)
        };
        ApplyLibraryQueryScope(listQuery, parent);
        PrepareItemsQuery(listQuery);

        var years = _libraryManager.GetItemsResult(listQuery).Items
            .Where(i => i.ProductionYear.HasValue)
            .Select(i => i.ProductionYear!.Value)
            .Distinct()
            .OrderByDescending(y => y)
            .Select(y => new ServerItem(parent, StubType.BrowseByYearItem, parent.Id, productionYear: y))
            .ToArray();

        return new QueryResult<ServerItem>(null, years.Length, years);
    }

    private QueryResult<ServerItem> GetMixedBrowseByYearItems(
        BaseItem parent,
        InternalItemsQuery query,
        int? productionYear,
        BrowseTimingScope? timing = null)
    {
        if (!productionYear.HasValue)
        {
            return new QueryResult<ServerItem>();
        }

        var yearKey = productionYear.Value.ToString(CultureInfo.InvariantCulture);
        var indexed = IndexBrowseHelper.TryGetFacetItems(
            _indexStore,
            _libraryManager,
            _indexService,
            GetPluginConfiguration(),
            parent,
            query,
            FacetType.Year,
            yearKey,
            timing);
        if (indexed is not null)
        {
            return ApplyIndexedResult(indexed, timing);
        }

        var listQuery = new InternalItemsQuery(query.User)
        {
            OrderBy = query.OrderBy,
            IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Series],
            Years = [productionYear.Value],
            IsPlaceHolder = false,
            IsVirtualItem = false,
            DtoOptions = GetBrowseListDtoOptions()
        };
        ApplyLibraryQueryScope(listQuery, parent);
        PrepareItemsQuery(listQuery);

        var allItems = _libraryManager.GetItemsResult(listQuery);
        var totalCount = Math.Max(allItems.TotalRecordCount, allItems.Items.Count);

        return ToResult(null, totalCount, allItems.Items);
    }

    private QueryResult<ServerItem> GetBrowseByYearItems(BaseItem parent, InternalItemsQuery query, BaseItemKind itemType, int? productionYear, BrowseTimingScope? timing = null)
    {
        if (!productionYear.HasValue)
        {
            return new QueryResult<ServerItem>();
        }

        var yearKey = productionYear.Value.ToString(CultureInfo.InvariantCulture);
        var indexed = IndexBrowseHelper.TryGetFacetItems(
            _indexStore,
            _libraryManager,
            _indexService,
            GetPluginConfiguration(),
            parent,
            query,
            FacetType.Year,
            yearKey,
            timing);
        if (indexed is not null)
        {
            return ApplyIndexedResult(indexed, timing);
        }

        var listQuery = new InternalItemsQuery(query.User)
        {
            OrderBy = query.OrderBy,
            IncludeItemTypes = [itemType],
            Years = [productionYear.Value],
            IsPlaceHolder = false,
            IsVirtualItem = false,
            DtoOptions = GetBrowseListDtoOptions()
        };
        ApplyLibraryQueryScope(listQuery, parent);
        PrepareItemsQuery(listQuery);

        var allItems = _libraryManager.GetItemsResult(listQuery);
        var totalCount = Math.Max(allItems.TotalRecordCount, allItems.Items.Count);

        return ToResult(null, totalCount, allItems.Items);
    }

    private static DlnaPluginConfiguration GetPluginConfiguration()
        => DlnaPlugin.Instance?.Configuration ?? new DlnaPluginConfiguration();

    private QueryResult<ServerItem>? TryGetIndexedLibraryChildren(
        BaseItem parent,
        InternalItemsQuery query,
        BaseItemKind itemType,
        BrowseTimingScope? timing)
    {
        if (!LibraryBrowseQueryHelper.IsDlnaLibraryView(parent))
        {
            return null;
        }

        VirtualListType? listType = itemType switch
        {
            BaseItemKind.Series => VirtualListType.SeriesAll,
            BaseItemKind.Movie => VirtualListType.MoviesAll,
            _ => null
        };
        if (listType is null)
        {
            return null;
        }

        var indexed = IndexBrowseHelper.TryGetVirtualList(
            _indexStore,
            _libraryManager,
            _indexService,
            GetPluginConfiguration(),
            parent,
            query,
            listType.Value,
            timing);
        if (indexed is null)
        {
            return null;
        }

        return ApplyIndexedResult(indexed, timing);
    }

    private QueryResult<ServerItem>? TryGetIndexedSeriesOrSeasonChildren(
        BaseItem item,
        User? user,
        InternalItemsQuery query,
        BrowseTimingScope? timing)
    {
        FacetType? facetType = item switch
        {
            MediaBrowser.Controller.Entities.TV.Series => FacetType.SeasonOfSeries,
            MediaBrowser.Controller.Entities.TV.Season => FacetType.EpisodeOfSeason,
            _ => null
        };
        if (facetType is null)
        {
            return null;
        }

        var indexed = IndexBrowseHelper.TryGetParentChildren(
            _indexStore,
            _libraryManager,
            _indexService,
            GetPluginConfiguration(),
            item,
            query,
            facetType.Value,
            timing);
        if (indexed is null)
        {
            return null;
        }

        return AppendSeriesSeasonExtrasStub(item, user, ApplyIndexedResult(indexed, timing));
    }

    private QueryResult<ServerItem> AppendSeriesSeasonExtrasStub(BaseItem item, User? user, QueryResult<ServerItem> result)
    {
        if (DlnaPlugin.Instance?.Configuration.EnableExtras != true)
        {
            return result;
        }

        if (item is not MediaBrowser.Controller.Entities.TV.Series and not MediaBrowser.Controller.Entities.TV.Season)
        {
            return result;
        }

        try
        {
            var hasExtras = item.GetExtras().Any();
            if (!hasExtras && item is MediaBrowser.Controller.Entities.TV.Season season && user is not null)
            {
                var extrasCheckQuery = new InternalItemsQuery(user)
                {
                    Parent = season,
                    Recursive = true,
                    IsPlaceHolder = false,
                    IsVirtualItem = false,
                    Limit = 1,
                    DtoOptions = GetBrowseListDtoOptions()
                };
                hasExtras = _libraryManager.GetItemsResult(extrasCheckQuery)
                    .Items.Any(i => i.ExtraType.HasValue);
            }

            if (hasExtras)
            {
                var itemsList = result.Items.ToList();
                itemsList.Add(new ServerItem(item, StubType.Extras));
                return new QueryResult<ServerItem>(
                    result.StartIndex,
                    result.TotalRecordCount + 1,
                    itemsList.ToArray());
            }
        }
        catch
        {
            // Silently ignore - extras display is optional
        }

        return result;
    }

    private bool MovieHasExtras(ServerItem movieItem, Guid libraryId)
    {
        if (!movieItem.IsSummaryBacked)
        {
            return movieItem.Item.GetExtras().Any();
        }

        var parentKey = movieItem.Summary!.ItemId.ToString("N", CultureInfo.InvariantCulture);
        return _indexStore.GetFacetItems(libraryId, FacetType.Extra, parentKey).Count > 0;
    }

    private QueryResult<ServerItem> ApplyIndexedResult(BrowsableQueryResult indexed, BrowseTimingScope? timing)
    {
        MarkIndexHit(timing);
        if (indexed.SummaryHit)
        {
            MarkSummaryHit(timing);
        }

        return new QueryResult<ServerItem>(null, indexed.TotalRecordCount, indexed.Items.ToArray());
    }

    private static void MarkSummaryHit(BrowseTimingScope? timing)
    {
        if (timing is not null)
        {
            timing.SummaryHit = true;
        }
    }

    private static void MarkIndexHit(BrowseTimingScope? timing)
    {
        if (timing is not null)
        {
            timing.IndexHit = true;
        }
    }
}
