# ★★FREYJA（フレイア）Python・地獄級能力試験【全10問】

**受験AIエージェント：★★FREYJA（フレイア）**  
**試験責任者・最終評価者：Phantom Nightmare**  
**試験言語：Python 3.12／標準ライブラリのみ**  
**試験区分：地獄級／非同期・バッファー・AST・SQLite・共有メモリ・メタプログラミング・外部記憶・安全性・DAG**  
**問題数：10問／各10点／100点満点**  
**著作権表示：© 2026 Phantom Nightmare. All rights reserved.**

## 0. 全問共通：クライアントオーダー

1. **Q01〜Q10は互いに独立した問題**であり、各問の危険な元コードを分析して、要求仕様をすべて満たす**独自の修正版Pythonコード**を設計・実装する。
2. **元の問題文・危険コード・要求仕様・必須検証・採点基準は変更・削除・緩和しない。** 変更してよいのは各問題の「★★FREYJAの回答【Qxx・記入欄】」内部と最終提出レポートだけとする。
3. 各問の回答には(A)根本原因・危険性、(B)**修正版の完全なPythonコード**、(C)**実際の実行・テストコマンドと環境**、(D)**実際に使用したテストコード全文**、(E)期待結果・実測結果・実施状況、(F)アルゴリズム・正当性の根拠、(G)未実施・未解決事項、の7項目を必須とする。
4. Python 3.12の標準ライブラリのみを使用可能とする。**pip/外部パッケージ（pytest・numpy・pydantic等）・第三者の完成コードのコピーは禁止**。テストフレームワークはunittest、doctestなど標準機能を使う。OS/CPUに依存するテストは実施環境と未実施環境を分けて報告する。
5. 各問について実際に起動し、正常系・境界値・異常系・所有権・例外安全・並列/非同期競合・障害注入等の**問題文が指定する必須検証**を優先して実施。危険な元コード自体を攻撃的入力で実行する必要はない。
6. 未実行テストを成功と報告しない。実測件数、使用した環境、期待値と実測値を区別。完全に実装できない条件は未実装と記録する。**自身による自己採点・自己合格判定は禁止**。
7. 問題に明記されていない評価条件を後から追加しない。仕様の範囲内での実装方法は★★FREYJAが自律判断する。PythonのGCやGILの偶然の性質だけに依存して同期の正しさを主張しない。
8. **試験参加者は★★FREYJAのみ。** Claude Codeは必要な操作を補助できるが試験参加者・共同解答者・採点者ではない。ユーザーの指示の無断変更・追加、担当変更、不要な追加エージェントの起動を禁止する。
9. 問題解決に必要な自律設計・実装・修正・関連テストを許可するが、無関係な調査、他ファイルの大量読み込み、無制限再試行、成功済み無関係テストの再実行を禁止。CLAUDE.md・エージェント設定・権限・README・LICENSE・他エージェント資料は無断変更しない。
10. テストは必ず専用の一時領域・ダミーデータで実行する。既存のユーザーデータや実運用DBを破壊しない。本試験はGUIアプリケーション試験ではないためGUI自動操作は不要。PC操作中のマウス・キーボード占有は行わない。
11. 参考資料の著作権・ライセンスを調べ、独自コードを優先する。Python公式仕様（https://docs.python.org/3.12/）を参考にしてよいが、完成コードの無断転載は禁止。
12. **GitHubへのcommit・pushはPhantom Nightmareから明示の許可がある場合に限る。** ローカル回答完成とGitHub提出済みを混同しない。許可があればpush後にGitHubから回答を再取得して全問の反映を確認すること。

**採点基準：1問10点（原因と危険性2点／仕様に適合する完全なコード6点／必須検証と実測2点）、全10問100点。最終採点権限はPhantom Nightmareが保有する。**

---

## Q01【地獄級】memoryview・canonical ULEB128・CRC32による逐次フレーム解析器

**試験の狙い：** Pythonのバッファープロトコル、バイト単位の状態機械、所有権、分割受信、異常時の不変条件。

### 元コード（危険・不完全）

```python
import zlib
def decode_bad(chunk: bytes) -> bytes:
    n = chunk[2]                 # 長さとセグメント境界を未検査
    payload = memoryview(chunk)[3:3+n]
    return payload              # 型も所有権も不正、CRCを無視
```

### 要求仕様

1. `FrameDecoder` に `feed(data: bytes | bytearray | memoryview) -> list[bytes]`、`finish() -> None`、`reset() -> None` を実装。`feed`は**完了した**フレームのpayloadだけを、入力順で独立した`bytes`として返す。未完成分は内部に保持。
2. 形式は `[D3 91][length: canonical unsigned ULEB128][payload:length bytes][crc32:u32 LE]`。lengthは0〜65536。ULEB128は最短表現のみ、最大3バイト。CRCは**ULEB128の符号化バイト列 + payload** に `zlib.crc32` を適用し、0〜2^32-1として比較する。magicとCRC自体はCRC対象外。
3. magic不一致、非canonical ULEB128、3バイトを超える長さ、65536超過、CRC不一致は `FrameError(kind, offset)`（独自例外）で拒否。`kind`は`MAGIC`/`VARINT`/`LENGTH`/`CRC`。offsetはストリーム全体の0始まりで、エラーを確定した入力バイト（CRC不一致はCRCの先頭バイト）を示す。
4. エラー後はsticky状態。`reset`まで`feed`/`finish`が同じ失敗を通知する。`finish`はフレーム境界でのみ成功し、途中切れは`TruncatedFrameError(offset=総入力長)`。完了したフレームの返却は、その後のエラーによって撤回しない。ただし同じ`feed`内でエラーが生じた場合、その呼出しは例外となりリストは返さない。
5. 入力は呼出し中だけ有効でよく、デコーダーは入力の`bytearray`や`memoryview`を呼出し後に保持しない。`bytearray`のリサイズによる`BufferError`を誘発しないこと。保留状態を含むメモリ使用量はO(65536)。`data`全体を常時連結コピーする方式は禁止。
6. 連結フレームと0長payloadに対応。極端に長い入力でも`feed`の返却値以外に入力全体のコピーを保持しない。追加パッケージは禁止。

### 必須検証

1. 0/1/127/128/16383/16384/65536バイトのpayload、0長、既知のCRC32検査値、長さの境界を正確な期待バイト列と比較。
2. 1バイトずつfeed、全2分割位置、短い複合フレームの全3分割、複数フレームを1回にfeed、空入力。
3. 非canonical `80 00`、`81 00`、4バイトULEB128、長さ65537、magic破損、CRCの各ビット反転、全途中切断位置、sticky/Reset。
4. 入力元の可変bytearrayをfeed後に上書き・リサイズしても返却値が不変。ランダム有効・破損フレーム計10万件を独立参照実装と比較。

### ★★FREYJAの回答【Q01・記入欄】

> **以下が専用回答スペース。** 問題本文、元コード、要求仕様、必須検証は改変しない。各項目の例示テキストを実際の内容へ置き換えること。

#### 1. 根本原因・危険性・影響

**元コード `decode_bad` の問題点**

| # | 問題箇所 | 発生条件 | 危険性・実害 |
| --- | --- | --- | --- |
| 1 | `chunk[2]` を長さとみなす | 入力が 3 バイト未満、分割受信 | IndexError。長さが 1 バイトに固定され、ULEB128（最大 3 バイト）を解釈できない |
| 2 | magic `D3 91` を検査しない | 不正・ずれたストリーム | ゴミを正しいフレームとして受け入れる |
| 3 | 長さ・境界を検査しない | payload が届いていない、長さが 65536 を超える | 途中までの payload を完了扱いで返す。上限がない |
| 4 | CRC を無視する | 破損したデータ | 壊れた payload をそのまま上位へ渡す |
| 5 | `memoryview` を返す | 呼出し元が元のバッファーを再利用・リサイズ | 返却値が後から変わる（所有権が不正）。`bytearray` のリサイズが `BufferError` になる |
| 6 | 状態を持たない | 分割受信・連結フレーム | 2 回目以降の feed や、1 回の入力に複数フレームがある場合を扱えない |

**修正方針**
- `FrameDecoder` を、バイト単位の状態機械（MAGIC0 → MAGIC1 → LEN → PAYLOAD → CRC）として実装しました。
- 未完成のフレームは、payload（最大 65536 バイト）だけを自分の `bytearray` にコピーして保持します。CRC は `zlib.crc32` で逐次更新します。
- 入力の `memoryview` はすべて `with` で release し、呼出し後に入力を参照しません。
- エラーは `FrameError(kind, offset)` で、sticky です（`reset()` まで同じ kind と offset を送出します）。

#### 2. 完全な修正版Pythonコード

```python
"""Q01: canonical ULEB128 + CRC32 による逐次フレーム解析器（Python 3.12・標準ライブラリのみ）。

フレーム形式: [D3 91][length: canonical ULEB128, 1..3 バイト][payload][crc32: u32 LE]
CRC 対象は「ULEB128 の符号化バイト列 + payload」。magic と CRC 自体は対象外。
"""
from __future__ import annotations

import zlib

__all__ = ["FrameDecoder", "FrameError", "TruncatedFrameError", "MAX_PAYLOAD"]

MAGIC0 = 0xD3
MAGIC1 = 0x91
MAX_PAYLOAD = 65536
MAX_VARINT_BYTES = 3

_S_MAGIC0 = 0
_S_MAGIC1 = 1
_S_LEN = 2
_S_PAYLOAD = 3
_S_CRC = 4


class FrameError(Exception):
    """フレームの不正。kind は MAGIC / VARINT / LENGTH / CRC（途中切れは TRUNCATED）。

    offset はストリーム全体での 0 始まりの位置で、エラーを確定した入力バイトを指す。
    """

    def __init__(self, kind: str, offset: int) -> None:
        super().__init__(f"{kind} error at stream offset {offset}")
        self.kind = kind
        self.offset = offset

    def _clone(self) -> "FrameError":
        return FrameError(self.kind, self.offset)


class TruncatedFrameError(FrameError):
    """finish() 時にフレーム境界でなかった。offset は総入力長。"""

    def __init__(self, offset: int) -> None:
        super().__init__("TRUNCATED", offset)

    def _clone(self) -> "TruncatedFrameError":
        return TruncatedFrameError(self.offset)


class FrameDecoder:
    """分割受信に対応したフレームデコーダー。

    * feed() は完了したフレームの payload だけを、入力順に独立した bytes で返す。
    * 入力オブジェクト（bytearray / memoryview）は呼出し中だけ参照し、呼出し後は保持しない。
      内部で作る memoryview はすべて with で release するので、呼出し後に元の
      bytearray をリサイズしても BufferError にならない。
    * 保留状態は「未完成フレームの payload（最大 65536 バイト）+ 定数個の整数」だけ。
    * エラー後は sticky。reset() まで feed()/finish() が同じエラー（kind・offset）を送出する。
    """

    __slots__ = (
        "_state", "_pos", "_crc", "_len_value", "_len_nbytes", "_need",
        "_payload", "_crc_value", "_crc_nbytes", "_crc_start", "_error",
    )

    def __init__(self) -> None:
        self._payload = bytearray()
        self.reset()

    def reset(self) -> None:
        """全状態（sticky エラー・未完成フレーム・ストリーム位置）を初期化する。"""
        self._state = _S_MAGIC0
        self._pos = 0              # これまでに消費した総入力バイト数
        self._crc = 0              # 現フレームの CRC32（ULEB128 + payload）途中値
        self._len_value = 0
        self._len_nbytes = 0
        self._need = 0             # payload の残りバイト数
        self._payload = bytearray()
        self._crc_value = 0
        self._crc_nbytes = 0
        self._crc_start = 0
        self._error: FrameError | None = None

    def feed(self, data: bytes | bytearray | memoryview) -> list[bytes]:
        if not isinstance(data, (bytes, bytearray, memoryview)):
            raise TypeError("data must be bytes, bytearray or memoryview")
        if self._error is not None:
            raise self._error._clone()
        out: list[bytes] = []
        with memoryview(data) as raw:
            if raw.ndim == 1 and raw.format == "B":
                view = raw
            else:
                view = raw.cast("B")  # 非連続なら TypeError（状態は未変更）
            with view as mv:
                try:
                    self._consume(mv, out)
                except FrameError as exc:
                    self._error = exc
                    raise
        return out

    def finish(self) -> None:
        if self._error is not None:
            raise self._error._clone()
        if self._state != _S_MAGIC0:
            err = TruncatedFrameError(self._pos)
            self._error = err
            raise err

    @property
    def pending_bytes(self) -> int:
        """診断用: 内部に保持している未完成 payload のバイト数（<= 65536）。"""
        return len(self._payload)

    # ------------------------------------------------------------------ 内部
    def _consume(self, mv: memoryview, out: list[bytes]) -> None:
        n = len(mv)
        i = 0
        while i < n:
            st = self._state
            if st == _S_PAYLOAD:
                take = self._need if self._need < n - i else n - i
                with mv[i:i + take] as part:
                    self._payload += part              # 必要な分だけ所有コピー
                    self._crc = zlib.crc32(part, self._crc)
                i += take
                self._pos += take
                self._need -= take
                if self._need == 0:
                    self._enter_crc()
                continue
            b = mv[i]
            off = self._pos
            i += 1
            self._pos += 1
            if st == _S_MAGIC0:
                if b != MAGIC0:
                    raise FrameError("MAGIC", off)
                self._state = _S_MAGIC1
            elif st == _S_MAGIC1:
                if b != MAGIC1:
                    raise FrameError("MAGIC", off)
                self._state = _S_LEN
                self._crc = 0
                self._len_value = 0
                self._len_nbytes = 0
            elif st == _S_LEN:
                self._crc = zlib.crc32(bytes((b,)), self._crc)
                self._len_value |= (b & 0x7F) << (7 * self._len_nbytes)
                self._len_nbytes += 1
                if b & 0x80:
                    if self._len_nbytes == MAX_VARINT_BYTES:
                        # 3 バイト目に継続ビット = 4 バイト以上の ULEB128 が確定
                        raise FrameError("VARINT", off)
                else:
                    if self._len_nbytes > 1 and b == 0:
                        # 最上位グループが 0 = 最短表現ではない（例: 80 00, 81 00）
                        raise FrameError("VARINT", off)
                    if self._len_value > MAX_PAYLOAD:
                        raise FrameError("LENGTH", off)
                    self._need = self._len_value
                    if self._need == 0:
                        self._enter_crc()
                    else:
                        self._state = _S_PAYLOAD
            else:  # _S_CRC
                self._crc_value |= b << (8 * self._crc_nbytes)
                self._crc_nbytes += 1
                if self._crc_nbytes == 4:
                    if self._crc_value != (self._crc & 0xFFFFFFFF):
                        raise FrameError("CRC", self._crc_start)
                    out.append(bytes(self._payload))   # 独立した bytes
                    self._payload = bytearray()        # 64KiB のバッファーも解放
                    self._state = _S_MAGIC0

    def _enter_crc(self) -> None:
        self._state = _S_CRC
        self._crc_value = 0
        self._crc_nbytes = 0
        self._crc_start = self._pos
```

#### 3. 実行・ビルド方法と使用環境

```powershell
# 作業フォルダー（試験専用の一時領域）: scratchpad\py\q01 に q01.py（実装）と test_q01.py（テスト）を置く
cd <scratchpad>\py\q01
python3.12 --version            # Python 3.12.15
python3.12 -X dev -m unittest -v test_q01
```

**使用環境（全問共通）**

- Python：**CPython 3.12.15**（`3.12.15 (main, Oct  9 2026) [MSC v.1944 64 bit (AMD64)]`）。この PC に既にあった `python3.12` コマンドで実行しました。新しいバージョンへの変更・pip によるパッケージ導入はしていません。
- ライブラリ：Python 標準ライブラリだけです。テストは `unittest` で行いました。
- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド、x86-64）。
- 実行方法：問題ごとの作業フォルダー（`scratchpad\py\qNN`。試験専用の一時領域）に、実装 `qNN.py` とテスト `test_qNN.py` を置いて実行しました。`-X dev`（開発モード：ResourceWarning や asyncio の検査を有効化）を付けています。
- 未実施の環境：Linux・macOS・ARM での実行はしていません。

- **実行結果：** `Ran 16 tests in 7.641s` → `OK`、終了コード 0（1 回目の実行で全件合格）。

#### 4. 実際に使用したテストコード

```python
"""Q01 のテスト（unittest）。参照実装 ref_parse は FrameDecoder と独立に、全体バイト列を添字で解析する。"""
from __future__ import annotations

import itertools
import random
import time
import tracemalloc
import unittest
import zlib

from q01 import FrameDecoder, FrameError, TruncatedFrameError


# ---------------------------------------------------------------- 独立した参照実装
def uleb(n: int) -> bytes:
    out = bytearray()
    while True:
        b = n & 0x7F
        n >>= 7
        if n:
            out.append(b | 0x80)
        else:
            out.append(b)
            return bytes(out)


def make_frame(payload: bytes) -> bytes:
    ln = uleb(len(payload))
    return b"\xd3\x91" + ln + payload + zlib.crc32(ln + payload).to_bytes(4, "little")


def ref_parse(s: bytes):
    """戻り値: (frames, error)。error は None または (kind, offset)。途中切れは ("TRUNCATED", len(s))。"""
    frames = []
    i = 0
    n = len(s)
    while i < n:
        if s[i] != 0xD3:
            return frames, ("MAGIC", i)
        if i + 1 >= n:
            return frames, ("TRUNCATED", n)
        if s[i + 1] != 0x91:
            return frames, ("MAGIC", i + 1)
        j = i + 2
        vals = []
        while True:
            if j >= n:
                return frames, ("TRUNCATED", n)
            vals.append(s[j])
            if s[j] < 0x80:
                break
            if len(vals) == 3:
                return frames, ("VARINT", j)
            j += 1
        if len(vals) > 1 and vals[-1] == 0:
            return frames, ("VARINT", j)
        length = sum((v & 0x7F) * (128 ** k) for k, v in enumerate(vals))
        if length > 65536:
            return frames, ("LENGTH", j)
        p0 = j + 1
        c0 = p0 + length
        if c0 + 4 > n:
            return frames, ("TRUNCATED", n)
        expect = zlib.crc32(s[i + 2:c0])
        if int.from_bytes(s[c0:c0 + 4], "little") != expect:
            return frames, ("CRC", c0)
        frames.append(s[p0:c0])
        i = c0 + 4
    return frames, None


def run_decoder(stream: bytes, cuts):
    """cuts の位置で分割して feed。戻り値: (返却された frames, error or None)。"""
    d = FrameDecoder()
    got = []
    prev = 0
    try:
        for c in list(cuts) + [len(stream)]:
            got.extend(d.feed(stream[prev:c]))
            prev = c
        d.finish()
    except FrameError as e:
        return got, (e.kind, e.offset)
    return got, None


class KnownVectors(unittest.TestCase):
    def test_lengths_exact_bytes(self):
        expect_len = {0: "00", 1: "01", 127: "7f", 128: "8001", 16383: "ff7f",
                      16384: "808001", 65536: "808004"}
        for n, hx in expect_len.items():
            payload = bytes((k * 7 + 3) & 0xFF for k in range(n))
            frame = make_frame(payload)
            self.assertEqual(frame[:2], b"\xd3\x91")
            self.assertEqual(frame[2:2 + len(hx) // 2].hex(), hx)
            self.assertEqual(len(frame), 2 + len(hx) // 2 + n + 4)
            d = FrameDecoder()
            out = d.feed(frame)
            d.finish()
            self.assertEqual(out, [payload])
            self.assertIs(type(out[0]), bytes)

    def test_known_crc_values(self):
        # 標準 CRC-32 検査値
        self.assertEqual(zlib.crc32(b"123456789"), 0xCBF43926)
        # 0 長フレームの完全なバイト列（CRC は b"\x00" に対して 0xD202EF8D）
        self.assertEqual(make_frame(b"").hex(), "d391008def02d2")
        d = FrameDecoder()
        self.assertEqual(d.feed(bytes.fromhex("d391008def02d2")), [b""])
        d.finish()
        # payload "123456789": CRC は 09 + "123456789"
        f = bytes.fromhex("d39109") + b"123456789" + zlib.crc32(b"\x09123456789").to_bytes(4, "little")
        self.assertEqual(FrameDecoder().feed(f), [b"123456789"])

    def test_length_boundary_65536_ok_65537_rejected(self):
        d = FrameDecoder()
        self.assertEqual(len(d.feed(make_frame(b"\xaa" * 65536))[0]), 65536)
        d = FrameDecoder()
        with self.assertRaises(FrameError) as cm:
            d.feed(b"\xd3\x91" + bytes.fromhex("818004"))
        self.assertEqual((cm.exception.kind, cm.exception.offset), ("LENGTH", 4))


class Splitting(unittest.TestCase):
    def setUp(self):
        self.frames = [b"", b"a", bytes(range(200)), b"xyz" * 50, b"\x00"]
        self.stream = b"".join(make_frame(p) for p in self.frames)

    def test_one_byte_at_a_time(self):
        d = FrameDecoder()
        got = []
        for k in range(len(self.stream)):
            got.extend(d.feed(self.stream[k:k + 1]))
        d.finish()
        self.assertEqual(got, self.frames)

    def test_all_two_way_splits(self):
        for c in range(len(self.stream) + 1):
            got, err = run_decoder(self.stream, [c])
            self.assertIsNone(err)
            self.assertEqual(got, self.frames)

    def test_all_three_way_splits(self):
        short = [b"", b"q", b"hello", b"\xff" * 3]
        s = b"".join(make_frame(p) for p in short)
        count = 0
        for a in range(len(s) + 1):
            for b in range(a, len(s) + 1):
                got, err = run_decoder(s, [a, b])
                self.assertIsNone(err)
                self.assertEqual(got, short)
                count += 1
        print(f"\n[Q01] three-way splits checked: {count} (stream {len(s)} bytes)")

    def test_many_frames_one_feed_and_empty(self):
        d = FrameDecoder()
        self.assertEqual(d.feed(b""), [])
        self.assertEqual(d.feed(bytearray()), [])
        self.assertEqual(d.feed(memoryview(b"")), [])
        self.assertEqual(d.feed(self.stream), self.frames)
        self.assertEqual(d.feed(b""), [])
        d.finish()

    def test_input_types(self):
        f = make_frame(b"abc")
        for data in (f, bytearray(f), memoryview(f), memoryview(bytearray(f))):
            self.assertEqual(FrameDecoder().feed(data), [b"abc"])
        # 1 バイト以外の書式の memoryview も byte 列として扱う
        import array
        arr = array.array("B", f)
        self.assertEqual(FrameDecoder().feed(memoryview(arr)), [b"abc"])
        for bad in ("abc", [1, 2], 5, None):
            with self.assertRaises(TypeError):
                FrameDecoder().feed(bad)


class Errors(unittest.TestCase):
    def expect(self, data: bytes, kind: str, offset: int):
        d = FrameDecoder()
        with self.assertRaises(FrameError) as cm:
            d.feed(data)
        self.assertEqual((cm.exception.kind, cm.exception.offset), (kind, offset))
        self.assertEqual(ref_parse(data)[1], (kind, offset))
        return d

    def test_noncanonical_and_long_varints(self):
        self.expect(bytes.fromhex("d3918000"), "VARINT", 3)
        self.expect(bytes.fromhex("d3918100"), "VARINT", 3)
        self.expect(bytes.fromhex("d391808000"), "VARINT", 4)
        self.expect(bytes.fromhex("d39180808001"), "VARINT", 4)   # 4 バイト ULEB128
        self.expect(bytes.fromhex("d391ffffff7f"), "VARINT", 4)
        self.expect(bytes.fromhex("d391818004"), "LENGTH", 4)     # 65537
        self.expect(bytes.fromhex("d391ffff7f"), "LENGTH", 4)

    def test_magic(self):
        good = make_frame(b"zz")
        self.expect(b"\x00" + good[1:], "MAGIC", 0)
        self.expect(b"\xd3\x92" + good[2:], "MAGIC", 1)
        self.expect(good + b"\xd4", "MAGIC", len(good))

    def test_every_bit_flip_of_frame(self):
        payload = b"bitflip-payload"
        frame = make_frame(payload)
        crc_off = len(frame) - 4
        for byte in range(len(frame)):
            for bit in range(8):
                bad = bytearray(frame)
                bad[byte] ^= 1 << bit
                bad = bytes(bad)
                ref = ref_parse(bad)
                got = run_decoder(bad, [])
                self.assertEqual(got, ref, (byte, bit))
                self.assertNotEqual(ref, ([payload], None))   # 改変が通らない
                if byte >= crc_off:
                    self.assertEqual(got[1], ("CRC", crc_off))

    def test_all_truncation_positions(self):
        s = make_frame(b"") + make_frame(b"abc") + make_frame(bytes(300))
        bounds = {0, 7, 7 + 10}
        for n in range(len(s) + 1):
            d = FrameDecoder()
            d.feed(s[:n])
            if n in bounds or n == len(s):
                d.finish()
            else:
                with self.assertRaises(TruncatedFrameError) as cm:
                    d.finish()
                self.assertEqual((cm.exception.kind, cm.exception.offset), ("TRUNCATED", n))
                self.assertEqual(ref_parse(s[:n])[1], ("TRUNCATED", n))

    def test_sticky_and_reset(self):
        d = FrameDecoder()
        self.assertEqual(d.feed(make_frame(b"ok")), [b"ok"])   # 返却済みは撤回されない
        with self.assertRaises(FrameError) as cm:
            d.feed(make_frame(b"in-same-feed") + b"\x00")       # 同じ feed 内のエラーはリストを返さない
        first = (cm.exception.kind, cm.exception.offset)
        self.assertEqual(first, ("MAGIC", len(make_frame(b"ok")) + len(make_frame(b"in-same-feed"))))
        for _ in range(3):
            with self.assertRaises(FrameError) as cm2:
                d.feed(make_frame(b"x"))
            self.assertEqual((cm2.exception.kind, cm2.exception.offset), first)
            with self.assertRaises(FrameError) as cm3:
                d.finish()
            self.assertEqual((cm3.exception.kind, cm3.exception.offset), first)
        d.reset()
        self.assertEqual(d.feed(make_frame(b"again")), [b"again"])
        d.finish()
        # 途中切れも sticky
        d.feed(b"\xd3")
        with self.assertRaises(TruncatedFrameError):
            d.finish()
        with self.assertRaises(TruncatedFrameError) as cm4:
            d.feed(b"\x91\x00")
        self.assertEqual(cm4.exception.offset, len(make_frame(b"again")) + 1)
        d.reset()
        self.assertEqual(d.feed(make_frame(b"")), [b""])


class Ownership(unittest.TestCase):
    def test_bytearray_mutation_and_resize_after_feed(self):
        frame = make_frame(b"payload-0123456789")
        src = bytearray(frame[:10])
        d = FrameDecoder()
        self.assertEqual(d.feed(src), [])
        src[:] = b"\xff" * 10        # 上書き
        src.extend(b"\x00" * 1000)   # リサイズ（BufferError にならない）
        del src[:500]
        src2 = bytearray(frame[10:])
        mv = memoryview(src2)
        out = d.feed(mv)
        mv.release()
        src2.extend(b"zz")           # 呼出し後はエクスポートが残っていない
        src2[:] = b""
        self.assertEqual(out, [b"payload-0123456789"])
        whole = bytearray(frame)
        out2 = FrameDecoder().feed(whole)
        whole[:] = b"\x00" * len(whole)
        whole.append(1)
        self.assertEqual(out2, [b"payload-0123456789"])

    def test_memory_bound_on_long_stream(self):
        frame = make_frame(bytes(range(256)) * 256)          # payload 65536
        stream = frame * 300                                  # 約 19.7 MB
        chunk = 1 << 20
        d = FrameDecoder()
        max_pending = 0
        total = 0
        tracemalloc.start()
        try:
            for k in range(0, len(stream), chunk):
                with memoryview(stream)[k:k + chunk] as part:
                    total += len(d.feed(part))
                max_pending = max(max_pending, d.pending_bytes)
            _, peak = tracemalloc.get_traced_memory()
        finally:
            tracemalloc.stop()
        d.finish()
        self.assertEqual(total, 300)
        self.assertLessEqual(max_pending, 65536)
        self.assertLess(peak, 4 * 1024 * 1024)               # 入力全体（約 19.7 MB）のコピーなし
        print(f"\n[Q01] stream {len(stream)} bytes, tracemalloc peak {peak} bytes, max pending {max_pending}")


class RandomVsReference(unittest.TestCase):
    def test_100k_random_valid_and_corrupted(self):
        rng = random.Random(20261117)
        stats = {"valid": 0, "corrupt": 0, "errors": {}}
        t0 = time.perf_counter()
        for case in range(100_000):
            nfr = rng.randint(1, 3)
            parts = []
            for _ in range(nfr):
                r = rng.random()
                n = rng.randint(0, 40) if r < 0.98 else rng.choice([127, 128, 129, 16383, 16384, 65535, 65536])
                parts.append(make_frame(rng.randbytes(n)))
            s = bytearray(b"".join(parts))
            corrupt = case % 2 == 1
            if corrupt:
                kind = rng.randrange(5)
                if kind == 0:                       # ビット反転
                    p = rng.randrange(len(s))
                    s[p] ^= 1 << rng.randrange(8)
                elif kind == 1:                     # 途中切れ
                    del s[rng.randrange(len(s)):]
                elif kind == 2:                     # ゴミの挿入
                    s.insert(rng.randrange(len(s) + 1), rng.randrange(256))
                elif kind == 3:                     # 非canonical / 長すぎる / 巨大な長さ
                    s += b"\xd3\x91" + rng.choice([b"\x80\x00", b"\x81\x00", b"\x80\x80\x80\x01",
                                                   b"\x81\x80\x04", b"\xff\xff\x7f"])
                else:                               # CRC バイトの破壊
                    s[-rng.randrange(1, 5)] ^= 0xFF
            s = bytes(s)
            ncut = rng.randrange(4)
            cuts = sorted(rng.randrange(len(s) + 1) for _ in range(ncut))
            ref_frames, ref_err = ref_parse(s)
            got, err = run_decoder(s, cuts)
            self.assertEqual(err, ref_err, case)
            if err is None:
                self.assertEqual(got, ref_frames, case)
                stats["valid"] += 1
            else:
                # エラーを起こした feed のリストは返らないので、返却分は参照の先頭部分と一致
                self.assertEqual(got, ref_frames[:len(got)], case)
                stats["corrupt"] += 1
                stats["errors"][err[0]] = stats["errors"].get(err[0], 0) + 1
        print(f"\n[Q01] 100000 random cases vs reference: {stats} in {time.perf_counter() - t0:.1f}s")


if __name__ == "__main__":
    unittest.main(verbosity=2)
```

#### 5. 検証結果（期待値・実測値・実施状況）

独立参照実装 `ref_parse` は、入力全体を添字で解析するもので、デコーダーのコードを共有していません。

| 必須テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| payload 長 0/1/127/128/16383/16384/65536 | 長さの符号化は順に `00`/`01`/`7f`/`8001`/`ff7f`/`808001`/`808004`。フレーム長が正確で、payload が一致し、型は bytes | 全 7 通り一致 | 実施 |
| 既知の CRC32 検査値 | `crc32(b"123456789") = 0xCBF43926`。0 長フレームの完全なバイト列が `d391008def02d2` | 一致 | 実施 |
| 長さの境界 | 65536 は受理、65537（`81 80 04`）は `LENGTH` @4 | 一致 | 実施 |
| 1 バイトずつの feed | 5 フレーム（0/1/200/150/1 バイト）がすべて正しい順で返る | 一致 | 実施 |
| 全 2 分割の位置 | 全位置で同じ結果 | 一致 | 実施 |
| 短い複合フレームの全 3 分割 | 37 バイトのストリームの全 741 通りで同じ結果 | 741/741 一致 | 実施 |
| 複数フレームを 1 回で feed、空入力 | 1 回の feed で全フレームが返る。空の bytes/bytearray/memoryview は `[]` | 一致 | 実施 |
| 非canonical `80 00`・`81 00` | `VARINT` @3 | 一致（`80 80 00` も @4） | 実施 |
| 4 バイトの ULEB128 | `VARINT` @4（3 バイト目に継続ビット） | 一致 | 実施 |
| magic の破損 | 1 バイト目 → `MAGIC` @0、2 バイト目 → @1、フレームの後 → その位置 | 一致 | 実施 |
| CRC の各ビット反転 | 22 バイトのフレーム（payload 15 バイト）の全 176 ビットを反転。参照実装と一致し、改変は 1 件も通らない。CRC 部の反転は `CRC`（CRC の先頭位置） | 176/176 一致 | 実施 |
| 全途中切断位置 | フレーム境界以外の全長で `finish()` が `TruncatedFrameError(offset=総入力長)` | 一致 | 実施 |
| sticky / reset | 返却済みのフレームは撤回されない。同じ feed 内のエラーでは例外となり、リストは返らない。以後の feed/finish は同じ kind と offset を 3 回とも送出。reset 後は正常に再利用でき、途中切れも sticky | 一致 | 実施 |
| 入力元の bytearray の上書き・リサイズ | feed の後に上書き・extend・del・memoryview の release をしても BufferError にならない。返却値は不変 | 一致 | 実施 |
| メモリ O(65536) | 約 19.7 MB（65536 バイトのフレーム × 300）を 1 MiB ずつ feed。保留 payload の最大値 ≤ 65536、tracemalloc のピークが 4 MiB 未満 | 保留の最大 65,396 バイト、ピーク 1,116,153 バイト | 実施 |
| 入力型 | bytes・bytearray・memoryview・`array('B')` の memoryview を受理。str・list・int・None は TypeError | 一致 | 実施 |
| ランダムな有効・破損フレーム 10 万件と参照実装の比較 | 結果の種別（kind・offset）とフレーム列が参照実装と一致 | 100,000/100,000 一致（エラーなし 50,444、CRC 27,506、TRUNCATED 10,253、VARINT 5,989、LENGTH 4,000、MAGIC 1,808）、7.4 秒 | 実施 |

ランダム試験は、偶数番目を有効なフレームとして生成し、奇数番目を破損させて（ビット反転・切断・ゴミ挿入・非canonical の長さ・CRC 破損）生成しました。切断位置などの関係で、結果的にエラーにならないケースもあります（その件数は上の「エラーなし」に含まれます）。

#### 6. 技術的な正当性・設計判断

- **所有権：**
  - 入力は `with memoryview(data)` の範囲だけで使います。payload の部分スライスも `with mv[i:j]` で即座に release するので、呼出し後に入力のエクスポートは残りません（bytearray のリサイズで BufferError にならない）。
  - 返却値は `bytes(self._payload)` による独立コピーです。返却した後は、内部バッファーを新しい `bytearray()` に差し替えます。
- **メモリ：** 保持するのは「未完成 payload ≤ 65536 バイト + 整数の状態」だけです。入力全体の連結コピーは作らず、payload は必要な量だけを一括でコピーします。
- **CRC：** ULEB128 の各バイトと payload の断片を、到着順に `zlib.crc32(x, crc)` で連鎖させます。これは連結した全体に crc32 を適用した結果と等しく、分割の位置に依存しません。比較は `& 0xFFFFFFFF` の符号なしで行います。
- **エラー offset の定義：**
  - `MAGIC`：その不一致バイト。
  - `VARINT`：非canonical の最上位バイト `00`、または 3 バイト目に継続ビットが立っていた（4 バイト以上が確定した）バイト。
  - `LENGTH`：長さが確定した最後の ULEB128 バイト。
  - `CRC`：CRC の先頭バイト。
  - `TRUNCATED`：総入力長。
- **例外安全：** `FrameError` は記録してから送出します（sticky）。同じ feed 内で完了したフレームは返しません（仕様どおり）。TypeError（入力型の不正）は状態を変えません。
- **計算量：** 1 回の feed は O(len(data))。ヘッダーは 1 バイトずつ、payload は一括で処理します。

#### 7. 未実施項目・制約・残課題

- 非連続（ストライドのある）memoryview は `cast('B')` ができないため TypeError としました（状態は変わりません）。
- Linux・macOS での実行はしていません。
- 長さの ULEB128 が 3 バイトを超える場合は、`LENGTH` ではなく `VARINT` に分類します（上記の offset の定義のとおり）。

---

## Q02【地獄級】asyncio有界KeyedExecutor・厳密FIFO・キャンセル競合

**試験の狙い：** asyncio.Task、Semaphore、バックプレッシャー、キーごとの厳密順序、キャンセルとクローズ処理を設計。

### 元コード（危険・不完全）

```python
import asyncio
class ExecutorBad:
    async def submit(self, key, job):
        asyncio.create_task(job())  # 例外無監視、同時実行数無制限
        return None
```

### 要求仕様

1. `AsyncKeyedExecutor` に`async submit(key: str, work: Callable[[], Awaitable[int]]) -> int`、`async aclose() -> None`、`async __aenter__/__aexit__` を実装。keyは非空文字列、workは呼出可能でなければTypeError/ValueError。
2. **受理済みで未完了のジョブは最大128、実行中は最大4**。満杯ならsubmitは非同期に空きを待つ。受付順は枠取得後、内部キュー登録を完了した順番と定義する。待ち中にキャンセルされた呼出しは未受付としてworkを実行しない。
3. 同一keyのworkは受付順FIFOであり、前のworkが終了するまで次を開始しない。異なるkey間の厳密な公平性・全順序は要求しない。単一のイベントループで使用し、複数OSスレッドから同時に呼ばない契約。
4. 受付後・実行前にsubmit呼出し元がキャンセルされた場合、そのworkは実行せず枠を解放。実行中キャンセルの場合は対応workへ`Task.cancel()`し、workが抑制・完了するまで必要な後片付けを行う。workがキャンセルを握り潰して値を返した場合はその値を返すかキャンセルのどちらかとし、そのポリシーを固定して説明する。
5. workの正常値、例外、CancelledErrorは対応するsubmitにだけ伝える。別キーのジョブを停止せず、未監視Task例外を残さない。受理したジョブは待ちの発生に関わらず枠をちょうど1回解放。
6. `aclose()`開始後は新規submitをRuntimeErrorで拒否。受付済みジョブはworkの正常終了または呼出し元キャンセルまで進め、全ジョブが終わってからaclose完了。aclose二重呼出しは安全。workが永久に戻らなければacloseも終わらないことを契約とする。
7. `asyncio.sleep`を使ったビジー待ち、無制限のcreate_task、同期的なthreading.Lockによるイベントループ占有は禁止。Python 3.12標準機能のみ使用。

### 必須検証

1. 同一keyの100件の開始・終了順が完全FIFO、20キーで最大同時実行数ちょうど4かつ超過0。
2. 128件を`asyncio.Event`で停止して129件目を受付待ち、待ちキャンセル、再開後の全枠回復を決定的に検証。
3. 受付前/受付後・実行前/実行中のキャンセルとworkの同期・非同期例外を混在させ、実行漏れ・二重実行・未監視例外0。
4. acloseとsubmitの競合、二重aclose、1万件の完了・取消・失敗混合負荷で受付数が最後に0、イベントループが停止しないこと。

### ★★FREYJAの回答【Q02・記入欄】

> **以下が専用回答スペース。** 問題本文、元コード、要求仕様、必須検証は改変しない。各項目の例示テキストを実際の内容へ置き換えること。

#### 1. 根本原因・危険性・影響

**元コード `ExecutorBad.submit` の問題点**

| # | 問題箇所 | 危険性・実害 |
| --- | --- | --- |
| 1 | `asyncio.create_task(job())` の戻り値を捨てる | Task の強参照がなく、GC で消えることがある。例外は誰も観測せず「Task exception was never retrieved」になる |
| 2 | 同時実行数・受付数の上限がない | 大量の submit で無制限にタスクとメモリが増える（バックプレッシャーがない） |
| 3 | 同じキーの順序を保証しない | 同一キーの処理が並行・逆順で走り、データが競合する |
| 4 | 結果を返さない（`return None`） | 呼出し元が値・例外・キャンセルを受け取れない |
| 5 | キャンセル・終了処理がない | 呼出し元をキャンセルしても work は走り続ける。終了時に未完了のタスクが残る |

**修正方針**
- 受付枠（128）は、自前の FIFO 待ち行列で管理します。満杯なら submit は Future で非同期に待ちます。
- 実行枠（4）は `asyncio.Semaphore(4)` で管理します。
- キーごとのキュー（deque）と、キーごとに高々 1 つのポンプ Task で、同一キーの FIFO と直列実行を保証します。
- work は開始時にだけ Task 化し、その Task は常に 4 個以下です。結果・例外・キャンセルは、対応する submit にだけ返します。

#### 2. 完全な修正版Pythonコード

```python
"""Q02: asyncio 有界 KeyedExecutor（キーごとの厳密 FIFO・受付上限 128・実行上限 4）。

キャンセルのポリシー（固定）:
  submit の呼出し元がキャンセルされた場合、submit は常に CancelledError を送出する。
  実行中の work がキャンセルを握り潰して値を返しても、その値は捨てる（呼出し元へは返さない）。
"""
from __future__ import annotations

import asyncio
from collections import deque
from typing import Awaitable, Callable

__all__ = ["AsyncKeyedExecutor", "MAX_ACCEPTED", "MAX_RUNNING"]

MAX_ACCEPTED = 128
MAX_RUNNING = 4

_PENDING = 0     # 受付済み・未開始（キーのキュー内）
_RUNNING = 1     # work の Task が存在する
_DONE = 2        # work が終了し、枠を解放済み
_DROPPED = 3     # 開始前に呼出し元がキャンセル（work は実行しない）


class _Job:
    __slots__ = ("key", "work", "state", "task", "finished", "value", "exc", "cancelled", "queue")

    def __init__(self, key: str, work: Callable[[], Awaitable[int]], finished: asyncio.Future,
                 queue: deque) -> None:
        self.key = key
        self.work = work
        self.state = _PENDING
        self.task: asyncio.Task | None = None
        self.finished = finished          # pump が後片付け完了後に結果 None を設定する
        self.value: object = None
        self.exc: BaseException | None = None
        self.cancelled = False
        self.queue = queue


class AsyncKeyedExecutor:
    """単一イベントループ専用。複数 OS スレッドから同時に呼ばない契約。"""

    def __init__(self) -> None:
        self._closed = False
        self._free = MAX_ACCEPTED                   # 受付枠の空き
        self._waiters: deque[asyncio.Future] = deque()   # 受付枠待ち（FIFO）
        self._run_sem = asyncio.Semaphore(MAX_RUNNING)
        self._queues: dict[str, deque[_Job]] = {}
        self._pumps: set[asyncio.Task] = set()      # キーごとに高々 1 つ
        self._accepted = 0                          # 受理済み・未完了のジョブ数
        self._running = 0
        self._idle = asyncio.Event()
        self._idle.set()
        # 診断用（テストの観測点）。None 以外なら受付時に (key, 受付番号) で呼ぶ。
        self.accept_hook: Callable[[str, int], None] | None = None
        self._accept_seq = 0

    # ------------------------------------------------------------ 公開 API
    @property
    def accepted_count(self) -> int:
        return self._accepted

    @property
    def running_count(self) -> int:
        return self._running

    @property
    def free_slots(self) -> int:
        return self._free

    async def __aenter__(self) -> "AsyncKeyedExecutor":
        return self

    async def __aexit__(self, exc_type, exc, tb) -> None:
        await self.aclose()

    async def submit(self, key: str, work: Callable[[], Awaitable[int]]) -> int:
        if not isinstance(key, str):
            raise TypeError("key must be str")
        if not key:
            raise ValueError("key must be a non-empty string")
        if not callable(work):
            raise TypeError("work must be callable")
        if self._closed:
            raise RuntimeError("executor is closed")

        await self._acquire_slot()        # キャンセル・クローズ時は未受付のまま例外
        if self._closed:
            self._release_slot()
            raise RuntimeError("executor is closed")

        # ---- 受付（ここから await まで中断点なし）
        loop = asyncio.get_running_loop()
        q = self._queues.get(key)
        if q is None:
            q = deque()
            self._queues[key] = q
            pump = loop.create_task(self._pump(key, q))
            self._pumps.add(pump)
            pump.add_done_callback(self._pumps.discard)
        job = _Job(key, work, loop.create_future(), q)
        q.append(job)
        self._accepted += 1
        self._idle.clear()
        self._accept_seq += 1
        if self.accept_hook is not None:
            self.accept_hook(key, self._accept_seq)

        try:
            await asyncio.shield(job.finished)
        except asyncio.CancelledError:
            if job.state == _PENDING:
                # 受付後・実行前のキャンセル: work は実行せず、枠をここで 1 回だけ解放
                job.state = _DROPPED
                job.queue.remove(job)
                self._job_released()
                raise
            if job.state == _RUNNING and job.task is not None:
                job.task.cancel()
            # work が終了し pump が後片付けするまで待つ（追加のキャンセル要求は吸収し、最後に送出）
            while not job.finished.done():
                try:
                    await asyncio.shield(job.finished)
                except asyncio.CancelledError:
                    pass
            raise
        if job.cancelled:
            raise asyncio.CancelledError()
        if job.exc is not None:
            exc = job.exc
            job.exc = None
            raise exc
        return job.value  # type: ignore[return-value]

    async def aclose(self) -> None:
        if not self._closed:
            self._closed = True
            err = RuntimeError("executor is closed")
            while self._waiters:          # 受付枠を待っている submit を即座に拒否
                w = self._waiters.popleft()
                if not w.done():
                    w.set_exception(err)
        while self._accepted:
            await self._idle.wait()
        if self._pumps:
            await asyncio.gather(*list(self._pumps))

    # ------------------------------------------------------------ 受付枠
    async def _acquire_slot(self) -> None:
        if self._free > 0 and not self._waiters:
            self._free -= 1
            return
        fut = asyncio.get_running_loop().create_future()
        self._waiters.append(fut)
        try:
            await fut
        except asyncio.CancelledError:
            if fut.cancelled():
                try:
                    self._waiters.remove(fut)
                except ValueError:
                    pass
            elif fut.exception() is None:
                # 枠を譲り受けた直後にキャンセルされた: 枠を返す
                self._release_slot()
            raise

    def _release_slot(self) -> None:
        while self._waiters:
            w = self._waiters.popleft()
            if not w.done():
                w.set_result(None)        # 枠を待ち手へ直接移譲
                return
        self._free += 1

    def _job_released(self) -> None:
        """受理済みジョブ 1 件の終了処理（各ジョブについて正確に 1 回だけ呼ばれる）。"""
        self._accepted -= 1
        if self._accepted == 0:
            self._idle.set()
        self._release_slot()

    # ------------------------------------------------------------ キーごとの実行
    async def _pump(self, key: str, q: deque) -> None:
        try:
            while q:
                await self._run_sem.acquire()
                if not q:                 # 待っている間に全ジョブが取り消された
                    self._run_sem.release()
                    break
                job = q.popleft()
                job.state = _RUNNING
                self._running += 1
                task = asyncio.get_running_loop().create_task(self._invoke(job.work))
                job.task = task
                try:
                    await asyncio.wait((task,))
                finally:
                    self._running -= 1
                    self._run_sem.release()
                if task.cancelled():
                    job.cancelled = True
                else:
                    exc = task.exception()          # 例外を必ず観測する
                    if exc is not None:
                        job.exc = exc
                    else:
                        job.value = task.result()
                job.task = None
                job.state = _DONE
                self._job_released()                # 受理したジョブの枠はちょうど 1 回解放
                if not job.finished.done():
                    job.finished.set_result(None)
        finally:
            if self._queues.get(key) is q:
                del self._queues[key]

    @staticmethod
    async def _invoke(work: Callable[[], Awaitable[int]]):
        return await work()
```

#### 3. 実行・ビルド方法と使用環境

```powershell
# 作業フォルダー（試験専用の一時領域）: scratchpad\py\q02 に q02.py（実装）と test_q02.py（テスト）を置く
cd <scratchpad>\py\q02
python3.12 --version            # Python 3.12.15
python3.12 -X dev -m unittest -v test_q02
```

**使用環境（全問共通）**

- Python：**CPython 3.12.15**（`3.12.15 (main, Oct  9 2026) [MSC v.1944 64 bit (AMD64)]`）。この PC に既にあった `python3.12` コマンドで実行しました。新しいバージョンへの変更・pip によるパッケージ導入はしていません。
- ライブラリ：Python 標準ライブラリだけです。テストは `unittest` で行いました。
- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド、x86-64）。
- 実行方法：問題ごとの作業フォルダー（`scratchpad\py\qNN`。試験専用の一時領域）に、実装 `qNN.py` とテスト `test_qNN.py` を置いて実行しました。`-X dev`（開発モード：ResourceWarning や asyncio の検査を有効化）を付けています。
- 未実施の環境：Linux・macOS・ARM での実行はしていません。

- **最終の実行結果：** `Ran 7 tests in 10.066s` → `OK`、終了コード 0。
- テストは `unittest.IsolatedAsyncioTestCase` です。各テストの終わりに、イベントループの例外ハンドラーへの通知が 0 件であること（未監視の Task 例外がない）と、残った Task が 0 件であることを検査しました。

#### 4. 実際に使用したテストコード

```python
"""Q02 のテスト（unittest.IsolatedAsyncioTestCase）。"""
from __future__ import annotations

import asyncio
import gc
import random
import time
import unittest

from q02 import AsyncKeyedExecutor


async def spin(n: int = 20) -> None:
    """テスト側の観測用にイベントループを n 回回す（実装はビジー待ちを使わない）。"""
    for _ in range(n):
        await asyncio.sleep(0)


class Base(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.loop_errors = []
        loop = asyncio.get_running_loop()
        loop.set_exception_handler(lambda l, ctx: self.loop_errors.append(ctx))
        loop.slow_callback_duration = 10.0   # -X dev の低速コールバック通知を抑止（検査対象外）

    async def asyncTearDown(self):
        gc.collect()
        await spin(5)
        self.assertEqual(self.loop_errors, [], "unobserved task exceptions / loop errors")
        others = [t for t in asyncio.all_tasks() if t is not asyncio.current_task()]
        self.assertEqual(others, [], "leaked tasks")


class Validation(Base):
    async def test_arguments_and_context_manager(self):
        async with AsyncKeyedExecutor() as ex:
            async def w():
                return 1
            with self.assertRaises(TypeError):
                await ex.submit(1, w)
            with self.assertRaises(ValueError):
                await ex.submit("", w)
            with self.assertRaises(TypeError):
                await ex.submit("k", 123)
            self.assertEqual(await ex.submit("k", w), 1)
        with self.assertRaises(RuntimeError):
            await ex.submit("k", w)


class FifoAndLimits(Base):
    async def test_same_key_100_fifo(self):
        ex = AsyncKeyedExecutor()
        accept_order, log = [], []
        rng = random.Random(1)

        def make(i):
            async def w():
                log.append(("start", i))
                for _ in range(rng.randrange(3)):
                    await asyncio.sleep(0)
                log.append(("end", i))
                return i
            return w

        async def sub(i):
            accept_order.append(i)  # submit 呼出し順（枠は空いているので即受付）
            return await ex.submit("same", make(i))

        results = await asyncio.gather(*(sub(i) for i in range(100)))
        self.assertEqual(results, list(range(100)))
        expected = []
        for i in accept_order:
            expected += [("start", i), ("end", i)]
        self.assertEqual(log, expected)   # 開始・終了が完全に交互かつ受付順
        await ex.aclose()

    async def test_20_keys_max_running_exactly_4(self):
        ex = AsyncKeyedExecutor()
        running = 0
        peak = 0
        over = 0
        per_key_seq = {}
        rng = random.Random(2)

        def make(k, round_no):
            async def w():
                nonlocal running, peak, over
                running += 1
                peak = max(peak, running)
                if running > 4:
                    over += 1
                per_key_seq.setdefault(k, []).append(round_no)
                for _ in range(rng.randrange(1, 4)):
                    await asyncio.sleep(0)
                running -= 1
                return 0
            return w

        tasks = []
        for r in range(10):
            for k in range(20):
                tasks.append(asyncio.ensure_future(ex.submit(f"k{k}", make(f"k{k}", r))))
        await asyncio.gather(*tasks)
        self.assertEqual(peak, 4)
        self.assertEqual(over, 0)
        for k, seqs in per_key_seq.items():
            self.assertEqual(seqs, list(range(10)), k)   # 各キーの開始は受付（投入）順
        await ex.aclose()
        self.assertEqual(ex.accepted_count, 0)


class Backpressure(Base):
    async def test_128_blocked_129th_waits_cancel_and_recovery(self):
        ex = AsyncKeyedExecutor()
        gate = asyncio.Event()
        started = []

        def make(i):
            async def w():
                started.append(i)
                await gate.wait()
                return i
            return w

        first = [asyncio.ensure_future(ex.submit(f"key{i % 32}", make(i))) for i in range(128)]
        await spin()
        self.assertEqual(ex.accepted_count, 128)
        self.assertEqual(ex.free_slots, 0)
        self.assertEqual(ex.running_count, 4)
        self.assertEqual(len(started), 4)

        ran_129 = []

        async def w129():
            ran_129.append(1)
            return 129
        t129 = asyncio.ensure_future(ex.submit("x", w129))
        await spin()
        self.assertFalse(t129.done())
        self.assertEqual(ex.accepted_count, 128)       # 129 件目は未受付
        t129.cancel()                                   # 受付待ち中のキャンセル
        with self.assertRaises(asyncio.CancelledError):
            await t129
        self.assertEqual(ran_129, [])
        self.assertEqual(ex.accepted_count, 128)

        t130 = asyncio.ensure_future(ex.submit("x", w129))
        await spin()
        self.assertFalse(t130.done())
        gate.set()
        results = await asyncio.gather(*first)
        self.assertEqual(results, list(range(128)))
        self.assertEqual(await t130, 129)
        self.assertEqual(ran_129, [1])
        self.assertEqual(sorted(started), list(range(128)))
        self.assertEqual(ex.accepted_count, 0)
        self.assertEqual(ex.free_slots, 128)           # 全枠回復
        # 回復後に 128 件をもう一度受付できる
        gate2 = asyncio.Event()

        async def w2():
            await gate2.wait()
            return 7
        again = [asyncio.ensure_future(ex.submit(f"z{i}", w2)) for i in range(128)]
        await spin()
        self.assertEqual(ex.accepted_count, 128)
        gate2.set()
        self.assertEqual(await asyncio.gather(*again), [7] * 128)
        await ex.aclose()


class CancellationAndErrors(Base):
    async def test_mixed_cancellation_points_and_exceptions(self):
        ex = AsyncKeyedExecutor()
        runs = {}
        block = asyncio.Event()

        def counted(name, body):
            async def w():
                runs[name] = runs.get(name, 0) + 1
                return await body()
            return w

        async def wait_block():
            await block.wait()
            return 1

        # (a) 実行中キャンセル: work は CancelledError を受け取る
        seen_cancel = []

        async def cancellable():
            try:
                await asyncio.Event().wait()
            except asyncio.CancelledError:
                seen_cancel.append("a")
                raise
        ta = asyncio.ensure_future(ex.submit("A", counted("a", cancellable)))
        # (b) 受付後・実行前（A のキューで a の後ろ）
        tb = asyncio.ensure_future(ex.submit("A", counted("b", wait_block)))
        # (c) work がキャンセルを握り潰して値を返す → ポリシーにより CancelledError
        async def swallow():
            try:
                await asyncio.Event().wait()
            except asyncio.CancelledError:
                return 99
        tc = asyncio.ensure_future(ex.submit("C", counted("c", swallow)))
        await spin()
        self.assertEqual(ex.accepted_count, 3)
        tb.cancel()
        with self.assertRaises(asyncio.CancelledError):
            await tb
        self.assertNotIn("b", runs)
        self.assertEqual(ex.accepted_count, 2)
        ta.cancel()
        tc.cancel()
        with self.assertRaises(asyncio.CancelledError):
            await ta
        with self.assertRaises(asyncio.CancelledError):
            await tc
        self.assertEqual(seen_cancel, ["a"])
        self.assertEqual(runs, {"a": 1, "c": 1})
        self.assertEqual(ex.accepted_count, 0)

        # (d) 同期例外（work() 呼出し自体が送出）、(e) 非同期例外、(f) 非 awaitable、(g) work 自身の CancelledError
        def sync_raise():
            runs["d"] = runs.get("d", 0) + 1
            raise KeyError("sync")

        async def async_raise():
            runs["e"] = runs.get("e", 0) + 1
            await asyncio.sleep(0)
            raise ValueError("async")

        def not_awaitable():
            runs["f"] = runs.get("f", 0) + 1
            return 5

        async def self_cancel():
            runs["g"] = runs.get("g", 0) + 1
            raise asyncio.CancelledError()

        async def ok():
            runs["h"] = runs.get("h", 0) + 1
            return 42
        outcomes = await asyncio.gather(
            ex.submit("D", sync_raise), ex.submit("E", async_raise), ex.submit("F", not_awaitable),
            ex.submit("D", ok), return_exceptions=True)
        self.assertIsInstance(outcomes[0], KeyError)
        self.assertIsInstance(outcomes[1], ValueError)
        self.assertIsInstance(outcomes[2], TypeError)
        self.assertEqual(outcomes[3], 42)               # 同じキー D の失敗後も後続は実行される
        with self.assertRaises(asyncio.CancelledError):
            await ex.submit("G", self_cancel)
        self.assertEqual(runs, {"a": 1, "c": 1, "d": 1, "e": 1, "f": 1, "g": 1, "h": 1})

        # (h) 受付前（枠待ち）のキャンセル
        fill = [asyncio.ensure_future(ex.submit(f"f{i}", counted(f"fill{i}", wait_block))) for i in range(128)]
        await spin()
        tw = asyncio.ensure_future(ex.submit("W", counted("w", wait_block)))
        await spin()
        tw.cancel()
        with self.assertRaises(asyncio.CancelledError):
            await tw
        block.set()
        await asyncio.gather(*fill)
        self.assertNotIn("w", runs)
        self.assertTrue(all(runs[f"fill{i}"] == 1 for i in range(128)))
        await ex.aclose()
        self.assertEqual(ex.accepted_count, 0)
        self.assertEqual(ex.free_slots, 128)


class CloseAndLoad(Base):
    async def test_aclose_submit_race_and_double_close(self):
        ex = AsyncKeyedExecutor()
        gate = asyncio.Event()
        done = []

        def make(i):
            async def w():
                await gate.wait()
                done.append(i)
                return i
            return w
        accepted = [asyncio.ensure_future(ex.submit(f"k{i % 7}", make(i))) for i in range(128)]
        await spin()
        waiting = [asyncio.ensure_future(ex.submit("late", make(1000 + i))) for i in range(5)]
        await spin()
        c1 = asyncio.ensure_future(ex.aclose())
        c2 = asyncio.ensure_future(ex.aclose())   # 二重 aclose
        await spin()
        for t in waiting:                           # 枠待ちの submit は RuntimeError
            with self.assertRaises(RuntimeError):
                await t
        with self.assertRaises(RuntimeError):
            await ex.submit("new", make(5000))
        self.assertFalse(c1.done())
        self.assertFalse(c2.done())                 # 受付済みが終わるまで完了しない
        gate.set()
        self.assertEqual(await asyncio.gather(*accepted), list(range(128)))
        await asyncio.wait_for(asyncio.gather(c1, c2), 5)
        await ex.aclose()                            # 3 回目も安全
        self.assertEqual(sorted(done), list(range(128)))
        self.assertEqual(ex.accepted_count, 0)

    async def test_10000_mixed_load(self):
        # IsolatedAsyncioTestCase は debug モードで Future 生成ごとにスタックを採取し非常に遅いので、
        # この負荷試験だけ debug を切る（例外ハンドラーによる未監視例外の検出は有効のまま）。
        asyncio.get_running_loop().set_debug(False)
        ex = AsyncKeyedExecutor()
        rng = random.Random(10_000)
        runs = [0] * 10_000
        plan = []
        outcomes = {"ok": 0, "err": 0, "self_cancel": 0, "caller_cancel": 0, "cancel_while_running": 0}
        task_of = {}
        max_running = 0

        def make(i, kind, steps):
            async def w():
                nonlocal max_running
                runs[i] += 1
                max_running = max(max_running, ex.running_count)
                if kind == "cancel_while_running":
                    task_of[i].cancel()          # 実行開始後に呼出し元 submit をキャンセル
                    await asyncio.Event().wait()  # Task.cancel() で起こされる
                for _ in range(steps):
                    await asyncio.sleep(0)
                if kind == "err":
                    raise ArithmeticError(i)
                if kind == "self_cancel":
                    raise asyncio.CancelledError()
                return i
            return w

        tasks = []
        t0 = time.perf_counter()
        for i in range(10_000):
            kind = rng.choice(["ok", "ok", "err", "self_cancel", "caller_cancel", "cancel_while_running"])
            plan.append(kind)
            t = asyncio.ensure_future(ex.submit(f"k{rng.randrange(50)}", make(i, kind, rng.randrange(4))))
            tasks.append(t)
            task_of[i] = t
            if kind == "caller_cancel":
                asyncio.get_running_loop().call_later(rng.random() * 0.002, t.cancel)
            if i % 97 == 0:
                await asyncio.sleep(0)
        results = await asyncio.wait_for(asyncio.gather(*tasks, return_exceptions=True), 120)
        for i, (kind, r) in enumerate(zip(plan, results)):
            if kind == "ok":
                self.assertEqual(r, i)
                self.assertEqual(runs[i], 1)
            elif kind == "err":
                self.assertIsInstance(r, ArithmeticError)
                self.assertEqual(runs[i], 1)
            elif kind in ("self_cancel", "cancel_while_running"):
                self.assertIsInstance(r, asyncio.CancelledError)
                self.assertEqual(runs[i], 1)
            else:
                # 呼出し元キャンセル: タイミングにより完了済み/取消済みのどちらか。実行は高々 1 回
                self.assertTrue(r == i or isinstance(r, asyncio.CancelledError), r)
                self.assertLessEqual(runs[i], 1)
            outcomes[kind] += 1
        await asyncio.wait_for(ex.aclose(), 10)
        self.assertEqual(ex.accepted_count, 0)
        self.assertEqual(ex.free_slots, 128)
        self.assertLessEqual(max_running, 4)
        never_ran = sum(1 for i, k in enumerate(plan) if k == "caller_cancel" and runs[i] == 0)
        print(f"\n[Q02] 10000 mixed jobs: {outcomes}, caller-cancelled & never run: {never_ran}, "
              f"max running {max_running}, {time.perf_counter() - t0:.2f}s")


if __name__ == "__main__":
    unittest.main(verbosity=2)
```

#### 5. 検証結果（期待値・実測値・実施状況）

| 必須テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 同一キー 100 件の FIFO | 開始・終了が `start i, end i` の交互で、受付順と完全一致 | 一致 | 実施 |
| 20 キーの最大同時実行数 | 20 キー × 10 件で、最大同時実行数がちょうど 4、超過 0。各キーの開始は投入順 | ピーク 4、超過 0 | 実施 |
| 128 件の停止と 129 件目 | Event で 128 件を停止。受付数 128・空き 0・実行中 4。129 件目は未受付のまま待つ | 一致 | 実施 |
| 受付待ちのキャンセル | 129 件目をキャンセルすると CancelledError。work は実行されず、受付数は 128 のまま | 一致 | 実施 |
| 再開後の全枠回復 | 再開後、全 128 件と待っていた 130 件目が完了。受付数 0・空き 128。さらに 128 件を再受付できる | 一致 | 実施 |
| 受付後・実行前のキャンセル | 同じキーの後ろで待つジョブをキャンセル → work は未実行、受付数がすぐ減る | 一致 | 実施 |
| 実行中のキャンセル | work に CancelledError が届く。submit は CancelledError | 一致 | 実施 |
| キャンセルを握り潰した work | 値 99 を返しても、submit は CancelledError（固定ポリシー） | 一致 | 実施 |
| 同期・非同期の例外 | `work()` 自体の KeyError、await 中の ValueError、非 awaitable（TypeError）、work 自身の CancelledError は、それぞれ対応する submit にだけ伝わる。同じキーの後続は実行される | 一致 | 実施 |
| 実行漏れ・二重実行 | 各 work の実行回数は 0（受付前・実行前のキャンセル）か 1 | 一致 | 実施 |
| aclose と submit の競合 | 枠待ち中の 5 件は aclose で即座に RuntimeError。aclose 後の新規 submit も RuntimeError。受付済みの 128 件が終わるまで aclose は完了しない | 一致 | 実施 |
| 二重 aclose | 2 つの aclose が並行しても両方完了し、3 回目も安全 | 一致 | 実施 |
| 1 万件の混合負荷 | 50 キーに 10,000 件（正常・例外・自己キャンセル・受付前後のキャンセル・実行中キャンセル）。結果がすべて計画どおり、最終の受付数 0・空き 128、最大実行数 ≤ 4、停止なし | ok 3,273、err 1,654、self_cancel 1,687、caller_cancel 1,732（全件未実行で終了）、cancel_while_running 1,654、最大 4、1.45 秒 | 実施 |
| 引数検証・async with | key が非 str → TypeError、空文字 → ValueError、work が呼出し不能 → TypeError。`async with` の終了で aclose | 一致 | 実施 |
| 未監視例外・タスクのリーク | 全テストで例外ハンドラーへの通知 0、残存 Task 0 | 0 / 0 | 実施 |

**自律修正**
- **実装の不具合（テストで発見）：** 受付後・実行前にキャンセルした経路で、受付数（`accepted_count`）を減らしていませんでした（枠は返していた）。そこで、受付済みジョブの後片付けを `_job_released()` の 1 か所にまとめ、どの経路でもちょうど 1 回だけ呼ぶようにしました。
- **テストの強化：** 1 万件の試験で、呼出し元のキャンセルがすべて実行前に起きていたため、実行開始後にキャンセルする種別 `cancel_while_running` を追加しました。
- **試験時間：** IsolatedAsyncioTestCase は debug モードで動き、Future を作るたびにスタックを採取するため、1 万件の試験だけで約 90 秒かかっていました（cProfile で `traceback` の処理が約 109 秒中の大半と確認）。そこで、この試験に限り `set_debug(False)` にしました（未監視例外の検出は有効のまま）。
- 修正後に Q02 の全テストを再実行し、合格しました。

#### 6. 技術的な正当性・設計判断

- **受付の定義：** 枠を取得した後、`await` を挟まずにキーのキューへ追加した時点です（`accept_hook` の番号）。受付枠は自前の FIFO 待ち行列で、解放時に待ち手へ直接移譲します。
  - 待ち中にキャンセルされた場合は、待ち行列から外します。
  - 移譲された直後にキャンセルされた場合は、枠を次の待ち手へ返します。
  - どちらの場合も枠は失われず、重複もしません。
- **FIFO：** 各キーのポンプが deque の先頭から 1 件ずつ取り出し、work の Task が終わるまで次へ進みません。そのため、同一キーの開始・終了は受付順で直列になります。
- **実行枠：** `asyncio.Semaphore(4)` をポンプが取得し、work の終了後に finally で解放します。work の Task は常に 4 個以下、ポンプの Task は受付済みの件数（≤ 128）以下で、無制限な create_task はしません。
- **キャンセルのポリシー（固定）：** 呼出し元がキャンセルされた submit は、常に CancelledError で終わります。
  - 実行前なら、キューから外し、枠をその場で 1 回だけ解放します。
  - 実行中なら、`Task.cancel()` を送り、work が終わって後片付けが済むまで `shield` で待ってから CancelledError を送出します。
  - work がキャンセルを握り潰して返した値は捨てます。
- **例外の観測：** ポンプが `task.exception()` を必ず呼び、値・例外・キャンセルの区別を Job に記録します。対応する submit だけが、それを送出・返却します。
- **aclose：** `_closed` を立て、枠待ちの submit をすべて RuntimeError で起こします。そのうえで受付数が 0 になるまで `Event` を待ち、ポンプの終了も待ちます。再入しても安全です。

#### 7. 未実施項目・制約・残課題

- 契約により、複数の OS スレッドからの同時呼出しは扱いません（単一のイベントループ専用）。
- 契約により、work が永久に戻らない場合、aclose も終わりません。
- uvloop など別のイベントループ実装、Linux・macOS での実行はしていません。

---

## Q03【地獄級】ASTホワイトリストで実装する安全な式インタープリタ

**試験の狙い：** eval/execを使わず、構文木の検査・実行量の制御・巨大整数計算の拒否・例外の分類を正確に行う。

### 元コード（危険・不完全）

```python
def calc_bad(source: str, variables: dict[str, int]) -> int:
    return eval(source, {}, variables)  # 任意コード実行の危険
```

### 要求仕様

1. `evaluate(source: str, variables: Mapping[str, int]) -> int` を実装。`ast.parse(source, mode='eval')`のみ構文解析に使用し、`eval`、`exec`、`compile`、`ast.literal_eval`、第三者の計算ライブラリは禁止。
2. 許可するノードは`Expression`, `Constant`（type(value)が厳密にint）, `Name`（Loadのみ）, `UnaryOp`（UAdd/USub）, `BinOp`（Add/Sub/Mult/FloorDiv/Mod/Pow）のみ。Call、Attribute、Subscript、BoolOp、Compare、IfExp、Comprehension、lambda、タプル、true/false、浮動小数はすべて拒否。
3. `source`は最大4096文字、AST全ノード数は最大128、深さは最大32。整数定数と変数値は`abs(v).bit_length()<=256`、最終値および**全中間計算結果**も<=256bit。`variables`の値はboolを拒否し、名前がない場合は`UnknownNameError`。
4. べき乗の指数は0〜64の整数のみ。除数0は`ZeroDivisionError`。負数の`//`・`%`はPython標準の数学的な床除算と剰余の意味。関数呼出しや属性アクセスは評価しない。
5. 上限に違反した式は`EvaluationLimitError`、許可外の構文は`UnsafeExpressionError`、字句・構文エラーは`SyntaxError`、未知名は`UnknownNameError`とする。最初にASTノード数・深さ・構文許可を検査し、**危険な計算を実行する前**に予測可能な巨大値を拒否する。
6. 特に乗算・累乗は、不要な巨大整数を生成せずに上限違反を判断する。ゼロと符号の例外、累乗の境界を正しく扱う。再帰評価する場合、前記深さ制限が有効であることを証明する。

### 必須検証

1. `1+2*3`、負数の床除算/剰余、括弧、単項符号、変数、-2**2の優先順位、2**64と指数65。
2. `__import__('os')`、属性・添字・ラムダ・比較・bool・浮動小数・巨大文字列の全拒否。副作用が一度も実行されないこと。
3. 32/33段・128/129ノード・4096/4097文字・256/257bit、0の巨大指数、分母0、未知変数。
4. 許可された小さな式1万件を別の安全な参照演算モデルと比較。境界の不正式をランダム生成し例外の種類・実行時間を確認。

### ★★FREYJAの回答【Q03・記入欄】

> **以下が専用回答スペース。** 問題本文、元コード、要求仕様、必須検証は改変しない。各項目の例示テキストを実際の内容へ置き換えること。

#### 1. 根本原因・危険性・影響

**元コード `calc_bad` の問題点**

| # | 問題箇所 | 発生条件 | 危険性・実害 |
| --- | --- | --- | --- |
| 1 | `eval(source, {}, variables)` | 任意の入力 | 空の globals でも `__builtins__` が自動で補われるため、`__import__('os').system(...)` などの任意コードが実行できる |
| 2 | 構文・型の制限がない | 属性・添字・ラムダ・比較・浮動小数 | 内部オブジェクトへのアクセス（`().__class__.__mro__`…）、int 以外の結果 |
| 3 | 計算量の上限がない | `9**9**9`、巨大な乗算 | CPU とメモリを使い果たす（DoS） |
| 4 | 入力の大きさ・深さの上限がない | 長大な式・深い入れ子 | パーサーの RecursionError、メモリ消費 |
| 5 | 例外の分類がない | 未知名・0 除算・構文エラー | 呼出し元がエラーを区別できない |

**修正方針**
- 構文解析は `ast.parse(source, mode='eval')` だけで行います。eval・exec・compile・literal_eval は使いません。
- 評価の前に、非再帰の走査でノード数・深さ・ノード種別・定数の型と大きさを検査します。
- 評価は、許可した 5 種類のノードだけを扱う独自のインタープリタで行います。乗算と累乗は、結果のビット長の下限を先に計算し、上限を超えるものは巨大な整数を作らずに拒否します。

#### 2. 完全な修正版Pythonコード

```python
"""Q03: AST ホワイトリストによる安全な整数式インタープリタ（eval/exec/compile/literal_eval 不使用）。

上限の定義（本実装で固定）:
  * source の長さ           : len(source) <= 4096 文字
  * AST 全ノード数          : ast.walk() が列挙する全ノード（Expression・演算子・Load を含む）<= 128
  * 深さ                    : 式ノード（Constant/Name/UnaryOp/BinOp 等の ast.expr）の入れ子段数 <= 32
                              （本体の式が 1 段目。Expression ラッパーと演算子・ctx ノードは段数に数えない）
  * 整数の大きさ            : 定数・変数値・全中間結果・最終値について abs(v).bit_length() <= 256
  * べき乗の指数            : 0 <= exponent <= 64
"""
from __future__ import annotations

import ast
from typing import Mapping

__all__ = [
    "evaluate", "EvaluationLimitError", "UnsafeExpressionError", "UnknownNameError",
    "MAX_SOURCE_CHARS", "MAX_NODES", "MAX_DEPTH", "MAX_BITS", "MAX_EXPONENT",
]

MAX_SOURCE_CHARS = 4096
MAX_NODES = 128
MAX_DEPTH = 32
MAX_BITS = 256
MAX_EXPONENT = 64


class EvaluationLimitError(Exception):
    """長さ・ノード数・深さ・整数ビット長・指数の上限違反。"""


class UnsafeExpressionError(Exception):
    """許可されていない構文（ノード種別・演算子・定数型）。"""


class UnknownNameError(NameError):
    """variables に存在しない名前。"""


_ALLOWED_NODE_TYPES = (ast.Expression, ast.Constant, ast.Name, ast.UnaryOp, ast.BinOp,
                       ast.Load, ast.UAdd, ast.USub,
                       ast.Add, ast.Sub, ast.Mult, ast.FloorDiv, ast.Mod, ast.Pow)


def _bits(v: int) -> int:
    return (v if v >= 0 else -v).bit_length()


def _check_int(v: int, what: str) -> int:
    if _bits(v) > MAX_BITS:
        raise EvaluationLimitError(f"{what} exceeds {MAX_BITS} bits")
    return v


def _validate(tree: ast.Expression) -> None:
    """非再帰の走査でノード数・深さ・許可構文をすべて検査する（評価より前に実行）。"""
    count = 0
    max_depth = 0
    first_unsafe: str | None = None
    stack: list[tuple[ast.AST, int]] = [(tree, 0)]
    while stack:
        node, depth = stack.pop()
        count += 1
        if isinstance(node, ast.expr):
            depth += 1
            if depth > max_depth:
                max_depth = depth
        if first_unsafe is None:
            if type(node) not in _ALLOWED_NODE_TYPES:
                first_unsafe = type(node).__name__
            elif type(node) is ast.Constant and type(node.value) is not int:
                first_unsafe = f"constant of type {type(node.value).__name__}"
            elif type(node) is ast.Name and type(node.ctx) is not ast.Load:
                first_unsafe = "non-load name"
        for child in ast.iter_child_nodes(node):
            stack.append((child, depth))
    if count > MAX_NODES:
        raise EvaluationLimitError(f"AST has {count} nodes (max {MAX_NODES})")
    if max_depth > MAX_DEPTH:
        raise EvaluationLimitError(f"AST depth {max_depth} (max {MAX_DEPTH})")
    if first_unsafe is not None:
        raise UnsafeExpressionError(f"disallowed syntax: {first_unsafe}")
    # 定数の大きさは評価前に確認する（巨大リテラルを演算に使わない）
    for node in ast.walk(tree):
        if type(node) is ast.Constant:
            _check_int(node.value, "integer constant")


def _mul(a: int, b: int) -> int:
    if a == 0 or b == 0:
        return 0
    # bit_length(a*b) は la+lb-1 または la+lb。下限が上限を超えるなら掛け算しない。
    if _bits(a) + _bits(b) - 1 > MAX_BITS:
        raise EvaluationLimitError("product exceeds 256 bits")
    return _check_int(a * b, "product")


def _pow(base: int, exp: int) -> int:
    if not 0 <= exp <= MAX_EXPONENT:
        raise EvaluationLimitError(f"exponent must be within 0..{MAX_EXPONENT}")
    if exp == 0:
        return 1
    if base == 0 or base == 1:
        return base
    if base == -1:
        return -1 if exp & 1 else 1
    lb = _bits(base)
    # |base| >= 2 のとき bit_length(base**exp) >= (lb-1)*exp + 1。下限で先に拒否する。
    if (lb - 1) * exp + 1 > MAX_BITS:
        raise EvaluationLimitError("power exceeds 256 bits")
    return _check_int(base ** exp, "power")      # ここでの最大は lb*exp <= 256+exp-1 < 320 ビット


def _eval(node: ast.AST, variables: Mapping[str, int]) -> int:
    # 再帰の深さ <= 検査済みの式の深さ (<= 32) なので、Python の再帰上限に達しない。
    t = type(node)
    if t is ast.Constant:
        return node.value
    if t is ast.Name:
        try:
            v = variables[node.id]
        except KeyError:
            raise UnknownNameError(f"unknown name {node.id!r}") from None
        if type(v) is not int:            # bool・float などは拒否（bool は int の派生型）
            raise TypeError(f"variable {node.id!r} must be int, not {type(v).__name__}")
        return _check_int(v, f"variable {node.id!r}")
    if t is ast.UnaryOp:
        v = _eval(node.operand, variables)
        return -v if type(node.op) is ast.USub else v
    if t is ast.BinOp:
        a = _eval(node.left, variables)
        b = _eval(node.right, variables)
        op = type(node.op)
        if op is ast.Add:
            return _check_int(a + b, "sum")
        if op is ast.Sub:
            return _check_int(a - b, "difference")
        if op is ast.Mult:
            return _mul(a, b)
        if op is ast.FloorDiv:
            if b == 0:
                raise ZeroDivisionError("integer division by zero")
            return _check_int(a // b, "quotient")   # Python 標準の床除算
        if op is ast.Mod:
            if b == 0:
                raise ZeroDivisionError("integer modulo by zero")
            return _check_int(a % b, "remainder")   # 除数と同符号の剰余
        if op is ast.Pow:
            return _pow(a, b)
    raise UnsafeExpressionError(f"disallowed syntax: {t.__name__}")   # 検査済みなので到達しない


def evaluate(source: str, variables: Mapping[str, int]) -> int:
    if not isinstance(source, str):
        raise TypeError("source must be str")
    if len(source) > MAX_SOURCE_CHARS:
        raise EvaluationLimitError(f"source longer than {MAX_SOURCE_CHARS} characters")
    try:
        tree = ast.parse(source, mode="eval")
    except (RecursionError, MemoryError):
        # 4096 文字以内でも極端な入れ子はパーサーの再帰上限に達する。深さ上限の違反として扱う。
        raise EvaluationLimitError("expression nesting too deep") from None
    except ValueError as exc:            # 念のため（3.12 では NUL も SyntaxError）
        raise SyntaxError(str(exc)) from None
    _validate(tree)
    return _eval(tree.body, variables)
```

#### 3. 実行・ビルド方法と使用環境

```powershell
# 作業フォルダー（試験専用の一時領域）: scratchpad\py\q03 に q03.py（実装）と test_q03.py（テスト）を置く
cd <scratchpad>\py\q03
python3.12 --version            # Python 3.12.15
python3.12 -X dev -m unittest -v test_q03
```

**使用環境（全問共通）**

- Python：**CPython 3.12.15**（`3.12.15 (main, Oct  9 2026) [MSC v.1944 64 bit (AMD64)]`）。この PC に既にあった `python3.12` コマンドで実行しました。新しいバージョンへの変更・pip によるパッケージ導入はしていません。
- ライブラリ：Python 標準ライブラリだけです。テストは `unittest` で行いました。
- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド、x86-64）。
- 実行方法：問題ごとの作業フォルダー（`scratchpad\py\qNN`。試験専用の一時領域）に、実装 `qNN.py` とテスト `test_qNN.py` を置いて実行しました。`-X dev`（開発モード：ResourceWarning や asyncio の検査を有効化）を付けています。
- 未実施の環境：Linux・macOS・ARM での実行はしていません。

- **最終の実行結果：** `Ran 14 tests in 2.731s` → `OK`、終了コード 0。

#### 4. 実際に使用したテストコード

```python
"""Q03 のテスト。参照モデル RefExpr は文字列を介さず木構造を直接計算する独立実装。"""
from __future__ import annotations

import ast
import random
import time
import unittest
from collections.abc import Mapping

from q03 import (EvaluationLimitError, UnknownNameError, UnsafeExpressionError, evaluate)

LIMIT_BITS = 256


class RecordingMapping(Mapping):
    """アクセスを記録する変数表（副作用・評価の有無を観測する）。"""

    def __init__(self, data):
        self.data = dict(data)
        self.accessed = []

    def __getitem__(self, k):
        self.accessed.append(k)
        return self.data[k]

    def __iter__(self):
        return iter(self.data)

    def __len__(self):
        return len(self.data)


def balanced_sum(n: int) -> str:
    """n 個の '1' を均衡二分木の + で結ぶ（ノード数 3n-1、深さ ceil(log2 n)+1）。"""
    if n == 1:
        return "1"
    h = n // 2
    return f"({balanced_sum(h)}+{balanced_sum(n - h)})"


class Basics(unittest.TestCase):
    def test_arithmetic(self):
        v = {"x": 7, "y": -3, "big": 2**255}
        cases = {
            "1+2*3": 7, "(1+2)*3": 9, "-7//2": -4, "-7%2": 1, "7//-2": -4, "7%-2": -1,
            "-7//-2": 3, "-7%-2": -1, "+5": 5, "--5": 5, "-+-5": 5, "-2**2": -4, "(-2)**2": 4,
            "2**64": 18446744073709551616, "x*y-x//y": -21 - (7 // -3), "x%y": 7 % -3,
            "big-big+big": 2**255, "big-1+big": 2**256 - 1, "2**3**2": 512, "0**0": 1, "(-1)**63": -1, "1**64": 1,
            "1 +\t2 ": 3,
        }
        for src, want in cases.items():
            self.assertEqual(evaluate(src, v), want, src)
            self.assertIs(type(evaluate(src, v)), int)

    def test_exponent_65_and_negative(self):
        for src in ("2**65", "0**65", "1**65", "0**1000000", "2**-1", "(-1)**65"):
            with self.assertRaises(EvaluationLimitError, msg=src):
                evaluate(src, {})

    def test_zero_division_and_unknown_name(self):
        for src in ("1//0", "1%0", "x//(y-y)"):
            with self.assertRaises(ZeroDivisionError, msg=src):
                evaluate(src, {"x": 1, "y": 2})
        with self.assertRaises(UnknownNameError):
            evaluate("a+1", {})
        with self.assertRaises(NameError):          # UnknownNameError は NameError の派生
            evaluate("q", {"x": 1})

    def test_variable_values_rejected(self):
        for bad in (True, False, 1.0, "1", None):
            with self.assertRaises(TypeError, msg=repr(bad)):
                evaluate("x", {"x": bad})
        with self.assertRaises(EvaluationLimitError):
            evaluate("x", {"x": 2**256})
        self.assertEqual(evaluate("x", {"x": -(2**256 - 1)}), -(2**256 - 1))

    def test_syntax_errors(self):
        for src in ("1+", "(", "1 2", "x = 1", "", "1\x002", " 1", "import os"):
            with self.assertRaises(SyntaxError, msg=repr(src)):
                evaluate(src, {})
        with self.assertRaises(TypeError):
            evaluate(b"1", {})


class UnsafeRejected(unittest.TestCase):
    def test_all_rejected_without_side_effects(self):
        side = []

        class Evil:
            def __getattr__(self, name):
                side.append(name)
                return 1
        env = RecordingMapping({"x": 1, "obj": 1, "__import__": 1})
        unsafe = [
            "__import__('os')", "__import__('os').system('echo pwned')", "x.real", "obj.attr",
            "x[0]", "lambda: 1", "(lambda: x)()", "x < 1", "1 == 1", "x and 1", "not x", "~x",
            "1 if x else 2", "True", "False", "None", "1.5", "1e3", "2j", "(1, 2)", "[1]", "{1: 2}",
            "{1}", "[i for i in x]", "'abc'", "b'abc'", "f'{x}'", "x / 2", "x @ x", "x << 2",
            "x | 1", "x ^ 1", "x & 1", "x >> 1", "(x := 1)", "...",
            "'" + "a" * 4000 + "'", "-True", "1 + True", "2 ** 1.0",
        ]
        for src in unsafe:
            with self.assertRaises(UnsafeExpressionError, msg=src):
                evaluate(src, env)
        self.assertEqual(env.accessed, [])          # 検査段階で拒否され、変数表に一度も触れない
        self.assertEqual(side, [])

    def test_huge_string_over_length(self):
        with self.assertRaises(EvaluationLimitError):
            evaluate("'" + "a" * 5000 + "'", {})


class Limits(unittest.TestCase):
    def test_depth_32_33(self):
        self.assertEqual(evaluate("-" * 31 + "1", {}), -1)      # 31 段の UnaryOp + 定数 = 32 段
        with self.assertRaises(EvaluationLimitError):
            evaluate("-" * 32 + "1", {})
        nested = "1"
        for _ in range(31):
            nested = f"(1+{nested})"
        self.assertEqual(evaluate(nested, {}), 32)                # BinOp 31 段 + 定数 = 32 段
        with self.assertRaises(EvaluationLimitError):
            evaluate(f"(1+{nested})", {})
        with self.assertRaises(EvaluationLimitError):            # パーサーの再帰上限 → 深さ違反
            evaluate("-" * 4000 + "1", {})

    def test_nodes_128_129(self):
        s128 = balanced_sum(43)
        self.assertEqual(sum(1 for _ in ast.walk(ast.parse(s128, mode="eval"))), 128)
        self.assertEqual(evaluate(s128, {}), 43)
        s129 = "-(-" + balanced_sum(42) + ")"
        self.assertEqual(sum(1 for _ in ast.walk(ast.parse(s129, mode="eval"))), 129)
        with self.assertRaises(EvaluationLimitError):
            evaluate(s129, {})

    def test_length_4096_4097(self):
        s = "1" + " " * 4095
        self.assertEqual(len(s), 4096)
        self.assertEqual(evaluate(s, {}), 1)
        with self.assertRaises(EvaluationLimitError):
            evaluate(s + " ", {})

    def test_bits_256_257(self):
        m = 2**256 - 1
        self.assertEqual(evaluate(str(m), {}), m)
        self.assertEqual(evaluate(f"-{m}", {}), -m)
        with self.assertRaises(EvaluationLimitError):
            evaluate(str(2**256), {})
        self.assertEqual(evaluate("2**64*2**64*2**64*2**63", {}), 2**255)        # 指数は 0..64 のみ
        self.assertEqual(evaluate("x-1+x", {"x": 2**255}), 2**256 - 1)        # 中間値 2**255-1 は 255 ビット
        self.assertEqual(evaluate(f"{m}-1+1", {}), m)
        for src in ("x*2", "x+x", f"{m}+1", f"-{m}-1", "(2**64)**2*(2**64)**2", "(2**64)**4",
                    "(2**64)**4*2", "((3**27)**3)**2", "x*x", f"{m}*{m}", "-x-x"):
            with self.assertRaises(EvaluationLimitError, msg=src):
                evaluate(src, {"x": 2**255})
        self.assertEqual(evaluate("(3**23)**7", {}), 3**161)      # 3**161 は 256 ビット（指数は各 64 以下）
        self.assertEqual((3**161).bit_length(), 256)
        self.assertEqual(evaluate("(x-1)*(x+1)", {"x": 2**128}), 2**256 - 1)

    def test_big_multiplication_not_materialized(self):
        # 256 ビットの値同士の積は計算前に拒否される（ビット長の下限で判断）
        t0 = time.perf_counter()
        for _ in range(1000):
            with self.assertRaises(EvaluationLimitError):
                evaluate("x*x", {"x": 2**255})
            with self.assertRaises(EvaluationLimitError):
                evaluate("x**64", {"x": 2**255})
        self.assertLess(time.perf_counter() - t0, 2.0)


# ------------------------------------------------------------------ 参照モデル
class RefLimit(Exception):
    pass


def ref_eval(t, env):
    kind = t[0]
    if kind == "c":
        return t[1]
    if kind == "v":
        return env[t[1]]
    if kind == "neg":
        return -ref_eval(t[1], env)
    if kind == "pos":
        return ref_eval(t[1], env)
    a = ref_eval(t[1], env)
    b = ref_eval(t[2], env)
    if kind == "+":
        r = a + b
    elif kind == "-":
        r = a - b
    elif kind == "*":
        r = a * b
    elif kind == "//":
        if b == 0:
            raise ZeroDivisionError
        r = a // b
    elif kind == "%":
        if b == 0:
            raise ZeroDivisionError
        r = a % b
    else:
        if b < 0 or b > 64:
            raise RefLimit
        r = a ** b          # 参照側は小さい値だけ扱う（|a| <= 2**256, b <= 64）
    if abs(r).bit_length() > LIMIT_BITS:
        raise RefLimit
    return r


def to_src(t):
    kind = t[0]
    if kind == "c":
        return str(t[1]) if t[1] >= 0 else f"({t[1]})"
    if kind == "v":
        return t[1]
    if kind == "neg":
        return f"(-{to_src(t[1])})"
    if kind == "pos":
        return f"(+{to_src(t[1])})"
    return f"({to_src(t[1])}{kind}{to_src(t[2])})"


def gen(rng, depth):
    if depth == 0 or rng.random() < 0.3:
        if rng.random() < 0.4:
            return ("v", rng.choice("abc"))
        return ("c", rng.choice([0, 1, 2, 3, 7, 10, 255, 2**64, 2**200, rng.randrange(-1000, 1000)]))
    r = rng.random()
    if r < 0.15:
        return (rng.choice(["neg", "pos"]), gen(rng, depth - 1))
    op = rng.choice(["+", "-", "*", "//", "%", "**"])
    if op == "**":
        return (op, gen(rng, depth - 1), ("c", rng.choice([0, 1, 2, 3, 5, 64, 65])))
    return (op, gen(rng, depth - 1), gen(rng, depth - 1))


class RandomVsReference(unittest.TestCase):
    def test_10000_small_expressions(self):
        rng = random.Random(3)
        stats = {"value": 0, "limit": 0, "zerodiv": 0}
        n = 0
        while n < 10_000:
            tree = gen(rng, 4)
            src = to_src(tree)
            env = {"a": rng.randrange(-50, 50), "b": rng.randrange(-5, 5), "c": rng.choice([2**100, -(2**255)])}
            if sum(1 for _ in ast.walk(ast.parse(src, mode="eval"))) > 128:
                continue
            n += 1
            try:
                want = ("value", ref_eval(tree, env))
            except RefLimit:
                want = ("limit", None)
            except ZeroDivisionError:
                want = ("zerodiv", None)
            try:
                got = ("value", evaluate(src, env))
            except EvaluationLimitError:
                got = ("limit", None)
            except ZeroDivisionError:
                got = ("zerodiv", None)
            self.assertEqual(got, want, src)
            stats[want[0]] += 1
        print(f"\n[Q03] 10000 expressions vs reference model: {stats}")

    def test_random_invalid_boundary_expressions(self):
        rng = random.Random(4)
        counts = {}
        worst = 0.0
        for i in range(3000):
            kind = rng.randrange(6)
            if kind == 0:
                src, exp = "-" * rng.randint(32, 60) + "1", EvaluationLimitError         # 深さ 33 以上
            elif kind == 1:
                src, exp = balanced_sum(rng.randint(44, 300)), EvaluationLimitError      # ノード数 129 以上
            elif kind == 2:
                src, exp = "1" + " " * rng.randint(4096, 6000), EvaluationLimitError     # 4097 文字以上
            elif kind == 3:
                src, exp = str(2**256 + rng.randrange(2**300)), EvaluationLimitError     # 257 ビット以上
            elif kind == 4:
                src = rng.choice(["x.y", "f()", "x[1]", "1<2", "True", "1.0", "(1,)", "lambda:0", "x if x else x"])
                src, exp = f"1+({src})", UnsafeExpressionError
            else:
                src, exp = rng.choice(["2**", "(1", "1+*2", "))"]), SyntaxError
            t0 = time.perf_counter()
            with self.assertRaises(exp, msg=src[:40]):
                evaluate(src, {"x": 1})
            worst = max(worst, time.perf_counter() - t0)
            counts[exp.__name__] = counts.get(exp.__name__, 0) + 1
        self.assertLess(worst, 0.5)
        print(f"\n[Q03] 3000 invalid boundary expressions: {counts}, worst time {worst * 1000:.2f} ms")


if __name__ == "__main__":
    unittest.main(verbosity=2)
```

#### 5. 検証結果（期待値・実測値・実施状況）

**上限の定義（本実装で固定）**
- ノード数：`ast.walk` が列挙する全ノード（Expression・演算子・Load を含む）。
- 深さ：式ノード（ast.expr）の入れ子段数。本体の式が 1 段目です。

| 必須テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| `1+2*3`・括弧・単項符号 | 7、`(1+2)*3`=9、`--5`=5、`-+-5`=5 | 一致 | 実施 |
| 負数の床除算・剰余 | `-7//2`=-4、`-7%2`=1、`7//-2`=-4、`7%-2`=-1、`-7//-2`=3 | 一致 | 実施 |
| 変数 | `x*y-x//y`（x=7, y=-3）が Python の意味と一致 | 一致 | 実施 |
| `-2**2` の優先順位 | -4（`(-2)**2` は 4） | 一致 | 実施 |
| `2**64` と指数 65 | 18446744073709551616 / `2**65`・`0**65`・`1**65`・`2**-1` は EvaluationLimitError | 一致 | 実施 |
| `__import__('os')` など | Call・Attribute・Subscript・Lambda・Compare・BoolOp・not・~・IfExp・True/False/None・浮動小数・複素数・タプル・リスト・辞書・集合・内包表記・文字列・bytes・f 文字列・`/`・`@`・ビット演算・`:=`・`...` の計 40 式がすべて UnsafeExpressionError | 40/40 | 実施 |
| 副作用が一度も起きない | 記録用の Mapping へのアクセスが 0 回（評価前の検査で拒否されるため） | 0 回 | 実施 |
| 巨大な文字列 | 4000 文字の文字列定数は Unsafe、5000 文字は長さの上限で EvaluationLimitError | 一致 | 実施 |
| 深さ 32 / 33 段 | 単項 31 段 + 定数 = 32 段は OK、33 段は Limit。BinOp 31 段 + 定数も同様。単項 4000 段（パーサーの再帰上限）も Limit | 一致 | 実施 |
| ノード数 128 / 129 | 均衡和（43 項）= 128 ノードは OK、129 ノードは Limit（テスト側で `ast.walk` の個数も確認） | 一致 | 実施 |
| 4096 / 4097 文字 | 4096 文字は OK、4097 文字は Limit | 一致 | 実施 |
| 256 / 257 ビット | `2**256-1` の定数と `-(2**256-1)` は OK、`2**256` の定数は Limit。中間値・最終値が 257 ビットになる式 11 種（`x*2`・`x+x`・`(2**64)**4`・`((3**27)**3)**2`・`x*x` など）はすべて Limit。`(3**23)**7`（256 ビット）・`(x-1)*(x+1)` は OK | 一致 | 実施 |
| 巨大な積を計算しない | `x*x`・`x**64`（x=2**255）を各 1000 回、ビット長の下限で拒否。合計 2 秒未満 | 一致 | 実施 |
| 0 の巨大な指数・0 除算・未知の変数 | `0**1000000` は Limit、`1//0`・`1%0`・`x//(y-y)` は ZeroDivisionError、未知名は UnknownNameError（NameError の派生） | 一致 | 実施 |
| 変数値の検証 | True/False/1.0/"1"/None は TypeError、2**256 は Limit | 一致 | 実施 |
| 構文エラー | `1+`・`(`・`1 2`・`x = 1`・空文字・NUL・先頭の空白・`import os` は SyntaxError | 一致 | 実施 |
| 小さな式 1 万件と参照モデルの比較 | 木構造を直接計算する参照モデル（文字列を経由しない）と、値・Limit・ZeroDivision の種別が一致 | 10,000/10,000 一致（値 7,328、Limit 1,769、0 除算 903） | 実施 |
| 境界の不正式 3000 件 | 深さ 33 以上・ノード数 129 以上・4097 文字以上・257 ビット以上・許可外の構文・構文エラーをランダム生成し、例外の種類が期待どおり。1 件あたりの最大時間 | Limit 1,996、Syntax 516、Unsafe 488。最大 5.26 ms | 実施 |

**自律修正（すべてテスト側の期待値・入力の誤り）**
- `big+big` と `x*2` は中間値が 2**256（257 ビット）なので、Limit が正しい挙動でした。
- `2**255`・`3**161`・`2**128` は指数の上限 64 に違反するので、Limit が正しい挙動でした。ビット長の境界は、指数 64 以下の式に組み直しました。
- 先頭に空白がある式は、`ast.parse` の eval モードでは IndentationError（SyntaxError の派生）になるので、期待値を直しました。
- `1+lambda:0` は構文エラーになるので、括弧で囲みました。
- 実装は一度も変更していません。修正後に Q03 の全テストを再実行し、合格しました。

#### 6. 技術的な正当性・設計判断

- **安全性：** `ast.parse` は構文木を作るだけで、コードを実行しません。評価器は Constant・Name・UnaryOp・BinOp だけを解釈します。Call や Attribute は検査段階で拒否されるので、評価器に到達しません。変数は与えられた Mapping からだけ読み、組込み関数や globals には一切アクセスしません。
- **検査の順序：**
  1. 長さ（4096 文字以下）。
  2. `ast.parse`（RecursionError・MemoryError は深さ違反として Limit）。
  3. 明示的なスタックを使う非再帰の走査で、全ノード数・深さ・最初の許可外ノードを求める。判定はノード数 → 深さ → 許可外の構文の順。
  4. 全定数のビット長。
  5. 評価（変数値の型と大きさは、参照する時点で検査）。
- **巨大な整数を作らない根拠：**
  - 乗算：`bitlen(a*b) ≥ la+lb-1` なので、`la+lb-1 > 256` なら計算せずに拒否します。それ以外の積は高々 512 ビットです。
  - 累乗：|base| ≥ 2 のとき `bitlen(base**e) ≥ (lb-1)*e + 1` なので、これが 256 を超えれば拒否します。それ以外の計算結果は `lb*e ≤ 256 + e - 1 < 320` ビットです。
  - 0・±1 の底と指数 0 は、計算せずに結果を決めます。
  - 加減算と剰余は、256 ビットの値同士なので結果は高々 257 ビットで、計算した後に検査します。
- **再帰の深さの証明：** `_eval` の再帰の深さは、式ノードの入れ子段数と等しく、評価の前に 32 以下であることを確認済みです。したがって再帰は高々 32 段で、Python の既定の再帰上限（1000）に達しません。
- **意味：** `//` と `%` は Python の int 演算をそのまま使うので、床除算・除数と同符号の剰余という数学的な意味になります。

#### 7. 未実施項目・制約・残課題

- 深さとノード数の数え方は、上の定義で固定しました。問題文には厳密な定義がないため、この定義に依存する境界値になります。
- 変数値が int 以外（bool を含む）の場合は TypeError としました。問題文は「bool を拒否」とだけ規定しているため、例外の種類はこの実装で選んだものです。
- Linux・macOS での実行はしていません。

---

## Q04【地獄級】SQLite WALで実装するexactly-once送金台帳

**試験の狙い：** トランザクション、冪等キー、一意性制約、クラッシュ時の原子性、複数プロセスからの競合。

### 元コード（危険・不完全）

```python
import sqlite3
def send_bad(db, src, dst, cents):
    db.execute("UPDATE accounts SET balance=balance-? WHERE id=?", (cents,src))
    db.execute("UPDATE accounts SET balance=balance+? WHERE id=?", (cents,dst))
    db.commit()  # 残高不足・冪等性・競合管理を無視
```

### 要求仕様

1. `SqliteLedger(path: str)` に `create_account(account_id: str, cents: int) -> None`、`transfer(op_id: str, src: str, dst: str, cents: int) -> TransferResult`、`balances() -> dict[str,int]`、`close()`を実装。SQLite3標準モジュールのみ使用。口座名とop_idは1〜64文字のASCII英数字・`_`・`-`のみ。
2. 初期残高・送金額はセント単位の非負の整数。口座残高は0〜(2^63-1)。送金時はcents>0、src!=dst、両口座が存在し、残高不足や送金先の上限超過を拒否。SQLにはすべてパラメータを使用し文字列連結でSQLを組み立てない。
3. `op_id`は送金の冪等キー。**成功済みの同一op_idと同一の(src,dst,cents)なら再実行しても残高を変更せず、既存結果を返す**。同じop_idで異なるパラメータは`ConflictingOperationError`。失敗（残高不足等）は成功扱いで記録しないので、その後の正しい再試行を許す。
4. DBスキーマを自動作成し、journal_mode=WAL、synchronous=FULLを設定。送金は`BEGIN IMMEDIATE`から更新・成功記録まで1トランザクションにし、途中エラー時はROLLBACK。成功結果を返す前にCOMMITが成功している必要がある。
5. 同一DBに対する**4つの独立したプロセス**からの送金を安全に処理。SQLiteのbusy_timeoutを設定し、ロック取得失敗は有界回数の再試行後に明示的失敗として通知してよい。無限ループで待ち続けない。
6. 各送金で口座総額保存・負残高なし・成功op_idちょうど1記録。例外注入フックは `after_debit`・`after_credit`・`before_commit` の3か所。自分が作ったテストDBだけで試験し、既存の利用者ファイルには触れない。
7. Pythonのスレッド間で1つのsqlite3.Connectionを無許可共有しない。各プロセスが独自コネクションを保持すること。

### 必須検証

1. 残高0/1/MAX、存在しない口座、同一口座、負額・ゼロ・上限超過、文字列検証・SQLインジェクション拒否。
2. 成功したop_idを100回再送して残高が1回しか変化しないこと、同op_id異パラメータ拒否、失敗後同op_idの再試行。
3. 各障害フックで例外を発生させた後にDBを再オープンし、残高と成功記録が両方旧版であることを検証。
4. spawn方式の4プロセスから合計10万送金を試し、成功数/総額/冪等性/エラーを検証。WALとsynchronous設定およびDB integrity_checkの結果を記録。

### ★★FREYJAの回答【Q04・記入欄】

> **以下が専用回答スペース。** 問題本文、元コード、要求仕様、必須検証は改変しない。各項目の例示テキストを実際の内容へ置き換えること。

#### 1. 根本原因・危険性・影響

**元コード `send_bad` の問題点**

| # | 問題箇所 | 発生条件 | 危険性・実害 |
| --- | --- | --- | --- |
| 1 | 残高不足を検査しない | 残高を超える送金 | 負の残高（存在しないお金の送金） |
| 2 | 口座の存在を確認しない | 存在しない src・dst | UPDATE が 0 行でも commit され、お金が消える・増える（総額が保存されない） |
| 3 | 冪等性がない | 通信断・タイムアウト後の再送 | 同じ送金が二重に実行される |
| 4 | トランザクション境界が暗黙的 | 2 つの UPDATE の間の例外・クラッシュ | 引き落としだけ確定する可能性。ROLLBACK がない |
| 5 | 先にロックを取らない（暗黙の DEFERRED） | 複数プロセスの同時送金 | 読み取りから書込みへの昇格で SQLITE_BUSY やデッドロック。busy_timeout がない |
| 6 | 入力を検証しない | 負の額・0・同一口座・上限超過・不正な ID | 逆向きの送金、オーバーフロー |
| 7 | WAL・synchronous の設定がない | 電源断・並行読み書き | 耐久性・並行性が不足 |

**修正方針**
- 送金は `BEGIN IMMEDIATE`（最初に書込みロックを取る）から、残高の検査・2 回の UPDATE・成功記録の INSERT・COMMIT までを 1 トランザクションにします。
- 失敗や例外では必ず ROLLBACK します。
- `transfers.op_id` の UNIQUE と事前の照会で exactly-once を保証します。同じ op_id・同じ引数の再送には、保存した結果を返します。異なる引数なら `ConflictingOperationError` にします。
- SQL はすべてプレースホルダーを使います。ID は `[A-Za-z0-9_-]{1,64}` に一致するか検証します。

#### 2. 完全な修正版Pythonコード

```python
"""Q04: SQLite WAL による exactly-once 送金台帳（sqlite3 標準モジュールのみ）。"""
from __future__ import annotations

import re
import sqlite3
from dataclasses import dataclass
from typing import Callable

__all__ = [
    "SqliteLedger", "TransferResult", "LedgerError", "InvalidArgumentError", "UnknownAccountError",
    "AccountExistsError", "InsufficientFundsError", "BalanceOverflowError",
    "ConflictingOperationError", "LedgerBusyError", "MAX_BALANCE", "FAULT_POINTS",
]

MAX_BALANCE = 2**63 - 1
FAULT_POINTS = ("after_debit", "after_credit", "before_commit")
_ID_RE = re.compile(r"[A-Za-z0-9_-]{1,64}")

_SCHEMA = (
    "CREATE TABLE IF NOT EXISTS accounts ("
    " id TEXT PRIMARY KEY,"
    " balance INTEGER NOT NULL CHECK (balance >= 0))",
    "CREATE TABLE IF NOT EXISTS transfers ("
    " seq INTEGER PRIMARY KEY AUTOINCREMENT,"
    " op_id TEXT NOT NULL UNIQUE,"
    " src TEXT NOT NULL REFERENCES accounts(id),"
    " dst TEXT NOT NULL REFERENCES accounts(id),"
    " cents INTEGER NOT NULL CHECK (cents > 0),"
    " src_after INTEGER NOT NULL CHECK (src_after >= 0),"
    " dst_after INTEGER NOT NULL CHECK (dst_after >= 0),"
    " CHECK (src <> dst))",
)


class LedgerError(Exception):
    """台帳操作の失敗（下記の派生クラスで種別を区別する）。"""


class InvalidArgumentError(LedgerError, ValueError):
    pass


class UnknownAccountError(LedgerError):
    pass


class AccountExistsError(LedgerError):
    pass


class InsufficientFundsError(LedgerError):
    pass


class BalanceOverflowError(LedgerError):
    pass


class ConflictingOperationError(LedgerError):
    """成功済みの op_id が異なる (src, dst, cents) で再送された。"""


class LedgerBusyError(LedgerError):
    """有界回数の再試行後もロックを取得できなかった（送金は適用されていない）。"""


@dataclass(frozen=True)
class TransferResult:
    op_id: str
    src: str
    dst: str
    cents: int
    src_balance_after: int
    dst_balance_after: int
    seq: int                 # transfers テーブルの記録番号（再送でも同じ値）


def _check_id(name: str, value: object) -> str:
    if type(value) is not str or _ID_RE.fullmatch(value) is None:
        raise InvalidArgumentError(f"{name} must be 1..64 chars of ASCII [A-Za-z0-9_-]")
    return value


def _check_amount(name: str, value: object, *, positive: bool) -> int:
    if type(value) is not int:          # bool・float は拒否
        raise InvalidArgumentError(f"{name} must be int")
    if value < (1 if positive else 0) or value > MAX_BALANCE:
        raise InvalidArgumentError(f"{name} out of range")
    return value


def _is_busy(exc: sqlite3.OperationalError) -> bool:
    code = getattr(exc, "sqlite_errorcode", None)
    if code is not None:
        return code & 0xFF in (sqlite3.SQLITE_BUSY, sqlite3.SQLITE_LOCKED)
    msg = str(exc).lower()
    return "locked" in msg or "busy" in msg


class SqliteLedger:
    """1 インスタンス = 1 コネクション。sqlite3 の check_same_thread=True により、
    作成したスレッド以外からの使用は ProgrammingError になる（スレッド間で共有しない）。"""

    def __init__(self, path: str, *, busy_timeout_ms: int = 5000, max_retries: int = 8,
                 fault_hook: Callable[[str], None] | None = None) -> None:
        if not isinstance(path, str) or not path:
            raise InvalidArgumentError("path must be a non-empty str")
        self._max_retries = max_retries
        self._fault_hook = fault_hook
        # timeout 引数が sqlite3_busy_timeout を設定する（PRAGMA を文字列で組み立てない）
        self._conn: sqlite3.Connection | None = sqlite3.connect(
            path, timeout=busy_timeout_ms / 1000.0, isolation_level=None, check_same_thread=True)
        try:
            mode = self._retry(lambda: self._conn.execute("PRAGMA journal_mode=WAL").fetchone()[0])
            if str(mode).lower() != "wal":
                raise LedgerError(f"could not enable WAL (journal_mode={mode})")
            self._conn.execute("PRAGMA synchronous=FULL")
            self._conn.execute("PRAGMA foreign_keys=ON")
            self._retry(self._create_schema)
        except BaseException:
            self._conn.close()
            self._conn = None
            raise

    # ------------------------------------------------------------ 公開 API
    def create_account(self, account_id: str, cents: int) -> None:
        _check_id("account_id", account_id)
        _check_amount("cents", cents, positive=False)

        def op() -> None:
            conn = self._connection()
            conn.execute("BEGIN IMMEDIATE")
            try:
                if conn.execute("SELECT 1 FROM accounts WHERE id = ?", (account_id,)).fetchone():
                    raise AccountExistsError(account_id)
                conn.execute("INSERT INTO accounts (id, balance) VALUES (?, ?)", (account_id, cents))
                conn.execute("COMMIT")
            except BaseException:
                self._rollback()
                raise
        self._retry(op)

    def transfer(self, op_id: str, src: str, dst: str, cents: int) -> TransferResult:
        _check_id("op_id", op_id)
        _check_id("src", src)
        _check_id("dst", dst)
        _check_amount("cents", cents, positive=True)
        if src == dst:
            raise InvalidArgumentError("src and dst must differ")
        return self._retry(lambda: self._transfer_once(op_id, src, dst, cents))

    def balances(self) -> dict[str, int]:
        rows = self._retry(lambda: self._connection().execute(
            "SELECT id, balance FROM accounts ORDER BY id").fetchall())
        return {k: v for k, v in rows}

    def diagnostics(self) -> dict[str, object]:
        """診断用: このコネクションの journal_mode・synchronous・busy_timeout と integrity_check。"""
        conn = self._connection()
        return {
            "journal_mode": conn.execute("PRAGMA journal_mode").fetchone()[0],
            "synchronous": conn.execute("PRAGMA synchronous").fetchone()[0],   # 2 = FULL
            "busy_timeout_ms": conn.execute("PRAGMA busy_timeout").fetchone()[0],
            "integrity_check": conn.execute("PRAGMA integrity_check").fetchone()[0],
        }

    def close(self) -> None:
        if self._conn is not None:
            try:
                self._rollback()
            finally:
                self._conn.close()
                self._conn = None

    def __enter__(self) -> "SqliteLedger":
        return self

    def __exit__(self, *exc) -> None:
        self.close()

    # ------------------------------------------------------------ 内部
    def _connection(self) -> sqlite3.Connection:
        if self._conn is None:
            raise LedgerError("ledger is closed")
        return self._conn

    def _create_schema(self) -> None:
        conn = self._connection()
        conn.execute("BEGIN IMMEDIATE")
        try:
            for stmt in _SCHEMA:
                conn.execute(stmt)
            conn.execute("COMMIT")
        except BaseException:
            self._rollback()
            raise

    def _rollback(self) -> None:
        conn = self._conn
        if conn is not None and conn.in_transaction:
            conn.execute("ROLLBACK")

    def _retry(self, fn):
        last: sqlite3.OperationalError | None = None
        for _ in range(self._max_retries):
            try:
                return fn()
            except sqlite3.OperationalError as exc:
                if not _is_busy(exc):
                    raise
                last = exc
        raise LedgerBusyError(f"database busy after {self._max_retries} attempts") from last

    def _hook(self, point: str) -> None:
        if self._fault_hook is not None:
            self._fault_hook(point)

    def _transfer_once(self, op_id: str, src: str, dst: str, cents: int) -> TransferResult:
        conn = self._connection()
        conn.execute("BEGIN IMMEDIATE")        # 書込みロックを先に取得（読み取り後の昇格競合を避ける）
        try:
            row = conn.execute(
                "SELECT seq, src, dst, cents, src_after, dst_after FROM transfers WHERE op_id = ?",
                (op_id,)).fetchone()
            if row is not None:
                seq, r_src, r_dst, r_cents, s_after, d_after = row
                conn.execute("ROLLBACK")
                if (r_src, r_dst, r_cents) != (src, dst, cents):
                    raise ConflictingOperationError(op_id)
                return TransferResult(op_id, r_src, r_dst, r_cents, s_after, d_after, seq)
            src_row = conn.execute("SELECT balance FROM accounts WHERE id = ?", (src,)).fetchone()
            dst_row = conn.execute("SELECT balance FROM accounts WHERE id = ?", (dst,)).fetchone()
            if src_row is None or dst_row is None:
                raise UnknownAccountError(src if src_row is None else dst)
            src_bal, dst_bal = src_row[0], dst_row[0]
            if src_bal < cents:
                raise InsufficientFundsError(f"{src} has {src_bal}, needs {cents}")
            if dst_bal > MAX_BALANCE - cents:
                raise BalanceOverflowError(f"{dst} would exceed 2**63-1")
            conn.execute("UPDATE accounts SET balance = balance - ? WHERE id = ?", (cents, src))
            self._hook("after_debit")
            conn.execute("UPDATE accounts SET balance = balance + ? WHERE id = ?", (cents, dst))
            self._hook("after_credit")
            cur = conn.execute(
                "INSERT INTO transfers (op_id, src, dst, cents, src_after, dst_after)"
                " VALUES (?, ?, ?, ?, ?, ?)",
                (op_id, src, dst, cents, src_bal - cents, dst_bal + cents))
            seq = cur.lastrowid
            self._hook("before_commit")
            conn.execute("COMMIT")               # COMMIT 成功後にだけ結果を返す
        except BaseException:
            self._rollback()                     # 失敗は記録しない（同じ op_id で再試行できる）
            raise
        return TransferResult(op_id, src, dst, cents, src_bal - cents, dst_bal + cents, seq)
```

#### 3. 実行・ビルド方法と使用環境

```powershell
# 作業フォルダー（試験専用の一時領域）: scratchpad\py\q04 に q04.py（実装）と test_q04.py（テスト）を置く
cd <scratchpad>\py\q04
python3.12 --version            # Python 3.12.15
python3.12 -X dev -m unittest -v test_q04
```

**使用環境（全問共通）**

- Python：**CPython 3.12.15**（`3.12.15 (main, Oct  9 2026) [MSC v.1944 64 bit (AMD64)]`）。この PC に既にあった `python3.12` コマンドで実行しました。新しいバージョンへの変更・pip によるパッケージ導入はしていません。
- ライブラリ：Python 標準ライブラリだけです。テストは `unittest` で行いました。
- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド、x86-64）。
- 実行方法：問題ごとの作業フォルダー（`scratchpad\py\qNN`。試験専用の一時領域）に、実装 `qNN.py` とテスト `test_qNN.py` を置いて実行しました。`-X dev`（開発モード：ResourceWarning や asyncio の検査を有効化）を付けています。
- 未実施の環境：Linux・macOS・ARM での実行はしていません。

- **SQLite：** `sqlite3.sqlite_version` = 3.53.1（Python 3.12.15 に同梱）。
- **テスト DB：** `scratchpad\py\q04\testdata\q04-*`（テストごとの一時フォルダー）にだけ作り、各テストの終わりに削除しました（終了後に残ったファイルは 0 件）。
- **最終の実行結果：** `Ran 7 tests in 286.012s` → `OK`、終了コード 0。そのうち 4 プロセスでの 10 万送金が 284.7 秒です（synchronous=FULL で、COMMIT ごとに fsync するため。事前に測った COMMIT 1 回の時間は約 1.25 ms）。

#### 4. 実際に使用したテストコード

```python
"""Q04 のテスト。DB はすべてテスト専用の一時フォルダー（q04 の隣の testdata 配下）に作成し、終了時に削除する。"""
from __future__ import annotations

import concurrent.futures
import multiprocessing
import os
import random
import shutil
import sqlite3
import tempfile
import threading
import time
import unittest

from q04 import (MAX_BALANCE, AccountExistsError, BalanceOverflowError, ConflictingOperationError,
                 InsufficientFundsError, InvalidArgumentError, LedgerBusyError, SqliteLedger,
                 TransferResult, UnknownAccountError)

HERE = os.path.dirname(os.path.abspath(__file__))
BASE = os.path.join(HERE, "testdata")


def raw_rows(path):
    """台帳とは別のコネクションで DB の生の内容を読む。"""
    c = sqlite3.connect(path)
    try:
        acc = dict(c.execute("SELECT id, balance FROM accounts").fetchall())
        tr = c.execute("SELECT op_id, src, dst, cents FROM transfers ORDER BY seq").fetchall()
        return acc, tr
    finally:
        c.close()


class TmpDb(unittest.TestCase):
    def setUp(self):
        os.makedirs(BASE, exist_ok=True)
        self.dir = tempfile.mkdtemp(prefix="q04-", dir=BASE)
        self.path = os.path.join(self.dir, "ledger.db")

    def tearDown(self):
        shutil.rmtree(self.dir)


class Validation(TmpDb):
    def test_balances_and_bounds(self):
        with SqliteLedger(self.path) as L:
            L.create_account("zero", 0)
            L.create_account("one", 1)
            L.create_account("max", MAX_BALANCE)
            L.create_account("mid", 100)
            self.assertEqual(L.balances(), {"zero": 0, "one": 1, "max": MAX_BALANCE, "mid": 100})
            with self.assertRaises(InsufficientFundsError):
                L.transfer("t1", "zero", "mid", 1)
            r = L.transfer("t2", "one", "zero", 1)
            self.assertEqual((r.src_balance_after, r.dst_balance_after), (0, 1))
            with self.assertRaises(BalanceOverflowError):
                L.transfer("t3", "mid", "max", 1)
            r = L.transfer("t4", "max", "mid", MAX_BALANCE - 100)
            self.assertEqual(r.dst_balance_after, MAX_BALANCE)
            with self.assertRaises(BalanceOverflowError):
                L.transfer("t5", "mid", "max", MAX_BALANCE)        # 100 + MAX は上限超過
            r = L.transfer("t5", "mid", "max", MAX_BALANCE - 100)  # 送金先がちょうど上限
            self.assertEqual((r.src_balance_after, r.dst_balance_after), (100, MAX_BALANCE))
            with self.assertRaises(UnknownAccountError):
                L.transfer("t6", "nobody", "mid", 1)
            with self.assertRaises(UnknownAccountError):
                L.transfer("t7", "max", "nobody", 1)
            with self.assertRaises(AccountExistsError):
                L.create_account("one", 5)
            for bad in (0, -1, MAX_BALANCE + 1, True, 1.0, "1", None):
                with self.assertRaises(InvalidArgumentError, msg=repr(bad)):
                    L.transfer("t8", "max", "zero", bad)
            with self.assertRaises(InvalidArgumentError):
                L.transfer("t9", "max", "max", 1)                 # 同一口座
            for bad in (-1, MAX_BALANCE + 1, False, 2.5):
                with self.assertRaises(InvalidArgumentError):
                    L.create_account("x", bad)
            self.assertEqual(sum(L.balances().values()), 0 + 1 + MAX_BALANCE + 100)

    def test_identifier_validation_and_sql_injection(self):
        with SqliteLedger(self.path) as L:
            L.create_account("a", 10)
            L.create_account("A-b_9", 10)
            L.create_account("x" * 64, 0)
            bad_ids = ["", "x" * 65, "a b", "a'b", "a;b", "a\nb", "a\n", "ä", "a.b", "a/b",
                       "a'; DROP TABLE accounts;--", "1 OR 1=1", "\x00", 5, None, b"a"]
            for bad in bad_ids:
                with self.assertRaises(InvalidArgumentError, msg=repr(bad)):
                    L.create_account(bad, 1)
                with self.assertRaises(InvalidArgumentError, msg=repr(bad)):
                    L.transfer(bad, "a", "A-b_9", 1)
                with self.assertRaises(InvalidArgumentError, msg=repr(bad)):
                    L.transfer("op", bad, "a", 1)
            self.assertEqual(L.balances(), {"a": 10, "A-b_9": 10, "x" * 64: 0})
            self.assertEqual(raw_rows(self.path)[1], [])

    def test_connection_not_shared_between_threads(self):
        L = SqliteLedger(self.path)
        L.create_account("a", 1)
        errors = []

        def other():
            try:
                L.balances()
            except sqlite3.ProgrammingError as e:
                errors.append(e)
        t = threading.Thread(target=other)
        t.start()
        t.join()
        self.assertEqual(len(errors), 1)
        L.close()
        L.close()                               # 二重 close は安全


class Idempotency(TmpDb):
    def test_replay_100_times_conflict_and_retry_after_failure(self):
        with SqliteLedger(self.path) as L:
            L.create_account("a", 1000)
            L.create_account("b", 0)
            first = L.transfer("op-1", "a", "b", 250)
            for _ in range(100):
                self.assertEqual(L.transfer("op-1", "a", "b", 250), first)
            self.assertEqual(L.balances(), {"a": 750, "b": 250})
            for params in (("a", "b", 251), ("b", "a", 250)):
                with self.assertRaises(ConflictingOperationError):
                    L.transfer("op-1", *params)
            self.assertEqual(L.balances(), {"a": 750, "b": 250})
            # 失敗（残高不足）は記録されないので、同じ op_id で後から正しく再試行できる
            with self.assertRaises(InsufficientFundsError):
                L.transfer("op-2", "b", "a", 500)
            with self.assertRaises(InsufficientFundsError):
                L.transfer("op-2", "b", "a", 500)
            L.transfer("op-x", "a", "b", 250)
            r = L.transfer("op-2", "b", "a", 500)
            self.assertEqual((r.src_balance_after, r.dst_balance_after), (0, 1000))
            # 失敗後に別パラメーターで同じ op_id を使うことも可能（成功記録がないため）
            with self.assertRaises(UnknownAccountError):
                L.transfer("op-3", "a", "zz", 1)
            self.assertEqual(L.transfer("op-3", "a", "b", 1).cents, 1)
        acc, tr = raw_rows(self.path)
        self.assertEqual([t[0] for t in tr], ["op-1", "op-x", "op-2", "op-3"])
        self.assertEqual(acc, {"a": 999, "b": 1})


class FaultInjection(TmpDb):
    def test_each_fault_point_rolls_back(self):
        for point in ("after_debit", "after_credit", "before_commit"):
            with self.subTest(point=point):
                path = os.path.join(self.dir, f"{point}.db")
                with SqliteLedger(path) as L:
                    L.create_account("a", 100)
                    L.create_account("b", 5)
                    L.transfer("ok-1", "a", "b", 10)

                def hook(p, point=point):
                    if p == point:
                        raise RuntimeError(f"injected at {p}")
                L = SqliteLedger(path, fault_hook=hook)
                with self.assertRaisesRegex(RuntimeError, "injected"):
                    L.transfer("op-f", "a", "b", 30)
                self.assertEqual(L.balances(), {"a": 90, "b": 15})   # 同じコネクションでも旧版
                L.close()
                acc, tr = raw_rows(path)                              # 再オープンして確認
                self.assertEqual(acc, {"a": 90, "b": 15})
                self.assertEqual([t[0] for t in tr], ["ok-1"])
                with SqliteLedger(path) as L2:
                    self.assertEqual(L2.balances(), {"a": 90, "b": 15})
                    r = L2.transfer("op-f", "a", "b", 30)             # 同じ op_id で再試行できる
                    self.assertEqual((r.src_balance_after, r.dst_balance_after), (60, 45))
                    self.assertEqual(L2.diagnostics()["integrity_check"], "ok")

    def test_bounded_busy_retry(self):
        with SqliteLedger(self.path) as L:
            L.create_account("a", 10)
            L.create_account("b", 0)
        L = SqliteLedger(self.path, busy_timeout_ms=50, max_retries=3)
        blocker = sqlite3.connect(self.path, isolation_level=None)
        blocker.execute("BEGIN IMMEDIATE")                  # 書込みロックを保持し続ける
        try:
            t0 = time.perf_counter()
            with self.assertRaises(LedgerBusyError):
                L.transfer("op-b", "a", "b", 1)
            elapsed = time.perf_counter() - t0
            self.assertLess(elapsed, 5.0)                   # 無限に待たない
            L.close()
        finally:
            blocker.execute("ROLLBACK")
            blocker.close()
        acc, tr = raw_rows(self.path)
        self.assertEqual(acc, {"a": 10, "b": 0})
        self.assertEqual(tr, [])


# ------------------------------------------------------------ spawn 4 プロセス
NUM_ACCOUNTS = 16
INITIAL = 1_000_000
PER_PROCESS = 25_000


def shared_params(k):
    rng = random.Random(10_000 + k)
    s = rng.randrange(NUM_ACCOUNTS)
    d = (s + 1 + rng.randrange(NUM_ACCOUNTS - 1)) % NUM_ACCOUNTS
    return f"acct{s}", f"acct{d}", rng.randint(1, 5000)


def worker(path, wid, seed):
    """子プロセス: 独自の SqliteLedger（独自コネクション）で 25,000 回 transfer を呼ぶ。"""
    rng = random.Random(seed)
    stats = {"ok": 0, "insufficient": 0, "conflict": 0, "busy": 0, "overflow": 0}
    results = {}
    L = SqliteLedger(path, busy_timeout_ms=10_000, max_retries=20)
    try:
        for i in range(PER_PROCESS):
            r = rng.random()
            if r < 0.04:                                     # 全プロセス共通の op_id（同じ引数）
                k = rng.randrange(400)
                op = f"shared-{k}"
                src, dst, cents = shared_params(k)
            elif r < 0.05:                                   # 共通の op_id だが引数がプロセスごとに異なる
                k = rng.randrange(50)
                op = f"conf-{k}"
                src, dst, cents = "acct0", "acct1", wid + 1
            else:
                op = f"w{wid}-{i}"
                s = rng.randrange(NUM_ACCOUNTS)
                d = (s + 1 + rng.randrange(NUM_ACCOUNTS - 1)) % NUM_ACCOUNTS
                src, dst, cents = f"acct{s}", f"acct{d}", rng.choice([1, 10, 100, 1000, 50_000, 3_000_000])
            try:
                res = L.transfer(op, src, dst, cents)
                stats["ok"] += 1          # 新規適用または既存結果の返却
                results[op] = res.seq
            except InsufficientFundsError:
                stats["insufficient"] += 1
            except ConflictingOperationError:
                stats["conflict"] += 1
            except LedgerBusyError:
                stats["busy"] += 1
            except BalanceOverflowError:
                stats["overflow"] += 1
        diag = L.diagnostics()
    finally:
        L.close()
    stats["calls"] = PER_PROCESS
    return stats, results, diag


class MultiProcess(TmpDb):
    def test_four_spawn_processes_100k_transfers(self):
        with SqliteLedger(self.path) as L:
            for a in range(NUM_ACCOUNTS):
                L.create_account(f"acct{a}", INITIAL)
        ctx = multiprocessing.get_context("spawn")
        t0 = time.perf_counter()
        with concurrent.futures.ProcessPoolExecutor(max_workers=4, mp_context=ctx) as pool:
            futs = [pool.submit(worker, self.path, w, 777 + w) for w in range(4)]
            outs = [f.result(timeout=1800) for f in futs]
        elapsed = time.perf_counter() - t0
        total_calls = sum(o[0]["calls"] for o in outs)
        self.assertEqual(total_calls, 100_000)
        with SqliteLedger(self.path) as L:
            bal = L.balances()
            diag = L.diagnostics()
        acc, tr = raw_rows(self.path)
        # 総額保存・負残高なし
        self.assertEqual(sum(bal.values()), NUM_ACCOUNTS * INITIAL)
        self.assertTrue(all(v >= 0 for v in bal.values()))
        # 成功 op_id はちょうど 1 記録（UNIQUE 制約 + 件数照合）
        ids = [t[0] for t in tr]
        self.assertEqual(len(ids), len(set(ids)))
        # 記録から残高を再計算して一致（記録と残高の原子性）
        recomputed = {f"acct{a}": INITIAL for a in range(NUM_ACCOUNTS)}
        for op, src, dst, cents in tr:
            recomputed[src] -= cents
            recomputed[dst] += cents
        self.assertEqual(recomputed, bal)
        # 各プロセスが受け取った seq は DB 上の記録と一致（共有 op_id は全プロセスで同じ seq）
        seq_of = {}
        c = sqlite3.connect(self.path)
        for op, seq in c.execute("SELECT op_id, seq FROM transfers"):
            seq_of[op] = seq
        c.close()
        for _, results, _ in outs:
            for op, seq in results.items():
                self.assertEqual(seq_of[op], seq, op)
        # 成功を返した op_id の集合 = DB の成功記録の集合（成功を返したのに記録なし・記録の重複がない）
        union = set().union(*(set(r) for _, r, _ in outs))
        self.assertEqual(union, set(ids))
        shared_calls_ok = sum(1 for _, r, _ in outs for op in r if op.startswith("shared-"))
        # conf-k は 1 回だけ記録され、その引数は 1 つのプロセスのもの
        conf = [t for t in tr if t[0].startswith("conf-")]
        self.assertTrue(all(t[1:3] == ("acct0", "acct1") and 1 <= t[3] <= 4 for t in conf))
        new_total = len(tr)
        agg = {k: sum(o[0][k] for o in outs) for k in outs[0][0]}
        self.assertEqual(agg["busy"], 0)
        for _, _, d in outs:
            self.assertEqual((str(d["journal_mode"]).lower(), d["synchronous"]), ("wal", 2))
        self.assertEqual(diag["integrity_check"], "ok")
        print(f"\n[Q04] 4 spawn processes, {total_calls} transfer calls in {elapsed:.1f}s; "
              f"committed transfers {new_total}; per-call outcomes {agg}; shared op_ids succeeded in >=1 process: "
              f"{len([i for i in ids if i.startswith('shared-')])} (per-process successes {shared_calls_ok}); "
              f"journal_mode={diag['journal_mode']} synchronous={diag['synchronous']} "
              f"busy_timeout={outs[0][2]['busy_timeout_ms']}ms integrity_check={diag['integrity_check']}")


if __name__ == "__main__":
    unittest.main(verbosity=2)
```

#### 5. 検証結果（期待値・実測値・実施状況）

| 必須テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 残高 0 / 1 / MAX | 0 からの送金は InsufficientFundsError。1 → 0 は成功（後残高 0 と 1）。MAX の口座への送金は BalanceOverflowError。MAX-100 を送って、送金先がちょうど 2**63-1 になるのは成功 | 一致 | 実施 |
| 存在しない口座 | src・dst のどちらでも UnknownAccountError | 一致 | 実施 |
| 同一口座・負額・0・上限超過 | src==dst、cents が 0・-1・2**63・True・1.0・"1"・None は InvalidArgumentError（ValueError の派生）。create_account の負額・上限超過・bool・float も同様 | 一致 | 実施 |
| 文字列の検証・SQL インジェクション | 空文字・65 文字・空白・`'`・`;`・改行・末尾改行・非 ASCII・`.`・`/`・`a'; DROP TABLE accounts;--`・`1 OR 1=1`・NUL・非 str の 16 種を、口座 ID・op_id・src のどれでも拒否。残高と記録は不変 | 一致（transfers は 0 行のまま） | 実施 |
| 成功した op_id の 100 回再送 | 100 回とも初回と同じ TransferResult（seq も同じ）。残高は 1 回分しか変わらない | 一致（a=750, b=250） | 実施 |
| 同じ op_id・異なる引数 | (a,b,251)・(b,a,250) は ConflictingOperationError。残高は不変 | 一致 | 実施 |
| 失敗後の同じ op_id での再試行 | 残高不足で 2 回失敗した op-2 が、入金後に成功する。未知の口座で失敗した op-3 も、別の引数で成功する | 一致 | 実施 |
| 障害フック after_debit / after_credit / before_commit | 例外の後、同じコネクションでも別のコネクション（再オープン）でも、残高と成功記録の両方が旧版。その後、同じ op_id の再試行が成功し、integrity_check は ok | 3 か所とも一致 | 実施 |
| ロック取得の有界な再試行 | 別のコネクションが書込みロックを保持し続けると、busy_timeout 50 ms × 3 回で LedgerBusyError（5 秒未満）。DB は不変 | 一致 | 実施 |
| スレッド間の共有禁止 | 別のスレッドからの使用は sqlite3.ProgrammingError。二重 close は安全 | 一致 | 実施 |
| spawn の 4 プロセスから合計 10 万送金 | 16 口座 × 初期 1,000,000。各プロセスが独自の SqliteLedger で 25,000 回ずつ呼ぶ（約 4% は 4 プロセス共通の op_id・同じ引数、約 1% は共通の op_id・プロセスごとに異なる引数） | 呼出し 100,000：成功の返却 83,311、残高不足 16,003、引数の衝突 686、busy 0、上限超過 0 | 実施 |
| 総額の保存・負残高なし | 合計 = 16,000,000、全残高 ≥ 0 | 一致 | 実施 |
| 成功 op_id の記録がちょうど 1 件 | 成功記録 79,468 件で op_id の重複なし。「成功を返した op_id の集合 = DB の記録の集合」。各プロセスが受け取った seq が DB と一致。共有 op_id 400 種は全プロセスで同じ seq（成功の返却は延べ 1,480 回） | 一致 | 実施 |
| 記録と残高の整合 | 初期残高 + 全記録の入出金を再計算した結果 = 最終残高 | 一致 | 実施 |
| WAL・synchronous・integrity_check | 各子プロセス：journal_mode=wal、synchronous=2（FULL）、busy_timeout=10000 ms。最後の integrity_check は ok | 一致 | 実施 |

**自律修正（テスト側）**
- 残高の境界テストで、t4 の送金の後の残高を誤って計算していました（max=100、mid=MAX の状態で MAX を送ると、上限超過になるのが正しい挙動です）。そこで、「MAX は上限超過で拒否、MAX-100 で送金先がちょうど上限」を確認する形に直しました。
- ロック待ちの試験は、ロックを保持した後に台帳を開くと、スキーマ作成が待たされてしまいます。実行前の見直しで、台帳を開いてからロックを取る順に直しました。
- `PRAGMA busy_timeout` を文字列で組み立てるのをやめ、`sqlite3.connect(timeout=...)` で設定する形に変えました（「SQL を文字列連結で組み立てない」を厳守するため）。これは実装側の変更で、実行前に行いました。

#### 6. 技術的な正当性・設計判断

- **exactly-once：**
  - op_id の照会・残高の検査・更新・記録の挿入は、すべて同じ `BEGIN IMMEDIATE` トランザクションの中で行います。書込みロックは DB で 1 つなので、op_id の照会から COMMIT まで、他のプロセスが同じ op_id を挟み込むことはできません。
  - 加えて、`op_id UNIQUE` 制約が最後の防御になります。
  - 成功結果は COMMIT が成功した後にだけ返します。COMMIT の前の失敗は ROLLBACK するので、残高と記録はともに旧版です。
- **原子性：** 引き落とし・入金・記録の 3 つは、1 トランザクションです。例外注入（after_debit・after_credit・before_commit）の後に再オープンしても旧版であることを確認しました。WAL + synchronous=FULL により、コミット済みの内容は WAL の fsync の後に確定します。
- **競合：**
  - `BEGIN IMMEDIATE` でロックを先に取るので、読み取りから書込みへの昇格での競合（SQLITE_BUSY のデッドロック）を避けます。
  - 待機は `sqlite3.connect(timeout=)` で設定した busy_timeout によって SQLite 内で行います。それでも取れなければ、有界回数（既定 8 回）だけ再試行し、`LedgerBusyError` を返します（無限ループにしない）。
- **値の範囲：**
  - 残高は SQLite の INTEGER（64 ビット符号付き）で、`CHECK (balance >= 0)` を付けています。
  - 上限の検査は Python の任意精度で `dst_bal > MAX - cents` と事前に行うので、SQL でのオーバーフローは起きません。
- **コネクションの所有：** 1 インスタンス = 1 コネクションで、`check_same_thread=True`（既定値）により、スレッド間の共有を拒否します。テストでは、各プロセスが自分の SqliteLedger を開きました。

#### 7. 未実施項目・制約・残課題

- 実際の電源断・OS クラッシュの試験は未実施です（障害は例外の注入で模擬しました）。
- ネットワークファイルシステム上の DB は、SQLite の WAL が非対応のため対象外です。
- Linux・macOS での実行はしていません。
- 失敗した送金は記録しない仕様なので、失敗を返した後に同じ op_id を異なる引数で送ると、新しい送金として受理されます（仕様どおり）。

---

## Q05【地獄級】multiprocessing.shared_memoryによるプロセス間SPSCリング

**試験の狙い：** プロセス境界を越えるメモリ所有権、同期プリミティブ、終了処理・リーク回避を検証。

### 元コード（危険・不完全）

```python
from multiprocessing import shared_memory
def push_bad(shm, index, value):
    shm.buf[index*8:(index+1)*8] = value.to_bytes(8, 'little')
    # 同期なし、満杯・空の検出なし、プロセス終了時の後始末なし
```

### 要求仕様

1. `SharedSpscRing(capacity: int, *, context)` を実装。capacityは2の冪で2〜4096。`multiprocessing.get_context('spawn')`に対応。共有領域はcapacity×8バイトの`SharedMemory`、各要素は**符号なし64ビット整数（0..2^64-1）**。
2. APIは送信側`put(value: int, timeout: float | None = None) -> bool`、受信側`get(timeout: float | None = None) -> tuple[bool,int | None]`、`close()`、所有プロセス側`unlink()`。putは満杯時に空きを待ち、タイムアウトならfalse。getは空のとき待ち、タイムアウトなら(false,None)。不正valueはValueError/TypeError。
3. ちょうど**1送信プロセスと1受信プロセス**の契約。セマフォ等の標準同期手段により、put完了前に未公開データをgetしない。非同期タスク用のasyncio.Lockではなくプロセス間で利用できる同期機構を使用。busy-spinは禁止。
4. 各参加プロセスはSharedMemoryを名前でattachし、親が作った同期プリミティブをspawn時の引数として安全に受け取る。子の`close`はdetachのみ、`unlink`は所有親が**全子プロセスの終了後ちょうど1回**実施。
5. 単調増加の送信位置/受信位置は64ビット以上の共有カウンターで維持。実行時の合計操作数は2^62未満と仮定。FIFO、重複なし、欠損なしを保証。値のエンコードは`struct.pack_into/unpack_from('<Q',...)`。
6. 送受信で例外が出てもセマフォの個数やカウンターが破壊されないようにする。異常なプロセスクラッシュ後の継続運用・自動復旧は要求せず、**親がtimeout後に残存子プロセスを停止・回収する**テストを実施する。
7. POSIXとWindowsのspawnで利用できる標準APIのみ。`multiprocessing.Queue/Pipe/Manager`でキュー自体を代替実装することは禁止。

### 必須検証

1. 容量2/4/1024/4096、空/満杯タイムアウト、0/UINT64_MAX/範囲外、循環再利用、参照結果が送信順と完全一致。
2. spawnした親子2プロセスで100万件を送受信、欠損0、重複0、順序違反0、デッドロックなし。
3. 送信者だけ先に開始/受信者だけ先に開始、極端な速度差、遅延注入による未公開データの読取なし。
4. プロセス終了後にshared memoryをunlinkし再attach不可、close二重呼び出し・クラッシュ時の親による有界待機と後片付け。

### ★★FREYJAの回答【Q05・記入欄】

> **以下が専用回答スペース。** 問題本文、元コード、要求仕様、必須検証は改変しない。各項目の例示テキストを実際の内容へ置き換えること。

#### 1. 根本原因・危険性・影響

**元コード `push_bad` の問題点**

| # | 問題箇所 | 発生条件 | 危険性・実害 |
| --- | --- | --- | --- |
| 1 | 同期がない | 送信と受信が並行 | 書きかけの値の読み取り、メモリ順序の保証がない |
| 2 | 満杯・空を検出しない | 送信が受信より速い、または遅い | 未読の要素の上書き（欠損）、空の時に古い値を読む（重複） |
| 3 | 呼出し元が index を指定する | 位置の管理を誤る | 範囲外への書込み（ValueError）、FIFO が崩れる |
| 4 | 値を検証しない（`to_bytes(8)` 任せ） | 負数・2**64 以上 | OverflowError、型の不正 |
| 5 | 後始末がない | プロセスの終了・クラッシュ | 共有メモリのリーク（POSIX では /dev/shm に残る）、誰が unlink するかが不明 |

**修正方針**
- capacity × 8 バイトの `SharedMemory` を、u64 のリングとして使います。
- 送信位置と受信位置は、64 ビットの共有カウンター `RawValue(c_uint64)` に置きます。
- 同期は、プロセス間セマフォ 2 つ（空き枠 `free`、公開済みの要素 `items`）で行います。待機はセマフォのブロッキング `acquire(timeout)` だけで行い、ビジースピンはしません。
- 子プロセスは、pickle を復元する時に名前で attach します。`unlink` は所有者だけがちょうど 1 回行います。

#### 2. 完全な修正版Pythonコード

```python
"""Q05: multiprocessing.shared_memory によるプロセス間 SPSC リング（spawn 対応・標準ライブラリのみ）。

同期の設計:
  * free  = Semaphore(capacity) … 空きスロット数。put が acquire、get が release。
  * items = Semaphore(0)        … 公開済み要素数。put が release、get が acquire。
  put は「データを書く → 送信位置を進める → items.release()」の順で公開し、get は items.acquire()
  に成功した後でだけスロットを読む。OS のセマフォ操作はプロセス間のメモリ同期点となるため、
  put 完了前（release 前）のデータを get が読むことはない。待機はすべてセマフォのブロッキング
  acquire(timeout) で行い、ビジースピンはしない。
"""
from __future__ import annotations

import ctypes
import math
import os
import struct
from multiprocessing import shared_memory

__all__ = ["SharedSpscRing", "UINT64_MAX"]

UINT64_MAX = 2**64 - 1
_FMT = struct.Struct("<Q")


def _check_timeout(timeout: float | None) -> float | None:
    if timeout is None:
        return None
    if isinstance(timeout, bool) or not isinstance(timeout, (int, float)):
        raise TypeError("timeout must be a number or None")
    if math.isnan(timeout) or timeout < 0:
        raise ValueError("timeout must be >= 0")
    return float(timeout)


class SharedSpscRing:
    """1 送信プロセス・1 受信プロセス専用の有界リング（要素は u64）。

    親（所有者）が生成し、spawn の Process 引数として子へ渡す。子では pickle 復元時に
    SharedMemory へ名前で attach する。子の close() は detach のみ。unlink() は所有者だけが、
    全子プロセスの終了後にちょうど 1 回実行する（2 回目以降は何もしない）。
    """

    def __init__(self, capacity: int, *, context) -> None:
        if type(capacity) is not int:
            raise TypeError("capacity must be int")
        if capacity < 2 or capacity > 4096 or capacity & (capacity - 1):
            raise ValueError("capacity must be a power of two within 2..4096")
        self._capacity = capacity
        self._mask = capacity - 1
        self._shm = shared_memory.SharedMemory(create=True, size=capacity * 8)
        self._name = self._shm.name
        try:
            # 64 ビットの単調増加カウンター（送信位置・受信位置）。各同期はセマフォで行うのでロックなし。
            self._sent = context.RawValue(ctypes.c_uint64, 0)
            self._recv = context.RawValue(ctypes.c_uint64, 0)
            self._free = context.Semaphore(capacity)
            self._items = context.Semaphore(0)
        except BaseException:
            self._shm.close()
            self._shm.unlink()
            raise
        self._owner_pid = os.getpid()
        self._is_owner = True
        self._closed = False
        self._unlinked = False
        self._hook = None          # テスト用の遅延注入点（pickle しない）

    # ---------------------------------------------------------------- pickle（spawn 引数）
    def __getstate__(self):
        if self._closed:
            raise RuntimeError("cannot share a closed ring")
        return {"capacity": self._capacity, "name": self._name, "sent": self._sent,
                "recv": self._recv, "free": self._free, "items": self._items,
                "owner_pid": self._owner_pid}

    def __setstate__(self, st):
        self._capacity = st["capacity"]
        self._mask = self._capacity - 1
        self._name = st["name"]
        self._sent = st["sent"]
        self._recv = st["recv"]
        self._free = st["free"]
        self._items = st["items"]
        self._owner_pid = st["owner_pid"]
        self._is_owner = False
        self._closed = False
        self._unlinked = False
        self._hook = None
        self._shm = shared_memory.SharedMemory(name=self._name, create=False)   # 名前で attach
        if os.name == "posix":
            # Python 3.12 の POSIX では attach 側も resource_tracker に登録され、子の終了時に
            # 所有者より先に unlink されうる。所有者だけが unlink するよう登録を外す。
            try:
                from multiprocessing import resource_tracker
                resource_tracker.unregister(self._shm._name, "shared_memory")  # type: ignore[attr-defined]
            except Exception:
                pass

    # ---------------------------------------------------------------- 公開 API
    @property
    def capacity(self) -> int:
        return self._capacity

    @property
    def name(self) -> str:
        return self._name

    @property
    def sent_count(self) -> int:
        return self._sent.value

    @property
    def received_count(self) -> int:
        return self._recv.value

    def put(self, value: int, timeout: float | None = None) -> bool:
        if type(value) is not int:
            raise TypeError("value must be int")
        if value < 0 or value > UINT64_MAX:
            raise ValueError("value must be within 0..2**64-1")
        t = _check_timeout(timeout)
        self._ensure_open()
        if not self._free.acquire(True, t):
            return False
        try:
            pos = self._sent.value
            _FMT.pack_into(self._shm.buf, (pos & self._mask) * 8, value)
            if self._hook is not None:
                self._hook("before_publish")
            self._sent.value = pos + 1
        except BaseException:
            self._free.release()          # 未公開のまま失敗: 空き枠を返し、カウンターは不変
            raise
        self._items.release()             # 公開（この時点から get が読める）
        return True

    def get(self, timeout: float | None = None) -> tuple[bool, int | None]:
        t = _check_timeout(timeout)
        self._ensure_open()
        if not self._items.acquire(True, t):
            return (False, None)
        try:
            pos = self._recv.value
            (value,) = _FMT.unpack_from(self._shm.buf, (pos & self._mask) * 8)
            if self._hook is not None:
                self._hook("before_release")
            self._recv.value = pos + 1
        except BaseException:
            self._items.release()         # 読み取りを取り消し、要素は残す
            raise
        self._free.release()
        return (True, value)

    def close(self) -> None:
        """この プロセスの attach を外す（detach のみ）。2 回目以降は何もしない。"""
        if not self._closed:
            self._closed = True
            self._shm.close()

    def unlink(self) -> None:
        """所有者（生成したプロセス）だけが呼べる。全子プロセスの終了後に呼ぶ契約。"""
        if not self._is_owner or os.getpid() != self._owner_pid:
            raise RuntimeError("only the owning process may unlink")
        if self._unlinked:
            return
        self._unlinked = True
        self.close()
        self._shm.unlink()      # POSIX: 名前を削除。Windows: 全ハンドルが閉じた時点で OS が破棄する

    def __enter__(self) -> "SharedSpscRing":
        return self

    def __exit__(self, *exc) -> None:
        self.close()

    def _ensure_open(self) -> None:
        if self._closed:
            raise ValueError("ring is closed in this process")
```

#### 3. 実行・ビルド方法と使用環境

```powershell
# 作業フォルダー（試験専用の一時領域）: scratchpad\py\q05 に q05.py（実装）と test_q05.py（テスト）を置く
cd <scratchpad>\py\q05
python3.12 --version            # Python 3.12.15
python3.12 -X dev -m unittest -v test_q05
# テストクラスを修正した後は、そのクラスだけを再実行した
python3.12 -X dev -m unittest -v test_q05.SingleProcess
```

**使用環境（全問共通）**

- Python：**CPython 3.12.15**（`3.12.15 (main, Oct  9 2026) [MSC v.1944 64 bit (AMD64)]`）。この PC に既にあった `python3.12` コマンドで実行しました。新しいバージョンへの変更・pip によるパッケージ導入はしていません。
- ライブラリ：Python 標準ライブラリだけです。テストは `unittest` で行いました。
- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド、x86-64）。
- 実行方法：問題ごとの作業フォルダー（`scratchpad\py\qNN`。試験専用の一時領域）に、実装 `qNN.py` とテスト `test_qNN.py` を置いて実行しました。`-X dev`（開発モード：ResourceWarning や asyncio の検査を有効化）を付けています。
- 未実施の環境：Linux・macOS・ARM での実行はしていません。

- すべての子プロセスは `multiprocessing.get_context('spawn')` で起動しました（Windows の既定でもあります）。
- **1 回目の全体実行：** `Ran 9 tests in 18.552s`。8 件合格、1 件失敗。失敗はテストの設計ミスで、下記で説明します。
- **修正後の再実行（SingleProcess クラス）：** `Ran 3 tests in 1.360s` → `OK`、終了コード 0。
- 他の 6 件は修正していないテストで、1 回目の実行で合格しています。成功済みの無関係なテストの再実行は禁止されているため、再実行していません。

#### 4. 実際に使用したテストコード

```python
"""Q05 のテスト。子プロセスはすべて spawn。子の処理関数はモジュール直下に置く（spawn で import するため）。"""
from __future__ import annotations

import multiprocessing
import time
import unittest
from multiprocessing import shared_memory

from q05 import UINT64_MAX, SharedSpscRing

CTX = multiprocessing.get_context("spawn")
N_MILLION = 1_000_000
GOLD = 0x9E3779B97F4A7C15


def value_of(i: int) -> int:
    """0..2**64-1 全域に散らばる値（0 と UINT64_MAX も含む）。"""
    if i == 0:
        return 0
    if i == 1:
        return UINT64_MAX
    return (i * GOLD) & UINT64_MAX


# ---------------------------------------------------------------- 子プロセス用関数
def child_producer(ring, n, slow_every, slow_sec):
    try:
        for i in range(n):
            if not ring.put(value_of(i), timeout=60):
                raise SystemExit(2)
            if slow_every and i % slow_every == 0:
                time.sleep(slow_sec)
    finally:
        ring.close()
        ring.close()          # 二重 close も安全


def child_consumer(ring, n, slow_every, slow_sec, result):
    bad = 0
    try:
        for i in range(n):
            ok, v = ring.get(timeout=60)
            if not ok or v != value_of(i):
                bad += 1
            if slow_every and i % slow_every == 0:
                time.sleep(slow_sec)
        extra_ok, _ = ring.get(timeout=0.2)     # 余分な要素（重複）がないこと
        result.value = bad + (1 if extra_ok else 0)
    finally:
        ring.close()


def child_publish_gate(ring, written, go):
    """1 要素を書いた後、公開（items.release）の直前で親の合図を待つ。"""
    def hook(point):
        if point == "before_publish":
            written.set()
            go.wait(30)
    ring._hook = hook
    ring.put(123456789)
    ring._hook = None
    ring.put(987654321)
    ring.close()


def child_block_forever(ring):
    ring.get(timeout=None)    # 送信者がいないので永久に待つ（親が停止させる）


def child_crash_mid(ring):
    ring.put(1)
    ring.put(2)
    import os
    os._exit(3)               # close せずに異常終了


def child_try_unlink(ring, result):
    try:
        ring.unlink()
        result.value = 1
    except RuntimeError:
        result.value = 2
    finally:
        ring.close()


# ---------------------------------------------------------------- テスト
class SingleProcess(unittest.TestCase):
    def test_capacities_cycle_order_and_timeouts(self):
        for cap in (2, 4, 1024, 4096):
            ring = SharedSpscRing(cap, context=CTX)
            try:
                self.assertEqual(ring._shm.size >= cap * 8, True)
                t0 = time.perf_counter()
                self.assertEqual(ring.get(timeout=0.05), (False, None))         # 空でタイムアウト
                self.assertGreaterEqual(time.perf_counter() - t0, 0.045)
                sent, got = [], []
                k = 0
                for rnd in range(3):                                            # 3 周の循環再利用
                    for _ in range(cap):
                        v = value_of(k)
                        k += 1
                        self.assertTrue(ring.put(v, timeout=0))
                        sent.append(v)
                    t0 = time.perf_counter()
                    self.assertFalse(ring.put(5, timeout=0.05))                 # 満杯でタイムアウト
                    self.assertGreaterEqual(time.perf_counter() - t0, 0.045)
                    self.assertFalse(ring.put(5, timeout=0))
                    for _ in range(cap):
                        ok, v = ring.get(timeout=0)
                        self.assertTrue(ok)
                        got.append(v)
                    self.assertEqual(ring.get(timeout=0), (False, None))
                # 位置をずらした交互操作（満杯のときだけ 1 件取り出してから入れる）
                for i in range(cap * 5):
                    while not ring.put(value_of(10_000 + i), timeout=0):
                        ok, v = ring.get(timeout=0)
                        self.assertTrue(ok)
                        got.append(v)
                    sent.append(value_of(10_000 + i))
                    if i % 3 != 2:
                        got.append(ring.get(timeout=0)[1])
                while True:
                    ok, v = ring.get(timeout=0)
                    if not ok:
                        break
                    got.append(v)
                self.assertEqual(got, sent)
                self.assertEqual(ring.sent_count, len(sent))
                self.assertEqual(ring.received_count, len(sent))
            finally:
                ring.unlink()

    def test_value_and_argument_validation(self):
        ring = SharedSpscRing(4, context=CTX)
        try:
            self.assertTrue(ring.put(0, timeout=0))
            self.assertTrue(ring.put(UINT64_MAX, timeout=0))
            for bad in (-1, UINT64_MAX + 1, -(2**64)):
                with self.assertRaises(ValueError):
                    ring.put(bad, timeout=0)
            for bad in (1.0, True, "1", None, b"\x01"):
                with self.assertRaises(TypeError):
                    ring.put(bad, timeout=0)
            with self.assertRaises(ValueError):
                ring.put(1, timeout=-1)
            with self.assertRaises(ValueError):
                ring.get(timeout=float("nan"))
            self.assertEqual(ring.get(timeout=0), (True, 0))
            self.assertEqual(ring.get(timeout=0), (True, UINT64_MAX))
            self.assertEqual(ring.get(timeout=0), (False, None))
            self.assertEqual((ring.sent_count, ring.received_count), (2, 2))   # 不正値で壊れない
        finally:
            ring.unlink()
        for bad in (0, 1, 3, 6, 8192, 4097, 5000):
            with self.assertRaises(ValueError):
                SharedSpscRing(bad, context=CTX)
        for bad in (2.0, True, "4"):
            with self.assertRaises(TypeError):
                SharedSpscRing(bad, context=CTX)

    def test_exception_during_put_and_get_keeps_counts(self):
        ring = SharedSpscRing(2, context=CTX)
        try:
            def boom(point):
                raise RuntimeError(point)
            ring._hook = boom
            with self.assertRaises(RuntimeError):
                ring.put(7, timeout=0)
            ring._hook = None
            self.assertEqual(ring.get(timeout=0), (False, None))   # 未公開の値は見えない
            self.assertTrue(ring.put(8, timeout=0))
            self.assertTrue(ring.put(9, timeout=0))                # 空き枠は 2 のまま
            self.assertFalse(ring.put(10, timeout=0))
            ring._hook = boom
            with self.assertRaises(RuntimeError):
                ring.get(timeout=0)
            ring._hook = None
            self.assertEqual(ring.get(timeout=0), (True, 8))       # 取り消した読み取りは残っている
            self.assertEqual(ring.get(timeout=0), (True, 9))
            self.assertEqual((ring.sent_count, ring.received_count), (2, 2))
        finally:
            ring.unlink()


class CrossProcess(unittest.TestCase):
    def run_pair(self, n, producer_first, p_slow=(0, 0.0), c_slow=(0, 0.0), cap=1024):
        ring = SharedSpscRing(cap, context=CTX)
        result = CTX.Value("q", -1)
        prod = CTX.Process(target=child_producer, args=(ring, n, *p_slow))
        cons = CTX.Process(target=child_consumer, args=(ring, n, *c_slow, result))
        first, second = (prod, cons) if producer_first else (cons, prod)
        t0 = time.perf_counter()
        first.start()
        time.sleep(0.5)                       # 片方だけ先に動かす
        second.start()
        for p in (prod, cons):
            p.join(600)
        alive = [p for p in (prod, cons) if p.is_alive()]
        for p in alive:
            p.terminate()
            p.join(10)
        elapsed = time.perf_counter() - t0
        ring.unlink()
        self.assertEqual(alive, [], "deadlock / hang")
        self.assertEqual((prod.exitcode, cons.exitcode), (0, 0))
        self.assertEqual(result.value, 0)       # 欠損・重複・順序違反 0
        return elapsed

    def test_one_million_child_to_parent(self):
        ring = SharedSpscRing(4096, context=CTX)
        prod = CTX.Process(target=child_producer, args=(ring, N_MILLION, 0, 0.0))
        t0 = time.perf_counter()
        prod.start()
        mismatch = 0
        expect = 0
        for _ in range(N_MILLION):
            ok, v = ring.get(timeout=60)
            self.assertTrue(ok)
            if v != value_of(expect):
                mismatch += 1
            expect += 1
        extra = ring.get(timeout=0.3)
        prod.join(60)
        elapsed = time.perf_counter() - t0
        self.assertEqual(prod.exitcode, 0)
        self.assertEqual(extra, (False, None))
        self.assertEqual(mismatch, 0)            # 位置ごとの一致 + 余分なし = 欠損・重複・順序違反 0
        self.assertEqual(ring.sent_count, N_MILLION)
        self.assertEqual(ring.received_count, N_MILLION)
        ring.unlink()
        print(f"\n[Q05] 1,000,000 u64 child->parent via spawn: {elapsed:.2f}s, "
              f"positional mismatches {mismatch}, extra item {extra}")

    def test_one_million_child_to_child(self):
        e = self.run_pair(N_MILLION, producer_first=True, cap=1024)
        print(f"\n[Q05] 1,000,000 u64 producer-child -> consumer-child: {e:.2f}s, all checks 0")

    def test_start_order_and_speed_difference(self):
        e1 = self.run_pair(20_000, producer_first=True, cap=4)           # 送信者だけ先（満杯で待つ）
        e2 = self.run_pair(20_000, producer_first=False, cap=4)          # 受信者だけ先（空で待つ）
        e3 = self.run_pair(5_000, True, p_slow=(500, 0.05), cap=2)       # 送信側が極端に遅い
        e4 = self.run_pair(5_000, True, c_slow=(500, 0.05), cap=2)       # 受信側が極端に遅い
        print(f"\n[Q05] producer-first {e1:.2f}s, consumer-first {e2:.2f}s, slow producer {e3:.2f}s, "
              f"slow consumer {e4:.2f}s")

    def test_no_read_of_unpublished_data(self):
        ring = SharedSpscRing(4, context=CTX)
        written, go = CTX.Event(), CTX.Event()
        p = CTX.Process(target=child_publish_gate, args=(ring, written, go))
        p.start()
        self.assertTrue(written.wait(30))                   # 子はスロットに書いたが未公開
        raw = int.from_bytes(bytes(ring._shm.buf[0:8]), "little")
        self.assertEqual(raw, 123456789)                    # データは共有メモリに既にある
        self.assertEqual(ring.get(timeout=0.3), (False, None))   # それでも get は読まない
        go.set()
        self.assertEqual(ring.get(timeout=30), (True, 123456789))
        self.assertEqual(ring.get(timeout=30), (True, 987654321))
        p.join(30)
        self.assertEqual(p.exitcode, 0)
        ring.unlink()


class Lifecycle(unittest.TestCase):
    def test_unlink_after_children_and_no_reattach(self):
        ring = SharedSpscRing(8, context=CTX)
        name = ring.name
        result = CTX.Value("q", 0)
        p = CTX.Process(target=child_try_unlink, args=(ring, result))
        p.start()
        p.join(30)
        self.assertEqual(p.exitcode, 0)
        self.assertEqual(result.value, 2)                   # 子は unlink できない
        ring.close()
        ring.close()
        ring.unlink()
        ring.unlink()                                        # 2 回目は何もしない（実施は 1 回）
        with self.assertRaises(FileNotFoundError):
            shared_memory.SharedMemory(name=name)
        with self.assertRaises(ValueError):
            ring.put(1)

    def test_parent_bounded_wait_and_cleanup_of_stuck_and_crashed_children(self):
        ring = SharedSpscRing(4, context=CTX)
        name = ring.name
        stuck = CTX.Process(target=child_block_forever, args=(ring,))
        stuck.start()
        stuck.join(1.0)                                      # 有界待機
        self.assertTrue(stuck.is_alive())
        t0 = time.perf_counter()
        stuck.terminate()                                    # 残存子プロセスを停止
        stuck.join(10)
        self.assertFalse(stuck.is_alive())
        self.assertIsNotNone(stuck.exitcode)
        crash = CTX.Process(target=child_crash_mid, args=(ring,))
        crash.start()
        crash.join(30)
        self.assertEqual(crash.exitcode, 3)
        self.assertEqual(ring.get(timeout=1), (True, 1))     # クラッシュ前に公開された分は読める
        self.assertEqual(ring.get(timeout=1), (True, 2))
        ring.unlink()
        with self.assertRaises(FileNotFoundError):
            shared_memory.SharedMemory(name=name)
        print(f"\n[Q05] stuck child terminated and reaped in {time.perf_counter() - t0:.2f}s, "
              f"crashed child exitcode {crash.exitcode}, shared memory removed")


if __name__ == "__main__":
    unittest.main(verbosity=2)
```

#### 5. 検証結果（期待値・実測値・実施状況）

| 必須テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 容量 2 / 4 / 1024 / 4096 | 各容量で 3 周の循環再利用。満杯・空の判定、送信順と受信順の完全一致 | 4 通りすべて一致 | 実施（修正後） |
| 空・満杯のタイムアウト | 空の get(0.05) は約 0.05 秒後に (False, None)。満杯の put(0.05) は False。timeout=0 は即座に返る | 一致 | 実施（修正後） |
| 0 / UINT64_MAX / 範囲外 | 0 と 2**64-1 は往復で一致。-1・2**64・-(2**64) は ValueError。1.0・True・"1"・None・bytes は TypeError。不正値でカウンターが壊れない | 一致 | 実施 |
| 不正な容量・timeout | 0・1・3・6・4097・5000・8192 は ValueError。2.0・True・"4" は TypeError。timeout が負・NaN は ValueError | 一致 | 実施 |
| put・get の途中の例外 | 公開前に例外が出ても、値は見えず、空き枠は 2 のまま。読み取り中に例外が出ても、要素は残る。カウンター（2, 2）は正しい | 一致 | 実施 |
| spawn した子 → 親で 100 万件 | 子の put を、親が 100 万回 get。位置ごとの値の不一致 0、余分な要素なし、カウンターは 1,000,000 / 1,000,000 | 5.86 秒、不一致 0、余分 0 | 実施 |
| spawn した子 → 子で 100 万件 | 送信と受信の子プロセス。受信側が全件を照合し、不一致 0、余分なし。両方とも終了コード 0、停止なし | 4.68 秒、0 | 実施 |
| 送信者だけ先に開始 | 送信の子を 0.5 秒先に起動（容量 4 が満杯で待つ）。2 万件で 0 | 1.12 秒、0 | 実施 |
| 受信者だけ先に開始 | 受信の子を 0.5 秒先に起動（空で待つ）。2 万件で 0 | 1.06 秒、0 | 実施 |
| 極端な速度差 | 容量 2 で、送信側または受信側が 500 件ごとに 50 ms 停止。5000 件で 0 | 1.70 秒 / 1.70 秒、0 | 実施 |
| 遅延注入による未公開データの読取なし | 子が値をスロットに書いた（共有メモリ上に 123456789 があることを親が確認）後、公開の直前で停止。その間、親の get(0.3) は (False, None)。公開後は 2 件を順に読める | 一致 | 実施 |
| unlink 後の再 attach 不可 | 子の unlink は RuntimeError（所有者でない）。全子プロセスの終了後に親が unlink。2 回目は何もしない。`SharedMemory(name=…)` は FileNotFoundError。閉じた後の put は ValueError | 一致 | 実施 |
| close の二重呼出し | 子・親とも二重 close は例外なし | 一致 | 実施 |
| 残った子・クラッシュの後始末 | 永久に get を待つ子を join(1.0) で有界に待ち、terminate → join で回収。put 2 件の後 `os._exit(3)` で異常終了した子の終了コードは 3。公開済みの 2 件は読め、その後 unlink で共有メモリを削除 | 回収 0.29 秒、終了コード 3、削除を確認 | 実施 |

**自律修正（テスト側）：** 容量のテストの「交互操作」で、1 周に 3 件入れて 2 件取り出していたため、やがて満杯になり、put(timeout=0) が False になっていました（実装は正しく満杯を報告していました）。満杯のときは 1 件取り出してから入れる形に直し、SingleProcess クラスを再実行して合格しました。

#### 6. 技術的な正当性・設計判断

- **公開の順序：**
  - put：`free.acquire` → スロットへ `struct.pack_into('<Q')` → 送信位置 += 1 → `items.release()`。
  - get：`items.acquire` に成功してからスロットを読む → 受信位置 += 1 → `free.release()`。
  - OS のセマフォ操作はプロセス間のメモリ同期点として働くので、release の前に書いたデータは、acquire した後の読み取りから見えます。`items` の個数は公開済みの要素数と等しいので、未公開のデータを読むことはありません（遅延注入で確認しました）。
- **FIFO・欠損なし・重複なし：** 送信位置は送信者だけが、受信位置は受信者だけが更新します（SPSC の契約）。スロットは `位置 & (capacity-1)` で、`free` が capacity 個を超える上書きを、`items` が未書込みの読み取りを防ぎます。カウンターは 64 ビットの単調増加で、契約（合計操作数 < 2**62）の範囲では周回しません。
- **例外安全：** セマフォを取得した後に例外が出たら、取得した側のセマフォを返し、カウンターは進めません。そのため、セマフォの個数とカウンターの不変条件（`items + free = capacity`、`sent - recv = items`）が保たれます。
- **所有権：**
  - 親が SharedMemory・カウンター・セマフォを作ります。子には spawn の引数（pickle）として渡し、子の `__setstate__` で名前で attach します。
  - 子の `close()` は detach だけです。`unlink()` は所有者のプロセス（pid も照合）だけが、全子プロセスの join の後に 1 回呼びます。2 回目以降は何もしません。
  - Windows では、全ハンドルが閉じた時点で OS が実体を破棄します（再 attach 不可を確認しました）。
- **POSIX への配慮：** Python 3.12 の POSIX では、attach した側も resource_tracker に登録され、所有者より先に unlink されることがあります。そこで、子では登録を解除するようにしました（Windows では resource_tracker を使いません）。
- **ビジースピンなし：** 待機はすべて `Semaphore.acquire(True, timeout)` で行います。

#### 7. 未実施項目・制約・残課題

- **POSIX（Linux・macOS）の spawn では未実施です。** resource_tracker の登録解除の処理は、Windows では通らない経路で、動作は未確認です。
- 契約により、送信者 1・受信者 1 を超える使い方は実行時に検出しません（SPSC の契約違反は未定義）。
- クラッシュした後の継続運用・自動復旧は、仕様どおり対象外です。セマフォを取得した直後にクラッシュすると、枠が失われることがあります（親が子を回収して破棄する前提）。
- 実行時間は、上記の Windows・CPU での測定値です。

---

## Q06【地獄級】デスクリプタ・メタクラス・MROで構築する不変スナップショットRecord

**試験の狙い：** __set_name__、__slots__、継承フィールド、バッチ更新の原子性、スレッド安全な公開を扱う。

### 元コード（危険・不完全）

```python
class FieldBad:
    def __init__(self, default=0): self.value = default
    def __get__(self, obj, typ=None): return self.value
    def __set__(self, obj, v): self.value = v  # 全インスタンスが同じ値を共有
class AccountBad:
    id = FieldBad("")
    balance = FieldBad(0)
```

### 要求仕様

1. `Field(expected_type: type, *, default=...)`、`SchemaMeta`、`RecordBase`を作る。`RecordBase`の公開APIは `snapshot() -> Mapping[str,object]` と `set_many(**changes) -> None`、各Fieldの属性アクセスと単一属性代入。Fieldが許す型はstr/int/boolのみで、`type(value) is expected_type`による**厳密な型判定**（boolをintとして受け付けない）。
2. Fieldはクラス宣言時の定義順に集める。単一継承で親のFieldを受け継ぎ、同名Fieldを子で上書き可能（順序は元の位置を維持）。同名の通常クラス属性で親Fieldを隠す場合はクラス作成時にTypeError。多重継承は禁止と明示する。
3. `RecordBase.__slots__`に必要な内部状態と`__weakref__`を設け、生成したサブクラスは任意属性を追加できない（`__dict__`を持たない）。各インスタンスの値は独立して保持し、クラスのField属性へのアクセスはそのFieldオブジェクト自身を返す。
4. コンストラクタはフィールドのデフォルト値とキーワード引数で初期化。必須フィールド（default未指定）が欠ければTypeError、未知フィールドもTypeError。`set_many`は**すべての検査が済むまで状態を更新しない**。失敗時は全フィールドとスナップショットが元のまま。単一属性代入も同じ検査経路。
5. 各インスタンスは`threading.RLock`等で同期し、`snapshot`は一貫した全フィールドの**独立コピー**を返す。複数スレッドでset_manyとsnapshotを同時実行しても混合状態を返さない。
6. Fieldの定義情報は各子クラスで独立して固定。クラスを生成した後で通常API経由のField定義追加は認めない。外部の`attrs`・`pydantic`・`dataclasses`による代替は不可（本問はメタクラスとdescriptorの独自実装試験）。

### 必須検証

1. 2個のインスタンスの値が独立、文字列/数値/真偽、厳密型判定、未指定必須項目と未知項目。
2. 親→子→孫の継承、Fieldの上書きと定義順、通常属性による親Fieldの隠蔽拒否、多重継承の拒否。
3. 代入失敗・set_many途中失敗時の原子的ロールバック、__dict__なし、weakrefが作れること。
4. 4書込者＋8読者で10万回のset_many/snapshot、各スナップショットが一貫した世代を表し混合値0。

### ★★FREYJAの回答【Q06・記入欄】

> **以下が専用回答スペース。** 問題本文、元コード、要求仕様、必須検証は改変しない。各項目の例示テキストを実際の内容へ置き換えること。

#### 1. 根本原因・危険性・影響

**元コード `FieldBad` / `AccountBad` の問題点**

| # | 問題箇所 | 発生条件 | 危険性・実害 |
| --- | --- | --- | --- |
| 1 | 値をデスクリプター自身（`self.value`）に保存する | インスタンスが 2 つ以上 | デスクリプターはクラス属性で 1 つだけなので、全インスタンスが同じ値を共有する（片方への代入が他方を書き換える） |
| 2 | 型を検査しない | 誤った型の代入 | 文字列の残高、bool を int として受け入れる |
| 3 | クラスからアクセスしても値を返す | `AccountBad.id` | Field オブジェクトを取得できず、スキーマを調べられない |
| 4 | フィールドの一覧・順序・継承の管理がない | 継承・上書き | 定義順・上書き・隠蔽を扱えない |
| 5 | 一括更新・同期がない | 複数フィールドの更新中の失敗、並行アクセス | 一部だけ更新された状態や、混ざった状態が見える |
| 6 | `__dict__` が残る | — | 任意の属性を追加できる |

**修正方針**
- 値は各インスタンスの `_values` 辞書（`__slots__`）に保持します。Field はスキーマ情報と型の厳密検査だけを担当します。
- `SchemaMeta` が、クラス作成時に Field を定義順に集め、親から継承します（同名の Field の上書きは元の位置を維持）。次の場合はクラス作成時に TypeError にします。
  - 通常の属性（値やメソッド）による親 Field の隠蔽
  - 多重継承
  - 空でない `__slots__`
  - Field の再利用
  - 作成後の Field の追加・変更・削除
- 更新は「全件の検査 → 新しい辞書の作成 → RLock の下で 1 回だけ差し替え」で行い、snapshot も同じロックの下で独立コピーを返します。

#### 2. 完全な修正版Pythonコード

```python
"""Q06: デスクリプタ・メタクラスによる型厳密な Record（単一継承のみ・スレッド安全な一括更新）。

方針:
  * 多重継承は禁止（クラス作成時に TypeError）。
  * 値は各インスタンスの辞書 _values に保持し、更新は「検査 → 新しい辞書を作る → ロック下で差し替え」。
    読み取り・snapshot もロック下で行う（GIL の偶然の性質に依存しない）。
"""
from __future__ import annotations

import threading
from types import MappingProxyType
from typing import Any, Mapping

__all__ = ["Field", "SchemaMeta", "RecordBase"]

_MISSING: Any = object()
_ALLOWED_TYPES = (str, int, bool)
_RESERVED = frozenset({"snapshot", "set_many"})


class Field:
    """型厳密なフィールド（type(value) is expected_type）。1 つの Field は 1 つのクラスだけに属する。"""

    __slots__ = ("expected_type", "default", "name", "owner")

    def __init__(self, expected_type: type, *, default: Any = _MISSING) -> None:
        if expected_type not in _ALLOWED_TYPES:
            raise TypeError("Field type must be str, int or bool")
        if default is not _MISSING and type(default) is not expected_type:
            raise TypeError(f"default must be exactly {expected_type.__name__}")
        self.expected_type = expected_type
        self.default = default
        self.name: str | None = None
        self.owner: type | None = None

    @property
    def required(self) -> bool:
        return self.default is _MISSING

    def __set_name__(self, owner: type, name: str) -> None:
        # 実際の登録と検査は SchemaMeta が行う（Field の再利用もそこで拒否する）
        pass

    def check(self, value: Any) -> None:
        if type(value) is not self.expected_type:
            raise TypeError(f"field {self.name!r} requires exactly {self.expected_type.__name__}, "
                            f"got {type(value).__name__}")

    def __get__(self, obj: Any, objtype: type | None = None) -> Any:
        if obj is None:
            return self                      # クラスからのアクセスは Field 自身
        return obj._read_field(self.name)

    def __set__(self, obj: Any, value: Any) -> None:
        obj.set_many(**{self.name: value})  # 単一代入も set_many と同じ検査経路

    def __delete__(self, obj: Any) -> None:
        raise AttributeError(f"field {self.name!r} cannot be deleted")

    def __repr__(self) -> str:
        d = "" if self.default is _MISSING else f", default={self.default!r}"
        return f"Field({self.expected_type.__name__}{d}) name={self.name!r}"


class SchemaMeta(type):
    def __new__(mcls, name: str, bases: tuple, ns: dict, **kw: Any):
        if len(bases) > 1:
            raise TypeError("multiple inheritance is not supported for records")
        parent_fields: Mapping[str, Field] = {}
        if bases:
            base = bases[0]
            if not isinstance(base, SchemaMeta):
                raise TypeError("records must derive from RecordBase")
            parent_fields = base.__fields__
            slots = ns.get("__slots__", ())
            if isinstance(slots, str):
                slots = (slots,)
            if tuple(slots) != ():
                raise TypeError("record subclasses cannot declare instance attributes (__slots__ must be empty)")
            ns["__slots__"] = ()             # __dict__ を作らせない
            for reserved in ("__fields__", "_field_names"):
                if reserved in ns:
                    raise TypeError(f"{reserved} is reserved")
        own: list[tuple[str, Field]] = [(k, v) for k, v in ns.items() if isinstance(v, Field)]
        for key, value in ns.items():
            if key in parent_fields and not isinstance(value, Field):
                raise TypeError(f"attribute {key!r} would hide inherited field {key!r}")
        ordered: dict[str, Field] = dict(parent_fields)
        for key, f in own:
            if key.startswith("_") or key in _RESERVED:
                raise TypeError(f"invalid field name {key!r}")
            if f.owner is not None:
                raise TypeError(f"Field object already belongs to {f.owner.__name__}.{f.name}")
            ordered[key] = f                 # 上書きは元の位置を維持、新規は末尾
        cls = super().__new__(mcls, name, bases, ns, **kw)
        for key, f in own:
            f.name = key
            f.owner = cls
        type.__setattr__(cls, "__fields__", MappingProxyType(ordered))
        type.__setattr__(cls, "_field_names", tuple(ordered))
        type.__setattr__(cls, "_schema_frozen", True)
        return cls

    def __setattr__(cls, name: str, value: Any) -> None:
        if cls.__dict__.get("_schema_frozen", False):
            if isinstance(value, Field) or name in cls.__fields__ or name in (
                    "__fields__", "_field_names", "_schema_frozen", "__slots__"):
                raise TypeError("record schema is fixed after class creation")
        super().__setattr__(name, value)

    def __delattr__(cls, name: str) -> None:
        if name in cls.__fields__ or name in ("__fields__", "_field_names", "_schema_frozen"):
            raise TypeError("record schema is fixed after class creation")
        super().__delattr__(name)


class RecordBase(metaclass=SchemaMeta):
    __slots__ = ("_values", "_lock", "__weakref__")

    def __init__(self, **kwargs: Any) -> None:
        fields = type(self).__fields__
        unknown = [k for k in kwargs if k not in fields]
        if unknown:
            raise TypeError(f"unknown field(s): {', '.join(sorted(unknown))}")
        values: dict[str, Any] = {}
        missing = []
        for name, f in fields.items():
            if name in kwargs:
                f.check(kwargs[name])
                values[name] = kwargs[name]
            elif f.required:
                missing.append(name)
            else:
                values[name] = f.default
        if missing:
            raise TypeError(f"missing required field(s): {', '.join(missing)}")
        object.__setattr__(self, "_lock", threading.RLock())
        object.__setattr__(self, "_values", values)

    def __setattr__(self, name: str, value: Any) -> None:
        if name in type(self).__fields__:
            object.__setattr__(self, name, value)        # → Field.__set__
            return
        raise AttributeError(f"{type(self).__name__} has no field {name!r}")

    def __delattr__(self, name: str) -> None:
        raise AttributeError(f"cannot delete {name!r}")

    def _read_field(self, name: str) -> Any:
        with self._lock:
            return self._values[name]

    def snapshot(self) -> Mapping[str, object]:
        """全フィールドの一貫した独立コピー（読み取り専用）。値は不変型 str/int/bool のみ。"""
        with self._lock:
            return MappingProxyType(dict(self._values))

    def set_many(self, **changes: Any) -> None:
        fields = type(self).__fields__
        for name, value in changes.items():           # 全件の検査が済むまで状態に触れない
            f = fields.get(name)
            if f is None:
                raise TypeError(f"unknown field {name!r}")
            f.check(value)
        with self._lock:
            new_values = dict(self._values)
            new_values.update(changes)
            object.__setattr__(self, "_values", new_values)   # 1 回の差し替えで公開

    def __repr__(self) -> str:
        snap = self.snapshot()
        inner = ", ".join(f"{k}={v!r}" for k, v in snap.items())
        return f"{type(self).__name__}({inner})"
```

#### 3. 実行・ビルド方法と使用環境

```powershell
# 作業フォルダー（試験専用の一時領域）: scratchpad\py\q06 に q06.py（実装）と test_q06.py（テスト）を置く
cd <scratchpad>\py\q06
python3.12 --version            # Python 3.12.15
python3.12 -X dev -m unittest -v test_q06
```

**使用環境（全問共通）**

- Python：**CPython 3.12.15**（`3.12.15 (main, Oct  9 2026) [MSC v.1944 64 bit (AMD64)]`）。この PC に既にあった `python3.12` コマンドで実行しました。新しいバージョンへの変更・pip によるパッケージ導入はしていません。
- ライブラリ：Python 標準ライブラリだけです。テストは `unittest` で行いました。
- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド、x86-64）。
- 実行方法：問題ごとの作業フォルダー（`scratchpad\py\qNN`。試験専用の一時領域）に、実装 `qNN.py` とテスト `test_qNN.py` を置いて実行しました。`-X dev`（開発モード：ResourceWarning や asyncio の検査を有効化）を付けています。
- 未実施の環境：Linux・macOS・ARM での実行はしていません。

- **実行結果：** `Ran 10 tests in 1.166s` → `OK`、終了コード 0（1 回目の実行で全件合格）。

#### 4. 実際に使用したテストコード

```python
"""Q06 のテスト。"""
from __future__ import annotations

import random
import threading
import time
import unittest
import weakref

from q06 import Field, RecordBase, SchemaMeta


class Account(RecordBase):
    id = Field(str)
    balance = Field(int, default=0)
    active = Field(bool, default=True)


class Basics(unittest.TestCase):
    def test_independent_instances_and_types(self):
        a = Account(id="a")
        b = Account(id="b", balance=5, active=False)
        a.balance = 10
        self.assertEqual((a.id, a.balance, a.active), ("a", 10, True))
        self.assertEqual((b.id, b.balance, b.active), ("b", 5, False))
        self.assertIsInstance(Account.balance, Field)          # クラスからは Field 自身
        self.assertIs(Account.__dict__["balance"], Account.balance)
        self.assertEqual(dict(a.snapshot()), {"id": "a", "balance": 10, "active": True})

    def test_strict_type_checks(self):
        a = Account(id="x")
        for field, bad in (("balance", True), ("balance", 1.0), ("balance", "1"), ("active", 1),
                           ("active", 0), ("id", 1), ("id", None), ("id", b"x")):
            with self.assertRaises(TypeError, msg=(field, bad)):
                setattr(a, field, bad)
            with self.assertRaises(TypeError):
                a.set_many(**{field: bad})
            with self.assertRaises(TypeError):
                Account(**{"id": "y", field: bad} if field != "id" else {"id": bad})
        self.assertEqual(dict(a.snapshot()), {"id": "x", "balance": 0, "active": True})
        with self.assertRaises(TypeError):
            Field(float)
        with self.assertRaises(TypeError):
            Field(int, default=True)                             # bool は int として受け付けない
        with self.assertRaises(TypeError):
            Field(bool, default=1)

    def test_required_and_unknown(self):
        with self.assertRaises(TypeError):
            Account()                                           # 必須 id がない
        with self.assertRaises(TypeError):
            Account(id="a", nope=1)
        a = Account(id="a")
        with self.assertRaises(TypeError):
            a.set_many(nope=1)
        with self.assertRaises(AttributeError):
            a.nope = 1
        with self.assertRaises(AttributeError):
            del a.balance


class Inheritance(unittest.TestCase):
    def test_parent_child_grandchild_order_and_override(self):
        class P(RecordBase):
            a = Field(int, default=1)
            b = Field(str, default="b")

        class C(P):
            c = Field(bool, default=False)
            a = Field(str, default="override")                # 上書き（位置は元の 1 番目）

        class G(C):
            d = Field(int)
            b = Field(int, default=2)

        self.assertEqual(list(P.__fields__), ["a", "b"])
        self.assertEqual(list(C.__fields__), ["a", "b", "c"])
        self.assertEqual(list(G.__fields__), ["a", "b", "c", "d"])
        self.assertEqual(dict(G(d=4).snapshot()), {"a": "override", "b": 2, "c": False, "d": 4})
        self.assertEqual(dict(P().snapshot()), {"a": 1, "b": "b"})
        with self.assertRaises(TypeError):
            G(d=4, a=1)                                         # 子では a は str
        P(a=5)
        self.assertIsNot(P.__fields__["a"], C.__fields__["a"])
        self.assertIs(C.__fields__["b"], P.__fields__["b"])   # 継承した定義は共有（不変）
        self.assertTrue(isinstance(G, SchemaMeta))

    def test_hiding_field_with_plain_attribute_rejected(self):
        class P(RecordBase):
            a = Field(int, default=1)
        with self.assertRaises(TypeError):
            class Bad(P):
                a = 5
        with self.assertRaises(TypeError):
            class Bad2(P):
                def a(self):
                    return 1

    def test_multiple_inheritance_rejected(self):
        class A(RecordBase):
            x = Field(int, default=0)

        class B(RecordBase):
            y = Field(int, default=0)
        with self.assertRaises(TypeError):
            class AB(A, B):
                pass

        class Mixin:
            pass
        with self.assertRaises(TypeError):
            class AM(A, Mixin):
                pass

    def test_schema_fixed_after_creation(self):
        class P(RecordBase):
            a = Field(int, default=1)
        with self.assertRaises(TypeError):
            P.b = Field(int, default=2)
        with self.assertRaises(TypeError):
            P.a = 3
        with self.assertRaises(TypeError):
            del P.a
        with self.assertRaises(TypeError):
            P.__fields__ = {}
        with self.assertRaises(TypeError):
            P.__fields__["z"] = Field(int)                     # MappingProxy は読み取り専用
        f = Field(int, default=0)

        class Q(RecordBase):
            v = f
        with self.assertRaises(TypeError):                     # 同じ Field の別クラスへの再利用
            class R(RecordBase):
                w = f
        with self.assertRaises(TypeError):
            class S(RecordBase):
                __slots__ = ("extra",)
                v = Field(int, default=0)
        self.assertEqual(list(Q.__fields__), ["v"])


class AtomicityAndSlots(unittest.TestCase):
    def test_rollback_on_failure(self):
        a = Account(id="a", balance=1, active=True)
        before = a.snapshot()
        with self.assertRaises(TypeError):
            a.set_many(balance=2, id="new", active="no")       # 最後の項目で失敗
        with self.assertRaises(TypeError):
            a.set_many(balance=3, missing=1)
        with self.assertRaises(TypeError):
            a.balance = True
        self.assertEqual(dict(a.snapshot()), dict(before))
        self.assertEqual((a.id, a.balance, a.active), ("a", 1, True))
        a.set_many(balance=9, id="z")
        self.assertEqual(dict(before), {"id": "a", "balance": 1, "active": True})   # 過去の snapshot は独立
        with self.assertRaises(TypeError):
            before["balance"] = 5                              # 読み取り専用

    def test_no_dict_and_weakref(self):
        a = Account(id="a")
        self.assertFalse(hasattr(a, "__dict__"))
        with self.assertRaises(AttributeError):
            a.arbitrary = 1
        r = weakref.ref(a)
        self.assertIs(r(), a)
        del a
        import gc
        gc.collect()
        self.assertIsNone(r())


class Threads(unittest.TestCase):
    def test_4_writers_8_readers_100k_operations(self):
        class Gen(RecordBase):
            a = Field(int, default=0)
            b = Field(int, default=0)
            c = Field(int, default=0)
            d = Field(int, default=0)
            s = Field(str, default="0")
            flag = Field(bool, default=True)

        rec = Gen()
        writes_per_writer = 12_500
        reads_per_reader = 6_250            # 4*12,500 + 8*6,250 = 100,000 回
        mixed = []
        rejected = [0]
        counts = {"set_many": 0, "snapshot": 0}
        lock = threading.Lock()
        start = threading.Barrier(12)

        def writer(wid):
            rng = random.Random(wid)
            start.wait()
            n_ok = 0
            for i in range(writes_per_writer):
                g = wid * 1_000_000 + i
                if rng.random() < 0.1:
                    try:
                        rec.set_many(a=-1, b=-1, c=-1, d=-1, s="bad", flag=1)   # 最後で失敗
                    except TypeError:
                        with lock:
                            rejected[0] += 1
                    continue
                rec.set_many(a=g, b=g, c=g, d=g, s=str(g), flag=(g % 2 == 0))
                n_ok += 1
            with lock:
                counts["set_many"] += writes_per_writer

        def reader(rid):
            start.wait()
            local_bad = 0
            for _ in range(reads_per_reader):
                snap = rec.snapshot()
                g = snap["a"]
                if not (snap["b"] == snap["c"] == snap["d"] == g and snap["s"] == str(g)
                        and snap["flag"] is (g % 2 == 0)):
                    local_bad += 1
            with lock:
                counts["snapshot"] += reads_per_reader
                if local_bad:
                    mixed.append(local_bad)

        threads = [threading.Thread(target=writer, args=(w,)) for w in range(4)]
        threads += [threading.Thread(target=reader, args=(r,)) for r in range(8)]
        t0 = time.perf_counter()
        for t in threads:
            t.start()
        for t in threads:
            t.join(120)
        self.assertFalse(any(t.is_alive() for t in threads))
        self.assertEqual(mixed, [])
        self.assertEqual(counts["set_many"] + counts["snapshot"], 100_000)
        final = rec.snapshot()
        self.assertTrue(final["a"] == final["b"] == final["c"] == final["d"])
        print(f"\n[Q06] 4 writers + 8 readers: {counts} (rejected batches {rejected[0]}), "
              f"mixed snapshots {sum(mixed)}, {time.perf_counter() - t0:.2f}s")


if __name__ == "__main__":
    unittest.main(verbosity=2)
```

#### 5. 検証結果（期待値・実測値・実施状況）

| 必須テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 2 つのインスタンスの値が独立 | a.balance=10 が b に影響しない | 一致 | 実施 |
| 文字列・数値・真偽 | str/int/bool の Field の初期化・代入・snapshot | 一致 | 実施 |
| 厳密な型判定 | int の Field に True・1.0・"1"、bool の Field に 1・0、str の Field に 1・None・b"x" → 代入・set_many・コンストラクターのいずれでも TypeError。`Field(float)`・`Field(int, default=True)`・`Field(bool, default=1)` も TypeError | 一致 | 実施 |
| 必須項目の未指定・未知の項目 | `Account()`・`Account(id="a", nope=1)`・`set_many(nope=1)` は TypeError。未知の属性への代入は AttributeError。del は AttributeError | 一致 | 実施 |
| 親 → 子 → 孫の継承 | 定義順 P:[a,b] → C:[a,b,c] → G:[a,b,c,d]。上書きは元の位置のまま。孫の既定値・型は上書き後のもの | 一致 | 実施 |
| Field の上書き | 子の a は str（元の 1 番目のまま）。孫で a=1 は TypeError | 一致 | 実施 |
| 通常の属性による隠蔽の拒否 | 子で `a = 5`、または `def a` → クラス作成時に TypeError | 一致 | 実施 |
| 多重継承の拒否 | `class AB(A, B)`・`class AM(A, Mixin)` → TypeError | 一致 | 実施 |
| スキーマの固定 | 作成後の `P.b = Field(...)`・`P.a = 3`・`del P.a`・`P.__fields__ = {}`・`__fields__` の項目追加は TypeError。Field の別クラスへの再利用、空でない `__slots__` も TypeError | 一致 | 実施 |
| 代入・set_many の途中失敗時の原子的ロールバック | `set_many(balance=2, id="new", active="no")`（最後で失敗）・未知の項目の混在・単一代入の失敗の後、全フィールドと snapshot が元のまま。過去の snapshot は後の更新の影響を受けず、読み取り専用 | 一致 | 実施 |
| `__dict__` なし・weakref | `hasattr(a, "__dict__")` は False、任意の属性は AttributeError。weakref が作れ、del と GC の後は None | 一致 | 実施 |
| 4 書込み + 8 読取りで 10 万回 | 書込み 4 × 12,500 回の set_many（約 10% は最後の項目で型エラー）+ 読取り 8 × 6,250 回の snapshot = 10 万回。各 snapshot で a=b=c=d、s=str(a)、flag=(a が偶数) | set_many 50,000（拒否 5,036）、snapshot 50,000、混合値 0、1.16 秒 | 実施 |

#### 6. 技術的な正当性・設計判断

- **デスクリプター：**
  - `Field.__get__(None, cls)` は Field 自身を返し、インスタンスからは `obj._read_field(name)` を返します。
  - `__set__` は `set_many(**{name: value})` に委ねるので、単一の代入も一括更新と同じ検査経路を通ります。
  - Field はデータデスクリプター（`__set__` を持つ）なので、インスタンス側の属性より常に優先されます。
- **メタクラス：**
  - クラス本体の名前空間（定義順を保つ dict）から Field を集め、`dict(親の __fields__)` に上書き・追加します。こうすると、上書きは元の位置に、新しい項目は末尾に並びます。
  - 結果は `MappingProxyType` で固定し、クラスごとに独立させます。
  - `SchemaMeta.__setattr__`・`__delattr__` は、作成後の Field の追加・変更・削除を拒否します。
- **スロット：** RecordBase の `__slots__ = ("_values", "_lock", "__weakref__")` に対し、サブクラスには `__slots__ = ()` を強制します。そのため `__dict__` を持たず、weakref は使えます。`RecordBase.__setattr__` は、Field 以外の名前への代入を AttributeError にします。
- **原子性とスレッド安全性：**
  - set_many は、すべての値の型検査を終えてから、ロックの下で `dict(self._values)` に変更を適用した新しい辞書を作り、`_values` を 1 回で差し替えます。検査の失敗では、状態に一切触れません。
  - 読み取り（`_read_field`・snapshot）も同じ RLock の下で行うので、GIL の偶然の性質には依存しません。
  - snapshot は `MappingProxyType(dict(...))` で、値はすべて不変型（str/int/bool）です。そのため、浅いコピーで完全に独立し、変更もできません。
- **多重継承：** フィールドの合成順（MRO）の曖昧さを避けるため、仕様として明示的に禁止しました（`len(bases) > 1` で TypeError）。

#### 7. 未実施項目・制約・残課題

- 契約により、多重継承（mixin を含む）は禁止です。
- RecordBase の内部スロット（`_values`・`_lock`）は、`object.__setattr__` を直接使えば書き換えられます（Python の言語仕様上、完全には防げない内部 API）。
- 内部の状態は Python のオブジェクトなので、pickle・copy には対応していません（問題文の要求外）。
- Linux・macOS、free-threaded（GIL なし）ビルドでの実行はしていません（同期はロックで行っているので、設計上は依存しません）。

---

## Q07【地獄級】外部マージソート・100万件JSONL・安定順序と原子的出力

**試験の狙い：** 外部記憶アルゴリズム、bounded memory、heapqマージ、失敗時の掃除、ファイル公開の原子性。

### 元コード（危険・不完全）

```python
import json
def sort_bad(src, dst):
    rows = [json.loads(x) for x in open(src, encoding='utf-8')]
    rows.sort(key=lambda x: x["timestamp"])
    open(dst, 'w', encoding='utf-8').write("\n".join(map(json.dumps, rows)))
    # 入力全件常駐、失敗すると出力が壊れる
```

### 要求仕様

1. `external_stable_sort(source: Path, target: Path, *, max_rows_in_memory: int = 4096) -> SortStats`を実装。入力はUTF-8のJSON Lines。各行は`{"timestamp": signed int64, "payload": str}`の**ちょうど2フィールド**。1行のUTF-8バイト長は最大4096。空行・未知フィールド・float・bool timestamp・不正JSON・不正UTF-8は拒否。
2. ソートキーは`timestamp`の数値昇順。等しいtimestampの行は**元ファイル内の物理行順**を維持する安定ソート。出力のJSON表現は`json.dumps(..., ensure_ascii=False, separators=(',',':'))`とし、各行末尾にLFを付ける。入力末尾の最終行だけLFがなくても受理。
3. 入力はストリームで読み取り、1回に保持する入力レコードは最大`max_rows_in_memory`件（許容範囲1〜4096）。一時runを整列させて書き出し、`heapq`によるk-way mergeを行う。全件のPython listを保持する実装は禁止。
4. run数が多数になった場合の同時オープンファイル上限は32。必要なら段階的マージを行い、オープン中のrunファイルは最大32。中間runの1レコードには元ファイルの行番号を含めて安定性を維持する。ピークメモリはO(max_rows_in_memory + 開いているrun数)レコード以内。
5. 中間ファイルと最終一時出力はtargetと同じ親ディレクトリ内の**専用一時フォルダー**に作成。完了時に最終一時出力をflush+fsyncしてから`os.replace`でtargetへ公開。失敗時は既存targetを無変更とし、自身の一時ファイルを除去。
6. sourceとtargetが同一パスの場合はValueError。検証用のファイル読込/書込/merge/replaceの障害注入ポイントを設け、失敗した際に既存targetを破壊しない。別プロセスがtargetを同時変更しない契約。
7. SortStatsは総入力行数、生成run数、マージ段数、最大同時オープンrun数を保持。標準ライブラリのみ使用。

### 必須検証

1. 空/1行/4096/4097/100万行、timestampの最小・最大値、同じtimestampを異なるrunにまたがって配置し安定順序を確認。
2. 不正UTF-8、未知キー、空行、bool・float timestamp、長すぎる1行、不正JSON、最終行LFなし。
3. runが32を超える入力で多段マージし、すべての入力レコードがちょうど1回現れ、並びが参照ソートと一致。
4. 各障害注入位置で既存targetと自身の一時フォルダーに問題がないこと、最大常駐件数と最大同時ファイル数の計測。

### ★★FREYJAの回答【Q07・記入欄】

> **以下が専用回答スペース。** 問題本文、元コード、要求仕様、必須検証は改変しない。各項目の例示テキストを実際の内容へ置き換えること。

#### 1. 根本原因・危険性・影響

**元コード `sort_bad` の問題点**

| # | 問題箇所 | 発生条件 | 危険性・実害 |
| --- | --- | --- | --- |
| 1 | 全行を list に読み込む | 大きな入力（100 万行以上） | メモリ枯渇（入力サイズに比例して常駐） |
| 2 | `open(dst, 'w')` で直接書き込む | 書込み途中の例外・中断 | 既存の出力が切り詰められて壊れる（原子性がない） |
| 3 | 入力を検証しない | 不正な JSON・未知のキー・float・bool の timestamp・不正な UTF-8 | 不正データの混入、型の異なる値の比較での TypeError |
| 4 | 出力形式が仕様と異なる | — | `json.dumps` の既定（ASCII エスケープ・空白入り）。最終行の LF もない |
| 5 | ファイルを閉じない・fsync しない | — | 書込みが失われる、ファイルハンドルのリーク |
| 6 | source == target を検査しない | 同じパス | 読み込みと書込みが衝突する |

**修正方針**
- 入力を 1 行ずつ（`readline(4097)` で長さを制限）読み、厳密に検証します。
- 最大 `max_rows_in_memory` 件ずつ、(timestamp, 元の行番号) で整列した run をバイナリ形式で書き出します。
- `heapq.merge` による k-way マージは、同時に開く run を 32 以下に制限し、必要なら多段で行います。
- 最終出力は、target と同じ親にある専用の一時フォルダーに書き、flush + fsync してから `os.replace` で公開します。失敗時は一時フォルダーだけを除去します。

#### 2. 完全な修正版Pythonコード

```python
"""Q07: JSON Lines の外部安定マージソート（有界メモリ・多段マージ・原子的な公開）。"""
from __future__ import annotations

import heapq
import json
import os
import shutil
import struct
import tempfile
from dataclasses import dataclass
from pathlib import Path
from typing import Callable, Iterator

__all__ = ["external_stable_sort", "SortStats", "InputFormatError", "MAX_FAN_IN", "MAX_LINE_BYTES"]

MAX_LINE_BYTES = 4096          # LF を除く 1 行の UTF-8 バイト長の上限
MAX_FAN_IN = 32                # 同時に開く run ファイルの上限
INT64_MIN = -(2**63)
INT64_MAX = 2**63 - 1
_REC = struct.Struct("<qQI")   # run レコード: timestamp(i64) + 元の行番号(u64) + payload 長(u32) + payload(UTF-8)


class InputFormatError(ValueError):
    def __init__(self, line_no: int, reason: str) -> None:
        super().__init__(f"line {line_no}: {reason}")
        self.line_no = line_no
        self.reason = reason


@dataclass(frozen=True)
class SortStats:
    total_rows: int             # 入力行数
    runs_created: int           # 生成した run 数（初回 run + 中間 run）
    merge_passes: int           # マージ段数（最終出力の段を含む）
    max_open_runs: int          # 同時に開いていた run ファイル数の最大値
    peak_rows_in_memory: int    # 計測: 同時に保持した入力レコード数の最大値（run 作成バッファー + マージのヒープ）


class _Obj(list):
    """json の object_pairs_hook 用（重複キーの検出と、配列との区別）。"""


def _reject_float(_s: str):
    raise ValueError("floating point numbers are not allowed")


def _reject_constant(_s: str):
    raise ValueError("NaN/Infinity are not allowed")


_DECODER = json.JSONDecoder(object_pairs_hook=_Obj, parse_float=_reject_float,
                            parse_constant=_reject_constant)


def _parse_line(raw: bytes, line_no: int) -> tuple[int, str]:
    if not raw:
        raise InputFormatError(line_no, "empty line")
    try:
        text = raw.decode("utf-8")
    except UnicodeDecodeError:
        raise InputFormatError(line_no, "invalid UTF-8") from None
    try:
        obj = _DECODER.decode(text)
    except ValueError as exc:
        raise InputFormatError(line_no, f"invalid JSON ({exc})") from None
    if type(obj) is not _Obj:
        raise InputFormatError(line_no, "top-level value must be an object")
    keys = [k for k, _ in obj]
    if len(keys) != 2 or set(keys) != {"timestamp", "payload"}:
        raise InputFormatError(line_no, f"object must have exactly timestamp and payload, got {keys}")
    d = dict(obj)
    ts, payload = d["timestamp"], d["payload"]
    if type(ts) is not int:
        raise InputFormatError(line_no, "timestamp must be an integer (not bool/float)")
    if not INT64_MIN <= ts <= INT64_MAX:
        raise InputFormatError(line_no, "timestamp out of signed int64 range")
    if type(payload) is not str:
        raise InputFormatError(line_no, "payload must be a string")
    try:
        payload.encode("utf-8")
    except UnicodeEncodeError:
        raise InputFormatError(line_no, "payload contains an unpaired surrogate") from None
    return ts, payload


def _read_records(f, hook) -> Iterator[tuple[int, int, str]]:
    line_no = 0
    while True:
        if hook is not None and line_no % 256 == 0:
            hook("read")
        raw = f.readline(MAX_LINE_BYTES + 1)
        if not raw:
            return
        line_no += 1
        if raw.endswith(b"\n"):
            raw = raw[:-1]
        elif len(raw) > MAX_LINE_BYTES:
            raise InputFormatError(line_no, f"line longer than {MAX_LINE_BYTES} bytes")
        # LF なしで終わるのは最終行だけ（readline の仕様上、上限未満で LF がなければ EOF）
        ts, payload = _parse_line(raw, line_no)
        yield ts, line_no, payload


class _Sorter:
    def __init__(self, workdir: str, max_rows: int, hook) -> None:
        self.workdir = workdir
        self.max_rows = max_rows
        self.hook = hook
        self.counter = 0
        self.runs_created = 0
        self.open_runs = 0
        self.max_open_runs = 0
        self.peak_rows = 0
        self.merge_passes = 0

    def _new_path(self) -> str:
        self.counter += 1
        return os.path.join(self.workdir, f"run-{self.counter:08d}.bin")

    def write_run(self, records) -> str:
        if self.hook is not None:
            self.hook("write")
        path = self._new_path()
        try:
            with open(path, "wb") as out:
                for ts, line, payload in records:
                    data = payload.encode("utf-8")
                    out.write(_REC.pack(ts, line, len(data)))
                    out.write(data)
        finally:
            close = getattr(records, "close", None)   # マージ元のジェネレーターを確実に閉じる
            if close is not None:
                close()
        self.runs_created += 1
        return path

    def read_run(self, path: str) -> Iterator[tuple[int, int, str]]:
        f = open(path, "rb", buffering=65536)
        self.open_runs += 1
        self.max_open_runs = max(self.max_open_runs, self.open_runs)
        try:
            while True:
                head = f.read(_REC.size)
                if not head:
                    return
                if len(head) != _REC.size:
                    raise OSError(f"corrupt run file {path}")
                ts, line, n = _REC.unpack(head)
                data = f.read(n)
                if len(data) != n:
                    raise OSError(f"corrupt run file {path}")
                yield ts, line, data.decode("utf-8")
        finally:
            f.close()
            self.open_runs -= 1

    def merge(self, paths: list[str]) -> Iterator[tuple[int, int, str]]:
        """(timestamp, 元の行番号) の昇順で k-way マージ（heapq）。行番号は一意なので安定。"""
        assert len(paths) <= MAX_FAN_IN
        self.peak_rows = max(self.peak_rows, len(paths))   # ヒープには各 run の先頭 1 件ずつ
        gens = [self.read_run(p) for p in paths]
        try:
            yield from heapq.merge(*gens)
        finally:
            for g in gens:
                g.close()

    def build_runs(self, src) -> tuple[int, list[str]]:
        runs: list[str] = []
        buf: list[tuple[int, int, str]] = []
        total = 0
        for rec in _read_records(src, self.hook):
            buf.append(rec)
            total += 1
            if len(buf) > self.peak_rows:
                self.peak_rows = len(buf)
            if len(buf) == self.max_rows:
                buf.sort()
                runs.append(self.write_run(buf))
                buf = []
        if buf:
            buf.sort()
            runs.append(self.write_run(buf))
        return total, runs

    def reduce_runs(self, runs: list[str]) -> list[str]:
        """run 数が 32 以下になるまで段階的にマージする。"""
        while len(runs) > MAX_FAN_IN:
            self.merge_passes += 1
            nxt = []
            for i in range(0, len(runs), MAX_FAN_IN):
                group = runs[i:i + MAX_FAN_IN]
                if self.hook is not None:
                    self.hook("merge")
                nxt.append(self.write_run(self.merge(group)))
                for p in group:
                    os.remove(p)
            runs = nxt
        return runs


def external_stable_sort(source: Path, target: Path, *, max_rows_in_memory: int = 4096,
                         fault_hook: Callable[[str], None] | None = None) -> SortStats:
    """fault_hook は検証用の障害注入点（"read" / "write" / "merge" / "replace"）。通常は None。"""
    if type(max_rows_in_memory) is not int:
        raise TypeError("max_rows_in_memory must be int")
    if not 1 <= max_rows_in_memory <= 4096:
        raise ValueError("max_rows_in_memory must be within 1..4096")
    source = Path(source)
    target = Path(target)
    src_abs = Path(os.path.abspath(source))
    tgt_abs = Path(os.path.abspath(target))
    if os.path.normcase(str(src_abs)) == os.path.normcase(str(tgt_abs)) or (
            target.exists() and source.exists() and os.path.samefile(source, target)):
        raise ValueError("source and target must be different paths")
    parent = tgt_abs.parent
    if not parent.is_dir():
        raise FileNotFoundError(f"target directory does not exist: {parent}")

    workdir = tempfile.mkdtemp(prefix=f".{tgt_abs.name}.sort-", dir=parent)   # 専用一時フォルダー
    try:
        sorter = _Sorter(workdir, max_rows_in_memory, fault_hook)
        with open(source, "rb") as src:
            total, runs = sorter.build_runs(src)
        runs = sorter.reduce_runs(runs)
        tmp_out = os.path.join(workdir, "output.jsonl.tmp")
        with open(tmp_out, "wb") as out:
            if runs:
                sorter.merge_passes += 1
                if fault_hook is not None:
                    fault_hook("merge")
                merged = sorter.merge(runs)
                try:
                    for ts, _line, payload in merged:
                        out.write(json.dumps({"timestamp": ts, "payload": payload}, ensure_ascii=False,
                                             separators=(",", ":")).encode("utf-8"))
                        out.write(b"\n")
                finally:
                    merged.close()                # run ファイルを確実に閉じる
            out.flush()
            os.fsync(out.fileno())
        if fault_hook is not None:
            fault_hook("replace")
        os.replace(tmp_out, tgt_abs)          # 公開（失敗時は既存 target は無変更）
        if os.name == "posix":                # ディレクトリエントリの永続化（POSIX のみ可能）
            dfd = os.open(parent, os.O_RDONLY)
            try:
                os.fsync(dfd)
            finally:
                os.close(dfd)
        return SortStats(total, sorter.runs_created, sorter.merge_passes, sorter.max_open_runs,
                         sorter.peak_rows)
    finally:
        shutil.rmtree(workdir, ignore_errors=True)   # 自分の一時フォルダーだけを除去
```

#### 3. 実行・ビルド方法と使用環境

```powershell
# 作業フォルダー（試験専用の一時領域）: scratchpad\py\q07 に q07.py（実装）と test_q07.py（テスト）を置く
cd <scratchpad>\py\q07
python3.12 --version            # Python 3.12.15
python3.12 -X dev -m unittest -v test_q07
```

**使用環境（全問共通）**

- Python：**CPython 3.12.15**（`3.12.15 (main, Oct  9 2026) [MSC v.1944 64 bit (AMD64)]`）。この PC に既にあった `python3.12` コマンドで実行しました。新しいバージョンへの変更・pip によるパッケージ導入はしていません。
- ライブラリ：Python 標準ライブラリだけです。テストは `unittest` で行いました。
- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド、x86-64）。
- 実行方法：問題ごとの作業フォルダー（`scratchpad\py\qNN`。試験専用の一時領域）に、実装 `qNN.py` とテスト `test_qNN.py` を置いて実行しました。`-X dev`（開発モード：ResourceWarning や asyncio の検査を有効化）を付けています。
- 未実施の環境：Linux・macOS・ARM での実行はしていません。

- **テストデータ：** `scratchpad\py\q07\testdata\q07-*`（テストごとの専用一時フォルダー）にだけ作り、終了時に削除しました（残ったファイルは 0 件）。
- **実行結果：** `Ran 10 tests in 107.389s` → `OK`、終了コード 0（1 回目の実行で全件合格）。

#### 4. 実際に使用したテストコード

```python
"""Q07 のテスト。すべて q07/testdata 配下の専用一時フォルダーで実行し、終了時に削除する。"""
from __future__ import annotations

import json
import os
import random
import shutil
import tempfile
import time
import tracemalloc
import unittest
from pathlib import Path

from q07 import InputFormatError, SortStats, external_stable_sort

HERE = Path(__file__).resolve().parent
BASE = HERE / "testdata"
I64MIN, I64MAX = -(2**63), 2**63 - 1


def dumps(ts, payload):
    return json.dumps({"timestamp": ts, "payload": payload}, ensure_ascii=False, separators=(",", ":"))


def reference_bytes(rows):
    """参照: Python の安定ソート（list.sort は安定）を全件メモリ上で行う。"""
    ordered = sorted(rows, key=lambda r: r[0])
    return b"".join((dumps(t, p) + "\n").encode("utf-8") for t, p in ordered)


def write_rows(path, rows, *, final_lf=True, key_order=("timestamp", "payload")):
    with open(path, "wb") as f:
        for i, (t, p) in enumerate(rows):
            obj = {k: (t if k == "timestamp" else p) for k in key_order}
            line = json.dumps(obj, ensure_ascii=bool(i % 2)).encode("utf-8")   # 入力側は表現を混在
            f.write(line)
            if final_lf or i != len(rows) - 1:
                f.write(b"\n")


class TmpDir(unittest.TestCase):
    def setUp(self):
        BASE.mkdir(exist_ok=True)
        self.dir = Path(tempfile.mkdtemp(prefix="q07-", dir=BASE))
        self.src = self.dir / "in.jsonl"
        self.dst = self.dir / "out.jsonl"

    def tearDown(self):
        shutil.rmtree(self.dir)

    def leftovers(self):
        return [p.name for p in self.dir.iterdir() if p.name.startswith(".out.jsonl.sort-")]


class Sizes(TmpDir):
    def check(self, rows, max_rows=4096, **kw):
        write_rows(self.src, rows, **kw)
        stats = external_stable_sort(self.src, self.dst, max_rows_in_memory=max_rows)
        self.assertEqual(self.dst.read_bytes(), reference_bytes(rows))
        self.assertEqual(stats.total_rows, len(rows))
        self.assertLessEqual(stats.max_open_runs, 32)
        self.assertLessEqual(stats.peak_rows_in_memory, max(max_rows, 32))
        self.assertEqual(self.leftovers(), [])
        return stats

    def test_empty_one_4096_4097(self):
        s = self.check([])
        self.assertEqual((s.total_rows, s.runs_created, s.merge_passes), (0, 0, 0))
        self.assertEqual(self.dst.read_bytes(), b"")
        s = self.check([(5, "only")])
        self.assertEqual((s.runs_created, s.merge_passes), (1, 1))
        rng = random.Random(1)
        rows = [(rng.randrange(-50, 50), f"r{i}") for i in range(4096)]
        s = self.check(rows)
        self.assertEqual((s.runs_created, s.merge_passes, s.peak_rows_in_memory), (1, 1, 4096))
        rows = [(rng.randrange(-50, 50), f"r{i}") for i in range(4097)]
        s = self.check(rows)
        self.assertEqual((s.runs_created, s.merge_passes, s.max_open_runs), (2, 1, 2))

    def test_extremes_and_stability_across_runs(self):
        rows = []
        for i in range(3000):
            rows.append((0 if i % 3 else (I64MIN if i % 2 else I64MAX), f"p{i:05d}-é✓😀"))
        s = self.check(rows, max_rows=100)                   # 同じ timestamp が 30 個の run にまたがる
        self.assertEqual(s.runs_created, 30)
        out = self.dst.read_text("utf-8").splitlines()
        self.assertEqual(json.loads(out[0])["timestamp"], I64MIN)
        self.assertEqual(json.loads(out[-1])["timestamp"], I64MAX)
        zeros = [json.loads(x)["payload"] for x in out if json.loads(x)["timestamp"] == 0]
        self.assertEqual(zeros, sorted(zeros))               # 元の物理行順（番号順）を維持

    def test_final_line_without_lf_and_key_order(self):
        self.check([(2, "b"), (1, "a"), (2, "c")], final_lf=False, key_order=("payload", "timestamp"))
        self.assertEqual(self.dst.read_bytes(),
                         b'{"timestamp":1,"payload":"a"}\n{"timestamp":2,"payload":"b"}\n'
                         b'{"timestamp":2,"payload":"c"}\n')

    def test_one_million_rows(self):
        rng = random.Random(7)
        rows = [(rng.randrange(-10**6, 10**6) if i % 10 else rng.choice([I64MIN, I64MAX, 0]), f"{i}")
                for i in range(1_000_000)]
        write_rows(self.src, rows)
        t0 = time.perf_counter()
        s = external_stable_sort(self.src, self.dst)
        el = time.perf_counter() - t0
        self.assertEqual(self.dst.read_bytes(), reference_bytes(rows))
        self.assertEqual(s.total_rows, 1_000_000)
        self.assertEqual(s.runs_created, 245 + 8)           # 初回 245 run + 第1段の中間 run 8
        self.assertEqual(s.merge_passes, 2)
        self.assertLessEqual(s.max_open_runs, 32)
        self.assertEqual(self.leftovers(), [])
        print(f"\n[Q07] 1,000,000 rows: {s} in {el:.1f}s (input {self.src.stat().st_size} bytes)")


class InvalidInput(TmpDir):
    def expect_error(self, content: bytes, line_no: int):
        self.src.write_bytes(content)
        self.dst.write_bytes(b"OLD TARGET")
        with self.assertRaises(InputFormatError) as cm:
            external_stable_sort(self.src, self.dst, max_rows_in_memory=2)
        self.assertEqual(cm.exception.line_no, line_no, cm.exception)
        self.assertEqual(self.dst.read_bytes(), b"OLD TARGET")
        self.assertEqual(self.leftovers(), [])

    def test_rejections(self):
        good = b'{"timestamp":1,"payload":"x"}\n'
        cases = {
            "bad utf8": good + b'{"timestamp":1,"payload":"\xff"}\n',
            "unknown key": good + b'{"timestamp":1,"payload":"x","z":1}\n',
            "missing key": good + b'{"timestamp":1}\n',
            "duplicate key": good + b'{"timestamp":1,"timestamp":2,"payload":"x"}\n',
            "empty line": good + b"\n" + good,
            "bool ts": good + b'{"timestamp":true,"payload":"x"}\n',
            "float ts": good + b'{"timestamp":1.0,"payload":"x"}\n',
            "exp ts": good + b'{"timestamp":1e3,"payload":"x"}\n',
            "string ts": good + b'{"timestamp":"1","payload":"x"}\n',
            "ts too big": good + b'{"timestamp":9223372036854775808,"payload":"x"}\n',
            "ts too small": good + b'{"timestamp":-9223372036854775809,"payload":"x"}\n',
            "payload not str": good + b'{"timestamp":1,"payload":5}\n',
            "invalid json": good + b'{"timestamp":1,"payload":"x"\n',
            "array": good + b'[1,"x"]\n',
            "nan": good + b'{"timestamp":NaN,"payload":"x"}\n',
            "lone surrogate": good + b'{"timestamp":1,"payload":"\\ud800"}\n',
            "bom": good + b'\xef\xbb\xbf{"timestamp":1,"payload":"x"}\n',
        }
        for name, content in cases.items():
            with self.subTest(name):
                self.expect_error(content, 2)
        self.expect_error(b"\n", 1)

    def test_line_length_4096_vs_4097(self):
        pad = 4096 - len(b'{"timestamp":1,"payload":""}')
        ok = b'{"timestamp":1,"payload":"' + b"a" * pad + b'"}'
        self.assertEqual(len(ok), 4096)
        self.src.write_bytes(ok + b"\n" + ok)                  # 最終行は LF なし
        external_stable_sort(self.src, self.dst)
        self.assertEqual(len(self.dst.read_bytes().splitlines()), 2)
        too_long = b'{"timestamp":1,"payload":"' + b"a" * (pad + 1) + b'"}'
        self.expect_error(ok + b"\n" + too_long + b"\n", 2)
        self.expect_error(ok + b"\n" + too_long, 2)            # LF なしの最終行でも同じ

    def test_arguments(self):
        self.src.write_bytes(b'{"timestamp":1,"payload":"x"}\n')
        for bad in (0, 4097, -1):
            with self.assertRaises(ValueError):
                external_stable_sort(self.src, self.dst, max_rows_in_memory=bad)
        for bad in (True, 2.0, "8"):
            with self.assertRaises(TypeError):
                external_stable_sort(self.src, self.dst, max_rows_in_memory=bad)
        with self.assertRaises(ValueError):
            external_stable_sort(self.src, self.src)
        with self.assertRaises(ValueError):
            external_stable_sort(self.src, self.dir / "sub" / ".." / "in.jsonl")
        self.assertEqual(self.src.read_bytes(), b'{"timestamp":1,"payload":"x"}\n')


class MultiPass(TmpDir):
    def test_more_than_32_runs(self):
        rng = random.Random(11)
        for n, max_rows, want_runs, want_passes in ((5000, 10, 500 + 16, 2), (1500, 1, 1500 + 47 + 2, 3)):
            rows = [(rng.randrange(0, 50), f"v{i}") for i in range(n)]
            write_rows(self.src, rows)
            s = external_stable_sort(self.src, self.dst, max_rows_in_memory=max_rows)
            data = self.dst.read_bytes()
            self.assertEqual(data, reference_bytes(rows))
            payloads = [json.loads(x)["payload"] for x in data.splitlines()]
            self.assertEqual(sorted(payloads), sorted(f"v{i}" for i in range(n)))   # ちょうど 1 回ずつ
            self.assertEqual(len(set(payloads)), n)
            self.assertEqual((s.runs_created, s.merge_passes), (want_runs, want_passes))
            self.assertEqual(s.max_open_runs, 32)
            self.assertEqual(self.leftovers(), [])
            print(f"\n[Q07] n={n} max_rows={max_rows}: {s}")


class FaultInjection(TmpDir):
    def test_each_fault_point(self):
        rng = random.Random(5)
        rows = [(rng.randrange(100), f"q{i}") for i in range(3000)]
        write_rows(self.src, rows)
        sibling = self.dir / "unrelated.txt"
        sibling.write_bytes(b"keep me")
        results = []
        for point in ("read", "write", "merge", "replace"):
            for occurrence in (1, 2, 5):
                for target_exists in (True, False):
                    if self.dst.exists():
                        self.dst.unlink()
                    if target_exists:
                        self.dst.write_bytes(b"OLD")
                        before = os.stat(self.dst)
                    seen = {"n": 0}

                    def hook(p, point=point, occurrence=occurrence, seen=seen):
                        if p == point:
                            seen["n"] += 1
                            if seen["n"] == occurrence:
                                raise OSError(f"injected {p}#{occurrence}")
                    try:
                        external_stable_sort(self.src, self.dst, max_rows_in_memory=10, fault_hook=hook)
                        fired = False
                    except OSError as e:
                        fired = "injected" in str(e)
                    if point == "replace" and occurrence > 1:
                        self.assertFalse(fired)                 # replace は 1 回だけ → 2 回目以降は発火しない
                        self.assertEqual(self.dst.read_bytes(), reference_bytes(rows))
                        continue
                    self.assertTrue(fired, (point, occurrence))
                    if target_exists:
                        self.assertEqual(self.dst.read_bytes(), b"OLD")
                        self.assertEqual(os.stat(self.dst).st_mtime_ns, before.st_mtime_ns)
                    else:
                        self.assertFalse(self.dst.exists())
                    self.assertEqual(self.leftovers(), [])        # 自分の一時フォルダーは除去済み
                    self.assertEqual(sibling.read_bytes(), b"keep me")
                    results.append((point, occurrence, target_exists))
        print(f"\n[Q07] fault injections fired and verified: {len(results)}")

    def test_bounded_memory_measurement(self):
        rng = random.Random(9)
        rows = [(rng.randrange(-1000, 1000), "x" * 40 + str(i)) for i in range(200_000)]
        write_rows(self.src, rows)
        size = self.src.stat().st_size
        del rows
        tracemalloc.start()
        try:
            s = external_stable_sort(self.src, self.dst, max_rows_in_memory=1000)
            _, peak = tracemalloc.get_traced_memory()
        finally:
            tracemalloc.stop()
        self.assertLessEqual(s.peak_rows_in_memory, 1000)
        self.assertLessEqual(s.max_open_runs, 32)
        self.assertLess(peak, 4 * 1024 * 1024)
        print(f"\n[Q07] 200,000 rows ({size} bytes) max_rows=1000: {s}, tracemalloc peak {peak} bytes")


if __name__ == "__main__":
    unittest.main(verbosity=2)
```

#### 5. 検証結果（期待値・実測値・実施状況）

参照は、全件をメモリに読み、Python の安定ソート（`sorted`）で並べたものです（テスト側だけで使用）。出力はバイト単位で比較しました。

| 必須テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 空 | 空の出力。行 0・run 0・段数 0 | 一致 | 実施 |
| 1 行 | 出力 1 行。run 1・段数 1 | 一致 | 実施 |
| 4096 行 | run 1、常駐の最大 4096 件 | 一致 | 実施 |
| 4097 行 | run 2、同時に開く run 2 | 一致 | 実施 |
| 100 万行 | 参照ソートとバイト単位で一致。初回 run 245 + 中間 run 8 = 253、段数 2、同時に開く run 32 | `SortStats(total_rows=1000000, runs_created=253, merge_passes=2, max_open_runs=32, peak_rows_in_memory=4096)`、25.9 秒（入力 43,969,694 バイト） | 実施 |
| timestamp の最小値・最大値 | -2**63 が先頭、2**63-1 が末尾 | 一致 | 実施 |
| 同じ timestamp が異なる run にまたがる | max_rows=100 で 30 run にまたがる timestamp 0 の行が、元の行順のまま | 一致 | 実施 |
| 最終行に LF がない・キーの順序 | 受理する。出力は `{"timestamp":…,"payload":…}` + LF の正規形 | 一致 | 実施 |
| 不正な UTF-8・未知のキー・空行 | InputFormatError（行番号付き）。既存の target は不変、一時フォルダーは残らない | 一致 | 実施 |
| bool・float の timestamp | `true`・`1.0`・`1e3` を拒否。文字列・範囲外（±2**63 の外側）も拒否 | 一致 | 実施 |
| 長すぎる 1 行 | ちょうど 4096 バイトの行は受理。4097 バイトは LF の有無にかかわらず拒否 | 一致 | 実施 |
| 不正な JSON など | 閉じ括弧の欠落・配列・NaN・重複キー・キーの欠落・payload が非 str・BOM・不対サロゲート（`\ud800`）を拒否（各ケースの 2 行目に置き、行番号 2 を確認） | 全 17 種 + 空行のみのファイル、一致 | 実施 |
| 引数 | max_rows が 0・4097・-1 は ValueError、True・2.0・"8" は TypeError。source==target（別表記の `sub/../in.jsonl` も含む）は ValueError で、入力は不変 | 一致 | 実施 |
| run が 32 を超える多段マージ | 5,000 行・max_rows=10 → run 500+16、段数 2。1,500 行・max_rows=1 → run 1500+47+2、段数 3。全レコードがちょうど 1 回ずつ現れ、参照ソートと一致 | 一致（両方とも max_open_runs=32） | 実施 |
| 各障害注入位置 | read・write・merge（それぞれ 1・2・5 回目）と replace（1 回目）で例外を注入。既存の target がある場合は内容も mtime も不変、ない場合は作られない。自分の一時フォルダーは除去済み、隣のファイルは不変 | 20 通り、すべて一致（replace の 2・5 回目は発火点がないので正常に完了） | 実施 |
| 最大常駐件数・最大同時ファイル数 | 20 万行（15,967,213 バイト）を max_rows=1000 で並べる。常駐の最大 ≤ 1000、同時に開く run ≤ 32、tracemalloc のピーク < 4 MiB | 常駐 1000、run 32、ピーク 2,262,290 バイト | 実施 |

#### 6. 技術的な正当性・設計判断

- **安定性：** 各レコードを (timestamp, 元の物理行番号, payload) の組で扱い、run の整列もマージもこの組の辞書順で行います。行番号は一意なので、比較が payload に及ぶことはありません。等しい timestamp の行は、どの run に分かれても元の行順になります（多段マージでも、行番号を run ファイルに保存して引き継ぎます）。
- **有界メモリ：**
  - run を作る時のバッファーは最大 `max_rows_in_memory` 件です。
  - マージ中のヒープは、開いている run ごとに先頭の 1 件だけです（≤ 32）。
  - 入力は 1 行ずつ読み、1 行は 4097 バイトまでしか読みません。
  - 中間 run は、`<q`（timestamp）・`<Q`（行番号）・`<I`（長さ）と UTF-8 の payload からなるバイナリ形式です。
  - ピーク = O(max_rows + 開いている run 数) レコードです（実測で確認）。
- **多段マージ：** run が 32 を超える間は、32 個ずつまとめて中間 run にマージし（1 段）、使い終えた run を削除します。最後に 32 個以下を最終出力へマージします（段数に含めます）。同時に開く run は常に 32 以下です。
- **原子的な公開：**
  - 中間ファイルと最終の一時出力は、`target.parent` にある `.<name>.sort-XXXX`（mkdtemp）の中だけに作ります。
  - 最終出力は flush + `os.fsync` の後、`os.replace` で target を置き換えます（同じファイルシステム内の名前の置換）。POSIX では、親ディレクトリの fsync も行います。
  - どの段階で失敗しても、一時フォルダーを `rmtree` で除去し、既存の target には触れません。ジェネレーターは明示的に close し、Windows でファイルハンドルが残って削除できない問題を避けました。
- **入力検証：**
  - `json.JSONDecoder` の `object_pairs_hook` で、重複キーの検出と配列との区別を行います。`parse_float` と `parse_constant` で、float・NaN・Infinity を拒否します。
  - timestamp は `type(v) is int` の符号付き 64 ビット、payload は str で、UTF-8 で符号化できる（不対サロゲートを含まない）ことを確認します。
- **計算量：** O(N log max_rows + N log 32 × 段数)。I/O は 1 段あたり入力 1 回分です。

#### 7. 未実施項目・制約・残課題

- 実際の電源断の試験は未実施です。Windows では、ディレクトリエントリの fsync が標準 API でできないため、名前の置換の永続化は OS に依存します。
- 契約により、別のプロセスが target を同時に変更する場合は対象外です。
- Linux・macOS での実行（POSIX の親ディレクトリ fsync の経路を含む）はしていません。
- 一時フォルダーの除去は、`rmtree(ignore_errors=True)` によるベストエフォートです。テストでは、すべての障害注入の後に残骸が 0 件であることを確認しました。

---

## Q08【地獄級】ZIP Slip・symlink・ZIP bombを拒否する安全な展開器

**試験の狙い：** 不正アーカイブの検証、パストラバーサル、容量上限、TOCTOU境界、原子的な成果物公開。

### 元コード（危険・不完全）

```python
from zipfile import ZipFile
def extract_bad(zip_path, dest):
    with ZipFile(zip_path) as z:
        z.extractall(dest)  # 非信頼入力と容量・上書きに無制限
```

### 要求仕様

1. `safe_extract(zip_path: Path, destination: Path) -> ExtractStats`を実装。destinationは**存在しないパス**で、その親ディレクトリは実在する通常のディレクトリとする。親ディレクトリは他プロセスや攻撃者が処理中に変更しないという試験契約。
2. 受け付けるZIPエントリ名はASCIIの`A-Z a-z 0-9 _ . - /`のみ。絶対パス、先頭/末尾スラッシュの不整合（ディレクトリ名末尾`/`は許す）、`\\`、ドライブ名、空のパス成分、`.`、`..`、先頭`/`を拒否。ファイル・ディレクトリ名を区別し、casefold後の同名重複、ファイルとその配下パスの衝突を拒否。
3. ZIPのUnix symlink・特殊ファイルを`ZipInfo.external_attr`のモードで検出して拒否。通常ファイルとディレクトリのみ許可。外部リンク・既存ファイルへの上書きは禁止。ZIPエントリ数は最大128、各展開ファイルは最大2MiB、総展開データは最大16MiB。1ファイルのサイズが0でなければ`file_size/compress_size <= 100`（compress_size=0なら不許可）。
4. 展開を始める前に中央ディレクトリで確認できる全条件を**全件検証**。実展開中もchunk最大65536バイトで復号し、実測展開量の個別/全体上限を再確認。ZIPのメタデータが虚偽でも上限突破・中途公開しない。`ZipFile.open`とストリームコピーだけを使用（extractallは禁止）。
5. destinationの親に自分専用の一時ディレクトリを作り、全件成功した後にdestinationへディレクトリ名を変更して公開。実行中にdestinationが新規出現した場合は明示エラーとし既存内容を上書きしない。途中失敗時は自身の一時ディレクトリだけ除去。
6. 暗号化ZIP、ZIP内symlink、特殊ファイル、ZIP64は許可しない。OSの権限・外部プロセスによる名前変更・意図的な競合攻撃まで完全に防ぐことは要求しない（明示した親ディレクトリ契約内での安全性を評価）。

### 必須検証

1. 入れ子3段の通常ZIP、空ファイル、最大128件、2MiB/16MiB境界、元データとのバイト一致。
2. `../x`、`/abs`、`a//b`、`a/./b`、`a/../b`、`C:/x`、バックスラッシュ、同名と大小文字衝突、ファイル/ディレクトリ衝突、symlink/特殊ファイルを拒否。
3. 高圧縮率のZIP bomb、虚偽サイズ、総量超過、エントリ数超過、破損CRC、暗号化ZIP、ZIP64の拒否。
4. 展開途中例外・容量超過でもdestination不在、別の既存ディレクトリ不変、自身の一時ディレクトリ除去、正常時だけ完成状態が公開。

### ★★FREYJAの回答【Q08・記入欄】

> **以下が専用回答スペース。** 問題本文、元コード、要求仕様、必須検証は改変しない。各項目の例示テキストを実際の内容へ置き換えること。

#### 1. 根本原因・危険性・影響

**元コード `extract_bad` の問題点**

| # | 問題箇所 | 発生条件 | 危険性・実害 |
| --- | --- | --- | --- |
| 1 | `extractall` を非信頼の書庫に使う | 名前が `../x`・`/abs`・`C:/x` など | 展開先の外へ書き込む（ZIP Slip）。zipfile 自身の名前の無害化は、展開先の外への書込みを防ぐだけで、名前の衝突・大小文字の重複・Windows のデバイス名は防がない |
| 2 | 容量の上限がない | ZIP bomb（高圧縮率・巨大な宣言サイズ） | ディスクとメモリの枯渇 |
| 3 | 既存ファイルの上書き | dest が既存、同じ名前の重複 | ユーザーデータの破壊 |
| 4 | エントリの種別を検査しない | symlink・デバイスなどの特殊ファイル（`external_attr`） | 他のツールで再展開・利用する際のリンク経由のアクセスなど |
| 5 | 途中失敗の後始末・原子性がない | 破損・CRC エラー・例外 | 途中まで展開した状態が dest に公開されて残る |
| 6 | 暗号化・ZIP64・エントリ数を制限しない | — | パスワードの要求、巨大な書庫、大量のエントリによる負荷 |

**修正方針**
1. 展開前に、中央ディレクトリの全エントリを検証します（名前の文字種・成分、大小文字を無視した重複、ファイルとディレクトリの衝突、種別、暗号化、ZIP64、件数・サイズ・圧縮率）。
2. destination の親に専用の一時ディレクトリを作り、`ZipFile.open` + 64 KiB 単位のストリームコピーで展開します（実際の展開量の上限も再確認します）。
3. 全件が成功したときだけ、`os.rename` で destination に公開します。失敗時は、自分の一時ディレクトリだけを削除します。

#### 2. 完全な修正版Pythonコード

```python
"""Q08: ZIP Slip・symlink・ZIP bomb を拒否する安全な展開器（zipfile 標準モジュールのみ・extractall 不使用）。"""
from __future__ import annotations

import os
import re
import shutil
import stat
import struct
import tempfile
import zipfile
import zlib
from dataclasses import dataclass
from pathlib import Path
from typing import Callable

__all__ = ["safe_extract", "ExtractStats", "UnsafeArchiveError",
           "MAX_ENTRIES", "MAX_FILE_BYTES", "MAX_TOTAL_BYTES", "MAX_RATIO", "CHUNK"]

MAX_ENTRIES = 128
MAX_FILE_BYTES = 2 * 1024 * 1024
MAX_TOTAL_BYTES = 16 * 1024 * 1024
MAX_RATIO = 100
CHUNK = 65536
_NAME_RE = re.compile(r"[A-Za-z0-9_./-]+")
# Windows で特別扱いされる名前（デバイス名）。末尾の '.' も Windows では除去されて別名と衝突するため拒否する。
_WIN_RESERVED = {"con", "prn", "aux", "nul", "conin$", "conout$",
                 *(f"com{i}" for i in range(10)), *(f"lpt{i}" for i in range(10))}


class UnsafeArchiveError(Exception):
    """アーカイブを安全に展開できない（不正な名前・種別・サイズ・暗号化・ZIP64・破損など）。"""


@dataclass(frozen=True)
class ExtractStats:
    files: int
    directories: int
    total_bytes: int


@dataclass(frozen=True)
class _Entry:
    info: zipfile.ZipInfo
    parts: tuple[str, ...]
    is_dir: bool


def _has_zip64_locator(path: Path) -> bool:
    """中央ディレクトリ終端（EOCD）の直前に ZIP64 終端ロケーター（PK\\x06\\x07）があるか。"""
    size = path.stat().st_size
    with open(path, "rb") as f:
        n = min(size, 22 + 65535 + 20)
        f.seek(size - n)
        tail = f.read(n)
    pos = tail.rfind(b"PK\x05\x06")
    return pos >= 20 and tail[pos - 20:pos - 16] == b"PK\x06\x07"


def _extra_has_zip64(extra: bytes) -> bool:
    i = 0
    while i + 4 <= len(extra):
        hid, ln = struct.unpack_from("<HH", extra, i)
        if hid == 0x0001:
            return True
        i += 4 + ln
    return False


def _local_extra(f, info: zipfile.ZipInfo) -> bytes:
    """ローカルヘッダーの拡張フィールドを読む（中央ディレクトリと別に ZIP64 指定がないか確認する）。"""
    f.seek(info.header_offset)
    head = f.read(30)
    if len(head) != 30 or head[:4] != b"PK\x03\x04":
        raise UnsafeArchiveError(f"bad local header for {info.orig_filename!r}")
    name_len, extra_len = struct.unpack_from("<HH", head, 26)
    f.seek(name_len, os.SEEK_CUR)
    extra = f.read(extra_len)
    if len(extra) != extra_len:
        raise UnsafeArchiveError(f"truncated local header for {info.orig_filename!r}")
    return extra


def _split_name(info: zipfile.ZipInfo) -> tuple[tuple[str, ...], bool]:
    raw = info.orig_filename
    if raw != info.filename:          # NUL 切り詰めや '\\'→'/' 変換が起きた名前は拒否
        raise UnsafeArchiveError(f"entry name was altered by normalization: {raw!r}")
    if not raw or _NAME_RE.fullmatch(raw) is None:
        raise UnsafeArchiveError(f"entry name has disallowed characters: {raw!r}")
    is_dir = raw.endswith("/")
    core = raw[:-1] if is_dir else raw
    if not core or core.startswith("/"):
        raise UnsafeArchiveError(f"absolute or empty entry name: {raw!r}")
    parts = tuple(core.split("/"))
    for p in parts:
        if p == "" or p in (".", ".."):
            raise UnsafeArchiveError(f"empty, '.' or '..' path component: {raw!r}")
        if p.endswith(".") or p.split(".")[0].casefold() in _WIN_RESERVED:
            raise UnsafeArchiveError(f"component not portable/safe on Windows: {raw!r}")
    return parts, is_dir


def _check_kind(info: zipfile.ZipInfo, is_dir: bool) -> None:
    mode = (info.external_attr >> 16) & 0xFFFF
    dos = info.external_attr & 0xFF
    if mode:
        fmt = stat.S_IFMT(mode)
        if fmt == stat.S_IFLNK:
            raise UnsafeArchiveError(f"symlink entry rejected: {info.orig_filename!r}")
        if fmt not in (stat.S_IFREG, stat.S_IFDIR, 0):
            raise UnsafeArchiveError(f"special file entry rejected: {info.orig_filename!r}")
        if fmt == stat.S_IFDIR and not is_dir:
            raise UnsafeArchiveError(f"directory mode without trailing '/': {info.orig_filename!r}")
        if fmt == stat.S_IFREG and is_dir:
            raise UnsafeArchiveError(f"regular-file mode with trailing '/': {info.orig_filename!r}")
    if info.external_attr & 0x400:      # Windows の再解析ポイント（リンク）属性
        raise UnsafeArchiveError(f"reparse-point entry rejected: {info.orig_filename!r}")
    if dos & 0x10 and not is_dir:
        raise UnsafeArchiveError(f"DOS directory attribute without trailing '/': {info.orig_filename!r}")


def _validate(zf: zipfile.ZipFile, zip_path: Path) -> list[_Entry]:
    infos = zf.infolist()
    if len(infos) > MAX_ENTRIES:
        raise UnsafeArchiveError(f"too many entries: {len(infos)} > {MAX_ENTRIES}")
    with open(zip_path, "rb") as f:
        local_extras = {id(i): _local_extra(f, i) for i in infos}
    entries: list[_Entry] = []
    seen: set[str] = set()
    file_keys: set[str] = set()
    dir_keys: set[str] = set()
    total = 0
    for info in infos:
        if info.flag_bits & 0x1 or info.flag_bits & 0x40:
            raise UnsafeArchiveError(f"encrypted entry rejected: {info.orig_filename!r}")
        if (_extra_has_zip64(info.extra) or _extra_has_zip64(local_extras[id(info)])
                or info.extract_version == 45 or info.file_size > 0xFFFFFFFF or info.compress_size > 0xFFFFFFFF):
            raise UnsafeArchiveError(f"ZIP64 entry rejected: {info.orig_filename!r}")
        parts, is_dir = _split_name(info)
        _check_kind(info, is_dir)
        key = "/".join(parts).casefold()
        if key in seen:
            raise UnsafeArchiveError(f"duplicate entry (case-insensitive): {info.orig_filename!r}")
        seen.add(key)
        for k in range(1, len(parts)):                       # 暗黙の親ディレクトリ
            dir_keys.add("/".join(parts[:k]).casefold())
        if is_dir:
            if info.file_size != 0:
                raise UnsafeArchiveError(f"directory entry with data: {info.orig_filename!r}")
            dir_keys.add(key)
        else:
            if info.file_size > MAX_FILE_BYTES:
                raise UnsafeArchiveError(f"file too large: {info.orig_filename!r}")
            if info.file_size > 0:
                if info.compress_size == 0 or info.file_size > MAX_RATIO * info.compress_size:
                    raise UnsafeArchiveError(f"compression ratio exceeds {MAX_RATIO}: {info.orig_filename!r}")
            total += info.file_size
            if total > MAX_TOTAL_BYTES:
                raise UnsafeArchiveError("total uncompressed size exceeds 16 MiB")
            file_keys.add(key)
        entries.append(_Entry(info, parts, is_dir))
    clash = file_keys & dir_keys
    if clash:
        raise UnsafeArchiveError(f"file/directory path conflict: {sorted(clash)[0]!r}")
    return entries


def safe_extract(zip_path: Path, destination: Path, *,
                 fault_hook: Callable[[str], None] | None = None) -> ExtractStats:
    """fault_hook は検証用の障害注入点（"file"：各ファイルの書込み前、"publish"：公開直前）。"""
    zip_path = Path(zip_path)
    destination = Path(os.path.abspath(destination))
    parent = destination.parent
    if os.path.lexists(destination):
        raise FileExistsError(f"destination already exists: {destination}")
    if not parent.is_dir() or parent.is_symlink():
        raise FileNotFoundError(f"destination parent is not an existing regular directory: {parent}")
    try:
        if _has_zip64_locator(zip_path):
            raise UnsafeArchiveError("ZIP64 archives are not allowed")
        zf = zipfile.ZipFile(zip_path)
    except (zipfile.BadZipFile, OSError, ValueError) as exc:
        raise UnsafeArchiveError(f"cannot read archive: {exc}") from exc
    with zf:
        entries = _validate(zf, zip_path)                    # 展開前に中央ディレクトリを全件検証
        work = tempfile.mkdtemp(prefix=f".{destination.name}.extract-", dir=parent)
        try:
            stats = _extract_all(zf, entries, Path(work), fault_hook)
            if fault_hook is not None:
                fault_hook("publish")
            if os.path.lexists(destination):
                raise FileExistsError(f"destination appeared during extraction: {destination}")
            os.rename(work, destination)                     # Windows: 既存なら失敗（上書きしない）
            return stats
        except BaseException:
            shutil.rmtree(work, ignore_errors=True)          # 自分の一時ディレクトリだけを除去
            raise


def _extract_all(zf: zipfile.ZipFile, entries: list[_Entry], work: Path, hook) -> ExtractStats:
    root = os.path.realpath(work)
    dirs: set[tuple[str, ...]] = set()
    for e in entries:
        for k in range(1, len(e.parts) + (1 if e.is_dir else 0)):
            dirs.add(e.parts[:k])
    for d in sorted(dirs, key=len):
        os.mkdir(os.path.join(work, *d))
    total = 0
    files = 0
    for e in entries:
        if e.is_dir:
            continue
        if hook is not None:
            hook("file")
        out_path = os.path.join(work, *e.parts)
        if os.path.commonpath([root, os.path.realpath(out_path)]) != root:   # 多重防御
            raise UnsafeArchiveError(f"path escapes destination: {e.info.orig_filename!r}")
        written = 0
        try:
            with zf.open(e.info) as src, open(out_path, "xb") as dst:      # "x": 既存ファイルを上書きしない
                while True:
                    chunk = src.read(CHUNK)
                    if not chunk:
                        break
                    written += len(chunk)
                    if written > e.info.file_size or written > MAX_FILE_BYTES:
                        raise UnsafeArchiveError(f"entry larger than declared/allowed: {e.info.orig_filename!r}")
                    if total + written > MAX_TOTAL_BYTES:
                        raise UnsafeArchiveError("total extracted size exceeds 16 MiB")
                    dst.write(chunk)
        except (zipfile.BadZipFile, zipfile.LargeZipFile, zlib.error, NotImplementedError, RuntimeError,
                EOFError) as exc:
            raise UnsafeArchiveError(f"cannot extract {e.info.orig_filename!r}: {exc}") from exc
        if written != e.info.file_size:
            raise UnsafeArchiveError(f"size mismatch for {e.info.orig_filename!r}")
        total += written
        files += 1
    return ExtractStats(files=files, directories=len(dirs), total_bytes=total)
```

#### 3. 実行・ビルド方法と使用環境

```powershell
# 作業フォルダー（試験専用の一時領域）: scratchpad\py\q08 に q08.py（実装）と test_q08.py（テスト）を置く
cd <scratchpad>\py\q08
python3.12 --version            # Python 3.12.15
python3.12 -X dev -m unittest -v test_q08
# テスト（Normal クラス）を修正した後は、そのクラスだけを再実行した
python3.12 -X dev -m unittest -v test_q08.Normal
```

**使用環境（全問共通）**

- Python：**CPython 3.12.15**（`3.12.15 (main, Oct  9 2026) [MSC v.1944 64 bit (AMD64)]`）。この PC に既にあった `python3.12` コマンドで実行しました。新しいバージョンへの変更・pip によるパッケージ導入はしていません。
- ライブラリ：Python 標準ライブラリだけです。テストは `unittest` で行いました。
- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド、x86-64）。
- 実行方法：問題ごとの作業フォルダー（`scratchpad\py\qNN`。試験専用の一時領域）に、実装 `qNN.py` とテスト `test_qNN.py` を置いて実行しました。`-X dev`（開発モード：ResourceWarning や asyncio の検査を有効化）を付けています。
- 未実施の環境：Linux・macOS・ARM での実行はしていません。

- **テスト用の ZIP：** すべてテストの中で zipfile を使って生成しました（虚偽サイズ・CRC 破損・暗号化フラグ・ZIP64 ロケーターはバイト列を直接書き換えて作成）。`scratchpad\py\q08\testdata\q08-*` の一時フォルダーだけを使い、終了時に削除しました（残ったものは 0 件）。
- **1 回目の全体実行：** `Ran 9 tests in 1.748s`。8 件合格、1 件エラー（テストデータの誤り。下記で説明）。
- **修正後の再実行（Normal クラス）：** `Ran 2 tests in 0.593s` → `OK`、終了コード 0。
- 他の 7 件は修正していないテストで、1 回目の実行で合格しています。成功済みの無関係なテストは再実行していません。

#### 4. 実際に使用したテストコード

```python
"""Q08 のテスト。ZIP はすべてテスト内で生成し、q08/testdata 配下の専用一時フォルダーだけを使う。"""
from __future__ import annotations

import os
import random
import shutil
import stat
import struct
import tempfile
import unittest
import zipfile
from pathlib import Path

from q08 import (MAX_FILE_BYTES, MAX_TOTAL_BYTES, ExtractStats, UnsafeArchiveError, safe_extract)

HERE = Path(__file__).resolve().parent
BASE = HERE / "testdata"
DIR_ATTR = ((stat.S_IFDIR | 0o755) << 16) | 0x10
FILE_ATTR = (stat.S_IFREG | 0o644) << 16


def add(zf, name, data=b"", *, attr=None, method=zipfile.ZIP_STORED, system=3, extra=b""):
    zi = zipfile.ZipInfo("placeholder")
    zi.filename = name                        # 正規化を通さず生の名前を書く
    zi.orig_filename = name
    zi.compress_type = method
    zi.create_system = system
    zi.external_attr = attr if attr is not None else (DIR_ATTR if name.endswith("/") else FILE_ATTR)
    zi.extra = extra
    zf.writestr(zi, data)


def tree(root: Path):
    out = {}
    for dirpath, dirnames, filenames in os.walk(root):
        rel = os.path.relpath(dirpath, root).replace(os.sep, "/")
        for d in dirnames:
            out[(rel + "/" + d).lstrip("./") + "/"] = None
        for f in filenames:
            out[(rel + "/" + f).lstrip("./")] = (Path(dirpath) / f).read_bytes()
    return out


def patch_entry(path: Path, index: int, *, crc=None, usize=None, flags_or=0):
    """index 番目のエントリの中央ディレクトリ（と対応するローカルヘッダー）を書き換える。"""
    data = bytearray(path.read_bytes())
    cpos = -1
    for _ in range(index + 1):
        cpos = data.index(b"PK\x01\x02", cpos + 1)
    lpos = struct.unpack_from("<I", data, cpos + 42)[0]
    for base, off_flags, off_crc, off_usize in ((cpos, 8, 16, 24), (lpos, 6, 14, 22)):
        flags = struct.unpack_from("<H", data, base + off_flags)[0]
        struct.pack_into("<H", data, base + off_flags, flags | flags_or)
        if crc is not None:
            struct.pack_into("<I", data, base + off_crc, crc)
        if usize is not None:
            struct.pack_into("<I", data, base + off_usize, usize)
    path.write_bytes(bytes(data))


class TmpDir(unittest.TestCase):
    def setUp(self):
        BASE.mkdir(exist_ok=True)
        self.dir = Path(tempfile.mkdtemp(prefix="q08-", dir=BASE))
        self.zip = self.dir / "in.zip"
        self.dest = self.dir / "out"
        self.other = self.dir / "existing"                # 別の既存ディレクトリ（不変であること）
        self.other.mkdir()
        (self.other / "keep.txt").write_bytes(b"keep")

    def tearDown(self):
        shutil.rmtree(self.dir)

    def leftovers(self):
        return [p.name for p in self.dir.iterdir() if ".extract-" in p.name]

    def assert_rejected(self, exc=UnsafeArchiveError, **kw):
        with self.assertRaises(exc) as cm:
            safe_extract(self.zip, self.dest, **kw)
        self.assertFalse(os.path.lexists(self.dest))
        self.assertEqual(self.leftovers(), [])
        self.assertEqual(tree(self.other), {"keep.txt": b"keep"})
        return cm.exception


class Normal(TmpDir):
    def test_nested_three_levels_and_empty_file(self):
        content = {
            "top.txt": b"top", "a/": None, "a/one.bin": bytes(range(256)) * 10, "a/b/": None,
            "a/b/two.txt": random.Random(2).randbytes(3000),   # 圧縮率 100 以下になるデータ
            "a/b/c/": None, "a/b/c/three.txt": b"3",
            "a/b/c/empty.txt": b"", "x/y/z/implicit.txt": b"parents not listed",
        }
        with zipfile.ZipFile(self.zip, "w") as zf:
            for name, data in content.items():
                add(zf, name, b"" if data is None else data,
                    method=zipfile.ZIP_DEFLATED if data and len(data) > 100 else zipfile.ZIP_STORED)
        st = safe_extract(self.zip, self.dest)
        got = tree(self.dest)
        for name, data in content.items():
            self.assertEqual(got[name], data, name)
        self.assertIn("x/y/", got)
        self.assertEqual(st, ExtractStats(files=6, directories=6, total_bytes=sum(len(d) for d in content.values() if d)))
        self.assertEqual(self.leftovers(), [])

    def test_128_entries_and_size_boundaries(self):
        with zipfile.ZipFile(self.zip, "w") as zf:
            for i in range(128):
                add(zf, f"f{i:03d}.txt", str(i).encode())
        self.assertEqual(safe_extract(self.zip, self.dest).files, 128)
        shutil.rmtree(self.dest)
        with zipfile.ZipFile(self.zip, "w") as zf:
            for i in range(129):
                add(zf, f"f{i:03d}.txt", b"x")
        self.assert_rejected()
        big = os.urandom(MAX_FILE_BYTES)                     # 2 MiB ちょうど（非圧縮性データ）
        with zipfile.ZipFile(self.zip, "w") as zf:
            add(zf, "big.bin", big)
        safe_extract(self.zip, self.dest)
        self.assertEqual((self.dest / "big.bin").read_bytes(), big)
        shutil.rmtree(self.dest)
        with zipfile.ZipFile(self.zip, "w") as zf:
            add(zf, "big.bin", big + b"!")
        self.assert_rejected()
        parts = [os.urandom(MAX_FILE_BYTES) for _ in range(8)]   # 合計 16 MiB ちょうど
        with zipfile.ZipFile(self.zip, "w") as zf:
            for i, p in enumerate(parts):
                add(zf, f"d/p{i}.bin", p)
        st = safe_extract(self.zip, self.dest)
        self.assertEqual(st.total_bytes, MAX_TOTAL_BYTES)
        for i, p in enumerate(parts):
            self.assertEqual((self.dest / "d" / f"p{i}.bin").read_bytes(), p)
        shutil.rmtree(self.dest)
        with zipfile.ZipFile(self.zip, "w") as zf:
            for i, p in enumerate(parts):
                add(zf, f"d/p{i}.bin", p)
            add(zf, "d/one-more.bin", b"1")                  # 16 MiB + 1 バイト
        self.assert_rejected()


class BadNames(TmpDir):
    def test_name_rejections(self):
        cases = {
            "dotdot": ["../x"], "abs": ["/abs"], "double slash": ["a//b"], "dot": ["a/./b"],
            "inner dotdot": ["a/../b"], "drive": ["C:/x"], "backslash": ["a\\b"],
            "backslash dotdot": ["..\\evil"], "same name": ["x", "x"], "case collision": ["Readme", "README"],
            "file vs subpath": ["a", "a/b"], "file vs dir": ["a", "A/"], "dir then file child ok?": None,
            "leading dot slash": ["./a"], "only slash": ["/"], "space": ["a b"], "non-ascii": ["é.txt"],
            "colon": ["a:b"], "nul": ["a\x00b"], "windows device": ["CON"], "device ext": ["d/nul.txt"],
            "trailing dot": ["a."], "empty": [""],
        }
        for label, names in cases.items():
            if names is None:
                continue
            with self.subTest(label):
                with zipfile.ZipFile(self.zip, "w") as zf:
                    for n in names:
                        add(zf, n, b"" if n.endswith("/") else b"data")
                self.assert_rejected()

    def test_symlink_and_special_files(self):
        for label, mode in (("symlink", stat.S_IFLNK | 0o777), ("fifo", stat.S_IFIFO | 0o644),
                            ("char", stat.S_IFCHR | 0o644), ("block", stat.S_IFBLK | 0o644),
                            ("socket", stat.S_IFSOCK | 0o644)):
            with self.subTest(label):
                with zipfile.ZipFile(self.zip, "w") as zf:
                    add(zf, "ok.txt", b"fine")
                    add(zf, "link", b"/etc/passwd", attr=mode << 16)
                self.assert_rejected()
        with zipfile.ZipFile(self.zip, "w") as zf:                 # ディレクトリ属性なのに末尾 / なし
            add(zf, "notdir", b"", attr=DIR_ATTR)
        self.assert_rejected()
        with zipfile.ZipFile(self.zip, "w") as zf:                 # Windows の再解析ポイント属性
            add(zf, "rp", b"x", attr=0x400, system=0)
        self.assert_rejected()


class BombsAndCorruption(TmpDir):
    def test_high_ratio_bomb_and_oversize(self):
        with zipfile.ZipFile(self.zip, "w") as zf:
            add(zf, "zeros.bin", bytes(MAX_FILE_BYTES), method=zipfile.ZIP_DEFLATED)
        info = zipfile.ZipFile(self.zip).infolist()[0]
        self.assertGreater(info.file_size / info.compress_size, 100)
        e = self.assert_rejected()
        self.assertIn("ratio", str(e))
        with zipfile.ZipFile(self.zip, "w") as zf:
            add(zf, "huge.bin", bytes(50 * 1024 * 1024), method=zipfile.ZIP_DEFLATED)
        self.assert_rejected()
        with zipfile.ZipFile(self.zip, "w") as zf:                 # 0 バイトのファイルは比率検査の対象外
            add(zf, "a.txt", b"")
        safe_extract(self.zip, self.dest)
        shutil.rmtree(self.dest)

    def test_lying_sizes_and_bad_crc(self):
        data = bytes(1024 * 1024)
        with zipfile.ZipFile(self.zip, "w") as zf:
            add(zf, "ok.txt", b"first")
            add(zf, "liar.bin", data, method=zipfile.ZIP_DEFLATED)
        csize = zipfile.ZipFile(self.zip).infolist()[1].compress_size
        patch_entry(self.zip, 1, usize=min(50 * csize, MAX_FILE_BYTES))   # 実際より小さい展開サイズを申告
        self.assert_rejected()
        rnd = os.urandom(1000)
        with zipfile.ZipFile(self.zip, "w") as zf:
            add(zf, "short.bin", rnd)
        patch_entry(self.zip, 0, usize=50_000)                 # 実データより大きい展開サイズを申告
        self.assert_rejected()
        with zipfile.ZipFile(self.zip, "w") as zf:
            add(zf, "a.txt", b"hello world")
            add(zf, "b.txt", b"second")
        patch_entry(self.zip, 1, crc=0xDEADBEEF)               # CRC 破損
        self.assert_rejected()
        with zipfile.ZipFile(self.zip, "w") as zf:
            add(zf, "d.bin", os.urandom(5000), method=zipfile.ZIP_DEFLATED)
        raw = bytearray(self.zip.read_bytes())
        lpos = raw.index(b"PK\x03\x04")
        raw[lpos + 30 + len("d.bin") + 10] ^= 0xFF            # 圧縮データ本体の破損
        self.zip.write_bytes(bytes(raw))
        self.assert_rejected()

    def test_encrypted_and_zip64(self):
        with zipfile.ZipFile(self.zip, "w") as zf:
            add(zf, "secret.txt", b"x" * 100)
        patch_entry(self.zip, 0, flags_or=0x1)                 # 暗号化フラグ
        self.assertIn("encrypted", str(self.assert_rejected()))
        with zipfile.ZipFile(self.zip, "w") as zf:            # 中央ディレクトリの ZIP64 拡張フィールド
            add(zf, "z.txt", b"abc", extra=struct.pack("<HHQQ", 0x0001, 16, 3, 3))
        self.assertIn("ZIP64", str(self.assert_rejected()))
        with zipfile.ZipFile(self.zip, "w") as zf:            # zipfile の force_zip64
            with zf.open("f.txt", "w", force_zip64=True) as w:
                w.write(b"data")
        self.assertIn("ZIP64", str(self.assert_rejected()))
        # ZIP64 終端レコード + ロケーターを EOCD の直前に挿入した書庫
        with zipfile.ZipFile(self.zip, "w") as zf:
            add(zf, "a.txt", b"abc")
        raw = self.zip.read_bytes()
        eocd = raw.rindex(b"PK\x05\x06")
        _, _, _, n_disk, n_total, cd_size, cd_off, _ = struct.unpack_from("<IHHHHIIH", raw, eocd)
        z64 = struct.pack("<IQHHIIQQQQ", 0x06064B50, 44, 45, 45, 0, 0, n_disk, n_total, cd_size, cd_off)
        loc = struct.pack("<IIQI", 0x07064B50, 0, eocd, 1)
        self.zip.write_bytes(raw[:eocd] + z64 + loc + raw[eocd:])
        self.assertIn("ZIP64", str(self.assert_rejected()))


class Atomicity(TmpDir):
    def make(self, n=5):
        with zipfile.ZipFile(self.zip, "w") as zf:
            for i in range(n):
                add(zf, f"dir/f{i}.txt", f"file {i}".encode() * 100)

    def test_exception_mid_extraction(self):
        self.make()
        count = {"n": 0}

        def hook(point):
            if point == "file":
                count["n"] += 1
                if count["n"] == 3:
                    raise RuntimeError("injected mid-extraction failure")
        with self.assertRaises(RuntimeError):
            safe_extract(self.zip, self.dest, fault_hook=hook)
        self.assertFalse(os.path.lexists(self.dest))
        self.assertEqual(self.leftovers(), [])
        self.assertEqual(tree(self.other), {"keep.txt": b"keep"})
        st = safe_extract(self.zip, self.dest)                  # 正常時だけ完成状態が公開される
        self.assertEqual(st.files, 5)
        self.assertEqual(sorted(tree(self.dest)), ["dir/"] + [f"dir/f{i}.txt" for i in range(5)])

    def test_destination_preexisting_or_appearing(self):
        self.make()
        self.dest.mkdir()
        (self.dest / "mine.txt").write_bytes(b"user data")
        with self.assertRaises(FileExistsError):
            safe_extract(self.zip, self.dest)
        self.assertEqual(tree(self.dest), {"mine.txt": b"user data"})
        shutil.rmtree(self.dest)

        def appear(point):
            if point == "publish":
                self.dest.mkdir()
                (self.dest / "racer.txt").write_bytes(b"other process")
        with self.assertRaises(FileExistsError):
            safe_extract(self.zip, self.dest, fault_hook=appear)
        self.assertEqual(tree(self.dest), {"racer.txt": b"other process"})   # 上書きしない
        self.assertEqual(self.leftovers(), [])
        with self.assertRaises(FileNotFoundError):
            safe_extract(self.zip, self.dir / "missing-parent" / "out")
        self.zip.write_bytes(b"not a zip")
        shutil.rmtree(self.dest)
        self.assert_rejected()


if __name__ == "__main__":
    unittest.main(verbosity=2)
```

#### 5. 検証結果（期待値・実測値・実施状況）

すべての拒否ケースで、`assert_rejected` が次の 4 点も確認しています。
- destination が存在しない
- 自分の一時ディレクトリ（`.out.extract-*`）が残っていない
- 別の既存ディレクトリ `existing/keep.txt` が不変
- 期待した例外の型である

| 必須テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 入れ子 3 段の通常の ZIP | `a/b/c/` までの明示ディレクトリと、`x/y/z/` の暗黙の親ディレクトリ。stored・deflated の混在。全ファイルが元データとバイト一致。ExtractStats(files=6, directories=6) | 一致 | 実施（修正後） |
| 空ファイル | 0 バイトのファイルを展開する（圧縮率の検査の対象外） | 一致 | 実施 |
| 最大 128 件 / 129 件 | 128 件は展開、129 件は拒否 | 一致 | 実施 |
| 2 MiB の境界 | ちょうど 2 MiB は展開してバイト一致、2 MiB+1 は拒否 | 一致 | 実施 |
| 16 MiB の境界 | 2 MiB × 8 = 16 MiB ちょうどは展開してバイト一致、+1 バイトのファイルを加えると拒否 | 一致 | 実施 |
| 不正な名前 | `../x`・`/abs`・`a//b`・`a/./b`・`a/../b`・`C:/x`・`a\b`・`..\evil`・同名・大小文字の衝突（Readme/README）・ファイルとその配下（a と a/b）・ファイルとディレクトリ（a と A/）・`./a`・`/`・空白・非 ASCII・`:`・NUL・`CON`・`d/nul.txt`・末尾 `.`・空の名前 | 22 種すべて拒否 | 実施 |
| symlink・特殊ファイル | `external_attr` のモードが S_IFLNK・S_IFIFO・S_IFCHR・S_IFBLK・S_IFSOCK のものは拒否。ディレクトリのモードで末尾 `/` がないもの、Windows の再解析ポイント属性も拒否 | 7 種すべて拒否 | 実施 |
| 高圧縮率の ZIP bomb | 2 MiB のゼロを deflate（圧縮率 100 超）は「ratio」で拒否。50 MiB のゼロはサイズ上限で拒否 | 一致 | 実施 |
| 虚偽のサイズ | 宣言の展開サイズが実際より小さい（1 MiB → 約 50 倍の値）、または実データより大きい（1000 → 50,000）書庫は、展開中に検出して拒否 | 一致 | 実施 |
| 総量超過・エントリ数超過 | 上の 16 MiB+1、129 件 | 一致 | 実施 |
| 破損した CRC・圧縮データ | 中央ディレクトリの CRC 書換えは展開中に BadZipFile → 拒否。deflate データの破損も拒否 | 一致 | 実施 |
| 暗号化 ZIP | 汎用フラグのビット 0 を立てた書庫は「encrypted」で拒否 | 一致 | 実施 |
| ZIP64 | 中央ディレクトリの ZIP64 拡張フィールド、`force_zip64` で書いた書庫（ローカルヘッダーの ZIP64 拡張と extract_version 45）、EOCD の直前に ZIP64 終端レコードとロケーターを挿入した書庫は、いずれも「ZIP64」で拒否 | 3 種すべて一致 | 実施 |
| 展開途中の例外 | 3 つ目のファイルの前で例外を注入すると、destination は不在、一時ディレクトリは除去、別の既存ディレクトリは不変。その後の正常な展開だけが完成状態で公開される | 一致 | 実施 |
| destination が既存・途中で出現 | 既存なら FileExistsError で中身は不変。公開の直前に別のディレクトリが出現したら FileExistsError で、その中身（racer.txt）は上書きしない。親が存在しなければ FileNotFoundError。ZIP でないファイルは拒否 | 一致 | 実施 |

**自律修正**
- **実装（実行前の見直し）：** zipfile の `force_zip64` で書いた書庫は、中央ディレクトリに ZIP64 拡張フィールドを持ちません。そこで、各ローカルヘッダーの拡張フィールドと extract_version（45）も検査するようにしました。また、ZIP64 ロケーターの検査を ZipFile を開く前に移しました。
- **テスト：** 通常ケースのデータ `b"two" * 1000` は圧縮率が 100 を超えるため、拒否されるのが仕様どおりの正しい挙動でした。圧縮率 100 以下になる乱数データに差し替えました。
- その編集の際、コメントの位置の誤りで 2 項目がコメントになっていたため、それも直しました。Normal クラスを再実行し、合格しました。

#### 6. 技術的な正当性・設計判断

- **名前の検証：**
  - `ZipInfo.orig_filename` を使い、それが zipfile による正規化後の `filename` と異なれば拒否します。zipfile は Windows で `\` を `/` に変換し、NUL 以降を切り捨てるため、この比較で `a\b` や NUL を確実に検出できます。
  - 文字種 `[A-Za-z0-9_./-]+`、絶対パス、空・`.`・`..` の成分を拒否します。末尾 `/` はディレクトリの場合だけ許します。
  - `C:` は `:` が許可外の文字なので拒否されます。
  - 加えて、Windows で別名に化ける名前（末尾 `.`、`CON`・`NUL`・`COM1` などのデバイス名）も拒否します。これは、上書き禁止と展開先の外への書込み禁止を Windows で保証するための追加の防御です。
- **衝突：** casefold した完全パスの重複を拒否します。ファイルのパスの集合と、ディレクトリ（明示 + 全ファイル・ディレクトリの親）のパスの集合が交わる場合も拒否します（a と a/b、a と A/）。
- **サイズと圧縮率：**
  - 事前の検査：各 `file_size ≤ 2 MiB`、合計 ≤ 16 MiB、`file_size ≤ 100 × compress_size`。compress_size が 0 で中身がある場合は拒否します。
  - 展開中：64 KiB ずつ読み、実際の展開量が宣言値・2 MiB・16 MiB を超えた時点で中止します。最後に宣言値との一致も確認します。zipfile は宣言サイズで読取りを止め、CRC を照合するので、虚偽のメタデータは BadZipFile・EOFError となり、拒否されます。
- **原子的な公開：**
  - 一時ディレクトリは `mkdtemp(dir=親)` で作ります（同じファイルシステム上）。ファイルは `open(..., "xb")`（既存なら失敗）で作り、`realpath` での展開先内の確認も重ねます。
  - 全件が成功した後、destination の不在を再確認してから `os.rename` します。Windows の `os.rename` は、宛先が存在すると失敗し、上書きしません。
  - 例外時は `rmtree(自分の一時ディレクトリ)` だけを行い、他のパスには触れません。
- **種別：** `external_attr >> 16` の S_IFMT が通常ファイル・ディレクトリ・0（DOS 由来）以外なら拒否します。名前の末尾 `/` とモードの矛盾、DOS のディレクトリ属性の矛盾も拒否します。

#### 7. 未実施項目・制約・残課題

- 仕様どおり、OS の権限や外部プロセスによる意図的な競合（公開直前の確認から rename までのわずかな時間）は、親ディレクトリの契約の範囲外です。
- POSIX の `os.rename` は、宛先が空のディレクトリだと置き換えてしまいます（Windows では失敗します）。そのため、POSIX では直前の存在確認に依存します（未試験）。
- Linux・macOS での実行はしていません。
- Windows のデバイス名・末尾 `.` の拒否は、問題文に明記はありませんが、上書き禁止を保証するために追加しました。

---

## Q09【地獄級】RFC 6901ポインタと原子的JSON Patch適用器

**試験の狙い：** 深いコピー、JSONポインタ、リスト操作、テスト失敗時の全体ロールバック、型厳密比較。

### 元コード（危険・不完全）

```python
def patch_bad(doc, ops):
    for op in ops:
        doc[op["path"]] = op["value"]  # RFC6901未対応・途中失敗で入力を破壊
    return doc
```

### 要求仕様

1. `apply_patch(document: JSONValue, operations: list[dict]) -> JSONValue` を実装。JSONValueはNone/bool/int/str/list/dict（キーはstr）だけ。float・tuple・その他の型・循環参照は禁止。入力は最大ネスト64、ノード数100000、operations最大1000。
2. サポートする操作は`add`, `remove`, `replace`, `move`, `copy`, `test`の6つ。各操作の仕様はRFC 6902の対応する意味に基づくが、本試験の**明示条件を優先**する。JSONポインタはRFC 6901の`~0`→`~`、`~1`→`/`を正しく復号し、未知エスケープを拒否。
3. dictキー操作は文字列完全一致。listインデックスは正規10進表記のみ（`0` または非ゼロから始まる数字列）、負数と先頭0禁止。add時だけ末尾を示す`-`を許可。addはindex<=len、remove/replace/testはindex<len。中間パスがない場合は失敗。
4. トップレベルパス`''`は文書全体を表し、add/replace/test/copyの宛先として許可。**remove/moveの移動元がrootの場合は `PatchError`**。moveで宛先が移動元自身の子孫になれば拒否。moveでは移動元を削除後、宛先パスを評価（リストのインデックスずれを含める）。
5. `test`ではboolとintの違いを区別する型厳密比較を使用。dictのキー順は比較しない、listの順序は比較する。値のコピーはdeepcopy相当の独立した所有権を保証し、処理後の入力オブジェクトと操作内のvalueを変更しても結果は変わらない。
6. 6操作の途中で1つでも構文・パス・test失敗があれば`PatchError(index, reason)`を送出し、**元documentとoperationsを一切変更しない**。一時コピーを最初に生成し成功した全操作だけを返す。root置換の場合も元documentは不変。
7. 再帰の最大深さ制限を守る。未知フィールドは各操作の仕様外フィールドとして拒否。必要な値/パスキーが欠けていれば拒否。

### 必須検証

1. add/remove/replace/copy/move/testをdict/listで各10ケース以上、root置換、リスト`-`追加、リスト前方へのmoveによるインデックス変化。
2. キー`a/b`・`~`・空文字・Unicode、`~0`・`~1`・不正`~2`、空ポインタ、先頭0、負index、存在しない中間パス。
3. 1000個のops中999個目で失敗し入力documentがバイト同等の内容のまま、別のネストとリストも変更なし。
4. 1とTrueのtestは不一致、dictのキー順は無関係、異なるリスト順は不一致。独立参照実装とランダムの適法/不正パッチ各1万件を比較。

### ★★FREYJAの回答【Q09・記入欄】

> **以下が専用回答スペース。** 問題本文、元コード、要求仕様、必須検証は改変しない。各項目の例示テキストを実際の内容へ置き換えること。

#### 1. 根本原因・危険性・影響

**元コード `patch_bad` の問題点**

| # | 問題箇所 | 発生条件 | 危険性・実害 |
| --- | --- | --- | --- |
| 1 | `doc[op["path"]]` でポインターを解釈しない | `/a/b` などの階層パス | トップレベルに `"/a/b"` というキーができる。リストでは TypeError |
| 2 | 入力 document を直接書き換える | 途中の操作で失敗 | 呼出し元の文書が中途半端な状態で壊れる（原子性がない） |
| 3 | `op` の種類を見ない | remove・move・copy・test | すべてを代入として扱い、意味が異なる |
| 4 | `value` をコピーしない | 結果や操作を後で変更 | 結果と操作・入力が同じオブジェクトを共有し、相互に影響する |
| 5 | 検証がない | float・循環参照・深い入れ子・未知のフィールド | 不正な JSON、無限再帰、資源の枯渇 |
| 6 | 比較・エスケープの規則がない | `~0`/`~1`、1 と True | RFC 6901・6902 の意味と一致しない |

**修正方針**
- 最初に document を非再帰で検査しながら、独立した作業コピーを作ります（型・循環・ネスト 64・ノード数 100000）。
- RFC 6901 のポインター（`~0`/`~1` を復号し、未知のエスケープは拒否）と、正規の 10 進のリスト添字で、6 操作をその作業コピーにだけ適用します。
- 1 つでも失敗すれば `PatchError(index, reason)` を送出し、元の document と operations には一切触れません。
- 操作ごとに、結果のネスト・ノード数の上限も検査します。

#### 2. 完全な修正版Pythonコード

```python
"""Q09: RFC 6901 JSON ポインターと原子的な JSON Patch（add/remove/replace/move/copy/test）。

* 入力 document の型・循環・ネスト（最大 64）・ノード数（最大 100000）を最初に検査しつつ、
  非再帰で独立した作業コピーを作る。全操作はその作業コピーにだけ適用し、全件成功したときだけ返す。
  失敗時は PatchError(index, reason) を送出し、元の document と operations には一切触れない。
* ネストの定義: スカラーは 0、コンテナーは 1 + 子の最大値（[] は 1）。操作後の文書にも同じ上限を課す。
* PatchError.index は失敗した操作の 0 始まりの番号。document 自体・operations 全体の不正は index = -1。
"""
from __future__ import annotations

import re
from typing import Any, Union

__all__ = ["apply_patch", "PatchError", "MAX_DEPTH", "MAX_NODES", "MAX_OPERATIONS"]

JSONValue = Union[None, bool, int, str, list, dict]
MAX_DEPTH = 64
MAX_NODES = 100_000
MAX_OPERATIONS = 1000
_INDEX_RE = re.compile(r"0|[1-9][0-9]*")
_FIELDS = {
    "add": frozenset({"op", "path", "value"}),
    "replace": frozenset({"op", "path", "value"}),
    "test": frozenset({"op", "path", "value"}),
    "remove": frozenset({"op", "path"}),
    "move": frozenset({"op", "from", "path"}),
    "copy": frozenset({"op", "from", "path"}),
}
_SCALARS = (type(None), bool, int, str)


class PatchError(Exception):
    def __init__(self, index: int, reason: str) -> None:
        super().__init__(f"operation {index}: {reason}")
        self.index = index
        self.reason = reason


class _Fail(Exception):
    """内部用: 操作番号を付けて PatchError に変換する。"""


def _copy_checked(value: Any) -> tuple[Any, int, int]:
    """非再帰で検査しながら独立コピーを作る。戻り値: (コピー, ノード数, ネストの深さ)。"""
    t = type(value)
    if t in _SCALARS:
        return value, 1, 0
    if t is not list and t is not dict:
        raise _Fail(f"unsupported type {t.__name__}")
    root = [] if t is list else {}
    nodes = 1
    max_depth = 1
    on_path: set[int] = {id(value)}
    # スタック要素: (元のコンテナー, コピー先, 子のイテレーター, 深さ)
    stack = [(value, root, iter(value.items()) if t is dict else iter(value), 1)]
    while stack:
        src, dst, it, depth = stack[-1]
        try:
            item = next(it)
        except StopIteration:
            on_path.discard(id(src))
            stack.pop()
            continue
        if type(dst) is dict:
            key, child = item
            if type(key) is not str:
                raise _Fail("object keys must be str")
        else:
            key, child = None, item
        ct = type(child)
        nodes += 1
        if nodes > MAX_NODES:
            raise _Fail(f"more than {MAX_NODES} nodes")
        if ct in _SCALARS:
            c = child
        elif ct is list or ct is dict:
            if id(child) in on_path:
                raise _Fail("circular reference")
            if depth + 1 > MAX_DEPTH:
                raise _Fail(f"nesting deeper than {MAX_DEPTH}")
            max_depth = max(max_depth, depth + 1)
            c = [] if ct is list else {}
            on_path.add(id(child))
            stack.append((child, c, iter(child.items()) if ct is dict else iter(child), depth + 1))
        else:
            raise _Fail(f"unsupported type {ct.__name__}")
        if key is None:
            dst.append(c)
        else:
            dst[key] = c
    return root, nodes, max_depth


def _measure(value: Any) -> tuple[int, int]:
    """作業コピー内の部分木のノード数とネスト（非再帰。作業コピーは検査済みで循環なし）。"""
    nodes = 0
    max_depth = 0
    stack = [(value, 0)]
    while stack:
        v, d = stack.pop()
        nodes += 1
        if type(v) is list:
            max_depth = max(max_depth, d + 1)
            stack.extend((c, d + 1) for c in v)
        elif type(v) is dict:
            max_depth = max(max_depth, d + 1)
            stack.extend((c, d + 1) for c in v.values())
    return nodes, max_depth


def _parse_pointer(ptr: Any) -> list[str]:
    if type(ptr) is not str:
        raise _Fail("pointer must be a string")
    if ptr == "":
        return []
    if not ptr.startswith("/"):
        raise _Fail(f"pointer must be empty or start with '/': {ptr!r}")
    tokens = []
    for raw in ptr[1:].split("/"):
        out = []
        i = 0
        while i < len(raw):
            ch = raw[i]
            if ch == "~":
                nxt = raw[i + 1] if i + 1 < len(raw) else ""
                if nxt == "0":
                    out.append("~")
                elif nxt == "1":
                    out.append("/")
                else:
                    raise _Fail(f"invalid escape in pointer {ptr!r}")
                i += 2
            else:
                out.append(ch)
                i += 1
        tokens.append("".join(out))
    return tokens


def _list_index(token: str, length: int, *, allow_end: bool) -> int:
    if allow_end and token == "-":
        return length
    if _INDEX_RE.fullmatch(token) is None:
        raise _Fail(f"invalid array index {token!r}")
    idx = int(token)
    if idx > length or (idx == length and not allow_end):
        raise _Fail(f"array index {idx} out of range (len {length})")
    return idx


def _resolve(root: Any, tokens: list[str]) -> Any:
    cur = root
    for tok in tokens:
        if type(cur) is dict:
            if tok not in cur:
                raise _Fail(f"path segment {tok!r} not found")
            cur = cur[tok]
        elif type(cur) is list:
            cur = cur[_list_index(tok, len(cur), allow_end=False)]
        else:
            raise _Fail(f"cannot descend into a scalar at {tok!r}")
    return cur


def _strict_equal(a: Any, b: Any) -> bool:
    stack = [(a, b)]
    while stack:
        x, y = stack.pop()
        if type(x) is not type(y):
            return False                          # bool と int、1 と True を区別する
        if type(x) is dict:
            if x.keys() != y.keys():              # キー順は無関係
                return False
            stack.extend((x[k], y[k]) for k in x)
        elif type(x) is list:
            if len(x) != len(y):
                return False
            stack.extend(zip(x, y))               # 順序は比較する
        elif x != y:
            return False
    return True


class _Doc:
    def __init__(self, root: Any, nodes: int) -> None:
        self.root = root
        self.nodes = nodes

    def _check_limits(self, added_nodes: int, removed_nodes: int, depth_at: int, value_depth: int) -> None:
        if depth_at + value_depth > MAX_DEPTH:
            raise _Fail(f"result would nest deeper than {MAX_DEPTH}")
        if self.nodes + added_nodes - removed_nodes > MAX_NODES:
            raise _Fail(f"result would exceed {MAX_NODES} nodes")

    def add(self, tokens: list[str], value: Any, nodes: int, depth: int) -> None:
        if not tokens:
            self._check_limits(nodes, self.nodes, 0, depth)
            self.root, self.nodes = value, nodes
            return
        parent = _resolve(self.root, tokens[:-1])
        last = tokens[-1]
        if type(parent) is dict:
            old = _measure(parent[last])[0] if last in parent else 0
            self._check_limits(nodes, old, len(tokens), depth)
            parent[last] = value
            self.nodes += nodes - old
        elif type(parent) is list:
            idx = _list_index(last, len(parent), allow_end=True)
            self._check_limits(nodes, 0, len(tokens), depth)
            parent.insert(idx, value)
            self.nodes += nodes
        else:
            raise _Fail("parent of target is not a container")

    def remove(self, tokens: list[str]) -> Any:
        if not tokens:
            raise _Fail("cannot remove the document root")
        parent = _resolve(self.root, tokens[:-1])
        last = tokens[-1]
        if type(parent) is dict:
            if last not in parent:
                raise _Fail(f"member {last!r} not found")
            value = parent.pop(last)
        elif type(parent) is list:
            value = parent.pop(_list_index(last, len(parent), allow_end=False))
        else:
            raise _Fail("parent of target is not a container")
        self.nodes -= _measure(value)[0]
        return value

    def replace(self, tokens: list[str], value: Any, nodes: int, depth: int) -> None:
        if not tokens:
            self._check_limits(nodes, self.nodes, 0, depth)
            self.root, self.nodes = value, nodes
            return
        parent = _resolve(self.root, tokens[:-1])
        last = tokens[-1]
        if type(parent) is dict:
            if last not in parent:
                raise _Fail(f"member {last!r} not found")
            key: Any = last
        elif type(parent) is list:
            key = _list_index(last, len(parent), allow_end=False)
        else:
            raise _Fail("parent of target is not a container")
        old = _measure(parent[key])[0]
        self._check_limits(nodes, old, len(tokens), depth)
        parent[key] = value
        self.nodes += nodes - old


def apply_patch(document: JSONValue, operations: list[dict]) -> JSONValue:
    try:
        work, nodes, _depth = _copy_checked(document)
    except _Fail as exc:
        raise PatchError(-1, f"invalid document: {exc}") from None
    if type(operations) is not list:
        raise PatchError(-1, "operations must be a list")
    if len(operations) > MAX_OPERATIONS:
        raise PatchError(-1, f"more than {MAX_OPERATIONS} operations")
    doc = _Doc(work, nodes)
    for index, op in enumerate(operations):
        try:
            _apply_one(doc, op)
        except _Fail as exc:
            raise PatchError(index, str(exc)) from None
    return doc.root


def _apply_one(doc: _Doc, op: Any) -> None:
    if type(op) is not dict:
        raise _Fail("operation must be an object")
    name = op.get("op")
    if type(name) is not str or name not in _FIELDS:
        raise _Fail(f"unknown op {name!r}")
    allowed = _FIELDS[name]
    keys = set(op)
    if keys - allowed:
        raise _Fail(f"unknown field(s) {sorted(k for k in keys - allowed if isinstance(k, str))!r}"
                    if all(isinstance(k, str) for k in keys) else "non-string field name")
    if allowed - keys:
        raise _Fail(f"missing field(s) {sorted(allowed - keys)}")
    path = _parse_pointer(op["path"])
    if name in ("add", "replace", "test"):
        value, vnodes, vdepth = _copy_checked(op["value"])   # 操作内の value とは独立した所有権
        if name == "add":
            doc.add(path, value, vnodes, vdepth)
        elif name == "replace":
            doc.replace(path, value, vnodes, vdepth)
        else:
            if not _strict_equal(_resolve(doc.root, path), value):
                raise _Fail("test failed")
    elif name == "remove":
        doc.remove(path)
    elif name == "copy":
        src = _parse_pointer(op["from"])
        value, vnodes, vdepth = _copy_checked(_resolve(doc.root, src))
        doc.add(path, value, vnodes, vdepth)
    else:  # move
        src = _parse_pointer(op["from"])
        if not src:
            raise _Fail("cannot move the document root")
        if len(path) > len(src) and path[:len(src)] == src:
            raise _Fail("cannot move a value into its own descendant")
        _resolve(doc.root, src)                  # 移動元の存在確認（削除前）
        value = doc.remove(src)                  # 移動元を削除してから宛先を評価する
        vnodes, vdepth = _measure(value)
        doc.add(path, value, vnodes, vdepth)
```

#### 3. 実行・ビルド方法と使用環境

```powershell
# 作業フォルダー（試験専用の一時領域）: scratchpad\py\q09 に q09.py（実装）と test_q09.py（テスト）を置く
cd <scratchpad>\py\q09
python3.12 --version            # Python 3.12.15
python3.12 -X dev -m unittest -v test_q09
```

**使用環境（全問共通）**

- Python：**CPython 3.12.15**（`3.12.15 (main, Oct  9 2026) [MSC v.1944 64 bit (AMD64)]`）。この PC に既にあった `python3.12` コマンドで実行しました。新しいバージョンへの変更・pip によるパッケージ導入はしていません。
- ライブラリ：Python 標準ライブラリだけです。テストは `unittest` で行いました。
- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド、x86-64）。
- 実行方法：問題ごとの作業フォルダー（`scratchpad\py\qNN`。試験専用の一時領域）に、実装 `qNN.py` とテスト `test_qNN.py` を置いて実行しました。`-X dev`（開発モード：ResourceWarning や asyncio の検査を有効化）を付けています。
- 未実施の環境：Linux・macOS・ARM での実行はしていません。

- **実行結果：** `Ran 11 tests in 5.952s` → `OK`、終了コード 0（1 回目の実行で全件合格）。

#### 4. 実際に使用したテストコード

```python
"""Q09 のテスト。RefPatch は copy.deepcopy と再帰で書いた独立参照実装（上限検査なし・小さい入力用）。"""
from __future__ import annotations

import copy
import json
import random
import re
import time
import unittest

from q09 import MAX_DEPTH, MAX_NODES, PatchError, apply_patch


def strict_eq(a, b):
    if type(a) is not type(b):
        return False
    if isinstance(a, dict):
        return a.keys() == b.keys() and all(strict_eq(a[k], b[k]) for k in a)
    if isinstance(a, list):
        return len(a) == len(b) and all(strict_eq(x, y) for x, y in zip(a, b))
    return a == b


def fingerprint(x):
    """キー順も含めたバイト列表現（入力が変更されていないことの確認用）。"""
    return json.dumps(x, ensure_ascii=False).encode("utf-8")


# ------------------------------------------------------------------ 参照実装
class RefError(Exception):
    def __init__(self, index):
        self.index = index


def ref_tokens(p):
    if not isinstance(p, str):
        raise ValueError
    if p == "":
        return []
    if p[0] != "/":
        raise ValueError
    toks = p.split("/")[1:]
    out = []
    for t in toks:
        if re.search(r"~(?![01])", t):
            raise ValueError
        out.append(t.replace("~1", "/").replace("~0", "~"))
    return out


def ref_idx(tok, n, end_ok):
    if end_ok and tok == "-":
        return n
    if not re.fullmatch(r"0|[1-9]\d*", tok):
        raise ValueError
    i = int(tok)
    if i > n or (i == n and not end_ok):
        raise ValueError
    return i


def ref_get(doc, toks):
    for t in toks:
        if isinstance(doc, dict):
            doc = doc[t]
        elif isinstance(doc, list):
            doc = doc[ref_idx(t, len(doc), False)]
        else:
            raise ValueError
    return doc


def ref_apply(doc, ops):
    doc = copy.deepcopy(doc)
    fields = {"add": {"op", "path", "value"}, "replace": {"op", "path", "value"}, "test": {"op", "path", "value"},
              "remove": {"op", "path"}, "move": {"op", "from", "path"}, "copy": {"op", "from", "path"}}
    for i, op in enumerate(ops):
        try:
            if not isinstance(op, dict) or op.get("op") not in fields or set(op) != fields[op["op"]]:
                raise ValueError
            kind = op["op"]
            path = ref_tokens(op["path"])

            def put(d, toks, v):
                if not toks:
                    return v
                parent = ref_get(d, toks[:-1])
                if isinstance(parent, dict):
                    parent[toks[-1]] = v
                elif isinstance(parent, list):
                    parent.insert(ref_idx(toks[-1], len(parent), True), v)
                else:
                    raise ValueError
                return d

            def delete(d, toks):
                if not toks:
                    raise ValueError
                parent = ref_get(d, toks[:-1])
                if isinstance(parent, dict):
                    return parent.pop(toks[-1])
                if isinstance(parent, list):
                    return parent.pop(ref_idx(toks[-1], len(parent), False))
                raise ValueError

            if kind == "add":
                doc = put(doc, path, copy.deepcopy(op["value"]))
            elif kind == "remove":
                delete(doc, path)
            elif kind == "replace":
                ref_get(doc, path)
                if path:
                    delete(doc, path)
                    parent = ref_get(doc, path[:-1])
                    if isinstance(parent, list):
                        parent.insert(int(path[-1]), copy.deepcopy(op["value"]))
                    else:
                        parent[path[-1]] = copy.deepcopy(op["value"])
                else:
                    doc = copy.deepcopy(op["value"])
            elif kind == "test":
                if not strict_eq(ref_get(doc, path), op["value"]):
                    raise ValueError
            elif kind == "copy":
                v = copy.deepcopy(ref_get(doc, ref_tokens(op["from"])))
                doc = put(doc, path, v)
            else:
                src = ref_tokens(op["from"])
                if not src or (len(path) > len(src) and path[:len(src)] == src):
                    raise ValueError
                v = delete(doc, src)
                doc = put(doc, path, v)
        except (ValueError, KeyError, IndexError, TypeError):
            raise RefError(i)
    return doc


class Operations(unittest.TestCase):
    def ok(self, doc, ops, want):
        before_doc, before_ops = fingerprint(doc), fingerprint(ops)
        got = apply_patch(doc, ops)
        self.assertTrue(strict_eq(got, want), (ops, got, want))
        self.assertTrue(strict_eq(ref_apply(doc, ops), want))
        self.assertEqual(fingerprint(doc), before_doc)
        self.assertEqual(fingerprint(ops), before_ops)
        return got

    def bad(self, doc, ops, index, ref_too=True):
        before_doc, before_ops = fingerprint(doc), fingerprint(ops)
        with self.assertRaises(PatchError) as cm:
            apply_patch(doc, ops)
        self.assertEqual(cm.exception.index, index, cm.exception)
        self.assertEqual(fingerprint(doc), before_doc)
        self.assertEqual(fingerprint(ops), before_ops)
        if ref_too:                      # 参照実装は値の型検査（float・tuple）をしないので該当ケースは除外
            with self.assertRaises(RefError):
                ref_apply(doc, ops)

    def test_add(self):
        d = {"a": 1, "l": [1, 2]}
        self.ok(d, [{"op": "add", "path": "/b", "value": 2}], {"a": 1, "l": [1, 2], "b": 2})
        self.ok(d, [{"op": "add", "path": "/a", "value": [3]}], {"a": [3], "l": [1, 2]})
        self.ok(d, [{"op": "add", "path": "/l/0", "value": 0}], {"a": 1, "l": [0, 1, 2]})
        self.ok(d, [{"op": "add", "path": "/l/2", "value": 3}], {"a": 1, "l": [1, 2, 3]})
        self.ok(d, [{"op": "add", "path": "/l/-", "value": {"x": None}}], {"a": 1, "l": [1, 2, {"x": None}]})
        self.ok(d, [{"op": "add", "path": "", "value": [True]}], [True])
        self.ok([], [{"op": "add", "path": "/-", "value": 1}, {"op": "add", "path": "/-", "value": 2}], [1, 2])
        self.ok({}, [{"op": "add", "path": "/", "value": "empty key"}], {"": "empty key"})
        self.bad(d, [{"op": "add", "path": "/l/3", "value": 1}], 0)
        self.bad(d, [{"op": "add", "path": "/x/y", "value": 1}], 0)
        self.bad(d, [{"op": "add", "path": "/a/b", "value": 1}], 0)
        self.bad(d, [{"op": "add", "path": "/l/01", "value": 1}], 0)
        self.bad(d, [{"op": "add", "path": "b", "value": 1}], 0)
        self.bad(d, [{"op": "add", "path": "/b"}], 0)

    def test_remove(self):
        d = {"a": 1, "l": [1, 2, 3], "n": {"x": {"y": 1}}}
        self.ok(d, [{"op": "remove", "path": "/a"}], {"l": [1, 2, 3], "n": {"x": {"y": 1}}})
        self.ok(d, [{"op": "remove", "path": "/l/0"}], {"a": 1, "l": [2, 3], "n": {"x": {"y": 1}}})
        self.ok(d, [{"op": "remove", "path": "/l/2"}], {"a": 1, "l": [1, 2], "n": {"x": {"y": 1}}})
        self.ok(d, [{"op": "remove", "path": "/n/x/y"}], {"a": 1, "l": [1, 2, 3], "n": {"x": {}}})
        self.ok(d, [{"op": "remove", "path": "/n"}], {"a": 1, "l": [1, 2, 3]})
        self.bad(d, [{"op": "remove", "path": ""}], 0)
        self.bad(d, [{"op": "remove", "path": "/l/3"}], 0)
        self.bad(d, [{"op": "remove", "path": "/l/-"}], 0)
        self.bad(d, [{"op": "remove", "path": "/zz"}], 0)
        self.bad(d, [{"op": "remove", "path": "/l/-1"}], 0)
        self.bad(d, [{"op": "remove", "path": "/a", "value": 1}], 0)       # 仕様外フィールド

    def test_replace(self):
        d = {"a": 1, "l": [1, 2], "b": False}
        self.ok(d, [{"op": "replace", "path": "/a", "value": "s"}], {"a": "s", "l": [1, 2], "b": False})
        self.ok(d, [{"op": "replace", "path": "/l/1", "value": [9]}], {"a": 1, "l": [1, [9]], "b": False})
        self.ok(d, [{"op": "replace", "path": "/l/0", "value": None}], {"a": 1, "l": [None, 2], "b": False})
        self.ok(d, [{"op": "replace", "path": "", "value": 5}], 5)
        self.ok(d, [{"op": "replace", "path": "/b", "value": True}], {"a": 1, "l": [1, 2], "b": True})
        self.bad(d, [{"op": "replace", "path": "/zz", "value": 1}], 0)
        self.bad(d, [{"op": "replace", "path": "/l/2", "value": 1}], 0)
        self.bad(d, [{"op": "replace", "path": "/l/-", "value": 1}], 0)
        self.bad(d, [{"op": "replace", "path": "/a", "value": 1.5}], 0, ref_too=False)
        self.bad(d, [{"op": "replace", "path": "/a", "value": (1,)}], 0, ref_too=False)

    def test_copy(self):
        d = {"a": {"x": 1}, "l": [1, 2]}
        r = self.ok(d, [{"op": "copy", "from": "/a", "path": "/b"}], {"a": {"x": 1}, "l": [1, 2], "b": {"x": 1}})
        self.assertIsNot(r["a"], r["b"])
        self.ok(d, [{"op": "copy", "from": "/l/0", "path": "/l/-"}], {"a": {"x": 1}, "l": [1, 2, 1]})
        self.ok(d, [{"op": "copy", "from": "/l", "path": "/a/l"}], {"a": {"x": 1, "l": [1, 2]}, "l": [1, 2]})
        self.ok(d, [{"op": "copy", "from": "", "path": "/self"}],
                {"a": {"x": 1}, "l": [1, 2], "self": {"a": {"x": 1}, "l": [1, 2]}})
        self.ok(d, [{"op": "copy", "from": "/a/x", "path": ""}], 1)
        self.ok(d, [{"op": "copy", "from": "/l/1", "path": "/l/0"}], {"a": {"x": 1}, "l": [2, 1, 2]})
        self.bad(d, [{"op": "copy", "from": "/zz", "path": "/b"}], 0)
        self.bad(d, [{"op": "copy", "from": "/a", "path": "/q/r"}], 0)
        self.bad(d, [{"op": "copy", "path": "/b"}], 0)
        self.bad(d, [{"op": "copy", "from": "/l/5", "path": "/b"}], 0)

    def test_move(self):
        lst = {"l": [0, 1, 2, 3, 4]}
        self.ok(lst, [{"op": "move", "from": "/l/1", "path": "/l/3"}], {"l": [0, 2, 3, 1, 4]})   # 削除後に評価
        self.ok(lst, [{"op": "move", "from": "/l/3", "path": "/l/1"}], {"l": [0, 3, 1, 2, 4]})
        self.ok(lst, [{"op": "move", "from": "/l/0", "path": "/l/-"}], {"l": [1, 2, 3, 4, 0]})
        self.ok(lst, [{"op": "move", "from": "/l/4", "path": "/l/4"}], {"l": [0, 1, 2, 3, 4]})
        d = {"a": {"b": 1}, "c": [1]}
        self.ok(d, [{"op": "move", "from": "/a/b", "path": "/z"}], {"a": {}, "c": [1], "z": 1})
        self.ok(d, [{"op": "move", "from": "/a", "path": "/c/0"}], {"c": [{"b": 1}, 1]})
        self.ok(d, [{"op": "move", "from": "/c", "path": "/a/c"}], {"a": {"b": 1, "c": [1]}})
        self.ok(d, [{"op": "move", "from": "/a", "path": ""}], {"b": 1})
        self.bad(d, [{"op": "move", "from": "", "path": "/x"}], 0)                    # 移動元が root
        self.bad(d, [{"op": "move", "from": "/a", "path": "/a/b/c"}], 0)              # 自分の子孫へ
        self.bad(d, [{"op": "move", "from": "/a", "path": "/a/x"}], 0)
        self.bad(d, [{"op": "move", "from": "/zz", "path": "/x"}], 0)
        self.bad(lst, [{"op": "move", "from": "/l/1", "path": "/l/5"}], 0)            # 削除後は長さ 4

    def test_test_op(self):
        d = {"a": 1, "b": True, "c": {"x": [1, 2], "y": None}, "s": "é"}
        for path, value in (("/a", 1), ("/b", True), ("/c", {"y": None, "x": [1, 2]}), ("/s", "é"),
                            ("", copy.deepcopy(d)), ("/c/x/1", 2), ("/c/y", None)):
            self.ok(d, [{"op": "test", "path": path, "value": value}], d)
        for path, value in (("/a", True), ("/b", 1), ("/a", 1.0), ("/c/x", [2, 1]), ("/c", {"x": [1, 2]}),
                            ("/s", "e"), ("/c/y", False), ("/zz", None), ("/a", "1"), ("/c/x", [1, 2, 3])):
            self.bad(d, [{"op": "test", "path": path, "value": value}], 0)
        self.bad({"n": 0}, [{"op": "test", "path": "/n", "value": False}], 0)


class Pointers(unittest.TestCase):
    def test_escapes_and_special_keys(self):
        d = {"a/b": 1, "~": 2, "": 3, "é✓": 4, "~1": 5, "m~n": 6, " ": 7}
        for ptr, want in (("/a~1b", 1), ("/~0", 2), ("/", 3), ("/é✓", 4), ("/~01", 5), ("/m~0n", 6), ("/ ", 7)):
            self.assertTrue(strict_eq(apply_patch(d, [{"op": "test", "path": ptr, "value": want}]), d))
        for ptr in ("/~2", "/~", "/a~1b~", "/~a", "a", "#/a"):
            with self.assertRaises(PatchError):
                apply_patch(d, [{"op": "test", "path": ptr, "value": 1}])
        self.assertEqual(apply_patch(d, [{"op": "add", "path": "/x~1y", "value": 0}])["x/y"], 0)
        lst = [10, 20, 30]
        self.assertEqual(apply_patch(lst, [{"op": "test", "path": "/0", "value": 10}]), lst)
        for ptr in ("/00", "/01", "/-1", "/+1", "/1.0", "/ 1", "/3", "/-"):
            with self.assertRaises(PatchError, msg=ptr):
                apply_patch(lst, [{"op": "test", "path": ptr, "value": 10}])
        with self.assertRaises(PatchError):
            apply_patch({"a": {}}, [{"op": "add", "path": "/a/b/c", "value": 1}])   # 中間パスなし


class Atomicity(unittest.TestCase):
    def test_failure_at_999th_of_1000_ops(self):
        doc = {"keep": {"deep": [1, {"x": [2, 3]}]}, "list": list(range(20)), "counter": 0}
        ops = [{"op": "add", "path": f"/k{i}", "value": {"n": [i]}} for i in range(997)]
        ops.append({"op": "replace", "path": "/list/0", "value": -1})
        ops.append({"op": "test", "path": "/counter", "value": 1})       # 999 個目（index 998）で失敗
        ops.append({"op": "remove", "path": "/keep"})
        self.assertEqual(len(ops), 1000)
        doc_before, ops_before = fingerprint(doc), fingerprint(ops)
        nested, lst = doc["keep"]["deep"][1]["x"], doc["list"]
        with self.assertRaises(PatchError) as cm:
            apply_patch(doc, ops)
        self.assertEqual(cm.exception.index, 998)
        self.assertEqual(fingerprint(doc), doc_before)
        self.assertEqual(fingerprint(ops), ops_before)
        self.assertIs(doc["keep"]["deep"][1]["x"], nested)
        self.assertEqual(nested, [2, 3])
        self.assertEqual(lst, list(range(20)))
        ops[998] = {"op": "test", "path": "/counter", "value": 0}
        out = apply_patch(doc, ops)
        self.assertNotIn("keep", out)
        self.assertEqual(out["list"][0], -1)
        self.assertEqual(fingerprint(doc), doc_before)

    def test_ownership_independence(self):
        val = {"inner": [1, 2]}
        doc = {"a": [0]}
        out = apply_patch(doc, [{"op": "add", "path": "/v", "value": val},
                                {"op": "replace", "path": "", "value": {"root": val}}])
        val["inner"].append(99)
        doc["a"].append(5)
        self.assertEqual(out, {"root": {"inner": [1, 2]}})
        out["root"]["inner"].append(7)
        self.assertEqual(val, {"inner": [1, 2, 99]})
        same = apply_patch(doc, [])
        self.assertEqual(same, doc)
        self.assertIsNot(same, doc)
        self.assertIsNot(same["a"], doc["a"])


class Limits(unittest.TestCase):
    def test_input_validation(self):
        nest = []
        cur = nest
        for _ in range(MAX_DEPTH - 1):
            nxt = []
            cur.append(nxt)
            cur = nxt
        self.assertEqual(apply_patch(nest, []), nest)                     # ネスト 64
        with self.assertRaises(PatchError):
            apply_patch([nest], [])                                       # ネスト 65
        with self.assertRaises(PatchError):                               # 操作結果がネスト 65
            apply_patch(nest, [{"op": "add", "path": "/0" * 63 + "/-", "value": []}])
        self.assertEqual(len(apply_patch(list(range(MAX_NODES - 1)), [])), MAX_NODES - 1)   # 100000 ノード
        with self.assertRaises(PatchError):
            apply_patch(list(range(MAX_NODES)), [])                       # 100001 ノード
        with self.assertRaises(PatchError) as cm:                         # copy の倍増で上限超過
            apply_patch({"x": list(range(30_000))},
                        [{"op": "copy", "from": "/x", "path": f"/c{i}"} for i in range(4)])
        self.assertEqual(cm.exception.index, 2)          # 30002 → 60003 → 90004 → 120005（3 個目で超過）
        cyc = []
        cyc.append(cyc)
        for bad in (1.5, (1, 2), {1: "a"}, cyc, {"a": [1, {"b": set()}]}, float("nan"), b"x"):
            with self.assertRaises(PatchError):
                apply_patch(bad, [])
        shared = [1]
        self.assertEqual(apply_patch({"a": shared, "b": shared}, []), {"a": [1], "b": [1]})   # 共有参照は可
        apply_patch({}, [{"op": "test", "path": "", "value": {}}] * 1000)
        with self.assertRaises(PatchError) as cm:
            apply_patch({}, [{"op": "test", "path": "", "value": {}}] * 1001)
        self.assertEqual(cm.exception.index, -1)
        for bad_ops in ("x", None, ({"op": "test", "path": "", "value": {}},)):
            with self.assertRaises(PatchError):
                apply_patch({}, bad_ops)
        for bad_op in ({"op": "nope", "path": ""}, {"path": ""}, "add", {"op": "add", "path": 1, "value": 1},
                       {"op": "add", "path": "/a", "value": 1, "extra": 0}, {"op": "move", "path": "/a", "from": 1}):
            with self.assertRaises(PatchError) as cm:
                apply_patch({"a": 1}, [{"op": "test", "path": "/a", "value": 1}, bad_op])
            self.assertEqual(cm.exception.index, 1)


# ------------------------------------------------------------------ ランダム比較
KEYS = ["a", "b", "a/b", "~", "", "é", "x~1"]


def rand_value(rng, depth=0):
    r = rng.random()
    if depth >= 3 or r < 0.4:
        return rng.choice([None, True, False, 0, 1, -7, 2**70, "", "s", "é"])
    if r < 0.7:
        return [rand_value(rng, depth + 1) for _ in range(rng.randrange(4))]
    return {rng.choice(KEYS): rand_value(rng, depth + 1) for _ in range(rng.randrange(4))}


def pointers(doc, prefix=""):
    out = [prefix]
    if isinstance(doc, dict):
        for k, v in doc.items():
            out += pointers(v, prefix + "/" + k.replace("~", "~0").replace("/", "~1"))
    elif isinstance(doc, list):
        for i, v in enumerate(doc):
            out += pointers(v, f"{prefix}/{i}")
        out.append(prefix + "/-")
    return out


def rand_ops(rng, doc, invalid):
    ptrs = pointers(doc)
    ops = []
    for _ in range(rng.randint(1, 6)):
        kind = rng.choice(["add", "remove", "replace", "move", "copy", "test"])
        p = rng.choice(ptrs)
        if rng.random() < 0.3:
            p = p + "/" + rng.choice(KEYS + ["0", "1"]).replace("~", "~0").replace("/", "~1")
        if kind in ("add", "replace", "test"):
            v = rand_value(rng)
            if kind == "test" and rng.random() < 0.5:
                try:
                    v = copy.deepcopy(ref_get(doc, ref_tokens(p)))
                except Exception:
                    pass
            op = {"op": kind, "path": p, "value": v}
        elif kind == "remove":
            op = {"op": kind, "path": p}
        else:
            op = {"op": kind, "from": rng.choice(ptrs), "path": p}
        if invalid and rng.random() < 0.3:
            m = rng.randrange(6)
            if m == 0:
                op["path"] = rng.choice(["/~2", "x", "/01", "/-1", "/zzz/q"])
            elif m == 1:
                op["extra"] = 1
            elif m == 2:
                op.pop("path")
            elif m == 3 and "value" in op:
                op["value"] = rng.choice([True, 1, "1", [1], {"a": 1}])
            elif m == 4:
                op["op"] = "merge"
            else:
                op["path"] = 5
        ops.append(op)
    return ops


class RandomVsReference(unittest.TestCase):
    def test_10000_valid_and_10000_invalid_patches(self):
        rng = random.Random(6902)
        counts = {"valid": 0, "invalid": 0}
        t0 = time.perf_counter()
        attempts = 0
        while (counts["valid"] < 10_000 or counts["invalid"] < 10_000) and attempts < 400_000:
            attempts += 1
            doc = rand_value(rng)
            want_invalid = counts["valid"] >= 10_000 or (counts["invalid"] < 10_000 and rng.random() < 0.5)
            ops = rand_ops(rng, doc, want_invalid)
            try:
                want = ("ok", ref_apply(doc, ops))
            except RefError as e:
                want = ("err", e.index)
            cls = "valid" if want[0] == "ok" else "invalid"
            if counts[cls] >= 10_000:
                continue
            before = fingerprint(doc), fingerprint(ops)
            try:
                got = ("ok", apply_patch(doc, ops))
            except PatchError as e:
                got = ("err", e.index)
            self.assertEqual(got[0], want[0], (doc, ops))
            if got[0] == "ok":
                self.assertTrue(strict_eq(got[1], want[1]), (doc, ops))
            else:
                self.assertEqual(got[1], want[1], (doc, ops))
            self.assertEqual((fingerprint(doc), fingerprint(ops)), before)
            counts[cls] += 1
        self.assertEqual(counts, {"valid": 10_000, "invalid": 10_000})
        print(f"\n[Q09] random patches vs reference: {counts} ({attempts} generated) "
              f"in {time.perf_counter() - t0:.1f}s")


if __name__ == "__main__":
    unittest.main(verbosity=2)
```

#### 5. 検証結果（期待値・実測値・実施状況）

独立参照実装 `ref_apply` は、`copy.deepcopy`・再帰・正規表現によるポインター解析で書いた別実装です（上限の検査は省略）。各ケースで、本実装・参照実装・期待値の 3 者が一致し、入力の document と operations がバイト単位で不変であることを確認しました。

| 必須テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| add（dict / list） | 新規キー、既存キーの置換、先頭・中間・末尾への挿入、`-` による追加、root の置換、連続追加、空キー `/`。不正：範囲外の添字・中間パスなし・スカラーの下・先頭 0・`/` なし・value の欠落 | 14 ケース一致 | 実施 |
| remove | キー・先頭・末尾・入れ子・部分木の削除。不正：root・範囲外・`-`・存在しないキー・負の添字・余分なフィールド | 11 ケース一致 | 実施 |
| replace | dict・list の要素、None、root の置換、bool。不正：存在しない・範囲外・`-`・float の値・tuple の値 | 10 ケース一致 | 実施 |
| copy | 部分木（独立したオブジェクトになる）・リストの末尾・入れ子・root から子へ・root の置換・リスト内の前方。不正：from がない・宛先の中間がない・from の欠落・範囲外 | 10 ケース一致 | 実施 |
| move | リストの前方から後方へ（`[0,1,2,3,4]` の /1 → /3 で `[0,2,3,1,4]`。削除後に評価）、後方から前方へ、末尾へ、同じ位置、dict ↔ list、root の置換。不正：root からの move・自分の子孫へ（2 種）・存在しない移動元・削除後の範囲外 | 13 ケース一致 | 実施 |
| test | int・bool・キー順の異なる dict・Unicode・root・入れ子は一致。不正：1 と True・True と 1・1 と 1.0・リストの順序違い・キーの過不足・文字列の違い・None と False・存在しないパス・"1" と 1・長さ違い・0 と False | 18 ケース一致 | 実施 |
| root の置換・`-` 追加・前方への move による添字のずれ | 上の add・replace・move に含まれる | 一致 | 実施 |
| キー `a/b`・`~`・空文字・Unicode | `/a~1b`→"a/b"、`/~0`→"~"、`/`→""、`/é✓`、`/~01`→"~1"、`/m~0n`→"m~n"、空白キー | 一致 | 実施 |
| 不正な `~2` など | `/~2`・`/~`・末尾の `~`・`/~a`・`a`・`#/a` は PatchError | 一致 | 実施 |
| 空ポインター・先頭 0・負の添字 | `""` は root。`/00`・`/01`・`/-1`・`/+1`・`/1.0`・`/ 1`・範囲外・test での `-` は拒否 | 一致 | 実施 |
| 存在しない中間パス | `/a/b/c`（b がない）への add は拒否 | 一致 | 実施 |
| 1000 個の操作の 999 個目で失敗 | 997 個の add + replace + test（index 998 で失敗）+ remove。PatchError.index == 998。document と operations は json の表現でバイト同等、入れ子のリストは同じオブジェクトで中身も不変。test を直すと全体が成功する | 一致 | 実施 |
| 所有権の独立 | 適用後に入力の value・document を変更しても結果は不変、結果を変更しても入力は不変。空のパッチも新しいオブジェクトを返す | 一致 | 実施 |
| 上限 | ネスト 64 は可、65 は拒否。操作の結果がネスト 65 になる add も拒否。100,000 ノードは可、100,001 は拒否。copy の倍増で上限を超えると index 2 で拒否。operations の 1000 個は可、1001 個は index -1 | 一致 | 実施 |
| 型・循環・フィールドの検証 | float・tuple・非 str のキー・循環参照・set・NaN・bytes は拒否。共有参照（循環でない）は可。operations が非 list なら拒否。未知の op・op の欠落・文字列の操作・非 str の path・余分なフィールド・非 str の from は、該当する index で拒否 | 一致 | 実施 |
| ランダムな適法・不正パッチ各 1 万件と参照実装の比較 | 成否、成功時の結果（型厳密比較）、失敗時の index が一致。入力は不変 | 適法 10,000・不正 10,000（生成 80,007 件）すべて一致、5.8 秒 | 実施 |

#### 6. 技術的な正当性・設計判断

- **原子性：**
  - 最初に `_copy_checked` で document の独立した作業コピーを作り、全操作はその作業コピーにだけ適用します。
  - 失敗すると作業コピーを捨てて `PatchError` を送出するので、元の document には一度も書き込みません（root の置換も、作業コピーの root を差し替えるだけです）。
  - operations は読むだけです。値は `_copy_checked` でコピーしてから使うので、操作内の value オブジェクトを結果が共有することはありません。
- **非再帰と上限：**
  - コピーと検査・`_measure`・型厳密比較は、すべて明示的なスタックで行う非再帰の処理です。深さ 64 を超える入力でも、Python の再帰上限に依存しません。
  - 循環参照は、DFS の経路上の id の集合で検出します（DAG 状の共有参照は許し、木としてコピーします）。
  - ネストの定義：スカラーは 0、コンテナーは 1 + 子の最大値。
  - 操作の後のネストは「パスのトークン数 + 挿入する値のネスト」、ノード数は増減を追跡して、上限を超えたらその操作の index で失敗させます（copy の繰り返しによる指数的な増大を防ぎます）。
- **ポインター：**
  - `""` は root、それ以外は `/` で始まることが必要です。各トークンを左から走査して `~0`→`~`、`~1`→`/` を復号し、`~` の後がそれ以外なら拒否します（RFC 6901 の順序どおり、`~01` は `~1` になります）。
  - リストの添字は `0|[1-9][0-9]*` だけです。`-` は add の宛先の末尾だけで許します。add は `index ≤ len`、remove・replace・test は `index < len` です。
- **各操作：** RFC 6902 の意味に従います（add は既存キーなら置換、リストなら挿入）。
  - move：移動元が root なら拒否、宛先が移動元の真の子孫なら拒否します。移動元を削除してから宛先を評価します。
  - copy：独立したコピーを宛先に add します。
  - test：`type(x) is type(y)` から始まる型厳密比較です（bool と int を区別し、dict のキー順は無関係、list の順序は比較）。
- **フィールドの検証：** 操作ごとに許可するフィールドの集合と完全一致を求めます。未知のフィールド・欠落は、その index で PatchError にします。
- **index の規約：** 操作の失敗はその 0 始まりの番号です。document 自体・operations 全体（型・件数）の不正は `index = -1` です。

#### 7. 未実施項目・制約・残課題

- 共有参照（同じ list が 2 か所にある、循環ではない DAG）は受理し、結果では独立した 2 つのコピーになります。ノード数は、共有を展開した数で数えます。
- 1 操作あたりの計算量は、影響する部分木の大きさに比例します（最大でノード数 10 万 × 操作 1000 が理論上の上限）。
- Linux・macOS での実行はしていません。

---

## Q10【最終地獄級】10万ノードDAG用の非同期実行エンジン・依存順序・失敗時全取消

**試験の狙い：** 大規模グラフアルゴリズム、asyncioタスクの寿命、失敗伝搬、並列度制限、原子的な結果公開。

### 元コード（危険・不完全）

```python
import asyncio
async def run_bad(jobs: dict):
    return await asyncio.gather(*(f() for f in jobs.values()))
    # 依存関係・循環・同時実行上限・失敗時の始末がない
```

### 要求仕様

1. `async run_dag(jobs: Mapping[str, Job], *, concurrency: int = 8) -> dict[str,int]`を実装。Jobは`deps: tuple[str,...]`と`work: Callable[[Mapping[str,int]], Awaitable[int]]`を持つ独自データ型。ジョブIDは1〜64文字のASCII英数字と`_`・`-`、件数は最大100000、依存辺は最大400000。`concurrency`は1〜32。
2. ジョブ間依存は有向グラフとして検証。不存在の依存ID、自分への依存、同一Job内での重複依存、閉路は**work実行前**に明示エラーで拒否。入力jobsは関数実行中に呼出し側が変更しない契約。
3. 依存先がすべて成功するまでジョブを開始しない。workへ渡す依存結果は**そのジョブの直接依存先だけ**の変更不能なMappingである。work返却値はtype(value) is intであること（boolは禁止）。成功した結果はジョブごとに一度だけ確定。
4. 実行中ジョブ数はconcurrency以下。同時に開始可能なジョブはIDの辞書順最小を先に選ぶ。**開始可能になった時点の全体順序は完了イベントに依存する**ため、異なる完了時刻でも全実行が同じ順序になることは要求しない。新規workの開始順は、各スケジューリング回での辞書順ルールに従う。
5. どれか1つのworkが例外または`CancelledError`で終了したら、新規ジョブ開始を停止し、実行中ジョブにcancelを送って必ずawaitで回収してから元の失敗を伝える。成功済みの部分結果を返してはならない。外部からrun_dag自身がキャンセルされた場合も同様。
6. 各workはキャンセルに協力する契約。永続的にキャンセルを無視するworkの終了は保証しない。トポロジカル検証・スケジューリングは**非再帰**とし、グラフ管理メモリO(V+E)、検証時間O(V+E)、スケジューリング時間O((V+E)log V)を満たす。1ノード1タスクを事前に全起動する方式は禁止。
7. 外部パッケージ禁止。`asyncio`、`heapq`、`collections`等の標準ライブラリは使用可。実行結果のdictは呼出し元に対し独立した所有権を持つ。

### 必須検証

1. 空グラフ、直列100ノード、ダイヤモンド、複数依存、100000ノード・400000辺、循環・自己参照・不存在・重複・入力上限の拒否。
2. `asyncio.Event`でタスクを停止し最大同時実行が8を超えないこと、準備済みジョブの辞書順開始、依存未完了のwork未実行。
3. 実行途中の例外・外部キャンセル、完了済み/未完了の混在、キャンセルされた子タスクが全部回収されて無監視例外0、結果の部分公開なし。
4. 1万件の並列DAGを独立参照実装と比較、workがbool・strを返すと失敗、依存結果Mappingの変更拒否、処理量・メモリ実測と無限待機なし。

### ★★FREYJAの回答【Q10・記入欄】

> **以下が専用回答スペース。** 問題本文、元コード、要求仕様、必須検証は改変しない。各項目の例示テキストを実際の内容へ置き換えること。

#### 1. 根本原因・危険性・影響

**元コード `run_bad` の問題点**

| # | 問題箇所 | 発生条件 | 危険性・実害 |
| --- | --- | --- | --- |
| 1 | 依存関係を無視して全ジョブを同時起動する | 依存のあるジョブ | 依存先が完了する前に実行され、誤った入力で計算する |
| 2 | 閉路・存在しない依存の検査がない | 不正なグラフ | 実行の順序が定義できない。エラーにならず誤った結果を返す |
| 3 | 並列数の上限がない | 10 万ノード | 10 万個の Task を一度に作り、メモリと外部資源を枯渇させる |
| 4 | `gather` の失敗時の始末がない | 1 つのジョブが失敗 | 他のジョブは走り続け、回収されない（未監視の Task・例外が残る） |
| 5 | 戻り値の検証・名前との対応がない | bool・str を返すジョブ | list で返すのでジョブ ID との対応がない。int 以外が混入する |
| 6 | 外部キャンセル時の後始末がない | run_dag 自身のキャンセル | 子の Task がキャンセル・回収されない |

**修正方針**
- 実行前に、ID・型・依存（不存在・自己依存・重複）・件数と辺数の上限・閉路（非再帰の Kahn 法）を O(V+E) で検証します。
- 実行は、準備済みジョブの最小ヒープ（ID の辞書順位）と、実行中 ≤ concurrency の Task 集合で行います。`asyncio.wait(FIRST_COMPLETED)` で完了を待つたびに、結果を確定し、後続の残り依存数を減らします。
- 失敗・外部キャンセルの場合は、新規の開始を止め、実行中の全 Task に cancel を送り、await で回収してから元の失敗を送出します。部分結果は返しません。

#### 2. 完全な修正版Pythonコード

```python
"""Q10: 10 万ノード DAG 用の非同期実行エンジン（依存順序・並列度上限・辞書順開始・失敗時の全取消）。"""
from __future__ import annotations

import asyncio
import heapq
import re
from collections import deque
from dataclasses import dataclass
from types import MappingProxyType
from typing import Awaitable, Callable, Mapping

__all__ = ["Job", "run_dag", "DagValidationError", "MAX_JOBS", "MAX_EDGES"]

MAX_JOBS = 100_000
MAX_EDGES = 400_000
_ID_RE = re.compile(r"[A-Za-z0-9_-]{1,64}")


@dataclass(frozen=True)
class Job:
    deps: tuple[str, ...]
    work: Callable[[Mapping[str, int]], Awaitable[int]]


class DagValidationError(ValueError):
    """グラフの不正（不存在・自己依存・重複依存・閉路・上限超過・不正 ID）。work 実行前に送出する。"""


def _validate(jobs: Mapping[str, Job]) -> tuple[list[str], list[tuple[int, ...]], list[list[int]]]:
    """非再帰・O(V+E)（ID の辞書順の付番だけ O(V log V)）。
    戻り値: (辞書順の ID 一覧, 各ノードの依存先番号, 各ノードの被依存先番号)。番号 = 辞書順の順位。"""
    if not isinstance(jobs, Mapping):
        raise TypeError("jobs must be a Mapping")
    n = len(jobs)
    if n > MAX_JOBS:
        raise DagValidationError(f"too many jobs: {n} > {MAX_JOBS}")
    ids = list(jobs.keys())
    for jid in ids:
        if type(jid) is not str or _ID_RE.fullmatch(jid) is None:
            raise DagValidationError(f"invalid job id {jid!r}")
    ids.sort()
    rank = {jid: i for i, jid in enumerate(ids)}
    deps_of: list[tuple[int, ...]] = [()] * n
    edges = 0
    for jid in ids:
        job = jobs[jid]
        if not isinstance(job, Job):
            raise TypeError(f"job {jid!r} is not a Job")
        if type(job.deps) is not tuple:
            raise TypeError(f"deps of {jid!r} must be a tuple")
        if not callable(job.work):
            raise TypeError(f"work of {jid!r} must be callable")
        edges += len(job.deps)
        if edges > MAX_EDGES:
            raise DagValidationError(f"too many dependency edges (> {MAX_EDGES})")
        seen = set()
        ds = []
        for d in job.deps:
            if type(d) is not str:
                raise DagValidationError(f"dependency of {jid!r} must be str: {d!r}")
            if d == jid:
                raise DagValidationError(f"job {jid!r} depends on itself")
            if d in seen:
                raise DagValidationError(f"job {jid!r} lists dependency {d!r} twice")
            r = rank.get(d)
            if r is None:
                raise DagValidationError(f"job {jid!r} depends on unknown job {d!r}")
            seen.add(d)
            ds.append(r)
        deps_of[rank[jid]] = tuple(ds)
    dependents: list[list[int]] = [[] for _ in range(n)]
    indeg = [0] * n
    for v in range(n):
        indeg[v] = len(deps_of[v])
        for u in deps_of[v]:
            dependents[u].append(v)
    # Kahn 法（非再帰）で閉路を検出
    q = deque(v for v in range(n) if indeg[v] == 0)
    done = 0
    while q:
        u = q.popleft()
        done += 1
        for v in dependents[u]:
            indeg[v] -= 1
            if indeg[v] == 0:
                q.append(v)
    if done != n:
        sample = [ids[v] for v in range(n) if indeg[v] > 0][:5]
        raise DagValidationError(f"dependency cycle detected (involving e.g. {sample})")
    return ids, deps_of, dependents


async def _invoke(work, mapping):
    return await work(mapping)


async def _cancel_and_reap(tasks: set[asyncio.Task]) -> None:
    """実行中タスクに cancel を送り、全部が終わるまで await で回収する（例外も必ず観測する）。"""
    for t in tasks:
        t.cancel()
    pending = set(tasks)
    interrupted = False
    while pending:
        try:
            _, pending = await asyncio.wait(pending)
        except asyncio.CancelledError:
            interrupted = True          # 回収中の追加キャンセルは吸収し、回収完了後に送出する
            for t in pending:
                t.cancel()
    for t in tasks:
        if not t.cancelled():
            t.exception()               # 未観測例外を残さない
    if interrupted:
        raise asyncio.CancelledError()


async def run_dag(jobs: Mapping[str, Job], *, concurrency: int = 8) -> dict[str, int]:
    if type(concurrency) is not int:
        raise TypeError("concurrency must be int")
    if not 1 <= concurrency <= 32:
        raise ValueError("concurrency must be within 1..32")
    ids, deps_of, dependents = _validate(jobs)
    n = len(ids)
    works = [jobs[jid].work for jid in ids]
    remaining = [len(d) for d in deps_of]
    results: list[int | None] = [None] * n
    ready = [v for v in range(n) if remaining[v] == 0]
    heapq.heapify(ready)                 # 番号 = ID の辞書順位なので、最小番号 = 辞書順最小
    running: dict[asyncio.Task, int] = {}
    loop = asyncio.get_running_loop()
    completed = 0
    try:
        while completed < n:
            while ready and len(running) < concurrency:
                v = heapq.heappop(ready)
                mapping = MappingProxyType({ids[u]: results[u] for u in deps_of[v]})   # 直接依存だけ・変更不能
                task = loop.create_task(_invoke(works[v], mapping), name=f"dag:{ids[v]}")
                running[task] = v
            done, _ = await asyncio.wait(running, return_when=asyncio.FIRST_COMPLETED)
            for task in sorted(done, key=running.__getitem__):
                v = running.pop(task)
                if task.cancelled():
                    raise _JobFailed(ids[v], asyncio.CancelledError(f"job {ids[v]!r} was cancelled"))
                exc = task.exception()
                if exc is not None:
                    raise _JobFailed(ids[v], exc)
                value = task.result()
                if type(value) is not int:
                    raise _JobFailed(ids[v], TypeError(
                        f"job {ids[v]!r} returned {type(value).__name__}, expected int"))
                results[v] = value                     # ジョブごとに一度だけ確定
                completed += 1
                for w in dependents[v]:
                    remaining[w] -= 1
                    if remaining[w] == 0:
                        heapq.heappush(ready, w)
    except _JobFailed as failure:
        # 新規開始を止め、実行中を cancel して回収してから元の失敗を伝える（部分結果は返さない）
        others = set(running)
        running.clear()
        await _cancel_and_reap(others)
        original = failure.original
        original.add_note(f"raised by DAG job {failure.job_id!r}")
        raise original from None
    except BaseException:
        # run_dag 自身の外部キャンセルなど
        others = set(running)
        running.clear()
        try:
            await _cancel_and_reap(others)
        except asyncio.CancelledError:
            pass
        raise
    return {ids[v]: results[v] for v in range(n)}          # 呼出し元が所有する新しい dict


class _JobFailed(Exception):
    def __init__(self, job_id: str, original: BaseException) -> None:
        super().__init__(job_id)
        self.job_id = job_id
        self.original = original
```

#### 3. 実行・ビルド方法と使用環境

```powershell
# 作業フォルダー（試験専用の一時領域）: scratchpad\py\q10 に q10.py（実装）と test_q10.py（テスト）を置く
cd <scratchpad>\py\q10
python3.12 --version            # Python 3.12.15
python3.12 -X dev -m unittest -v test_q10
# 修正したテストだけを再実行した
python3.12 -X dev -m unittest -v test_q10.Shapes.test_100k_nodes_400k_edges
```

**使用環境（全問共通）**

- Python：**CPython 3.12.15**（`3.12.15 (main, Oct  9 2026) [MSC v.1944 64 bit (AMD64)]`）。この PC に既にあった `python3.12` コマンドで実行しました。新しいバージョンへの変更・pip によるパッケージ導入はしていません。
- ライブラリ：Python 標準ライブラリだけです。テストは `unittest` で行いました。
- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド、x86-64）。
- 実行方法：問題ごとの作業フォルダー（`scratchpad\py\qNN`。試験専用の一時領域）に、実装 `qNN.py` とテスト `test_qNN.py` を置いて実行しました。`-X dev`（開発モード：ResourceWarning や asyncio の検査を有効化）を付けています。
- 未実施の環境：Linux・macOS・ARM での実行はしていません。

- **1 回目の全体実行：** `Ran 8 tests in 180.362s`。7 件合格、1 件エラー（テストのグラフ生成の誤り。下記で説明）。
- **修正後の再実行（test_100k_nodes_400k_edges）：** `Ran 1 test in 14.753s` → `OK`、終了コード 0。
- 他の 7 件は修正していないテストで、1 回目の実行で合格しています。成功済みの無関係なテストは再実行していません。
- 各テストの終わりに、イベントループの例外ハンドラーへの通知が 0 件であること（未監視の例外がない）と、残存 Task が 0 件であることを検査しています。

#### 4. 実際に使用したテストコード

```python
"""Q10 のテスト（unittest.IsolatedAsyncioTestCase）。参照実装は同期的な Kahn 法で値を計算する独立実装。"""
from __future__ import annotations

import asyncio
import gc
import heapq
import random
import time
import tracemalloc
import unittest

from q10 import DagValidationError, Job, run_dag


def ref_eval(spec):
    """spec: {id: (deps, f)}。f は依存結果 dict → int の同期関数。辞書順 Kahn 法で逐次評価。"""
    indeg = {k: len(d) for k, (d, _) in spec.items()}
    rev = {k: [] for k in spec}
    for k, (d, _) in spec.items():
        for x in d:
            rev[x].append(k)
    heap = [k for k, c in indeg.items() if c == 0]
    heapq.heapify(heap)
    out = {}
    while heap:
        k = heapq.heappop(heap)
        deps, f = spec[k]
        out[k] = f({x: out[x] for x in deps})
        for y in rev[k]:
            indeg[y] -= 1
            if indeg[y] == 0:
                heapq.heappush(heap, y)
    return out


def make_jobs(spec, yields=0):
    def mk(f):
        async def w(m):
            for _ in range(yields):
                await asyncio.sleep(0)
            return f(dict(m))
        return w
    return {k: Job(tuple(d), mk(f)) for k, (d, f) in spec.items()}


class Base(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.loop_errors = []
        loop = asyncio.get_running_loop()
        loop.set_exception_handler(lambda l, ctx: self.loop_errors.append(ctx))
        loop.slow_callback_duration = 30.0

    async def asyncTearDown(self):
        gc.collect()
        for _ in range(5):
            await asyncio.sleep(0)
        self.assertEqual(self.loop_errors, [])
        others = [t for t in asyncio.all_tasks() if t is not asyncio.current_task()]
        self.assertEqual(others, [], "leaked tasks")


class Shapes(Base):
    async def test_empty_chain_diamond_multi(self):
        self.assertEqual(await run_dag({}), {})
        chain = {f"c{i:03d}": (((f"c{i - 1:03d}",) if i else ()), (lambda m, i=i: (sum(m.values()) + 1)))
                 for i in range(100)}
        res = await run_dag(make_jobs(chain, yields=1))
        self.assertEqual(res["c099"], 100)
        self.assertEqual(res, ref_eval(chain))
        diamond = {"a": ((), lambda m: 1), "b": (("a",), lambda m: m["a"] + 10),
                   "c": (("a",), lambda m: m["a"] * 5), "d": (("b", "c"), lambda m: m["b"] * 100 + m["c"])}
        self.assertEqual(await run_dag(make_jobs(diamond)), {"a": 1, "b": 11, "c": 5, "d": 1105})
        multi = {"x": ((), lambda m: 2), "y": ((), lambda m: 3), "z": ((), lambda m: 4),
                 "w": (("x", "y", "z"), lambda m: m["x"] * m["y"] * m["z"])}
        self.assertEqual((await run_dag(make_jobs(multi), concurrency=1))["w"], 24)

    async def test_direct_deps_only_and_immutable_mapping(self):
        seen = {}

        def mk(name):
            async def w(m):
                seen[name] = set(m)
                try:
                    m["hack"] = 1                     # 変更不能であること
                except TypeError:
                    pass
                else:
                    raise AssertionError("mapping was mutable")
                return 1
            return w
        jobs = {"a": Job((), mk("a")), "b": Job(("a",), mk("b")), "c": Job(("b",), mk("c")),
                "d": Job(("a", "c"), mk("d"))}
        res = await run_dag(jobs)
        self.assertEqual(seen, {"a": set(), "b": {"a"}, "c": {"b"}, "d": {"a", "c"}})
        res["a"] = 999                                  # 返却 dict は呼出し元が所有
        self.assertEqual((await run_dag(jobs))["a"], 1)

    async def test_100k_nodes_400k_edges(self):
        # debug モードは Task 生成ごとにスタックを採取して極端に遅いので、この大規模試験だけ切る
        asyncio.get_running_loop().set_debug(False)
        rng = random.Random(10)
        n = 100_000
        names = [f"n{i:06d}" for i in range(n)]
        deps = [()] * n
        edges = 0
        for i in range(4, n):
            k = 5 if 20 <= i < 36 else 4                 # (n-4)*4 + 16 = 400,000 辺
            deps[i] = tuple(names[j] for j in rng.sample(range(i), k))
            edges += k
        self.assertEqual(edges, 400_000)
        calls = [0]

        def mk(i):
            async def w(m):
                calls[0] += 1
                return (sum(m.values()) + i) % 1_000_003
            return w
        jobs = {names[i]: Job(deps[i], mk(i)) for i in range(n)}
        gc.collect()
        tracemalloc.start()
        t0 = time.perf_counter()
        try:
            res = await asyncio.wait_for(run_dag(jobs, concurrency=32), 600)
            elapsed = time.perf_counter() - t0
            _, peak = tracemalloc.get_traced_memory()
        finally:
            tracemalloc.stop()
        self.assertEqual(calls[0], n)
        spec = {names[i]: (deps[i], (lambda m, i=i: (sum(m.values()) + i) % 1_000_003)) for i in range(n)}
        self.assertEqual(res, ref_eval(spec))
        print(f"\n[Q10] 100,000 nodes / 400,000 edges: {elapsed:.1f}s (with tracemalloc), "
              f"tracemalloc peak during run_dag {peak / 1024 / 1024:.1f} MiB")


class Validation(Base):
    async def test_rejections_before_any_work(self):
        ran = []

        async def w(m):
            ran.append(1)
            return 1
        bad = {
            "cycle": {"a": Job(("b",), w), "b": Job(("a",), w)},
            "long cycle": {**{f"v{i}": Job((f"v{(i + 1) % 50}",), w) for i in range(50)}, "free": Job((), w)},
            "self": {"a": Job(("a",), w)},
            "missing": {"a": Job(("zz",), w)},
            "duplicate": {"a": Job((), w), "b": Job(("a", "a"), w)},
            "bad id": {"a b": Job((), w)},
            "long id": {"x" * 65: Job((), w)},
            "empty id": {"": Job((), w)},
        }
        for label, jobs in bad.items():
            with self.subTest(label):
                with self.assertRaises(DagValidationError):
                    await run_dag(jobs)
        big = {f"j{i}": Job((), w) for i in range(100_001)}
        with self.assertRaises(DagValidationError):
            await run_dag(big)
        names = [f"e{i}" for i in range(1001)]
        many_edges = {nm: Job((), w) for nm in names}
        for i in range(401):                                    # 401 * 1000 = 401,000 辺
            many_edges[f"z{i}"] = Job(tuple(names[:1000]), w)
        with self.assertRaises(DagValidationError):
            await run_dag(many_edges)
        for conc in (0, 33, -1):
            with self.assertRaises(ValueError):
                await run_dag({"a": Job((), w)}, concurrency=conc)
        for conc in (True, 8.0, "8"):
            with self.assertRaises(TypeError):
                await run_dag({"a": Job((), w)}, concurrency=conc)
        with self.assertRaises(TypeError):
            await run_dag({"a": Job(["b"], w), "b": Job((), w)})     # deps は tuple
        with self.assertRaises(TypeError):
            await run_dag({"a": (("b",), w)})                         # Job 型でない
        self.assertEqual(ran, [])                                     # どの work も実行されていない
        ok = {f"j{i}": Job((), w) for i in range(100_000)}            # 上限ちょうどは受理
        self.assertEqual(len(await run_dag(ok, concurrency=32)), 100_000)


class Scheduling(Base):
    async def test_concurrency_cap_lexicographic_start_and_dependency_gate(self):
        rng = random.Random(3)
        names = [f"job-{rng.randrange(10**6):06d}-{i}" for i in range(20)]
        gates = {nm: asyncio.Event() for nm in names}
        start_order = []
        active = 0
        peak = 0

        def mk(nm):
            async def w(m):
                nonlocal active, peak
                active += 1
                peak = max(peak, active)
                start_order.append(nm)
                try:
                    await gates[nm].wait()
                finally:
                    active -= 1
                return 1
            return w
        jobs = {nm: Job((), mk(nm)) for nm in names}
        jobs["zz-after"] = Job((names[0],), mk("zz-after"))           # names[0] の完了まで開始しない
        gates["zz-after"] = asyncio.Event()
        gates["zz-after"].set()
        t = asyncio.ensure_future(run_dag(jobs, concurrency=8))
        for _ in range(20):
            await asyncio.sleep(0)
        ordered = sorted(names)
        self.assertEqual(start_order, ordered[:8])                    # 準備済みは辞書順で開始
        self.assertEqual(active, 8)
        self.assertNotIn("zz-after", start_order)
        gates[ordered[3]].set()                                       # 1 つ完了 → 次の辞書順最小が開始
        for _ in range(20):
            await asyncio.sleep(0)
        self.assertEqual(start_order, ordered[:9])
        self.assertNotIn("zz-after", start_order)                     # 依存先が未完了なので未実行
        for nm in names:
            gates[nm].set()
        res = await asyncio.wait_for(t, 10)
        self.assertEqual(peak, 8)
        self.assertLess(start_order.index(names[0]), start_order.index("zz-after"))
        self.assertEqual(len(res), 21)


class Failures(Base):
    async def setup_running(self, fail_kind):
        log = {"cancelled": [], "finished": [], "started": []}
        block = asyncio.Event()
        boom = ValueError("job failed")

        def mk(nm, kind):
            async def w(m):
                log["started"].append(nm)
                try:
                    if kind == "quick":
                        return 1
                    if kind == "fail":
                        await asyncio.sleep(0)
                        if fail_kind == "exception":
                            raise boom
                        if fail_kind == "self_cancel":
                            raise asyncio.CancelledError()
                        if fail_kind == "bool":
                            return True
                        if fail_kind == "str":
                            return "1"
                        await block.wait()
                    await block.wait()
                    return 1
                except asyncio.CancelledError:
                    log["cancelled"].append(nm)
                    raise
                finally:
                    log["finished"].append(nm)
            return w
        jobs = {"a1": Job((), mk("a1", "quick")), "a2": Job((), mk("a2", "quick")),
                "b1": Job((), mk("b1", "block")), "b2": Job((), mk("b2", "block")),
                "b3": Job((), mk("b3", "block")), "a3-fail": Job(("a1",), mk("a3-fail", "fail")),
                "d-later": Job(("b1",), mk("d-later", "quick"))}
        return jobs, log, boom

    async def test_failure_cancels_and_reaps_everything(self):
        for kind, exc_type in (("exception", ValueError), ("self_cancel", asyncio.CancelledError),
                               ("bool", TypeError), ("str", TypeError)):
            with self.subTest(kind):
                jobs, log, boom = await self.setup_running(kind)
                with self.assertRaises(exc_type) as cm:
                    await asyncio.wait_for(run_dag(jobs, concurrency=3), 10)
                if kind == "exception":
                    self.assertIs(cm.exception, boom)                     # 元の例外オブジェクト
                self.assertEqual(sorted(log["cancelled"]), ["b1", "b2"] if kind != "self_cancel"
                                 else ["a3-fail", "b1", "b2"])
                self.assertNotIn("d-later", log["started"])               # 新規開始なし
                self.assertNotIn("b3", log["started"])                    # 枠待ちだったジョブも開始しない
                self.assertEqual(sorted(log["started"]), sorted(log["finished"]))   # 全部回収済み
                others = [t for t in asyncio.all_tasks() if t is not asyncio.current_task()]
                self.assertEqual(others, [])

    async def test_external_cancellation(self):
        jobs, log, _ = await self.setup_running("never")
        t = asyncio.ensure_future(run_dag(jobs, concurrency=3))
        for _ in range(20):
            await asyncio.sleep(0)
        self.assertTrue({"a1", "a2"} <= set(log["finished"]))          # 完了済みと実行中が混在
        self.assertTrue(set(log["started"]) - set(log["finished"]))
        t.cancel()
        with self.assertRaises(asyncio.CancelledError):
            await t
        self.assertEqual(sorted(log["started"]), sorted(log["finished"]))
        self.assertTrue(log["cancelled"])
        others = [x for x in asyncio.all_tasks() if x is not asyncio.current_task()]
        self.assertEqual(others, [])


class RandomVsReference(Base):
    async def test_10000_job_dag_vs_reference(self):
        rng = random.Random(42)
        n = 10_000
        names = [f"t{rng.randrange(10**9):09d}x{i}" for i in range(n)]   # 辞書順とトポロジー順は無関係
        order = list(range(n))
        rng.shuffle(order)
        spec = {}
        for pos, i in enumerate(order):
            k = min(pos, rng.randrange(0, 6))
            ds = tuple(names[order[j]] for j in rng.sample(range(pos), k)) if k else ()
            spec[names[i]] = (ds, (lambda m, i=i: (sum(m.values()) * 31 + i) % 2_147_483_647))
        t0 = time.perf_counter()
        res = await asyncio.wait_for(run_dag(make_jobs(spec, yields=2), concurrency=8), 300)
        el = time.perf_counter() - t0
        self.assertEqual(res, ref_eval(spec))
        print(f"\n[Q10] 10,000-job random DAG ({sum(len(d) for d, _ in spec.values())} edges) "
              f"matches reference, {el:.2f}s")


if __name__ == "__main__":
    unittest.main(verbosity=2)
```

#### 5. 検証結果（期待値・実測値・実施状況）

独立参照実装 `ref_eval` は、同期的な辞書順 Kahn 法で、各ジョブの関数を依存結果から逐次計算するものです。

| 必須テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 空のグラフ | `{}` | 一致 | 実施 |
| 直列 100 ノード | c099 = 100、参照実装と一致 | 一致 | 実施 |
| ダイヤモンド | a=1, b=11, c=5, d=1105 | 一致 | 実施 |
| 複数の依存 | w = x*y*z = 24（concurrency=1） | 一致 | 実施 |
| 10 万ノード・40 万辺 | 各ノードの依存は過去のノードから無作為に 4（16 ノードは 5）本で、ちょうど 400,000 辺。全 10 万の work が各 1 回呼ばれ、結果が参照実装と一致 | 一致。12.0 秒（tracemalloc 有効）、run_dag 実行中の tracemalloc のピーク 35.0 MiB | 実施（修正後） |
| 閉路・自己参照・不存在・重複の拒否 | 2 ノードの閉路・50 ノードの閉路・自己依存・不存在の ID・同じ依存の 2 回・不正な ID（空白・65 文字・空）は DagValidationError | 8 種すべて一致 | 実施 |
| 入力上限の拒否 | 100,001 ジョブ・401,000 辺は DagValidationError。100,000 ジョブちょうどは受理して実行 | 一致 | 実施 |
| work の実行前に拒否 | 上の全不正ケースで、work の呼出しは 0 回 | 0 回 | 実施 |
| concurrency・型 | 0・33・-1 は ValueError、True・8.0・"8" は TypeError。deps が list、Job でない値は TypeError | 一致 | 実施 |
| Event による停止と最大同時実行数 | 20 個の独立ジョブを Event で停止。実行中はちょうど 8、ピークも 8 で超過しない | 一致 | 実施 |
| 準備済みジョブの辞書順開始 | 開始順 = 辞書順の先頭 8 件。1 件を完了させると、次の辞書順最小が開始する | 一致 | 実施 |
| 依存が未完了の work は未実行 | `zz-after` は依存先が完了するまで開始しない。完了後に開始する | 一致 | 実施 |
| 実行途中の例外 | 完了済み（a1・a2）と実行中（b1・b2）が混在する中で、a3-fail が ValueError。run_dag は**同じ例外オブジェクト**を送出する。b1・b2 は CancelledError を受け取り、すべて回収される。待機中の b3、依存が未完了の d-later は開始しない | 一致 | 実施 |
| work 自身の CancelledError | run_dag は CancelledError、他は取り消して回収 | 一致 | 実施 |
| bool・str を返す work | TypeError で失敗し、実行中のジョブは取り消して回収 | 一致（各 1 ケース） | 実施 |
| 外部キャンセル | 完了済みと実行中が混在する状態で、run_dag の Task をキャンセルすると CancelledError。開始した全ジョブが終了（回収）済み | 一致 | 実施 |
| 無監視例外 0・部分公開なし | 例外ハンドラーへの通知 0、残存 Task 0。失敗時は例外だけで、結果の dict は返らない | 0 / 0 | 実施 |
| 依存結果の Mapping | work に渡すのは直接依存先だけ（d は {a, c}。推移的な a→b→c の b は含まない）。代入は TypeError（変更不能） | 一致 | 実施 |
| 結果 dict の所有権 | 返された dict を変更しても、次回の実行には影響しない | 一致 | 実施 |
| 1 万件の並列 DAG と参照実装の比較 | 辞書順とトポロジー順が無関係な ID で、10,000 ジョブ・24,788 辺。各 work は 2 回 yield する。concurrency=8 で、結果が参照実装と完全一致 | 一致、24.25 秒（debug モードの asyncio） | 実施 |
| 無限待機なし | すべての実行を `asyncio.wait_for` のタイムアウトの中で行い、タイムアウトは 0 件 | 0 件 | 実施 |

**自律修正**
- **テストの調整（実行前の見直し）：** 失敗の試験で、concurrency 4 では待機させるはずの b3 も開始してしまう配置でした。そこで、失敗ジョブの名前を `a3-fail`、concurrency を 3 にして、「完了済み・実行中・枠待ち・依存待ち」が確実に混在するようにしました。
- **テストのエラー：** 10 万ノードのグラフの生成で、ノード番号 4 で過去の 4 ノードから 5 本を選ぼうとして ValueError になりました（`random.sample` の母集団不足）。5 本にするノードを 20 ≦ i < 36 に変えて、ちょうど 400,000 辺にしました。
- **試験時間：** IsolatedAsyncioTestCase の debug モードでは、Task を作るたびにスタックを採取するため、10 万件規模では極端に遅くなります（1 回目の全体実行は 180 秒）。そこで、この大規模試験に限り `set_debug(False)` にして再実行しました。
- 実装は変更していません。

#### 6. 技術的な正当性・設計判断

- **検証（work の実行前・非再帰）：**
  - ID の形式、Job・tuple・callable の型、依存の種類（不存在・自己・重複）を検査します。辺の数は数えながら、400,000 を超えた時点で打ち切ります。
  - ID を辞書順に並べて 0..V-1 の番号を振り、依存を番号の配列にします。
  - 被依存の隣接リストと入次数を作り、deque で Kahn 法を行います。処理できたノード数が V 未満なら閉路です。
  - 時間 O(V+E)（付番の整列だけ O(V log V)）、メモリ O(V+E) です。
- **スケジューリング：**
  - 準備済みのジョブは、番号（＝ ID の辞書順位）の最小ヒープに入れます。毎回、空き枠の数だけ最小のものから開始します。
  - 完了ごとに、後続の残り依存数を減らし、0 になったものをヒープに入れます。
  - Task は開始時にだけ作り、同時に存在するのは concurrency 個以下です（事前に全ノードを起動しません）。
  - ヒープ操作は 1 ノードあたり O(log V)、後続の更新は辺ごとに O(1) なので、合計 O((V+E) log V) です。`asyncio.wait` の待機の登録は、1 回あたり O(concurrency ≤ 32) です。
- **結果の確定：** `type(value) is int` を確認し、`results[v]` に一度だけ書きます。work には、直接依存先だけの `MappingProxyType` を渡します（変更不能）。成功時の戻り値は、新しく作った dict です。
- **失敗時の全取消：**
  - 失敗を検出したら、ループを抜けて新規の開始を止めます。実行中の全 Task に `cancel()` を送り、`asyncio.wait` で全部が終わるまで回収します。
  - 回収中の追加のキャンセルは吸収し、回収が終わってから送出します。
  - キャンセル以外で終わった Task は `exception()` で観測し、未監視の例外を残しません。
  - そのうえで、元の例外オブジェクトに `add_note` でジョブ ID を付けて送出します。
  - work が CancelledError で終わった場合は、CancelledError（ジョブ ID 付き）を送出します。
  - 外部から run_dag がキャンセルされた場合も、同じ回収をしてから CancelledError を伝えます。

#### 7. 未実施項目・制約・残課題

- 契約により、キャンセルを永久に無視する work がある場合、回収（したがって run_dag の終了）は保証しません。
- 同じ完了バッチの中に複数の失敗がある場合は、ID の辞書順で最初の失敗を「元の失敗」として送出します。他の失敗は観測だけして捨てます。
- 10 万ノードの試験は debug モードを切って測定しました。メモリは tracemalloc のピーク（Python のヒープ）で、プロセス全体の RSS ではありません。
- Linux・macOS、uvloop での実行はしていません。

---
## ★★FREYJA 最終提出レポート【記入欄】

- Q01〜Q10それぞれの解答状況：全 10 問に回答しました（未完成の問題はありません）。各回答欄に 7 項目すべてを記入しています（根本原因、修正版コード全文、実行方法と環境、テストコード全文、期待値・実測値・実施状況、技術的な根拠、未実施項目）。
  - Q01 FrameDecoder
  - Q02 AsyncKeyedExecutor
  - Q03 evaluate
  - Q04 SqliteLedger
  - Q05 SharedSpscRing
  - Q06 Field / SchemaMeta / RecordBase
  - Q07 external_stable_sort
  - Q08 safe_extract
  - Q09 apply_patch
  - Q10 run_dag
- 各問の修正版コードとテストコード全文が回答欄に記載されているか：記載しています。各回答欄の「2.」に `qNN.py` の全文、「4.」に実際に実行した `test_qNN.py` の全文を載せました。どちらも、実際に実行したファイルを組み立てスクリプトで一字一句そのまま差し込みました。
- Pythonバージョン・OS/CPU・実行方法：
  - CPython 3.12.15（MSC v.1944、64 ビット）。この PC に既にあった `python3.12` を使い、バージョンの変更・pip による導入はしていません。標準ライブラリだけを使用しました。
  - Windows 11 Pro 10.0.26200（x64）、Intel Core i7-4710MQ（4 コア 8 スレッド）。
  - 各問の作業フォルダー `scratchpad\py\qNN` で `python3.12 -X dev -m unittest -v test_qNN` を実行しました（unittest、開発モード）。
- 実際のテスト件数、合格数、失敗数、実行ログ：最終状態で、テストメソッド全 101 件が合格、失敗 0 件です。
  - 内訳：Q01 16、Q02 7、Q03 14、Q04 7、Q05 9、Q06 10、Q07 10、Q08 9、Q09 11、Q10 8。
  - Q05・Q08・Q10 は、1 回目の全体実行でそれぞれ 1 件の失敗・エラーがありました（いずれもテスト側の誤り）。修正した箇所を含むテストだけを再実行して合格し、他のテストは 1 回目で合格しています。
  - 主な実測値：
    - Q01：10 万件のランダム比較で全件一致。
    - Q02：1 万件の混合負荷で受付数が最後に 0。
    - Q03：1 万式の比較で一致。
    - Q04：spawn 4 プロセス・10 万送金で総額を保存し、integrity_check ok。
    - Q05：spawn の 100 万件で不一致 0。
    - Q06：10 万回で混合値 0。
    - Q07：100 万行で参照と一致。
    - Q08：22 種の不正な名前・7 種の種別を拒否。
    - Q09：適法・不正各 1 万件で参照と一致。
    - Q10：10 万ノード・40 万辺と 1 万件 DAG で参照と一致。
  - 実行ログ：`scratchpad\py\qNN\log.txt`（Q08・Q10 の再実行は `log_rerun.txt`。Q05 の再実行結果は、回答欄の「3.」に記載）。
- 不具合の発見・修正内容・必要な再テストの結果：
  - **実装の不具合：**
    - Q02：受付後・実行前のキャンセルで受付数を減らしていなかったので、後片付けを `_job_released()` の 1 か所にまとめました。
    - Q04：busy_timeout の PRAGMA を文字列で組み立てるのをやめ、connect の timeout で設定する形に変えました（実行前の見直し）。
    - Q07：ジェネレーターを明示的に close するようにしました（Windows で一時ファイルが削除できない問題の予防。実行前の見直し）。
    - Q08：ZIP64 の検出に、ローカルヘッダーの拡張フィールド・extract_version・ロケーターの事前検査を追加しました（実行前の見直し）。
  - **テスト側の誤り：**
    - Q03：指数・中間値の上限に違反する期待値、IndentationError、lambda の括弧。
    - Q04：残高の計算違い。
    - Q05：交互操作で満杯になる設計。
    - Q08：圧縮率 100 を超えるデータ、コメントの位置の誤り。
    - Q10：グラフ生成の母集団不足、失敗試験の配置。
  - **試験時間：** Q02・Q10 の大規模試験では、IsolatedAsyncioTestCase の debug モードによるスタック採取が支配的だったので、その試験だけ debug を切りました。
  - **再テスト：** 修正の影響を受けるテストは再実行し、すべて合格しました。成功済みの無関係なテストは再実行していません。
- 実行できなかった必須検証と未実装の内容：
  - 未実装の要求仕様はありません。
  - 未実施の環境：Linux・macOS・ARM での実行。Q05 の POSIX spawn と resource_tracker の経路、Q07・Q08 の POSIX 固有の経路（親ディレクトリの fsync、rename の挙動）は、Windows では通らないため未検証です。
  - 実際の電源断・OS クラッシュの試験は未実施です。Q04・Q07 は例外の注入で、Q05 はプロセスの強制終了で模擬しました。
  - 上限の数え方（Q03 のノード数・深さ、Q09 のネスト）は、問題文に厳密な定義がないため、回答欄に記載した定義で固定しました。
- 所有権・asyncio・SQLite・プロセス間同期の技術的根拠：
  - **所有権：** 入力の memoryview は with で release し、返却値は独立した bytes にします（Q01）。snapshot は不変値の独立コピーです（Q06）。JSON Patch は作業コピーにだけ適用します（Q09）。結果の dict は新しく作ります（Q10）。
  - **asyncio：** Task は開始時にだけ作って上限を守ります。例外は `task.exception()` で必ず観測し、キャンセル時は cancel を送ってから await で回収します（Q02・Q10）。
  - **SQLite：** `BEGIN IMMEDIATE` から COMMIT までが 1 トランザクションです。op_id の UNIQUE と事前照会で exactly-once を保証し、WAL + synchronous=FULL、有界の再試行を行います（Q04）。
  - **プロセス間同期：** 2 つのセマフォで公開と空き枠を管理し、「書込み → release」「acquire → 読取り」の順序で、未公開のデータを読まないことを保証します。unlink は所有者だけが 1 回行います（Q05）。
  - **スレッド：** 全読み書きを RLock の下で行い、検査の後に 1 回だけ差し替えます（Q06）。
  - いずれも、GC・GIL の偶然の性質には依存しない設計です。
- 参考資料、著作権・ライセンス確認結果：
  - コードとテストはすべて独自に実装しました。第三者の完成コードは複製しておらず、外部ライブラリ・pip も使っていません。
  - 仕様として参照したのは、Python 3.12 公式ドキュメント（https://docs.python.org/3.12/ の asyncio・sqlite3・multiprocessing.shared_memory・zipfile・ast・json・heapq など。PSF ライセンスの文書で、仕様の確認だけに使用）、RFC 6901 / RFC 6902、ZIP の APPNOTE（ヘッダーの配置）、SQLite の公式ドキュメント（WAL・BEGIN IMMEDIATE・busy_timeout）です。
  - 広く知られた手法（Kahn 法、k-way マージ、セマフォによる有界バッファー）は、考え方だけを参考にしました。
  - README・LICENSE・著作権表示は変更していません。
- 回答を保存した一時/ローカル作業パス：
  - `C:\Users\user\AppData\Local\Temp\claude\ec29f026d20d029c\f8ca8f7c-9d91-45c8-b573-9bb9b309efc9\scratchpad\py\`（q01〜q10 の実装・テスト・log、answers に回答の原稿）。
  - 回答を記入した試験ファイルは、同じ scratchpad の repo クローンの中にあります。
- GitHubのcommit・push状況（所有者の明示許可がない場合は未提出と記録）：Phantom Nightmare の指示書による、対象ファイルに限った明示的な commit・push の許可に基づいて、この試験ファイル 1 つだけを main への commit・push の対象にしています（force push なし、Co-Authored-By なし）。
- GitHubへ提出した場合のURL・コミットSHA・再取得の確認結果：push の後に main から再取得し、全 10 問の回答が反映されていることを確認します。コミットの SHA と確認結果は、提出後の報告で示します。
- 問題本文・評価基準・他エージェント資料を変更していないこと：変更していません。編集したのは Q01〜Q10 の回答欄と、この最終提出レポートの欄だけです。commit 前に、それ以外の部分が元のファイルと同一であることを確認しました。
- **自己採点・自己合格判定を行わず、最終評価をPhantom Nightmareに委ねること：** 自己採点・合否判定はしていません。最終評価は Phantom Nightmare に委ねます。

**指定作業の完了後は結果を報告し、追加指示があるまで別作業を開始しない。**
