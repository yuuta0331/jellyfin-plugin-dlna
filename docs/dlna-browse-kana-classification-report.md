# DLNA 五十音（Browse By Kana）分類改善 報告書

| 項目 | 内容 |
|------|------|
| 対象 | Jellyfin DLNA プラグイン（フォーク: jellyfin-plugin-dlna） |
| 機能 | Browse By Kana（五十音フォルダ）によるシリーズ／映画の行別閲覧 |
| 修正ファイル | `KanaTitleClassifier.cs`（新規）、`KanaClassificationOptions.cs`（新規）、`KanaRowHelper.cs`、`ControlHandler.cs`、`DlnaPluginConfiguration.cs`、`config.html`、`config.js`、`Jellyfin.Plugin.Dlna.Tests`（新規） |
| 報告日 | 2026-06-23 |

---

## 1. エグゼクティブサマリー

五十音フォルダの作品振り分けが、タイトル表記のゆれ（全角／半角、カタカナ、濁音、記号、英字混在）や Jellyfin メタデータ（`SortName` がローマ字、`Name` が日本語表示名）の違いにより、意図しない行に分散していた。

本修正では **正規化パイプライン** と **頭文字（先頭1文字）による分類** を導入し、あわせて **英数字**・**その他** の2行を追加した。接頭辞（劇場版・映画・OVA 等）の除外はプラグイン設定で制御可能とした。

---

## 2. 修正前の問題

### 2.1 先頭1文字の単純照合

`KanaRowHelper.MatchesRow` は `SortName` の **先頭1文字** を五十音リストと比較するだけだった。

| 症状 | 例 |
|------|-----|
| 括弧付きタイトルが不一致 | `【推しの子】` |
| カタカナ先頭が不一致 | `ガンダム` |
| 濁音が別行扱い | `ぼっち・ざ・ろっく！` |
| 英字先頭が不一致 | `Re:ゼロ…`、`ONE PIECE` |

### 2.2 ローマ字 SortName による英数字行への一括分類

Jellyfin のアニメメタデータでは `SortName` がローマ字（例: `Sousou no Frieren`）、`Name` が日本語（例: `葬送のフリーレン`）であることが多い。当初の改善で `SortName` を優先した結果、日本語タイトルが **英数字** 行に集まってしまった。

### 2.3 タイトル途中のかなによる分散

漢字をスキップしてタイトル内の最初のかな（`の`、`と`、`ゲ` 等）で分類したため、同種の作品が **な行・た行・か行** などにバラバラに入った。

---

## 3. 修正内容

### 3.1 分類アーキテクチャ

```text
SortName / Name
  → NFKC（全角／半角統一）
  → カタカナ→ひらがな、ヴ/ゔ→う、旧仮名（ゑ/ゐ）正規化
  → 接頭辞除去（設定 ON 時）
  → 先頭の記号・括弧をスキップ
  → 頭文字1つで分類
       かな   → 濁点除去・小書き→通常 → あ〜わ行
       英数字 → 英数字行
       漢字   → その他（途中の助詞は見ない）
```

### 3.2 行構成（10行 → 12行）

| Index | 表示（JA） | 表示（EN） |
|-------|-----------|-----------|
| 0–9 | あ行〜わ行 | A row〜Wa row |
| 10 | 英数字 | Alphanumeric |
| 11 | その他 | Other |

既存の `kanarow_{libraryId}_{0-9}` ID は互換性を維持。10・11 が新規追加。

### 3.3 SortName / Name の優先順位

両方を評価し、次の順で採用する。

1. `SortName` の頭文字がかな → その行（読み仮名付き SortName を優先）
2. `Name` の頭文字がかな → その行（ひらがな／カタカナ始まりの表示名）
3. `SortName` の頭文字が英数字（かつ `Name` が漢字始まりでない）→ 英数字行
4. `Name` の頭文字が英数字 → 英数字行
5. 上記以外 → その他

漢字始まりの日本語タイトルは、ローマ字 `SortName` の頭文字（`S` 等）で英数字行に入れない。

### 3.4 プラグイン設定

| 設定 | 説明 | デフォルト |
|------|------|-----------|
| `EnableKanaPrefixStripping` | 分類前に接頭辞を除去 | ON |
| `KanaTitlePrefixes` | 除去する接頭辞（1行1件） | 劇場版、映画、OVA、TV、特撮 |

設定 UI は「五十音」チェックボックス直下に追加（日英対応）。

### 3.5 単体テスト

`tests/Jellyfin.Plugin.Dlna.Tests` を追加。27 件の xUnit テストで以下を検証。

- 正規化例（ガンダム、Re:ゼロ、ぼっち・ざ・ろっく！ 等）
- ローマ字 SortName + 日本語 Name の組み合わせ
- 頭文字のみで分類（助詞 `の` を使わない）
- 接頭辞除外 ON/OFF

---

## 4. 分類例

| Name | SortName | 頭文字 | 行 |
|------|----------|--------|-----|
| ようこそ実力至上主義の教室へ | Youkoso… | よ | や行 |
| ガンダム | — | が | か行 |
| 葬送のフリーレン | Sousou no Frieren | 葬（漢字） | その他 |
| 鬼滅の刃 | きめつのやいば | き（SortName） | か行 |
| ONE PIECE | ONE PIECE | O | 英数字 |
| Re:ゼロから始める異世界生活 | — | R | 英数字 |

---

## 5. 制限事項と運用上の推奨

- **漢字は読みに自動変換しない。** 漢字から始まるタイトルは、頭文字が漢字のため **その他** に入る（途中の「の」等で振り分けない）。
- 五十音行へ正しく入れたい場合は、Jellyfin の **SortName にかな読み**（例: `そうそうのふりーりん`）を設定する。
- 英字のみの作品（`ONE PIECE` 等）は **英数字** 行が自然な挙動。

---

## 6. 変更ファイル一覧

| ファイル | 内容 |
|---------|------|
| `ContentDirectory/KanaTitleClassifier.cs` | 正規化・頭文字分類ロジック |
| `ContentDirectory/KanaClassificationOptions.cs` | 設定の受け渡し |
| `ContentDirectory/KanaRowHelper.cs` | 12行対応・API 更新 |
| `ContentDirectory/ControlHandler.cs` | `SortName` + `Name` + 設定でフィルタ |
| `Configuration/DlnaPluginConfiguration.cs` | 接頭辞設定 |
| `Configuration/config.html` / `config.js` | 設定 UI |
| `Jellyfin.Plugin.Dlna.csproj` | `InternalsVisibleTo` |
| `tests/Jellyfin.Plugin.Dlna.Tests/` | 単体テスト |
| `Jellyfin.Plugin.Dlna.sln` | テストプロジェクト追加 |

---

## 7. テスト手順

```bash
dotnet test tests/Jellyfin.Plugin.Dlna.Tests/Jellyfin.Plugin.Dlna.Tests.csproj
```

DLNA クライアントでの確認:

1. プラグインを再ビルド・再配置し Jellyfin を再起動
2. シリーズ／映画ライブラリ → **五十音** を開く
3. あ行〜わ行・**英数字**・**その他** の12フォルダが表示されること
4. ひらがな／カタカナ始まりの作品が頭文字の行に入ること
5. 漢字始まりで SortName がローマ字のみの作品が **その他** にまとまること
6. 設定で接頭辞除外の ON/OFF・リスト編集が保存されること

---

## 8. まとめ

五十音 Browse は、単純な先頭文字比較から **正規化付き頭文字分類** に置き換えた。ローマ字 `SortName` と日本語 `Name` の両方を考慮し、タイトル途中の助詞で行がバラける問題を解消した。漢字始まり作品の五十音振り分けには、メタデータ側の SortName（かな読み）設定が引き続き有効である。
