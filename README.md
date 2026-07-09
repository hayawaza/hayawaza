# ShortcutOverlay

Excel / PowerPoint の操作中にキーボードショートカット一覧を半透明オーバーレイで表示する Windows 常駐デスクトップアプリ。

## 特徴

- 最前面アプリが Excel / PowerPoint に切り替わると自動でオーバーレイ表示
- クリックスルー対応（オーバーレイ越しに下のウィンドウを操作可能）
- 常時表示 / ホットキー呼び出し の2モード
- ショートカット個別 ON/OFF、透過率・サイズ・位置の GUI 調整
- 非管理者権限で動作、インストーラー不要 (xcopy 配布)
- ネットワーク通信なし

## 動作環境

| 項目 | 要件 |
|------|------|
| OS | Windows 10 / 11 |
| .NET | .NET 8 以上 (self-contained ビルド時は不要) |
| 権限 | 非管理者で動作 |

## ビルド手順

`reports/2026-07-09-build-guide.md` を参照してください。

## プロジェクト構成

```
src/ShortcutOverlay/
├── ShortcutOverlay.csproj    プロジェクト定義
├── app.manifest              高DPI対応・非管理者宣言
├── App.xaml / App.xaml.cs    エントリ・トレイ常駐
├── MainWindow.xaml / .cs     オーバーレイ本体 (PoC-1/2/3)
├── Models/
│   ├── ShortcutEntry.cs      JSON データモデル
│   ├── ShortcutViewModel.cs  表示用 ViewModel
│   └── AppSettings.cs        設定スキーマ
├── Services/
│   ├── SettingsService.cs    設定保存/復元
│   ├── ForegroundWatcher.cs  前面プロセス監視 (PoC-3)
│   └── ShortcutDataService.cs ショートカットデータ読み込み
├── Views/
│   └── SettingsWindow.xaml / .cs  設定 GUI (MVP 5-9)
└── Data/
    └── shortcuts.json        ショートカット定義 (差し込み可)
```

## ショートカットデータの拡張

`Data/shortcuts.json` を編集してショートカットを追加・変更できます。スキーマは `docs/SPEC.md` セクション 7 を参照。

## レッドライン

- グローバルキー/マウスフック 禁止
- 他アプリの内容読み取り 禁止
- ネットワーク通信 一切禁止
- 管理者権限要求 禁止
