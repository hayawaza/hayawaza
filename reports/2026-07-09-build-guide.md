# ビルド手順書 - ShortcutOverlay

**作成日**: 2026-07-09
**対象**: ShortcutOverlay v0.1 (PoC-0〜3 + MVP 1-9 実装済み)

---

## 1. 前提確認

### 1.1 .NET 8 SDK の確認

```powershell
dotnet --version
```

`8.0.x` が表示されれば OK。表示されない場合は次節でインストール。

### 1.2 .NET 8 SDK のインストール（未インストールの場合）

```powershell
# winget が使える場合
winget install Microsoft.DotNet.SDK.8

# winget が使えない場合
# https://dotnet.microsoft.com/download/dotnet/8.0 から
# "SDK x64 Installer" をダウンロードして実行（非管理者インストール可）
```

インストール後、ターミナルを再起動して `dotnet --version` で確認。

### 1.3 NuGet 接続確認

```powershell
dotnet nuget list source
```

`nuget.org` が `Enabled` になっていれば OK。
プロキシ環境の場合は別途 NuGet プロキシ設定が必要（IT 部門に確認）。

---

## 2. ビルド

### 2.1 プロジェクトディレクトリへ移動

```powershell
cd "C:\Users\atomu.handa\OneDrive - Accenture\00.claude\shortcut-overlay\src\ShortcutOverlay"
```

### 2.2 依存パッケージの復元

```powershell
dotnet restore
```

### 2.3 デバッグビルド（動作確認用）

```powershell
dotnet build
```

成功すると `bin\Debug\net8.0-windows\` に実行ファイルが生成される。

### 2.4 起動

```powershell
dotnet run
```

または

```powershell
.\bin\Debug\net8.0-windows\ShortcutOverlay.exe
```

---

## 3. 動作確認手順

### 3.1 起動後の確認

1. タスクトレイに ShortcutOverlay アイコンが表示される
2. Excel または PowerPoint を起動してアクティブにする
3. オーバーレイが画面左上（既定位置）に表示される
4. Excel / PowerPoint 以外のウィンドウをアクティブにするとオーバーレイが消える

### 3.2 クリックスルーの確認

1. オーバーレイ表示中に、オーバーレイの下にあるウィンドウをクリックできることを確認
2. タスクトレイアイコンをダブルクリック → 設定ウィンドウが開くことを確認

### 3.3 設定UIの確認

| 項目 | 確認内容 |
|------|---------|
| 表示モード | 「ホットキーで呼び出し」に変更 → 呼び出しキーのコンボが表示される |
| 透過率スライダー | 動かすと % が更新される |
| ショートカット ON/OFF | チェックを外して保存 → オーバーレイから消える |
| デバッグプロセス | `notepad` と入力して保存 → メモ帳をアクティブにするとオーバーレイ表示 |

### 3.4 ホットキーモードの確認

1. 設定で「ホットキーで呼び出し」に変更
2. Excel をアクティブにしてもオーバーレイが表示されないことを確認
3. Ctrl+Shift+S を押すとオーバーレイが表示される（もう一度で非表示）

---

## 4. self-contained 配布ビルド

.NET 8 がインストールされていない端末でも動作させる場合:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

出力先: `bin\Release\net8.0-windows\win-x64\publish\ShortcutOverlay.exe`

このファイル単体（+ `Data\shortcuts.json` を同じフォルダに配置）で動作する。

---

## 5. 既知の制限・注意事項

| 項目 | 内容 |
|------|------|
| アイコン未設定 | `Resources\icon.ico` がない場合は標準アイコンを使用。ICO ファイルを配置すれば反映される |
| ホットキー競合 | 他アプリが同じキーを使っていると登録に失敗する（設定UIで別キーに変更） |
| DPI | PerMonitorV2 対応済み。100%/125%/150%/200% で動作確認を推奨 |
| shortcuts.json | コメント非対応の純 JSON。編集後は JSON validator で確認を推奨 |

---

## 6. トラブルシューティング

### オーバーレイが表示されない

1. タスクトレイにアイコンがあるか確認（起動できているか）
2. 設定の「デバッグプロセス」に余分な値が入っていないか確認
3. Excel のプロセス名を確認: タスクマネージャー → 詳細 → `EXCEL.EXE`（プロセス名は `excel`）

### ビルドエラー: `UseWindowsForms` 関連

`.csproj` に `<UseWindowsForms>true</UseWindowsForms>` が設定されているか確認。

### `dotnet: command not found`

.NET SDK のインストール先がPATHに含まれていない。ターミナルを再起動するか、PATH を手動追加:

```powershell
$env:PATH += ";$env:LOCALAPPDATA\Programs\dotnet"
```
