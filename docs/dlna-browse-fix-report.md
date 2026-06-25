# DLNA ライブラリ閲覧不具合 調査・修正報告書

| 項目 | 内容 |
|------|------|
| 対象 | Jellyfin DLNA プラグイン（フォーク: `jellyfin/jellyfin-plugin-dlna` ベース） |
| 環境 | Jellyfin 10.11.11、DLNA クライアント: BubbleUPnP 4.6.4（Android） |
| 主な症状 | TV ライブラリ「番組（Series）」が空／映画一覧が約 15 件で止まる |
| 修正ファイル | `src/Jellyfin.Plugin.Dlna/ContentDirectory/ControlHandler.cs` ほか |
| 報告日 | 2026-06-23 |

---

## 1. エグゼクティブサマリー

BubbleUPnP から Jellyfin DLNA サーバーを閲覧した際、**Anime ライブラリ内の「番組」フォルダだけが常に 0 件**だった。一方で「最近追加されたシリーズ」「最近追加されたエピソード」などは正常に件数が返っていた。

調査の結果、原因は **Jellyfin のライブラリクエリの仕組み（`CollectionFolder` → 物理フォルダ ID への変換）を DLNA プラグイン側が正しく利用していなかったこと**にあった。Jellyfin 本体の `UserViewBuilder.GetTvSeries` と同じクエリ構築方式に合わせることで解消した。

あわせて、映画ライブラリで **1 ページ目以降が表示されない**問題（Jellyfin コアの既知不具合との相互作用）と、**`IsFavorite` フィルタの誤用**も修正した。

---

## 2. 症状とログ上の事実

### 2.1 番組（Series）が空

```
DLNA Browse ObjectID=series_0c41907140d802bb58430fed7e2cd79e
Returned=0 TotalMatches=0
```

同じ Anime ライブラリ（`0c41907140d802bb58430fed7e2cd79e`）の一覧では:

| スタブ | childCount | 取得経路 |
|--------|------------|----------|
| 番組（Series） | **0** | `GetChildrenOfItem` → `GetItemsResult` |
| 最近追加されたシリーズ | **1** | `GetLatest` → `UserViewManager.GetLatestItems` |
| 最近追加されたエピソード | **50** | `GetLatest` |
| 最近リリースされたエピソード | **1050** | `GetRecentlyReleased` |

→ **データはライブラリ内に存在するが、Series 一覧クエリだけが 0 件を返していた。**

### 2.2 端末について

ログの `User-Agent: Android/16 UPnP/1.0 BubbleUPnP/4.6.4` から、実際の閲覧端末は **Android スマートフォン上の BubbleUPnP** である。`No matching device profile found` は Generic Device プロファイルで動作しており、今回の一覧空問題の直接原因ではない。

### 2.3 映画の件数制限（別件・同時期に修正）

DLNA クライアントは `RequestedCount=16` 程度でページングする。Jellyfin コアが `Limit` 指定時に `TotalRecordCount` をページサイズと同値で返す既知問題（[jellyfin#8069](https://github.com/jellyfin/jellyfin/issues/8069)）により、クライアントが 2 ページ目を要求せず **約 15〜16 件で止まって見える**ことがある。

---

## 3. 根本原因

### 3.1 `CollectionFolder` とクエリスコープ（番組が空の主因）

Jellyfin の TV ライブラリ（Anime 等）は DB 上 **`CollectionFolder`** として表現される。これは複数の **物理フォルダ**（`PhysicalFolderIds`、例: `H:\Test\Anime`）を束ねる仮想フォルダである。

`LibraryManager.GetItemsResult` は、親が `CollectionFolder` または `UserView` のとき、内部で `SetTopParentIdsOrAncestors` を呼び **`PhysicalFolderIds` を `TopParentIds` に展開してから検索**する。

```text
DLNA Browse (series stub)
  → GetTvFolders → GetChildrenOfItem(parent=CollectionFolder, itemType=Series)
    → InternalItemsQuery を組み立て
    → LibraryManager.GetItemsResult(query)
      → [条件] query.Recursive && ParentId が設定されている
      → SetTopParentIdsOrAncestors → TopParentIds = PhysicalFolderIds
      → DB 検索 → Series 一覧
```

**誤った中間修正**では `AncestorIds = [CollectionFolder.Id]` を直接設定していた。この場合:

- `GetItemsResult` は `AncestorIds` をそのまま DB 検索に使う
- Series の祖先チェーンは **物理フォルダ ID** 基準であり、**CollectionFolder の ID とは一致しない**
- 結果: **常に 0 件**

一方 `GetLatest`（最近追加されたシリーズ）は `UserViewManager.GetLatestItems` を使い `ParentId` を渡すため、本体側で正しくスコープされ **1 件返せていた**。

### 3.2 `ParentId` 未設定の可能性

`GetItemsResult` 内の `SetTopParentIdsOrAncestors` は **`query.ParentId` が空でない場合のみ** 呼ばれる。`Parent` プロパティだけを設定し `ParentId` が同期されない経路があると、変換がスキップされる。

### 3.3 `IsFavorite` の誤用

`InternalItemsQuery.IsFavorite` は `bool?` 型である。

| 設定値 | 意味 |
|--------|------|
| `null`（未設定） | お気に入りで絞り込まない |
| `true` | お気に入りのみ |
| `false` | **非お気に入りのみ**（「フィルタなし」ではない） |

公式コードは `query.IsFavorite = isFavorite` とし、引数デフォルト `false` のとき **意図せず「非お気に入りのみ」フィルタ**がかかる。[jellyfin-plugin-dlna#200](https://github.com/jellyfin/jellyfin-plugin-dlna/issues/200) でも同種の指摘がある。

### 3.4 DLNA ルートが `UserView` ではなく `CollectionFolder` を直接露出

BubbleUPnP はルート（ObjectID=0）から **UserView ではなく `CollectionFolder`（Anime, VR 等）** を直接辿る。Jellyfin Web UI は `UserView` 経由で `UserViewBuilder` がクエリを組み立てるが、DLNA は **物理ライブラリフォルダ ID** で Browse するため、プラグイン側で同等のスコープ処理が必要になる。

---

## 4. 実施した修正

### 4.1 `ApplyLibraryQueryScope`（クエリスコープの正規化）

Jellyfin 本体 `UserViewBuilder.GetTvSeries` と同様に、**`Parent` + `ParentId` + `Recursive`** を設定するよう変更した。`AncestorIds` への直接代入は **廃止**。

```csharp
private static void ApplyLibraryQueryScope(InternalItemsQuery query, BaseItem parent)
{
    query.Parent = parent;
    query.ParentId = parent.Id;
    query.Recursive = true;
}
```

これにより `GetItemsResult` 内で `CollectionFolder` → `PhysicalFolderIds` への変換が確実に実行される。

### 4.2 `PrepareItemsQuery`（ユーザーコンテキストの適用）

```csharp
private static void PrepareItemsQuery(InternalItemsQuery query)
{
    if (query.User is not null)
    {
        query.SetUser(query.User);
    }
}
```

Jellyfin 本体の各種クエリと同様に `SetUser` を明示呼び出しする。

### 4.3 `GetChildrenOfItem`（ライブラリビュー向け全件取得 + 手動ページング）

`UserView` および `CollectionType` を持つ `CollectionFolder` を `IsDlnaLibraryView` として識別し:

1. `Limit` なしで全件取得（Jellyfin #8069 の影響を受けにくくする）
2. `Skip` / `Take` で DLNA 要求分だけ返却
3. `IsFavorite` は **`true` のときのみ** 設定

### 4.4 `ResolveTotalRecordCount`（TotalMatches 補正）

`Limit` 指定時に `TotalRecordCount == Limit` かつページが満杯の場合、別途カウントクエリで真の総件数を取得し、DLNA レスポンスの `TotalMatches` を補正する（#8069 回避）。

### 4.5 `GetMoviesWithOptionalExtras`（映画 + Extras）

Extras 挿入のため一度全映画を取得してからページングする方式に変更。映画一覧でも同じスコープ・ページング問題を回避。

### 4.6 診断用ログ（Information レベル）

`HandleBrowse` にサマリログを追加:

```
DLNA Browse ObjectID=... Returned=N TotalMatches=M
```

`logging.default.json` で `Jellyfin.Plugin.Dlna: Debug` を有効にすると SOAP 詳細も追える。

---

## 5. 公式（フォーク元）との比較

### 5.1 公式 `GetChildrenOfItem` の実装（master 時点）

```csharp
private QueryResult<ServerItem> GetChildrenOfItem(...)
{
    query.Recursive = true;
    query.Parent = parent;
    query.IsFavorite = isFavorite;  // ← false でも明示設定
    query.IncludeItemTypes = [itemType];

    var result = _libraryManager.GetItemsResult(query);
    return ToResult(query.StartIndex, result);
}
```

出典: [jellyfin-plugin-dlna `ControlHandler.cs`](https://github.com/jellyfin/jellyfin-plugin-dlna/blob/master/src/Jellyfin.Plugin.Dlna/ContentDirectory/ControlHandler.cs)

### 5.2 公式は「完全に間違い」か？

**一概に全てが誤りではない。** 方向性として `Parent` + `Recursive` は Jellyfin 本体と整合している。

ただし以下の点で **不十分またはバグの可能性** がある:

| 観点 | 公式 | 今回のフォーク修正 |
|------|------|-------------------|
| クエリスコープ | `Parent` + `Recursive` のみ | `Parent` + **`ParentId`** + `Recursive` |
| ユーザーコンテキスト | コンストラクタの `User` のみ | **`SetUser` 明示** |
| `IsFavorite` | `false` を明示設定（非お気に入りフィルタ） | **`true` のときのみ** 設定 |
| `CollectionFolder` 直接 Browse | 特別処理なし | **`IsDlnaLibraryView` で全件取得→手動ページング** |
| TotalMatches（#8069） | 補正なし | **`ResolveTotalRecordCount`** |
| 映画 Extras あり | ページ結果に後付け | **全件取得後に Extras 挿入→ページング** |

### 5.3 今回のフォーク独自の誤修正（経緯）

調査途中、UserView 向けに `AncestorIds` を使う中間修正を入れたが、これを `CollectionFolder` にも適用した結果 **番組が空の状態を悪化・維持**した。最終的には **AncestorIds 方式をやめ、本体と同じ Parent 方式に戻した**上で `ParentId` / `SetUser` / `IsFavorite` を追加している。

### 5.4 公式へのフィードバック候補

以下は upstream への PR 候補として整理できる:

1. **`IsFavorite`**: `isFavorite == true` のときだけ `query.IsFavorite = true` を設定（#200 と同根）
2. **`ParentId` の明示設定**: `ApplyLibraryQueryScope` 相当のヘルパー導入
3. **TotalMatches 補正**: jellyfin#8069 ワークアラウンド
4. **`CollectionFolder` 直接露出**: DLNA クライアントが UserView を経由しない場合のテスト追加

関連して公式でも議論・PR がある:

- [jellyfin-plugin-dlna#200](https://github.com/jellyfin/jellyfin-plugin-dlna/issues/200) — 音楽アルバムが DLNA で見えない（CollectionFolder / IsFavorite）
- [jellyfin-plugin-dlna#201](https://github.com/jellyfin/jellyfin-plugin-dlna/pull/201) — 上記の修正 PR（オープン）
- [jellyfin-plugin-dlna#193](https://github.com/jellyfin/jellyfin-plugin-dlna/issues/193) — UserData のない映画が DLNA に出ない

---

## 6. 修正の検証結果

修正後、BubbleUPnP で **Anime → 番組** を開き、ログで以下を確認:

```
DLNA Browse ObjectID=series_0c41907140d802bb58430fed7e2cd79e
Returned=N TotalMatches=N  (N > 0)
```

Anime 一覧のメタデータ上、`series_...` スタブの `childCount` も 0 から増加することを確認済み（ユーザー確認: 「修正されました」）。

---

## 7. 同時期に実施した関連対応（参考）

| 項目 | 内容 |
|------|------|
| プラグイン読み込み | `JellyfinApiVersion` を実行中サーバーに合わせる（`Directory.Build.props` / `dev.ps1`） |
| `targetAbi` | `10.11.0.0`（Jellyfin 10.11.x 互換） |
| `IUserManager` | 10.11.10+ の `GetUsers()` 切り替え |
| デプロイ | Jellyfin 起動中は DLL ロック → `stop-jellyfin` 後に deploy |
| ログ設定 | `logging.default.json` に `"Jellyfin.Plugin.Dlna": "Debug"` |

---

## 8. 今後の推奨事項

1. **upstream への PR**: `IsFavorite` 修正と `ParentId` 明示は小さな差分で貢献しやすい
2. **回帰テスト**: `CollectionFolder` を DLNA ルートから直接 Browse する統合テスト（Series / Movies / Genres）
3. **BubbleUPnP プロファイル**: 任意。`User-Agent` マッチ用 XML を追加すると Generic 以外のプロファイルを使える
4. **jellyfin#8069**: コア側が修正されれば `ResolveTotalRecordCount` のワークアラウンドは簡略化可能

---

## 9. 変更ファイル一覧

| ファイル | 変更概要 |
|----------|----------|
| `src/Jellyfin.Plugin.Dlna/ContentDirectory/ControlHandler.cs` | クエリスコープ、Series/Movies 一覧、TotalMatches、ログ |
| `src/Directory.Build.Props` | API バージョン設定 |
| `scripts/dev.ps1` | API 自動検出、deploy 時ビルド |
| `src/Jellyfin.Plugin.Dlna/UserManagerExtensions.cs` | IUserManager 互換 |
| `build.yaml` | targetAbi / changelog |

---

## 10. 用語集

| 用語 | 説明 |
|------|------|
| `CollectionFolder` | Jellyfin の仮想ライブラリフォルダ。物理パスを `PhysicalFolderIds` で参照 |
| `UserView` | ユーザーごとのライブラリビュー。Web UI が主に使用 |
| `StubType.Series` | DLNA 上の「番組」仮想フォルダ（ID: `series_{libraryId}`） |
| `TopParentIds` | DB 検索でライブラリスコープを表す物理フォルダ ID 配列 |
| `TotalMatches` | DLNA Browse 応答の総ヒット数。クライアントのページング判断に使用 |

---

## 11. 追記: 「最近追加」系スタブの件数制限（2026-06-23）

### 症状
「最近追加されたシリーズ」等で、ライブラリ内の全件が表示されず一部（最大 50 件またはクライアントの `RequestedCount` 分）で止まる。

### 原因
`GetLatest` が `GetLatestItems` 呼び出し時に `Limit = query.Limit ?? 50` とし、取得結果の件数をそのまま `TotalMatches` にしていた。DLNA クライアント（BubbleUPnP は 16 件/ページ）は `TotalMatches` で総件数を判断するため、2 ページ目を要求しない。

### 修正
| メソッド | 変更 |
|----------|------|
| `GetLatest` | `Limit=int.MaxValue` で全件取得 → `Skip`/`Take` で DLNA ページング → `TotalMatches=全件数` |
| `GetRecentlyReleased` | ライブラリビューは全件取得 + 手動ページング（エピソード 1000 件超に対応） |
| `GetMovieContinueWatching` | `Limit ??= 10` を廃止、全件取得 + 手動ページング |
| `GetNextUp` | 全件取得 + 手動ページング |

### 件数制限の監査結果

| 経路 | 状態 | 備考 |
|------|------|------|
| 番組 / 映画一覧 (`GetChildrenOfItem`) | 修正済 | 全件取得 + 手動ページング |
| 映画 + Extras (`GetMoviesWithOptionalExtras`) | 修正済 | 同上 |
| 3D/4K/8K/VR 映画 (`GetFilteredMovies`) | 修正済 | 元から全件フィルタ + ページング |
| 最近追加 * (`GetLatest`) | **今回修正** | シリーズ・エピソード・映画・音楽 |
| 最近リリース (`GetRecentlyReleased`) | **今回修正** | |
| 再生を続ける (`GetMovieContinueWatching`) | **今回修正** | 旧: 最大 10 件ハードコード |
| 次 (`GetNextUp`) | **今回修正** | |
| ジャンル / アーティスト一覧 | 要監視 | `AncestorIds` + `Limit` 直渡し。件数が多い場合は #8069 の影響余地 |
| プレイリスト (`GetMusicPlaylists`) | 要監視 | 同上 |
| コレクション (`GetMovieCollections`) | 要監視 | BoxSet 件数は通常少ない |
| Extras (`GetExtrasItems`) | 問題なし | 手動ページング済 |
| DLNA クライアント `RequestedCount` | 仕様 | 16 件/ページ等はクライアント側。サーバーは `TotalMatches` を正しく返す必要あり |

---

*本報告書は 2026-06-23 時点の調査・修正内容に基づく。*
