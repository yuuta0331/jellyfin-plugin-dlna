using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Jellyfin.Plugin.Dlna.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.Indexing;

/// <summary>
/// Executes Browse SOAP requests to populate the response cache after indexing.
/// </summary>
public sealed class DlnaBrowsePrewarmService : IDlnaBrowsePrewarmService
{
    private const string ContentDirectoryNamespace = "urn:schemas-upnp-org:service:ContentDirectory:1";

    private readonly IServiceProvider _serviceProvider;
    private readonly ILibraryManager _libraryManager;
    private readonly IVirtualIndexStore _indexStore;
    private readonly IDlnaVirtualIndexService _indexService;
    private readonly IServerApplicationHost _applicationHost;
    private readonly INetworkManager _networkManager;
    private readonly ILogger<DlnaBrowsePrewarmService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DlnaBrowsePrewarmService"/> class.
    /// </summary>
    public DlnaBrowsePrewarmService(
        IServiceProvider serviceProvider,
        ILibraryManager libraryManager,
        IVirtualIndexStore indexStore,
        IDlnaVirtualIndexService indexService,
        IServerApplicationHost applicationHost,
        INetworkManager networkManager,
        ILogger<DlnaBrowsePrewarmService> logger)
    {
        _serviceProvider = serviceProvider;
        _libraryManager = libraryManager;
        _indexStore = indexStore;
        _indexService = indexService;
        _applicationHost = applicationHost;
        _networkManager = networkManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PrewarmAsync(Guid? libraryId, CancellationToken cancellationToken)
    {
        var config = DlnaPlugin.Instance.Configuration;
        if (!config.PrewarmBrowseResponses)
        {
            return;
        }

        if (!config.EnableBrowseResponseCache && !config.EnableQuestCompatibilityMode)
        {
            return;
        }

        var libraries = GetTargetLibraries(libraryId);
        if (libraries.Count == 0)
        {
            return;
        }

        var serverId = _applicationHost.SystemId;
        var requestedUrl = BuildPrewarmRequestedUrl(serverId);

        var prewarmed = 0;
        foreach (var library in libraries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var objectIds = BrowsePrewarmPaths.GetObjectIds(config, library, _indexStore, _indexService, _libraryManager);
            foreach (var objectId in objectIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await ExecuteBrowseAsync(objectId, requestedUrl, serverId, cancellationToken).ConfigureAwait(false);
                    prewarmed++;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "DLNA browse prewarm failed for ObjectID={ObjectId}", objectId);
                }
            }
        }

        _logger.LogInformation(
            "DLNA browse prewarm completed Libraries={LibraryCount} Responses={ResponseCount}",
            libraries.Count,
            prewarmed);
    }

    private IReadOnlyList<BaseItem> GetTargetLibraries(Guid? libraryId)
    {
        if (libraryId is Guid id)
        {
            var library = _libraryManager.GetItemById(id);
            if (library is null || !LibraryBrowseQueryHelper.IsDlnaLibraryView(library))
            {
                return [];
            }

            return [library];
        }

        return _libraryManager.GetUserRootFolder().Children
            .Where(LibraryBrowseQueryHelper.IsDlnaLibraryView)
            .ToList();
    }

    private async Task ExecuteBrowseAsync(
        string objectId,
        string requestedUrl,
        string serverId,
        CancellationToken cancellationToken)
    {
        using var stream = CreateBrowseRequestStream(objectId);
        var request = new ControlRequest(new HeaderDictionary())
        {
            InputXml = stream,
            TargetServerUuId = serverId,
            RequestedUrl = requestedUrl
        };

        var contentDirectory = _serviceProvider.GetRequiredService<IContentDirectory>();
        await contentDirectory.ProcessControlRequestAsync(request).ConfigureAwait(false);
    }

    private string BuildPrewarmRequestedUrl(string serverId)
    {
        var bindAddress = GetPreferredLanBindAddress();
        var serverAddress = _applicationHost.GetSmartApiUrl(bindAddress);
        if (string.IsNullOrWhiteSpace(serverAddress))
        {
            serverAddress = string.Format(CultureInfo.InvariantCulture, "http://{0}", bindAddress);
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}/dlna/{1}/contentdirectory/control",
            serverAddress.TrimEnd('/'),
            serverId);
    }

    private IPAddress GetPreferredLanBindAddress()
    {
        var validInterfaces = _networkManager.GetInternalBindAddresses()
            .Where(x => x.Address is not null)
            .Where(x => x.AddressFamily == AddressFamily.InterNetwork)
            .Where(x => !x.Address!.Equals(IPAddress.Loopback))
            .Where(x => x.SupportsMulticast)
            .ToList();

        if (validInterfaces.Count > 0)
        {
            return validInterfaces[0].Address!;
        }

        var loopbacks = _networkManager.GetLoopbacks().ToList();
        if (loopbacks.Count > 0 && loopbacks[0].Address is not null)
        {
            return loopbacks[0].Address;
        }

        return IPAddress.Loopback;
    }

    private static MemoryStream CreateBrowseRequestStream(string objectId)
    {
        var stream = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            CloseOutput = false,
            OmitXmlDeclaration = false
        };

        using (var writer = XmlWriter.Create(stream, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("s", "Envelope", "http://schemas.xmlsoap.org/soap/envelope/");
            writer.WriteAttributeString("s", "encodingStyle", null, "http://schemas.xmlsoap.org/soap/encoding/");
            writer.WriteStartElement("s", "Body", null);
            writer.WriteStartElement("u", "Browse", ContentDirectoryNamespace);
            writer.WriteElementString("ObjectID", objectId);
            writer.WriteElementString("BrowseFlag", "BrowseDirectChildren");
            writer.WriteElementString("Filter", "*");
            writer.WriteElementString("StartingIndex", "0");
            writer.WriteElementString("RequestedCount", "0");
            writer.WriteElementString("SortCriteria", string.Empty);
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        stream.Position = 0;
        return stream;
    }
}
