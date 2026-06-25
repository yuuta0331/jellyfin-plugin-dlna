# DLNA 閲覧不具合 調査・修正報告書（第2弾）

| 項目 | 内容 |
|------|------|
| 対象 | Jellyfin DLNA プラグイン（フォーク: [jellyfin/jellyfin-plugin-dlna](https://github.com/jellyfin/jellyfin-plugin-dlna) ベース） |
| 環境 | Jellyfin 10.11.11、DLNA クライアント: BubbleUPnP 4.6.4（Android） |
| 主な症状 | 最近追加されたシリーズが一部のみ／ジャンル内に他ライブラリの作品が混在／件数が 16 件前後で止まる |
| 修正ファイル | `ControlHandler.cs`、`DidlBuilder.cs`、`ServerItem.cs` |
| 報告日 | 2026-06-23 |
| 関連報告書 | [dlna-browse-fix-report.md](./dlna-browse-fix-report.md)（番組一覧が空・映画 15 件問題） |

---

## 1. エグゼクティブサマリー

[第1弾報告書](./dlna-browse-fix-report.md)で **番組（Series）一覧が空**、**映画が約 15 件で止まる** 問題を修正した後、BubbleUPnP で次の 3 点が残っていた。

1. **最近追加されたシリーズ** — ライブラリ内の全シリーズが表示されない
2. **ジャンル** — Anime ライブラリのジャンルを開いても、映画ライブラリ等の作品が混ざる
3. **ページング** — DLNA クライアントが 2 ページ目を要求せず、一覧が途中で止まる

本修正では、**DLNA クライアントはページングを信頼できない** という前提に切り替え、Browse 応答は常に全件返却とした。あわせて「最近追加」取得を Jellyfin 本体の `UserViewBuilder` に揃え、ジャンルはライブラリ単位でスコープする ObjectID 設計を導入した。

---

## 2. 症状

### 2.1 最近追加されたシリーズが一部のみ

| 観察 | 内容 |
|------|------|
| 期待 | Anime ライブラリの「最近追加されたシリーズ」に、該当ライブラリ内の全シリーズ（または Web UI と同等の件数） |
| 実際 | 数件〜数十件で止まる。`RequestedCount=16` 前後の件数と一致することがある |
| ログ例 | `DLNA Browse ObjectID=recentlyaddedseries_{libraryId} Returned=16 TotalMatches=16`（総件数より少ない） |

第1弾後の中間修正では `GetLatestItems` を `Limit=int.MaxValue` で呼び、サーバー側で `Skip`/`Take` による手動ページングを行っていたが、**BubbleUPnP が 2 ページ目を要求しない** ため、依然として先頭 16 件程度しか表示されなかった。

### 2.2 ジャンルがライブラリ横断になる

| 観察 | 内容 |
|------|------|
| 期待 | Anime → ジャンル → 「アクション」で、Anime ライブラリ内のシリーズのみ |
| 実際 | 映画ライブラリの作品も同一ジャンル名で混在 |
| 原因の手がかり | ジャンルフォルダを開いたときのクエリに **親ライブラリのスコープがない** |

### 2.3 ページング全般

BubbleUPnP は Browse 時に `RequestedCount=16` 等を送るが、**`TotalMatches` を見て次ページを取りに行かない**、または 2 ページ目の要求が不安定な挙動が確認されている。サーバーが 16 件だけ返す設計では、どの一覧でも同様に途中で止まる。

---

## 3. 根本原因

### 3.1 最近追加シリーズ — `GetLatestItems` と DLNA ページングの組み合わせ

#### 公式・第1弾中間修正の経路

```text
GetTvFolders (RecentlyAddedSeries)
  → GetLatest
    → UserViewManager.GetLatestItems(Limit = query.Limit ?? 50)
    → DLNA の RequestedCount を Limit に渡す
    → 最大 16〜50 件を返却
```

公式 `GetLatest`（master 時点）:

```csharp
limit = query.Limit ?? 50;
var items = _userViewManager.GetLatestItems(
    new LatestItemsQuery { Limit = limit, ParentId = parent.Id, ... });
return ToResult(query.StartIndex, items);
```

問題点は二層ある。

| 層 | 内容 |
|----|------|
| API 上限 | `GetLatestItems` は内部ロジック・`GroupItems` 等があり、`CollectionFolder` を `ParentId` に渡した場合の件数が Web UI と一致しないことがある |
| DLNA ページング | `RequestedCount` を `Limit` にそのまま渡すと、応答の `NumberReturned` が 16 等になり、クライアントがそれ以上取得しない |

第1弾中間修正（`Limit=int.MaxValue` + `Skip`/`Take`）では `TotalMatches` は全件数になっても、**返却アイテム自体が `Take(16)` で切られていた**ため、症状は解消しなかった。

#### 本体（Web UI）の経路

Jellyfin Web UI の TV ジャンル一覧は `UserViewBuilder.GetTvGenres` が **ライブラリ内の Series を再帰取得し、ジャンル名を抽出**する。最近追加シリーズの並びは `DateLastContentAdded` 等を使う一覧クエリに近い。

DLNA プラグインは `GetLatestItems` に依存しており、**本体と異なる API・ソート・件数制限**を使っていた。

### 3.2 ジャンル — ライブラリコンテキストの欠落

#### ジャンル一覧（`GetGenres`）

公式実装:

```csharp
query.AncestorIds = [parent.Id];
var genresResult = _libraryManager.GetGenres(query);
```

`GetGenres` API は `AncestorIds` を解釈するが、Jellyfin 本体の `UserViewBuilder.GetTvGenres` は次の方式である。

```csharp
parent.QueryRecursive(new InternalItemsQuery {
    IncludeItemTypes = [BaseItemKind.Series],
    Recursive = true
}).Items
    .SelectMany(i => i.Genres)
    .DistinctNames()
    .Select(name => _libraryManager.GetGenre(name));
```

`CollectionFolder` に対する `AncestorIds` 指定は、第1弾で判明した通り **Series 一覧と同様に 0 件・不正スコープになりうる**。仮にジャンル名一覧が取れても、次の問題が残る。

#### ジャンル内の作品（`GetGenreItems`）

公式実装:

```csharp
var query = new InternalItemsQuery(user) {
    Recursive = true,
    GenreIds = [item.Id],
    IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Series],
    ...
};
```

**親ライブラリ（`Parent` / `TopParentIds`）が未設定**のため、サーバー内の **全 Movie + 全 Series** から該当ジャンル ID で検索される。これが「Anime のジャンルなのに映画が出る」直接原因である。

また DLNA の ObjectID はジャンルエンティティの GUID のみ（`GetClientId(genre.Id)`）であり、**どのライブラリから辿ってきたか**が Browse 時に失われる。

### 3.3 ページング — DLNA クライアントの制約

UPnP ContentDirectory の Browse は `StartingIndex` / `RequestedCount` でページング可能だが、実機の DLNA レンダラー・コントローラは次のような制約がある。

| 事象 | 影響 |
|------|------|
| `RequestedCount=16` で 16 件だけ返す | クライアントが満足して終了 |
| `TotalMatches` が正しくても 2 ページ目を要求しない | 手動ページング設計が無意味 |
| Jellyfin #8069（Limit 時の TotalRecordCount 誤り） | ページングを試みるクライアントでも早期終了 |

**結論:** 本環境（BubbleUPnP）では、サーバー側ページングを前提にした設計は成立しない。**常に全件を 1 回の Browse で返す**のが正しい対処である。

---

## 4. 実施した修正

### 4.1 Browse の全件返却（ページング廃止）

`HandleBrowse` は `RequestedCount` / `StartingIndex` を **ログ出力用に読むのみ**とし、`GetUserItems` には渡さない。

```csharp
// DLNA clients (e.g. BubbleUPnP) do not page Browse results reliably — always return the full set.
int? requestedCount = null;
int? start = null;
// ... ログ用にクライアント送信値は parse するが、クエリには使わない

var childrenResult = GetUserItems(serverItem, _user, sortCriteria);
```

全取得経路から `Skip` / `Take`、`InternalItemsQuery.Limit` / `StartIndex` の設定を削除した。

| 対象メソッド | 変更 |
|--------------|------|
| `GetChildrenOfItem` | 全件 `GetItemsResult`、ページングなし |
| `GetRecentlyAdded`（新設、後述） | 同上 |
| `GetRecentlyReleased` | 同上 |
| `GetMovieContinueWatching` / `GetNextUp` | 同上 |
| `GetMoviesWithOptionalExtras` / `GetFilteredMovies` | 同上 |
| `GetFolders` / スタブ一覧（TV・映画・音楽） | `GetTrimmedServerItemsArray` 廃止 |
| `GetExtrasItems` | 同上 |

`ResolveTotalRecordCount`（#8069 ワークアラウンド）は、Limit を使わないため **削除**した。

### 4.2 最近追加 — `GetLatest` を `GetRecentlyAdded` に置換

`UserViewManager.GetLatestItems` をやめ、本体のライブラリ一覧と同じ **`GetItemsResult` + `ApplyLibraryQueryScope`** に統一した。

```csharp
private QueryResult<ServerItem> GetRecentlyAdded(BaseItem parent, InternalItemsQuery query, BaseItemKind itemType)
{
    var sortField = itemType == BaseItemKind.Series
        ? ItemSortBy.DateLastContentAdded   // 新エピソード追加を反映
        : ItemSortBy.DateCreated;

    var listQuery = new InternalItemsQuery(query.User) {
        OrderBy = [(sortField, Descending), (SortName, Ascending)],
        IncludeItemTypes = [itemType],
        ...
    };
    ApplyLibraryQueryScope(listQuery, parent);
    PrepareItemsQuery(listQuery);

    var allItems = _libraryManager.GetItemsResult(listQuery);
    return ToResult(null, totalCount, allItems.Items);  // 全件返却
}
```

適用スタブ:

| スタブ | itemType | ソート |
|--------|----------|--------|
| `RecentlyAddedSeries` | `Series` | `DateLastContentAdded` |
| `RecentlyAddedEpisodes` / `Latest`（TV） | `Episode` | `DateCreated` |
| `RecentlyAddedMovies` / `Latest`（映画） | `Movie` | `DateCreated` |
| `Latest`（音楽） | `Audio` | `DateCreated` |

### 4.3 ジャンル — ライブラリスコープの導入

#### ジャンル一覧: 本体 `UserViewBuilder` 方式

```csharp
private QueryResult<ServerItem> GetGenres(BaseItem parent, InternalItemsQuery query, BaseItemKind itemType)
{
    // TV: Series / 映画: Movie をライブラリ内から再帰取得
    ApplyLibraryQueryScope(listQuery, parent);
    var genreNames = _libraryManager.GetItemsResult(listQuery).Items
        .SelectMany(i => i.Genres)
        .DistinctNames();

    var serverItems = genreNames
        .Select(name => _libraryManager.GetGenre(name))
        .Select(g => new ServerItem(g, null, parent.Id))  // ライブラリ ID を保持
        .ToArray();
}
```

`GetMusicGenres` も同様に、ライブラリ内の `Audio` / `MusicAlbum` から抽出する方式に変更した。

#### ジャンル内作品: `ApplyLibraryQueryScope` + 種別限定

```csharp
if (libraryScopeId is Guid libraryId) {
    ApplyLibraryQueryScope(query, parent);
    query.IncludeItemTypes = parent.CollectionType == movies
        ? [Movie] : [Series];
}
```

#### ObjectID にライブラリ ID を埋め込み

DLNA 上のジャンルフォルダ ID を **ライブラリスコープ付き**に変更した。

| 種別 | ObjectID 形式 | 例 |
|------|---------------|-----|
| TV/映画ジャンル | `genre_{libraryId}_{genreId}` | `genre_0c41907..._a1b2c3...` |
| 音楽ジャンル | `musicgenre_{libraryId}_{genreId}` | 同上 |

変更ファイル:

| ファイル | 役割 |
|----------|------|
| `DidlBuilder.GetScopedClientId` | ライブラリコンテキスト付きで ID 生成 |
| `ControlHandler.ParseItemId` | 上記 ID をパースし `ServerItem.LibraryScopeId` に格納 |
| `ServerItem.LibraryScopeId` | Browse 時に `GetGenreItems` へ伝播 |
| `GetUserItems(ServerItem)` | ジャンル case で `libraryScopeId` を利用 |

旧形式（ジャンル GUID のみ）の ObjectID を開いた場合は、従来どおり全ライブラリ横断のフォールバック動作となる。ブックマーク済み端末では **一度ライブラリからジャンルを開き直す**と新 ID が使われる。

---

## 5. 公式（フォーク元）との比較

### 5.1 公式は「完全に間違い」か

**方向性が一部正しくても、DLNA 実機（BubbleUPnP）と Jellyfin 本体の挙動を満たすには不十分な箇所が複数ある。** 第1弾の `IsFavorite` / `ParentId` 問題に加え、本件は次の独立した欠陥である。

### 5.2 機能別比較表

| 機能 | 公式（master） | 本フォーク（第2弾後） | 評価 |
|------|----------------|----------------------|------|
| Browse ページング | `RequestedCount` を `Limit` に渡す | **常に全件返却** | 公式は DLNA 実機向けに不適切 |
| 最近追加 | `GetLatestItems`、`Limit ?? 50` | **`GetItemsResult` + 日付ソート、無制限** | 公式は件数上限あり |
| 最近追加シリーズのソート | `GetLatestItems` 内部ロジック | **`DateLastContentAdded`** | 本体の「最近追加」に近い |
| ジャンル一覧 | `GetGenres` API + `AncestorIds` | **ライブラリ内アイテムから抽出**（`UserViewBuilder` 同等） | 公式はスコープが不安定 |
| ジャンル内作品 | `GenreIds` のみ、Movie+Series 全庫 | **`Parent` スコープ + 種別限定** | **公式は明確なバグ** |
| ジャンル ObjectID | ジャンル GUID のみ | **`genre_{lib}_{genre}`** | DLNA ではライブラリ文脈の保持が必要 |
| 番組一覧等（第1弾） | `IsFavorite=false` 等 | 第1弾報告書の修正を継承 | 別紙参照 |

### 5.3 公式コードの問題箇所（抜粋）

**最近追加（50 件上限）:**

```csharp
// 公式 GetLatest
limit = query.Limit ?? 50;
_userViewManager.GetLatestItems(new LatestItemsQuery { Limit = limit, ... });
```

**ジャンル内が全ライブラリ横断:**

```csharp
// 公式 GetGenreItems
query.Recursive = true;
query.GenreIds = [item.Id];
query.IncludeItemTypes = [Movie, Series];
// Parent / TopParentIds なし → サーバー全体を検索
```

**ジャンル一覧（本体と異なる API）:**

```csharp
// 公式 GetGenres
query.AncestorIds = [parent.Id];
_libraryManager.GetGenres(query);
// UserViewBuilder は QueryRecursive + DistinctNames 方式
```

### 5.4 本フォークの設計判断

| 判断 | 理由 |
|------|------|
| ページングを「ないもの」として扱う | BubbleUPnP が 2 ページ目を取りに来ない。`TotalMatches` 補正だけでは不十分 |
| `GetLatestItems` を使わない | 上限・`GroupItems`・DLNA 向けでないソート。`GetItemsResult` の方が予測可能 |
| ObjectID にライブラリを埋め込む | DLNA は HTTP のようにセッション状態を持たない。再 Browse 時に文脈を復元する必要がある |
| 第1弾の `ApplyLibraryQueryScope` を継承 | ジャンル・最近追加でも `CollectionFolder` → `PhysicalFolderIds` 変換が必須 |

### 5.5 upstream へのフィードバック候補

第1弾に加え、以下は PR 候補として整理できる。

1. **`GetGenreItems` に親ライブラリスコープを追加**（バグ修正として最優先）
2. **`GetGenres` を `UserViewBuilder` 方式に変更**（TV: Series、映画: Movie を分離）
3. **DLNA 向けオプション: Browse 全件返却**（設定で切替可能にすると upstream も受け入れやすい）
4. **最近追加を `GetLatestItems` から切り離す**検討
5. **ライブラリスコープ付き ObjectID**（破壊的変更のため、メジャーバージョンまたは設定フラグが望ましい）

関連 Issue（第1弾より）:

- [jellyfin-plugin-dlna#200](https://github.com/jellyfin/jellyfin-plugin-dlna/issues/200) — CollectionFolder / `IsFavorite`
- [jellyfin/jellyfin#8069](https://github.com/jellyfin/jellyfin/issues/8069) — `TotalRecordCount`（本修正では Limit 不使用により影響低減）

---

## 6. 修正の検証手順

### 6.1 デプロイ

```powershell
pwsh scripts/dev.ps1 stop-jellyfin
pwsh scripts/dev.ps1 deploy-jellyfin-debug
pwsh scripts/dev.ps1 run-jellyfin
```

### 6.2 確認項目

| # | 操作 | 期待結果 | ログ確認 |
|---|------|----------|----------|
| 1 | Anime → 最近追加されたシリーズ | ライブラリ内の全シリーズ（Web UI と同程度） | `Returned` = `TotalMatches` = 実件数 |
| 2 | Anime → ジャンル → 任意ジャンル | **Anime 内の Series のみ** | ObjectID が `genre_{libId}_{genreId}` |
| 3 | 映画ライブラリ → ジャンル | **映画のみ** | `IncludeItemTypes = Movie` |
| 4 | 映画一覧 | 15 件で止まらない | 全件が 1 回の Browse で返る |
| 5 | 番組一覧（第1弾回帰） | 0 件でない | 第1弾修正の維持確認 |

ログ設定（任意）:

```json
"Jellyfin.Plugin.Dlna": "Debug"
```

Information レベルでも Browse サマリが出る:

```
DLNA Browse ObjectID=recentlyaddedseries_... Returned=N TotalMatches=N
```

---

## 7. 変更ファイル一覧

| ファイル | 変更概要 |
|----------|----------|
| `src/Jellyfin.Plugin.Dlna/ContentDirectory/ControlHandler.cs` | 全件返却、`GetRecentlyAdded`、`GetGenres` 再実装、`GetGenreItems` スコープ、`ParseItemId`、`GetUserItems(ServerItem)` |
| `src/Jellyfin.Plugin.Dlna/Didl/DidlBuilder.cs` | `GetScopedClientId`、ライブラリスコープ付きジャンル ID |
| `src/Jellyfin.Plugin.Dlna/ContentDirectory/ServerItem.cs` | `LibraryScopeId` プロパティ追加 |

第1弾で変更済みの `ApplyLibraryQueryScope` / `PrepareItemsQuery` / `IsDlnaLibraryView` / `IsFavorite` 修正は **そのまま利用**している。

---

## 8. 修正経緯（時系列）

```text
第1弾
  番組が空 → ApplyLibraryQueryScope / IsFavorite 修正
  映画 15 件 → 全件取得 + 手動ページング + ResolveTotalRecordCount

中間（第2弾前）
  最近追加 → GetLatestItems(Limit=int.MaxValue) + Skip/Take
  → TotalMatches は改善も、Returned が 16 のまま → ユーザー報告「直っていない」

第2弾（本報告）
  ページング自体を廃止（常に全件返却）
  GetRecentlyAdded（GetItemsResult + DateLastContentAdded）
  ジャンルのライブラリスコープ + composite ObjectID
```

---

## 9. 用語集

| 用語 | 説明 |
|------|------|
| `RequestedCount` | DLNA Browse でクライアントが要求する最大返却件数（BubbleUPnP は 16 等） |
| `TotalMatches` | DLNA Browse 応答の総ヒット数 |
| `DateLastContentAdded` | シリーズに新コンテンツが追加された日時。最近追加シリーズの並びに使用 |
| `LibraryScopeId` | ジャンル等がどのライブラリから露出されたかを示す GUID |
| `DistinctNames` | Jellyfin 拡張。ジャンル名の重複排除（大文字小文字無視） |

---

## 10. 今後の推奨事項

1. **回帰テスト**: `CollectionFolder` 直下の Browse で、最近追加・ジャンル・一覧の件数をアサートする統合テスト
2. **upstream PR**: `GetGenreItems` のスコープ漏れは単体でもマージ価値が高い
3. **大規模ライブラリ**: 全件返却は 1 万件超で SOAP 応答が巨大化しうる。問題が出たらカテゴリ別の上限（例: 最近追加のみ直近 N 件）を **設定可能**にする余地あり
4. **第1弾報告書との関係**: 本書は第2弾専用。番組が空・`AncestorIds` 誤用・#8069 の詳細は [dlna-browse-fix-report.md](./dlna-browse-fix-report.md) を参照

---

*本報告書は 2026-06-23 時点の第2弾修正（最近追加・ジャンル・ページング廃止）に基づく。*
