# DLNA 設定画面 UI 改善 実装報告書

| 項目 | 内容 |
|------|------|
| 対象 | Jellyfin DLNA プラグイン（フォーク） |
| 前提 | [ストレージ管理・デバッグログ制御 報告書](dlna-storage-and-debug-logging-report.md)（ストレージ管理 UI 追加済み） |
| 目的 | 設定項目の整理・日本語化漏れの解消・技術用語の説明追加・ストレージ統計の視認性向上 |
| 修正範囲 | `Configuration/config.html`, `Configuration/config.js` |
| 報告日 | 2026-06-24 |

---

## 1. エグゼクティブサマリー

DLNA プラグイン設定画面は、フェーズ追加に伴い **単一スクロールページに 60 以上の設定項目** が縦に並ぶ状態になっていた。本対応では **5 タブ構成** に再編し、Browse / インデックス / ストレージなどの設定を分類した。

あわせて、日本語 UI で英語ラベルが残る **ローカライズ漏れ**（`inputsToTranslate` 未登録）を修正し、プリウォーム・TotalMatches・キャッシュ TTL など **技術用語に説明文（`*Help`）** を追加した。ストレージタブでは統計を **概要 + カテゴリ別** に表示するよう改善した。

---

## 2. 背景・課題

| 課題 | 内容 |
|------|------|
| 設定の探索困難 | 一般設定・Browse・インデックス・ストレージが 1 ページに混在し、目的の項目を見つけにくい |
| ローカライズ漏れ | `Prewarm max series:` 等、`config.js` 辞書にはあるが `inputsToTranslate` 未接続のため日本語 UI でも英語表示 |
| 用語の難解さ | Browse Node Cache TTL、TotalMatches、プリウォーム等の説明がなく、Quest 向け推奨設定の判断が難しい |
| ストレージ統計の視認性 | `fieldDescription` の縦並びのみで、DB サイズ・キャッシュ・ヒット率の関係が把握しづらい |
| ヘッダータブ未動作 | 初回実装で `import('../components/maintabsmanager')` を使用したが、プラグイン埋め込み JS からはパス解決できずタブが表示されない |

---

## 3. 実装内容

### 3.1 タブ構成

設定画面を **ページ内タブ**（Jellyfin 標準の `emby-tabs` / `emby-tab-button`）で 5 区分に整理した。保存 / キャンセルは全タブ共通でフォーム末尾に固定。

| タブ | 内容 |
|------|------|
| 一般 | Play To、デフォルトユーザー、SSDP / ネットワーク |
| ライブラリ | TV番組、映画、特典映像 |
| Browse | Quest 互換、レスポンスキャッシュ、画像表示、ページングと上限 |
| インデックス | 基本、プリウォーム、インデックス対象、表示フォルダ、ライブラリ更新時、大量フォルダ分割 |
| ストレージ | 使用状況、メンテナンス、デバッグログ |

**タブ実装の注意:** Jellyfin 本体の `mainTabsManager.setTabs()` はプラグイン `config.js` からの dynamic import が失敗するため、**ページ内 `emby-tabs` + クリックハンドラ** で `tabContent` の `is-active` を切り替える方式を採用した。

### 3.2 ローカライズ修正

| 対応 | 詳細 |
|------|------|
| `inputsToTranslate` 追加 | `prewarmHierarchyMaxSeries`, `prewarmHierarchyMaxSeasonsPerSeries`, `browseNodeCacheTtlSeconds`, `kanaTitlePrefixes` |
| セレクトオプション | `browseNodeCacheTtlSeconds` の TTL=0 を「ライブラリ更新まで」に翻訳 |
| タブ名キー | `TabGeneral`, `TabLibrary`, `TabBrowse`, `TabIndex`, `TabStorage`（en/ja） |
| ユーザー選択 | `None` / `なし` を辞書キーに統一 |

### 3.3 説明文（`*Help`）の追加

主要設定に `fieldDescription` + `data-i18n="*Help"` を追加。例:

- **インデックス:** 起動時インデックス作成、フォルダ一覧の事前生成、プリウォーム件数上限
- **Browse:** レスポンスキャッシュ TTL、子フォルダ一覧キャッシュ、1 回の最大返却件数、総件数の正確な返却
- **一般:** Play To、デフォルトユーザー、SSDP 関連
- **ストレージ:** 各メンテナンスボタンの作用

ラベルも平易化（例: 「起動時に検索用インデックスを作成する」「フォルダ内の総件数を正確に返す」）。

### 3.4 ストレージ統計 UI の改善

`renderStorageStats()` を再設計:

1. **概要** — 推定合計使用量（インデックス DB + Browse キャッシュ）、インデックス済みライブラリ数
2. **カテゴリ別** — インデックス DB、Browse キャッシュ、childCount キャッシュ、Browse メトリクス、世代
3. **ラベル改善** — `library_indexed 行数` 等の内部名を「ライブラリ登録件数」等のユーザー向け表記に変更
4. **メンテナンス** — 各ボタン下に 1 行説明を配置

バックエンド API（`GET /Dlna/Storage/Stats`）の変更はなし。

---

## 4. 変更ファイル

| ファイル | 変更内容 |
|---------|---------|
| `src/Jellyfin.Plugin.Dlna/Configuration/config.html` | タブバー追加、5 タブパネル化、サブセクション分割、Help 要素追加 |
| `src/Jellyfin.Plugin.Dlna/Configuration/config.js` | タブ制御、翻訳キー拡充、`renderStorageStats` 改善 |

---

## 5. 動作確認

| 項目 | 結果 |
|------|------|
| ビルド | `dotnet build` 成功 |
| 単体テスト | 97 件すべて合格 |
| タブ表示 | タイトル下に 5 タブ（一般 / ライブラリ / Browse / インデックス / ストレージ）が表示されること |
| タブ切替 | 各パネルのみ表示、保存 / キャンセルは常に表示 |
| 日本語 UI | プリウォーム件数上限等の英語ラベル残存なし |
| ストレージタブ | カテゴリ別統計表示、更新・クリア操作 |

---

## 6. 運用上の注意

- 設定 UI を更新した場合は **プラグイン DLL の再デプロイ** とブラウザの **強制リロード**（Ctrl+Shift+R）が必要
- VSCode Launch 前にビルドが走らないと古い `config.html` / `config.js` が残ることがある（[VSCode タスクガイド](vscode-tasks-guide.ja.md) 参照）

---

## 7. 関連ドキュメント

| ファイル | 内容 |
|---------|------|
| [README.ja.md](../README.ja.md) | 推奨設定・設定画面の案内 |
| [ストレージ管理・デバッグログ制御 報告書](dlna-storage-and-debug-logging-report.md) | ストレージ API・メンテナンス機能の詳細 |
