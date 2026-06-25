# DLNA Browse パフォーマンス改善（フェーズ2）実装報告書

| 項目 | 内容 |
|------|------|
| 対象 | Jellyfin DLNA プラグイン（フォーク: `jellyfin/jellyfin-plugin-dlna` ベース） |
| 前提 | [フェーズ1報告書](dlna-browse-performance-phase1-report.md)（コミット `03a8d8d`）で childCount N+1・XML キャッシュを解消済み |
| 目的 | 初回 Browse の「検索・集計」ボトルネックを SQLite 仮想インデックスで解消し、ライブラリ更新時のキャッシュ破棄を選択的にする |
| 主な症状 | 五十音・最近追加・スタジオ別等の初回 Browse が全件 DB 走査；1 アイテム更新で全 XML キャッシュ破棄 |
| 修正範囲 | `Indexing/*`, `ControlHandler`, キャッシュ無効化, 設定 UI, スケジュールタスク, 単体テスト |
| 報告日 | 2026-06-23 |

---

## 1. エグゼクティブサマリー

フェーズ1で DIDL XML キャッシュ（層4）と childCount N+1 を解消したが、**初回 Browse は依然として Jellyfin DB の全件検索・集計が支配的**であった。特に `GetBrowseByKanaRowItems`（全件取得 + LINQ 分類）、最近追加系、スタジオ/タグ/レーティング（未実装）がボトルネックだった。

フェーズ2では以下を実装した。

1. **SQLite 仮想インデックス（層1）** — 仮想フォルダごとの事前ソート済み `Guid[]`
2. **item_summary テーブル（層2）** — インデックス構築時の最小メタデータ保存
3. **Browse 計測ログ** — `BrowseTimingScope` による構造化ログと `IBrowseMetrics`
4. **選択的キャッシュ無効化** — ライブラリ単位 + 60 秒デバウンス
5. **新仮想フォルダ** — 最近更新シリーズ、スタジオ/タグ/レーティング別、シリーズ範囲分割
6. **ウォームアップ・手動タスク** — 起動時構築 + `Rebuild DLNA Quest Index`
7. **フェーズ2追補** — Browse XML プリウォーム、設定変更時の全破棄、`InvalidatePattern`
8. **バグ修正** — `kana_row` DELETE クエリの列名誤り（`list_type` → `item_type`/`row_index`）

目標は「全件返却を維持しつつ、Browse 時に全件検索をしない」ことである。

---

## 2. アーキテクチャ

### 2.1 4 層キャッシュ

```text
DLNA Browse
  ├─ L4 BrowseResponseCache（DIDL XML）     … 既存拡張
  ├─ L3 BrowseNodeCache（中間ノード）      … 未実装（任意）
  ├─ L2 item_summary（SQLite）              … 新規
  └─ L1 VirtualIndex（SQLite）             … 新規
        ↑ IndexBuilder / Warmup / ScheduledTask
```

| 層 | 新規/既存 | 内容 |
|----|----------|------|
| 1 VirtualIndex | **新規** | `virtual_list`, `kana_row`, `facet_index` |
| 2 ItemSummary | **新規** | `item_summary` テーブル |
| 3 BrowseNodeCache | 任意 | 後続フェーズ |
| 4 BrowseResponseCache | 拡張 | `InvalidateLibrary()`, `IndexGeneration` をキャッシュキーに追加 |

### 2.2 インデックス DB

- パス: `{PluginConfigurationsPath}/dlna/dlna-index.db`
- 依存: `Microsoft.Data.Sqlite` 9.0.11

**主要テーブル:**

| テーブル | 用途 |
|----------|------|
| `library_indexed` | ライブラリごとの構築完了フラグ |
| `virtual_list` | 最近追加/最近更新/全シリーズ等のソート済み ItemId |
| `kana_row` | 五十音行 × アイテム種別ごとの ItemId |
| `facet_index` | スタジオ/タグ/レーティング/Extras の逆引き |
| `item_summary` | ItemId ごとの最小メタデータ |

---

## 3. 実装内容

### 3.1 新規コンポーネント

| ファイル | 役割 |
|----------|------|
| `Indexing/VirtualIndexStore.cs` | SQLite CRUD |
| `Indexing/DlnaIndexBuilder.cs` | ライブラリ走査とインデックス書き込み |
| `Indexing/DlnaVirtualIndexService.cs` | 再構築・無効化・世代カウンタ |
| `Indexing/IndexBrowseHelper.cs` | ControlHandler からのインデックス参照 + フォールバック |
| `ContentDirectory/BrowseTimingScope.cs` | Browse 計測ログ |
| `ContentDirectory/ContentInvalidationService.cs` | デバウンス付き選択的無効化 |
| `Tasks/RebuildDlnaQuestIndexTask.cs` | 手動/定期インデックス再構築 |
| `Indexing/DlnaIndexWarmupService.cs` | 起動時バックグラウンド構築 |

### 3.2 インデックス対象と Browse 統合

| 仮想フォルダ | VirtualList / Facet | フォールバック |
|-------------|---------------------|----------------|
| 最近追加（EP/Series/Movie） | `virtual_list` | 従来 `GetRecentlyAdded` |
| 最近更新シリーズ | `RecentlyUpdatedSeries` | 最近追加 Series 相当 |
| 五十音行 | `kana_row` | 全件 + `KanaRowHelper` |
| スタジオ/タグ/レーティング | `facet_index` | 空（インデックス未構築時） |
| Extras | `facet_index` (Extra) | `item.GetExtras()` |
| シリーズ範囲分割 | `SeriesAll` スライス | `GetChildrenOfItem` |

`EnableVirtualFolderIndex == false` またはインデックス未構築時は従来パスにフォールバックする。

### 3.3 新規 StubType / ObjectID

| StubType | 説明 |
|----------|------|
| `RecentlyUpdatedSeries` (37) | 子エピソード `DateCreated` 最大でソート |
| `BrowseByStudio` (38) / `StudioItem` (41) | スタジオ別 |
| `BrowseByTag` (39) / `TagItem` (42) | タグ別 |
| `BrowseByRating` (40) / `RatingItem` (43) | レーティング別 |
| `SeriesRange` (44) | 範囲フォルダ（例: 0001-0500） |

ObjectID 例:

- `studio_{libraryId:N}_{name}`
- `tag_{libraryId:N}_{name}`
- `rating_{libraryId:N}_{key}`
- `range_{libraryId:N}_{start}_{end}`

### 3.4 キャッシュ無効化

| イベント | 動作 |
|----------|------|
| Item 追加/更新/削除 | デバounce（既定 60s）後、該当 `libraryId` のみ XML キャッシュ + インデックス無効化 |
| 無効化後 | `RebuildIndexAfterLibraryScan` ON なら該当ライブラリを再構築 |
| 起動 | `WarmupIndexOnStartup` ON なら全ライブラリをバックグラウンド構築 |

`InvalidateByLibraryScope == false` の場合は従来どおり全破棄。

### 3.5 新規設定項目

| プロパティ | デフォルト | 説明 |
|-----------|-----------|------|
| `EnableVirtualFolderIndex` | `true` | 仮想インデックスの利用 |
| `WarmupIndexOnStartup` | `true` | 起動時ウォームアップ |
| `RebuildIndexAfterLibraryScan` | `true` | 変更後のインデックス再構築 |
| `DebounceLibraryChangeInvalidation` | `true` | 無効化のデバウンス |
| `LibraryChangeDebounceSeconds` | `60` | デバウンス秒数 |
| `EnableRecentlyUpdatedSeries` | `true` | 最近更新シリーズフォルダ表示 |
| `EnableBrowseByStudio/Tag/Rating` | `true` | facet 仮想フォルダ表示 |
| `LargeFolderRangeSplitThreshold` | `500` | 範囲分割の閾値 |
| `RangeFolderSize` | `500` | 1 範囲フォルダあたりの件数 |
| `PrewarmBrowseResponses` | `false` | インデックス構築後に主要 ObjectID の Browse XML を事前生成（層4ウォーム） |

管理 UI「Virtual Index Settings」セクションを `config.html` / `config.js` に追加。

### 3.6 計測ログ

```
[DLNA Browse] ObjectId=... StubType=RecentlyUpdatedSeries CacheHit=false IndexHit=true QueryMs=12 IndexMs=3 DtoMs=0 DidlMs=45 TotalMs=60 Items=1260 XmlBytes=5200000
```

`IBrowseMetrics` でキャッシュヒット率・インデックスヒット率・無効化回数を集計可能。

---

## 4. バグ修正（運用中に発見）

### 4.1 `kana_row` DELETE クエリの列名誤り

**症状:** `Rebuild DLNA Quest Index` 実行時に `SQLite Error 1: 'no such column: list_type'`

**原因:** `ReplaceOrderedList` の DELETE 分岐が `key2Column == null` のとき常に `list_type` 列を参照。`ReplaceKanaRow` は `key2Column = null` で呼ばれるため、`kana_row` テーブル（`item_type` / `row_index`）に対して誤った SQL が発行されていた。

**修正:** DELETE も INSERT と同様にテーブル名（`virtual_list` / `kana_row` / `facet_index`）で分岐。

### 4.2 DI 循環参照（Browse プリウォーム起動時）

**症状:** Jellyfin 起動失敗 `A circular dependency was detected for IDlnaBrowsePrewarmService`

**原因:** `DlnaBrowsePrewarmService` → `IContentDirectory` → `LibraryChangeNotifier` → `ContentInvalidationService` → `IDlnaBrowsePrewarmService` のループ。

**修正:** `IContentDirectory` をコンストラクタ注入せず、`PrewarmAsync` 実行時に `IServiceProvider` で遅延解決。

---

## 5. テスト

| テスト | 内容 |
|--------|------|
| `BrowseTimingScopeTests` | ログフィールドの存在 |
| `FacetObjectIdParserTests` | studio/range ObjectID の往復 |
| `BrowseResponseCacheLibraryTests` | ライブラリ単位キャッシュ無効化、`InvalidatePattern` |
| 既存 37 件 | フェーズ1 回帰 |

**合計 42 件合格**（`dotnet test`）

---

## 6. 運用手順

1. プラグインをビルド・配置し Jellyfin を再起動
2. 起動時ウォームアップ（設定 ON）または手動で **Rebuild DLNA Quest Index** を実行
3. DLNA クライアントで Browse；ログで `IndexHit=true` を確認
4. ライブラリ更新後はデバウンス経過後に該当ライブラリのみ再構築
5. （任意）**インデックス構築後に Browse XML をプリウォームする** を ON にし、ログで `DLNA browse prewarm completed` と `[DLNA Browse] CacheHit=True` を確認

### 6.1 Browse XML プリウォームの確認

| 確認項目 | ログの例 |
|---------|---------|
| プリウォーム完了 | `DLNA browse prewarm completed Libraries=1 Responses=25` |
| キャッシュヒット | `[DLNA Browse] ObjectId=recentlyaddedseries_... CacheHit=True TotalMs=数ms` |
| キャッシュミス | `CacheHit=False QueryMs=... DidlMs=...` |

**プリウォーム対象フォルダ:** 最近追加系、最近更新シリーズ、五十音行、スタジオ/タグ/レーティング一覧、シリーズ一覧（範囲分割含む）。

**対象外:** 最近リリース、ジャンル、年別、お気に入り、続きから等（従来パス）。

**注意:** キャッシュキーにデバイスプロファイル ID が含まれる。プリウォームはデフォルトプロファイルで実行するため、Quest 等が別プロファイルを使う場合は初回 `CacheHit=False` になることがある（2 回目でヒットすればキャッシュ自体は正常）。

---

## 7. 期待効果

| 操作 | フェーズ1 | フェーズ2 |
|------|-----------|-----------|
| 最近更新シリーズ（初回） | 全 Episode 走査 | インデックス読取 |
| 五十音行（初回） | 全件 + LINQ | インデックス読取 |
| スタジオ一覧（初回） | 未実装 | facet 集計済み |
| ライブラリ 1 件更新後 | 全 XML キャッシュ破棄 | debounce + 該当ライブラリのみ |
| スキャン後の初回 Browse | コールド | ウォームアップ済みなら即応答 |

---

## 8. 未実装・後続候補

フェーズ5で層2・層3および残りのインデックス候補を実装済み。詳細は [フェーズ5報告書](dlna-browse-performance-phase5-report.md)。

| 項目 | 状態 |
|------|------|
| 続きから / お気に入り / 次に見る | ユーザー別のため従来パス維持 |

### 8.1 フェーズ2追補（実装済み）

| 項目 | 実装 |
|------|------|
| `PrewarmBrowseResponses` | `DlnaBrowsePrewarmService` + `BrowsePrewarmPaths` — インデックス構築後に主要 ObjectID を Browse して層4を温める |
| `InvalidatePattern(objectIdPrefix)` | `IBrowseResponseCache.InvalidatePattern` |
| 設定変更時の全破棄 + 再構築 | `DlnaPlugin.UpdateConfiguration` + `DlnaPluginConfigurationMonitor` |
| VSCode Launch | `prepare-launch-jellyfin` に `build-jellyfin-debug` を追加（古い DLL デプロイ防止） |

### 8.2 フェーズ3（実装済み）

[フェーズ3報告書](dlna-browse-performance-phase3-report.md) を参照。

| 項目 | 実装 |
|------|------|
| ジャンル / 年別インデックス | `FacetType.Genre`, `FacetType.Year` |
| 最近リリースインデックス | `VirtualListType.RecentlyReleased*` |
| Browse 統合 | `GetGenres`, `GetBrowseByYear*`, `GetRecentlyReleased*` |
| プリウォーム拡張 | ジャンル・年別・最近リリース + `PrewarmFacetItemFolders` |

### 8.3 フェーズ4（実装済み）

[フェーズ4報告書](dlna-browse-performance-phase4-report.md) を参照。

| 項目 | 実装 |
|------|------|
| シリーズ / 映画一覧 | `SeriesAll`, `MoviesAll` |
| シーズン / エピソード一覧 | `SeasonOfSeries`, `EpisodeOfSeason` facet |
| Browse 統合 | `GetChildrenOfItem`, `GetMoviesWithOptionalExtras`, 物理 Series/Season フォルダ |

### 8.4 フェーズ5（実装済み）

[フェーズ5報告書](dlna-browse-performance-phase5-report.md) を参照。

| 項目 | 実装 |
|------|------|
| 層2 item_summary Browse | `GetItemSummaries`, `WriteSummaryElement`, `SummaryHit` |
| 層3 BrowseNodeCache | `BrowseNodeCache`, L3 hit で DIDL 再組立 |
| 音楽ジャンル / 出演者 / 最近メタデータ更新 | `MusicGenre`, `Person`, `RecentlyModified*` |
| 階層プリウォーム | `PrewarmHierarchyFolders` + 件数上限 |

---

## 9. 主要変更ファイル

| 領域 | ファイル |
|------|----------|
| 新規 Indexing | `Indexing/VirtualIndexStore.cs`, `DlnaIndexBuilder.cs`, `DlnaBrowsePrewarmService.cs` 等 |
| 新規 Tasks | `Tasks/RebuildDlnaQuestIndexTask.cs` |
| 計測・無効化 | `BrowseTimingScope.cs`, `ContentInvalidationService.cs`, `BrowseMetrics.cs` |
| 拡張 | `ControlHandler.cs`, `StubType.cs`, `ServerItem.cs`, `DidlBuilder.cs` |
| キャッシュ | `BrowseResponseCache.cs`, `LibraryChangeNotifier.cs`, `BrowseCacheModels.cs` |
| 設定 | `DlnaPluginConfiguration.cs`, `config.html`, `config.js` |
| DI | `DlnaServiceRegistrator.cs`, `DlnaPluginConfigurationMonitor.cs` |
| 開発 | `.vscode/tasks.json` |
| テスト | `BrowseTimingScopeTests.cs`, `FacetObjectIdParserTests.cs`, `BrowseResponseCacheLibraryTests.cs` |
| ドキュメント | `README.ja.md`, 本報告書 |
