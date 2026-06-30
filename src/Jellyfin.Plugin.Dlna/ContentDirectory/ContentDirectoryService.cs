using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Plugin.Dlna.Indexing;
using Jellyfin.Plugin.Dlna.Model;
using Jellyfin.Plugin.Dlna.Service;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Globalization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

/// <summary>
/// Defines the <see cref="ContentDirectoryService" />.
/// </summary>
public class ContentDirectoryService : BaseService, IContentDirectory
{
    private readonly ILibraryManager _libraryManager;
    private readonly IImageProcessor _imageProcessor;
    private readonly IUserDataManager _userDataManager;
    private readonly IDlnaManager _dlna;
    private readonly IUserManager _userManager;
    private readonly ILocalizationManager _localization;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly IUserViewManager _userViewManager;
    private readonly IMediaEncoder _mediaEncoder;
    private readonly ITVSeriesManager _tvSeriesManager;
    private readonly IBrowseResponseCache _browseResponseCache;
    private readonly IBrowseNodeCache _browseNodeCache;
    private readonly ChildCountCache _childCountCache;
    private readonly LibraryChangeNotifier _libraryChangeNotifier;
    private readonly IVirtualIndexStore _indexStore;
    private readonly IDlnaVirtualIndexService _indexService;
    private readonly IBrowseMetrics _browseMetrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentDirectoryService"/> class.
    /// </summary>
    /// <param name="dlna">The <see cref="IDlnaManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
    /// <param name="userDataManager">The <see cref="IUserDataManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
    /// <param name="imageProcessor">The <see cref="IImageProcessor"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
    /// <param name="libraryManager">The <see cref="ILibraryManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
    /// <param name="userManager">The <see cref="IUserManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
    /// <param name="logger">The <see cref="ILogger{ContentDirectoryService}"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
    /// <param name="httpClient">The <see cref="IHttpClientFactory"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
    /// <param name="localization">The <see cref="ILocalizationManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
    /// <param name="mediaSourceManager">The <see cref="IMediaSourceManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
    /// <param name="userViewManager">The <see cref="IUserViewManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
    /// <param name="mediaEncoder">The <see cref="IMediaEncoder"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
    /// <param name="tvSeriesManager">The <see cref="ITVSeriesManager"/> to use in the <see cref="ContentDirectoryService"/> instance.</param>
    /// <param name="browseResponseCache">The browse response cache.</param>
    /// <param name="browseNodeCache">The browse node cache.</param>
    /// <param name="childCountCache">The child count cache.</param>
    /// <param name="libraryChangeNotifier">The library change notifier.</param>
    /// <param name="indexStore">The virtual index store.</param>
    /// <param name="indexService">The virtual index service.</param>
    /// <param name="browseMetrics">The browse metrics collector.</param>
    public ContentDirectoryService(
        IDlnaManager dlna,
        IUserDataManager userDataManager,
        IImageProcessor imageProcessor,
        ILibraryManager libraryManager,
        IUserManager userManager,
        ILogger<ContentDirectoryService> logger,
        IHttpClientFactory httpClient,
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
        _dlna = dlna;
        _userDataManager = userDataManager;
        _imageProcessor = imageProcessor;
        _libraryManager = libraryManager;
        _userManager = userManager;
        _localization = localization;
        _mediaSourceManager = mediaSourceManager;
        _userViewManager = userViewManager;
        _mediaEncoder = mediaEncoder;
        _tvSeriesManager = tvSeriesManager;
        _browseResponseCache = browseResponseCache;
        _browseNodeCache = browseNodeCache;
        _childCountCache = childCountCache;
        _libraryChangeNotifier = libraryChangeNotifier;
        _indexStore = indexStore;
        _indexService = indexService;
        _browseMetrics = browseMetrics;
    }

    /// <inheritdoc />
    public string GetServiceXml()
    {
        return ContentDirectoryXmlBuilder.GetXml();
    }

    /// <inheritdoc />
    public Task<ControlResponse> ProcessControlRequestAsync(ControlRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profile = _dlna.ResolveStreamingProfile(request.Headers).Profile;

        var serverAddress = request.RequestedUrl[..request.RequestedUrl.IndexOf("/dlna", StringComparison.OrdinalIgnoreCase)];

        var user = GetUser(profile);

        return new ControlHandler(
                Logger,
                _libraryManager,
                profile,
                serverAddress,
                null,
                _imageProcessor,
                _userDataManager,
                user,
                _libraryChangeNotifier.LibraryGeneration,
                _localization,
                _mediaSourceManager,
                _userViewManager,
                _mediaEncoder,
                _tvSeriesManager,
                _browseResponseCache,
                _browseNodeCache,
                _childCountCache,
                _libraryChangeNotifier,
                _indexStore,
                _indexService,
                _browseMetrics)
            .ProcessControlRequestAsync(request);
    }

    /// <summary>
    /// Get the user stored in the device profile.
    /// </summary>
    /// <param name="profile">The <see cref="DeviceProfile"/>.</param>
    /// <returns>The <see cref="User"/>.</returns>
    private User? GetUser(DlnaDeviceProfile profile)
    {
        if (!string.IsNullOrEmpty(profile.UserId))
        {
            var user = _userManager.GetUserById(Guid.Parse(profile.UserId));

            if (user is not null)
            {
                return user;
            }
        }

        var userId = DlnaPlugin.Instance.Configuration.DefaultUserId;

        if (userId is not null && !userId.Equals(default))
        {
            var user = _userManager.GetUserById(userId.Value);

            if (user is not null)
            {
                return user;
            }
        }

        foreach (var user in _userManager.GetUsers())
        {
            if (user.HasPermission(PermissionKind.IsAdministrator))
            {
                return user;
            }
        }

        return _userManager.GetUsers().FirstOrDefault();
    }
}
