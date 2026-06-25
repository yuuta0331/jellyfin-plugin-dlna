# DLNA Browse パフォーマンス改善（フェーズ4）実装報告書

| 項目 | 内容 |
|------|------|
| 対象 | Jellyfin DLNA プラグイン（フォーク） |
| 前提 | [フェーズ3報告書](dlna-browse-performance-phase3-report.md)（ジャンル・年別・最近リリースのインデックス化まで完了） |
| 目的 | ライブラリ階層の主要一覧（シリーズ・映画・シーズン・エピソード）で残っていた `IndexHit=False` を解消する |
| 修正範囲 | `Indexing/*`, `ControlHandler`, 設定 UI, プリウォーム, 単体テスト |
| 報告日 | 2026-06-23 |

---

## 1. エグゼクティブサマリー

フェーズ3までで仮想フォルダ（ジャンル・年別・最近リリース等）はインデックス化済みだったが、**TV/映画ライブラリの本体一覧**は依然として Jellyfin DB の全件検索または `folder.GetItems` に依存していた。ログ上では `StubType=Series` や物理シリーズ/シーズンフォルダの Browse で `IndexHit=False` が確認されていた。

フェーズ4では次の4経路をインデックス優先に切り替えた。

| 一覧 | 従来 | フェーズ4 |
|------|------|-----------|
| シリーズ一覧 | `GetChildrenOfItem`（ライブラリ全件 DB 検索） | `VirtualListType.SeriesAll` |
| 映画一覧 | `GetMoviesWithOptionalExtras`（全件 DB 検索） | `VirtualListType.MoviesAll` |
| シーズン一覧 | `Series` フォルダの `folder.GetItems` | `FacetType.SeasonOfSeries` |
| エピソード一覧 | `Season` フォルダの `folder.GetItems` | `FacetType.EpisodeOfSeason` |

Extras スタブの挿入（映画・シリーズ・シーズン）は従来どおり維持する。

---

## 2. 実装内容

### 2.1 スキーマ拡張

| 種別 | 追加 |
|------|------|
| `VirtualListType` | `MoviesAll` |
| `FacetType` | `SeasonOfSeries`, `EpisodeOfSeason` |

`SeriesAll` はフェーズ2で構築済みだったが、Browse では未使用だった。フェーズ4でシリーズ一覧 Browse に接続した。

### 2.2 インデックス構築（`DlnaIndexBuilder`）

| 対象 | 構築方法 | 設定フラグ |
|------|----------|-----------|
| シリーズ全件 | `SortName` 昇順 → `SeriesAll` | `EnableIndexSeriesList` |
| 映画全件 | `SortName` 昇順 → `MoviesAll` | `EnableIndexMoviesList` |
| シーズン | `Season` を `ParentId`（シリーズ）でグループ化、`IndexNumber` 昇順 | `EnableIndexSeasonList` |
| エピソード | `Episode` を `ParentId`（シーズン）でグループ化、`IndexNumber` 昇順 | `EnableIndexEpisodeList` |

シーズン構築時のみ `BaseItemKind.Season` を追加クエリする。エピソードはフェーズ2以来の `episodeItems` を再利用する。

### 2.3 Browse 統合（`ControlHandler` + `IndexBrowseHelper`）

| 経路 | 変更 |
|------|------|
| `StubType.Series` | `GetChildrenOfItem` → `TryGetVirtualList(SeriesAll)` 優先 |
| `StubType.Movies` | `GetMoviesWithOptionalExtras` → `TryGetVirtualList(MoviesAll)` 優先 |
| 物理 `Series` フォルダ | `TryGetParentChildren(SeasonOfSeries)` 優先 |
| 物理 `Season` フォルダ | `TryGetParentChildren(EpisodeOfSeason)` 優先 |

新規ヘルパー:

- `TryGetIndexedLibraryChildren` — ライブラリビュー配下の Series/Movie 仮想リスト
- `TryGetIndexedSeriesOrSeasonChildren` — 親 ID キーの facet 参照
- `AppendSeriesSeasonExtrasStub` — Extras スタブ挿入の共通化

`IndexBrowseHelper.TryGetParentChildren` は `TryGetExtras` と同様に `ResolveLibraryId` でライブラリ ID を解決する。

### 2.4 新規設定

| プロパティ | デフォルト | 説明 |
|-----------|-----------|------|
| `EnableIndexSeriesList` | `true` | シリーズ一覧（`SeriesAll`） |
| `EnableIndexMoviesList` | `true` | 映画一覧（`MoviesAll`） |
| `EnableIndexSeasonList` | `true` | シリーズ配下のシーズン一覧 |
| `EnableIndexEpisodeList` | `true` | シーズン配下のエピソード一覧 |

`BrowseConfigFingerprint` に4フラグを追加。設定変更時に XML キャッシュとインデックスが再構築される。

### 2.5 プリウォーム拡張（`BrowsePrewarmPaths`）

- 映画ライブラリ: `StubType.Movies` をプリウォーム対象に追加（`EnableIndexMoviesList` ON 時）
- TV ライブラリ: 既存の `StubType.Series` / シリーズ範囲プリウォームは `SeriesAll` に依存（`EnableIndexSeriesList` が OFF の場合は範囲分割フォルダも生成されない）

シーズン・エピソードの物理フォルダは件数が膨大になりうるため、プリウォーム対象には含めていない。

---

## 3. テスト

| テスト | 内容 |
|--------|------|
| `IndexBrowseHelperFacetTests` | `IsFacetIndexEnabled`（Season/Episode）、`IsVirtualListBrowseEnabled`（Series/Movies） |
| `VirtualIndexStoreFacetTests` | Genre/Year facet（フェーズ3） |
| 既存テスト | 回帰 |

**合計 49 件合格**

---

## 4. 運用手順

1. ビルド・デプロイ後 Jellyfin を再起動
2. **Rebuild DLNA Quest Index** を実行（または起動時ウォームアップ）
3. ログで以下を確認:

```
[DLNA Browse] ObjectId=series_<libraryId> StubType=Series ... IndexHit=True
[DLNA Browse] ObjectId=movies_<libraryId> StubType=Movies ... IndexHit=True
[DLNA Browse] ObjectId=<series-guid> StubType=Folder ... IndexHit=True
[DLNA Browse] ObjectId=<season-guid> StubType=Folder ... IndexHit=True
```

4. `IndexHit=False` のままの場合:
   - `EnableVirtualFolderIndex` が ON か
   - 該当の `EnableIndex*List` フラグが ON か
   - インデックス構築ログ `DLNA index built for library ...` が出ているか

を確認する。

---

## 5. スコープ外（後続候補）

フェーズ5で [層2・層3・音楽ジャンル・出演者・最近メタデータ更新・階層プリウォーム](dlna-browse-performance-phase5-report.md) を実装済み。

| 項目 | 状態 |
|------|------|
| 続きから / お気に入り | ユーザー別のため従来パス維持 |

---

## 6. 関連ドキュメント

- [フェーズ1報告書](dlna-browse-performance-phase1-report.md)
- [フェーズ2報告書](dlna-browse-performance-phase2-report.md)
- [フェーズ3報告書](dlna-browse-performance-phase3-report.md)
- [README.ja.md](../README.ja.md)
