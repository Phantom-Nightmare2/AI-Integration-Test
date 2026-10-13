# ★HERCULES C#テスト10問

## 試験概要

対象：★HERCULES（スターエージェント）

言語：C# / .NET 8

難易度：中級～上級

問題数：全10問

満点：100点（各10点）

### 解答ルール
1. 各問題の不具合を調査すること。
2. 問題の元コードは変更せず残すこと。
3. 各問題の直下に原因、修正済みコード、検証結果を記載すること。
4. 修正後はコンパイルと動作確認を実施すること。
5. 検証できなかった場合は理由を報告すること。
6. GitHubへのコミット・プッシュは明示的な許可がある場合のみ実施すること。

各問題は独立したC#コンソールプログラムとして扱うこと。
提示されていない外部パッケージは使用しないこと。

---

## Q01：数値変換とオーバーフロー

### クライアント要望
文字列の数値を合計するプログラムです。
不正な文字列が含まれても停止せず、
正しい合計値を表示するように修正してください。

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

期待結果：2147483663

要件：
- 不正な文字列を安全に処理
- 数値のオーバーフロー防止
- 無効データを報告

### HERCULES回答欄
原因：

修正済みコード：

検証結果：

---

## Q02：請求金額の計算エラー

### クライアント要望
商品代金から10%割引し、
割引後の金額に8%の税金を加算してください。

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

期待結果：48.60

要件：
- 正確な割引率
- 正確な税計算
- decimalによる金額計算

### HERCULES回答欄
原因：

修正済みコード：

検証結果：

---

## Q03：Dictionaryのデータ削除エラー

### クライアント要望
在庫数が0以下の商品を削除してください。

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
        stock.Remove(item.Key);
    }
}

foreach (var item in stock)
{
    Console.WriteLine(
        $"{item.Key}: {item.Value}"
    );
}
```

期待結果：

A: 5

D: 2

要件：
- 安全なコレクション操作
- 条件に合う商品の削除
- 残存データの正確性

### HERCULES回答欄
原因：

修正済みコード：

検証結果：

---

## Q04：LINQ売上集計バグ

### クライアント要望
商品ごとの売上金額を集計し、
売上金額の高い順に表示してください。

```csharp
record Sale(
    string Product,
    int Quantity,
    decimal Price
);

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
```

期待結果：

A: 140

B: 120

要件：
- 数量と単価による集計
- 商品別グループ化
- 売上金額の降順ソート

### HERCULES回答欄
原因：

修正済みコード：

検証結果：

---

## Q05：日付をまたぐ時間計算

### クライアント要望
開始時刻と終了時刻から
正確な経過時間を計算してください。

```csharp
DateTime start =
    new(2026, 10, 13, 23, 50, 0);

DateTime end =
    new(2026, 10, 14, 0, 10, 0);

int minutes =
    end.Minute - start.Minute;

Console.WriteLine(minutes);
```

期待結果：20分

要件：
- 日付をまたぐ時間の計算
- 正しい経過時間
- 終了時刻が開始時刻より前の場合の検出

### HERCULES回答欄
原因：

修正済みコード：

検証結果：

---

## Q06：非同期処理の競合状態

### クライアント要望
複数のTaskから共有カウンターを更新しています。
何度実行しても正確な結果になるよう修正してください。

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

期待結果：10000

要件：
- スレッドセーフな処理
- データ競合防止
- 複数回の動作検証

### HERCULES回答欄
原因：

修正済みコード：

検証結果：

---

## Q07：非同期処理のキャンセル制御

### クライアント要望
キャンセル要求を受けたら、
非同期処理を安全に終了してください。

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

期待結果：
- キャンセル要求に応答する
- 未処理例外で終了しない
- キャンセル時にCompletedを表示しない

要件：
- CancellationTokenの適切な利用
- キャンセル例外の適切な処理
- 安全な非同期終了

### HERCULES回答欄
原因：

修正済みコード：

検証結果：

---

## Q08：JSONファイルの復旧処理

### クライアント要望
JSONファイルからスコアを取得します。
ファイルが存在しない場合や、
データが破損している場合でも
アプリが停止しないよう修正してください。

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

検証条件：

1. ファイルが存在しない
2. JSONの形式が不正
3. JSONが正常である
4. HERCULESの項目が存在しない

正常JSONの例：

```json
{
  "HERCULES": 95
}
```

期待結果：
- 正常時は95
- データ取得不能時は0
- 異常の理由を表示
- 元ファイルを勝手に破壊しない

### HERCULES回答欄
原因：

修正済みコード：

検証結果：

---

## Q09：SemaphoreSlimのデッドロック

### クライアント要望
最初の処理で例外が発生した後も、
次の処理が正常に実行できるようにしてください。

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

期待結果：
- 最初の処理は例外を報告
- 2回目の処理は正常終了
- セマフォが適切に解放される

要件：
- デッドロックの原因調査
- 例外発生時のリソース解放
- 再実行可能な処理

### HERCULES回答欄
原因：

修正済みコード：

検証結果：

---

## Q10：銀行口座の同時引き出し処理

### クライアント要望
複数の引き出し要求が同時に実行されても、
口座残高が不正にならないようにしてください。

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

初期残高：100

同時要求数：10件

引き出し金額：各30

期待結果：

Success: 3

Balance: 10

要件：
- 同時引き出しによる競合の防止
- 残高不足時の安全な拒否
- 0以下の引き出し金額の拒否
- 残高更新の一貫性
- 例外が発生しても同期制御が破綻しない設計
- 複数回の並列実行による検証

### HERCULES回答欄
原因：

修正済みコード：

検証結果：

---

# 試験終了報告

★HERCULESは全10問について、次を報告してください。

- 修正できた問題数
- 検証できた問題数
- 未解決の問題
- 未検証の理由
- 各問題の修正内容
- 全体の作業結果

最終採点は試験評価担当者が実施します。
