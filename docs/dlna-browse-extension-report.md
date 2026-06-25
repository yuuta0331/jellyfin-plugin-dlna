# DLNA ブラウズ拡張・設定追加 実装報告書

| 項目 | 内容 |
|------|------|
| 対象 | Jellyfin DLNA プラグイン（フォーク: [jellyfin/jellyfin-plugin-dlna](https://github.com/jellyfin/jellyfin-plugin-dlna) ベース） |
| 主な変更 | 仮想フォルダ追加、Browse ページング設定、Quest 互換モード、設定 UI 拡張 |
| 修正ファイル | `ControlHandler.cs`、`StubType.cs`、`ServerItem.cs`、`KanaRowHelper.cs`（新規）、`DidlBuilder.cs`、`DlnaPluginConfiguration.cs`、`config.html`、`config.js` |
| 報告日 | 2026-06-23 |
| 関連報告書 | [dlna-browse-fix-report.md](./dlna-browse-fix-report.md)、[dlna-browse-fix-report-recent-genres-paging.md](./dlna-browse-fix-report-recent-genres-paging.md) |

---

## 1. エグゼクティブサマリー

DLNA ライブラリ閲覧に以下を追加した。

1. **新規仮想フォルダ** — 最近リリースされたシリーズ、放送中、五十音（10行）、年別（TV・映画）
2. **Browse 制御設定** — 1 回の最大返却件数、RequestedCount 尊重、TotalMatches 厳密返却、仮想フォルダ件数上限
3. **Quest 互換モード** — Meta Quest（Commedia 等）向けのページング・URL 正規化プリセット

既存の BubbleUPnP 向け「全件返却」はデフォルト維持（`RespectRequestedCount=OFF`）。Quest 等のクライアントでは設定でページングを有効化できる。

---

## 2. 追加した仮想フォルダ

### 2.1 最近リリースされたシリーズ

| 項目 | 内容 |
|------|------|
| StubType | `RecentlyReleasedSeries` |
| 並び順 | ライブラリ内 Episode を `PremiereDate` 降順で走査し、初出の親 Series を順序保持で収集 |
| 設定 | `EnableRecentlyReleasedSeries`（デフォルト ON） |

「最近リリースされたエピソード」と対になる Series 向けフォルダ。子エピソードの最新 `PremiereDate` に基づきシリーズを並べる。

### 2.2 放送中

| 項目 | 内容 |
|------|------|
| StubType | `CurrentlyAiring` |
| フィルタ | `SeriesStatuses = [Continuing]` |
| 設定 | `EnableCurrentlyAiring`（デフォルト ON） |

### 2.3 五十音（TV・映画）

| 項目 | 内容 |
|------|------|
| StubType | `BrowseByKana`（親）→ `BrowseByKanaRow`（あ行〜わ行、10 フォルダ） |
| ObjectID | `kanarow_{libraryId}_{rowIndex}` |
| フィルタ | `SortName` 先頭 1 文字が行に属する Series / Movie |
| 設定 | `EnableBrowseByKana`（デフォルト ON） |

### 2.4 年別（TV・映画）

| 項目 | 内容 |
|------|------|
| StubType | `BrowseByYear`（親）→ `BrowseByYearItem`（年フォルダ） |
| ObjectID | `year_{libraryId}_{yyyy}` |
| フィルタ | `InternalItemsQuery.Years = [year]` |
| 設定 | `EnableBrowseByYear`（デフォルト ON） |

---

## 3. 追加した設定項目

### 3.1 Browse / 互換性設定（新セクション）

| プロパティ | 選択肢 | デフォルト | 説明 |
|-----------|--------|-----------|------|
| `EnableQuestCompatibilityMode` | ON / OFF | OFF | Quest 向けプリセット |
| `MaxBrowseItemsPerResponse` | 10 / 50 / 100 / 300 / 1000 | 1000 | 1 回の Browse 最大件数 |
| `RespectRequestedCount` | ON / OFF | OFF | DLNA `RequestedCount` を尊重 |
| `EnableStrictTotalMatches` | ON | OFF | `TotalMatches` を厳密に返す |
| `MaxRecentlyAddedItems` | 50 / 100 / 300 | 300 | 最近追加系仮想フォルダの上限 |
| `MaxSeriesListItems` | 無制限 / 500 / 1000 | 無制限 | シリーズ一覧の上限 |

### 3.2 シリーズ設定への追加

| プロパティ | デフォルト |
|-----------|-----------|
| `EnableRecentlyReleasedSeries` | ON |
| `EnableCurrentlyAiring` | ON |
| `EnableBrowseByKana` | ON |
| `EnableBrowseByYear` | ON |

### 3.3 Quest 互換モード ON 時の挙動

| 動作 | 内容 |
|------|------|
| ページング | `RespectRequestedCount` を強制 ON |
| TotalMatches | `EnableStrictTotalMatches` を強制 ON |
| 最大件数 | `MaxBrowseItemsPerResponse` を 50 にクランプ |
| メディア URL | `?` 以降のクエリ文字列を除去（Commedia の `&` 非対応対策） |

参考: [jellyfin-plugin-dlna#87](https://github.com/jellyfin/jellyfin-plugin-dlna/issues/87)

### 3.4 既存設定の整合確認

| 確認項目 | 結果 |
|---------|------|
| `DlnaPluginConfiguration` ↔ `config.html` ↔ `config.js` | 全項目対応済み |
| `loadConfiguration` / `save` の読み書き | 全項目対応済み |
| 日英翻訳 (`config.js`) | 新規項目含め定義済み |
| `defaultAliveInterval` | C# デフォルト 180 に合わせて JS を修正 |

---

## 4. 実装アーキテクチャ

```text
HandleBrowse
  → ResolveBrowsePaging (設定に基づく Limit / StartIndex)
  → GetUserItems
      → GetTvFolders / GetMovieFolders
          → 新 StubType 分岐
          → ApplyRecentlyAddedLimit / ApplySeriesListLimit
  → ApplyBrowsePaging (Skip / Take、TotalMatches 補正)
  → DidlBuilder.WriteFolderElement (ServerItem 対応)
```

### 4.1 新規・変更クラス

| ファイル | 役割 |
|---------|------|
| `KanaRowHelper.cs` | 五十音 10 行の文字定義・マッチング |
| `ServerItem.cs` | `KanaRowIndex`、`ProductionYear` プロパティ追加 |
| `StubType.cs` | `RecentlyReleasedSeries`、`CurrentlyAiring`、`BrowseByKana`、`BrowseByKanaRow`、`BrowseByYear`、`BrowseByYearItem` |
| `DidlBuilder.cs` | 表示名、ObjectID パーサ、`GetClientId(ServerItem)`、Quest URL 正規化 |

---

## 5. 既存「最近追加 / リリース」仕様の照合結果

| 仕様 | 現行実装 | 判定 |
|------|---------|------|
| 最近追加されたエピソード → `DateCreated` が新しい Episode | `GetRecentlyAdded(Episode)` + `ItemSortBy.DateCreated` | **一致** |
| 最近追加されたシリーズ → 子エピソード最新 `DateCreated` で並べる | `ItemSortBy.DateLastContentAdded` | **概ね一致**（Jellyfin の子追加日フィールド。厳密な `max(DateCreated)` とは DB 更新タイミングで差が出る可能性あり） |
| 最近追加された映画 → `DateCreated` が新しい Movie | `GetRecentlyAdded(Movie)` + `DateCreated` | **一致** |
| 最近更新された作品 → 子・画像・メタデータ更新含む | 該当フォルダなし | **未対応** |
| 最近リリースされたエピソード → `PremiereDate` が新しい Episode | `GetRecentlyReleased(Episode)` | **一致** |
| 最近リリースされた映画 → `PremiereDate` が新しい Movie | `GetRecentlyReleased(Movie)` | **一致** |
| 最近リリースされたシリーズ → 子エピソード最新 `PremiereDate` | `GetRecentlyReleasedSeries`（今回追加） | **新規実装** |

---

## 6. 推奨設定

| クライアント | 推奨設定 |
|-------------|---------|
| BubbleUPnP | `RespectRequestedCount=OFF`（全件返却）、Quest モード OFF |
| Meta Quest / Commedia | `EnableQuestCompatibilityMode=ON` |
| 大規模ライブラリ | `MaxRecentlyAddedItems` / `MaxSeriesListItems` で応答サイズを調整 |

---

## 7. テスト項目（手動確認用）

- [ ] TV ライブラリに「最近リリースされたシリーズ」「放送中」「五十音」「年別」が表示される
- [ ] 映画ライブラリに「五十音」「年別」が表示される
- [ ] 五十音あ行〜わ行で期待どおりの作品がフィルタされる
- [ ] 年別フォルダで制作年ごとの作品が表示される
- [ ] DLNA 設定画面に Browse / 互換性セクションが表示・保存できる
- [ ] BubbleUPnP: `RespectRequestedCount=OFF` で従来どおり全件表示
- [ ] Quest: `EnableQuestCompatibilityMode=ON` で一覧・再生が動作する

---

## 8. 既知の制限

- 五十音フィルタは `SortName` 先頭文字ベース。漢字のみのタイトルは Jellyfin の `SortName`（かな併記）に依存する。
- `X_BrowseByLetter`（DLNA 拡張 API）は未実装のまま。五十音は仮想フォルダ方式で提供。
- 「最近更新された作品」（メタデータ・画像更新含む）フォルダは未実装。
