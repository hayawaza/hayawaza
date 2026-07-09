# CONTEXT.md - Shortcut Overlay

**Status**: 🚧 開始
**Last Updated**: 2026-07-09

---

## プロジェクト概要

**目的**: Windows 上で最前面のアプリ (Excel / PowerPoint) に応じたキーボードショートカット一覧をオーバーレイ表示する常駐デスクトップアプリ
**背景**: 操作中アプリのショートカット一覧をマウス不要・邪魔にならない形で常時参照したい

---

## 技術スタック

| 項目 | 内容 |
|------|------|
| 言語 | C# (.NET 8) |
| UI | WPF |
| Win32 連携 | P/Invoke (GetForegroundWindow, SetWindowLong 等) |
| 外部依存 | なし (.NET 標準 + Win32 のみ) |
| IDE | VS Code + dotnet CLI |

---

## ディレクトリ構成

```
shortcut-overlay/
├── CONTEXT.md
├── .clauderc.json
├── README.md
├── docs/
│   └── SPEC.md           # 仕様書（唯一の実装基準）
├── reports/              # ビルドガイド等
└── src/
    └── ShortcutOverlay/  # C# WPF プロジェクト本体
        ├── ShortcutOverlay.csproj
        ├── app.manifest
        ├── App.xaml / App.xaml.cs
        ├── MainWindow.xaml / MainWindow.xaml.cs
        ├── Models/
        ├── Services/
        ├── Views/
        └── Data/
```

---

## 現在のタスク

- [x] プロジェクト初期化 (CONTEXT.md / SPEC.md / .clauderc.json)
- [x] PoC-0: scaffold
- [x] PoC-1: 透過・最前面・枠なしウィンドウ
- [x] PoC-2: クリックスルー動的切替
- [x] PoC-3: 前面プロセス判定 + ポーリング
- [ ] MVP 1-9: 全機能実装
- [ ] ビルドガイド作成
- [ ] git commit (各フェーズ)

---

## アクティブファイル

| ファイル | 役割 |
|---------|------|
| `src/ShortcutOverlay/ShortcutOverlay.csproj` | プロジェクト定義 |
| `src/ShortcutOverlay/App.xaml.cs` | アプリエントリ・トレイ常駐 |
| `src/ShortcutOverlay/MainWindow.xaml.cs` | オーバーレイ本体・Win32連携 |
| `src/ShortcutOverlay/Models/ShortcutEntry.cs` | ショートカットデータモデル |
| `src/ShortcutOverlay/Services/ForegroundWatcher.cs` | 前面プロセス監視 |
| `src/ShortcutOverlay/Services/SettingsService.cs` | 設定保存/復元 |
| `src/ShortcutOverlay/Views/SettingsWindow.xaml.cs` | 設定UI |
| `src/ShortcutOverlay/Data/shortcuts.json` | ショートカット定義データ |

---

## 注意事項

- **管理者権限なし**: ClickOnce 等の管理者インストール不可。`dotnet publish` の self-contained 出力を xcopy 配布予定
- **SetWindowLong の WS_EX_TRANSPARENT**: クリックスルー有効時に設定、設定UI表示時は解除
- **GetForegroundWindow ポーリング**: 200-500ms 間隔。負荷が問題になる場合は settings.json の `pollingIntervalMs` で調整
- **RegisterHotKey**: 呼び出しキーの OS ホットキー登録。他アプリとの競合は設定UIで変更可能
- **WS_EX_LAYERED 必須**: AllowsTransparency=true の WPF ウィンドウに自動付与される。SetWindowLong で EX スタイル変更時は LAYERED を保持すること
- **デバッグ用プロセス差し替え**: settings.json の `debugTargetProcesses` フィールドで excel/powerpnt を notepad 等に差し替え可能

---

## 実装レッドライン（絶対禁止）

1. グローバルキー/マウスフック (SetWindowsHookEx 低レベルフック)
2. 他アプリ内容読み取り (UI Automation で他アプリ内部を覗く)
3. ネットワーク通信 (テレメトリ・ライセンス・アップデート含む)
4. 管理者権限要求
5. 既定でのレジストリ自動起動登録
6. 他プロセスへの書き込み・インジェクション
7. 設定 JSON のユーザー手編集強制 (全 GUI 完結)
