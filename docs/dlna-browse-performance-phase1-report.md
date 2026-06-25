# DLNA Browse パフォーマンス改善（フェーズ1）実装報告書

| 項目 | 内容 |
|------|------|
| 対象 | Jellyfin DLNA プラグイン（フォーク: `jellyfin/jellyfin-plugin-dlna` ベース） |
| 目的 | Quest 3 等の DLNA プレイヤー向けに、全件表示を維持したまま Browse 応答を高速化する |
| 主な症状 | シリーズ一覧・ライブラリトップの Browse が極端に遅い（N+1 クエリ、重い DTO、毎回 XML 再生成） |
| 修正範囲 | `ControlHandler`, `DidlBuilder`, 設定 UI, キャッシュサービス, 単体テスト |
| 報告日 | 2026-06-23 |

---

## 1. エグゼクティブサマリー

DLNA Browse の遅延の主因は「全件取得」そのものではなく、**各フォルダの `childCount` を数えるために `GetUserItems()` を再実行していた N+1 クエリ**であった。さらに仮想フォルダ（シリーズ・五十音・ジャンル等のスタブ）でも同様の先読みが発生し、ライブラリを開いただけで多数のフルクエリが走っていた。

フェーズ1では以下を実装した。

1. **childCount 計算の制御**（省略 / 推定 / 正確）とスタブフォルダの先読み停止
2. **Quest 互換モードの挙動修正**（`RequestedCount` を無視して全件返却、50 件クランプ削除）
3. **一覧用 `DtoOptions` の軽量化**（`new DtoOptions(false)`）
4. **DIDL-Lite XML キャッシュ**（ライブラリ更新で破棄）
5. **設定 UI・単体テスト**の追加

目標は「件数制限で諦める」のではなく、**全件表示を維持したまま初回・2 回目以降を速くする**ことである。

---

## 2. 問題の構造

### 2.1 childCount による N+1

`HandleBrowse()` の `BrowseDirectChildren` では、親フォルダの子一覧取得後、**フォルダ表示される各子**に対して再度 `GetUserItems()` を呼び、`TotalRecordCount` を `childCount` 属性に書き込んでいた。

```text
シリーズ一覧 Browse
  → GetUserItems(親) で 500 シリーズ取得
  → 各シリーズごとに GetUserItems(子) でエピソード数カウント
  → 合計 501 回相当のクエリ
```

### 2.2 仮想フォルダの先読み

`GetTvFolders()` / `GetMovieFolders()` はトップ階層でスタブ（`StubType.Series` 等）のみ返す設計だが、`childCount` 計算が各スタブの中身まで問い合わせていた。

```text
アニメライブラリを開く
  → スタブ一覧は軽量
  → childCount 計算で「シリーズ全件」「ジャンル全件」等を連鎖的に先読み
```

### 2.3 Quest 互換モードの逆効果

従来の `ResolveBrowsePaging()` は Quest モード ON 時に `RespectRequestedCount` を強制 ON にし、上限 50 件にクランプしていた。Quest 3 プレイヤーが `RequestedCount=10` を送り次ページを取らない場合、**見えない項目が出る**ため、要件と逆の挙動であった。

### 2.4 その他のコスト

- 一覧 Browse で `new DtoOptions(true)` により MediaStreams 等の重いメタデータを毎回取得
- 同一フォルダの再 Browse でも毎回 Jellyfin DB 問い合わせ + XML 生成
- `UpdateID` が時刻ベースで、キャッシュ整合性に不向き

---

## 3. 実装内容

### 3.1 新規設定項目

| プロパティ | デフォルト | 説明 |
|-----------|-----------|------|
| `ChildCountCalculation` | `Estimate` | `Disabled` / `Estimate` / `Accurate` |
| `EnableChildCountCache` | `true` | 正確計算結果のキャッシュ |
| `EnableBrowseResponseCache` | `true` | Browse レスポンス XML のキャッシュ |
| `BrowseResponseCacheTtlSeconds` | `300` | TTL（0 = ライブラリ更新まで） |

Quest 互換モード ON 時（サーバー側）:

- `RequestedCount` を無視（全件返却）
- `ChildCountCalculation` は実質 `Disabled`（属性省略）
- Browse キャッシュを有効化

### 3.2 childCount 解決 (`ResolveChildCount`)

新規クラス `ChildCountResolution` と `ControlHandler.ResolveChildCount()` により:

| 条件 | 動作 |
|------|------|
| Quest モード or `Disabled` | `childCount` 属性を出力しない |
| 仮想スタブフォルダ | 中身を問い合わせない（`Estimate` 時は 0、`Accurate` でも省略） |
| `Accurate` + 物理フォルダ | `GetUserItems()` でカウント（キャッシュ可） |

`DidlBuilder.WriteFolderElement` は `int? childCount` を受け取り、`null` のとき属性を省略する。

`BrowseMetadata` でもフォルダ自身の childCount に同ロジックを適用し、不要な `GetUserItems()` をスキップする。

### 3.3 一覧 DtoOptions 軽量化

`GetBrowseListDtoOptions()` → `new DtoOptions(false)` を一覧系クエリ（シリーズ・映画・五十音行・フィルタ一覧等）に適用。`GetPlaybackDtoOptions()` は再生向けに `new DtoOptions(true)` を維持。

### 3.4 Browse レスポンスキャッシュ

| コンポーネント | 役割 |
|---------------|------|
| `BrowseResponseCache` | DIDL XML + NumberReturned + TotalMatches + UpdateID を保持 |
| `LibraryChangeNotifier` | `ILibraryManager` の ItemAdded/Updated/Removed で世代をインクリメントしキャッシュ破棄 |
| `BrowseConfigFingerprint` | 設定変更をキャッシュキーに反映 |

キャッシュキー: `UserId | ObjectId | BrowseFlag | SortCriteria | Filter | DeviceProfileId | LibraryGeneration | ConfigFingerprint | StartIndex | Limit`

`UpdateID` は `LibraryChangeNotifier.LibraryGeneration` を使用。

### 3.5 Quest 互換モード修正 (`BrowsePagingResolver`)

```csharp
// 修正後
var respect = !quest && config.RespectRequestedCount;
var strict = !quest && config.EnableStrictTotalMatches;
// 50 件クランプは削除
```

### 3.6 設定 UI

`config.html` / `config.js` に Browse セクションの項目を追加（en/ja 翻訳付き）。Quest モード ON 時は関連項目をグレーアウト。

---

## 4. 変更ファイル一覧

### 新規

| ファイル | 内容 |
|----------|------|
| `Configuration/ChildCountMode.cs` | childCount 計算モード enum |
| `ContentDirectory/BrowsePagingResolver.cs` | ページング解決 |
| `ContentDirectory/BrowsePagingContext.cs` | ページングコンテキスト |
| `ContentDirectory/ChildCountResolution.cs` | childCount 解決ロジック |
| `ContentDirectory/ChildCountCache.cs` | childCount キャッシュ |
| `ContentDirectory/IBrowseResponseCache.cs` | Browse キャッシュ IF |
| `ContentDirectory/BrowseResponseCache.cs` | Browse キャッシュ実装 |
| `ContentDirectory/BrowseCacheModels.cs` | キャッシュキー・エントリ |
| `ContentDirectory/BrowseConfigFingerprint.cs` | 設定フィンガープリント |
| `ContentDirectory/LibraryChangeNotifier.cs` | ライブラリ変更監視 |
| `tests/.../BrowsePagingResolverTests.cs` | ページングテスト |
| `tests/.../ChildCountResolutionTests.cs` | childCount テスト |
| `tests/.../BrowseResponseCacheTests.cs` | キャッシュテスト |

### 変更

| ファイル | 内容 |
|----------|------|
| `Configuration/DlnaPluginConfiguration.cs` | 新設定プロパティ |
| `Configuration/config.html`, `config.js` | UI・翻訳 |
| `ContentDirectory/ControlHandler.cs` | 中核ロジック |
| `Didl/DidlBuilder.cs` | childCount 省略対応 |
| `ContentDirectory/ContentDirectoryService.cs` | DI・UpdateID |
| `DlnaServiceRegistrator.cs` | サービス登録 |

---

## 5. 期待される効果

| 操作 | 改善前 | 改善後 |
|------|--------|--------|
| シリーズ一覧（500件）初回 | 501 回クエリ + 重い DTO | 1 回クエリ + 軽量 DTO |
| アニメライブラリを開く | 各仮想フォルダの中身を先読み | スタブのみ、childCount なし |
| 同じフォルダを再表示 | 毎回フル生成 | キャッシュ即返却 |
| Quest `RequestedCount=10` | 50 件に制限（バグ） | 全件返却 |

---

## 6. テスト結果

```
dotnet test Jellyfin.Plugin.Dlna.sln
成功: 37 件（新規 10 件含む）
```

追加テスト:

- `BrowsePagingResolverTests` — Quest ON で `Limit=null`、Respect OFF で全件
- `ChildCountResolutionTests` — スタブ/Disabled/Accurate の分岐
- `BrowseResponseCacheTests` — ヒット・世代不一致・Invalidate

---

## 7. 運用上の推奨設定（Quest 3）

| 設定 | 推奨値 |
|------|--------|
| Quest 互換モード | ON |
| Respect RequestedCount | OFF（Quest モードで自動） |
| childCount 計算 | 省略（Quest モードで自動） |
| Browse レスポンスキャッシュ | ON |
| シリーズ一覧上限 | 無制限（0） |

---

## 8. フェーズ2以降（未実装）

- 範囲別フォルダ分割（`0001-0500` 等）
- スタジオ / タグ / レーティング仮想フォルダ
- インデックス DB（五十音・スタジオ等の事前索引）
- キャスト索引

---

## 9. 関連ドキュメント

- [dlna-browse-fix-report.md](./dlna-browse-fix-report.md) — ライブラリスコープ・全件返却の修正
- [dlna-browse-extension-report.md](./dlna-browse-extension-report.md) — 仮想フォルダ拡張
- [dlna-browse-kana-classification-report.md](./dlna-browse-kana-classification-report.md) — 五十音分類
