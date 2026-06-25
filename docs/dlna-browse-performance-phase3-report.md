# DLNA Browse パフォーマンス改善（フェーズ3）実装報告書

| 項目 | 内容 |
|------|------|
| 対象 | Jellyfin DLNA プラグイン（フォーク） |
| 前提 | [フェーズ2報告書](dlna-browse-performance-phase2-report.md)（仮想インデックス・プリウォーム追補まで完了） |
| 目的 | ジャンル・年別・最近リリースの初回 Browse で残っていた全件 DB 走査をインデックス参照に置き換える |
| 修正範囲 | `Indexing/*`, `ControlHandler`, 設定 UI, プリウォーム, 単体テスト |
| 報告日 | 2026-06-23 |

---

## 1. エグゼクティブサマリー

フェーズ2までで五十音・最近追加・スタジオ等はインデックス化済みだったが、**ジャンル一覧・年別・最近リリース**は依然としてライブラリ全件走査だった。フェーズ3では既存の `facet_index` / `virtual_list` を拡張し、これらのフォルダもインデックス優先に切り替えた。あわせて Browse XML プリウォーム対象を拡張した。

---

## 2. 実装内容

### 2.1 スキーマ拡張

| 種別 | 追加 |
|------|------|
| `FacetType` | `Genre`, `Year` |
| `VirtualListType` | `RecentlyReleasedEpisodes`, `RecentlyReleasedMovies`, `RecentlyReleasedSeries` |

### 2.2 インデックス構築（`DlnaIndexBuilder`）

| 対象 | 構築方法 |
|------|----------|
| ジャンル | `BuildStringFacet` → `FacetType.Genre` |
| 年別 | `BuildYearFacet`（`ProductionYear` を key に） |
| 最近リリース EP/映画 | `PremiereDate ?? DateCreated` でソートした `virtual_list` |
| 最近リリースシリーズ | 全 Episode をプレミア日降順で走査し Series を一意化（従来 `GetRecentlyReleasedSeries` と同ロジック） |

### 2.3 Browse 統合（`ControlHandler` + `IndexBrowseHelper`）

| メソッド | 変更 |
|----------|------|
| `GetGenres` | `TryGetGenreFolders`（facet keys + `GetGenre`） |
| `GetGenreItems` | ライブラリスコープ時 `TryGetFacetItems(Genre)` |
| `GetBrowseByYearFolders` | `TryGetYearFolders` |
| `GetBrowseByYearItems` | `TryGetFacetItems(Year)` |
| `GetRecentlyReleased` / `GetRecentlyReleasedSeries` | `TryGetVirtualList` 優先 |

インデックス未構築時は従来パスへフォールバック。

### 2.4 新規設定

| プロパティ | デフォルト | 説明 |
|-----------|-----------|------|
| `EnableIndexGenre` | `true` | ジャンル facet |
| `EnableIndexYear` | `true` | 年別 facet |
| `EnableIndexRecentlyReleasedEpisodes` | `true` | 最近リリース EP |
| `EnableIndexRecentlyReleasedMovies` | `true` | 最近リリース映画 |
| `EnableIndexRecentlyReleasedSeries` | `true` | 最近リリースシリーズ |
| `PrewarmFacetItemFolders` | `false` | スタジオ/タグ/レーティング子フォルダのプリウォーム |

### 2.5 プリウォーム拡張（`BrowsePrewarmPaths`）

常時対象（設定 ON 時）:

- ジャンル親 + 各 `genre_{libraryId}_{genreId}`
- 年別親 + 各 `year_{libraryId}_{year}`
- 最近リリース EP/シリーズ/映画

オプション（`PrewarmFacetItemFolders`）:

- `studio_` / `tag_` / `rating_` 各子フォルダ

TV/映画ライブラリ種別に応じて最近リリース・シリーズ関連パスを出し分け。

---

## 3. テスト

| テスト | 内容 |
|--------|------|
| `VirtualIndexStoreFacetTests` | Genre/Year facet、RecentlyReleased virtual list の往復 |
| `IndexBrowseHelperFacetTests` | `IsFacetIndexEnabled`、`TryGetYearFolders` |
| 既存 42 件 | 回帰 |

**合計 47 件合格**

---

## 4. 運用手順

1. ビルド・デプロイ後 Jellyfin を再起動
2. **Rebuild DLNA Quest Index** を実行（または起動時ウォームアップ）
3. ログで以下を確認:
   - `[DLNA Browse] ... StubType=Genres IndexHit=True`
   - `[DLNA Browse] ObjectId=genre_... IndexHit=True`
   - `[DLNA Browse] ... BrowseByYear IndexHit=True`
4. （任意）`PrewarmBrowseResponses=ON` で `DLNA browse prewarm completed Responses=N` の増加を確認

---

## 5. スコープ外（後続候補）

| 項目 | 状態 |
|------|------|
| `item_summary` の Browse 時利用（層2完成） | 未実装 |
| BrowseNodeCache（層3） | 未実装 |
| PersonIndex（キャスト） | 後回し |
| 音楽ジャンルインデックス | 未実装 |
| 続きから / お気に入り | ユーザー別のため従来パス維持 |

| シリーズ一覧 / 映画一覧 / シーズン・エピソード一覧 | フェーズ4で対応 — [フェーズ4報告書](dlna-browse-performance-phase4-report.md) |

---

## 6. 関連ドキュメント

- [フェーズ1報告書](dlna-browse-performance-phase1-report.md)
- [フェーズ2報告書](dlna-browse-performance-phase2-report.md)
- [フェーズ4報告書](dlna-browse-performance-phase4-report.md)
- [README.ja.md](../README.ja.md)
