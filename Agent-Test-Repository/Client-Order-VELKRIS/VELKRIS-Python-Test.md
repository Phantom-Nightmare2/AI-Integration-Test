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
**原因：** 未回答

**修正版コード：**
```python
# ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

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
**原因：** 未回答

**修正版コード：**
```python
# ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

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
**原因：** 未回答

**修正版コード：**
```python
# ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

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
**原因：** 未回答

**修正版コード：**
```python
# ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

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
**原因：** 未回答

**修正版コード：**
```python
# ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

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
**原因：** 未回答

**修正版コード：**
```python
# ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

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
**原因：** 未回答

**修正版コード：**
```python
# ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

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
**原因：** 未回答

**修正版コード：**
```python
# ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

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
**原因：** 未回答

**修正版コード：**
```python
# ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

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
**原因：** 未回答

**修正版コード：**
```python
# ここに修正版コード全文を記入してください。
```

**確認方法・結果：** 未回答

---
## 提出報告欄（回答欄ではなく報告時にチャットで提出）
- 回答済み：Q01〜Q10のうち何問
- 自己確認：検証済み／未検証の区別
- ローカルコミット：SHA
- GitHubへのpush：所有者が許可した場合のみ実施し、その結果を報告
- 採点・合否：クライアント側で決定

**VELKRISへの指示：上記の条件を確認し、Q01から順にPython能力テストを開始してください。**
