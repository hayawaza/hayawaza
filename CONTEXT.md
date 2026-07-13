# CONTEXT.md - Hayawaza (早業)

**Status**: 🚧 開発中 (v0.1 機能完成 / ストア出品前)
**Last Updated**: 2026-07-10

---

## プロジェクト概要

**目的**: Windows 上で最前面のアプリ (Excel / PowerPoint) に応じたキーボードショートカット一覧をオーバーレイ表示する常駐デスクトップアプリ  
**アプリ名**: Hayawaza（早業）  
**トリガー**: 右 Shift 押下中に表示、左 Shift でカテゴリ送り

---

## 技術スタック

| 項目 | 内容 |
|------|------|
| 言語 | C# (.NET 8) |
| UI | WPF (`UseWPF=true`, `UseWindowsForms=true`) |
| Win32 連携 | P/Invoke (低レベルキーボードフック, SetWindowLong 等) |
| アセンブリ名 | `Hayawaza` |
| 外部依存 | なし (.NET 標準 + Win32 のみ) |

---

## 実装済み機能

| 機能 | 実装詳細 |
|------|---------|
| 透過オーバーレイ | WPF AllowsTransparency + WindowStyle=None |
| クリックスルー | WS_EX_TRANSPARENT 動的切替 |
| 前面プロセス監視 | ForegroundWatcher (SetWinEventHook / GetForegroundWindow) |
| キー検知 | SetWindowsHookEx (WH_KEYBOARD_LL) — VK_RSHIFT=表示, VK_LSHIFT=カテゴリ送り |
| カテゴリページング | 右Shift押下中に左Shiftで送り / Ctrl+Alt+. /, でも操作可能 |
| セパレーター | Alt キー系を第3キーまでグルーピング (SeparatorItem + DataTemplateSelector) |
| マルチモニター | MonitorFromWindow でアクティブアプリのモニタに追従 |
| システムトレイ | NotifyIcon (WinForms) — ダブルクリックで設定 |
| テーマ切替 | DarkTheme.xaml / WhiteTheme.xaml (DynamicResource) |
| 設定 UI | SettingsWindow — テーマ/透明度/スケール/ショートカット表示切替 |
| アイコン | 稲妻デザイン (パープル→シアン) — 256/48/32/16px マルチサイズ ICO |
| 多重起動防止 | Mutex "Hayawaza_SingleInstance_v2" |
| スクリーンキャプチャ除外 | SetWindowDisplayAffinity (WDA_EXCLUDEFROMCAPTURE) |

---

## ショートカットデータ (shortcuts.json)

**Excel (3 カテゴリ)**:
- 移動/シート: 10件 (Ctrl矢印/Home/End/PgDn/PgUp + Alt HOR/HDS)
- 選択/編集: 13件 (Ctrl Z/Y/X/C/V/D + F4/F2 + Ctrl-Shift矢印)
- 書式/罫線: 15件 (Ctrl B/1 + Alt系各種)

**PowerPoint (3 カテゴリ)**:
- 編集: 8件
- オブジェクト: 14件
- テキスト: 15件

---

## 残タスク（キュー）

### 🔴 リリース前必須
- [ ] README.md 更新 (インストール手順・使い方)
- [ ] アプリ説明文・スクリーンショット準備 (Store 出品用)
- [ ] Settings で「スタートアップ登録」ON/OFF を実装 (現状: 手動のみ)
- [ ] バージョン表示を設定 UI に追加

### 🟡 品質向上
- [ ] ショートカットデータ: PPT カテゴリも Excel 同様に3カテゴリへ整理確認
- [ ] 設定ウィンドウの挙動確認 (クリックスルー解除が確実に機能しているか)
- [ ] 日本語以外のロケール対応確認

### 🟢 Store 出品 (別途項目参照)
- [ ] MSIX パッケージ作成
- [ ] Partner Center 登録 ($19 / 個人)
- [ ] 審査提出

---

## ファイル構成

```
shortcut-overlay/
├── CONTEXT.md
├── .clauderc.json
├── docs/SPEC.md
└── src/ShortcutOverlay/
    ├── ShortcutOverlay.csproj   (AssemblyName=Hayawaza)
    ├── app.manifest
    ├── App.xaml / App.xaml.cs   (起動・トレイ・Mutex)
    ├── MainWindow.xaml / .cs    (オーバーレイ本体・フック)
    ├── Assets/icon.png + icon.ico
    ├── Data/shortcuts.json
    ├── Models/
    ├── Services/                (ForegroundWatcher, SettingsService, ShortcutDataService)
    ├── Themes/                  (DarkTheme.xaml, WhiteTheme.xaml)
    └── Views/                   (SettingsWindow)
```

---

## ビルド

```powershell
cd src\ShortcutOverlay
dotnet build -c Release
# 出力: bin\Release\net8.0-windows\Hayawaza.exe
```

---

## 注意事項

- **SetWindowsHookEx 使用中**: 当初レッドラインに記載していたが、右Shiftトリガー実現のため採用。ストア審査では Restricted Capability 扱いの可能性あり
- **SetWindowLong の WS_EX_TRANSPARENT**: クリックスルー有効時に設定、設定UI表示時は解除
- **RegisterHotKey**: Ctrl+Alt+. / Ctrl+Alt+, をカテゴリナビ用に登録
- **アイコン読み込み**: XAML の Icon= 属性は使わず App.xaml.cs で BitmapFrame.Create(new Uri(path)) で設定（XamlParseException 回避）
- **Mutex 名**: "Hayawaza_SingleInstance_v2" (v1 は廃棄プロセスが占有していたため変更)
