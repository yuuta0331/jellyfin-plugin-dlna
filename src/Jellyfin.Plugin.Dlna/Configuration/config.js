const DlnaConfigurationPage = {
    pluginUniqueId: '33EBA9CD-7DA1-4720-967F-DD7DAE7B74A1',
    defaultDiscoveryInterval: 60,
    defaultAliveInterval: 180,
    defaultTitleBrowsePresetId: 'alphabet',
    libraryTitleBrowseOverrides: [],
    storageTabIndex: 4,
    translations: {
        'en': {
            'TitleDlnaSettings': 'DLNA Settings',
            'TabGeneral': 'General',
            'TabLibrary': 'Library',
            'TabBrowse': 'Browse',
            'TabIndex': 'Index',
            'TabStorage': 'Storage',
            'SectionGeneral': 'General Settings',
            'GeneralIndexStartupNote': 'Startup index rebuild can be configured on the Index tab.',
            'EnablePlayTo': 'Enable Play To',
            'EnablePlayToHelp': 'Allows DLNA devices to remotely control playback on this server.',
            'DefaultUser': 'Default User:',
            'DefaultUserHelp': 'User whose library is exposed when a DLNA client does not authenticate.',
            'None': 'None',
            'SectionNetwork': 'SSDP / Network Settings',
            'ClientDiscoveryInterval': 'Client Discovery Interval:',
            'ClientDiscoveryIntervalHelp': 'The SSDP client discovery interval time in seconds. This is the time after which the server will send a SSDP search request.',
            'BlastAliveMessages': 'Blast Alive Messages',
            'BlastAliveMessagesHelp': 'Sends multiple SSDP alive announcements at once when the server starts.',
            'AliveMessageInterval': 'Alive Message Interval:',
            'AliveMessageIntervalHelp': 'The frequency at which SSDP alive notifications are transmitted in seconds.',
            'SendOnlyMatchedHost': 'Send only to matched host',
            'SendOnlyMatchedHostHelp': 'Restricts SSDP responses to clients on the same subnet as the server.',
            'SectionSeries': 'Series (TV Shows) Settings',
            'EnableRecentlyAddedEpisodes': 'Enable Recently Added Episodes',
            'EnableRecentlyAddedSeries': 'Enable Recently Added Series',
            'EnableRecentlyReleasedEpisodes': 'Enable Recently Released Episodes',
            'EnableRecentlyReleasedSeries': 'Enable Recently Released Series',
            'EnableCurrentlyAiring': 'Enable Currently Airing',
            'EnableBrowseByKana': 'Enable Browse By Title',
            'ActiveTitleBrowsePresetId': 'Active Title Browse Preset:',
            'ActiveTitleBrowsePresetIdHelp': 'Choose the default grouping preset for title browse folders (A-Z, Japanese kana, or custom).',
            'HideEmptyVirtualFolders': 'Hide empty virtual folders',
            'TitleBrowsePresetsJson': 'Title Browse Presets (JSON):',
            'TitleBrowsePresetsJsonHelp': 'Advanced: edit preset definitions as JSON. Built-in alphabet and Japanese kana presets are preserved on save.',
            'CollapsibleAdvancedHint': '(click to expand/collapse)',
            'SectionLibraryTitleBrowse': 'Per-Library Title Browse',
            'LibraryTitleBrowseLibraryId': 'Library:',
            'LibraryTitleBrowsePresetOverride': 'Preset Override:',
            'LibraryTitleStripRegexes': 'Title Strip Regexes:',
            'LibraryTitleStripRegexesHelp': 'One regex per line. Matched prefixes are removed before classification for the selected library.',
            'LibraryTitleBrowseUseGlobal': '(Use global preset)',
            'EnableBrowseByYear': 'Enable Browse By Year',
            'SectionMovies': 'Movie Settings',
            'EnableRecentlyAddedMovies': 'Enable Recently Added Movies',
            'EnableRecentlyReleasedMovies': 'Enable Recently Released Movies',
            'EnableThreeDMovies': 'Enable 3D Movies Folder',
            'EnableAuto3DTagging': 'Enable Auto 3D Tagging (for VR Auto-detection)',
            'EnableFourKMovies': 'Enable 4K Movies Folder',
            'EnableEightKMovies': 'Enable 8K Movies Folder',
            'EnableVrMovies': 'Enable VR Videos Folder',
            'EnableEightKVrMovies': 'Enable 8K VR Videos Folder',
            'EnableAutoResolutionTagging': 'Enable Auto Resolution Tagging (4K/8K)',
            'EnableAutoVrTagging': 'Enable Auto VR Tagging (VR180/VR360)',
            'SectionBrowse': 'Browse / Compatibility Settings',
            'SectionBrowseQuest': 'Quest Compatibility',
            'SectionBrowseCache': 'Response Caching',
            'SectionBrowseImages': 'Image Presentation',
            'SectionBrowsePaging': 'Paging and Limits',
            'EnableQuestCompatibilityMode': 'Quest Compatibility Mode',
            'SectionIndex': 'Virtual Index Settings',
            'SectionIndexBasic': 'Index Basics',
            'SectionIndexPrewarm': 'Prewarm',
            'SectionIndexTargets': 'Index Targets',
            'SectionIndexTargetsHelp': 'Choose which library views are stored in the index database for fast DLNA browsing.',
            'SectionIndexFolders': 'Browse Folders',
            'SectionIndexLibraryChanges': 'Library Changes',
            'SectionIndexLargeFolders': 'Large Folder Splitting',
            'EnableVirtualFolderIndex': 'Enable virtual folder index',
            'EnableVirtualFolderIndexHelp': 'Builds a SQLite database for fast virtual folders such as recently added, genres, and kana rows.',
            'WarmupIndexOnStartup': 'Rebuild index on startup',
            'WarmupIndexOnStartupHelp': 'Rebuilds the SQLite index database when Jellyfin starts. Turn off to skip startup rebuild until the first browse or a manual rebuild.',
            'PrewarmBrowseResponses': 'Pre-generate folder listings after indexing',
            'PrewarmBrowseResponsesHelp': 'After indexing, pre-generates Browse XML for major folders so clients get instant responses on first access.',
            'PrewarmFacetItemFolders': 'Prewarm studio/tag/rating item folders',
            'PrewarmFacetItemFoldersHelp': 'Also pre-generates listings inside studio, tag, and rating subfolders. Disable for very large libraries.',
            'EnableIndexGenre': 'Index genres',
            'EnableIndexGenreHelp': 'Stores genre folders in the index instead of scanning the library each time.',
            'EnableIndexYear': 'Index production years',
            'EnableIndexYearHelp': 'Stores year-based browse folders in the index.',
            'EnableIndexRecentlyReleasedEpisodes': 'Index recently released episodes',
            'EnableIndexRecentlyReleasedSeries': 'Index recently released series',
            'EnableIndexRecentlyReleasedMovies': 'Index recently released movies',
            'EnableIndexSeriesList': 'Index full series list',
            'EnableIndexSeriesListHelp': 'Stores the full series list per TV library in the index.',
            'EnableIndexMoviesList': 'Index full movies list',
            'EnableIndexMoviesListHelp': 'Stores the full movie list per movies library in the index.',
            'EnableIndexSeasonList': 'Index seasons under series',
            'EnableIndexEpisodeList': 'Index episodes under seasons',
            'RebuildIndexAfterLibraryScan': 'Rebuild index after library changes',
            'RebuildIndexAfterLibraryScanHelp': 'Rebuilds the virtual index when media libraries are scanned or updated.',
            'DebounceLibraryChangeInvalidation': 'Debounce cache invalidation',
            'DebounceLibraryChangeInvalidationHelp': 'Waits before clearing caches when multiple library changes occur in quick succession.',
            'LibraryChangeDebounceSeconds': 'Library change debounce (seconds):',
            'LibraryChangeDebounceSecondsHelp': 'Seconds to wait after the last library change before invalidating caches.',
            'EnableRecentlyUpdatedSeries': 'Enable Recently Updated Series folder',
            'EnableBrowseByStudio': 'Enable Browse By Studio',
            'EnableBrowseByTag': 'Enable Browse By Tag',
            'EnableBrowseByRating': 'Enable Browse By Rating',
            'LargeFolderRangeSplitThreshold': 'Range split threshold:',
            'LargeFolderRangeSplitThresholdHelp': 'When a series list exceeds this count, it is split into range subfolders (e.g. A–D, E–H).',
            'RangeFolderSize': 'Series per range folder:',
            'RangeFolderSizeHelp': 'Number of series placed in each range subfolder when splitting large lists.',
            'EnableQuestCompatibilityModeHelp': 'Optimizes for Quest DLNA players: returns all items, omits childCount, strips stream URL query strings, and enables response caching. Playback is resolved server-side.',
            'EnsurePlaybackUrlsInBrowse': 'Include playback URLs in browse listings',
            'EnsurePlaybackUrlsInBrowseHelp': 'Adds video/audio stream URLs to indexed browse responses. Keep enabled for Meta Quest playback.',
            'ChildCountCalculation': 'Child Count Calculation:',
            'ChildCountCalculationHelp': 'Controls folder childCount attributes. Disabled or Estimate avoids N+1 queries (recommended for Quest).',
            'ChildCountDisabled': 'Disabled (omit)',
            'ChildCountEstimate': 'Estimate',
            'ChildCountAccurate': 'Accurate (slow)',
            'EnableChildCountCache': 'Cache childCount values',
            'EnableChildCountCacheHelp': 'Stores folder item counts from Browse operations. Accurate mode also queries counts when listing folders.',
            'EnableChildCountCacheHelpDisabled': 'Unavailable when childCount is omitted.',
            'EnableBrowseResponseCache': 'Cache Browse responses',
            'EnableBrowseResponseCacheHelp': 'Keeps generated Browse XML responses in memory so repeat folder access is faster.',
            'BrowseResponseCacheTtlSeconds': 'Browse response cache TTL (seconds):',
            'BrowseResponseCacheTtlSecondsHelp': 'How long Browse XML responses stay cached. "Until library update" clears the cache when a library scan completes.',
            'EnableItemSummaryBrowse': 'Use indexed item summaries (layer 2)',
            'EnableItemSummaryBrowseHelp': 'Uses pre-built item summaries from the index database for folder listings instead of querying Jellyfin each time.',
            'VirtualListImagePresentation': 'Virtual List Image Style:',
            'VirtualListImagePresentationHelp': 'Image style for movies/series virtual folders and similar lists. Default is poster (Primary).',
            'SearchImagePresentation': 'Search Result Image Style:',
            'SearchImagePresentationHelp': 'Image style for DLNA search results for movies and series. Episodes still use thumbnails.',
            'EpisodeListImageSource': 'Episode List Image Source:',
            'EpisodeListImageSourceHelp': 'Image source for recently added, recently released, favorite episodes, and similar lists. Episode mode uses the episode thumbnail when available, otherwise the series thumbnail.',
            'EpisodeListImageSourceEpisode': 'Episode thumbnail',
            'EpisodeListImageSourceSeries': 'Series thumbnail',
            'ImagePresentationPoster': 'Poster',
            'ImagePresentationThumbnail': 'Thumbnail',
            'EnableBrowseNodeCache': 'Cache child folder lists (layer 3)',
            'EnableBrowseNodeCacheHelp': 'Caches the list of child items under each folder in memory.',
            'BrowseNodeCacheTtlSeconds': 'Child folder list cache TTL (seconds):',
            'BrowseNodeCacheTtlSecondsHelp': 'How long child folder lists stay cached. "Until library update" keeps them until the next library scan completes.',
            'EnableIndexMusicGenre': 'Index music genres',
            'EnableIndexPerson': 'Index persons (cast)',
            'EnableBrowseByPerson': 'Enable Browse By Person',
            'EnableIndexRecentlyModifiedSeries': 'Index recently modified series',
            'EnableIndexRecentlyModifiedEpisodes': 'Index recently modified episodes',
            'EnableIndexRecentlyModifiedMovies': 'Index recently modified movies',
            'EnableRecentlyModifiedSeries': 'Recently Modified Series folder',
            'EnableRecentlyModifiedEpisodes': 'Recently Modified Episodes folder',
            'EnableRecentlyModifiedMovies': 'Recently Modified Movies folder',
            'PrewarmHierarchyFolders': 'Prewarm series/season folders (limited)',
            'PrewarmHierarchyFoldersHelp': 'Pre-generates Browse responses for physical series and season folders. Limited by the counts below.',
            'PrewarmHierarchyMaxSeries': 'Max series to prewarm:',
            'PrewarmHierarchyMaxSeriesHelp': 'Maximum number of series folders to prewarm. Use a lower value for large libraries.',
            'PrewarmHierarchyMaxSeasonsPerSeries': 'Max seasons per series to prewarm:',
            'PrewarmHierarchyMaxSeasonsPerSeriesHelp': 'Maximum seasons prewarmed under each series folder.',
            'BrowseCacheTtlUntilUpdate': 'Until library update',
            'MaxBrowseItemsPerResponse': 'Max items per Browse response:',
            'MaxBrowseItemsPerResponseHelp': 'Maximum number of items returned in a single DLNA Browse response. This is the upper limit the server sends per request.',
            'RespectRequestedCount': 'Respect client page size (RequestedCount)',
            'RespectRequestedCountHelp': 'When disabled, the full result set is returned in one response (recommended for BubbleUPnP).',
            'EnableStrictTotalMatches': 'Return accurate total item count',
            'EnableStrictTotalMatchesHelp': 'Returns the exact total number of items in DLNA Browse responses (TotalMatches). Some clients use this for paging; BubbleUPnP works better with this off.',
            'MaxRecentlyAddedItems': 'Max Recently Added Items:',
            'MaxRecentlyAddedItemsHelp': 'Maximum items shown in recently-added virtual folders.',
            'MaxSeriesListItems': 'Max Series List Items:',
            'MaxSeriesListItemsHelp': 'Maximum series shown in the series list virtual folder. Use Unlimited for no cap.',
            'Unlimited': 'Unlimited',
            'SectionExtras': 'Extras Settings',
            'EnableExtras': 'Enable Extras (OP/ED & Bonus Features)',
            'EnableExtrasHelp': 'Shows bonus videos such as OP/ED and behind-the-scenes in a dedicated folder.',
            'SectionStorage': 'Storage / Cache Management',
            'SectionStorageUsage': 'Usage',
            'SectionStorageMaintenance': 'Maintenance',
            'SectionStorageHelp': 'View DLNA index and cache usage. Maintenance actions run immediately and do not require saving settings.',
            'RefreshStorageStats': 'Refresh statistics',
            'RefreshStorageStatsHelp': 'Reloads current index and cache statistics from the server.',
            'ClearBrowseCache': 'Clear Browse cache',
            'ClearBrowseCacheHelp': 'Clears in-memory Browse XML cache only. The index database is kept.',
            'ClearIndex': 'Clear index database',
            'ClearIndexHelp': 'Deletes the SQLite virtual index database file. Caches are kept.',
            'ClearAllStorage': 'Clear all caches and index',
            'ClearAllStorageHelp': 'Clears all in-memory caches and deletes the index database.',
            'RebuildIndex': 'Rebuild index',
            'RebuildIndexHelp': 'Rebuilds the virtual index in the background. Uses the "Pre-generate folder listings" setting if enabled.',
            'ClearAndRebuild': 'Clear all and rebuild index',
            'ClearAndRebuildHelp': 'Clears everything then rebuilds the index in the background.',
            'StorageMaintenanceRunning': 'Maintenance in progress...',
            'StorageSummaryTitle': 'Summary',
            'StorageTotalEstimated': 'Estimated total usage',
            'StorageIndexDatabase': 'Index database',
            'StorageBrowseCache': 'Browse response cache (L4)',
            'StorageBrowseNodeCache': 'Browse node cache (L3)',
            'StorageChildCountCache': 'Child count cache',
            'StorageBrowseMetrics': 'Browse metrics',
            'StorageGenerations': 'Generations',
            'StoragePath': 'Path',
            'StorageFileSize': 'File size',
            'StorageEntryCount': 'Entries',
            'StorageEstimatedMemory': 'Estimated memory',
            'StorageBrowseCount': 'Browse count',
            'StorageCacheHitRate': 'Cache hit rate (L3+L4)',
            'StorageResponseCacheHitRate': 'Response cache hit rate (L4)',
            'StorageNodeCacheHitRate': 'Node cache hit rate (L3)',
            'StorageIndexHitRate': 'Index hit rate',
            'StorageInvalidationCount': 'Invalidation count',
            'StorageIndexGeneration': 'Index generation',
            'StorageLibraryGeneration': 'Library generation',
            'StorageIndexedLibraries': 'Indexed libraries',
            'StorageTableLibraryIndexed': 'Library registrations',
            'StorageTableItemSummary': 'Item summaries',
            'StorageTableVirtualList': 'Virtual list entries',
            'StorageTableKanaRow': 'Kana row entries',
            'StorageTableFacetIndex': 'Facet index entries',
            'ConfirmClearBrowseCache': 'Clear the Browse response and node caches?',
            'ConfirmClearIndex': 'Clear the virtual index database?',
            'ConfirmClearAllStorage': 'Clear all caches and the index database?',
            'ConfirmRebuildIndex': 'Rebuild the virtual index in the background?',
            'ConfirmClearAndRebuild': 'Clear all storage and rebuild the index in the background?',
            'StorageActionCompleted': 'Maintenance action completed.',
            'StorageActionStarted': 'Maintenance action started in the background.',
            'StorageActionFailed': 'Maintenance action failed.',
            'StorageStatsLoading': 'Loading storage statistics...',
            'StorageStatsLoadFailed': 'Failed to load storage statistics. Check administrator permissions and reload the page.',
            'SectionDebug': 'Advanced / Debug',
            'EnableDebugLogging': 'Enable debug logging',
            'EnableDebugLoggingHelp': 'Emits verbose DLNA logs including SOAP/XML traffic and per-browse performance summaries. Keep disabled in production. Click Save to apply.',
            'Save': 'Save',
            'Cancel': 'Cancel',
            'Help': 'Help'
        },
        'ja': {
            'TitleDlnaSettings': 'DLNA設定',
            'TabGeneral': '一般',
            'TabLibrary': 'ライブラリ',
            'TabBrowse': 'Browse',
            'TabIndex': 'インデックス',
            'TabStorage': 'ストレージ',
            'SectionGeneral': '一般設定',
            'GeneralIndexStartupNote': '起動時のインデックス再構築は Index タブで設定できます。',
            'EnablePlayTo': 'Play To（デバイスからのリモート再生機能）を有効にする',
            'EnablePlayToHelp': 'DLNAデバイスからこのサーバー上のメディア再生をリモート操作できるようにします。',
            'DefaultUser': 'デフォルトユーザー:',
            'DefaultUserHelp': 'DLNAクライアントが認証しない場合にライブラリを公開するユーザーです。',
            'None': 'なし',
            'SectionNetwork': 'SSDP / ネットワーク設定',
            'ClientDiscoveryInterval': 'クライアント検出間隔:',
            'ClientDiscoveryIntervalHelp': 'SSDPクライアント検出の実行間隔（秒単位）。サーバーがネットワーク上のデバイスを検索する間隔を設定します。',
            'BlastAliveMessages': '生存（Alive）メッセージを一括送信する',
            'BlastAliveMessagesHelp': 'サーバー起動時にSSDP生存通知をまとめて複数回送信します。',
            'AliveMessageInterval': '生存メッセージ送信間隔:',
            'AliveMessageIntervalHelp': 'SSDP生存（Alive）通知が送信される頻度（秒単位）。',
            'SendOnlyMatchedHost': '一致するホストにのみ送信する',
            'SendOnlyMatchedHostHelp': 'サーバーと同じサブネット上のクライアントにのみSSDP応答を送ります。',
            'SectionSeries': 'シリーズ（TV番組）設定',
            'EnableRecentlyAddedEpisodes': '「最近追加されたエピソード」フォルダを有効にする',
            'EnableRecentlyAddedSeries': '「最近追加されたシリーズ」フォルダを有効にする',
            'EnableRecentlyReleasedEpisodes': '「最近リリースされたエピソード」フォルダを有効にする',
            'EnableRecentlyReleasedSeries': '「最近リリースされたシリーズ」フォルダを有効にする',
            'EnableCurrentlyAiring': '「放送中」フォルダを有効にする',
            'EnableBrowseByKana': '「頭文字別」フォルダを有効にする',
            'ActiveTitleBrowsePresetId': '頭文字分類プリセット:',
            'ActiveTitleBrowsePresetIdHelp': '頭文字別フォルダの既定グループ（A-Z、日本語五十音、カスタム）を選択します。',
            'HideEmptyVirtualFolders': '空の仮想フォルダを非表示にする',
            'TitleBrowsePresetsJson': '頭文字分類プリセット (JSON):',
            'TitleBrowsePresetsJsonHelp': '上級者向け: プリセット定義を JSON で編集します。組み込みのアルファベット・日本語五十音プリセットは保存時に維持されます。',
            'CollapsibleAdvancedHint': '（クリックで開閉）',
            'SectionLibraryTitleBrowse': 'ライブラリ別頭文字分類',
            'LibraryTitleBrowseLibraryId': 'ライブラリ:',
            'LibraryTitleBrowsePresetOverride': 'プリセット上書き:',
            'LibraryTitleStripRegexes': 'タイトル除去正規表現:',
            'LibraryTitleStripRegexesHelp': '1行に1つの正規表現。選択したライブラリで分類前に一致した接頭辞を除去します。',
            'LibraryTitleBrowseUseGlobal': '（グローバルプリセットを使用）',
            'EnableBrowseByYear': '「年別」フォルダを有効にする',
            'SectionMovies': '映画設定',
            'EnableRecentlyAddedMovies': '「最近追加された映画」フォルダを有効にする',
            'EnableRecentlyReleasedMovies': '「最近リリースされた映画」フォルダを有効にする',
            'EnableThreeDMovies': '「3D映画」フォルダを有効にする',
            'EnableAuto3DTagging': '3D映像へのタグ自動付与（VRデバイス自動認識用）を有効にする',
            'EnableFourKMovies': '「4K映画」フォルダを有効にする',
            'EnableEightKMovies': '「8K映画」フォルダを有効にする',
            'EnableVrMovies': '「VR動画」フォルダを有効にする',
            'EnableEightKVrMovies': '「8K VR動画」フォルダを有効にする',
            'EnableAutoResolutionTagging': '解像度（4K/8K）タグの自動付与を有効にする',
            'EnableAutoVrTagging': 'VR映像タグ（VR180/VR360等）の自動付与を有効にする',
            'SectionBrowse': 'Browse / 互換性設定',
            'SectionBrowseQuest': 'Quest互換',
            'SectionBrowseCache': 'レスポンスキャッシュ',
            'SectionBrowseImages': '画像表示',
            'SectionBrowsePaging': 'ページングと上限',
            'EnableQuestCompatibilityMode': 'Quest互換モード',
            'EnableQuestCompatibilityModeHelp': 'Quest向けDLNAプレイヤー向け最適化: 全件返却、childCount省略、ストリームURLのクエリ除去、レスポンスキャッシュを有効化します。再生パラメータはサーバー側で自動解決されます。',
            'EnsurePlaybackUrlsInBrowse': 'Browse一覧に再生URLを含める',
            'EnsurePlaybackUrlsInBrowseHelp': 'インデックスBrowse応答に動画/音声のストリームURLを追加します。Meta Questでの再生には有効のままにしてください。',
            'ChildCountCalculation': 'childCount計算:',
            'ChildCountCalculationHelp': 'フォルダのchildCount属性の算出方法。Disabled/EstimateはN+1クエリを回避します（Quest推奨）。',
            'ChildCountDisabled': '省略',
            'ChildCountEstimate': '推定',
            'ChildCountAccurate': '正確（低速）',
            'EnableChildCountCache': 'childCountをキャッシュする',
            'EnableChildCountCacheHelp': 'Browse操作で取得したフォルダ件数を保持します。Accurateモードでは一覧表示時にも件数を問い合わせます。',
            'EnableChildCountCacheHelpDisabled': 'childCountを省略する設定では利用できません。',
            'EnableBrowseResponseCache': 'Browseレスポンスをキャッシュする',
            'EnableBrowseResponseCacheHelp': '生成済みのBrowse XMLをメモリに保持し、同じフォルダへの再アクセスを高速化します。',
            'BrowseResponseCacheTtlSeconds': 'Browseレスポンスキャッシュの保持時間（秒）:',
            'BrowseResponseCacheTtlSecondsHelp': 'Browse XMLをキャッシュする時間です。「ライブラリ更新まで」はライブラリスキャン完了まで有効です。',
            'EnableItemSummaryBrowse': 'インデックスの項目サマリーを利用する（層2）',
            'EnableItemSummaryBrowseHelp': 'インデックスDBに保存した項目サマリーを使ってフォルダ一覧を返します。毎回Jellyfinへ問い合わせません。',
            'VirtualListImagePresentation': '仮想リストの画像スタイル:',
            'VirtualListImagePresentationHelp': '映画・番組などの仮想フォルダ一覧で使う画像です。既定はポスター（Primary）です。',
            'SearchImagePresentation': '検索結果の画像スタイル:',
            'SearchImagePresentationHelp': 'DLNA検索結果の映画・番組に使う画像です。エピソードは引き続きサムネイルです。',
            'EpisodeListImageSource': 'エピソード一覧の画像ソース:',
            'EpisodeListImageSourceHelp': '最近追加・最近リリース・お気に入りのエピソードなどの一覧で使う画像です。エピソードのサムネイルを優先し、ない場合はシリーズのサムネイルを使います。',
            'EpisodeListImageSourceEpisode': 'エピソードのサムネイル',
            'EpisodeListImageSourceSeries': 'シリーズのサムネイル',
            'ImagePresentationPoster': 'ポスター',
            'ImagePresentationThumbnail': 'サムネイル',
            'EnableBrowseNodeCache': '子フォルダ一覧をキャッシュする（層3）',
            'EnableBrowseNodeCacheHelp': '各フォルダ直下の子項目リストをメモリに保持します。',
            'BrowseNodeCacheTtlSeconds': '子フォルダ一覧キャッシュの保持時間（秒）:',
            'BrowseNodeCacheTtlSecondsHelp': 'フォルダ直下の子項目リストをメモリに保持する時間です。「ライブラリ更新まで」はスキャン完了まで有効です。',
            'EnableIndexMusicGenre': '音楽ジャンルをインデックス化する',
            'EnableIndexPerson': '出演者をインデックス化する',
            'EnableBrowseByPerson': '「出演者別」フォルダを有効にする',
            'EnableIndexRecentlyModifiedSeries': '最近メタデータ更新シリーズをインデックス化する',
            'EnableIndexRecentlyModifiedEpisodes': '最近メタデータ更新エピソードをインデックス化する',
            'EnableIndexRecentlyModifiedMovies': '最近メタデータ更新映画をインデックス化する',
            'EnableRecentlyModifiedSeries': '「最近メタデータ更新（シリーズ）」フォルダを有効にする',
            'EnableRecentlyModifiedEpisodes': '「最近メタデータ更新（エピソード）」フォルダを有効にする',
            'EnableRecentlyModifiedMovies': '「最近メタデータ更新（映画）」フォルダを有効にする',
            'PrewarmHierarchyFolders': 'シリーズ/シーズンフォルダをプリウォームする（件数上限あり）',
            'PrewarmHierarchyFoldersHelp': '物理的なシリーズ・シーズンフォルダのBrowse応答を事前生成します。下の件数上限で制御します。',
            'PrewarmHierarchyMaxSeries': 'プリウォームするシリーズ数上限:',
            'PrewarmHierarchyMaxSeriesHelp': 'プリウォームするシリーズフォルダの最大数です。ライブラリが大きい場合は小さくしてください。',
            'PrewarmHierarchyMaxSeasonsPerSeries': 'シリーズあたりのプリウォームシーズン数上限:',
            'PrewarmHierarchyMaxSeasonsPerSeriesHelp': '各シリーズ配下でプリウォームするシーズン数の上限です。',
            'BrowseCacheTtlUntilUpdate': 'ライブラリ更新まで',
            'MaxBrowseItemsPerResponse': '1回のBrowseで返す最大件数:',
            'MaxBrowseItemsPerResponseHelp': 'DLNAのBrowse応答で1回に返すアイテム数の上限です。クライアントが一度に受け取れる最大件数を制御します。',
            'RespectRequestedCount': 'クライアントのページサイズ（RequestedCount）を尊重する',
            'RespectRequestedCountHelp': 'OFFの場合は全件を1回の応答で返します（BubbleUPnP 利用時はOFF推奨）。',
            'EnableStrictTotalMatches': 'フォルダ内の総件数を正確に返す',
            'EnableStrictTotalMatchesHelp': 'DLNA Browse応答の総件数（TotalMatches）を正確に返します。一部クライアントのページ送りに影響します。BubbleUPnPでは通常OFFを推奨します。',
            'MaxRecentlyAddedItems': '仮想フォルダの最大表示件数（最近追加）:',
            'MaxRecentlyAddedItemsHelp': '「最近追加」系の仮想フォルダに表示する最大件数です。',
            'MaxSeriesListItems': '仮想フォルダの最大表示件数（シリーズ一覧）:',
            'MaxSeriesListItemsHelp': 'シリーズ一覧仮想フォルダに表示する最大件数です。「無制限」で上限なしです。',
            'Unlimited': '無制限',
            'SectionIndex': '仮想フォルダインデックス設定',
            'SectionIndexBasic': 'インデックス基本',
            'SectionIndexPrewarm': 'プリウォーム',
            'SectionIndexTargets': 'インデックス対象',
            'SectionIndexTargetsHelp': 'DLNAブラウズを高速化するため、どの一覧をインデックスDBに保存するかを選びます。',
            'SectionIndexFolders': '表示フォルダ',
            'SectionIndexLibraryChanges': 'ライブラリ更新時',
            'SectionIndexLargeFolders': '大量フォルダの分割',
            'EnableVirtualFolderIndex': '仮想フォルダインデックスを有効にする',
            'EnableVirtualFolderIndexHelp': '最近追加・五十音・ジャンルなどの仮想フォルダを高速表示するためのSQLiteデータベースを構築します。',
            'WarmupIndexOnStartup': '起動時にインデックスを再構築する',
            'WarmupIndexOnStartupHelp': 'Jellyfin起動時にSQLiteインデックスDBを再構築します。OFFにすると、初回Browseまたは手動再構築まで起動時の再構築をスキップします。',
            'PrewarmBrowseResponses': 'インデックス作成後にフォルダ一覧を事前生成する',
            'PrewarmBrowseResponsesHelp': 'インデックス構築後、主要フォルダのBrowse XMLをサーバー側で事前生成します。初回アクセスの待ち時間を短縮できます。',
            'PrewarmFacetItemFolders': 'スタジオ/タグ/レーティングの子フォルダもプリウォームする',
            'PrewarmFacetItemFoldersHelp': 'スタジオ・タグ・レーティング配下の子フォルダ一覧も事前生成します。ライブラリが大きい場合はOFFを推奨します。',
            'EnableIndexGenre': 'ジャンルをインデックス化する',
            'EnableIndexGenreHelp': 'ジャンルフォルダをインデックスに保存し、毎回ライブラリ全体を走査しません。',
            'EnableIndexYear': '制作年をインデックス化する',
            'EnableIndexYearHelp': '年別ブラウズフォルダをインデックスに保存します。',
            'EnableIndexRecentlyReleasedEpisodes': '最近リリースのエピソードをインデックス化する',
            'EnableIndexRecentlyReleasedSeries': '最近リリースのシリーズをインデックス化する',
            'EnableIndexRecentlyReleasedMovies': '最近リリースの映画をインデックス化する',
            'EnableIndexSeriesList': 'シリーズ一覧をインデックス化する',
            'EnableIndexSeriesListHelp': 'TVライブラリのシリーズ一覧をインデックスに保存します。',
            'EnableIndexMoviesList': '映画一覧をインデックス化する',
            'EnableIndexMoviesListHelp': '映画ライブラリの映画一覧をインデックスに保存します。',
            'EnableIndexSeasonList': 'シーズン一覧をインデックス化する',
            'EnableIndexEpisodeList': 'エピソード一覧をインデックス化する',
            'RebuildIndexAfterLibraryScan': 'ライブラリ変更後にインデックスを再構築する',
            'RebuildIndexAfterLibraryScanHelp': 'メディアライブラリのスキャンや更新後に仮想インデックスを再構築します。',
            'DebounceLibraryChangeInvalidation': 'キャッシュ無効化をデバウンスする',
            'DebounceLibraryChangeInvalidationHelp': '短時間に複数のライブラリ変更があった場合、キャッシュクリアを待機してから実行します。',
            'LibraryChangeDebounceSeconds': 'ライブラリ変更デバウンス（秒）:',
            'LibraryChangeDebounceSecondsHelp': '最後のライブラリ変更からキャッシュを無効化するまでの待機秒数です。',
            'EnableRecentlyUpdatedSeries': '「最近更新されたシリーズ」フォルダを有効にする',
            'EnableBrowseByStudio': '「スタジオ別」フォルダを有効にする',
            'EnableBrowseByTag': '「タグ別」フォルダを有効にする',
            'EnableBrowseByRating': '「レーティング別」フォルダを有効にする',
            'LargeFolderRangeSplitThreshold': '範囲分割の閾値:',
            'LargeFolderRangeSplitThresholdHelp': 'シリーズ数がこの値を超えると、一覧を範囲サブフォルダ（例: A〜D）に分割します。',
            'RangeFolderSize': '範囲フォルダあたりのシリーズ数:',
            'RangeFolderSizeHelp': '大量フォルダ分割時に、各範囲サブフォルダに入れるシリーズ数です。',
            'SectionExtras': '特典映像設定',
            'EnableExtras': '「特典映像」（OP/ED、ボーナス映像など）フォルダを有効にする',
            'EnableExtrasHelp': 'OP/EDやメイキングなどのボーナス映像を専用フォルダに表示します。',
            'SectionStorage': 'ストレージ / キャッシュ管理',
            'SectionStorageUsage': '使用状況',
            'SectionStorageMaintenance': 'メンテナンス',
            'SectionStorageHelp': 'DLNAインデックスとキャッシュの使用状況を確認できます。メンテナンス操作は即時実行され、設定の保存は不要です。',
            'RefreshStorageStats': '統計を更新',
            'RefreshStorageStatsHelp': 'サーバーから最新のインデックス・キャッシュ統計を再読み込みします。',
            'ClearBrowseCache': 'Browseキャッシュをクリア',
            'ClearBrowseCacheHelp': 'メモリ上のBrowse XMLキャッシュのみをクリアします。インデックスDBは保持されます。',
            'ClearIndex': 'インデックスDBをクリア',
            'ClearIndexHelp': 'SQLite仮想インデックスDBファイルを削除します。キャッシュは保持されます。',
            'ClearAllStorage': 'すべてのキャッシュとインデックスをクリア',
            'ClearAllStorageHelp': 'すべてのメモリキャッシュとインデックスDBをクリアします。',
            'RebuildIndex': 'インデックスを再生成',
            'RebuildIndexHelp': 'バックグラウンドで仮想インデックスを再構築します。「フォルダ一覧を事前生成」がONならプリウォームも実行します。',
            'ClearAndRebuild': 'すべてクリアしてインデックスを再生成',
            'ClearAndRebuildHelp': 'すべてをクリアした後、バックグラウンドでインデックスを再構築します。',
            'StorageMaintenanceRunning': 'メンテナンス実行中...',
            'StorageSummaryTitle': '概要',
            'StorageTotalEstimated': '推定合計使用量',
            'StorageIndexDatabase': 'インデックスDB',
            'StorageBrowseCache': 'Browseレスポンスキャッシュ（L4）',
            'StorageBrowseNodeCache': 'Browseノードキャッシュ（L3）',
            'StorageChildCountCache': 'childCountキャッシュ',
            'StorageBrowseMetrics': 'Browseメトリクス',
            'StorageGenerations': '世代',
            'StoragePath': 'パス',
            'StorageFileSize': 'ファイルサイズ',
            'StorageEntryCount': 'エントリ数',
            'StorageEstimatedMemory': '推定メモリ',
            'StorageBrowseCount': 'Browse回数',
            'StorageCacheHitRate': 'キャッシュヒット率（L3+L4）',
            'StorageResponseCacheHitRate': 'レスポンスキャッシュヒット率（L4）',
            'StorageNodeCacheHitRate': 'ノードキャッシュヒット率（L3）',
            'StorageIndexHitRate': 'インデックスヒット率',
            'StorageInvalidationCount': '無効化回数',
            'StorageIndexGeneration': 'インデックス世代',
            'StorageLibraryGeneration': 'ライブラリ世代',
            'StorageIndexedLibraries': 'インデックス済みライブラリ',
            'StorageTableLibraryIndexed': 'ライブラリ登録件数',
            'StorageTableItemSummary': '項目サマリー件数',
            'StorageTableVirtualList': '仮想リスト件数',
            'StorageTableKanaRow': '五十音行件数',
            'StorageTableFacetIndex': 'ファセットインデックス件数',
            'ConfirmClearBrowseCache': 'Browseレスポンスキャッシュとノードキャッシュをクリアしますか？',
            'ConfirmClearIndex': '仮想インデックスDBをクリアしますか？',
            'ConfirmClearAllStorage': 'すべてのキャッシュとインデックスDBをクリアしますか？',
            'ConfirmRebuildIndex': 'バックグラウンドで仮想インデックスを再生成しますか？',
            'ConfirmClearAndRebuild': 'すべてをクリアしてバックグラウンドでインデックスを再生成しますか？',
            'StorageActionCompleted': 'メンテナンス操作が完了しました。',
            'StorageActionStarted': 'バックグラウンドでメンテナンスを開始しました。',
            'StorageActionFailed': 'メンテナンス操作に失敗しました。',
            'StorageStatsLoading': 'ストレージ統計を読み込み中...',
            'StorageStatsLoadFailed': 'ストレージ統計の取得に失敗しました。管理者権限を確認し、ページを再読み込みしてください。',
            'SectionDebug': '詳細 / デバッグ',
            'EnableDebugLogging': 'デバッグログを有効にする',
            'EnableDebugLoggingHelp': 'SOAP/XMLトラフィックやBrowseごとのパフォーマンスサマリなど、詳細なDLNAログを出力します。本番環境ではOFFを推奨します。変更後は保存してください。',
            'Save': '保存',
            'Cancel': 'キャンセル',
            'Help': 'ヘルプ'
        }
    },
    getLanguage: function() {
        const storedLang = localStorage.getItem('displayLanguage') || localStorage.getItem('language') || localStorage.getItem('i18nextLng');
        if (storedLang) return storedLang.toLowerCase();
        const htmlLang = document.documentElement.lang;
        if (htmlLang) return htmlLang.toLowerCase();
        if (navigator.language) return navigator.language.toLowerCase();
        return 'en';
    },
    getDictionary: function(page) {
        const lang = DlnaConfigurationPage.getLanguage().startsWith('ja') ? 'ja' : 'en';
        return DlnaConfigurationPage.translations[lang] || DlnaConfigurationPage.translations['en'];
    },
    bindTabs: function(page) {
        const tabs = page.querySelector('#dlnaConfigTabs');
        if (!tabs || tabs.getAttribute('data-dlna-tabs-bound') === 'true') {
            return;
        }

        tabs.setAttribute('data-dlna-tabs-bound', 'true');

        if (window.CustomElements && window.CustomElements.upgradeSubtree) {
            window.CustomElements.upgradeSubtree(tabs);
        }

        const containers = page.querySelectorAll('.tabContent');
        const buttons = tabs.querySelectorAll('.emby-tab-button');

        const activateTab = function(index) {
            containers.forEach(function(panel, panelIndex) {
                if (panelIndex === index) {
                    panel.classList.add('is-active');
                } else {
                    panel.classList.remove('is-active');
                }
            });

            buttons.forEach(function(button) {
                const buttonIndex = parseInt(button.getAttribute('data-index'), 10);
                if (buttonIndex === index) {
                    button.classList.add('emby-tab-button-active');
                } else {
                    button.classList.remove('emby-tab-button-active');
                }
            });

            if (index === DlnaConfigurationPage.storageTabIndex) {
                DlnaConfigurationPage.loadStorageStats(page);
            }
        };

        buttons.forEach(function(button) {
            button.addEventListener('click', function(e) {
                e.preventDefault();
                const index = parseInt(button.getAttribute('data-index'), 10);
                if (!isNaN(index)) {
                    activateTab(index);
                }
            });
        });

        tabs.addEventListener('beforetabchange', function(e) {
            const previousIndex = e.detail.previousIndex;
            const selectedIndex = e.detail.selectedTabIndex;

            if (previousIndex != null && containers[previousIndex]) {
                containers[previousIndex].classList.remove('is-active');
            }

            if (containers[selectedIndex]) {
                containers[selectedIndex].classList.add('is-active');
            }
        });

        tabs.addEventListener('tabchange', function(e) {
            if (e.detail && e.detail.selectedTabIndex === DlnaConfigurationPage.storageTabIndex) {
                DlnaConfigurationPage.loadStorageStats(page);
            }
        });
    },
    fitAdvancedTextarea: function(textarea, measureWidth) {
        if (!textarea) {
            return;
        }

        const body = textarea.closest('.dlna-advanced-body');
        const isHidden = body && body.classList.contains('hide');
        const minRows = parseInt(textarea.getAttribute('data-min-rows'), 10) || 4;
        const maxRows = parseInt(textarea.getAttribute('data-max-rows'), 10) || 20;
        const minCols = parseInt(textarea.getAttribute('data-min-cols'), 10) || 48;
        const maxCols = parseInt(textarea.getAttribute('data-max-cols'), 10) || 120;
        const value = textarea.value || '';
        const lines = value.length === 0 ? [''] : value.split(/\r\n|\r|\n/);
        const lineCount = lines.length;
        const longestLine = lines.reduce(function(max, line) {
            return Math.max(max, line.length);
        }, 0);

        textarea.rows = Math.min(maxRows, Math.max(minRows, lineCount || minRows));
        textarea.cols = Math.min(maxCols, Math.max(minCols, longestLine + 2));

        if (!measureWidth || isHidden) {
            return;
        }

        textarea.style.width = '';
        textarea.style.height = '';

        const container = textarea.closest('.inputContainer');
        if (!container || container.clientWidth <= 0) {
            return;
        }

        const containerWidth = container.clientWidth;
        if (textarea.offsetWidth > containerWidth) {
            textarea.style.width = containerWidth + 'px';
        }
    },
    scheduleAdvancedTextareaFit: function(textarea) {
        if (!textarea) {
            return;
        }

        requestAnimationFrame(function() {
            requestAnimationFrame(function() {
                DlnaConfigurationPage.fitAdvancedTextarea(textarea, true);
            });
        });
    },
    bindCollapsible: function(page) {
        page.querySelectorAll('.dlna-advanced-section').forEach(function(section) {
            const toggle = section.querySelector('.dlna-advanced-toggle');
            const body = section.querySelector('.dlna-advanced-body');
            if (!toggle || !body) {
                return;
            }

            if (section.getAttribute('data-dlna-collapse-bound') === 'true') {
                return;
            }

            section.setAttribute('data-dlna-collapse-bound', 'true');

            const setExpanded = function(expanded) {
                body.classList.toggle('hide', !expanded);
                section.classList.toggle('is-expanded', expanded);
                toggle.setAttribute('aria-expanded', expanded ? 'true' : 'false');
                if (expanded) {
                    body.querySelectorAll('textarea.emby-textarea').forEach(function(textarea) {
                        DlnaConfigurationPage.scheduleAdvancedTextareaFit(textarea);
                    });
                }
            };

            setExpanded(false);

            toggle.addEventListener('click', function() {
                setExpanded(body.classList.contains('hide'));
            });

            toggle.addEventListener('keydown', function(e) {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    setExpanded(body.classList.contains('hide'));
                }
            });

            body.querySelectorAll('textarea.emby-textarea').forEach(function(textarea) {
                DlnaConfigurationPage.fitAdvancedTextarea(textarea, false);
            });
        });
    },
    translatePage: function(page) {
        const dict = DlnaConfigurationPage.getDictionary(page);

        page.querySelectorAll('[data-i18n]').forEach(function(element) {
            const key = element.getAttribute('data-i18n');
            if (dict[key]) {
                element.textContent = dict[key];
            }
        });

        page.querySelectorAll('[data-plugin-i18n]').forEach(function(element) {
            const key = element.getAttribute('data-plugin-i18n');
            if (dict[key]) {
                element.textContent = dict[key];
            }
        });

        const inputsToTranslate = [
            { id: 'dlnaDiscoveryInterval', key: 'ClientDiscoveryInterval' },
            { id: 'dlnaAliveInterval', key: 'AliveMessageInterval' },
            { id: 'dlnaSelectUser', key: 'DefaultUser' },
            { id: 'maxBrowseItemsPerResponse', key: 'MaxBrowseItemsPerResponse' },
            { id: 'childCountCalculation', key: 'ChildCountCalculation' },
            { id: 'browseResponseCacheTtlSeconds', key: 'BrowseResponseCacheTtlSeconds' },
            { id: 'browseNodeCacheTtlSeconds', key: 'BrowseNodeCacheTtlSeconds' },
            { id: 'virtualListImagePresentation', key: 'VirtualListImagePresentation' },
            { id: 'searchImagePresentation', key: 'SearchImagePresentation' },
            { id: 'episodeListImageSource', key: 'EpisodeListImageSource' },
            { id: 'maxRecentlyAddedItems', key: 'MaxRecentlyAddedItems' },
            { id: 'maxSeriesListItems', key: 'MaxSeriesListItems' },
            { id: 'libraryChangeDebounceSeconds', key: 'LibraryChangeDebounceSeconds' },
            { id: 'largeFolderRangeSplitThreshold', key: 'LargeFolderRangeSplitThreshold' },
            { id: 'rangeFolderSize', key: 'RangeFolderSize' },
            { id: 'prewarmHierarchyMaxSeries', key: 'PrewarmHierarchyMaxSeries' },
            { id: 'prewarmHierarchyMaxSeasonsPerSeries', key: 'PrewarmHierarchyMaxSeasonsPerSeries' },
            { id: 'activeTitleBrowsePresetId', key: 'ActiveTitleBrowsePresetId' },
            { id: 'libraryTitleBrowseLibraryId', key: 'LibraryTitleBrowseLibraryId' },
            { id: 'libraryTitleBrowsePresetOverride', key: 'LibraryTitleBrowsePresetOverride' }
        ];

        inputsToTranslate.forEach(function(item) {
            const input = page.querySelector('#' + item.id);
            if (input && dict[item.key]) {
                input.setAttribute('label', dict[item.key]);
                const labelEl = page.querySelector('label[for="' + item.id + '"]');
                if (labelEl) {
                    labelEl.textContent = dict[item.key];
                }
            }
        });

        const unlimitedOption = page.querySelector('#maxSeriesListItems option[value="0"]');
        if (unlimitedOption && dict['Unlimited']) {
            unlimitedOption.textContent = dict['Unlimited'];
        }

        const childCountOptions = {
            'Disabled': dict['ChildCountDisabled'],
            'Estimate': dict['ChildCountEstimate'],
            'Accurate': dict['ChildCountAccurate']
        };
        Object.keys(childCountOptions).forEach(function(value) {
            const option = page.querySelector('#childCountCalculation option[value="' + value + '"]');
            if (option && childCountOptions[value]) {
                option.textContent = childCountOptions[value];
            }
        });

        const imagePresentationOptions = {
            'Poster': dict['ImagePresentationPoster'],
            'Thumbnail': dict['ImagePresentationThumbnail']
        };
        Object.keys(imagePresentationOptions).forEach(function(value) {
            ['virtualListImagePresentation', 'searchImagePresentation'].forEach(function(selectId) {
                const option = page.querySelector('#' + selectId + ' option[value="' + value + '"]');
                if (option && imagePresentationOptions[value]) {
                    option.textContent = imagePresentationOptions[value];
                }
            });
        });

        const episodeListImageOptions = {
            'Episode': dict['EpisodeListImageSourceEpisode'],
            'Series': dict['EpisodeListImageSourceSeries']
        };
        Object.keys(episodeListImageOptions).forEach(function(value) {
            const option = page.querySelector('#episodeListImageSource option[value="' + value + '"]');
            if (option && episodeListImageOptions[value]) {
                option.textContent = episodeListImageOptions[value];
            }
        });

        ['browseResponseCacheTtlSeconds', 'browseNodeCacheTtlSeconds'].forEach(function(selectId) {
            const ttlZeroOption = page.querySelector('#' + selectId + ' option[value="0"]');
            if (ttlZeroOption && dict['BrowseCacheTtlUntilUpdate']) {
                ttlZeroOption.textContent = dict['BrowseCacheTtlUntilUpdate'];
            }
        });
    },
    formatBytes: function(bytes) {
        if (!bytes) {
            return '0 B';
        }

        const units = ['B', 'KB', 'MB', 'GB', 'TB'];
        let value = bytes;
        let unitIndex = 0;
        while (value >= 1024 && unitIndex < units.length - 1) {
            value /= 1024;
            unitIndex++;
        }

        return value.toFixed(unitIndex === 0 ? 0 : 1) + ' ' + units[unitIndex];
    },
    formatPercent: function(rate) {
        return (rate * 100).toFixed(1) + '%';
    },
    pickValue: function(object, pascalName, camelName) {
        if (!object) {
            return undefined;
        }

        if (object[pascalName] !== undefined && object[pascalName] !== null) {
            return object[pascalName];
        }

        return object[camelName];
    },
    normalizeStorageStats: function(raw) {
        if (!raw) {
            return null;
        }

        const indexDb = DlnaConfigurationPage.pickValue(raw, 'IndexDatabase', 'indexDatabase') || {};
        const browseCache = DlnaConfigurationPage.pickValue(raw, 'BrowseCache', 'browseCache') || {};
        const browseNodeCache = DlnaConfigurationPage.pickValue(raw, 'BrowseNodeCache', 'browseNodeCache') || {};
        const childCountCache = DlnaConfigurationPage.pickValue(raw, 'ChildCountCache', 'childCountCache') || {};
        const browseMetrics = DlnaConfigurationPage.pickValue(raw, 'BrowseMetrics', 'browseMetrics') || {};
        const indexedLibraryIds = DlnaConfigurationPage.pickValue(indexDb, 'IndexedLibraryIds', 'indexedLibraryIds') || [];

        return {
            IndexDatabase: {
                DatabasePath: DlnaConfigurationPage.pickValue(indexDb, 'DatabasePath', 'databasePath') || '',
                FileSizeBytes: DlnaConfigurationPage.pickValue(indexDb, 'FileSizeBytes', 'fileSizeBytes') || 0,
                LibraryIndexedCount: DlnaConfigurationPage.pickValue(indexDb, 'LibraryIndexedCount', 'libraryIndexedCount') || 0,
                ItemSummaryCount: DlnaConfigurationPage.pickValue(indexDb, 'ItemSummaryCount', 'itemSummaryCount') || 0,
                VirtualListCount: DlnaConfigurationPage.pickValue(indexDb, 'VirtualListCount', 'virtualListCount') || 0,
                KanaRowCount: DlnaConfigurationPage.pickValue(indexDb, 'KanaRowCount', 'kanaRowCount') || 0,
                FacetIndexCount: DlnaConfigurationPage.pickValue(indexDb, 'FacetIndexCount', 'facetIndexCount') || 0,
                IndexedLibraryIds: indexedLibraryIds
            },
            BrowseCache: {
                EntryCount: DlnaConfigurationPage.pickValue(browseCache, 'EntryCount', 'entryCount') || 0,
                EstimatedBytes: DlnaConfigurationPage.pickValue(browseCache, 'EstimatedBytes', 'estimatedBytes') || 0
            },
            BrowseNodeCache: {
                EntryCount: DlnaConfigurationPage.pickValue(browseNodeCache, 'EntryCount', 'entryCount') || 0,
                EstimatedBytes: DlnaConfigurationPage.pickValue(browseNodeCache, 'EstimatedBytes', 'estimatedBytes') || 0
            },
            ChildCountCache: {
                EntryCount: DlnaConfigurationPage.pickValue(childCountCache, 'EntryCount', 'entryCount') || 0
            },
            BrowseMetrics: {
                BrowseCount: DlnaConfigurationPage.pickValue(browseMetrics, 'BrowseCount', 'browseCount') || 0,
                CacheHitRate: DlnaConfigurationPage.pickValue(browseMetrics, 'CacheHitRate', 'cacheHitRate') || 0,
                ResponseCacheHitRate: DlnaConfigurationPage.pickValue(browseMetrics, 'ResponseCacheHitRate', 'responseCacheHitRate') || 0,
                NodeCacheHitRate: DlnaConfigurationPage.pickValue(browseMetrics, 'NodeCacheHitRate', 'nodeCacheHitRate') || 0,
                IndexHitRate: DlnaConfigurationPage.pickValue(browseMetrics, 'IndexHitRate', 'indexHitRate') || 0,
                InvalidationCount: DlnaConfigurationPage.pickValue(browseMetrics, 'InvalidationCount', 'invalidationCount') || 0
            },
            IndexGeneration: DlnaConfigurationPage.pickValue(raw, 'IndexGeneration', 'indexGeneration') || 0,
            LibraryGeneration: DlnaConfigurationPage.pickValue(raw, 'LibraryGeneration', 'libraryGeneration') || 0,
            IsMaintenanceRunning: !!DlnaConfigurationPage.pickValue(raw, 'IsMaintenanceRunning', 'isMaintenanceRunning')
        };
    },
    renderStatRow: function(label, value) {
        return '<div class="fieldDescription flex justify-content-space-between">' +
            '<span>' + label + '</span>' +
            '<span><strong>' + value + '</strong></span>' +
            '</div>';
    },
    renderStorageStats: function(page, stats) {
        const dict = DlnaConfigurationPage.getDictionary(page);
        const container = page.querySelector('#dlnaStorageStats');
        if (!container || !stats) {
            return;
        }

        const indexDb = stats.IndexDatabase || {};
        const browseCache = stats.BrowseCache || {};
        const browseNodeCache = stats.BrowseNodeCache || {};
        const childCountCache = stats.ChildCountCache || {};
        const browseMetrics = stats.BrowseMetrics || {};
        const indexedLibraries = (indexDb.IndexedLibraryIds || []).length;
        const totalBytes = (indexDb.FileSizeBytes || 0) + (browseCache.EstimatedBytes || 0) + (browseNodeCache.EstimatedBytes || 0);

        let html = '';

        if (stats.IsMaintenanceRunning) {
            html += '<div class="verticalSection verticalSection-extrabottompadding">';
            html += '<div class="fieldDescription"><strong>' + dict['StorageMaintenanceRunning'] + '</strong></div>';
            html += '</div>';
        }

        html += '<div class="verticalSection verticalSection-extrabottompadding">';
        html += '<h3 class="sectionTitle">' + dict['StorageSummaryTitle'] + '</h3>';
        html += DlnaConfigurationPage.renderStatRow(dict['StorageTotalEstimated'], DlnaConfigurationPage.formatBytes(totalBytes));
        html += DlnaConfigurationPage.renderStatRow(dict['StorageIndexedLibraries'], indexedLibraries);
        html += '</div>';

        html += '<div class="verticalSection verticalSection-extrabottompadding">';
        html += '<h3 class="sectionTitle">' + dict['StorageIndexDatabase'] + '</h3>';
        html += '<div class="fieldDescription">' + dict['StoragePath'] + ': ' + (indexDb.DatabasePath || '-') + '</div>';
        html += DlnaConfigurationPage.renderStatRow(dict['StorageFileSize'], DlnaConfigurationPage.formatBytes(indexDb.FileSizeBytes || 0));
        html += DlnaConfigurationPage.renderStatRow(dict['StorageTableLibraryIndexed'], indexDb.LibraryIndexedCount || 0);
        html += DlnaConfigurationPage.renderStatRow(dict['StorageTableItemSummary'], indexDb.ItemSummaryCount || 0);
        html += DlnaConfigurationPage.renderStatRow(dict['StorageTableVirtualList'], indexDb.VirtualListCount || 0);
        html += DlnaConfigurationPage.renderStatRow(dict['StorageTableKanaRow'], indexDb.KanaRowCount || 0);
        html += DlnaConfigurationPage.renderStatRow(dict['StorageTableFacetIndex'], indexDb.FacetIndexCount || 0);
        html += '</div>';

        html += '<div class="verticalSection verticalSection-extrabottompadding">';
        html += '<h3 class="sectionTitle">' + dict['StorageBrowseCache'] + '</h3>';
        html += DlnaConfigurationPage.renderStatRow(dict['StorageEntryCount'], browseCache.EntryCount || 0);
        html += DlnaConfigurationPage.renderStatRow(dict['StorageEstimatedMemory'], DlnaConfigurationPage.formatBytes(browseCache.EstimatedBytes || 0));
        html += '</div>';

        html += '<div class="verticalSection verticalSection-extrabottompadding">';
        html += '<h3 class="sectionTitle">' + dict['StorageBrowseNodeCache'] + '</h3>';
        html += DlnaConfigurationPage.renderStatRow(dict['StorageEntryCount'], browseNodeCache.EntryCount || 0);
        html += DlnaConfigurationPage.renderStatRow(dict['StorageEstimatedMemory'], DlnaConfigurationPage.formatBytes(browseNodeCache.EstimatedBytes || 0));
        html += '</div>';

        html += '<div class="verticalSection verticalSection-extrabottompadding">';
        html += '<h3 class="sectionTitle">' + dict['StorageChildCountCache'] + '</h3>';
        html += DlnaConfigurationPage.renderStatRow(dict['StorageEntryCount'], childCountCache.EntryCount || 0);
        html += '</div>';

        html += '<div class="verticalSection verticalSection-extrabottompadding">';
        html += '<h3 class="sectionTitle">' + dict['StorageBrowseMetrics'] + '</h3>';
        html += DlnaConfigurationPage.renderStatRow(dict['StorageBrowseCount'], browseMetrics.BrowseCount || 0);
        html += DlnaConfigurationPage.renderStatRow(dict['StorageCacheHitRate'], DlnaConfigurationPage.formatPercent(browseMetrics.CacheHitRate || 0));
        html += DlnaConfigurationPage.renderStatRow(dict['StorageResponseCacheHitRate'], DlnaConfigurationPage.formatPercent(browseMetrics.ResponseCacheHitRate || 0));
        html += DlnaConfigurationPage.renderStatRow(dict['StorageNodeCacheHitRate'], DlnaConfigurationPage.formatPercent(browseMetrics.NodeCacheHitRate || 0));
        html += DlnaConfigurationPage.renderStatRow(dict['StorageIndexHitRate'], DlnaConfigurationPage.formatPercent(browseMetrics.IndexHitRate || 0));
        html += DlnaConfigurationPage.renderStatRow(dict['StorageInvalidationCount'], browseMetrics.InvalidationCount || 0);
        html += '</div>';

        html += '<div class="verticalSection verticalSection-extrabottompadding">';
        html += '<h3 class="sectionTitle">' + dict['StorageGenerations'] + '</h3>';
        html += DlnaConfigurationPage.renderStatRow(dict['StorageIndexGeneration'], stats.IndexGeneration || 0);
        html += DlnaConfigurationPage.renderStatRow(dict['StorageLibraryGeneration'], stats.LibraryGeneration || 0);
        html += '</div>';

        container.innerHTML = html;
    },
    loadStorageStats: function(page) {
        const dict = DlnaConfigurationPage.getDictionary(page);
        const container = page.querySelector('#dlnaStorageStats');
        if (container) {
            container.innerHTML = '<div class="fieldDescription">' + dict['StorageStatsLoading'] + '</div>';
        }

        return ApiClient.ajax({
            url: ApiClient.getUrl('Dlna/Storage/Stats'),
            type: 'GET',
            dataType: 'json'
        }).then(function(stats) {
            DlnaConfigurationPage.renderStorageStats(page, DlnaConfigurationPage.normalizeStorageStats(stats));
            return stats;
        }).catch(function() {
            if (container) {
                container.innerHTML = '<div class="fieldDescription">' + dict['StorageStatsLoadFailed'] + '</div>';
            }
        });
    },
    runStorageAction: function(page, options) {
        const dict = DlnaConfigurationPage.getDictionary(page);
        const message = options.confirmMessage;
        if (message && !confirm(message)) {
            return Promise.resolve();
        }

        Dashboard.showLoadingMsg();
        return ApiClient.ajax({
            url: options.url,
            type: 'POST'
        }).then(function() {
            const successMessage = options.startedInBackground ? dict['StorageActionStarted'] : dict['StorageActionCompleted'];
            Dashboard.alert(successMessage);
            return DlnaConfigurationPage.loadStorageStats(page);
        }).catch(function() {
            Dashboard.alert(dict['StorageActionFailed']);
        }).finally(function() {
            Dashboard.hideLoadingMsg();
        });
    },
    bindStorageActions: function(page) {
        const dict = DlnaConfigurationPage.getDictionary(page);
        const refreshButton = page.querySelector('#dlnaRefreshStorageStats');
        if (!refreshButton) {
            return;
        }

        refreshButton.addEventListener('click', function() {
            Dashboard.showLoadingMsg();
            DlnaConfigurationPage.loadStorageStats(page).finally(function() {
                Dashboard.hideLoadingMsg();
            });
        });

        page.querySelector('#dlnaClearBrowseCache').addEventListener('click', function() {
            DlnaConfigurationPage.runStorageAction(page, {
                url: ApiClient.getUrl('Dlna/Storage/ClearBrowseCache'),
                confirmMessage: dict['ConfirmClearBrowseCache']
            });
        });

        page.querySelector('#dlnaClearIndex').addEventListener('click', function() {
            DlnaConfigurationPage.runStorageAction(page, {
                url: ApiClient.getUrl('Dlna/Storage/ClearIndex'),
                confirmMessage: dict['ConfirmClearIndex']
            });
        });

        page.querySelector('#dlnaClearAllStorage').addEventListener('click', function() {
            DlnaConfigurationPage.runStorageAction(page, {
                url: ApiClient.getUrl('Dlna/Storage/ClearAll'),
                confirmMessage: dict['ConfirmClearAllStorage']
            });
        });

        page.querySelector('#dlnaRebuildIndex').addEventListener('click', function() {
            const prewarm = page.querySelector('#prewarmBrowseResponses').checked;
            DlnaConfigurationPage.runStorageAction(page, {
                url: ApiClient.getUrl('Dlna/Storage/RebuildIndex', { prewarm: prewarm }),
                confirmMessage: dict['ConfirmRebuildIndex'],
                startedInBackground: true
            });
        });

        page.querySelector('#dlnaClearAndRebuild').addEventListener('click', function() {
            const prewarm = page.querySelector('#prewarmBrowseResponses').checked;
            DlnaConfigurationPage.runStorageAction(page, {
                url: ApiClient.getUrl('Dlna/Storage/ClearAndRebuild', { prewarm: prewarm }),
                confirmMessage: dict['ConfirmClearAndRebuild'],
                startedInBackground: true
            });
        });
    },
    applyIndexUi: function(page) {
        const indexEnabled = page.querySelector('#enableVirtualFolderIndex').checked;
        const warmupCheckbox = page.querySelector('#warmupIndexOnStartup');
        if (warmupCheckbox) {
            warmupCheckbox.disabled = !indexEnabled;
        }
    },
    applyQuestModeUi: function(page) {
        const questMode = page.querySelector('#enableQuestCompatibilityMode').checked;
        const questManagedIds = [
            'childCountCalculation',
            'enableChildCountCache',
            'enableBrowseResponseCache',
            'browseResponseCacheTtlSeconds',
            'respectRequestedCount'
        ];

        questManagedIds.forEach(function(id) {
            const element = page.querySelector('#' + id);
            if (element) {
                element.disabled = questMode;
            }
        });

        DlnaConfigurationPage.syncChildCountCacheControl(page);
    },
    syncChildCountCacheControl: function(page) {
        const dict = DlnaConfigurationPage.getDictionary(page);
        const questMode = page.querySelector('#enableQuestCompatibilityMode').checked;
        const mode = page.querySelector('#childCountCalculation').value;
        const checkbox = page.querySelector('#enableChildCountCache');
        const help = page.querySelector('#enableChildCountCacheHelp');
        const disabled = questMode || mode === 'Disabled';

        if (checkbox) {
            checkbox.disabled = disabled;
        }

        if (help) {
            help.textContent = disabled
                ? dict['EnableChildCountCacheHelpDisabled']
                : dict['EnableChildCountCacheHelp'];
        }
    },
    loadConfiguration: function(page) {
        DlnaConfigurationPage.translatePage(page);
        DlnaConfigurationPage.bindCollapsible(page);

        ApiClient.getPluginConfiguration(this.pluginUniqueId)
            .then(function(config) {
                page.querySelector('#dlnaPlayTo').checked = config.EnablePlayTo;
                page.querySelector('#dlnaDiscoveryInterval').value = parseInt(config.ClientDiscoveryIntervalSeconds) || DlnaConfigurationPage.defaultDiscoveryInterval;
                page.querySelector('#dlnaBlastAlive').checked = config.BlastAliveMessages;
                page.querySelector('#dlnaAliveInterval').value = parseInt(config.AliveMessageIntervalSeconds) || DlnaConfigurationPage.defaultAliveInterval;
                page.querySelector('#dlnaMatchedHost').checked = config.SendOnlyMatchedHost;
                page.querySelector('#enableRecentlyAddedEpisodes').checked = config.EnableRecentlyAddedEpisodes;
                page.querySelector('#enableRecentlyAddedSeries').checked = config.EnableRecentlyAddedSeries;
                page.querySelector('#enableRecentlyReleasedEpisodes').checked = config.EnableRecentlyReleasedEpisodes;
                page.querySelector('#enableRecentlyReleasedSeries').checked = config.EnableRecentlyReleasedSeries !== false;
                page.querySelector('#enableCurrentlyAiring').checked = config.EnableCurrentlyAiring !== false;
                page.querySelector('#enableBrowseByKana').checked = config.EnableBrowseByKana !== false;
                DlnaConfigurationPage.libraryTitleBrowseOverrides = config.LibraryTitleBrowseOverrides || [];
                DlnaConfigurationPage.populateTitleBrowsePresetOptions(page, config);
                page.querySelector('#activeTitleBrowsePresetId').value = config.ActiveTitleBrowsePresetId || DlnaConfigurationPage.defaultTitleBrowsePresetId;
                page.querySelector('#hideEmptyVirtualFolders').checked = config.HideEmptyVirtualFolders === true;
                page.querySelector('#titleBrowsePresetsJson').value = DlnaConfigurationPage.formatTitleBrowsePresetsJson(config.TitleBrowsePresets);
                DlnaConfigurationPage.fitAdvancedTextarea(page.querySelector('#titleBrowsePresetsJson'), false);
                DlnaConfigurationPage.populateLibraryTitleBrowseOptions(page, config);
                page.querySelector('#enableBrowseByYear').checked = config.EnableBrowseByYear !== false;
                page.querySelector('#enableRecentlyAddedMovies').checked = config.EnableRecentlyAddedMovies;
                page.querySelector('#enableRecentlyReleasedMovies').checked = config.EnableRecentlyReleasedMovies;
                page.querySelector('#enableThreeDMovies').checked = config.EnableThreeDMovies;
                page.querySelector('#enableAuto3DTagging').checked = config.EnableAuto3DTagging;
                page.querySelector('#enableFourKMovies').checked = config.EnableFourKMovies;
                page.querySelector('#enableEightKMovies').checked = config.EnableEightKMovies;
                page.querySelector('#enableVrMovies').checked = config.EnableVrMovies;
                page.querySelector('#enableEightKVrMovies').checked = config.EnableEightKVrMovies;
                page.querySelector('#enableAutoResolutionTagging').checked = config.EnableAutoResolutionTagging;
                page.querySelector('#enableAutoVrTagging').checked = config.EnableAutoVrTagging;
                page.querySelector('#enableExtras').checked = config.EnableExtras;
                page.querySelector('#enableQuestCompatibilityMode').checked = config.EnableQuestCompatibilityMode === true;
                page.querySelector('#ensurePlaybackUrlsInBrowse').checked = config.EnsurePlaybackUrlsInBrowse !== false;
                page.querySelector('#childCountCalculation').value = config.ChildCountCalculation || 'Estimate';
                page.querySelector('#enableChildCountCache').checked = config.EnableChildCountCache !== false;
                page.querySelector('#enableBrowseResponseCache').checked = config.EnableBrowseResponseCache !== false;
                page.querySelector('#browseResponseCacheTtlSeconds').value = config.BrowseResponseCacheTtlSeconds ?? 300;
                page.querySelector('#enableItemSummaryBrowse').checked = config.EnableItemSummaryBrowse !== false;
                page.querySelector('#virtualListImagePresentation').value = config.VirtualListImagePresentation || 'Poster';
                page.querySelector('#searchImagePresentation').value = config.SearchImagePresentation || 'Poster';
                page.querySelector('#episodeListImageSource').value = config.EpisodeListImageSource || 'Episode';
                page.querySelector('#enableBrowseNodeCache').checked = config.EnableBrowseNodeCache !== false;
                page.querySelector('#browseNodeCacheTtlSeconds').value = config.BrowseNodeCacheTtlSeconds ?? 300;
                page.querySelector('#maxBrowseItemsPerResponse').value = config.MaxBrowseItemsPerResponse || 1000;
                page.querySelector('#respectRequestedCount').checked = config.RespectRequestedCount === true;
                page.querySelector('#enableStrictTotalMatches').checked = config.EnableStrictTotalMatches === true;
                page.querySelector('#maxRecentlyAddedItems').value = config.MaxRecentlyAddedItems || 300;
                page.querySelector('#maxSeriesListItems').value = config.MaxSeriesListItems || 0;
                page.querySelector('#enableVirtualFolderIndex').checked = config.EnableVirtualFolderIndex !== false;
                page.querySelector('#warmupIndexOnStartup').checked = config.WarmupIndexOnStartup !== false;
                page.querySelector('#prewarmBrowseResponses').checked = config.PrewarmBrowseResponses === true;
                page.querySelector('#prewarmFacetItemFolders').checked = config.PrewarmFacetItemFolders === true;
                page.querySelector('#prewarmHierarchyFolders').checked = config.PrewarmHierarchyFolders === true;
                page.querySelector('#prewarmHierarchyMaxSeries').value = config.PrewarmHierarchyMaxSeries ?? 20;
                page.querySelector('#prewarmHierarchyMaxSeasonsPerSeries').value = config.PrewarmHierarchyMaxSeasonsPerSeries ?? 3;
                page.querySelector('#enableIndexSeriesList').checked = config.EnableIndexSeriesList !== false;
                page.querySelector('#enableIndexMoviesList').checked = config.EnableIndexMoviesList !== false;
                page.querySelector('#enableIndexSeasonList').checked = config.EnableIndexSeasonList !== false;
                page.querySelector('#enableIndexEpisodeList').checked = config.EnableIndexEpisodeList !== false;
                page.querySelector('#enableIndexMusicGenre').checked = config.EnableIndexMusicGenre !== false;
                page.querySelector('#enableIndexPerson').checked = config.EnableIndexPerson !== false;
                page.querySelector('#enableIndexRecentlyModifiedSeries').checked = config.EnableIndexRecentlyModifiedSeries !== false;
                page.querySelector('#enableIndexRecentlyModifiedEpisodes').checked = config.EnableIndexRecentlyModifiedEpisodes !== false;
                page.querySelector('#enableIndexRecentlyModifiedMovies').checked = config.EnableIndexRecentlyModifiedMovies !== false;
                page.querySelector('#enableIndexGenre').checked = config.EnableIndexGenre !== false;
                page.querySelector('#enableIndexYear').checked = config.EnableIndexYear !== false;
                page.querySelector('#enableIndexRecentlyReleasedEpisodes').checked = config.EnableIndexRecentlyReleasedEpisodes !== false;
                page.querySelector('#enableIndexRecentlyReleasedSeries').checked = config.EnableIndexRecentlyReleasedSeries !== false;
                page.querySelector('#enableIndexRecentlyReleasedMovies').checked = config.EnableIndexRecentlyReleasedMovies !== false;
                page.querySelector('#rebuildIndexAfterLibraryScan').checked = config.RebuildIndexAfterLibraryScan !== false;
                page.querySelector('#debounceLibraryChangeInvalidation').checked = config.DebounceLibraryChangeInvalidation !== false;
                page.querySelector('#libraryChangeDebounceSeconds').value = config.LibraryChangeDebounceSeconds ?? 60;
                page.querySelector('#enableRecentlyUpdatedSeries').checked = config.EnableRecentlyUpdatedSeries !== false;
                page.querySelector('#enableBrowseByStudio').checked = config.EnableBrowseByStudio !== false;
                page.querySelector('#enableBrowseByTag').checked = config.EnableBrowseByTag !== false;
                page.querySelector('#enableBrowseByRating').checked = config.EnableBrowseByRating !== false;
                page.querySelector('#enableBrowseByPerson').checked = config.EnableBrowseByPerson !== false;
                page.querySelector('#enableRecentlyModifiedSeries').checked = config.EnableRecentlyModifiedSeries !== false;
                page.querySelector('#enableRecentlyModifiedEpisodes').checked = config.EnableRecentlyModifiedEpisodes !== false;
                page.querySelector('#enableRecentlyModifiedMovies').checked = config.EnableRecentlyModifiedMovies !== false;
                page.querySelector('#largeFolderRangeSplitThreshold').value = config.LargeFolderRangeSplitThreshold ?? 500;
                page.querySelector('#rangeFolderSize').value = config.RangeFolderSize ?? 500;
                page.querySelector('#enableDebugLogging').checked = config.EnableDebugLogging === true;

                DlnaConfigurationPage.applyQuestModeUi(page);
                DlnaConfigurationPage.applyIndexUi(page);
                page.querySelector('#enableQuestCompatibilityMode').addEventListener('change', function() {
                    DlnaConfigurationPage.applyQuestModeUi(page);
                });
                page.querySelector('#enableVirtualFolderIndex').addEventListener('change', function() {
                    DlnaConfigurationPage.applyIndexUi(page);
                });
                page.querySelector('#childCountCalculation').addEventListener('change', function() {
                    DlnaConfigurationPage.syncChildCountCacheControl(page);
                });

                DlnaConfigurationPage.loadStorageStats(page);

                ApiClient.getUsers()
                    .then(function(users) {
                        DlnaConfigurationPage.populateUsers(page, users, config.DefaultUserId);
                    })
                    .finally(function() {
                        Dashboard.hideLoadingMsg();
                    });
            })
            .catch(function(err) {
                console.error('[DLNA] Failed to load plugin configuration', err);
                Dashboard.hideLoadingMsg();
            });
    },
    populateUsers: function(page, users, selectedId) {
        const dict = DlnaConfigurationPage.getDictionary(page);
        const noneLabel = dict['None'] || 'None';
        let html = '';
        html += '<option value="">' + noneLabel + '</option>';
        for (let i = 0, length = users.length; i < length; i++) {
            const user = users[i];
            html += '<option value="' + user.Id + '">' + user.Name + '</option>';
        }

        page.querySelector('#dlnaSelectUser').innerHTML = html;
        page.querySelector('#dlnaSelectUser').value = selectedId;
    },
    formatTitleBrowsePresetsJson: function(presets) {
        try {
            return JSON.stringify(presets || [], null, 2);
        } catch (e) {
            return '[]';
        }
    },
    parseTitleBrowsePresetsJson: function(value) {
        if (!value || !value.trim()) {
            return [];
        }

        return JSON.parse(value);
    },
    populateTitleBrowsePresetOptions: function(page, config) {
        const presets = (config.TitleBrowsePresets && config.TitleBrowsePresets.length)
            ? config.TitleBrowsePresets
            : DlnaConfigurationPage.getDefaultTitleBrowsePresets();
        const activeSelect = page.querySelector('#activeTitleBrowsePresetId');
        const overrideSelect = page.querySelector('#libraryTitleBrowsePresetOverride');
        if (!activeSelect || !overrideSelect) {
            return;
        }

        const isJa = DlnaConfigurationPage.getLanguage().startsWith('ja');
        let html = '';

        for (let i = 0; i < presets.length; i++) {
            const preset = presets[i];
            const label = isJa ? (preset.NameJa || preset.Id) : (preset.NameEn || preset.Id);
            html += '<option value="' + preset.Id + '">' + label + '</option>';
        }

        activeSelect.innerHTML = html;
        const dict = DlnaConfigurationPage.getDictionary(page);
        overrideSelect.innerHTML = '<option value="">' + (dict['LibraryTitleBrowseUseGlobal'] || '(Use global preset)') + '</option>' + html;
    },
    getDefaultTitleBrowsePresets: function() {
        return [
            { Id: 'alphabet', NameJa: 'アルファベット', NameEn: 'Alphabet' },
            { Id: 'japanese-kana', NameJa: '日本語五十音', NameEn: 'Japanese Kana' }
        ];
    },
    populateLibraryTitleBrowseOptions: function(page, config) {
        const select = page.querySelector('#libraryTitleBrowseLibraryId');
        if (!select) {
            return;
        }

        const dict = DlnaConfigurationPage.getDictionary(page);

        ApiClient.getJSON(ApiClient.getUrl('Library/VirtualFolders')).then(function(libraries) {
            let html = '<option value="">' + (dict['None'] || '(None)') + '</option>';
            for (let i = 0; i < libraries.length; i++) {
                const library = libraries[i];
                html += '<option value="' + library.ItemId + '">' + library.Name + '</option>';
            }

            select.innerHTML = html;
            select.onchange = function() {
                DlnaConfigurationPage.loadLibraryTitleBrowseOverride(page);
            };
            DlnaConfigurationPage.loadLibraryTitleBrowseOverride(page);
        }).catch(function() {
            select.innerHTML = '<option value="">' + (dict['None'] || '(None)') + '</option>';
        });
    },
    loadLibraryTitleBrowseOverride: function(page) {
        const librarySelect = page.querySelector('#libraryTitleBrowseLibraryId');
        const presetOverrideSelect = page.querySelector('#libraryTitleBrowsePresetOverride');
        const regexField = page.querySelector('#libraryTitleStripRegexes');
        if (!librarySelect || !presetOverrideSelect || !regexField) {
            return;
        }

        const libraryId = librarySelect.value;
        const override = (DlnaConfigurationPage.libraryTitleBrowseOverrides || []).find(function(entry) {
            return entry.LibraryId === libraryId;
        });

        presetOverrideSelect.value = override && override.PresetId ? override.PresetId : '';
        regexField.value = override && override.TitleStripRegexes
            ? override.TitleStripRegexes.join('\n')
            : '';
        DlnaConfigurationPage.fitAdvancedTextarea(regexField, false);
    },
    saveLibraryTitleBrowseOverride: function(page) {
        const libraryId = page.querySelector('#libraryTitleBrowseLibraryId').value;
        if (!libraryId) {
            return;
        }

        const presetId = page.querySelector('#libraryTitleBrowsePresetOverride').value || null;
        const regexValue = page.querySelector('#libraryTitleStripRegexes').value || '';
        const regexes = regexValue
            .split(/\r?\n/)
            .map(function(line) { return line.trim(); })
            .filter(function(line) { return line.length > 0; });

        const overrides = (DlnaConfigurationPage.libraryTitleBrowseOverrides || []).filter(function(entry) {
            return entry.LibraryId !== libraryId;
        });

        if (presetId || regexes.length > 0) {
            overrides.push({
                LibraryId: libraryId,
                PresetId: presetId,
                TitleStripRegexes: regexes
            });
        }

        DlnaConfigurationPage.libraryTitleBrowseOverrides = overrides;
    },
    save: function(page) {
        Dashboard.showLoadingMsg();
        return new Promise(function(_) {
            ApiClient.getPluginConfiguration(DlnaConfigurationPage.pluginUniqueId)
                .then(function(config) {
                    config.EnablePlayTo = page.querySelector('#dlnaPlayTo').checked;
                    config.ClientDiscoveryIntervalSeconds = parseInt(page.querySelector('#dlnaDiscoveryInterval').value) || DlnaConfigurationPage.defaultDiscoveryInterval;
                    config.BlastAliveMessages = page.querySelector('#dlnaBlastAlive').checked;
                    config.AliveMessageIntervalSeconds = parseInt(page.querySelector('#dlnaAliveInterval').value) || DlnaConfigurationPage.defaultAliveInterval;
                    config.SendOnlyMatchedHost = page.querySelector('#dlnaMatchedHost').checked;
                    config.EnableRecentlyAddedEpisodes = page.querySelector('#enableRecentlyAddedEpisodes').checked;
                    config.EnableRecentlyAddedSeries = page.querySelector('#enableRecentlyAddedSeries').checked;
                    config.EnableRecentlyReleasedEpisodes = page.querySelector('#enableRecentlyReleasedEpisodes').checked;
                    config.EnableRecentlyReleasedSeries = page.querySelector('#enableRecentlyReleasedSeries').checked;
                    config.EnableCurrentlyAiring = page.querySelector('#enableCurrentlyAiring').checked;
                    config.EnableBrowseByKana = page.querySelector('#enableBrowseByKana').checked;
                    DlnaConfigurationPage.saveLibraryTitleBrowseOverride(page);
                    config.ActiveTitleBrowsePresetId = page.querySelector('#activeTitleBrowsePresetId').value || DlnaConfigurationPage.defaultTitleBrowsePresetId;
                    config.HideEmptyVirtualFolders = page.querySelector('#hideEmptyVirtualFolders').checked;
                    try {
                        const parsedPresets = DlnaConfigurationPage.parseTitleBrowsePresetsJson(page.querySelector('#titleBrowsePresetsJson').value);
                        if (parsedPresets && parsedPresets.length) {
                            config.TitleBrowsePresets = parsedPresets;
                        }
                    } catch (e) {
                        Dashboard.alert('Invalid title browse presets JSON.');
                        Dashboard.hideLoadingMsg();
                        return;
                    }
                    config.LibraryTitleBrowseOverrides = DlnaConfigurationPage.libraryTitleBrowseOverrides || [];
                    config.EnableBrowseByYear = page.querySelector('#enableBrowseByYear').checked;
                    config.EnableRecentlyAddedMovies = page.querySelector('#enableRecentlyAddedMovies').checked;
                    config.EnableRecentlyReleasedMovies = page.querySelector('#enableRecentlyReleasedMovies').checked;
                    config.EnableThreeDMovies = page.querySelector('#enableThreeDMovies').checked;
                    config.EnableAuto3DTagging = page.querySelector('#enableAuto3DTagging').checked;
                    config.EnableFourKMovies = page.querySelector('#enableFourKMovies').checked;
                    config.EnableEightKMovies = page.querySelector('#enableEightKMovies').checked;
                    config.EnableVrMovies = page.querySelector('#enableVrMovies').checked;
                    config.EnableEightKVrMovies = page.querySelector('#enableEightKVrMovies').checked;
                    config.EnableAutoResolutionTagging = page.querySelector('#enableAutoResolutionTagging').checked;
                    config.EnableAutoVrTagging = page.querySelector('#enableAutoVrTagging').checked;
                    config.EnableExtras = page.querySelector('#enableExtras').checked;
                    config.EnableQuestCompatibilityMode = page.querySelector('#enableQuestCompatibilityMode').checked;
                    config.EnsurePlaybackUrlsInBrowse = page.querySelector('#ensurePlaybackUrlsInBrowse').checked;
                    config.ChildCountCalculation = page.querySelector('#childCountCalculation').value;
                    config.EnableChildCountCache = page.querySelector('#enableChildCountCache').checked;
                    config.EnableBrowseResponseCache = page.querySelector('#enableBrowseResponseCache').checked;
                    config.BrowseResponseCacheTtlSeconds = parseInt(page.querySelector('#browseResponseCacheTtlSeconds').value) || 0;
                    config.EnableItemSummaryBrowse = page.querySelector('#enableItemSummaryBrowse').checked;
                    config.VirtualListImagePresentation = page.querySelector('#virtualListImagePresentation').value;
                    config.SearchImagePresentation = page.querySelector('#searchImagePresentation').value;
                    config.EpisodeListImageSource = page.querySelector('#episodeListImageSource').value;
                    config.EnableBrowseNodeCache = page.querySelector('#enableBrowseNodeCache').checked;
                    config.BrowseNodeCacheTtlSeconds = parseInt(page.querySelector('#browseNodeCacheTtlSeconds').value) || 0;
                    config.MaxBrowseItemsPerResponse = parseInt(page.querySelector('#maxBrowseItemsPerResponse').value) || 1000;
                    config.RespectRequestedCount = page.querySelector('#respectRequestedCount').checked;
                    config.EnableStrictTotalMatches = page.querySelector('#enableStrictTotalMatches').checked;
                    config.MaxRecentlyAddedItems = parseInt(page.querySelector('#maxRecentlyAddedItems').value) || 300;
                    config.MaxSeriesListItems = parseInt(page.querySelector('#maxSeriesListItems').value) || 0;
                    config.EnableVirtualFolderIndex = page.querySelector('#enableVirtualFolderIndex').checked;
                    config.WarmupIndexOnStartup = page.querySelector('#warmupIndexOnStartup').checked;
                    config.PrewarmBrowseResponses = page.querySelector('#prewarmBrowseResponses').checked;
                    config.PrewarmFacetItemFolders = page.querySelector('#prewarmFacetItemFolders').checked;
                    config.PrewarmHierarchyFolders = page.querySelector('#prewarmHierarchyFolders').checked;
                    config.PrewarmHierarchyMaxSeries = parseInt(page.querySelector('#prewarmHierarchyMaxSeries').value) || 20;
                    config.PrewarmHierarchyMaxSeasonsPerSeries = parseInt(page.querySelector('#prewarmHierarchyMaxSeasonsPerSeries').value) || 3;
                    config.EnableIndexSeriesList = page.querySelector('#enableIndexSeriesList').checked;
                    config.EnableIndexMoviesList = page.querySelector('#enableIndexMoviesList').checked;
                    config.EnableIndexSeasonList = page.querySelector('#enableIndexSeasonList').checked;
                    config.EnableIndexEpisodeList = page.querySelector('#enableIndexEpisodeList').checked;
                    config.EnableIndexMusicGenre = page.querySelector('#enableIndexMusicGenre').checked;
                    config.EnableIndexPerson = page.querySelector('#enableIndexPerson').checked;
                    config.EnableIndexRecentlyModifiedSeries = page.querySelector('#enableIndexRecentlyModifiedSeries').checked;
                    config.EnableIndexRecentlyModifiedEpisodes = page.querySelector('#enableIndexRecentlyModifiedEpisodes').checked;
                    config.EnableIndexRecentlyModifiedMovies = page.querySelector('#enableIndexRecentlyModifiedMovies').checked;
                    config.EnableIndexGenre = page.querySelector('#enableIndexGenre').checked;
                    config.EnableIndexYear = page.querySelector('#enableIndexYear').checked;
                    config.EnableIndexRecentlyReleasedEpisodes = page.querySelector('#enableIndexRecentlyReleasedEpisodes').checked;
                    config.EnableIndexRecentlyReleasedSeries = page.querySelector('#enableIndexRecentlyReleasedSeries').checked;
                    config.EnableIndexRecentlyReleasedMovies = page.querySelector('#enableIndexRecentlyReleasedMovies').checked;
                    config.RebuildIndexAfterLibraryScan = page.querySelector('#rebuildIndexAfterLibraryScan').checked;
                    config.DebounceLibraryChangeInvalidation = page.querySelector('#debounceLibraryChangeInvalidation').checked;
                    config.LibraryChangeDebounceSeconds = parseInt(page.querySelector('#libraryChangeDebounceSeconds').value) || 60;
                    config.EnableRecentlyUpdatedSeries = page.querySelector('#enableRecentlyUpdatedSeries').checked;
                    config.EnableBrowseByStudio = page.querySelector('#enableBrowseByStudio').checked;
                    config.EnableBrowseByTag = page.querySelector('#enableBrowseByTag').checked;
                    config.EnableBrowseByRating = page.querySelector('#enableBrowseByRating').checked;
                    config.EnableBrowseByPerson = page.querySelector('#enableBrowseByPerson').checked;
                    config.EnableRecentlyModifiedSeries = page.querySelector('#enableRecentlyModifiedSeries').checked;
                    config.EnableRecentlyModifiedEpisodes = page.querySelector('#enableRecentlyModifiedEpisodes').checked;
                    config.EnableRecentlyModifiedMovies = page.querySelector('#enableRecentlyModifiedMovies').checked;
                    config.LargeFolderRangeSplitThreshold = parseInt(page.querySelector('#largeFolderRangeSplitThreshold').value) || 500;
                    config.RangeFolderSize = parseInt(page.querySelector('#rangeFolderSize').value) || 500;
                    config.EnableDebugLogging = page.querySelector('#enableDebugLogging').checked;

                    const selectedUser = page.querySelector('#dlnaSelectUser').value;
                    config.DefaultUserId = selectedUser.length > 0 ? selectedUser : null;

                    ApiClient.updatePluginConfiguration(DlnaConfigurationPage.pluginUniqueId, config).then(Dashboard.processPluginConfigurationUpdateResult);
                });
        });
    }
};

export default function(view) {
    view.querySelector('#dlnaForm').addEventListener('submit', function(e) {
        DlnaConfigurationPage.save(view);
        e.preventDefault();
        return false;
    });

    DlnaConfigurationPage.bindStorageActions(view);
    DlnaConfigurationPage.bindTabs(view);
    DlnaConfigurationPage.bindCollapsible(view);

    view.addEventListener('viewshow', function() {
        Dashboard.showLoadingMsg();
        DlnaConfigurationPage.loadConfiguration(view);
    });
}
