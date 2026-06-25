# 起動時インデックス制御・エピソード一覧サムネイル設定 実装報告書

| 項目 | 内容 |
|------|------|
| 対象 | Jellyfin DLNA プラグイン（フォーク） |
| 前提 | [設定画面 UI 改善 報告書](dlna-settings-ui-improvement-report.md)、[Browse 画像表示改善 報告書](dlna-browse-image-presentation-report.md) |
| 目的 | 起動時インデックス再構築の ON/OFF を分かりやすくし、エピソード系仮想フォルダのサムネイル表示をエピソード/シリーズで切替可能にする |
| 修正範囲 | `Configuration/*`, `Didl/DlnaImageResolver.cs`, `ContentDirectory/BrowseConfigFingerprint.cs`, 単体テスト |
| 報告日 | 2026-06-24 |

---

## 1. エグゼクティブサマリー

ユーザー要望に応じ、次の 2 点を実装した。

| 項目 | 内容 |
|------|------|
| **起動時インデックス ON/OFF** | 既存の `WarmupIndexOnStartup` を Index タブで分かりやすく表示。General タブから案内、仮想インデックス OFF 時はチェックボックスを無効化 |
| **エピソード一覧の画像ソース** | 新設定 `EpisodeListImageSource` を追加。エピソードのサムネイルを優先し、ない場合はシリーズのサムネイルへ自動フォールバック。シリーズ優先モードも選択可能 |

バックエンドの起動時処理は **新規追加せず**、既存の `DlnaIndexWarmupService` が `WarmupIndexOnStartup` を参照する構成のままである。

---

## 2. 起動時インデックス再構築

### 2.1 既存の仕組み

```text
Jellyfin 起動
  → DlnaIndexWarmupService.StartAsync()
    → WarmupIndexOnStartup == false → 何もしない
    → WarmupIndexOnStartup == true  → RebuildAllAsync() + PrewarmAsync()
```

設定プロパティ: `DlnaPluginConfiguration.WarmupIndexOnStartup`（デフォルト `true`）

### 2.2 UI 改善

| 変更 | 詳細 |
|------|------|
| ラベル明確化 | 「起動時に検索用インデックスを作成する」→「起動時にインデックスを再構築する」 |
| Help 文 | OFF 時は起動時再構築をスキップし、初回 Browse または手動再構築まで待つ旨を追記 |
| General タブ案内 | 「起動時のインデックス再構築は Index タブで設定できます」を表示 |
| 依存関係 UI | `EnableVirtualFolderIndex` が OFF のとき `#warmupIndexOnStartup` を無効化（`applyIndexUi`） |

### 2.3 設定場所

ダッシュボード → プラグイン → **DLNA** → **インデックス** タブ → **起動時にインデックスを再構築する**

---

## 3. エピソード一覧のサムネイル設定

### 3.1 対象フォルダ

`DlnaImageBrowseContext.EpisodeList` にマップされる仮想フォルダ:

- 最近追加されたエピソード (`RecentlyAddedEpisodes`)
- 最近リリースされたエピソード (`RecentlyReleasedEpisodes`)
- お気に入りのエピソード (`FavoriteEpisodes`)
- 続きを見る / 次のエピソード / 最近更新されたエピソード 等

### 3.2 新設定 `EpisodeListImageSource`

| 値 | 表示名 | 挙動 |
|---|---|---|
| `Episode`（既定） | エピソードのサムネイル | エピソード自身の Thumb → Primary を優先。なければシリーズの画像を使用 |
| `Series` | シリーズのサムネイル | シリーズの Thumb → Primary を優先。なければエピソードの画像を使用 |

いずれのモードも **画像なしよりは何か表示する** 方針で、相互フォールバックを行う。

### 3.3 画像解決ロジック（`DlnaImageResolver`）

**インデックス経路**（`Resolve(ItemSummaryRecord, ...)`）:

1. エピソード + `EpisodeList` コンテキストのとき、設定に応じて所有者フィルタを適用
2. `Episode` モード: エピソード所有スロット → 任意スロット（シリーズフォールバック）
3. `Series` モード: シリーズ所有スロット → エピソード所有スロット

**ランタイム経路**（`Resolve(BaseItem, ...)`）:

- `Episode` モード: エピソード自身の画像 → 親シリーズへフォールバック（従来どおり）
- `Series` モード: 先にシリーズの画像を解決 → なければエピソード自身

**インデックス構築**（`PopulateSummaryImages`）は変更なし。エピソードに画像がない場合、従来どおりシリーズの Primary / Thumb を `item_summary` に記録する。表示切替は解決時のみで完結するため、**設定変更にインデックス再構築は不要**（Browse キャッシュは fingerprint により自動無効化）。

### 3.4 キャッシュ fingerprint

`BrowseConfigFingerprint` に `EpisodeListImageSource` を追加。層3 `BrowseNodeCache` に保存済みの画像 URI が設定変更後も残らないようにする。

### 3.5 設定場所

ダッシュボード → プラグイン → **DLNA** → **Browse** タブ → **Image Presentation** → **エピソード一覧の画像ソース**

---

## 4. 変更ファイル一覧

| ファイル | 変更内容 |
|---------|----------|
| `Configuration/EpisodeListImageSource.cs` | 新規 enum |
| `Configuration/DlnaPluginConfiguration.cs` | `EpisodeListImageSource` プロパティ追加 |
| `Didl/DlnaImageResolver.cs` | エピソード一覧の画像解決分岐 |
| `ContentDirectory/BrowseConfigFingerprint.cs` | fingerprint 追加 |
| `Configuration/config.html` | UI 追加・ラベル改善 |
| `Configuration/config.js` | i18n、load/save、`applyIndexUi` |
| `tests/.../DlnaImageResolverTests.cs` | Episode/Series モードのテスト追加 |

---

## 5. テスト

`DlnaImageResolverTests` に以下を追加・更新した。

| テスト | 内容 |
|--------|------|
| `ResolveSummary_EpisodeList_EpisodeSource_UsesEpisodeOwnedThumb` | エピソード所有 Thumb を優先 |
| `ResolveSummary_EpisodeList_EpisodeSource_FallsBackToSeriesThumbWhenEpisodeMissing` | エピソード画像なし時にシリーズへフォールバック |
| `ResolveSummary_EpisodeList_SeriesSource_UsesSeriesFallbackThumb` | シリーズ優先モードでシリーズ Thumb |
| `ResolveSummary_EpisodeList_SeriesSource_PrefersSeriesThumbOverEpisodeThumb` | 両方ある場合にシリーズ側を優先 |
| `BrowseContextMapper_MapsStubTypes` | `RecentlyReleasedEpisodes` / `FavoriteEpisodes` を追加 |

```powershell
dotnet test Jellyfin.Plugin.Dlna.sln --filter "FullyQualifiedName~DlnaImageResolverTests"
```

---

## 6. 運用上の注意

| 項目 | 内容 |
|------|------|
| 起動時インデックス OFF | 初回 Browse やライブラリ変更後の再構築まで、インデックスが古い可能性がある。手動再構築はストレージタブから可能 |
| エピソードサムネイル | Jellyfin 側にエピソード個別のサムネイルが未設定でも、既定モードではシリーズ画像が表示される |
| 設定変更後 | Browse キャッシュは自動無効化。体感に差があればストレージタブから Browse キャッシュクリアを実行 |

---

## 7. 関連ドキュメント

- [Browse 画像表示改善 報告書](dlna-browse-image-presentation-report.md) — ポスター/サムネイル表示の基盤
- [設定画面 UI 改善 報告書](dlna-settings-ui-improvement-report.md) — 5 タブ構成
- [フェーズ2報告書](dlna-browse-performance-phase2-report.md) — 起動時ウォームアップの概要
