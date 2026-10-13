# Client Order VELKRIS — Python Ability Test (10 Questions)

> **利用条件：All Rights Reserved — Copyright (c) 2026 Phantom Nightmare.** 本問題集は新規のオリジナル能力テストです。法令・GitHub利用規約で認められる場合を除き、無断転載・再配布・改変・他のテストへの転用には所有者の事前許可が必要です。所有者が指示するVELKRISのテスト実施と指定回答欄への記入は許可します。[利用条件全文](../../LICENSE.md)

## クライアントオーダー / Client Order

- **クライアント：** ワトソン（架空のクライアント）
- **依頼先：** VELKRIS
- **対象：** Pythonの参照・コピー・クロージャ・日時・ストリーミング・Decimal・SQLite・パス安全性・非同期処理に関する能力テスト10問
- **難易度：** Q01が中級、Q10が最上級。C#・C++テストとは別内容で、複合的な条件を追加
- **実行環境：** Python 3.11以上、標準ライブラリのみ。Visual Studio Code等で検証可能

### 依頼内容
Q01からQ10まで順番に、各問題の不具合を特定し、要求仕様を満たす**修正版コード全文、変更理由、検証方法と結果**を、このファイルの各回答欄へ記入してください。必要な調査、独立した一時ファイルでのテスト、エラー修正、再検証は可能な範囲で自律的に実施してください。

### 必須ルール
1. 各問題の**要求仕様と問題コードは編集・削除禁止**。変更できるのはこのファイルの各「VELKRISの回答」欄だけです。
2. 回答には **原因／修正版コード全文／確認方法・結果** を必ず記載してください。コード変更箇所には `#` コメントで理由を記載します。
3. 各問題は**独立したPythonプログラム**です。問題コードをひとつの実行ファイルに結合しないでください。
4. 検証にはリポジトリ外の一時ファイル／一時ディレクトリを使い、GitHubには提出しないでください。元コードが危険なファイル操作やデータ変更につながる場合は、安全なテスト環境で検証してください。
5. 各必須ケースについて**期待値と実測結果を明確に区別**してください。実行できなかった項目は「未実行」、理由、再現手順、予想結果を記載し、実行済みと偽らないこと。
6. 提出時には本ファイルの**回答欄以外を変更しない**でください。他のC#・C++テスト、README、LICENSE、エージェント設定、認証情報、APIキーを変更・公開しないこと。
7. 全10問の回答を確認してローカルでコミットし、**コミットSHAと回答数**を報告してください。**GitHubへのpushは所有者から明示的に指示された場合のみ**実施してください。
8. **VELKRIS自身による得点や合否の判定は禁止**。最終採点はクライアント（レビュー担当）が行います。
9. 外部ネットワーク、外部サービス、有料ライブラリは使用しないでください。第三者のコードの無断転載を避けてください。

### 採点（100点満点）
各問10点（**原因の特定2点／要求仕様を満たす修正版コード6点／検証方法・結果2点**）。合計100点。要求された複数ケースへの適合、安全性、再現できる検証内容を評価します。未実行項目は未実行の申告と妥当な検証手順・予想結果で評価します。

---

## Q01 — 可変デフォルト引数と呼び出し間の状態分離（中級）

### 要求仕様
関数 collect(label, items=None) 相当の機能を実装し、既存のリストを受け取った場合はその内容を保った新しいリストへ label を末尾追加して返してください。元のリストは変更禁止です。items を省略した場合は毎回独立した新しいリストを返してください。戻り値は引数で受け取ったリストと同一オブジェクトであってはいけません。

必須ケース：collect("A") → ["A"]、続けて collect("B") → ["B"]。seed=["x"] に対して collect("y",seed) → ["x","y"] かつ seed は ["x"] のまま。空リストを明示的に渡した場合も正常動作してください。

### 問題コード（変更禁止）
```python
def collect(label: str, items: list[str] = []) -> list[str]:
    items.append(label)
    return items


if __name__ == "__main__":
    print(collect("A"))
    print(collect("B"))
```

### VELKRISの回答
**原因：** デフォルト引数 `items: list[str] = []` は関数定義時に1回だけ評価され、同じリストオブジェクトが全呼び出しで共有される。さらに `items.append(label)` で引数のリストを直接変更してそのまま返すため、①省略時の呼び出し間で状態が蓄積し、②渡した seed が書き換えられ、③戻り値が引数と同一オブジェクトになる。（元コード実測：`collect("A")`→`collect("B")` 後はどちらの戻り値も同一リスト `['A', 'B']`、`collect("y", seed)` 後に seed が `['x', 'y']`、`r is seed` が True）

**修正版コード：**
```python
def collect(label: str, items: list[str] | None = None) -> list[str]:
    # デフォルト値の [] は関数定義時に1回だけ生成され全呼び出しで共有されるため、None を既定値にする
    # 受け取ったリストは変更せず、内容をコピーした新しいリストを作る（省略時は呼び出しごとに新しい空リスト）
    result = [] if items is None else list(items)
    result.append(label)
    return result


if __name__ == "__main__":
    print(collect("A"))
    print(collect("B"))
```

**確認方法・結果：**
- 検証環境：Windows 11 Pro、Python 3.14.7（vcpkg 同梱の CPython）と Python 3.11.4（Android NDK 同梱の CPython）。リポジトリ外の一時ディレクトリ（Claude scratchpad 配下の `velkris-python\`）に修正版 `qNN.py` とテスト `test_qNN.py` を問題ごとに別ファイルで作成して実行。元の問題コードも同ディレクトリに別ファイルとして抽出し、不具合の再現に使用。
- 実行：`python test_q01.py`（3.14.7 / 3.11.4 の両方で実行済み）

| ケース | 期待値 | 実測結果 |
|---|---|---|
| `collect("A")` | `["A"]` | `['A']` |
| 続けて `collect("B")` | `["B"]` | `['B']`（1回目の戻り値も `['A']` のまま、別オブジェクト） |
| `seed=["x"]` で `collect("y", seed)` | `["x","y"]`、seed は `["x"]` | `['x', 'y']`、seed は `['x']`、`r is seed` は False |
| 空リスト `e=[]` を明示して `collect("z", e)` | `["z"]`、e は `[]`、別オブジェクト | `['z']`、e は `[]`、`r is e` は False |
| その後の省略呼び出し `collect("C")` | `["C"]` | `['C']` |

- 実行結果：3.14.7・3.11.4 とも全11項目で期待値と実測が一致。

---

## Q02 — 深いコピーとネストしたデータの非破壊更新（中級）

### 要求仕様
apply_discount(cart, percent) は商品辞書のリストから、割引後の **独立した新しいデータ構造** を返してください。各辞書の price（0以上の整数）に percent％ の割引を適用し、price 以外のキーと値は保ちます。入力 cart も、その内部にある辞書・リストも変更禁止です。返却データを後から編集しても入力側には影響してはいけません。

本テストでは percent は 0 または 10、元の price は10の倍数とし、割引後も整数です。

必須ケース：cart=[{"sku":"A","price":200,"meta":{"tags":["new"]}},{"sku":"B","price":100,"meta":{"tags":[]}}]、percent=10 → 新しい price は180と90。返却側の meta.tags に値を追加しても元の cart は変化しないこと。空リストは空リストを返してください。

### 問題コード（変更禁止）
```python
def apply_discount(cart: list[dict], percent: int) -> list[dict]:
    copy = cart.copy()
    for item in copy:
        item["price"] = item["price"] * (100 - percent) // 100
    return copy


if __name__ == "__main__":
    cart = [{"sku": "A", "price": 200, "meta": {"tags": ["new"]}}]
    print(apply_discount(cart, 10))
    print(cart)
```

### VELKRISの回答
**原因：** `cart.copy()` は外側のリストだけを複製する浅いコピーで、中の辞書（およびその中の `meta` 辞書・`tags` リスト）は入力と同じオブジェクトを共有している。そのため `item["price"] = ...` が入力 cart の辞書を直接書き換え、返却データのネストした値を編集しても入力側に波及する。（元コード実測：呼び出し後の cart の price が 200→180 に変化）

**修正版コード：**
```python
import copy  # ネストした辞書・リストまで複製するために deepcopy を使う


def apply_discount(cart: list[dict], percent: int) -> list[dict]:
    # list.copy() は外側のリストだけの浅いコピーで、中の辞書は入力と共有されていたため deepcopy に変更
    discounted = copy.deepcopy(cart)
    for item in discounted:
        # 複製側の辞書だけを書き換えるので、入力 cart の price は変化しない
        item["price"] = item["price"] * (100 - percent) // 100
    return discounted


if __name__ == "__main__":
    cart = [{"sku": "A", "price": 200, "meta": {"tags": ["new"]}}]
    print(apply_discount(cart, 10))
    print(cart)
```

**確認方法・結果：**
- 検証環境：Windows 11 Pro、Python 3.14.7（vcpkg 同梱の CPython）と Python 3.11.4（Android NDK 同梱の CPython）。リポジトリ外の一時ディレクトリ（Claude scratchpad 配下の `velkris-python\`）に修正版 `qNN.py` とテスト `test_qNN.py` を問題ごとに別ファイルで作成して実行。元の問題コードも同ディレクトリに別ファイルとして抽出し、不具合の再現に使用。
- 実行：`python test_q02.py`（3.14.7 / 3.11.4 の両方で実行済み）

| ケース | 期待値 | 実測結果 |
|---|---|---|
| 必須ケースの cart、percent=10 の price | `[180, 90]` | `[180, 90]` |
| price 以外のキー・値（sku, meta） | 元のまま | `('A', {'tags': ['new']}), ('B', {'tags': []})` |
| 呼び出し直後の入力 cart | 変化なし | 呼び出し前の deepcopy スナップショットと等しい |
| 返却側の `meta.tags` に追加・sku 変更・要素追加をした後の cart | 変化なし | スナップショットと等しい |
| 返却側と入力側の外側リスト／辞書／meta／tags の同一性 | すべて別オブジェクト | `is` 比較がすべて False |
| percent=0 | `[200, 100]` | `[200, 100]` |
| 空リスト | `[]`（新しいオブジェクト） | `[]`、`r is e` は False |

- 実行結果：3.14.7・3.11.4 とも全9項目で期待値と実測が一致。

---

## Q03 — クロージャとループ変数の遅延束縛（中級〜上級）

### 要求仕様
build_thresholds(limits) は、limits の各整数について「入力値がそのしきい値以上なら True」を返す関数を1つずつ生成し、同じ順番でリストにして返してください。各関数は独立したしきい値を保持し、生成後の呼び出し結果がループ終了時の変数に左右されてはいけません。

必須ケース：limits=[2,5,8] で作った3関数へ 6 を渡す → [True,True,False]。5 を渡す → [True,True,False]。8 を渡す → [True,True,True]。空の limits → 空リスト。別の build_thresholds 呼び出しの影響を受けないこと。

### 問題コード（変更禁止）
```python
def build_thresholds(limits: list[int]):
    rules = []
    for limit in limits:
        rules.append(lambda value: value >= limit)
    return rules


if __name__ == "__main__":
    rules = build_thresholds([2, 5, 8])
    print([rule(6) for rule in rules])
```

### VELKRISの回答
**原因：** ループ内の `lambda value: value >= limit` は `limit` の値ではなく変数そのものを参照するクロージャ（遅延束縛）で、呼び出し時点で `limit` を読み出す。ループ終了後の `limit` は最後の値 8 なので、3つの関数がすべて `value >= 8` として動作する。（元コード実測：6 を渡すと `[False, False, False]`）

**修正版コード：**
```python
from collections.abc import Callable


def _make_rule(limit: int) -> Callable[[int], bool]:
    # 呼び出しごとに新しいスコープを作り、その時点の limit をクロージャに固定する
    # （ループ内の lambda はループ変数 limit を遅延参照し、全関数が最後の値を見ていた）
    def rule(value: int) -> bool:
        return value >= limit

    return rule


def build_thresholds(limits: list[int]) -> list[Callable[[int], bool]]:
    # しきい値ごとに独立した関数を生成し、入力と同じ順番で返す
    return [_make_rule(limit) for limit in limits]


if __name__ == "__main__":
    rules = build_thresholds([2, 5, 8])
    print([rule(6) for rule in rules])
```

**確認方法・結果：**
- 検証環境：Windows 11 Pro、Python 3.14.7（vcpkg 同梱の CPython）と Python 3.11.4（Android NDK 同梱の CPython）。リポジトリ外の一時ディレクトリ（Claude scratchpad 配下の `velkris-python\`）に修正版 `qNN.py` とテスト `test_qNN.py` を問題ごとに別ファイルで作成して実行。元の問題コードも同ディレクトリに別ファイルとして抽出し、不具合の再現に使用。
- 実行：`python test_q03.py`（3.14.7 / 3.11.4 の両方で実行済み）

| ケース | 期待値 | 実測結果 |
|---|---|---|
| `limits=[2,5,8]` に 6 | `[True,True,False]` | `[True, True, False]` |
| 5 | `[True,True,False]` | `[True, True, False]` |
| 8 | `[True,True,True]` | `[True, True, True]` |
| 1（追加） | `[False,False,False]` | `[False, False, False]` |
| 空の limits | `[]` | `[]` |
| 別の `build_thresholds([100])` 呼び出し後に元の関数へ 6 | `[True,True,False]` | `[True, True, False]`（別呼び出しの関数は 99→False / 100→True） |
| 生成後に入力リスト `limits` を変更（追加） | 影響なし | `[True, True]`（変更前のしきい値を保持） |

- 実行結果：3.14.7・3.11.4 とも全8項目で期待値と実測が一致。

---

## Q04 — タイムゾーン付き日時の正規化と安定ソート（上級）

### 要求仕様
sort_utc_events(events) は (名前, ISO 8601日時文字列) のタプル列を受け取り、実際の時刻（UTCの同一時系列）で古い順に並べた **名前のリスト** を返してください。UTCオフセット付き日時と末尾 Z のUTC表記を受け入れます。同一時刻の場合は入力順を維持してください。タイムゾーン情報がない日時や不正な日時では ValueError を送出してください。入力リストは変更禁止です。

必須ケース：
[("A","2026-02-10T09:00:00+09:00"),("B","2026-02-10T00:30:00+00:00"),("C","2026-02-09T23:45:00-01:00"),("D","2026-02-10T00:30:00Z")] → ["A","B","D","C"]。日時が "2026-02-10T09:00:00" の場合は ValueError。空入力 → []。Python 3.11以上の標準ライブラリを使用してください。

### 問題コード（変更禁止）
```python
def sort_utc_events(events: list[tuple[str, str]]) -> list[str]:
    ordered = sorted(events, key=lambda item: item[1])
    return [name for name, _ in ordered]


if __name__ == "__main__":
    events = [
        ("A", "2026-02-10T09:00:00+09:00"),
        ("B", "2026-02-10T00:30:00+00:00"),
        ("C", "2026-02-09T23:45:00-01:00"),
        ("D", "2026-02-10T00:30:00Z"),
    ]
    print(sort_utc_events(events))
```

### VELKRISの回答
**原因：** 日時を解析せず ISO 8601 文字列のまま辞書順でソートしているため、UTC オフセットの違いが無視され、実際の時刻順にならない。また、タイムゾーンなしの日時や不正な文字列も検証されず、ValueError にならない。（元コード実測：必須ケースが `['C', 'B', 'D', 'A']`、`"2026-02-10T09:00:00"` も例外なく `['A']` を返す）

**修正版コード：**
```python
from datetime import datetime, timezone


def _to_utc(text: str) -> datetime:
    # 文字列比較ではオフセットの違いを考慮できないため、日時として解析して UTC に正規化する
    if not isinstance(text, str):
        raise ValueError(f"datetime must be a string: {text!r}")
    try:
        # Python 3.11 以降の fromisoformat は末尾 Z と UTC オフセット表記を受け付ける
        moment = datetime.fromisoformat(text)
    except ValueError as exc:
        raise ValueError(f"invalid ISO 8601 datetime: {text!r}") from exc
    # タイムゾーン情報のない日時は実時刻を確定できないため拒否する
    if moment.tzinfo is None or moment.utcoffset() is None:
        raise ValueError(f"datetime without timezone: {text!r}")
    return moment.astimezone(timezone.utc)


def sort_utc_events(events: list[tuple[str, str]]) -> list[str]:
    # 先に全件を検証・変換してから並べ替える（入力リスト自体は変更しない）
    keyed = [(_to_utc(when), name) for name, when in events]
    # sort は安定ソートなので、UTC 時刻が同じ要素は入力順を維持する（キーは時刻のみ）
    keyed.sort(key=lambda pair: pair[0])
    return [name for _, name in keyed]


if __name__ == "__main__":
    events = [
        ("A", "2026-02-10T09:00:00+09:00"),
        ("B", "2026-02-10T00:30:00+00:00"),
        ("C", "2026-02-09T23:45:00-01:00"),
        ("D", "2026-02-10T00:30:00Z"),
    ]
    print(sort_utc_events(events))
```

**確認方法・結果：**
- 検証環境：Windows 11 Pro、Python 3.14.7（vcpkg 同梱の CPython）と Python 3.11.4（Android NDK 同梱の CPython）。リポジトリ外の一時ディレクトリ（Claude scratchpad 配下の `velkris-python\`）に修正版 `qNN.py` とテスト `test_qNN.py` を問題ごとに別ファイルで作成して実行。元の問題コードも同ディレクトリに別ファイルとして抽出し、不具合の再現に使用。
- 実行：`python test_q04.py`（3.14.7 / 3.11.4 の両方で実行済み。3.11 の `fromisoformat` で末尾 `Z` が受理されることも確認）

| ケース | 期待値 | 実測結果 |
|---|---|---|
| 必須ケース A/B/C/D | `["A","B","D","C"]` | `['A', 'B', 'D', 'C']`（UTC で A=00:00, B=D=00:30, C=00:45） |
| 入力リスト | 変更なし | 呼び出し前のコピーと等しい |
| 同一時刻を D, B の順で入力（追加） | `["D","B"]`（入力順維持） | `['D', 'B']` |
| +09:00 / Z / -05:00 で同一時刻の3件（追加） | 入力順 `["x","y","z"]` | `['x', 'y', 'z']` |
| `"2026-02-10T09:00:00"`（タイムゾーンなし） | ValueError | ValueError |
| `"2026-13-01T00:00:00Z"`・`"not-a-date"`・空文字・None（追加） | ValueError | すべて ValueError |
| 空入力 | `[]` | `[]` |

- 実行結果：3.14.7・3.11.4 とも全10項目で期待値と実測が一致。

---

## Q05 — 一度きりのジェネレーターとストリーミング集計（上級）

### 要求仕様
summarize_positive(values) は整数を順番に1回だけ読み取り、**正の整数（0より大きい値）だけ**の (件数, 合計, 最大値) を返してください。正の整数が1件もない場合は (0,0,None) を返します。values は再走査できないジェネレーターやイテレーターの場合があり、**全件を list や tuple に展開してはいけません**。入力値はすべて int とします。

必須ケース：iter([3,-2,5,0]) → (2,8,5)、(n for n in [-4,0]) → (0,0,None)、iter([]) → (0,0,None)、iter([7]) → (1,7,7)。

### 問題コード（変更禁止）
```python
def summarize_positive(values):
    positives = (number for number in values if number > 0)
    count = sum(1 for _ in positives)
    total = sum(positives)
    maximum = max(positives, default=None)
    return count, total, maximum


if __name__ == "__main__":
    print(summarize_positive(iter([3, -2, 5, 0])))
```

### VELKRISの回答
**原因：** `positives` はジェネレーター式で、1回しか走査できない。`count = sum(1 for _ in positives)` で最後まで消費されるため、その後の `sum(positives)` は 0、`max(positives, default=None)` は None になる。（元コード実測：`iter([3,-2,5,0])` → `(2, 0, None)`）

**修正版コード：**
```python
from collections.abc import Iterable


def summarize_positive(values: Iterable[int]) -> tuple[int, int, int | None]:
    # ジェネレーターは1回しか走査できないため、1パスで件数・合計・最大値を同時に更新する
    # （元コードは count の計算で positives を使い切り、total と maximum が空になっていた）
    count = 0
    total = 0
    maximum = None
    for number in values:  # 1件ずつ読むだけで list や tuple には展開しない
        if number > 0:  # 0 と負数は集計対象外
            count += 1
            total += number
            if maximum is None or number > maximum:
                maximum = number
    return count, total, maximum


if __name__ == "__main__":
    print(summarize_positive(iter([3, -2, 5, 0])))
```

**確認方法・結果：**
- 検証環境：Windows 11 Pro、Python 3.14.7（vcpkg 同梱の CPython）と Python 3.11.4（Android NDK 同梱の CPython）。リポジトリ外の一時ディレクトリ（Claude scratchpad 配下の `velkris-python\`）に修正版 `qNN.py` とテスト `test_qNN.py` を問題ごとに別ファイルで作成して実行。元の問題コードも同ディレクトリに別ファイルとして抽出し、不具合の再現に使用。
- 実行：`python test_q05.py`（3.14.7 / 3.11.4 の両方で実行済み）

| ケース | 期待値 | 実測結果 |
|---|---|---|
| `iter([3,-2,5,0])` | `(2,8,5)` | `(2, 8, 5)` |
| `(n for n in [-4,0])` | `(0,0,None)` | `(0, 0, None)` |
| `iter([])` | `(0,0,None)` | `(0, 0, None)` |
| `iter([7])` | `(1,7,7)` | `(1, 7, 7)` |
| `iter([-1,-9,2])`（追加） | `(1,2,2)` | `(1, 2, 2)` |
| 読み取り回数を数える1回限りのイテレーター（追加） | 各要素を1回ずつ読む（読取4回・`iter()` 1回） | 読取4回・`iter()` 1回、結果 `(2, 8, 5)` |
| 100万件のジェネレーター（追加・list 化していないことの確認） | 結果 `(499999, 124999750000, 499999)`、追加メモリが小さい（基準 100KB 未満） | 結果一致、tracemalloc のピークは 3.14.7 で 448 バイト、3.11.4 で 592 バイト |

- 実行結果：3.14.7・3.11.4 とも全9項目で期待値と実測が一致。

---

## Q06 — Decimalによる端数処理と金額検証（上級）

### 要求仕様
calculate_total(subtotal, rate) は文字列で渡された小計と税率から税込総額を計算し、**小数点以下が必ず2桁の文字列**を返してください。rate は 0～1 の割合（例：0.075 = 7.5％）です。計算に float を使わず decimal.Decimal を使用し、最終金額は ROUND_HALF_UP（通常の四捨五入）で0.01単位に丸めてください。

入力は10進の非負数文字列です。subtotal は整数部分最大10桁・小数部最大3桁、rate は0～1で小数部最大4桁。負値、NaN、Infinity、空文字、不正な文字列、および範囲外の rate は ValueError にしてください。表記は数字と任意の小数点のみとし、符号・指数表記・空白は許可しません。

必須ケース：("1.005","0") → "1.01"、("0.10","0.10") → "0.11"、("100.00","0.075") → "107.50"、("0","0") → "0.00"、("-1","0.1")・("1","1.1")・("NaN","0")・("","0") → ValueError。

### 問題コード（変更禁止）
```python
def calculate_total(subtotal: str, rate: str) -> str:
    result = round(float(subtotal) * (1 + float(rate)), 2)
    return f"{result:.2f}"


if __name__ == "__main__":
    print(calculate_total("1.005", "0"))
```

### VELKRISの回答
**原因：** float で計算しているため、`1.005` が2進数で `1.00499999...` として表され、さらに `round()` は偶数丸め（銀行丸め）なので `1.01` にならない。また入力検証がなく、負値・`NaN`・`Infinity`・指数表記・範囲外の rate を受け付けてしまう（空文字は float 変換の ValueError になるだけで、仕様上の検証ではない）。（元コード実測：`("1.005","0")` → `"1.00"`、`("-1","0.1")` → `"-1.10"`、`("NaN","0")` → `"nan"`）

**修正版コード：**
```python
import re
from decimal import ROUND_HALF_UP, Decimal

# float は 1.005 などを2進数で正確に表せず、round() も偶数丸めのため Decimal と ROUND_HALF_UP を使う
# 書式は ASCII 数字と任意の小数点のみ（符号・指数・空白・NaN・Infinity・"1."・".5" は不一致で拒否）
_SUBTOTAL_PATTERN = re.compile(r"[0-9]{1,10}(?:\.[0-9]{1,3})?")  # 整数部最大10桁・小数部最大3桁
_RATE_PATTERN = re.compile(r"[0-9]+(?:\.[0-9]{1,4})?")  # 小数部最大4桁（範囲 0～1 は数値で確認）
_CENT = Decimal("0.01")


def _parse(text: str, pattern: re.Pattern[str], name: str) -> Decimal:
    # Decimal() は "NaN"・"Infinity"・"-1"・"1e2"・前後の空白を受け付けるため、変換前に書式を厳密に検証する
    # fullmatch を使い、"$" が末尾改行の直前にも一致する問題を避ける
    if not isinstance(text, str) or pattern.fullmatch(text) is None:
        raise ValueError(f"invalid {name}: {text!r}")
    return Decimal(text)


def calculate_total(subtotal: str, rate: str) -> str:
    amount = _parse(subtotal, _SUBTOTAL_PATTERN, "subtotal")
    ratio = _parse(rate, _RATE_PATTERN, "rate")
    if ratio > 1:  # 書式上負値は来ないため、上限だけを確認する
        raise ValueError(f"rate out of range: {rate!r}")
    # 入力は最大13桁×5桁なので既定精度（28桁）で丸め前の積は誤差なく計算できる
    total = amount * (Decimal(1) + ratio)
    # 最終金額だけを0.01単位に通常の四捨五入で丸め、小数点以下2桁の文字列にする
    return str(total.quantize(_CENT, rounding=ROUND_HALF_UP))


if __name__ == "__main__":
    print(calculate_total("1.005", "0"))
```

**確認方法・結果：**
- 検証環境：Windows 11 Pro、Python 3.14.7（vcpkg 同梱の CPython）と Python 3.11.4（Android NDK 同梱の CPython）。リポジトリ外の一時ディレクトリ（Claude scratchpad 配下の `velkris-python\`）に修正版 `qNN.py` とテスト `test_qNN.py` を問題ごとに別ファイルで作成して実行。元の問題コードも同ディレクトリに別ファイルとして抽出し、不具合の再現に使用。
- 入力書式の扱い：ASCII の数字と小数点のみを許可し、小数点の前後には必ず1桁以上の数字が必要とした（`"1."` と `".5"` は ValueError）。整数部の先頭ゼロ（例 `"007"`）は許可。全角数字・桁区切りカンマ・前後空白・末尾改行も ValueError。
- 実行：`python test_q06.py`（3.14.7 / 3.11.4 の両方で実行済み）

| ケース | 期待値 | 実測結果 |
|---|---|---|
| `("1.005","0")` | `"1.01"` | `'1.01'` |
| `("0.10","0.10")` | `"0.11"` | `'0.11'` |
| `("100.00","0.075")` | `"107.50"` | `'107.50'` |
| `("0","0")` | `"0.00"` | `'0.00'` |
| `("1","1")`（rate 上限・追加） | `"2.00"` | `'2.00'` |
| `("9999999999.999","0.9999")`（最大桁・追加。整数演算で 19998999999.9980001 を算出し期待値とした） | `"19999000000.00"` | `'19999000000.00'` |
| `("2.675","0")`（追加） | `"2.68"` | `'2.68'` |
| `("-1","0.1")`・`("1","1.1")`・`("NaN","0")`・`("","0")` | ValueError | すべて ValueError |
| 追加の不正入力：`("1","")`・`"Infinity"`・rate `"NaN"`・`"1e2"`・`" 1"`・`"1 "`・`"1\n"`・`"+1"`・`"1."`・`".5"`・11桁整数・小数4桁の subtotal・小数5桁の rate・rate `"-0.1"`・rate `"1.0001"`・全角 `"１"`・`"1,000"`・`"abc"`・非文字列 `1.0` | ValueError | すべて ValueError |

- 実行結果：3.14.7・3.11.4 とも全30項目で期待値と実測が一致。

---

## Q07 — SQLite残高移動の検証とトランザクション（上級）

### 要求仕様
transfer(conn, sender, receiver, amount) は SQLite の accounts(account_id INTEGER PRIMARY KEY, balance INTEGER NOT NULL) テーブルで、指定した2口座間の金額を移動してください。sender/receiver は異なる既存口座ID、amount は1以上の整数とします。送金元の残高不足、存在しない口座、同一口座、amount が0以下の場合は ValueError を送出してください。

**成功時だけ両残高を変更してコミットし、失敗時はどちらの残高も元どおり**にしてください。途中の SQL 例外でもロールバックして不完全な更新を残さないこと。SQL文にはプレースホルダーを使用してください。関数呼び出し前に conn はトランザクション外にあるものとし、呼び出し後は成功・失敗を問わずトランザクション外に戻すこと。単一コネクションで検証します。

必須ケース：ID1=100、ID2=20 から 40 移動 → (60,60)。残高を超える500の移動、存在しないID99への移動、amount=0、送金元と送金先が同じ場合 → ValueError かつ残高に変更なし。

### 問題コード（変更禁止）
```python
import sqlite3


def transfer(conn: sqlite3.Connection, sender: int, receiver: int, amount: int) -> None:
    conn.execute(f"UPDATE accounts SET balance = balance - {amount} WHERE account_id = {sender}")
    conn.commit()
    conn.execute(f"UPDATE accounts SET balance = balance + {amount} WHERE account_id = {receiver}")
    conn.commit()


if __name__ == "__main__":
    conn = sqlite3.connect(":memory:")
    conn.execute("CREATE TABLE accounts (account_id INTEGER PRIMARY KEY, balance INTEGER NOT NULL)")
    conn.executemany("INSERT INTO accounts VALUES (?, ?)", [(1, 100), (2, 20)])
    conn.commit()
    transfer(conn, 1, 2, 40)
    print(conn.execute("SELECT account_id, balance FROM accounts ORDER BY account_id").fetchall())
    conn.close()
```

### VELKRISの回答
**原因：** ①減算の直後に `commit()` しているため、加算が失敗しても減算だけが確定し、不完全な更新が残る。②残高不足・口座の存在・同一口座・amount の検証が一切ない（存在しない口座への UPDATE は0行更新で黙って成功し、送金元だけ減る）。③f-string で値を SQL に埋め込んでおり、SQL インジェクションや型の取り違えに無防備。（元コード実測：ID1=100, ID2=20 で 500 移動と ID99 への 10 移動を実行すると `[(1, -410), (2, 520)]` になり、例外も出ない）

**修正版コード：**
```python
import sqlite3


def _is_int(value: object) -> bool:
    # bool は int のサブクラスなので True/False を金額や口座IDとして受け付けない
    return isinstance(value, int) and not isinstance(value, bool)


def transfer(conn: sqlite3.Connection, sender: int, receiver: int, amount: int) -> None:
    # DBに触れる前に引数を検証する（文字列などを SQL に混ぜないため、整数以外は拒否）
    if not (_is_int(sender) and _is_int(receiver)):
        raise ValueError("account ids must be integers")
    if not _is_int(amount) or amount <= 0:
        raise ValueError("amount must be an integer of 1 or more")
    if sender == receiver:
        raise ValueError("sender and receiver must be different accounts")

    # 確認と更新を1つのトランザクションにまとめる。IMMEDIATE で書き込みロックを先に取り、
    # 残高確認から更新までの間に他の接続が残高を変えられないようにする
    # （BEGIN 自体の失敗時は呼び出し元の状態を巻き戻さないよう try の外で実行する）
    conn.execute("BEGIN IMMEDIATE")
    try:
        # SQL には値を埋め込まずプレースホルダーを使う
        row = conn.execute(
            "SELECT balance FROM accounts WHERE account_id = ?", (sender,)
        ).fetchone()
        if row is None:
            raise ValueError(f"sender account {sender} does not exist")
        if conn.execute(
            "SELECT 1 FROM accounts WHERE account_id = ?", (receiver,)
        ).fetchone() is None:
            raise ValueError(f"receiver account {receiver} does not exist")
        if row[0] < amount:
            raise ValueError("insufficient balance")
        conn.execute(
            "UPDATE accounts SET balance = balance - ? WHERE account_id = ?",
            (amount, sender),
        )
        conn.execute(
            "UPDATE accounts SET balance = balance + ? WHERE account_id = ?",
            (amount, receiver),
        )
        # 元コードは減算直後にコミットしており、加算失敗時に片側だけ更新が残っていた
        # 明示的に開始したトランザクションなので SQL の COMMIT で確定する
        # （Python 3.12 以降の autocommit=True 接続では conn.commit() が何もしないため）
        conn.execute("COMMIT")
    except BaseException:
        # 検証エラー・SQL例外・中断のいずれでも両方の更新を取り消し、トランザクション外に戻す
        # （SQLite がエラーで既にロールバック済みの場合に備えて in_transaction を確認する）
        if conn.in_transaction:
            conn.execute("ROLLBACK")
        raise


if __name__ == "__main__":
    conn = sqlite3.connect(":memory:")
    conn.execute("CREATE TABLE accounts (account_id INTEGER PRIMARY KEY, balance INTEGER NOT NULL)")
    conn.executemany("INSERT INTO accounts VALUES (?, ?)", [(1, 100), (2, 20)])
    conn.commit()
    transfer(conn, 1, 2, 40)
    print(conn.execute("SELECT account_id, balance FROM accounts ORDER BY account_id").fetchall())
    conn.close()
```

**確認方法・結果：**
- 検証環境：Windows 11 Pro、Python 3.14.7（vcpkg 同梱の CPython）と Python 3.11.4（Android NDK 同梱の CPython）。リポジトリ外の一時ディレクトリ（Claude scratchpad 配下の `velkris-python\`）に修正版 `qNN.py` とテスト `test_qNN.py` を問題ごとに別ファイルで作成して実行。元の問題コードも同ディレクトリに別ファイルとして抽出し、不具合の再現に使用。
- 補足：Python 3.11.4（Android NDK 同梱版）には `_sqlite3` 拡張モジュールが含まれていないため、Q07 は **3.11.4 では未実行**（`ModuleNotFoundError: No module named '_sqlite3'`）。3.14.7（SQLite 3.50.4）でのみ実行済み。3.11 の通常版 CPython（`_sqlite3` 付属）で `python test_q07.py` を実行すれば再現でき、使用している API（`BEGIN IMMEDIATE`／`COMMIT`／`ROLLBACK` の SQL 実行、`in_transaction`）は 3.11 にも存在するため同じ結果になると予想。
- 実行：`python test_q07.py`。初期残高 ID1=100, ID2=20, ID3=0 の `:memory:` DB を、既定接続・`isolation_level=None`・`autocommit=True`（3.12 以降のみ）の3種類で作成し、同じケースを各接続で実行。

| ケース | 期待値 | 実測結果（3接続すべて） |
|---|---|---|
| ID1→ID2 に 40 | (60, 60)、終了後 `in_transaction` は False | `[(1, 60), (2, 60), (3, 0)]`、False |
| 500 の移動（残高超過） | ValueError、残高不変、`in_transaction` False | ValueError、`[(1, 60), (2, 60), (3, 0)]` のまま、False |
| 存在しない ID99 への移動／ID99 からの移動 | 同上 | 同上 |
| amount=0、amount=-5 | 同上 | 同上 |
| 送金元と送金先が同じ（1→1） | 同上 | 同上 |
| amount=True（bool）・1.5（float）・`"40; DROP TABLE accounts"`、sender=`"1 OR 1=1"`（追加） | 同上 | 同上 |
| 残高ちょうど（ID2→ID1 に 60、追加） | `[(1,120),(2,0),(3,0)]` | `[(1, 120), (2, 0), (3, 0)]` |
| 途中の SQL 例外（ID3 の UPDATE で `RAISE(ABORT)` するトリガーを作成し 1→3 に 10。減算実行後に失敗、追加） | 例外が伝播、減算も取り消し、`in_transaction` False | `IntegrityError` が伝播、`[(1, 120), (2, 0), (3, 0)]` のまま、False |
| ファイル DB で成功（40）と失敗（500）を実行し、別コネクションで確認（追加） | `[(1,60),(2,60)]` | `[(1, 60), (2, 60)]` |

- 実行結果：3.14.7 で全109項目（3接続×36項目＋ファイルDB 1項目）が期待値と実測で一致。3.11.4 は上記理由で未実行。

---

## Q08 — パス・トラバーサルとシンボリックリンクの防御（上級）

### 要求仕様
read_report(root, relative) は、root/reports フォルダーの**内部にある実在する .txt 通常ファイル**だけを UTF-8 で読み込み、文字列を返してください。root/reports は実在するディレクトリで、root は pathlib.Path を受け取ります。relative は文字列で、内部のサブフォルダーへの相対パスは許可します。

絶対パス、親ディレクトリ要素 ".."、.txt 以外の拡張子、シンボリックリンクを介した reports 外への脱出は ValueError で拒否してください。存在しない .txt ファイルは FileNotFoundError にしてください。アクセス拒否時は元の OSError を伝播してかまいません。ファイルを読み取る前にパスを検証し、外部ファイルの内容を読まないこと。

必須ケース：reports/ok.txt → 読込成功、reports/sub/note.txt → 読込成功、relative="../secret.txt"・"../../secret.txt"・絶対パス・"ok.json" → ValueError。reports/out.txt が外部ファイルへのシンボリックリンクなら ValueError（実行環境でリンク作成が許可されている場合に確認）。

### 問題コード（変更禁止）
```python
from pathlib import Path


def read_report(root: Path, relative: str) -> str:
    file_path = root / "reports" / relative
    if file_path.suffix != ".txt":
        raise ValueError("Only .txt files are allowed")
    return file_path.read_text(encoding="utf-8")
```

### VELKRISの回答
**原因：** `root / "reports" / relative` をそのまま読み込み、拡張子しか検証していない。`"../secret.txt"` のような親ディレクトリ要素、絶対パス（`Path` の `/` 演算子は右辺が絶対パスだと左辺を捨てる）、reports 内のシンボリックリンクやジャンクションを辿って reports の外のファイルを読めてしまう。また、通常ファイルかどうかの確認もない。（元コード実測：一時ディレクトリで `relative="../secret.txt"` と `root/secret.txt` の絶対パスの両方で、reports 外の `secret.txt` の内容 `SECRET` が返った）

**修正版コード：**
```python
import errno
import os
from pathlib import Path


def read_report(root: Path, relative: str) -> str:
    # 元コードは拡張子しか見ておらず、".." や絶対パス、リンク経由で reports の外を読めた
    if not isinstance(relative, str) or relative == "" or "\x00" in relative:
        raise ValueError("relative must be a non-empty path string")
    candidate = Path(relative)
    # 絶対パスに加え、Windows の "\x" や "C:x" のようなルート・ドライブ付きの相対パスも拒否する
    if candidate.is_absolute() or candidate.anchor:
        raise ValueError("absolute paths are not allowed")
    # Windows ではコロンはドライブ指定か代替データストリームにしか使われないため拒否する
    if os.name == "nt" and ":" in relative:
        raise ValueError("':' is not allowed in report paths")
    # 親ディレクトリ要素は位置に関係なく拒否する
    if ".." in candidate.parts:
        raise ValueError("'..' is not allowed")
    if candidate.suffix != ".txt":
        raise ValueError("Only .txt files are allowed")

    # reports 自体とアクセス先をシンボリックリンク・ジャンクションを解決した実パスで比較する
    reports_dir = (root / "reports").resolve(strict=True)
    target = (reports_dir / candidate).resolve()
    # リンクを辿った結果が reports の外なら、存在有無より先に ValueError で拒否する
    if not target.is_relative_to(reports_dir):
        raise ValueError("path escapes the reports directory")
    # リンク先が .txt 以外のファイルでも拒否する
    if target.suffix != ".txt":
        raise ValueError("Only .txt files are allowed")
    if not target.exists():
        raise FileNotFoundError(errno.ENOENT, os.strerror(errno.ENOENT), str(target))
    # ディレクトリやデバイスなど通常ファイル以外は読まない
    if not target.is_file():
        raise ValueError("not a regular file")
    # 検証済みの実パスだけを読み込む（アクセス拒否時の OSError はそのまま伝播）
    return target.read_text(encoding="utf-8")
```

**確認方法・結果：**
- 検証環境：Windows 11 Pro、Python 3.14.7（vcpkg 同梱の CPython）と Python 3.11.4（Android NDK 同梱の CPython）。リポジトリ外の一時ディレクトリ（Claude scratchpad 配下の `velkris-python\`）に修正版 `qNN.py` とテスト `test_qNN.py` を問題ごとに別ファイルで作成して実行。元の問題コードも同ディレクトリに別ファイルとして抽出し、不具合の再現に使用。
- 実行：`python test_q08.py`。一時ディレクトリに `root/reports/ok.txt`・`root/reports/sub/note.txt`・`root/reports/ok.json`・`root/reports/dir.txt/`（ディレクトリ）・reports 外の `root/secret.txt`・`outside/secret.txt` を作成し、終了時に削除（3.14.7 / 3.11.4 の両方で実行）。

| ケース | 期待値 | 実測結果 |
|---|---|---|
| `ok.txt` | 内容を読込 | `'OK本文'` |
| `sub/note.txt`（および `sub\note.txt`） | 内容を読込 | `'NOTE本文'` |
| `"../secret.txt"`、`"../../secret.txt"` | ValueError | ValueError |
| `"sub/../ok.txt"`・`"..\secret.txt"`（追加） | ValueError | ValueError |
| 絶対パス（reports 外の secret.txt／reports 内の ok.txt の両方） | ValueError | ValueError |
| `"\secret.txt"`・`"/secret.txt"`・`"C:secret.txt"`・`"ok.txt:x.txt"`（Windows 固有、追加） | ValueError | ValueError |
| `"ok.json"`、拡張子なし `"ok"`、空文字 | ValueError | ValueError |
| ディレクトリ `"dir.txt"`（通常ファイル以外、追加） | ValueError | ValueError |
| 存在しない `"missing.txt"`・`"sub/missing.txt"` | FileNotFoundError | FileNotFoundError |
| reports 内のジャンクション `jdir` → reports 外ディレクトリ経由の `"jdir/secret.txt"`・`"jdir/missing.txt"`（リンク経由の脱出の代替確認、追加） | ValueError | ValueError（`mklink /J` で作成、管理者権限不要） |
| 拒否ケースで外部ファイルを開かないこと（`io.open` を監視。対照として正常ケースの open が観測できることも確認） | secret/.json の open なし | 対照で ok.txt の open を観測、拒否4ケースでの open は `[]` |
| **`reports/out.txt` が外部ファイルへのシンボリックリンク** | ValueError | **未実行** |

- 実行結果：3.14.7・3.11.4 とも実行した23項目で期待値と実測が一致。
- **未実行項目（シンボリックリンク）**：この Windows 環境では `os.symlink` が `OSError: [WinError 1314] クライアントは要求された特権を保有していません。` となり、ファイルのシンボリックリンクを作成できなかった（開発者モード無効・非管理者）。ディレクトリジャンクション経由の脱出は上記のとおり実行・確認済み。
  - 再現手順：Windows の開発者モードを有効にするか管理者として、または Linux/macOS で同じ `test_q08.py` を実行する。テストは `reports/out.txt` → `outside/secret.txt` のリンクを作成して `read_report(root, "out.txt")` を呼び、あわせて reports 内の ok.txt を指す `inner.txt`（読込成功を期待）と ok.json を指す `json-link.txt`（ValueError を期待）も確認する。
  - 予想結果：`out.txt` は `resolve()` で `outside/secret.txt` に解決され、`is_relative_to(reports)` が False になるため ValueError（ファイル内容は読まない）。`inner.txt` は `'OK本文'`、`json-link.txt` は解決先の拡張子が `.json` のため ValueError。
- 残る制約：検証から読込までの間にリンクを差し替える競合（TOCTOU）は、標準ライブラリだけでは全プラットフォーム共通に防げない。検証済みの解決後パスを読むことで影響範囲を小さくしている。

---

## Q09 — asyncioの並行数制限と結果順序の維持（上級〜最上級）

### 要求仕様
fetch_all(ids, fetch) は、各IDに対して非同期関数 fetch(id) を**ちょうど1回**実行し、結果を入力ID順のリストで返してください。fetch は非同期関数で、この問題では正常終了します。処理の同時実行数は**最大3**、IDが3件以上ある場合は非同期処理が実際に重なるように起動してください（単なる逐次 await は不可）。結果の完了順が入れ替わっても返却順は固定です。空入力は []。関数が正常に返るとき未完了のタスクを残さないこと。外部サービスは利用せず標準ライブラリだけで実装してください。

必須ケース：ids=[1,2,3,4,5]、fetch(id) が待機時間の異なる非同期処理の後に id*10 を返すとき → [10,20,30,40,50]。同時実行数の計測で最大3を超えず、同時実行のピークが3に達すること。ids=[] → []。

### 問題コード（変更禁止）
```python
import asyncio


async def fetch_all(ids: list[int], fetch) -> list[int]:
    results = []
    for identifier in ids:
        results.append(await fetch(identifier))
    return results


if __name__ == "__main__":
    async def fake_fetch(identifier: int) -> int:
        await asyncio.sleep(0.01)
        return identifier * 10

    print(asyncio.run(fetch_all([1, 2, 3, 4, 5], fake_fetch)))
```

### VELKRISの回答
**原因：** ループ内で `await fetch(identifier)` を1件ずつ待っているため完全な逐次実行になり、非同期処理が重ならない（同時実行数は常に1）。仕様の「最大3件を並行して実行」を満たさない。（元コード実測：結果は `[10, 20, 30, 40, 50]` だが、同時実行数のピークは 1）

**修正版コード：**
```python
import asyncio

MAX_CONCURRENCY = 3  # 同時実行数の上限


async def fetch_all(ids: list[int], fetch) -> list[int]:
    # 元コードは1件ずつ await しており逐次実行になっていたため、タスクとして並行に起動する
    semaphore = asyncio.Semaphore(MAX_CONCURRENCY)

    async def run_one(identifier: int) -> int:
        # セマフォで実行中の fetch を最大3件に制限する
        async with semaphore:
            return await fetch(identifier)

    # TaskGroup はブロックを抜ける時点で全タスクの完了を待つため、未完了タスクを残さない
    # （例外やキャンセル時は残りのタスクをキャンセルして回収する）
    async with asyncio.TaskGroup() as group:
        # 各IDにつき fetch はちょうど1回だけ起動する
        tasks = [group.create_task(run_one(identifier)) for identifier in ids]
    # 完了順ではなく作成順（入力ID順）に結果を並べる
    return [task.result() for task in tasks]


if __name__ == "__main__":
    async def fake_fetch(identifier: int) -> int:
        await asyncio.sleep(0.01)
        return identifier * 10

    print(asyncio.run(fetch_all([1, 2, 3, 4, 5], fake_fetch)))
```

**確認方法・結果：**
- 検証環境：Windows 11 Pro、Python 3.14.7（vcpkg 同梱の CPython）と Python 3.11.4（Android NDK 同梱の CPython）。リポジトリ外の一時ディレクトリ（Claude scratchpad 配下の `velkris-python\`）に修正版 `qNN.py` とテスト `test_qNN.py` を問題ごとに別ファイルで作成して実行。元の問題コードも同ディレクトリに別ファイルとして抽出し、不具合の再現に使用。
- 実行：`python test_q09.py`。テスト用 fetch は実行中件数とピークを記録し、ID ごとに異なる待機時間（1:0.05s, 2:0.01s, 3:0.04s, 4:0.02s, 5:0.03s）の後 `id*10` を返す（3.14.7 / 3.11.4 の両方で実行）。

| ケース | 期待値 | 実測結果 |
|---|---|---|
| `ids=[1,2,3,4,5]` の結果 | `[10,20,30,40,50]` | `[10, 20, 30, 40, 50]` |
| 完了順（参考） | 入力順と異なる | 3.14.7 で `[2, 3, 4, 1, 5]`、3.11.4 で `[2, 4, 3, 1, 5]`（返却順は入力順のまま） |
| 各 ID の fetch 呼び出し回数 | すべて1回 | `{1: 1, 2: 1, 3: 1, 4: 1, 5: 1}` |
| 同時実行ピーク | 3（3を超えない） | 3 |
| 所要時間（並行の確認） | 逐次合計 0.15s より短い | 3.14.7 で約 0.074s、3.11.4 で約 0.080s |
| 返却時の残存タスク（`asyncio.all_tasks()` から現在のタスクを除いた数） | 0 | 0 |
| `ids=[]` | `[]` | `[]` |
| 20件（追加） | 入力順の結果、ピーク3、残存0 | 一致、ピーク3、残存0 |

- 実行結果：3.14.7・3.11.4 とも全10項目で期待値と実測が一致（所要時間などの参考値は実行ごとに変動）。

---

## Q10 — 非同期ジョブのリトライ・タイムアウト・エラー分離（最上級）

### 要求仕様
run_jobs(ids, worker) は ID を入力順に処理する非同期バッチ実行器です。ids は**重複しない文字列のリスト**、worker(id) は結果文字列を返す非同期関数とします。各ジョブを**最大4件まで同時実行**し、逐次処理にはしないでください。各試行で worker(id) を asyncio.wait_for により **0.2秒でタイムアウト**させます。

TimeoutError が起きたジョブだけ最大3試行（初回＋再試行2回）してください。3回とも TimeoutError ならそのジョブは error="timeout" として確定します。ValueError を含む TimeoutError 以外の通常例外では再試行せず、error="failed" にします。1ジョブの失敗で他のジョブを失敗扱いにせず、全ジョブの結果を入力順に返してください。正常終了時には未完了タスクを残さず、呼び出し元が run_jobs をキャンセルした場合はキャンセルを握り潰さず、実行中の子タスクもキャンセル・回収してください。外部パッケージは禁止です。

結果辞書の形式：
- 成功：{"id":"alpha","status":"ok","result":"A"}
- タイムアウトで失敗：{"id":"gamma","status":"error","error":"timeout"}
- それ以外の例外で失敗：{"id":"delta","status":"error","error":"failed"}

必須ケース：ids=["alpha","beta","gamma","delta","epsilon"]。テスト用 worker は alpha→"A"、beta→1回目に TimeoutError・2回目に "B"、gamma→毎回0.8秒待機、delta→ValueError、epsilon→"E" を返す／送出するものとします。期待結果は順に alpha成功、beta成功、gammaタイムアウト失敗、delta一般失敗、epsilon成功。betaの試行は2回、gammaは3回、deltaは1回。追加検証：4件以上の待機ジョブで並行数が4を超えずピーク4に達すること、空入力は []、途中で呼び出しをキャンセルしたら asyncio.CancelledError が伝播し残存タスクがないこと。

### 問題コード（変更禁止）
```python
import asyncio


async def run_jobs(ids: list[str], worker) -> list[dict]:
    results = []
    for job_id in ids:
        try:
            value = await worker(job_id)
            results.append({"id": job_id, "status": "ok", "result": value})
        except Exception:
            results.append({"id": job_id, "status": "error", "error": "failed"})
    return results


if __name__ == "__main__":
    async def example_worker(job_id: str) -> str:
        await asyncio.sleep(0.01)
        return job_id.upper()

    print(asyncio.run(run_jobs(["alpha", "epsilon"], example_worker)))
```

### VELKRISの回答
**原因：** ①ループ内で1件ずつ `await` しており逐次実行で、並行数4の要件を満たさない。②`asyncio.wait_for` によるタイムアウトがなく、gamma のような長時間ジョブをそのまま待つ。③TimeoutError の再試行がなく、TimeoutError も含めすべて `"failed"` として扱うため、`"timeout"` と区別できない。（元コード実測：alpha/beta/gamma で beta が1回目の TimeoutError で `failed`、gamma は 0.8s 待って `ok`、試行回数はすべて1回）

**修正版コード：**
```python
import asyncio

MAX_CONCURRENCY = 4  # 同時に実行するジョブ数の上限
TIMEOUT_SECONDS = 0.2  # 1試行あたりのタイムアウト
MAX_ATTEMPTS = 3  # TimeoutError 時の最大試行回数（初回＋再試行2回）


async def run_jobs(ids: list[str], worker) -> list[dict]:
    # 元コードは逐次実行・タイムアウトなし・リトライなしで、タイムアウトも "failed" 扱いだった
    semaphore = asyncio.Semaphore(MAX_CONCURRENCY)

    async def run_one(job_id: str) -> dict:
        # ジョブ単位でセマフォを確保し、同時実行を最大4件に制限する
        async with semaphore:
            for _ in range(MAX_ATTEMPTS):
                try:
                    # 各試行を0.2秒で打ち切る（タイムアウト時は worker 側のコルーチンもキャンセルされる）
                    value = await asyncio.wait_for(worker(job_id), TIMEOUT_SECONDS)
                except TimeoutError:
                    # Python 3.11 以降 asyncio.TimeoutError は組み込み TimeoutError と同一。これだけ再試行する
                    continue
                except Exception:
                    # ValueError などその他の通常例外は再試行せず、このジョブだけ失敗として確定する
                    # CancelledError は BaseException のサブクラスなのでここでは捕まえず、そのまま伝播する
                    return {"id": job_id, "status": "error", "error": "failed"}
                return {"id": job_id, "status": "ok", "result": value}
            # 3試行すべて TimeoutError だった場合
            return {"id": job_id, "status": "error", "error": "timeout"}

    # TaskGroup は正常終了時に全子タスクの完了を待ち、呼び出し元がキャンセルした場合は
    # 実行中の子タスクをキャンセルして回収したうえで CancelledError を再送出する
    async with asyncio.TaskGroup() as group:
        tasks = [group.create_task(run_one(job_id)) for job_id in ids]
    # 完了順ではなく入力順に結果を返す
    return [task.result() for task in tasks]


if __name__ == "__main__":
    async def example_worker(job_id: str) -> str:
        await asyncio.sleep(0.01)
        return job_id.upper()

    print(asyncio.run(run_jobs(["alpha", "epsilon"], example_worker)))
```

**確認方法・結果：**
- 検証環境：Windows 11 Pro、Python 3.14.7（vcpkg 同梱の CPython）と Python 3.11.4（Android NDK 同梱の CPython）。リポジトリ外の一時ディレクトリ（Claude scratchpad 配下の `velkris-python\`）に修正版 `qNN.py` とテスト `test_qNN.py` を問題ごとに別ファイルで作成して実行。元の問題コードも同ディレクトリに別ファイルとして抽出し、不具合の再現に使用。
- キャンセル時の扱い：`CancelledError` は Python 3.8 以降 `BaseException` のサブクラスで `except Exception` に捕まらないため握り潰さない。TaskGroup が子タスクをキャンセル・回収したうえで呼び出し元へ再送出する。
- 実行：`python test_q10.py`（3.14.7 / 3.11.4 の両方で実行）

| ケース | 期待値 | 実測結果 |
|---|---|---|
| 必須ケース（alpha〜epsilon）の結果 | alpha ok "A"、beta ok "B"、gamma error "timeout"、delta error "failed"、epsilon ok "E"（入力順） | 期待どおりの5件の辞書が入力順で返る |
| 試行回数 | beta 2回、gamma 3回、delta 1回（alpha・epsilon 1回） | `{'alpha': 1, 'beta': 2, 'gamma': 3, 'delta': 1, 'epsilon': 1}` |
| gamma のタイムアウト時に worker コルーチンがキャンセルされた回数（追加） | 3 | 3 |
| 所要時間（参考） | 約 0.6s（gamma 0.2s×3） | 3.14.7 で約 0.63s、3.11.4 で約 0.61s |
| 正常終了時の残存タスク | 0 | 0 |
| 0.05s 待機ジョブ10件：同時実行ピーク | 4（4を超えない） | 4 |
| 同10件の所要時間（参考） | 逐次 0.5s より短い（4並行で約 0.15s） | 3.14.7 で約 0.165s、3.11.4 で約 0.166s |
| 空入力 | `[]` | `[]` |
| RuntimeError を送出するジョブ／毎回 `asyncio.TimeoutError` を送出するジョブ（追加） | failed（1回）／timeout（3回） | failed・試行1回／timeout・試行3回 |
| 6件（各0.15s）実行中、0.05s 後に run_jobs のタスクを `cancel()` | CancelledError が伝播、タスクは cancelled、起動済み4件の worker がすべてキャンセル、残存タスク0 | CancelledError、`task.cancelled()` は True、起動4件・キャンセル4件、残存0 |
| 呼び出し側の `asyncio.timeout(0.05)` で中断（追加） | TimeoutError（内部はキャンセル）、子ジョブ2件キャンセル、残存0 | TimeoutError、t1・t2 ともキャンセル、残存0 |

- 実行結果：3.14.7・3.11.4 とも全18項目で期待値と実測が一致（所要時間は参考値で実行ごとに変動）。
- 補足：Python 3.11 の `asyncio.wait_for` には、内側の処理の完了とキャンセルがほぼ同時に起きた場合にキャンセルが失われることがある既知の問題（3.12 で `asyncio.timeout` ベースの実装に変更）がある。今回の 3.11.4 のテストでは発生していない。

---
## 提出報告欄（回答欄ではなく報告時にチャットで提出）
- 回答済み：Q01〜Q10のうち何問
- 自己確認：検証済み／未検証の区別
- ローカルコミット：SHA
- GitHubへのpush：所有者が許可した場合のみ実施し、その結果を報告
- 採点・合否：クライアント側で決定

**VELKRISへの指示：上記の条件を確認し、Q01から順にPython能力テストを開始してください。**
