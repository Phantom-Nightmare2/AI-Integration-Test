# VELKRIS — C# Error and Bug Test (10 Questions)

これはAI開発エージェントVELKRISの能力テストです。問題はQ01からQ10へ徐々に難しくなります。
「C#」は言語名で、10は問題数です。C# 10以降の構文を扱えるコンソール環境を使用してください。

## 回答ルール
- 以下の「問題コード」と「要求仕様」は変更・削除しないでください。
- 各問題の直下にある「VELKRISの回答」だけを編集してください。
- 回答には原因、修正版コード全文、確認方法・結果を記載してください。
- 修正版は回答欄の `csharp` コードブロックに書き、修正箇所に理由を説明する `//` コメントを付けてください。
- 各問題は独立したプログラムです。まとめて1つのプロジェクトに入れないでください。
- 検証する場合は一時的な別のコンソールプロジェクトを使い、このリポジトリの既存 `.cs` ファイルは変更しないでください。
- 実行した確認と未実行の確認を区別してください。未実行の場合は理由と予想結果を書いてください。
- 要求仕様を満たすための修正に集中してください。
- この問題集には解答例を含めません。採点は回答提出後に行います。

## 採点
各問10点、合計100点。原因の説明2点、要求を満たす修正版6点、指定ケースの確認方法・結果2点。
実行していない場合も、その事実を明記し再現可能な確認手順と予想結果を書けば確認内容を評価できます。
得点と合否はレビュアーが記入します。VELKRIS自身は合格の宣言や採点を記入しないでください。
このテストで評価する範囲は、掲載されたC#のエラー・バグへの対応です。

## Q01 — 文法エラー

### 要求仕様
10と5の合計を表示してください。期待する表示は `15` です。

### 問題コード（変更禁止）
```csharp
using System;
class Program
{
    static void Main()
    {
        int first = 10;
        int second = 5
        Console.WriteLine(first + second);
    }
}
```

### VELKRISの回答

**原因：** 未回答

**修正版コード：**
```csharp
// ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

---

## Q02 — 変数とメソッド

### 要求仕様
長方形の面積を表示してください。期待する表示は `24` です。

### 問題コード（変更禁止）
```csharp
using System;
class Program
{
    static void Main()
    {
        int width = 6;
        int height = 4;
        int area = width * hight;
        Console.WritLine(area);
    }
}
```

### VELKRISの回答

**原因：** 未回答

**修正版コード：**
```csharp
// ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

---

## Q03 — 条件分岐

### 要求仕様
正数・ゼロ・負数をそれぞれ Positive / Zero / Negative と表示してください。5→Positive、0→Zero、-3→Negative を確認してください。

### 問題コード（変更禁止）
```csharp
using System;
class Program
{
    static void Main()
    {
        int value = 5;
        if (value >= 0)
            Console.WriteLine("Positive");
        else if (value == 0)
            Console.WriteLine("Zero");
        else
            Console.WriteLine("Negative");
    }
}
```

### VELKRISの回答

**原因：** 未回答

**修正版コード：**
```csharp
// ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

---

## Q04 — ループと配列

### 要求仕様
配列の全要素を合計してください。{2,4,6}→12、空配列→0 が期待値です。

### 問題コード（変更禁止）
```csharp
using System;
class Program
{
    static int Sum(int[] values)
    {
        int total = 0;
        for (int i = 0; i <= values.Length; i++)
            total += values[i];
        return total;
    }
    static void Main() => Console.WriteLine(Sum(new[] { 2, 4, 6 }));
}
```

### VELKRISの回答

**原因：** 未回答

**修正版コード：**
```csharp
// ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

---

## Q05 — 数値計算

### 要求仕様
平均値を小数まで計算してください。{1,2}→1.5、{2,4,6}→4。空配列は ArgumentException としてください。入力は非nullで、合計はintの範囲内です。

### 問題コード（変更禁止）
```csharp
using System;
class Program
{
    static double Average(int[] values)
    {
        int total = 0;
        foreach (int value in values)
            total += value;
        return total / values.Length;
    }
    static void Main() => Console.WriteLine(Average(new[] { 1, 2 }));
}
```

### VELKRISの回答

**原因：** 未回答

**修正版コード：**
```csharp
// ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

---

## Q06 — 入力処理

### 要求仕様
非負のintとして有効な文字列だけを受け付けてください。成功時はtrueと数値、失敗時はfalseと0を返してください。null、空文字、abc、-1、2147483648 は失敗、0と42は成功です。前後の空白は許容します。

### 問題コード（変更禁止）
```csharp
using System;
class Program
{
    static bool TryReadCount(string? text, out int count)
    {
        count = int.Parse(text!);
        return count > 0;
    }
    static void Main()
    {
        Console.WriteLine(TryReadCount("42", out int count));
        Console.WriteLine(count);
    }
}
```

### VELKRISの回答

**原因：** 未回答

**修正版コード：**
```csharp
// ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

---

## Q07 — コレクションの更新

### 要求仕様
負の値をすべて削除し、残った値の順番を保ってください。{-1,-2,0,3,-4}→{0,3}、空リスト→空リストです。入力は非nullです。

### 問題コード（変更禁止）
```csharp
using System;
using System.Collections.Generic;
class Program
{
    static void RemoveNegative(List<int> values)
    {
        foreach (int value in values)
        {
            if (value < 0)
                values.Remove(value);
        }
    }
    static void Main()
    {
        var values = new List<int> { -1, -2, 0, 3, -4 };
        RemoveNegative(values);
        Console.WriteLine(string.Join(",", values));
    }
}
```

### VELKRISの回答

**原因：** 未回答

**修正版コード：**
```csharp
// ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

---

## Q08 — LINQと実行タイミング

### 要求仕様
Addを呼ぶ前の偶数のスナップショットを、元の順番で返してください。{1,2,3,4}→戻り値{2,4}、元リスト{1,2,3,4,6}。空リスト→戻り値空、元リスト{6}。戻り値は何度列挙しても同じ内容としてください。

### 問題コード（変更禁止）
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
class Program
{
    static IEnumerable<int> CaptureEvenAndAppend(List<int> values)
    {
        var captured = values.Where(value => value % 2 == 0);
        values.Add(6);
        return captured;
    }
    static void Main()
    {
        var values = new List<int> { 1, 2, 3, 4 };
        Console.WriteLine(string.Join(",", CaptureEvenAndAppend(values)));
    }
}
```

### VELKRISの回答

**原因：** 未回答

**修正版コード：**
```csharp
// ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

---

## Q09 — 非同期処理

### 要求仕様
両方の処理の完了後に合計42を返してください。Task.Delayは完了を待ってください。Thread.Sleep、同期的なWait、Resultは使わず、async/awaitで修正してください。

### 問題コード（変更禁止）
```csharp
using System;
using System.Threading.Tasks;
class Program
{
    static async Task<int> ReadAsync(int value)
    {
        Task.Delay(20);
        return value;
    }
    static async Task<int> CalculateAsync()
    {
        Task<int> first = ReadAsync(20);
        Task<int> second = ReadAsync(22);
        return first.Result + second.Result;
    }
    static async Task Main() => Console.WriteLine(await CalculateAsync());
}
```

### VELKRISの回答

**原因：** 未回答

**修正版コード：**
```csharp
// ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

---

## Q10 — 並行処理と共有状態

### 要求仕様
100個の並行タスクが各1000回加算し、すべてのタスク完了後に毎回100000を返すよう修正してください。タスクの並行実行は維持し、スレッドセーフな加算を使ってください。複数回の呼び出しも独立して100000を返してください。

### 問題コード（変更禁止）
```csharp
using System;
using System.Threading.Tasks;
class Program
{
    static async Task<int> CountAsync()
    {
        int count = 0;
        var tasks = new Task[100];
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < 1000; j++)
                    count++;
            });
        }
        Task.WhenAll(tasks);
        return count;
    }
    static async Task Main() => Console.WriteLine(await CountAsync());
}
```

### VELKRISの回答

**原因：** 未回答

**修正版コード：**
```csharp
// ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

---

