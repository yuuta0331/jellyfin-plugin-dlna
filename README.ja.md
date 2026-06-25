# Jellyfin DLNA プラグイン（Browse 高速化フォーク）

[Jellyfin 公式 DLNA プラグイン](https://github.com/jellyfin/jellyfin-plugin-dlna) をベースに、**Quest 3 / BubbleUPnP 等の DLNA クライアント向け Browse パフォーマンス改善**を加えたフォークです。

上流の `README.md` は公式プロジェクト用のまま残しています。本リポジトリのビルド・設定・検証手順は **このファイル（`README.ja.md`）** を参照してください。

## フォークの目的

DLNA Browse で「全件表示」を維持したまま、次のボトルネックを解消します。

1. **childCount N+1 クエリ**（フェーズ1）
2. **仮想フォルダの全件 DB 検索・集計**（フェーズ2〜4）
3. **初回 Browse の DIDL XML 生成**（フェーズ2追補: プリウォーム）

## 主な機能

### フェーズ1

- childCount 計算の制御（省略 / 推定 / 正確）
- Quest 互換モード（全件返却、`RequestedCount` 無視）
- 一覧用 `DtoOptions` 軽量化
- **BrowseResponseCache**（DIDL-Lite XML キャッシュ）

### フェーズ2

- **SQLite 仮想インデックス**（`plugins/configurations/dlna/dlna-index.db`）
  - 最近追加 / 最近更新シリーズ、五十音、スタジオ・タグ・レーティング、シリーズ範囲分割
- **item_summary** テーブル（一覧用メタデータ）
- ライブラリ単位の選択的キャッシュ無効化 + デバウンス
- 起動時インデックスウォームアップ / **Rebuild DLNA Quest Index** タスク
- 構造化ログ `[DLNA Browse] ... CacheHit= ... IndexHit= ...`

### フェーズ2追補

- **Browse XML プリウォーム**（`PrewarmBrowseResponses`）
- `InvalidatePattern(objectIdPrefix)` による XML キャッシュの部分破棄
- プラグイン設定保存時の全キャッシュ破棄 + インデックス再構築

### フェーズ3

- **ジャンル・年別・最近リリース**の仮想インデックス化
- Browse 時の `IndexHit=True` 対応拡大
- プリウォーム対象の拡張（ジャンル/年/最近リリース、`PrewarmFacetItemFolders` オプション）

詳細: [フェーズ3報告書](docs/dlna-browse-performance-phase3-report.md)

### フェーズ4

- **シリーズ一覧・映画一覧・シーズン一覧・エピソード一覧**のインデックス化
- `SeriesAll` / `MoviesAll` 仮想リストと親キー facet（`SeasonOfSeries`, `EpisodeOfSeason`）
- 物理シリーズ/シーズンフォルダ Browse の `IndexHit=True` 対応

詳細: [フェーズ4報告書](docs/dlna-browse-performance-phase4-report.md)

### フェーズ5

- **層2** `item_summary` の Browse 読取（`SummaryHit=True` で DTO 省略）
- **層3** `BrowseNodeCache`（子ノードキャッシュ → DIDL 再組立）
- **音楽ジャンル** / **出演者別** / **最近メタデータ更新** 仮想フォルダのインデックス化
- **階層プリウォーム**（シリーズ/シーズン、件数上限付き・デフォルト OFF）

詳細: [フェーズ5報告書](docs/dlna-browse-performance-phase5-report.md)

### Browse キャッシュ信頼性・画像表示の改善

フェーズ5の 4 層キャッシュ運用で見つかった問題（L4 ヒット率低下、画像欠落、サムネイル遅延）を修正しました。

- **キャッシュキー安定化** — `DefaultProfile` 固定 ID、世代番号をキーから除外、`ServerBase` をキーに追加
- **L4 の loopback 汚染防止** — `127.0.0.1` / `localhost` でのキャッシュ書き込みを拒否
- **L3 高速パス復活** — 簡略 XML ではなく `_didlBuilder` でフル品質 DIDL（画像 URL 含む）を再生成
- **プリウォームの LAN URL 化** — SSDP と同様のバインドアドレスで L4 を事前投入
- **メトリクス分離** — L3 / L4 ヒット率をストレージタブで個別表示
- **childCount キャッシュ** — Estimate モードでも Browse 時の件数を書き込み

詳細: [Browse キャッシュ信頼性・画像表示 改善報告書](docs/dlna-browse-cache-reliability-report.md)

**デプロイ後:** ストレージタブから **Browseキャッシュをクリア** し、必要ならインデックス再構築を実行してください（古い localhost エントリの除去）。

### ポスター/サムネイル表示（Browse 画像）

- **層2/層3 経路での画像出力** — `item_summary` Browse・`BrowseNodeCache` でも `upnp:albumArtURI` / `upnp:icon` を出力
- **Jellyfin Web UI 相当の画像ルール** — 映画・番組一覧はポスター、エピソード一覧はサムネイル、シーズンはポスター等
- **インデックスへの画像メタデータ保存** — `item_summary` に Primary / Thumb の画像タグを保持（DTO 省略を維持）
- **設定項目** — 仮想リスト・検索結果それぞれでポスター/サムネイルを切替（デフォルト: ポスター）
- **エピソード一覧の画像ソース** — 最近追加/リリース/お気に入り等のエピソード一覧で、エピソードのサムネイルを優先（なければシリーズ）。シリーズ優先モードにも切替可

詳細: [Browse 画像表示改善 報告書](docs/dlna-browse-image-presentation-report.md)

### 起動時インデックス制御・エピソード一覧サムネイル

- **起動時インデックス ON/OFF** — Index タブの「起動時にインデックスを再構築する」で Jellyfin 起動時の SQLite 再構築を制御（既存 `WarmupIndexOnStartup` の UI 改善）。General タブから案内、仮想インデックス OFF 時はチェック無効化
- **エピソード一覧の画像ソース** — Browse タブで「エピソードのサムネイル」（既定）または「シリーズのサムネイル」を選択。どちらも画像がない場合は相互フォールバック

詳細: [起動時インデックス・エピソードサムネイル 報告書](docs/dlna-index-startup-and-episode-thumbnail-report.md)

### Meta Quest 3 再生修正

層2/層3 Browse 高速化により **動画ストリーム URL が一覧応答から欠落**し、Quest 3 でサムネイルのみ表示される問題を修正しました。

- **Browse 一覧への再生 URL 付与** — `item_summary` / `BrowseNodeCache` 経路でも動画/音声 `<res>` を出力（サムネイルより先）
- **クエリなしストリームの自動解決** — Quest 互換モードでクエリが除去された `/dlna/videos/{id}/stream` をサーバー側で `StreamBuilder` 解決
- **Meta Quest デバイスプロファイル** — Quest / Oculus / Commedia / Horizon の UA マッチ
- **設定 `EnsurePlaybackUrlsInBrowse`** — 再生 URL 付与の ON/OFF（デフォルト ON）
- **回帰テスト** — `DidlPlaybackResourceTests`（132 テスト中 +8）

実機確認: Meta Quest 3 で **再生成功**。FFmpeg ログは `-codec copy`（リマックスのみ、再エンコードなし）。

詳細: [Meta Quest 3 再生修正 報告書](docs/dlna-quest-playback-fix-report.md)

### ライブラリ種別の仮想フォルダ対応

DLNA Browse で仮想フォルダが表示されるライブラリ種別:

| Jellyfin ライブラリ種別 | 対応 |
|---|---|
| 映画 / 番組（TV） / 音楽 | 従来どおり |
| **その他**（TV+映画混在、`CollectionTypeOptions.mixed`） | フラット統合の TV+映画スタブ（続きから見る・番組・映画・ジャンル・五十音等） |
| **ホームビデオと写真** | 最近追加・ビデオ・写真・お気に入り等 |
| **ミュージックビデオ** | 最近追加・全件・アーティスト・ジャンル |

未対応: ブック、フォルダ型のみのライブラリ等（従来どおり物理一覧）。

> **混在ライブラリ（その他）について**  
> Web UI では `mixed` と表示されますが、サーバー内部の `CollectionType` 列挙型に `mixed` はなく、**`null`（まれに `unknown`）** として保存されます。本プラグインはこの値を混在ライブラリとして認識し、インデックス構築・Browse ルーティングの両方で扱います。  
> DLNA クライアントが `folder_<GUID>` 形式でライブラリを開く場合でも、ライブラリ直下は仮想フォルダ一覧にルーティングされます。

混在ライブラリが物理一覧のままの場合、ログに次が出ていないか確認してください。

```
DLNA library browse using physical children (no virtual folder route): <ライブラリ名> CollectionType=... StubType=...
```

正常時はライブラリ直下の `[DLNA Browse]` で `Items=15` 前後（仮想フォルダ数）が返ります。

### 本番運用（ストレージ管理・デバッグログ制御）

- **ストレージ / キャッシュ管理 UI** — インデックス DB 容量・Browse/childCount キャッシュ件数・Browse ヒット率の表示
- **メンテナンス API** — Browse キャッシュ / childCount / インデックスの個別クリア、全クリア、再構築（`DlnaController` `/Dlna/Storage/*`）
- **`EnableDebugLogging`**（デフォルト OFF）— SOAP/XML・SSDP・`StreamBuilder` 詳細ログを本番で抑制
- **二重防御のログ制御** — `DlnaPluginLog` ゲート + `VerboseDependencyLogger`（Jellyfin 本体向け）+ `LoggerFilterOptions`（補助）
- 設定保存後 **再起動なしでログ ON/OFF が即時反映**

詳細: [ストレージ管理・デバッグログ制御 報告書](docs/dlna-storage-and-debug-logging-report.md)

### 設定画面 UI 改善

ダッシュボード → プラグイン → **DLNA** から開く設定画面を、次の **5 タブ** に整理しました。

| タブ | 主な内容 |
|------|----------|
| 一般 | Play To、デフォルトユーザー、SSDP / ネットワーク |
| ライブラリ | TV番組・映画・特典映像の仮想フォルダ |
| Browse | Quest 互換、キャッシュ、画像表示、ページング |
| インデックス | 仮想インデックス、プリウォーム、インデックス対象 |
| ストレージ | 使用状況・メンテナンス・デバッグログ |

- **日本語 UI** — プリウォーム件数上限など、以前英語のまま残っていたラベルを修正
- **説明文** — プリウォーム、TotalMatches、キャッシュ TTL など難しい用語に `fieldDescription` で説明を追加
- **ストレージタブ** — 推定合計使用量・カテゴリ別統計でインデックス DB / キャッシュの状態を確認しやすく改善

詳細: [設定画面 UI 改善 報告書](docs/dlna-settings-ui-improvement-report.md)

## 要件

- Jellyfin **10.11.x**（`targetAbi: 10.11.0.0`）
- .NET **9.0**
- `Directory.Build.props` の `JellyfinApiVersion` を実行中サーバーに合わせること

## ビルド

```powershell
dotnet build Jellyfin.Plugin.Dlna.sln -c Release
```

成果物: `src/Jellyfin.Plugin.Dlna/bin/Release/net9.0/`

## 開発環境（VSCode）

`.vscode/settings.json` に Jellyfin のパスを設定し、**Launch Jellyfin** を実行します。

```
stop-jellyfin → build-jellyfin-debug → deploy-jellyfin-debug → Jellyfin 起動
```

`scripts/dev.ps1` が `data/plugins/DLNA_<version>/` へ DLL をコピーします。

> Launch 前にビルドが走らないと、古い DLL がデプロイされ設定 UI が更新されないことがあります（`tasks.json` でビルドを組み込み済み）。

**VSCode タスクの一覧・リリース手順は [docs/vscode-tasks-guide.ja.md](docs/vscode-tasks-guide.ja.md) を参照してください。**  
配布用 ZIP を作るときはタスク **`release-jellyfin-plugin`** を使います。

## 推奨設定（Quest 3 向け）

| 設定 | 推奨 |
|------|------|
| Quest 互換モード | ON |
| Browse一覧に再生URLを含める | ON（デフォルト。層2/層3 Browse に動画ストリーム URL を付与） |
| childCount 計算 | 省略 または 推定 |
| Browse レスポンスをキャッシュする | ON |
| 仮想フォルダインデックスを有効にする | ON |
| 起動時にインデックスを再構築する | ON（OFF で起動時の再構築をスキップ。初回 Browse または手動再構築まで待つ） |
| インデックス作成後にフォルダ一覧を事前生成する | ON（任意・初回応答をさらに短縮） |
| スタジオ/タグ/レーティング子フォルダもプリウォーム | OFF（任意・ライブラリが大きい場合） |
| ジャンル・年別・最近リリースのインデックス | ON（フェーズ3・デフォルト有効） |
| シリーズ/映画/シーズン/エピソード一覧のインデックス | ON（フェーズ4・デフォルト有効） |
| item_summary でインデックス Browse（層2） | ON（フェーズ5・デフォルト有効） |
| Browse 子ノードキャッシュ（層3） | ON（フェーズ5・デフォルト有効） |
| 仮想リストの画像スタイル | ポスター（Web UI と同様。サムネイルにも切替可） |
| 検索結果の画像スタイル | ポスター（サムネイルにも切替可） |
| エピソード一覧の画像ソース | エピソードのサムネイル（なければシリーズ。シリーズ優先にも切替可） |
| 出演者別 / 最近メタデータ更新フォルダ | ON（任意） |
| シリーズ/シーズンの階層プリウォーム | OFF（件数が多い場合） |
| デバッグログを有効にする | OFF（本番推奨。トラブルシュート時のみ ON） |

**デバッグログの動作**

- 設定を保存すると **再起動なしで即時反映** されます（Browse の SOAP/XML 詳細ログの有無も含む）。
- Jellyfin 本体の `logging.json` で `Jellyfin.Plugin.Dlna: Debug` を有効にしていても、プラグイン設定で OFF のときは **プラグイン由来の Debug/Trace は出力されません**（呼び出し元ゲート + Jellyfin 本体 `StreamBuilder` 等へのロガー抑制）。
- Information 以上（インデックス構築完了、設定変更通知、エラー等）は **常に出力** されます。`[DLNA Browse]` のパフォーマンス行はデバッグログ ON 時のみです。

プラグイン設定画面の **ストレージ** タブから、インデックス DB の容量確認・キャッシュクリア・インデックス再生成が行えます。

**Browse 画像を初めて有効にした場合**は、設定画面から **インデックス再構築**（またはライブラリスキャン）を実行してください。`item_summary` に画像タグが保存され、映画・番組一覧にポスターが表示されます。

**Quest 3 で再生できない場合**は、プラグイン更新後に **ストレージタブで Browse キャッシュをクリア**し、**Rebuild DLNA Quest Index** を実行してください。Quest 互換モードはストリーム URL からクエリを除去しますが、サーバーが `/dlna/videos/{id}/stream` へのリクエストを自動解決します。ログで再生時の GET が `/Items/.../Images/` ではなく `/dlna/videos/` になっていることを確認してください。

FFmpeg ログに `-codec:v:0 copy -codec:a:0 copy` と出ていれば **再エンコードではなくリマックス**です。同一動画で FFmpeg が短時間に 2 回起動することは、DLNA クライアントの二重リクエストによるもので、`exit code 0` なら問題ありません。

## 動作確認

### Quest 3 再生

1. 映画/エピソード一覧から作品を選択して再生
2. Jellyfin ログで次を確認:
   - 再生 GET が `/dlna/videos/{id}/stream` であること
   - FFmpeg に `copy` が含まれること（再エンコードなし）

```
ffmpeg ... -codec:v:0 copy ... -codec:a:0 copy ...
FFmpeg exited with code 0
```

### インデックス

ログに以下が出ればインデックス構築成功です。

```
DLNA index warmup completed
```

手動実行: ダッシュボード → スケジュールされたタスク → **Rebuild DLNA Quest Index**

### Browse 高速化

```
[DLNA Browse] ObjectId=series_<libraryId> ... IndexHit=True SummaryHit=True ...
[DLNA Browse] ObjectId=<series-id> ... IndexHit=True SummaryHit=True ...   # シーズン/エピソード一覧（層2有効時）
```

### XML プリウォーム

```
DLNA browse prewarm completed Libraries=1 Responses=25
[DLNA Browse] ObjectId=recentlyaddedseries_... CacheHit=True TotalMs=...
```

プリウォームは **LAN バインドアドレス**（`GetSmartApiUrl`）で Browse を実行し、クライアント到達可能な URL で L4 キャッシュを温めます。`127.0.0.1` 固定では loopback 拒否により L4 に書き込まれません。

プリウォーム対象: 最近追加/更新/リリース、五十音、ジャンル・年別、シリーズ/映画一覧、スタジオ等の一覧（facet 子は `PrewarmFacetItemFolders` ON 時）。シーズン・エピソードの物理フォルダ、お気に入り・続きから等は対象外です。

### キャッシュヒット（L3 / L4）

ストレージタブで **レスポンスキャッシュヒット率（L4）** と **ノードキャッシュヒット率（L3）** を個別に確認できます。

```
# L4 ヒット（同一フォルダの再アクセス）
[DLNA Browse] ObjectId=... CacheHit=True ...

# L3 ヒット（インデックス問い合わせ省略、フル品質 DIDL 再生成）
[DLNA Browse] ObjectId=... CacheHit=True ... Items=...
```

DLNA クライアントが多くのフォルダを 1 回ずつ開く UI では、絶対ヒット率は低く見えることがあります。再訪問フォルダでの体感速度を確認してください。

## ドキュメント

| ファイル | 内容 |
|---------|------|
| [フェーズ1報告書](docs/dlna-browse-performance-phase1-report.md) | childCount N+1 解消、XML キャッシュ |
| [フェーズ2報告書](docs/dlna-browse-performance-phase2-report.md) | 仮想インデックス、プリウォーム、運用手順 |
| [フェーズ3報告書](docs/dlna-browse-performance-phase3-report.md) | ジャンル・年別・最近リリースのインデックス化 |
| [フェーズ4報告書](docs/dlna-browse-performance-phase4-report.md) | シリーズ/映画/シーズン/エピソード一覧のインデックス化 |
| [フェーズ5報告書](docs/dlna-browse-performance-phase5-report.md) | 層2/層3キャッシュ、音楽ジャンル、出演者、最近メタデータ更新 |
| [Browse キャッシュ信頼性・画像表示 改善報告書](docs/dlna-browse-cache-reliability-report.md) | L3/L4 ヒット率修正、画像欠落解消、プリウォーム LAN URL 化 |
| [Browse 画像表示改善 報告書](docs/dlna-browse-image-presentation-report.md) | ポスター/サムネイル表示、層2/層3 画像出力、設定項目 |
| [起動時インデックス・エピソードサムネイル 報告書](docs/dlna-index-startup-and-episode-thumbnail-report.md) | 起動時インデックス ON/OFF UI、エピソード一覧の画像ソース設定 |
| [ストレージ管理・デバッグログ制御 報告書](docs/dlna-storage-and-debug-logging-report.md) | キャッシュ/DB メンテナンス UI、本番向けログ制御 |
| [設定画面 UI 改善 報告書](docs/dlna-settings-ui-improvement-report.md) | 5 タブ構成、ローカライズ、説明文、ストレージ統計表示 |
| [VSCode タスクガイド](docs/vscode-tasks-guide.ja.md) | 開発・デプロイ・リリース ZIP 手順 |
| [Browse 修正報告（ジャンル等）](docs/dlna-browse-fix-report-recent-genres-paging.md) | ライブラリスコープ・ページング |

## テスト

```powershell
dotnet test Jellyfin.Plugin.Dlna.sln -c Release
```

## 上流との関係

- 公式 `README.md` / CI バッジは変更していません
- 本フォーク独自の変更は主に `ContentDirectory/`, `Indexing/`, 設定 UI, `docs/` に集中
- マージや upstream への PR を行う場合は、Browse 最適化と Quest 向け設定を分離して検討してください

## ライセンス

上流リポジトリと同じライセンス（GPL-2.0）に従います。
