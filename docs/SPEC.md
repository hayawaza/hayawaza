# SPEC.md - ShortcutOverlay 仕様書

**Version**: 1.0
**Last Updated**: 2026-07-09
**Status**: 実装中（フェーズ0〜1）

---

## 1. プロジェクト概要

### 1.1 目的

Windows PC 上で最前面のアプリ（Excel / PowerPoint）に応じたキーボードショートカット一覧を、半透明オーバーレイで常時表示する常駐デスクトップツール。

### 1.2 対象ユーザー

- 会社支給 Windows PC（非管理者権限）
- Excel / PowerPoint を日常的に使う業務ユーザー

### 1.3 価値提案

- ショートカットをその場で確認できる → マニュアル検索不要
- オーバーレイ表示のため作業を中断しない
- 表示/非表示・クリックスルー切替で邪魔にならない

---

## 2. 実行環境制約

| 項目 | 内容 |
|------|------|
| OS | Windows 10 / 11 |
| 権限 | 非管理者 |
| .NET | .NET 8 (self-contained 配布) |
| フレームワーク | WPF |
| 外部依存 | .NET 標準ライブラリ + Win32 P/Invoke のみ |
| コスト | 開発期 ¥0（有料ライブラリ・パッケージ一切禁止） |
| コード署名 | 開発期は自己署名、配布時 Microsoft Store ($19) 予定 |

---

## 3. 機能一覧

### F1 前面アプリ検出

- GetForegroundWindow → GetWindowThreadProcessId → Process.GetProcessById().ProcessName
- 判定対象: `excel`, `powerpnt`（大文字小文字無視）
- DispatcherTimer で 200–500ms 間隔ポーリング（settings.json で変更可能）

### F2 表示モード切替

| モード | 動作 |
|--------|------|
| 常時表示 | 対象アプリが前面の間、常にオーバーレイ表示 |
| 呼び出し | OS 登録ホットキー（RegisterHotKey）で一時表示 |

呼び出しキーはプルダウンで選択（Ctrl+Shift+S など）。

### F3 ショートカット個別 ON/OFF

- チェックボックスでショートカット単位の表示/非表示を切替
- 設定はローカル保存（settings.json の `hiddenShortcuts` 配列）

### F4 オーバーレイ表示

- カテゴリごとにグループ表示
- クリックスルー有効（WS_EX_TRANSPARENT）
- 設定UIを開いた時のみクリックスルー解除

### F5 ビジュアル調整

- 透過率スライダー（0–100%）
- サイズスライダー（縦横比固定スケール）
- 位置スライダー or ドラッグ移動

### F6 表示位置・サイズ記憶

- 位置・サイズを settings.json に保存し、次回起動時復元

### F7 カテゴリ分類

ショートカットデータの `category` フィールドでグループ化して表示。

### F8 ショートカットデータ構造

後述セクション 7 を参照。

---

## 4. オーバーレイ仕様

### 4.1 ウィンドウ属性

```xml
Topmost="True"
AllowsTransparency="True"
WindowStyle="None"
ShowInTaskbar="False"
Background="Transparent"
```

### 4.2 クリックスルー

- 通常時: WS_EX_TRANSPARENT 付与（マウスイベントを下のウィンドウに透過）
- 設定UI表示時: WS_EX_TRANSPARENT 除去（クリック受付）

```csharp
const int GWL_EXSTYLE = -20;
const int WS_EX_TRANSPARENT = 0x20;
const int WS_EX_LAYERED = 0x80000;

[DllImport("user32.dll")]
static extern int GetWindowLong(IntPtr hwnd, int index);
[DllImport("user32.dll")]
static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

void EnableClickThrough(IntPtr hwnd) {
    var style = GetWindowLong(hwnd, GWL_EXSTYLE);
    SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_TRANSPARENT | WS_EX_LAYERED);
}

void DisableClickThrough(IntPtr hwnd) {
    var style = GetWindowLong(hwnd, GWL_EXSTYLE);
    SetWindowLong(hwnd, GWL_EXSTYLE, style & ~WS_EX_TRANSPARENT);
}
```

### 4.3 表示/非表示ロジック

```
前面プロセス判定 (200ms ポーリング)
  ├─ excel / powerpnt → アプリ切替 + オーバーレイ表示
  └─ それ以外 → オーバーレイ非表示
```

---

## 5. 設定仕様

### 5.1 保存先

`%LOCALAPPDATA%\ShortcutOverlay\settings.json`

### 5.2 スキーマ

```json
{
  "displayMode": "always",
  "hotkeyModifiers": 3,
  "hotkeyVk": 83,
  "overlayOpacity": 0.85,
  "overlayScale": 1.0,
  "overlayLeft": 100.0,
  "overlayTop": 100.0,
  "hiddenShortcuts": [],
  "pollingIntervalMs": 300,
  "debugTargetProcesses": []
}
```

| フィールド | 型 | 説明 |
|-----------|-----|------|
| `displayMode` | string | `"always"` or `"hotkey"` |
| `hotkeyModifiers` | int | RegisterHotKey の fsModifiers (MOD_SHIFT=4, MOD_CTRL=2, MOD_ALT=1) |
| `hotkeyVk` | int | 仮想キーコード |
| `overlayOpacity` | double | 0.0–1.0 |
| `overlayScale` | double | スケール倍率 |
| `overlayLeft` / `overlayTop` | double | 画面左上からのピクセル位置 |
| `hiddenShortcuts` | string[] | 非表示にした id リスト |
| `pollingIntervalMs` | int | ポーリング間隔 ms（既定 300） |
| `debugTargetProcesses` | string[] | 空なら既定 excel/powerpnt。notepad 等を指定でデバッグ |

---

## 6. デバッグ機構

### 6.1 プロセス差し替え

`settings.json` の `debugTargetProcesses` に任意プロセス名を列挙することで、前面判定対象を差し替えられる。

```json
{
  "debugTargetProcesses": ["notepad", "calc"]
}
```

空配列（既定）の場合は `excel`, `powerpnt` を使用。

### 6.2 ポーリング負荷確認

DispatcherTimer の Tick 内で `Stopwatch` でかかった時間をログ出力（Debug.WriteLine）することで、ポーリング負荷を後日評価可能にする。

---

## 7. ショートカットデータ構造

### 7.1 スキーマ (shortcuts.json)

```json
[
  {
    "id": "excel-ctrl-home",
    "app": "excel",
    "category": "移動",
    "label": "先頭セルに移動",
    "keys": ["Ctrl", "Home"],
    "keyType": "combo",
    "defaultVisible": true
  },
  {
    "id": "excel-alt-h-1",
    "app": "excel",
    "category": "書式",
    "label": "太字",
    "keys": ["Alt", "H", "1"],
    "keyType": "sequence",
    "defaultVisible": true
  }
]
```

| フィールド | 型 | 説明 |
|-----------|-----|------|
| `id` | string | 一意識別子 |
| `app` | string | `"excel"` or `"powerpnt"` |
| `category` | string | カテゴリ名（グループ表示用） |
| `label` | string | 操作内容の説明 |
| `keys` | string[] | キー名の配列 |
| `keyType` | string | `"combo"`（同時押し）/ `"sequence"`（順押し） |
| `defaultVisible` | bool | 初期表示状態 |

### 7.2 表示フォーマット

- **combo**: `Ctrl+Home`, `Ctrl+Shift+End`
- **sequence**: `Alt → H → 1`, `Alt → H → B`

---

## 8. 実装フェーズ

### フェーズ0: PoC 基盤

| ID | 内容 |
|----|------|
| PoC-0 | scaffold: ShortcutOverlay.csproj, App.xaml, MainWindow.xaml |
| PoC-1 | 透過・最前面・枠なしオーバーレイウィンドウ |
| PoC-2 | クリックスルー動的切替（WS_EX_TRANSPARENT） |
| PoC-3 | 前面プロセス判定 + DispatcherTimer ポーリング |

### フェーズ1: MVP

| # | 機能 | 参照 |
|---|------|------|
| 1 | トレイ常駐、終了メニュー、設定保存/復元 | F6 |
| 2 | 前面判定→表示アプリ切替 | F1, 4.3 |
| 3 | ショートカットデータ構造とサンプル | F8, 7章 |
| 4 | オーバーレイ表示（カテゴリ分類・クリックスルー） | F4, 4章 |
| 5 | 表示モード切替（常時/呼び出し・RegisterHotKey） | F2 |
| 6 | ショートカット個別 ON/OFF | F3 |
| 7 | ビジュアル調整（透過率・サイズ・位置） | F5 |
| 8 | 設定 UI 全 GUI 化 | — |
| 9 | 高 DPI 対応（DPI aware manifest） | N6 |

---

## 9. 非機能要件

| ID | 要件 |
|----|------|
| N1 | 非管理者で動作 |
| N2 | インストーラー不要（xcopy 配布） |
| N3 | 自動起動はユーザーが明示的に有効化した場合のみ |
| N4 | ネットワーク通信一切なし |
| N5 | 起動時 CPU < 1%、ポーリング時 CPU < 0.5% 目安 |
| N6 | 高 DPI (200%) でレイアウト崩れなし |
| N7 | 複数モニター対応（モニター設定を settings.json で保持） |

---

## 10. レッドライン（禁止事項）

1. `SetWindowsHookEx` 低レベルキー/マウスフック
2. UI Automation で他アプリ内部を読み取る
3. ネットワーク通信（テレメトリ・ライセンス・アップデート含む）
4. 管理者権限要求（UAC elevation）
5. レジストリ自動起動の既定有効化
6. 他プロセスへの書き込み・インジェクション
7. 設定 JSON をユーザーに手編集させる

---

## 11. 未確定項目（要確認）

| # | 項目 | 備考 |
|---|------|------|
| U1 | 呼び出しホットキーの既定値 | Ctrl+Shift+S 案 |
| U2 | オーバーレイの既定位置 | 右下固定 or ユーザー任意 |
| U3 | 自動起動 UI の要否 | 設定UIに「スタートアップ登録」ボタンを設けるか |
| U4 | PowerPoint 以外（Word / Outlook）の将来対応 | フェーズ2以降 |
| U5 | ショートカット全件数の目標 | ボスが後で差し込み、構造優先 |
