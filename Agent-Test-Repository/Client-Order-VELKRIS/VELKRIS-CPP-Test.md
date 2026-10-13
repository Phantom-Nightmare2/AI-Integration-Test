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
**原因：** `int second = 5` の文末にセミコロンがなく、次行の `std::cout` と1つの文として解析されてコンパイルエラーになる（MSVC: `error C2143: 構文エラー: ';' が 'std::cout' の前にありません`）。ロジック上の問題はなく、セミコロンを補えば仕様どおり `15` を表示する。

**修正版コード：**
```cpp
#include <iostream>

int main()
{
    int first = 10;
    int second = 5; // 修正: 文末のセミコロンが欠落して構文エラーになっていたため補った
    std::cout << first + second << '\n';
    return 0;
}
```

**確認方法・結果：**
- 共通の検証環境：Windows 11 / MSVC 19.51.36260（Visual Studio 18 Community, x64）、`cl /std:c++17 /EHsc /W4 /WX /permissive- /utf-8`。問題ごとにリポジトリ外の独立した一時フォルダで検証し、検証用ファイルは提出していない（Q02〜Q10も同じ）。
- 元コード：コンパイルのみ実施（実行不可）→ `error C2143` を確認。
- 修正版：警告なしでビルド成功。実行結果 `15`（期待値 `15`）、終了コード0。**検証済み・一致。**

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
**原因：** `Increase(int value)` は値渡しのため、関数内の `value += 1` は引数のコピーを変更するだけで、呼び出し元の `first`・`second` は変わらない。元コードを実行すると `8,0` と表示され、期待値 `9,1` にならない。

**修正版コード：**
```cpp
#include <iostream>

void Increase(int& value) // 修正: 値渡し(int)ではコピーが増えるだけなので、参照渡し(int&)にして呼び出し元の変数を更新する
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

**確認方法・結果：**
- 元コード（実行しても未定義動作のない、定義済みの動作）：実行結果 `8,0` → 不具合を再現。
- 修正版：警告なしでビルド成功。実行結果 `9,1`（期待値 `9,1`）。8→9、0→1 を確認。**検証済み・一致。**

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
**原因：**
1. `total / values.size()` は `int` と `std::size_t` の演算で、`total` が符号なし整数に変換されて整数除算になり、小数が切り捨てられる（`{1,2}` → `1`）。合計が負なら巨大な値にもなる。`double` で返しても除算後の変換なので手遅れ。
2. 空配列では `size()==0` でゼロ除算（未定義動作）になり、仕様の `std::invalid_argument` も送出されない。
3. 表示面：`std::cout` の既定書式では `4.0` が `4` と表示されるため、確認用に小数1桁固定で出力する。

**修正版コード：**
```cpp
#include <iomanip>
#include <iostream>
#include <vector>
#include <numeric>
#include <stdexcept>

double Average(const std::vector<int>& values)
{
    // 修正: 空配列はゼロ除算になるため、要求仕様どおり std::invalid_argument を送出する
    if (values.empty())
        throw std::invalid_argument("values must not be empty");

    int total = std::accumulate(values.begin(), values.end(), 0);
    // 修正: int / std::size_t は符号なし整数どうしの除算になり小数が切り捨てられる（負の合計では巨大値にもなる）。
    //       両辺を double に変換してから割る
    return static_cast<double>(total) / static_cast<double>(values.size());
}

int main()
{
    // 修正: 4.0 が "4" ではなく "4.0" と表示されるよう、小数1桁固定で出力する
    std::cout << std::fixed << std::setprecision(1);
    std::cout << Average({1, 2}) << '\n';
    std::cout << Average({2, 4, 6}) << '\n';
    try
    {
        Average({});
    }
    catch (const std::invalid_argument&)
    {
        std::cout << "invalid_argument\n";
    }
    return 0;
}
```

**確認方法・結果：**
- 元コード：コンパイル時に警告 C4244（`unsigned __int64` → `double` 変換）。実行結果 `1`（期待値 `1.5`）→ 不具合を再現。空配列の呼び出しはゼロ除算の未定義動作になるため実行していない。
- 修正版：警告なしでビルド成功。実行結果 `1.5` / `4.0` / `invalid_argument`（期待値と一致）。
- 別の検証用プログラムで関数を直接テスト：`{1,2}`→1.5、`{2,4,6}`→4.0、`{-1,-2}`→-1.5、`{}`→`std::invalid_argument` 送出。4件すべて合格。**検証済み。**

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
**原因：** ループ条件が `i <= values.size()` のため、最後に `values.at(values.size())` を参照し、`at` の境界チェックで `std::out_of_range` が送出される（キャッチされず異常終了）。空の `vector` でも `at(0)` で同じく例外になり、`0` を返せない。

**修正版コード：**
```cpp
#include <iostream>
#include <vector>

int Sum(const std::vector<int>& values)
{
    int total = 0;
    // 修正: i <= values.size() では最後に values.at(size()) を参照して std::out_of_range になる（空配列でも at(0) で例外）。
    //       範囲for文で実在する要素だけを走査し、境界外アクセスをなくす。空なら 0 のまま返る
    for (int value : values)
    {
        total += value;
    }
    return total;
}

int main()
{
    std::cout << Sum({2, 4, 6}) << '\n';
    std::cout << Sum({}) << '\n'; // 修正: 空入力のケースも確認できるよう表示を追加
    return 0;
}
```

**確認方法・結果：**
- 元コード：コードを読んで原因を確認し、実行は省略（キャッチされない `std::out_of_range` により `std::terminate` で異常終了するのが予想される結果）。
- 修正版：警告なしでビルド成功。実行結果 `12` と `0`（期待値と一致）。
- 別の検証用プログラム：`{2,4,6}`→12、`{}`→0、`{7}`→7、`{-3,3}`→0。4件すべて合格。**検証済み。**

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
**原因：** `std::remove_if` は削除しない要素を前に詰めて「新しい論理的な終端」のイテレーターを返すだけで、`vector` の要素数は変えない。戻り値を捨てているため、末尾に残った要素がそのまま残る（元コードの実行結果は `2,0,5,0,5`）。MSVCも警告 C4858（戻り値の破棄）を出す。

**修正版コード：**
```cpp
#include <iostream>
#include <vector>
#include <algorithm>

void RemoveNegative(std::vector<int>& values)
{
    // 修正: std::remove_if は残す要素を前へ詰めて新しい論理的終端を返すだけで、要素数は変えない（戻り値も捨てていた）。
    //       戻り値から end() までを erase する erase-remove イディオムで実際に削除する。remove_if は安定なので順番も保持される
    values.erase(std::remove_if(values.begin(), values.end(),
                                [](int value) { return value < 0; }),
                 values.end());
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

**確認方法・結果：**
- 元コード（定義済みの動作）：実行結果 `2,0,5,0,5`、警告 C4858 → 不具合を再現。
- 修正版：警告なしでビルド成功。実行結果 `2,0,5`（期待値と一致）。
- 別の検証用プログラム：`{-1,2,-3,0,5}`→`{2,0,5}`、空→空、`{-2,-1}`→空、`{3,1,2}`→`{3,1,2}`（順番を保持）。4件すべて合格。**検証済み。**

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
**原因：**
1. `std::stoi` は `""`・`"abc"` で `std::invalid_argument`、`"2147483648"` で `std::out_of_range` を送出し、`false/0` を返せない（例外で異常終了する）。
2. `std::stoi` は先頭の符号を受理する（`"+42"`→42、`"-1"`→-1）。また、数値の後ろの不要文字を黙って無視する（`"42x"`→42）。
3. `return count > 0;` のため `"0"` が失敗扱いになる。さらに失敗時に `count` が0にならない（`"-1"` なら -1 のまま）。

`std::from_chars`（例外を送出せず、ロケールにも依存しない）で変換し、先頭が数字であることと全文字を消費したことを確認する方式に変更した。

**修正版コード：**
```cpp
#include <charconv>
#include <iostream>
#include <string>
#include <system_error>

// 修正: std::isspace はロケール依存で、負の char 値では未定義動作になり得るため、ASCII空白を明示的に判定する
bool IsAsciiSpace(char c)
{
    return c == ' ' || c == '\t' || c == '\n' || c == '\v' || c == '\f' || c == '\r';
}

bool TryReadCount(const std::string& text, int& count)
{
    // 修正: 失敗時は必ず count = 0 を返す仕様のため、最初に 0 にしておく
    count = 0;

    // 修正: 前後のASCII空白だけを取り除く（数字の間の空白は後段の全文字消費チェックで不正になる）
    std::size_t first = 0;
    std::size_t last = text.size();
    while (first < last && IsAsciiSpace(text[first])) ++first;
    while (last > first && IsAsciiSpace(text[last - 1])) --last;

    // 修正: 空文字・空白のみ・符号(+/-)・数字以外で始まる文字列を拒否する
    //       （std::stoi は "+42" や "-1" を受理し、"" や "abc" では std::invalid_argument を送出していた）
    if (first == last || text[first] < '0' || text[first] > '9')
        return false;

    const char* begin = text.data() + first;
    const char* end = text.data() + last;
    int value = 0;
    // 修正: 例外を送出しない std::from_chars（10進）で変換する。
    //       int の範囲外（"2147483648" など）は std::errc::result_out_of_range として検出できる
    const auto [ptr, ec] = std::from_chars(begin, end, value, 10);
    // 修正: std::stoi は "42x" の末尾の不要文字を黙って無視していたため、全文字を消費したかを確認する
    if (ec != std::errc() || ptr != end)
        return false;

    // 修正: 元の count > 0 では "0" が失敗扱いになっていた。先頭が数字なので value は必ず 0 以上で、0 も成功とする
    count = value;
    return true;
}

int main()
{
    int count = 0;
    std::cout << TryReadCount("42", count) << "," << count << '\n';
    return 0;
}
```

**確認方法・結果：**
- 元コード：`main` の `"42"` だけを実行→ `1,42`（このケースだけは偶然正しい）。`""`・`"abc"`・`"2147483648"` は例外で異常終了するため、元コードでは実行していない。
- 修正版：警告なしでビルド成功。実行結果 `1,42`（期待値と一致）。
- 別の検証用プログラムで、結果（true/false）と `count` の両方を確認：`"42"`→true/42、`"0"`→true/0、`" 42 "`→true/42、`"\t42\r\n"`→true/42、`"2147483647"`→true/2147483647、`"007"`→true/7、`""`・`"   "`・`"abc"`・`"-1"`・`"+42"`・`"42x"`・`"4 2"`・`"2147483648"`・`"99999999999999999999"`・`"4\0" "2"`（NULを含む）→ すべて false/0。16件すべて合格。**検証済み。**

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
**原因：** `scores[name]` の `operator[]` は、キーが存在しないとき値0の要素を**挿入して**から参照を返す。そのため、未登録の `"Mio"` を検索すると件数が2→3に増え、戻り値も `0` を持つ `optional` になるので、0点（Amy）と未登録を区別できない。元コードの実行結果は `1,3`（期待値 `0,2`）。

**修正版コード：**
```cpp
#include <iostream>
#include <optional>
#include <string>
#include <unordered_map>

std::optional<int> FindScore(
    const std::unordered_map<std::string, int>& scores, // 修正: 検索で map を変更しないことを型で保証するため const 参照にする
    const std::string& name)
{
    // 修正: operator[] は未登録キーを値 0 で挿入してしまうため、件数が増え、0点と未登録も区別できなかった。
    //       find で検索し、見つからなければ std::nullopt を返す
    const auto it = scores.find(name);
    if (it == scores.end())
        return std::nullopt;
    return it->second;
}

int main()
{
    std::unordered_map<std::string, int> scores{{"Amy", 0}, {"Kai", 75}};
    auto missing = FindScore(scores, "Mio");
    std::cout << missing.has_value() << "," << scores.size() << '\n';
    return 0;
}
```

**確認方法・結果：**
- 元コード（定義済みの動作）：実行結果 `1,3` → 不具合を再現。
- 修正版：警告なしでビルド成功。実行結果 `0,2`（未登録 → `has_value()==false`、件数2）。
- 別の検証用プログラム：Amy→値あり・0、Kai→値あり・75、Mio→`std::nullopt`、最終件数→2。4件すべて合格。**検証済み。**

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
**原因：** `std::unique_ptr` は所有権を1つだけ持つため、コピー代入演算子が削除されている。`target = source;` はコピーしようとするのでコンパイルエラーになる（MSVC: `error C2280: 削除された関数を参照しようとしています`）。所有権の移動には `std::move` が必要。

**修正版コード：**
```cpp
#include <iostream>
#include <memory>
#include <stdexcept>
#include <string>
#include <utility>

void TransferAndAppend(std::unique_ptr<std::string>& source,
                       std::unique_ptr<std::string>& target)
{
    // 修正: 前提（source は非null）に反する呼び出しでは null 参照外しを避け、何も変更せずに例外を送出する
    if (!source)
        throw std::invalid_argument("source must not be null");

    // 修正: unique_ptr はコピー代入が削除されているためコンパイルエラーになっていた。std::move で所有権を移動する。
    //       移動後 source は nullptr になり、target が以前所有していた文字列は自動的に解放される
    target = std::move(source);
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

**確認方法・結果：**
- 元コード：コンパイルのみ実施（実行不可）→ `error C2280` を確認。
- 修正版：警告なしでビルド成功。実行結果 `Ready!,1`（`source == nullptr` が true）。
- 別の検証用プログラム：①`"Ready"`、`target` が null → `"Ready!"`、`source` は null。②`"Go"`、`target` が別の文字列 `"Old"` を所有 → `"Go!"`、`source` は null。移動前後で文字列オブジェクトのアドレスが同じ（コピーではなく所有権の移動である）ことも確認。③前提外の null `source` → `std::invalid_argument` を送出し、`target` は `"Keep"` のまま。7件すべて合格。**検証済み。**

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
**原因：** 検証と追加を同じループで行っているため、負数を検出した時点で、その手前の要素がすでに `destination` に `push_back` されている。`{10}` + `{2,-1,3}` では例外の後に `destination` が `{10,2}` になり、「変更前と完全に同じ」という要件（強い例外保証）を満たさない。また、`push_back` の途中でメモリ確保に失敗（`std::bad_alloc`）した場合も、一部だけ追加された状態が残る。

**修正版コード：**
```cpp
#include <algorithm>
#include <iostream>
#include <vector>
#include <stdexcept>

void AppendNonNegative(std::vector<int>& destination,
                       const std::vector<int>& incoming)
{
    // 修正: 元コードは検証と追加を同じループで行うため、負数の手前の要素が追加された状態で例外が出ていた。
    //       追加の前に全要素を検証し、負数があれば destination に一切触れずに送出する
    if (std::any_of(incoming.begin(), incoming.end(),
                    [](int value) { return value < 0; }))
        throw std::invalid_argument("Negative values are not allowed");

    // 修正: 空の incoming なら変更なし（不要なコピーも避ける）
    if (incoming.empty())
        return;

    // 修正: 強い例外保証のため、作業用コピーに追加してから例外を送出しない swap で反映する。
    //       メモリ確保で std::bad_alloc が出ても destination は変更前のまま。
    //       incoming と destination が同じオブジェクトでも、作業用コピー側に追加するので安全
    std::vector<int> result;
    result.reserve(destination.size() + incoming.size());
    result.insert(result.end(), destination.begin(), destination.end());
    result.insert(result.end(), incoming.begin(), incoming.end());
    destination.swap(result);
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

**確認方法・結果：**
- 元コード（定義済みの動作）：実行結果 `rejected` / `10 2` → 不具合を再現。
- 修正版：警告なしでビルド成功。実行結果 `rejected` / `10`（期待値と一致）。
- 別の検証用プログラム：`{10}`+`{2,3}`→`{10,2,3}`。`{10}`+`{2,-1,3}`→例外が送出され、内容は `{10}` のまま、容量も変化なし。`{10}`+`{}`→変更なし。自分自身を追加（`d`+`d`、`{1,2}`）→`{1,2,1,2}`。空+`{0,5}`→`{0,5}`（0は非負として受理）。7件すべて合格。
- 未検証：`std::bad_alloc` の発生時の動作は、確保失敗を再現する仕組みがないため実行していない。設計上は、作業用コピーで確保に失敗した場合 `destination` に触れる前に例外が伝わり、反映は例外を送出しない `swap` だけなので、変更前の状態が保たれる。**上記以外は検証済み。**

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
**原因：** 100個のスレッドが共有の `int count` を同期なしで `++count` しており、データ競合（未定義動作）になる。`++` は読み込み・加算・書き込みの3段階で不可分ではないため、加算が失われ、結果が `100000` 未満になり得る。修正では `std::atomic<int>` で各加算を不可分にした。加えて、スレッドを生成している途中で例外が出た場合に、join されていない `std::thread` が破棄されて `std::terminate` になるのを防ぐため、RAIIで全スレッドを必ず join するようにした。

**修正版コード：**
```cpp
#include <atomic>
#include <iostream>
#include <thread>
#include <vector>

// 修正: スレッド生成の途中で例外（std::system_error など）が出ても、起動済みスレッドを必ず join するためのRAIIガード。
//       joinable な std::thread を join せずに破棄すると std::terminate になるため
class JoinAll
{
public:
    explicit JoinAll(std::vector<std::thread>& threads) : threads_(threads) {}
    ~JoinAll()
    {
        for (auto& thread : threads_)
            if (thread.joinable())
                thread.join();
    }
    JoinAll(const JoinAll&) = delete;
    JoinAll& operator=(const JoinAll&) = delete;

private:
    std::vector<std::thread>& threads_;
};

int Count()
{
    // 修正: 複数スレッドから非アトミックな int を ++ するのはデータ競合（未定義動作）で、加算が失われる。
    //       std::atomic<int> にして各加算を不可分にする。カウンターは呼び出しごとのローカル変数なので、
    //       Count を別々に同時実行しても互いに干渉しない
    std::atomic<int> count{0};
    std::vector<std::thread> workers;
    workers.reserve(100);
    {
        JoinAll joinGuard(workers); // 修正: 正常時も例外時も、このスコープを抜けるときに全スレッドの終了を待つ
        for (int i = 0; i < 100; ++i)
        {
            workers.emplace_back([&count]()
            {
                for (int j = 0; j < 1000; ++j)
                    count.fetch_add(1, std::memory_order_relaxed); // 修正: 不可分な加算。最終値は join による同期で確定するため relaxed で十分
            });
        }
    } // 修正: ここで全スレッドが join 済みになってから値を読む
    return count.load();
}

int main()
{
    std::cout << Count() << '\n';
    return 0;
}
```

**確認方法・結果：**
- 元コード：データ競合という未定義動作を含むため、ルールに従い実行していない。予想される結果は「`100000` 以下の不定の値（実行ごとに変わり得る）」。
- 修正版：警告なしでビルド成功。実行結果 `100000`。
- 別の検証用プログラム：①`Count()` を連続10回呼び出し → すべて `100000`。②`std::async(std::launch::async, Count)` で4つの `Count` を同時に実行 → それぞれ `100000`。14件すべて合格。**検証済み。**
- 補足（未実施）：MSVCにはThreadSanitizerがないため、競合検出ツールによる検査は行っていない。再現手順：Linux の clang/gcc で `-std=c++17 -fsanitize=thread` を付けて同じプログラムをビルド・実行する。予想結果は、元コードではデータ競合の警告が出て、修正版では警告が出ない。

---

## 提出報告欄（回答欄ではなく報告時にチャットで提出）
- 回答済み：Q01〜Q10のうち何問
- 自己確認：検証済み／未検証の区別
- ローカルコミット：SHA
- GitHubへのpush：所有者が許可した場合のみ実施し、その結果を報告
- 採点・合否：クライアント側で決定

**VELKRISへの指示：上記の条件を確認し、Q01から順にテストを開始してください。**
