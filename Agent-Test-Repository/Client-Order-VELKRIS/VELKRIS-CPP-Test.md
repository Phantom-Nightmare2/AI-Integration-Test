# Client Order VELKRIS — C++ Ability Test (10 Questions)

> **利用条件：All Rights Reserved — Copyright (c) 2026 Phantom Nightmare.** 本問題集はオリジナルの能力テストです。法令・GitHub利用規約で認められる場合を除き、無断転載・再配布・改変・他のテストへの転用には所有者の事前許可が必要です。所有者が指示するVELKRISのテスト実施と回答記入は許可します。[利用条件全文](../../LICENSE.md)

## クライアントオーダー / Client Order

- **クライアント：** ワトソン（架空のクライアント）
- **依頼先：** VELKRIS
- **対象：** C++のコンパイルエラー・ロジックバグ・安全性に関する能力テスト10問
- **難易度：** Q01からQ10に向けて段階的に上昇
- **実行環境：** C++17準拠。Visual Studio/MSVC、またはC++17対応コンパイラー。標準ライブラリのみ使用

### 依頼内容
Q01からQ10まで順番に、各問題のエラーやバグを分析し、要求仕様を満たす修正版コード全文、変更理由、検証方法と結果を指定の回答欄に記入してください。コードの調査・エラー修正・動作検証は可能な範囲で自律的に実施してください。

### 必須ルール
1. 各問題の「要求仕様」と「問題コード」は**編集・削除禁止**。変更できるのは、このファイルの各「VELKRISの回答」欄だけです。
2. 各回答欄に **原因／修正版コード全文／確認方法・結果** を記入してください。
3. 修正版コードは `cpp` コードブロックに記載し、修正箇所に `//` コメントで修正理由を書いてください。
4. 各問題は**独立したプログラム**です。問題ごとに独立した一時プロジェクトで検証し、その検証用ファイルはGitHubに提出しないでください。
5. 必ず期待値と実際の結果を区別し、未実行なら**未実行と理由・再現手順・予想結果**を明記してください。コンパイル不可や未定義動作を含む元コードの無理な実行は避けてください。
6. 提出時にはこのファイルの回答欄**以外を変更しないこと**。C#テスト、README、LICENSE、エージェント設定、認証情報、APIキーなどは変更・公開しないでください。
7. 全10問を解いたら回答を確認してローカルでコミットし、コミットSHAと回答数を報告してください。**GitHubへのpushは所有者から明示的に指示された場合にのみ行うこと**。クローンやコミットで自動的にpushされるわけではありません。
8. VELKRIS自身による得点・合否判定は禁止です。最終採点はクライアント（レビュー担当）が行います。

### 採点（100点満点）
各問10点（**原因の特定2点／要求仕様を満たす修正版6点／確認方法・結果2点**）。回答の正確性と要求仕様への適合を評価します。実際に実行できなかった場合も、未実行である旨と再現可能な確認手順・予想結果が適切なら確認項目を評価します。

---

## Q01 — 基本文法とビルド（初級）

### 要求仕様
整数10と5の合計を1行に表示してください。期待する表示は `15` です。

### 問題コード（変更禁止）
```cpp
#include <iostream>

int main()
{
    int first = 10;
    int second = 5
    std::cout << first + second << '\n';
    return 0;
}
```

### VELKRISの回答
**原因：** 未回答

**修正版コード：**
```cpp
// ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

---

## Q02 — 参照渡し（初級）

### 要求仕様
関数 `Increase` は呼び出し元の整数を1増やすものとします。8は9に、0は1になります。mainの表示は `9,1` としてください。

### 問題コード（変更禁止）
```cpp
#include <iostream>

void Increase(int value)
{
    value += 1;
}

int main()
{
    int first = 8;
    int second = 0;
    Increase(first);
    Increase(second);
    std::cout << first << "," << second << '\n';
    return 0;
}
```

### VELKRISの回答
**原因：** 未回答

**修正版コード：**
```cpp
// ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

---

## Q03 — 整数除算と例外（初級〜中級）

### 要求仕様
`Average` は整数配列の平均を小数まで正しく計算し、`{1,2}` → `1.5`、`{2,4,6}` → `4.0` を返してください。空配列は `std::invalid_argument` を送出してください。入力の合計は`int`の範囲内とします。

### 問題コード（変更禁止）
```cpp
#include <iostream>
#include <vector>
#include <numeric>
#include <stdexcept>

double Average(const std::vector<int>& values)
{
    int total = std::accumulate(values.begin(), values.end(), 0);
    return total / values.size();
}

int main()
{
    std::cout << Average({1, 2}) << '\n';
    return 0;
}
```

### VELKRISの回答
**原因：** 未回答

**修正版コード：**
```cpp
// ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

---

## Q04 — vectorの境界と空入力（中級）

### 要求仕様
`Sum` はすべての整数を合計して返してください。`{2,4,6}` → `12`、空の`vector` → `0`。入力の合計は`int`の範囲内です。

### 問題コード（変更禁止）
```cpp
#include <iostream>
#include <vector>

int Sum(const std::vector<int>& values)
{
    int total = 0;
    for (std::size_t i = 0; i <= values.size(); ++i)
    {
        total += values.at(i);
    }
    return total;
}

int main()
{
    std::cout << Sum({2, 4, 6}) << '\n';
    return 0;
}
```

### VELKRISの回答
**原因：** 未回答

**修正版コード：**
```cpp
// ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

---

## Q05 — アルゴリズムとコンテナー削除（中級）

### 要求仕様
`RemoveNegative` は負数をすべて削除し、残りの要素の順番を保持してください。`{-1,2,-3,0,5}` → `{2,0,5}`、空配列→空、`{-2,-1}`→空。

### 問題コード（変更禁止）
```cpp
#include <iostream>
#include <vector>
#include <algorithm>

void RemoveNegative(std::vector<int>& values)
{
    std::remove_if(values.begin(), values.end(),
                   [](int value) { return value < 0; });
}

int main()
{
    std::vector<int> values{-1, 2, -3, 0, 5};
    RemoveNegative(values);
    for (std::size_t i = 0; i < values.size(); ++i)
    {
        if (i != 0) std::cout << ",";
        std::cout << values[i];
    }
    std::cout << '\n';
    return 0;
}
```

### VELKRISの回答
**原因：** 未回答

**修正版コード：**
```cpp
// ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

---

## Q06 — 文字列からの安全な整数変換（中級）

### 要求仕様
`TryReadCount` は、非負の10進数`int`を表す文字列を安全に解釈してください。成功時は `true` と数値、失敗時は `false` と **0** を返します。前後のASCII空白は許容し、数値の後の不要文字、符号`+`/`-`、数字の間の空白は許容しません。

必須ケース：`"42"`→true/42、`"0"`→true/0、`" 42 "`→true/42、`""`・`"abc"`・`"-1"`・`"+42"`・`"42x"`・`"2147483648"`→false/0。

### 問題コード（変更禁止）
```cpp
#include <iostream>
#include <string>

bool TryReadCount(const std::string& text, int& count)
{
    count = std::stoi(text);
    return count > 0;
}

int main()
{
    int count = 0;
    std::cout << TryReadCount("42", count) << "," << count << '\n';
    return 0;
}
```

### VELKRISの回答
**原因：** 未回答

**修正版コード：**
```cpp
// ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

---

## Q07 — unordered_mapと未登録キー（中級〜上級）

### 要求仕様
`FindScore` は、登録された名前なら対応する点数を`std::optional<int>`で返し、未登録の名前なら`std::nullopt`を返してください。**0点と未登録を区別**し、検索によって`scores`の件数を増やさないでください。`{"Amy":0, "Kai":75}`に対し、Amy→0、Kai→75、Mio→未登録、最終件数→2。

### 問題コード（変更禁止）
```cpp
#include <iostream>
#include <optional>
#include <string>
#include <unordered_map>

std::optional<int> FindScore(
    std::unordered_map<std::string, int>& scores,
    const std::string& name)
{
    return scores[name];
}

int main()
{
    std::unordered_map<std::string, int> scores{{"Amy", 0}, {"Kai", 75}};
    auto missing = FindScore(scores, "Mio");
    std::cout << missing.has_value() << "," << scores.size() << '\n';
    return 0;
}
```

### VELKRISの回答
**原因：** 未回答

**修正版コード：**
```cpp
// ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

---

## Q08 — unique_ptrと所有権移動（上級）

### 要求仕様
`TransferAndAppend` は、`source`が所有する文字列を`target`へ**所有権移動**し、移動先の文字列の末尾に`!`を追加してください。呼び出し後、`source == nullptr`、`target`は有効です。`"Ready"`→`"Ready!"`、`"Go"`→`"Go!"`。呼び出し前の`source`は非nullで、`target`はnullまたは別の文字列を所有しているものとします。

### 問題コード（変更禁止）
```cpp
#include <iostream>
#include <memory>
#include <string>

void TransferAndAppend(std::unique_ptr<std::string>& source,
                       std::unique_ptr<std::string>& target)
{
    target = source;
    target->append("!");
}

int main()
{
    auto source = std::make_unique<std::string>("Ready");
    std::unique_ptr<std::string> target;
    TransferAndAppend(source, target);
    std::cout << *target << "," << (source == nullptr) << '\n';
    return 0;
}
```

### VELKRISの回答
**原因：** 未回答

**修正版コード：**
```cpp
// ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

---

## Q09 — 例外安全性と状態の保持（上級）

### 要求仕様
`AppendNonNegative` は、`incoming`の全要素が非負なら`destination`の末尾へ順番に追加してください。1つでも負数があれば`std::invalid_argument`を送出し、**destinationを変更前と完全に同じ内容に保つ**こと。例外安全性も考慮してください。

必須ケース：`destination={10}`, `incoming={2,3}`→`{10,2,3}`。`destination={10}`, `incoming={2,-1,3}`→例外・`destination={10}`のまま。空の`incoming`なら変更なし。

### 問題コード（変更禁止）
```cpp
#include <iostream>
#include <vector>
#include <stdexcept>

void AppendNonNegative(std::vector<int>& destination,
                       const std::vector<int>& incoming)
{
    for (int value : incoming)
    {
        if (value < 0)
            throw std::invalid_argument("Negative values are not allowed");
        destination.push_back(value);
    }
}

int main()
{
    std::vector<int> values{10};
    try
    {
        AppendNonNegative(values, {2, -1, 3});
    }
    catch (const std::invalid_argument&)
    {
        std::cout << "rejected\n";
    }
    for (int value : values) std::cout << value << " ";
    std::cout << '\n';
    return 0;
}
```

### VELKRISの回答
**原因：** 未回答

**修正版コード：**
```cpp
// ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

---

## Q10 — 並行処理とデータ競合（最上級）

### 要求仕様
`Count` は**100個のスレッド**を起動し、各スレッドが共有カウンターへ**1000回**加算して、全スレッド終了後に必ず`100000`を返してください。スレッドを維持したまま**競合のない安全な加算**を使用すること。連続10回の呼び出し、および`Count`を別々に同時実行した場合も、各呼び出しはそれぞれ`100000`を返すこと。スレッド終了待ちは必須です。

### 問題コード（変更禁止）
```cpp
#include <iostream>
#include <thread>
#include <vector>

int Count()
{
    int count = 0;
    std::vector<std::thread> workers;
    for (int i = 0; i < 100; ++i)
    {
        workers.emplace_back([&count]()
        {
            for (int j = 0; j < 1000; ++j)
                ++count;
        });
    }
    for (auto& worker : workers)
        worker.join();
    return count;
}

int main()
{
    std::cout << Count() << '\n';
    return 0;
}
```

### VELKRISの回答
**原因：** 未回答

**修正版コード：**
```cpp
// ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

---

## 提出報告欄（回答欄ではなく報告時にチャットで提出）
- 回答済み：Q01〜Q10のうち何問
- 自己確認：検証済み／未検証の区別
- ローカルコミット：SHA
- GitHubへのpush：所有者が許可した場合のみ実施し、その結果を報告
- 採点・合否：クライアント側で決定

**VELKRISへの指示：上記の条件を確認し、Q01から順にテストを開始してください。**
