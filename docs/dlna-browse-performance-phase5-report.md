# DLNA Browse パフォーマンス改善（フェーズ5）実装報告書

| 項目 | 内容 |
|------|------|
| 対象 | Jellyfin DLNA プラグイン（フォーク） |
| 前提 | [フェーズ4報告書](dlna-browse-performance-phase4-report.md)（シリーズ/映画/シーズン/エピソード一覧のインデックス化まで完了） |
| 目的 | 4層キャッシュ設計の残件（層2・層3）とフェーズ4「後続候補」をすべて実装する |
| 修正範囲 | `Indexing/*`, `ContentDirectory/*`, `Didl/*`, 設定 UI, プリウォーム, 単体テスト |
| 報告日 | 2026-06-23 |

---

## 1. エグゼクティブサマリー

フェーズ4までで **VirtualIndex（層1）** と **BrowseResponseCache（層4）** は完成していたが、`IndexHit=True` でも Jellyfin DTO バッチ取得（`QueryMs`）が残っていた。フェーズ5では次を実装した。

| 層 / 機能 | 内容 |
|-----------|------|
| **層2** `item_summary` Browse 利用 | インデックス ID 解決後、SQLite summary が全件揃えば `ServerItem(summary)` で DIDL 生成。`SummaryHit=True` で DTO 省略 |
| **層3** `BrowseNodeCache` | L4 miss 時に子ノード軽量リストを再利用し DIDL 再組立。`IndexHit` 時に保存。無効化は L4 と同一経路 |
| **音楽ジャンル** | `FacetType.MusicGenre` + `GetMusicGenres` / `GetMusicGenreItems` インデックス統合 |
| **出演者別** | `FacetType.Person` + `BrowseByPerson` / `PersonItem` 仮想フォルダ |
| **最近メタデータ更新** | `RecentlyModified*` 仮想リスト（`DateModified` 降順）。既存「最近更新シリーズ」（子 EP の `DateCreated`）と区別 |
| **階層プリウォーム** | `PrewarmHierarchyFolders`（デフォルト OFF）+ シリーズ/シーズン件数上限 |

---

## 2. 層2: item_summary Browse 利用

### スキーマ拡張（`ItemSummaryRecord` / `item_summary`）

- `IndexNumber`, `IsFolder`, `DateModifiedTicks` を追加
- `IVirtualIndexStore.GetItemSummaries(libraryId, itemIds)` でバッチ読取

### Browse 統合

- `IndexBrowseHelper.LoadBrowsableItems` — summary 全件ヒット時 `BrowsableQueryResult(SummaryHit=true)`
- `DidlBuilder.WriteSummaryElement` — title / upnp:class / parentID / dc:date の最小 DIDL
- `BrowseTimingScope` — `SummaryHit`, `SummaryMs` をログ出力

### 成功指標（ログ例）

```
ObjectId=... IndexHit=True SummaryHit=True QueryMs=... SummaryMs=... DtoMs=0 ...
```

`DtoMs` が 0 に近いほど層2が有効。

---

## 3. 層3: BrowseNodeCache

| 項目 | 内容 |
|------|------|
| キー | `BrowseCacheKey`（L4 と同一） |
| 値 | `BrowseNodeRecord` リスト（ClientId, Title, UpnpClass, IsFolder, ChildCount, ParentId） |
| フロー | L4 miss → L3 hit で XML 再組立 / L3 miss → 通常 Browse → `IndexHit` 時に L3 保存 |
| 設定 | `EnableBrowseNodeCache`（デフォルト ON）, `BrowseNodeCacheTtlSeconds` |
| 無効化 | `ContentInvalidationService` が L3 もクリア |

---

## 4. 新規インデックス / 仮想フォルダ

| 機能 | インデックス | Browse |
|------|-------------|--------|
| 音楽ジャンル | `FacetType.MusicGenre`（MusicAlbum のジャンル名） | `GetMusicGenres` → `TryGetMusicGenreFolders` |
| 出演者別 | `FacetType.Person`（Actor のみ） | `BrowseByPerson` → `PersonItem` |
| 最近メタデータ更新 | `VirtualListType.RecentlyModified*`（`DateModified` 降順） | TV/映画ライブラリに専用スタブ |

ObjectID 例: `person_{libraryId:N}_{personName}`

---

## 5. 階層プリウォーム

| 設定 | デフォルト | 説明 |
|------|-----------|------|
| `PrewarmHierarchyFolders` | OFF | 物理シリーズ/シーズンをプリウォーム |
| `PrewarmHierarchyMaxSeries` | 20 | シリーズ数上限 |
| `PrewarmHierarchyMaxSeasonsPerSeries` | 3 | シリーズあたりシーズン上限 |

`BrowsePrewarmPaths` は音楽ジャンル・出演者・最近メタデータ更新フォルダも対象に追加。

---

## 6. 設定 UI / fingerprint

`config.html` / `config.js` に層2・層3・音楽ジャンル・出演者・最近メタデータ更新・階層プリウォームの各フラグを追加。  
`BrowseConfigFingerprint` にフェーズ5フラグをすべて含め、設定変更時にキャッシュを無効化。

---

## 7. テスト

| テスト | 内容 |
|--------|------|
| `BrowseNodeCacheTests` | Get/Set, `InvalidateLibrary` |
| `FacetObjectIdParserTests` | `PersonItem` ObjectID 往復 |
| 回帰 | 既存 68+ 件（フェーズ5追加分含む） |

---

## 8. 検証手順

1. プラグイン設定で仮想インデックス ON → インデックス再構築
2. TV ライブラリのシリーズ一覧を Browse — `IndexHit=True SummaryHit=True` を確認
3. 同じフォルダを再 Browse — L4 または L3 で `CacheHit=True`
4. 「出演者別」「最近メタデータ更新」フォルダの表示と子一覧
5. 音楽ライブラリのジャンル一覧（インデックス ON 時 `IndexHit=True`）

---

## 9. スコープ外（設計上維持）

| 項目 | 理由 |
|------|------|
| お気に入り / 続きから / 次に見る | ユーザー別クエリのため従来パス維持 |
| 最近メタデータ更新の DB フォールバック | `ItemSortBy.DateModified` 非対応のため `DateCreated` 降順（インデックス優先） |

---

## 10. 関連ドキュメント

- [フェーズ2報告書](dlna-browse-performance-phase2-report.md)（4層設計）
- [フェーズ4報告書](dlna-browse-performance-phase4-report.md)
- [README.ja.md](../README.ja.md)
