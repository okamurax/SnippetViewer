# Git Snippets

Gitでよく使うコマンド集

## 基本操作

### 状態確認

```bash
git status
```

### 変更をステージ

```bash
git add .
git add filename
```

### コミット

```bash
git commit -m "コミットメッセージ"
```

## ブランチ

### ブランチ一覧

```bash
git branch
git branch -a
```

### ブランチ作成・切り替え

```bash
git checkout -b feature/new-feature
git switch -c feature/new-feature
```

### ブランチ削除

```bash
git branch -d branch-name
git branch -D branch-name  # 強制削除
```

## リモート

### プッシュ

```bash
git push origin main
git push -u origin feature/new-feature
```

### プル

```bash
git pull origin main
```

### フェッチ

```bash
git fetch origin
```

## 取り消し

### 直前のコミットを修正

```bash
git commit --amend
```

### ステージング取り消し

```bash
git reset HEAD filename
```

### 変更を破棄

```bash
git checkout -- filename
git restore filename
```
