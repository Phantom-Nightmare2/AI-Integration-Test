# ★★FREYJA（フレイア）C言語・地獄級能力試験【全10問】

**担当AIエージェント：★★FREYJA（フレイア）**  
**依頼者・最終評価者：Phantom Nightmare**  
**言語：C17（ISO C。C++は禁止）**  
**試験区分：地獄級／システムプログラミング・メモリ安全性・並列処理・障害復旧**  
**問題数：10問／各10点／100点満点**  
**著作権表示：© 2026 Phantom Nightmare. All rights reserved.**

## 0. クライアントオーダー／全問題共通

1. Q01〜Q10を順番に解くこと。各問題は独立課題。元コードには意図的な不具合・未実装部分がある。
2. **問題本文、元コード、要求仕様、評価基準を編集・削除・緩和しないこと。** 各問の「★★FREYJAの回答」欄だけに回答する。
3. 各問に **(A)根本原因・危険性、(B)要求を満たす完全な修正版Cコード、(C)ビルド手順、(D)正常系・異常系・境界値のテストコードと実測結果、(E)未実施項目** を記入する。差分だけ・口頭の説明だけでは不十分。
4. 元コードは未定義動作を含み得る。危険な入力による元コードの実行を必須としない。まず静的に原因を説明し、**修正版を**検証すること。
5. 原則ISO C17標準ライブラリのみ。例外として **Q03・Q10の並列テストはPOSIX pthreadを使用可、Q06のみLinux/POSIX APIを使用**。C++、Rust、外部パッケージ、インターネットAPI、有料サービスは禁止。問題文が許したテスト補助ツールは利用できる。
6. GCCまたはClangで C17 指定、警告を有効にしてビルドすること。対応環境では AddressSanitizer・UndefinedBehaviorSanitizer、並列問題ではThreadSanitizer等を使い、実行したものだけ実施済みと報告すること。サニタイザーの警告がないことだけを正当性の証明としない。
7. **失敗時の出力パラメーターの不変、整数オーバーフロー、境界外読み書き、アライメント、所有権、例外的なOSエラー、メモリ解放**を確認する。使用できないツール・実行環境・再現できない検証は「未実施」と記載し、合格扱いにしない。
8. テストデータは専用の一時領域・ダミーデータだけを使用すること。実際のユーザーファイル、秘密情報、GitHub認証情報、運用環境を破壊・変更しない。**C言語の問題なのでGUI操作は不要**。PCが使用中か不明な場合、マウス・キーボードを占有するGUI自動テストは実行しない。
9. 著作権・ライセンスを守ること。第三者のコード・素材をコピーして完成品として提出しない。必要な資料を参照した場合は出典を記載し、実装は独自に行う。
10. 問題の担当・目的・仕様・提出範囲をClaude Code等が無断で変更・拡張してはならない。自律調査・自律修正・自律テストは**各問の指定範囲内だけ**許可する。CLAUDE.md、エージェント設定、権限設定、他エージェントの資料、README、LICENSEを変更しない。
11. 同じ成功済みテストを理由なく無制限に繰り返さない。問題が判明した部分だけ再テストし、必要な関連回帰テストを実施する。
12. GitHubへのコミット・pushは所有者の明示許可があるときだけ。★★FREYJAは自分で採点・合否判定しない。最終判定はPhantom Nightmareが行う。

**採点：各問10点＝根本原因・危険性2点／正しい修正版コード6点／検証方法と実測結果2点。全10問100点。** 明確に定めた仕様に合わない場合、見た目の完成度や説明量だけで加点しない。

---

## Q01【地獄級】分割受信・可変長整数・CRC32を備えたバイナリフレーム解析

**狙い：** 1バイトずつの受信でも正しく動く状態機械、境界外アクセス・整数シフト・リソース枯渇・破損データ処理。

### 通信仕様（この問題内で固定）

1フレームの並びは **マジック2バイト（CA D0）／バージョン1バイト（01）／payload長のcanonical ULEB128／payload本体／CRC32のリトルエンディアン4バイト**。
長さは0〜65535、ULEB128は最短表現のみ、最大3バイト。CRCは初期値0xFFFFFFFF、反転多項式0xEDB88320、最後にビット反転するIEEE CRC-32。**CRC対象はバージョン＋ULEB128の生バイト列＋payload**（マジックとCRC自身は除外）。

### 元コード（危険）

~~~c
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
int decode_bad(const uint8_t *p, size_t n, uint8_t **out, size_t *len) {
    if (n < 4 || p[0] != 0xCA || p[1] != 0xD0) return -1;
    size_t i = 3;
    uint32_t x = 0, shift = 0;
    while (p[i] & 0x80) {
        x |= (uint32_t)(p[i++] & 0x7F) << shift;
        shift += 7;
    }
    x |= (uint32_t)p[i++] << shift;
    *out = malloc(x);
    memcpy(*out, p + i, x);
    *len = x;
    return 0;
}
~~~

### 要求仕様

- 新たに **decoder_init / decoder_feed / decoder_finish / decoder_reset / decoder_destroy** を設計・実装する。feed は任意長チャンクを受け、1チャンク内の複数フレームも順番どおりに処理する。
- 完全なフレームのCRCが一致した場合だけコールバックでpayloadを通知する。payloadはコールバック実行中のみ有効でよい。空payloadを許可する。
- **データの途中でfeedが終わっても失敗扱いにしない**。finish時に未完了なら明示エラー。破損・不正長・不正バージョン・CRC不一致なら失敗状態となり、resetまで追加入力を拒否する。自動再同期はしない。
- 長さ・オフセット・メモリ確保の全操作でオーバーフローを防ぐ。エラー時はリークしない。コールバックが失敗を返す場合もエラー状態にする。
- 入力長が0の場合は状態を維持。NULL入力は長さ0の場合だけ可。成功フレームの順序と回数を厳守。

### 必須検証

1バイトずつ受信／全体一括受信／すべての分割境界／空payload／65535バイトpayload／複数連結フレーム／payload中のCA D0／非最短ULEB128（例80 00）／65536以上／途中切れ／CRCを1ビット反転／コールバック失敗／破損後reset。

### ★★FREYJAの回答【Q01・記入欄】

> **ここから下が回答用スペースです。** GitHubの鉛筆アイコンからファイルを編集し、各「（ここに記入）」やコード枠内の例示文を自分の回答に置き換えてください。問題文・要求仕様・元コードは変更しないこと。

#### 1. 根本原因・危険性・影響

**元コード `decode_bad` の問題点（静的解析。危険入力による元コードの実行はしていません）**

| # | 箇所 | 根本原因 | 発生条件 | 危険性・影響 |
| --- | --- | --- | --- | --- |
| 1 | `while (p[i] & 0x80)` | 入力長 `n` と添字 `i` を比較していない | 長さ欄が `80 80 80 …` のまま入力が終わる | 境界外読み取り（クラッシュ、隣接メモリの読み出し） |
| 2 | `<< shift` | `shift` に上限がない | 継続バイトが5個以上 | `shift >= 32` のシフトは未定義動作（C17 6.5.7）。長さ値も壊れる |
| 3 | ULEB128 の検証なし | 最短表現・最大3バイト・65535以下を検査していない | `80 00`、`80 80 04` など | 規格外フレームを受理し、巨大な長さを信用する |
| 4 | `p[2]` 未検査 | バージョン検査がない | `CA D0 02 …` | 非対応形式を誤解釈する |
| 5 | `malloc(x)` | 戻り値を検査せず、`x` は攻撃者が最大 2^32−1 まで指定可能 | 巨大な長さ、またはメモリ不足 | NULL への `memcpy`（クラッシュ）、メモリ枯渇による DoS |
| 6 | `memcpy(*out, p + i, x)` | `i + x <= n` を検査していない（32ビット環境では `i + x` 自体も桁あふれし得る） | 長さ欄が実データより大きい | 入力バッファの境界外読み取り（情報漏えい・クラッシュ） |
| 7 | CRC 未検査 | CRC32 を計算も比較もしない | 伝送誤り・改ざん | 破損データを正常データとして上位へ渡す |
| 8 | 一括処理前提 | 状態を持たず、1回の呼び出しに1フレーム全体が必要 | 分割受信、1チャンク内の複数フレーム | 分割受信を扱えず、後続フレームを黙って捨てる |
| 9 | 所有権 | 確保したバッファを呼び出し元へ渡すが解放規約がない。エラー時の出力も未定義 | 通常利用 | リーク・二重解放の誘発 |

**修正方針：** 1バイト単位でも動く有限状態機械（MAGIC0 → MAGIC1 → VERSION → LEN → PAYLOAD → CRC）にしました。ULEB128 は1バイトごとに検査し、3バイト目でも継続ビットが立っていれば即エラー、2バイト目以降の最終バイトが 0 なら非最短としてエラー、値が 65535 を超えればエラーとします。シフト量は最大14なので未定義動作は起きません。CRC は受信しながら逐次計算します。payload と CRC がそのチャンク内に揃っている場合は、コピーせずに入力領域を直接コールバックへ渡します（確保0回）。チャンクをまたぐ場合だけ、長さ分（最大65535）の作業バッファを確保して再利用します。エラーは保持され（スティッキー）、`decoder_reset` まで入力を拒否します。自動再同期はしません。メモリ確保はアロケーターを差し替えられるようにし、失敗注入とリーク計数で検証しました。

#### 2. 完全な修正版Cコード

```c
/*
 * Q01: streaming binary frame decoder (C17)
 *
 * Frame: CA D0 | 01 | canonical ULEB128 payload length (1..3 bytes, 0..65535)
 *        | payload | CRC-32 (IEEE, reflected 0xEDB88320, init/xorout 0xFFFFFFFF) LE
 * CRC covers: version byte + raw ULEB128 bytes + payload.
 *
 * Original implementation by ★★FREYJA for the C hell-level exam.
 */
#include <stddef.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

/* ------------------------------------------------------------------ API */

#define FRAME_MAGIC0      0xCAu
#define FRAME_MAGIC1      0xD0u
#define FRAME_VERSION     0x01u
#define FRAME_MAX_PAYLOAD 65535u
#define FRAME_MAX_LEB     3u

typedef enum {
    DEC_OK = 0,
    DEC_ERR_ARG,       /* invalid argument; decoder state is NOT changed      */
    DEC_ERR_MAGIC,     /* first two bytes are not CA D0                        */
    DEC_ERR_VERSION,   /* version byte is not 01                               */
    DEC_ERR_LENGTH,    /* non-canonical ULEB128, > 3 bytes, or length > 65535  */
    DEC_ERR_CRC,       /* CRC mismatch                                         */
    DEC_ERR_CALLBACK,  /* frame callback returned non-zero                     */
    DEC_ERR_NOMEM,     /* payload buffer allocation failed                     */
    DEC_ERR_TRUNCATED  /* decoder_finish() called in the middle of a frame     */
} dec_status;

/* Called once per valid frame, in stream order. `payload` is valid only during
 * the call (it is never NULL, also for an empty payload). Return 0 to continue,
 * non-zero to put the decoder into the failed state (DEC_ERR_CALLBACK).
 * The callback must not call decoder_* functions on the same decoder. */
typedef int (*dec_frame_cb)(void *user, const uint8_t *payload, size_t len);

/* Optional allocator (used for tests: failure injection / leak accounting). */
typedef struct {
    void *(*alloc)(void *ctx, size_t n);
    void (*release)(void *ctx, void *p);
    void *ctx;
} dec_allocator;

enum dec_state { DS_MAGIC0, DS_MAGIC1, DS_VERSION, DS_LEN, DS_PAYLOAD, DS_CRC, DS_FAILED };

typedef struct {
    dec_frame_cb  cb;
    void         *user;
    dec_allocator al;
    int           state;       /* enum dec_state                         */
    dec_status    err;         /* sticky error while state == DS_FAILED  */
    uint32_t      len;         /* payload length being decoded           */
    unsigned      leb_count;   /* ULEB128 bytes consumed so far          */
    uint32_t      crc;         /* running CRC register (not yet inverted) */
    uint32_t      crc_rx;      /* received CRC being assembled           */
    unsigned      crc_count;   /* CRC bytes consumed so far              */
    uint8_t      *buf;         /* payload staging buffer (split frames)  */
    size_t        cap;         /* capacity of buf                        */
    size_t        got;         /* payload bytes staged so far            */
} decoder;

dec_status decoder_init(decoder *d, dec_frame_cb cb, void *user);
dec_status decoder_init_ex(decoder *d, dec_frame_cb cb, void *user, const dec_allocator *al);
dec_status decoder_feed(decoder *d, const uint8_t *data, size_t n);
dec_status decoder_finish(decoder *d);
void       decoder_reset(decoder *d);
void       decoder_destroy(decoder *d);
uint32_t   frame_crc32_update(uint32_t reg, const uint8_t *p, size_t n);

/* ------------------------------------------------------- implementation */

/* 4-bit table for the reflected polynomial 0xEDB88320 (two lookups per byte). */
static const uint32_t crc_nibble[16] = {
    0x00000000u, 0x1DB71064u, 0x3B6E20C8u, 0x26D930ACu,
    0x76DC4190u, 0x6B6B51F4u, 0x4DB26158u, 0x5005713Cu,
    0xEDB88320u, 0xF00F9344u, 0xD6D6A3E8u, 0xCB61B38Cu,
    0x9B64C2B0u, 0x86D3D2D4u, 0xA00AE278u, 0xBDBDF21Cu
};

uint32_t frame_crc32_update(uint32_t reg, const uint8_t *p, size_t n)
{
    for (size_t i = 0; i < n; ++i) {
        reg ^= p[i];
        reg = (reg >> 4) ^ crc_nibble[reg & 0x0Fu];
        reg = (reg >> 4) ^ crc_nibble[reg & 0x0Fu];
    }
    return reg;
}

static void *default_alloc(void *ctx, size_t n) { (void)ctx; return malloc(n); }
static void default_release(void *ctx, void *p) { (void)ctx; free(p); }

static const uint8_t empty_payload[1] = { 0 };

static dec_status dec_fail(decoder *d, dec_status e)
{
    d->state = DS_FAILED;
    d->err = e;
    return e;
}

static void dec_clear_frame(decoder *d)
{
    d->state = DS_MAGIC0;
    d->err = DEC_OK;
    d->len = 0;
    d->leb_count = 0;
    d->crc = 0;
    d->crc_rx = 0;
    d->crc_count = 0;
    d->got = 0;
}

dec_status decoder_init_ex(decoder *d, dec_frame_cb cb, void *user, const dec_allocator *al)
{
    if (d == NULL || cb == NULL)
        return DEC_ERR_ARG;
    if (al != NULL && (al->alloc == NULL || al->release == NULL))
        return DEC_ERR_ARG;
    d->cb = cb;
    d->user = user;
    if (al != NULL) {
        d->al = *al;
    } else {
        d->al.alloc = default_alloc;
        d->al.release = default_release;
        d->al.ctx = NULL;
    }
    d->buf = NULL;
    d->cap = 0;
    dec_clear_frame(d);
    return DEC_OK;
}

dec_status decoder_init(decoder *d, dec_frame_cb cb, void *user)
{
    return decoder_init_ex(d, cb, user, NULL);
}

/* Ensure the staging buffer can hold `need` (1..65535) bytes. The old buffer
 * never holds live data at this point (called only when got == 0). */
static int dec_reserve(decoder *d, size_t need)
{
    if (d->cap >= need)
        return 1;
    if (d->buf != NULL) {
        d->al.release(d->al.ctx, d->buf);
        d->buf = NULL;
        d->cap = 0;
    }
    uint8_t *p = (uint8_t *)d->al.alloc(d->al.ctx, need);
    if (p == NULL)
        return 0;
    d->buf = p;
    d->cap = need;
    return 1;
}

static uint32_t load_le32(const uint8_t *p)
{
    return (uint32_t)p[0] | ((uint32_t)p[1] << 8) | ((uint32_t)p[2] << 16) | ((uint32_t)p[3] << 24);
}

/* Validate the completed frame and hand it to the callback. */
static dec_status dec_deliver(decoder *d, const uint8_t *payload, uint32_t received_crc)
{
    if ((d->crc ^ 0xFFFFFFFFu) != received_crc)
        return dec_fail(d, DEC_ERR_CRC);
    d->state = DS_MAGIC0;              /* frame consumed before the callback runs */
    if (d->cb(d->user, payload != NULL ? payload : empty_payload, (size_t)d->len) != 0)
        return dec_fail(d, DEC_ERR_CALLBACK);
    return DEC_OK;
}

dec_status decoder_feed(decoder *d, const uint8_t *data, size_t n)
{
    if (d == NULL)
        return DEC_ERR_ARG;
    if (d->state == DS_FAILED)
        return d->err;                  /* rejected until decoder_reset()   */
    if (n == 0)
        return DEC_OK;                  /* NULL allowed only with n == 0     */
    if (data == NULL)
        return DEC_ERR_ARG;

    size_t i = 0;
    while (i < n) {
        switch (d->state) {
        case DS_MAGIC0:
            if (data[i++] != FRAME_MAGIC0)
                return dec_fail(d, DEC_ERR_MAGIC);
            d->state = DS_MAGIC1;
            break;
        case DS_MAGIC1:
            if (data[i++] != FRAME_MAGIC1)
                return dec_fail(d, DEC_ERR_MAGIC);
            d->state = DS_VERSION;
            break;
        case DS_VERSION: {
            uint8_t b = data[i++];
            if (b != FRAME_VERSION)
                return dec_fail(d, DEC_ERR_VERSION);
            d->crc = frame_crc32_update(0xFFFFFFFFu, &b, 1);
            d->len = 0;
            d->leb_count = 0;
            d->state = DS_LEN;
            break;
        }
        case DS_LEN: {
            uint8_t b = data[i++];
            d->crc = frame_crc32_update(d->crc, &b, 1);
            /* leb_count <= 2 here, so the shift is at most 14: never undefined. */
            d->len |= (uint32_t)(b & 0x7Fu) << (7u * d->leb_count);
            d->leb_count++;
            if (b & 0x80u) {
                if (d->leb_count >= FRAME_MAX_LEB)          /* 4th byte would be needed */
                    return dec_fail(d, DEC_ERR_LENGTH);
                break;
            }
            if (d->leb_count > 1 && b == 0)                 /* e.g. 80 00: not shortest */
                return dec_fail(d, DEC_ERR_LENGTH);
            if (d->len > FRAME_MAX_PAYLOAD)                 /* 65536 .. 2^21-1        */
                return dec_fail(d, DEC_ERR_LENGTH);
            d->got = 0;
            d->crc_rx = 0;
            d->crc_count = 0;
            d->state = (d->len != 0) ? DS_PAYLOAD : DS_CRC;
            break;
        }
        case DS_PAYLOAD: {
            size_t avail = n - i;                       /* > 0              */
            size_t need = (size_t)d->len - d->got;      /* 1..65535         */
            if (d->got == 0 && avail >= need && avail - need >= 4) {
                /* Whole payload + CRC inside this chunk: validate in place (no copy). */
                const uint8_t *pl = data + i;
                d->crc = frame_crc32_update(d->crc, pl, need);
                uint32_t rx = load_le32(pl + need);
                i += need + 4;
                dec_status st = dec_deliver(d, pl, rx);
                if (st != DEC_OK)
                    return st;
                break;
            }
            if (d->got == 0 && !dec_reserve(d, (size_t)d->len))
                return dec_fail(d, DEC_ERR_NOMEM);
            size_t take = (avail < need) ? avail : need;
            memcpy(d->buf + d->got, data + i, take);
            d->crc = frame_crc32_update(d->crc, data + i, take);
            d->got += take;
            i += take;
            if (d->got == (size_t)d->len)
                d->state = DS_CRC;
            break;
        }
        case DS_CRC: {
            d->crc_rx |= (uint32_t)data[i++] << (8u * d->crc_count);  /* shift <= 24 */
            d->crc_count++;
            if (d->crc_count == 4) {
                dec_status st = dec_deliver(d, d->len != 0 ? d->buf : NULL, d->crc_rx);
                if (st != DEC_OK)
                    return st;
            }
            break;
        }
        default:
            return dec_fail(d, DEC_ERR_ARG);   /* unreachable: corrupted state */
        }
    }
    return DEC_OK;
}

dec_status decoder_finish(decoder *d)
{
    if (d == NULL)
        return DEC_ERR_ARG;
    if (d->state == DS_FAILED)
        return d->err;
    if (d->state != DS_MAGIC0)
        return dec_fail(d, DEC_ERR_TRUNCATED);
    return DEC_OK;
}

void decoder_reset(decoder *d)
{
    if (d == NULL)
        return;
    dec_clear_frame(d);          /* the staging buffer is kept for reuse */
}

void decoder_destroy(decoder *d)
{
    if (d == NULL)
        return;
    if (d->buf != NULL)
        d->al.release(d->al.ctx, d->buf);
    d->buf = NULL;
    d->cap = 0;
    dec_clear_frame(d);
    d->cb = NULL;
}
```

#### 3. ビルド方法・使用環境

```sh
# PowerShell 7 で MSVC 開発環境（vcvars64.bat）を読み込んだ後、build.ps1 が次の4構成を順にビルド・実行する
# q01_test.c は #include "q01.c" により実装と同じ翻訳単位でビルドする
W='-Wall -Wextra -Wpedantic -Wshadow -Wconversion -Wsign-conversion -Wcast-qual -Wstrict-prototypes -Wmissing-prototypes -Wvla -Wformat=2'
# (1) Clang -O2
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O2 -fuse-ld=link q01_test.c -o q01_rel.exe
# (2) Clang + AddressSanitizer（MSVC 同梱の ASan ランタイムでリンク）
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O1 -g -fsanitize=address -fno-omit-frame-pointer -fms-runtime-lib=dll -c q01_test.c -o q01_asan.obj
link /nologo /DEBUG /OUT:q01_asan.exe q01_asan.obj clang_rt.asan_dynamic-x86_64.lib clang_rt.asan_dynamic_runtime_thunk-x86_64.lib msvcrt.lib ucrt.lib vcruntime.lib legacy_stdio_definitions.lib
# (3) Clang + UndefinedBehaviorSanitizer（トラップ方式）
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O1 -g -fsanitize=undefined -fsanitize-trap=undefined -fno-sanitize-recover=all -fuse-ld=link q01_test.c -o q01_ubsan.exe
# (4) MSVC（2つ目のコンパイラ）
cl /nologo /std:c17 /W4 /experimental:c11atomics /utf-8 /O2 /D_CRT_SECURE_NO_WARNINGS q01_test.c /Fe:q01_msvc.exe
# 各 .exe を実行し、終了コード 0 と "RESULT: N checks passed, 0 failed" を確認
```

**使用環境（全問共通）**

- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド、x86-64）
- コンパイラ1：**Clang 18.0.3**（Android NDK r27c に同梱。ターゲットは `x86_64-pc-windows-msvc`。ヘッダー・ライブラリは MSVC 14.52.37025 と Windows SDK/UCRT 10.0.26100、リンカーは link.exe）。`-std=c17` を指定。
- コンパイラ2：**MSVC cl 19.52.37029**（Visual Studio 18 Community）、`/std:c17 /W4`。
- GCC：この PC にはなく、使用していません。Linux／WSL の環境もありません（外部パッケージの無断導入禁止のため、導入していません）。
- AddressSanitizer：Clang でインストルメントし、MSVC 同梱の ASan ランタイム（`clang_rt.asan_dynamic-x86_64`）でリンクしました。ヒープ境界外書き込みの陽性対照で、`heap-buffer-overflow` を検出することを確認済みです。
- UndefinedBehaviorSanitizer：`-fsanitize=undefined -fsanitize-trap=undefined`（トラップ方式。ランタイムを使わず、未定義動作で不正命令の例外として異常終了します）。符号付きオーバーフローの陽性対照で、異常終了することを確認済みです。
- LeakSanitizer：Windows 版 ASan にないため未実施です。リークは各問の計数アロケーター（確保と解放の数の差）で検査しました。ThreadSanitizer：Windows では使えないため未実施です。
- ビルド補助 `shim/stdatomic.h`（製品コード外）：NDK の Clang に付属する `stdatomic.h` は Android（bionic）専用で Windows ではコンパイルできないため、MSVC の C11 実装 `vcruntime_c11_stdatomic.h`（Clang 互換の `__c11_atomic` 組み込み関数を使用）を読み込む1ファイルを置きました。Clang 18 が `__typeof_unqual__` に対応していないため、`atomic_is_lock_free` だけは `__c11_atomic_is_lock_free` で定義し直しています。
- `-D_CRT_SECURE_NO_WARNINGS`：MSVC の CRT が出す「`fopen` などの代わりに `_s` 版を使うように」という非推奨の通知だけを止める定義です（C17 標準関数の使用に対する通知であり、コードの欠陥を隠すものではありません）。

- Q01 の結果：Clang の全構成で警告 0 件、MSVC でも警告 0 件。4構成すべてビルド成功。

#### 4. テストコード

```c
/* Q01 tests: build with the implementation in one translation unit. */
#include "q01.c"
#include <stdio.h>

static int g_fail = 0, g_pass = 0;
#define CHECK(cond, ...) do { if (cond) { ++g_pass; } else { ++g_fail; \
    printf("  FAIL %s:%d: ", __FILE__, __LINE__); printf(__VA_ARGS__); printf("\n"); } } while (0)
static void report(const char *name, int before_fail)
{
    printf("[%s] %s\n", g_fail == before_fail ? "PASS" : "FAIL", name);
}

/* ---------- independent reference: bitwise CRC-32 and frame encoder ---------- */
static uint32_t ref_crc32(const uint8_t *p, size_t n)
{
    uint32_t c = 0xFFFFFFFFu;
    for (size_t i = 0; i < n; ++i) {
        c ^= p[i];
        for (int k = 0; k < 8; ++k)
            c = (c & 1u) ? (c >> 1) ^ 0xEDB88320u : (c >> 1);
    }
    return c ^ 0xFFFFFFFFu;
}

/* Builds a frame; returns its size. out must have len + 12 bytes. */
static size_t build_frame(uint8_t *out, const uint8_t *payload, size_t len)
{
    size_t k = 0;
    out[k++] = 0xCA; out[k++] = 0xD0; out[k++] = 0x01;
    size_t v = len;
    do {
        uint8_t b = (uint8_t)(v & 0x7Fu);
        v >>= 7;
        if (v) b |= 0x80u;
        out[k++] = b;
    } while (v);
    if (len) memcpy(out + k, payload, len);
    k += len;
    uint32_t c = ref_crc32(out + 2, k - 2);
    out[k++] = (uint8_t)c; out[k++] = (uint8_t)(c >> 8);
    out[k++] = (uint8_t)(c >> 16); out[k++] = (uint8_t)(c >> 24);
    return k;
}

/* Raw frame with an arbitrary (possibly invalid) length field. */
static size_t build_raw(uint8_t *out, const uint8_t *leb, size_t nleb, const uint8_t *payload, size_t len)
{
    size_t k = 0;
    out[k++] = 0xCA; out[k++] = 0xD0; out[k++] = 0x01;
    memcpy(out + k, leb, nleb); k += nleb;
    if (len) memcpy(out + k, payload, len);
    k += len;
    uint32_t c = ref_crc32(out + 2, k - 2);
    out[k++] = (uint8_t)c; out[k++] = (uint8_t)(c >> 8);
    out[k++] = (uint8_t)(c >> 16); out[k++] = (uint8_t)(c >> 24);
    return k;
}

/* ---------- recording callback ---------- */
typedef struct {
    size_t   count;
    size_t   fail_at;        /* 1-based frame index that fails; 0 = never */
    uint8_t *log;            /* concatenation: 2-byte length + payload      */
    size_t   log_len, log_cap;
    int      null_seen;
} rec;

static void rec_init(rec *r, size_t cap) { memset(r, 0, sizeof *r); r->log = (uint8_t *)malloc(cap); r->log_cap = cap; }
static void rec_clear(rec *r) { r->count = 0; r->log_len = 0; r->null_seen = 0; }
static void rec_free(rec *r) { free(r->log); r->log = NULL; }

static int rec_cb(void *user, const uint8_t *p, size_t len)
{
    rec *r = (rec *)user;
    r->count++;
    if (p == NULL) r->null_seen = 1;
    if (r->log_len + len + 2 <= r->log_cap) {
        r->log[r->log_len++] = (uint8_t)(len & 0xFF);
        r->log[r->log_len++] = (uint8_t)(len >> 8);
        if (len) memcpy(r->log + r->log_len, p, len);
        r->log_len += len;
    }
    return (r->fail_at != 0 && r->count == r->fail_at) ? 1 : 0;
}

/* expected log for a list of payloads */
static size_t make_log(uint8_t *log, const uint8_t *const *pl, const size_t *len, size_t nfr)
{
    size_t k = 0;
    for (size_t f = 0; f < nfr; ++f) {
        log[k++] = (uint8_t)(len[f] & 0xFF); log[k++] = (uint8_t)(len[f] >> 8);
        if (len[f]) memcpy(log + k, pl[f], len[f]);
        k += len[f];
    }
    return k;
}

/* ---------- counting / failing allocator ---------- */
typedef struct { long outstanding; long total; long fail_from; /* fail when total+1 >= fail_from (0=never) */ } acct;
static void *acct_alloc(void *ctx, size_t n)
{
    acct *a = (acct *)ctx;
    if (a->fail_from != 0 && a->total + 1 >= a->fail_from) return NULL;
    void *p = malloc(n);
    if (p) { a->outstanding++; a->total++; }
    return p;
}
static void acct_release(void *ctx, void *p) { acct *a = (acct *)ctx; if (p) { a->outstanding--; free(p); } }

static uint64_t rng_state = 0x9E3779B97F4A7C15u;
static uint64_t rnd(void) { rng_state ^= rng_state << 13; rng_state ^= rng_state >> 7; rng_state ^= rng_state << 17; return rng_state; }

/* feed `s` split at the given cut points; returns feed status of first failure or finish status */
static dec_status run_split(decoder *d, const uint8_t *s, size_t n, const size_t *cuts, size_t ncuts)
{
    size_t pos = 0;
    for (size_t c = 0; c <= ncuts; ++c) {
        size_t end = (c < ncuts) ? cuts[c] : n;
        dec_status st = decoder_feed(d, s + pos, end - pos);
        if (st != DEC_OK) return st;
        pos = end;
    }
    return decoder_finish(d);
}

int main(void)
{
    int f0;
    acct A = {0, 0, 0};
    dec_allocator AL = { acct_alloc, acct_release, &A };
    rec R; rec_init(&R, 1u << 20);
    decoder D;

    /* T1: CRC table vs reference */
    f0 = g_fail;
    {
        uint8_t b[256];
        for (int i = 0; i < 256; ++i) b[i] = (uint8_t)i;
        for (int i = 0; i < 256; ++i)
            CHECK((frame_crc32_update(0xFFFFFFFFu, b + i, 1) ^ 0xFFFFFFFFu) == ref_crc32(b + i, 1), "crc byte %d", i);
        CHECK((frame_crc32_update(0xFFFFFFFFu, b, 256) ^ 0xFFFFFFFFu) == ref_crc32(b, 256), "crc 0..255");
        const uint8_t chk[] = "123456789";
        CHECK((frame_crc32_update(0xFFFFFFFFu, chk, 9) ^ 0xFFFFFFFFu) == 0xCBF43926u, "check value");
        CHECK(ref_crc32(chk, 9) == 0xCBF43926u, "reference check value");
    }
    report("T1 CRC-32 table / check value CBF43926", f0);

    /* common stream: frames with payload 0, 5, 130 (2-byte LEB), with CA D0 inside */
    static uint8_t p5[5] = { 'h', 0xCA, 0xD0, 'l', 'o' };
    static uint8_t p130[130];
    for (int i = 0; i < 130; ++i) p130[i] = (uint8_t)(i * 7 + 1);
    p130[10] = 0xCA; p130[11] = 0xD0; p130[12] = 0x01;
    const uint8_t *pls[3] = { p5, p5, p130 };
    size_t lens[3] = { 0, 5, 130 };
    static uint8_t stream[512];
    size_t sl = 0;
    for (int f = 0; f < 3; ++f) sl += build_frame(stream + sl, pls[f], lens[f]);
    static uint8_t exp_log[1024];
    size_t exp_len = make_log(exp_log, pls, lens, 3);

    /* T2: whole stream in one chunk, and 1 byte at a time */
    f0 = g_fail;
    {
        CHECK(decoder_init_ex(&D, rec_cb, &R, &AL) == DEC_OK, "init");
        rec_clear(&R);
        CHECK(decoder_feed(&D, stream, sl) == DEC_OK, "feed all");
        CHECK(decoder_finish(&D) == DEC_OK, "finish");
        CHECK(R.count == 3 && R.log_len == exp_len && memcmp(R.log, exp_log, exp_len) == 0, "frames/order");
        CHECK(R.null_seen == 0, "payload pointer never NULL");
        decoder_reset(&D); rec_clear(&R);
        for (size_t i = 0; i < sl; ++i) CHECK(decoder_feed(&D, stream + i, 1) == DEC_OK, "feed byte %zu", i);
        CHECK(decoder_finish(&D) == DEC_OK, "finish bytewise");
        CHECK(R.count == 3 && R.log_len == exp_len && memcmp(R.log, exp_log, exp_len) == 0, "bytewise frames/order");
        decoder_destroy(&D);
    }
    report("T2 one chunk / one byte at a time (3 frames: 0,5,130 bytes)", f0);

    /* T3: every 2-way and every 3-way split of the stream */
    f0 = g_fail;
    {
        long runs = 0;
        CHECK(decoder_init_ex(&D, rec_cb, &R, &AL) == DEC_OK, "init");
        for (size_t a = 0; a <= sl; ++a) {
            decoder_reset(&D); rec_clear(&R);
            size_t cuts[1] = { a };
            dec_status st = run_split(&D, stream, sl, cuts, 1);
            CHECK(st == DEC_OK && R.count == 3 && R.log_len == exp_len && memcmp(R.log, exp_log, exp_len) == 0, "2-split %zu", a);
            ++runs;
        }
        for (size_t a = 0; a <= sl; ++a)
            for (size_t b = a; b <= sl; ++b) {
                decoder_reset(&D); rec_clear(&R);
                size_t cuts[2] = { a, b };
                dec_status st = run_split(&D, stream, sl, cuts, 2);
                if (!(st == DEC_OK && R.count == 3 && R.log_len == exp_len && memcmp(R.log, exp_log, exp_len) == 0))
                    CHECK(0, "3-split %zu %zu", a, b);
                ++runs;
            }
        decoder_destroy(&D);
        CHECK(A.outstanding == 0, "no leak (outstanding=%ld)", A.outstanding);
        printf("  split runs: %ld (stream %zu bytes)\n", runs, sl);
    }
    report("T3 all 2-way and 3-way split boundaries", f0);

    /* T4: empty payload frame alone */
    f0 = g_fail;
    {
        uint8_t fr[16]; size_t n = build_frame(fr, NULL, 0);
        CHECK(n == 8 && fr[3] == 0x00, "empty frame layout (CA D0 01 00 + CRC4)");
        CHECK(decoder_init_ex(&D, rec_cb, &R, &AL) == DEC_OK, "init");
        rec_clear(&R);
        CHECK(decoder_feed(&D, fr, n) == DEC_OK && decoder_finish(&D) == DEC_OK, "decode");
        CHECK(R.count == 1 && R.log_len == 2 && R.log[0] == 0 && R.log[1] == 0 && !R.null_seen, "one empty payload");
        decoder_destroy(&D);
    }
    report("T4 empty payload", f0);

    /* T5: 65535-byte payload (LEB FF FF 03) */
    f0 = g_fail;
    {
        size_t L = 65535;
        uint8_t *pl = (uint8_t *)malloc(L);
        uint8_t *fr = (uint8_t *)malloc(L + 16);
        for (size_t i = 0; i < L; ++i) pl[i] = (uint8_t)(rnd() >> 24);
        size_t n = build_frame(fr, pl, L);
        CHECK(n == L + 10 && fr[3] == 0xFF && fr[4] == 0xFF && fr[5] == 0x03, "65535 frame layout");
        rec big; rec_init(&big, L + 16);
        CHECK(decoder_init_ex(&D, rec_cb, &big, &AL) == DEC_OK, "init");
        long allocs_before = A.total;
        CHECK(decoder_feed(&D, fr, n) == DEC_OK && decoder_finish(&D) == DEC_OK, "one chunk");
        CHECK(big.count == 1 && big.log_len == L + 2 && memcmp(big.log + 2, pl, L) == 0, "payload ok (one chunk)");
        long allocs_one_chunk = A.total - allocs_before;
        CHECK(allocs_one_chunk == 0, "one-chunk frame decoded without allocation");
        decoder_reset(&D); rec_clear(&big);
        for (size_t i = 0; i < n; ++i) if (decoder_feed(&D, fr + i, 1) != DEC_OK) { CHECK(0, "bytewise feed %zu", i); break; }
        CHECK(decoder_finish(&D) == DEC_OK, "bytewise finish");
        CHECK(big.count == 1 && big.log_len == L + 2 && memcmp(big.log + 2, pl, L) == 0, "payload ok (bytewise)");
        /* split at every header boundary, every CRC boundary and 2000 random points */
        size_t pts[32]; size_t np = 0;
        for (size_t i = 0; i <= 8; ++i) pts[np++] = i;
        for (size_t i = n - 6; i <= n; ++i) pts[np++] = i;
        for (size_t k = 0; k < np + 2000; ++k) {
            size_t cut = (k < np) ? pts[k] : (size_t)(rnd() % (n + 1));
            decoder_reset(&D); rec_clear(&big);
            size_t cuts[1] = { cut };
            dec_status st = run_split(&D, fr, n, cuts, 1);
            if (!(st == DEC_OK && big.count == 1 && memcmp(big.log + 2, pl, L) == 0)) CHECK(0, "split at %zu", cut);
        }
        decoder_destroy(&D);
        CHECK(A.outstanding == 0, "no leak");
        printf("  allocations for one-chunk 65535 decode: %ld (zero-copy path)\n", allocs_one_chunk);
        rec_free(&big); free(pl); free(fr);
    }
    report("T5 65535-byte payload (whole / bytewise / boundary + random splits)", f0);

    /* T6: 200 concatenated frames of random sizes in one chunk and random chunks */
    f0 = g_fail;
    {
        enum { NF = 200 };
        uint8_t *pay[NF]; size_t pln[NF];
        size_t total = 0;
        for (int f = 0; f < NF; ++f) {
            pln[f] = (f % 17 == 0) ? 0 : (size_t)(rnd() % 3000);
            pay[f] = (uint8_t *)malloc(pln[f] + 1);
            for (size_t i = 0; i < pln[f]; ++i) pay[f][i] = (uint8_t)rnd();
            total += pln[f] + 12;
        }
        uint8_t *s = (uint8_t *)malloc(total);
        size_t n = 0;
        for (int f = 0; f < NF; ++f) n += build_frame(s + n, pay[f], pln[f]);
        uint8_t *el = (uint8_t *)malloc(total + 2 * NF);
        size_t elen = make_log(el, (const uint8_t *const *)pay, pln, NF);
        CHECK(decoder_init_ex(&D, rec_cb, &R, &AL) == DEC_OK, "init");
        rec_clear(&R);
        CHECK(decoder_feed(&D, s, n) == DEC_OK && decoder_finish(&D) == DEC_OK, "one chunk");
        CHECK(R.count == NF && R.log_len == elen && memcmp(R.log, el, elen) == 0, "order/count one chunk");
        for (int rep = 0; rep < 50; ++rep) {
            decoder_reset(&D); rec_clear(&R);
            size_t pos = 0; dec_status st = DEC_OK;
            while (pos < n && st == DEC_OK) {
                size_t c = (size_t)(rnd() % 5000);
                if (c > n - pos) c = n - pos;
                st = decoder_feed(&D, s + pos, c);
                pos += c;
            }
            if (st == DEC_OK) st = decoder_finish(&D);
            if (!(st == DEC_OK && R.count == NF && R.log_len == elen && memcmp(R.log, el, elen) == 0)) CHECK(0, "random chunks rep %d", rep);
        }
        decoder_destroy(&D);
        CHECK(A.outstanding == 0, "no leak");
        for (int f = 0; f < NF; ++f) free(pay[f]);
        free(s); free(el);
    }
    report("T6 200 concatenated frames (one chunk + 50 random chunkings)", f0);

    /* T7: payload containing CA D0 and a whole embedded frame */
    f0 = g_fail;
    {
        uint8_t inner[32]; size_t ni = build_frame(inner, p5, 5);
        uint8_t fr[64]; size_t n = build_frame(fr, inner, ni);
        CHECK(decoder_init_ex(&D, rec_cb, &R, &AL) == DEC_OK, "init");
        rec_clear(&R);
        CHECK(decoder_feed(&D, fr, n) == DEC_OK && decoder_finish(&D) == DEC_OK, "decode");
        CHECK(R.count == 1 && R.log_len == ni + 2 && memcmp(R.log + 2, inner, ni) == 0, "embedded frame is payload");
        decoder_destroy(&D);
    }
    report("T7 payload containing CA D0 / embedded frame", f0);

    /* T8/T9: length field validation */
    f0 = g_fail;
    {
        struct { uint8_t leb[4]; size_t n; size_t paylen; dec_status expect; const char *name; } cases[] = {
            { {0x80, 0x00}, 2, 0, DEC_ERR_LENGTH, "80 00 (non-minimal 0)" },
            { {0x81, 0x00}, 2, 1, DEC_ERR_LENGTH, "81 00 (non-minimal 1)" },
            { {0x80, 0x80, 0x00}, 3, 0, DEC_ERR_LENGTH, "80 80 00" },
            { {0xFF, 0x80, 0x00}, 3, 127, DEC_ERR_LENGTH, "FF 80 00 (non-minimal 127)" },
            { {0x80, 0x80, 0x80, 0x01}, 4, 0, DEC_ERR_LENGTH, "4-byte ULEB" },
            { {0x80, 0x80, 0x04}, 3, 0, DEC_ERR_LENGTH, "65536 (80 80 04)" },
            { {0xFF, 0xFF, 0x7F}, 3, 0, DEC_ERR_LENGTH, "2^21-1 (FF FF 7F)" },
            { {0x7F}, 1, 127, DEC_OK, "127 (7F)" },
            { {0x80, 0x01}, 2, 128, DEC_OK, "128 (80 01)" },
            { {0xFF, 0x7F}, 2, 16383, DEC_OK, "16383 (FF 7F)" },
            { {0x80, 0x80, 0x01}, 3, 16384, DEC_OK, "16384 (80 80 01)" },
        };
        static uint8_t pl[70000], fr[70100];
        memset(pl, 0x5A, sizeof pl);
        CHECK(decoder_init_ex(&D, rec_cb, &R, &AL) == DEC_OK, "init");
        for (size_t c = 0; c < sizeof cases / sizeof cases[0]; ++c) {
            size_t n = build_raw(fr, cases[c].leb, cases[c].n, pl, cases[c].paylen);
            /* one chunk and bytewise must agree */
            for (int mode = 0; mode < 2; ++mode) {
                decoder_reset(&D); rec_clear(&R);
                dec_status st = DEC_OK;
                if (mode == 0) st = decoder_feed(&D, fr, n);
                else for (size_t i = 0; i < n && st == DEC_OK; ++i) st = decoder_feed(&D, fr + i, 1);
                if (st == DEC_OK) st = decoder_finish(&D);
                CHECK(st == cases[c].expect, "%s mode %d: got %d expected %d", cases[c].name, mode, (int)st, (int)cases[c].expect);
                CHECK(R.count == (cases[c].expect == DEC_OK ? 1u : 0u), "%s mode %d callbacks %zu", cases[c].name, mode, R.count);
                if (st != DEC_OK) CHECK(decoder_feed(&D, fr, n) == st && R.count == 0, "%s rejected after failure", cases[c].name);
            }
        }
        decoder_destroy(&D);
    }
    report("T8/T9 ULEB128: non-minimal, >3 bytes, >=65536 rejected; boundaries accepted", f0);

    /* T10: truncated frame at every position */
    f0 = g_fail;
    {
        uint8_t fr[200]; size_t n = build_frame(fr, p130, 130);
        CHECK(decoder_init_ex(&D, rec_cb, &R, &AL) == DEC_OK, "init");
        for (size_t cut = 1; cut < n; ++cut) {
            decoder_reset(&D); rec_clear(&R);
            dec_status a = decoder_feed(&D, fr, cut);
            dec_status b = decoder_finish(&D);
            if (!(a == DEC_OK && b == DEC_ERR_TRUNCATED && R.count == 0)) CHECK(0, "cut %zu: feed=%d finish=%d", cut, (int)a, (int)b);
            if (decoder_feed(&D, fr + cut, n - cut) != DEC_ERR_TRUNCATED) CHECK(0, "cut %zu not rejected after finish", cut);
        }
        decoder_reset(&D); rec_clear(&R);
        CHECK(decoder_finish(&D) == DEC_OK, "finish with no data is OK");
        decoder_destroy(&D);
    }
    report("T10 truncated input at every position -> feed OK, finish TRUNCATED", f0);

    /* T11: flip every single bit (except magic) -> never delivered, always an error */
    f0 = g_fail;
    {
        uint8_t fr[200], bad[200]; size_t n = build_frame(fr, p130, 130);
        CHECK(decoder_init_ex(&D, rec_cb, &R, &AL) == DEC_OK, "init");
        long flips = 0;
        for (size_t byte = 2; byte < n; ++byte)
            for (int bit = 0; bit < 8; ++bit) {
                memcpy(bad, fr, n);
                bad[byte] ^= (uint8_t)(1u << bit);
                decoder_reset(&D); rec_clear(&R);
                dec_status st = decoder_feed(&D, bad, n);
                if (st == DEC_OK) st = decoder_finish(&D);
                if (!(st != DEC_OK && R.count == 0)) CHECK(0, "flip byte %zu bit %d: st=%d count=%zu", byte, bit, (int)st, R.count);
                if (byte >= n - 4 && st != DEC_ERR_CRC) CHECK(0, "CRC-field flip must be DEC_ERR_CRC (byte %zu)", byte);
                if (byte >= 5 && byte < n - 4 && st != DEC_ERR_CRC) CHECK(0, "payload flip must be DEC_ERR_CRC (byte %zu)", byte);
                ++flips;
            }
        decoder_destroy(&D);
        printf("  single-bit flips tested: %ld\n", flips);
    }
    report("T11 1-bit corruption (version/length/payload/CRC) never delivered", f0);

    /* T12: callback failure */
    f0 = g_fail;
    {
        CHECK(decoder_init_ex(&D, rec_cb, &R, &AL) == DEC_OK, "init");
        rec_clear(&R); R.fail_at = 2;
        CHECK(decoder_feed(&D, stream, sl) == DEC_ERR_CALLBACK, "feed returns CALLBACK");
        CHECK(R.count == 2, "first frame delivered, second failed, third not called (count=%zu)", R.count);
        CHECK(decoder_feed(&D, stream, sl) == DEC_ERR_CALLBACK && R.count == 2, "rejected after failure");
        CHECK(decoder_finish(&D) == DEC_ERR_CALLBACK, "finish reports failure");
        R.fail_at = 0;
        decoder_reset(&D); rec_clear(&R);
        CHECK(decoder_feed(&D, stream, sl) == DEC_OK && decoder_finish(&D) == DEC_OK && R.count == 3, "works after reset");
        decoder_destroy(&D);
    }
    report("T12 callback failure -> error state, later frames not delivered", f0);

    /* T13: reset after corruption */
    f0 = g_fail;
    {
        uint8_t bad[512]; memcpy(bad, stream, sl); bad[sl - 1] ^= 0x01; /* last CRC byte */
        CHECK(decoder_init_ex(&D, rec_cb, &R, &AL) == DEC_OK, "init");
        rec_clear(&R);
        CHECK(decoder_feed(&D, bad, sl) == DEC_ERR_CRC, "CRC error");
        CHECK(R.count == 2, "two good frames before the bad one kept");
        CHECK(decoder_feed(&D, stream, sl) == DEC_ERR_CRC, "rejected until reset");
        decoder_reset(&D); rec_clear(&R);
        CHECK(decoder_feed(&D, stream, sl) == DEC_OK && decoder_finish(&D) == DEC_OK && R.count == 3, "decodes after reset");
        uint8_t m[4] = { 0xCA, 0xD1, 0x01, 0x00 };
        decoder_reset(&D);
        CHECK(decoder_feed(&D, m, 4) == DEC_ERR_MAGIC, "bad magic");
        uint8_t v[4] = { 0xCA, 0xD0, 0x02, 0x00 };
        decoder_reset(&D);
        CHECK(decoder_feed(&D, v, 4) == DEC_ERR_VERSION, "bad version");
        decoder_destroy(&D);
    }
    report("T13 corruption -> reject until reset; reset restores; bad magic/version", f0);

    /* T14: argument rules: NULL only with length 0, zero-length keeps state */
    f0 = g_fail;
    {
        CHECK(decoder_init_ex(&D, rec_cb, &R, &AL) == DEC_OK, "init");
        CHECK(decoder_init(NULL, rec_cb, &R) == DEC_ERR_ARG && decoder_init(&D, NULL, &R) == DEC_ERR_ARG, "init args");
        decoder_destroy(&D);
        CHECK(decoder_init_ex(&D, rec_cb, &R, &AL) == DEC_OK, "re-init");
        rec_clear(&R);
        CHECK(decoder_feed(&D, NULL, 0) == DEC_OK, "NULL,0 ok");
        CHECK(decoder_feed(&D, stream, 7) == DEC_OK, "partial");
        CHECK(decoder_feed(&D, NULL, 0) == DEC_OK && decoder_feed(&D, stream + 7, 0) == DEC_OK, "zero-length keeps state");
        CHECK(decoder_feed(&D, NULL, 3) == DEC_ERR_ARG, "NULL with n>0 rejected");
        CHECK(decoder_feed(&D, stream + 7, sl - 7) == DEC_OK && decoder_finish(&D) == DEC_OK, "state intact after ARG error");
        CHECK(R.count == 3 && R.log_len == exp_len && memcmp(R.log, exp_log, exp_len) == 0, "frames intact");
        CHECK(decoder_feed(NULL, stream, 1) == DEC_ERR_ARG && decoder_finish(NULL) == DEC_ERR_ARG, "NULL decoder");
        decoder_destroy(&D);
    }
    report("T14 NULL/zero-length rules", f0);

    /* T15: allocation failure injection (split frame needs the staging buffer) */
    f0 = g_fail;
    {
        acct B = {0, 0, 1};   /* every allocation fails */
        dec_allocator BL = { acct_alloc, acct_release, &B };
        uint8_t fr[200]; size_t n = build_frame(fr, p130, 130);
        CHECK(decoder_init_ex(&D, rec_cb, &R, &BL) == DEC_OK, "init");
        rec_clear(&R);
        CHECK(decoder_feed(&D, fr, n) == DEC_OK && R.count == 1, "one chunk needs no allocation");
        CHECK(decoder_feed(&D, fr, 10) == DEC_ERR_NOMEM, "split frame -> NOMEM");
        CHECK(decoder_feed(&D, fr + 10, n - 10) == DEC_ERR_NOMEM && R.count == 1, "rejected after NOMEM");
        B.fail_from = 0;
        decoder_reset(&D); rec_clear(&R);
        CHECK(decoder_feed(&D, fr, 10) == DEC_OK && decoder_feed(&D, fr + 10, n - 10) == DEC_OK && R.count == 1, "works after reset");
        decoder_destroy(&D);
        CHECK(B.outstanding == 0, "no leak after NOMEM path (outstanding=%ld)", B.outstanding);
    }
    report("T15 allocation failure -> NOMEM, no leak, recover after reset", f0);

    CHECK(A.outstanding == 0, "global allocator outstanding=%ld", A.outstanding);
    rec_free(&R);
    printf("RESULT: %d checks passed, %d failed\n", g_pass, g_fail);
    return g_fail ? 1 : 0;
}
```

#### 5. 検証結果（期待値と実測値を区別）

各行は Clang（-O2）、Clang+ASan、Clang+UBSan、MSVC の4構成で実行しました。4構成とも `RESULT: 700 checks passed, 0 failed`、終了コード 0 でした。

| テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| CRC-32 表の検証（T1） | 全256バイトでビット単位の参照実装と一致。"123456789" → CBF43926 | 一致、CBF43926 | 実施（4構成） |
| 全体一括受信（T2） | payload 長 0・5・130 の3フレームが順番どおり各1回 | 3回・順序・内容とも一致 | 実施（4構成） |
| 1バイトずつ受信（T2） | 同上 | 一致 | 実施（4構成） |
| すべての分割境界（T3） | 160バイトの連結ストリームで、全2分割（161通り）と全3分割（13,041通り）の結果が一括時と同一 | 13,202 回すべて一致 | 実施（4構成） |
| 空payload（T4） | 長さ0のコールバック1回、ポインターは非NULL | 一致 | 実施（4構成） |
| 65535バイトpayload（T5） | 長さ欄 FF FF 03。一括・1バイトずつ・境界15点と乱数2000点の分割で内容一致。一括受信ではメモリ確保0回 | 一致、確保0回 | 実施（4構成） |
| 複数連結フレーム（T6） | 200フレームの一括受信と乱数分割50通りで、順序・回数・内容が一致 | 一致 | 実施（4構成） |
| payload中のCA D0（T7） | CA D0 01 を含む payload、フレーム全体を payload に入れた場合も payload として扱う | 一致 | 実施（4構成） |
| 非最短ULEB128（T8） | `80 00`、`81 00`、`80 80 00`、`FF 80 00` → DEC_ERR_LENGTH、コールバック0回（一括と1バイトずつの両方） | すべて DEC_ERR_LENGTH、0回 | 実施（4構成） |
| 4バイトULEB128（T8） | `80 80 80 01` → DEC_ERR_LENGTH | 一致 | 実施（4構成） |
| 65536以上（T9） | `80 80 04`（65536）、`FF FF 7F` → DEC_ERR_LENGTH | 一致 | 実施（4構成） |
| 長さの境界値（T9） | 127・128・16383・16384・65535 は受理 | 受理 | 実施（4構成） |
| 途中切れ（T10） | 139バイトのフレームを1〜138バイト目で切断すると feed は DEC_OK、finish は DEC_ERR_TRUNCATED、コールバック0回、以後の入力は拒否 | 138位置すべて一致 | 実施（4構成） |
| CRCを1ビット反転（T11） | マジック以外の全1,096ビットを1つずつ反転すると必ずエラーになり配信0回。payload と CRC 欄の反転は DEC_ERR_CRC | 1,096通りすべて一致 | 実施（4構成） |
| コールバック失敗（T12） | 2件目で失敗 → DEC_ERR_CALLBACK。1件目は配信済み、3件目は呼ばれない。以後の feed と finish はエラーを返す | 一致 | 実施（4構成） |
| 破損後reset（T13） | CRC 不一致で拒否状態。reset 後は正常に復号 | 一致 | 実施（4構成） |
| 不正マジック・不正バージョン（T13） | DEC_ERR_MAGIC / DEC_ERR_VERSION | 一致 | 実施（4構成） |
| NULL・長さ0（T14） | (NULL,0) は DEC_OK で状態維持。(NULL,n>0) は DEC_ERR_ARG で状態不変 | 一致 | 実施（4構成） |
| メモリ確保失敗の注入（T15） | 分割フレームで DEC_ERR_NOMEM、リーク0、reset 後に回復 | 一致、未解放0 | 実施（4構成） |
| リーク検査 | 計数アロケーターの未解放数が全テスト後に0 | 0 | 実施（4構成） |
| ASan / UBSan | 報告なし | 報告なし（終了コード0） | 実施 |
| GCC・Linux でのビルドと実行 | — | 未実施（環境なし） | 未実施 |

（テストの初回実行では T4 が1件失敗しました。原因はテスト側で、空 payload のフレーム長の期待値を 9 と書いていましたが、正しくは CA D0 01 00 と CRC 4バイトの 8 です。あわせて、T5 の「確保回数」の表示が累計値だったので差分で測るように直し、「一括受信では確保0回」を検査項目に加えました。製品コードは変更していません。）

#### 6. 未実施項目・制約・残課題

- GCC と Linux でのビルド・実行は未実施です（WSL 未導入。外部パッケージの無断導入禁止のため導入していません）。
- LeakSanitizer は Windows 版 ASan にないため使えません。リークは計数アロケーターで確認しました。
- libFuzzer などによるカバレッジ誘導型ファジングは未実施です（分割と1ビット反転は総当たりで実施）。
- 制約：コールバック内から同じデコーダーの関数を呼ぶ再入は禁止です（コメントで規定していますが、実行時には検出しません）。1つのデコーダーを複数スレッドから同時に使うことは対象外です。空 payload のときは、長さ0とともに非NULLの静的領域を渡します。

---

## Q02【地獄級】64KiB固定メモリプールのアロケータ・解放・再確保

**狙い：** アライメント、境界タグまたはフリーリスト、分割・連結、断片化、二重解放、再確保時の例外安全性。

### 元コード（危険）

~~~c
#include <stddef.h>
static unsigned char arena[65536];
static size_t cursor;
void *pool_alloc_bad(size_t n) {
    void *p = arena + cursor;
    cursor += n;
    return p;
}
void pool_free_bad(void *p) { (void)p; }
~~~

### 要求仕様

- **pool_init / pool_alloc / pool_free / pool_realloc / pool_validate** をC17で実装。固定64KiB領域外へのmalloc/calloc/reallocは使わない。内部メタデータも同じプール内に収める。
- 非NULLの割当てアドレスは **_Alignof(max_align_t)** に適合。サイズ0のallocはNULLを返し状態不変。free(NULL)は成功・状態不変。
- 分割した空き領域を再利用でき、隣接した解放領域は結合する。**同じプールから返した有効ポインターについてのみ**free/reallocを受け付ける。二重解放とプール内の途中アドレスはエラーとして検出する。全く無関係な任意ポインターの入力は対象外。
- realloc失敗時は旧データ・旧割当て・プール状態を保持。縮小時は保持した長さのデータが変わらない。拡大時は旧内容を保持し、必要なら移動してよい。
- ポインター演算・メタデータサイズ・端数切上げでオーバーフローを発生させない。pool_validateはブロック境界・重複・空き領域の整合性を検証する。
- 1スレッド限定。外部ヒープなし。プール内の有効な独立割当て領域は重ならないこと。

### 必須検証

1/2/3/7/8/16/4096バイト、最大確保可能サイズ、端数アライメント、メモリ満杯、先頭・中間・末尾のfree、連続領域の結合、二重free、途中ポインター、realloc縮小・拡大・移動・失敗、10000回以上の決定的ランダム操作と参照モデル比較。

### ★★FREYJAの回答【Q02・記入欄】

> **ここから下が回答用スペースです。** GitHubの鉛筆アイコンからファイルを編集し、各「（ここに記入）」やコード枠内の例示文を自分の回答に置き換えてください。問題文・要求仕様・元コードは変更しないこと。

#### 1. 根本原因・危険性・影響

**元コードの問題点（静的解析）**

| # | 箇所 | 根本原因 | 危険性・影響 |
| --- | --- | --- | --- |
| 1 | `cursor += n` | 残量 `65536 - cursor` との比較がない | 65536 を超えた位置のポインターを返し、配列外への書き込み（他の静的変数の破壊） |
| 2 | 同上 | `cursor + n` の桁あふれを検査していない（例：`n = SIZE_MAX`） | `cursor` が小さな値に戻り、既存領域と重なるポインターを返す |
| 3 | `arena + cursor` | 端数を切り上げない | `_Alignof(max_align_t)` 境界にない（例：1バイト確保の次に double を置くと未整列）。未定義動作・性能低下 |
| 4 | `pool_free_bad` | 何もしない | 解放領域を再利用できず、確保と解放を繰り返すと必ず枯渇する |
| 5 | メタデータなし | ブロック境界を記録していない | 二重解放、途中ポインター、無関係なポインターを検出できない。realloc を実装できない |
| 6 | `n == 0` | 特別扱いがない | 次の確保と同じアドレスを返し、独立した割り当てが重なる |

**修正方針：** 64KiB の静的配列（`_Alignas(max_align_t)`）を、ブロックで先頭から末尾まで隙間なく敷き詰めます。各ブロックの先頭に16バイトの境界タグ {size, prev_size, state, check} を置き、配列の内部に保持します（外部ヒープは不使用）。読み書きは `memcpy` で行い、整列違反や型の別名規則違反を避けます。

- 確保は first-fit で、残りが最小ブロック以上なら分割します。
- 解放では前後の空きブロックと即座に結合するため、「空きブロックが隣り合わない」状態を常に保ちます。
- ポインターは、プールの範囲を `uintptr_t` で判定した後、ポインターの等値比較で再確認し、ブロック列を走査して「使用中ブロックの payload 先頭」と一致する場合だけ受け付けます。p を参照外しすることはありません。このため、二重解放（DOUBLE_FREE、結合後なら NOT_ALLOCATED）と途中アドレス（INVALID_PTR）を区別して検出できます。
- サイズ計算は、まず `n > 65536 − ヘッダー` を除外してから切り上げるため、桁あふれは起きません。
- realloc は「縮小（その場で分割）」「後続の空きブロックへのその場拡張」「新規確保 → コピー → 旧領域の解放」の順に試します。新規確保が失敗した時点では何も変更していないので、旧データ・旧割り当て・プールの状態はすべて保たれます。

#### 2. 完全な修正版Cコード

```c
/*
 * Q02: 64 KiB fixed memory pool allocator (C17, single thread, no external heap)
 *
 * Layout: the arena is tiled by blocks from offset 0 to POOL_SIZE.
 *   block = [header (POOL_HDR bytes) | payload ...]
 *   header = { size, prev_size, state, check }  (all uint32_t, stored with memcpy)
 *   - size      : whole block size in bytes (header included), multiple of POOL_ALIGN
 *   - prev_size : size of the physically previous block (0 for the first block)
 *   - state     : POOL_TAG_FREE or POOL_TAG_USED
 *   - check     : size ^ prev_size ^ state ^ POOL_CHECK_SALT (corruption detection)
 * Invariants: blocks tile the arena exactly, no two free blocks are adjacent
 * (eager coalescing), every payload address is a multiple of _Alignof(max_align_t).
 *
 * Original implementation by ★★FREYJA for the C hell-level exam.
 */
#include <stddef.h>
#include <stdint.h>
#include <string.h>

/* ------------------------------------------------------------------ API */

#define POOL_SIZE 65536u

typedef enum {
    POOL_OK = 0,
    POOL_ERR_ARG,            /* invalid argument (e.g. realloc to size 0)            */
    POOL_ERR_NOMEM,          /* no free block large enough                           */
    POOL_ERR_INVALID_PTR,    /* not the start of a live allocation from this pool    */
    POOL_ERR_DOUBLE_FREE,    /* pointer is the start of an already freed block       */
    POOL_ERR_NOT_ALLOCATED,  /* pointer lies inside free space (stale / double free  */
                             /* after the block was coalesced with a neighbour)      */
    POOL_ERR_CORRUPT,        /* pool_validate found broken metadata                  */
    POOL_ERR_STATE           /* pool_init() has not been called                       */
} pool_status;

typedef struct {
    size_t blocks, used_blocks, free_blocks;
    size_t used_bytes, free_bytes;   /* whole block sizes, headers included */
    size_t largest_free_payload;     /* largest request that can succeed now */
} pool_stats;

void        pool_init(void);
void       *pool_alloc(size_t n);
pool_status pool_free(void *p);
pool_status pool_realloc(void **pp, size_t n);   /* *pp updated only on success */
pool_status pool_validate(pool_stats *st);       /* st may be NULL              */
size_t      pool_usable_size(const void *p);     /* 0 if p is not a live block  */
size_t      pool_max_alloc(void);

/* ------------------------------------------------------- implementation */

#define POOL_ALIGN      ((size_t)_Alignof(max_align_t))
#define POOL_RAW_HDR    (4u * sizeof(uint32_t))
#define POOL_ROUND(x)   (((x) + POOL_ALIGN - 1u) / POOL_ALIGN * POOL_ALIGN)   /* x small, no overflow */
#define POOL_HDR        POOL_ROUND((size_t)POOL_RAW_HDR)
#define POOL_MIN_BLOCK  (POOL_HDR + POOL_ALIGN)
#define POOL_TAG_FREE   0x46524545u   /* "FREE" */
#define POOL_TAG_USED   0x55534544u   /* "USED" */
#define POOL_CHECK_SALT 0xA5C3F00Du

_Static_assert((_Alignof(max_align_t) & (_Alignof(max_align_t) - 1)) == 0, "alignment must be a power of two");
_Static_assert(_Alignof(max_align_t) <= 4096, "unexpected alignment");
_Static_assert(POOL_SIZE % _Alignof(max_align_t) == 0, "pool size must be a multiple of the alignment");
_Static_assert(POOL_SIZE <= UINT32_MAX, "sizes are stored in uint32_t");

static _Alignas(max_align_t) unsigned char g_arena[POOL_SIZE];
static int g_ready = 0;

typedef struct { uint32_t size, prev_size, state, check; } blk_hdr;

static blk_hdr hdr_load(size_t off)
{
    blk_hdr h;
    memcpy(&h, g_arena + off, sizeof h);
    return h;
}

static void hdr_store(size_t off, uint32_t size, uint32_t prev_size, uint32_t state)
{
    blk_hdr h;
    h.size = size;
    h.prev_size = prev_size;
    h.state = state;
    h.check = size ^ prev_size ^ state ^ POOL_CHECK_SALT;
    memcpy(g_arena + off, &h, sizeof h);
}

/* Update prev_size of the block that follows the block at `off` (if any). */
static void fix_next_prev(size_t off, size_t size)
{
    size_t nxt = off + size;
    if (nxt < POOL_SIZE) {
        blk_hdr n = hdr_load(nxt);
        hdr_store(nxt, n.size, (uint32_t)size, n.state);
    }
}

size_t pool_max_alloc(void) { return (size_t)POOL_SIZE - POOL_HDR; }

void pool_init(void)
{
    hdr_store(0, (uint32_t)POOL_SIZE, 0u, POOL_TAG_FREE);
    g_ready = 1;
}

/* Block size needed for an n-byte request; 0 when it can never fit. */
static size_t block_size_for(size_t n)
{
    if (n == 0 || n > (size_t)POOL_SIZE - POOL_HDR)   /* also rejects SIZE_MAX etc. */
        return 0;
    return POOL_HDR + POOL_ROUND(n);                   /* <= POOL_SIZE, no overflow   */
}

/* Mark block at off (currently FREE, size bsz) as USED with size `need`,
 * splitting off the tail as a new free block when it is large enough. */
static void carve(size_t off, size_t bsz, size_t need, uint32_t prev_size)
{
    if (bsz - need >= POOL_MIN_BLOCK) {
        hdr_store(off, (uint32_t)need, prev_size, POOL_TAG_USED);
        size_t tail = off + need;
        hdr_store(tail, (uint32_t)(bsz - need), (uint32_t)need, POOL_TAG_FREE);
        fix_next_prev(tail, bsz - need);
        /* tail's right neighbour cannot be free: it was adjacent to a free block. */
    } else {
        hdr_store(off, (uint32_t)bsz, prev_size, POOL_TAG_USED);
    }
}

void *pool_alloc(size_t n)
{
    if (!g_ready)
        return NULL;
    size_t need = block_size_for(n);
    if (need == 0)
        return NULL;                                   /* size 0 or too large: no state change */
    for (size_t off = 0; off < POOL_SIZE;) {           /* first fit */
        blk_hdr h = hdr_load(off);
        if (h.state == POOL_TAG_FREE && h.size >= need) {
            carve(off, h.size, need, h.prev_size);
            return g_arena + off + POOL_HDR;
        }
        off += h.size;
    }
    return NULL;
}

/* Locate p. Returns POOL_OK and the block offset for a live allocation start;
 * otherwise the precise error code. Never dereferences p. */
static pool_status locate(const void *p, size_t *blk_off)
{
    /* Range test through uintptr_t (implementation-defined, not undefined);
     * the result is then confirmed with a pointer equality test. */
    uintptr_t base = (uintptr_t)(const void *)g_arena;
    uintptr_t u = (uintptr_t)p;
    if (u < base || u - base >= POOL_SIZE)
        return POOL_ERR_INVALID_PTR;
    size_t pos = (size_t)(u - base);
    if ((const void *)(g_arena + pos) != p)
        return POOL_ERR_INVALID_PTR;
    for (size_t off = 0; off < POOL_SIZE;) {
        blk_hdr h = hdr_load(off);
        size_t end = off + h.size;
        if (h.size == 0 || end > POOL_SIZE)
            return POOL_ERR_CORRUPT;
        if (pos < end) {
            if (pos == off + POOL_HDR) {
                if (h.state == POOL_TAG_USED) { *blk_off = off; return POOL_OK; }
                return POOL_ERR_DOUBLE_FREE;
            }
            return (h.state == POOL_TAG_FREE) ? POOL_ERR_NOT_ALLOCATED : POOL_ERR_INVALID_PTR;
        }
        off = end;
    }
    return POOL_ERR_INVALID_PTR;
}

pool_status pool_free(void *p)
{
    if (p == NULL)
        return POOL_OK;                     /* free(NULL): success, no change */
    if (!g_ready)
        return POOL_ERR_STATE;
    size_t off;
    pool_status st = locate(p, &off);
    if (st != POOL_OK)
        return st;
    blk_hdr h = hdr_load(off);
    size_t start = off, size = h.size;
    uint32_t prev_size = h.prev_size;
    /* coalesce with the next block */
    size_t nxt = off + size;
    if (nxt < POOL_SIZE) {
        blk_hdr n = hdr_load(nxt);
        if (n.state == POOL_TAG_FREE)
            size += n.size;
    }
    /* coalesce with the previous block */
    if (off != 0) {
        size_t prv = off - h.prev_size;
        blk_hdr q = hdr_load(prv);
        if (q.state == POOL_TAG_FREE) {
            start = prv;
            size += q.size;
            prev_size = q.prev_size;
        }
    }
    hdr_store(start, (uint32_t)size, prev_size, POOL_TAG_FREE);
    fix_next_prev(start, size);
    return POOL_OK;
}

size_t pool_usable_size(const void *p)
{
    size_t off;
    if (!g_ready || p == NULL || locate(p, &off) != POOL_OK)
        return 0;
    return (size_t)hdr_load(off).size - POOL_HDR;
}

pool_status pool_realloc(void **pp, size_t n)
{
    if (pp == NULL)
        return POOL_ERR_ARG;
    if (!g_ready)
        return POOL_ERR_STATE;
    if (*pp == NULL) {                      /* behaves like pool_alloc */
        if (n == 0)
            return POOL_OK;                 /* nothing allocated, *pp stays NULL */
        void *q = pool_alloc(n);
        if (q == NULL)
            return POOL_ERR_NOMEM;
        *pp = q;
        return POOL_OK;
    }
    if (n == 0)
        return POOL_ERR_ARG;                /* explicit: use pool_free() to release */
    size_t off;
    pool_status st = locate(*pp, &off);
    if (st != POOL_OK)
        return st;
    size_t need = block_size_for(n);
    if (need == 0)
        return POOL_ERR_NOMEM;              /* can never fit; nothing changed */
    blk_hdr h = hdr_load(off);
    size_t cur = h.size;

    if (need <= cur) {                      /* shrink (or same) in place */
        size_t rest = cur - need;
        if (rest >= POOL_MIN_BLOCK) {
            hdr_store(off, (uint32_t)need, h.prev_size, POOL_TAG_USED);
            size_t tail = off + need;
            size_t tsz = rest;
            size_t nxt = off + cur;
            if (nxt < POOL_SIZE) {
                blk_hdr nb = hdr_load(nxt);
                if (nb.state == POOL_TAG_FREE)
                    tsz += nb.size;         /* merge tail with following free block */
            }
            hdr_store(tail, (uint32_t)tsz, (uint32_t)need, POOL_TAG_FREE);
            fix_next_prev(tail, tsz);
        }
        return POOL_OK;                     /* data untouched */
    }

    /* grow in place into the following free block */
    size_t nxt = off + cur;
    if (nxt < POOL_SIZE) {
        blk_hdr nb = hdr_load(nxt);
        if (nb.state == POOL_TAG_FREE && cur + nb.size >= need) {
            size_t total = cur + nb.size;
            if (total - need >= POOL_MIN_BLOCK) {
                hdr_store(off, (uint32_t)need, h.prev_size, POOL_TAG_USED);
                size_t tail = off + need;
                hdr_store(tail, (uint32_t)(total - need), (uint32_t)need, POOL_TAG_FREE);
                fix_next_prev(tail, total - need);
            } else {
                hdr_store(off, (uint32_t)total, h.prev_size, POOL_TAG_USED);
                fix_next_prev(off, total);
            }
            return POOL_OK;
        }
    }

    /* move: allocate first, so failure leaves everything unchanged */
    void *q = pool_alloc(n);
    if (q == NULL)
        return POOL_ERR_NOMEM;
    memcpy(q, *pp, cur - POOL_HDR);         /* old usable size < n <= new usable size */
    (void)pool_free(*pp);                   /* cannot fail: *pp was located above      */
    *pp = q;
    return POOL_OK;
}

pool_status pool_validate(pool_stats *out)
{
    if (!g_ready)
        return POOL_ERR_STATE;
    pool_stats s;
    memset(&s, 0, sizeof s);
    size_t off = 0, prev = 0;
    int prev_free = 0;
    while (off < POOL_SIZE) {
        if (POOL_SIZE - off < POOL_MIN_BLOCK)
            return POOL_ERR_CORRUPT;                       /* no room for a header */
        blk_hdr h = hdr_load(off);
        if (h.check != (h.size ^ h.prev_size ^ h.state ^ POOL_CHECK_SALT))
            return POOL_ERR_CORRUPT;
        if (h.state != POOL_TAG_FREE && h.state != POOL_TAG_USED)
            return POOL_ERR_CORRUPT;
        if (h.size < POOL_MIN_BLOCK || h.size % POOL_ALIGN != 0 || h.size > POOL_SIZE - off)
            return POOL_ERR_CORRUPT;                       /* bounds / overlap */
        if (h.prev_size != prev)
            return POOL_ERR_CORRUPT;                       /* boundary tag mismatch */
        if (((uintptr_t)(void *)(g_arena + off + POOL_HDR)) % POOL_ALIGN != 0)
            return POOL_ERR_CORRUPT;
        int is_free = (h.state == POOL_TAG_FREE);
        if (is_free && prev_free)
            return POOL_ERR_CORRUPT;                       /* missed coalescing */
        s.blocks++;
        if (is_free) {
            s.free_blocks++;
            s.free_bytes += h.size;
            if ((size_t)h.size - POOL_HDR > s.largest_free_payload)
                s.largest_free_payload = (size_t)h.size - POOL_HDR;
        } else {
            s.used_blocks++;
            s.used_bytes += h.size;
        }
        prev = h.size;
        prev_free = is_free;
        off += h.size;
    }
    if (off != POOL_SIZE || s.used_bytes + s.free_bytes != POOL_SIZE)
        return POOL_ERR_CORRUPT;
    if (out != NULL)
        *out = s;
    return POOL_OK;
}
```

#### 3. ビルド方法・使用環境

```sh
# PowerShell 7 で MSVC 開発環境（vcvars64.bat）を読み込んだ後、build.ps1 が次の4構成を順にビルド・実行する
# q02_test.c は #include "q02.c" により実装と同じ翻訳単位でビルドする
W='-Wall -Wextra -Wpedantic -Wshadow -Wconversion -Wsign-conversion -Wcast-qual -Wstrict-prototypes -Wmissing-prototypes -Wvla -Wformat=2'
# (1) Clang -O2
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O2 -fuse-ld=link q02_test.c -o q02_rel.exe
# (2) Clang + AddressSanitizer（MSVC 同梱の ASan ランタイムでリンク）
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O1 -g -fsanitize=address -fno-omit-frame-pointer -fms-runtime-lib=dll -c q02_test.c -o q02_asan.obj
link /nologo /DEBUG /OUT:q02_asan.exe q02_asan.obj clang_rt.asan_dynamic-x86_64.lib clang_rt.asan_dynamic_runtime_thunk-x86_64.lib msvcrt.lib ucrt.lib vcruntime.lib legacy_stdio_definitions.lib
# (3) Clang + UndefinedBehaviorSanitizer（トラップ方式）
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O1 -g -fsanitize=undefined -fsanitize-trap=undefined -fno-sanitize-recover=all -fuse-ld=link q02_test.c -o q02_ubsan.exe
# (4) MSVC（2つ目のコンパイラ）
cl /nologo /std:c17 /W4 /experimental:c11atomics /utf-8 /O2 /D_CRT_SECURE_NO_WARNINGS q02_test.c /Fe:q02_msvc.exe
# 各 .exe を実行し、終了コード 0 と "RESULT: N checks passed, 0 failed" を確認
```

**使用環境（全問共通）**

- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド、x86-64）
- コンパイラ1：**Clang 18.0.3**（Android NDK r27c に同梱。ターゲットは `x86_64-pc-windows-msvc`。ヘッダー・ライブラリは MSVC 14.52.37025 と Windows SDK/UCRT 10.0.26100、リンカーは link.exe）。`-std=c17` を指定。
- コンパイラ2：**MSVC cl 19.52.37029**（Visual Studio 18 Community）、`/std:c17 /W4`。
- GCC：この PC にはなく、使用していません。Linux／WSL の環境もありません（外部パッケージの無断導入禁止のため、導入していません）。
- AddressSanitizer：Clang でインストルメントし、MSVC 同梱の ASan ランタイム（`clang_rt.asan_dynamic-x86_64`）でリンクしました。ヒープ境界外書き込みの陽性対照で、`heap-buffer-overflow` を検出することを確認済みです。
- UndefinedBehaviorSanitizer：`-fsanitize=undefined -fsanitize-trap=undefined`（トラップ方式。ランタイムを使わず、未定義動作で不正命令の例外として異常終了します）。符号付きオーバーフローの陽性対照で、異常終了することを確認済みです。
- LeakSanitizer：Windows 版 ASan にないため未実施です。リークは各問の計数アロケーター（確保と解放の数の差）で検査しました。ThreadSanitizer：Windows では使えないため未実施です。
- ビルド補助 `shim/stdatomic.h`（製品コード外）：NDK の Clang に付属する `stdatomic.h` は Android（bionic）専用で Windows ではコンパイルできないため、MSVC の C11 実装 `vcruntime_c11_stdatomic.h`（Clang 互換の `__c11_atomic` 組み込み関数を使用）を読み込む1ファイルを置きました。Clang 18 が `__typeof_unqual__` に対応していないため、`atomic_is_lock_free` だけは `__c11_atomic_is_lock_free` で定義し直しています。
- `-D_CRT_SECURE_NO_WARNINGS`：MSVC の CRT が出す「`fopen` などの代わりに `_s` 版を使うように」という非推奨の通知だけを止める定義です（C17 標準関数の使用に対する通知であり、コードの欠陥を隠すものではありません）。

- Q02 の結果：Clang の3構成（rel・ASan・UBSan）はビルド成功・警告0件です。MSVC は `error C2061: 構文エラー: 識別子 'max_align_t'` でビルドできませんでした。MSVC の C モードは `<stddef.h>` に C11 の `max_align_t` を定義しておらず、これは MSVC 側の規格非準拠です。問題文の指定（`_Alignof(max_align_t)` に適合）を守るため、製品コードは ISO C のまま変更していません。このため Q02 の MSVC 構成は「対象外（コンパイラ非対応）」です。
- この環境（x86_64-pc-windows-msvc）では `_Alignof(max_align_t) = 8` で、ヘッダーは16バイト、最小ブロックは24バイト、最大確保サイズは 65520 バイトです。コードは整列値を固定していないため、16 の環境でもそのまま動作します（16 での実行は未実施）。

#### 4. テストコード

```c
/* Q02 tests (single translation unit with the implementation). */
#include "q02.c"
#include <stdio.h>
#include <stdlib.h>

static int g_fail = 0, g_pass = 0;
#define CHECK(cond, ...) do { if (cond) { ++g_pass; } else { ++g_fail; \
    printf("  FAIL %s:%d: ", __FILE__, __LINE__); printf(__VA_ARGS__); printf("\n"); } } while (0)
static void report(const char *name, int before_fail)
{
    printf("[%s] %s\n", g_fail == before_fail ? "PASS" : "FAIL", name);
}

static unsigned char snap[POOL_SIZE];
static void take_snapshot(void) { memcpy(snap, g_arena, POOL_SIZE); }
static int same_as_snapshot(void) { return memcmp(snap, g_arena, POOL_SIZE) == 0; }
static int aligned(const void *p) { return ((uintptr_t)p % _Alignof(max_align_t)) == 0; }

static void fill(void *p, size_t n, unsigned seed)
{
    unsigned char *b = (unsigned char *)p;
    for (size_t i = 0; i < n; ++i) b[i] = (unsigned char)(seed * 31u + i * 7u + (i >> 8));
}
static int verify(const void *p, size_t n, unsigned seed)
{
    const unsigned char *b = (const unsigned char *)p;
    for (size_t i = 0; i < n; ++i) if (b[i] != (unsigned char)(seed * 31u + i * 7u + (i >> 8))) return 0;
    return 1;
}
static int valid(void) { return pool_validate(NULL) == POOL_OK; }

static uint64_t rs = 0x243F6A8885A308D3u;
static uint64_t rnd(void) { rs ^= rs << 13; rs ^= rs >> 7; rs ^= rs << 17; return rs; }

typedef struct { unsigned char *p; size_t n; unsigned seed; } live_t;
static int cmp_live(const void *a, const void *b)
{
    uintptr_t x = (uintptr_t)((const live_t *)a)->p, y = (uintptr_t)((const live_t *)b)->p;
    return (x > y) - (x < y);
}

int main(void)
{
    int f0;
    printf("  _Alignof(max_align_t)=%zu header=%zu min_block=%zu max_alloc=%zu\n",
           (size_t)POOL_ALIGN, (size_t)POOL_HDR, (size_t)POOL_MIN_BLOCK, pool_max_alloc());

    /* T0: use before init */
    f0 = g_fail;
    CHECK(pool_alloc(8) == NULL, "alloc before init");
    CHECK(pool_validate(NULL) == POOL_ERR_STATE, "validate before init");
    { void *q = NULL; CHECK(pool_realloc(&q, 8) == POOL_ERR_STATE && q == NULL, "realloc before init"); }
    CHECK(pool_free(NULL) == POOL_OK, "free(NULL) before init");
    report("T0 calls before pool_init are rejected", f0);

    /* T1: small sizes, alignment, usable size */
    f0 = g_fail;
    {
        pool_init();
        size_t sizes[] = { 1, 2, 3, 7, 8, 16, 4096, 15, 17, 31, 33 };
        void *ps[11];
        for (int i = 0; i < 11; ++i) {
            ps[i] = pool_alloc(sizes[i]);
            CHECK(ps[i] != NULL && aligned(ps[i]), "alloc %zu aligned", sizes[i]);
            CHECK(pool_usable_size(ps[i]) >= sizes[i] && pool_usable_size(ps[i]) % POOL_ALIGN == 0, "usable %zu", sizes[i]);
            fill(ps[i], sizes[i], (unsigned)i);
            CHECK(valid(), "valid after alloc %zu", sizes[i]);
        }
        for (int i = 0; i < 11; ++i) CHECK(verify(ps[i], sizes[i], (unsigned)i), "content %zu", sizes[i]);
        for (int i = 0; i < 11; ++i) CHECK(pool_free(ps[i]) == POOL_OK, "free %d", i);
        pool_stats s; CHECK(pool_validate(&s) == POOL_OK && s.blocks == 1 && s.free_bytes == POOL_SIZE, "all coalesced back");
    }
    report("T1 sizes 1/2/3/7/8/16/4096 (+odd sizes): alignment, usable size, content", f0);

    /* T2: size 0, too large, maximum */
    f0 = g_fail;
    {
        pool_init();
        take_snapshot();
        CHECK(pool_alloc(0) == NULL && same_as_snapshot(), "alloc(0) NULL, state unchanged");
        CHECK(pool_alloc(SIZE_MAX) == NULL && same_as_snapshot(), "alloc(SIZE_MAX)");
        CHECK(pool_alloc(SIZE_MAX - POOL_HDR + 1) == NULL && same_as_snapshot(), "alloc near SIZE_MAX (rounding overflow)");
        CHECK(pool_alloc(POOL_SIZE) == NULL && same_as_snapshot(), "alloc(65536)");
        CHECK(pool_alloc(pool_max_alloc() + 1) == NULL && same_as_snapshot(), "alloc(max+1)");
        void *m = pool_alloc(pool_max_alloc());
        CHECK(m != NULL && aligned(m) && valid(), "alloc(max) = %zu succeeds", pool_max_alloc());
        fill(m, pool_max_alloc(), 9);
        CHECK(pool_alloc(1) == NULL, "pool full after max alloc");
        CHECK(verify(m, pool_max_alloc(), 9), "max block content");
        CHECK(pool_free(m) == POOL_OK && valid(), "free max");
    }
    report("T2 size 0 / SIZE_MAX / max+1 rejected without change; max size works", f0);

    /* T3: odd sizes are rounded to alignment, neighbours do not overlap */
    f0 = g_fail;
    {
        pool_init();
        unsigned char *a = (unsigned char *)pool_alloc(1), *b = (unsigned char *)pool_alloc(POOL_ALIGN + 1), *c = (unsigned char *)pool_alloc(3);
        CHECK(a && b && c && aligned(a) && aligned(b) && aligned(c), "aligned");
        CHECK((size_t)(b - a) == POOL_HDR + POOL_ALIGN, "1 byte -> one alignment unit (%td)", b - a);
        CHECK((size_t)(c - b) == POOL_HDR + 2 * POOL_ALIGN, "align+1 -> two units (%td)", c - b);
        memset(a, 0x11, 1); memset(b, 0x22, POOL_ALIGN + 1); memset(c, 0x33, 3);
        CHECK(valid() && a[0] == 0x11 && b[POOL_ALIGN] == 0x22 && c[2] == 0x33, "no overlap");
    }
    report("T3 rounding to alignment", f0);

    /* T4: memory full with minimum blocks */
    f0 = g_fail;
    {
        pool_init();
        static void *ptrs[POOL_SIZE / 16 + 8];
        size_t k = 0;
        for (;;) { void *q = pool_alloc(1); if (!q) break; ptrs[k++] = q; }
        size_t expect = POOL_SIZE / POOL_MIN_BLOCK;
        CHECK(k == expect, "filled with %zu blocks (expected %zu)", k, expect);
        pool_stats s; CHECK(pool_validate(&s) == POOL_OK && s.free_blocks == 0 && s.used_bytes == POOL_SIZE, "no free space left");
        take_snapshot();
        CHECK(pool_alloc(1) == NULL && same_as_snapshot(), "alloc when full fails, no change");
        for (size_t i = 0; i < k; i += 2) CHECK(pool_free(ptrs[i]) == POOL_OK, "free even %zu", i);
        CHECK(pool_validate(&s) == POOL_OK && s.free_blocks == (k + 1) / 2, "fragmented: %zu free blocks", s.free_blocks);
        CHECK(pool_alloc(POOL_ALIGN + 1) == NULL, "fragmentation: 2-unit request fails");
        void *q = pool_alloc(POOL_ALIGN);
        CHECK(q == ptrs[0], "1-unit request reuses first hole");
        CHECK(pool_free(q) == POOL_OK, "free reuse");
        for (size_t i = 1; i < k; i += 2) CHECK(pool_free(ptrs[i]) == POOL_OK, "free odd %zu", i);
        CHECK(pool_validate(&s) == POOL_OK && s.blocks == 1 && s.free_blocks == 1, "fully coalesced");
    }
    report("T4 memory full / fragmentation / reuse / full coalescing", f0);

    /* T5: free head / middle / tail and coalescing in each direction */
    f0 = g_fail;
    {
        pool_init();
        void *a = pool_alloc(100), *b = pool_alloc(200), *c = pool_alloc(300), *d = pool_alloc(400);
        void *guard = pool_alloc(8);
        pool_stats s;
        CHECK(a && b && c && d && guard, "allocs");
        CHECK(pool_free(b) == POOL_OK && pool_validate(&s) == POOL_OK && s.free_blocks == 2, "free middle (b)");
        CHECK(pool_free(a) == POOL_OK && pool_validate(&s) == POOL_OK && s.free_blocks == 2, "free head (a): merges with b");
        CHECK(pool_free(c) == POOL_OK && pool_validate(&s) == POOL_OK && s.free_blocks == 2, "free c: merges a+b+c");
        void *big = pool_alloc(POOL_ROUND(100) + POOL_ROUND(200) + POOL_ROUND(300) + 2 * POOL_HDR);
        CHECK(big == a, "merged region a+b+c is reusable as one block");
        CHECK(pool_free(big) == POOL_OK, "free big");
        CHECK(pool_free(guard) == POOL_OK && pool_validate(&s) == POOL_OK && s.free_blocks == 2, "free tail (guard): merges with trailing space");
        CHECK(pool_free(d) == POOL_OK && pool_validate(&s) == POOL_OK && s.blocks == 1, "free d: everything merged");
    }
    report("T5 head/middle/tail free and coalescing", f0);

    /* T6: double free / interior pointers / header pointers */
    f0 = g_fail;
    {
        pool_init();
        unsigned char *a = (unsigned char *)pool_alloc(64), *b = (unsigned char *)pool_alloc(64), *c = (unsigned char *)pool_alloc(64);
        CHECK(pool_free(b) == POOL_OK, "free b");
        take_snapshot();
        CHECK(pool_free(b) == POOL_ERR_DOUBLE_FREE && same_as_snapshot(), "double free detected");
        CHECK(pool_free(a + 1) == POOL_ERR_INVALID_PTR && same_as_snapshot(), "interior pointer +1");
        CHECK(pool_free(a + POOL_ALIGN) == POOL_ERR_INVALID_PTR && same_as_snapshot(), "interior aligned pointer");
        CHECK(pool_free(a - POOL_HDR) == POOL_ERR_INVALID_PTR && same_as_snapshot(), "header address");
        CHECK(pool_free(b + 8) == POOL_ERR_NOT_ALLOCATED && same_as_snapshot(), "pointer into free block");
        void *q = a + 5;
        CHECK(pool_realloc(&q, 10) == POOL_ERR_INVALID_PTR && q == a + 5 && same_as_snapshot(), "realloc interior pointer");
        q = b;
        CHECK(pool_realloc(&q, 10) == POOL_ERR_DOUBLE_FREE && q == b && same_as_snapshot(), "realloc freed pointer");
        CHECK(pool_free(a) == POOL_OK, "free a (merges with b)");
        take_snapshot();
        CHECK(pool_free(b) == POOL_ERR_NOT_ALLOCATED && same_as_snapshot(), "double free after coalescing detected");
        CHECK(pool_free(a) == POOL_ERR_DOUBLE_FREE && same_as_snapshot(), "double free of merged head");
        CHECK(pool_free(c) == POOL_OK && valid(), "free c");
        CHECK(pool_free(c) != POOL_OK && valid(), "double free c");
    }
    report("T6 double free / interior / header pointers rejected, state unchanged", f0);

    /* T7: realloc shrink / grow in place / move / failure */
    f0 = g_fail;
    {
        pool_init();
        pool_stats s0, s1;
        void *a = pool_alloc(1000); fill(a, 1000, 1);
        void *b = pool_alloc(100);  fill(b, 100, 2);
        void *p = a;
        CHECK(pool_validate(&s0) == POOL_OK, "valid");
        CHECK(pool_realloc(&p, 300) == POOL_OK && p == a && verify(p, 300, 1), "shrink in place keeps data");
        CHECK(pool_validate(&s1) == POOL_OK && s1.free_blocks == s0.free_blocks + 1, "shrink split off a free block");
        CHECK(pool_realloc(&p, 900) == POOL_OK && p == a && verify(p, 300, 1), "grow in place into freed tail");
        CHECK(verify(b, 100, 2), "neighbour intact");
        fill(p, 900, 3);
        void *old = p;
        CHECK(pool_realloc(&p, 5000) == POOL_OK && p != old && verify(p, 900, 3) && aligned(p), "grow with move keeps data");
        CHECK(pool_usable_size(old) == 0, "old block released");
        CHECK(verify(b, 100, 2) && valid(), "neighbour intact after move");
        take_snapshot();
        void *keep = p;
        CHECK(pool_realloc(&p, 65000) == POOL_ERR_NOMEM && p == keep && same_as_snapshot(), "failed grow: pointer, data and pool unchanged");
        CHECK(pool_realloc(&p, SIZE_MAX) == POOL_ERR_NOMEM && p == keep && same_as_snapshot(), "realloc SIZE_MAX unchanged");
        CHECK(pool_realloc(&p, 0) == POOL_ERR_ARG && p == keep && same_as_snapshot(), "realloc to 0 rejected unchanged");
        CHECK(verify(p, 900, 3), "data still intact after failures");
        void *z = NULL;
        CHECK(pool_realloc(&z, 0) == POOL_OK && z == NULL && same_as_snapshot(), "realloc(NULL,0) no-op");
        CHECK(pool_realloc(&z, 40) == POOL_OK && z != NULL && aligned(z), "realloc(NULL,n) allocates");
        CHECK(pool_realloc(NULL, 5) == POOL_ERR_ARG, "NULL pp");
        CHECK(pool_free(z) == POOL_OK && pool_free(p) == POOL_OK && pool_free(b) == POOL_OK, "free all");
        pool_stats s; CHECK(pool_validate(&s) == POOL_OK && s.blocks == 1, "single free block");
        /* grow to exactly the whole pool in place */
        void *w = pool_alloc(16); fill(w, 16, 4);
        CHECK(pool_realloc(&w, pool_max_alloc()) == POOL_OK && verify(w, 16, 4) && valid(), "grow in place to max");
        CHECK(pool_realloc(&w, 1) == POOL_OK && verify(w, 1, 4) && valid(), "shrink max to 1");
        CHECK(pool_free(w) == POOL_OK && valid(), "free");
    }
    report("T7 realloc shrink / grow in place / move / failure keeps old state", f0);

    /* T8: 20000 deterministic random operations against a reference model */
    f0 = g_fail;
    {
        pool_init();
        enum { MAXLIVE = 2200, OPS = 20000 };
        static live_t live[MAXLIVE], sorted[MAXLIVE];
        size_t nl = 0;
        long n_alloc = 0, n_fail = 0, n_free = 0, n_re = 0, n_refail = 0, n_bad = 0;
        unsigned seed = 1;
        for (int op = 0; op < OPS; ++op) {
            uint64_t r = rnd();
            unsigned kind = (unsigned)(r % 100);
            size_t sz;
            uint64_t r2 = rnd();
            switch (r2 % 4) { case 0: sz = 1 + (size_t)(r2 >> 8) % 16; break; case 1: sz = 1 + (size_t)(r2 >> 8) % 256; break;
                               case 2: sz = 1 + (size_t)(r2 >> 8) % 4096; break; default: sz = 1 + (size_t)(r2 >> 8) % 20000; }
            pool_stats st; pool_validate(&st);
            if (kind < 45 || nl == 0) {
                void *q = pool_alloc(sz);
                int could = st.largest_free_payload >= POOL_ROUND(sz);
                if (q) {
                    CHECK(could && aligned(q) && pool_usable_size(q) >= sz, "op %d alloc %zu consistent", op, sz);
                    if (nl < MAXLIVE) { fill(q, sz, seed); live[nl].p = (unsigned char *)q; live[nl].n = sz; live[nl].seed = seed++; ++nl; }
                    else pool_free(q);
                    ++n_alloc;
                } else { CHECK(!could, "op %d alloc %zu failed although a block fits", op, sz); ++n_fail; }
            } else if (kind < 80) {
                size_t i = (size_t)(rnd() % nl);
                CHECK(verify(live[i].p, live[i].n, live[i].seed), "op %d content before free", op);
                CHECK(pool_free(live[i].p) == POOL_OK, "op %d free", op);
                CHECK(pool_free(live[i].p) != POOL_OK, "op %d immediate double free rejected", op);
                live[i] = live[--nl]; ++n_free;
            } else if (kind < 97) {
                size_t i = (size_t)(rnd() % nl);
                void *q = live[i].p;
                take_snapshot();
                pool_status rs2 = pool_realloc(&q, sz);
                if (rs2 == POOL_OK) {
                    size_t keep = live[i].n < sz ? live[i].n : sz;
                    CHECK(aligned(q) && verify(q, keep, live[i].seed), "op %d realloc %zu->%zu keeps data", op, live[i].n, sz);
                    live[i].p = (unsigned char *)q; live[i].n = sz; fill(q, sz, live[i].seed);
                    ++n_re;
                } else {
                    CHECK(rs2 == POOL_ERR_NOMEM && q == live[i].p && same_as_snapshot(), "op %d failed realloc changed state", op);
                    ++n_refail;
                }
            } else {
                size_t i = (size_t)(rnd() % nl);
                take_snapshot();
                size_t off = 1 + (size_t)(rnd() % (live[i].n > 1 ? live[i].n - 1 : 1));
                if (off < live[i].n) CHECK(pool_free(live[i].p + off) == POOL_ERR_INVALID_PTR && same_as_snapshot(), "op %d interior pointer", op);
                ++n_bad;
            }
            if (!valid()) { CHECK(0, "op %d pool_validate failed", op); break; }
            if (op % 200 == 0 || op == OPS - 1) {
                pool_stats s2; pool_validate(&s2);
                CHECK(s2.used_blocks == nl, "op %d model live=%zu pool used=%zu", op, nl, s2.used_blocks);
                memcpy(sorted, live, nl * sizeof live[0]);
                qsort(sorted, nl, sizeof sorted[0], cmp_live);
                for (size_t i = 0; i < nl; ++i) {
                    if (!verify(sorted[i].p, sorted[i].n, sorted[i].seed)) CHECK(0, "op %d content of live %zu", op, i);
                    if (i + 1 < nl && (uintptr_t)(sorted[i].p + sorted[i].n) > (uintptr_t)sorted[i + 1].p) CHECK(0, "op %d overlap", op);
                }
            }
        }
        for (size_t i = 0; i < nl; ++i) CHECK(pool_free(live[i].p) == POOL_OK, "final free");
        pool_stats s; CHECK(pool_validate(&s) == POOL_OK && s.blocks == 1 && s.free_bytes == POOL_SIZE, "pool empty at end");
        printf("  ops=%d alloc_ok=%ld alloc_fail=%ld free=%ld realloc_ok=%ld realloc_fail=%ld bad_ptr=%ld\n",
               OPS, n_alloc, n_fail, n_free, n_re, n_refail, n_bad);
    }
    report("T8 20000 deterministic random ops vs reference model", f0);

    /* T9: validate detects corruption (test-only header damage) */
    f0 = g_fail;
    {
        pool_init();
        unsigned char *a = (unsigned char *)pool_alloc(32); (void)pool_alloc(32);
        CHECK(valid(), "valid");
        a[-1] ^= 0x40; /* corrupt a header byte on purpose */
        CHECK(pool_validate(NULL) == POOL_ERR_CORRUPT, "corruption detected");
        a[-1] ^= 0x40;
        CHECK(valid(), "valid again");
    }
    report("T9 pool_validate detects damaged boundary tags", f0);

    printf("RESULT: %d checks passed, %d failed\n", g_pass, g_fail);
    return g_fail ? 1 : 0;
}
```

#### 5. 検証結果（期待値と実測値を区別）

Clang の3構成とも `RESULT: 36999 checks passed, 0 failed`、終了コード 0 でした（MSVC は上記の理由で対象外）。「状態不変」は、プール全体 65536 バイトのスナップショットを `memcmp` で比較して確認しています。

| テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 初期化前の呼び出し（T0） | alloc は NULL、validate/realloc は POOL_ERR_STATE、free(NULL) は成功 | 一致 | 実施（Clang 3構成） |
| 1/2/3/7/8/16/4096バイト（T1） | 非NULL、`_Alignof(max_align_t)` に整列、使用可能サイズ ≥ n、内容が壊れない | 一致（15/17/31/33 も追加で確認） | 実施 |
| サイズ0（T2） | NULL を返しプールは1バイトも変わらない | 一致 | 実施 |
| SIZE_MAX、SIZE_MAX−16+1、65536、最大+1（T2） | NULL、状態不変（切り上げの桁あふれなし） | 一致 | 実施 |
| 最大確保可能サイズ（T2） | 65520 バイトが成功し、その後 alloc(1) は失敗 | 一致 | 実施 |
| 端数アライメント（T3） | 1バイト → 1単位、整列値+1 → 2単位。隣接領域は重ならない | 一致 | 実施 |
| メモリ満杯（T4） | 1バイト確保を繰り返すと 65536/24 = 2730 個で満杯。満杯時の alloc は失敗し状態不変 | 2730 個、状態不変 | 実施 |
| 断片化と再利用（T4） | 偶数番目を解放すると空き1365個。2単位の要求は失敗し、1単位の要求は最初の穴を再利用 | 一致 | 実施 |
| 先頭・中間・末尾の free と結合（T5） | b → a → c の順に解放すると a+b+c が1ブロックに結合して再利用でき、最後は全体が1ブロック | 一致 | 実施 |
| 二重free（T6） | 隣が使用中なら POOL_ERR_DOUBLE_FREE、結合後は POOL_ERR_NOT_ALLOCATED。状態不変 | 一致 | 実施 |
| 途中ポインター（T6） | +1、+整列値、ヘッダー位置 → POOL_ERR_INVALID_PTR。realloc でも同様。状態不変 | 一致 | 実施 |
| realloc 縮小（T7） | 同じアドレスでデータを保持し、残りを空きブロックへ分割 | 一致 | 実施 |
| realloc 拡大（その場）（T7） | 後続の空きを使い同じアドレスのまま。旧内容と隣接ブロックを保持 | 一致 | 実施 |
| realloc 移動（T7） | 別アドレスへ移動し旧内容を保持。旧ブロックは解放される | 一致 | 実施 |
| realloc 失敗（T7） | 65000 / SIZE_MAX → NOMEM、0 → ARG。ポインター・データ・プール全体が不変 | 一致（スナップショット一致） | 実施 |
| realloc(NULL, n) / (NULL, 0)（T7） | 確保 / 何もしない | 一致 | 実施 |
| 決定的ランダム操作と参照モデル（T8） | 20,000 操作（確保 7,066、確保失敗 1,927、解放 6,993、realloc 成功 2,687、失敗 686、不正ポインター 641）。毎回 validate 成功。200操作ごとに全生存領域の内容・重なりなし・使用ブロック数がモデルと一致。確保失敗は「最大空きブロックが不足」の場合だけ | すべて一致 | 実施 |
| pool_validate の検出力（T9） | ヘッダーを1ビット壊すと POOL_ERR_CORRUPT、戻すと OK | 一致 | 実施 |
| ASan / UBSan | 報告なし | 報告なし | 実施 |
| MSVC ビルド | — | `max_align_t` 未定義のためビルド不可 | 対象外 |
| GCC・Linux | — | 未実施 | 未実施 |

#### 6. 未実施項目・制約・残課題

- MSVC ではビルドできません（前述のコンパイラ非準拠）。GCC・Linux と、`_Alignof(max_align_t) = 16` の環境での実行は未実施です。
- 計算量：free・realloc・usable_size はブロック列を先頭から走査するため O(ブロック数)（最大2730）、alloc は first-fit で O(ブロック数) です。仕様の範囲では十分ですが、O(1) のフリーリスト方式ではありません。
- ISO C の型の規則上の制約：静的な `unsigned char` 配列から切り出した領域へ利用者が別の型で書き込むことは、C17 の有効型規則（6.5p6）では malloc の領域ほど明確には保証されていません。これは純粋な C で書くメモリプール全般に共通する限界です。プール自身のメタデータは `memcpy` だけで読み書きしています。
- 範囲判定は `uintptr_t` への変換（処理系定義の動作）で行い、その後にポインターの等値比較で検証しています。プールと全く無関係なポインターは問題文のとおり対象外です。
- 1スレッド専用です（排他制御なし）。

---

## Q03【地獄級】C11アトミックによるSPSCリングバッファとメモリ順序の証明

**狙い：** data race、happens-before、release/acquire、unsignedカウンターの周回、非ブロッキング処理。

### 元コード（危険）

~~~c
#include <stdatomic.h>
#include <stdint.h>
typedef struct {
    atomic_uint_fast64_t read_pos, write_pos;
    uint64_t slots[256];
} Spsc;
int push_bad(Spsc *q, uint64_t v) {
    uint64_t w = atomic_load_explicit(&q->write_pos, memory_order_relaxed);
    uint64_t r = atomic_load_explicit(&q->read_pos, memory_order_relaxed);
    if (w - r == 256) return 0;
    q->slots[w & 255] = v;
    atomic_store_explicit(&q->write_pos, w + 1, memory_order_relaxed);
    return 1;
}
~~~

### 要求仕様

- 1 producer・1 consumer限定の **spsc_init / spsc_try_push / spsc_try_pop** を作り、256個のuint64_tを格納する。
- fullのpushとemptyのpopは即時失敗し、既存のデータや失敗したpopの出力先を変更しない。動的メモリ・mutexは禁止。
- atomicのロード・ストアに妥当なmemory_orderを指定し、**非atomicなslotsを並列で安全に使える理由**をhappens-beforeの関係で説明する。
- uint_fast64_tの符号なし算術によるカウンター周回を考慮し、容量と差分の不変条件を説明する。テストでは両位置を UINT_FAST64_MAX 近傍の同じ値に安全に初期設定して周回させる。
- 64ビット幅以上のuint_fast64_tと、そのアトミック型が使用可能なC17環境を対象とする。並列テストはpthreadを使用可。

### 必須検証

空・満杯・FIFO順序・交互実行・カウンター周回・100万件以上の2スレッド送受信・全要素が重複なく順序保持されること・可能ならThreadSanitizer。ベンチマーク結果だけで正当性を主張しない。

### ★★FREYJAの回答【Q03・記入欄】

> **ここから下が回答用スペースです。** GitHubの鉛筆アイコンからファイルを編集し、各「（ここに記入）」やコード枠内の例示文を自分の回答に置き換えてください。問題文・要求仕様・元コードは変更しないこと。

#### 1. 根本原因・危険性・影響

**元コード `push_bad` の問題点（静的解析）**

| # | 箇所 | 根本原因 | 危険性・影響 |
| --- | --- | --- | --- |
| 1 | `store(write_pos, w+1, relaxed)` | release になっていない | 消費者が `write_pos` の新しい値を読んでも、`slots[w]` への書き込みとの happens-before が成立しない。非atomic な `slots` へのデータ競合は C11 では未定義動作で、未書き込みの古い値やちぎれた値を読む可能性がある（弱いメモリ順序の CPU やコンパイラの並べ替えで顕在化） |
| 2 | `load(read_pos, relaxed)` | acquire になっていない | 消費者がスロットを読み終える前に生産者が上書きできる（読み取り→書き込みの競合、WAR）。データ破損 |
| 3 | `uint64_t w = ...` | `uint_fast64_t` を `uint64_t` で受けている | `uint_fast64_t` が64ビットより広い処理系では値が切り詰められ、差分と周回の計算が壊れる |
| 4 | pop がない、初期化がない | — | 消費者側の正しい順序づけ（acquire で読み、release で解放）が定義されていない |
| 5 | 同じキャッシュラインに両カウンター | 配置を考慮していない | 偽共有（性能の問題のみ） |

**修正方針：** 各位置は所有者のスレッドだけが書き込みます（`write_pos` は生産者、`read_pos` は消費者）。自分の位置は relaxed で読み、相手の位置は acquire で読み、自分の位置の更新は release で公開します。満杯の判定は符号なしの差分 `w − r ≥ 256`、空の判定は `w == r` です。失敗時は `slots` にも出力先にも触れません。動的メモリと mutex は使っていません。テスト用に `spsc_init_at(q, start)` を用意し、両位置を `UINT_FAST64_MAX` 付近の同じ値から始めて周回させられるようにしました。

#### 2. 完全な修正版Cコード

```c
/*
 * Q03: single-producer / single-consumer ring buffer with C11 atomics (C17)
 *
 * - capacity 256 uint64_t, no dynamic memory, no mutex
 * - write_pos is written only by the producer, read_pos only by the consumer
 * - positions are free-running uint_fast64_t counters (modulo 2^N, N >= 64);
 *   slot index = pos % 256, fill level = write_pos - read_pos (unsigned)
 *
 * Original implementation by ★★FREYJA for the C hell-level exam.
 */
#include <stdatomic.h>
#include <stdbool.h>
#include <stdint.h>

/* ------------------------------------------------------------------ API */

#define SPSC_CAPACITY 256u

typedef struct {
    _Alignas(64) atomic_uint_fast64_t write_pos;   /* producer-owned cache line */
    _Alignas(64) atomic_uint_fast64_t read_pos;    /* consumer-owned cache line */
    _Alignas(64) uint64_t slots[SPSC_CAPACITY];
} spsc_queue;

/* Not thread-safe: call before the producer and consumer threads start. */
void spsc_init(spsc_queue *q);
void spsc_init_at(spsc_queue *q, uint_fast64_t start);   /* test hook: start counters at `start` */
bool spsc_try_push(spsc_queue *q, uint64_t value);       /* producer thread only */
bool spsc_try_pop(spsc_queue *q, uint64_t *out);         /* consumer thread only */

/* ------------------------------------------------------- implementation */

_Static_assert((SPSC_CAPACITY & (SPSC_CAPACITY - 1u)) == 0, "capacity must be a power of two");
_Static_assert(UINT_FAST64_MAX >= UINT64_MAX, "uint_fast64_t must be at least 64 bits");
/* 2^N is a multiple of 256 for any N >= 8, so pos % 256 stays continuous when
 * the counter wraps from UINT_FAST64_MAX to 0. */

void spsc_init_at(spsc_queue *q, uint_fast64_t start)
{
    atomic_init(&q->write_pos, start);
    atomic_init(&q->read_pos, start);
    for (unsigned i = 0; i < SPSC_CAPACITY; ++i)
        q->slots[i] = 0;
}

void spsc_init(spsc_queue *q) { spsc_init_at(q, 0); }

bool spsc_try_push(spsc_queue *q, uint64_t value)
{
    /* Own counter: only this thread writes it, relaxed is enough. */
    uint_fast64_t w = atomic_load_explicit(&q->write_pos, memory_order_relaxed);
    /* acquire pairs with the consumer's release store of read_pos: the
     * consumer's read of the slot we may now overwrite happens-before our write. */
    uint_fast64_t r = atomic_load_explicit(&q->read_pos, memory_order_acquire);
    uint_fast64_t used = (uint_fast64_t)(w - r);          /* 0..256, wrap-safe */
    if (used >= SPSC_CAPACITY)
        return false;                                      /* full: nothing touched */
    q->slots[(size_t)(w % SPSC_CAPACITY)] = value;
    /* release publishes the slot content before the new write_pos. */
    atomic_store_explicit(&q->write_pos, (uint_fast64_t)(w + 1u), memory_order_release);
    return true;
}

bool spsc_try_pop(spsc_queue *q, uint64_t *out)
{
    uint_fast64_t r = atomic_load_explicit(&q->read_pos, memory_order_relaxed);
    /* acquire pairs with the producer's release store of write_pos: the slot
     * write happens-before our read of it. */
    uint_fast64_t w = atomic_load_explicit(&q->write_pos, memory_order_acquire);
    if (w == r)
        return false;                                      /* empty: *out untouched */
    uint64_t v = q->slots[(size_t)(r % SPSC_CAPACITY)];
    /* release: our read of the slot happens-before the producer reusing it. */
    atomic_store_explicit(&q->read_pos, (uint_fast64_t)(r + 1u), memory_order_release);
    *out = v;
    return true;
}
```

#### 3. ビルド方法・使用環境

```sh
# PowerShell 7 で MSVC 開発環境（vcvars64.bat）を読み込んだ後、build.ps1 が次の4構成を順にビルド・実行する
# q03_test.c は #include "q03.c" により実装と同じ翻訳単位でビルドする
W='-Wall -Wextra -Wpedantic -Wshadow -Wconversion -Wsign-conversion -Wcast-qual -Wstrict-prototypes -Wmissing-prototypes -Wvla -Wformat=2'
# (1) Clang -O2
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O2 -fuse-ld=link q03_test.c -o q03_rel.exe
# (2) Clang + AddressSanitizer（MSVC 同梱の ASan ランタイムでリンク）
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O1 -g -fsanitize=address -fno-omit-frame-pointer -fms-runtime-lib=dll -c q03_test.c -o q03_asan.obj
link /nologo /DEBUG /OUT:q03_asan.exe q03_asan.obj clang_rt.asan_dynamic-x86_64.lib clang_rt.asan_dynamic_runtime_thunk-x86_64.lib msvcrt.lib ucrt.lib vcruntime.lib legacy_stdio_definitions.lib
# (3) Clang + UndefinedBehaviorSanitizer（トラップ方式）
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O1 -g -fsanitize=undefined -fsanitize-trap=undefined -fno-sanitize-recover=all -fuse-ld=link q03_test.c -o q03_ubsan.exe
# (4) MSVC（2つ目のコンパイラ）
cl /nologo /std:c17 /W4 /experimental:c11atomics /utf-8 /O2 /D_CRT_SECURE_NO_WARNINGS q03_test.c /Fe:q03_msvc.exe
# 各 .exe を実行し、終了コード 0 と "RESULT: N checks passed, 0 failed" を確認
# 追加（コンパイルのみ・実行は未実施）: 弱いメモリ順序の aarch64 向け
clang --target=aarch64-linux-android34 -std=c17 -Wall -Wextra -Wpedantic -Wshadow -Wconversion -Wsign-conversion -O2 q03_test.c -o q03_test_aarch64
```

**使用環境（全問共通）**

- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド、x86-64）
- コンパイラ1：**Clang 18.0.3**（Android NDK r27c に同梱。ターゲットは `x86_64-pc-windows-msvc`。ヘッダー・ライブラリは MSVC 14.52.37025 と Windows SDK/UCRT 10.0.26100、リンカーは link.exe）。`-std=c17` を指定。
- コンパイラ2：**MSVC cl 19.52.37029**（Visual Studio 18 Community）、`/std:c17 /W4`。
- GCC：この PC にはなく、使用していません。Linux／WSL の環境もありません（外部パッケージの無断導入禁止のため、導入していません）。
- AddressSanitizer：Clang でインストルメントし、MSVC 同梱の ASan ランタイム（`clang_rt.asan_dynamic-x86_64`）でリンクしました。ヒープ境界外書き込みの陽性対照で、`heap-buffer-overflow` を検出することを確認済みです。
- UndefinedBehaviorSanitizer：`-fsanitize=undefined -fsanitize-trap=undefined`（トラップ方式。ランタイムを使わず、未定義動作で不正命令の例外として異常終了します）。符号付きオーバーフローの陽性対照で、異常終了することを確認済みです。
- LeakSanitizer：Windows 版 ASan にないため未実施です。リークは各問の計数アロケーター（確保と解放の数の差）で検査しました。ThreadSanitizer：Windows では使えないため未実施です。
- ビルド補助 `shim/stdatomic.h`（製品コード外）：NDK の Clang に付属する `stdatomic.h` は Android（bionic）専用で Windows ではコンパイルできないため、MSVC の C11 実装 `vcruntime_c11_stdatomic.h`（Clang 互換の `__c11_atomic` 組み込み関数を使用）を読み込む1ファイルを置きました。Clang 18 が `__typeof_unqual__` に対応していないため、`atomic_is_lock_free` だけは `__c11_atomic_is_lock_free` で定義し直しています。
- `-D_CRT_SECURE_NO_WARNINGS`：MSVC の CRT が出す「`fopen` などの代わりに `_s` 版を使うように」という非推奨の通知だけを止める定義です（C17 標準関数の使用に対する通知であり、コードの欠陥を隠すものではありません）。

- Q03 の結果：Clang の全構成で警告0件。MSVC は C4324（`_Alignas(64)` による意図したパディングの通知）のみ。4構成ともビルド成功。aarch64 向けもビルド成功（警告0件、ldar/stlr 系の命令が生成されていることを逆アセンブルで確認）。
- 並列テストのスレッドには ISO C11 の `<threads.h>` を使いました（POSIX pthread と同等の用途で、標準Cの範囲内です）。

#### 4. テストコード

```c
/* Q03 tests. Threads: ISO C11 <threads.h> (POSIX pthread would be equivalent). */
#include "q03.c"
#include <stdio.h>
#include <string.h>
#include <threads.h>
#include <time.h>

static int g_fail = 0, g_pass = 0;
#define CHECK(cond, ...) do { if (cond) { ++g_pass; } else { ++g_fail; \
    printf("  FAIL %s:%d: ", __FILE__, __LINE__); printf(__VA_ARGS__); printf("\n"); } } while (0)
static void report(const char *name, int before_fail)
{
    printf("[%s] %s\n", g_fail == before_fail ? "PASS" : "FAIL", name);
}

static spsc_queue Q;

/* ---------- concurrent test ---------- */
typedef struct {
    spsc_queue *q;
    uint64_t count;
    uint64_t base;            /* values are base, base+1, ... */
    uint64_t spins;           /* failed attempts */
    uint64_t errors;          /* consumer: out-of-order / duplicate / missing */
    uint64_t first_bad_index;
    uint64_t sum;
    atomic_int *go;
} worker;

static int producer_main(void *arg)
{
    worker *w = (worker *)arg;
    while (!atomic_load_explicit(w->go, memory_order_acquire)) { }
    for (uint64_t i = 0; i < w->count; ++i) {
        while (!spsc_try_push(w->q, w->base + i)) w->spins++;
    }
    return 0;
}

static int consumer_main(void *arg)
{
    worker *w = (worker *)arg;
    while (!atomic_load_explicit(w->go, memory_order_acquire)) { }
    for (uint64_t i = 0; i < w->count; ++i) {
        uint64_t v = 0xDEADBEEFDEADBEEFu;
        while (!spsc_try_pop(w->q, &v)) {
            w->spins++;
            if (v != 0xDEADBEEFDEADBEEFu) { w->errors++; v = 0xDEADBEEFDEADBEEFu; }   /* failed pop wrote output */
        }
        if (v != w->base + i) {               /* exact sequence: no loss, no duplicate, FIFO */
            if (w->errors == 0) w->first_bad_index = i;
            w->errors++;
        }
        w->sum += v;
    }
    return 0;
}

static int run_concurrent(uint_fast64_t start, uint64_t n, int consumer_first, double *secs, uint64_t *pspins, uint64_t *cspins)
{
    spsc_init_at(&Q, start);
    atomic_int go;
    atomic_init(&go, 0);
    worker p = { &Q, n, 1000, 0, 0, 0, 0, &go }, c = { &Q, n, 1000, 0, 0, 0, 0, &go };
    thrd_t tp, tc;
    int ok = 1;
    if (consumer_first) {
        ok &= thrd_create(&tc, consumer_main, &c) == thrd_success;
        ok &= thrd_create(&tp, producer_main, &p) == thrd_success;
    } else {
        ok &= thrd_create(&tp, producer_main, &p) == thrd_success;
        ok &= thrd_create(&tc, consumer_main, &c) == thrd_success;
    }
    if (!ok) return -1;
    struct timespec t0, t1;
    timespec_get(&t0, TIME_UTC);
    atomic_store_explicit(&go, 1, memory_order_release);
    thrd_join(tp, NULL);
    thrd_join(tc, NULL);
    timespec_get(&t1, TIME_UTC);
    *secs = (double)(t1.tv_sec - t0.tv_sec) + (double)(t1.tv_nsec - t0.tv_nsec) / 1e9;
    *pspins = p.spins; *cspins = c.spins;
    uint64_t expect_sum = n * 1000u + n * (n - 1) / 2;
    uint_fast64_t endw = atomic_load(&Q.write_pos), endr = atomic_load(&Q.read_pos);
    if (c.errors) printf("  consumer errors=%llu first bad index=%llu\n", (unsigned long long)c.errors, (unsigned long long)c.first_bad_index);
    return (c.errors == 0 && c.sum == expect_sum && endw == endr && endw == (uint_fast64_t)(start + n)) ? 0 : 1;
}

int main(void)
{
    int f0;
    const uint64_t SENT = 0x0123456789ABCDEFu;
    printf("  sizeof(uint_fast64_t)=%zu atomic_is_lock_free=%d\n", sizeof(uint_fast64_t), (int)atomic_is_lock_free(&Q.write_pos));

    /* T1: empty */
    f0 = g_fail;
    {
        spsc_init(&Q);
        uint64_t v = SENT;
        CHECK(!spsc_try_pop(&Q, &v) && v == SENT, "pop on empty fails, output unchanged");
        CHECK(!spsc_try_pop(&Q, &v) && v == SENT, "again");
    }
    report("T1 empty pop fails and leaves output untouched", f0);

    /* T2: full */
    f0 = g_fail;
    {
        spsc_init(&Q);
        for (uint64_t i = 0; i < 256; ++i) CHECK(spsc_try_push(&Q, i * 3u), "push %llu", (unsigned long long)i);
        uint64_t before[256]; memcpy(before, Q.slots, sizeof before);
        CHECK(!spsc_try_push(&Q, 999), "257th push fails");
        CHECK(memcmp(before, Q.slots, sizeof before) == 0, "failed push did not modify slots");
        CHECK(atomic_load(&Q.write_pos) - atomic_load(&Q.read_pos) == 256, "fill level 256");
        for (uint64_t i = 0; i < 256; ++i) { uint64_t v = SENT; CHECK(spsc_try_pop(&Q, &v) && v == i * 3u, "pop %llu", (unsigned long long)i); }
        uint64_t v = SENT;
        CHECK(!spsc_try_pop(&Q, &v) && v == SENT, "empty again");
    }
    report("T2 full: 256 accepted, 257th rejected, data unchanged, FIFO", f0);

    /* T3: interleaved push/pop pattern */
    f0 = g_fail;
    {
        spsc_init(&Q);
        uint64_t next_in = 0, next_out = 0;
        for (int round = 0; round < 5000; ++round) {
            int np = (round * 7) % 13, nc = (round * 5) % 11;
            for (int k = 0; k < np; ++k) { if (spsc_try_push(&Q, next_in)) ++next_in; }
            for (int k = 0; k < nc; ++k) { uint64_t v = SENT; if (spsc_try_pop(&Q, &v)) { if (v != next_out) CHECK(0, "order %llu", (unsigned long long)v); ++next_out; } }
            CHECK(next_in - next_out <= 256, "fill level bound");
        }
        uint64_t v;
        while (spsc_try_pop(&Q, &v)) { if (v != next_out) CHECK(0, "drain order"); ++next_out; }
        CHECK(next_in == next_out, "everything popped exactly once");
    }
    report("T3 interleaved single-thread push/pop keeps FIFO", f0);

    /* T4: counter wrap-around near UINT_FAST64_MAX */
    f0 = g_fail;
    {
        uint_fast64_t starts[] = { UINT_FAST64_MAX, UINT_FAST64_MAX - 1u, UINT_FAST64_MAX - 100u, UINT_FAST64_MAX - 255u, UINT_FAST64_MAX - 256u, UINT_FAST64_MAX - 1000u };
        for (size_t s = 0; s < sizeof starts / sizeof starts[0]; ++s) {
            spsc_init_at(&Q, starts[s]);
            /* fill completely across the wrap point, check full, drain */
            for (uint64_t i = 0; i < 256; ++i) CHECK(spsc_try_push(&Q, i), "wrap push");
            CHECK(!spsc_try_push(&Q, 7), "full detected across wrap (start %zu)", s);
            for (uint64_t i = 0; i < 256; ++i) { uint64_t v = SENT; CHECK(spsc_try_pop(&Q, &v) && v == i, "wrap pop"); }
            uint64_t v = SENT;
            CHECK(!spsc_try_pop(&Q, &v) && v == SENT, "empty detected across wrap");
            /* stream 3000 items through so that both counters pass the wrap point */
            uint64_t in = 0, out = 0;
            while (out < 3000) {
                while (in < 3000 && spsc_try_push(&Q, in)) ++in;
                while (spsc_try_pop(&Q, &v)) { if (v != out) CHECK(0, "stream order"); ++out; }
            }
            CHECK(atomic_load(&Q.write_pos) == (uint_fast64_t)(starts[s] + 256u + 3000u), "write_pos wrapped as expected");
            CHECK(atomic_load(&Q.write_pos) < 4000u, "counter actually wrapped past zero");
        }
    }
    report("T4 counter wrap-around (start = UINT_FAST64_MAX - k)", f0);

    /* T5: 2 threads, 10,000,000 values, wrap during the run, both start orders, 3 runs */
    f0 = g_fail;
    {
        const uint64_t N = 10000000u;
        struct { uint_fast64_t start; int cf; } runs[] = {
            { UINT_FAST64_MAX - 5000000u, 0 }, { UINT_FAST64_MAX - 123u, 1 }, { 0, 0 }, { UINT_FAST64_MAX - 9999999u, 1 },
        };
        for (size_t r = 0; r < sizeof runs / sizeof runs[0]; ++r) {
            double secs = 0; uint64_t ps = 0, cs = 0;
            int rc = run_concurrent(runs[r].start, N, runs[r].cf, &secs, &ps, &cs);
            CHECK(rc == 0, "concurrent run %zu", r);
            printf("  run %zu: start=UINT_FAST64_MAX-%llu consumer_first=%d items=%llu time=%.3fs (%.1f M/s) push_retries=%llu pop_retries=%llu -> %s\n",
                   r, (unsigned long long)(UINT_FAST64_MAX - runs[r].start), runs[r].cf, (unsigned long long)N, secs,
                   (double)N / secs / 1e6, (unsigned long long)ps, (unsigned long long)cs, rc == 0 ? "exact sequence" : "ERROR");
        }
    }
    report("T5 two threads x 10M items: exact order, no loss/duplicate, wrap during run", f0);

    printf("RESULT: %d checks passed, %d failed\n", g_pass, g_fail);
    return g_fail ? 1 : 0;
}
```

#### 5. 検証結果（期待値と実測値を区別）

4構成とも `RESULT: 8619 checks passed, 0 failed`、終了コード 0 でした。下表の並列テストの数値は Clang -O2 版の実測値です（他の構成も全回 exact sequence）。

| テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 空（T1） | pop は false、出力先は番兵値のまま | 一致 | 実施（4構成） |
| 満杯（T2） | 256件成功、257件目は false。slots の内容は不変。その後 FIFO で256件取り出せる | 一致 | 実施（4構成） |
| FIFO 順序・交互実行（T3） | 5000ラウンドの不規則な push/pop で順序どおり、充填数 ≤ 256、全件ちょうど1回 | 一致 | 実施（4構成） |
| カウンター周回（T4） | 開始値 UINT_FAST64_MAX、−1、−100、−255、−256、−1000 で、周回をまたいでも満杯・空の判定と FIFO が正しい。3256件流した後の write_pos は 0 付近まで周回 | 6通りすべて一致 | 実施（4構成） |
| 2スレッド・1000万件 ×4回（T5） | 消費者が受け取る値が 1000, 1001, … と1件の欠けも重複もなく完全に連続。合計値・最終位置も一致 | 4回すべて exact sequence | 実施（4構成） |
| 　run0：開始 UINT_FAST64_MAX−5,000,000（実行中に周回） | 同上 | 0.238秒（42.1M件/秒） | 実施 |
| 　run1：開始 UINT_FAST64_MAX−123、消費者を先に起動 | 同上 | 0.160秒 | 実施 |
| 　run2：開始 0 | 同上 | 0.192秒 | 実施 |
| 　run3：開始 UINT_FAST64_MAX−9,999,999（終了時にちょうど0へ周回） | 同上 | 0.185秒 | 実施 |
| 失敗 pop が出力先を書き換えない（並列中） | 番兵値が変わらない | 0件（違反なし） | 実施 |
| ThreadSanitizer | — | Windows（MSVC ターゲット）では TSan が使えない | 未実施 |
| 弱いメモリ順序の CPU（ARM）での実行 | — | aarch64 向けのビルドのみ成功、実機なし | 未実施 |

ベンチマークの数値は参考値です。正当性の根拠は、6. の happens-before の論証と、完全な連番の検査です。

#### 6. メモリ順序と正当性の説明

**release / acquire・happens-before・unsignedカウンター周回の根拠：**

1. **生産者の書き込み → 消費者の読み取り（RAW）**
   生産者は `slots[w % 256] = v`（非atomic）の後に、`write_pos` へ `w+1` を **release** で store します。消費者は `write_pos` を **acquire** で load し、`w+1` 以降の値を読んだ場合だけ `slots[w % 256]` を読みます。release の store と、その値（またはそれ以降の修正順序の値）を読む acquire の load は synchronizes-with の関係になるため、「スロットへの書き込み → (sequenced-before) → release の store → (synchronizes-with) → acquire の load → (sequenced-before) → スロットの読み取り」が成り立ち、書き込みは読み取りに happens-before します。
2. **消費者の読み取り → 生産者の再利用（WAR）**
   消費者はスロットを読んだ後に、`read_pos` へ `r+1` を **release** で store します。生産者は `read_pos` を **acquire** で load し、`w − r < 256` のとき、つまり読み取り済みの位置 `w − 256 < r` が確定している場合だけ `slots[w % 256]` へ書き込みます。同じ論法で、旧値の読み取りは上書きに happens-before します。
3. **自分の位置は relaxed で十分な理由**
   `write_pos` を書くのは生産者だけです。生産者が自分の最新の store を読むことは、sequenced-before とキャッシュの一貫性（coherence）で保証されます。`read_pos` と消費者の関係も同様です。
4. **データ競合がない理由**
   同じスロットに対する競合しうるアクセスの組（生産者の書き込みと消費者の読み取り、消費者の読み取りと次周の生産者の書き込み）は、すべて 1. と 2. の happens-before で順序づけられています。したがって C11 5.1.2.4 の定義によるデータ競合は存在せず、`slots` を非atomic のまま安全に使えます。
5. **古い値を読んだ場合も安全**
   相手の位置を古い値で読んだ場合、生産者は「満杯」寄り、消費者は「空」寄りの判断をするだけです（偽の失敗）。未公開のデータを読んだり、未読のデータを上書きしたりすることはありません。
6. **符号なしカウンターの周回**
   位置は `uint_fast64_t`（幅 N ≥ 64。この環境では N = 64）の自由に増え続けるカウンターで、算術は 2^N を法として行われます（符号なし整数の周回は定義済みの動作）。不変条件は「0 ≤ (w − r) mod 2^N ≤ 256」です。生産者は差が256未満のときだけ w を1増やし、消費者は w ≠ r のときだけ r を1増やすので、この条件は常に保たれます。差 `w − r` は、w だけが 0 へ周回した直後でも正しい充填数になります。256 は 2^N を割り切るので、添字 `pos % 256` は UINT_FAST64_MAX（%256 = 255）の次が 0 となり、周回の前後で連続します。テスト T4・T5 で実際に周回させて確認しました。

#### 7. 未実施項目・制約・残課題

- ThreadSanitizer は未実施です（Windows／MSVC ターゲットの Clang では利用不可。Linux 環境もありません）。
- テストを実行した CPU は x86-64（TSO）です。x86 では acquire/release が不足していても表面化しにくいため、メモリ順序の正しさの根拠は 6. の形式的な論証です。aarch64 向けのビルドは成功しましたが、実行はしていません。
- 制約：生産者・消費者はそれぞれ1スレッドに限ります（SPSC）。`spsc_init` / `spsc_init_at` はスレッド開始前に呼ぶ必要があります。

---

## Q04【地獄級】RFC 3629に適合するストリーミングUTF-8厳密デコーダ

**狙い：** 1〜4バイト文字、過長符号化、サロゲート、範囲外スカラー、チャンク境界、エラー位置。

### 元コード（危険）

~~~c
#include <stdint.h>
uint32_t decode3_bad(const unsigned char *p) {
    return ((uint32_t)(p[0] & 15) << 12)
         | ((uint32_t)(p[1] & 63) << 6)
         |  (uint32_t)(p[2] & 63);
}
~~~

### 要求仕様

- **utf8_init / utf8_feed / utf8_finish / utf8_reset** を実装。最大4バイトの未完了文字のみ状態保持し、入力全体のコピーを持たない。
- 入力をUnicodeスカラー値（U+0000〜U+10FFFF、ただしU+D800〜U+DFFFを除く）へ復号し、完成したスカラーのみコールバックへ渡す。NUL文字・U+FEFFは有効とする。
- **RFC 3629の最短表現**のみ許す。C0/C1、F5〜FF、不正な継続バイト、ED A0〜BF、F4 90〜BF、5/6バイト形、過長符号化を拒否する。
- 正常スカラーが同一feed内でエラーより先に完成した場合、その通知済み分は取り消さない。エラー後はresetまで拒否。失敗位置は**問題のシーケンスの先頭バイトのストリーム全体での0始まりオフセット**。finishで未完了の場合も同じ位置を返す。
- コールバック失敗を通知し停止する。長い入力でもオフセットのsize_tオーバーフローを検出する。
- UTF-16変換、正規化、文字列の見た目の処理はしない。

### 必須検証

全ASCII境界、C2 80、E0 A0 80、ED 9F BF、EE 80 80、F4 8F BF BF、C0 80、E0 80 80、ED A0 80、F4 90 80 80、F5 80 80 80、孤立80、すべての分割境界、finishで途中切れ、エラー位置、reset後の正常入力。

### ★★FREYJAの回答【Q04・記入欄】

> **ここから下が回答用スペースです。** GitHubの鉛筆アイコンからファイルを編集し、各「（ここに記入）」やコード枠内の例示文を自分の回答に置き換えてください。問題文・要求仕様・元コードは変更しないこと。

#### 1. 根本原因・危険性・影響

**元コード `decode3_bad` の問題点（静的解析）**

| # | 根本原因 | 発生例 | 危険性・影響 |
| --- | --- | --- | --- |
| 1 | 入力の長さを受け取らず `p[1]`・`p[2]` を無条件に読む | 末尾が `E2` だけ | 境界外読み取り |
| 2 | 先頭バイトが 3バイト形式（1110xxxx）かを確認しない | `41 42 43`、`F0 …` | 任意のバイト列を3バイト文字として誤って復号する |
| 3 | 継続バイトが 10xxxxxx かを確認しない（`& 63` で上位ビットを捨てる） | `E2 41 42` | 不正な列を受理する。文字境界がずれる |
| 4 | 過長符号化を拒否しない | `E0 80 80` → U+0000、`E0 81 AF` → '/' | セキュリティ上の典型的な脆弱性（NUL や '/' の検査のすり抜け、ディレクトリトラバーサル） |
| 5 | サロゲートを拒否しない | `ED A0 80` → U+D800 | Unicode スカラー値でない値を後段へ渡す |
| 6 | ストリーミング、エラー位置、1〜4バイト文字に対応していない | 分割入力 | 実用できない |

**修正方針：** RFC 3629 と Unicode 表 3-7 の「2バイト目の許容範囲」を状態として保持します（E0 は A0..BF、ED は 80..9F、F0 は 90..BF、F4 は 80..8F、それ以外は 80..BF）。不正な列は、その時点で確定できる最初のバイトで即座に拒否します（例：`E0 80` は3バイト目を待たずに拒否）。C0・C1・F5〜FF・単独の継続バイトは、先頭位置で拒否します。保持する状態は、組み立て中のスカラー値・残りバイト数・次のバイトの範囲・オフセットだけで、入力のコピーは持ちません。エラー位置は「問題の列の先頭バイト」の、ストリーム全体での0始まりのオフセットです（finish 時の途中切れも同じ）。オフセットが `SIZE_MAX` に達した時点で `UTF8_ERR_OFFSET_OVERFLOW` とし、桁あふれを検出します。

#### 2. 完全な修正版Cコード

```c
/*
 * Q04: strict streaming UTF-8 decoder (RFC 3629, C17)
 *
 * Accepts exactly the well-formed byte sequences of RFC 3629 / Unicode Table 3-7:
 *   00..7F
 *   C2..DF 80..BF
 *   E0     A0..BF 80..BF        E1..EC 80..BF 80..BF
 *   ED     80..9F 80..BF        EE..EF 80..BF 80..BF
 *   F0     90..BF 80..BF 80..BF F1..F3 80..BF 80..BF 80..BF
 *   F4     80..8F 80..BF 80..BF
 * Everything else (C0, C1, F5..FF, stray continuation bytes, overlongs,
 * surrogates ED A0..BF, > U+10FFFF F4 90..BF, 5/6-byte forms) is rejected.
 * State kept between feeds: only the partially decoded scalar (<= 4 bytes).
 *
 * Original implementation by ★★FREYJA for the C hell-level exam.
 */
#include <stddef.h>
#include <stdint.h>

/* ------------------------------------------------------------------ API */

typedef enum {
    UTF8_OK = 0,
    UTF8_ERR_ARG,              /* invalid argument; state unchanged                 */
    UTF8_ERR_INVALID,          /* ill-formed sequence                               */
    UTF8_ERR_TRUNCATED,        /* utf8_finish() inside a multi-byte sequence        */
    UTF8_ERR_CALLBACK,         /* callback returned non-zero                        */
    UTF8_ERR_OFFSET_OVERFLOW   /* stream longer than SIZE_MAX bytes                 */
} utf8_status;

/* Receives each completed scalar value. Return 0 to continue, non-zero to stop. */
typedef int (*utf8_cb)(void *user, uint32_t scalar);

typedef struct {
    utf8_cb     cb;
    void       *user;
    uint32_t    cp;          /* scalar bits accumulated so far             */
    unsigned    need;        /* continuation bytes still required (0..3)   */
    unsigned char lo, hi;    /* allowed range of the NEXT continuation byte */
    size_t      pos;         /* stream offset of the next byte             */
    size_t      seq_start;   /* stream offset of the current lead byte     */
    int         failed;
    utf8_status err;
    size_t      err_off;     /* offset reported for the sticky error       */
} utf8_decoder;

utf8_status utf8_init(utf8_decoder *d, utf8_cb cb, void *user);
/* On error, *err_off (if non-NULL) receives the 0-based stream offset of the
 * first byte of the offending sequence. On success *err_off is not written. */
utf8_status utf8_feed(utf8_decoder *d, const unsigned char *p, size_t n, size_t *err_off);
utf8_status utf8_finish(utf8_decoder *d, size_t *err_off);
void        utf8_reset(utf8_decoder *d);

#ifdef UTF8_ENABLE_TEST_HOOKS
/* Test-only: pretend `pos` bytes have already been consumed (offset overflow tests). */
void utf8_test_set_position(utf8_decoder *d, size_t pos);
#endif

/* ------------------------------------------------------- implementation */

static void utf8_clear(utf8_decoder *d)
{
    d->cp = 0;
    d->need = 0;
    d->lo = 0x80;
    d->hi = 0xBF;
    d->pos = 0;
    d->seq_start = 0;
    d->failed = 0;
    d->err = UTF8_OK;
    d->err_off = 0;
}

utf8_status utf8_init(utf8_decoder *d, utf8_cb cb, void *user)
{
    if (d == NULL || cb == NULL)
        return UTF8_ERR_ARG;
    d->cb = cb;
    d->user = user;
    utf8_clear(d);
    return UTF8_OK;
}

void utf8_reset(utf8_decoder *d)
{
    if (d != NULL)
        utf8_clear(d);
}

#ifdef UTF8_ENABLE_TEST_HOOKS
void utf8_test_set_position(utf8_decoder *d, size_t pos) { d->pos = pos; d->seq_start = pos; }
#endif

static utf8_status utf8_fail(utf8_decoder *d, utf8_status e, size_t off, size_t *err_off)
{
    d->failed = 1;
    d->err = e;
    d->err_off = off;
    if (err_off != NULL)
        *err_off = off;
    return e;
}

utf8_status utf8_feed(utf8_decoder *d, const unsigned char *p, size_t n, size_t *err_off)
{
    if (d == NULL)
        return UTF8_ERR_ARG;
    if (d->failed) {
        if (err_off != NULL)
            *err_off = d->err_off;
        return d->err;                       /* rejected until utf8_reset() */
    }
    if (n == 0)
        return UTF8_OK;
    if (p == NULL)
        return UTF8_ERR_ARG;

    for (size_t i = 0; i < n; ++i) {
        unsigned char b = p[i];
        size_t off = d->pos;
        if (off == SIZE_MAX)                 /* the next offset would not be representable */
            return utf8_fail(d, UTF8_ERR_OFFSET_OVERFLOW, off, err_off);
        d->pos = off + 1u;

        if (d->need == 0) {
            d->seq_start = off;
            if (b < 0x80u) {
                if (d->cb(d->user, (uint32_t)b) != 0)
                    return utf8_fail(d, UTF8_ERR_CALLBACK, off, err_off);
                continue;
            }
            if (b >= 0xC2u && b <= 0xDFu) {
                d->cp = (uint32_t)(b & 0x1Fu); d->need = 1; d->lo = 0x80; d->hi = 0xBF;
            } else if (b >= 0xE0u && b <= 0xEFu) {
                d->cp = (uint32_t)(b & 0x0Fu); d->need = 2;
                d->lo = (b == 0xE0u) ? 0xA0 : 0x80;      /* E0: no overlong      */
                d->hi = (b == 0xEDu) ? 0x9F : 0xBF;      /* ED: no surrogates    */
            } else if (b >= 0xF0u && b <= 0xF4u) {
                d->cp = (uint32_t)(b & 0x07u); d->need = 3;
                d->lo = (b == 0xF0u) ? 0x90 : 0x80;      /* F0: no overlong      */
                d->hi = (b == 0xF4u) ? 0x8F : 0xBF;      /* F4: <= U+10FFFF      */
            } else {
                /* 80..BF stray continuation, C0/C1 overlong, F5..FF out of range */
                return utf8_fail(d, UTF8_ERR_INVALID, off, err_off);
            }
            continue;
        }

        if (b < d->lo || b > d->hi)          /* also catches a new lead byte too early */
            return utf8_fail(d, UTF8_ERR_INVALID, d->seq_start, err_off);
        d->cp = (d->cp << 6) | (uint32_t)(b & 0x3Fu);   /* cp < 2^21: no overflow */
        d->lo = 0x80;
        d->hi = 0xBF;
        if (--d->need == 0) {
            if (d->cb(d->user, d->cp) != 0)
                return utf8_fail(d, UTF8_ERR_CALLBACK, d->seq_start, err_off);
            d->cp = 0;
        }
    }
    return UTF8_OK;
}

utf8_status utf8_finish(utf8_decoder *d, size_t *err_off)
{
    if (d == NULL)
        return UTF8_ERR_ARG;
    if (d->failed) {
        if (err_off != NULL)
            *err_off = d->err_off;
        return d->err;
    }
    if (d->need != 0)
        return utf8_fail(d, UTF8_ERR_TRUNCATED, d->seq_start, err_off);
    return UTF8_OK;
}
```

#### 3. ビルド方法・使用環境

```sh
# PowerShell 7 で MSVC 開発環境（vcvars64.bat）を読み込んだ後、build.ps1 が次の4構成を順にビルド・実行する
# q04_test.c は #include "q04.c" により実装と同じ翻訳単位でビルドする
W='-Wall -Wextra -Wpedantic -Wshadow -Wconversion -Wsign-conversion -Wcast-qual -Wstrict-prototypes -Wmissing-prototypes -Wvla -Wformat=2'
# (1) Clang -O2
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O2 -fuse-ld=link q04_test.c -o q04_rel.exe
# (2) Clang + AddressSanitizer（MSVC 同梱の ASan ランタイムでリンク）
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O1 -g -fsanitize=address -fno-omit-frame-pointer -fms-runtime-lib=dll -c q04_test.c -o q04_asan.obj
link /nologo /DEBUG /OUT:q04_asan.exe q04_asan.obj clang_rt.asan_dynamic-x86_64.lib clang_rt.asan_dynamic_runtime_thunk-x86_64.lib msvcrt.lib ucrt.lib vcruntime.lib legacy_stdio_definitions.lib
# (3) Clang + UndefinedBehaviorSanitizer（トラップ方式）
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O1 -g -fsanitize=undefined -fsanitize-trap=undefined -fno-sanitize-recover=all -fuse-ld=link q04_test.c -o q04_ubsan.exe
# (4) MSVC（2つ目のコンパイラ）
cl /nologo /std:c17 /W4 /experimental:c11atomics /utf-8 /O2 /D_CRT_SECURE_NO_WARNINGS q04_test.c /Fe:q04_msvc.exe
# 各 .exe を実行し、終了コード 0 と "RESULT: N checks passed, 0 failed" を確認
```

**使用環境（全問共通）**

- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド、x86-64）
- コンパイラ1：**Clang 18.0.3**（Android NDK r27c に同梱。ターゲットは `x86_64-pc-windows-msvc`。ヘッダー・ライブラリは MSVC 14.52.37025 と Windows SDK/UCRT 10.0.26100、リンカーは link.exe）。`-std=c17` を指定。
- コンパイラ2：**MSVC cl 19.52.37029**（Visual Studio 18 Community）、`/std:c17 /W4`。
- GCC：この PC にはなく、使用していません。Linux／WSL の環境もありません（外部パッケージの無断導入禁止のため、導入していません）。
- AddressSanitizer：Clang でインストルメントし、MSVC 同梱の ASan ランタイム（`clang_rt.asan_dynamic-x86_64`）でリンクしました。ヒープ境界外書き込みの陽性対照で、`heap-buffer-overflow` を検出することを確認済みです。
- UndefinedBehaviorSanitizer：`-fsanitize=undefined -fsanitize-trap=undefined`（トラップ方式。ランタイムを使わず、未定義動作で不正命令の例外として異常終了します）。符号付きオーバーフローの陽性対照で、異常終了することを確認済みです。
- LeakSanitizer：Windows 版 ASan にないため未実施です。リークは各問の計数アロケーター（確保と解放の数の差）で検査しました。ThreadSanitizer：Windows では使えないため未実施です。
- ビルド補助 `shim/stdatomic.h`（製品コード外）：NDK の Clang に付属する `stdatomic.h` は Android（bionic）専用で Windows ではコンパイルできないため、MSVC の C11 実装 `vcruntime_c11_stdatomic.h`（Clang 互換の `__c11_atomic` 組み込み関数を使用）を読み込む1ファイルを置きました。Clang 18 が `__typeof_unqual__` に対応していないため、`atomic_is_lock_free` だけは `__c11_atomic_is_lock_free` で定義し直しています。
- `-D_CRT_SECURE_NO_WARNINGS`：MSVC の CRT が出す「`fopen` などの代わりに `_s` 版を使うように」という非推奨の通知だけを止める定義です（C17 標準関数の使用に対する通知であり、コードの欠陥を隠すものではありません）。

- Q04 の結果：4構成とも警告0件・ビルド成功。テストは `UTF8_ENABLE_TEST_HOOKS` を定義し、オフセットを SIZE_MAX 付近に設定するテスト専用の関数を有効にしています（製品ビルドでは無効）。

#### 4. テストコード

```c
/* Q04 tests */
#define UTF8_ENABLE_TEST_HOOKS 1
#include "q04.c"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

static int g_fail = 0, g_pass = 0;
#define CHECK(cond, ...) do { if (cond) { ++g_pass; } else { ++g_fail; \
    printf("  FAIL %s:%d: ", __FILE__, __LINE__); printf(__VA_ARGS__); printf("\n"); } } while (0)
static void report(const char *name, int before_fail)
{
    printf("[%s] %s\n", g_fail == before_fail ? "PASS" : "FAIL", name);
}

/* ---------- collecting callback ---------- */
typedef struct { uint32_t *v; size_t n, cap; size_t stop_at; } sink;
static int sink_cb(void *u, uint32_t cp)
{
    sink *s = (sink *)u;
    if (s->n < s->cap) s->v[s->n] = cp;
    s->n++;
    return (s->stop_at != 0 && s->n == s->stop_at) ? 1 : 0;
}

/* ---------- independent reference (interval feasibility) ---------- */
static size_t ref_encode(uint32_t cp, unsigned char *o)
{
    if (cp < 0x80) { o[0] = (unsigned char)cp; return 1; }
    if (cp < 0x800) { o[0] = (unsigned char)(0xC0 | (cp >> 6)); o[1] = (unsigned char)(0x80 | (cp & 0x3F)); return 2; }
    if (cp < 0x10000) { o[0] = (unsigned char)(0xE0 | (cp >> 12)); o[1] = (unsigned char)(0x80 | ((cp >> 6) & 0x3F)); o[2] = (unsigned char)(0x80 | (cp & 0x3F)); return 3; }
    o[0] = (unsigned char)(0xF0 | (cp >> 18)); o[1] = (unsigned char)(0x80 | ((cp >> 12) & 0x3F));
    o[2] = (unsigned char)(0x80 | ((cp >> 6) & 0x3F)); o[3] = (unsigned char)(0x80 | (cp & 0x3F)); return 4;
}

/* Can a sequence of total length L whose known prefix value is P (with `rem`
 * continuation bytes still missing) still become a valid scalar? */
static int ref_viable(unsigned L, uint32_t P, unsigned rem)
{
    static const uint32_t minv[5] = { 0, 0, 0x80, 0x800, 0x10000 };
    static const uint32_t maxv[5] = { 0, 0x7F, 0x7FF, 0xFFFF, 0x10FFFF };
    uint64_t lo = (uint64_t)P << (6 * rem), hi = lo | ((1ull << (6 * rem)) - 1);
    if (hi < minv[L] || lo > maxv[L]) return 0;
    if (lo < minv[L]) lo = minv[L];
    if (hi > maxv[L]) hi = maxv[L];
    if (lo >= 0xD800 && hi <= 0xDFFF) return 0;          /* only surrogates reachable */
    return 1;
}

/* returns status, fills scalars (up to cap) and count, err offset */
static utf8_status ref_decode(const unsigned char *s, size_t n, uint32_t *out, size_t cap, size_t *cnt, size_t *eoff)
{
    size_t i = 0, k = 0;
    while (i < n) {
        unsigned char b = s[i];
        unsigned L; uint32_t P;
        if ((b & 0x80) == 0) { L = 1; P = b; }
        else if ((b & 0xE0) == 0xC0) { L = 2; P = b & 0x1Fu; }
        else if ((b & 0xF0) == 0xE0) { L = 3; P = b & 0x0Fu; }
        else if ((b & 0xF8) == 0xF0) { L = 4; P = b & 0x07u; }
        else { *cnt = k; *eoff = i; return UTF8_ERR_INVALID; }         /* 80..BF, F8..FF */
        if (!ref_viable(L, P, L - 1)) { *cnt = k; *eoff = i; return UTF8_ERR_INVALID; }
        for (unsigned j = 1; j < L; ++j) {
            if (i + j >= n) { *cnt = k; *eoff = i; return UTF8_ERR_TRUNCATED; }
            unsigned char c = s[i + j];
            if ((c & 0xC0) != 0x80) { *cnt = k; *eoff = i; return UTF8_ERR_INVALID; }
            P = (P << 6) | (c & 0x3Fu);
            if (!ref_viable(L, P, L - 1 - j)) { *cnt = k; *eoff = i; return UTF8_ERR_INVALID; }
        }
        if (k < cap) out[k] = P;
        ++k;
        i += L;
    }
    *cnt = k;
    return UTF8_OK;
}

/* run the decoder over s with the given cut points, then finish */
static utf8_status run(const unsigned char *s, size_t n, const size_t *cuts, size_t nc, sink *sk, size_t *eoff)
{
    utf8_decoder d;
    utf8_init(&d, sink_cb, sk);
    size_t pos = 0;
    for (size_t c = 0; c <= nc; ++c) {
        size_t end = c < nc ? cuts[c] : n;
        utf8_status st = utf8_feed(&d, s + pos, end - pos, eoff);
        if (st != UTF8_OK) return st;
        pos = end;
    }
    return utf8_finish(&d, eoff);
}

/* compare decoder vs reference on s (one chunk). returns 1 when they agree */
static uint32_t bufA[64], bufB[64];
static int agree(const unsigned char *s, size_t n)
{
    sink sk = { bufA, 0, 64, 0 };
    size_t e1 = (size_t)-1, e2 = (size_t)-1, c2 = 0;
    utf8_status a = run(s, n, NULL, 0, &sk, &e1);
    utf8_status b = ref_decode(s, n, bufB, 64, &c2, &e2);
    if (a != b || sk.n != c2) return 0;
    if (a != UTF8_OK && e1 != e2) return 0;
    return memcmp(bufA, bufB, c2 * sizeof(uint32_t)) == 0;
}

static uint64_t rs = 0x853C49E6748FEA9Bu;
static uint64_t rnd(void) { rs ^= rs << 13; rs ^= rs >> 7; rs ^= rs << 17; return rs; }

int main(void)
{
    int f0;
    uint32_t vbuf[1024];

    /* T1: all ASCII */
    f0 = g_fail;
    {
        unsigned char a[128];
        for (int i = 0; i < 128; ++i) {
            a[i] = (unsigned char)i;
            sink sk = { vbuf, 0, 1024, 0 }; size_t e = 777;
            CHECK(run(a + i, 1, NULL, 0, &sk, &e) == UTF8_OK && sk.n == 1 && vbuf[0] == (uint32_t)i && e == 777, "ascii %d", i);
        }
        sink sk = { vbuf, 0, 1024, 0 }; size_t e = 777;
        CHECK(run(a, 128, NULL, 0, &sk, &e) == UTF8_OK && sk.n == 128, "all ascii one chunk");
        for (int i = 0; i < 128; ++i) CHECK(vbuf[i] == (uint32_t)i, "ascii order %d", i);
    }
    report("T1 every ASCII byte 00..7F (NUL accepted)", f0);

    /* T2: required valid vectors */
    f0 = g_fail;
    {
        struct { const char *b; size_t n; uint32_t cp; const char *name; } v[] = {
            { "\xC2\x80", 2, 0x80, "C2 80" }, { "\xDF\xBF", 2, 0x7FF, "DF BF" },
            { "\xE0\xA0\x80", 3, 0x800, "E0 A0 80" }, { "\xED\x9F\xBF", 3, 0xD7FF, "ED 9F BF" },
            { "\xEE\x80\x80", 3, 0xE000, "EE 80 80" }, { "\xEF\xBB\xBF", 3, 0xFEFF, "EF BB BF (U+FEFF valid)" },
            { "\xEF\xBF\xBF", 3, 0xFFFF, "EF BF BF" }, { "\xF0\x90\x80\x80", 4, 0x10000, "F0 90 80 80" },
            { "\xF4\x8F\xBF\xBF", 4, 0x10FFFF, "F4 8F BF BF" }, { "\x00", 1, 0, "00 (NUL valid)" },
        };
        for (size_t i = 0; i < sizeof v / sizeof v[0]; ++i) {
            sink sk = { vbuf, 0, 1024, 0 }; size_t e = 777;
            utf8_status st = run((const unsigned char *)v[i].b, v[i].n, NULL, 0, &sk, &e);
            CHECK(st == UTF8_OK && sk.n == 1 && vbuf[0] == v[i].cp && e == 777, "%s -> U+%04X (st=%d n=%zu got %X)", v[i].name, (unsigned)v[i].cp, (int)st, sk.n, (unsigned)vbuf[0]);
        }
    }
    report("T2 C2 80, E0 A0 80, ED 9F BF, EE 80 80, F4 8F BF BF, U+FEFF, NUL", f0);

    /* T3: required invalid vectors, preceded by "AB" -> error offset 2, A and B already delivered */
    f0 = g_fail;
    {
        struct { const char *b; size_t n; utf8_status st; const char *name; } v[] = {
            { "\xC0\x80", 2, UTF8_ERR_INVALID, "C0 80" }, { "\xC1\xBF", 2, UTF8_ERR_INVALID, "C1 BF" },
            { "\xE0\x80\x80", 3, UTF8_ERR_INVALID, "E0 80 80" }, { "\xE0\x9F\xBF", 3, UTF8_ERR_INVALID, "E0 9F BF" },
            { "\xED\xA0\x80", 3, UTF8_ERR_INVALID, "ED A0 80" }, { "\xED\xBF\xBF", 3, UTF8_ERR_INVALID, "ED BF BF" },
            { "\xF0\x8F\xBF\xBF", 4, UTF8_ERR_INVALID, "F0 8F BF BF" }, { "\xF4\x90\x80\x80", 4, UTF8_ERR_INVALID, "F4 90 80 80" },
            { "\xF5\x80\x80\x80", 4, UTF8_ERR_INVALID, "F5 80 80 80" }, { "\x80", 1, UTF8_ERR_INVALID, "lone 80" },
            { "\xBF", 1, UTF8_ERR_INVALID, "lone BF" }, { "\xF8\x88\x80\x80\x80", 5, UTF8_ERR_INVALID, "5-byte F8" },
            { "\xFC\x84\x80\x80\x80\x80", 6, UTF8_ERR_INVALID, "6-byte FC" }, { "\xFE", 1, UTF8_ERR_INVALID, "FE" },
            { "\xFF", 1, UTF8_ERR_INVALID, "FF" }, { "\xC2\x41", 2, UTF8_ERR_INVALID, "C2 41" },
            { "\xE1\x80\x41", 3, UTF8_ERR_INVALID, "E1 80 41" }, { "\xE1\x80\xC2\x80", 4, UTF8_ERR_INVALID, "lead inside sequence" },
            { "\xC2", 1, UTF8_ERR_TRUNCATED, "C2 then finish" }, { "\xE1\x80", 2, UTF8_ERR_TRUNCATED, "E1 80 then finish" },
            { "\xF1\x80\x80", 3, UTF8_ERR_TRUNCATED, "F1 80 80 then finish" },
        };
        unsigned char buf[16];
        for (size_t i = 0; i < sizeof v / sizeof v[0]; ++i) {
            buf[0] = 'A'; buf[1] = 'B'; memcpy(buf + 2, v[i].b, v[i].n);
            sink sk = { vbuf, 0, 1024, 0 }; size_t e = 777;
            utf8_status st = run(buf, v[i].n + 2, NULL, 0, &sk, &e);
            CHECK(st == v[i].st && e == 2 && sk.n == 2 && vbuf[0] == 'A' && vbuf[1] == 'B', "%s: st=%d off=%zu delivered=%zu", v[i].name, (int)st, e, sk.n);
            CHECK(agree(buf, v[i].n + 2), "%s agrees with reference", v[i].name);
        }
        /* error position deep in the stream, in a later feed */
        utf8_decoder d; sink sk = { vbuf, 0, 1024, 0 }; size_t e = 777;
        utf8_init(&d, sink_cb, &sk);
        CHECK(utf8_feed(&d, (const unsigned char *)"\xE2\x82\xAC\xF0\x9F", 5, &e) == UTF8_OK && e == 777, "first feed ok");
        CHECK(utf8_feed(&d, (const unsigned char *)"\x98\x41", 2, &e) == UTF8_ERR_INVALID && e == 3, "error at lead F0 (offset 3), got %zu", e);
        CHECK(sk.n == 1 && vbuf[0] == 0x20AC, "euro delivered before the error");
        e = 0;
        CHECK(utf8_feed(&d, (const unsigned char *)"A", 1, &e) == UTF8_ERR_INVALID && e == 3 && sk.n == 1, "rejected until reset, same offset");
        CHECK(utf8_finish(&d, &e) == UTF8_ERR_INVALID && e == 3, "finish reports sticky error");
        utf8_reset(&d);
        e = 777;
        CHECK(utf8_feed(&d, (const unsigned char *)"\xC3\xA9", 2, &e) == UTF8_OK && utf8_finish(&d, &e) == UTF8_OK && sk.n == 2 && vbuf[1] == 0xE9 && e == 777, "reset then valid input");
        CHECK(utf8_feed(&d, (const unsigned char *)"\x80", 1, &e) == UTF8_ERR_INVALID && e == 2, "offsets restart from 0 after reset (got %zu)", e);
    }
    report("T3 C0 80, E0 80 80, ED A0 80, F4 90 80 80, F5.., lone 80, 5/6-byte, truncation, offsets, reset", f0);

    /* T4: every scalar value round-trips (individually and as one stream) */
    f0 = g_fail;
    {
        size_t total = 0;
        unsigned char *all = (unsigned char *)malloc(4 * 0x110000);
        uint32_t *exp = (uint32_t *)malloc(sizeof(uint32_t) * 0x110000);
        size_t ne = 0; long bad = 0;
        for (uint32_t cp = 0; cp <= 0x10FFFF; ++cp) {
            if (cp >= 0xD800 && cp <= 0xDFFF) continue;
            unsigned char e[4]; size_t n = ref_encode(cp, e);
            sink sk = { vbuf, 0, 1024, 0 }; size_t off = 777;
            if (!(run(e, n, NULL, 0, &sk, &off) == UTF8_OK && sk.n == 1 && vbuf[0] == cp)) { if (bad++ < 5) CHECK(0, "cp %X", (unsigned)cp); }
            memcpy(all + total, e, n); total += n; exp[ne++] = cp;
        }
        CHECK(bad == 0, "all %zu scalars decode individually", ne);
        uint32_t *got = (uint32_t *)malloc(sizeof(uint32_t) * 0x110000);
        sink sk = { got, 0, 0x110000, 0 }; size_t off = 777;
        CHECK(run(all, total, NULL, 0, &sk, &off) == UTF8_OK && sk.n == ne && memcmp(got, exp, ne * sizeof(uint32_t)) == 0, "one stream of %zu bytes", total);
        /* same stream in random chunks */
        utf8_decoder d; sk.n = 0;
        utf8_init(&d, sink_cb, &sk);
        size_t pos = 0; utf8_status st = UTF8_OK;
        while (pos < total && st == UTF8_OK) { size_t c = (size_t)(rnd() % 9); if (c > total - pos) c = total - pos; st = utf8_feed(&d, all + pos, c, &off); pos += c; }
        CHECK(st == UTF8_OK && utf8_finish(&d, &off) == UTF8_OK && sk.n == ne && memcmp(got, exp, ne * sizeof(uint32_t)) == 0, "random 0..8 byte chunks");
        /* every surrogate encoded naively is rejected at offset 0 */
        long sbad = 0;
        for (uint32_t cp = 0xD800; cp <= 0xDFFF; ++cp) {
            unsigned char e[3] = { (unsigned char)(0xE0 | (cp >> 12)), (unsigned char)(0x80 | ((cp >> 6) & 0x3F)), (unsigned char)(0x80 | (cp & 0x3F)) };
            sink s2 = { vbuf, 0, 1024, 0 }; size_t o2 = 777;
            if (!(run(e, 3, NULL, 0, &s2, &o2) == UTF8_ERR_INVALID && o2 == 0 && s2.n == 0)) ++sbad;
        }
        CHECK(sbad == 0, "all 2048 surrogate encodings rejected");
        free(all); free(exp); free(got);
    }
    report("T4 all 1,112,064 scalar values round-trip; all surrogates rejected", f0);

    /* T5: exhaustive 1/2/3-byte sequences and sampled 4-byte sequences vs reference */
    f0 = g_fail;
    {
        long mism = 0, cnt = 0;
        unsigned char s[4];
        for (unsigned a = 0; a < 256; ++a) { s[0] = (unsigned char)a; if (!agree(s, 1)) ++mism; ++cnt; }
        for (unsigned a = 0; a < 65536; ++a) { s[0] = (unsigned char)(a >> 8); s[1] = (unsigned char)a; if (!agree(s, 2)) ++mism; ++cnt; }
        for (unsigned a = 0; a < (1u << 24); ++a) {
            s[0] = (unsigned char)(a >> 16); s[1] = (unsigned char)(a >> 8); s[2] = (unsigned char)a;
            if (!agree(s, 3)) { if (mism < 5) printf("  mismatch %02X %02X %02X\n", s[0], s[1], s[2]); ++mism; }
            ++cnt;
        }
        static const unsigned char pick[] = { 0x00, 0x41, 0x7F, 0x80, 0x8F, 0x90, 0x9F, 0xA0, 0xBF, 0xC0, 0xC2, 0xF4, 0xFF };
        for (unsigned a = 0xF0; a <= 0xFF; ++a)
            for (unsigned b = 0; b < 256; ++b)
                for (size_t c = 0; c < sizeof pick; ++c)
                    for (size_t e = 0; e < sizeof pick; ++e) {
                        s[0] = (unsigned char)a; s[1] = (unsigned char)b; s[2] = pick[c]; s[3] = pick[e];
                        if (!agree(s, 4)) ++mism;
                        ++cnt;
                    }
        for (int k = 0; k < 2000000; ++k) {
            uint64_t r = rnd();
            s[0] = (unsigned char)(0xF0 | (r & 0x0F)); s[1] = (unsigned char)(r >> 8); s[2] = (unsigned char)(r >> 16); s[3] = (unsigned char)(r >> 24);
            if (!agree(s, 4)) ++mism;
            ++cnt;
        }
        CHECK(mism == 0, "%ld mismatches out of %ld sequences", mism, cnt);
        printf("  sequences compared with reference: %ld\n", cnt);
    }
    report("T5 exhaustive 1/2/3-byte + 4-byte (structured + 2M random) vs independent reference", f0);

    /* T6: every 2-way and 3-way split of a mixed valid string and of an invalid string */
    f0 = g_fail;
    {
        const unsigned char good[] = "a\xC3\xA9\xE2\x82\xAC\xF0\x9F\x98\x80\x00z\xEF\xBB\xBF\xF4\x8F\xBF\xBF\xDF\xBF";
        size_t gn = sizeof good - 1;
        uint32_t expv[16]; size_t ec = 0, eo = 0;
        CHECK(ref_decode(good, gn, expv, 16, &ec, &eo) == UTF8_OK && ec == 9, "reference decodes test string (9 scalars)");
        const unsigned char bad[] = "x\xE2\x82\xAC\xF0\x9F\x98\x80\xED\xA0\x80q";
        size_t bn = sizeof bad - 1;
        long runs = 0;
        for (size_t a = 0; a <= gn; ++a)
            for (size_t b = a; b <= gn; ++b) {
                size_t cuts[2] = { a, b }; sink sk = { vbuf, 0, 1024, 0 }; size_t e = 777;
                utf8_status st = run(good, gn, cuts, 2, &sk, &e);
                if (!(st == UTF8_OK && sk.n == ec && memcmp(vbuf, expv, ec * 4) == 0 && e == 777)) CHECK(0, "good split %zu %zu", a, b);
                ++runs;
            }
        for (size_t a = 0; a <= bn; ++a)
            for (size_t b = a; b <= bn; ++b) {
                size_t cuts[2] = { a, b }; sink sk = { vbuf, 0, 1024, 0 }; size_t e = 777;
                utf8_status st = run(bad, bn, cuts, 2, &sk, &e);
                if (!(st == UTF8_ERR_INVALID && e == 8 && sk.n == 3 && vbuf[0] == 'x' && vbuf[1] == 0x20AC && vbuf[2] == 0x1F600)) CHECK(0, "bad split %zu %zu st=%d e=%zu n=%zu", a, b, (int)st, e, sk.n);
                ++runs;
            }
        /* truncated at every position of the good string: finish reports the lead offset */
        for (size_t cut = 0; cut <= gn; ++cut) {
            sink sk = { vbuf, 0, 1024, 0 }; size_t e = 777, rc = 0, ro = 0;
            utf8_status st = run(good, cut, NULL, 0, &sk, &e);
            utf8_status rst = ref_decode(good, cut, bufB, 64, &rc, &ro);
            if (!(st == rst && sk.n == rc && (st == UTF8_OK || e == ro))) CHECK(0, "truncation at %zu", cut);
            ++runs;
        }
        printf("  split/truncation runs: %ld\n", runs);
    }
    report("T6 all 2-way/3-way split boundaries (valid + invalid), finish at every cut", f0);

    /* T7: callback failure */
    f0 = g_fail;
    {
        const unsigned char s[] = "a\xC3\xA9\xE2\x82\xAC" "b";
        sink sk = { vbuf, 0, 1024, 3 }; size_t e = 777;
        utf8_status st = run(s, sizeof s - 1, NULL, 0, &sk, &e);
        CHECK(st == UTF8_ERR_CALLBACK && e == 3 && sk.n == 3, "stops at 3rd scalar (offset 3), st=%d e=%zu n=%zu", (int)st, e, sk.n);
    }
    report("T7 callback failure stops decoding and reports the scalar's offset", f0);

    /* T8: offset overflow detection (test hook positions the stream near SIZE_MAX) */
    f0 = g_fail;
    {
        utf8_decoder d; sink sk = { vbuf, 0, 1024, 0 }; size_t e = 777;
        utf8_init(&d, sink_cb, &sk);
        utf8_test_set_position(&d, SIZE_MAX - 3);
        CHECK(utf8_feed(&d, (const unsigned char *)"abc", 3, &e) == UTF8_OK && sk.n == 3, "bytes at offsets MAX-3..MAX-1 accepted");
        CHECK(utf8_feed(&d, (const unsigned char *)"d", 1, &e) == UTF8_ERR_OFFSET_OVERFLOW && e == SIZE_MAX && sk.n == 3, "next byte -> OFFSET_OVERFLOW");
        utf8_reset(&d); sk.n = 0;
        utf8_test_set_position(&d, SIZE_MAX - 10);
        CHECK(utf8_feed(&d, (const unsigned char *)"\xE0\x80", 2, &e) == UTF8_ERR_INVALID && e == SIZE_MAX - 10, "large error offset exact");
        utf8_reset(&d);
        utf8_test_set_position(&d, SIZE_MAX - 2);
        CHECK(utf8_feed(&d, (const unsigned char *)"\xE2\x82", 2, &e) == UTF8_OK, "partial near the end");
        CHECK(utf8_finish(&d, &e) == UTF8_ERR_TRUNCATED && e == SIZE_MAX - 2, "truncation offset near SIZE_MAX");
    }
    report("T8 size_t offset overflow detected; large offsets exact", f0);

    /* T9: argument rules */
    f0 = g_fail;
    {
        utf8_decoder d; sink sk = { vbuf, 0, 1024, 0 }; size_t e = 777;
        CHECK(utf8_init(NULL, sink_cb, &sk) == UTF8_ERR_ARG && utf8_init(&d, NULL, &sk) == UTF8_ERR_ARG, "init args");
        utf8_init(&d, sink_cb, &sk);
        CHECK(utf8_feed(&d, (const unsigned char *)"\xE2", 1, &e) == UTF8_OK, "partial");
        CHECK(utf8_feed(&d, NULL, 0, &e) == UTF8_OK, "NULL,0 keeps state");
        CHECK(utf8_feed(&d, NULL, 2, &e) == UTF8_ERR_ARG && e == 777, "NULL,n rejected, offset untouched");
        CHECK(utf8_feed(&d, (const unsigned char *)"\x82\xAC", 2, NULL) == UTF8_OK && sk.n == 1 && vbuf[0] == 0x20AC, "state intact; NULL err_off allowed");
        CHECK(utf8_finish(&d, &e) == UTF8_OK && e == 777, "finish ok, offset untouched");
    }
    report("T9 NULL/zero-length rules, out-parameter untouched on success", f0);

    printf("RESULT: %d checks passed, %d failed\n", g_pass, g_fail);
    return g_fail ? 1 : 0;
}
```

#### 5. 検証結果（期待値と実測値を区別）

4構成とも `RESULT: 334 checks passed, 0 failed`、終了コード 0 でした。参照実装（`ref_decode`）は、表を使わない独立した方式です。「残りのバイトで取り得る値の区間が、有効なスカラー値の範囲と交わるか」で、不正な列を最も早い時点で判定します。

| テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 全ASCII境界（T1） | 00〜7F を1バイトずつ、および128バイト一括で、各値そのもの（NUL も有効） | 一致 | 実施（4構成） |
| C2 80 / E0 A0 80 / ED 9F BF / EE 80 80 / F4 8F BF BF（T2） | U+0080 / U+0800 / U+D7FF / U+E000 / U+10FFFF | 一致 | 実施 |
| U+FEFF（EF BB BF）、DF BF、EF BF BF、F0 90 80 80（T2） | 有効 | 一致 | 実施 |
| C0 80、C1 BF（T3） | INVALID、位置 = 先頭バイト | 一致（前置 "AB" の後で offset 2） | 実施 |
| E0 80 80、E0 9F BF、F0 8F BF BF（過長）（T3） | INVALID、offset 2 | 一致 | 実施 |
| ED A0 80、ED BF BF（サロゲート）（T3） | INVALID、offset 2 | 一致 | 実施 |
| F4 90 80 80（範囲外）、F5 80 80 80、FE、FF（T3） | INVALID、offset 2 | 一致 | 実施 |
| 孤立した 80、BF（T3） | INVALID、offset 2 | 一致 | 実施 |
| 5/6バイト形（F8 88 80 80 80、FC 84 80 80 80 80）（T3） | INVALID、offset 2 | 一致 | 実施 |
| 不正な継続バイト、列の途中の先頭バイト（T3） | INVALID、位置 = 列の先頭 | 一致 | 実施 |
| エラー前に完成したスカラー（T3） | "AB"、€ などはエラー前に通知され、取り消されない | 一致 | 実施 |
| feed をまたぐエラー位置（T3） | 2回目の feed での失敗でも、列の先頭のストリーム上の位置（3）を返す | 一致 | 実施 |
| reset 後の正常入力（T3） | 拒否状態が解除され、オフセットは0から数え直す | 一致 | 実施 |
| 全スカラー値の往復（T4） | U+0000〜U+10FFFF（サロゲートを除く1,112,064個）を個別に、一括（4,382,592バイト）で、0〜8バイトの乱数分割で、すべて正しく復号 | 一致 | 実施 |
| 全サロゲート（T4） | 2048個すべて INVALID・offset 0 | 一致 | 実施 |
| 参照実装との総当たり比較（T5） | 1バイト全256通り・2バイト全65,536通り・3バイト全16,777,216通り、4バイトは先頭 F0〜FF の構造化パターン 692,224通りと乱数200万通り。状態・エラー位置・スカラー列が一致 | 19,535,232 列で不一致0 | 実施 |
| すべての分割境界（T6） | 有効な混在文字列（21バイト・9スカラー）と不正な文字列で、全2分割・全3分割の結果が同一 | 一致 | 実施 |
| finish で途中切れ（T6） | 全切断位置で、参照と同じ状態・同じ先頭位置 | 一致 | 実施 |
| コールバック失敗（T7） | 3番目のスカラーで停止、CALLBACK、位置3 | 一致 | 実施 |
| オフセットの桁あふれ（T8） | SIZE_MAX−3 から3バイトは受理、次のバイトで OFFSET_OVERFLOW。SIZE_MAX 付近のエラー位置も正確 | 一致 | 実施 |
| NULL・長さ0、成功時に出力引数へ書かない（T9） | (NULL,0) は OK で状態維持、(NULL,n) は ARG。成功時の err_off は不変 | 一致 | 実施 |
| ASan / UBSan | 報告なし | 報告なし | 実施 |
| GCC・Linux | — | 未実施 | 未実施 |

（テストの初回実行では T6 の前提チェックが1件失敗しました。原因はテスト側で、21バイトのテスト文字列に含まれるスカラー数の期待値を 10 と数え間違えていました（正しくは 9）。修正後に全件合格しました。製品コードは変更していません。）

#### 6. 未実施項目・制約・残課題

- GCC・Linux での実行は未実施です。4バイト列（約43億通り）の完全な総当たりはしておらず、構造化パターンと乱数200万通りによる比較です（2・3バイトは完全な総当たり）。
- 仕様上の制約：処理できる長さは最大 SIZE_MAX バイトです（オフセット SIZE_MAX のバイトは、その次の位置を表せないため OFFSET_OVERFLOW になります）。エラー時の `err_off` には位置を書き込み、成功時には書き込みません。`err_off` に NULL を渡すこともできます。
- UTF-16 変換・正規化・表示幅の処理は、要求どおり行いません。

---

## Q05【地獄級】128ビット型禁止の64ビット精密乗算・銀行丸め

**狙い：** INT64_MIN、オーバーフロー、手作業の多倍長算術、符号処理、ties-to-even。

### 元コード（危険）

~~~c
#include <stdint.h>
#include <stdbool.h>
bool mul_div_bad(int64_t a, int64_t b, uint64_t d, int64_t *out) {
    if (!d || !out) return false;
    *out = (a * b + (int64_t)(d / 2)) / (int64_t)d;
    return true;
}
~~~

### 要求仕様

- 次のAPIを実装する：**bool mul_div_round_even(int64_t a, int64_t b, uint64_t d, int64_t *out)**。
- 数学上の厳密値 **a×b÷d** を最も近い整数に丸め、ちょうど中間の場合は**結果の偶数側**へ丸める。負数は絶対値の丸め後、適切に符号を戻す。
- dは **1〜1000000000** のみ許可。d=0、範囲外、out=NULL、丸め後の結果がint64_t範囲外ならfalseを返し、非NULLの*outを変更しない。
- ISO C17のみ。**__int128、long double、浮動小数点、外部多倍長ライブラリは禁止**。64ビット符号なし演算、32ビット分割、筆算などで正確な中間値を処理する。
- 処理中に符号付きオーバーフロー、未定義シフト、負数の絶対値のオーバーフローを発生させない。INT64_MINの扱いを説明する。

### 必須検証

(1,5,2)→2、(3,5,2)→8、(-3,5,2)→-8、(1,3,2)→2、(1,1,2)→0、(INT64_MIN,1,1)→INT64_MIN、(INT64_MAX,2,1)→失敗、d=0→失敗、d=1000000000の大数、符号の4組合せ、境界周辺の多数の入力。テスト用参照計算では外部スクリプト等による高精度算術を利用してよい（製品実装は禁止）。

### ★★FREYJAの回答【Q05・記入欄】

> **ここから下が回答用スペースです。** GitHubの鉛筆アイコンからファイルを編集し、各「（ここに記入）」やコード枠内の例示文を自分の回答に置き換えてください。問題文・要求仕様・元コードは変更しないこと。

#### 1. 根本原因・危険性・影響

**元コード `mul_div_bad` の問題点（静的解析）**

| # | 箇所 | 根本原因 | 発生例 | 影響 |
| --- | --- | --- | --- | --- |
| 1 | `a * b` | 64ビットの符号付き乗算が桁あふれする | (INT64_MAX, 2, 1)、(INT64_MIN, −1, 1) | 符号付き整数の桁あふれは未定義動作。最適化で予測不能な結果になる |
| 2 | `+ (int64_t)(d / 2)` | 加算でも桁あふれし得る | a×b が INT64_MAX 付近 | 同上 |
| 3 | 丸め方式 | 「d/2 を足して切り捨て」は正の数の四捨五入にすぎない | (1,5,2) は 2.5 → 3 になる（正解は偶数側の 2） | 中間値で偶数側へ丸めない |
| 4 | 負数の扱い | C の除算は0方向へ切り捨てるため、負数に d/2 を足すと誤る | (−3,5,2)：(−15+1)/2 = −7（正解は −8） | 負数で誤った結果 |
| 5 | d の範囲 | 1〜1e9 の検査がない。`(int64_t)d` は d > INT64_MAX で処理系定義の変換 | d = 2^63 | 負の除数として扱われる |
| 6 | 失敗の判定 | 結果が int64 に収まるかを判定する手段がない | 結果が範囲外 | 誤った値を成功として返す |

**INT64_MIN の扱い：** |INT64_MIN| = 2^63 は int64_t では表せません。そこで `(uint64_t)0 − (uint64_t)a` で絶対値を求めます。負の値から符号なし型への変換と、符号なしの減算は、どちらも 2^64 を法として定義された動作です。符号付きの単項マイナスは使いません。最後に符号を戻すときも、大きさがちょうど 2^63 で結果が負なら `INT64_MIN` を直接代入します（`-(int64_t)2^63` は桁あふれするため使いません）。結果が正で大きさが 2^63 以上なら失敗です。

**修正方針：** |a|・|b| を32ビットに分割し、筆算で128ビットの積を4つの32ビットの桁に組み立てます（各部分和は ≤ 2^64−1 で桁あふれしません）。上位の桁から順に d で割ります。余り r < d ≤ 1e9 < 2^30 なので、`(r << 32) | 桁` < 2^62 となり、uint64_t に収まります。丸めは余りで判定します。2r > d なら切り上げ、2r = d なら商が奇数のときだけ切り上げ（2r < 2^31）。大きさの段階で偶数丸めをしても、符号を戻した後の偶数性は変わりません。__int128・long double・浮動小数点・外部ライブラリは使っていません。失敗時（d が範囲外、out が NULL、範囲外の結果）は false を返し、`*out` には書き込みません。

#### 2. 完全な修正版Cコード

```c
/*
 * Q05: exact round-half-to-even of a*b/d without 128-bit types (C17)
 *
 * |a|*|b| (< 2^126) is formed as four 32-bit limbs by schoolbook
 * multiplication, divided by d (1..1e9 < 2^30) limb by limb, and rounded with
 * the remainder: 2*rem > d -> up, 2*rem == d -> to the even quotient.
 * Only uint32_t/uint64_t arithmetic is used (no __int128, long double, float).
 *
 * Original implementation by ★★FREYJA for the C hell-level exam.
 */
#include <stdbool.h>
#include <stdint.h>

/* ------------------------------------------------------------------ API */

#define MULDIV_D_MAX 1000000000u

bool mul_div_round_even(int64_t a, int64_t b, uint64_t d, int64_t *out);

/* ------------------------------------------------------- implementation */

/* |x| as uint64_t. For INT64_MIN the unsigned negation yields 2^63 exactly;
 * no signed negation happens, so there is no overflow. */
static uint64_t abs_u64(int64_t x)
{
    return (x < 0) ? (uint64_t)0 - (uint64_t)x : (uint64_t)x;
}

/* 64 x 64 -> 128 bit product as limbs p[0] (least significant) .. p[3]. */
static void mul_64x64(uint64_t x, uint64_t y, uint32_t p[4])
{
    uint64_t x0 = x & 0xFFFFFFFFu, x1 = x >> 32;
    uint64_t y0 = y & 0xFFFFFFFFu, y1 = y >> 32;
    uint64_t t;
    /* every partial product is <= (2^32-1)^2 and every sum below is
     * <= (2^32-1)^2 + 2*(2^32-1) = 2^64 - 1, so nothing overflows. */
    t = x0 * y0;
    p[0] = (uint32_t)t;
    t = x1 * y0 + (t >> 32);
    uint64_t mid_lo = t & 0xFFFFFFFFu, mid_hi = t >> 32;
    t = x0 * y1 + mid_lo;
    p[1] = (uint32_t)t;
    t = x1 * y1 + mid_hi + (t >> 32);
    p[2] = (uint32_t)t;
    p[3] = (uint32_t)(t >> 32);
}

bool mul_div_round_even(int64_t a, int64_t b, uint64_t d, int64_t *out)
{
    if (out == NULL || d == 0 || d > MULDIV_D_MAX)
        return false;

    bool negative = (a < 0) != (b < 0);
    uint32_t n[4];
    mul_64x64(abs_u64(a), abs_u64(b), n);

    /* long division by d: remainder r < d < 2^30, so (r << 32) | limb < 2^62. */
    uint32_t q[4];
    uint64_t r = 0;
    for (int i = 3; i >= 0; --i) {
        uint64_t cur = (r << 32) | n[i];
        q[i] = (uint32_t)(cur / d);
        r = cur % d;
    }

    /* round half to even on the magnitude (parity is sign independent) */
    uint64_t twice = r * 2u;                  /* < 2^31 */
    bool round_up = (twice > d) || (twice == d && (q[0] & 1u) != 0);
    if (round_up) {
        for (int i = 0; i < 4; ++i) {         /* 128-bit increment with carry */
            q[i] = (uint32_t)(q[i] + 1u);
            if (q[i] != 0)
                break;
        }
    }

    if (q[2] != 0 || q[3] != 0)
        return false;                         /* magnitude >= 2^64 */
    uint64_t mag = ((uint64_t)q[1] << 32) | q[0];
    const uint64_t lim_pos = (uint64_t)INT64_MAX;            /* 2^63 - 1 */
    const uint64_t lim_neg = (uint64_t)INT64_MAX + 1u;       /* 2^63     */
    if (negative) {
        if (mag > lim_neg)
            return false;
        /* mag == 2^63 maps to INT64_MIN; otherwise negate a representable value */
        *out = (mag == lim_neg) ? INT64_MIN : -(int64_t)mag;
    } else {
        if (mag > lim_pos)
            return false;
        *out = (int64_t)mag;
    }
    return true;
}
```

#### 3. ビルド方法・使用環境

```sh
# PowerShell 7 で MSVC 開発環境（vcvars64.bat）を読み込んだ後、build.ps1 が次の4構成を順にビルド・実行する
# q05_test.c は #include "q05.c" により実装と同じ翻訳単位でビルドする
W='-Wall -Wextra -Wpedantic -Wshadow -Wconversion -Wsign-conversion -Wcast-qual -Wstrict-prototypes -Wmissing-prototypes -Wvla -Wformat=2'
# (1) Clang -O2
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O2 -fuse-ld=link q05_test.c -o q05_rel.exe
# (2) Clang + AddressSanitizer（MSVC 同梱の ASan ランタイムでリンク）
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O1 -g -fsanitize=address -fno-omit-frame-pointer -fms-runtime-lib=dll -c q05_test.c -o q05_asan.obj
link /nologo /DEBUG /OUT:q05_asan.exe q05_asan.obj clang_rt.asan_dynamic-x86_64.lib clang_rt.asan_dynamic_runtime_thunk-x86_64.lib msvcrt.lib ucrt.lib vcruntime.lib legacy_stdio_definitions.lib
# (3) Clang + UndefinedBehaviorSanitizer（トラップ方式）
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O1 -g -fsanitize=undefined -fsanitize-trap=undefined -fno-sanitize-recover=all -fuse-ld=link q05_test.c -o q05_ubsan.exe
# (4) MSVC（2つ目のコンパイラ）
cl /nologo /std:c17 /W4 /experimental:c11atomics /utf-8 /O2 /D_CRT_SECURE_NO_WARNINGS q05_test.c /Fe:q05_msvc.exe
# 各 .exe を実行し、終了コード 0 と "RESULT: N checks passed, 0 failed" を確認
# 参照ベクトルの生成（テスト専用、問題文で許可された外部スクリプト）
pwsh -File gen_vectors.ps1      # PowerShell 7 の System.Numerics.BigInteger（任意精度整数）で vectors.txt を生成
```

**使用環境（全問共通）**

- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド、x86-64）
- コンパイラ1：**Clang 18.0.3**（Android NDK r27c に同梱。ターゲットは `x86_64-pc-windows-msvc`。ヘッダー・ライブラリは MSVC 14.52.37025 と Windows SDK/UCRT 10.0.26100、リンカーは link.exe）。`-std=c17` を指定。
- コンパイラ2：**MSVC cl 19.52.37029**（Visual Studio 18 Community）、`/std:c17 /W4`。
- GCC：この PC にはなく、使用していません。Linux／WSL の環境もありません（外部パッケージの無断導入禁止のため、導入していません）。
- AddressSanitizer：Clang でインストルメントし、MSVC 同梱の ASan ランタイム（`clang_rt.asan_dynamic-x86_64`）でリンクしました。ヒープ境界外書き込みの陽性対照で、`heap-buffer-overflow` を検出することを確認済みです。
- UndefinedBehaviorSanitizer：`-fsanitize=undefined -fsanitize-trap=undefined`（トラップ方式。ランタイムを使わず、未定義動作で不正命令の例外として異常終了します）。符号付きオーバーフローの陽性対照で、異常終了することを確認済みです。
- LeakSanitizer：Windows 版 ASan にないため未実施です。リークは各問の計数アロケーター（確保と解放の数の差）で検査しました。ThreadSanitizer：Windows では使えないため未実施です。
- ビルド補助 `shim/stdatomic.h`（製品コード外）：NDK の Clang に付属する `stdatomic.h` は Android（bionic）専用で Windows ではコンパイルできないため、MSVC の C11 実装 `vcruntime_c11_stdatomic.h`（Clang 互換の `__c11_atomic` 組み込み関数を使用）を読み込む1ファイルを置きました。Clang 18 が `__typeof_unqual__` に対応していないため、`atomic_is_lock_free` だけは `__c11_atomic_is_lock_free` で定義し直しています。
- `-D_CRT_SECURE_NO_WARNINGS`：MSVC の CRT が出す「`fopen` などの代わりに `_s` 版を使うように」という非推奨の通知だけを止める定義です（C17 標準関数の使用に対する通知であり、コードの欠陥を隠すものではありません）。

- Q05 の結果：4構成とも警告0件・ビルド成功。参照値の生成には PowerShell 7（.NET の `System.Numerics.BigInteger`）を使いました。これはテストだけに使い、製品コードは ISO C17 の uint32_t/uint64_t 演算だけです。

#### 4. テストコード

```c
/* Q05 tests: explicit cases, BigInteger reference vectors (vectors.txt), and an
 * independent bit-serial reference over millions of random inputs. */
#include "q05.c"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

static int g_fail = 0, g_pass = 0;
#define CHECK(cond, ...) do { if (cond) { ++g_pass; } else { ++g_fail; \
    printf("  FAIL %s:%d: ", __FILE__, __LINE__); printf(__VA_ARGS__); printf("\n"); } } while (0)
static void report(const char *name, int before_fail)
{
    printf("[%s] %s\n", g_fail == before_fail ? "PASS" : "FAIL", name);
}

#define SENT ((int64_t)0x5A5A5A5A5A5A5A5A)

/* expect_ok=0 -> must fail and leave *out untouched */
static int expect(int64_t a, int64_t b, uint64_t d, int expect_ok, int64_t want)
{
    int64_t o = SENT;
    bool r = mul_div_round_even(a, b, d, &o);
    if (expect_ok) return r && o == want;
    return !r && o == SENT;
}

/* ---------- independent bit-serial reference (two uint64 words) ---------- */
typedef struct { uint64_t hi, lo; } u128;
static u128 ref_mul(uint64_t x, uint64_t y)
{
    u128 acc = { 0, 0 };
    for (int i = 63; i >= 0; --i) {
        acc.hi = (acc.hi << 1) | (acc.lo >> 63);   /* acc <<= 1 */
        acc.lo <<= 1;
        if ((y >> i) & 1u) {                       /* acc += x  */
            uint64_t lo = acc.lo + x;
            acc.hi += (lo < acc.lo);
            acc.lo = lo;
        }
    }
    return acc;
}
static bool ref_muldiv(int64_t a, int64_t b, uint64_t d, int64_t *out)
{
    if (!out || d == 0 || d > 1000000000u) return false;
    uint64_t ua = a < 0 ? ~(uint64_t)a + 1u : (uint64_t)a;
    uint64_t ub = b < 0 ? ~(uint64_t)b + 1u : (uint64_t)b;
    u128 p = ref_mul(ua, ub), q = { 0, 0 };
    uint64_t r = 0;
    for (int i = 127; i >= 0; --i) {
        uint64_t bit = i >= 64 ? (p.hi >> (i - 64)) & 1u : (p.lo >> i) & 1u;
        r = (r << 1) | bit;
        q.hi = (q.hi << 1) | (q.lo >> 63);
        q.lo <<= 1;
        if (r >= d) { r -= d; q.lo |= 1u; }
    }
    /* half-even */
    if (2 * r > d || (2 * r == d && (q.lo & 1u))) { q.lo++; if (q.lo == 0) q.hi++; }
    bool neg = (a < 0) ^ (b < 0);
    if (q.hi != 0) return false;
    if (!neg && q.lo > 9223372036854775807u) return false;
    if (neg && q.lo > 9223372036854775808u) return false;
    if (neg) *out = (q.lo == 9223372036854775808u) ? INT64_MIN : -(int64_t)q.lo;
    else *out = (int64_t)q.lo;
    return true;
}

static uint64_t rs = 0xD1B54A32D192ED03u;
static uint64_t rnd(void) { rs ^= rs << 13; rs ^= rs >> 7; rs ^= rs << 17; return rs; }
static int64_t rnd_i64(void)
{
    uint64_t u = rnd();
    int bits = (int)(rnd() % 64) + 1;
    u >>= (64 - bits);
    int64_t v;
    memcpy(&v, &u, sizeof v);            /* two's complement reinterpretation, test only */
    return (rnd() & 1) ? (v == INT64_MIN ? v : -v) : v;
}
static uint64_t rnd_d(void)
{
    switch (rnd() % 4) {
    case 0: return 1 + rnd() % 1000000000u;
    case 1: return 1 + rnd() % 16u;
    case 2: return 1000000000u - rnd() % 64u;
    default: return 2 * (1 + rnd() % 500000000u);
    }
}

int main(int argc, char **argv)
{
    int f0;
    const char *vec = argc > 1 ? argv[1] : "vectors.txt";

    /* T1: required examples */
    f0 = g_fail;
    CHECK(expect(1, 5, 2, 1, 2), "(1,5,2)->2 (2.5 -> even 2)");
    CHECK(expect(3, 5, 2, 1, 8), "(3,5,2)->8 (7.5 -> even 8)");
    CHECK(expect(-3, 5, 2, 1, -8), "(-3,5,2)->-8");
    CHECK(expect(1, 3, 2, 1, 2), "(1,3,2)->2 (1.5 -> even 2)");
    CHECK(expect(1, 1, 2, 1, 0), "(1,1,2)->0 (0.5 -> even 0)");
    CHECK(expect(INT64_MIN, 1, 1, 1, INT64_MIN), "(INT64_MIN,1,1)->INT64_MIN");
    CHECK(expect(INT64_MAX, 2, 1, 0, 0), "(INT64_MAX,2,1) fails, out untouched");
    CHECK(expect(5, 5, 0, 0, 0), "d=0 fails, out untouched");
    CHECK(!mul_div_round_even(1, 1, 1, NULL), "out=NULL fails");
    report("T1 required examples", f0);

    /* T2: divisor range and INT64_MIN handling */
    f0 = g_fail;
    CHECK(expect(7, 7, 1000000001u, 0, 0), "d=1e9+1 rejected");
    CHECK(expect(7, 7, UINT64_MAX, 0, 0), "d=UINT64_MAX rejected");
    CHECK(expect(INT64_MAX, INT64_MAX, 1000000000u, 0, 0), "huge positive overflow");
    CHECK(expect(INT64_MAX, 1, 1000000000u, 1, 9223372037), "INT64_MAX/1e9 = 9223372036.854... -> 9223372037");
    CHECK(expect(INT64_MIN, 1, 1000000000u, 1, -9223372037), "INT64_MIN/1e9 -> -9223372037");
    CHECK(expect(INT64_MIN, INT64_MIN, 1000000000u, 0, 0), "INT64_MIN^2/1e9 overflows");
    CHECK(expect(INT64_MIN, -1, 1, 0, 0), "INT64_MIN*-1 = 2^63 not representable");
    CHECK(expect(INT64_MIN, 1, 2, 1, INT64_MIN / 2), "INT64_MIN/2");
    CHECK(expect(INT64_MIN, -1, 2, 1, (int64_t)1 << 62), "-INT64_MIN/2 = 2^62");
    CHECK(expect(INT64_MIN, 2, 2, 1, INT64_MIN), "INT64_MIN*2/2 = INT64_MIN (intermediate 2^64)");
    CHECK(expect(INT64_MIN, -2, 2, 0, 0), "INT64_MIN*-2/2 = 2^63 fails");
    CHECK(expect(INT64_MAX, INT64_MAX, 1, 0, 0), "(2^63-1)^2 fails");
    CHECK(expect(INT64_MAX, 3, 3, 1, INT64_MAX), "INT64_MAX*3/3 exact");
    CHECK(expect(INT64_MIN, 3, 3, 1, INT64_MIN), "INT64_MIN*3/3 exact");
    CHECK(expect(INT64_MIN, 999999999, 1000000000u, 1, -9223372027631403771), "INT64_MIN*0.999999999");
    CHECK(expect(0, INT64_MIN, 7, 1, 0) && expect(INT64_MIN, 0, 1000000000u, 1, 0), "zero products");
    report("T2 divisor range / INT64_MIN / overflow boundaries", f0);

    /* T3: four sign combinations, ties to even both directions */
    f0 = g_fail;
    {
        struct { int64_t a, b; int64_t want; } t[] = {
            { 5, 1, 2 }, { -5, 1, -2 }, { 5, -1, -2 }, { -5, -1, 2 },     /* 2.5  -> 2  */
            { 7, 1, 4 }, { -7, 1, -4 }, { 7, -1, -4 }, { -7, -1, 4 },     /* 3.5  -> 4  */
            { 11, 1, 6 }, { -11, 1, -6 }, { 11, -1, -6 }, { -11, -1, 6 }, /* 5.5  -> 6  */
            { 9, 1, 4 }, { -9, 1, -4 }, { 9, -1, -4 }, { -9, -1, 4 },     /* 4.5  -> 4  */
        };
        for (size_t i = 0; i < sizeof t / sizeof t[0]; ++i)
            CHECK(expect(t[i].a, t[i].b, 2, 1, t[i].want), "(%lld,%lld,2)", (long long)t[i].a, (long long)t[i].b);
        CHECK(expect(10, 1, 4, 1, 2) && expect(-10, 1, 4, 1, -2) && expect(14, 1, 4, 1, 4) && expect(-14, -1, 4, 1, 4), "x/4 ties");
        CHECK(expect(1, 1, 3, 1, 0) && expect(2, 1, 3, 1, 1) && expect(-2, 1, 3, 1, -1) && expect(-1, 1, 3, 1, 0), "thirds (no tie)");
    }
    report("T3 sign combinations and ties-to-even", f0);

    /* T4: BigInteger vectors */
    f0 = g_fail;
    {
        FILE *fp = fopen(vec, "r");
        CHECK(fp != NULL, "open %s", vec);
        long n = 0, okc = 0, failc = 0, bad = 0;
        if (fp) {
            char line[256];
            while (fgets(line, sizeof line, fp)) {
                char ea[64], eb[64], ed[64], ex[64];
                if (sscanf(line, "%63s %63s %63s %63s", ea, eb, ed, ex) != 4) continue;
                int64_t a = (int64_t)strtoll(ea, NULL, 10), b = (int64_t)strtoll(eb, NULL, 10);
                uint64_t d = (uint64_t)strtoull(ed, NULL, 10);
                int ok = strcmp(ex, "F") != 0;
                int64_t want = ok ? (int64_t)strtoll(ex, NULL, 10) : 0;
                if (!expect(a, b, d, ok, want)) { if (bad < 10) printf("  mismatch: %s", line); ++bad; }
                ++n; ok ? ++okc : ++failc;
            }
            fclose(fp);
        }
        CHECK(n > 40000 && bad == 0, "%ld vectors, %ld mismatches", n, bad);
        printf("  BigInteger vectors: %ld (expected success %ld, expected failure %ld)\n", n, okc, failc);
    }
    report("T4 reference vectors from System.Numerics.BigInteger", f0);

    /* T5: 5,000,000 random inputs vs independent bit-serial reference */
    f0 = g_fail;
    {
        long n = 0, bad = 0, ties = 0, fails = 0;
        for (long k = 0; k < 5000000; ++k) {
            int64_t a = rnd_i64(), b = rnd_i64();
            uint64_t d = rnd_d();
            if (k % 5 == 0) {                /* force an exact tie: a = m*d + d/2 (d even), b odd */
                d = 2 * (1 + rnd() % 500000000u);
                uint64_t m = rnd() >> (1 + rnd() % 63);
                if (m > (uint64_t)(INT64_MAX - (int64_t)d) / d) m %= 1000000u;
                a = (int64_t)(m * d + d / 2);
                b = (int64_t)(2 * (rnd() % 100000u) + 1) * ((rnd() & 1) ? -1 : 1);
                if (rnd() & 1) a = -a;
                ++ties;
            }
            int64_t o1 = SENT, o2 = SENT;
            bool r1 = mul_div_round_even(a, b, d, &o1);
            bool r2 = ref_muldiv(a, b, d, &o2);
            if (r1 != r2 || o1 != o2) { if (bad < 5) printf("  mismatch a=%lld b=%lld d=%llu: %d/%lld vs %d/%lld\n", (long long)a, (long long)b, (unsigned long long)d, r1, (long long)o1, r2, (long long)o2); ++bad; }
            if (!r1) ++fails;
            ++n;
        }
        CHECK(bad == 0, "%ld random cases, %ld mismatches", n, bad);
        printf("  random cases: %ld (forced ties %ld, expected failures %ld)\n", n, ties, fails);
    }
    report("T5 5M random inputs (20%% exact ties) vs independent bit-serial reference", f0);

    printf("RESULT: %d checks passed, %d failed\n", g_pass, g_fail);
    return g_fail ? 1 : 0;
}
```

参照ベクトルの生成に使用したスクリプト（テスト専用）：

```powershell
# Q05 reference vectors with arbitrary-precision System.Numerics.BigInteger (test only).
# Output lines: "<a> <b> <d> <expected>" where expected is an integer or F (= must return false).
param([string]$Out = (Join-Path $PSScriptRoot 'vectors.txt'))
$ErrorActionPreference = 'Stop'
[System.Numerics.BigInteger]$I64MIN = [System.Numerics.BigInteger]::Parse('-9223372036854775808')
[System.Numerics.BigInteger]$I64MAX = [System.Numerics.BigInteger]::Parse('9223372036854775807')
[System.Numerics.BigInteger]$DMAX = [System.Numerics.BigInteger]1000000000

function Ref([System.Numerics.BigInteger]$a, [System.Numerics.BigInteger]$b, [System.Numerics.BigInteger]$d) {
    if ($d -le 0 -or $d -gt $DMAX) { return 'F' }
    $p = $a * $b
    $neg = $p.Sign -lt 0
    $m = [System.Numerics.BigInteger]::Abs($p)
    $r = [System.Numerics.BigInteger]::Zero
    $q = [System.Numerics.BigInteger]::DivRem($m, $d, [ref]$r)
    $c = [System.Numerics.BigInteger]::Compare($r * 2, $d)
    if ($c -gt 0 -or ($c -eq 0 -and -not $q.IsEven)) { $q = $q + 1 }
    if ($neg) { $q = -$q }
    if ($q -lt $I64MIN -or $q -gt $I64MAX) { return 'F' }
    return $q.ToString()
}

$sb = [System.Text.StringBuilder]::new()
function Add([System.Numerics.BigInteger]$a, [System.Numerics.BigInteger]$b, [System.Numerics.BigInteger]$d) {
    [void]$sb.AppendLine(('{0} {1} {2} {3}' -f $a, $b, $d, (Ref $a $b $d)))
}

# 1) required examples
Add 1 5 2; Add 3 5 2; Add -3 5 2; Add 1 3 2; Add 1 1 2
Add $I64MIN 1 1; Add $I64MAX 2 1; Add 5 5 0

# 2) boundary grid
$vals = @('0','1','-1','2','-2','3','-3','7','-7','4294967295','-4294967295','4294967296','-4294967296','4294967297',
          '2147483647','-2147483648','1000000000','-1000000000','999999999','3037000499','-3037000499','3037000500',
          '4611686018427387904','-4611686018427387904','9223372036854775807','-9223372036854775807','-9223372036854775808',
          '9223372036854775806','500000000000000000','-500000000000000001','1844674407370955161','6148914691236517205')
$ds = @('1','2','3','7','10','64','1024','65536','536870912','536870913','999999999','1000000000','999999998','500000000')
foreach ($x in $vals) { foreach ($y in $vals) { foreach ($z in $ds) {
    Add ([System.Numerics.BigInteger]::Parse($x)) ([System.Numerics.BigInteger]::Parse($y)) ([System.Numerics.BigInteger]::Parse($z))
} } }
# invalid divisors (out written as F)
foreach ($x in @('1','-1','9223372036854775807')) { foreach ($z in @('0','1000000001','18446744073709551615','4294967296')) {
    Add ([System.Numerics.BigInteger]::Parse($x)) 3 ([System.Numerics.BigInteger]::Parse($z))
} }

# 3) deterministic pseudo-random cases (xorshift64), with many exact ties
# deterministic generator: System.Random with a fixed seed, values as BigInteger in [0, 2^64)
$script:rng = [System.Random]::new(20261117)
$script:TWO64 = [System.Numerics.BigInteger]::Pow(2, 64)
function NextU64 {
    $buf = [byte[]]::new(9)
    $script:rng.NextBytes($buf)
    $buf[8] = 0
    return [System.Numerics.BigInteger]::new($buf)
}
function RandI64 {
    $bits = [int]((NextU64) % 64) + 1
    $v = (NextU64) / [System.Numerics.BigInteger]::Pow(2, 64 - $bits)
    if ($v -gt $I64MAX) { $v = $v - $script:TWO64 }
    if (((NextU64) % 2) -eq 1) { $v = -$v }
    if ($v -lt $I64MIN) { $v = $I64MIN }
    if ($v -gt $I64MAX) { $v = $I64MAX }
    return $v
}
function RandD { $u = NextU64; $mode = $u % 3; $w = $u / 256; if ($mode -eq 0) { return ($w % 1000000000) + 1 } elseif ($mode -eq 1) { return ($w % 100) + 1 } else { return 1000000000 - ($w % 1000) } }
for ($k = 0; $k -lt 15000; $k++) { Add (RandI64) (RandI64) (RandD) }
# ties: a = m*d + d/2 (d even), b odd -> a*b/d is exactly half-way
for ($k = 0; $k -lt 8000; $k++) {
    $d = [System.Numerics.BigInteger](2 * (((NextU64) % 500000000) + 1))
    $m = [System.Numerics.BigInteger]((NextU64) % 4000000000)
    if (((NextU64) % 4) -eq 0) { $m = [System.Numerics.BigInteger]((NextU64) / [System.Numerics.BigInteger]::Pow(2, 34)) }
    $a = $m * $d + $d / 2
    if ($a -gt $I64MAX) { continue }
    $b = [System.Numerics.BigInteger](2 * ((NextU64) % 1000) + 1)
    if (((NextU64) % 2) -eq 1) { $a = -$a }
    if (((NextU64) % 2) -eq 1) { $b = -$b }
    Add $a $b $d
}
# results near the int64 limits: a*b ~ +-2^63 * d
for ($k = 0; $k -lt 4000; $k++) {
    $d = RandD
    $t = [System.Numerics.BigInteger]::Pow(2, 63) * $d + [System.Numerics.BigInteger](((NextU64) % 2000000001)) - 1000000000
    $b = [System.Numerics.BigInteger]((NextU64) % 1000 + 1) * $d
    $a = $t / $b
    if ($a -gt $I64MAX -or $b -gt $I64MAX) { continue }
    if (((NextU64) % 2) -eq 1) { $a = -$a }
    Add $a $b $d
    if ($a -ge $I64MIN + 1) { Add ($a - 1) $b $d }
}
[System.IO.File]::WriteAllText($Out, $sb.ToString(), [System.Text.UTF8Encoding]::new($false))
$lines = ($sb.ToString() -split "`n").Count - 1
Write-Output "wrote $lines vectors to $Out"
```

#### 5. 検証結果（期待値と実測値を区別）

4構成とも `RESULT: 46 checks passed, 0 failed`、終了コード 0 でした。失敗のケースでは、`*out` に置いた番兵値が変わらないことも毎回確認しています。

| テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| (1,5,2) | 2（2.5 → 偶数側） | 2 | 実施（4構成） |
| (3,5,2) | 8（7.5 → 偶数側） | 8 | 実施 |
| (−3,5,2) | −8 | −8 | 実施 |
| (1,3,2) | 2（1.5 → 2） | 2 | 実施 |
| (1,1,2) | 0（0.5 → 0） | 0 | 実施 |
| (INT64_MIN,1,1) | INT64_MIN | INT64_MIN | 実施 |
| (INT64_MAX,2,1) | 失敗、*out 不変 | 失敗、不変 | 実施 |
| d=0 | 失敗、*out 不変 | 失敗、不変 | 実施 |
| out=NULL | 失敗 | 失敗 | 実施 |
| d=1e9+1、d=UINT64_MAX | 失敗 | 失敗 | 実施 |
| d=1000000000 の大数 | INT64_MAX/1e9 → 9223372037、INT64_MIN/1e9 → −9223372037、INT64_MIN×999999999/1e9 → −9223372027631403771 | 一致 | 実施 |
| INT64_MIN の境界 | (MIN,−1,1)・(MIN,−2,2) は 2^63 で失敗。(MIN,2,2) は中間値 2^64 を経て MIN。(MIN,−1,2) は 2^62 | 一致 | 実施 |
| 符号の4組み合わせ（T3） | (±5,±1,2) → ±2、(±7,±1,2) → ±4、(±11,±1,2) → ±6、(±9,±1,2) → ±4 の16通りと、÷4・÷3 の各組み合わせ | 一致 | 実施 |
| BigInteger の参照ベクトル（T4） | 45,354件（成功を期待 34,535件、失敗を期待 10,819件）。必須例8件、32値×32値×14除数の境界格子14,336件、無効な除数12件、乱数15,000件、厳密な中間値と結果が ±2^63 付近になるケースを合わせて15,998件 | 不一致0 | 実施（4構成） |
| 境界周辺を含む乱数500万件（T5） | ビット単位の独立した筆算による参照実装と、成否・値が完全一致。20%（100万件）は厳密な中間値を強制 | 不一致0（失敗を期待した件数 873,240件。MSVC 版は 873,246件。差はテストの乱数式の評価順序がコンパイラで異なるため） | 実施（4構成） |
| ASan / UBSan（符号付き桁あふれ・シフトの検出） | 報告なし | 報告なし | 実施 |
| GCC・Linux | — | 未実施 | 未実施 |

#### 6. 未実施項目・制約・残課題

- GCC・Linux での実行は未実施です。
- 入力空間（2^64 × 2^64 × 10^9）は総当たりできないため、境界格子・任意精度の参照値・500万件の乱数比較による検証です。
- MSVC 版でテストの「失敗を期待した件数」の表示が6件違うのは、テスト中の乱数式で関数呼び出しの評価順序が未規定であり、コンパイラによって生成される入力列が異なるためです。どちらも参照実装との不一致は0件です。

---

## Q06【地獄級】停電・強制終了に耐える単一ファイルのトランザクション保存

**狙い：** short write、EINTR、fsync、renameのコミット境界、ディレクトリ同期、復旧可能性。

**対象環境：Linux/POSIXローカルファイルシステム**。この問だけPOSIXシステムコールを許可。クラッシュ整合性については、同一ディレクトリでのatomic renameとfsyncの通常の永続化契約に従う環境を前提とする。機器の故障・ファイルシステム破損までは保証対象外。

### 元コード（危険）

~~~c
#include <stdio.h>
int save_bad(const char *name, const void *data, size_t len) {
    FILE *f = fopen(name, "wb");
    if (!f) return -1;
    fwrite(data, 1, len, f);
    fclose(f);
    return 0;
}
~~~

### ファイル形式とAPI

- ファイル内容は **MAGIC=43 43 41 44（4バイト）／VERSION=01（1バイト）／payload長uint32リトルエンディアン／payload／Q01と同じCRC32(uint32リトルエンディアン)**。CRC対象はVERSION＋長さ4バイト＋payload。最大payloadは16MiB。
- **save_atomic(dir_path, basename, data, len)** を設計。basenameは英数字・アンダースコア・ハイフン・ピリオドの1〜64バイト、ただし "." と ".." を拒否。パス区切り文字を拒否。対象は通常ファイルで、同じ対象への外部同時書換え・シンボリックリンクは対象外（検出可能な場合は安全に拒否）。
- 書込み先と同じディレクトリに衝突しない一時ファイルを O_EXCL で作る。short writeとEINTRに対応し、**一時ファイルのfsync成功 → renameatによる置換 → 親ディレクトリのfsync** の順を守る。
- 戻り値は **SAVE_OK / SAVE_ERROR_BEFORE_RENAME / SAVE_COMMIT_UNCERTAIN**。rename前の失敗なら既存の対象ファイルの内容を保持。rename後のfsync等の失敗ならコミット済みか不確定として明示し、成功とは偽らない。
- エラー・中断で開いたfdや一時ファイルを適切に処理する。対象ファイルがなかった場合の新規作成にも対応。書込み途中の壊れた内容を完成版として公開しない。
- テスト用にwrite/fsync/renameat失敗を注入できる小さな抽象化を設けてもよい。テスト専用の一時ディレクトリを使い、既存ユーザーデータは絶対に書き換えない。

### 必須検証

初回作成、既存置換、空payload、最大payload、short write、EINTR、ディスク満杯相当の失敗注入、temp fsync失敗、rename失敗、directory fsync失敗、basename拒否、成功ファイルのCRC検証、各段階のプロセス強制終了後に観測される内容が旧完全版か新完全版であること。未確認のクラッシュ耐性を「保証済み」としない。

### ★★FREYJAの回答【Q06・記入欄】

> **ここから下が回答用スペースです。** GitHubの鉛筆アイコンからファイルを編集し、各「（ここに記入）」やコード枠内の例示文を自分の回答に置き換えてください。問題文・要求仕様・元コードは変更しないこと。

#### 1. 根本原因・危険性・影響

**元コード `save_bad` の問題点（静的解析）**

| # | 箇所 | 根本原因 | 障害時に起きること |
| --- | --- | --- | --- |
| 1 | `fopen(name, "wb")` | 開いた瞬間に既存の対象ファイルを長さ0に切り詰める | 書き込み中に強制終了・停電すると、旧版も新版も失われる（空や書きかけの内容が完成版として残る） |
| 2 | `fwrite` の戻り値を無視 | short write、ENOSPC、EIO を検出しない | 書きかけのファイルなのに 0（成功）を返す |
| 3 | `fclose` の戻り値を無視 | stdio のバッファーは fclose 時にまとめて書き出され、遅れて起きた書き込みエラーがここで報告される | エラーを見落とす |
| 4 | fsync なし | データがページキャッシュにしかない | 停電でデータが失われる（遅延割り当てのファイルシステムでは長さ0のファイルが残ることもある） |
| 5 | 原子的な置換なし | 同じファイルをその場で書き換える | 読み手が書きかけの内容を観測する |
| 6 | ディレクトリの fsync なし | 新規作成や名前の変更が永続化されない | 停電後にファイルそのものが消えることがある |
| 7 | 完全性の情報なし | マジック・長さ・CRC がない | 壊れた内容を読み込み時に検出できない |
| 8 | 名前の検証なし | `../x`、絶対パス、シンボリックリンクをそのまま開く | パストラバーサルや、シンボリックリンクを使った任意ファイルの上書き |
| 9 | EINTR・長さの上限 | 考慮していない | シグナルで失敗する。巨大な入力を受け付ける |

**修正方針（コミットの手順）：** 「対象ディレクトリを `O_DIRECTORY` で開く」→「対象を `fstatat(AT_SYMLINK_NOFOLLOW)` で確認し、シンボリックリンクと通常ファイル以外は拒否」→「同じディレクトリに `.<basename>.tmp-<64ビット乱数の16進>` を `O_CREAT|O_EXCL|O_NOFOLLOW|O_CLOEXEC` で作成（EEXIST なら名前を変えて最大32回）」→「ヘッダー・payload・CRC を、short write と EINTR を再試行しながらすべて書く」→「一時ファイルの fsync（EINTR のときだけ再試行。EIO の後は再試行しない）」→「close」→「**renameat による置換（コミット点）**」→「親ディレクトリの fsync」の順に実行します。rename より前の失敗は、一時ファイルを削除し、旧ファイルに一切触れずに `SAVE_ERROR_BEFORE_RENAME` を返します。rename より後の失敗は `SAVE_COMMIT_UNCERTAIN` を返し、成功とは偽りません。開いた fd はすべての経路で閉じます。テストのために、システムコールを `save_ops` の関数ポインターで差し替えられるようにしました（問題文で許可された小さな抽象化）。実際の POSIX 実装は `posix_ops` です。

#### 2. 完全な修正版Cコード

```c
/*
 * Q06: crash-safe single-file transactional save (C17 + POSIX, Linux target)
 *
 * File format: 43 43 41 44 | 01 | payload length (uint32 LE) | payload | CRC-32 (uint32 LE)
 * CRC-32 (IEEE, as in Q01) over VERSION + 4 length bytes + payload. Max payload 16 MiB.
 *
 * Commit protocol (same directory):
 *   open dir -> refuse symlink / non-regular target -> create unique temp with O_EXCL
 *   -> write all (short write / EINTR safe) -> fsync(temp) -> close(temp)
 *   -> renameat(temp, target)            <- commit point
 *   -> fsync(dir)                        <- durability of the new directory entry
 * Any failure before renameat leaves the old target untouched and removes the temp
 * file (SAVE_ERROR_BEFORE_RENAME). A failure after renameat is reported as
 * SAVE_COMMIT_UNCERTAIN, never as success.
 *
 * The system calls go through `save_ops` so that tests can inject faults; the
 * real POSIX implementation is compiled unless SAVE_NO_POSIX is defined.
 *
 * Original implementation by ★★FREYJA for the C hell-level exam.
 */
#include <errno.h>
#include <stddef.h>
#include <stdint.h>
#include <string.h>

/* ------------------------------------------------------------------ API */

#define SAVE_MAX_PAYLOAD  (16u * 1024u * 1024u)
#define SAVE_MAX_BASENAME 64u
#define SAVE_HEADER_SIZE  9u
#define SAVE_TRAILER_SIZE 4u

typedef enum {
    SAVE_OK = 0,
    SAVE_ERROR_BEFORE_RENAME,   /* nothing committed: the previous target is intact     */
    SAVE_COMMIT_UNCERTAIN       /* rename happened, durability could not be confirmed   */
} save_result;

/* System-call abstraction. All functions return -1 and set errno on failure. */
typedef struct {
    void *ctx;
    int  (*open_dir)(void *ctx, const char *path);                        /* -> dir fd            */
    int  (*check_target)(void *ctx, int dirfd, const char *name);         /* 0: absent or regular */
    int  (*create_excl)(void *ctx, int dirfd, const char *name);          /* O_EXCL|O_NOFOLLOW    */
    long (*write)(void *ctx, int fd, const void *buf, size_t n);          /* may be short         */
    int  (*fsync)(void *ctx, int fd);
    int  (*rename)(void *ctx, int dirfd, const char *from, const char *to); /* renameat, same dir */
    int  (*unlink)(void *ctx, int dirfd, const char *name);
    int  (*close)(void *ctx, int fd);
    uint64_t (*entropy)(void *ctx);                                       /* temp-name randomness */
} save_ops;

int         save_basename_valid(const char *basename);
uint32_t    save_crc32(uint32_t reg, const unsigned char *p, size_t n);
save_result save_atomic_ops(const save_ops *ops, const char *dir_path, const char *basename,
                            const void *data, size_t len, int *err_out);
#ifndef SAVE_NO_POSIX
save_result save_atomic(const char *dir_path, const char *basename, const void *data, size_t len);
save_result save_atomic_err(const char *dir_path, const char *basename, const void *data, size_t len, int *err_out);
#endif

/* ------------------------------------------------------- implementation */

static const uint32_t save_crc_nibble[16] = {
    0x00000000u, 0x1DB71064u, 0x3B6E20C8u, 0x26D930ACu, 0x76DC4190u, 0x6B6B51F4u, 0x4DB26158u, 0x5005713Cu,
    0xEDB88320u, 0xF00F9344u, 0xD6D6A3E8u, 0xCB61B38Cu, 0x9B64C2B0u, 0x86D3D2D4u, 0xA00AE278u, 0xBDBDF21Cu
};

uint32_t save_crc32(uint32_t reg, const unsigned char *p, size_t n)
{
    for (size_t i = 0; i < n; ++i) {
        reg ^= p[i];
        reg = (reg >> 4) ^ save_crc_nibble[reg & 0x0Fu];
        reg = (reg >> 4) ^ save_crc_nibble[reg & 0x0Fu];
    }
    return reg;
}

int save_basename_valid(const char *s)
{
    if (s == NULL)
        return 0;
    size_t n = 0;
    for (; s[n] != '\0'; ++n) {
        if (n >= SAVE_MAX_BASENAME)
            return 0;                                   /* longer than 64 bytes */
        unsigned char c = (unsigned char)s[n];
        int ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') ||
                 c == '_' || c == '-' || c == '.';
        if (!ok)
            return 0;                                   /* includes '/', '\\', spaces, >= 0x80 */
    }
    if (n == 0)
        return 0;
    if ((n == 1 && s[0] == '.') || (n == 2 && s[0] == '.' && s[1] == '.'))
        return 0;
    return 1;
}

static void put_le32(unsigned char *p, uint32_t v)
{
    p[0] = (unsigned char)v; p[1] = (unsigned char)(v >> 8);
    p[2] = (unsigned char)(v >> 16); p[3] = (unsigned char)(v >> 24);
}

/* Write everything, retrying on EINTR and short writes. */
static int write_all(const save_ops *ops, int fd, const unsigned char *p, size_t n)
{
    while (n > 0) {
        size_t chunk = n > ((size_t)1 << 20) ? ((size_t)1 << 20) : n;   /* fits in long */
        errno = 0;
        long r = ops->write(ops->ctx, fd, p, chunk);
        if (r < 0) {
            if (errno == EINTR)
                continue;
            if (errno == 0)
                errno = EIO;
            return -1;
        }
        if (r == 0 || (unsigned long)r > chunk) {   /* no progress / impossible count */
            errno = EIO;
            return -1;
        }
        p += (size_t)r;
        n -= (size_t)r;
    }
    return 0;
}

static int fsync_retry(const save_ops *ops, int fd)
{
    for (;;) {
        errno = 0;
        if (ops->fsync(ops->ctx, fd) == 0)
            return 0;
        if (errno != EINTR) {          /* never retry after EIO: the error would be lost */
            if (errno == 0)
                errno = EIO;
            return -1;
        }
    }
}

static const char hexdig[] = "0123456789abcdef";

/* ".<basename>.tmp-<16 hex>" : at most 1 + 64 + 5 + 16 + 1 = 87 bytes */
static void make_temp_name(char *out, const char *base, uint64_t r)
{
    size_t k = 0, bl = strlen(base);
    out[k++] = '.';
    memcpy(out + k, base, bl); k += bl;
    memcpy(out + k, ".tmp-", 5); k += 5;
    for (int i = 15; i >= 0; --i)
        out[k++] = hexdig[(r >> (4 * i)) & 0xFu];
    out[k] = '\0';
}

save_result save_atomic_ops(const save_ops *ops, const char *dir_path, const char *basename,
                            const void *data, size_t len, int *err_out)
{
    int err = 0;
    save_result res = SAVE_ERROR_BEFORE_RENAME;
    int dirfd = -1, tfd = -1, temp_created = 0;
    char tname[1 + SAVE_MAX_BASENAME + 5 + 16 + 1];
    unsigned char hdr[SAVE_HEADER_SIZE], trl[SAVE_TRAILER_SIZE];
    uint32_t crc;
    int cr;

    if (ops == NULL || dir_path == NULL || !save_basename_valid(basename) || (data == NULL && len != 0)) {
        err = EINVAL;
        goto out;
    }
    if (len > SAVE_MAX_PAYLOAD) {
        err = EFBIG;
        goto out;
    }

    errno = 0;
    dirfd = ops->open_dir(ops->ctx, dir_path);
    if (dirfd < 0) { err = errno ? errno : EIO; goto out; }

    errno = 0;
    if (ops->check_target(ops->ctx, dirfd, basename) != 0) { err = errno ? errno : EINVAL; goto out; }

    for (int attempt = 0; attempt < 32; ++attempt) {
        make_temp_name(tname, basename, ops->entropy(ops->ctx));
        errno = 0;
        tfd = ops->create_excl(ops->ctx, dirfd, tname);
        if (tfd >= 0 || errno != EEXIST)
            break;
    }
    if (tfd < 0) { err = errno ? errno : EIO; goto out; }
    temp_created = 1;

    hdr[0] = 0x43; hdr[1] = 0x43; hdr[2] = 0x41; hdr[3] = 0x44; hdr[4] = 0x01;
    put_le32(hdr + 5, (uint32_t)len);
    crc = save_crc32(0xFFFFFFFFu, hdr + 4, 5);
    if (len != 0)
        crc = save_crc32(crc, (const unsigned char *)data, len);
    put_le32(trl, crc ^ 0xFFFFFFFFu);

    if (write_all(ops, tfd, hdr, sizeof hdr) != 0 ||
        (len != 0 && write_all(ops, tfd, (const unsigned char *)data, len) != 0) ||
        write_all(ops, tfd, trl, sizeof trl) != 0) {
        err = errno;
        goto out;
    }
    if (fsync_retry(ops, tfd) != 0) { err = errno; goto out; }

    errno = 0;
    cr = ops->close(ops->ctx, tfd);
    tfd = -1;                                  /* the descriptor is released either way */
    if (cr != 0 && errno != EINTR) { err = errno ? errno : EIO; goto out; }

    errno = 0;
    if (ops->rename(ops->ctx, dirfd, tname, basename) != 0) { err = errno ? errno : EIO; goto out; }
    temp_created = 0;                          /* the temp name no longer exists */
    res = SAVE_COMMIT_UNCERTAIN;               /* from here on the new content is visible */

    if (fsync_retry(ops, dirfd) != 0) { err = errno; goto out; }
    res = SAVE_OK;

out:
    {
        int saved = errno;
        if (tfd >= 0)
            (void)ops->close(ops->ctx, tfd);
        if (temp_created)
            (void)ops->unlink(ops->ctx, dirfd, tname);   /* best effort; target untouched */
        if (dirfd >= 0)
            (void)ops->close(ops->ctx, dirfd);           /* after fsync, close cannot undo it */
        errno = saved;
    }
    if (err_out != NULL)
        *err_out = (res == SAVE_OK) ? 0 : err;
    return res;
}

/* --------------------------------------------- real POSIX implementation */
#ifndef SAVE_NO_POSIX
#include <fcntl.h>
#include <stdatomic.h>
#include <stdio.h>          /* renameat (POSIX.1-2008 declares it in <stdio.h>) */
#include <sys/stat.h>
#include <sys/types.h>
#include <time.h>
#include <unistd.h>

static int px_open_dir(void *ctx, const char *path)
{
    (void)ctx;
    int fd;
    do { fd = open(path, O_RDONLY | O_DIRECTORY | O_CLOEXEC); } while (fd < 0 && errno == EINTR);
    return fd;
}

static int px_check_target(void *ctx, int dirfd, const char *name)
{
    (void)ctx;
    struct stat st;
    if (fstatat(dirfd, name, &st, AT_SYMLINK_NOFOLLOW) != 0)
        return (errno == ENOENT) ? 0 : -1;             /* absent: first save */
    if (S_ISLNK(st.st_mode)) { errno = ELOOP; return -1; }
    if (!S_ISREG(st.st_mode)) { errno = EISDIR; return -1; }
    return 0;
}

static int px_create_excl(void *ctx, int dirfd, const char *name)
{
    (void)ctx;
    int fd;
    do {
        fd = openat(dirfd, name, O_WRONLY | O_CREAT | O_EXCL | O_CLOEXEC | O_NOFOLLOW, 0644);
    } while (fd < 0 && errno == EINTR);
    return fd;
}

static long px_write(void *ctx, int fd, const void *buf, size_t n)
{
    (void)ctx;
    return (long)write(fd, buf, n);
}

static int px_fsync(void *ctx, int fd) { (void)ctx; return fsync(fd); }
static int px_rename(void *ctx, int dirfd, const char *from, const char *to) { (void)ctx; return renameat(dirfd, from, dirfd, to); }
static int px_unlink(void *ctx, int dirfd, const char *name) { (void)ctx; return unlinkat(dirfd, name, 0); }
static int px_close(void *ctx, int fd) { (void)ctx; return close(fd); }

static uint64_t px_entropy(void *ctx)
{
    (void)ctx;
    static atomic_uint_fast64_t counter;                 /* zero-initialised, race-free */
    struct timespec ts;
    clock_gettime(CLOCK_REALTIME, &ts);
    uint64_t x = ((uint64_t)(unsigned)getpid() << 32) ^ (uint64_t)ts.tv_nsec ^ ((uint64_t)ts.tv_sec << 20) ^ ((uint64_t)(atomic_fetch_add(&counter, 1u) + 1u) * 0x9E3779B97F4A7C15u);
    x ^= x >> 31; x *= 0xBF58476D1CE4E5B9u; x ^= x >> 29;   /* mix */
    return x;
}

static const save_ops posix_ops = {
    NULL, px_open_dir, px_check_target, px_create_excl, px_write, px_fsync, px_rename, px_unlink, px_close, px_entropy
};

save_result save_atomic_err(const char *dir_path, const char *basename, const void *data, size_t len, int *err_out)
{
    return save_atomic_ops(&posix_ops, dir_path, basename, data, len, err_out);
}

save_result save_atomic(const char *dir_path, const char *basename, const void *data, size_t len)
{
    return save_atomic_ops(&posix_ops, dir_path, basename, data, len, NULL);
}
#endif /* SAVE_NO_POSIX */
```

#### 3. ビルド方法・使用環境

```sh
# PowerShell 7 で MSVC 開発環境（vcvars64.bat）を読み込んだ後、build.ps1 が次の4構成を順にビルド・実行する
# q06_test.c は #include "q06.c" により実装と同じ翻訳単位でビルドする
W='-Wall -Wextra -Wpedantic -Wshadow -Wconversion -Wsign-conversion -Wcast-qual -Wstrict-prototypes -Wmissing-prototypes -Wvla -Wformat=2'
# (1) Clang -O2
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O2 -fuse-ld=link q06_test.c -o q06_rel.exe
# (2) Clang + AddressSanitizer（MSVC 同梱の ASan ランタイムでリンク）
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O1 -g -fsanitize=address -fno-omit-frame-pointer -fms-runtime-lib=dll -c q06_test.c -o q06_asan.obj
link /nologo /DEBUG /OUT:q06_asan.exe q06_asan.obj clang_rt.asan_dynamic-x86_64.lib clang_rt.asan_dynamic_runtime_thunk-x86_64.lib msvcrt.lib ucrt.lib vcruntime.lib legacy_stdio_definitions.lib
# (3) Clang + UndefinedBehaviorSanitizer（トラップ方式）
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O1 -g -fsanitize=undefined -fsanitize-trap=undefined -fno-sanitize-recover=all -fuse-ld=link q06_test.c -o q06_ubsan.exe
# (4) MSVC（2つ目のコンパイラ）
cl /nologo /std:c17 /W4 /experimental:c11atomics /utf-8 /O2 /D_CRT_SECURE_NO_WARNINGS q06_test.c /Fe:q06_msvc.exe
# 各 .exe を実行し、終了コード 0 と "RESULT: N checks passed, 0 failed" を確認
# 実POSIX経路のコンパイル確認（bionic = Linux/POSIX libc。実行は未実施）
clang --target=x86_64-linux-android34  -std=c17 -D_POSIX_C_SOURCE=200809L -Wall -Wextra -Wpedantic -Wshadow -Wconversion -Wsign-conversion -Wstrict-prototypes -O2 -c q06.c
clang --target=aarch64-linux-android34 -std=c17 -D_POSIX_C_SOURCE=200809L -Wall -Wextra -Wpedantic -Wshadow -Wconversion -Wsign-conversion -Wstrict-prototypes -O2 -c q06.c
clang --target=x86_64-linux-android34  -std=c17 -Wall -Wextra -Wpedantic -O1 -g q06_posix_test.c -o q06_posix_test_x86_64
clang --target=aarch64-linux-android34 -std=c17 -Wall -Wextra -Wpedantic -O1 -g q06_posix_test.c -o q06_posix_test_aarch64
# Linux での想定コマンド（環境がないため未実施）
gcc -std=c17 -D_POSIX_C_SOURCE=200809L -Wall -Wextra -Wpedantic -O1 -g -fsanitize=address,undefined q06_posix_test.c -o q06_posix_test && ./q06_posix_test
```

**使用環境（全問共通）**

- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド、x86-64）
- コンパイラ1：**Clang 18.0.3**（Android NDK r27c に同梱。ターゲットは `x86_64-pc-windows-msvc`。ヘッダー・ライブラリは MSVC 14.52.37025 と Windows SDK/UCRT 10.0.26100、リンカーは link.exe）。`-std=c17` を指定。
- コンパイラ2：**MSVC cl 19.52.37029**（Visual Studio 18 Community）、`/std:c17 /W4`。
- GCC：この PC にはなく、使用していません。Linux／WSL の環境もありません（外部パッケージの無断導入禁止のため、導入していません）。
- AddressSanitizer：Clang でインストルメントし、MSVC 同梱の ASan ランタイム（`clang_rt.asan_dynamic-x86_64`）でリンクしました。ヒープ境界外書き込みの陽性対照で、`heap-buffer-overflow` を検出することを確認済みです。
- UndefinedBehaviorSanitizer：`-fsanitize=undefined -fsanitize-trap=undefined`（トラップ方式。ランタイムを使わず、未定義動作で不正命令の例外として異常終了します）。符号付きオーバーフローの陽性対照で、異常終了することを確認済みです。
- LeakSanitizer：Windows 版 ASan にないため未実施です。リークは各問の計数アロケーター（確保と解放の数の差）で検査しました。ThreadSanitizer：Windows では使えないため未実施です。
- ビルド補助 `shim/stdatomic.h`（製品コード外）：NDK の Clang に付属する `stdatomic.h` は Android（bionic）専用で Windows ではコンパイルできないため、MSVC の C11 実装 `vcruntime_c11_stdatomic.h`（Clang 互換の `__c11_atomic` 組み込み関数を使用）を読み込む1ファイルを置きました。Clang 18 が `__typeof_unqual__` に対応していないため、`atomic_is_lock_free` だけは `__c11_atomic_is_lock_free` で定義し直しています。
- `-D_CRT_SECURE_NO_WARNINGS`：MSVC の CRT が出す「`fopen` などの代わりに `_s` 版を使うように」という非推奨の通知だけを止める定義です（C17 標準関数の使用に対する通知であり、コードの欠陥を隠すものではありません）。

- Q06 の構成：Windows には POSIX の `openat`・`renameat`・`fsync` がありません。そこで Windows 上の4構成では `SAVE_NO_POSIX` を定義して実POSIX部分を除外し、模擬ファイルシステム（`q06_test.c`）で保存の手順・障害注入・クラッシュの整合性を検証しました。実POSIX部分（`posix_ops`）は、Android NDK の bionic（Linux カーネル上の POSIX libc）向けにコンパイル・リンクして確認しました（x86_64・aarch64 とも警告0件。`renameat`・`fsync`・`openat`・`fstatat`・`unlinkat`・`fork`・`mkdtemp` の参照が解決されることを `llvm-nm` で確認）。
- **自律修正：** 最初の Android 向けコンパイルで「`renameat` の暗黙の宣言」エラーを検出しました。原因は、POSIX.1-2008 では `renameat` が `<stdio.h>` で宣言されるのに、POSIX 部分でこのヘッダーを読み込んでいなかったことです。`#include <stdio.h>` を追加して修正し、再コンパイルで警告0件を確認しました。この変更は `SAVE_NO_POSIX` で除外される部分だけなので、Windows のテストには影響しません。
- **検査の検出力の確認：** 作業用のコピーで、一時ファイルの fsync を削除した変異版を作りました。T1・T2・T5 と、T10 のクラッシュ検査（停電時に「内容不明」と判定）が失敗することを確認しました。製品コードは変更していません。

#### 4. テストコード

模擬ファイルシステムによるテスト（Windows で実行）：

```c
/* Q06 tests against an in-memory model file system with fault and crash injection.
 * The model distinguishes the volatile state (what a killed process leaves
 * behind) from the durable state (what survives a power loss): file data
 * becomes durable only by fsync(file), directory entries only by fsync(dir). */
#define SAVE_NO_POSIX 1
#include "q06.c"
#include <stdio.h>
#include <stdlib.h>

static int g_fail = 0, g_pass = 0;
#define CHECK(cond, ...) do { if (cond) { ++g_pass; } else { ++g_fail; \
    printf("  FAIL %s:%d: ", __FILE__, __LINE__); printf(__VA_ARGS__); printf("\n"); } } while (0)
static void report(const char *name, int before_fail)
{
    printf("[%s] %s\n", g_fail == before_fail ? "PASS" : "FAIL", name);
}

/* ---------------------------------------------------------------- model FS */
#define MAXINO 64
#define MAXENT 64
#define MAXFD  16
typedef struct { int used; unsigned char *data; size_t len, cap; unsigned char *dur; size_t dlen; int has_dur; } inode_t;
typedef struct { char name[128]; int ino; } dent_t;
typedef struct {
    inode_t ino[MAXINO];
    dent_t vol[MAXENT]; int nvol;            /* volatile namespace                 */
    dent_t dur[MAXENT]; int ndur;            /* durable namespace (last dir fsync)  */
    int fd_kind[MAXFD], fd_ino[MAXFD];       /* 0 free, 1 file, 2 directory        */
    /* fault injection */
    int fail_open_dir, target_kind, eexist_first, eintr_every, fsync_eintr_once;
    size_t short_max, quota;
    int fail_fsync_file, fail_fsync_dir, fail_rename, fail_close_file;
    long crash_at, ops, writes;
    int crashed;
    uint64_t ent;
    /* observations captured at the crash point: 0 absent 1 old 2 new 3 other */
    int obs_kill, obs_power_durable_ns, obs_power_volatile_ns;
    long n_create, n_unlink, n_fsync_file, n_fsync_dir, n_rename;
} mfs;

static const unsigned char *g_old; static size_t g_old_len;
static const unsigned char *g_new; static size_t g_new_len;

static uint32_t le32(const unsigned char *p) { return (uint32_t)p[0] | ((uint32_t)p[1] << 8) | ((uint32_t)p[2] << 16) | ((uint32_t)p[3] << 24); }

/* independent check of the file format (bitwise CRC) */
static uint32_t ref_crc(const unsigned char *p, size_t n)
{
    uint32_t c = 0xFFFFFFFFu;
    for (size_t i = 0; i < n; ++i) { c ^= p[i]; for (int k = 0; k < 8; ++k) c = (c & 1u) ? (c >> 1) ^ 0xEDB88320u : c >> 1; }
    return ~c;
}
static int parse_file(const unsigned char *f, size_t n, const unsigned char **pl, size_t *pll)
{
    if (n < 13 || f[0] != 0x43 || f[1] != 0x43 || f[2] != 0x41 || f[3] != 0x44 || f[4] != 0x01) return 0;
    uint32_t L = le32(f + 5);
    if ((size_t)L + 13u != n || L > SAVE_MAX_PAYLOAD) return 0;
    if (ref_crc(f + 4, 5u + L) != le32(f + 9 + L)) return 0;
    *pl = f + 9; *pll = L;
    return 1;
}
static int classify(const unsigned char *f, size_t n)
{
    const unsigned char *pl; size_t pll;
    if (!parse_file(f, n, &pl, &pll)) return 3;
    if (g_old && pll == g_old_len && memcmp(pl, g_old, pll) == 0) return 1;
    if (pll == g_new_len && (pll == 0 || memcmp(pl, g_new, pll) == 0)) return 2;
    return 3;
}

static int find_ent(const dent_t *e, int n, const char *name) { for (int i = 0; i < n; ++i) if (strcmp(e[i].name, name) == 0) return i; return -1; }

static int observe(mfs *m, const dent_t *ns, int nns, int durable_data)
{
    int i = find_ent(ns, nns, "target.bin");
    if (i < 0) return 0;
    inode_t *in = &m->ino[ns[i].ino];
    if (durable_data) return in->has_dur ? classify(in->dur, in->dlen) : 3;   /* un-synced data: unknown */
    return classify(in->data, in->len);
}

static int maybe_crash(mfs *m)
{
    if (m->crashed) { errno = EIO; return 1; }
    m->ops++;
    if (m->crash_at != 0 && m->ops == m->crash_at) {
        m->crashed = 1;
        m->obs_kill = observe(m, m->vol, m->nvol, 0);
        m->obs_power_durable_ns = observe(m, m->dur, m->ndur, 1);
        m->obs_power_volatile_ns = observe(m, m->vol, m->nvol, 1);
        errno = EIO;
        return 1;
    }
    return 0;
}

static int new_fd(mfs *m, int kind, int ino) { for (int i = 3; i < MAXFD; ++i) if (!m->fd_kind[i]) { m->fd_kind[i] = kind; m->fd_ino[i] = ino; return i; } errno = EMFILE; return -1; }
static int new_ino(mfs *m) { for (int i = 1; i < MAXINO; ++i) if (!m->ino[i].used) { memset(&m->ino[i], 0, sizeof m->ino[i]); m->ino[i].used = 1; return i; } return -1; }

static int m_open_dir(void *c, const char *p)
{
    mfs *m = (mfs *)c; (void)p;
    if (maybe_crash(m)) return -1;
    if (m->fail_open_dir) { errno = ENOENT; return -1; }
    return new_fd(m, 2, 0);
}
static int m_check(void *c, int dfd, const char *name)
{
    mfs *m = (mfs *)c; (void)dfd; (void)name;
    if (maybe_crash(m)) return -1;
    if (m->target_kind == 1) { errno = ELOOP; return -1; }
    if (m->target_kind == 2) { errno = EISDIR; return -1; }
    return 0;
}
static int m_create(void *c, int dfd, const char *name)
{
    mfs *m = (mfs *)c; (void)dfd;
    if (maybe_crash(m)) return -1;
    m->n_create++;
    if (m->eexist_first > 0) { m->eexist_first--; errno = EEXIST; return -1; }
    if (find_ent(m->vol, m->nvol, name) >= 0) { errno = EEXIST; return -1; }
    if (strlen(name) >= sizeof m->vol[0].name || m->nvol >= MAXENT) { errno = ENAMETOOLONG; return -1; }
    int in = new_ino(m);
    if (in < 0) { errno = ENOSPC; return -1; }
    strcpy(m->vol[m->nvol].name, name); m->vol[m->nvol].ino = in; m->nvol++;
    return new_fd(m, 1, in);
}
static long m_write(void *c, int fd, const void *buf, size_t n)
{
    mfs *m = (mfs *)c;
    if (maybe_crash(m)) return -1;
    if (fd < 0 || fd >= MAXFD || m->fd_kind[fd] != 1) { errno = EBADF; return -1; }
    m->writes++;
    if (m->eintr_every && m->writes % m->eintr_every == 0) { errno = EINTR; return -1; }
    inode_t *in = &m->ino[m->fd_ino[fd]];
    size_t k = n;
    if (m->short_max && k > m->short_max) k = m->short_max;
    if (m->quota) {
        size_t room = m->quota > in->len ? m->quota - in->len : 0;
        if (room == 0) { errno = ENOSPC; return -1; }
        if (k > room) k = room;
    }
    if (in->len + k > in->cap) {
        size_t nc = in->cap ? in->cap : 64;
        while (nc < in->len + k) nc *= 2;
        unsigned char *nd = (unsigned char *)realloc(in->data, nc);
        if (!nd) { errno = ENOMEM; return -1; }
        in->data = nd; in->cap = nc;
    }
    memcpy(in->data + in->len, buf, k);
    in->len += k;
    return (long)k;
}
static int m_fsync(void *c, int fd)
{
    mfs *m = (mfs *)c;
    if (maybe_crash(m)) return -1;
    if (fd < 0 || fd >= MAXFD || !m->fd_kind[fd]) { errno = EBADF; return -1; }
    if (m->fsync_eintr_once) { m->fsync_eintr_once = 0; errno = EINTR; return -1; }
    if (m->fd_kind[fd] == 2) {
        m->n_fsync_dir++;
        if (m->fail_fsync_dir) { errno = EIO; return -1; }
        memcpy(m->dur, m->vol, sizeof m->vol); m->ndur = m->nvol;
        return 0;
    }
    m->n_fsync_file++;
    if (m->fail_fsync_file) { errno = EIO; return -1; }
    inode_t *in = &m->ino[m->fd_ino[fd]];
    free(in->dur);
    in->dur = (unsigned char *)malloc(in->len ? in->len : 1);
    if (in->len) memcpy(in->dur, in->data, in->len);
    in->dlen = in->len; in->has_dur = 1;
    return 0;
}
static int m_rename(void *c, int dfd, const char *from, const char *to)
{
    mfs *m = (mfs *)c; (void)dfd;
    if (maybe_crash(m)) return -1;
    m->n_rename++;
    if (m->fail_rename) { errno = EIO; return -1; }
    int i = find_ent(m->vol, m->nvol, from);
    if (i < 0) { errno = ENOENT; return -1; }
    int ino = m->vol[i].ino;
    m->vol[i] = m->vol[--m->nvol];                          /* remove source */
    int j = find_ent(m->vol, m->nvol, to);
    if (j >= 0) m->vol[j].ino = ino;                         /* atomic replace */
    else { strcpy(m->vol[m->nvol].name, to); m->vol[m->nvol].ino = ino; m->nvol++; }
    return 0;
}
static int m_unlink(void *c, int dfd, const char *name)
{
    mfs *m = (mfs *)c; (void)dfd;
    if (maybe_crash(m)) return -1;
    m->n_unlink++;
    int i = find_ent(m->vol, m->nvol, name);
    if (i < 0) { errno = ENOENT; return -1; }
    m->vol[i] = m->vol[--m->nvol];
    return 0;
}
static int m_close(void *c, int fd)
{
    mfs *m = (mfs *)c;
    if (maybe_crash(m)) return -1;
    if (fd < 0 || fd >= MAXFD || !m->fd_kind[fd]) { errno = EBADF; return -1; }
    int kind = m->fd_kind[fd];
    m->fd_kind[fd] = 0;                                      /* released even on error (Linux) */
    if (kind == 1 && m->fail_close_file) { errno = EIO; return -1; }
    return 0;
}
static uint64_t m_entropy(void *c) { mfs *m = (mfs *)c; m->ent += 0x9E3779B97F4A7C15u; return m->ent; }

static save_ops make_ops(mfs *m)
{
    save_ops o = { m, m_open_dir, m_check, m_create, m_write, m_fsync, m_rename, m_unlink, m_close, m_entropy };
    return o;
}

static unsigned char *build_file(const unsigned char *pl, size_t n, size_t *out_len)
{
    unsigned char *f = (unsigned char *)malloc(n + 13);
    f[0] = 0x43; f[1] = 0x43; f[2] = 0x41; f[3] = 0x44; f[4] = 0x01;
    f[5] = (unsigned char)n; f[6] = (unsigned char)(n >> 8); f[7] = (unsigned char)(n >> 16); f[8] = (unsigned char)(n >> 24);
    if (n) memcpy(f + 9, pl, n);
    uint32_t c = ref_crc(f + 4, 5 + n);
    f[9 + n] = (unsigned char)c; f[10 + n] = (unsigned char)(c >> 8); f[11 + n] = (unsigned char)(c >> 16); f[12 + n] = (unsigned char)(c >> 24);
    *out_len = n + 13;
    return f;
}

/* fresh model; with_old: target.bin exists (durable) with g_old content */
static void mfs_init(mfs *m, int with_old)
{
    memset(m, 0, sizeof *m);
    if (with_old) {
        int in = new_ino(m);
        size_t fl; unsigned char *f = build_file(g_old, g_old_len, &fl);
        m->ino[in].data = f; m->ino[in].len = fl; m->ino[in].cap = fl;
        m->ino[in].dur = (unsigned char *)malloc(fl); memcpy(m->ino[in].dur, f, fl); m->ino[in].dlen = fl; m->ino[in].has_dur = 1;
        strcpy(m->vol[0].name, "target.bin"); m->vol[0].ino = in; m->nvol = 1;
        memcpy(m->dur, m->vol, sizeof m->vol); m->ndur = 1;
    }
}
static void mfs_free(mfs *m) { for (int i = 0; i < MAXINO; ++i) { free(m->ino[i].data); free(m->ino[i].dur); } }
static int open_fds(const mfs *m) { int k = 0; for (int i = 0; i < MAXFD; ++i) k += m->fd_kind[i] != 0; return k; }
static int temp_left(const mfs *m) { for (int i = 0; i < m->nvol; ++i) if (strstr(m->vol[i].name, ".tmp-")) return 1; return 0; }
static int target_now(mfs *m) { return observe(m, m->vol, m->nvol, 0); }
static int target_durable(mfs *m) { return observe(m, m->dur, m->ndur, 1); }

static save_result do_save(mfs *m, const unsigned char *data, size_t len, int *err)
{
    save_ops o = make_ops(m);
    g_new = data; g_new_len = len;
    return save_atomic_ops(&o, "/test-dir", "target.bin", data, len, err);
}

int main(void)
{
    int f0, err;
    static unsigned char oldp[1000], newp[777];
    for (size_t i = 0; i < sizeof oldp; ++i) oldp[i] = (unsigned char)('A' + i % 26);
    for (size_t i = 0; i < sizeof newp; ++i) newp[i] = (unsigned char)(i * 13 + 5);
    g_old = oldp; g_old_len = sizeof oldp;
    mfs M;

    /* T1: first creation */
    f0 = g_fail;
    mfs_init(&M, 0);
    CHECK(do_save(&M, newp, sizeof newp, &err) == SAVE_OK && err == 0, "result OK");
    CHECK(target_now(&M) == 2 && target_durable(&M) == 2, "new file visible and durable");
    CHECK(!temp_left(&M) && open_fds(&M) == 0 && M.nvol == 1, "no temp file, no open fd");
    CHECK(M.n_fsync_file == 1 && M.n_fsync_dir == 1 && M.n_rename == 1, "fsync(temp), rename, fsync(dir) once each");
    mfs_free(&M);
    report("T1 initial creation (target absent)", f0);

    /* T2: replace existing */
    f0 = g_fail;
    mfs_init(&M, 1);
    CHECK(target_now(&M) == 1, "precondition: old content");
    CHECK(do_save(&M, newp, sizeof newp, &err) == SAVE_OK, "result OK");
    CHECK(target_now(&M) == 2 && target_durable(&M) == 2 && !temp_left(&M) && open_fds(&M) == 0, "replaced, durable, clean");
    mfs_free(&M);
    report("T2 replace existing file", f0);

    /* T3: empty payload, NULL data with len 0 */
    f0 = g_fail;
    mfs_init(&M, 1);
    CHECK(do_save(&M, NULL, 0, &err) == SAVE_OK && target_now(&M) == 2, "empty payload saved");
    { inode_t *in = &M.ino[M.vol[0].ino]; CHECK(in->len == 13, "empty file is 13 bytes"); }
    mfs_free(&M);
    report("T3 empty payload", f0);

    /* T4: maximum payload 16 MiB, and 16 MiB + 1 rejected before any syscall */
    f0 = g_fail;
    {
        unsigned char *big = (unsigned char *)malloc(SAVE_MAX_PAYLOAD + 1);
        for (size_t i = 0; i <= SAVE_MAX_PAYLOAD; ++i) big[i] = (unsigned char)(i ^ (i >> 9));
        mfs_init(&M, 1);
        M.short_max = 3u * 1024u * 1024u + 17u;            /* also exercise short writes */
        CHECK(do_save(&M, big, SAVE_MAX_PAYLOAD, &err) == SAVE_OK && target_now(&M) == 2, "16 MiB payload saved and CRC verified");
        mfs_free(&M);
        mfs_init(&M, 1);
        CHECK(do_save(&M, big, SAVE_MAX_PAYLOAD + 1u, &err) == SAVE_ERROR_BEFORE_RENAME && err == EFBIG && M.ops == 0, "16 MiB + 1 rejected (EFBIG), no syscall");
        CHECK(target_now(&M) == 1, "old intact");
        mfs_free(&M);
        free(big);
    }
    report("T4 maximum payload / oversize", f0);

    /* T5: short writes (1 byte per call) and EINTR */
    f0 = g_fail;
    mfs_init(&M, 1); M.short_max = 1;
    CHECK(do_save(&M, newp, sizeof newp, &err) == SAVE_OK && target_now(&M) == 2, "1-byte short writes");
    CHECK(M.writes == (long)(sizeof newp + 13), "one write call per byte (%ld)", M.writes);
    mfs_free(&M);
    mfs_init(&M, 1); M.short_max = 5; M.eintr_every = 2; M.fsync_eintr_once = 1;
    CHECK(do_save(&M, newp, sizeof newp, &err) == SAVE_OK && target_now(&M) == 2, "EINTR on every 2nd write + fsync EINTR retried");
    CHECK(M.n_fsync_file == 1, "fsync retried after EINTR");
    mfs_free(&M);
    report("T5 short write and EINTR", f0);

    /* T6..T10: failures before rename keep the old file and remove the temp */
    struct { const char *name; int what; int expect_err; } fb[] = {
        { "disk full (ENOSPC at 300 bytes)", 1, ENOSPC }, { "temp fsync failure", 2, EIO }, { "rename failure", 3, EIO },
        { "temp close failure", 4, EIO }, { "open dir failure", 5, ENOENT }, { "symlink target", 6, ELOOP },
        { "directory target", 7, EISDIR }, { "temp name collision x40", 8, EEXIST },
    };
    for (size_t k = 0; k < sizeof fb / sizeof fb[0]; ++k) {
        f0 = g_fail;
        mfs_init(&M, 1);
        switch (fb[k].what) {
        case 1: M.quota = 300; break; case 2: M.fail_fsync_file = 1; break; case 3: M.fail_rename = 1; break;
        case 4: M.fail_close_file = 1; break; case 5: M.fail_open_dir = 1; break; case 6: M.target_kind = 1; break;
        case 7: M.target_kind = 2; break; case 8: M.eexist_first = 40; break; default: break;
        }
        save_result r = do_save(&M, newp, sizeof newp, &err);
        CHECK(r == SAVE_ERROR_BEFORE_RENAME && err == fb[k].expect_err, "%s: result %d err %d", fb[k].name, (int)r, err);
        CHECK(target_now(&M) == 1 && target_durable(&M) == 1, "%s: old file intact", fb[k].name);
        CHECK(!temp_left(&M) && open_fds(&M) == 0, "%s: temp removed, fds closed", fb[k].name);
        mfs_free(&M);
        char title[96]; snprintf(title, sizeof title, "T6 failure before rename: %s", fb[k].name);
        report(title, f0);
    }

    /* T7: name collisions are retried */
    f0 = g_fail;
    mfs_init(&M, 1); M.eexist_first = 3;
    CHECK(do_save(&M, newp, sizeof newp, &err) == SAVE_OK && target_now(&M) == 2 && M.n_create == 4, "3 collisions then success");
    mfs_free(&M);
    report("T7 O_EXCL collision retried with a new name", f0);

    /* T8: directory fsync failure after rename -> COMMIT_UNCERTAIN */
    f0 = g_fail;
    mfs_init(&M, 1); M.fail_fsync_dir = 1;
    CHECK(do_save(&M, newp, sizeof newp, &err) == SAVE_COMMIT_UNCERTAIN && err == EIO, "reported as uncertain, not success");
    CHECK(target_now(&M) == 2 && target_durable(&M) == 1, "new visible, directory entry not yet durable");
    CHECK(!temp_left(&M) && open_fds(&M) == 0, "clean");
    mfs_free(&M);
    report("T8 directory fsync failure -> SAVE_COMMIT_UNCERTAIN", f0);

    /* T9: basename / argument validation */
    f0 = g_fail;
    {
        const char *bad[] = { "", ".", "..", "a/b", "/abs", "a\\b", "a b", "na\xC3\xAFve", "x:y", "*", "tab\t",
                              "12345678901234567890123456789012345678901234567890123456789012345" };
        const char *good[] = { "a", "...", ".hidden", "file.bin", "A-Z_0-9.x", "1234567890123456789012345678901234567890123456789012345678901234" };
        for (size_t i = 0; i < sizeof bad / sizeof bad[0]; ++i) {
            mfs_init(&M, 1); save_ops o = make_ops(&M);
            save_result r = save_atomic_ops(&o, "/d", bad[i], newp, 10, &err);
            CHECK(r == SAVE_ERROR_BEFORE_RENAME && err == EINVAL && M.ops == 0, "reject \"%s\"", bad[i]);
            mfs_free(&M);
        }
        for (size_t i = 0; i < sizeof good / sizeof good[0]; ++i) CHECK(save_basename_valid(good[i]), "accept \"%s\"", good[i]);
        CHECK(!save_basename_valid(NULL), "NULL basename");
        mfs_init(&M, 1); save_ops o = make_ops(&M);
        CHECK(save_atomic_ops(&o, NULL, "a", newp, 1, &err) == SAVE_ERROR_BEFORE_RENAME && err == EINVAL, "NULL dir");
        CHECK(save_atomic_ops(&o, "/d", "a", NULL, 1, &err) == SAVE_ERROR_BEFORE_RENAME && err == EINVAL, "NULL data with len>0");
        CHECK(save_atomic_ops(NULL, "/d", "a", newp, 1, &err) == SAVE_ERROR_BEFORE_RENAME && err == EINVAL, "NULL ops");
        CHECK(M.ops == 0 && target_now(&M) == 1, "nothing touched");
        mfs_free(&M);
    }
    report("T9 basename rules (1..64 of [A-Za-z0-9_.-], not . or ..) and argument checks", f0);

    /* T10: crash before every single system call (kill -9 and power loss views) */
    f0 = g_fail;
    {
        long total_points = 0;
        for (int with_old = 0; with_old <= 1; ++with_old)
            for (int shortw = 0; shortw <= 1; ++shortw) {
                mfs_init(&M, with_old); M.short_max = shortw ? 64 : 0;
                CHECK(do_save(&M, newp, sizeof newp, &err) == SAVE_OK, "reference run");
                long nops = M.ops;
                mfs_free(&M);
                for (long k = 1; k <= nops + 1; ++k) {
                    mfs_init(&M, with_old); M.short_max = shortw ? 64 : 0; M.crash_at = k;
                    (void)do_save(&M, newp, sizeof newp, &err);
                    int allowed_old = with_old ? 1 : 0;
                    int ok;
                    if (M.crashed) {
                        ok = (M.obs_kill == allowed_old || M.obs_kill == 2) &&
                             (M.obs_power_durable_ns == allowed_old || M.obs_power_durable_ns == 2) &&
                             (M.obs_power_volatile_ns == allowed_old || M.obs_power_volatile_ns == 2);
                    } else {
                        ok = (target_now(&M) == 2 && target_durable(&M) == 2);   /* crash after completion */
                    }
                    if (!ok) CHECK(0, "crash before op %ld (old=%d short=%d): kill=%d power(durable ns)=%d power(volatile ns)=%d",
                                   k, with_old, shortw, M.obs_kill, M.obs_power_durable_ns, M.obs_power_volatile_ns);
                    else ++g_pass;
                    ++total_points;
                    mfs_free(&M);
                }
                printf("  old=%d short_writes=%d: %ld system calls, crash points %ld..%ld checked\n", with_old, shortw, nops, 1L, nops + 1);
            }
        printf("  crash points checked: %ld\n", total_points);
    }
    report("T10 crash before each system call: observed content is old-complete or new-complete", f0);

    printf("RESULT: %d checks passed, %d failed\n", g_pass, g_fail);
    return g_fail ? 1 : 0;
}
```

実 Linux/POSIX 用の統合テスト（実ファイル、シンボリックリンク・ディレクトリの拒否、fork と SIGKILL による各システムコール直前での強制終了。コンパイル・リンクのみ確認、**実行は未実施**）：

```c
/* Q06 integration test for a real Linux/POSIX file system (run on Linux only).
 * Uses a private directory created with mkdtemp under $TMPDIR (default /tmp).
 * Crash test: a forked child is killed with SIGKILL right before its k-th system
 * call; the parent then checks that target.bin is the old or the new complete file.
 * Build: cc -std=c17 -D_POSIX_C_SOURCE=200809L -Wall -Wextra -O1 -g -fsanitize=address,undefined q06_posix_test.c */
#define _POSIX_C_SOURCE 200809L
#include "q06.c"
#include <dirent.h>
#include <signal.h>
#include <stdio.h>
#include <stdlib.h>
#include <sys/wait.h>

static int g_fail = 0, g_pass = 0;
#define CHECK(cond, ...) do { if (cond) { ++g_pass; } else { ++g_fail; \
    printf("  FAIL %s:%d: ", __FILE__, __LINE__); printf(__VA_ARGS__); printf("\n"); } } while (0)

static char g_dir[512];

static unsigned char *read_all(const char *name, size_t *n)
{
    char path[700]; snprintf(path, sizeof path, "%s/%s", g_dir, name);
    int fd = open(path, O_RDONLY | O_CLOEXEC);
    if (fd < 0) return NULL;
    size_t cap = 1 << 16, len = 0; unsigned char *b = malloc(cap);
    for (;;) {
        if (len == cap) { cap *= 2; b = realloc(b, cap); }
        ssize_t r = read(fd, b + len, cap - len);
        if (r < 0 && errno == EINTR) continue;
        if (r <= 0) break;
        len += (size_t)r;
    }
    close(fd);
    *n = len;
    return b;
}

/* 0 absent, 1 == payload a, 2 == payload b, 3 other */
static int classify(const unsigned char *a, size_t al, const unsigned char *b, size_t bl)
{
    size_t n; unsigned char *f = read_all("target.bin", &n);
    if (!f) return 0;
    int r = 3;
    if (n >= 13 && memcmp(f, "CCAD\x01", 5) == 0) {
        uint32_t L = (uint32_t)f[5] | ((uint32_t)f[6] << 8) | ((uint32_t)f[7] << 16) | ((uint32_t)f[8] << 24);
        if ((size_t)L + 13 == n) {
            uint32_t c = save_crc32(0xFFFFFFFFu, f + 4, 5u + L) ^ 0xFFFFFFFFu;
            uint32_t s = (uint32_t)f[9 + L] | ((uint32_t)f[10 + L] << 8) | ((uint32_t)f[11 + L] << 16) | ((uint32_t)f[12 + L] << 24);
            if (c == s) {
                if (a && L == al && memcmp(f + 9, a, al) == 0) r = 1;
                else if (L == bl && (bl == 0 || memcmp(f + 9, b, bl) == 0)) r = 2;
            }
        }
    }
    free(f);
    return r;
}

static int count_temps(void)
{
    DIR *d = opendir(g_dir); int k = 0;
    struct dirent *e;
    while ((e = readdir(d)) != NULL) if (strstr(e->d_name, ".tmp-")) ++k;
    closedir(d);
    return k;
}

/* ---- crash injection wrapper around the real POSIX ops ---- */
static long g_calls, g_kill_at;
static void tick(void) { if (++g_calls == g_kill_at) raise(SIGKILL); }
static int k_open_dir(void *c, const char *p) { tick(); return px_open_dir(c, p); }
static int k_check(void *c, int d, const char *n) { tick(); return px_check_target(c, d, n); }
static int k_create(void *c, int d, const char *n) { tick(); return px_create_excl(c, d, n); }
static long k_write(void *c, int fd, const void *b, size_t n) { tick(); return px_write(c, fd, b, n > 1000 ? 1000 : n); }
static int k_fsync(void *c, int fd) { tick(); return px_fsync(c, fd); }
static int k_rename(void *c, int d, const char *a, const char *b) { tick(); return px_rename(c, d, a, b); }
static int k_unlink(void *c, int d, const char *n) { tick(); return px_unlink(c, d, n); }
static int k_close(void *c, int fd) { tick(); return px_close(c, fd); }
static const save_ops kill_ops = { NULL, k_open_dir, k_check, k_create, k_write, k_fsync, k_rename, k_unlink, k_close, px_entropy };

int main(void)
{
    const char *tmp = getenv("TMPDIR");
    snprintf(g_dir, sizeof g_dir, "%s/q06test.XXXXXX", tmp && *tmp ? tmp : "/tmp");
    if (!mkdtemp(g_dir)) { perror("mkdtemp"); return 2; }
    printf("test directory: %s\n", g_dir);
    static unsigned char a[5000], b[3000];
    for (size_t i = 0; i < sizeof a; ++i) a[i] = (unsigned char)('a' + i % 26);
    for (size_t i = 0; i < sizeof b; ++i) b[i] = (unsigned char)(i * 7);
    int err;

    CHECK(save_atomic(g_dir, "target.bin", a, sizeof a) == SAVE_OK && classify(NULL, 0, a, sizeof a) == 2, "initial create");
    CHECK(save_atomic(g_dir, "target.bin", b, sizeof b) == SAVE_OK && classify(a, sizeof a, b, sizeof b) == 2, "replace");
    CHECK(save_atomic(g_dir, "target.bin", NULL, 0) == SAVE_OK && classify(a, sizeof a, b, 0) == 2, "empty payload");
    unsigned char *big = malloc(SAVE_MAX_PAYLOAD + 1);
    for (size_t i = 0; i <= SAVE_MAX_PAYLOAD; ++i) big[i] = (unsigned char)(i * 31 + (i >> 11));
    CHECK(save_atomic(g_dir, "target.bin", big, SAVE_MAX_PAYLOAD) == SAVE_OK && classify(NULL, 0, big, SAVE_MAX_PAYLOAD) == 2, "16 MiB");
    CHECK(save_atomic_err(g_dir, "target.bin", big, SAVE_MAX_PAYLOAD + 1, &err) == SAVE_ERROR_BEFORE_RENAME && err == EFBIG, "16 MiB + 1");
    CHECK(save_atomic_err(g_dir, "..", a, 1, &err) == SAVE_ERROR_BEFORE_RENAME && err == EINVAL, "reject ..");
    CHECK(save_atomic_err(g_dir, "x/y", a, 1, &err) == SAVE_ERROR_BEFORE_RENAME && err == EINVAL, "reject separator");
    /* symlink and directory targets are refused */
    char p1[700]; snprintf(p1, sizeof p1, "%s/link.bin", g_dir);
    CHECK(symlink("target.bin", p1) == 0, "make symlink");
    CHECK(save_atomic_err(g_dir, "link.bin", a, 10, &err) == SAVE_ERROR_BEFORE_RENAME && err == ELOOP, "symlink target refused");
    char p2[700]; snprintf(p2, sizeof p2, "%s/sub", g_dir);
    CHECK(mkdir(p2, 0700) == 0, "make dir");
    CHECK(save_atomic_err(g_dir, "sub", a, 10, &err) == SAVE_ERROR_BEFORE_RENAME && err == EISDIR, "directory target refused");
    CHECK(save_atomic_err("/nonexistent-q06-dir", "t.bin", a, 10, &err) == SAVE_ERROR_BEFORE_RENAME && err == ENOENT, "missing directory");
    CHECK(count_temps() == 0, "no temp files left");

    /* crash test: reference count of system calls, then SIGKILL before each one */
    CHECK(save_atomic(g_dir, "target.bin", a, sizeof a) == SAVE_OK, "reset to a");
    g_calls = 0; g_kill_at = 0;
    CHECK(save_atomic_ops(&kill_ops, g_dir, "target.bin", b, sizeof b, &err) == SAVE_OK, "reference run");
    long n = g_calls;
    CHECK(save_atomic(g_dir, "target.bin", a, sizeof a) == SAVE_OK, "reset to a");
    long ok_old = 0, ok_new = 0;
    for (long k = 1; k <= n; ++k) {
        pid_t pid = fork();
        if (pid == 0) { g_calls = 0; g_kill_at = k; (void)save_atomic_ops(&kill_ops, g_dir, "target.bin", b, sizeof b, &err); _exit(0); }
        int st = 0;
        waitpid(pid, &st, 0);
        int c = classify(a, sizeof a, b, sizeof b);
        CHECK(WIFSIGNALED(st) && WTERMSIG(st) == SIGKILL, "child %ld killed", k);
        CHECK(c == 1 || c == 2, "after SIGKILL before call %ld target is old or new complete (got %d)", k, c);
        if (c == 1) ++ok_old; else if (c == 2) ++ok_new;
        if (c == 2) CHECK(save_atomic(g_dir, "target.bin", a, sizeof a) == SAVE_OK, "reset");
    }
    printf("crash points: %ld (old observed %ld, new observed %ld); stale temp files from killed children: %d\n", n, ok_old, ok_new, count_temps());
    free(big);
    printf("RESULT: %d checks passed, %d failed (directory %s left for inspection)\n", g_pass, g_fail, g_dir);
    return g_fail ? 1 : 0;
}
```

#### 5. 検証結果（期待値と実測値を区別）

模擬ファイルシステムのテストは4構成とも `RESULT: 143 checks passed, 0 failed`、終了コード 0 でした。この模擬ファイルシステムは、揮発状態（強制終了したプロセスの後に見える内容）と永続状態（停電後に残る内容）を分けて保持します。ファイルのデータは fsync(ファイル) で、ディレクトリのエントリーは fsync(ディレクトリ) で初めて永続化されます。下表の「実測結果」は、特に断りがない限り、この模擬ファイルシステム上の実測値です。

| テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 初回作成（T1） | SAVE_OK。新ファイルが見えて永続化済み。一時ファイルと開いた fd が残らない。fsync(temp)・rename・fsync(dir) を各1回 | 一致 | 実施（模擬FS・4構成）／実POSIX：未実施 |
| 既存置換（T2） | SAVE_OK。新版に置き換わり永続化済み | 一致 | 同上 |
| 空payload（T3） | 13バイトのファイル、CRC 正常 | 一致 | 同上 |
| 最大payload 16MiB（T4） | 3MiB 単位の short write を含めて SAVE_OK、CRC 検証も合格 | 一致 | 同上 |
| 16MiB+1（T4） | SAVE_ERROR_BEFORE_RENAME / EFBIG。システムコール0回、旧版のまま | 一致 | 同上 |
| short write（T5） | 1回の write で1バイトしか書けなくても完成する（write 790回） | 一致 | 同上 |
| EINTR（T5） | write の2回に1回と fsync の1回が EINTR でも再試行して完成 | 一致 | 同上 |
| ディスク満杯相当（T6） | 300バイトで ENOSPC → BEFORE_RENAME / ENOSPC。旧版は揮発・永続とも無傷。一時ファイルは削除、fd は閉じる | 一致 | 同上 |
| temp fsync 失敗（T6） | BEFORE_RENAME / EIO。旧版無傷、一時ファイル削除 | 一致 | 同上 |
| rename 失敗（T6） | BEFORE_RENAME / EIO。旧版無傷、一時ファイル削除 | 一致 | 同上 |
| temp close 失敗、ディレクトリを開けない（T6） | BEFORE_RENAME。旧版無傷 | 一致 | 同上 |
| シンボリックリンク・ディレクトリの対象（T6） | ELOOP / EISDIR で拒否 | 一致 | 同上 |
| O_EXCL の衝突（T6・T7） | 3回衝突なら4回目の名前で成功。40回衝突なら EEXIST で BEFORE_RENAME | 一致 | 同上 |
| directory fsync 失敗（T8） | SAVE_COMMIT_UNCERTAIN / EIO（成功と偽らない）。新版は見えるが、ディレクトリのエントリーはまだ永続化されていない | 一致 | 同上 |
| basename の拒否（T9） | ""、"."、".."、"a/b"、"/abs"、"a\\b"、空白、非ASCII、":"、"*"、タブ、65バイト → EINVAL（システムコール0回）。"..."、".hidden"、64バイトなどは受理 | 一致 | 同上 |
| 成功ファイルの CRC 検証 | 独立したビット単位の CRC 実装で、マジック・バージョン・長さ・CRC を検証 | 全成功ケースで合格 | 同上 |
| 各段階でのプロセス強制終了・停電（T10） | 旧版の有無×short write の有無の4条件で、全システムコールの直前（11または23か所）と完了後にクラッシュさせる。①強制終了時の見え方、②停電後（永続化済みの名前空間）、③停電後（揮発の名前空間＋永続化済みのデータ）のどれでも、対象は「旧完全版（新規作成時は不在）」か「新完全版」 | 72か所すべて一致 | 同上 |
| 変異版（fsync(temp) の削除）での検出力 | 検査が失敗すること | T1・T2・T5・T10 が失敗（停電時の観測値 = 内容不明） | 実施 |
| 実POSIXでの初回作成・置換・空・16MiB・拒否・SIGKILL によるクラッシュ検査 | 旧完全版か新完全版 | — | **未実施**（Linux 環境なし。コンパイル・リンクのみ成功） |
| ASan / UBSan（模擬FSテスト） | 報告なし | 報告なし | 実施 |

**クラッシュ耐性の位置づけ：** 上記は、問題文が前提とする「同じディレクトリ内での原子的な rename と fsync の通常の永続化契約」を模擬したモデル上での検証です。実機の停電試験や実 Linux での実行はしていないため、実環境でのクラッシュ耐性を「保証済み」とはしません。

#### 6. 未実施項目・制約・残課題

- **未実施：** 実 Linux での実行（`q06_posix_test.c` の通常系、シンボリックリンクの拒否、fork と SIGKILL によるクラッシュ検査）、実ディスクの満杯や I/O エラー、実機の停電試験、GCC でのビルド。
- **制約：**
  - 強制終了したプロセスの一時ファイル（`.target.bin.tmp-*`）は自動では削除しません（対象ファイルは常に完全な版のままです）。
  - 新しい inode に置き換えるため、旧ファイルの所有者・権限・拡張属性は引き継ぎません（作成時のモードは 0644 に umask を適用）。
  - 同じ対象への外部からの同時書き換えは、問題文のとおり対象外です。対象の確認から rename までの間に差し替えられた場合も、rename はリンク先ではなくエントリー自体を置き換えるため、リンク先のファイルを書き換えることはありません。
  - close が EINTR を返した場合は、Linux の仕様（fd は解放済み）に従って成功扱いで続行します。それ以外の close エラーは rename 前の失敗として扱います。
  - fsync が EIO を返した後は再試行しません（再試行すると、失われたデータを成功と誤認するおそれがあるため）。
  - NFS など、rename や fsync の意味が異なるファイルシステムは考慮していません。

---

## Q07【地獄級】10万頂点のDAGを再帰なしで検証し、辞書順最小のトポロジカル順序を得る

**狙い：** 巨大データ、再帰スタック枯渇、循環判定、決定的結果、失敗時の出力維持、複雑度。

### 元コード（不十分）

~~~c
#include <stdint.h>
#include <stddef.h>
void topo_bad(uint32_t v, size_t e,
              const uint32_t *src, const uint32_t *dst,
              uint32_t *out) {
    uint32_t indeg[100000] = {0};
    for (size_t i = 0; i < e; ++i) ++indeg[dst[i]];
    size_t k = 0;
    for (uint32_t x = 0; x < v; ++x)
        if (!indeg[x]) out[k++] = x;
}
~~~

### 要求仕様

- APIは **topo_order(V, E, src, dst, out, out_capacity)** を設計してよい。Vは1〜100000、Eは0〜400000、頂点IDは0〜V-1。自己ループ、平行辺を含み得る。
- 閉路がなければ全頂点を**辞書順で最小のトポロジカル順序**に並べる。各段階で現在入次数0の最小IDを選ぶという定義とする。
- 閉路があればCYCLEを返す。入力不正ならINVALID、メモリ不足ならNOMEMを返す。失敗時は呼び出し元のout配列を**1要素も変更しない**。out_capacity<VもINVALID。
- 再帰を禁止。入力配列は読み取り専用。重複辺は独立した辺として扱う。O((V+E) log V)時間以内、O(V+E)追加記憶以内で実装する（より良い計算量は可）。
- 内部メモリ確保やサイズ計算でオーバーフローを検査し、エラー時にリークさせない。

### 必須検証

空辺集合、鎖、分岐と同時に選べる頂点、自己ループ、2頂点閉路、長い閉路、平行辺、入力順をシャッフルしても同じ結果、V=100000/E=400000、メモリ確保失敗注入、出力未変更、提示された計算量と測定した実行規模。

### ★★FREYJAの回答【Q07・記入欄】

> **ここから下が回答用スペースです。** GitHubの鉛筆アイコンからファイルを編集し、各「（ここに記入）」やコード枠内の例示文を自分の回答に置き換えてください。問題文・要求仕様・元コードは変更しないこと。

#### 1. 根本原因・危険性・影響

**元コード `topo_bad` の問題点（静的解析）**

| # | 箇所 | 根本原因 | 影響 |
| --- | --- | --- | --- |
| 1 | `uint32_t indeg[100000]` | 400KB の配列を自動記憶域（スタック）に置いている | 既定のスタックが1MB程度の環境では、呼び出しの深さ次第でスタックあふれ |
| 2 | `++indeg[dst[i]]` | 頂点IDの範囲を検査していない | dst[i] ≥ 100000 で境界外書き込み（スタック破壊） |
| 3 | V の検査なし | V > 100000 や V = 0 | 境界外アクセス、意味のない出力 |
| 4 | アルゴリズム | 最初に入次数0の頂点を並べるだけで、辺を消費しない | トポロジカル順序にならない（大半の頂点が出力されない） |
| 5 | 閉路判定なし | 戻り値がない | 閉路を検出できず、呼び出し元は結果が正しいかを判断できない |
| 6 | `out[k++]` | 出力の容量を受け取らない | out の境界外書き込み。失敗時にも部分的に書き込む |
| 7 | src を使っていない | 隣接関係を持たない | 辞書順最小の条件も満たせない |

**修正方針：** 入力をすべて検証します（V は 1..100000、E ≤ 400000、ID < V、ポインター、out_capacity ≥ V）。辺は始点ごとの計数ソートで CSR 形式（隣接配列）に変換し、Kahn 法を最小ヒープで実行します。各段階で「入次数0の頂点のうち最小のID」を取り出すので、問題文の定義どおり辞書順最小になります。再帰は使いません。自己ループや閉路に含まれる頂点は入次数が0にならないため、取り出せた数が V 未満なら TOPO_CYCLE です。平行辺は入次数に重複して数え、独立した辺として扱います。結果は内部バッファに作り、成功したときだけ `out` へコピーします（失敗時は1要素も変更しません）。メモリは V+1、E、V、V、V 個の uint32_t の5配列で O(V+E)、時間は O((V+E) log V) です。サイズの計算は掛け算の前に桁あふれを検査し、確保に失敗した場合は確保済みのものをすべて解放して TOPO_NOMEM を返します。

API：`topo_status topo_order(uint32_t V, size_t E, const uint32_t *src, const uint32_t *dst, uint32_t *out, size_t out_capacity)`。戻り値は `TOPO_OK / TOPO_CYCLE / TOPO_INVALID / TOPO_NOMEM` です。確保失敗の注入用に `topo_order_ex`（アロケーター指定版）も用意しました。

#### 2. 完全な修正版Cコード

```c
/*
 * Q07: lexicographically smallest topological order without recursion (C17)
 *
 * Kahn's algorithm with a binary min-heap of the vertices whose in-degree is 0:
 * at each step the smallest such ID is emitted (the required definition).
 * Adjacency is stored in CSR form (counting sort of the edges by source).
 * Time O((V + E) log V), extra memory O(V + E): five arrays of V+1, V, V, V and
 * E uint32_t. Self loops and cycles leave vertices with in-degree > 0 and are
 * reported as TOPO_CYCLE. `out` is written only after complete success.
 *
 * Original implementation by ★★FREYJA for the C hell-level exam.
 */
#include <stddef.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

/* ------------------------------------------------------------------ API */

#define TOPO_MAX_V 100000u
#define TOPO_MAX_E 400000u

typedef enum { TOPO_OK = 0, TOPO_CYCLE, TOPO_INVALID, TOPO_NOMEM } topo_status;

typedef struct {
    void *(*alloc)(void *ctx, size_t n);
    void (*release)(void *ctx, void *p);
    void *ctx;
} topo_allocator;

topo_status topo_order(uint32_t V, size_t E, const uint32_t *src, const uint32_t *dst,
                       uint32_t *out, size_t out_capacity);
topo_status topo_order_ex(uint32_t V, size_t E, const uint32_t *src, const uint32_t *dst,
                          uint32_t *out, size_t out_capacity, const topo_allocator *al);

/* ------------------------------------------------------- implementation */

static void *topo_malloc(void *ctx, size_t n) { (void)ctx; return malloc(n); }
static void topo_free(void *ctx, void *p) { (void)ctx; free(p); }

/* n * sizeof(uint32_t) with overflow check; 0 elements -> 1 byte (never NULL on success) */
static int arr_bytes(size_t n, size_t *bytes)
{
    if (n > SIZE_MAX / sizeof(uint32_t))
        return 0;
    *bytes = n ? n * sizeof(uint32_t) : 1u;
    return 1;
}

static void heap_push(uint32_t *h, uint32_t *n, uint32_t v)
{
    uint32_t i = (*n)++;
    while (i > 0) {
        uint32_t p = (i - 1u) / 2u;
        if (h[p] <= v)
            break;
        h[i] = h[p];
        i = p;
    }
    h[i] = v;
}

static uint32_t heap_pop(uint32_t *h, uint32_t *n)
{
    uint32_t top = h[0];
    uint32_t last = h[--(*n)];
    uint32_t i = 0, cnt = *n;
    for (;;) {
        uint32_t l = 2u * i + 1u;            /* i < cnt <= 1e5: no overflow */
        if (l >= cnt)
            break;
        uint32_t c = (l + 1u < cnt && h[l + 1u] < h[l]) ? l + 1u : l;
        if (h[c] >= last)
            break;
        h[i] = h[c];
        i = c;
    }
    if (cnt > 0)
        h[i] = last;
    return top;
}

topo_status topo_order_ex(uint32_t V, size_t E, const uint32_t *src, const uint32_t *dst,
                          uint32_t *out, size_t out_capacity, const topo_allocator *al)
{
    topo_allocator def = { topo_malloc, topo_free, NULL };
    if (al == NULL)
        al = &def;
    if (al->alloc == NULL || al->release == NULL)
        return TOPO_INVALID;
    if (V < 1u || V > TOPO_MAX_V || E > TOPO_MAX_E || out == NULL || out_capacity < (size_t)V)
        return TOPO_INVALID;
    if (E > 0 && (src == NULL || dst == NULL))
        return TOPO_INVALID;
    for (size_t i = 0; i < E; ++i)
        if (src[i] >= V || dst[i] >= V)
            return TOPO_INVALID;

    size_t b_off, b_v, b_e;
    if (!arr_bytes((size_t)V + 1u, &b_off) || !arr_bytes((size_t)V, &b_v) || !arr_bytes(E, &b_e))
        return TOPO_NOMEM;

    topo_status st = TOPO_NOMEM;
    uint32_t *off = (uint32_t *)al->alloc(al->ctx, b_off);   /* CSR row starts (then cursors) */
    uint32_t *adj = (uint32_t *)al->alloc(al->ctx, b_e);     /* targets grouped by source     */
    uint32_t *indeg = (uint32_t *)al->alloc(al->ctx, b_v);
    uint32_t *heap = (uint32_t *)al->alloc(al->ctx, b_v);
    uint32_t *res = (uint32_t *)al->alloc(al->ctx, b_v);     /* private result buffer         */
    if (!off || !adj || !indeg || !heap || !res)
        goto done;

    /* counts; E <= 4e5 so every count fits in uint32_t */
    memset(off, 0, b_off);
    memset(indeg, 0, b_v);
    for (size_t i = 0; i < E; ++i) {
        off[src[i] + 1u]++;
        indeg[dst[i]]++;
    }
    for (uint32_t v = 0; v < V; ++v)
        off[v + 1u] += off[v];
    /* scatter (stable: keeps input order inside a row; order does not affect the result) */
    uint32_t *cur = heap;                   /* reuse heap as fill cursor before the main loop */
    memcpy(cur, off, b_v);
    for (size_t i = 0; i < E; ++i)
        adj[cur[src[i]]++] = dst[i];

    uint32_t hn = 0, k = 0;
    for (uint32_t v = 0; v < V; ++v)
        if (indeg[v] == 0)
            heap_push(heap, &hn, v);        /* pushes in increasing order: O(1) each */
    /* note: heap_push above overwrites cur[], which is no longer needed */
    while (hn > 0) {
        uint32_t u = heap_pop(heap, &hn);
        res[k++] = u;
        for (uint32_t j = off[u]; j < off[u + 1u]; ++j) {
            uint32_t w = adj[j];
            if (--indeg[w] == 0)
                heap_push(heap, &hn, w);
        }
    }
    if (k != V) {
        st = TOPO_CYCLE;                    /* remaining vertices lie on or behind a cycle */
        goto done;
    }
    memcpy(out, res, b_v);                  /* the caller's array is touched only here */
    st = TOPO_OK;

done:
    if (res) al->release(al->ctx, res);
    if (heap) al->release(al->ctx, heap);
    if (indeg) al->release(al->ctx, indeg);
    if (adj) al->release(al->ctx, adj);
    if (off) al->release(al->ctx, off);
    return st;
}

topo_status topo_order(uint32_t V, size_t E, const uint32_t *src, const uint32_t *dst,
                       uint32_t *out, size_t out_capacity)
{
    return topo_order_ex(V, E, src, dst, out, out_capacity, NULL);
}
```

#### 3. ビルド方法・使用環境

```sh
# PowerShell 7 で MSVC 開発環境（vcvars64.bat）を読み込んだ後、build.ps1 が次の4構成を順にビルド・実行する
# q07_test.c は #include "q07.c" により実装と同じ翻訳単位でビルドする
W='-Wall -Wextra -Wpedantic -Wshadow -Wconversion -Wsign-conversion -Wcast-qual -Wstrict-prototypes -Wmissing-prototypes -Wvla -Wformat=2'
# (1) Clang -O2
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O2 -fuse-ld=link q07_test.c -o q07_rel.exe
# (2) Clang + AddressSanitizer（MSVC 同梱の ASan ランタイムでリンク）
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O1 -g -fsanitize=address -fno-omit-frame-pointer -fms-runtime-lib=dll -c q07_test.c -o q07_asan.obj
link /nologo /DEBUG /OUT:q07_asan.exe q07_asan.obj clang_rt.asan_dynamic-x86_64.lib clang_rt.asan_dynamic_runtime_thunk-x86_64.lib msvcrt.lib ucrt.lib vcruntime.lib legacy_stdio_definitions.lib
# (3) Clang + UndefinedBehaviorSanitizer（トラップ方式）
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O1 -g -fsanitize=undefined -fsanitize-trap=undefined -fno-sanitize-recover=all -fuse-ld=link q07_test.c -o q07_ubsan.exe
# (4) MSVC（2つ目のコンパイラ）
cl /nologo /std:c17 /W4 /experimental:c11atomics /utf-8 /O2 /D_CRT_SECURE_NO_WARNINGS q07_test.c /Fe:q07_msvc.exe
# 各 .exe を実行し、終了コード 0 と "RESULT: N checks passed, 0 failed" を確認
```

**使用環境（全問共通）**

- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド、x86-64）
- コンパイラ1：**Clang 18.0.3**（Android NDK r27c に同梱。ターゲットは `x86_64-pc-windows-msvc`。ヘッダー・ライブラリは MSVC 14.52.37025 と Windows SDK/UCRT 10.0.26100、リンカーは link.exe）。`-std=c17` を指定。
- コンパイラ2：**MSVC cl 19.52.37029**（Visual Studio 18 Community）、`/std:c17 /W4`。
- GCC：この PC にはなく、使用していません。Linux／WSL の環境もありません（外部パッケージの無断導入禁止のため、導入していません）。
- AddressSanitizer：Clang でインストルメントし、MSVC 同梱の ASan ランタイム（`clang_rt.asan_dynamic-x86_64`）でリンクしました。ヒープ境界外書き込みの陽性対照で、`heap-buffer-overflow` を検出することを確認済みです。
- UndefinedBehaviorSanitizer：`-fsanitize=undefined -fsanitize-trap=undefined`（トラップ方式。ランタイムを使わず、未定義動作で不正命令の例外として異常終了します）。符号付きオーバーフローの陽性対照で、異常終了することを確認済みです。
- LeakSanitizer：Windows 版 ASan にないため未実施です。リークは各問の計数アロケーター（確保と解放の数の差）で検査しました。ThreadSanitizer：Windows では使えないため未実施です。
- ビルド補助 `shim/stdatomic.h`（製品コード外）：NDK の Clang に付属する `stdatomic.h` は Android（bionic）専用で Windows ではコンパイルできないため、MSVC の C11 実装 `vcruntime_c11_stdatomic.h`（Clang 互換の `__c11_atomic` 組み込み関数を使用）を読み込む1ファイルを置きました。Clang 18 が `__typeof_unqual__` に対応していないため、`atomic_is_lock_free` だけは `__c11_atomic_is_lock_free` で定義し直しています。
- `-D_CRT_SECURE_NO_WARNINGS`：MSVC の CRT が出す「`fopen` などの代わりに `_s` 版を使うように」という非推奨の通知だけを止める定義です（C17 標準関数の使用に対する通知であり、コードの欠陥を隠すものではありません）。

- Q07 の結果：4構成とも警告0件・ビルド成功。

#### 4. テストコード

```c
/* Q07 tests */
#include "q07.c"
#include <stdio.h>
#include <time.h>

static int g_fail = 0, g_pass = 0;
#define CHECK(cond, ...) do { if (cond) { ++g_pass; } else { ++g_fail; \
    printf("  FAIL %s:%d: ", __FILE__, __LINE__); printf(__VA_ARGS__); printf("\n"); } } while (0)
static void report(const char *name, int before_fail)
{
    printf("[%s] %s\n", g_fail == before_fail ? "PASS" : "FAIL", name);
}

typedef struct { long outstanding, total, fail_at; } acct;   /* fail_at: 1-based allocation index that fails */
static void *a_alloc(void *c, size_t n) { acct *a = (acct *)c; a->total++; if (a->fail_at && a->total == a->fail_at) return NULL; void *p = malloc(n); if (p) a->outstanding++; return p; }
static void a_free(void *c, void *p) { acct *a = (acct *)c; if (p) { a->outstanding--; free(p); } }

static uint64_t rs = 0x2545F4914F6CDD1Du;
static uint64_t rnd(void) { rs ^= rs << 13; rs ^= rs >> 7; rs ^= rs << 17; return rs; }

#define SENT 0xA5A5A5A5u
static void fill_sent(uint32_t *o, size_t n) { for (size_t i = 0; i < n; ++i) o[i] = SENT; }
static int all_sent(const uint32_t *o, size_t n) { for (size_t i = 0; i < n; ++i) if (o[i] != SENT) return 0; return 1; }

/* naive reference: repeatedly take the smallest vertex with no remaining incoming edge */
static topo_status ref_topo(uint32_t V, size_t E, const uint32_t *s, const uint32_t *d, uint32_t *out)
{
    static uint32_t indeg[64]; static unsigned char done[64];
    memset(indeg, 0, sizeof indeg); memset(done, 0, sizeof done);
    for (size_t i = 0; i < E; ++i) indeg[d[i]]++;
    for (uint32_t k = 0; k < V; ++k) {
        uint32_t pick = V;
        for (uint32_t v = 0; v < V; ++v) if (!done[v] && indeg[v] == 0) { pick = v; break; }
        if (pick == V) return TOPO_CYCLE;
        done[pick] = 1; out[k] = pick;
        for (size_t i = 0; i < E; ++i) if (s[i] == pick) indeg[d[i]]--;
    }
    return TOPO_OK;
}

/* check that `o` is a permutation and a valid topological order */
static int is_topo(uint32_t V, size_t E, const uint32_t *s, const uint32_t *d, const uint32_t *o, uint32_t *pos)
{
    for (uint32_t v = 0; v < V; ++v) pos[v] = UINT32_MAX;
    for (uint32_t i = 0; i < V; ++i) { if (o[i] >= V || pos[o[i]] != UINT32_MAX) return 0; pos[o[i]] = i; }
    for (size_t i = 0; i < E; ++i) if (pos[s[i]] >= pos[d[i]]) return 0;
    return 1;
}

/* independent large-graph reference: bitset of available vertices, always scan
 * from vertex 0 for the first available one (O(V^2/64 + E)), adjacency by
 * linked lists built from the edge arrays. */
static topo_status ref_big(uint32_t V, size_t E, const uint32_t *s, const uint32_t *d, uint32_t *out)
{
    uint64_t *bits = (uint64_t *)calloc((V + 63) / 64, sizeof(uint64_t));
    uint32_t *ind = (uint32_t *)calloc(V, sizeof(uint32_t));
    int32_t *head = (int32_t *)malloc(V * sizeof(int32_t)), *next = (int32_t *)malloc((E ? E : 1) * sizeof(int32_t));
    for (uint32_t v = 0; v < V; ++v) head[v] = -1;
    for (size_t i = 0; i < E; ++i) { ind[d[i]]++; next[i] = head[s[i]]; head[s[i]] = (int32_t)i; }
    for (uint32_t v = 0; v < V; ++v) if (!ind[v]) bits[v / 64] |= 1ull << (v % 64);
    topo_status st = TOPO_OK;
    for (uint32_t k = 0; k < V; ++k) {
        uint32_t w = 0, nw = (V + 63) / 64;
        while (w < nw && bits[w] == 0) ++w;
        if (w == nw) { st = TOPO_CYCLE; break; }
        uint32_t b = 0; while (!((bits[w] >> b) & 1u)) ++b;
        uint32_t u = w * 64 + b;
        bits[w] &= ~(1ull << b);
        out[k] = u;
        for (int32_t e = head[u]; e >= 0; e = next[e]) if (--ind[d[e]] == 0) bits[d[e] / 64] |= 1ull << (d[e] % 64);
    }
    free(bits); free(ind); free(head); free(next);
    return st;
}

static double now(void) { struct timespec t; timespec_get(&t, TIME_UTC); return (double)t.tv_sec + (double)t.tv_nsec / 1e9; }

int main(void)
{
    int f0;
    static uint32_t out[TOPO_MAX_V + 8], pos[TOPO_MAX_V], s[TOPO_MAX_E + 8], d[TOPO_MAX_E + 8], s2[TOPO_MAX_E], d2[TOPO_MAX_E], out2[TOPO_MAX_V];
    acct A = { 0, 0, 0 };
    topo_allocator AL = { a_alloc, a_free, &A };

    /* T1: no edges -> 0,1,2,... */
    f0 = g_fail;
    fill_sent(out, 10);
    CHECK(topo_order_ex(5, 0, NULL, NULL, out, 5, &AL) == TOPO_OK, "E=0 ok (src/dst may be NULL)");
    CHECK(out[0] == 0 && out[1] == 1 && out[2] == 2 && out[3] == 3 && out[4] == 4 && out[5] == SENT, "identity, nothing beyond V written");
    fill_sent(out, 2);
    CHECK(topo_order_ex(1, 0, NULL, NULL, out, 1, &AL) == TOPO_OK && out[0] == 0 && out[1] == SENT, "V=1");
    report("T1 empty edge set", f0);

    /* T2: chain and reversed chain, branching with simultaneous choices */
    f0 = g_fail;
    {
        uint32_t cs[4] = { 3, 2, 1, 0 }, cd[4] = { 2, 1, 0, 4 };       /* 3->2->1->0->4 */
        CHECK(topo_order_ex(5, 4, cs, cd, out, 5, &AL) == TOPO_OK && out[0] == 3 && out[1] == 2 && out[2] == 1 && out[3] == 0 && out[4] == 4, "chain 3,2,1,0,4");
        /* 5->0, 4->0, 2->1 : available initially {2,3,4,5}; smallest-first gives 2,1,3,4,5,0 */
        uint32_t bs[3] = { 5, 4, 2 }, bd[3] = { 0, 0, 1 };
        CHECK(topo_order_ex(6, 3, bs, bd, out, 6, &AL) == TOPO_OK, "branch ok");
        CHECK(out[0] == 2 && out[1] == 1 && out[2] == 3 && out[3] == 4 && out[4] == 5 && out[5] == 0, "lexicographically smallest: 2 1 3 4 5 0");
        uint32_t ref[6]; CHECK(ref_topo(6, 3, bs, bd, ref) == TOPO_OK && memcmp(ref, out, sizeof ref) == 0, "matches naive reference");
    }
    report("T2 chain / branches / simultaneously available vertices", f0);

    /* T3: self loop, 2-cycle, cycle behind DAG part; out untouched */
    f0 = g_fail;
    {
        uint32_t a[1] = { 2 }, b[1] = { 2 };
        fill_sent(out, 8);
        CHECK(topo_order_ex(4, 1, a, b, out, 4, &AL) == TOPO_CYCLE && all_sent(out, 8), "self loop -> CYCLE, out unchanged");
        uint32_t c1[2] = { 0, 1 }, c2[2] = { 1, 0 };
        CHECK(topo_order_ex(3, 2, c1, c2, out, 3, &AL) == TOPO_CYCLE && all_sent(out, 8), "2-cycle");
        uint32_t e1[4] = { 0, 1, 2, 3 }, e2[4] = { 1, 2, 3, 2 };     /* 0->1->2<->3 */
        CHECK(topo_order_ex(4, 4, e1, e2, out, 4, &AL) == TOPO_CYCLE && all_sent(out, 8), "cycle reachable from DAG part");
        CHECK(A.outstanding == 0, "no leak");
    }
    report("T3 self loop / 2-vertex cycle -> CYCLE with output unchanged", f0);

    /* T4: parallel edges counted independently */
    f0 = g_fail;
    {
        uint32_t ps[5] = { 1, 1, 1, 2, 2 }, pd[5] = { 0, 0, 0, 0, 1 };
        CHECK(topo_order_ex(3, 5, ps, pd, out, 3, &AL) == TOPO_OK && out[0] == 2 && out[1] == 1 && out[2] == 0, "parallel edges: 2 1 0");
        uint32_t qs[3] = { 0, 0, 1 }, qd[3] = { 1, 1, 0 };
        CHECK(topo_order_ex(2, 3, qs, qd, out, 2, &AL) == TOPO_CYCLE, "parallel + back edge -> CYCLE");
    }
    report("T4 parallel edges", f0);

    /* T5: invalid input, out untouched */
    f0 = g_fail;
    {
        uint32_t a[2] = { 0, 5 }, b[2] = { 1, 1 };
        fill_sent(out, 8);
        CHECK(topo_order_ex(0, 0, NULL, NULL, out, 8, &AL) == TOPO_INVALID, "V=0");
        CHECK(topo_order_ex(TOPO_MAX_V + 1u, 0, NULL, NULL, out, TOPO_MAX_V + 8, &AL) == TOPO_INVALID, "V=100001");
        CHECK(topo_order_ex(3, TOPO_MAX_E + 1u, s, d, out, 8, &AL) == TOPO_INVALID, "E=400001");
        CHECK(topo_order_ex(3, 2, a, b, out, 8, &AL) == TOPO_INVALID, "vertex id >= V");
        CHECK(topo_order_ex(3, 2, NULL, b, out, 8, &AL) == TOPO_INVALID, "src NULL");
        CHECK(topo_order_ex(6, 2, a, b, NULL, 8, &AL) == TOPO_INVALID, "out NULL");
        CHECK(topo_order_ex(6, 2, a, b, out, 5, &AL) == TOPO_INVALID, "out_capacity < V");
        CHECK(all_sent(out, 8) && A.outstanding == 0, "out untouched, no allocation leak");
    }
    report("T5 invalid arguments -> INVALID, out untouched", f0);

    /* T6: allocation failure at each of the 5 allocations */
    f0 = g_fail;
    {
        uint32_t bs[3] = { 5, 4, 2 }, bd[3] = { 0, 0, 1 };
        for (long k = 1; k <= 5; ++k) {
            acct B = { 0, 0, k }; topo_allocator BL = { a_alloc, a_free, &B };
            fill_sent(out, 8);
            CHECK(topo_order_ex(6, 3, bs, bd, out, 6, &BL) == TOPO_NOMEM && all_sent(out, 8) && B.outstanding == 0, "allocation %ld fails -> NOMEM, out unchanged, no leak", k);
        }
        acct B = { 0, 0, 6 }; topo_allocator BL = { a_alloc, a_free, &B };
        CHECK(topo_order_ex(6, 3, bs, bd, out, 6, &BL) == TOPO_OK && B.outstanding == 0 && B.total == 5, "exactly 5 allocations, all released");
    }
    report("T6 allocation failure injection", f0);

    /* T7: 20000 random small graphs vs naive reference (with and without cycles) */
    f0 = g_fail;
    {
        long bad = 0, cyc = 0;
        for (int t = 0; t < 20000; ++t) {
            uint32_t V = 1 + (uint32_t)(rnd() % 12);
            size_t E = (size_t)(rnd() % 30);
            int dag = (rnd() % 3) != 0;
            for (size_t i = 0; i < E; ++i) {
                uint32_t x = (uint32_t)(rnd() % V), y = (uint32_t)(rnd() % V);
                if (dag) {           /* orient by a hidden random-ish rank to keep it acyclic */
                    if (x == y) { y = (x + 1) % V; if (V == 1) { E = i; break; } }
                    uint32_t rx = (x * 7u + 3u) % 13u, ry = (y * 7u + 3u) % 13u;
                    if (rx > ry) { uint32_t tmp = x; x = y; y = tmp; }
                }
                s[i] = x; d[i] = y;
            }
            uint32_t ref[64];
            topo_status r1 = topo_order_ex(V, E, s, d, out, V, &AL);
            topo_status r2 = ref_topo(V, E, s, d, ref);
            if (r1 != r2 || (r1 == TOPO_OK && memcmp(out, ref, V * sizeof(uint32_t)) != 0)) ++bad;
            if (r2 == TOPO_CYCLE) ++cyc;
        }
        CHECK(bad == 0 && A.outstanding == 0, "%ld mismatches", bad);
        printf("  random graphs: 20000 (with cycle: %ld)\n", cyc);
    }
    report("T7 20000 random graphs vs naive reference", f0);

    /* T8: V=100000, E=400000 random DAG; shuffled input gives the same order */
    f0 = g_fail;
    {
        uint32_t V = TOPO_MAX_V; size_t E = TOPO_MAX_E;
        static uint32_t rank[TOPO_MAX_V];
        for (uint32_t v = 0; v < V; ++v) rank[v] = v;
        for (uint32_t v = V - 1; v > 0; --v) { uint32_t j = (uint32_t)(rnd() % (v + 1)); uint32_t t = rank[v]; rank[v] = rank[j]; rank[j] = t; }
        for (size_t i = 0; i < E; ++i) {
            uint32_t x = (uint32_t)(rnd() % V), y = (uint32_t)(rnd() % V);
            if (x == y) y = (y + 1) % V;
            if (rank[x] > rank[y]) { uint32_t t = x; x = y; y = t; }
            s[i] = x; d[i] = y;
        }
        for (size_t i = 0; i < 1000; ++i) { s[E - 1 - i] = s[i]; d[E - 1 - i] = d[i]; }   /* parallel duplicates */
        double t0 = now();
        topo_status r = topo_order_ex(V, E, s, d, out, V, &AL);
        double t1 = now();
        CHECK(r == TOPO_OK && is_topo(V, E, s, d, out, pos), "valid topological order of 100000 vertices");
        /* lexicographic minimality certificate: at each step no smaller available vertex was skipped.
           Equivalent check: out == result of an independent run on a shuffled edge list. */
        memcpy(s2, s, E * sizeof(uint32_t)); memcpy(d2, d, E * sizeof(uint32_t));
        for (size_t i = E - 1; i > 0; --i) { size_t j = (size_t)(rnd() % (i + 1)); uint32_t a = s2[i], b = d2[i]; s2[i] = s2[j]; d2[i] = d2[j]; s2[j] = a; d2[j] = b; }
        CHECK(topo_order_ex(V, E, s2, d2, out2, V, &AL) == TOPO_OK && memcmp(out, out2, V * sizeof(uint32_t)) == 0, "shuffled edge order -> identical result");
        CHECK(ref_big(V, E, s, d, out2) == TOPO_OK && memcmp(out, out2, V * sizeof(uint32_t)) == 0, "equals independent bitset reference (lexicographic minimality)");
        printf("  V=100000 E=400000 random DAG: %.3f s\n", t1 - t0);
        /* scaling: time per (V+E) log2 V should stay roughly constant */
        uint32_t Vs[3] = { 25000, 50000, 100000 };
        for (int k = 0; k < 3; ++k) {
            uint32_t vv = Vs[k]; size_t ee = (size_t)vv * 4u;
            for (size_t i = 0; i < ee; ++i) { uint32_t x = (uint32_t)(rnd() % vv), y = (uint32_t)(rnd() % vv); if (x == y) y = (y + 1) % vv; if (x > y) { uint32_t t = x; x = y; y = t; } s2[i] = x; d2[i] = y; }
            double best = 1e9;
            for (int rep = 0; rep < 5; ++rep) { double a0 = now(); (void)topo_order_ex(vv, ee, s2, d2, out2, vv, &AL); double a1 = now(); if (a1 - a0 < best) best = a1 - a0; }
            double lg = 0; for (uint32_t x = vv; x > 1; x >>= 1) lg += 1;
            printf("  scaling V=%u E=%zu: best of 5 = %.4f s, ns per (V+E)log2V = %.2f\n", vv, ee, best, best * 1e9 / ((double)(vv + ee) * lg));
        }
        CHECK(A.outstanding == 0, "no leak");
    }
    report("T8 V=100000 / E=400000, shuffle invariance, measured scaling", f0);

    /* T9: deep structures that would overflow a recursive DFS */
    f0 = g_fail;
    {
        uint32_t V = TOPO_MAX_V;
        for (uint32_t i = 0; i + 1 < V; ++i) { s[i] = V - 1 - i; d[i] = V - 2 - i; }   /* 99999 -> 99998 -> ... -> 0 */
        CHECK(topo_order_ex(V, V - 1, s, d, out, V, &AL) == TOPO_OK, "chain of 100000");
        int okc = 1; for (uint32_t i = 0; i < V; ++i) if (out[i] != V - 1 - i) { okc = 0; break; }
        CHECK(okc, "chain order 99999..0");
        for (uint32_t i = 0; i < V; ++i) { s[i] = i; d[i] = (i + 1) % V; }               /* one cycle through all vertices */
        fill_sent(out, V);
        CHECK(topo_order_ex(V, V, s, d, out, V, &AL) == TOPO_CYCLE && all_sent(out, V), "cycle of length 100000 -> CYCLE, out unchanged");
        CHECK(A.outstanding == 0, "no leak");
    }
    report("T9 100000-long chain and 100000-long cycle without recursion", f0);

    printf("RESULT: %d checks passed, %d failed\n", g_pass, g_fail);
    return g_fail ? 1 : 0;
}
```

#### 5. 検証結果（期待値と実測値を区別）

4構成とも `RESULT: 36 checks passed, 0 failed`、終了コード 0 でした（大きな単位の検査をまとめて1件と数えています）。

| テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 空辺集合（T1） | 0,1,2,3,4。V を超える位置には書き込まない。V=1 | 一致 | 実施（4構成） |
| 鎖（T2） | 3→2→1→0→4 なら 3,2,1,0,4 | 一致 | 実施 |
| 分岐と同時に選べる頂点（T2） | 5→0、4→0、2→1 なら 2,1,3,4,5,0（辞書順最小）。素朴な参照実装とも一致 | 一致 | 実施 |
| 自己ループ（T3） | TOPO_CYCLE、out は不変 | 一致 | 実施 |
| 2頂点閉路、DAG 部分の先にある閉路（T3） | TOPO_CYCLE、out は不変 | 一致 | 実施 |
| 平行辺（T4） | 1→0 を3本、2→0、2→1 なら 2,1,0。平行辺＋逆辺は CYCLE | 一致 | 実施 |
| 入力不正（T5） | V=0、V=100001、E=400001、ID ≥ V、src=NULL、out=NULL、out_capacity < V → TOPO_INVALID。out は不変 | 一致 | 実施 |
| メモリ確保失敗の注入（T6） | 5か所の確保それぞれを失敗させると TOPO_NOMEM、out は不変、リーク0。成功時は確保5回・全解放 | 一致 | 実施 |
| ランダムな小グラフ2万個（T7） | V ≤ 12・E < 30（閉路あり5,831個を含む）で、O(V²) の素朴な参照実装と状態・順序が完全一致 | 不一致0 | 実施 |
| V=100000 / E=400000（T8） | 有効なトポロジカル順序（全辺の前後関係と順列であることを検証）。ビット集合を毎回先頭から走査する独立した参照実装（辞書順最小の直接計算）と一致 | 一致。0.020秒（-O2） | 実施 |
| 入力順のシャッフル（T8） | 辺の並びを乱数で入れ替えても結果が同一 | 同一 | 実施 |
| 計算量と実行規模の実測（T8） | (V+E)·log₂V あたりの時間がほぼ一定 | V=25k/E=100k：3.4ms（1.91ns）、50k/200k：7.7ms（2.05ns）、100k/400k：17.6ms（2.20ns） | 実施（-O2） |
| 再帰なし：長さ10万の鎖（T9） | 99999,99998,…,0（再帰の DFS なら深さ10万） | 一致 | 実施 |
| 長さ10万の閉路（T9） | TOPO_CYCLE、out は不変 | 一致 | 実施 |
| リーク検査 | 計数アロケーターの未解放数が0 | 0 | 実施 |
| ASan / UBSan | 報告なし | 報告なし | 実施 |
| GCC・Linux | — | 未実施 | 未実施 |

#### 6. 未実施項目・制約・残課題

- GCC・Linux での実行は未実施です。時間は Windows 上の1台の PC（i7-4710MQ）での実測で、計算量の主張は解析と規模を変えた実測によります。
- 大規模グラフで辞書順最小であることは、独立した参照実装との一致で確認しました（数学的な証明は、各段階で最小IDを選ぶという定義をそのまま実装したことによります）。
- 制約：追加メモリは最大で約 (4V + 4) + 4E + 12V バイト（V=1e5・E=4e5 で約3.2MB）です。入力配列は読むだけで変更しません。

---

## Q08【地獄級】Robin Hoodハッシュテーブルの衝突処理・後方詰め削除・障害時整合性

**狙い：** 衝突連鎖、探索停止条件、wrap-around、動的所有権、失敗時の強い安全性。

### 元コード（危険）

~~~c
#include <stdint.h>
#include <stddef.h>
#include <string.h>
#define CAP 1024u
typedef struct { const unsigned char *key; size_t n; uint64_t val; } Entry;
static Entry table[CAP];
size_t slot_bad(uint64_t hash) { return (size_t)(hash & CAP); }
int equal_bad(const Entry *e, const unsigned char *k, size_t n) {
    return memcmp(e->key, k, n) == 0;
}
~~~

### 要求仕様

- 容量1024固定（2のべき乗）・単一スレッドの **rh_init / rh_put / rh_get / rh_delete / rh_destroy / rh_validate** を設計。挿入した**キーのバイト列は自前で所有**し、呼び出し元のバッファの寿命に依存しない。
- キーは長さ0〜255の任意のバイト列で、NULを含んでもよい。識別にはキー長と全バイトを用いる。値はuint64_t。hashはFNV-1a 64-bit、ただしテスト時に衝突を強制するhash注入を許可。
- Robin Hood probingで距離に基づく挿入と検索を正しく行い、削除では**backward-shift deletion**を実装して墓石を使わない。満杯時の新規挿入は失敗、既存キーの値更新は許可。
- 新規挿入でメモリ確保に失敗した場合はテーブルの全状態を変えない。削除した所有メモリは適切に解放し、更新時にリークしない。失敗したgetは出力先を変更しない。
- キー長の違う同一プレフィックスを別キーとして扱う。ループ停止条件と距離のwrap-aroundを厳密に扱う。rh_validateでスロットとキーの整合性を確認。
- 非悪意的なhash分布で期待O(1)を目指すが、敵対的な全衝突では線形探索を許す。

### 必須検証

空キー、NULを含むキー、同じ接頭部、上書き、全衝突、テーブル満杯、削除後の後続探索、境界をまたぐ衝突列、100000回以上の決定的ランダムput/get/deleteと参照マップ比較、確保失敗注入、リーク検査。

### ★★FREYJAの回答【Q08・記入欄】

> **ここから下が回答用スペースです。** GitHubの鉛筆アイコンからファイルを編集し、各「（ここに記入）」やコード枠内の例示文を自分の回答に置き換えてください。問題文・要求仕様・元コードは変更しないこと。

#### 1. 根本原因・危険性・影響

**元コードの問題点（静的解析）**

| # | 箇所 | 根本原因 | 影響 |
| --- | --- | --- | --- |
| 1 | `hash & CAP` | マスクが `CAP − 1`（1023）ではなく 1024 | 結果が 0 か 1024 の2値しかない。1024 は `table[1024]` の境界外アクセスになり、分散もしない |
| 2 | `memcmp(e->key, k, n)` | 格納済みのキー長 `e->n` と比較していない | "ab" と "abc" を同一視する。短いほうのキーを越えて読む境界外読み取りもある |
| 3 | `memcmp` の NULL | 空きスロットの `key == NULL` を参照外しし得る | 未定義動作 |
| 4 | `const unsigned char *key` | 呼び出し元のバッファを指すだけで、コピーを持たない | 呼び出し元が解放・再利用すると、ダングリングポインターや内容の変化が起きる |
| 5 | 衝突処理なし | 探索・挿入・削除のロジックがない | 同じスロットへの上書きでデータを失う |
| 6 | 静的なグローバル表 | 状態が1つしかない | 複数の表を持てない。テストできない |

**修正方針：** 容量1024・マスク1023の Robin Hood 法です。各スロットに、所有するキーのコピー、長さ（0〜255）、64ビットのハッシュ値、値を持たせます。探索距離は `(i − (h & 1023)) & 1023` で、周回しても正しく求まります。探索は「空きスロット」「探索距離より起点に近い先客（Robin Hood の不変条件）」「1024回の探索」のいずれかで必ず止まります。挿入は、まず既存キーかを確かめます（更新ならメモリを確保せず値だけを変更）。新規なら、満杯の判定とキーのコピーの確保を**表に触れる前に**済ませます。確保に失敗したら NOMEM を返し、表は1バイトも変えません。その後、先客より自分の距離が大きければ入れ替えながら進めます。削除は後方シフト（backward-shift）で行い、後続の「起点から離れた」要素を1つずつ詰めるので、墓石は使いません。キーの一致は、ハッシュ・長さ・全バイトがすべて等しいことで判定します。空キーは確保なし（key = NULL、len = 0）で扱います。hash は既定が FNV-1a 64 で、テスト用に差し替えられます。

#### 2. 完全な修正版Cコード

```c
/*
 * Q08: fixed-capacity (1024) Robin Hood hash table with backward-shift deletion (C17)
 *
 * - keys: owned copies of 0..255 arbitrary bytes (NUL allowed); identity = length + bytes
 * - hash: FNV-1a 64-bit by default, injectable for tests
 * - probe distance of slot i holding hash h: (i - (h & MASK)) & MASK  (wrap-around safe)
 * - Robin Hood invariant: along a probe run, an entry never sits further from home
 *   than the entry before it plus one; lookup stops at an empty slot or when the
 *   resident is closer to its home than the probe distance travelled.
 * - deletion shifts the following run back by one slot (no tombstones).
 *
 * Original implementation by ★★FREYJA for the C hell-level exam.
 */
#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

/* ------------------------------------------------------------------ API */

#define RH_CAPACITY 1024u
#define RH_MASK     (RH_CAPACITY - 1u)
#define RH_MAX_KEY  255u

typedef enum { RH_OK = 0, RH_NOT_FOUND, RH_FULL, RH_NOMEM, RH_ERR_ARG, RH_CORRUPT } rh_status;

typedef uint64_t (*rh_hash_fn)(void *ctx, const unsigned char *key, size_t len);
typedef struct {
    void *(*alloc)(void *ctx, size_t n);
    void (*release)(void *ctx, void *p);
    void *ctx;
} rh_allocator;

typedef struct {
    unsigned char *key;   /* owned; NULL for the empty key (len 0) */
    uint64_t hash;
    uint64_t val;
    uint8_t  len;
    uint8_t  used;
} rh_slot;

typedef struct {
    rh_slot      slot[RH_CAPACITY];
    size_t       count;
    rh_hash_fn   hash;
    void        *hash_ctx;
    rh_allocator al;
} rh_table;

uint64_t  rh_fnv1a64(void *ctx, const unsigned char *key, size_t len);
rh_status rh_init(rh_table *t, rh_hash_fn hash, void *hash_ctx, const rh_allocator *al);  /* NULL -> defaults */
rh_status rh_put(rh_table *t, const void *key, size_t len, uint64_t val);
rh_status rh_get(const rh_table *t, const void *key, size_t len, uint64_t *out);      /* *out untouched on failure */
rh_status rh_delete(rh_table *t, const void *key, size_t len);
void      rh_destroy(rh_table *t);
rh_status rh_validate(const rh_table *t);

/* ------------------------------------------------------- implementation */

uint64_t rh_fnv1a64(void *ctx, const unsigned char *key, size_t len)
{
    (void)ctx;
    uint64_t h = 0xCBF29CE484222325u;
    for (size_t i = 0; i < len; ++i) {
        h ^= key[i];
        h *= 0x00000100000001B3u;          /* unsigned: wraps modulo 2^64 by definition */
    }
    return h;
}

static void *rh_malloc(void *ctx, size_t n) { (void)ctx; return malloc(n); }
static void rh_free(void *ctx, void *p) { (void)ctx; free(p); }

static size_t rh_dist(size_t i, uint64_t h) { return (i - (size_t)(h & RH_MASK)) & RH_MASK; }

rh_status rh_init(rh_table *t, rh_hash_fn hash, void *hash_ctx, const rh_allocator *al)
{
    if (t == NULL)
        return RH_ERR_ARG;
    if (al != NULL && (al->alloc == NULL || al->release == NULL))
        return RH_ERR_ARG;
    memset(t->slot, 0, sizeof t->slot);
    t->count = 0;
    t->hash = hash ? hash : rh_fnv1a64;
    t->hash_ctx = hash_ctx;
    if (al) {
        t->al = *al;
    } else {
        t->al.alloc = rh_malloc;
        t->al.release = rh_free;
        t->al.ctx = NULL;
    }
    return RH_OK;
}

static int key_eq(const rh_slot *s, uint64_t h, const unsigned char *k, size_t n)
{
    return s->hash == h && s->len == n && (n == 0 || memcmp(s->key, k, n) == 0);
}

/* Index of the key, or RH_CAPACITY if absent. At most RH_CAPACITY probes. */
static size_t rh_find(const rh_table *t, const unsigned char *k, size_t n, uint64_t h)
{
    size_t i = (size_t)(h & RH_MASK);
    for (size_t d = 0; d < RH_CAPACITY; ++d, i = (i + 1u) & RH_MASK) {
        const rh_slot *s = &t->slot[i];
        if (!s->used)
            return RH_CAPACITY;               /* empty slot ends every probe run   */
        if (rh_dist(i, s->hash) < d)
            return RH_CAPACITY;               /* resident is "richer": key absent  */
        if (key_eq(s, h, k, n))
            return i;
    }
    return RH_CAPACITY;                       /* full table scanned */
}

static int args_ok(const void *key, size_t len) { return len <= RH_MAX_KEY && (key != NULL || len == 0); }

rh_status rh_put(rh_table *t, const void *key, size_t len, uint64_t val)
{
    if (t == NULL || !args_ok(key, len))
        return RH_ERR_ARG;
    const unsigned char *k = (const unsigned char *)key;
    uint64_t h = t->hash(t->hash_ctx, k, len);
    size_t at = rh_find(t, k, len, h);
    if (at != RH_CAPACITY) {
        t->slot[at].val = val;                /* update: no allocation, nothing to leak */
        return RH_OK;
    }
    if (t->count >= RH_CAPACITY)
        return RH_FULL;
    unsigned char *copy = NULL;
    if (len > 0) {
        copy = (unsigned char *)t->al.alloc(t->al.ctx, len);
        if (copy == NULL)
            return RH_NOMEM;                  /* table not modified yet */
        memcpy(copy, k, len);
    }
    /* Robin Hood insertion: carry the poorer entry forward. An empty slot exists
     * (count < capacity), so this terminates within RH_CAPACITY steps. */
    rh_slot carry = { copy, h, val, (uint8_t)len, 1 };
    size_t i = (size_t)(h & RH_MASK), d = 0;
    for (size_t step = 0; step < RH_CAPACITY; ++step, i = (i + 1u) & RH_MASK, ++d) {
        rh_slot *s = &t->slot[i];
        if (!s->used) {
            *s = carry;
            t->count++;
            return RH_OK;
        }
        size_t sd = rh_dist(i, s->hash);
        if (sd < d) {                          /* steal from the rich */
            rh_slot tmp = *s;
            *s = carry;
            carry = tmp;
            d = sd;
        }
    }
    /* unreachable while the invariants hold; keep ownership consistent anyway */
    if (carry.key)
        t->al.release(t->al.ctx, carry.key);
    return RH_CORRUPT;
}

rh_status rh_get(const rh_table *t, const void *key, size_t len, uint64_t *out)
{
    if (t == NULL || out == NULL || !args_ok(key, len))
        return RH_ERR_ARG;
    const unsigned char *k = (const unsigned char *)key;
    size_t at = rh_find(t, k, len, t->hash(t->hash_ctx, k, len));
    if (at == RH_CAPACITY)
        return RH_NOT_FOUND;
    *out = t->slot[at].val;
    return RH_OK;
}

rh_status rh_delete(rh_table *t, const void *key, size_t len)
{
    if (t == NULL || !args_ok(key, len))
        return RH_ERR_ARG;
    const unsigned char *k = (const unsigned char *)key;
    size_t at = rh_find(t, k, len, t->hash(t->hash_ctx, k, len));
    if (at == RH_CAPACITY)
        return RH_NOT_FOUND;
    if (t->slot[at].key)
        t->al.release(t->al.ctx, t->slot[at].key);
    /* backward shift: move each following displaced entry one slot closer to home */
    size_t hole = at;
    for (size_t step = 1; step < RH_CAPACITY; ++step) {
        size_t nx = (hole + 1u) & RH_MASK;
        rh_slot *s = &t->slot[nx];
        if (!s->used || rh_dist(nx, s->hash) == 0)
            break;
        t->slot[hole] = *s;
        hole = nx;
    }
    memset(&t->slot[hole], 0, sizeof t->slot[hole]);
    t->count--;
    return RH_OK;
}

void rh_destroy(rh_table *t)
{
    if (t == NULL)
        return;
    for (size_t i = 0; i < RH_CAPACITY; ++i) {
        if (t->slot[i].used && t->slot[i].key)
            t->al.release(t->al.ctx, t->slot[i].key);
    }
    memset(t->slot, 0, sizeof t->slot);
    t->count = 0;
}

rh_status rh_validate(const rh_table *t)
{
    if (t == NULL)
        return RH_ERR_ARG;
    size_t used = 0;
    for (size_t i = 0; i < RH_CAPACITY; ++i) {
        const rh_slot *s = &t->slot[i];
        if (!s->used) {
            if (s->key != NULL || s->len != 0)
                return RH_CORRUPT;                         /* stale data in empty slot */
            continue;
        }
        ++used;
        if ((s->len == 0) != (s->key == NULL))
            return RH_CORRUPT;                             /* ownership mismatch */
        if (t->hash(t->hash_ctx, s->key, s->len) != s->hash)
            return RH_CORRUPT;                             /* stored hash wrong */
        size_t d = rh_dist(i, s->hash);
        if (d > 0) {                                       /* predecessor must be occupied and not richer */
            const rh_slot *p = &t->slot[(i + RH_CAPACITY - 1u) & RH_MASK];
            if (!p->used || rh_dist((i + RH_CAPACITY - 1u) & RH_MASK, p->hash) + 1u < d)
                return RH_CORRUPT;
        }
        if (rh_find(t, s->key, s->len, s->hash) != i)
            return RH_CORRUPT;                             /* unreachable or duplicate key */
    }
    if (used != t->count)
        return RH_CORRUPT;
    return RH_OK;
}
```

#### 3. ビルド方法・使用環境

```sh
# PowerShell 7 で MSVC 開発環境（vcvars64.bat）を読み込んだ後、build.ps1 が次の4構成を順にビルド・実行する
# q08_test.c は #include "q08.c" により実装と同じ翻訳単位でビルドする
W='-Wall -Wextra -Wpedantic -Wshadow -Wconversion -Wsign-conversion -Wcast-qual -Wstrict-prototypes -Wmissing-prototypes -Wvla -Wformat=2'
# (1) Clang -O2
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O2 -fuse-ld=link q08_test.c -o q08_rel.exe
# (2) Clang + AddressSanitizer（MSVC 同梱の ASan ランタイムでリンク）
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O1 -g -fsanitize=address -fno-omit-frame-pointer -fms-runtime-lib=dll -c q08_test.c -o q08_asan.obj
link /nologo /DEBUG /OUT:q08_asan.exe q08_asan.obj clang_rt.asan_dynamic-x86_64.lib clang_rt.asan_dynamic_runtime_thunk-x86_64.lib msvcrt.lib ucrt.lib vcruntime.lib legacy_stdio_definitions.lib
# (3) Clang + UndefinedBehaviorSanitizer（トラップ方式）
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O1 -g -fsanitize=undefined -fsanitize-trap=undefined -fno-sanitize-recover=all -fuse-ld=link q08_test.c -o q08_ubsan.exe
# (4) MSVC（2つ目のコンパイラ）
cl /nologo /std:c17 /W4 /experimental:c11atomics /utf-8 /O2 /D_CRT_SECURE_NO_WARNINGS q08_test.c /Fe:q08_msvc.exe
# 各 .exe を実行し、終了コード 0 と "RESULT: N checks passed, 0 failed" を確認
```

**使用環境（全問共通）**

- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド、x86-64）
- コンパイラ1：**Clang 18.0.3**（Android NDK r27c に同梱。ターゲットは `x86_64-pc-windows-msvc`。ヘッダー・ライブラリは MSVC 14.52.37025 と Windows SDK/UCRT 10.0.26100、リンカーは link.exe）。`-std=c17` を指定。
- コンパイラ2：**MSVC cl 19.52.37029**（Visual Studio 18 Community）、`/std:c17 /W4`。
- GCC：この PC にはなく、使用していません。Linux／WSL の環境もありません（外部パッケージの無断導入禁止のため、導入していません）。
- AddressSanitizer：Clang でインストルメントし、MSVC 同梱の ASan ランタイム（`clang_rt.asan_dynamic-x86_64`）でリンクしました。ヒープ境界外書き込みの陽性対照で、`heap-buffer-overflow` を検出することを確認済みです。
- UndefinedBehaviorSanitizer：`-fsanitize=undefined -fsanitize-trap=undefined`（トラップ方式。ランタイムを使わず、未定義動作で不正命令の例外として異常終了します）。符号付きオーバーフローの陽性対照で、異常終了することを確認済みです。
- LeakSanitizer：Windows 版 ASan にないため未実施です。リークは各問の計数アロケーター（確保と解放の数の差）で検査しました。ThreadSanitizer：Windows では使えないため未実施です。
- ビルド補助 `shim/stdatomic.h`（製品コード外）：NDK の Clang に付属する `stdatomic.h` は Android（bionic）専用で Windows ではコンパイルできないため、MSVC の C11 実装 `vcruntime_c11_stdatomic.h`（Clang 互換の `__c11_atomic` 組み込み関数を使用）を読み込む1ファイルを置きました。Clang 18 が `__typeof_unqual__` に対応していないため、`atomic_is_lock_free` だけは `__c11_atomic_is_lock_free` で定義し直しています。
- `-D_CRT_SECURE_NO_WARNINGS`：MSVC の CRT が出す「`fopen` などの代わりに `_s` 版を使うように」という非推奨の通知だけを止める定義です（C17 標準関数の使用に対する通知であり、コードの欠陥を隠すものではありません）。

- Q08 の結果：4構成とも警告0件・ビルド成功。

#### 4. テストコード

```c
/* Q08 tests */
#include "q08.c"
#include <stdio.h>

static int g_fail = 0, g_pass = 0;
#define CHECK(cond, ...) do { if (cond) { ++g_pass; } else { ++g_fail; \
    printf("  FAIL %s:%d: ", __FILE__, __LINE__); printf(__VA_ARGS__); printf("\n"); } } while (0)
static void report(const char *name, int before_fail)
{
    printf("[%s] %s\n", g_fail == before_fail ? "PASS" : "FAIL", name);
}

typedef struct { long outstanding, total, fail_next; } acct;
static void *a_alloc(void *c, size_t n) { acct *a = (acct *)c; if (a->fail_next) { a->fail_next--; return NULL; } void *p = malloc(n); if (p) { a->outstanding++; a->total++; } return p; }
static void a_free(void *c, void *p) { acct *a = (acct *)c; if (p) { a->outstanding--; free(p); } }

/* injected hashes */
static uint64_t h_const(void *c, const unsigned char *k, size_t n) { (void)c; (void)k; (void)n; return 1020; }   /* all collide, run wraps */
static uint64_t h_few(void *c, const unsigned char *k, size_t n) { return rh_fnv1a64(c, k, n) & 7u; }            /* 8 homes */
static uint64_t h_end(void *c, const unsigned char *k, size_t n) { return 1016u + (rh_fnv1a64(c, k, n) % 8u); }    /* homes 1016..1023 */

static uint64_t rs = 0x9FB21C651E98DF25u;
static uint64_t rnd(void) { rs ^= rs << 13; rs ^= rs >> 7; rs ^= rs << 17; return rs; }

/* reference map */
#define NKEYS 1600
typedef struct { unsigned char k[256]; size_t n; uint64_t v; int present; } refent;
static refent R[NKEYS];
static size_t ref_count(void) { size_t c = 0; for (int i = 0; i < NKEYS; ++i) c += (size_t)R[i].present; return c; }
static long ref_owned(void) { long c = 0; for (int i = 0; i < NKEYS; ++i) c += (R[i].present && R[i].n > 0); return c; }

static void make_keys(void)
{
    for (int i = 0; i < NKEYS; ++i) {
        size_t n;
        switch (i % 5) { case 0: n = (size_t)(rnd() % 4); break; case 1: n = (size_t)(rnd() % 16); break; case 2: n = 255; break; default: n = (size_t)(rnd() % 256); }
        int unique, attempt = 0;
        do {
            for (size_t j = 0; j < n; ++j) R[i].k[j] = (unsigned char)((rnd() % 4 == 0) ? 0 : rnd());   /* many NUL bytes */
            if (attempt++ == 0 && i > 0 && i % 7 == 0 && R[i - 1].n > 0) {         /* prefix family: previous key minus one byte */
                n = R[i - 1].n - 1; memcpy(R[i].k, R[i - 1].k, n);
            }
            R[i].n = n;
            unique = 1;
            for (int j = 0; j < i && unique; ++j) if (R[j].n == n && memcmp(R[j].k, R[i].k, n) == 0) unique = 0;
            if (!unique) { n = 1 + (size_t)(rnd() % 255); }
        } while (!unique);
        R[i].present = 0;
    }
}

static int run_random(rh_hash_fn hf, const char *name, long ops, acct *A)
{
    rh_allocator al = { a_alloc, a_free, A };
    static rh_table T;
    for (int i = 0; i < NKEYS; ++i) R[i].present = 0;
    if (rh_init(&T, hf, NULL, &al) != RH_OK) return 0;
    long bad = 0, n_full = 0, n_put = 0, n_upd = 0, n_del = 0, n_get = 0, n_nomem = 0;
    static rh_table snap;
    for (long op = 0; op < ops; ++op) {
        int i = (int)(rnd() % NKEYS);
        unsigned kind = (unsigned)(rnd() % 100);
        if (kind < 45) {
            uint64_t v = rnd();
            int inject = (rnd() % 50 == 0);
            if (inject) { A->fail_next = 1; memcpy(&snap, &T, sizeof T); }
            rh_status st = rh_put(&T, R[i].k, R[i].n, v);
            if (R[i].present) {
                if (st != RH_OK) ++bad; else { R[i].v = v; ++n_upd; }       /* update never allocates */
                if (inject) A->fail_next = 0;
            } else if (ref_count() >= RH_CAPACITY) {
                if (st != RH_FULL) ++bad; ++n_full;
                if (inject) A->fail_next = 0;
            } else if (inject && R[i].n > 0) {
                if (st != RH_NOMEM || memcmp(&snap, &T, sizeof T) != 0) ++bad;   /* state unchanged */
                ++n_nomem;
            } else {
                if (inject) A->fail_next = 0;
                if (st != RH_OK) ++bad; else { R[i].present = 1; R[i].v = v; ++n_put; }
            }
        } else if (kind < 75) {
            uint64_t o = 0x1111111111111111u;
            rh_status st = rh_get(&T, R[i].k, R[i].n, &o);
            if (R[i].present ? (st != RH_OK || o != R[i].v) : (st != RH_NOT_FOUND || o != 0x1111111111111111u)) ++bad;
            ++n_get;
        } else {
            rh_status st = rh_delete(&T, R[i].k, R[i].n);
            if (R[i].present ? st != RH_OK : st != RH_NOT_FOUND) ++bad;
            if (R[i].present) { R[i].present = 0; ++n_del; }
        }
        if (op % 2000 == 0 || op == ops - 1) {
            if (rh_validate(&T) != RH_OK) { ++bad; printf("  validate failed at op %ld\n", op); }
            if (T.count != ref_count() || A->outstanding != ref_owned()) { ++bad; printf("  count/ownership mismatch at op %ld\n", op); }
            for (int j = 0; j < NKEYS; ++j) {            /* full comparison with the reference */
                uint64_t o = 7;
                rh_status st = rh_get(&T, R[j].k, R[j].n, &o);
                if (R[j].present ? (st != RH_OK || o != R[j].v) : st != RH_NOT_FOUND) { ++bad; break; }
            }
        }
        if (bad) { printf("  first mismatch at op %ld\n", op); break; }
    }
    rh_destroy(&T);
    printf("  %s: ops=%ld put=%ld update=%ld delete=%ld get=%ld full=%ld nomem_injected=%ld -> %s\n",
           name, ops, n_put, n_upd, n_del, n_get, n_full, n_nomem, bad ? "MISMATCH" : "match");
    return bad == 0 && A->outstanding == 0;
}

int main(void)
{
    int f0;
    acct A = { 0, 0, 0 };
    rh_allocator AL = { a_alloc, a_free, &A };
    static rh_table T;

    /* T1: empty key, NUL bytes, prefixes, ownership */
    f0 = g_fail;
    {
        CHECK(rh_init(&T, NULL, NULL, &AL) == RH_OK, "init");
        unsigned char buf[8] = { 'a', 0, 'b', 'c' };
        uint64_t o;
        CHECK(rh_put(&T, NULL, 0, 100) == RH_OK, "empty key insert");
        CHECK(rh_put(&T, buf, 3, 1) == RH_OK && rh_put(&T, buf, 4, 2) == RH_OK && rh_put(&T, buf, 1, 3) == RH_OK && rh_put(&T, buf, 2, 4) == RH_OK, "a\\0b, a\\0bc, a, a\\0");
        CHECK(T.count == 5 && A.outstanding == 4, "4 owned copies (empty key owns nothing)");
        memset(buf, 0x77, sizeof buf);                                   /* caller buffer reused */
        unsigned char q[4] = { 'a', 0, 'b', 'c' };
        CHECK(rh_get(&T, q, 3, &o) == RH_OK && o == 1, "a\\0b");
        CHECK(rh_get(&T, q, 4, &o) == RH_OK && o == 2, "a\\0bc");
        CHECK(rh_get(&T, q, 1, &o) == RH_OK && o == 3, "a");
        CHECK(rh_get(&T, q, 2, &o) == RH_OK && o == 4, "a\\0");
        CHECK(rh_get(&T, "", 0, &o) == RH_OK && o == 100, "empty key (non-NULL pointer)");
        unsigned char q2[3] = { 'a', 0, 'c' };
        o = 55;
        CHECK(rh_get(&T, q2, 3, &o) == RH_NOT_FOUND && o == 55, "a\\0c absent, out untouched");
        CHECK(rh_put(&T, q, 3, 11) == RH_OK && A.outstanding == 4 && T.count == 5, "overwrite: no new allocation");
        CHECK(rh_get(&T, q, 3, &o) == RH_OK && o == 11, "updated value");
        CHECK(rh_delete(&T, NULL, 0) == RH_OK && rh_get(&T, NULL, 0, &o) == RH_NOT_FOUND, "delete empty key");
        CHECK(rh_delete(&T, q, 1) == RH_OK && A.outstanding == 3, "delete frees the copy");
        CHECK(rh_delete(&T, q, 1) == RH_NOT_FOUND, "second delete not found");
        CHECK(rh_validate(&T) == RH_OK, "valid");
        unsigned char big[256] = { 0 };
        CHECK(rh_put(&T, big, 256, 1) == RH_ERR_ARG && rh_put(&T, NULL, 3, 1) == RH_ERR_ARG && rh_get(&T, q, 1, NULL) == RH_ERR_ARG, "argument checks");
        CHECK(rh_put(&T, big, 255, 9) == RH_OK && rh_get(&T, big, 255, &o) == RH_OK && o == 9, "255-byte key");
        rh_destroy(&T);
        CHECK(A.outstanding == 0, "destroy releases everything");
    }
    report("T1 empty key / NUL bytes / common prefixes / overwrite / ownership", f0);

    /* T2: full collision chain crossing the table end, delete in the middle */
    f0 = g_fail;
    {
        CHECK(rh_init(&T, h_const, NULL, &AL) == RH_OK, "init");
        char k[16];
        for (int i = 0; i < 10; ++i) { snprintf(k, sizeof k, "k%d", i); CHECK(rh_put(&T, k, strlen(k), (uint64_t)i) == RH_OK, "put %s", k); }
        CHECK(T.slot[1020].used && T.slot[1023].used && T.slot[0].used && T.slot[5].used && !T.slot[6].used, "run 1020..1023,0..5 wraps around");
        CHECK(rh_validate(&T) == RH_OK, "valid");
        CHECK(rh_delete(&T, "k3", 2) == RH_OK, "delete k3 (inside the run, before the wrap)");
        CHECK(!T.slot[5].used && rh_validate(&T) == RH_OK, "run shifted back by one, no tombstone");
        for (int i = 0; i < 10; ++i) {
            snprintf(k, sizeof k, "k%d", i); uint64_t o = 999;
            rh_status st = rh_get(&T, k, strlen(k), &o);
            CHECK(i == 3 ? (st == RH_NOT_FOUND && o == 999) : (st == RH_OK && o == (uint64_t)i), "after delete, get %s", k);
        }
        CHECK(rh_delete(&T, "k0", 2) == RH_OK && rh_delete(&T, "k9", 2) == RH_OK && rh_validate(&T) == RH_OK, "delete head and tail");
        for (int i = 1; i < 9; ++i) { snprintf(k, sizeof k, "k%d", i); uint64_t o; if (i != 3) CHECK(rh_get(&T, k, strlen(k), &o) == RH_OK && o == (uint64_t)i, "still found %s", k); }
        rh_destroy(&T);
    }
    report("T2 all-colliding keys, run wrapping past slot 1023, backward-shift deletion", f0);

    /* T3: table full, update allowed, insert after delete */
    f0 = g_fail;
    {
        rh_hash_fn hs[3] = { NULL, h_const, h_end };
        for (int m = 0; m < 3; ++m) {
            CHECK(rh_init(&T, hs[m], NULL, &AL) == RH_OK, "init");
            char k[16];
            int ok = 1;
            for (int i = 0; i < 1024; ++i) { snprintf(k, sizeof k, "key%d", i); if (rh_put(&T, k, strlen(k), (uint64_t)i) != RH_OK) ok = 0; }
            CHECK(ok && T.count == 1024 && rh_validate(&T) == RH_OK, "mode %d: 1024 inserted", m);
            long before = A.outstanding;
            CHECK(rh_put(&T, "new", 3, 1) == RH_FULL && A.outstanding == before, "mode %d: 1025th insert FULL, no leak", m);
            CHECK(rh_put(&T, "key77", 5, 4242) == RH_OK, "mode %d: update in full table", m);
            uint64_t o = 0;
            CHECK(rh_get(&T, "key77", 5, &o) == RH_OK && o == 4242, "updated");
            o = 5;
            CHECK(rh_get(&T, "absent", 6, &o) == RH_NOT_FOUND && o == 5, "mode %d: lookup of absent key in a full table terminates", m);
            CHECK(rh_delete(&T, "key500", 6) == RH_OK && rh_put(&T, "new", 3, 1) == RH_OK && rh_validate(&T) == RH_OK, "mode %d: insert after delete", m);
            rh_destroy(&T);
            CHECK(A.outstanding == 0, "released");
        }
    }
    report("T3 full table: new key FULL, update OK, absent lookup terminates (FNV / all-collide / wrap)", f0);

    /* T4: allocation failure leaves the table byte-for-byte unchanged */
    f0 = g_fail;
    {
        static rh_table before;
        CHECK(rh_init(&T, h_few, NULL, &AL) == RH_OK, "init");
        char k[16];
        for (int i = 0; i < 300; ++i) { snprintf(k, sizeof k, "x%d", i); (void)rh_put(&T, k, strlen(k), (uint64_t)i); }
        memcpy(&before, &T, sizeof T);
        long out_before = A.outstanding;
        A.fail_next = 1;
        CHECK(rh_put(&T, "brand-new", 9, 1) == RH_NOMEM, "NOMEM");
        CHECK(memcmp(&before, &T, sizeof T) == 0 && A.outstanding == out_before, "table unchanged, nothing leaked");
        A.fail_next = 1;
        CHECK(rh_put(&T, "x5", 2, 77) == RH_OK, "update needs no allocation even when allocation would fail");
        A.fail_next = 0;
        CHECK(rh_validate(&T) == RH_OK, "valid");
        rh_destroy(&T);
        CHECK(A.outstanding == 0, "no leak");
    }
    report("T4 allocation failure injection: strong guarantee", f0);

    /* T5: >= 100000 deterministic random operations per hash mode vs reference map */
    f0 = g_fail;
    make_keys();
    CHECK(run_random(NULL, "FNV-1a", 200000, &A), "FNV-1a random");
    CHECK(run_random(h_few, "8 homes", 150000, &A), "few homes random");
    CHECK(run_random(h_end, "homes 1016..1023 (wrap)", 150000, &A), "wrap random");
    CHECK(run_random(h_const, "all collide", 100000, &A), "all-collide random");
    CHECK(A.outstanding == 0, "no leak overall (outstanding=%ld)", A.outstanding);
    report("T5 600000 random put/get/delete ops vs reference map (4 hash modes, NOMEM injection)", f0);

    /* T6: FNV-1a known values */
    f0 = g_fail;
    CHECK(rh_fnv1a64(NULL, (const unsigned char *)"", 0) == 0xCBF29CE484222325u, "FNV-1a(\"\")");
    CHECK(rh_fnv1a64(NULL, (const unsigned char *)"a", 1) == 0xAF63DC4C8601EC8Cu, "FNV-1a(\"a\")");
    CHECK(rh_fnv1a64(NULL, (const unsigned char *)"foobar", 6) == 0x85944171F73967E8u, "FNV-1a(\"foobar\")");
    report("T6 FNV-1a 64 reference values", f0);

    printf("RESULT: %d checks passed, %d failed\n", g_pass, g_fail);
    return g_fail ? 1 : 0;
}
```

#### 5. 検証結果（期待値と実測値を区別）

4構成とも `RESULT: 90 checks passed, 0 failed`、終了コード 0 でした。

| テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 空キー（T1） | 長さ0のキーを挿入・取得・削除できる。確保は0回 | 一致 | 実施（4構成） |
| NULを含むキー・同じ接頭部（T1） | "a"、"a\0"、"a\0b"、"a\0bc" はそれぞれ別のキー。"a\0c" は不在で、出力先は不変 | 一致 | 実施 |
| キーの所有（T1） | 挿入後に呼び出し元のバッファを書き換えても、別に用意したキーで取得できる | 一致 | 実施 |
| 上書き（T1） | 既存キーの put は値だけを更新し、新たな確保はしない | 一致 | 実施 |
| 255バイトのキー、256バイト・NULL キーは引数エラー（T1） | OK / RH_ERR_ARG | 一致 | 実施 |
| 全衝突・境界をまたぐ衝突列（T2） | 起点1020に10キー → スロット1020〜1023、0〜5 に並ぶ（周回） | 一致 | 実施 |
| 削除後の後続探索（T2） | 列の途中の k3 を削除すると後続が1つずつ詰まり、スロット5が空く（墓石なし）。残りの全キーを取得できる。先頭・末尾の削除後も同様 | 一致 | 実施 |
| テーブル満杯（T3） | FNV／全衝突／末尾集中の3方式で1024件挿入できる。1025件目の新規キーは RH_FULL でリークなし。満杯でも既存キーの更新は可能。不在キーの検索も停止する。1件削除すると再び挿入できる | 3方式とも一致 | 実施 |
| 確保失敗の注入（T4） | 新規挿入が RH_NOMEM になり、表全体（構造体のバイト列）が完全に不変、未解放数も不変。更新は確保が必要ないので成功 | 一致 | 実施 |
| 決定的なランダム put/get/delete と参照マップの比較（T5） | 1,600種のキー（空・NUL 多用・255バイト・接頭部の族）で、FNV-1a 20万回、起点8個 15万回、起点1016〜1023（周回）15万回、全衝突10万回、計60万回。各操作の結果と、2000操作ごとの全キーの照合、rh_validate、件数、所有キー数（＝未解放の確保数）が参照と一致。満杯の発生と NOMEM 注入（約2%）も含む | 4方式とも match（例：FNV は put 32,303・更新 55,668・削除 31,303・満杯 1,091・NOMEM 660） | 実施 |
| リーク検査 | destroy 後の未解放数0 | 0 | 実施 |
| FNV-1a 64 の既知値（T6） | ""→CBF29CE484222325、"a"→AF63DC4C8601EC8C、"foobar"→85944171F73967E8 | 一致 | 実施 |
| ASan / UBSan | 報告なし | 報告なし | 実施 |
| GCC・Linux | — | 未実施 | 未実施 |

#### 6. 未実施項目・制約・残課題

- GCC・Linux での実行は未実施です。LeakSanitizer は Windows で使えないため、リークは計数アロケーターで確認しました。
- 計算量：通常のハッシュ分布では期待 O(1) です。全衝突のような敵対的な入力では、問題文で許されたとおり O(n)（最大1024回の探索）になります。`rh_validate` は検証用で、O(n × 探索長) です。
- 制約：容量は1024で固定で、伸長しません。1スレッド専用です。ハッシュ関数を差し替える場合は、同じキーに常に同じ値を返す必要があります（validate で検査します）。

---

## Q09【地獄級】改行・引用符・空欄に対応する完全ストリーミングCSVパーサ

**狙い：** 分割入力、有限状態機械、空フィールドの保持、レコード単位の障害分離、厳密な形式チェック。

### 元コード（不十分）

~~~c
#include <string.h>
#include <stdio.h>
void csv_bad(char *line) {
    char *part = strtok(line, ",");
    while (part) {
        puts(part);
        part = strtok(NULL, ",");
    }
}
~~~

### 対象仕様（この問題内のRFC4180系サブセット）

- 行区切りは引用符の外にある **CRLF**。最後の行だけCRLFなしでも可。空ファイルはレコード0件。最終CRLFだけで余計な空レコードを作らない。
- カンマ区切り。フィールドを二重引用符で囲むとカンマとCRLFを内容として格納できる。引用符内の **"" は1文字の "** にデコードする。
- 引用符なしフィールド内の " を拒否。閉じ引用符の直後はカンマ・CRLF・入力終端だけ許可。引用符外の孤立CR・LFはエラー。NULバイトを拒否。ほかの0x01〜0xFFはバイトとして許可し、UTF-8検証はしない。
- 1レコードのフィールド数最大128、**復号済み1フィールド長最大65535バイト**。超過を検出し、オーバーフローを起こさない。
- **csv_init / csv_feed / csv_finish / csv_reset / csv_destroy** を実装。任意チャンク境界で正しく動き、完全なレコードだけをコールバックで通知する。コールバック中のフィールドは長さ付きバイト列として有効。
- レコード完成前の不正入力は未完成レコードを通知しない。すでに通知済みのレコードは維持。失敗後はresetまで入力拒否。コールバック失敗にも対応。

### 必須検証

a,b,c／,,／"a,b",c／"a""b",c／"a(CRLF)b",c／空ファイル／最終CRLFあり・なし／すべての1バイト分割／CRとLFの間で分割／引用符2個の間で分割／閉じ引用符の後の不正文字／フィールド数129／フィールド65536バイト／NUL／孤立CR/LF／長大入力でリークなし。

### ★★FREYJAの回答【Q09・記入欄】

> **ここから下が回答用スペースです。** GitHubの鉛筆アイコンからファイルを編集し、各「（ここに記入）」やコード枠内の例示文を自分の回答に置き換えてください。問題文・要求仕様・元コードは変更しないこと。

#### 1. 根本原因・危険性・影響

**元コード `csv_bad` の問題点（静的解析）**

| # | 根本原因 | 影響 |
| --- | --- | --- |
| 1 | `strtok` は連続する区切り文字を1つにまとめ、先頭・末尾の区切りも無視する | `,,` が0件になるなど、空フィールドが消えて列がずれる |
| 2 | 引用符の処理がない | `"a,b"` が2つに割れる。`""` を復号しない。引用符内の CRLF を扱えない |
| 3 | `strtok` は入力を書き換え、内部に静的な状態を持つ | 文字列リテラルを渡すと未定義動作。再入不可・スレッド安全でない。呼び出し元の他の strtok と干渉する |
| 4 | NUL 終端の1行を前提とする | 分割入力（ストリーミング）ができない。NUL を含むデータが切り詰められる。行の長さが無制限 |
| 5 | CRLF・孤立 CR/LF・不正な引用符を検査しない | 不正な入力を検出できない |
| 6 | `puts` で出力するだけ | フィールドの境界・長さ・レコードの区切りが呼び出し元に伝わらない。エラーを報告する手段がない |

**修正方針：** 1バイトずつ遷移する有限状態機械（FIELD_START / UNQUOTED / QUOTED / QUOTE_SEEN / CR / FAILED）にしました。チャンクの境界は状態に吸収されるため、どこで分割しても結果は同じです。復号した1レコード分のバイトを1つのバッファに貯め、フィールドは (開始位置, 長さ) の配列で管理します。CRLF の LF を受けた時点か finish の時点で、完全なレコードだけを (ポインター, 長さ) の配列としてコールバックへ渡します。1フィールドの上限65535バイトは追加の直前に検査し、フィールド数は129個目の開始（128個目のカンマ）で拒否します。バッファの最大は 128×65535 バイトなので、倍々に伸ばしても桁あふれしません。エラーのときは未完成のレコードを通知せず、通知済みのレコードはそのまま残します。エラー状態は reset まで保持し、入力を拒否します。

**仕様上の解釈（明記）：** 引用符の外では CR は直後の LF とだけ組み合わせられます。引用符の中では、CR・LF・カンマは単独でもデータとして受け付けます。途中の空行（`CRLF` だけの行）は「空フィールド1個のレコード」として扱います（RFC 4180 と同じ）。ファイル末尾の最後の CRLF の後には、レコードを作りません。

#### 2. 完全な修正版Cコード

```c
/*
 * Q09: streaming CSV parser for the RFC 4180 subset of the exam (C17)
 *
 * - records end with CRLF outside quotes; the last record may omit it
 * - "" inside a quoted field decodes to one '"'; after a closing quote only
 *   ',' CRLF or end of input may follow
 * - errors: '"' inside an unquoted field, lone CR / LF outside quotes, NUL,
 *   more than 128 fields, a decoded field longer than 65535 bytes,
 *   unterminated quote at end of input
 * - only complete records are reported; an error discards the incomplete record
 *   and the parser refuses input until csv_reset()
 *
 * Original implementation by ★★FREYJA for the C hell-level exam.
 */
#include <stddef.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

/* ------------------------------------------------------------------ API */

#define CSV_MAX_FIELDS    128u
#define CSV_MAX_FIELD_LEN 65535u

typedef enum {
    CSV_OK = 0,
    CSV_ERR_ARG,                /* invalid argument; state unchanged          */
    CSV_ERR_QUOTE_IN_UNQUOTED,  /* '"' inside an unquoted field               */
    CSV_ERR_AFTER_QUOTE,        /* illegal byte after a closing quote         */
    CSV_ERR_LONE_CR,            /* CR not followed by LF (outside quotes)     */
    CSV_ERR_LONE_LF,            /* LF without CR (outside quotes)             */
    CSV_ERR_NUL,                /* NUL byte                                   */
    CSV_ERR_TOO_MANY_FIELDS,    /* more than 128 fields in a record           */
    CSV_ERR_FIELD_TOO_LONG,     /* decoded field longer than 65535 bytes      */
    CSV_ERR_UNTERMINATED_QUOTE, /* end of input inside a quoted field         */
    CSV_ERR_CALLBACK,           /* record callback returned non-zero          */
    CSV_ERR_NOMEM
} csv_status;

typedef struct { const unsigned char *data; size_t len; } csv_field;

/* Called once per complete record. Field data is valid only during the call. */
typedef int (*csv_record_cb)(void *user, const csv_field *fields, size_t nfields);

typedef struct {
    void *(*alloc)(void *ctx, size_t n);
    void *(*resize)(void *ctx, void *p, size_t n);
    void (*release)(void *ctx, void *p);
    void *ctx;
} csv_allocator;

enum csv_state { CS_FIELD_START, CS_UNQUOTED, CS_QUOTED, CS_QUOTE_SEEN, CS_CR, CS_FAILED };

typedef struct {
    csv_record_cb cb;
    void         *user;
    csv_allocator al;
    int           state;
    csv_status    err;
    int           rec_started;               /* any byte of the current record seen */
    unsigned char *buf;                      /* decoded bytes of the current record */
    size_t        len, cap;
    size_t        nfields;                   /* completed fields of the current record */
    size_t        field_start;               /* offset of the field being decoded   */
    size_t        fstart[CSV_MAX_FIELDS];
    size_t        flen[CSV_MAX_FIELDS];
    csv_field     view[CSV_MAX_FIELDS];
} csv_parser;

csv_status csv_init(csv_parser *p, csv_record_cb cb, void *user);
csv_status csv_init_ex(csv_parser *p, csv_record_cb cb, void *user, const csv_allocator *al);
csv_status csv_feed(csv_parser *p, const void *data, size_t n);
csv_status csv_finish(csv_parser *p);
void       csv_reset(csv_parser *p);
void       csv_destroy(csv_parser *p);

/* ------------------------------------------------------- implementation */

static void *csv_malloc(void *c, size_t n) { (void)c; return malloc(n); }
static void *csv_realloc(void *c, void *p, size_t n) { (void)c; return realloc(p, n); }
static void csv_free(void *c, void *p) { (void)c; free(p); }

static void csv_clear_record(csv_parser *p)
{
    p->state = CS_FIELD_START;
    p->rec_started = 0;
    p->len = 0;
    p->nfields = 0;
    p->field_start = 0;
}

csv_status csv_init_ex(csv_parser *p, csv_record_cb cb, void *user, const csv_allocator *al)
{
    if (p == NULL || cb == NULL)
        return CSV_ERR_ARG;
    if (al != NULL && (al->alloc == NULL || al->resize == NULL || al->release == NULL))
        return CSV_ERR_ARG;
    p->cb = cb;
    p->user = user;
    if (al != NULL) {
        p->al = *al;
    } else {
        p->al.alloc = csv_malloc;
        p->al.resize = csv_realloc;
        p->al.release = csv_free;
        p->al.ctx = NULL;
    }
    p->buf = NULL;
    p->cap = 0;
    p->err = CSV_OK;
    csv_clear_record(p);
    return CSV_OK;
}

csv_status csv_init(csv_parser *p, csv_record_cb cb, void *user) { return csv_init_ex(p, cb, user, NULL); }

static csv_status csv_fail(csv_parser *p, csv_status e)
{
    p->state = CS_FAILED;
    p->err = e;
    return e;
}

/* Append one decoded byte to the current field. */
static csv_status csv_put(csv_parser *p, unsigned char c)
{
    if (p->len - p->field_start >= CSV_MAX_FIELD_LEN)
        return csv_fail(p, CSV_ERR_FIELD_TOO_LONG);
    if (p->len == p->cap) {
        /* record size is bounded by 128 * 65535 bytes, so doubling cannot overflow */
        size_t nc = p->cap ? p->cap * 2u : 256u;
        unsigned char *nb = (unsigned char *)(p->buf ? p->al.resize(p->al.ctx, p->buf, nc)
                                                     : p->al.alloc(p->al.ctx, nc));
        if (nb == NULL)
            return csv_fail(p, CSV_ERR_NOMEM);   /* old buffer still owned by p */
        p->buf = nb;
        p->cap = nc;
    }
    p->buf[p->len++] = c;
    return CSV_OK;
}

/* Close the current field. `more` = a ',' follows, so another field begins. */
static csv_status csv_end_field(csv_parser *p, int more)
{
    p->fstart[p->nfields] = p->field_start;
    p->flen[p->nfields] = p->len - p->field_start;
    p->nfields++;
    if (more && p->nfields >= CSV_MAX_FIELDS)
        return csv_fail(p, CSV_ERR_TOO_MANY_FIELDS);   /* a 129th field would start */
    p->field_start = p->len;
    p->state = CS_FIELD_START;
    return CSV_OK;
}

static csv_status csv_emit(csv_parser *p)
{
    for (size_t i = 0; i < p->nfields; ++i) {
        p->view[i].data = p->buf ? p->buf + p->fstart[i] : (const unsigned char *)"";
        p->view[i].len = p->flen[i];
    }
    size_t nf = p->nfields;
    csv_clear_record(p);                        /* the record is consumed even if the callback fails */
    if (p->cb(p->user, p->view, nf) != 0)
        return csv_fail(p, CSV_ERR_CALLBACK);
    return CSV_OK;
}

/* A byte outside quotes, at the start of or inside an unquoted field. */
static csv_status csv_unquoted_byte(csv_parser *p, unsigned char c)
{
    if (c == ',')
        return csv_end_field(p, 1);
    if (c == '\r') { p->state = CS_CR; return CSV_OK; }
    if (c == '\n')
        return csv_fail(p, CSV_ERR_LONE_LF);
    if (c == '"')
        return csv_fail(p, CSV_ERR_QUOTE_IN_UNQUOTED);
    csv_status st = csv_put(p, c);
    if (st == CSV_OK)
        p->state = CS_UNQUOTED;
    return st;
}

csv_status csv_feed(csv_parser *p, const void *data, size_t n)
{
    if (p == NULL)
        return CSV_ERR_ARG;
    if (p->state == CS_FAILED)
        return p->err;
    if (n == 0)
        return CSV_OK;
    if (data == NULL)
        return CSV_ERR_ARG;
    const unsigned char *s = (const unsigned char *)data;
    csv_status st;
    for (size_t i = 0; i < n; ++i) {
        unsigned char c = s[i];
        if (c == 0)
            return csv_fail(p, CSV_ERR_NUL);
        p->rec_started = 1;
        switch (p->state) {
        case CS_FIELD_START:
            if (c == '"') { p->state = CS_QUOTED; break; }
            if ((st = csv_unquoted_byte(p, c)) != CSV_OK) return st;   /* unquoted (maybe empty) field */
            break;
        case CS_UNQUOTED:
            if ((st = csv_unquoted_byte(p, c)) != CSV_OK) return st;
            break;
        case CS_QUOTED:
            if (c == '"') p->state = CS_QUOTE_SEEN;
            else if ((st = csv_put(p, c)) != CSV_OK) return st;   /* ',', CR, LF are data here */
            break;
        case CS_QUOTE_SEEN:
            if (c == '"') { if ((st = csv_put(p, '"')) != CSV_OK) return st; p->state = CS_QUOTED; }
            else if (c == ',') { if ((st = csv_end_field(p, 1)) != CSV_OK) return st; }
            else if (c == '\r') p->state = CS_CR;
            else return csv_fail(p, CSV_ERR_AFTER_QUOTE);
            break;
        case CS_CR:
            if (c != '\n')
                return csv_fail(p, CSV_ERR_LONE_CR);
            if ((st = csv_end_field(p, 0)) != CSV_OK) return st;
            if ((st = csv_emit(p)) != CSV_OK) return st;
            break;
        default:
            return csv_fail(p, CSV_ERR_ARG);
        }
    }
    return CSV_OK;
}

csv_status csv_finish(csv_parser *p)
{
    if (p == NULL)
        return CSV_ERR_ARG;
    if (p->state == CS_FAILED)
        return p->err;
    if (!p->rec_started)
        return CSV_OK;                          /* empty input / input ended with CRLF */
    switch (p->state) {
    case CS_QUOTED: return csv_fail(p, CSV_ERR_UNTERMINATED_QUOTE);
    case CS_CR:     return csv_fail(p, CSV_ERR_LONE_CR);
    default: break;                             /* FIELD_START, UNQUOTED, QUOTE_SEEN */
    }
    csv_status st = csv_end_field(p, 0);
    if (st != CSV_OK)
        return st;
    return csv_emit(p);
}

void csv_reset(csv_parser *p)
{
    if (p == NULL)
        return;
    p->err = CSV_OK;
    csv_clear_record(p);                        /* buffer kept for reuse */
}

void csv_destroy(csv_parser *p)
{
    if (p == NULL)
        return;
    if (p->buf)
        p->al.release(p->al.ctx, p->buf);
    p->buf = NULL;
    p->cap = 0;
    p->err = CSV_OK;
    csv_clear_record(p);
}
```

#### 3. ビルド方法・使用環境

```sh
# PowerShell 7 で MSVC 開発環境（vcvars64.bat）を読み込んだ後、build.ps1 が次の4構成を順にビルド・実行する
# q09_test.c は #include "q09.c" により実装と同じ翻訳単位でビルドする
W='-Wall -Wextra -Wpedantic -Wshadow -Wconversion -Wsign-conversion -Wcast-qual -Wstrict-prototypes -Wmissing-prototypes -Wvla -Wformat=2'
# (1) Clang -O2
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O2 -fuse-ld=link q09_test.c -o q09_rel.exe
# (2) Clang + AddressSanitizer（MSVC 同梱の ASan ランタイムでリンク）
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O1 -g -fsanitize=address -fno-omit-frame-pointer -fms-runtime-lib=dll -c q09_test.c -o q09_asan.obj
link /nologo /DEBUG /OUT:q09_asan.exe q09_asan.obj clang_rt.asan_dynamic-x86_64.lib clang_rt.asan_dynamic_runtime_thunk-x86_64.lib msvcrt.lib ucrt.lib vcruntime.lib legacy_stdio_definitions.lib
# (3) Clang + UndefinedBehaviorSanitizer（トラップ方式）
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O1 -g -fsanitize=undefined -fsanitize-trap=undefined -fno-sanitize-recover=all -fuse-ld=link q09_test.c -o q09_ubsan.exe
# (4) MSVC（2つ目のコンパイラ）
cl /nologo /std:c17 /W4 /experimental:c11atomics /utf-8 /O2 /D_CRT_SECURE_NO_WARNINGS q09_test.c /Fe:q09_msvc.exe
# 各 .exe を実行し、終了コード 0 と "RESULT: N checks passed, 0 failed" を確認
```

**使用環境（全問共通）**

- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド、x86-64）
- コンパイラ1：**Clang 18.0.3**（Android NDK r27c に同梱。ターゲットは `x86_64-pc-windows-msvc`。ヘッダー・ライブラリは MSVC 14.52.37025 と Windows SDK/UCRT 10.0.26100、リンカーは link.exe）。`-std=c17` を指定。
- コンパイラ2：**MSVC cl 19.52.37029**（Visual Studio 18 Community）、`/std:c17 /W4`。
- GCC：この PC にはなく、使用していません。Linux／WSL の環境もありません（外部パッケージの無断導入禁止のため、導入していません）。
- AddressSanitizer：Clang でインストルメントし、MSVC 同梱の ASan ランタイム（`clang_rt.asan_dynamic-x86_64`）でリンクしました。ヒープ境界外書き込みの陽性対照で、`heap-buffer-overflow` を検出することを確認済みです。
- UndefinedBehaviorSanitizer：`-fsanitize=undefined -fsanitize-trap=undefined`（トラップ方式。ランタイムを使わず、未定義動作で不正命令の例外として異常終了します）。符号付きオーバーフローの陽性対照で、異常終了することを確認済みです。
- LeakSanitizer：Windows 版 ASan にないため未実施です。リークは各問の計数アロケーター（確保と解放の数の差）で検査しました。ThreadSanitizer：Windows では使えないため未実施です。
- ビルド補助 `shim/stdatomic.h`（製品コード外）：NDK の Clang に付属する `stdatomic.h` は Android（bionic）専用で Windows ではコンパイルできないため、MSVC の C11 実装 `vcruntime_c11_stdatomic.h`（Clang 互換の `__c11_atomic` 組み込み関数を使用）を読み込む1ファイルを置きました。Clang 18 が `__typeof_unqual__` に対応していないため、`atomic_is_lock_free` だけは `__c11_atomic_is_lock_free` で定義し直しています。
- `-D_CRT_SECURE_NO_WARNINGS`：MSVC の CRT が出す「`fopen` などの代わりに `_s` 版を使うように」という非推奨の通知だけを止める定義です（C17 標準関数の使用に対する通知であり、コードの欠陥を隠すものではありません）。

- Q09 の結果：4構成とも警告0件・ビルド成功。

#### 4. テストコード

```c
/* Q09 tests */
#include "q09.c"
#include <stdio.h>

static int g_fail = 0, g_pass = 0;
#define CHECK(cond, ...) do { if (cond) { ++g_pass; } else { ++g_fail; \
    printf("  FAIL %s:%d: ", __FILE__, __LINE__); printf(__VA_ARGS__); printf("\n"); } } while (0)
static void report(const char *name, int before_fail)
{
    printf("[%s] %s\n", g_fail == before_fail ? "PASS" : "FAIL", name);
}

/* ---------- canonical log of records: 'R' n:u16 { len:u32 bytes }* ---------- */
typedef struct { unsigned char *b; size_t n, cap; size_t records; size_t fail_at; uint64_t hash; int hash_only; } logbuf;
static void lg_put(logbuf *l, const void *p, size_t n)
{
    const unsigned char *s = (const unsigned char *)p;
    for (size_t i = 0; i < n; ++i) l->hash = (l->hash ^ s[i]) * 0x100000001B3u;
    if (l->hash_only) { l->n += n; return; }
    if (l->n + n > l->cap) { size_t nc = l->cap ? l->cap : 1024; while (nc < l->n + n) nc *= 2; l->b = (unsigned char *)realloc(l->b, nc); l->cap = nc; }
    memcpy(l->b + l->n, s, n); l->n += n;
}
static void lg_clear(logbuf *l) { l->n = 0; l->records = 0; l->hash = 0xCBF29CE484222325u; }
static void lg_record(logbuf *l, const csv_field *f, size_t nf)
{
    unsigned char h[3] = { 'R', (unsigned char)nf, (unsigned char)(nf >> 8) };
    lg_put(l, h, 3);
    for (size_t i = 0; i < nf; ++i) {
        unsigned char L[4] = { (unsigned char)f[i].len, (unsigned char)(f[i].len >> 8), (unsigned char)(f[i].len >> 16), (unsigned char)(f[i].len >> 24) };
        lg_put(l, L, 4);
        if (f[i].len) lg_put(l, f[i].data, f[i].len);
    }
    l->records++;
}
static int rec_cb(void *u, const csv_field *f, size_t nf)
{
    logbuf *l = (logbuf *)u;
    for (size_t i = 0; i < nf; ++i) if (f[i].data == NULL) return 1;        /* never NULL */
    lg_record(l, f, nf);
    return (l->fail_at && l->records == l->fail_at) ? 1 : 0;
}

/* ---------- counting allocator ---------- */
typedef struct { long outstanding; long fail_after; long calls; } acct;   /* fail_after: fail when calls > fail_after (0 = never) */
static void *a_alloc(void *c, size_t n) { acct *a = (acct *)c; a->calls++; if (a->fail_after && a->calls > a->fail_after) return NULL; void *p = malloc(n); if (p) a->outstanding++; return p; }
static void *a_resize(void *c, void *p, size_t n) { acct *a = (acct *)c; a->calls++; if (a->fail_after && a->calls > a->fail_after) return NULL; return realloc(p, n); }
static void a_free(void *c, void *p) { acct *a = (acct *)c; if (p) { a->outstanding--; free(p); } }
static acct A = { 0, 0, 0 };
static csv_allocator AL = { a_alloc, a_resize, a_free, &A };

/* parse s split at cuts; returns final status (first failure or finish) */
static csv_status parse_cuts(const void *s, size_t n, const size_t *cuts, size_t nc, logbuf *l)
{
    csv_parser P;
    lg_clear(l);
    csv_init_ex(&P, rec_cb, l, &AL);
    const unsigned char *b = (const unsigned char *)s;
    size_t pos = 0;
    csv_status st = CSV_OK;
    for (size_t c = 0; c <= nc && st == CSV_OK; ++c) { size_t e = c < nc ? cuts[c] : n; st = csv_feed(&P, b + pos, e - pos); pos = e; }
    if (st == CSV_OK) st = csv_finish(&P);
    csv_destroy(&P);
    return st;
}
static csv_status parse_bytewise(const void *s, size_t n, logbuf *l)
{
    csv_parser P; lg_clear(l); csv_init_ex(&P, rec_cb, l, &AL);
    const unsigned char *b = (const unsigned char *)s;
    csv_status st = CSV_OK;
    for (size_t i = 0; i < n && st == CSV_OK; ++i) st = csv_feed(&P, b + i, 1);
    if (st == CSV_OK) st = csv_finish(&P);
    csv_destroy(&P);
    return st;
}

/* expected log from explicit fields */
static void expect_log(logbuf *l, const char *const *fields, const size_t *lens, const size_t *per_rec, size_t nrec)
{
    lg_clear(l);
    size_t k = 0;
    csv_field f[CSV_MAX_FIELDS + 2];
    for (size_t r = 0; r < nrec; ++r) {
        for (size_t i = 0; i < per_rec[r]; ++i, ++k) { f[i].data = (const unsigned char *)fields[k]; f[i].len = lens ? lens[k] : strlen(fields[k]); }
        lg_record(l, f, per_rec[r]);
    }
}
static int same(const logbuf *a, const logbuf *b) { return a->n == b->n && a->records == b->records && (a->n == 0 || memcmp(a->b, b->b, a->n) == 0); }

static int check_doc(const char *doc, size_t n, csv_status want, const char *const *fields, const size_t *per_rec, size_t nrec)
{
    logbuf got = { 0 }, exp = { 0 };
    expect_log(&exp, fields, NULL, per_rec, nrec);
    csv_status st = parse_cuts(doc, n, NULL, 0, &got);
    int ok = st == want && same(&got, &exp);
    csv_status st2 = parse_bytewise(doc, n, &got);
    ok = ok && st2 == want && same(&got, &exp);
    free(got.b); free(exp.b);
    return ok;
}

#define DOC(s) (s), (sizeof(s) - 1)   /* literal and its exact length (no terminating NUL) */
static uint64_t rs = 0xBF58476D1CE4E5B9u;
static uint64_t rnd(void) { rs ^= rs << 13; rs ^= rs >> 7; rs ^= rs << 17; return rs; }

/* serialize random records (independent writer) */
static size_t write_field(unsigned char *o, const unsigned char *f, size_t n, int force_quote)
{
    int q = force_quote;
    for (size_t i = 0; i < n; ++i) if (f[i] == ',' || f[i] == '"' || f[i] == '\r' || f[i] == '\n') q = 1;
    size_t k = 0;
    if (q) o[k++] = '"';
    for (size_t i = 0; i < n; ++i) { if (f[i] == '"') o[k++] = '"'; o[k++] = f[i]; }
    if (q) o[k++] = '"';
    return k;
}

int main(void)
{
    int f0;

    /* T1: required basic documents */
    f0 = g_fail;
    {
        const char *r1[] = { "a", "b", "c" }; size_t p1[] = { 3 };
        CHECK(check_doc(DOC("a,b,c"), CSV_OK, r1, p1, 1), "a,b,c");
        CHECK(check_doc(DOC("a,b,c\r\n"), CSV_OK, r1, p1, 1), "a,b,c CRLF");
        const char *r2[] = { "", "", "" }; size_t p2[] = { 3 };
        CHECK(check_doc(DOC(",,"), CSV_OK, r2, p2, 1), ",, -> 3 empty fields");
        const char *r3[] = { "a,b", "c" }; size_t p3[] = { 2 };
        CHECK(check_doc(DOC("\"a,b\",c"), CSV_OK, r3, p3, 1), "\"a,b\",c");
        const char *r4[] = { "a\"b", "c" };
        CHECK(check_doc(DOC("\"a\"\"b\",c"), CSV_OK, r4, p3, 1), "\"a\"\"b\",c");
        const char *r5[] = { "a\r\nb", "c" };
        CHECK(check_doc(DOC("\"a\r\nb\",c"), CSV_OK, r5, p3, 1), "quoted CRLF");
        CHECK(check_doc(DOC(""), CSV_OK, NULL, NULL, 0), "empty file -> 0 records");
        const char *r6[] = { "a", "b" }; size_t p6[] = { 1, 1 };
        CHECK(check_doc(DOC("a\r\nb\r\n"), CSV_OK, r6, p6, 2), "final CRLF: no extra empty record");
        CHECK(check_doc(DOC("a\r\nb"), CSV_OK, r6, p6, 2), "no final CRLF");
        const char *r7[] = { "a", "", "b" }; size_t p7[] = { 1, 1, 1 };
        CHECK(check_doc(DOC("a\r\n\r\nb"), CSV_OK, r7, p7, 3), "empty line in the middle = one empty field");
        const char *r8[] = { "", "x" }; size_t p8[] = { 2 };
        CHECK(check_doc(DOC("\"\",x"), CSV_OK, r8, p8, 1), "empty quoted field");
        const char *r9[] = { "\xFF\x01\x7F", "\xC3\xA9" }; size_t p9[] = { 2 };
        CHECK(check_doc(DOC("\xFF\x01\x7F,\xC3\xA9"), CSV_OK, r9, p9, 1), "bytes 0x01..0xFF accepted (no UTF-8 check)");
        const char *r10[] = { "a\nb", "c\rd" }; size_t p10[] = { 2 };
        CHECK(check_doc(DOC("\"a\nb\",\"c\rd\""), CSV_OK, r10, p10, 1), "lone LF / CR allowed inside quotes");
    }
    report("T1 a,b,c / ,, / quoted comma / \"\" / quoted CRLF / empty file / final CRLF with and without", f0);

    /* T2: error cases (complete records before the error are kept) */
    f0 = g_fail;
    {
        const char *ok1[] = { "ok" }; size_t pk[] = { 1 };
        struct { const char *d; size_t n; csv_status st; const char *name; } e[] = {
            { "ok\r\n\"a\"x,b\r\n", 12, CSV_ERR_AFTER_QUOTE, "char after closing quote" },
            { "ok\r\n\"a\"\n", 8, CSV_ERR_AFTER_QUOTE, "LF after closing quote" },
            { "ok\r\na\"b\r\n", 9, CSV_ERR_QUOTE_IN_UNQUOTED, "quote in unquoted field" },
            { "ok\r\na\rb\r\n", 9, CSV_ERR_LONE_CR, "lone CR" },
            { "ok\r\na\nb\r\n", 9, CSV_ERR_LONE_LF, "lone LF" },
            { "ok\r\na\r", 6, CSV_ERR_LONE_CR, "CR at end of input" },
            { "ok\r\n\"abc", 8, CSV_ERR_UNTERMINATED_QUOTE, "unterminated quote" },
            { "ok\r\na\0b\r\n", 9, CSV_ERR_NUL, "NUL in unquoted field" },
            { "ok\r\n\"a\0b\"\r\n", 11, CSV_ERR_NUL, "NUL in quoted field" },
        };
        for (size_t i = 0; i < sizeof e / sizeof e[0]; ++i)
            CHECK(check_doc(e[i].d, e[i].n, e[i].st, ok1, pk, 1), "%s", e[i].name);
    }
    report("T2 invalid char after quote / quote in unquoted / lone CR, LF / NUL / unterminated", f0);

    /* T3: field count 128 ok, 129 rejected; field length 65535 ok, 65536 rejected */
    f0 = g_fail;
    {
        static char doc[300000]; size_t n = 0;
        for (int i = 0; i < 128; ++i) { if (i) doc[n++] = ','; doc[n++] = 'x'; }
        logbuf l = { 0 };
        CHECK(parse_cuts(doc, n, NULL, 0, &l) == CSV_OK && l.records == 1, "128 fields accepted");
        doc[n++] = ','; doc[n++] = 'x';
        CHECK(parse_cuts(doc, n, NULL, 0, &l) == CSV_ERR_TOO_MANY_FIELDS && l.records == 0, "129 fields rejected");
        n = 0; for (int i = 0; i < 128; ++i) doc[n++] = ',';
        CHECK(parse_cuts(doc, n, NULL, 0, &l) == CSV_ERR_TOO_MANY_FIELDS, "128 commas = 129 empty fields rejected");
        memset(doc, 'a', 65536);
        CHECK(parse_cuts(doc, 65535, NULL, 0, &l) == CSV_OK && l.records == 1, "65535-byte field accepted");
        CHECK(parse_cuts(doc, 65536, NULL, 0, &l) == CSV_ERR_FIELD_TOO_LONG && l.records == 0, "65536-byte field rejected");
        /* quoted: 65535 decoded bytes including escaped quotes */
        n = 0; doc[n++] = '"';
        for (int i = 0; i < 65535; ++i) { if (i % 3 == 0) { doc[n++] = '"'; doc[n++] = '"'; } else doc[n++] = 'q'; }
        doc[n++] = '"';
        CHECK(parse_cuts(doc, n, NULL, 0, &l) == CSV_OK && l.records == 1 && l.n == 3 + 4 + 65535, "quoted 65535 decoded bytes accepted");
        memmove(doc + 2, doc + 1, n - 1); doc[1] = 'q'; n++;
        CHECK(parse_cuts(doc, n, NULL, 0, &l) == CSV_ERR_FIELD_TOO_LONG, "quoted 65536 decoded bytes rejected");
        /* 128 fields of 65535 bytes: the largest legal record */
        static char big[128 * 65536 + 16]; size_t bn = 0;
        for (int i = 0; i < 128; ++i) { if (i) big[bn++] = ','; memset(big + bn, 'A' + i % 26, 65535); bn += 65535; }
        CHECK(parse_cuts(big, bn, NULL, 0, &l) == CSV_OK && l.records == 1 && l.n == 3 + 128 * (4 + 65535), "128 x 65535 record");
        free(l.b);
        CHECK(A.outstanding == 0, "no leak");
    }
    report("T3 128/129 fields, 65535/65536-byte fields (unquoted and quoted)", f0);

    /* T4: every 1-byte split, all 2-way and 3-way splits of a composite document */
    f0 = g_fail;
    {
        const char doc[] = "a,\"b,\r\n\"\"x\"\"\",c\r\n,,\r\n\"\"\r\n\"q\"\"\",z\r\nlast,\"\"";
        size_t n = sizeof doc - 1;
        logbuf ref = { 0 }, got = { 0 };
        CHECK(parse_cuts(doc, n, NULL, 0, &ref) == CSV_OK && ref.records == 5, "composite parses (5 records)");
        CHECK(parse_bytewise(doc, n, &got) == CSV_OK && same(&got, &ref), "byte-by-byte");
        long runs = 0, bad = 0;
        for (size_t a = 0; a <= n; ++a)
            for (size_t b = a; b <= n; ++b) {
                size_t cuts[2] = { a, b };
                if (!(parse_cuts(doc, n, cuts, 2, &got) == CSV_OK && same(&got, &ref))) ++bad;
                ++runs;
            }
        CHECK(bad == 0, "%ld of %ld split runs differ", bad, runs);
        /* explicit: split between CR and LF, and between the two quotes of "" */
        const char d2[] = "x\r\ny"; size_t c1[1] = { 2 };
        logbuf r2 = { 0 };
        CHECK(parse_cuts(d2, 4, NULL, 0, &r2) == CSV_OK && parse_cuts(d2, 4, c1, 1, &got) == CSV_OK && same(&got, &r2) && got.records == 2, "split between CR and LF");
        const char d3[] = "\"a\"\"b\""; size_t c3[1] = { 3 };
        CHECK(parse_cuts(d3, 6, NULL, 0, &r2) == CSV_OK && parse_cuts(d3, 6, c3, 1, &got) == CSV_OK && same(&got, &r2), "split between the two quotes of \"\"");
        printf("  split runs: %ld (document %zu bytes)\n", runs, n);
        free(ref.b); free(got.b); free(r2.b);
    }
    report("T4 all split boundaries (1-byte, 2-way, 3-way), CR|LF and \"|\" splits", f0);

    /* T5: callback failure, reject until reset, argument rules */
    f0 = g_fail;
    {
        logbuf l = { 0 }; lg_clear(&l); l.fail_at = 2;
        csv_parser P; CHECK(csv_init_ex(&P, rec_cb, &l, &AL) == CSV_OK, "init");
        CHECK(csv_feed(&P, "a\r\nb\r\nc\r\n", 9) == CSV_ERR_CALLBACK && l.records == 2, "stops at failing record");
        CHECK(csv_feed(&P, "d\r\n", 3) == CSV_ERR_CALLBACK && csv_finish(&P) == CSV_ERR_CALLBACK && l.records == 2, "rejected until reset");
        csv_reset(&P); l.fail_at = 0; lg_clear(&l);
        CHECK(csv_feed(&P, "x,y", 3) == CSV_OK && csv_finish(&P) == CSV_OK && l.records == 1, "works after reset");
        CHECK(csv_feed(&P, NULL, 0) == CSV_OK && csv_feed(&P, NULL, 1) == CSV_ERR_ARG, "NULL only with length 0");
        CHECK(csv_feed(&P, "p", 1) == CSV_OK && csv_feed(&P, NULL, 3) == CSV_ERR_ARG && csv_feed(&P, "q", 1) == CSV_OK && csv_finish(&P) == CSV_OK, "ARG error does not change state");
        CHECK(l.records == 2 && l.n >= 4 && memcmp(l.b + l.n - 2, "pq", 2) == 0, "field 'pq' intact");
        csv_destroy(&P);
        CHECK(csv_init(NULL, rec_cb, NULL) == CSV_ERR_ARG && csv_init(&P, NULL, NULL) == CSV_ERR_ARG, "init args");
        free(l.b);
        CHECK(A.outstanding == 0, "no leak");
    }
    report("T5 callback failure / reset / NULL rules", f0);

    /* T6: random valid records -> independent writer -> parser (random chunks) round trip */
    f0 = g_fail;
    {
        static unsigned char doc[1 << 20]; static unsigned char fbuf[4096];
        long bad = 0;
        for (int t = 0; t < 3000; ++t) {
            size_t n = 0, nrec = 1 + (size_t)(rnd() % 6);
            logbuf exp = { 0 }; lg_clear(&exp);
            for (size_t r = 0; r < nrec; ++r) {
                size_t nf = 1 + (size_t)(rnd() % 6);
                csv_field fl[8]; static unsigned char store[8][64];
                for (size_t i = 0; i < nf; ++i) {
                    size_t L = (size_t)(rnd() % 8);
                    static const unsigned char alpha[] = "ab,\"\r\n\x01\xFF ";
                    for (size_t j = 0; j < L; ++j) store[i][j] = alpha[rnd() % (sizeof alpha - 1)];
                    fl[i].data = store[i]; fl[i].len = L;
                    if (nf == 1 && L == 0 && r + 1 == nrec) fl[i].len = L = 1, store[i][0] = 'z';   /* avoid ambiguous trailing empty record */
                    if (i) doc[n++] = ',';
                    size_t w = write_field(fbuf, store[i], L, (int)(rnd() % 4 == 0));
                    memcpy(doc + n, fbuf, w); n += w;
                }
                lg_record(&exp, fl, nf);
                if (r + 1 < nrec || rnd() % 2) { doc[n++] = '\r'; doc[n++] = '\n'; }
            }
            logbuf got = { 0 };
            size_t cuts[8]; size_t nc = (size_t)(rnd() % 8);
            for (size_t c = 0; c < nc; ++c) cuts[c] = (size_t)(rnd() % (n + 1));
            for (size_t c = 1; c < nc; ++c) for (size_t d = c; d > 0 && cuts[d - 1] > cuts[d]; --d) { size_t x = cuts[d]; cuts[d] = cuts[d - 1]; cuts[d - 1] = x; }
            if (!(parse_cuts(doc, n, cuts, nc, &got) == CSV_OK && same(&got, &exp))) ++bad;
            free(got.b); free(exp.b);
        }
        CHECK(bad == 0, "%ld of 3000 round trips differ", bad);
    }
    report("T6 3000 random documents: writer -> parser round trip with random chunking", f0);

    /* T7: random byte soup: result and status identical for one-shot, bytewise and random chunks */
    f0 = g_fail;
    {
        static const unsigned char alpha[] = "a,\"\r\n\0";
        unsigned char doc[64]; long bad = 0, errs = 0;
        for (int t = 0; t < 20000; ++t) {
            size_t n = (size_t)(rnd() % 40);
            for (size_t i = 0; i < n; ++i) doc[i] = alpha[rnd() % (rnd() % 50 == 0 ? 6 : 5)];
            logbuf a = { 0 }, b = { 0 };
            csv_status s1 = parse_cuts(doc, n, NULL, 0, &a);
            csv_status s2 = parse_bytewise(doc, n, &b);
            if (s1 != s2 || !same(&a, &b)) ++bad;
            size_t cut[1] = { n ? (size_t)(rnd() % n) : 0 };
            csv_status s3 = parse_cuts(doc, n, cut, 1, &b);
            if (s1 != s3 || !same(&a, &b)) ++bad;
            if (s1 != CSV_OK) ++errs;
            free(a.b); free(b.b);
        }
        CHECK(bad == 0, "%ld inconsistencies", bad);
        printf("  random byte documents: 20000 (%ld ended in an error)\n", errs);
    }
    report("T7 20000 random byte documents: chunking never changes records or status", f0);

    /* T8: long input (~50 MB, 400000 records) with leak accounting; allocation failure */
    f0 = g_fail;
    {
        static unsigned char chunk[1 << 16];
        logbuf l = { 0 }; lg_clear(&l); l.hash_only = 1;
        logbuf e = { 0 }; lg_clear(&e); e.hash_only = 1;
        csv_parser P; CHECK(csv_init_ex(&P, rec_cb, &l, &AL) == CSV_OK, "init");
        size_t total = 0, cl = 0; csv_status st = CSV_OK;
        for (long r = 0; r < 400000 && st == CSV_OK; ++r) {
            char f1[32]; int k = snprintf(f1, sizeof f1, "%ld", r);
            size_t big = (r % 1000 == 0) ? 20000 : (size_t)(rnd() % 100);
            static unsigned char f2[20001];
            for (size_t j = 0; j < big; ++j) f2[j] = (unsigned char)("xy,\"\r\n"[j % 6]);
            csv_field fl[2] = { { (const unsigned char *)f1, (size_t)k }, { f2, big } };
            lg_record(&e, fl, 2);
            static unsigned char rec[50000]; size_t rn = 0;
            memcpy(rec, f1, (size_t)k); rn = (size_t)k; rec[rn++] = ',';
            rn += write_field(rec + rn, f2, big, 0);
            rec[rn++] = '\r'; rec[rn++] = '\n';
            for (size_t j = 0; j < rn && st == CSV_OK; ) {
                size_t take = rn - j < sizeof chunk - cl ? rn - j : sizeof chunk - cl;
                memcpy(chunk + cl, rec + j, take); cl += take; j += take;
                if (cl == sizeof chunk) { st = csv_feed(&P, chunk, cl); total += cl; cl = 0; }
            }
        }
        if (st == CSV_OK && cl) { st = csv_feed(&P, chunk, cl); total += cl; }
        if (st == CSV_OK) st = csv_finish(&P);
        CHECK(st == CSV_OK && l.records == 400000 && l.hash == e.hash && l.n == e.n, "400000 records, log identical");
        CHECK(A.outstanding == 1, "one reusable record buffer while alive");
        csv_destroy(&P);
        CHECK(A.outstanding == 0, "no leak after destroy");
        printf("  long input: %zu bytes, %zu records\n", total, l.records);
        /* allocation failure: the first allocation fails, then a later growth fails */
        acct B = { 0, 1, 0 }; csv_allocator BL = { a_alloc, a_resize, a_free, &B };
        logbuf m = { 0 }; lg_clear(&m);
        CHECK(csv_init_ex(&P, rec_cb, &m, &BL) == CSV_OK, "init");
        static char longf[5000]; memset(longf, 'k', sizeof longf);
        CHECK(csv_feed(&P, "a\r\n", 3) == CSV_OK && m.records == 1, "first record fits in first buffer");
        CHECK(csv_feed(&P, longf, sizeof longf) == CSV_ERR_NOMEM && m.records == 1, "growth failure -> NOMEM");
        CHECK(csv_feed(&P, "b", 1) == CSV_ERR_NOMEM, "rejected after NOMEM");
        csv_destroy(&P);
        CHECK(B.outstanding == 0, "no leak after NOMEM");
        free(m.b);
    }
    report("T8 ~40 MB / 400000 records streaming with leak accounting; NOMEM", f0);

    printf("RESULT: %d checks passed, %d failed\n", g_pass, g_fail);
    return g_fail ? 1 : 0;
}
```

#### 5. 検証結果（期待値と実測値を区別）

4構成とも `RESULT: 56 checks passed, 0 failed`、終了コード 0 でした。期待値は、フィールドの配列から作った正規化ログ（'R' + フィールド数 + 各 [長さ + バイト列]）と、実際のコールバックの記録をバイト単位で比較しています。主要な文書は、一括入力と1バイトずつの入力の両方で確認しました。

| テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| a,b,c（CRLF の有無とも）（T1） | [a][b][c] | 一致 | 実施（4構成） |
| ,,（T1） | 空フィールド3個 | 一致 | 実施 |
| "a,b",c（T1） | [a,b][c] | 一致 | 実施 |
| "a""b",c（T1） | [a"b][c] | 一致 | 実施 |
| "a(CRLF)b",c（T1） | [a\r\nb][c] | 一致 | 実施 |
| 空ファイル（T1） | レコード0件 | 0件 | 実施 |
| 最終CRLFあり・なし（T1） | どちらも2レコード（余分な空レコードを作らない） | 一致 | 実施 |
| 途中の空行、空の引用フィールド、0x01〜0xFF のバイト、引用符内の単独 LF/CR（T1） | 仕様どおり | 一致 | 実施 |
| 閉じ引用符の後の不正文字（T2） | `"a"x` → AFTER_QUOTE、`"a"LF` → AFTER_QUOTE。直前の完全なレコード "ok" は保持し、未完成のレコードは通知しない | 一致 | 実施 |
| 引用符なしフィールド内の " ・孤立CR・孤立LF・末尾のCR（T2） | QUOTE_IN_UNQUOTED / LONE_CR / LONE_LF / LONE_CR | 一致 | 実施 |
| NUL（引用符の外・中）（T2） | CSV_ERR_NUL | 一致 | 実施 |
| 閉じていない引用符（T2） | finish で UNTERMINATED_QUOTE | 一致 | 実施 |
| フィールド数128・129（T3） | 128 は受理、129 は TOO_MANY_FIELDS（カンマ128個だけの行も129フィールドとして拒否） | 一致 | 実施 |
| フィールド65535・65536バイト（T3） | 引用符なし・引用符あり（`""` を1バイトと数える）とも、65535 は受理、65536 は FIELD_TOO_LONG | 一致 | 実施 |
| 最大のレコード 128×65535（T3） | 受理し、内容が一致 | 一致 | 実施 |
| すべての1バイト分割・全2分割・全3分割（T4） | 41バイトの複合文書（引用符・"" ・引用符内の CRLF・空フィールド・空行を含む）で、1バイトずつと全903通りの分割の結果が一括入力と同一 | 不一致0 | 実施 |
| CR と LF の間で分割、引用符2個の間で分割（T4） | 一括入力と同一 | 一致 | 実施 |
| コールバック失敗（T5） | 2件目で CALLBACK、以後の feed と finish はエラー。reset 後は回復 | 一致 | 実施 |
| NULL・長さ0（T5） | (NULL,0) は OK、(NULL,n) は ARG で状態不変 | 一致 | 実施 |
| ランダムな往復（T6） | 独立した書き出し関数で作った3,000文書を、乱数分割で解析すると元のレコードに戻る | 不一致0 | 実施 |
| ランダムなバイト列（T7） | 2万文書で、一括・1バイトずつ・乱数分割の結果と状態が常に同一 | 不一致0 | 実施 |
| 長大入力でリークなし（T8） | 36,624,973バイト・40万レコード（2万バイトの引用フィールドを含む）を64KiB 単位で入力し、正規化ログのハッシュと長さが一致。生存中の確保は1つ（再利用）、destroy 後は0 | 一致、未解放0 | 実施 |
| 確保失敗（T8） | バッファの拡張に失敗すると NOMEM、以後拒否、リーク0 | 一致 | 実施 |
| ASan / UBSan | 報告なし | 報告なし | 実施 |
| GCC・Linux | — | 未実施 | 未実施 |

（テストの初回実行では4件が失敗しました。原因はすべてテスト側でした。手で数えた文書長が1バイト多く、終端の NUL まで入力していたことが3件、65536バイトの文書を65535バイトしか埋めていなかったことが1件です。長さを `sizeof` から求めるマクロに直して再実行し、全件合格しました。製品コードは変更していません。）

#### 6. 未実施項目・制約・残課題

- GCC・Linux での実行は未実施です。カバレッジ誘導型のファジングは未実施です（分割の総当たりとランダム文書で代替）。
- 制約：コールバックに渡すフィールドの領域は、コールバックの実行中だけ有効です。空フィールドにも非NULLのポインターを渡します。1つのパーサーを複数スレッドから使うことは対象外です。UTF-8 の検証はしません（仕様どおり）。

---

## Q10【最終地獄級】4生産者・4消費者で破綻しない真のlock-free MPMCリングキュー

**狙い：** 複数スレッドの競合、ABA相当の世代識別、slotごとのsequence番号、CAS、線形化点、メモリモデルの説明。

### 元コード（危険）

~~~c
#include <stdatomic.h>
#include <stdint.h>
#define QCAP 256u
typedef struct {
    atomic_size_t enq, deq;
    uint64_t data[QCAP];
} Queue;
int enqueue_bad(Queue *q, uint64_t v) {
    size_t w = atomic_load(&q->enq);
    size_t r = atomic_load(&q->deq);
    if (w - r >= QCAP) return 0;
    q->data[w & (QCAP - 1)] = v;
    atomic_store(&q->enq, w + 1);
    return 1;
}
~~~

### 要求仕様

- **mpmc_init / mpmc_try_enqueue / mpmc_try_dequeue** を実装。容量256・格納値uint64_t・固定サイズ・ヒープ不要。複数producer / consumerが同時に呼ぶ。
- mutex、spinlock、condvar、外部キューライブラリを使わない。**C11 atomicsのみでシステム全体としてlock-freeの進行保証**を説明する。対象環境はsize_tアトミックがlock-freeで、64bit size_tを持つC17プラットフォーム。
- **各slotにatomic sequence番号を持たせて世代を区別**し、予約・公開・読み取り・再利用を競合なしに実装する。CAS失敗時の再試行、適切なacquire/release、非atomic payloadへのアクセス安全性を説明する。
- try_enqueueは満杯でfalse、try_dequeueは空でfalseを返す。失敗時のpop出力先は不変。返された成功要素の**重複・消失・破損は禁止**。線形化点を各操作について説明する。
- size_tカウンターがプログラムの試験期間中に周回しない（総操作数1億未満）という前提を明示してよい。Q03と違い周回テストは必須ではない。開始時は両カウンター0とする。
- キューへの試行呼び出しは、混雑していても少なくともいずれかのスレッドが進行できること。個々のスレッドの無飢餓は要求しない。

### 必須検証

1対1、4対4、8対8。4対4では各生産者が25万件を投入して**合計100万件**。投入失敗は再試行可能だが、計測結果からは成功件数のみを数える。終了時、ユニークIDを用いて100万件すべての **重複ゼロ・欠損ゼロ・範囲外ゼロ** を検証し、可能ならproducer別の順序も検証。満杯・空・スレッド開始順の違い・大量競合・複数回実行。可能な環境ではThreadSanitizerを使い、結果を記載。カウンター合計だけの一致は不十分。

### ★★FREYJAの回答【Q10・記入欄】

> **ここから下が回答用スペースです。** GitHubの鉛筆アイコンからファイルを編集し、各「（ここに記入）」やコード枠内の例示文を自分の回答に置き換えてください。問題文・要求仕様・元コードは変更しないこと。

#### 1. 根本原因・危険性・影響

**元コード `enqueue_bad` の問題点（静的解析）**

| # | 根本原因 | 発生する競合 | 影響 |
| --- | --- | --- | --- |
| 1 | 「enq を読む → 満杯を判定 → data に書く → enq に w+1 を書く」が、全体として原子的でない（CAS がない） | 2つの生産者が同じ w を読み、両方が `data[w & 255]` に書き、両方が `enq = w+1` を書く | 後から書いた値で先の値が消える（**消失**）。カウンターは1しか進まない。非atomic な `data` への同時書き込みはデータ競合（未定義動作） |
| 2 | スロットごとの世代情報（sequence）がない | 消費者は enq の増加しか見られないため、別の生産者が予約しただけで書き込み前のスロットを読む | 未初期化や前の周回の値を読む（**破損・重複**） |
| 3 | 満杯の判定に、古い r を使う可能性がある | 消費者がまだ読んでいるスロットを上書きする | データ破損 |
| 4 | 生産者が複数いると、`atomic_store(&q->enq, w + 1)` が他の生産者の更新を巻き戻す | 更新の消失 | カウンターと実データの不整合 |
| 5 | dequeue がない | — | 消費側の順序づけが未定義 |

**修正方針：** 各セルに `atomic_size_t seq`（世代番号）を持たせる、有界 MPMC 方式です。初期状態は `seq = i`、`enq_pos = deq_pos = 0` です。生産者は「セルの seq が自分の位置 pos と等しい（このセルは pos 番の書き込みに空いている）」ことを確認してから、`enq_pos` を pos → pos+1 に CAS して pos 番を独占します。値を書いた後、`seq = pos + 1` を release で公開します。消費者は「`seq == pos + 1`（pos 番の値が公開済み）」を確認してから `deq_pos` を CAS で独占し、値を読んで `seq = pos + 256`（次の周回の生産者に空いた）を release で書きます。mutex・spinlock・condvar・ヒープは使っていません。

#### 2. 完全な修正版Cコード

```c
/*
 * Q10: bounded lock-free MPMC ring queue (capacity 256, uint64_t, C11 atomics only)
 *
 * Every cell carries an atomic sequence number that encodes its generation:
 *   seq == pos          cell is free for the enqueue holding ticket `pos`
 *   seq == pos + 1      cell holds the value of ticket `pos`, ready for dequeue
 *   seq == pos + 256    (after dequeue) free for ticket pos + 256 (next lap)
 * Producers claim a ticket by CAS on enq_pos, consumers by CAS on deq_pos.
 * Assumption (stated by the exam): fewer than 1e8 operations per run, so the
 * size_t counters never wrap; the comparisons below are nevertheless written
 * in a wrap-tolerant way (distance computed modulo 2^N).
 *
 * Original implementation by ★★FREYJA for the C hell-level exam.
 */
#include <stdatomic.h>
#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>

/* ------------------------------------------------------------------ API */

#define MPMC_CAPACITY 256u

typedef struct {
    atomic_size_t seq;
    uint64_t      value;      /* plain memory, protected by the seq protocol */
} mpmc_cell;

typedef struct {
    _Alignas(64) atomic_size_t enq_pos;
    _Alignas(64) atomic_size_t deq_pos;
    _Alignas(64) mpmc_cell     cell[MPMC_CAPACITY];
} mpmc_queue;

void mpmc_init(mpmc_queue *q);                          /* not concurrent with other calls */
bool mpmc_try_enqueue(mpmc_queue *q, uint64_t v);       /* false: full (or head slot still being consumed) */
bool mpmc_try_dequeue(mpmc_queue *q, uint64_t *out);    /* false: empty (or head slot still being published); *out untouched */
bool mpmc_is_lock_free(const mpmc_queue *q);

/* ------------------------------------------------------- implementation */

_Static_assert((MPMC_CAPACITY & (MPMC_CAPACITY - 1u)) == 0, "capacity must be a power of two");
_Static_assert(sizeof(size_t) >= 8, "64-bit size_t required by the exam");

void mpmc_init(mpmc_queue *q)
{
    for (size_t i = 0; i < MPMC_CAPACITY; ++i) {
        atomic_init(&q->cell[i].seq, i);
        q->cell[i].value = 0;
    }
    atomic_init(&q->enq_pos, (size_t)0);
    atomic_init(&q->deq_pos, (size_t)0);
}

bool mpmc_is_lock_free(const mpmc_queue *q)
{
    return atomic_is_lock_free(&q->enq_pos) && atomic_is_lock_free(&q->cell[0].seq);
}

/* "a is before b" for free-running counters: (b - a) taken modulo 2^N is in (0, 2^(N-1)) */
static bool seq_before(size_t a, size_t b) { return (size_t)(b - a) - 1u < (SIZE_MAX >> 1); }

bool mpmc_try_enqueue(mpmc_queue *q, uint64_t v)
{
    size_t pos = atomic_load_explicit(&q->enq_pos, memory_order_relaxed);
    for (;;) {
        mpmc_cell *c = &q->cell[pos & (MPMC_CAPACITY - 1u)];
        /* acquire: pairs with the consumer's release that freed this cell, so the
         * consumer's read of the previous value happens-before our write. */
        size_t seq = atomic_load_explicit(&c->seq, memory_order_acquire);
        if (seq == pos) {
            /* claim ticket `pos`; on failure `pos` is reloaded with the current value */
            if (atomic_compare_exchange_weak_explicit(&q->enq_pos, &pos, pos + 1u,
                                                      memory_order_relaxed, memory_order_relaxed)) {
                c->value = v;
                /* release: publishes the value together with the new generation */
                atomic_store_explicit(&c->seq, pos + 1u, memory_order_release);
                return true;
            }
        } else if (seq_before(seq, pos)) {
            /* cell still holds the value of ticket pos - 256 (not yet dequeued): full */
            return false;
        } else {
            /* another producer already took this ticket: catch up */
            pos = atomic_load_explicit(&q->enq_pos, memory_order_relaxed);
        }
    }
}

bool mpmc_try_dequeue(mpmc_queue *q, uint64_t *out)
{
    size_t pos = atomic_load_explicit(&q->deq_pos, memory_order_relaxed);
    for (;;) {
        mpmc_cell *c = &q->cell[pos & (MPMC_CAPACITY - 1u)];
        /* acquire: pairs with the producer's release store of seq = pos + 1 */
        size_t seq = atomic_load_explicit(&c->seq, memory_order_acquire);
        size_t ready = pos + 1u;
        if (seq == ready) {
            if (atomic_compare_exchange_weak_explicit(&q->deq_pos, &pos, pos + 1u,
                                                      memory_order_relaxed, memory_order_relaxed)) {
                uint64_t v = c->value;
                /* release: our read of value happens-before the next producer's write */
                atomic_store_explicit(&c->seq, pos + MPMC_CAPACITY, memory_order_release);
                *out = v;
                return true;
            }
        } else if (seq_before(seq, ready)) {
            /* ticket `pos` not yet published: queue empty (or enqueue in progress) */
            return false;
        } else {
            pos = atomic_load_explicit(&q->deq_pos, memory_order_relaxed);
        }
    }
}
```

#### 3. ビルド方法・使用環境

```sh
# PowerShell 7 で MSVC 開発環境（vcvars64.bat）を読み込んだ後、build.ps1 が次の4構成を順にビルド・実行する
# q10_test.c は #include "q10.c" により実装と同じ翻訳単位でビルドする
W='-Wall -Wextra -Wpedantic -Wshadow -Wconversion -Wsign-conversion -Wcast-qual -Wstrict-prototypes -Wmissing-prototypes -Wvla -Wformat=2'
# (1) Clang -O2
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O2 -fuse-ld=link q10_test.c -o q10_rel.exe
# (2) Clang + AddressSanitizer（MSVC 同梱の ASan ランタイムでリンク）
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O1 -g -fsanitize=address -fno-omit-frame-pointer -fms-runtime-lib=dll -c q10_test.c -o q10_asan.obj
link /nologo /DEBUG /OUT:q10_asan.exe q10_asan.obj clang_rt.asan_dynamic-x86_64.lib clang_rt.asan_dynamic_runtime_thunk-x86_64.lib msvcrt.lib ucrt.lib vcruntime.lib legacy_stdio_definitions.lib
# (3) Clang + UndefinedBehaviorSanitizer（トラップ方式）
clang --target=x86_64-pc-windows-msvc -std=c17 $W -Ishim -D_CRT_SECURE_NO_WARNINGS -O1 -g -fsanitize=undefined -fsanitize-trap=undefined -fno-sanitize-recover=all -fuse-ld=link q10_test.c -o q10_ubsan.exe
# (4) MSVC（2つ目のコンパイラ）
cl /nologo /std:c17 /W4 /experimental:c11atomics /utf-8 /O2 /D_CRT_SECURE_NO_WARNINGS q10_test.c /Fe:q10_msvc.exe
# 各 .exe を実行し、終了コード 0 と "RESULT: N checks passed, 0 failed" を確認
# 追加（コンパイルのみ・実行は未実施）: 弱いメモリ順序の aarch64 向け
clang --target=aarch64-linux-android34 -std=c17 -Wall -Wextra -Wpedantic -Wshadow -Wconversion -Wsign-conversion -O2 q10_test.c -o q10_test_aarch64
```

**使用環境（全問共通）**

- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド、x86-64）
- コンパイラ1：**Clang 18.0.3**（Android NDK r27c に同梱。ターゲットは `x86_64-pc-windows-msvc`。ヘッダー・ライブラリは MSVC 14.52.37025 と Windows SDK/UCRT 10.0.26100、リンカーは link.exe）。`-std=c17` を指定。
- コンパイラ2：**MSVC cl 19.52.37029**（Visual Studio 18 Community）、`/std:c17 /W4`。
- GCC：この PC にはなく、使用していません。Linux／WSL の環境もありません（外部パッケージの無断導入禁止のため、導入していません）。
- AddressSanitizer：Clang でインストルメントし、MSVC 同梱の ASan ランタイム（`clang_rt.asan_dynamic-x86_64`）でリンクしました。ヒープ境界外書き込みの陽性対照で、`heap-buffer-overflow` を検出することを確認済みです。
- UndefinedBehaviorSanitizer：`-fsanitize=undefined -fsanitize-trap=undefined`（トラップ方式。ランタイムを使わず、未定義動作で不正命令の例外として異常終了します）。符号付きオーバーフローの陽性対照で、異常終了することを確認済みです。
- LeakSanitizer：Windows 版 ASan にないため未実施です。リークは各問の計数アロケーター（確保と解放の数の差）で検査しました。ThreadSanitizer：Windows では使えないため未実施です。
- ビルド補助 `shim/stdatomic.h`（製品コード外）：NDK の Clang に付属する `stdatomic.h` は Android（bionic）専用で Windows ではコンパイルできないため、MSVC の C11 実装 `vcruntime_c11_stdatomic.h`（Clang 互換の `__c11_atomic` 組み込み関数を使用）を読み込む1ファイルを置きました。Clang 18 が `__typeof_unqual__` に対応していないため、`atomic_is_lock_free` だけは `__c11_atomic_is_lock_free` で定義し直しています。
- `-D_CRT_SECURE_NO_WARNINGS`：MSVC の CRT が出す「`fopen` などの代わりに `_s` 版を使うように」という非推奨の通知だけを止める定義です（C17 標準関数の使用に対する通知であり、コードの欠陥を隠すものではありません）。

- Q10 の結果：Clang の全構成で警告0件。MSVC は C4324（`_Alignas(64)` による意図したパディングの通知）のみ。初回の MSVC ビルドでは、テストコードの `free(seen)` に C4090（`_Atomic` 修飾の破棄）が出たため、`free((void *)seen)` に直して解消しました。このテストを rel と MSVC で再実行し、540/540 合格を確認しています。aarch64 向けのビルドも成功しました。
- この環境の `atomic_is_lock_free(&enq_pos)` は true（T0 で確認）、`sizeof(size_t)` は 8 です。MSVC のヘッダーでは `ATOMIC_LLONG_LOCK_FREE` が 1（「場合により lock-free」）と定義されていますが、実行時の判定は true でした。

#### 4. テストコード

```c
/* Q10 tests. Threads: ISO C11 <threads.h> (POSIX pthread would be equivalent). */
#include "q10.c"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <threads.h>
#include <time.h>

static int g_fail = 0, g_pass = 0;
#define CHECK(cond, ...) do { if (cond) { ++g_pass; } else { ++g_fail; \
    printf("  FAIL %s:%d: ", __FILE__, __LINE__); printf(__VA_ARGS__); printf("\n"); } } while (0)
static void report(const char *name, int before_fail)
{
    printf("[%s] %s\n", g_fail == before_fail ? "PASS" : "FAIL", name);
}

static mpmc_queue Q;

/* ---------- concurrent harness ---------- */
#define MAXT 16
typedef struct {
    int id, nprod;
    uint64_t per;                 /* items per producer */
    uint64_t total;
    atomic_int *go;
    atomic_ullong *consumed;
    atomic_uchar *seen;           /* seen[p * per + i] counts deliveries */
    uint64_t fails;               /* failed try_* calls (full / empty) */
    uint64_t out_of_range, order_violations, got;
} targ;

static int producer(void *a)
{
    targ *t = (targ *)a;
    while (!atomic_load_explicit(t->go, memory_order_acquire)) thrd_yield();
    for (uint64_t i = 0; i < t->per; ++i) {
        uint64_t v = ((uint64_t)t->id << 32) | i;
        while (!mpmc_try_enqueue(&Q, v)) { t->fails++; if ((t->fails & 1023u) == 0) thrd_yield(); }
    }
    return 0;
}

static int consumer(void *a)
{
    targ *t = (targ *)a;
    uint64_t last[MAXT];
    for (int p = 0; p < MAXT; ++p) last[p] = UINT64_MAX;      /* "nothing seen yet" */
    while (!atomic_load_explicit(t->go, memory_order_acquire)) thrd_yield();
    for (;;) {
        if (atomic_load_explicit(t->consumed, memory_order_relaxed) >= t->total) break;
        uint64_t v = 0xFFFFFFFFFFFFFFFFu;
        if (!mpmc_try_dequeue(&Q, &v)) {
            if (v != 0xFFFFFFFFFFFFFFFFu) t->out_of_range++;   /* failed dequeue wrote its output */
            t->fails++;
            if ((t->fails & 1023u) == 0) thrd_yield();
            continue;
        }
        atomic_fetch_add_explicit(t->consumed, 1u, memory_order_relaxed);
        uint64_t p = v >> 32, i = v & 0xFFFFFFFFu;
        if (p >= (uint64_t)t->nprod || i >= t->per) { t->out_of_range++; continue; }
        /* per-producer FIFO as observed by one consumer: strictly increasing */
        if (last[p] != UINT64_MAX && i <= last[p]) t->order_violations++;
        last[p] = i;
        atomic_fetch_add_explicit(&t->seen[p * t->per + i], (unsigned char)1, memory_order_relaxed);
        t->got++;
    }
    return 0;
}

typedef struct { double secs; uint64_t dup, missing, oor, order, pfails, cfails; uint64_t got; } result;

/* order: 0 producers first, 1 consumers first, 2 interleaved */
static int run(int np, int nc, uint64_t per, int order, result *r)
{
    mpmc_init(&Q);
    uint64_t total = per * (uint64_t)np;
    atomic_uchar *seen = (atomic_uchar *)calloc((size_t)total, sizeof(atomic_uchar));
    if (!seen) return -1;
    for (uint64_t k = 0; k < total; ++k) atomic_init(&seen[k], (unsigned char)0);
    atomic_int go; atomic_init(&go, 0);
    atomic_ullong consumed; atomic_init(&consumed, 0ull);
    targ P[MAXT], C[MAXT];
    thrd_t tp[MAXT], tc[MAXT];
    for (int i = 0; i < np; ++i) { memset(&P[i], 0, sizeof P[i]); P[i].id = i; P[i].nprod = np; P[i].per = per; P[i].total = total; P[i].go = &go; P[i].consumed = &consumed; P[i].seen = seen; }
    for (int i = 0; i < nc; ++i) { memset(&C[i], 0, sizeof C[i]); C[i].id = i; C[i].nprod = np; C[i].per = per; C[i].total = total; C[i].go = &go; C[i].consumed = &consumed; C[i].seen = seen; }
    int ok = 1;
    if (order == 1) { for (int i = 0; i < nc; ++i) ok &= thrd_create(&tc[i], consumer, &C[i]) == thrd_success; for (int i = 0; i < np; ++i) ok &= thrd_create(&tp[i], producer, &P[i]) == thrd_success; }
    else if (order == 0) { for (int i = 0; i < np; ++i) ok &= thrd_create(&tp[i], producer, &P[i]) == thrd_success; for (int i = 0; i < nc; ++i) ok &= thrd_create(&tc[i], consumer, &C[i]) == thrd_success; }
    else { for (int i = 0; i < (np > nc ? np : nc); ++i) { if (i < nc) ok &= thrd_create(&tc[i], consumer, &C[i]) == thrd_success; if (i < np) ok &= thrd_create(&tp[i], producer, &P[i]) == thrd_success; } }
    if (!ok) { free((void *)seen); return -1; }
    struct timespec t0, t1;
    timespec_get(&t0, TIME_UTC);
    atomic_store_explicit(&go, 1, memory_order_release);
    for (int i = 0; i < np; ++i) thrd_join(tp[i], NULL);
    for (int i = 0; i < nc; ++i) thrd_join(tc[i], NULL);
    timespec_get(&t1, TIME_UTC);
    memset(r, 0, sizeof *r);
    r->secs = (double)(t1.tv_sec - t0.tv_sec) + (double)(t1.tv_nsec - t0.tv_nsec) / 1e9;
    for (uint64_t k = 0; k < total; ++k) {
        unsigned char s = atomic_load(&seen[k]);
        if (s == 0) r->missing++;
        else if (s > 1) r->dup += s - 1u;
    }
    for (int i = 0; i < nc; ++i) { r->oor += C[i].out_of_range; r->order += C[i].order_violations; r->cfails += C[i].fails; r->got += C[i].got; }
    for (int i = 0; i < np; ++i) r->pfails += P[i].fails;
    uint64_t v;
    int empty_at_end = !mpmc_try_dequeue(&Q, &v);
    free((void *)seen);
    return (r->dup == 0 && r->missing == 0 && r->oor == 0 && r->order == 0 && r->got == total && empty_at_end &&
            atomic_load(&Q.enq_pos) == total && atomic_load(&Q.deq_pos) == total) ? 0 : 1;
}

int main(void)
{
    int f0;
    const uint64_t SENT = 0x0123456789ABCDEFu;

    /* T0: environment */
    f0 = g_fail;
    mpmc_init(&Q);
    CHECK(mpmc_is_lock_free(&Q), "atomic size_t is lock-free");
    CHECK(sizeof(size_t) == 8, "64-bit size_t");
    printf("  ATOMIC_LLONG_LOCK_FREE=%d sizeof(size_t)=%zu\n", (int)ATOMIC_LLONG_LOCK_FREE, sizeof(size_t));
    report("T0 size_t atomics are lock-free on this platform", f0);

    /* T1: empty / full / FIFO / several laps (single thread) */
    f0 = g_fail;
    {
        mpmc_init(&Q);
        uint64_t v = SENT;
        CHECK(!mpmc_try_dequeue(&Q, &v) && v == SENT, "empty dequeue fails, output untouched");
        for (uint64_t i = 0; i < 256; ++i) CHECK(mpmc_try_enqueue(&Q, i + 100), "enqueue %llu", (unsigned long long)i);
        CHECK(!mpmc_try_enqueue(&Q, 9), "257th enqueue fails (full)");
        for (uint64_t i = 0; i < 256; ++i) { v = SENT; CHECK(mpmc_try_dequeue(&Q, &v) && v == i + 100, "FIFO %llu", (unsigned long long)i); }
        v = SENT;
        CHECK(!mpmc_try_dequeue(&Q, &v) && v == SENT, "empty again");
        uint64_t in = 0, out = 0;
        for (int lap = 0; lap < 40; ++lap) {
            int k = 1 + (lap * 37) % 256;
            for (int j = 0; j < k; ++j) if (mpmc_try_enqueue(&Q, in)) ++in;
            for (int j = 0; j < k - lap % 3; ++j) { if (mpmc_try_dequeue(&Q, &v)) { if (v != out) CHECK(0, "lap order"); ++out; } }
        }
        while (mpmc_try_dequeue(&Q, &v)) { if (v != out) CHECK(0, "drain order"); ++out; }
        CHECK(in == out && in > 2560, "many laps, every value exactly once (%llu)", (unsigned long long)in);
    }
    report("T1 empty / full / FIFO / ring laps", f0);

    /* T2: concurrent runs */
    f0 = g_fail;
    {
        struct { int np, nc; uint64_t per; int order; int reps; } cfg[] = {
            { 1, 1, 1000000, 0, 2 },
            { 4, 4, 250000, 0, 4 }, { 4, 4, 250000, 1, 4 }, { 4, 4, 250000, 2, 4 },
            { 8, 8, 125000, 0, 2 }, { 8, 8, 125000, 1, 2 }, { 8, 8, 125000, 2, 2 },
            { 8, 1, 125000, 2, 1 }, { 1, 8, 1000000, 2, 1 },
        };
        static const char *on[3] = { "producers first", "consumers first", "interleaved" };
        for (size_t c = 0; c < sizeof cfg / sizeof cfg[0]; ++c)
            for (int rep = 0; rep < cfg[c].reps; ++rep) {
                result r;
                int rc = run(cfg[c].np, cfg[c].nc, cfg[c].per, cfg[c].order, &r);
                CHECK(rc == 0, "%dP/%dC run %d", cfg[c].np, cfg[c].nc, rep);
                printf("  %dP x %dC, %llu items (%s, run %d): %.3fs dup=%llu missing=%llu out_of_range=%llu order_violations=%llu full_retries=%llu empty_retries=%llu -> %s\n",
                       cfg[c].np, cfg[c].nc, (unsigned long long)(cfg[c].per * (uint64_t)cfg[c].np), on[cfg[c].order], rep, r.secs,
                       (unsigned long long)r.dup, (unsigned long long)r.missing, (unsigned long long)r.oor, (unsigned long long)r.order,
                       (unsigned long long)r.pfails, (unsigned long long)r.cfails, rc == 0 ? "OK" : "ERROR");
            }
    }
    report("T2 1:1, 4:4 (4 x 250000 = 1,000,000), 8:8, 8:1, 1:8 - no duplicate / loss / out-of-range, per-producer order", f0);

    printf("RESULT: %d checks passed, %d failed\n", g_pass, g_fail);
    return g_fail ? 1 : 0;
}
```

#### 5. 検証結果（期待値と実測値を区別）

4構成とも `RESULT: 540 checks passed, 0 failed`、終了コード 0 でした。下表の時間は Clang -O2 版の実測値です。全構成の全実行で、重複・欠損・範囲外・順序違反はいずれも0でした。各生産者は値 `(生産者ID << 32) | 連番` を投入します。各消費者は受け取った値ごとに `seen[ID × 件数 + 連番]` を数え、消費者ごとに生産者別の連番が単調増加しているかを検査しました。成功した件数だけを数え、失敗した試行は full_retries / empty_retries として別に記録しています。

| テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| lock-free 性（T0） | `atomic_is_lock_free` が true、size_t は64ビット | true、8バイト | 実施（4構成） |
| 空（T1） | dequeue は false、出力先は番兵値のまま | 一致 | 実施 |
| 満杯（T1） | 256件成功、257件目は false | 一致 | 実施 |
| FIFO・周回（T1） | 1スレッドで40周（2,560件超）しても順序どおり、全件ちょうど1回 | 一致 | 実施 |
| 1対1、100万件 ×2回 | 重複0・欠損0・範囲外0・順序違反0 | 0.055秒 / 0.061秒、すべて0 | 実施 |
| **4対4、各生産者25万件 = 合計100万件**（生産者を先に起動 ×4、消費者を先に起動 ×4、交互に起動 ×4） | 同上。最終的な enq_pos = deq_pos = 1,000,000、キューは空 | 12回すべて0、0.143〜0.158秒 | 実施 |
| 8対8、各12.5万件 = 100万件（3通りの起動順 ×2回） | 同上 | 6回すべて0、0.159〜0.166秒 | 実施 |
| 8対1、1対8（大量の競合と偏り） | 同上 | すべて0（満杯の再試行 720万回、空の再試行 289万回の偏った条件を含む） | 実施 |
| 失敗した dequeue が出力先を書き換えない（並列中） | 番兵値が変わらない | 違反0 | 実施 |
| 複数回実行 | 上記の22回 × 4構成 = 88回 | すべて OK | 実施 |
| ThreadSanitizer | — | Windows（MSVC ターゲット）では TSan が使えない | 未実施 |
| 弱いメモリ順序の CPU（ARM）での実行 | — | aarch64 向けのビルドのみ成功、実機なし | 未実施 |

#### 6. 並列動作の正当性の説明

**lock-freeの進行保証・線形化点・happens-beforeの根拠：**

1. **セルの世代（ABA 相当の対策）**
   pos 番のチケットが使うセルは `cell[pos % 256]` です。セルの seq は、「pos（pos 番の書き込み待ち）」→「pos+1（pos 番の値が入っている）」→「pos+256（次の周回の pos+256 番の書き込み待ち）」と、単調に増えます。seq は周回ごとに異なる値をとるため、古い pos を持った遅れたスレッドがセルを誤って使うことはありません（`seq == pos` と `seq == pos+1` の一致判定が世代の照合を兼ねます）。カウンターは64ビットで、試験中の操作数は1億未満なので周回しません（問題文で認められた前提）。比較は、それでも 2^N を法とした距離で書いています。
2. **チケットの独占（重複・消失がない理由）**
   `enq_pos` を pos → pos+1 に CAS できるのは、各 pos について1スレッドだけです（値は単調増加し、同じ値に戻らないため）。したがって、各チケットの書き込み手は1人です。消費側も同様で、`deq_pos` の CAS によって各チケットの読み手は1人です。消費者は `seq == pos+1`（公開済み）を確認してからでないと pos 番を取らないので、未公開のチケットを飛ばすことはありません。生産者は `seq == pos`（前の周回の値が読まれた後）でないと書かないので、未読の値を上書きしません。よって重複・消失・破損は起きません。
3. **happens-before**
   - 生産者：`value = v`（非atomic）→ `seq = pos+1` の release の store。消費者：`seq` の acquire の load が pos+1 を読み、その後で `value` を読む。release の store とそれを読む acquire の load は synchronizes-with の関係なので、値の書き込みは読み取りに happens-before します。
   - 消費者：`value` の読み取り → `seq = pos+256` の release の store。次の周回の生産者：acquire の load が pos+256（＝自分の pos）を読んでから書き込む。読み取りは上書きに happens-before します。
   - CAS 自体は relaxed で十分です。値へのアクセスは、すでに acquire の load の後（sequenced-after）にあり、順序づけは seq の release/acquire が担うからです。CAS が守るのはチケットの排他性だけです。
   - 以上より、同じ `value` に対する競合しうるアクセスはすべて happens-before で順序づけられ、データ競合はありません。
4. **線形化点**
   - 成功した enqueue：`enq_pos` の CAS が成功した時点（チケット順が FIFO の順序を決めます）。値が見えるようになるのは seq の release の store の時点です。
   - 成功した dequeue：`deq_pos` の CAS が成功した時点。
   - 失敗した enqueue（満杯）：`seq` の acquire の load が「seq が pos より前」（セルにまだ pos−256 番の値が残っている）を観測した時点。
   - 失敗した dequeue（空）：`seq` の acquire の load が「seq == pos」（pos 番の値が未公開）を観測した時点。
   - **正直な注記：** 予約（CAS）と公開（seq の store）の間に別の生産者がいる場合、それより後のチケットがすでに公開済みでも、try_dequeue は先頭のセルが未公開であるため false を返します。この false は「空、または先頭の書き込みが進行中」という意味です。つまり false を返すのは「空」のときに限りません。これはスロットごとの sequence 方式（Vyukov 型）に共通する性質で、厳密に「空」と線形化できるのは、進行中の enqueue がない場合です。try_enqueue の false も同様です。成功した操作どうしは、チケット順の FIFO として線形化可能です。問題文が「投入失敗は再試行可能」としているので、失敗を再試行する使い方で全件が正しく届くことを T2 で確認しました。
5. **lock-free の進行保証（システム全体）**
   ループが返らずに繰り返すのは、(a) CAS が失敗したとき、(b) `seq` が pos より先（他のスレッドがそのチケットをすでに取った）で位置を読み直すとき、の2つだけです。(a) は、他のスレッドの CAS が成功してカウンターを進めた場合にだけ起きます。(b) も他のスレッドの成功を意味します。したがって、繰り返しが続くのは他の操作が完了に向けて進んでいる間だけで、システム全体として lock-free です。どのスレッドも他のスレッドを待つループを持たず、満杯・空では即座に false を返します。個々のスレッドの無飢餓は保証しません（問題文どおり）。`compare_exchange_weak` の偽の失敗は LL/SC 型の CPU で起こり得ますが、これは他のスレッドの干渉なしには続かないという通常のハードウェアの前提に依存します。
   注意点として、予約と公開の間でスレッドが長時間停止すると、そのセルは公開されないままです。他の消費者の try_dequeue は false を返し続け（ブロックはしない）、生産者はそのセルまで周回すると満杯を返します。呼び出しがブロックすることはありませんが、そのセルの処理は停止したスレッドの再開を待つことになります。これはこの方式の既知の制約です。

#### 7. 未実施項目・制約・残課題

- ThreadSanitizer は未実施です（Windows では利用不可、Linux 環境なし）。
- テストを実行した CPU は x86-64（TSO、4コア8スレッド）です。弱いメモリ順序での動作は、6. の形式的な論証と aarch64 向けのビルド成功によるもので、ARM 実機での実行はしていません。
- 制約：false は「空・満杯」または「先頭セルの処理が進行中」を意味します（6. の 4.）。カウンターの周回は、問題文の前提（1億操作未満）により考慮していません。比較は周回に耐える形で書いていますが、周回を伴う試験はしていません。

---

## 最終提出レポート（★★FREYJAが回答する欄）

- Q01〜Q10の対応状況：全10問に回答しました。各問に、根本原因・危険性、修正版Cコードの全文、ビルド手順、実際に使ったテストコード、期待値と実測値を分けた検証結果、未実施項目を記入しています。Q06 の実POSIX経路は、Linux 環境がないためコンパイル・リンクの確認までで、実行は未実施です。
- 実際にビルド成功した問題番号：Q01〜Q10 の全問（Clang 18 の -O2・ASan・UBSan の3構成）。MSVC 19.52 では Q01・Q03〜Q10 が成功しました。Q02 は MSVC の C モードが `max_align_t` を定義していないため（コンパイラ側の非準拠）、MSVC だけビルドできません。Q06 の実POSIX部分と POSIX 統合テストは、Android NDK（bionic、x86_64・aarch64）向けにコンパイル・リンクに成功しました。Q03・Q10 のテストも aarch64 向けにビルドできました（いずれも実行は未実施）。
- 実際に実行したテスト数・内容・結果：全問の結果は、Clang（-O2）・Clang+ASan・Clang+UBSan・MSVC の各構成ですべて「0 failed」・終了コード 0 でした（Q02 は MSVC を除く3構成）。各問の検査件数（1構成あたり）は Q01 700、Q02 36,999、Q03 8,619、Q04 334、Q05 46、Q06 143、Q07 36、Q08 90、Q09 56、Q10 540 です。主な内容は以下のとおりです。
  - Q01：全2分割・全3分割 13,202 通り、1ビット反転 1,096 通り
  - Q02：決定的ランダム操作 20,000 回と参照モデルの比較
  - Q03：2スレッド・1000万件 × 4回（周回を含む）
  - Q04：参照実装との比較 19,535,232 系列、全スカラー値の往復
  - Q05：BigInteger の参照ベクトル 45,354 件、ランダム 500万件
  - Q06：模擬ファイルシステムで障害注入 8 種類、クラッシュ点 72 か所
  - Q07：V=100000・E=400000、ランダムグラフ 2万個
  - Q08：ランダム操作 60万回（4方式のハッシュ）
  - Q09：全分割 903 通り、往復 3,000 文書、ランダム 2万文書、40万レコード
  - Q10：4対4・100万件 × 12回を含む並列実行 22回
- 検出した不具合と修正内容：
  - 製品コード：Q06 の POSIX 部分で、`renameat` を宣言する `<stdio.h>` を読み込んでいませんでした。Android 向けのコンパイルで検出し、追加して修正しました。
  - テストコード：Q01（空フレーム長の期待値 9 → 8、確保回数の表示）、Q04（スカラー数の数え間違い 10 → 9）、Q09（手で数えた文書長と 65536 バイトの埋め不足）、Q10（MSVC C4090 に対するキャスト）の誤りを修正しました。実行前のレビューでは、Q02 の失敗テストの条件（60000 → 65000）と、Q08 のキー生成が無限ループし得る点も修正しています。
  - 環境：ビルドスクリプトで関数の出力が戻り値に混入する誤りを修正しました。また、NDK の Clang に付属する bionic 専用の `stdatomic.h` の代わりに、MSVC の C11 実装を使うビルド補助ヘッダーを追加しました（製品コード外）。
- 修正後の再テスト結果：修正した問題について、該当する構成を再実行し、全件合格しました。Q06 の Windows 側テストは、修正が `SAVE_NO_POSIX` で除外される部分だけなので再実行していません（Android 向けに再コンパイルして成功・警告0件を確認）。また、Q06 の検査の検出力を、fsync を削除した変異版で確認しました（変異版で検査が失敗）。
- 未解決・未実装・未検証項目：
  - GCC と Linux でのビルド・実行（環境なし。WSL 未導入で、外部パッケージの無断導入を禁止されているため導入していません）
  - Q06 の実ファイルシステムでの実行・SIGKILL によるクラッシュ試験・実機の停電試験。クラッシュ耐性はモデル上の検証で、保証済みとはしていません
  - ThreadSanitizer（Windows では利用不可）、LeakSanitizer（Windows の ASan にはないため、計数アロケーターで代替）
  - ARM 実機での Q03・Q10 の実行（x86-64 の TSO 上でのみ実行）
  - Q04 の4バイト列の完全な総当たり（一部とランダムで代替）
  - Q10 の try 系の false は「空・満杯」に加えて「先頭セルの処理が進行中」も含みます（Q10 の 6. に明記）
- ユーザーのPC操作を占有するGUIテストの実施有無（本試験では原則不要）：実施していません。テストはすべてコンソールのプログラムで、ファイル操作のテスト（Q06）はメモリ上の模擬ファイルシステムだけを使いました。実ファイルの作成・変更・削除はしていません。
- 第三者コード・ライブラリ利用と著作権・ライセンス確認結果：
  - 解答コードとテストコードはすべて独自に実装しました。第三者のコードの複製はしていません。
  - 外部ライブラリは使っていません。使ったのは C17 標準ライブラリ、ISO C11 の `<threads.h>`（Q03・Q10 のテスト）、POSIX API（Q06 の実装と統合テストだけ）です。
  - 参照した公開仕様は、RFC 3629（UTF-8）、Unicode 規格の表 3-7、RFC 4180（CSV）、IEEE CRC-32（多項式 0xEDB88320）、FNV-1a 64（Fowler/Noll/Vo の公開仕様と既知のテスト値）、C17 規格（6.5.7、5.1.2.4 など）、POSIX.1-2008 です。いずれも仕様として参照し、コードは書き写していません。Robin Hood ハッシュと backward-shift 削除、Kahn 法、スロットごとの sequence 番号による有界 MPMC キュー（D. Vyukov が公開した設計方式として知られるもの）は、広く知られたアルゴリズムの考え方を使い、実装は独自に書きました。
  - Q05 のテスト用の参照値の生成には、PowerShell 7 / .NET の `System.Numerics.BigInteger`（MIT ライセンスの .NET ランタイムの標準機能）を使いました。テストのためだけで、製品コードには含めていません。
  - リポジトリの著作権表示とライセンス（README.md、LICENSE.md、問題文の © 表示）は変更していません。
- GitHubへのcommit / pushの有無（無断実施禁止）：commit も push もしていません。回答はローカルの作業用クローンの試験ファイルに記入しただけです。GitHub への反映は、Phantom Nightmare の明示的な許可を得てから行います。
- 自己採点・自己合格判定は行わず、最終判定をPhantom Nightmareに委ねること：自己採点・自己合格判定はしていません。最終的な評価・採点・合否判定は Phantom Nightmare に委ねます。

**指定作業の完了後は結果を報告し、追加指示があるまで別作業を開始しない。**
