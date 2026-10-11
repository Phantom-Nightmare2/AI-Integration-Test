# Client Order VELKRIS — C# Ability Test

> **利用条件：All Rights Reserved** — Phantom Nightmareが保有する権利について、法令・GitHub利用規約で認められる場合を除き、問題文等の転載・再配布・改変・他のテストへの転用には事前の許可が必要です。所有者が指示するVELKRIS等のテスト実施・回答提出は許可します。[利用条件全文](../../LICENSE.md)

## クライアント
ワトソン（架空のクライアント）

## 依頼先
VELKRIS

## 依頼内容
このページ下部に掲載したC#エラー・バグ対応問題10問に、Q01から順番に回答してください。問題は段階的に難しくなります。

## 必須要件
1. 問題コードを直接修正しないでください。問題文と元のコードをそのまま残してください。
2. 各問題の下にある「VELKRISの回答」欄に、原因・修正版コード全文・確認方法と結果を書いてください。
3. 修正版コードの変更箇所には、修正理由を説明するコメントを付けてください。
4. 必要な調査と動作確認は自律的に行ってください。実行できなかった確認は未実行と明記し、理由と再現可能な手順・予想結果を書いてください。
5. 提出する変更は この `README.md` の各問題直下の回答欄だけに限定してください。
6. 全10問の回答を記入したらコミットし、提出報告にコミットへのリンクと回答済み問題数を記載してください。自分で採点・合格判定はしないでください。

## 評価
各問10点、合計100点。詳細は問題集の採点ルールに従います。原因の特定、要求に沿った修正、確認内容を評価します。

公開リポジトリに秘密のエージェント設定、認証情報、APIキーを記載しないでください。

---

## C#能力テスト — 全10問

これはAI開発エージェントVELKRISの能力テストです。問題はQ01からQ10へ徐々に難しくなります。
「C#」は言語名で、10は問題数です。C# 10以降の構文を扱えるコンソール環境を使用してください。

## 回答ルール
- 以下の「問題コード」と「要求仕様」は変更・削除しないでください。
- 各問題の直下にある「VELKRISの回答」だけを編集してください。
- 回答には原因、修正版コード全文、確認方法・結果を記載してください。
- 修正版は回答欄の `csharp` コードブロックに書き、修正箇所に理由を説明する `//` コメントを付けてください。
- 各問題は独立したプログラムです。まとめて1つのプロジェクトに入れないでください。
- 検証する場合は一時的な別のコンソールプロジェクトを使い、検証用ファイルはコミットに含めないでください。
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

**原因：** `int second = 5` の行末にセミコロンがないため、文が終わらずコンパイルエラー（CS1002: `;` が必要です）になり、プログラムが実行されません。

**修正版コード：**
```csharp
using System;
class Program
{
    static void Main()
    {
        int first = 10;
        int second = 5; // 文の終端に必要なセミコロンが抜けていたため追加（CS1002の解消）
        Console.WriteLine(first + second);
    }
}
```

**確認方法・結果：** 実行済み（.NET SDK 10.0.401、net10.0。問題ごとに作った一時コンソールプロジェクトで実行）。
- 問題コード `dotnet run` → ビルド失敗: `Program.cs(7,23): error CS1002: ; が必要です`
- 修正版 `dotnet run` → 出力: `15`（期待値 `15` と一致）

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

**原因：** コンパイルエラーが2つあります。
1. `width * hight` の `hight` はスペルミスで、宣言されていない変数です（CS0103）。正しくは `height` です。
2. `Console.WritLine` はスペルミスで、そのメソッドは存在しません（CS0117）。正しくは `Console.WriteLine` です。

（1の影響で `height` が未使用という警告 CS0219 も出ます。）

**修正版コード：**
```csharp
using System;
class Program
{
    static void Main()
    {
        int width = 6;
        int height = 4;
        int area = width * height; // 未定義の変数名 hight を宣言済みの height に修正（CS0103の解消）
        Console.WriteLine(area); // 存在しないメソッド WritLine を Console.WriteLine に修正（CS0117の解消）
    }
}
```

**確認方法・結果：** 実行済み（一時コンソールプロジェクト、net10.0）。
- 問題コード `dotnet run` → ビルド失敗: `Program.cs(8,28): error CS0103: 現在のコンテキストに 'hight' という名前は存在しません` / `Program.cs(9,17): error CS0117: 'Console' に 'WritLine' の定義がありません`（ほかに警告 CS0219）
- 修正版 `dotnet run` → 出力: `24`（期待値 `24` と一致）

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

**原因：** 最初の条件が `value >= 0` なので、0 もこの条件に一致して `Positive` と表示されます。そのため `else if (value == 0)` の分岐には到達せず、`Zero` は表示されません。正数の条件は `value > 0` にする必要があります。また、問題コードは `value = 5` に固定されているため、0 と -3 を確認できません。

**修正版コード：**
```csharp
using System;
class Program
{
    // 判定処理をメソッドに分離し、5・0・-3 の各ケースを同じロジックで確認できるようにした
    static string Classify(int value)
    {
        if (value > 0) // >= 0 だと 0 も Positive になり Zero の分岐に到達しないため、> 0 に修正
            return "Positive";
        else if (value == 0)
            return "Zero";
        else
            return "Negative";
    }
    static void Main()
    {
        // 要求仕様の確認ケース 5 / 0 / -3 をそれぞれ表示する
        foreach (int value in new[] { 5, 0, -3 })
            Console.WriteLine(Classify(value));
    }
}
```

**確認方法・結果：** 実行済み（一時コンソールプロジェクト、net10.0）。
- 問題コード（`value` を 5 / 0 / -3 に書き換えた一時コピーで実行）→ `Positive` / `Positive` / `Negative`。0 が `Zero` にならない不具合を再現しました。
- 修正版 `dotnet run` → 出力:
  ```
  Positive
  Zero
  Negative
  ```
- 一時ハーネス（修正版の `Classify` をリフレクションで呼び出して期待値と比較）→ `OK Classify(5): Positive` / `OK Classify(0): Zero` / `OK Classify(-3): Negative`

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

**原因：** ループ条件が `i <= values.Length` なので、最後に存在しない `values[values.Length]` を読み、`IndexOutOfRangeException` が発生します。有効なインデックスは `0` から `Length - 1` までです。空配列では最初の `values[0]` で例外になり、期待値 0 を返せません。

**修正版コード：**
```csharp
using System;
class Program
{
    static int Sum(int[] values)
    {
        int total = 0;
        for (int i = 0; i < values.Length; i++) // <= だと最後に values[Length] へアクセスして IndexOutOfRangeException になるため < に修正（空配列ではループせず 0 を返す）
            total += values[i];
        return total;
    }
    static void Main() => Console.WriteLine(Sum(new[] { 2, 4, 6 }));
}
```

**確認方法・結果：** 実行済み（一時コンソールプロジェクト、net10.0）。
- 問題コード `dotnet run` → `Unhandled exception. System.IndexOutOfRangeException: Index was outside the bounds of the array.`
- 問題コードをハーネスで実行 → `{2,4,6}`、空配列のどちらも `IndexOutOfRangeException`
- 修正版 `dotnet run` → 出力: `12`
- 一時ハーネス（修正版の `Sum` を呼び出して比較）→ `OK Sum({2,4,6}): actual=12` / `OK Sum({}): actual=0`

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

**原因：**
1. `total / values.Length` は int 同士の除算なので、小数部分が切り捨てられます（`3 / 2 = 1`）。戻り値の型は double ですが、変換されるのは切り捨て後です。そのため {1,2} の結果が 1.5 ではなく 1 になります。
2. 空配列のときに `values.Length` が 0 になり、int の 0 除算で `DivideByZeroException` が発生します。仕様で求められている `ArgumentException` になりません。

**修正版コード：**
```csharp
using System;
class Program
{
    static double Average(int[] values)
    {
        // 空配列は平均を定義できず、0 除算になるため仕様どおり ArgumentException を投げる
        if (values.Length == 0)
            throw new ArgumentException("values must contain at least one element.", nameof(values));
        int total = 0;
        foreach (int value in values)
            total += value;
        return (double)total / values.Length; // int 同士の除算では小数が切り捨てられるため、double にキャストして小数まで計算する
    }
    static void Main() => Console.WriteLine(Average(new[] { 1, 2 }));
}
```

**確認方法・結果：** 実行済み（一時コンソールプロジェクト、net10.0）。
- 問題コード `dotnet run` → 出力: `1`（小数が切り捨てられることを再現）
- 問題コードをハーネスで実行 → `{1,2}`: `1`、`{2,4,6}`: `4`、空配列: `DivideByZeroException`
- 修正版 `dotnet run` → 出力: `1.5`
- 一時ハーネス（修正版の `Average` を呼び出し、InvariantCulture で文字列化して比較）→ `OK Average({1,2}): actual=1.5` / `OK Average({2,4,6}): actual=4` / `OK Average({}): actual=ArgumentException`

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

**原因：**
1. `int.Parse` は不正な入力で false を返さず、例外を投げます。null では `ArgumentNullException`、空文字と `abc` では `FormatException`、`2147483648`（int の範囲外）では `OverflowException` が発生します。`text!` の null 許容抑制演算子は警告を消すだけで、null を防ぎません。
2. 成功条件が `count > 0` なので、有効な入力の `0` が失敗（false）になります。非負を受け付けるには `>= 0` が必要です。
3. `-1` のように解析できる負数では false を返しますが、`count` には -1 が残ります。失敗時に 0 を返す仕様を満たしません。
4. 前後の空白は int.Parse の既定でも許容されますが、例外ベースの実装なので、上記の失敗ケースを安全に扱えません。

**修正版コード：**
```csharp
using System;
using System.Globalization; // NumberStyles / CultureInfo を使うため追加
class Program
{
    static bool TryReadCount(string? text, out int count)
    {
        // int.Parse は null・空文字・abc・int 範囲外(2147483648)で例外を投げるため、例外を投げない int.TryParse に変更
        // NumberStyles.Integer は前後の空白と符号を許可する（前後の空白は仕様で許容）
        // カルチャ依存を避けるため InvariantCulture を指定
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out count)
            && count >= 0) // 元の count > 0 では 0 が失敗扱いになるため、非負(>= 0)を成功とする
        {
            return true;
        }
        count = 0; // 失敗時（負数など TryParse は成功したが範囲外のケースを含む）は必ず 0 を返す
        return false;
    }
    static void Main()
    {
        Console.WriteLine(TryReadCount("42", out int count));
        Console.WriteLine(count);
    }
}
```

**確認方法・結果：** 実行済み（一時コンソールプロジェクト、net10.0）。
- 問題コード `dotnet run` → `True` / `42`（Main の入力だけでは不具合が表に出ません）
- 問題コードをハーネスで全入力に対して実行（結果は「戻り値/count」または例外名）:
  ```
  FAIL TryReadCount(null): actual=ArgumentNullException expected=False/0
  FAIL TryReadCount(""): actual=FormatException expected=False/0
  FAIL TryReadCount("abc"): actual=FormatException expected=False/0
  FAIL TryReadCount("-1"): actual=False/-1 expected=False/0
  FAIL TryReadCount("2147483648"): actual=OverflowException expected=False/0
  FAIL TryReadCount("0"): actual=False/0 expected=True/0
  OK   TryReadCount("42"): actual=True/42 expected=True/42
  OK   TryReadCount(" 42 "): actual=True/42 expected=True/42
  FAIL TryReadCount("\t0\n"): actual=False/0 expected=True/0
  OK   TryReadCount("2147483647"): actual=True/2147483647 expected=True/2147483647
  ```
- 修正版 `dotnet run` → 出力: `True` / `42`
- 修正版をハーネスで実行:
  ```
  OK   TryReadCount(null): actual=False/0 expected=False/0
  OK   TryReadCount(""): actual=False/0 expected=False/0
  OK   TryReadCount("abc"): actual=False/0 expected=False/0
  OK   TryReadCount("-1"): actual=False/0 expected=False/0
  OK   TryReadCount("2147483648"): actual=False/0 expected=False/0
  OK   TryReadCount("0"): actual=True/0 expected=True/0
  OK   TryReadCount("42"): actual=True/42 expected=True/42
  OK   TryReadCount(" 42 "): actual=True/42 expected=True/42
  OK   TryReadCount("\t0\n"): actual=True/0 expected=True/0
  OK   TryReadCount("2147483647"): actual=True/2147483647 expected=True/2147483647
  ```

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

**原因：** `foreach` で `List<int>` を列挙している途中に、`values.Remove(value)` で同じリストを変更しています。リストのバージョンが変わるため、次の `MoveNext` で `InvalidOperationException`（Collection was modified）が発生します。仮に例外が出なくても、要素を削除しながら前から走査すると、詰められた要素を飛ばしてしまいます（例: -1 の直後の -2）。

**修正版コード：**
```csharp
using System;
using System.Collections.Generic;
class Program
{
    static void RemoveNegative(List<int> values)
    {
        // foreach で列挙中のリストを Remove で変更すると InvalidOperationException になるため、
        // 条件に一致する要素を一括削除し残りの順序を保つ RemoveAll に置き換える
        values.RemoveAll(value => value < 0);
    }
    static void Main()
    {
        var values = new List<int> { -1, -2, 0, 3, -4 };
        RemoveNegative(values);
        Console.WriteLine(string.Join(",", values));
    }
}
```

**確認方法・結果：** 実行済み（一時コンソールプロジェクト、net10.0）。
- 問題コード `dotnet run` → `Unhandled exception. System.InvalidOperationException: Collection was modified; enumeration operation may not execute.`
- 問題コードをハーネスで実行 → `{-1,-2,0,3,-4}`、`{-5,-6}`、`{1,-1,2}` は `InvalidOperationException`。空リストは `{}`（ループしないため）。
- 修正版 `dotnet run` → 出力: `0,3`
- 修正版をハーネスで実行 → `OK {-1,-2,0,3,-4} → {0,3}` / `OK {} → {}` / `OK {-5,-6} → {}` / `OK {1,-1,2} → {1,2}`（順序が保たれることも確認）

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

**原因：** LINQ の `Where` は遅延実行です。`captured` に入るのは結果ではなく「列挙時に元リストを絞り込むクエリ」です。実際の評価は `values.Add(6)` の後の列挙時に行われるため、追加された 6 も含まれて `2,4,6` になります。さらに、列挙するたびに元リストの現在の内容を読み直すため、呼び出し後に元リストが変わると戻り値の内容も変わり、「何度列挙しても同じ内容」になりません。

**修正版コード：**
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
class Program
{
    static IEnumerable<int> CaptureEvenAndAppend(List<int> values)
    {
        // Where は遅延実行で、列挙時に Add 後のリストを読むため 6 まで含まれてしまう。
        // ToList() で Add 前にその場で評価し、偶数のスナップショット（元の順番）を確定させる。
        // 戻り値は独立した List なので、何度列挙しても同じ内容になる。
        var captured = values.Where(value => value % 2 == 0).ToList();
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

**確認方法・結果：** 実行済み（一時コンソールプロジェクト、net10.0）。
- 問題コード `dotnet run` → 出力: `2,4,6`
- 問題コードをハーネスで実行 → 入力 {1,2,3,4}: 戻り値は3回の列挙とも `{2,4,6}`、元リストに 8 を追加した後は `{2,4,6,8}` に変化。入力 {}: 戻り値 `{6}`、8 を追加した後は `{6,8}`。
- 修正版 `dotnet run` → 出力: `2,4`
- 修正版をハーネスで実行（戻り値を3回列挙し、さらに元リストに `Add(8)` してから再列挙）:
  ```
  OK   input {1,2,3,4} return enum#1: actual={2,4} expected={2,4}
  OK   input {1,2,3,4} return enum#2: actual={2,4} expected={2,4}
  OK   input {1,2,3,4} return enum#3: actual={2,4} expected={2,4}
  OK   input {1,2,3,4} source list: actual={1,2,3,4,6} expected={1,2,3,4,6}
  OK   input {1,2,3,4} return after extra Add(8): actual={2,4} expected={2,4}
  OK   input {} return enum#1: actual={} expected={}
  OK   input {} return enum#2: actual={} expected={}
  OK   input {} return enum#3: actual={} expected={}
  OK   input {} source list: actual={6} expected={6}
  OK   input {} return after extra Add(8): actual={} expected={}
  ```

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

**原因：**
1. `ReadAsync` 内の `Task.Delay(20);` が await されていません（警告 CS4014）。遅延タスクを作るだけで完了を待たないため、`ReadAsync` は待機せず同期的に完了して即座に値を返します。仕様の「Task.Delay は完了を待つ」を満たしていません。
2. `CalculateAsync` は async メソッドですが、`first.Result + second.Result` で同期的にブロックしています。async メソッド内で `.Result` を使うと、待機中にスレッドを占有します。さらに SynchronizationContext のある環境（UI や旧 ASP.NET など）ではデッドロックの原因になります。例外も `AggregateException` に包まれます。await を使って非同期に待つ必要があります。

（問題コードでも出力は 42 ですが、それは遅延を待っていないために Result 時点でタスクが既に完了しているからです。）

**修正版コード：**
```csharp
using System;
using System.Threading.Tasks;
class Program
{
    static async Task<int> ReadAsync(int value)
    {
        await Task.Delay(20); // Task.Delay を await していなかったため遅延の完了を待たずに戻っていた。await で完了を待つ
        return value;
    }
    static async Task<int> CalculateAsync()
    {
        Task<int> first = ReadAsync(20);
        Task<int> second = ReadAsync(22);
        // .Result は同期的にスレッドをブロックし、デッドロックの原因にもなるため使わず、
        // 両方のタスクを開始済みのまま await で完了を待ってから合計する
        return await first + await second;
    }
    static async Task Main() => Console.WriteLine(await CalculateAsync());
}
```

**確認方法・結果：** 実行済み（一時コンソールプロジェクト、net10.0）。
- 問題コード `dotnet build` → `Program.cs(7,9): warning CS4014: この呼び出しを待たないため、…`。`dotnet run` → 出力: `42`
- 問題コードをハーネスで実行 → `ReadAsync(20)` は呼び出し直後に `IsCompleted=True`（遅延を待たずに同期完了）。`CalculateAsync` は3回とも `42` だが、所要時間は `0ms`。
- 修正版 `dotnet build --no-incremental` → `0 個の警告` / `0 エラー`。`dotnet run` → 出力: `42`
- 修正版をハーネスで実行 → `OK ReadAsync(20) IsCompleted immediately after call: actual=False`（遅延を待っている）、`OK await ReadAsync(20): 20`、`CalculateAsync` を3回実行して結果はすべて `42`（所要時間 30ms / 30ms / 34ms。2つの 20ms 遅延を並行に待ってから合計していることを確認）
- 修正版ソースを `grep -nE "Thread\.Sleep|\.Wait\(|\.Result"` で検索 → 一致は14行目のコメント（`.Result を使わない` 理由の説明）だけで、コード上の使用はありません。

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

**原因：**
1. `Task.WhenAll(tasks);` の戻り値を await していません（警告 CS4014）。全タスクの完了を待たずに、すぐ `return count;` が実行されます。そのため、加算がほとんど進んでいない値（実測では 0）が返ります。
2. `count++` は「読み取り→加算→書き込み」の非原子的な操作です。100個のタスクが同じキャプチャ変数を同時に更新すると、更新が上書きされて失われます（競合状態）。完了を待つようにしても、100000 未満の値になります。

**修正版コード：**
```csharp
using System;
using System.Threading; // Interlocked を使うため追加
using System.Threading.Tasks;
class Program
{
    static async Task<int> CountAsync()
    {
        int count = 0; // 呼び出しごとに独立させるためローカル変数のまま
        var tasks = new Task[100];
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < 1000; j++)
                    Interlocked.Increment(ref count); // count++ は読み取り・加算・書き込みが非原子的で複数タスク間で更新が失われるため、原子的な Interlocked.Increment に変更
            });
        }
        await Task.WhenAll(tasks); // WhenAll の戻り値を await しておらず全タスク完了前に count を返していたため、await で完了を待つ
        return count;
    }
    static async Task Main() => Console.WriteLine(await CountAsync());
}
```

**確認方法・結果：** 実行済み（一時コンソールプロジェクト、net10.0）。
- 問題コード `dotnet run` → 警告 CS4014、出力: `0`
- 問題コードをハーネスで実行 → 連続10回の呼び出しはすべて `0`、同時5回の呼び出しは `0,0,0,0,0`
- 競合状態の切り分け: 問題コードの `Task.WhenAll` にだけ `await` を付けた一時コピー（`count++` はそのまま）を5回呼び出し → `27605` / `23012` / `20120` / `15513` / `15696`。完了を待っても `count++` の競合で更新が失われることを再現しました。
- 修正版 `dotnet build --no-incremental` → `0 個の警告` / `0 エラー`。`dotnet run` → 出力: `100000`
- 修正版をハーネスで実行 → 連続10回の呼び出しはすべて `OK 100000`。`Task.WhenAll` で同時に5回呼び出した結果も `OK 100000,100000,100000,100000,100000`（ローカル変数なので呼び出しごとに独立していることを確認）

---

