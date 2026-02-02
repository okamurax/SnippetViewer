# SnippetViewer

マークダウン形式のコードスニペットを管理・閲覧するためのWindowsデスクトップアプリケーションです。

## 機能

- **スニペット管理**: `snippets`フォルダ内のMarkdownファイル(.md)を自動読み込み
- **階層表示**: Markdownの見出し(h1-h6)をインデント付きでリスト表示
- **インクリメンタル検索**: 見出しタイトルと内容をリアルタイムで絞り込み
- **クリップボードコピー**: コンテンツをクリックするだけでクリップボードにコピー
- **ファイル操作**: ダブルクリックで関連アプリケーションを開く、Ctrl+ダブルクリックでフォルダを開く
- **セッション保持**: ウィンドウサイズ、パネル幅、選択項目を記憶

## 動作環境

- Windows 10 以降
- .NET 8.0 Runtime

## ビルド方法

### 必要なもの

- .NET 8.0 SDK

### ビルドコマンド

```bash
# デバッグビルド
cd SnippetViewer
dotnet build

# リリースビルド
dotnet build -c Release

# 発行（自己完結型）
dotnet publish -c Release -r win-x64 --self-contained
```

## 使い方

1. `SnippetViewer/snippets`フォルダにMarkdownファイル(.md)を配置
2. アプリケーションを起動
3. 左パネルでファイルを選択
4. 中央パネルで見出しを選択
5. 右パネルでコンテンツを確認・コピー

### キーボードショートカット

- `ESC`: アプリケーションを閉じる
- `Ctrl + ダブルクリック`: ファイルのフォルダを開く

## Markdownファイルの書式

```markdown
# 大見出し

## 中見出し

コードや説明文をここに記述します。
クリックすると空行までの内容がクリップボードにコピーされます。

### 小見出し

さらに詳細な内容を記述できます。
```

## プロジェクト構成

```
CodeSnippets/
├── README.md
├── policy.md              # 仕様書
└── SnippetViewer/
    ├── SnippetViewer.cs   # メインソースコード
    ├── SnippetViewer.csproj
    └── snippets/          # スニペットファイル格納フォルダ
        ├── csharp.md
        └── git.md
```

## ライセンス

MIT License
