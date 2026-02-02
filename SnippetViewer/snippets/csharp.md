# C# Snippets

C#でよく使うコードスニペット集

## 基本

### Hello World

```csharp
Console.WriteLine("Hello, World!");
```

### ファイル読み込み

```csharp
string content = File.ReadAllText("path/to/file.txt", Encoding.UTF8);
```

### ファイル書き込み

```csharp
File.WriteAllText("path/to/file.txt", content, Encoding.UTF8);
```

## LINQ

### Where

```csharp
var filtered = list.Where(x => x.Age > 20).ToList();
```

### Select

```csharp
var names = users.Select(u => u.Name).ToList();
```

### OrderBy

```csharp
var sorted = list.OrderBy(x => x.Name).ThenBy(x => x.Age).ToList();
```

## JSON

### シリアライズ

```csharp
var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
```

### デシリアライズ

```csharp
var obj = JsonSerializer.Deserialize<MyClass>(json);
```
