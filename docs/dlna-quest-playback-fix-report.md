# Meta Quest 3 DLNA 再生不能 調査・修正報告書

| 項目 | 内容 |
|------|------|
| 対象 | Jellyfin DLNA プラグイン（フォーク: `jellyfin/jellyfin-plugin-dlna` ベース） |
| 環境 | Jellyfin 10.11.x、Meta Quest 3（DLNA クライアント） |
| 主な症状 | 動画が再生されず、サムネイル画像のみ約 10 秒表示される |
| 修正日 | 2026-06-25 |
| 実機確認 | **再生成功**（ユーザー確認済み） |

---

## 1. エグゼクティブサマリー

Meta Quest 3 から DLNA で Jellyfin を閲覧した際、**一覧から作品を選んでも動画が再生されず、ポスター/サムネイル画像だけが短時間表示される**問題が発生していた。

調査の結果、原因は **Browse 高速化（層2/層3）で動画ストリーム URL が DIDL 応答から欠落していたこと** と、**Quest 互換モードによるストリーム URL クエリ除去後のサーバー側未対応** の組み合わせであった。

修正により、インデックス Browse 応答に再生用 `<res>` を付与し、クエリなしの `/dlna/videos/{id}/stream` リクエストをサーバー側で自動解決するようにした。**実機で正常再生を確認済み**。

---

## 2. 症状

| 観察 | 内容 |
|------|------|
| 表示 | サムネイル/ポスター画像のみ表示（動画ではない） |
| 継続時間 | 約 10 秒で停止 |
| 発生条件 | Quest 互換モード ON + 層2/層3 Browse 有効（推奨設定） |
| サーバー側タイムアウト | コード上に「10 秒で打ち切る」再生制限は存在しない |

---

## 3. 根本原因

### 3.1 層2/層3 Browse に動画 `<res>` が無い（最有力）

`EnableItemSummaryBrowse=true`（デフォルト）かつ仮想インデックス使用時、一覧応答は `WriteSummaryItemElement` を経由する。

この経路では **title / upnp:class / albumArtURI のみ** が出力され、**動画/音声の `<res>` URL が含まれなかった**。

Quest 等の DLNA クライアントは `BrowseMetadata` を呼ばず一覧の DIDL だけで再生を開始する場合があり、利用可能な URL が画像（`/Items/.../Images/...`）のみになると **サムネイルを「動画」として誤再生** する。

### 3.2 サマリー経路での画像 `<res>` 誤出力

`AddSummaryCover` が `WriteCoverElements(null, null, ...)` を呼んでいたため、動画項目向けの画像 `<res>` ガード（`item.MediaType == Video`）をすり抜け、**JPEG の `<res>` が動画項目に付与**されていた。

### 3.3 Quest 互換モードの URL クエリ全削除

`NormalizeDlnaMediaUrl` は Quest モード時に `?` 以降をすべて除去する（Commedia 等の `&` 非対応対策）。

除去されるパラメータ: `Static`, `DeviceProfileId`, `VideoCodec`, `api_key`, `dlnaheaders` 等。

`BrowseMetadata` 経路で正しい URL が生成されても、クエリ除去後はサーバーがトランスコード/直接配信を判別できない場合があった。

### 3.4 Meta Quest 専用デバイスプロファイルの欠如

Quest/Oculus/Commedia の UA にマッチするプロファイルがなく、**Generic Device** にフォールバックしていた。

---

## 4. 修正内容

### 4.1 Browse 一覧への再生 URL 付与

| ファイル | 変更 |
|---------|------|
| `Didl/DlnaPlaybackUrlHelper.cs` | 新規。軽量ストリーム URL 生成・DIDL `<res>` 出力 |
| `Didl/DidlBuilder.cs` | `WriteSummaryItemElement` で再生 `<res>` をサムネイルより先に出力 |
| `Didl/DidlBuilder.cs` | `WriteCoverElementsForSummary` で動画/音声の画像 `<res>` 誤出力を防止 |

軽量 URL 例:

```
http://{server}/dlna/videos/{itemId}/stream.mp4
protocolInfo: http-get:*:video/mp4:*
```

Browse 時は `StreamBuilder` を毎件呼ばず、**実際の direct play / リマックス判定はストリーム GET 時**に実行する。

### 4.2 クエリなしストリームのサーバー側自動解決

| ファイル | 変更 |
|---------|------|
| `Playback/StreamingHelpers.cs` | 空クエリの `/dlna/videos/`・`/dlna/audio/` で `StreamBuilder` による自動解決 |
| `Playback/StreamingHelpers.cs` | DLNA ストリームパスで `EnableDlnaHeaders` をデフォルト ON |
| `Playback/Extensions/StreamInfoApplyExtensions.cs` | 新規。`StreamInfo` → リクエスト DTO への反映 |

### 4.3 Meta Quest デバイスプロファイル

| ファイル | 変更 |
|---------|------|
| `Profiles/Xml/Meta Quest.xml` | 新規。UA: Quest / Oculus / Commedia / Horizon / Meta |

### 4.4 設定・キャッシュ

| 項目 | 内容 |
|------|------|
| `EnsurePlaybackUrlsInBrowse` | 新設定（デフォルト ON）。Browse サマリーへの再生 URL 付与を制御 |
| `BrowseConfigFingerprint` | 上記設定を fingerprint に含め、変更時にキャッシュ無効化 |
| 設定 UI | `config.html` / `config.js` にチェックボックスと日英ヘルプを追加 |

### 4.5 回帰テスト

`tests/Jellyfin.Plugin.Dlna.Tests/DidlPlaybackResourceTests.cs` を追加（8 テスト）。

- サマリー DIDL に `video/mp4` の `<res>` が含まれること
- Quest モードでのクエリ除去
- bare URL 判定

---

## 5. 実機確認結果

| 項目 | 結果 |
|------|------|
| Meta Quest 3 再生 | **成功** |
| FFmpeg ログ | `-codec:v:0 copy -codec:a:0 copy`（**再エンコードではなくリマックス**） |
| 同一ファイルで FFmpeg 2 回起動 | クライアントの二重リクエストと判断。`exit code 0` で正常 |

ログ例（リマックスのみ、問題なし）:

```
ffmpeg ... -codec:v:0 copy ... -codec:a:0 copy -y "...\cache\transcodes\xxxxx.mkv"
FFmpeg exited with code 0
```

---

## 6. デプロイ手順

1. プラグインを再ビルド・デプロイ
2. DLNA 設定を確認:
   - **Quest 互換モード**: ON
   - **Browse一覧に再生URLを含める**: ON（デフォルト）
3. ストレージタブで **Browse キャッシュをクリア**
4. **Rebuild DLNA Quest Index** を実行
5. Quest 3 で再生テスト

### ログ確認ポイント

| 確認 | 期待値 |
|------|--------|
| Browse DIDL | `<res protocolInfo="http-get:*:video/` を含む |
| 再生時 GET | `/dlna/videos/{id}/stream`（`/Items/.../Images/` ではない） |
| FFmpeg | `copy` が多ければ再エンコードなし。`libx264` 等ならトランスコード |

---

## 7. 変更ファイル一覧

| ファイル | 概要 |
|---------|------|
| `src/Jellyfin.Plugin.Dlna/Didl/DlnaPlaybackUrlHelper.cs` | 軽量再生 URL ヘルパー |
| `src/Jellyfin.Plugin.Dlna/Didl/DidlBuilder.cs` | サマリー再生 URL・画像ガード |
| `src/Jellyfin.Plugin.Dlna.Playback/StreamingHelpers.cs` | bare URL 自動解決・DLNA ヘッダ |
| `src/Jellyfin.Plugin.Dlna.Playback/Extensions/StreamInfoApplyExtensions.cs` | StreamInfo 適用 |
| `src/Jellyfin.Plugin.Dlna/Profiles/Xml/Meta Quest.xml` | Quest プロファイル |
| `src/Jellyfin.Plugin.Dlna/Configuration/*` | 設定・UI |
| `tests/.../DidlPlaybackResourceTests.cs` | 回帰テスト |
| `README.ja.md` | 推奨設定・トラブルシュート追記 |

---

## 8. 既知の制限

- Browse 一覧の応答サイズがやや増加する（項目あたり 1 本の `<res>` 追加）
- 一部 DLNA クライアントが短時間に二重ストリーム要求すると、FFmpeg ジョブが 2 本起動することがある（再生に支障なし）
- 既存環境に `Meta Quest.xml` が未展開の場合、プラグイン再インストールまたは `data/plugins/profiles/` への配置が必要

---

## 9. 関連ドキュメント

- [Browse 高速化 フェーズ5報告書](dlna-browse-performance-phase5-report.md)
- [Browse 画像表示改善報告書](dlna-browse-image-presentation-report.md)
- [Quest 互換 Browse 拡張報告書](dlna-browse-extension-report.md)
