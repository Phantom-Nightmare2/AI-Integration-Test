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

**原因：** （未回答）

**修正版コード全文：**

~~~python
# ここに修正版コード全文を記入
~~~

**確認方法・結果：** （未回答）

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

**原因：** （未回答）

**修正版コード全文：**

~~~python
# ここに修正版コード全文を記入
~~~

**確認方法・結果：** （未回答）

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

**原因：** （未回答）

**修正版コード全文：**

~~~python
# ここに修正版コード全文を記入
~~~

**確認方法・結果：** （未回答）

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

**原因：** （未回答）

**修正版コード全文：**

~~~python
# ここに修正版コード全文を記入
~~~

**確認方法・結果：** （未回答）

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

**原因：** （未回答）

**修正版コード全文：**

~~~python
# ここに修正版コード全文を記入
~~~

**確認方法・結果：** （未回答）

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

**原因：** （未回答）

**修正版コード全文：**

~~~python
# ここに修正版コード全文を記入
~~~

**確認方法・結果：** （未回答）

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

**原因：** （未回答）

**修正版コード全文：**

~~~python
# ここに修正版コード全文を記入
~~~

**確認方法・結果：** （未回答）

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

**原因：** （未回答）

**修正版コード全文：**

~~~python
# ここに修正版コード全文を記入
~~~

**確認方法・結果：** （未回答）

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

**原因：** （未回答）

**修正版コード全文：**

~~~python
# ここに修正版コード全文を記入
~~~

**確認方法・結果：** （未回答）

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

**原因：** （未回答）

**修正版コード全文：**

~~~python
# ここに修正版コード全文を記入
~~~

**確認方法・結果：** （未回答）

---

## 確認テスト提出報告（チャットで報告）

- 回答した問題数：Q01〜Q10のうち何問
- 検証済み件数／未実行件数：各件数と未実行の理由
- ローカルのコミットSHA：値を記載
- GitHub push：所有者が許可した場合のみ実行・報告
- 採点・合否：クライアント側で実施

**★HERCULESへの最終指示：これはPythonの確認テストです。既存の問題コードを直接変更せず、Q01から順番に分析・修正・検証し、指定回答欄だけに報告してください。**
