# DLNA Browse ポスター/サムネイル表示改善 実装報告書

| 項目 | 内容 |
|------|------|
| 対象 | Jellyfin DLNA プラグイン（フォーク） |
| 前提 | [フェーズ5報告書](dlna-browse-performance-phase5-report.md)（層2 `item_summary` Browse・層3 `BrowseNodeCache` まで完了） |
| 目的 | 仮想フォルダ内の映画・番組等でポスター/サムネイルが表示されない問題を解消し、Jellyfin Web UI と同様の画像種別ルールを適用する |
| 修正範囲 | `Didl/*`, `Indexing/*`, `ContentDirectory/*`, 設定 UI, 単体テスト |
| 報告日 | 2026-06-24 |

---

## 1. エグゼクティブサマリー

フェーズ5の **層2（`item_summary` Browse）** と **層3（`BrowseNodeCache`）** はパフォーマンス向上に成功した一方、DIDL 出力が title / upnp:class 中心の最小 XML のままで **`upnp:albumArtURI` が欠落**していた。そのため DLNA クライアント（Quest 3 / BubbleUPnP 等）で映画・番組一覧に画像が表示されなかった。

本対応では次を実装した。

| 項目 | 内容 |
|------|------|
| **画像解決の共通化** | `DlnaImageResolver` でコンテキスト別に Poster / Thumbnail を選択 |
| **インデックス拡張** | `item_summary` に Primary / Thumb の画像タグ・サイズを保存 |
| **層2/層3 への画像出力** | `WriteSummaryElement` / `WriteBrowseNodeElement` で `albumArtURI` / `icon` を出力 |
| **Browse context 伝播** | 親 `StubType` から `DlnaImageBrowseContext` を決定し `ControlHandler` 経由で `DidlBuilder` に渡す |
| **ユーザー設定** | 仮想リスト・検索結果それぞれでポスター/サムネイルを切替可能（デフォルト: ポスター） |

---

## 2. 問題の根本原因

```text
Browse リクエスト
  → GetUserItems（仮想インデックス + EnableItemSummaryBrowse=true）
    → ItemSummaryRecord（画像フィールドなし）
      → WriteSummaryElement（albumArtURI なし）  ← 画像欠落

EnableBrowseNodeCache=true 時
  → BrowseNodeRecord（title/class のみ）
    → WriteBrowseNodeElement（albumArtURI なし）  ← キャッシュ経路でも欠落
```

フル `BaseItem` 経路では `DidlBuilder.AddCover` が動作していたが、画像種別は常に `Primary → Thumb` 固定で、エピソード一覧等の Web UI 相当の使い分けがなかった。

---

## 3. 画像表示ルール

| コンテキスト | デフォルト | 設定で切替 |
|---|---|---|
| 映画・番組仮想リスト（Movies / Series / 最近追加 等） | ポスター (Primary) | サムネイル (Thumb) |
| DLNA Search | ポスター (Primary) | サムネイル (Thumb) |
| エピソード一覧（最近追加 EP / 続きから視聴 / NextUp 等） | サムネイル (Thumb) | 固定 |
| シーズン一覧（シリーズ直下） | ポスター (Primary) | 固定 |
| 音楽アルバム / アーティスト | ポスター (Primary) | 固定 |
| 個別楽曲 | アルバムアート → Thumb | 固定 |

仮想フォルダ自体（「映画」「番組」スタブ）のアイコンは **現状維持**（ライブラリ `CollectionFolder` に画像があればそれを使用）。

設定は **DIDL 書き出し時に適用**されるため、ポスター/サムネイルの切替にインデックス再構築は不要。ただし画像タグ自体はインデックス構築時に収集するため、**初回デプロイ後はインデックス再構築が必要**（後述）。

---

## 4. 実装詳細

### 4.1 `DlnaImageResolver`

新規ファイル: `src/Jellyfin.Plugin.Dlna/Didl/DlnaImageResolver.cs`

- `DlnaImagePresentation` — `Poster` / `Thumbnail`
- `DlnaImageBrowseContext` — `VirtualList`, `Search`, `EpisodeList`, `SeasonList`, `MusicList`, `Default`
- `DlnaImageBrowseContextMapper.FromParent(StubType?, BaseItem?)` — 親フォルダからコンテキストを決定
- `Resolve(BaseItem, ...)` / `Resolve(ItemSummaryRecord, ...)` — 書き出し用 `DlnaResolvedImage` を返す
- `PopulateSummaryImages(...)` — インデックス構築時に Primary / Thumb 両方を `ItemSummaryRecord` へ保存

エピソードで自身に画像がない場合は、インデックス構築時に親シリーズの Primary / Thumb も記録する。

### 4.2 インデックス拡張（`item_summary`）

`ItemSummaryRecord` に追加したフィールド:

- `PrimaryImageItemId`, `PrimaryImageTag`, `PrimaryWidth`, `PrimaryHeight`
- `ThumbImageItemId`, `ThumbImageTag`, `ThumbWidth`, `ThumbHeight`

`VirtualIndexStore` は `EnsureColumn` で既存 DB にカラムを追加。`DlnaIndexBuilder` は `IImageProcessor` を注入し、`ToSummary` 後に `PopulateSummaryImages` を呼ぶ。

### 4.3 DIDL 出力

`DidlBuilder` の変更:

- `AddCover` — `DlnaImageResolver` に委譲し `imageContext` を受け取る
- `AddSummaryCover` — 層2 summary 経路で `albumArtURI` / `icon` / `res` 画像を出力
- `BrowseNodeRecord` — `AlbumArtUri`, `IconUri` を追加
- `CreateBrowseNodeRecord` / `WriteBrowseNodeElement` — 層3 キャッシュ XML にも画像要素を含める

`ControlHandler.HandleBrowse` は親 `serverItem.StubType` から `imageContext` を算出し、子要素の書き出し・ノードキャッシュ生成に渡す。`HandleSearch` は `DlnaImageBrowseContext.Search` を使用。

### 4.4 プラグイン設定

`DlnaPluginConfiguration` に追加:

| 設定 | デフォルト | 説明 |
|------|------------|------|
| `VirtualListImagePresentation` | `Poster` | 映画・番組等の仮想リスト一覧の画像スタイル |
| `SearchImagePresentation` | `Poster` | DLNA 検索結果（映画・番組）の画像スタイル |

設定 UI（Browse / 互換性セクション）と `BrowseConfigFingerprint` に反映済み。

---

## 5. 変更ファイル一覧

| ファイル | 変更内容 |
|---------|----------|
| `Didl/DlnaImageResolver.cs` | 新規: 画像種別ルール |
| `Didl/DlnaImageBrowseContext.cs` | 新規: Browse context と Stub マッピング |
| `Didl/DlnaResolvedImage.cs` | 新規: 解決済み画像メタデータ |
| `Didl/DidlBuilder.cs` | Resolver 統合、Summary/Node 画像出力 |
| `Configuration/DlnaImagePresentation.cs` | 新規: 設定用 enum |
| `Configuration/DlnaPluginConfiguration.cs` | 2 設定追加 |
| `Configuration/config.html` / `config.js` | UI・翻訳 |
| `Indexing/ItemSummaryRecord.cs` | 画像フィールド追加 |
| `Indexing/DlnaIndexBuilder.cs` | 画像メタデータ収集 |
| `Indexing/DlnaVirtualIndexService.cs` | `IImageProcessor` 注入 |
| `Indexing/VirtualIndexStore.cs` | DB スキーマ・読み書き |
| `ContentDirectory/ControlHandler.cs` | Browse context 伝播 |
| `ContentDirectory/BrowseNodeModels.cs` | キャッシュノードに画像 URL |
| `ContentDirectory/BrowseConfigFingerprint.cs` | フィンガープリント |
| `tests/.../DlnaImageResolverTests.cs` | 単体テスト |

---

## 6. デプロイ・運用手順

1. プラグインをビルド・デプロイする
2. DLNA 設定画面（Browse / 互換性）で画像スタイルを確認する（既定: ポスター）
3. **仮想インデックスを再構築する**（設定画面の「Rebuild index」またはライブラリスキャン）
   - 既存 `item_summary` 行には画像タグがないため、再構築前は一時的に画像なしとなる
4. 必要に応じて Browse キャッシュをクリアする（設定変更時はフィンガープリント更新で自動無効化）

### 推奨設定

| 設定 | 推奨 |
|------|------|
| 仮想リストの画像スタイル | ポスター（Web UI と同様） |
| 検索結果の画像スタイル | ポスター |
| item_summary でインデックス Browse（層2） | ON（画像は summary 経路でも出力される） |
| Browse 子ノードキャッシュ（層3） | ON（キャッシュ XML にも画像を含む） |

---

## 7. テスト

`tests/Jellyfin.Plugin.Dlna.Tests/DlnaImageResolverTests.cs` を追加。

- 映画 VirtualList + Poster → Primary
- 映画 VirtualList + Thumbnail → Thumb（なければ Primary フォールバック）
- エピソード EpisodeList → Thumb 優先
- シーズン SeasonList → Primary
- 検索 Poster / Thumbnail 切替
- StubType → BrowseContext マッピング

```powershell
dotnet test Jellyfin.Plugin.Dlna.sln -c Release
```

---

## 8. 既知の制限・リスク

| 項目 | 内容 |
|------|------|
| 画像 URL 認証 | `/Items/.../Images/...` に API トークンは付与しない（既存仕様）。認証必須環境ではクライアントから取得失敗する可能性あり |
| `EnableAlbumArtInDidl=false` | デバイスプロファイルで `res` サムネイルは抑制されるが、`albumArtURI` は出力される（既存動作） |
| インデックス未再構築 | 新カラム追加後、再構築前は summary に画像タグが空のため画像なし |

---

## 9. まとめ

層2/層3 の高速化経路でも DLNA クライアントにポスター/サムネイルが届くようになり、Jellyfin Web UI に近い画像種別ルールとユーザー設定（仮想リスト・検索）を提供できる。パフォーマンス最適化（DTO 省略・ノードキャッシュ）と画像表示は両立する。
