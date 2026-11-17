# ★HERCULES — Python 確認テスト（全10問）

> **利用条件：All Rights Reserved — Copyright (c) 2026 Phantom Nightmare.** 本問題集は★HERCULES専用のオリジナル確認テストです。法令・GitHub利用規約で認められる場合を除き、無断転載・再配布・改変・別テストへの転用には所有者の事前許可が必要です。所有者が指示する確認テストの実施・回答記入は許可します。[利用条件全文](../../LICENSE.md)

## 試験概要

- **対象：** ★HERCULES（スターエージェント）
- **対象言語：** Python 3.11以上（標準ライブラリのみ）
- **難易度：** 上級〜最難関（従来のC#能力テストより高い難易度。Q01〜Q03は導入・応用課題、Q04以降は上級〜最難関の技術を扱う）
- **問題数：** 10問／100点（各10点）
- **目的：** 厳密な入力検証・状態復元・データ整合性・SQLiteトランザクション・ファイル安全性・スレッド並行処理・非同期ジョブのタイムアウト／キャンセル制御を含む実践的なデバッグ能力の評価

## クライアントオーダー / Client Order

- **依頼者：** ワトソン（テスト用の架空クライアント）
- **担当エージェント：** ★HERCULES（ヘラクレス）
- **テスト名称：** Python確認テスト / Python Confirmation Test
- **環境：** 各問は独立したPythonプログラム。外部API・有料サービス・外部ライブラリは禁止

### 依頼内容

Q01からQ10まで順番に取り組み、要求仕様・必須確認ケースを満たす修正版の**コード全文**と、原因の解説、検証方法・実際の結果を各問の**「★HERCULESの回答」欄だけ**に記入してください。必要な調査・テスト・再検証は担当エージェントが自律的に行うこと。

これは**確認テスト**です。問題文の編集、採点前の模範解答の提示、問題内容の都合の良い変更は認めません。解けなかった問題は、その旨と調査内容を回答してください。

### 必須ルール

1. **「要求仕様」「必須確認ケース」「問題コード」は編集・削除禁止**。変更可能なのは各問の「★HERCULESの回答」欄のみ。元コードを直接書き換えて正解扱いにしない。
2. 回答は **(a) 不具合の原因、(b) 修正版コード全文、(c) 検証手順と実測結果** の3点セットとする。修正理由はコード内に Python の # コメントで明記する。
3. 各問は独立したプログラム。検証はリポジトリ外の一時フォルダで実施し、検証用コード・DB・ログ・一時ファイルをGitHubへ追加しない。
4. **期待値と実測値を必ず区別**する。実行できなかった場合は「未実行」、理由、再現可能な確認手順、予想結果を記載する。実行していないものを「合格」「実測」と書かない。
5. DBやファイルを扱う問題は、一時DB／一時ディレクトリだけを使う。既存の実データ、認証情報、APIキーには触れない。
6. この確認テスト以外のファイル、README、LICENSE、VELKRISの問題・回答、AIエージェント設定を変更しない。
7. 全10問を回答したら、回答数、検証済み件数、未実行件数、ローカルのコミットSHAを報告する。**GitHubへのpushは所有者が明示指示した場合だけ**実施する。クローンやローカルコミットだけでは自動pushされない。
8. **★HERCULES自身による点数・合否判定は禁止**。採点はクライアント側で行う。回答の見栄えではなく仕様への適合性を評価する。
9. 外部の模範解答・既存エージェントの回答のコピーは使用しない。各問の原因と修正を独立して説明する。

### 採点基準（100点）

各問10点＝**原因分析2点／要求仕様に適合した修正版コード6点／検証方法・結果2点**。境界条件、データ安全性、終了処理を含む必須ケースの正確性を確認する。

---

## Q01 — 安全な整数入力の解析（応用）

### 要求仕様

関数 parse_units(text) は、**ASCIIの数字だけ**で表された整数0〜1000を返す。前後に限り通常の半角スペース ' ' は許容し、数字の途中のスペース、タブ、改行、符号、全角数字、空文字、範囲外の数値は ValueError にする。先頭の0は許容する。引数は str とする。

### 必須確認ケース

- "0" → 0、" 007 " → 7、"1000" → 1000
- "1001"、"-1"、"+2"、"1 0"、"１２"、"\t2"、""、"   " → すべて ValueError

### 問題コード（変更禁止）

~~~python
def parse_units(text: str) -> int:
    text = text.strip()
    if not text.isdigit():
        raise ValueError("invalid units")
    result = int(text)
    if result > 1000:
        raise ValueError("too large")
    return result


if __name__ == "__main__":
    print(parse_units(" 007 "))
~~~

### ★HERCULESの回答

**原因：**

1. `text.strip()` は半角スペースだけでなく、タブ・改行・全角スペースなどの空白全般を除去します。このため `"\t2"` や `"2\n"` が通ってしまいます。
2. `str.isdigit()` は Unicode の数字全般（全角の `"１２"` や上付きの `"²"` など）でも真になります。さらに `int()` は全角数字も解釈するので、`"１２"` が 12 として受理されます。
3. 空文字・スペースだけの入力は `isdigit()` が偽になるため ValueError になります。ただし範囲判定は上限側（`> 1000`）だけで、「ASCII数字のみ」という条件を満たしていません。

修正前コードで実測した結果（いずれも ValueError になるべき入力が受理される）：`'１２' -> 12`、`'\t2' -> 2`、`'2\n' -> 2`、`'　5' -> 5`

**修正版コード全文：**

~~~python
MAX_UNITS = 1000
# 修正: isdigit() は "１２"（全角）や "²" なども真になるため、許可する文字をASCIIの0〜9に限定する
_ASCII_DIGITS = frozenset("0123456789")


def parse_units(text: str) -> int:
    # 修正: 引数は str に限定する（bytes や int を黙って受け付けない）
    if not isinstance(text, str):
        raise TypeError("text must be str")
    # 修正: str.strip() はタブ・改行・全角スペースなど空白全般を除去してしまうため、
    # 前後の除去対象を半角スペース ' ' だけに限定する
    digits = text.strip(" ")
    # 修正: 空文字・スペースだけの入力、符号、途中の空白、全角数字を ValueError にする
    if not digits or not all(ch in _ASCII_DIGITS for ch in digits):
        raise ValueError("invalid units")
    # 修正: 先頭の0は許容する。0を除いた有効桁数で先に範囲判定し、
    # 非常に長い0埋め文字列でも int() の桁数制限に依存せず正しく扱う
    significant = digits.lstrip("0") or "0"
    if len(significant) > len(str(MAX_UNITS)):
        raise ValueError("too large")
    result = int(significant)
    # 修正: 0〜1000 の範囲外は ValueError（負数は上の文字検査で既に除外済み）
    if result > MAX_UNITS:
        raise ValueError("too large")
    return result


if __name__ == "__main__":
    print(parse_units(" 007 "))
~~~

**確認方法・結果：**

- 実行環境：Windows 11、Python 3.12.15（Python 3.11 の実行環境がないため、3.11 での実行は**未実行**。3.11 以前から使える構文と API だけを使用）
- 手順：リポジトリ外の一時フォルダ（`%TEMP%\claude\...\scratchpad\hercules-python-test\`）に上記コードを `q01.py` として保存しました。必須ケースと追加ケースを呼び出す検証スクリプトと、`python q01.py` を実行しました。
- 期待値：`"0"→0`、`" 007 "→7`、`"1000"→1000`。`"1001"`、`"-1"`、`"+2"`、`"1 0"`、`"１２"`、`"\t2"`、`""`、`"   "` はすべて ValueError。
- 実測値（実行出力をそのまま転記）：

~~~text
$ python q01.py
7
[必須ケース: 正常]
  '0' -> 0  期待=0  一致
  ' 007 ' -> 7  期待=7  一致
  '1000' -> 1000  期待=1000  一致
[必須ケース: ValueError]
  '1001' -> ValueError(too large)  期待=ValueError  一致
  '-1' -> ValueError(invalid units)  期待=ValueError  一致
  '+2' -> ValueError(invalid units)  期待=ValueError  一致
  '1 0' -> ValueError(invalid units)  期待=ValueError  一致
  '１２' -> ValueError(invalid units)  期待=ValueError  一致
  '\t2' -> ValueError(invalid units)  期待=ValueError  一致
  '' -> ValueError(invalid units)  期待=ValueError  一致
  '   ' -> ValueError(invalid units)  期待=ValueError  一致
[追加ケース]
  '2\n'(len=2) -> ValueError(invalid units)
  '　5'(len=2) -> ValueError(invalid units)
  '²'(len=1) -> ValueError(invalid units)
  '000000000000'(len=5001) -> 1        # 先頭0が5000個 + "1"
  '000000000000'(len=5000) -> 0
  '999999999999'(len=5000) -> ValueError(too large)
  '  0001000  '(len=11) -> 1000
  b'1' -> TypeError(text must be str)
  1 -> TypeError(text must be str)
  None -> TypeError(text must be str)
~~~

- 必須ケース11件はすべて期待値どおりでした（実測）。str 以外の引数は TypeError にしています（仕様に「引数は str」とあるため、型違いは値の誤りと区別しました）。

---

## Q02 — 優先度付きの安定ソート（応用）

### 要求仕様

sort_shipments(rows) は、新しいリストを返す。辞書のキーは "id"（文字列）、"priority"（整数）、"created_at"（整数の登録順序番号）とする。**priority が高い順**、同じpriorityでは **created_at が小さい順**、それも同じなら元の入力順（安定ソート）とする。元の rows の順序および要素の内容を変更してはいけない。

### 必須確認ケース

- 入力 id/priority/created_at = A/2/10, B/3/20, C/3/5, D/3/5 → 戻り値のid順 ["C","D","B","A"]
- 同じ priority・created_at の項目は入力順を保持
- 空配列 → 空配列。元リストの順序は変更しない

### 問題コード（変更禁止）

~~~python
def sort_shipments(rows: list[dict]) -> list[dict]:
    return sorted(
        rows,
        key=lambda row: (row["priority"], row["created_at"]),
        reverse=True,
    )


if __name__ == "__main__":
    data = [
        {"id": "A", "priority": 2, "created_at": 10},
        {"id": "B", "priority": 3, "created_at": 20},
        {"id": "C", "priority": 3, "created_at": 5},
        {"id": "D", "priority": 3, "created_at": 5},
    ]
    print([row["id"] for row in sort_shipments(data)])
~~~

### ★HERCULESの回答

**原因：**

- `reverse=True` はキー `(priority, created_at)` の全体を降順にします。このため priority だけでなく **created_at も大きい順**になり、「同じ priority では created_at が小さい順」という仕様に反します。必須ケースでは修正前の結果が `['B', 'C', 'D', 'A']` になりました（実測。期待値は `['C', 'D', 'B', 'A']`）。
- 補足（実測で確認）：Python の `sorted(..., reverse=True)` は、**キーが完全に同じ要素の入力順は保持**します（公式ドキュメントにある仕様）。修正前コードでも、同じ priority・created_at の t1〜t4 は入力順のままでした。したがって不具合の本質は「安定性の破壊」ではなく、**第2キー（created_at）の向きの誤り**です。修正では、priority だけを符号反転して降順にし、created_at は昇順のまま、`reverse` を使わずに安定ソートします。

**修正版コード全文：**

~~~python
def sort_shipments(rows: list[dict]) -> list[dict]:
    # 修正: reverse=True はキー全体を逆順にするため、created_at まで降順になり
    # 「同じ priority では created_at が小さい順」という仕様に反していた。
    # （補足: reverse=True でもキーが完全に同じ要素の入力順は保たれるが、第2キーの向きが誤る）
    # priority だけを符号反転して降順にし、created_at は昇順のまま、reverse は使わない。
    # sorted() は安定ソートなので、キーが同じ要素は入力順を保持する。
    # sorted() は新しいリストを返し、元の rows の順序や各辞書の内容は変更しない。
    return sorted(rows, key=lambda row: (-row["priority"], row["created_at"]))


if __name__ == "__main__":
    data = [
        {"id": "A", "priority": 2, "created_at": 10},
        {"id": "B", "priority": 3, "created_at": 20},
        {"id": "C", "priority": 3, "created_at": 5},
        {"id": "D", "priority": 3, "created_at": 5},
    ]
    print([row["id"] for row in sort_shipments(data)])
~~~

**確認方法・結果：**

- 実行環境と手順：Windows 11、Python 3.12.15（3.11 は**未実行**）。リポジトリ外の一時フォルダで `q02.py` と検証スクリプトを実行しました。元リストは事前に `copy.deepcopy` で保存し、実行後に比較しました。各要素の `id()` の並びも比較しました。
- 期待値：ケース1は `['C','D','B','A']`、同順位は入力順、空配列は `[]`、元リストは不変。
- 実測値（実行出力をそのまま転記）：

~~~text
$ python q02.py
['C', 'D', 'B', 'A']
[修正前の再現] ['B', 'C', 'D', 'A']
[ケース1] 結果: ['C', 'D', 'B', 'A'] 期待: ['C', 'D', 'B', 'A'] 一致
  元リストの順序・内容不変: True
  戻り値は新しいリスト: True
[ケース2] 結果: ['x', 'y', 't1', 't2', 't3', 't4'] 期待: ['x', 'y', 't1', 't2', 't3', 't4'] 一致
  修正前: ['x', 't1', 't2', 't3', 't4', 'y']
[ケース3] 空配列: [] 新しいリスト: True 元: []
[追加] 負のpriority: ['n2', 'n1', 'n3'] 期待: ['n2', 'n1', 'n3']
~~~

- ケース2の入力は、同じ priority=1・created_at=1 の t1〜t4 の間に、x（priority=5）と y（priority=1, created_at=0）を挟んだものです。修正後は y が先頭側に来て、t1〜t4 は入力順を保ちました（実測）。修正前は y が最後になり、created_at の向きが逆であることを示しています。
- 必須ケース3件はすべて期待値どおりでした（実測）。戻り値の要素は元と同じ辞書オブジェクトです（内容は変更しません）。

---

## Q03 — コンテキストマネージャーのロールバック（応用〜上級）

### 要求仕様

StateSession(state) は、辞書 state（str→int）を with 内で編集するためのコンテキストマネージャー。**正常終了時は編集内容を保持して version キーを1増加**させる。**例外発生時はキーの追加・削除を含めて開始前の状態へ完全に戻し、例外は握りつぶさず呼び出し元へ伝播**させる。state 自体の辞書オブジェクトは置き換えない。開始時に version は必ず存在し整数とする。

### 必須確認ケース

- 初期 {"count": 2, "version": 0}、with 内で count=5 → 終了後 {"count":5,"version":1}
- 同じ初期状態から with 内で count削除・temp追加して RuntimeError → 呼び出し元で RuntimeError を捕捉、state は開始前と完全一致
- 同一の state に対して正常終了2回 → version が合計2増加

### 問題コード（変更禁止）

~~~python
class StateSession:
    def __init__(self, state: dict[str, int]):
        self.state = state
        self.before = dict(state)

    def __enter__(self) -> dict[str, int]:
        return self.state

    def __exit__(self, exc_type, exc, tb) -> bool:
        if exc_type is not None:
            self.state.update(self.before)
            return True
        self.state["version"] += 1
        return False


if __name__ == "__main__":
    state = {"count": 2, "version": 0}
    with StateSession(state) as current:
        current["count"] = 5
    print(state)
~~~

### ★HERCULESの回答

**原因：**

1. **例外を握りつぶしている**：`__exit__` が例外時に `True` を返すため、RuntimeError が呼び出し元に伝播しません。
2. **復元が不完全**：`self.state.update(self.before)` は既存キーの値を上書きするだけです。with 内で**追加されたキー（temp）が残り**、キーの並びも変わります。
3. **スナップショットの取得時期が誤り**：`before` を `__init__` で取得しているため、セッション生成後・with 開始前の変更や、同じセッションオブジェクトの再利用時に、古い状態へ「復元」してしまいます。

修正前コードで実測した結果（count削除・temp追加後に RuntimeError）：例外は捕捉されず `None`、state は `{'version': 0, 'temp': 99, 'count': 2}`（temp が残る）でした。

**修正版コード全文：**

~~~python
from types import TracebackType


class StateSession:
    def __init__(self, state: dict[str, int]):
        self.state = state
        # 修正: スナップショットを __init__ で取ると、生成から with 開始までの変更や
        # 同じセッションの再利用時に古い状態へ戻してしまう。__enter__ で取得する。
        self._before: dict[str, int] | None = None

    def __enter__(self) -> dict[str, int]:
        version = self.state.get("version")
        # 修正: 前提条件（version が存在し整数）を開始時に確認する（bool は整数扱いしない）
        if type(version) is not int:
            raise ValueError("state['version'] must exist and be an int")
        # 修正: 値は int なので浅いコピーで開始前の状態を完全に保存できる
        self._before = dict(self.state)
        return self.state

    def _restore(self) -> None:
        # 修正: update() だけでは with 内で追加されたキーが残り、削除されたキーしか戻らない。
        # clear() してから update() し、同じ辞書オブジェクトのまま開始前の内容へ完全に戻す。
        assert self._before is not None
        self.state.clear()
        self.state.update(self._before)

    def __exit__(
        self,
        exc_type: type[BaseException] | None,
        exc: BaseException | None,
        tb: TracebackType | None,
    ) -> bool:
        try:
            if exc_type is not None:
                # 修正: 例外時は完全に復元し、False を返して例外を呼び出し元へ伝播させる
                # （元コードは True を返して例外を握りつぶしていた）
                self._restore()
                return False
            version = self.state.get("version")
            # 修正: with 内で version が削除・非整数化された場合は正常に加算できないため、
            # 開始前へ戻したうえで例外にする（中途半端な状態を確定させない）
            if type(version) is not int:
                self._restore()
                raise ValueError("state['version'] was removed or is not an int")
            self.state["version"] = version + 1
            return False
        finally:
            self._before = None


if __name__ == "__main__":
    state = {"count": 2, "version": 0}
    with StateSession(state) as current:
        current["count"] = 5
    print(state)
~~~

**確認方法・結果：**

- 実行環境と手順：Windows 11、Python 3.12.15（3.11 は**未実行**）。リポジトリ外の一時フォルダで `q03.py` と検証スクリプトを実行しました。辞書の同一性は `is` で確認しました。
- 期待値：ケース1は `{"count":5,"version":1}`。ケース2は RuntimeError を捕捉でき、state が開始前と完全一致（キー集合・値・同一オブジェクト）。ケース3は version が2増加。
- 実測値（実行出力をそのまま転記）：

~~~text
$ python q03.py
{'count': 5, 'version': 1}
[ケース1] {'count': 5, 'version': 1} 期待: {'count': 5, 'version': 1} 一致 同一オブジェクト: True
[ケース2] 捕捉: RuntimeError('fail') state: {'count': 2, 'version': 0} 開始前と完全一致: True 同一オブジェクト: True
[ケース3] 正常2回: {'count': 4, 'version': 2} version増加= 2
[追加] 生成後の変更を開始前状態として復元: {'count': 10, 'version': 0} 期待: {'count': 10, 'version': 0}
[追加] 同一セッション再利用: {'count': 11, 'version': 1}
[追加] KeyboardInterrupt 伝播, state: {'count': 1, 'version': 3}
[追加] version削除: ValueError("state['version'] was removed or is not an int") state: {'count': 1, 'version': 0}
[追加] version欠落: ValueError("state['version'] must exist and be an int")
~~~

- 必須ケース3件はすべて期待値どおりでした（実測）。KeyboardInterrupt などの BaseException でも復元して伝播することを確認しました。with 内で version を削除して正常終了した場合（仕様外）は、開始前へ戻して ValueError にする設計としました。

---

## Q04 — 1回しか読めないイテレーターの分割（上級）

### 要求仕様

chunks(source, size) はイテレーターを**先読みしすぎずに遅延評価**し、size 個ずつリストに分けて yield する。最後の不足分も捨てない。size が1未満なら、sourceを消費する前に ValueError を送出する。sourceは1回しか走査できないジェネレーターでも正しく扱い、入力を丸ごと list 化しない。各出力リストは別オブジェクトとする。

### 必須確認ケース

- list(chunks(iter([1,2,3,4,5]), 2)) → [[1,2],[3,4],[5]]
- list(chunks(iter([]), 3)) → []
- size=1 → 要素ごとに1件。size=0・-1 → ValueError
- 最初の1チャンクを取得した時点で、sourceから読み取った要素数は size を超えない

### 問題コード（変更禁止）

~~~python
from collections.abc import Iterable, Iterator
from typing import TypeVar

T = TypeVar("T")


def chunks(source: Iterable[T], size: int) -> Iterator[list[T]]:
    if size < 1:
        return
    batch: list[T] = []
    for item in source:
        batch.append(item)
        if len(batch) > size:
            yield batch
            batch = []


if __name__ == "__main__":
    print(list(chunks(iter([1, 2, 3, 4, 5]), 2)))
~~~

### ★HERCULESの回答

**原因：**

1. **区切り条件の誤り**：`len(batch) > size` で yield するため、チャンクが size+1 件になります。さらに最初のチャンクを返すまでに source から size+1 件を読み取ってしまいます。
2. **末尾の不足分を捨てている**：ループ終了後に残った batch を yield していません。
3. **size<1 を検証していない**：`return` で黙って空の結果になります。しかも関数全体がジェネレーター関数なので、本体は最初の `next()` まで実行されず、呼び出し時点では何も起きません。

修正前コードで実測した結果：`list(chunks(iter([1,2,3,4,5]), 2))` は `[[1, 2, 3]]`（3件ずつで、4・5 は消失）、size=0 は例外なしで `[]` でした。

**修正版コード全文：**

~~~python
from collections.abc import Iterable, Iterator
from itertools import islice
from typing import TypeVar

T = TypeVar("T")


def chunks(source: Iterable[T], size: int) -> Iterator[list[T]]:
    # 修正: この関数自体はジェネレーター関数にしない。yield を含む関数は呼び出し時に
    # 本体が実行されないため、検証を外側に置いて「呼び出した時点」で即座に例外にする。
    # 修正: bool は int のサブクラスだが件数として不適切なので型エラーにする
    if isinstance(size, bool) or not isinstance(size, int):
        raise TypeError("size must be an int")
    # 修正: 元コードは size<1 で黙って return（空の結果）していた。source を消費する前に ValueError
    if size < 1:
        raise ValueError("size must be >= 1")
    # iter() はイテレーターを取得するだけで要素は読まない（1回しか走査できない入力もそのまま扱う）
    iterator = iter(source)
    return _generate(iterator, size)


def _generate(iterator: Iterator[T], size: int) -> Iterator[list[T]]:
    while True:
        # 修正: 元コードは len(batch) > size で判定していたため size+1 件ずつになり、
        # 末尾の不足分も yield せずに捨てていた。islice は最大 size 件だけ読み、それ以上先読みしない。
        # 毎回新しい list を作るので、各出力リストは別オブジェクトになる。
        batch = list(islice(iterator, size))
        if not batch:
            return
        # 修正: 最後の不足分（size 未満）も捨てずに返す
        yield batch


if __name__ == "__main__":
    print(list(chunks(iter([1, 2, 3, 4, 5]), 2)))
~~~

**確認方法・結果：**

- 実行環境と手順：Windows 11、Python 3.12.15（3.11 は**未実行**）。リポジトリ外の一時フォルダで `q04.py` と検証スクリプトを実行しました。読み取り件数は、1回しか走査できないジェネレーターを包んで `__next__` の回数を数えるクラスで計測しました。
- 期待値：`[[1,2],[3,4],[5]]`、`[]`、size=1 で1件ずつ。size=0・-1 は ValueError（source 未消費）。最初のチャンク取得時の読み取り件数は size 以下。
- 実測値（実行出力をそのまま転記）：

~~~text
$ python q04.py
[[1, 2], [3, 4], [5]]
[ケース1] [[1, 2], [3, 4], [5]] 期待: [[1, 2], [3, 4], [5]] 一致
[ケース2] [] 期待: [] 一致
[ケース3] size=1: [[7], [8], [9]] 期待: [[7], [8], [9]]
[ケース4] size=0: ValueError(size must be >= 1) 呼び出し時点で送出, source読み取り数=0
[ケース4] size=-1: ValueError(size must be >= 1) 呼び出し時点で送出, source読み取り数=0
  生成直後 size=1: 読み取り数=0 / 最初のチャンク=[1] 読み取り数=1 (<= size: True)
  生成直後 size=2: 読み取り数=0 / 最初のチャンク=[1, 2] 読み取り数=2 (<= size: True)
  生成直後 size=3: 読み取り数=0 / 最初のチャンク=[1, 2, 3] 読み取り数=3 (<= size: True)
  生成直後 size=5: 読み取り数=0 / 最初のチャンク=[1, 2, 3, 4, 5] 読み取り数=5 (<= size: True)
  生成直後 size=10: 読み取り数=0 / 最初のチャンク=[1, 2, 3, 4, 5, 6, 7] 読み取り数=7 (<= size: True)
[追加] 1回だけのジェネレーター: [[1, 2], [3, 4], [5]] 合計読み取り= 5
[追加] 各出力リストは別オブジェクト: True
[追加] 文字ジェネレーター size=3: [['a', 'b', 'c'], ['d', 'e']] 再走査: []
[追加] size=True: TypeError(size must be an int)
[追加] size=2.0: TypeError(size must be an int)
[追加] size='2': TypeError(size must be an int)
~~~

- 必須ケース4項目はすべて期待値どおりでした（実測）。`chunks(...)` を呼んだだけ（`next()` 前）で ValueError が出て、source の読み取り数が 0 であることを確認しました。生成直後の読み取り数も 0 で、遅延評価になっています。

---

## Q05 — TTLキャッシュとNoneの取り扱い（上級）

### 要求仕様

TTLCache(ttl_seconds, clock) は、clock() が返す浮動小数点の時刻を使うシンプルなキャッシュ。ttl_seconds は正数。get(key, loader) は同じキーの値をTTL内で再利用し、**None も有効なキャッシュ値**とする。取得時刻と保存時刻の差が **ttl以上になったら期限切れ**（境界ちょうども期限切れ）。期限切れまたは未登録時は loader(key) を1回だけ呼び、正常終了した値を、**loader完了後のclock()値**とともに保存する。loaderが例外を送出した場合はキャッシュを書き換えず、例外を伝播する。外部待機やsleepを使わず、注入されたclockで決定的に検証する。

### 必須確認ケース

- clock=0.0、ttl=5.0、loaderが "A" を返す → 初回ロード。clock=4.9ではロードなし
- clock=5.0では再ロード（境界を含む）
- loaderが None を返した場合も、期限内の2回目はloaderを呼ばない
- loaderが例外を送出したら、キャッシュに不完全な値を書かない
- ttl_seconds=0または負数 → ValueError

### 問題コード（変更禁止）

~~~python
from collections.abc import Callable
from typing import Any


class TTLCache:
    def __init__(self, ttl_seconds: float, clock: Callable[[], float]):
        self.ttl = ttl_seconds
        self.clock = clock
        self.data: dict[str, tuple[float, Any]] = {}

    def get(self, key: str, loader: Callable[[str], Any]) -> Any:
        cached = self.data.get(key)
        if cached and cached[1] is not None:
            if self.clock() - cached[0] <= self.ttl:
                return cached[1]
        value = loader(key)
        self.data[key] = (self.clock(), value)
        return value


if __name__ == "__main__":
    now = [0.0]
    cache = TTLCache(5.0, lambda: now[0])
    print(cache.get("x", lambda _: "A"))
~~~

### ★HERCULESの回答

**原因：**

1. **None をキャッシュできない**：`if cached and cached[1] is not None` により、値が None のエントリは常に未登録扱いになり、毎回 loader が呼ばれます。
2. **境界の扱いが逆**：`self.clock() - cached[0] <= self.ttl` のため、経過時間がちょうど ttl のときも有効扱いになります（仕様は「ttl以上で期限切れ」）。
3. **ttl の検証がない**：0・負数でもインスタンスが作れます（ttl<=0 では実質的にキャッシュされません）。

loader 完了後の `clock()` で保存する点と、例外時に代入まで到達しない点は元コードでも正しいので、修正版でも維持しました。

修正前コードで実測した結果：境界 clock=5.0 でのロード回数は 1（期待 2）、None の2回目でのロード回数は 2（期待 1）、`OrigTTLCache(0, clock)` は例外なく生成されました。

**修正版コード全文：**

~~~python
import math
from collections.abc import Callable
from typing import Any


class TTLCache:
    def __init__(self, ttl_seconds: float, clock: Callable[[], float]):
        # 修正: bool や数値以外は TTL として不正なので TypeError にする
        if isinstance(ttl_seconds, bool) or not isinstance(ttl_seconds, (int, float)):
            raise TypeError("ttl_seconds must be a number")
        # 修正: ttl は正数に限定。0・負数は ValueError。NaN は比較が常に偽になり
        # 永久に期限切れにならないため、これも ValueError にする
        if math.isnan(ttl_seconds) or ttl_seconds <= 0:
            raise ValueError("ttl_seconds must be > 0")
        if not callable(clock):
            raise TypeError("clock must be callable")
        self.ttl = float(ttl_seconds)
        self.clock = clock
        # 値は (保存時刻, 値)。値が None でも有効なエントリとして保持する
        self.data: dict[str, tuple[float, Any]] = {}

    def get(self, key: str, loader: Callable[[str], Any]) -> Any:
        now = self.clock()
        # 修正: 元コードは `if cached and cached[1] is not None` のため、値が None のエントリを
        # 常に未登録扱いにして毎回 loader を呼んでいた。存在判定は `in` で行い None もキャッシュする
        if key in self.data:
            stored_at, value = self.data[key]
            # 修正: 元コードは `<= ttl` で境界ちょうどを有効扱いにしていた。
            # 経過時間が ttl 以上なら期限切れなので、有効なのは `< ttl` の場合だけ
            if now - stored_at < self.ttl:
                return value
        # 期限切れ・未登録時は loader を1回だけ呼ぶ。
        # loader が例外を送出した場合は下の代入まで到達しないため、キャッシュは書き換わらず例外が伝播する
        value = loader(key)
        # 保存時刻は loader 完了後の clock() 値を使う（ロードに時間がかかった分を正しく反映する）
        self.data[key] = (self.clock(), value)
        return value


if __name__ == "__main__":
    now = [0.0]
    cache = TTLCache(5.0, lambda: now[0])
    print(cache.get("x", lambda _: "A"))
~~~

**確認方法・結果：**

- 実行環境と手順：Windows 11、Python 3.12.15（3.11 は**未実行**）。リポジトリ外の一時フォルダで `q05.py` と検証スクリプトを実行しました。時刻はリスト `now` を返す注入 clock で決定的に操作しました（sleep なし）。loader は呼び出し回数を数える呼び出し可能オブジェクトです。
- 期待値：clock=0.0 で初回ロード、4.9 でロードなし、5.0 で再ロード。None の2回目はロードなし。loader 例外時はキャッシュ不変で例外が伝播。ttl=0・負数は ValueError。
- 実測値（実行出力をそのまま転記）：

~~~text
$ python q05.py
A
[ケース1] clock=0.0 値='A' ロード回数=1 (期待1)
          clock=4.9 値='A' ロード回数=1 (期待1: ロードなし)
[ケース2] clock=5.0 値='A' ロード回数=2 (期待2: 境界で再ロード) 保存時刻=5.0
[ケース3] None: 1回目=None 2回目=None ロード回数=1 (期待1)
[ケース4] 未登録キーで例外伝播: RuntimeError('load failed') キャッシュに'e'なし: True
          期限切れキーで例外伝播: RuntimeError('load failed') キャッシュ不変: True 内容={'k': (0.0, 'old')}
          その後の再取得: 'new' ロード回数=1
[ケース5] ttl=0: ValueError(ttl_seconds must be > 0)
[ケース5] ttl=0.0: ValueError(ttl_seconds must be > 0)
[ケース5] ttl=-1: ValueError(ttl_seconds must be > 0)
[ケース5] ttl=-0.5: ValueError(ttl_seconds must be > 0)
[ケース5] ttl=nan: ValueError(ttl_seconds must be > 0)
[追加] ttl=True: TypeError(ttl_seconds must be a number)
[追加] ttl='5': TypeError(ttl_seconds must be a number)
[追加] ttl=None: TypeError(ttl_seconds must be a number)
[追加] 保存時刻(ロード完了後): 2.0 (期待2.0)
       clock=6.9 ロード回数: 0 (期待0: 2.0+5.0=7.0 未満)
       clock=7.0 ロード回数: 1 (期待1)
~~~

- 必須ケース5項目はすべて期待値どおりでした（実測）。追加ケースでは、ロード中に時計が 0.0→2.0 に進んだ場合、保存時刻が loader 完了後の 2.0 になり、期限が 7.0 になることを確認しました。
- 設計判断：bool と数値以外の ttl は「値の誤り」ではなく「型の誤り」なので TypeError にしています。`float("inf")` は「期限切れにならない正数」として受け付けます（実測：ttl=inf で clock を 1e300 に進めても、ロード回数は 1 のまま `'v'` を返しました）。

---

## Q06 — CSV集計と不正行の検出（上級）

### 要求仕様

summarize_csv(text) は標準ライブラリの csv を使い、**ヘッダーが item,qty の2列であるCSV**を読み込んで品目別合計を辞書として返す。itemは前後の半角スペースを除いて空でない文字列とし、qtyはASCII数字のみの正の整数とする。品目名が重複した場合は数量を加算する。カンマを含む引用符付きitemは正しく扱う。ヘッダーの欠落・余分な列・行の不足・余分な列・不正qty・空itemがあれば ValueError。エラー時に部分的な正常結果を返さない。

### 必須確認ケース

- "item,qty\napple,2\napple,3\nbanana,1\n" → {"apple":5,"banana":1}
- "item,qty\n\"nuts, mixed\",4\n" → {"nuts, mixed":4}
- "item,qty\napple,0\n"、"item,qty\napple,-1\n"、"item,qty\napple,2x\n" → ValueError
- "item,qty\n,2\n"、"item,qty\napple\n"、"item,qty\napple,2,unexpected\n" → ValueError
- "item,qty\n" → {}。ヘッダーが "qty,item" の場合は ValueError

### 問題コード（変更禁止）

~~~python
import csv
from io import StringIO


def summarize_csv(text: str) -> dict[str, int]:
    result: dict[str, int] = {}
    for row in csv.DictReader(StringIO(text)):
        item = row["item"].strip()
        qty = int(row["qty"])
        result[item] = qty
    return result


if __name__ == "__main__":
    print(summarize_csv("item,qty\napple,2\napple,3\nbanana,1\n"))
~~~

### ★HERCULESの回答

**原因：**

1. **同じ品目を加算せずに上書き**：`result[item] = qty` のため、apple は 2+3=5 ではなく 3 になります。
2. **列数を検証していない**：`csv.DictReader` は列が足りなければ値を `None`、余分な列は `restkey`（既定 `None`）キーに入れるだけで、エラーにしません。`apple,2,unexpected` は黙って受理されます。
3. **ヘッダーを検証していない**：DictReader は1行目を何であれヘッダーとして使います。`qty,item` の列順違いもそのまま読めてしまいます。
4. **qty の検証が不十分**：`int()` は 0・負数・`" 2"`（空白付き）・`"+2"`・`"1_000"`・全角数字を受け付けます。
5. **item の空チェックがない**：`.strip()` が空白全般を除去するうえ、空 item も受理されます。

修正前コードで実測した結果：重複は `{'apple': 3, 'banana': 1}`、`apple,0` は `{'apple': 0}`、`apple,-1` は `{'apple': -1}`、`,2` は `{'': 2}`、`apple,2,unexpected` は `{'apple': 2}`、`qty,item` ヘッダーは `{'apple': 2}`、`apple, 2` は `{'apple': 2}`（いずれも例外なし）。

**修正版コード全文：**

~~~python
import csv
from io import StringIO

EXPECTED_HEADER = ["item", "qty"]
# 修正: str.isdigit() や int() は全角数字・符号・前後空白・"1_000" なども受け付けるため、ASCII数字だけを許可する
_ASCII_DIGITS = frozenset("0123456789")


def summarize_csv(text: str) -> dict[str, int]:
    if not isinstance(text, str):
        raise TypeError("text must be str")
    # 修正: 引用符内の改行を csv モジュールに正しく解釈させるため newline="" で読む
    # 修正: DictReader は列の不足を None、余分な列を restkey(None) に入れてエラーにしないため、
    # csv.reader で各行を list として受け取り、列数とヘッダーを自分で厳密に検証する
    # 修正: strict=True で引用符の崩れた行（例: "a"b,1）も csv.Error として検出する
    reader = csv.reader(StringIO(text, newline=""), strict=True)
    # 修正: 結果は一時的な辞書に集計し、全行の検証が終わってから返す（エラー時に部分結果を返さない）
    totals: dict[str, int] = {}
    try:
        header = next(reader, None)
        # 修正: ヘッダーの欠落・列順違い・余分な列・余計な空白は ValueError（正確に item,qty の2列のみ許可）
        if header != EXPECTED_HEADER:
            raise ValueError(f"header must be exactly item,qty: {header!r}")
        for row in reader:
            line = reader.line_num
            # 修正: 列の不足（空行 [] を含む）・余分な列は ValueError
            if len(row) != 2:
                raise ValueError(f"line {line}: expected 2 columns, got {len(row)}")
            raw_item, raw_qty = row
            # 修正: item は前後の半角スペースだけを除去し、空なら ValueError
            item = raw_item.strip(" ")
            if not item:
                raise ValueError(f"line {line}: item is empty")
            # 修正: qty は ASCII 数字のみの正の整数（0、負数、"2x"、空白付き、全角数字は ValueError）
            if not raw_qty or not all(ch in _ASCII_DIGITS for ch in raw_qty):
                raise ValueError(f"line {line}: qty must be ASCII digits: {raw_qty!r}")
            qty = int(raw_qty)
            if qty <= 0:
                raise ValueError(f"line {line}: qty must be positive: {raw_qty!r}")
            # 修正: 元コードは上書き（result[item] = qty）していた。同じ品目の数量は加算する
            totals[item] = totals.get(item, 0) + qty
    except csv.Error as exc:
        # 修正: csv.Error は ValueError の派生ではないため、仕様どおり ValueError に変換して伝播する
        raise ValueError(f"malformed CSV: {exc}") from exc
    return totals


if __name__ == "__main__":
    print(summarize_csv("item,qty\napple,2\napple,3\nbanana,1\n"))
~~~

**確認方法・結果：**

- 実行環境と手順：Windows 11、Python 3.12.15（3.11 は**未実行**）。リポジトリ外の一時フォルダで `q06.py` と検証スクリプトを実行しました。
- 期待値：問題文の必須ケースどおり（2件は辞書、7件は ValueError、`"item,qty\n"` は `{}`、`qty,item` は ValueError）。
- 実測値（実行出力をそのまま転記）：

~~~text
$ python q06.py
{'apple': 5, 'banana': 1}
[必須ケース]
  'item,qty\napple,2\napple,3\nbanana,1\n' -> {'apple': 5, 'banana': 1}  期待={'apple': 5, 'banana': 1}  一致
  'item,qty\n"nuts, mixed",4\n' -> {'nuts, mixed': 4}  期待={'nuts, mixed': 4}  一致
  'item,qty\napple,0\n' -> ValueError(line 2: qty must be positive: '0')  期待=ValueError  一致
  'item,qty\napple,-1\n' -> ValueError(line 2: qty must be ASCII digits: '-1')  期待=ValueError  一致
  'item,qty\napple,2x\n' -> ValueError(line 2: qty must be ASCII digits: '2x')  期待=ValueError  一致
  'item,qty\n,2\n' -> ValueError(line 2: item is empty)  期待=ValueError  一致
  'item,qty\napple\n' -> ValueError(line 2: expected 2 columns, got 1)  期待=ValueError  一致
  'item,qty\napple,2,unexpected\n' -> ValueError(line 2: expected 2 columns, got 3)  期待=ValueError  一致
  'item,qty\n' -> {}  期待={}  一致
  'qty,item\n2,apple\n' -> ValueError(header must be exactly item,qty: ['qty', 'item'])  期待=ValueError  一致
[追加ケース]
  '' -> ValueError(header must be exactly item,qty: None)
  'item\napple\n' -> ValueError(header must be exactly item,qty: ['item'])
  'item,qty,note\napple,1,x\n' -> ValueError(header must be exactly item,qty: ['item', 'qty', 'note'])
  'item, qty\napple,1\n' -> ValueError(header must be exactly item,qty: ['item', ' qty'])
  '﻿item,qty\napple,1\n' -> ValueError(header must be exactly item,qty: ['﻿item', 'qty'])
  'item,qty\n  apple  ,2\napple,1\n' -> {'apple': 3}
  'item,qty\n   ,2\n' -> ValueError(line 2: item is empty)
  'item,qty\napple, 2\n' -> ValueError(line 2: qty must be ASCII digits: ' 2')
  'item,qty\napple,２\n' -> ValueError(line 2: qty must be ASCII digits: '２')
  'item,qty\napple,+2\n' -> ValueError(line 2: qty must be ASCII digits: '+2')
  'item,qty\napple,1_000\n' -> ValueError(line 2: qty must be ASCII digits: '1_000')
  'item,qty\napple,\n' -> ValueError(line 2: qty must be ASCII digits: '')
  'item,qty\napple,007\n' -> {'apple': 7}
  'item,qty\napple,2\n\nbanana,1\n' -> ValueError(line 3: expected 2 columns, got 0)
  'item,qty\r\napple,2\r\nbanana,1\r\n' -> {'apple': 2, 'banana': 1}
  'item,qty\n"multi\nline",3\n' -> {'multi\nline': 3}
  'item,qty\n"a"b,1\n' -> ValueError(malformed CSV: ',' expected after '"')
  'item,qty\napple,2\nbanana,0\n' -> ValueError(line 3: qty must be positive: '0')
~~~

- 必須ケース10件はすべて期待値どおりでした（実測）。最後の追加ケースのように後ろの行で不正があっても、apple の部分結果は返さず ValueError になります。
- 設計判断（仕様に明記がない点）：
  - **空行**：列数 0 の行として ValueError にしました（「行の不足」を厳格に扱うため）。末尾の改行1つは csv.reader が行を生成しないので問題ありません。
  - **BOM 付きヘッダー**：`﻿item` は item と一致しないため ValueError です。BOM 付きファイルは呼び出し側で `encoding="utf-8-sig"` で読む想定です。
  - **先頭0の qty**（`007`）：ASCII 数字のみの正の整数として 7 で受理しました。

---

## Q07 — SQLite在庫移動の原子性（上級）

### 要求仕様

move_stock(conn, sku, source, destination, amount) は inventory テーブルの同じ商品skuについて、source倉庫からdestination倉庫へ amount 個の在庫を移す。inventory の主キーは (sku,depot)、qtyは非負整数。amount は bool以外の正のintに限定。sourceとdestinationは異なる必要がある。両倉庫のレコードが存在し、sourceに十分な在庫がある時だけ **Trueを返し両方の在庫を同じトランザクションで更新**。条件を満たさなければFalseを返し、DBを変更しない。処理中にSQLiteの例外が起きた場合はロールバックして例外を伝播させる。呼び出し開始時点でconnに未完了トランザクションはないものとする。

### 必須確認ケース

- A:8、B:2、amount=3 → True、A:5・B:5（合計10を保持）
- A:8、B:2、amount=9 → False、変更なし
- destination行がない、source行がない、source==destination、amount=0・-1・True → False、変更なし
- 不意のSQLエラーが生じたときに片方の更新だけが残らない
- 何度か成功した後も在庫合計が一定、qtyが負にならない

### 問題コード（変更禁止）

~~~python
import sqlite3


def move_stock(
    conn: sqlite3.Connection,
    sku: str,
    source: str,
    destination: str,
    amount: int,
) -> bool:
    row = conn.execute(
        "SELECT qty FROM inventory WHERE sku=? AND depot=?",
        (sku, source),
    ).fetchone()
    if row is None or row[0] < amount:
        return False
    conn.execute(
        "UPDATE inventory SET qty=qty-? WHERE sku=? AND depot=?",
        (amount, sku, source),
    )
    conn.execute(
        "UPDATE inventory SET qty=qty+? WHERE sku=? AND depot=?",
        (amount, sku, destination),
    )
    conn.commit()
    return True


if __name__ == "__main__":
    db = sqlite3.connect(":memory:")
    db.execute(
        "CREATE TABLE inventory ("
        "sku TEXT NOT NULL, depot TEXT NOT NULL, "
        "qty INTEGER NOT NULL CHECK(qty >= 0), "
        "PRIMARY KEY(sku, depot))"
    )
    db.executemany(
        "INSERT INTO inventory VALUES (?, ?, ?)",
        [("P1", "A", 8), ("P1", "B", 2)],
    )
    db.commit()
    print(move_stock(db, "P1", "A", "B", 3))
    print(db.execute("SELECT depot,qty FROM inventory ORDER BY depot").fetchall())
~~~

### ★HERCULESの回答

**原因：**

1. **例外時にロールバックしない**：Python 3.11/3.12 の sqlite3 の既定（`isolation_level=""`、旧来のトランザクション制御）では、SELECT の前には BEGIN を発行せず、**最初の UPDATE の直前に暗黙の BEGIN** を発行します。2つ目の UPDATE が失敗すると例外がそのまま伝播し、1つ目の更新だけを含む未完了トランザクションが接続に残ります。後で呼び出し元が別の処理で `commit()` すると、片側だけの更新が確定します。
2. **確認と更新が同じトランザクションにない**：在庫確認の SELECT がトランザクション外なので、確認から更新までの間に他の接続が在庫を変更できます。
3. **入力検証の欠落**：destination 行の存在を確認しないため、存在しない倉庫への移動では2つ目の UPDATE が0行のまま成功扱い（在庫が消失）になります。amount の型・正数の検証もなく、source==destination も拒否しません。

修正前コードで実測した結果：

- トリガーで2つ目の UPDATE を失敗させると、`IntegrityError` の後も `in_transaction=True` が残り、A=5・B=2（3個消失）の状態が見えます。後続の `commit()` でそれが確定しました。
- destination 行がない場合は `True` を返し、A=5・B=2（3個消失）。
- amount=-1 の場合は `True` を返し、A=9・B=1（逆方向に移動）。

**修正版コード全文：**

~~~python
import sqlite3


def _rollback_quietly(conn: sqlite3.Connection, original: BaseException | None = None) -> None:
    # トランザクションが残っている場合だけ ROLLBACK する。
    # ROLLBACK 自体が失敗しても元の例外を隠さないよう、失敗内容は元の例外に注記して残す
    if not conn.in_transaction:
        return
    try:
        conn.execute("ROLLBACK")
    except sqlite3.Error as rollback_error:
        if original is None:
            raise
        original.add_note(f"ROLLBACK also failed: {rollback_error!r}")


def move_stock(
    conn: sqlite3.Connection,
    sku: str,
    source: str,
    destination: str,
    amount: int,
) -> bool:
    # 修正: amount は bool 以外の正の int に限定する（True は int のサブクラスなので type() で判定）。
    # 元コードは 0・負数・True でも在庫移動（負数なら逆方向の移動）を実行していた
    if type(amount) is not int or amount <= 0:
        return False
    # 修正: 同じ倉庫間の移動は無意味かつ誤操作なので False（DBには触れない）
    if source == destination:
        return False
    # 前提条件: 呼び出し時点で未完了トランザクションがないこと。
    # 残っていると BEGIN が失敗し、さらに後続の ROLLBACK で呼び出し元の作業まで消してしまうため、
    # DB に触れる前に検出して例外にする
    if conn.in_transaction:
        raise sqlite3.ProgrammingError("move_stock requires no open transaction on conn")

    # 修正: 元コードは SELECT（トランザクション外）で在庫を確認してから UPDATE しており、
    # 確認と更新の間に他の接続が在庫を変更できた。また2つの UPDATE の間で例外が起きると
    # 片方だけ更新された未完了トランザクションが接続に残っていた。
    # BEGIN IMMEDIATE で最初に書き込みロックを取得し、確認・2つの更新・COMMIT を1つのトランザクションにまとめる。
    # 暗黙のトランザクション制御（isolation_level や 3.12 の autocommit 設定）に依存しないよう、
    # BEGIN / COMMIT / ROLLBACK はすべて明示的な SQL で発行する
    # （3.12 の autocommit=True では conn.commit()/rollback() が何もしないため）。
    conn.execute("BEGIN IMMEDIATE")
    try:
        src = conn.execute(
            "SELECT qty FROM inventory WHERE sku=? AND depot=?",
            (sku, source),
        ).fetchone()
        # 修正: destination 行の存在も確認する（元コードは存在しない行への UPDATE が0件でも成功扱い）
        dst = conn.execute(
            "SELECT qty FROM inventory WHERE sku=? AND depot=?",
            (sku, destination),
        ).fetchone()
        if src is None or dst is None or src[0] < amount:
            # 条件を満たさない場合は何も変更せずにトランザクションを閉じる
            conn.execute("ROLLBACK")
            return False
        # 修正: 減算側にも qty>=? の条件を付け、確認後に想定外の変更があっても負にしない
        decreased = conn.execute(
            "UPDATE inventory SET qty=qty-? WHERE sku=? AND depot=? AND qty>=?",
            (amount, sku, source, amount),
        )
        increased = conn.execute(
            "UPDATE inventory SET qty=qty+? WHERE sku=? AND depot=?",
            (amount, sku, destination),
        )
        # 修正: 両方の UPDATE がちょうど1行ずつ反映された場合だけ確定する
        if decreased.rowcount != 1 or increased.rowcount != 1:
            conn.execute("ROLLBACK")
            return False
        conn.execute("COMMIT")
        return True
    except BaseException as exc:
        # 修正: SQLite のエラー（制約違反・トリガー・ロック等）や KeyboardInterrupt でも
        # 片方だけの更新を残さないよう、ロールバックしてから例外をそのまま伝播させる
        _rollback_quietly(conn, exc)
        raise


if __name__ == "__main__":
    db = sqlite3.connect(":memory:")
    db.execute(
        "CREATE TABLE inventory ("
        "sku TEXT NOT NULL, depot TEXT NOT NULL, "
        "qty INTEGER NOT NULL CHECK(qty >= 0), "
        "PRIMARY KEY(sku, depot))"
    )
    db.executemany(
        "INSERT INTO inventory VALUES (?, ?, ?)",
        [("P1", "A", 8), ("P1", "B", 2)],
    )
    db.commit()
    print(move_stock(db, "P1", "A", "B", 3))
    print(db.execute("SELECT depot,qty FROM inventory ORDER BY depot").fetchall())
~~~

**確認方法・結果：**

- 実行環境：Windows 11、Python 3.12.15、SQLite 3.53.1（Python 3.11 は**未実行**）。
- 手順：リポジトリ外の一時フォルダで `q07.py` と検証スクリプトを実行しました。DB は `:memory:`、または一時フォルダ内に `tempfile.mkdtemp` で作った一時 DB ファイルだけを使いました。
- SQL エラーの注入方法：`CREATE TRIGGER ... BEFORE UPDATE ON inventory WHEN NEW.depot='B' BEGIN SELECT RAISE(ABORT, 'injected failure'); END` で、指定倉庫の UPDATE だけを失敗させました。`RAISE(ABORT)` は文だけを中止し、トランザクションは継続します。このため、1つ目の UPDATE が残るかどうかを検証できます。ファイル DB では、**別の接続からも**内容を読み直して確認しました。
- 期待値：ケース1は True で A=5・B=5。ケース2・3は False でDB不変。SQL エラー時は例外が伝播し、両方とも更新前のまま。繰り返し後も合計が一定で、qty が負にならない。
- 実測値（実行出力をそのまま転記）：

~~~text
$ python q07.py
True
[('A', 5), ('B', 5)]
[ケース1] True [('P1', 'A', 5), ('P1', 'B', 5)] 合計 10 in_transaction: False
[ケース2] amount=9: False [('P1', 'A', 8), ('P1', 'B', 2)] in_transaction: False
[ケース3] destination行なし: False 変更なし=True in_transaction=False
[ケース3] source行なし: False 変更なし=True in_transaction=False
[ケース3] 別skuのみ存在(P2のB行なし): False 変更なし=True in_transaction=False
[ケース3] source==destination: False 変更なし=True in_transaction=False
[ケース3] amount=0: False 変更なし=True in_transaction=False
[ケース3] amount=-1: False 変更なし=True in_transaction=False
[ケース3] amount=True: False 変更なし=True in_transaction=False
[ケース3] amount=3.0: False 変更なし=True in_transaction=False
[ケース3] amount='3': False 変更なし=True in_transaction=False
[ケース4] 2つ目(destination)のUPDATEで失敗: 伝播=IntegrityError('injected failure') in_transaction=False 自接続=[('P1', 'A', 8), ('P1', 'B', 2)] 別接続=[('P1', 'A', 8), ('P1', 'B', 2)]
           トリガー削除後の再実行: True [('P1', 'A', 5), ('P1', 'B', 5)]
[ケース4] 1つ目(source)のUPDATEで失敗: 伝播=IntegrityError('injected failure') in_transaction=False 自接続=[('P1', 'A', 8), ('P1', 'B', 2)] 別接続=[('P1', 'A', 8), ('P1', 'B', 2)]
           トリガー削除後の再実行: True [('P1', 'A', 5), ('P1', 'B', 5)]
[ケース4b] CHECK違反: IntegrityError('CHECK constraint failed: qty >= 0') [('P1', 'A', 8), ('P1', 'B', 2)] in_transaction: False
[ケース4c] ロック競合: OperationalError('database is locked') in_transaction: False
            ロック解放後: True [('P1', 'A', 5), ('P1', 'B', 5)]
[ケース5] 200回: 成功 99 失敗 101 最終 [('P1', 'A', 0), ('P1', 'B', 7), ('P1', 'C', 3)] 合計 10 最小qty 0
[追加] isolation_level=None: True [('P1', 'A', 5), ('P1', 'B', 5)]
[追加] autocommit=True でのエラー注入: IntegrityError('injected failure') [('P1', 'A', 8), ('P1', 'B', 2)]
[追加] 未完了トランザクションあり: ProgrammingError('move_stock requires no open transaction on conn') 呼び出し元の未確定行は保持: True
~~~

- 補足：
  - ケース4b は、トリガーで B の qty を -1 にして CHECK 制約違反を起こしたものです。
  - ケース4c は、別の接続が `BEGIN IMMEDIATE` で書き込みロックを保持している状態で実行したものです（timeout=0.1 秒）。
  - ケース5 は、3倉庫（合計10）の間で乱数（seed=1234）による移動を200回行ったものです。
- 必須ケース5項目はすべて期待値どおりでした（実測）。既定モードに加え、`isolation_level=None` と Python 3.12 の `autocommit=True` の接続でも、原子性が保たれることを確認しました。

---

## Q08 — JSONファイルの原子的な保存（上級）

### 要求仕様

save_json(path, data) は data をUTF-8のJSONとして保存する。**同じディレクトリの一時ファイルに全部書いてから os.replace で対象を置き換え**、途中でjsonの直列化エラー・書き込みエラー等が発生した場合には**既存の対象ファイルの内容を変えず、一時ファイルも残さない**。異常は握りつぶさず伝播する。対象の親ディレクトリは存在し、通常ファイルへの保存を想定する。JSONの文字列としての日本語を読み直して一致確認する。標準ライブラリのみ使用。

### 必須確認ケース

- 既存ファイルが {"old":1}、正常に {"new":"日本語"} を保存 → 読み直して {"new":"日本語"}
- 既存ファイルがある状態で {"bad": object()} の保存を試す → TypeError、既存のバイト列は完全に不変
- 保存先が存在しない状態からの正常保存 → JSONファイルを新規作成
- 成功・失敗のいずれでも一時ファイルが残らない

### 問題コード（変更禁止）

~~~python
import json
from pathlib import Path
from typing import Any


def save_json(path: Path, data: Any) -> None:
    with path.open("w", encoding="utf-8") as stream:
        json.dump(data, stream, ensure_ascii=False)


if __name__ == "__main__":
    from tempfile import TemporaryDirectory
    with TemporaryDirectory() as folder:
        target = Path(folder) / "settings.json"
        save_json(target, {"new": "日本語"})
        print(json.loads(target.read_text(encoding="utf-8")))
~~~

### ★HERCULESの回答

**原因：**

1. `path.open("w")` は、開いた時点で**既存ファイルを長さ0に切り詰めます**。さらに `json.dump` はストリームへ少しずつ書き込みます。このため、直列化の途中で TypeError（`object()` など）が起きたり書き込みが失敗したりすると、既存ファイルが空や書きかけの不正な JSON のまま残ります。
2. 一時ファイル経由の置き換え（原子的な更新）と、`flush`/`fsync` によるディスクへの書き込み完了の保証がありません。

修正前コードで実測した結果：既存 `{"old": 1}` に `{"bad": object()}` を保存すると、TypeError の後のファイル内容は `b'{"bad": '`（壊れた JSON）でした。

**修正版コード全文：**

~~~python
import json
import os
import tempfile
from pathlib import Path
from typing import Any


def save_json(path: Path, data: Any) -> None:
    target = Path(path)
    # 修正: 元コードは path.open("w") で対象ファイルを最初に切り詰めてから書き込むため、
    # 直列化エラー（例: object()）や書き込みエラーが起きると既存ファイルが空・途中までの内容で壊れていた。
    # まずメモリ上で直列化と UTF-8 エンコードを完了させ、ここで失敗した場合はファイルに一切触れない。
    # allow_nan=False: NaN/Infinity は JSON 仕様外の値なので ValueError として拒否する
    payload = json.dumps(data, ensure_ascii=False, allow_nan=False).encode("utf-8")

    # 修正: 一時ファイルは対象と同じディレクトリに作る（os.replace を同一ボリューム内の置き換えにするため）。
    # mkstemp は既存ファイルと衝突しない名前で排他的に作成する
    fd, temp_name = tempfile.mkstemp(
        prefix=f".{target.name}.", suffix=".tmp", dir=target.parent
    )
    temp_path = Path(temp_name)
    try:
        # os.fdopen が成功した時点で fd の所有権は stream に移り、with を抜けると確実に閉じられる
        try:
            stream = os.fdopen(fd, "wb")
        except BaseException:
            os.close(fd)
            raise
        with stream:
            stream.write(payload)
            # 修正: OS のバッファまで書き出し、さらに fsync でディスクへの書き込みを完了させてから置き換える
            stream.flush()
            os.fsync(stream.fileno())
        # 修正: Windows では開いたままのファイルを置き換え・削除できないため、
        # with を抜けてハンドルを閉じた後に os.replace で対象を一括で置き換える
        os.replace(temp_path, target)
    except BaseException as exc:
        # 修正: 書き込み・fsync・置き換えのどこで失敗しても一時ファイルを削除し、例外は握りつぶさず伝播する。
        # 削除自体に失敗しても元の例外を優先し、削除失敗は注記として残す
        try:
            temp_path.unlink(missing_ok=True)
        except OSError as cleanup_error:
            exc.add_note(f"failed to remove temp file {temp_path}: {cleanup_error!r}")
        raise


if __name__ == "__main__":
    from tempfile import TemporaryDirectory
    with TemporaryDirectory() as folder:
        target = Path(folder) / "settings.json"
        save_json(target, {"new": "日本語"})
        print(json.loads(target.read_text(encoding="utf-8")))
~~~

**確認方法・結果：**

- 実行環境と手順：Windows 11（NTFS）、Python 3.12.15（3.11 は**未実行**）。リポジトリ外の一時フォルダの下に `TemporaryDirectory` を作り、その中だけで検証しました。各操作の後にディレクトリ内のファイル一覧を出力し、一時ファイル（`.settings.json.*.tmp`）が残っていないことを確認しました。
- 書き込みエラーの注入には `unittest.mock.patch` を使いました。`os.fsync` を OSError(28)、`os.replace` を PermissionError(13) にしたケースと、`os.fdopen` が返すストリームの `write` を3バイト書いた後に OSError(5) にしたケースです。
- 期待値：
  - ケース1：保存後に読み直して `{"new":"日本語"}`。
  - ケース2：TypeError で、既存のバイト列 `b'{"old": 1}'` が完全に不変。
  - ケース3：新規作成できる。
  - すべてのケースで一時ファイルが残らない。
- 実測値（実行出力をそのまま転記。最後の行のパスは一時フォルダのものなので省略しました）：

~~~text
$ python q08.py
{'new': '日本語'}
[ケース1] 読み直し: {'new': '日本語'} 一致: True UTF-8バイト列: b'{"new": "\xe6\x97\xa5\xe6\x9c\xac\xe8\xaa\x9e"}' ディレクトリ: ['settings.json']
[ケース2] TypeError: Object of type object is not JSON serializable / バイト列不変: True b'{"old": 1}' ディレクトリ: ['settings.json']
[ケース3] 保存前に存在: False / 保存後: [1, '二', None, {'x': 1.5}] ディレクトリ: ['created.json', 'settings.json']
[追加] fsync失敗: OSError([Errno 28] No space left on device (injected)) バイト列不変=True ディレクトリ=['created.json', 'settings.json']
[追加] replace失敗: PermissionError([Errno 13] Access denied (injected)) バイト列不変=True ディレクトリ=['created.json', 'settings.json']
[追加] write途中失敗: [Errno 5] I/O error (injected) バイト列不変=True ディレクトリ=['created.json', 'settings.json']
[追加] {'x': nan}: ValueError 不変=True ディレクトリ=['created.json', 'settings.json']
[追加] {'s': '\ud800'}: UnicodeEncodeError 不変=True ディレクトリ=['created.json', 'settings.json']
[追加] {(1, 2): 'tuple key'}: TypeError 不変=True ディレクトリ=['created.json', 'settings.json']
[追加] 循環参照: Circular reference detected ディレクトリ: ['created.json', 'settings.json']
[追加] strパス・大データ一致: True サイズ: 1800807 ディレクトリ: ['created.json', 'settings.json']
[追加] 対象を読み取りで開いたまま保存: PermissionError [WinError 5] アクセスが拒否されました。: '...\\.settings.json.68x_muqf.tmp' -> '...\\settings.json' ディレクトリ: ['created.json', 'settings.json']
一時ディレクトリ削除後に残骸なし: True
~~~

- 必須ケース4項目はすべて期待値どおりでした（実測）。ケース1では、保存後のバイト列が `\uXXXX` エスケープではなく UTF-8 の日本語そのものであることも確認しました。
- 制約（実測で判明した点を含む）：
  - Windows では、対象ファイルを他のプロセスなどが開いたままだと `os.replace` が PermissionError（WinError 5）になります。この場合も既存ファイルは不変で、一時ファイルは削除され、例外は伝播しました（実測）。再試行処理は仕様外なので入れていません。
  - POSIX 環境では、mkstemp が作る一時ファイルの権限は 0600 なので、置き換え後のファイル権限が元と変わることがあります。また、電源断に対する完全な耐久性には親ディレクトリの fsync も必要です。どちらも Windows 上では検証できないため**未実行**で、コードでも対応していません。

---

## Q09 — スレッド並行実行と入力順の保持（上級）

### 要求仕様

transform_all(values, fn, max_workers=3) は ThreadPoolExecutor で入力要素を並行処理する。戻り値は**入力順と同じ順番**のリストとし、各要素について成功は {"ok":True,"value":戻り値}、失敗は {"ok":False,"error":例外メッセージ}。fn が例外を送出しても残りは処理し、例外が呼び出し元へ漏れない（ただし KeyboardInterrupt 等の BaseException は対象外）。fnの戻り値がNoneでも成功とする。max_workersが1未満ならValueError。共有の結果リストへの競合した追加で並び順を決めないこと。

### 必須確認ケース

- [1,2,3] で fn(x)=x*10 → 1,2,3 の順に成功10,20,30
- 2だけが ValueError("bad 2") → 成功10、失敗"bad 2"、成功30
- 1だけ遅く終わっても戻り値は入力順を保持する
- fnがNoneを返す → {"ok":True,"value":None}
- 空入力 → []、max_workers=0 → ValueError

### 問題コード（変更禁止）

~~~python
from concurrent.futures import ThreadPoolExecutor, as_completed
from collections.abc import Callable, Iterable
from typing import Any


def transform_all(
    values: Iterable[int],
    fn: Callable[[int], Any],
    max_workers: int = 3,
) -> list[dict]:
    with ThreadPoolExecutor(max_workers=max_workers) as pool:
        futures = [pool.submit(fn, value) for value in values]
        return [{"ok": True, "value":future.result()}
                for future in as_completed(futures)]


if __name__ == "__main__":
    print(transform_all([1, 2, 3], lambda x: x * 10))
~~~

### ★HERCULESの回答

**原因：**

1. **順序の崩れ**：`as_completed(futures)` は**完了した順**に Future を返すため、結果が入力順になりません。
2. **例外の漏れ**：`future.result()` が fn の例外を再送出し、そのまま呼び出し元へ漏れます。残りの要素の結果も失われます。失敗を `{"ok": False, ...}` に変換する処理もありません。
3. **max_workers の検証**：修正前でも `max_workers=0` は ThreadPoolExecutor 自体が ValueError（`max_workers must be greater than 0`）を出していました（実測）。ただし関数としての明示的な検証ではなく、bool（True は 1 として通る）なども素通りします。

修正前コードで実測した結果：1だけ遅いと `[20, 30, 10]` の順。2が失敗すると `ValueError('bad 2')` が呼び出し元へ漏れました。

**修正版コード全文：**

~~~python
from concurrent.futures import Future, ThreadPoolExecutor
from collections.abc import Callable, Iterable
from typing import Any


def transform_all(
    values: Iterable[int],
    fn: Callable[[int], Any],
    max_workers: int = 3,
) -> list[dict]:
    # 修正: max_workers は bool 以外の int に限定し、1未満はスレッドを起動する前に ValueError にする
    if isinstance(max_workers, bool) or not isinstance(max_workers, int):
        raise TypeError("max_workers must be an int")
    if max_workers < 1:
        raise ValueError("max_workers must be >= 1")
    if not callable(fn):
        raise TypeError("fn must be callable")
    # 入力は1回しか走査できない Iterable の可能性があるため、先に確定させて順番を固定する
    items = list(values)
    if not items:
        return []

    with ThreadPoolExecutor(max_workers=max_workers) as pool:
        # 修正: 元コードは as_completed() で「完了した順」に結果を並べていたため、入力順が崩れていた。
        # Future を入力と同じ順番でリストに保持し、その順番で result() を取り出す。
        # 共有リストへスレッドから追加する方式ではないので、並び順が競合に左右されない。
        futures: list[Future[Any]] = [pool.submit(fn, value) for value in items]
        results: list[dict] = []
        try:
            for future in futures:
                try:
                    value = future.result()
                except Exception as exc:
                    # 修正: 元コードは fn の例外が result() から呼び出し元へ漏れ、残りの結果も失われていた。
                    # 通常の例外（Exception）だけを要素ごとの失敗結果に変換し、残りの要素の処理を続ける
                    results.append({"ok": False, "error": str(exc)})
                else:
                    # fn の戻り値が None でも成功として扱う
                    results.append({"ok": True, "value": value})
        except BaseException:
            # 修正: KeyboardInterrupt / SystemExit などの BaseException は変換せずに伝播させる。
            # その際、まだ開始していない処理はキャンセルし、不要な処理を走らせ続けない
            for future in futures:
                future.cancel()
            raise
    return results


if __name__ == "__main__":
    print(transform_all([1, 2, 3], lambda x: x * 10))
~~~

**確認方法・結果：**

- 実行環境と手順：Windows 11、Python 3.12.15（3.11 は**未実行**）。リポジトリ外の一時フォルダで `q09.py` と検証スクリプトを実行しました。タイミング依存の結果を確かめるため、検証スクリプト全体を計4回実行し、ケース3と同時実行ピークが4回とも同じ結果になることを確認しました。
- ケース3は sleep の長さに頼らず決定的にしました。要素1は `threading.Event` で「要素3の完了」を待ってから終わるようにし、完了順を記録しています。
- 期待値：
  - ケース1：成功 10・20・30。
  - ケース2：成功 10、失敗 "bad 2"、成功 30。
  - ケース3：1が最後に完了しても入力順。
  - ケース4：None は成功。
  - ケース5：空入力は `[]`、max_workers=0 は ValueError。
- 実測値（実行出力をそのまま転記）：

~~~text
$ python q09.py
[{'ok': True, 'value': 10}, {'ok': True, 'value': 20}, {'ok': True, 'value': 30}]
[ケース1] [{'ok': True, 'value': 10}, {'ok': True, 'value': 20}, {'ok': True, 'value': 30}]
[ケース2] [{'ok': True, 'value': 10}, {'ok': False, 'error': 'bad 2'}, {'ok': True, 'value': 30}]
[ケース3] 完了順: [2, 3, 1] 戻り値: [{'ok': True, 'value': 10}, {'ok': True, 'value': 20}, {'ok': True, 'value': 30}]
[ケース4] None: [{'ok': True, 'value': None}, {'ok': True, 'value': None}]
[ケース5] 空入力: []
[ケース5] max_workers=0: ValueError(max_workers must be >= 1)
[ケース5] max_workers=-1: ValueError(max_workers must be >= 1)
[追加] max_workers=True: TypeError(max_workers must be an int)
[追加] max_workers=2.0: TypeError(max_workers must be an int)
[追加] max_workers=None: TypeError(max_workers must be an int)
[追加] 40件/max_workers=4: 同時実行ピーク 4 入力順どおり: True
[追加] ジェネレーター入力: [{'ok': True, 'value': -5}, {'ok': True, 'value': -6}]
[追加] KeyboardInterrupt 伝播: KeyboardInterrupt('stop') 開始された要素数: 1 / 20
[追加] BaseException サブクラス伝播: Custom('base')
[追加] 終了後の残存スレッド: []
~~~

- 必須ケース5項目はすべて期待値どおりでした（実測）。
- 追加の確認：
  - 40件（7の倍数は RuntimeError）を max_workers=4 で処理し、同時実行数のピークが 4、結果が入力順どおりであることを確認しました。
  - KeyboardInterrupt は変換されずに伝播し、max_workers=1 では未開始の19件がキャンセルされました（開始は1件だけ）。
  - 終了後に残っているスレッドはありませんでした。

---

## Q10 — asyncioジョブ実行・障害分離・キャンセル清掃（最上級）

### 要求仕様

run_jobs(jobs, max_concurrency=3, timeout_s=0.1) は、引数jobsの**引数なし非同期関数**を並行実行する。各ジョブの結果は入力順に、成功={"status":"ok","value":値}、通常の例外={"status":"error","error":例外メッセージ}、タイムアウト={"status":"timeout"} として返す。個別のエラーやタイムアウトは他ジョブを中断させない。

同時に実行するジョブは最大 max_concurrency 個。**タイムアウト計測はセマフォ取得後、そのジョブを実行し始めてから**開始する。max_concurrency<1、timeout_s<=0ならValueError。外部から run_jobs 自体をキャンセルされた場合は CancelledError を伝播し、起動済みの子ジョブをキャンセル・回収し、**実行中タスクを残さない**。CancelledError を通常の失敗結果に変換してはならない。Python 3.11以上、標準ライブラリのみ。

### 必須確認ケース

- 正常ジョブ [20,22] → 入力順で ok/20、ok/22
- 2番目だけ ValueError("boom") → 1番目と3番目は成功、2番目だけerror/"boom"
- 1つだけ timeout_sを超えるジョブ → そのジョブだけtimeout、ほかは成功
- 実行中ジョブ数を計測し、どの時点でも max_concurrency 以下
- すでに実行中の長時間ジョブがあるとき、親タスクをcancel() → CancelledError が伝播、起動したジョブがキャンセルされ残存しない
- 不正な max_concurrency・timeout_s は ValueError

### 問題コード（変更禁止）

~~~python
import asyncio
from collections.abc import Awaitable, Callable
from typing import Any


async def run_jobs(
    jobs: list[Callable[[], Awaitable[Any]]],
    max_concurrency: int = 3,
    timeout_s: float = 0.1,
) -> list[dict]:
    semaphore = asyncio.Semaphore(max_concurrency)

    async def execute(job: Callable[[], Awaitable[Any]]) -> dict:
        async with semaphore:
            value = await asyncio.wait_for(job(), timeout_s)
            return {"status": "ok", "value": value}

    tasks = [asyncio.create_task(execute(job)) for job in jobs]
    return await asyncio.gather(*tasks)


if __name__ == "__main__":
    async def example() -> None:
        async def value_20() -> int:
            await asyncio.sleep(0.01)
            return 20

        async def value_22() -> int:
            await asyncio.sleep(0.01)
            return 22

        print(await run_jobs([value_20, value_22]))

    asyncio.run(example())
~~~

### ★HERCULESの回答

**原因：**

1. **障害が分離されていない**：`execute` が例外を捕捉しないため、1つのジョブの ValueError や `wait_for` の TimeoutError が `gather` からそのまま呼び出し元へ漏れます。他のジョブの結果も失われます。しかも、`gather` は最初の例外で戻るだけで、他の子タスクをキャンセルしないため、**子タスクが実行中のまま残ります**。
2. **引数を検証していない**：`max_concurrency=0` では `Semaphore(0)` が永久に取得できずハングします。`timeout_s<=0` も受け付けます。
3. **キャンセル時の後始末を保証していない**：親がキャンセルされたとき、子タスクを確実にキャンセルして回収する処理が明示されていません。子が BaseException を送出した場合の回収もありません。

元コードの `wait_for` はセマフォ取得後に開始されているので、タイムアウトの計測開始位置は正しく、修正版でも維持しました。

修正前コードで実測した結果：

- 2番目が `ValueError('boom')` だと例外が漏れ、直後に実行中の子タスクが1つ残っていました。
- タイムアウトでは `TimeoutError()` が漏れました。
- `max_concurrency=0` では、0.3秒待っても終わりませんでした（永久待ち）。

**修正版コード全文：**

~~~python
import asyncio
import math
from collections.abc import Awaitable, Callable
from typing import Any


async def run_jobs(
    jobs: list[Callable[[], Awaitable[Any]]],
    max_concurrency: int = 3,
    timeout_s: float = 0.1,
) -> list[dict]:
    # 修正: 引数をタスク生成前に検証する。元コードは max_concurrency<1 や timeout_s<=0 を受け付け、
    # Semaphore(0) では永久に待ち続け、timeout_s<=0 では全ジョブが即タイムアウト扱いになっていた
    if isinstance(max_concurrency, bool) or not isinstance(max_concurrency, int):
        raise TypeError("max_concurrency must be an int")
    if max_concurrency < 1:
        raise ValueError("max_concurrency must be >= 1")
    if isinstance(timeout_s, bool) or not isinstance(timeout_s, (int, float)):
        raise TypeError("timeout_s must be a number")
    if math.isnan(timeout_s) or timeout_s <= 0:
        raise ValueError("timeout_s must be > 0")
    job_list = list(jobs)
    for job in job_list:
        if not callable(job):
            raise TypeError("each job must be a callable without arguments")
    if not job_list:
        return []

    semaphore = asyncio.Semaphore(max_concurrency)

    async def execute(job: Callable[[], Awaitable[Any]]) -> dict:
        # 同時実行数はセマフォで max_concurrency 個までに制限する
        async with semaphore:
            # 修正: タイムアウトはセマフォ取得後、ジョブを開始する直前から計測する（順番待ちの時間を含めない）
            deadline = asyncio.timeout(timeout_s)
            try:
                async with deadline:
                    value = await job()
            except TimeoutError as exc:
                # 修正: 元コードは TimeoutError がそのまま gather へ伝播し、他ジョブの結果も失われていた。
                # このジョブのタイムアウトによるものだけを timeout とする。
                # ジョブ自身が送出した TimeoutError は通常の例外として error 扱いにする
                if deadline.expired():
                    return {"status": "timeout"}
                return {"status": "error", "error": str(exc)}
            except Exception as exc:
                # 修正: 通常の例外はそのジョブだけの error 結果に変換し、他のジョブを中断させない。
                # asyncio.CancelledError は BaseException の派生なのでここでは捕捉せず、
                # キャンセルを失敗結果に変換しない
                return {"status": "error", "error": str(exc)}
            return {"status": "ok", "value": value}

    tasks = [asyncio.create_task(execute(job)) for job in job_list]
    try:
        # gather は入力（tasks）と同じ順番で結果を返すため、入力順が保持される
        return list(await asyncio.gather(*tasks))
    except BaseException:
        # 修正: run_jobs 自体がキャンセルされた場合や、子ジョブから BaseException
        # （KeyboardInterrupt など）が出た場合に、起動済みの子タスクを残さない。
        # 全子タスクをキャンセルし、return_exceptions=True で終了まで待って回収してから、
        # 元の例外（CancelledError を含む）をそのまま再送出する
        for task in tasks:
            task.cancel()
        await asyncio.gather(*tasks, return_exceptions=True)
        raise


if __name__ == "__main__":
    async def example() -> None:
        async def value_20() -> int:
            await asyncio.sleep(0.01)
            return 20

        async def value_22() -> int:
            await asyncio.sleep(0.01)
            return 22

        print(await run_jobs([value_20, value_22]))

    asyncio.run(example())
~~~

**確認方法・結果：**

- 実行環境と手順：Windows 11、Python 3.12.15（3.11 は**未実行**。使用している `asyncio.timeout` と `Timeout.expired()` はいずれも 3.11 で追加された API です）。リポジトリ外の一時フォルダで `q10.py` と検証スクリプトを実行しました。タイミング依存の確認のため、検証スクリプト全体を計4回実行し、ケース3・ケース5・タイムアウト計測開始の確認が4回とも同じ結果になることを確認しました。
- 計測方法：
  - **同時実行数**：各ジョブの開始時に共有カウンタを +1、終了時（finally）に -1 し、開始時点の値をすべて記録して最大値を求めました。
  - **残存タスク**：`asyncio.all_tasks()` から現在のタスクを除いた集合で確認しました。
  - **親キャンセル**：`max_concurrency=2` で10秒かかるジョブを5件起動し、0.05秒後に親タスクを `cancel()` しました。
- 期待値：
  - ケース1：ok/20、ok/22。
  - ケース2：2番目だけ error/"boom"。
  - ケース3：遅いジョブだけ timeout。
  - ケース4：同時実行数は常に上限以下。
  - ケース5：CancelledError が伝播し、起動済みのジョブはキャンセルされ、残存タスクは0。
  - ケース6：不正な引数は ValueError。
- 実測値（実行出力をそのまま転記）：

~~~text
$ python q10.py
[{'status': 'ok', 'value': 20}, {'status': 'ok', 'value': 22}]
[ケース1] [{'status': 'ok', 'value': 20}, {'status': 'ok', 'value': 22}]
[ケース2] [{'status': 'ok', 'value': 1}, {'status': 'error', 'error': 'boom'}, {'status': 'ok', 'value': 3}]
[ケース3] [{'status': 'ok', 'value': 'a'}, {'status': 'timeout'}, {'status': 'ok', 'value': 'c'}] 所要 0.116s 残存子タスク: 0
[ケース4] max_concurrency=1: 計測ピーク=1 (<= 1: True) 全サンプル<=上限: True 入力順: True
[ケース4] max_concurrency=2: 計測ピーク=2 (<= 2: True) 全サンプル<=上限: True 入力順: True
[ケース4] max_concurrency=3: 計測ピーク=3 (<= 3: True) 全サンプル<=上限: True 入力順: True
[ケース4] max_concurrency=5: 計測ピーク=5 (<= 5: True) 全サンプル<=上限: True 入力順: True
[追加] max_concurrency=1, 各0.06s x4, timeout=0.1: ['ok', 'ok', 'ok', 'ok'] 合計 0.269s
[追加] タイムアウト後のセマフォ解放: [{'status': 'timeout'}, {'status': 'ok', 'value': 'next'}]
[ケース5] キャンセル前: 開始済み=[0, 1] 子タスク数=5
[ケース5] CancelledError 伝播: parent.cancelled()=True キャンセルされたジョブ=[0, 1] 未開始のまま=[2, 3, 4] 完了扱い=[]
          子タスク全終了=True 全てcancelled=True 残存タスク数=0
[ケース6] {'max_concurrency': 0}: ValueError(max_concurrency must be >= 1) 残存タスク数=0
[ケース6] {'max_concurrency': -1}: ValueError(max_concurrency must be >= 1) 残存タスク数=0
[ケース6] {'timeout_s': 0}: ValueError(timeout_s must be > 0) 残存タスク数=0
[ケース6] {'timeout_s': -0.1}: ValueError(timeout_s must be > 0) 残存タスク数=0
[ケース6] {'timeout_s': nan}: ValueError(timeout_s must be > 0) 残存タスク数=0
[追加] {'max_concurrency': True}: TypeError(max_concurrency must be an int)
[追加] {'max_concurrency': 2.0}: TypeError(max_concurrency must be an int)
[追加] {'timeout_s': '1'}: TypeError(timeout_s must be a number)
[追加] {'timeout_s': True}: TypeError(timeout_s must be a number)
[追加] ジョブ内CancelledError: 結果に変換されず CancelledError として伝播 残存タスク数= 0
[追加] ジョブ自身のTimeoutError: [{'status': 'error', 'error': 'upstream timed out'}]
[追加] BaseException 伝播: Fatal('fatal') 残存タスク数= 0
[追加] 空入力: []
[追加] 非awaitableを返すジョブ: [{'status': 'error', 'error': "object int can't be used in 'await' expression"}]
~~~

- 必須ケース6項目はすべて期待値どおりでした（実測）。4回の実行で、ケース3の所要時間は 0.101〜0.116s の範囲でした。
- 「タイムアウトはセマフォ取得後に開始」の確認：`max_concurrency=1`、各0.06秒のジョブ4件、timeout=0.1 で実行しました。合計所要時間（約0.27s）は timeout を超えていますが、全件 ok になりました。順番待ちの時間がタイムアウトに含まれていないことを示しています。
- 親キャンセル時の確認：実行中だった2件（0, 1）がジョブ内で CancelledError を受け取り、未開始の3件（2, 3, 4）はジョブ本体が一度も開始されませんでした。子タスク5件はすべて done かつ cancelled で、残存タスクは 0 でした。

---

## 確認テスト提出報告（チャットで報告）

- 回答した問題数：Q01〜Q10のうち何問
- 検証済み件数／未実行件数：各件数と未実行の理由
- ローカルのコミットSHA：値を記載
- GitHub push：所有者が許可した場合のみ実行・報告
- 採点・合否：クライアント側で実施

**★HERCULESへの最終指示：これはPythonの確認テストです。既存の問題コードを直接変更せず、Q01から順番に分析・修正・検証し、指定回答欄だけに報告してください。**
