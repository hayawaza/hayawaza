# Hayawaza — キーボードショートカット オーバーレイ

Excel / PowerPoint のキーボードショートカットを画面に重ねて表示するツールです。

## ダウンロード・インストール

1. [Releases](https://github.com/hayawaza/hayawaza/releases/latest) を開く
2. **`Hayawaza-stable-Setup.exe`** をダウンロード
3. ダブルクリックで実行（インストーラーが自動でセットアップします）

> SmartScreen の警告が出た場合: 「詳細情報」→「実行」で続行してください。

---

## 使い方

| 操作 | 動作 |
|---|---|
| **右 Shift** を押し続ける | ショートカット一覧を表示 |
| **右 Shift + 左 Shift** | カテゴリを次に切り替え（移動 / 編集 / 書式…） |
| **Ctrl+Alt+.** | カテゴリを次へ |
| **Ctrl+Alt+,** | カテゴリを前へ |
| タスクトレイアイコン ダブルクリック | 設定を開く |

Excel を起動していると Excel のショートカット、PowerPoint を起動していると PowerPoint のショートカットが自動で切り替わります。

---

## 設定

タスクトレイのアイコンをダブルクリックして設定を開きます。

| 設定 | 説明 |
|---|---|
| 表示モード | 常時表示 / ホットキーで呼び出し |
| 呼び出しキー | 右Shift / 右Alt / 右Ctrl など |
| カラーテーマ | ダーク / ホワイト |
| 透過率・サイズ | スライダーで調整 |
| 表示位置 | 4コーナーから選択 |
| ショートカット | カテゴリ・項目ごとに表示/非表示 |
| 自動起動 | Windows 起動時に自動起動（詳細タブ） |

---

## アンインストール

「設定」→「アプリ」→「Hayawaza」→「アンインストール」

---

## 動作環境

- Windows 10 / 11
- .NET 8 Desktop Runtime（インストーラーが自動でインストールします）

---

## よくある質問

**Q. SmartScreen でブロックされる**
A. コード署名証明書なしで配布しているため警告が表示されます。「詳細情報」→「実行」で続行できます。

**Q. ウイルス対策ソフトが反応する**
A. キー入力の検知（`GetAsyncKeyState`）と最前面ウィンドウの監視（`SetWinEventHook`）を行うため、一部のソフトが誤検知することがあります。ソースコードは公開しています。

**Q. スクリーンショットにオーバーレイが映らない**
A. 仕様です。Windows の `SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE)` を使用しているため、スクリーンショットツールには映りません。

**Q. 自動アップデートはいつ行われる？**
A. 起動時にバックグラウンドでアップデートを確認します。次回起動時に新しいバージョンが適用されます。

---

## プロジェクト構成

```
src/ShortcutOverlay/
├── App.xaml / App.xaml.cs         エントリ・トレイ常駐
├── MainWindow.xaml / .cs          オーバーレイ本体
├── Models/
│   ├── AppSettings.cs             設定スキーマ
│   ├── ShortcutEntry.cs           JSON データモデル
│   └── ShortcutViewModel.cs       表示用 ViewModel
├── Services/
│   ├── SettingsService.cs         設定保存/復元
│   ├── ShortcutDataService.cs     ショートカットデータ
│   ├── ForegroundWatcher.cs       前面プロセス監視
│   └── StartupService.cs          スタートアップ登録
├── Views/
│   ├── SettingsWindow.xaml / .cs  設定 GUI
│   └── WelcomeWindow.xaml / .cs   初回起動ウィザード
└── Data/
    └── shortcuts.json             ショートカット定義
```
