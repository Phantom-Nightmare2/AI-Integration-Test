# ★HERCULES C#テスト10問

## 試験概要

対象：★HERCULES（スターエージェント）

言語：C# / .NET 8

難易度：中級～上級

問題数：全10問

満点：100点（各10点）

## 解答ルール

1. 各問題の不具合を自律的に調査すること。
2. 問題の元コードは変更せず残すこと。
3. 各問題の直下にある「★HERCULES回答欄」に回答を記入すること。
4. 不具合の原因・修正済みコード・修正内容・検証結果を必ず記入すること。
5. 修正後はコンパイルと動作確認を実施すること。
6. 検証できなかった場合は、その理由を正確に報告すること。
7. 各問題を独立したC#コンソールプログラムとして検証すること。
8. .NET 8を使用し、暗黙的なusingを有効にすること。
9. 提示されていない外部パッケージは使用しないこと。
10. GitHubへのコミット・プッシュは明示的な許可がある場合のみ実施すること。

### 回答方法

各問題の「★HERCULES回答欄」に直接記入してください。

- 元コードは削除・変更しないこと。
- 修正済みコードは回答欄内のcsharpコードブロックに記入すること。
- コードの一部分ではなく、動作確認可能な完成コードを記入すること。
- 検証結果には実際の実行結果を記入すること。
- 実行していない場合は「未検証」と記載すること。
- 全10問に回答してから最終報告を記入すること。

---

# Q01：数値変換とオーバーフロー

## クライアント要望

文字列の数値を合計するプログラムです。

不正な文字列が含まれていても停止せず、正しい合計値を表示するように修正してください。

## 元コード（編集禁止）

```csharp
string[] values =
{
    "12", "-3", "abc", "2147483647", "7"
};

int total = 0;

foreach (string value in values)
{
    total += int.Parse(value);
}

Console.WriteLine(total);
```

## 期待結果

2147483663

## 要件

- 不正な文字列を安全に処理すること。
- 数値のオーバーフローを防止すること。
- 無効データを報告すること。
- 正しい合計値を表示すること。

## ★HERCULES回答欄 Q01

### 【不具合の原因】

1. `int.Parse("abc")` が `FormatException` を投げるため、不正な文字列が1つでもあるとプログラムが停止する（元コードの実行で `Unhandled exception. System.FormatException: The input string 'abc' was not in a correct format.` を確認）。
2. 合計値を `int` で保持しているため、`2147483647`（int.MaxValue）を加算すると桁あふれする。C#の既定は unchecked のため例外にならず、`-2147483633` のような誤った値に黙って回り込む。期待値 2147483663 は int の範囲を超えている。

### 【修正済みC#コード】

```csharp
using System.Globalization;

string[] values =
{
    "12", "-3", "abc", "2147483647", "7"
};

long total = 0;
var invalidValues = new List<string>();

foreach (string value in values)
{
    if (!long.TryParse(
            value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out long number))
    {
        invalidValues.Add(value);
        continue;
    }

    try
    {
        total = checked(total + number);
    }
    catch (OverflowException)
    {
        Console.WriteLine($"オーバーフローのため除外: \"{value}\"");
    }
}

foreach (string invalid in invalidValues)
{
    Console.WriteLine($"無効なデータ: \"{invalid}\"");
}

Console.WriteLine(total);
```

### 【修正内容】

- `int.Parse` を `long.TryParse`（`CultureInfo.InvariantCulture` 指定）に置き換え、変換できない値は例外にせず無効データとして記録するようにした。
- 合計値の型を `long` に変更し、int を超える合計を正しく保持できるようにした。
- 加算を `checked` で囲み、long の範囲も超える極端な入力では `OverflowException` を捕捉して、その値を除外したことを表示するようにした（回り込みによる誤った値を出さない）。
- 無効データを `無効なデータ: "abc"` の形式で報告するようにした。

### 【動作確認・検証結果】

- コンパイル結果：成功（net8.0 / ImplicitUsings有効、0エラー・0警告）
- 実行結果：
  ```text
  無効なデータ: "abc"
  2147483663
  ```
  終了コード 0
- 期待結果との比較：合計値 2147483663 が期待結果と一致。12 + (-3) + 2147483647 + 7 = 2147483663 で、"abc" は除外して報告されている。
- 補足事項：元コードで `FormatException` により停止することを修正前に確認済み。

### 【最終判定】

成功／失敗／未検証：成功

---

# Q02：請求金額の計算エラー

## クライアント要望

商品代金から10%割引し、割引後の金額に8%の税金を加算してください。

金額を正確に計算できるよう修正してください。

## 元コード（編集禁止）

```csharp
decimal price1 = 19.95m;
decimal price2 = 10.10m;

int quantity1 = 2;
int quantity2 = 1;

decimal subtotal =
    price1 * quantity1 +
    price2 * quantity2;

decimal discount = subtotal * (10 / 100);

decimal tax =
    (subtotal - discount) * (8 / 100);

decimal total =
    subtotal - discount + tax;

Console.WriteLine(total);
```

## 期待結果

48.60

## 要件

- 正確な割引率を計算すること。
- 正確な税金を計算すること。
- decimalによる金額計算を行うこと。
- 最終的な請求金額を正しく表示すること。

## ★HERCULES回答欄 Q02

### 【不具合の原因】

`10 / 100` と `8 / 100` が int 同士の整数除算になり、結果が 0 になる。そのため割引額も税額も 0 になり、元コードでは小計の `50.00` がそのまま表示される（元コードの実行で `50.00` を確認）。

### 【修正済みC#コード】

```csharp
using System.Globalization;

const decimal DiscountRate = 0.10m;
const decimal TaxRate = 0.08m;

decimal price1 = 19.95m;
decimal price2 = 10.10m;

int quantity1 = 2;
int quantity2 = 1;

decimal subtotal =
    price1 * quantity1 +
    price2 * quantity2;

decimal discount = Math.Round(
    subtotal * DiscountRate, 2, MidpointRounding.AwayFromZero);

decimal discounted = subtotal - discount;

decimal tax = Math.Round(
    discounted * TaxRate, 2, MidpointRounding.AwayFromZero);

decimal total = discounted + tax;

Console.WriteLine(total.ToString("0.00", CultureInfo.InvariantCulture));
```

### 【修正内容】

- 割引率と税率を decimal リテラルの定数 `DiscountRate = 0.10m`、`TaxRate = 0.08m` にし、整数除算をなくした。
- 割引額と税額はそれぞれ `Math.Round(..., 2, MidpointRounding.AwayFromZero)` で円・セント単位に丸めた（金額計算の慣例に合わせて四捨五入）。
- 割引後の金額（`discounted`）を変数に分け、「割引後の金額に税を加算する」という仕様をコード上で明確にした。
- 表示は `ToString("0.00", CultureInfo.InvariantCulture)` で小数2桁に固定し、カルチャによる小数点記号の違いが出ないようにした。

計算：小計 19.95×2 + 10.10 = 50.00 → 割引 5.00 → 45.00 → 税 3.60 → 合計 48.60

### 【動作確認・検証結果】

- コンパイル結果：成功（0エラー・0警告）
- 実行結果：
  ```text
  48.60
  ```
  終了コード 0
- 期待結果との比較：期待結果 48.60 と一致。
- 補足事項：元コードの出力が `50.00` であることを修正前に確認済み。

### 【最終判定】

成功／失敗／未検証：成功

---

# Q03：Dictionaryのデータ削除エラー

## クライアント要望

在庫数が0以下の商品を削除してください。

現在のプログラムでは、削除対象の商品が残ってしまいます。

正しい商品だけが残るように修正してください。

## 元コード（編集禁止）

```csharp
var stock = new Dictionary<string, int>
{
    ["A"] = 5,
    ["B"] = 0,
    ["C"] = -1,
    ["D"] = 2
};

foreach (var item in stock)
{
    if (item.Value <= 0)
    {
        stock.Remove(item.Value.ToString());
    }
}

foreach (var item in stock)
{
    Console.WriteLine(
        $"{item.Key}: {item.Value}"
    );
}
```

## 期待結果

```text
A: 5
D: 2
```

## 要件

- Dictionaryの構造を正しく理解すること。
- 削除対象を正確に判定すること。
- 安全なコレクション操作を行うこと。
- 残存データの正確性を確認すること。

## ★HERCULES回答欄 Q03

### 【不具合の原因】

1. `stock.Remove(item.Value.ToString())` が値（`"0"`、`"-1"`）をキーとして削除しようとしている。そのキーは存在しないため何も削除されない（元コードの実行で A, B, C, D がすべて残ることを確認）。
2. また、`foreach` で列挙している最中に同じ Dictionary を変更する設計は安全ではない。.NET Core 3.0 以降は列挙中の `Remove` が例外にならないが、`Add` などは `InvalidOperationException` になり、実装に依存した危険な書き方である。

### 【修正済みC#コード】

```csharp
var stock = new Dictionary<string, int>
{
    ["A"] = 5,
    ["B"] = 0,
    ["C"] = -1,
    ["D"] = 2
};

// 列挙中にDictionaryを変更しないよう、削除対象のキーを先に確定させる
List<string> keysToRemove = stock
    .Where(item => item.Value <= 0)
    .Select(item => item.Key)
    .ToList();

foreach (string key in keysToRemove)
{
    stock.Remove(key);
}

foreach (var item in stock)
{
    Console.WriteLine(
        $"{item.Key}: {item.Value}"
    );
}
```

### 【修正内容】

- 削除するキーを `item.Key` で判定し、`Where(...).Select(x => x.Key).ToList()` で先に確定させるようにした。
- 確定したキーのリストに対してループで `Remove` を行い、Dictionary を列挙しながら変更しないようにした。

### 【動作確認・検証結果】

- コンパイル結果：成功（0エラー・0警告）
- 実行結果：
  ```text
  A: 5
  D: 2
  ```
  終了コード 0
- 期待結果との比較：期待結果と完全に一致。在庫0のBと在庫-1のCが削除され、AとDが元の値のまま残っている。
- 補足事項：元コードでは `A: 5 / B: 0 / C: -1 / D: 2` が表示され、何も削除されないことを修正前に確認済み。

### 【最終判定】

成功／失敗／未検証：成功

---

# Q04：LINQ売上集計バグ

## クライアント要望

商品ごとの売上金額を集計し、売上金額の高い順に表示してください。

現在のプログラムは正しい売上金額を計算できていません。

LINQ処理を調査して修正してください。

## 元コード（編集禁止）

```csharp
var sales = new List<Sale>
{
    new("A", 2, 40m),
    new("A", 3, 20m),
    new("B", 2, 60m)
};

var result = sales
    .GroupBy(x => x.Product)
    .Select(g => new
    {
        Product = g.Key,
        Revenue = g.Count() * g.First().Price
    })
    .OrderBy(x => x.Revenue);

foreach (var item in result)
{
    Console.WriteLine(
        $"{item.Product}: {item.Revenue}"
    );
}

record Sale(
    string Product,
    int Quantity,
    decimal Price
);
```

## 期待結果

```text
A: 140
B: 120
```

## 要件

- 数量と単価による正確な売上集計を行うこと。
- 商品ごとにグループ化すること。
- 売上金額の降順に並べ替えること。
- LINQ処理の正確性を検証すること。

## ★HERCULES回答欄 Q04

### 【不具合の原因】

1. `g.Count() * g.First().Price` は「明細の件数 × 最初の明細の単価」を計算しており、数量（Quantity）を無視している。また、明細ごとに単価が違う場合に正しい値にならない（A = 2件 × 40 = 80 となる）。
2. `OrderBy` は昇順のため、売上の高い順という要件と逆になっている。

元コードの実行結果は `B: 60 / A: 80` で、金額も順序も誤っていることを確認した。

### 【修正済みC#コード】

```csharp
var sales = new List<Sale>
{
    new("A", 2, 40m),
    new("A", 3, 20m),
    new("B", 2, 60m)
};

var result = sales
    .GroupBy(x => x.Product)
    .Select(g => new
    {
        Product = g.Key,
        Revenue = g.Sum(x => x.Quantity * x.Price)
    })
    .OrderByDescending(x => x.Revenue)
    .ThenBy(x => x.Product);

foreach (var item in result)
{
    Console.WriteLine(
        $"{item.Product}: {item.Revenue}"
    );
}

record Sale(
    string Product,
    int Quantity,
    decimal Price
);
```

### 【修正内容】

- 売上を `g.Sum(x => x.Quantity * x.Price)` に変更し、明細ごとの「数量×単価」の合計にした（A = 2×40 + 3×20 = 140、B = 2×60 = 120）。
- `OrderBy` を `OrderByDescending` に変更して売上の降順にした。
- 売上が同額のときも順序が一定になるよう `ThenBy(x => x.Product)` を追加した。

### 【動作確認・検証結果】

- コンパイル結果：成功（0エラー・0警告）
- 実行結果：
  ```text
  A: 140
  B: 120
  ```
  終了コード 0
- 期待結果との比較：期待結果と完全に一致（金額・順序とも）。
- 補足事項：元コードの出力が `B: 60 / A: 80` であることを修正前に確認済み。

### 【最終判定】

成功／失敗／未検証：成功

---

# Q05：日付をまたぐ時間計算

## クライアント要望

開始時刻と終了時刻から正確な経過時間を計算してください。

日付をまたぐ場合も正確に計算できるよう修正してください。

## 元コード（編集禁止）

```csharp
DateTime start =
    new(2026, 10, 13, 23, 50, 0);

DateTime end =
    new(2026, 10, 14, 0, 10, 0);

int minutes =
    end.Minute - start.Minute;

Console.WriteLine(minutes);
```

## 期待結果

20分

## 要件

- 日付をまたぐ時間計算に対応すること。
- 正確な経過時間を計算すること。
- 終了時刻が開始時刻より前の場合を検出すること。
- 正しい計算結果を表示すること。

## ★HERCULES回答欄 Q05

### 【不具合の原因】

`end.Minute - start.Minute` は時刻のうち「分」の部分だけを引いているため、日付と時間（時）の差を無視している。23:50 → 翌0:10 の場合 `10 - 50 = -40` となる（元コードの実行で `-40` を確認）。また、終了時刻が開始時刻より前になっていないかの確認も行っていない。

### 【修正済みC#コード】

```csharp
DateTime start =
    new(2026, 10, 13, 23, 50, 0);

DateTime end =
    new(2026, 10, 14, 0, 10, 0);

PrintElapsed(start, end);

// 終了時刻が開始時刻より前の場合の検出確認（開始と終了を入れ替え）
PrintElapsed(end, start);

static void PrintElapsed(DateTime start, DateTime end)
{
    if (end < start)
    {
        Console.WriteLine(
            $"エラー: 終了時刻({end:yyyy-MM-dd HH:mm})が開始時刻({start:yyyy-MM-dd HH:mm})より前です。");
        return;
    }

    TimeSpan elapsed = end - start;
    Console.WriteLine($"{(long)elapsed.TotalMinutes}分");
}
```

### 【修正内容】

- `end - start` で `TimeSpan` を求め、`TotalMinutes` で日付をまたいでも正しい経過分数を算出するようにした。
- `end < start` の場合はエラーメッセージを表示して計算しないようにした。
- 単位付きの「20分」の形式で表示するようにした。
- 異常系を確認するため、開始と終了を入れ替えた呼び出しも同じプログラムで実行している。

### 【動作確認・検証結果】

- コンパイル結果：成功（0エラー・0警告）
- 実行結果：
  ```text
  20分
  エラー: 終了時刻(2026-10-13 23:50)が開始時刻(2026-10-14 00:10)より前です。
  ```
  終了コード 0
- 期待結果との比較：正常ケースの出力「20分」が期待結果と一致。逆転ケースも検出してエラーを表示した。
- 補足事項：`DateTime` の Kind は Unspecified のまま（元コードと同じ）。夏時間をまたぐ実時間の計算が必要な場合は `DateTimeOffset` またはUTCで扱う必要がある（本問の日本時間の条件では影響しない）。

### 【最終判定】

成功／失敗／未検証：成功

---

# Q06：非同期処理の競合状態

## クライアント要望

複数のTaskから共有カウンターを更新しています。

何度実行しても正確な結果になるよう修正してください。

## 元コード（編集禁止）

```csharp
int counter = 0;

var tasks = Enumerable
    .Range(0, 10000)
    .Select(_ => Task.Run(() =>
    {
        counter++;
    }));

await Task.WhenAll(tasks);

Console.WriteLine(counter);
```

## 期待結果

10000

## 要件

- スレッドセーフな処理を実装すること。
- データ競合を防止すること。
- 正確なカウント処理を実現すること。
- 複数回の動作検証を実施すること。

## ★HERCULES回答欄 Q06

### 【不具合の原因】

`counter++` は「読み取り → 加算 → 書き込み」の3段階からなる非アトミック操作である。複数のスレッドプールスレッドが同時に実行すると更新が失われる（lost update）。元コードの実行では `9238` となり、10000 に届かないことを確認した。

### 【修正済みC#コード】

```csharp
int counter = 0;

var tasks = Enumerable
    .Range(0, 10000)
    .Select(_ => Task.Run(() =>
    {
        Interlocked.Increment(ref counter);
    }));

await Task.WhenAll(tasks);

Console.WriteLine(Volatile.Read(ref counter));
```

### 【修正内容】

- `counter++` を `Interlocked.Increment(ref counter)` に置き換え、加算をアトミックに行うようにした。
- 結果の読み取りは `Volatile.Read` で行い、最新値を確実に読むようにした（`Task.WhenAll` の完了後なので実質的に安全だが、意図を明示した）。
- `lock` よりも軽量で、単純なカウンターにはこれが最適な方法である。

### 【動作確認・検証結果】

- コンパイル結果：成功（0エラー・0警告）
- 実行結果：
  ```text
  10000
  ```
  終了コード 0
- 複数回の検証結果：プロセスを個別に20回起動して実行し、20回すべてで `10000` を出力（`uniq -c` 集計：`20 10000`）。
- 期待結果との比較：期待結果 10000 と全回で一致。

### 【最終判定】

成功／失敗／未検証：成功

---

# Q07：非同期処理のキャンセル制御

## クライアント要望

キャンセル要求を受けたら、非同期処理を安全に終了してください。

現在のプログラムではキャンセル要求に対応していません。

## 元コード（編集禁止）

```csharp
using var cts =
    new CancellationTokenSource(
        TimeSpan.FromMilliseconds(250)
    );

await ExecuteAsync(cts.Token);

Console.WriteLine("Completed");

static async Task ExecuteAsync(
    CancellationToken token)
{
    for (int i = 0; i < 100; i++)
    {
        await Task.Delay(100);
        Console.WriteLine(i);
    }
}
```

## 期待結果

- キャンセル要求に応答すること。
- 未処理例外で終了しないこと。
- キャンセル時にCompletedを表示しないこと。

## 要件

- CancellationTokenを適切に利用すること。
- キャンセル例外を適切に処理すること。
- 安全な非同期終了を実現すること。
- 実際にキャンセル処理を検証すること。

## ★HERCULES回答欄 Q07

### 【不具合の原因】

`ExecuteAsync` は `token` を引数として受け取っているが、まったく使っていない。`Task.Delay(100)` にもトークンを渡していないため、250ms でキャンセルが要求されても無視され、ループが100回（約10秒）すべて実行されてから `Completed` が表示される。元コードの実行で、250ms を過ぎても 0, 1, 2, 3 … と出力が続くことを確認した。

### 【修正済みC#コード】

```csharp
// 引数でキャンセルまでの時間(ミリ秒)を変更可能。既定は元コードと同じ250ms
int timeoutMs = args.Length > 0 && int.TryParse(args[0], out int ms) ? ms : 250;

using var cts =
    new CancellationTokenSource(
        TimeSpan.FromMilliseconds(timeoutMs)
    );

try
{
    await ExecuteAsync(cts.Token);
    Console.WriteLine("Completed");
}
catch (OperationCanceledException) when (cts.IsCancellationRequested)
{
    Console.WriteLine("Cancelled: 処理はキャンセルされました。");
}

static async Task ExecuteAsync(
    CancellationToken token)
{
    for (int i = 0; i < 100; i++)
    {
        token.ThrowIfCancellationRequested();
        await Task.Delay(100, token);
        Console.WriteLine(i);
    }
}
```

### 【修正内容】

- `Task.Delay(100, token)` にトークンを渡し、待機中でもキャンセルにすぐ反応するようにした。
- ループの先頭で `token.ThrowIfCancellationRequested()` を呼び、各回の開始前にもキャンセルを確認するようにした。
- 呼び出し側で `OperationCanceledException` を捕捉し（例外フィルター `when (cts.IsCancellationRequested)` で、自分のトークンによるキャンセルだけを対象にした）、未処理例外にならずに `Cancelled` を表示して正常終了するようにした。
- `Completed` は `try` 内で処理が最後まで完了したときだけ表示されるため、キャンセル時には表示されない。
- 検証のため、コマンドライン引数でキャンセルまでの時間を変えられるようにした（既定は元コードと同じ250ms）。

### 【動作確認・検証結果】

- コンパイル結果：成功（0エラー・0警告）
- 実行結果（既定 250ms）：
  ```text
  0
  1
  Cancelled: 処理はキャンセルされました。
  ```
  終了コード 0
- キャンセル検証結果：
  - 250ms 設定で5回実行し、5回とも `0 1 Cancelled` で停止した。終了コードは0で、`Completed` は表示されなかった。100ms 間隔の2回の出力の後、3回目の Delay 中（約250ms）にキャンセルされており、タイミングどおりである。
  - 対照試験として、キャンセルまでの時間を60000msにして実行し、`98 / 99 / Completed` と最後まで完了して `Completed` が表示されることも確認した（正常経路も壊れていない）。
- 補足事項：未処理例外は発生していない。

### 【最終判定】

成功／失敗／未検証：成功

---

# Q08：JSONファイルの復旧処理

## クライアント要望

JSONファイルからスコアを取得するプログラムです。

ファイルが存在しない場合や、データが破損している場合でもアプリが停止しないように修正してください。

## 元コード（編集禁止）

```csharp
using System.Text.Json;

string path = Path.Combine(
    Path.GetTempPath(),
    "hercules-score-test.json"
);

string json = File.ReadAllText(path);

var scores =
    JsonSerializer.Deserialize<
        Dictionary<string, int>
    >(json)!;

Console.WriteLine(scores["HERCULES"]);
```

## 検証条件

1. ファイルが存在しない。
2. JSONの形式が不正である。
3. JSONが正常である。
4. HERCULESの項目が存在しない。

## 正常JSONの例

```json
{
  "HERCULES": 95
}
```

## 期待結果

- 正常時は95を表示すること。
- データ取得不能時は0を表示すること。
- 異常の理由を表示すること。
- 元ファイルを勝手に破壊しないこと。

## 要件

- ファイルの存在確認を行うこと。
- JSONデータの正確性を検証すること。
- 例外処理を適切に実装すること。
- 必要な検証条件をすべて確認すること。

## ★HERCULES回答欄 Q08

### 【不具合の原因】

1. ファイルの存在を確認せずに `File.ReadAllText` を呼んでいるため、ファイルがないと `FileNotFoundException` で停止する（元コードの実行で確認）。
2. `JsonSerializer.Deserialize` の `JsonException`（形式不正・型不一致）を処理していない。
3. `!`（null免除演算子）で null を握りつぶしているため、JSONが `null` のときに `NullReferenceException` になる。
4. `scores["HERCULES"]` はキーがないと `KeyNotFoundException` になる。

### 【修正済みC#コード】

```csharp
using System.Text.Json;

// 引数でパスを指定可能（検証用）。省略時は元コードと同じパスを読む
string path = args.Length > 0
    ? args[0]
    : Path.Combine(
        Path.GetTempPath(),
        "hercules-score-test.json"
    );

int score = LoadScore(path, "HERCULES", out string? error);

if (error is not null)
{
    Console.WriteLine($"エラー: {error}");
}

Console.WriteLine(score);

// ファイルは読み取りのみ行い、書き込み・削除は一切しない
static int LoadScore(string path, string key, out string? error)
{
    error = null;

    if (!File.Exists(path))
    {
        error = $"ファイルが存在しません: {path}";
        return 0;
    }

    string json;
    try
    {
        json = File.ReadAllText(path);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        error = $"ファイルを読み込めません: {ex.Message}";
        return 0;
    }

    Dictionary<string, int>? scores;
    try
    {
        scores = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
    }
    catch (JsonException ex)
    {
        error = $"JSONの形式が不正です: {ex.Message}";
        return 0;
    }

    if (scores is null)
    {
        error = "JSONの内容がnullです。";
        return 0;
    }

    if (!scores.TryGetValue(key, out int value))
    {
        error = $"項目 \"{key}\" が存在しません。";
        return 0;
    }

    return value;
}
```

### 【修正内容】

- 読み込み処理を `LoadScore` メソッドに分け、異常時は0を返し、理由を `error` で返すようにした。
- 次の異常をそれぞれ検出し、理由を表示するようにした。
  - ファイルの存在確認（`File.Exists`）
  - 読み込み時の `IOException` / `UnauthorizedAccessException`
  - JSONの形式不正・型不一致（`JsonException`）
  - JSONが `null` の場合
  - キーがない場合（`TryGetValue`）
- ファイルは読み取りだけを行い、作成・上書き・削除は一切しない（元ファイルを破壊しない）。
- 検証用に、コマンドライン引数で読み込むパスを指定できるようにした。引数を省略した場合は元コードと同じ `%TEMP%\hercules-score-test.json` を読む。

### 【動作確認・検証結果】

- コンパイル結果：成功（0エラー・0警告）
- ファイル不存在時：既定パス（`%TEMP%\hercules-score-test.json`、実際に存在しないことを `Test-Path` = False で確認）で実行し、`エラー: ファイルが存在しません: C:\Users\user\AppData\Local\Temp\hercules-score-test.json` の後に `0` を表示、終了コード 0。指定パスの存在しないファイルでも同様だった。
- JSON不正時：`{ "HERCULES": 95, ` で実行し、`エラー: JSONの形式が不正です: Expected start of a property name or value, but instead reached end of data. ...` の後に `0` を表示、終了コード 0。
- 正常JSON時：`{ "HERCULES": 95 }` で実行し、`95` を表示、終了コード 0。
- 項目不存在時：`{ "ATHENA": 80 }` で実行し、`エラー: 項目 "HERCULES" が存在しません。` の後に `0` を表示、終了コード 0。
- 総合結果：4条件すべてで期待どおりに動作した。追加で `null` のJSON（`エラー: JSONの内容がnullです。` → 0）と型不一致の `{ "HERCULES": "95" }`（`JSONの形式が不正です: ...could not be converted to System.Int32` → 0）も確認した。実行前後で全テストファイルの SHA256 ハッシュが一致し（ファイル無変更 = True）、存在しないファイルが作られていないことも確認した。テストファイルは作業用ディレクトリに作成し、`%TEMP%\hercules-score-test.json` は作成・変更していない。

### 【最終判定】

成功／失敗／未検証：成功

---

# Q09：SemaphoreSlimのデッドロック

## クライアント要望

最初の処理で例外が発生した後も、次の処理が正常に実行できるようにしてください。

現在のプログラムでは処理が正常に進まなくなる場合があります。

## 元コード（編集禁止）

```csharp
var runner = new JobRunner();

try
{
    await runner.RunAsync(true);
}
catch (InvalidOperationException)
{
    Console.WriteLine("First job failed");
}

await runner.RunAsync(false);
Console.WriteLine("Second job completed");

class JobRunner
{
    private readonly SemaphoreSlim gate =
        new(1, 1);

    public async Task RunAsync(bool fail)
    {
        await gate.WaitAsync();

        if (fail)
        {
            throw new InvalidOperationException(
                "Job failed"
            );
        }

        await Task.Delay(10);

        gate.Release();
    }
}
```

## 期待結果

- 最初の処理は例外を報告すること。
- 2回目の処理は正常終了すること。
- セマフォが適切に解放されること。

## 要件

- デッドロックの原因を調査すること。
- 例外発生時のリソース解放を実装すること。
- 再実行可能な処理に修正すること。
- 処理停止が発生しないことを確認すること。

## ★HERCULES回答欄 Q09

### 【不具合の原因】

`gate.WaitAsync()` でセマフォを取得した後、`fail == true` のときに例外が発生すると `gate.Release()` に到達しない。セマフォ（最大1）が取得されたまま残るため、2回目の `RunAsync(false)` が `WaitAsync()` で永久に待ち続ける（デッドロック）。元コードを15秒のタイムアウト付きで実行し、`First job failed` の表示後に停止し、タイムアウトで強制終了されること（exit=124）を確認した。

### 【修正済みC#コード】

```csharp
var runner = new JobRunner();

try
{
    await runner.RunAsync(true);
}
catch (InvalidOperationException)
{
    Console.WriteLine("First job failed");
}

await runner.RunAsync(false);
Console.WriteLine("Second job completed");
Console.WriteLine($"Semaphore available: {runner.AvailableCount}");

class JobRunner
{
    private readonly SemaphoreSlim gate =
        new(1, 1);

    public int AvailableCount => gate.CurrentCount;

    public async Task RunAsync(bool fail)
    {
        await gate.WaitAsync();

        try
        {
            if (fail)
            {
                throw new InvalidOperationException(
                    "Job failed"
                );
            }

            await Task.Delay(10);
        }
        finally
        {
            // 例外の有無に関わらず必ず解放する
            gate.Release();
        }
    }
}
```

### 【修正内容】

- セマフォを取得した直後から処理全体を `try { ... } finally { gate.Release(); }` で囲み、例外が発生しても必ず解放されるようにした。
- `WaitAsync()` は `try` の外に置いた。取得に失敗した（キャンセルされた）場合に、取得していないセマフォを `Release` して `SemaphoreFullException` になるのを防ぐためである。
- 解放されたことを確認するため、`AvailableCount`（`gate.CurrentCount`）を公開して最後に表示するようにした。

### 【動作確認・検証結果】

- コンパイル結果：成功（0エラー・0警告）
- 1回目の処理結果：`First job failed`（例外を捕捉して報告）
- 2回目の処理結果：`Second job completed`（ハングせずに正常終了、終了コード 0）
- リソース解放の確認：最後に `Semaphore available: 1` と表示され、セマフォが初期状態（空き1）に戻っていることを確認した。
- 補足事項：5回実行し、5回とも `First job failed / Second job completed / Semaphore available: 1` で、すべて正常終了した。

### 【最終判定】

成功／失敗／未検証：成功

---

# Q10：銀行口座の同時引き出し処理

## クライアント要望

複数の引き出し要求が同時に実行されても、口座残高が不正にならないようにしてください。

現在のプログラムでは、同時処理によって残高が正しく管理されない可能性があります。

## 元コード（編集禁止）

```csharp
var account = new BankAccount();

var tasks = Enumerable
    .Range(0, 10)
    .Select(_ =>
        account.WithdrawAsync(30m)
    );

bool[] results =
    await Task.WhenAll(tasks);

Console.WriteLine(
    $"Success: {results.Count(x => x)}"
);

Console.WriteLine(
    $"Balance: {account.Balance}"
);

class BankAccount
{
    public decimal Balance { get; private set; }
        = 100m;

    public async Task<bool> WithdrawAsync(
        decimal amount)
    {
        if (Balance >= amount)
        {
            await Task.Delay(30);

            Balance -= amount;

            return true;
        }

        return false;
    }
}
```

## テスト条件

初期残高：100

同時要求数：10件

引き出し金額：各30

## 期待結果

```text
Success: 3
Balance: 10
```

## 要件

- 同時引き出しによる競合を防止すること。
- 残高不足時は安全に拒否すること。
- 0以下の引き出し金額を拒否すること。
- 残高更新の一貫性を保つこと。
- 例外が発生しても同期制御が破綻しないこと。
- 複数回の並列実行で検証すること。
- 不正な残高にならないことを確認すること。

## ★HERCULES回答欄 Q10

### 【不具合の原因】

1. 「残高の確認（`Balance >= amount`）」と「残高の更新（`Balance -= amount`）」の間に `await Task.Delay(30)` があり、この2つが排他されていない（check-then-act の競合）。10件すべてが残高100を確認した後に減算するため、全件が成功してしまう。元コードの実行で `Success: 10 / Balance: -200` を確認した。
2. `Balance -= amount` 自体も非アトミックである。
3. 0以下の金額を拒否していないため、負の金額で残高を増やすことができてしまう。

### 【修正済みC#コード】

```csharp
var account = new BankAccount();

var tasks = Enumerable
    .Range(0, 10)
    .Select(_ =>
        account.WithdrawAsync(30m)
    );

bool[] results =
    await Task.WhenAll(tasks);

Console.WriteLine(
    $"Success: {results.Count(x => x)}"
);

Console.WriteLine(
    $"Balance: {account.Balance}"
);

// 「--verify」指定時のみ追加検証を行う
if (args.Contains("--verify"))
{
    await Verification.RunAsync();
}

class BankAccount
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private decimal balance = 100m;

    public decimal Balance
    {
        get
        {
            gate.Wait();
            try
            {
                return balance;
            }
            finally
            {
                gate.Release();
            }
        }
    }

    // 検証用フック：残高更新の直前に呼ばれる（例外注入に使用）
    internal Func<Task>? BeforeCommit { get; set; }

    public async Task<bool> WithdrawAsync(
        decimal amount)
    {
        if (amount <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount), amount,
                "引き出し金額は0より大きい必要があります。");
        }

        await gate.WaitAsync();
        try
        {
            if (balance < amount)
            {
                return false;
            }

            await Task.Delay(30);

            if (BeforeCommit is not null)
            {
                await BeforeCommit();
            }

            balance -= amount;
            return true;
        }
        finally
        {
            gate.Release();
        }
    }
}

static class Verification
{
    public static async Task RunAsync()
    {
        Console.WriteLine("--- 不正金額の検証 ---");
        var acc = new BankAccount();
        foreach (decimal bad in new[] { 0m, -10m })
        {
            try
            {
                await acc.WithdrawAsync(bad);
                Console.WriteLine($"{bad}: 受け付けてしまった(NG)");
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine($"{bad}: 拒否(OK)");
            }
        }
        Console.WriteLine($"残高: {acc.Balance} (100ならOK)");

        Console.WriteLine("--- 例外発生時の同期制御の検証 ---");
        var acc2 = new BankAccount();
        int calls = 0;
        acc2.BeforeCommit = () =>
            Interlocked.Increment(ref calls) == 1
                ? throw new InvalidOperationException("模擬障害")
                : Task.CompletedTask;
        try
        {
            await acc2.WithdrawAsync(30m);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"1件目: 例外「{ex.Message}」発生 / 残高={acc2.Balance} (100ならOK)");
        }
        bool[] follow = await Task.WhenAll(
                Enumerable.Range(0, 10).Select(_ => acc2.WithdrawAsync(30m)))
            .WaitAsync(TimeSpan.FromSeconds(5));
        Console.WriteLine(
            $"例外後の並列10件: Success={follow.Count(x => x)}, Balance={acc2.Balance} (3件/10ならOK)");

        Console.WriteLine("--- 繰り返し並列実行(50回) ---");
        int ok = 0;
        decimal minBalance = decimal.MaxValue;
        for (int run = 0; run < 50; run++)
        {
            var a = new BankAccount();
            bool[] r = await Task.WhenAll(
                Enumerable.Range(0, 10).Select(_ => a.WithdrawAsync(30m)));
            minBalance = Math.Min(minBalance, a.Balance);
            if (r.Count(x => x) == 3 && a.Balance == 10m) ok++;
        }
        Console.WriteLine($"50回中 {ok} 回で Success=3 / Balance=10、最小残高={minBalance}");
    }
}
```

### 【修正内容】

- `SemaphoreSlim(1, 1)` を導入し、残高確認から残高更新までを1つのクリティカルセクションにした。`lock` の中では `await` できないため、非同期対応の `SemaphoreSlim` を使った。
- `try/finally` で必ず `Release` するようにし、処理中に例外が発生しても同期制御が壊れないようにした（Q09と同じ対策）。
- `amount <= 0` の場合は、ロックを取得する前に `ArgumentOutOfRangeException` で拒否するようにした。
- 残高は private フィールドで保持し、`Balance` の読み取りも同じセマフォで保護した（decimal の読み取りはアトミックではないため）。
- 例外発生時の検証のため、残高更新の直前に呼ばれる internal の検証用フック `BeforeCommit` を追加した。通常の実行では null で、動作に影響しない。
- `--verify` 引数を付けたときだけ、追加検証（不正金額、例外注入、50回の繰り返し）を実行するようにした。引数なしの出力は期待結果と同じ2行のみである。

### 【動作確認・検証結果】

- コンパイル結果：成功（0エラー・0警告）
- 引き出し成功件数：`Success: 3`
- 最終残高：`Balance: 10`
- 不正金額の検証結果：`0` → 拒否（OK）、`-10` → 拒否（OK）、検証後の残高は 100 のまま（変化なし）。
- 複数回の並列実行結果：プロセスを個別に10回起動し、10回とも `Success: 3 / Balance: 10` だった。さらに `--verify` 内で10並列の引き出しを50回繰り返し、50回中50回が `Success=3 / Balance=10`、最小残高は 10（負の残高は一度も発生していない）。
- 同期制御の検証結果：1件目の引き出しで残高更新の直前に例外（模擬障害）を発生させたところ、残高は100のまま変化しなかった。その後の10並列の引き出しはハングせずに `Success=3, Balance=10` となり、例外の後もセマフォが正しく解放されていることを確認した（5秒のタイムアウト付きで待機し、タイムアウトは発生しなかった）。

### 【最終判定】

成功／失敗／未検証：成功

---

# ★HERCULES 試験終了報告

## 総合結果

回答した問題数：10

修正できた問題数：10

コンパイル成功数：10

動作検証成功数：10

未解決の問題数：0

未検証の問題数：0

## 問題別結果

| 問題 | 最終判定 | 検証状況 |
|---|---|---|
| Q01 | 成功 | 実行確認済み（合計 2147483663、"abc" を無効データとして報告） |
| Q02 | 成功 | 実行確認済み（48.60） |
| Q03 | 成功 | 実行確認済み（A: 5 / D: 2） |
| Q04 | 成功 | 実行確認済み（A: 140 / B: 120、降順） |
| Q05 | 成功 | 実行確認済み（20分、時刻の逆転も検出） |
| Q06 | 成功 | 20回の個別実行がすべて 10000 |
| Q07 | 成功 | 250msでキャンセル×5回、正常完了の対照試験も実施 |
| Q08 | 成功 | 4条件と追加2条件を実行、ファイル無変更をハッシュで確認 |
| Q09 | 成功 | 5回実行、2回目の処理が完了しセマフォの空きが1に戻る |
| Q10 | 成功 | 10回の個別実行と50回の繰り返しがすべて Success 3 / Balance 10、不正金額の拒否と例外注入も確認 |

## 未解決の問題

なし。

## 未検証の理由

未検証の項目はない。

検証環境について：インストールされているSDKは .NET SDK 10.0.401 だけだったため、各プロジェクトを `TargetFramework=net8.0`、`ImplicitUsings=enable` で作成し、.NET 8 ランタイム（Microsoft.NETCore.App 8.0.31）で実行した。生成物は `bin/Release/net8.0/` で、実行時のランタイムは .NET 8 である。C#の言語バージョンは net8.0 の既定値（C# 12）が適用される。外部パッケージは使っていない。

## 各問題の修正内容まとめ

- Q01：`long.TryParse` と `checked` 加算に変更し、無効データを報告するようにした。int の桁あふれと `FormatException` を解消した。
- Q02：整数除算（`10 / 100` = 0）を decimal の率（`0.10m`、`0.08m`）に変更し、2桁で四捨五入した。
- Q03：値ではなくキーで削除するように変更し、削除対象を先に確定してから削除するようにした。
- Q04：売上を `Count × First.Price` から `Sum(数量×単価)` に変更し、`OrderByDescending` で並べるようにした。
- Q05：`Minute` 同士の引き算を `TimeSpan.TotalMinutes` に変更し、時刻の逆転を検出するようにした。
- Q06：`counter++` を `Interlocked.Increment` に変更した。
- Q07：トークンを `Task.Delay` に伝え、ループでもキャンセルを確認するようにした。`OperationCanceledException` を捕捉し、キャンセル時は `Completed` を表示しない。
- Q08：ファイルの存在、I/Oエラー、JSON不正、null、キーの欠落を個別に処理し、理由を表示して0を返すようにした。読み取りのみ行う。
- Q09：`try/finally` で `SemaphoreSlim.Release` を必ず実行するようにした。
- Q10：残高の確認から更新までを `SemaphoreSlim` で排他し、`try/finally` で解放するようにした。0以下の金額は拒否し、残高の読み取りも保護した。

## 全体の作業結果

1. 各問題の元コードを Markdown から機械的に抽出し、元コード用の独立した net8.0 コンソールプロジェクト（10個）でビルド・実行した。全問題の不具合を再現した（Q01 FormatException、Q02 50.00、Q03 削除されない、Q04 B: 60 / A: 80、Q05 -40、Q06 9238、Q07 キャンセルが無視される、Q08 FileNotFoundException、Q09 ハングしてタイムアウト、Q10 Success: 10 / Balance: -200）。
2. 修正版用の独立した net8.0 コンソールプロジェクト（10個）を作成し、Releaseビルドで全問題が0エラー・0警告だった。
3. 各問題の期待結果と実際の出力を照合した。並行処理の問題（Q06、Q07、Q09、Q10）は複数回実行した。
4. 回答欄に記載したコードは、検証に使用した Program.cs をそのまま転記したものである。元コードのブロックは変更していない。
5. 作業用のプロジェクトとテスト用JSONは、リポジトリ外の一時作業ディレクトリに作成した。リポジトリ内で変更したのはこのMarkdownファイルだけである。GitHubへのコミット・プッシュは行っていない。

## ★HERCULES 最終報告

**実際に確認できた成果**

- 全10問について、元コードの不具合を実行して再現した。修正後のコードは net8.0 でビルドが成功し（0エラー・0警告）、期待結果と一致する出力を実際の実行で確認した。
- 並行処理の問題は複数回実行して確認した：Q06 は20回中20回、Q07 は5回中5回、Q09 は5回中5回、Q10 は10回の個別実行と50回の繰り返しのすべてで正しい結果だった。
- Q08 は指定された4条件すべてに加え、null と型不一致の2条件でも停止しないことを確認した。テストファイルが変更されていないことはハッシュで確認した。
- Q10 は不正金額（0、-10）の拒否と、例外を注入した後も同期制御が壊れないことを実際に確認した。

**残る問題・注意事項**

- Q01：通常の入力では起きないが、long の範囲も超える合計になる場合は、その値を除外して報告する方針にしている。任意精度が必要な場合は `BigInteger` への変更が必要になる。
- Q02：割引額と税額は、要素ごとに小数2桁で四捨五入（AwayFromZero）している。端数処理の規則（切り捨て・銀行丸めなど）がクライアントの規定と異なる場合は、変更が必要になる。
- Q05：夏時間をまたぐ実時間の計算が必要になる場合は、`DateTimeOffset` やUTCを使う必要がある。
- Q06・Q10：並行処理の正しさは実行回数だけでは完全に証明できない。ただし Interlocked と SemaphoreSlim による排他は、設計の上で競合をなくしている。

**検証できなかった事項**

- なし。ただし .NET 8 SDK そのものではなく、.NET SDK 10.0.401 で net8.0 向けにビルドし、.NET 8.0.31 ランタイムで実行した点を明記する。

---

# 採点について

満点：100点

各問題：10点

採点は提出された回答内容と検証結果をもとに実施します。

自己申告による成功判定のみでは合格としません。

実際の修正コードと検証内容を確認し、最終採点を行います。

## 評価担当

Phantom Nightmare / ChatGPT（Watson）

## 試験対象

★HERCULES

スターエージェント C#総合能力試験

---

# 終了

全10問の回答欄と最終報告欄を記入して、試験終了とします。
