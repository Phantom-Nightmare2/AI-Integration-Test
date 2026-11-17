# ★★FREYJA（フレイア）C#・地獄級能力試験【全10問】

**受験AIエージェント：★★FREYJA（フレイア）**  
**試験責任者・最終評価者：Phantom Nightmare**  
**試験環境：.NET 10 / C# 14（`net10.0`、`LangVersion=14.0`）**  
**分野：高度なC#開発／非同期処理・メモリ寿命・スレッド安全性・障害耐性・データ整合性**  
**問題数：10問／各10点／合計100点**  
**著作権表示：© 2026 Phantom Nightmare. All rights reserved.**

## 0. 全問共通：クライアントオーダー

1. Q01〜Q10は**互いに独立した問題**。それぞれ提示された**危険な元コード**を分析し、問題ごとの要求仕様を満たす独自の修正版C#コードを設計・実装する。
2. **問題文・元コード・要求仕様・必須検証・採点基準を削除、変更、緩和しないこと。** 実際の解答は各問題に設置した **「★★FREYJAの回答【Qxx・記入欄】」** の内部だけに記載する。
3. 各問題の回答は、(A)根本原因・危険性、(B)**完全な修正版C#コード全文**、(C)実際のビルドと実行コマンド、(D)**実際に使用したテストコード全文**、(E)期待結果・実測結果・実施有無、(F)仕様への適合理由、(G)未解決・未検証事項、の7項目を必須とする。
4. **.NET 10 / C# 14** を対象とし、.NET標準ライブラリ・共有フレームワーク内のAPIのみを利用。第三者ライブラリ・NuGet追加・外部の完成コードのコピーは禁止。テストに必要な独自の偽オブジェクト・失敗注入・参照モデルは自作可能。
5. 各問は原則として単独のConsoleプロジェクトで検証可能にする。ビルド/テストのための一時ファイルは専用の作業領域に限る。既存プロジェクト、開発環境の設定、ユーザーデータ、実運用ファイルは変更禁止。
6. **元コードには未定義・危険な動作や未実装が含まれる**ため、元コードを危険な入力のまま実行する必要はない。修正版を実際にビルド・検証する。パフォーマンスや並行実行の結果を報告する場合はテストのOS・CPU・ランタイム・ビルド構成を併記する。
7. 各問で正しくキャンセル・失敗・Dispose・例外安全性を扱うこと。テストしていない機能は「未実施」、コード上未対応なら「未実装」と明記し、成功と偽らない。最終採点・合否判定は**Phantom Nightmare** が行い、受験者は自己採点しない。
8. **試験参加者は★★FREYJAのみ。** Claude Codeは試験参加者・共同解答者・採点者ではない。指定範囲を超える独断のタスク追加、指示の変更、CLAUDE.mdやエージェント設定の変更、他エージェントへの委譲は禁止。
9. 回答のための自律調査・設計・実装・不具合修正・必要なテストは許可する。ただし、無関係な調査・ファイル大量読込み・無制限再試行・成功済みの無関係なテストの再実行は禁止。
10. GUI自動テスト標準ルールは維持するが、**本試験はC#ライブラリ/Consoleのコード試験のためGUI操作を要しない**。ユーザーがPC操作中はマウス・キーボードを占有しない。
11. **GitHubへのcommit・pushはPhantom Nightmareによる別途の明示的な許可がある場合のみ。** 解答はこの試験ファイルの各回答欄に正確に記入する。ローカル完成とGitHub提出済みを混同しない。更新前後の差分で回答欄以外が不変か確認する。
12. 著作権・利用ライセンスを遵守し、独自実装を優先する。参考にした仕様・資料のURLを報告し、README・LICENSE・C/C++試験・他エージェントの資料を無断変更しない。

**採点基準：各問10点（原因分析と危険性2点／仕様を満たすコード6点／実際の検証2点）、合計100点。出題していない条件を後から追加して減点しない。**

参考となる公的仕様：
- C#言語バージョンと.NETの対応：https://learn.microsoft.com/ja-jp/dotnet/csharp/language-reference/language-versioning
- .NETバッファーAPI：https://learn.microsoft.com/ja-jp/dotnet/standard/io/buffers
- 実装・テストでは問題文が指定する各APIのMicrosoft Learn仕様を必要に応じて参照すること。

---

## Q01【地獄級】ReadOnlySequenceとストリーミングCRCフレームデコーダ

**試験の狙い：** ReadOnlySequence<byte>、分割受信、canonical ULEB128、CRC32、所有権、エラー状態管理を同時に扱う。

### 元コード（危険・不完全）

```csharp
using System;
using System.Buffers;
using System.IO;
class FrameBad {
    public byte[] Decode(ReadOnlySequence<byte> input) {
        var a = input.FirstSpan;           // 先頭セグメントしか読まない
        int len = a[2];                    // 入力長・境界の検査なし
        return a.Slice(3, len).ToArray();  // CRC・canonical判定なし
    }
}
```

### 要求仕様

1. `FrameDecoder` に `IReadOnlyList<byte[]> Feed(ReadOnlySequence<byte> bytes)`、`void Complete()`、`void Reset()` を実装。Feedは0個以上の**完了した**フレームを順序どおり返し、未完成の部分のみ内部に保持する。返す配列は独立した所有権を持ち、入力の再利用後も内容が変わらない。
2. ワイヤ形式は `[C5 A7] [len: canonical unsigned ULEB128] [payload: lenバイト] [crc32: u32 LE]`。lenは0〜65536、ULEB128は最短符号化で最大3バイト。CRCはULEB128の**符号化済み生バイト列とpayloadを連結**した範囲にIEEE CRC-32（poly 0xEDB88320、初期0xFFFFFFFF、最後にXOR）を適用する。
3. 完全な1フレームを処理後、直後の次フレームを同じFeedで解析する。magic不一致、過長ULEB128、4バイト目継続、len上限超過、CRC不一致は `InvalidDataException`。最初のエラー後はstickyな失敗状態とし、ResetまでFeed/Completeを拒否する。
4. Completeは正常なフレーム境界でのみ成功し、magic/長さ/payload/CRCの途中切れを `EndOfStreamException` で拒否する。CRCが間違っているフレームを勝手にスキップして再同期してはならない。
5. 各フレームのpayload以外に保持するパーサー状態はO(1)とし、未完成の最大payloadを含めても保持メモリはO(65536)。各入力セグメントを結合して丸ごとコピーする実装は禁止。例外時に所有バッファーを破棄・解放する。
6. 空のReadOnlySequence、1バイトずつのFeed、空セグメント、境界が任意の位置に分かれた複数セグメントを受け入れる。入力は呼び出しの終了後は保持しない。

### 必須検証

1. 既知のCRC32検査値（ASCII `123456789` → `0xCBF43926`）、空payload、1/127/128/16383/16384/65536バイト、既知の期待バイト列。
2. 各境界での2分割・3分割、ReadOnlySequenceの空/多数/極小セグメント、複数フレーム連結、全バイトを1つずつFeed。
3. 0x80 0x00の非canonical長、0x80 0x80 0x00、長さ65537、未知magic、CRCの全1ビット反転、全切断位置、stickyエラーとReset。
4. ランダムな有効・無効フレーム合計10万件と独立したバイト配列参照デコーダを比較。入力配列の書換え後も出力が不変であること。

### ★★FREYJAの回答【Q01・記入欄】

> **以下は★★FREYJA専用の解答領域。** 問題本文と元コードには触れず、7項目をすべて実際の回答で置き換える。未実施の検証は未実施と正直に記載する。

#### 1. 根本原因・危険性・影響

**元コード `FrameBad.Decode` の問題点**

| # | 箇所 | 根本原因・発生条件 | 実害 |
| --- | --- | --- | --- |
| 1 | `input.FirstSpan` | 複数セグメントの `ReadOnlySequence` のうち、先頭のセグメントしか見ない | 分割受信（ネットワークや Pipe では普通に起きる）で、データを取りこぼす・誤読する |
| 2 | `a[2]` | 長さを検査していない | 短い入力で `IndexOutOfRangeException`。長さを1バイトに限定しており、ULEB128 を解釈しない |
| 3 | `a.Slice(3, len)` | len がセグメントより長い場合を考えない | 例外、または途中までのデータ |
| 4 | magic・canonical・上限・CRC の検査なし | — | 破損・改ざん・非最短符号化のデータを受理する |
| 5 | 1フレームしか処理しない・状態を持たない | — | 連結されたフレームや、フレームの途中で切れた入力を扱えない |
| 6 | エラー状態の管理なし | — | 破損の後も読み続け、誤ったフレームを返し得る |

**修正方針：**
- 状態機械（Magic0 → Magic1 → Length → Payload → Crc）で、各セグメントを `foreach (ReadOnlyMemory<byte> seg in sequence)` により、その場で処理します。セグメントの結合や、入力全体のコピーはしません。
- payload は、長さが確定した時点で `new byte[len]`（最大 65536）を確保し、各セグメントから直接コピーします。この配列は、そのまま呼出し元に所有権ごと渡します（入力を再利用しても内容は変わりません）。
- ULEB128 はバイトごとに検査します。3バイト目でも継続ビットが立っていれば拒否、2バイト目以降の最終バイトが 0 なら非 canonical として拒否、値が 65536 を超えれば拒否です。CRC はバイトを受け取るたびに更新します。
- 最初のエラーで失敗状態を保持し（sticky）、作業中のバッファーは破棄します。その後の Feed / Complete は、Reset まで `InvalidOperationException` で拒否します。
- `InvalidDataException` は sealed で派生できません。そのため、エラー位置と、同じ Feed 内でエラーの前に完了したフレームは、`Exception.Data["Offset"]` / `Data["CompletedFrames"]` と、デコーダーの `FailureOffset` / `CompletedBeforeFailure` で取得できるようにしました。

#### 2. 完全な修正版C#コード

```csharp
// Q01: streaming CRC frame decoder over ReadOnlySequence<byte> (C# 14 / .NET 10)
// Wire format: C5 A7 | len (canonical ULEB128, 1..3 bytes, 0..65536) | payload | CRC-32 (u32 LE)
// CRC-32 (IEEE, reflected 0xEDB88320, init/xorout 0xFFFFFFFF) over the raw ULEB128 bytes + payload.
// Original implementation by ★★FREYJA for the C# hell-level exam.
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace Exam;

public static class Crc32Ieee
{
    static readonly uint[] Table = Build();
    static uint[] Build()
    {
        var t = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint c = i;
            for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? (c >> 1) ^ 0xEDB88320u : c >> 1;
            t[i] = c;
        }
        return t;
    }
    public static uint Update(uint reg, ReadOnlySpan<byte> data)
    {
        foreach (byte b in data) reg = Table[(reg ^ b) & 0xFF] ^ (reg >> 8);
        return reg;
    }
    public static uint Compute(ReadOnlySpan<byte> data) => Update(0xFFFFFFFFu, data) ^ 0xFFFFFFFFu;
}

public sealed class FrameDecoder
{
    public const int MaxPayload = 65536;
    const byte Magic0 = 0xC5, Magic1 = 0xA7;

    enum State { Magic0, Magic1, Length, Payload, Crc }

    State _state = State.Magic0;
    uint _len;            // payload length being decoded
    int _lebCount;        // ULEB128 bytes consumed
    uint _crc;            // running CRC register
    uint _crcRx;          // received CRC
    int _crcCount;
    byte[]? _payload;     // owned buffer of the frame in progress (<= 65536 bytes)
    int _got;
    long _offset;         // stream offset of the next byte
    Exception? _failure;  // sticky failure

    /// <summary>Stream offset of the byte that caused the first InvalidDataException (-1 if none).</summary>
    public long FailureOffset { get; private set; } = -1;
    /// <summary>Frames completed in the failing Feed call before the error (also in exception.Data["CompletedFrames"]).</summary>
    public IReadOnlyList<byte[]> CompletedBeforeFailure { get; private set; } = Array.Empty<byte[]>();

    /// <summary>Consumes all bytes; returns the frames completed by this call, in order.</summary>
    public IReadOnlyList<byte[]> Feed(ReadOnlySequence<byte> bytes)
    {
        if (_failure is not null) throw new InvalidOperationException("decoder is in the failed state; call Reset()", _failure);
        var done = new List<byte[]>();
        foreach (ReadOnlyMemory<byte> segment in bytes)
            Consume(segment.Span, done);            // segments are processed in place, never concatenated
        return done;
    }

    /// <summary>Succeeds only at a frame boundary.</summary>
    public void Complete()
    {
        if (_failure is not null) throw new InvalidOperationException("decoder is in the failed state; call Reset()", _failure);
        if (_state != State.Magic0)
        {
            var e = new EndOfStreamException($"input ended inside a frame ({_state}) at stream offset {_offset}");
            Fail(e);
            throw e;
        }
    }

    public void Reset()
    {
        _state = State.Magic0;
        _len = 0; _lebCount = 0; _crc = 0; _crcRx = 0; _crcCount = 0;
        _payload = null; _got = 0; _offset = 0; _failure = null;
        FailureOffset = -1; CompletedBeforeFailure = Array.Empty<byte[]>();
    }

    void Fail(Exception e)
    {
        _failure = e;
        _payload = null;                    // drop the owned buffer
    }

    InvalidDataException Error(string message, List<byte[]> done)
    {
        var e = new InvalidDataException($"{message} (stream offset {_offset})");
        var completed = done.ToArray();
        e.Data["Offset"] = _offset;
        e.Data["CompletedFrames"] = completed;
        FailureOffset = _offset;
        CompletedBeforeFailure = completed;
        Fail(e);
        return e;
    }

    void Consume(ReadOnlySpan<byte> s, List<byte[]> done)
    {
        int i = 0;
        while (i < s.Length)
        {
            switch (_state)
            {
                case State.Magic0:
                    if (s[i] != Magic0) throw Error("bad magic byte 0", done);
                    i++; _offset++; _state = State.Magic1;
                    break;
                case State.Magic1:
                    if (s[i] != Magic1) throw Error("bad magic byte 1", done);
                    i++; _offset++;
                    _state = State.Length; _len = 0; _lebCount = 0; _crc = 0xFFFFFFFFu;
                    break;
                case State.Length:
                {
                    byte b = s[i];
                    if ((b & 0x80) != 0 && _lebCount == 2) throw Error("ULEB128 longer than 3 bytes", done);
                    if ((b & 0x80) == 0 && _lebCount > 0 && b == 0) throw Error("non-canonical ULEB128 (trailing zero byte)", done);
                    uint v = _len | ((uint)(b & 0x7F) << (7 * _lebCount));      // shift <= 14
                    if ((b & 0x80) == 0 && v > MaxPayload) throw Error($"length {v} exceeds {MaxPayload}", done);
                    _crc = Crc32Ieee.Update(_crc, s.Slice(i, 1));
                    _len = v; _lebCount++; i++; _offset++;
                    if ((b & 0x80) == 0)
                    {
                        _crcRx = 0; _crcCount = 0; _got = 0;
                        _payload = new byte[_len];                 // owned, handed to the caller as-is
                        _state = _len == 0 ? State.Crc : State.Payload;
                    }
                    break;
                }
                case State.Payload:
                {
                    int take = Math.Min(s.Length - i, (int)_len - _got);
                    var part = s.Slice(i, take);
                    part.CopyTo(_payload.AsSpan(_got));
                    _crc = Crc32Ieee.Update(_crc, part);
                    _got += take; i += take; _offset += take;
                    if (_got == _len) _state = State.Crc;
                    break;
                }
                case State.Crc:
                    _crcRx |= (uint)s[i] << (8 * _crcCount);
                    _crcCount++;
                    if (_crcCount == 4)
                    {
                        if ((_crc ^ 0xFFFFFFFFu) != _crcRx) { throw Error("CRC mismatch", done); }
                        i++; _offset++;
                        done.Add(_payload!);
                        _payload = null;
                        _state = State.Magic0;
                    }
                    else { i++; _offset++; }
                    break;
            }
        }
    }
}
```

#### 3. ビルド方法・使用環境

```powershell
# 作業フォルダー: scratchpad\cs\q01（試験専用の一時領域）。cs\build.ps1 -Q q01 が次を順に実行する
# (1) 下記の q01.csproj を生成  (2) Release ビルド  (3) テスト実行（終了コード 0 = 全件合格）
dotnet build q01.csproj -c Release -nologo
dotnet bin\Release\net10.0\q01.dll
# 結果: ビルド成功・0 個の警告・0 エラー、実行の終了コード 0
#
# q01.csproj（全問共通。AssemblyName だけが問題ごとに異なる）
# <Project Sdk="Microsoft.NET.Sdk">
#   <PropertyGroup>
#     <OutputType>Exe</OutputType>
#     <TargetFramework>net10.0</TargetFramework>
#     <LangVersion>14.0</LangVersion>
#     <Nullable>enable</Nullable>
#     <ImplicitUsings>disable</ImplicitUsings>
#     <AnalysisLevel>latest</AnalysisLevel>
#     <AllowUnsafeBlocks>false</AllowUnsafeBlocks>
#     <InvariantGlobalization>true</InvariantGlobalization>
#     <AssemblyName>q01</AssemblyName>
#     <RootNamespace>Exam</RootNamespace>
#   </PropertyGroup>
# </Project>
```

**使用環境（全問共通）**

- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド）
- ランタイム：**Microsoft.NETCore.App 10.0.15（.NET 10）**。実行時に `Environment.Version` = 10.0.15 であることを確認しました。
- 言語とターゲット：`TargetFramework=net10.0`、`LangVersion=14.0`（C# 14。C# 14 の extension メンバーのコンパイルも確認済み）、`Nullable=enable`、`AnalysisLevel=latest`、`ImplicitUsings=disable`、`AllowUnsafeBlocks=false`、構成は **Release**。
- SDK：この PC にインストールされている SDK は **.NET SDK 11.0.100** だけです。.NET 10 用の参照パック（10.0.15）を使い、`net10.0` 向けにビルドしました。.NET 10 SDK そのものはインストールされておらず、無断導入の禁止に従って追加していません。そのため、コンパイラは SDK 11 に同梱のもので、言語バージョンを 14.0 に固定して使っています。
- NuGet パッケージ・第三者ライブラリ：使用していません（.NET 共有フレームワークの API だけ）。
- 各問は独立したコンソールプロジェクトです（`qNN.csproj` = 下記の内容、`QNN.cs` = 実装、`Tests.cs` = テスト）。ビルド時の警告は全問 **0 件**、テストの終了コードは全問 0 です。

#### 4. 実際に使用したテストコード

```csharp
// Q01 tests
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Exam;

static class T
{
    public static int Pass, Fail;
    static int _f0;
    public static void Check(bool ok, string what) { if (ok) Pass++; else { Fail++; Console.WriteLine($"  FAIL: {what}"); } }
    public static void Begin() => _f0 = Fail;
    public static void Report(string name) => Console.WriteLine($"[{(Fail == _f0 ? "PASS" : "FAIL")}] {name}");
}

sealed class Seg : ReadOnlySequenceSegment<byte>
{
    public Seg(ReadOnlyMemory<byte> m) { Memory = m; }
    public Seg Append(ReadOnlyMemory<byte> m) { var s = new Seg(m) { RunningIndex = RunningIndex + Memory.Length }; Next = s; return s; }
}

static class Program
{
    // ---------- helpers ----------
    static ReadOnlySequence<byte> Seq(params byte[][] parts)
    {
        if (parts.Length == 0) return ReadOnlySequence<byte>.Empty;
        var first = new Seg(parts[0]); var last = first;
        for (int i = 1; i < parts.Length; i++) last = last.Append(parts[i]);
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }
    static byte[] Leb(int v) { var l = new List<byte>(); do { byte b = (byte)(v & 0x7F); v >>= 7; if (v != 0) b |= 0x80; l.Add(b); } while (v != 0); return l.ToArray(); }
    // independent bitwise CRC
    static uint RefCrc(IEnumerable<byte> d) { uint c = 0xFFFFFFFF; foreach (byte b in d) { c ^= b; for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? (c >> 1) ^ 0xEDB88320 : c >> 1; } return ~c; }
    static byte[] Frame(byte[] payload, byte[]? lebOverride = null)
    {
        var leb = lebOverride ?? Leb(payload.Length);
        uint crc = RefCrc(leb.Concat(payload));
        return [0xC5, 0xA7, .. leb, .. payload, (byte)crc, (byte)(crc >> 8), (byte)(crc >> 16), (byte)(crc >> 24)];
    }
    static byte[] Pat(int n, int seed) { var b = new byte[n]; for (int i = 0; i < n; i++) b[i] = (byte)(i * 31 + seed * 7); return b; }

    // Independent reference: whole byte[] at once. Result: frames, and "" / "format" / "eof".
    static (List<byte[]> frames, string end) RefDecode(byte[] s)
    {
        var f = new List<byte[]>(); int i = 0;
        while (i < s.Length)
        {
            if (s[i] != 0xC5) return (f, "format"); if (++i == s.Length) return (f, "eof");
            if (s[i] != 0xA7) return (f, "format"); i++;
            int len = 0, k = 0; int lebStart = i;
            while (true)
            {
                if (i == s.Length) return (f, "eof");
                byte b = s[i];
                if (k == 2 && (b & 0x80) != 0) return (f, "format");
                if (k > 0 && b == 0) return (f, "format");
                len |= (b & 0x7F) << (7 * k); k++; i++;
                if ((b & 0x80) == 0) break;
            }
            if (len > 65536) return (f, "format");
            int lebEnd = i;
            if (s.Length - i < len) return (f, "eof");
            var payload = s.AsSpan(i, len).ToArray(); i += len;
            if (s.Length - i < 4) return (f, "eof");
            uint rx = (uint)(s[i] | s[i + 1] << 8 | s[i + 2] << 16 | s[i + 3] << 24); i += 4;
            if (RefCrc(s[lebStart..lebEnd].Concat(payload)) != rx) return (f, "format");
            f.Add(payload);
        }
        return (f, "");
    }

    // Run the streaming decoder over `parts` (one Feed per part, each part split into sub-segments by `segs`).
    static (List<byte[]> frames, string end) Run(IEnumerable<ReadOnlySequence<byte>> feeds)
    {
        var d = new FrameDecoder(); var f = new List<byte[]>();
        try { foreach (var q in feeds) f.AddRange(d.Feed(q)); }
        catch (InvalidDataException) { f.AddRange(d.CompletedBeforeFailure); return (f, "format"); }
        try { d.Complete(); } catch (EndOfStreamException) { return (f, "eof"); }
        return (f, "");
    }
    static bool Same((List<byte[]> frames, string end) a, (List<byte[]> frames, string end) b)
        => a.end == b.end && a.frames.Count == b.frames.Count && a.frames.Zip(b.frames).All(p => p.First.AsSpan().SequenceEqual(p.Second));

    static int Main()
    {
        T.Begin();   // T1 CRC check value, sizes, known bytes
        {
            T.Check(Crc32Ieee.Compute("123456789"u8) == 0xCBF43926u, "CRC-32 check value CBF43926");
            byte[] known = Frame([0x41]);
            T.Check(known.AsSpan(0, 4).SequenceEqual(new byte[] { 0xC5, 0xA7, 0x01, 0x41 }) && known.Length == 8, "known layout C5 A7 01 41 + CRC LE");
            T.Check(BitConverter.ToUInt32(known, 4) == Crc32Ieee.Compute(new byte[] { 0x01, 0x41 }), "CRC covers raw ULEB bytes + payload");
            foreach (int n in new[] { 0, 1, 127, 128, 16383, 16384, 65536 })
            {
                var p = Pat(n, n); var fr = Frame(p);
                var r = Run([Seq(fr)]);
                T.Check(r.end == "" && r.frames.Count == 1 && r.frames[0].AsSpan().SequenceEqual(p), $"payload {n} bytes (ULEB {Convert.ToHexString(Leb(n))})");
            }
            T.Check(Convert.ToHexString(Leb(65536)) == "808004", "65536 encodes as 80 80 04");
        }
        T.Report("T1 CRC 123456789, empty / 1 / 127 / 128 / 16383 / 16384 / 65536-byte payloads, known bytes");

        // composite stream of several frames
        byte[][] payloads = [[], Pat(5, 1), Pat(130, 2), [0xC5, 0xA7, 0x01], Pat(1, 3)];
        byte[] stream = payloads.SelectMany(p => Frame(p)).ToArray();
        var want = RefDecode(stream);
        T.Begin();   // T2 splits, segments, byte-at-a-time
        {
            T.Check(want.end == "" && want.frames.Count == 5, "reference decodes the composite stream");
            T.Check(Same(Run([Seq(stream)]), want), "one Feed, one segment");
            T.Check(Same(Run(stream.Select(b => Seq(new[] { b }))), want), "one byte per Feed");
            T.Check(Same(Run([Seq(stream.Select(b => new[] { b }).ToArray())]), want), "one Feed of 1-byte segments");
            var withEmpty = new List<byte[]> { Array.Empty<byte>() }; foreach (byte b in stream) { withEmpty.Add([b]); withEmpty.Add([]); }
            T.Check(Same(Run([Seq(withEmpty.ToArray())]), want), "empty segments between every byte");
            T.Check(Same(Run([ReadOnlySequence<byte>.Empty, Seq(stream), ReadOnlySequence<byte>.Empty]), want), "empty ReadOnlySequence feeds");
            int bad = 0, runs = 0;
            for (int a = 0; a <= stream.Length; a++)
            {
                if (!Same(Run([Seq(stream[..a]), Seq(stream[a..])]), want)) bad++;                 // 2 feeds
                if (!Same(Run([Seq(stream[..a], stream[a..])]), want)) bad++;                      // 2 segments
                runs += 2;
                for (int b = a; b <= stream.Length; b++)
                {
                    if (!Same(Run([Seq(stream[..a]), Seq(stream[a..b], stream[b..])]), want)) bad++;  // feeds + segments
                    runs++;
                }
            }
            T.Check(bad == 0, $"{bad} of {runs} 2-way / 3-way splits (feeds and segments) differ");
            Console.WriteLine($"  split runs: {runs} (stream {stream.Length} bytes)");
        }
        T.Report("T2 every 2-way / 3-way split, many / tiny / empty segments, empty sequences, byte-by-byte Feed");

        T.Begin();   // T3 malformed input, sticky errors, truncation
        {
            void Bad(byte[] input, string kind, string name) { var r = Run([Seq(input)]); T.Check(r.end == kind && r.frames.Count == 0, $"{name}: {r.end}"); }
            Bad([0xC5, 0xA7, 0x80, 0x00, 0, 0, 0, 0], "format", "80 00 non-canonical");
            Bad([0xC5, 0xA7, 0x80, 0x80, 0x00, 0, 0, 0, 0], "format", "80 80 00 non-canonical");
            Bad([0xC5, 0xA7, 0x80, 0x80, 0x80, 0x01], "format", "4th ULEB byte (continuation in byte 3)");
            Bad(Frame(new byte[1], [0x81, 0x80, 0x04]), "format", "length 65537");
            Bad([0xC5, 0xA8], "format", "unknown magic");
            Bad([0x00], "format", "unknown first magic byte");
            byte[] one = Frame(Pat(40, 9)); int flips = 0, flagged = 0;
            for (int i = 2; i < one.Length; i++) for (int k = 0; k < 8; k++)
            {
                var x = (byte[])one.Clone(); x[i] ^= (byte)(1 << k); var r = Run([Seq(x)]); flips++;
                if (r.end != "" && r.frames.Count == 0) flagged++;   // rejected (format, or eof when a length flip makes the frame longer)
            }
            T.Check(flagged == flips, $"all {flips} single-bit flips (length, payload, CRC) rejected, never delivered");
            int crcFlips = 0; for (int i = one.Length - 4; i < one.Length; i++) for (int k = 0; k < 8; k++) { var x = (byte[])one.Clone(); x[i] ^= (byte)(1 << k); if (Run([Seq(x)]).end == "format") crcFlips++; }
            T.Check(crcFlips == 32, "every CRC bit flip -> InvalidDataException");
            int cuts = 0; for (int n = 1; n < stream.Length; n++) { var r = Run([Seq(stream[..n])]); var rf = RefDecode(stream[..n]); if (Same(r, rf) && (rf.end == "eof" || rf.end == "")) cuts++; }
            T.Check(cuts == stream.Length - 1, "every cut position: Complete -> EndOfStreamException exactly inside frames");
            var d = new FrameDecoder();
            bool threw = false; try { d.Feed(Seq([0xC5, 0x00])); } catch (InvalidDataException) { threw = true; }
            bool sticky1 = false; try { d.Feed(Seq(stream)); } catch (InvalidOperationException) { sticky1 = true; }
            bool sticky2 = false; try { d.Complete(); } catch (InvalidOperationException) { sticky2 = true; }
            T.Check(threw && sticky1 && sticky2, "first error sticky: Feed and Complete refused until Reset");
            d.Reset(); var ok = d.Feed(Seq(stream)); d.Complete();
            T.Check(ok.Count == 5, "Reset restores normal operation");
            var d2 = new FrameDecoder(); d2.Feed(Seq(stream[..3]));
            bool eof = false; try { d2.Complete(); } catch (EndOfStreamException) { eof = true; }
            bool eofSticky = false; try { d2.Feed(Seq(stream)); } catch (InvalidOperationException) { eofSticky = true; }
            T.Check(eof && eofSticky, "Complete inside a frame -> EndOfStreamException, then sticky");
            byte[] two = [.. Frame(Pat(3, 1)), .. Frame(Pat(4, 2)), 0xC5, 0xA7, 0x80, 0x00];
            try { new FrameDecoder().Feed(Seq(two)); T.Check(false, "expected error"); }
            catch (InvalidDataException e) { var cf = (byte[][])e.Data["CompletedFrames"]!; long off = (long)e.Data["Offset"]!; T.Check(cf.Length == 2 && off == two.Length - 1, $"frames completed before the error are reported ({cf.Length}), offset {off}"); }
        }
        T.Report("T3 80 00, 80 80 00, 4-byte ULEB, 65537, unknown magic, all CRC bit flips, all cuts, sticky error + Reset");

        T.Begin();   // T4 random valid / invalid frames vs reference, ownership
        {
            var rnd = new Random(20261117); int agree = 0, total = 0, invalid = 0;
            for (int t = 0; t < 100000; t++)
            {
                int nf = rnd.Next(1, 4); var parts = new List<byte>();
                for (int k = 0; k < nf; k++) { var p = new byte[rnd.Next(0, rnd.Next(10) == 0 ? 3000 : 40)]; rnd.NextBytes(p); parts.AddRange(Frame(p)); }
                byte[] s = parts.ToArray();
                if (rnd.Next(2) == 0)
                {
                    invalid++;
                    switch (rnd.Next(4))
                    {
                        case 0: s[rnd.Next(s.Length)] ^= (byte)(1 << rnd.Next(8)); break;
                        case 1: s = s[..rnd.Next(s.Length)]; break;
                        case 2: s[rnd.Next(s.Length)] = (byte)rnd.Next(256); break;
                        default: s = [.. s, (byte)rnd.Next(256)]; break;
                    }
                }
                // random feeds made of random segments
                var feeds = new List<ReadOnlySequence<byte>>(); int pos = 0;
                while (pos < s.Length)
                {
                    int segs = rnd.Next(1, 4); var arr = new List<byte[]>();
                    for (int g = 0; g < segs && pos < s.Length; g++) { int n = Math.Min(s.Length - pos, rnd.Next(0, 50)); arr.Add(s[pos..(pos + n)]); pos += n; }
                    feeds.Add(Seq(arr.ToArray()));
                }
                if (Same(Run(feeds), RefDecode(s))) agree++;
                total++;
            }
            T.Check(agree == total, $"{agree}/{total} random frame streams ({invalid} mutated) agree with the reference decoder");
            byte[] input = Frame(Pat(100, 5)); var d = new FrameDecoder();
            var outFrames = d.Feed(Seq(input)); Array.Fill(input, (byte)0xEE);
            T.Check(outFrames[0].AsSpan().SequenceEqual(Pat(100, 5)), "returned payload unaffected by overwriting the input array");
            var a1 = new FrameDecoder().Feed(Seq(stream)); var a2 = new FrameDecoder().Feed(Seq(stream));
            T.Check(!ReferenceEquals(a1[1], a2[1]) && a1[1].AsSpan().SequenceEqual(a2[1]), "each returned array is independently owned");
        }
        T.Report("T4 100,000 random valid / invalid frame streams vs independent reference, ownership after input reuse");

        Console.WriteLine($"RESULT: {T.Pass} checks passed, {T.Fail} failed");
        return T.Fail == 0 ? 0 : 1;
    }
}
```

#### 5. 検証結果（期待結果・実測結果・実施状況）

実行結果は `RESULT: 34 checks passed, 0 failed`、終了コード 0、所要 4.8 秒（Release）でした。

| テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| CRC-32 の検査値 | ASCII "123456789" → 0xCBF43926 | 一致 | 実施 |
| 既知の期待バイト列 | `C5 A7 01 41` + CRC(LE)。CRC の対象は ULEB の生バイトと payload | 一致 | 実施 |
| 空・1・127・128・16383・16384・65536 バイトの payload | 正しく復号（65536 は `80 80 04`） | 7通りすべて一致 | 実施 |
| 2分割・3分割の各境界 | 175バイトの5フレーム連結を、全2分割（Feed 分割・セグメント分割）と全3分割で処理し、参照と一致 | 15,928 通りすべて一致 | 実施 |
| 空・多数・極小のセグメント | 1バイトずつのセグメント、全バイトの間に空セグメント、空の ReadOnlySequence の Feed | 一致 | 実施 |
| 複数フレームの連結・全バイトを1つずつ Feed | 5フレームが順番どおり | 一致 | 実施 |
| `80 00` の非 canonical 長 | InvalidDataException | 一致 | 実施 |
| `80 80 00` | InvalidDataException | 一致 | 実施 |
| 4バイト目の継続（`80 80 80 01`） | InvalidDataException | 一致 | 実施 |
| 長さ 65537 | InvalidDataException | 一致 | 実施 |
| 未知の magic（`C5 A8`、`00`） | InvalidDataException | 一致 | 実施 |
| CRC の全1ビット反転 | CRC 欄の全32ビットの反転が InvalidDataException。長さ・payload を含む全 360 ビットの反転でも、フレームは決して配信されない | 一致 | 実施 |
| 全切断位置 | 174 の切断位置すべてで、参照と同じ（フレームの途中なら Complete が EndOfStreamException） | 一致 | 実施 |
| sticky エラーと Reset | エラー後は Feed も Complete も拒否。Reset で正常に戻る。Complete の途中切れも sticky | 一致 | 実施 |
| エラーの前に完了したフレーム | 同じ Feed 内の先行する2フレームとエラー位置を取得できる | 一致 | 実施 |
| ランダムな有効・無効フレーム10万件 | ランダムに分割した Feed とセグメントで、独立したバイト配列の参照デコーダ（ビット単位の CRC）と、フレーム列と終了状態が一致。うち約半数は変異させたもの | 100,000/100,000 | 実施 |
| 入力配列を書き換えた後の出力 | 返した payload は不変で、各配列は独立 | 一致 | 実施 |

#### 6. 技術的な正当性と設計判断

- **ReadOnlySequence の扱い：** `foreach` でセグメントを順に `ReadOnlySpan<byte>` として処理します。フレームの境界と CRC の状態はデコーダーのフィールドが持つので、セグメントやチャンクの境界がどこにあっても結果は同じです（全分割の試験で確認）。
- **メモリ：** パーサーの状態は O(1)（長さ・CRC・位置など）で、保持するのは作業中のフレームの payload（≤ 65536 バイト）だけです。入力は呼出しの後に保持しません。
- **所有権：** 返す `byte[]` は、フレームごとに新しく確保した配列です。デコーダーは返した後、参照を持ちません。
- **例外安全：** エラー時は作業中のバッファーを捨て、状態を「失敗」に固定します。CRC が不一致のフレームを飛ばして再同期することはしません。
- **計算量：** O(入力長)。CRC は 256 項目の表で計算します。

#### 7. 未実施項目・制約・残課題

- API の仕様上、Feed が例外を投げた場合、同じ呼出しで先に完了していたフレームは戻り値として返せません。代わりに、例外の `Data` とデコーダーのプロパティから取得します（`InvalidDataException` が sealed であるための設計判断です）。
- エラー後の Feed / Complete は、拒否を示す `InvalidOperationException`（InnerException は最初のエラー）を投げます。
- 未実施の項目はありません。実行環境の制約は「使用環境」の SDK についての記載を参照してください。

---

## Q02【地獄級】ArrayPoolとIBufferWriterによる機密バッファー管理

**試験の狙い：** Span/Memoryの有効期間、プールの返却、例外時状態不変、容量制限、機密データ消去を監査する。

### 元コード（危険・不完全）

```csharp
using System;
using System.Buffers;
class SecretBufferBad : IDisposable {
    byte[] data = ArrayPool<byte>.Shared.Rent(32);
    int written;
    public Span<byte> GetSpan(int n) => data.AsSpan(written, n);
    public void Advance(int n) => written += n;
    public void Dispose() => ArrayPool<byte>.Shared.Return(data); // 消去・二重返却対策なし
}
```

### 要求仕様

1. `SecretPooledWriter : IBufferWriter<byte>, IDisposable` を実装。構築時に `ArrayPool<byte>` を注入可能。`GetSpan(int sizeHint=0)`、`GetMemory(int sizeHint=0)`、`Advance(int count)`、`WrittenCount`、`WrittenMemory`、`ToArray()`、`Dispose()` を公開する。
2. 書込み上限は**1,048,576バイト**。sizeHint=0は利用可能な非空バッファーを返し、正のsizeHintは少なくともその長さを返す。上限を超える予約は `ArgumentOutOfRangeException`。負のsizeHint/countも拒否。初期容量は0でよい。
3. Advanceは**直前のGetSpan/GetMemoryが返した未書込み領域の長さ以内**のみ受理し、呼び出しごとに以前の予約を無効化する。Advance前のWrittenMemoryとWrittenCountは確定済みデータだけを示す。予約がない状態で正のAdvanceは禁止。
4. プールから借りた配列の物理長が上限を超えていても、公開する書込み可能長は論理上限を超えない。成長時は旧配列の確定済みデータを新配列にコピーし、旧配列を**クリアしてちょうど1回返す**。Rent失敗時は旧バイト列、WrittenCount、既存の確定済みデータを不変に保つ。
5. Disposeは冪等であり、確保した配列の全長をクリアして返す。Dispose後の操作は（Disposeの再呼出しを除き）`ObjectDisposedException`。ToArrayの結果は独立コピー。プールのRentが要求より短い配列を返すことは契約違反で、対応不要。
6. GetSpan/GetMemoryが返すビューは**次の取得・拡張・Advance・Disposeの時点で無効**とする（呼出し元の使用契約）。同一インスタンスの同時書込みは非対応。外部依存パッケージは禁止。

### 必須検証

1. sizeHint -1/0/1/4096/1,048,576/1,048,577、Advance -1/0/上限/過剰、未予約Advance、取得を繰り返した後のAdvance。
2. 成長50回、同じデータの連続書込み、ToArrayの独立性、WrittenMemoryの長さと内容、1MiBの境界。
3. 注入した記録用ArrayPoolでRent/Return回数、クリア有無、二重Returnなし、Rent失敗注入後のデータ不変を検証。
4. Dispose二重呼出し、Dispose後全API、GC後もToArray結果維持、機密バイトが返却時に0になっていること。

### ★★FREYJAの回答【Q02・記入欄】

> **以下は★★FREYJA専用の解答領域。** 問題本文と元コードには触れず、7項目をすべて実際の回答で置き換える。未実施の検証は未実施と正直に記載する。

#### 1. 根本原因・危険性・影響

**元コード `SecretBufferBad` の問題点**

| # | 箇所 | 根本原因 | 実害 |
| --- | --- | --- | --- |
| 1 | `data.AsSpan(written, n)` | 容量を検査しない・拡張しない | 32バイトを超えると `ArgumentOutOfRangeException` |
| 2 | `Advance(int n)` | 予約した長さとの比較や、負数の検査がない | `written` が配列長を超え、未初期化の内容（プール由来の他人のデータ）を「書いた」ことにする |
| 3 | `Return(data)` を消去せずに実行 | 機密データを含んだまま共有プールへ返す | 次に Rent した別のコードが秘密を読める（情報漏えい） |
| 4 | Dispose が冪等でない | 二重 Dispose で二重 Return | 同じ配列が2人に貸し出され、データの混線や破壊が起きる |
| 5 | Dispose 後の使用を検出しない | — | 返却済みの配列への書込み（他人のバッファーの破壊） |
| 6 | 上限・成長・Rent 失敗の扱いがない | — | 無制限の使用、失敗時の状態の不整合 |

**修正方針：**
- 確定済みの長さ `_written` と、直前の予約の長さ `_reserved` を分けて持ちます。
- `GetSpan` / `GetMemory` は、そのたびに以前の予約を無効にし、必要なら成長させます。公開する長さは `min(物理長, 1MiB) − written` までです。
- `Advance` は直前の予約の長さ以内だけを受け付け、呼ぶたびに予約を無効にします。
- 成長は「新しい配列を Rent → 確定分をコピー → 旧配列を `Array.Clear` で全長消去 → ちょうど1回 Return」の順です。Rent が失敗しても、何も変わりません。
- Dispose は冪等で、全長を消去して返却します。その後の操作はすべて `ObjectDisposedException` です。

#### 2. 完全な修正版C#コード

```csharp
// Q02: SecretPooledWriter - IBufferWriter<byte> over ArrayPool<byte> with secret wiping (C# 14 / .NET 10)
// Original implementation by ★★FREYJA for the C# hell-level exam.
using System;
using System.Buffers;

namespace Exam;

public sealed class SecretPooledWriter : IBufferWriter<byte>, IDisposable
{
    public const int MaxLength = 1_048_576;

    readonly ArrayPool<byte> _pool;
    byte[]? _buffer;        // rented array (null until the first reservation)
    int _written;           // committed bytes
    int _reserved = -1;     // length of the span/memory handed out by the last GetSpan/GetMemory; -1 = none
    bool _disposed;

    public SecretPooledWriter(ArrayPool<byte>? pool = null) => _pool = pool ?? ArrayPool<byte>.Shared;

    public int WrittenCount { get { ThrowIfDisposed(); return _written; } }

    public ReadOnlyMemory<byte> WrittenMemory
    {
        get { ThrowIfDisposed(); return _buffer is null ? ReadOnlyMemory<byte>.Empty : new ReadOnlyMemory<byte>(_buffer, 0, _written); }
    }

    public Span<byte> GetSpan(int sizeHint = 0) => Reserve(sizeHint).Span;

    public Memory<byte> GetMemory(int sizeHint = 0) => Reserve(sizeHint);

    public void Advance(int count)
    {
        ThrowIfDisposed();
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        int reserved = _reserved;
        _reserved = -1;                                     // every Advance ends the previous reservation
        if (count == 0) return;
        if (reserved < 0) throw new InvalidOperationException("Advance without a preceding GetSpan/GetMemory");
        if (count > reserved)
            throw new ArgumentOutOfRangeException(nameof(count), count, $"only {reserved} bytes were reserved");
        _written += count;                                  // reserved <= MaxLength - _written: no overflow
    }

    public byte[] ToArray()
    {
        ThrowIfDisposed();
        return WrittenMemory.ToArray();                     // independent copy
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _reserved = -1;
        byte[]? b = _buffer;
        _buffer = null;
        _written = 0;
        if (b is not null)
        {
            Array.Clear(b);                                 // wipe the whole physical array, not only the written part
            _pool.Return(b);
        }
    }

    Memory<byte> Reserve(int sizeHint)
    {
        ThrowIfDisposed();
        ArgumentOutOfRangeException.ThrowIfNegative(sizeHint);
        int need = Math.Max(sizeHint, 1);
        int room = MaxLength - _written;
        if (need > room)
            throw new ArgumentOutOfRangeException(nameof(sizeHint), sizeHint, $"would exceed the {MaxLength}-byte limit ({_written} already written)");
        _reserved = -1;                                     // a new request invalidates the old view
        int physical = _buffer?.Length ?? 0;
        if (Math.Min(physical, MaxLength) - _written < need)
            Grow(_written + need);                          // may throw: state unchanged
        int available = Math.Min(_buffer!.Length, MaxLength) - _written;   // never beyond the logical limit
        _reserved = available;
        return new Memory<byte>(_buffer, _written, available);
    }

    void Grow(int required)
    {
        int current = _buffer?.Length ?? 0;
        int target = Math.Max(required, Math.Min(MaxLength, Math.Max(256, current <= MaxLength / 2 ? current * 2 : MaxLength)));
        byte[] next = _pool.Rent(target);                   // a failing Rent leaves everything as it was
        byte[]? old = _buffer;
        if (old is not null)
        {
            old.AsSpan(0, _written).CopyTo(next);
            Array.Clear(old);                               // wipe before returning, exactly once
            _pool.Return(old);
        }
        _buffer = next;
    }

    void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
```

#### 3. ビルド方法・使用環境

```powershell
# 作業フォルダー: scratchpad\cs\q02（試験専用の一時領域）。cs\build.ps1 -Q q02 が次を順に実行する
# (1) 下記の q02.csproj を生成  (2) Release ビルド  (3) テスト実行（終了コード 0 = 全件合格）
dotnet build q02.csproj -c Release -nologo
dotnet bin\Release\net10.0\q02.dll
# 結果: ビルド成功・0 個の警告・0 エラー、実行の終了コード 0
#
# q02.csproj（全問共通。AssemblyName だけが問題ごとに異なる）
# <Project Sdk="Microsoft.NET.Sdk">
#   <PropertyGroup>
#     <OutputType>Exe</OutputType>
#     <TargetFramework>net10.0</TargetFramework>
#     <LangVersion>14.0</LangVersion>
#     <Nullable>enable</Nullable>
#     <ImplicitUsings>disable</ImplicitUsings>
#     <AnalysisLevel>latest</AnalysisLevel>
#     <AllowUnsafeBlocks>false</AllowUnsafeBlocks>
#     <InvariantGlobalization>true</InvariantGlobalization>
#     <AssemblyName>q02</AssemblyName>
#     <RootNamespace>Exam</RootNamespace>
#   </PropertyGroup>
# </Project>
```

**使用環境（全問共通）**

- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド）
- ランタイム：**Microsoft.NETCore.App 10.0.15（.NET 10）**。実行時に `Environment.Version` = 10.0.15 であることを確認しました。
- 言語とターゲット：`TargetFramework=net10.0`、`LangVersion=14.0`（C# 14。C# 14 の extension メンバーのコンパイルも確認済み）、`Nullable=enable`、`AnalysisLevel=latest`、`ImplicitUsings=disable`、`AllowUnsafeBlocks=false`、構成は **Release**。
- SDK：この PC にインストールされている SDK は **.NET SDK 11.0.100** だけです。.NET 10 用の参照パック（10.0.15）を使い、`net10.0` 向けにビルドしました。.NET 10 SDK そのものはインストールされておらず、無断導入の禁止に従って追加していません。そのため、コンパイラは SDK 11 に同梱のもので、言語バージョンを 14.0 に固定して使っています。
- NuGet パッケージ・第三者ライブラリ：使用していません（.NET 共有フレームワークの API だけ）。
- 各問は独立したコンソールプロジェクトです（`qNN.csproj` = 下記の内容、`QNN.cs` = 実装、`Tests.cs` = テスト）。ビルド時の警告は全問 **0 件**、テストの終了コードは全問 0 です。

#### 4. 実際に使用したテストコード

```csharp
// Q02 tests
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Exam;

static class T
{
    public static int Pass, Fail;
    static int _f0;
    public static void Check(bool ok, string what) { if (ok) Pass++; else { Fail++; Console.WriteLine($"  FAIL: {what}"); } }
    public static void Begin() => _f0 = Fail;
    public static void Report(string name) => Console.WriteLine($"[{(Fail == _f0 ? "PASS" : "FAIL")}] {name}");
    public static bool Throws<E>(Action a) where E : Exception { try { a(); } catch (E) { return true; } catch { return false; } return false; }
}

/// <summary>Recording pool: tracks every Rent/Return, detects double returns, checks wiping, can fail.</summary>
sealed class RecordingPool(int physicalExtra = 0) : ArrayPool<byte>
{
    public readonly HashSet<byte[]> Outstanding = new(ReferenceEqualityComparer.Instance);
    public int Rents, Returns, DoubleReturns, NotWiped, Foreign;
    public bool FailNextRent;
    public override byte[] Rent(int minimumLength)
    {
        if (FailNextRent) { FailNextRent = false; throw new OutOfMemoryException("injected Rent failure"); }
        var a = new byte[minimumLength + physicalExtra];
        Random.Shared.NextBytes(a);                       // pooled arrays may contain stale data
        Rents++; Outstanding.Add(a);
        return a;
    }
    public override void Return(byte[] array, bool clearArray = false)
    {
        if (!Outstanding.Remove(array)) { if (Rents > 0) DoubleReturns++; else Foreign++; return; }
        Returns++;
        if (array.AsSpan().ContainsAnyExcept((byte)0)) NotWiped++;
    }
}

static class Program
{
    static void Write(SecretPooledWriter w, ReadOnlySpan<byte> data)
    {
        while (!data.IsEmpty)
        {
            var span = w.GetSpan(Math.Min(data.Length, 777));
            int n = Math.Min(span.Length, data.Length);
            data[..n].CopyTo(span); w.Advance(n); data = data[n..];
        }
    }
    static byte[] Pat(int n, int seed) { var b = new byte[n]; for (int i = 0; i < n; i++) b[i] = (byte)(i * 13 + seed); return b; }

    static int Main()
    {
        const int Max = SecretPooledWriter.MaxLength;

        T.Begin();   // T1 sizeHint / Advance arguments
        {
            var pool = new RecordingPool(); var w = new SecretPooledWriter(pool);
            T.Check(T.Throws<ArgumentOutOfRangeException>(() => w.GetSpan(-1)) && T.Throws<ArgumentOutOfRangeException>(() => w.GetMemory(-1)), "sizeHint -1 rejected");
            T.Check(w.GetSpan(0).Length > 0, "sizeHint 0 returns a non-empty buffer");
            T.Check(w.GetSpan(1).Length >= 1 && w.GetSpan(4096).Length >= 4096, "sizeHint 1 / 4096");
            T.Check(w.GetSpan(Max).Length == Max, "sizeHint 1,048,576 on an empty writer: exactly the logical limit");
            T.Check(T.Throws<ArgumentOutOfRangeException>(() => w.GetSpan(Max + 1)), "sizeHint 1,048,577 rejected");
            T.Check(T.Throws<ArgumentOutOfRangeException>(() => w.Advance(-1)), "Advance -1 rejected");
            var s = w.GetSpan(10); int len = s.Length;
            T.Check(T.Throws<ArgumentOutOfRangeException>(() => w.Advance(len + 1)) && w.WrittenCount == 0, "Advance beyond the reservation rejected, nothing committed");
            T.Check(T.Throws<InvalidOperationException>(() => w.Advance(1)), "the failed Advance ended the reservation: positive Advance now rejected");
            w.Advance(0); T.Check(w.WrittenCount == 0, "Advance 0 is accepted");
            var w2 = new SecretPooledWriter(pool);
            T.Check(T.Throws<InvalidOperationException>(() => w2.Advance(1)), "Advance without any reservation rejected");
            w2.GetSpan(100); var last = w2.GetSpan(5); last[0] = 42; w2.Advance(last.Length);
            T.Check(w2.WrittenCount == last.Length, "after repeated GetSpan only the last reservation counts (Advance up to its length)");
            T.Check(T.Throws<InvalidOperationException>(() => w2.Advance(1)), "second Advance without a new reservation rejected");
            w.Dispose(); w2.Dispose();
            T.Check(pool.Outstanding.Count == 0 && pool.DoubleReturns == 0 && pool.NotWiped == 0, "all rented arrays returned once, wiped");
        }
        T.Report("T1 sizeHint -1/0/1/4096/1,048,576/1,048,577, Advance -1/0/limit/excess, unreserved Advance, repeated GetSpan");

        T.Begin();   // T2 growth, repeated data, ToArray independence, WrittenMemory, 1 MiB boundary
        {
            var pool = new RecordingPool(); int growths = 0, round = 0; bool contentOk = true, indepOk = true, returnOk = true;
            while (growths < 50)                                   // each writer grows ~12 times (doubling up to 1 MiB)
            {
                var w = new SecretPooledWriter(pool); var model = new List<byte>(); int rentsBefore = pool.Rents;
                for (int i = 0; model.Count < Max - 1000; i++)
                {
                    var chunk = Pat(1 + (i * 37 + round) % 997, i + round);
                    var span = w.GetSpan(chunk.Length);
                    chunk.CopyTo(span); w.Advance(chunk.Length); model.AddRange(chunk);
                }
                growths += pool.Rents - rentsBefore;
                contentOk &= w.WrittenMemory.Span.SequenceEqual(model.ToArray()) && w.WrittenCount == model.Count;
                var copy = w.ToArray(); copy[0] ^= 0xFF; indepOk &= w.WrittenMemory.Span[0] == model[0];
                returnOk &= pool.Outstanding.Count == 1 && pool.NotWiped == 0;     // only the current array is still rented
                w.Dispose(); round++;
            }
            T.Check(growths >= 50, $"{growths} growths over {round} writers");
            Console.WriteLine($"  growths={growths} writers={round} rents={pool.Rents} returns={pool.Returns}");
            T.Check(contentOk, "committed content and WrittenCount correct after every growth sequence");
            T.Check(indepOk, "ToArray is an independent copy");
            T.Check(returnOk && pool.Returns == pool.Rents && pool.DoubleReturns == 0, "every replaced array returned exactly once and wiped");            var same = new SecretPooledWriter(pool); var data = Pat(1000, 7);
            for (int k = 0; k < 100; k++) Write(same, data);
            bool ok = true; var wm = same.WrittenMemory.Span; for (int k = 0; k < 100; k++) ok &= wm.Slice(k * 1000, 1000).SequenceEqual(data);
            T.Check(ok && same.WrittenCount == 100_000, "100 consecutive writes of the same data");
            same.Dispose();
            var full = new SecretPooledWriter(pool); Write(full, Pat(Max, 3));
            T.Check(full.WrittenCount == Max, "exactly 1 MiB written");
            T.Check(T.Throws<ArgumentOutOfRangeException>(() => full.GetSpan(0)) && T.Throws<ArgumentOutOfRangeException>(() => full.GetSpan(1)), "no further reservation possible at 1 MiB");
            T.Check(full.ToArray().AsSpan().SequenceEqual(Pat(Max, 3)), "1 MiB content");
            full.Dispose();
            var big = new RecordingPool(physicalExtra: 3 * Max); var wb = new SecretPooledWriter(big);
            T.Check(wb.GetSpan(10).Length <= Max, "physical array larger than 1 MiB: exposed length capped at the limit");
            wb.Advance(10); T.Check(wb.GetSpan(0).Length == Max - 10 && wb.GetMemory(0).Length == Max - 10, "available length = limit - written");
            wb.Dispose();
            T.Check(pool.Outstanding.Count == 0 && pool.DoubleReturns == 0 && pool.NotWiped == 0 && big.Outstanding.Count == 0 && big.NotWiped == 0, "no leak, no double return, everything wiped");
        }
        T.Report("T2 growth, repeated identical writes, ToArray independence, WrittenMemory, 1 MiB boundary, oversized physical arrays");

        T.Begin();   // T3 recording pool counts, Rent failure
        {
            var pool = new RecordingPool(); var w = new SecretPooledWriter(pool);
            Write(w, Pat(300, 1));
            var before = w.ToArray(); int count = w.WrittenCount; var mem = w.WrittenMemory;
            pool.FailNextRent = true;
            T.Check(T.Throws<OutOfMemoryException>(() => w.GetSpan(100_000)), "injected Rent failure propagates");
            T.Check(w.WrittenCount == count && w.ToArray().AsSpan().SequenceEqual(before) && w.WrittenMemory.Span.SequenceEqual(mem.Span) && pool.Returns == pool.Rents - 1,
                    "after the failure: WrittenCount, committed bytes and the current array unchanged, nothing returned");
            Write(w, Pat(50_000, 2));
            T.Check(w.WrittenCount == 50_300 && w.ToArray().AsSpan(0, 300).SequenceEqual(before), "writer continues normally after the failure");
            w.Dispose();
            T.Check(pool.Rents == pool.Returns && pool.Outstanding.Count == 0 && pool.DoubleReturns == 0 && pool.NotWiped == 0 && pool.Foreign == 0, $"rents={pool.Rents} returns={pool.Returns}: each exactly once, wiped");
        }
        T.Report("T3 recorded Rent/Return counts, wiping, no double Return, Rent-failure injection keeps data");

        T.Begin();   // T4 Dispose
        {
            var pool = new RecordingPool(); var w = new SecretPooledWriter(pool);
            Write(w, "TOP-SECRET-KEY-MATERIAL"u8); var result = w.ToArray();
            w.Dispose(); w.Dispose();
            T.Check(pool.Returns == 1 && pool.DoubleReturns == 0 && pool.NotWiped == 0, "double Dispose returns once; secret bytes are zero on return");
            T.Check(T.Throws<ObjectDisposedException>(() => w.GetSpan()) && T.Throws<ObjectDisposedException>(() => w.GetMemory()) && T.Throws<ObjectDisposedException>(() => w.Advance(0))
                    && T.Throws<ObjectDisposedException>(() => _ = w.WrittenCount) && T.Throws<ObjectDisposedException>(() => _ = w.WrittenMemory) && T.Throws<ObjectDisposedException>(() => w.ToArray()),
                    "every API after Dispose -> ObjectDisposedException");
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            T.Check(result.AsSpan().SequenceEqual("TOP-SECRET-KEY-MATERIAL"u8), "ToArray result still valid after Dispose and GC");
            var empty = new SecretPooledWriter(pool); empty.Dispose();
            T.Check(pool.Rents == 1, "Dispose of an unused writer rents/returns nothing");
            var shared = new SecretPooledWriter(); Write(shared, Pat(5000, 1)); T.Check(shared.ToArray().Length == 5000, "default ArrayPool.Shared"); shared.Dispose();
        }
        T.Report("T4 double Dispose, all APIs after Dispose, ToArray after GC, secret bytes zero on return");

        Console.WriteLine($"RESULT: {T.Pass} checks passed, {T.Fail} failed");
        return T.Fail == 0 ? 0 : 1;
    }
}
```

#### 5. 検証結果（期待結果・実測結果・実施状況）

実行結果は `RESULT: 33 checks passed, 0 failed`、終了コード 0 でした。プールは、Rent と Return を記録し、二重返却と消去を確認するテスト用の `RecordingPool` を注入しています。

| テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| sizeHint −1 | ArgumentOutOfRangeException（GetSpan・GetMemory） | 一致 | 実施 |
| sizeHint 0 / 1 / 4096 | 空でない、または指定以上の長さ | 一致 | 実施 |
| sizeHint 1,048,576 / 1,048,577 | 空の状態からちょうど上限まで / 拒否 | 一致 | 実施 |
| Advance −1 / 0 / 上限 / 過剰 | 拒否 / 受理 / 受理 / 拒否で、確定分は不変 | 一致 | 実施 |
| 予約なしの Advance | InvalidOperationException。失敗した Advance も予約を終わらせる | 一致 | 実施 |
| 取得を繰り返した後の Advance | 最後の予約だけが有効 | 一致 | 実施 |
| 成長 50 回 | 4つの書き込み器を 1MiB 近くまで成長させ、計 **52 回**の成長で、確定データ・WrittenCount が正しく、置き換えた配列はちょうど1回 Return され消去済み | Rent 52・Return 52 | 実施 |
| 同じデータの連続書込み | 1000 バイト × 100 回 | 一致 | 実施 |
| ToArray の独立性 / WrittenMemory の長さと内容 | 独立したコピー / 確定分だけ | 一致 | 実施 |
| 1MiB の境界 | ちょうど 1MiB 書けて、その後の GetSpan(0)・GetSpan(1) は拒否 | 一致 | 実施 |
| 物理長が上限より大きい配列 | 公開する長さは上限 − 書込み済みまで | 一致 | 実施 |
| Rent 失敗の注入 | 例外が伝わり、WrittenCount・確定データ・現在の配列が不変。その後も書き続けられる | 一致 | 実施 |
| Rent/Return の回数・二重 Return なし・クリア | すべて一致、二重 Return 0、未消去 0 | 一致 | 実施 |
| Dispose の二重呼出し | Return は1回 | 一致 | 実施 |
| Dispose 後の全 API | GetSpan・GetMemory・Advance・WrittenCount・WrittenMemory・ToArray がすべて ObjectDisposedException | 一致 | 実施 |
| GC 後の ToArray の結果 | 有効 | 一致 | 実施 |
| 返却時に機密バイトが 0 | 返された全配列の全バイトが 0 | 一致 | 実施 |

#### 6. 技術的な正当性と設計判断

- **IBufferWriter の契約：** `GetSpan(0)` は空でないバッファーを返し、`Advance(n)` は直前に取得した長さ以内です。ビューは次の取得・成長・Advance・Dispose で無効になります（呼出し元の使用契約。同一インスタンスの同時使用は対象外です）。
- **機密データ：** 消去は、プールへ返す直前に、配列の**物理長全体**に対して `Array.Clear` で行います（書いた範囲だけではありません）。`Return(clearArray:true)` に頼らず自分で消去するので、どのプールの実装でも漏えいしません。
- **例外安全：** 成長で唯一失敗し得るのは Rent です。Rent は状態を変える前に呼ぶので、失敗しても旧配列・確定データ・WrittenCount は不変です。
- **整数あふれ：** すべて `MaxLength − _written` と比較して判定するので、加算の桁あふれはありません。

#### 7. 未実施項目・制約・残課題

- 制約：プールの Rent が要求より短い配列を返すことは契約違反として扱い、対応しません（問題文どおり）。
- 成長は2倍ずつなので、1つの書き込み器が 1MiB に達するまでの成長回数は約12回です。「成長50回」は、複数の書き込み器の合計（52回）で確認しました。
- 未実施の項目はありません。

---

## Q03【地獄級】有界KeyedExecutor：非同期FIFO・同時実行数・キャンセル

**試験の狙い：** Channel、SemaphoreSlim、Task/ValueTask、バックプレッシャー、キーごとの厳密なFIFO、例外隔離と終了処理を実装する。

### 元コード（危険・不完全）

```csharp
using System;
using System.Threading.Tasks;
class ExecutorBad {
    public Task Submit(Func<Task> job) => Task.Run(job); // 上限なし、順序なし、終了待機なし
}
```

### 要求仕様

1. `KeyedExecutor` を実装。公開APIは `ValueTask<int> SubmitAsync(string key, Func<CancellationToken, ValueTask<int>> work, CancellationToken cancellationToken=default)` と `ValueTask DisposeAsync()`。keyは空でない文字列、workは非null。引数不正は適切な例外で拒否。
2. 受付済みかつ完了していないジョブは**最大128件**。枠が満杯の場合、追加のSubmitAsyncは非同期で受付待ち（バックプレッシャー）になり、キャンセル可能。実際にworkが実行中の数は全キー合計で最大4。
3. 同じkeyの受付順はFIFOであり、前のworkが終了するまで次を開始しない。異なるkeyは並行実行可。鍵の比較はStringComparer.Ordinal。受け付けた順序は、空き枠獲得とキュー登録が完了した順序で定義する。
4. 受付前にキャンセルされた呼出しはworkを実行せずキャンセル完了。受付済みで開始前にキャンセルされたジョブもworkを実行せずキャンセル完了。開始後のキャンセルはworkに渡したtokenを通知し、workの最終結果（成功・例外・キャンセル）をそのまま返す。キャンセル済みでも完了したジョブの枠を必ず返す。
5. workが例外を投げても他ジョブを停止させず、そのSubmitAsyncにだけ例外を伝搬。非同期/同期で例外を投げるデリゲート両方を処理する。未監視例外や永久に戻らない枠を作らない。
6. DisposeAsync開始後の新規受付はObjectDisposedException。**受付済みジョブはキャンセルで明示されたものを除き最後まで処理**し、全受付済みジョブの終了を待ってからDisposeAsync完了。実行中のworkが戻らない場合はDisposeAsyncも完了しないという契約。
7. 明示的な要求がない公平性（異なるキー間の厳密な全順序）は要求しない。Task.Runの無制限起動やスレッドをSleepで占有して順序を作る方法は禁止。

### 必須検証

1. 1キーに100ジョブを投入した厳密な開始/終了順と、異なる20キーの最大同時実行数4。
2. 128件の受付を意図的に停止させ、129件目の待機・キャンセル・後続の復帰をTaskCompletionSourceで決定的に確認。
3. 受付前/受付後開始前/実行中のキャンセル、workの同期例外・非同期例外を交互に発生させ、他キーに影響しないこと。
4. DisposeAsyncとSubmitAsyncの競合、二重Dispose、1万ジョブでの取りこぼし・二重実行0、実行終了後の受付数0。

### ★★FREYJAの回答【Q03・記入欄】

> **以下は★★FREYJA専用の解答領域。** 問題本文と元コードには触れず、7項目をすべて実際の回答で置き換える。未実施の検証は未実施と正直に記載する。

#### 1. 根本原因・危険性・影響

**元コード `ExecutorBad.Submit` の問題点**

| # | 根本原因 | 実害 |
| --- | --- | --- |
| 1 | `Task.Run(job)` を無制限に起動する | 受付と同時実行の上限がなく、負荷が急増するとスレッドプールとメモリを使い尽くす（バックプレッシャーなし） |
| 2 | キーと順序の概念がない | 同じキーの処理が並行・順不同に走り、整合性が壊れる |
| 3 | キャンセルがない | 待機中・実行中の仕事を止められない |
| 4 | 終了処理がない | 受け付けた仕事の完了を待てない（Dispose 時の取りこぼし） |
| 5 | 例外の扱いが暗黙 | 結果を誰も待たなければ、例外が監視されない |

**修正方針：** 受付の枠に `SemaphoreSlim(128)`、実行の枠に `SemaphoreSlim(4)` を使います。
- 受付：`WaitAsync(ct)` で枠を得た後、ロックの中で「Dispose 中か・キャンセル済みかの確認 → 受付数を加算 → キーのキューへ登録 → キャンセルの登録」を一括して行います。ここが受付順序の定義（線形化点）です。
- 実行：キーごとに1つの非同期ポンプ（`Task.Yield` で開始）が、キューの先頭から1件ずつ、実行の枠を得て実行します。そのため、同じキーは FIFO で直列に実行され、全体の同時実行は最大4です。
- キャンセル：開始前にキャンセルされたジョブは、CAS（状態 0 → 2）で実行させず、その場で枠を返します。実行中なら、渡したトークンで通知します。
- 結果と例外：成功・例外・キャンセルを、そのジョブの `TaskCompletionSource` にだけ設定します。
- DisposeAsync：新規の受付を止め、受付済みの件数が 0 になるまで待ちます。

#### 2. 完全な修正版C#コード

```csharp
// Q03: KeyedExecutor - bounded admission (128), global concurrency (4), per-key FIFO, cancellation (C# 14 / .NET 10)
// Original implementation by ★★FREYJA for the C# hell-level exam.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Exam;

public sealed class KeyedExecutor : IAsyncDisposable
{
    public const int MaxAccepted = 128;
    public const int MaxRunning = 4;

    sealed class Job(string key, Func<CancellationToken, ValueTask<int>> work, CancellationToken token)
    {
        public readonly string Key = key;
        public readonly Func<CancellationToken, ValueTask<int>> Work = work;
        public readonly CancellationToken Token = token;
        public readonly TaskCompletionSource<int> Result = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int State;                       // 0 pending, 1 running, 2 cancelled before start
        public CancellationTokenRegistration Registration;
    }

    readonly SemaphoreSlim _admission = new(MaxAccepted, MaxAccepted);   // accepted-but-not-finished jobs
    readonly SemaphoreSlim _running = new(MaxRunning, MaxRunning);       // works executing right now
    readonly object _gate = new();
    readonly Dictionary<string, Queue<Job>> _queues = new(StringComparer.Ordinal);   // key -> queue (exists while a pump runs)
    readonly TaskCompletionSource _drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _accepted;                              // accepted jobs whose slot has not been returned
    long _totalAccepted;                        // diagnostics: acceptances so far
    bool _disposing;

    /// <summary>Diagnostics for tests: accepted jobs still holding an admission slot.</summary>
    public int AcceptedCount { get { lock (_gate) return _accepted; } }
    /// <summary>Diagnostics for tests: called under the lock at the moment of acceptance (defines the FIFO order).</summary>
    public Action<string, Func<CancellationToken, ValueTask<int>>>? AcceptedHook { get; set; }
    /// <summary>Diagnostics for tests: number of submissions accepted so far.</summary>
    public long TotalAccepted { get { lock (_gate) return _totalAccepted; } }

    public async ValueTask<int> SubmitAsync(string key, Func<CancellationToken, ValueTask<int>> work, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(work);
        lock (_gate) ObjectDisposedException.ThrowIf(_disposing, this);

        await _admission.WaitAsync(cancellationToken).ConfigureAwait(false);   // back-pressure; cancellable
        var job = new Job(key, work, cancellationToken);
        bool startPump;
        lock (_gate)
        {
            if (_disposing)
            {
                _admission.Release();
                throw new ObjectDisposedException(nameof(KeyedExecutor));
            }
            if (cancellationToken.IsCancellationRequested)
            {
                _admission.Release();
                cancellationToken.ThrowIfCancellationRequested();              // cancelled before acceptance
            }
            _accepted++;                                                       // accepted: slot + enqueue under one lock
            _totalAccepted++;
            startPump = !_queues.TryGetValue(key, out var q);
            if (startPump) { q = new Queue<Job>(); _queues.Add(key, q); }
            q!.Enqueue(job);
            AcceptedHook?.Invoke(key, work);
            // registered under the same lock, so no pump can take the job before the registration exists
            if (cancellationToken.CanBeCanceled)
                job.Registration = cancellationToken.Register(static s => { var a = (object[])s!; ((KeyedExecutor)a[0]).CancelPending((Job)a[1]); }, new object[] { this, job });
        }
        if (startPump) _ = PumpAsync(key);                                     // one pump per active key (bounded by 128)
        return await job.Result.Task.ConfigureAwait(false);
    }

    // Cancelled while still waiting in its key queue: never runs; its slot is returned immediately.
    void CancelPending(Job job)
    {
        if (Interlocked.CompareExchange(ref job.State, 2, 0) != 0) return;     // already running: the work sees the token
        job.Result.TrySetCanceled(job.Token);
        ReleaseSlot();
    }

    async Task PumpAsync(string key)
    {
        await Task.Yield();                                                    // never run work on the submitter's stack
        while (true)
        {
            Job? job;
            lock (_gate)
            {
                var q = _queues[key];
                if (q.Count == 0) { _queues.Remove(key); return; }
                job = q.Dequeue();
            }
            if (Volatile.Read(ref job.State) == 2) { job.Registration.Dispose(); continue; }   // skipped tombstone
            await _running.WaitAsync().ConfigureAwait(false);
            if (Interlocked.CompareExchange(ref job.State, 1, 0) != 0)          // cancelled while waiting for a run slot
            {
                _running.Release();
                job.Registration.Dispose();
                continue;
            }
            try
            {
                int r = await job.Work(job.Token).ConfigureAwait(false);       // synchronous throws land in catch too
                job.Result.TrySetResult(r);
            }
            catch (OperationCanceledException oce)
            {
                job.Result.TrySetCanceled(oce.CancellationToken);
            }
            catch (Exception ex)
            {
                job.Result.TrySetException(ex);                                // only this caller sees it
            }
            finally
            {
                _running.Release();
                job.Registration.Dispose();
                ReleaseSlot();
            }
        }
    }

    void ReleaseSlot()
    {
        bool drained;
        lock (_gate)
        {
            _accepted--;
            drained = _disposing && _accepted == 0;
        }
        _admission.Release();
        if (drained) _drained.TrySetResult();
    }

    /// <summary>Stops accepting; waits until every accepted job has finished (a work that never returns blocks this).</summary>
    public async ValueTask DisposeAsync()
    {
        bool done;
        lock (_gate)
        {
            _disposing = true;
            done = _accepted == 0;
        }
        if (done) _drained.TrySetResult();
        await _drained.Task.ConfigureAwait(false);
    }
}
```

#### 3. ビルド方法・使用環境

```powershell
# 作業フォルダー: scratchpad\cs\q03（試験専用の一時領域）。cs\build.ps1 -Q q03 が次を順に実行する
# (1) 下記の q03.csproj を生成  (2) Release ビルド  (3) テスト実行（終了コード 0 = 全件合格）
dotnet build q03.csproj -c Release -nologo
dotnet bin\Release\net10.0\q03.dll
# 結果: ビルド成功・0 個の警告・0 エラー、実行の終了コード 0
#
# q03.csproj（全問共通。AssemblyName だけが問題ごとに異なる）
# <Project Sdk="Microsoft.NET.Sdk">
#   <PropertyGroup>
#     <OutputType>Exe</OutputType>
#     <TargetFramework>net10.0</TargetFramework>
#     <LangVersion>14.0</LangVersion>
#     <Nullable>enable</Nullable>
#     <ImplicitUsings>disable</ImplicitUsings>
#     <AnalysisLevel>latest</AnalysisLevel>
#     <AllowUnsafeBlocks>false</AllowUnsafeBlocks>
#     <InvariantGlobalization>true</InvariantGlobalization>
#     <AssemblyName>q03</AssemblyName>
#     <RootNamespace>Exam</RootNamespace>
#   </PropertyGroup>
# </Project>
```

**使用環境（全問共通）**

- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド）
- ランタイム：**Microsoft.NETCore.App 10.0.15（.NET 10）**。実行時に `Environment.Version` = 10.0.15 であることを確認しました。
- 言語とターゲット：`TargetFramework=net10.0`、`LangVersion=14.0`（C# 14。C# 14 の extension メンバーのコンパイルも確認済み）、`Nullable=enable`、`AnalysisLevel=latest`、`ImplicitUsings=disable`、`AllowUnsafeBlocks=false`、構成は **Release**。
- SDK：この PC にインストールされている SDK は **.NET SDK 11.0.100** だけです。.NET 10 用の参照パック（10.0.15）を使い、`net10.0` 向けにビルドしました。.NET 10 SDK そのものはインストールされておらず、無断導入の禁止に従って追加していません。そのため、コンパイラは SDK 11 に同梱のもので、言語バージョンを 14.0 に固定して使っています。
- NuGet パッケージ・第三者ライブラリ：使用していません（.NET 共有フレームワークの API だけ）。
- 各問は独立したコンソールプロジェクトです（`qNN.csproj` = 下記の内容、`QNN.cs` = 実装、`Tests.cs` = テスト）。ビルド時の警告は全問 **0 件**、テストの終了コードは全問 0 です。

#### 4. 実際に使用したテストコード

```csharp
// Q03 tests
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Exam;

static class T
{
    public static int Pass, Fail;
    static int _f0;
    public static void Check(bool ok, string what) { if (ok) Pass++; else { Fail++; Console.WriteLine($"  FAIL: {what}"); } }
    public static void Begin() => _f0 = Fail;
    public static void Report(string name) => Console.WriteLine($"[{(Fail == _f0 ? "PASS" : "FAIL")}] {name}");
    public static async Task<bool> ThrowsAsync<E>(Func<Task> a) where E : Exception { try { await a(); } catch (E) { return true; } catch { return false; } return false; }
    public static async Task<bool> Eventually(Func<bool> cond, int ms = 5000) { var sw = System.Diagnostics.Stopwatch.StartNew(); while (!cond()) { if (sw.ElapsedMilliseconds > ms) return false; await Task.Delay(1); } return true; }
}

static class Program
{
    static async Task<int> Main()
    {
        _ = Task.Delay(TimeSpan.FromMinutes(3)).ContinueWith(_ => { Console.WriteLine("WATCHDOG: test did not finish within 3 minutes (hang)"); Environment.Exit(2); });
        T.Begin();   // T1 strict FIFO per key, global concurrency 4
        {
            await using var ex = new KeyedExecutor();
            var log = new ConcurrentQueue<string>();
            var tasks = Enumerable.Range(0, 100).Select(i => ex.SubmitAsync("one", async ct => { log.Enqueue($"s{i}"); await Task.Yield(); await Task.Delay(i % 3); log.Enqueue($"e{i}"); return i; }).AsTask()).ToArray();
            var results = await Task.WhenAll(tasks);
            var expect = Enumerable.Range(0, 100).SelectMany(i => new[] { $"s{i}", $"e{i}" });
            T.Check(log.SequenceEqual(expect), "100 jobs of one key: start/end strictly s0 e0 s1 e1 ... s99 e99");
            T.Check(results.SequenceEqual(Enumerable.Range(0, 100)), "each caller receives its own result");
            int cur = 0, max = 0;
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var many = new List<Task<int>>();
            for (int k = 0; k < 20; k++) for (int j = 0; j < 5; j++)
                many.Add(ex.SubmitAsync($"key{k}", async ct => { int c = Interlocked.Increment(ref cur); int m; while ((m = Volatile.Read(ref max)) < c && Interlocked.CompareExchange(ref max, c, m) != m) { } await gate.Task; await Task.Delay(1); Interlocked.Decrement(ref cur); return 0; }).AsTask());
            bool reached = await T.Eventually(() => Volatile.Read(ref cur) == 4);
            await Task.Delay(50);
            T.Check(reached && Volatile.Read(ref cur) == 4, $"with 20 busy keys exactly 4 works run (now {Volatile.Read(ref cur)})");
            gate.SetResult();
            await Task.WhenAll(many);
            T.Check(max == 4, $"maximum concurrent works over 100 jobs / 20 keys = {max}");
        }
        T.Report("T1 one key x 100 jobs strict start/end order; 20 keys never more than 4 running");

        T.Begin();   // T2 128 accepted, 129th waits, cancel, resume
        {
            var ex = new KeyedExecutor();
            var gates = new TaskCompletionSource<int>[128];
            var blocked = new Task<int>[128]; var startedFlags = new int[128];
            for (int i = 0; i < 128; i++) { int n = i; var g = gates[i] = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously); blocked[i] = ex.SubmitAsync($"b{i % 10}", ct => { Volatile.Write(ref startedFlags[n], 1); return new ValueTask<int>(g.Task); }).AsTask(); }
            T.Check(await T.Eventually(() => ex.AcceptedCount == 128), "128 jobs accepted");
            using var cts = new CancellationTokenSource();
            bool ran129 = false;
            var w129 = ex.SubmitAsync("x", ct => { ran129 = true; return ValueTask.FromResult(129); }, cts.Token).AsTask();
            await Task.Delay(100);
            T.Check(!w129.IsCompleted && ex.AcceptedCount == 128, "129th submission waits (back-pressure), not accepted");
            cts.Cancel();
            T.Check(await T.ThrowsAsync<OperationCanceledException>(() => w129) && !ran129, "cancelling the waiting 129th: canceled, work never ran");
            bool ran130 = false;
            var w130 = ex.SubmitAsync("y", ct => { ran130 = true; return ValueTask.FromResult(130); }).AsTask();
            await Task.Delay(50);
            T.Check(!w130.IsCompleted && !ran130, "next submission also waits");
            T.Check(await T.Eventually(() => startedFlags.Sum() == 4), "exactly 4 of the 128 accepted jobs are running");
            int victim = Array.IndexOf(startedFlags, 1);
            long acc0 = ex.TotalAccepted;
            gates[victim].SetResult(victim);                         // one running job finishes -> one admission slot frees up
            T.Check(await blocked[victim] == victim && await T.Eventually(() => ex.TotalAccepted == acc0 + 1) && ex.AcceptedCount == 128 && !w130.IsCompleted,
                    "after one running job finishes, the waiting submission is accepted (128 accepted again; it waits for a run slot)");
            for (int i = 0; i < 128; i++) gates[i].TrySetResult(i);
            await Task.WhenAll(blocked);
            T.Check(await w130 == 130 && ran130, "the resumed submission runs once the blocked jobs are released");
            await ex.DisposeAsync();
            T.Check(ex.AcceptedCount == 0, "all slots returned");
        }
        T.Report("T2 128 blocked jobs, 129th waits, cancel while waiting, later submission resumes (TaskCompletionSource)");

        T.Begin();   // T3 cancellation phases, sync / async exceptions isolated
        {
            var ex = new KeyedExecutor();
            using (var pre = new CancellationTokenSource()) { pre.Cancel(); bool ran = false;
                T.Check(await T.ThrowsAsync<OperationCanceledException>(() => ex.SubmitAsync("k", ct => { ran = true; return ValueTask.FromResult(1); }, pre.Token).AsTask()) && !ran && ex.AcceptedCount == 0, "cancelled before acceptance: never runs, no slot taken"); }
            var hold = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var first = ex.SubmitAsync("k", ct => new ValueTask<int>(hold.Task)).AsTask();
            using (var c2 = new CancellationTokenSource()) { bool ran = false;
                var second = ex.SubmitAsync("k", ct => { ran = true; return ValueTask.FromResult(2); }, c2.Token).AsTask();
                T.Check(await T.Eventually(() => ex.AcceptedCount == 2), "second job accepted behind the first");
                c2.Cancel();
                T.Check(await T.ThrowsAsync<OperationCanceledException>(() => second) && ex.AcceptedCount == 1, "cancelled after acceptance, before start: canceled and its slot returned at once");
                hold.SetResult(1); T.Check(await first == 1 && !ran, "the cancelled job never runs; the key continues"); }
            using (var c3 = new CancellationTokenSource()) {
                var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var running = ex.SubmitAsync("r", async ct => { started.SetResult(); await Task.Delay(Timeout.Infinite, ct); return 0; }, c3.Token).AsTask();
                await started.Task; c3.Cancel();
                T.Check(await T.ThrowsAsync<OperationCanceledException>(() => running), "cancel during execution: token observed, result canceled");
                var started2 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var proceed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                using var c4 = new CancellationTokenSource();
                var ignoring = ex.SubmitAsync("r", async ct => { started2.SetResult(); await proceed.Task; return ct.IsCancellationRequested ? 77 : 0; }, c4.Token).AsTask();
                await started2.Task; c4.Cancel(); proceed.SetResult();
                T.Check(await ignoring == 77, "work that finishes after cancellation: its own result is returned"); }
            var mixed = new List<(int i, Task<int> t)>();
            for (int i = 0; i < 40; i++)
            {
                int n = i;
                Func<CancellationToken, ValueTask<int>> w = (n % 4) switch
                {
                    0 => ct => throw new InvalidOperationException("sync" + n),
                    1 => async ct => { await Task.Yield(); throw new FormatException("async" + n); },
                    _ => async ct => { await Task.Yield(); return n; }
                };
                mixed.Add((n, ex.SubmitAsync($"m{n % 5}", w).AsTask()));
            }
            int okCount = 0;
            foreach (var (i, t) in mixed)
            {
                try { int r = await t; if (i % 4 >= 2 && r == i) okCount++; }
                catch (InvalidOperationException e) when (i % 4 == 0 && e.Message == "sync" + i) { okCount++; }
                catch (FormatException e) when (i % 4 == 1 && e.Message == "async" + i) { okCount++; }
            }
            T.Check(okCount == 40, $"sync and async exceptions delivered only to their own callers; other jobs unaffected ({okCount}/40)");
            await ex.DisposeAsync();
            T.Check(ex.AcceptedCount == 0, "no slot leaked after exceptions and cancellations");
        }
        T.Report("T3 cancel before acceptance / after acceptance / while running, sync and async exceptions isolated");

        T.Begin();   // T4 Dispose races, double Dispose, 10,000 jobs
        {
            var ex = new KeyedExecutor();
            var hold = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var pending = ex.SubmitAsync("d", ct => new ValueTask<int>(hold.Task)).AsTask();
            var queued = ex.SubmitAsync("d", ct => ValueTask.FromResult(5)).AsTask();
            await T.Eventually(() => ex.AcceptedCount == 2);
            var disposing = ex.DisposeAsync().AsTask();
            await Task.Delay(50);
            T.Check(!disposing.IsCompleted, "DisposeAsync waits for accepted jobs");
            T.Check(await T.ThrowsAsync<ObjectDisposedException>(() => ex.SubmitAsync("z", ct => ValueTask.FromResult(0)).AsTask()), "new submission after DisposeAsync started -> ObjectDisposedException");
            hold.SetResult(4);
            await disposing;
            T.Check(await pending == 4 && await queued == 5, "accepted jobs (running and queued) completed before DisposeAsync finished");
            await ex.DisposeAsync(); T.Check(true, "second DisposeAsync returns");

            var race = new KeyedExecutor(); int completed = 0, rejected = 0, other = 0;
            var subs = Enumerable.Range(0, 500).Select(i => Task.Run(async () => {
                try { await race.SubmitAsync($"q{i % 7}", async ct => { await Task.Yield(); return i; }); Interlocked.Increment(ref completed); }
                catch (ObjectDisposedException) { Interlocked.Increment(ref rejected); }
                catch { Interlocked.Increment(ref other); } })).ToArray();
            await Task.Delay(1);
            await race.DisposeAsync();
            await Task.WhenAll(subs);
            T.Check(completed + rejected == 500 && other == 0 && race.AcceptedCount == 0, $"DisposeAsync vs 500 concurrent submissions: completed {completed} + rejected {rejected}, no hang, 0 slots left");

            var big = new KeyedExecutor(); var runs = new int[10000]; var order = new ConcurrentDictionary<int, ConcurrentQueue<int>>();
            var rnd = new Random(7); var keys = Enumerable.Range(0, 10000).Select(_ => rnd.Next(50)).ToArray();
            var works = new Func<CancellationToken, ValueTask<int>>[10000]; var idOf = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
            for (int i = 0; i < 10000; i++) { int n = i; works[i] = async ct => { Interlocked.Increment(ref runs[n]); order.GetOrAdd(keys[n], _ => new()).Enqueue(n); if (n % 3 == 0) await Task.Yield(); return n * 2; }; idOf[works[i]] = i; }
            var acceptedOrder = new Dictionary<string, List<int>>();     // hook runs under the executor lock
            big.AcceptedHook = (k, w) => { if (!acceptedOrder.TryGetValue(k, out var l)) acceptedOrder[k] = l = new(); l.Add(idOf[w]); };
            var all = Enumerable.Range(0, 10000).Select(i => big.SubmitAsync($"k{keys[i]}", works[i]).AsTask()).ToArray();   // back-pressure engages after 128
            var res = await Task.WhenAll(all);
            bool fifo = order.All(kv => kv.Value.SequenceEqual(acceptedOrder[$"k{kv.Key}"])) && acceptedOrder.Values.Sum(l => l.Count) == 10000;
            T.Check(runs.All(r => r == 1) && res.Select((r, i) => r == i * 2).All(b => b), "10,000 jobs: every work ran exactly once, results correct");
            T.Check(fifo, "per-key execution order equals the acceptance order for all 50 keys (with back-pressure)");
            await big.DisposeAsync();
            T.Check(big.AcceptedCount == 0, "accepted count 0 after completion");
        }
        T.Report("T4 DisposeAsync vs SubmitAsync, double DisposeAsync, 10,000 jobs: no loss, no double run, 0 accepted at the end");

        Console.WriteLine($"RESULT: {T.Pass} checks passed, {T.Fail} failed");
        return T.Fail == 0 ? 0 : 1;
    }
}
```

#### 5. 検証結果（期待結果・実測結果・実施状況）

実行結果は `RESULT: 28 checks passed, 0 failed`、終了コード 0、所要 2.1 秒でした。

| テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 1キーに100ジョブ | 開始と終了が s0 e0 s1 e1 … s99 e99 と厳密に交互。各呼出し元が自分の結果を受け取る | 一致 | 実施 |
| 異なる20キーの最大同時実行数 | 100ジョブで同時実行がちょうど4に達し、超えない | max = 4 | 実施 |
| 128件の受付を停止させる | TaskCompletionSource で128件を止め、受付数 128。実行中はちょうど4件 | 一致 | 実施 |
| 129件目の待機とキャンセル | 受付されずに待機し、キャンセルでキャンセル完了。work は実行されない | 一致 | 実施 |
| 後続の復帰 | 実行中の1件を完了させると待機中の投入が受け付けられ（累計の受付数 +1、受付数は再び 128）、ゲートを解放すると実行される | 一致 | 実施 |
| 受付前のキャンセル | work は実行されず、枠も使わない | 一致 | 実施 |
| 受付後・開始前のキャンセル | キャンセル完了、枠はその場で返る、work は実行されない、同じキーの後続は続行 | 一致 | 実施 |
| 実行中のキャンセル | work に渡したトークンが通知され、結果はキャンセル。キャンセルを無視した work は自分の結果（77）を返す | 一致 | 実施 |
| 同期例外・非同期例外を交互に | 40件（5キー）で、各例外は自分の呼出し元だけに届き、他のジョブの結果は正しい | 40/40 | 実施 |
| DisposeAsync と受付済みジョブ | DisposeAsync は受付済み（実行中と待機中）の完了まで待つ。開始後の新規投入は ObjectDisposedException | 一致 | 実施 |
| 二重 Dispose | 2回目も正常に戻る | 一致 | 実施 |
| DisposeAsync と SubmitAsync の競合 | 500件の同時投入と Dispose の結果は、完了か ObjectDisposedException のいずれかで、ハングせず、受付数は 0 | 一致 | 実施 |
| 1万ジョブ | 各 work はちょうど1回実行され、結果は正しい。50キーすべてで実行順 = 受付順（バックプレッシャーがかかった状態を含む） | 取りこぼし0・二重実行0 | 実施 |
| 終了後の受付数 | 0 | 0 | 実施 |

**自律修正（テスト側）：**
- T2 で2回ハングしました。1回目は、まだ実行枠を得ていない（開始していない）ジョブのゲートを解放していたためです。2回目は、受付された 130 件目の「完了」を待ったものの、空いた実行枠を先に待っていた別のジョブが取るため、終わらなかったためです。どちらもテストの前提の誤りで、実装は仕様どおりでした。テストは「実際に開始したジョブを解放する」「受付を `TotalAccepted` で確認する」形に直しました。あわせて、3分の監視タイマーを入れました。
- 1万件の FIFO の検査は、当初「呼出し順」と比べていました。バックプレッシャーがかかると、待機していた投入の再開順はスレッドプールの都合で変わります。仕様の定義（受付順 = 枠の獲得と登録が完了した順）に合わせ、診断用の `AcceptedHook`（ロックの中で呼ぶ）で記録した受付順と比較するように直しました。

#### 6. 技術的な正当性と設計判断

- **受付の線形化点：** `_gate` ロックの中で、受付数の加算とキーのキューへの登録を同時に行う時点です。同じキーの FIFO は、この順序に対して保証します。異なるキーの間の公平性は要求されていないので、保証しません。
- **同時実行の上限：** 受付の枠（128）は SubmitAsync の `WaitAsync(ct)` で取り、キャンセルできます。実行の枠（4）は、ポンプが work の直前に取り、`finally` で必ず返します。ポンプは「キーに受付済みの仕事がある間だけ」存在し、その数は受付数（≤ 128）以下です。`Task.Run` を無制限に起動することはありません。
- **キャンセルの競合：** ジョブの状態の CAS（0 待機 → 1 実行／0 → 2 キャンセル）によって、「開始する」と「開始前にキャンセルする」のどちらか一方だけが起きます。キャンセルの登録はロックの中で行うので、登録前に別のポンプがジョブを取り出す競合もありません。
- **例外：** work の同期例外も、非同期の失敗も、`await job.Work(...)` を囲む try で捕まえ、その TCS にだけ設定します。TCS は `RunContinuationsAsynchronously` なので、呼出し元の継続がポンプのスレッドを占有しません。未監視の例外は生じません。
- **DisposeAsync：** フラグを立てた後の新規受付は拒否します。受付済みの件数が 0 になった時点で完了する TCS を待ちます。実行中の work が戻らない場合は、DisposeAsync も完了しません（問題文の契約）。

#### 7. 未実施項目・制約・残課題

- `AcceptedCount`・`TotalAccepted`・`AcceptedHook` は、試験のための診断 API です（本番では使いません）。
- 異なるキーの間の厳密な全順序（公平性）は、仕様どおり保証しません。
- 未実施の項目はありません。

---

## Q04【地獄級】TimeProviderベースのSingle-flight非同期キャッシュ

**試験の狙い：** 同一キー要求の集約、TTL境界、呼出し元だけのキャンセル、失敗時の再試行、Dispose時の寿命を扱う。

### 元コード（危険・不完全）

```csharp
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
class CacheBad {
    ConcurrentDictionary<string, Task<byte[]>> tasks = new();
    public Task<byte[]> Get(string k, Func<string, Task<byte[]>> load)
        => tasks.GetOrAdd(k, load); // 例外も永久キャッシュ、TTLなし
}
```

### 要求仕様

1. `SingleFlightCache` は `Func<string,CancellationToken,Task<byte[]>> factory`、`TimeProvider clock`、`TimeSpan ttl` を受け取り、`Task<byte[]> GetAsync(string key,CancellationToken ct=default)` と `ValueTask DisposeAsync()` を公開。keyはOrdinal比較。ttlは正の値に限定。
2. 各keyで同時実行中のfactoryは最大1つ。呼出しが同時に来た場合は**1回のfactory実行を共有**し、全利用者に独立したbyte[]コピーを返す。読み手が返却配列を書き換えても他利用者やキャッシュの内容は変化しない。
3. 成功結果だけをキャッシュし、TTLはfactoryが**成功して結果を公開した時刻**から計測。現在時刻が有効期限と等しい瞬間は期限切れ。TimeProvider.GetUtcNowにより判断し、Thread.Sleepに依存しない決定的テストを可能にする。
4. 個々のGetAsyncのctは**その呼出し元の待機だけ**をキャンセルし、他利用者と共有factoryをキャンセルしない。factoryへ渡すtokenはキャッシュ自身の寿命tokenとし、DisposeAsyncでキャンセルする。
5. factory例外またはキャンセル（Dispose時を含む）は結果をキャッシュせず、すべての待機者へ失敗を伝える。次のGetAsyncでは新しいfactory実行を許す。例外が無監視のまま残らないこと。
6. DisposeAsyncは新規要求を拒否し、factoryへキャンセルを伝え、進行中のfactoryタスクが終わるまで待つ。factoryがキャンセルを無視する場合には待ち続けることを明記し、結果の静かな破棄や待機者のハングを避ける。
7. factoryが返したbyte[]も外部から書き換えられる可能性があるため、キャッシュ公開時にコピーすること。外部パッケージ禁止。

### 必須検証

1. 同一key 100同時呼出しでfactory実行1回、異なるkeyは並行可能、全返却byte[]は別配列。
2. FakeTimeProvider相当の独自TimeProviderでTTL直前/ちょうど/直後、factoryが長時間実行中の時計進行、期限切れ100件の再集約。
3. 1人だけ待機キャンセル/全員が待機キャンセル/キャンセルと完了の競合でもfactoryの重複呼出しが起きない。
4. factoryが例外・キャンセル・byte[]を返す各経路、後続再試行、DisposeとGetの競合、リーク・未監視例外なし。

### ★★FREYJAの回答【Q04・記入欄】

> **以下は★★FREYJA専用の解答領域。** 問題本文と元コードには触れず、7項目をすべて実際の回答で置き換える。未実施の検証は未実施と正直に記載する。

#### 1. 根本原因・危険性・影響

**元コード `CacheBad.Get` の問題点**

| # | 根本原因 | 実害 |
| --- | --- | --- |
| 1 | `GetOrAdd(k, load)` は、競合すると valueFactory を複数回呼ぶことがある | 同じキーに対する load が重複して実行される（single-flight にならない） |
| 2 | 失敗した Task もそのまま永久にキャッシュする | 一度失敗すると、その後ずっと同じ例外を返す（再試行できない） |
| 3 | TTL がない | 古いデータを無期限に返す |
| 4 | 同じ byte[] を全員に返す | ある利用者の書換えが、他の利用者とキャッシュを汚染する |
| 5 | キャンセル・寿命管理がない | 呼出し元の待機を止められない。Dispose の時に load を止められない |

**修正方針：** キーごとの `Entry`（実行中の共有 Task、または「私的なコピーと有効期限」）をロックで管理します。
- 同時に来た要求は、1つの factory の実行を共有します。factory はロックの外で `Task.Yield` の後に呼ぶので、利用者のコードをロックの中で実行しません。
- 成功したら、結果を**コピー**してから公開し、`ExpiresAt = 公開時刻 + ttl` とします。`now < ExpiresAt` の間だけ有効です。
- 各呼出し元は `shared.WaitAsync(ct)` で待つので、ct はその待機だけを止めます。返す値は、必ず呼出し元ごとのコピーです。
- 失敗やキャンセルの結果は、エントリーごと削除して、キャッシュしません。
- factory へ渡すトークンはキャッシュ自身の寿命トークンで、DisposeAsync がこれをキャンセルし、実行中の factory の終了を待ちます。

#### 2. 完全な修正版C#コード

```csharp
// Q04: SingleFlightCache - one factory run per key, TTL on TimeProvider, caller-only cancellation (C# 14 / .NET 10)
// Original implementation by ★★FREYJA for the C# hell-level exam.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Exam;

public sealed class SingleFlightCache : IAsyncDisposable
{
    sealed class Entry
    {
        public Task<byte[]>? InFlight;      // shared factory run (result = private copy), null when cached
        public byte[]? Value;               // private copy of the last successful result
        public DateTimeOffset ExpiresAt;    // valid while now < ExpiresAt
    }

    readonly Func<string, CancellationToken, Task<byte[]>> _factory;
    readonly TimeProvider _clock;
    readonly TimeSpan _ttl;
    readonly object _gate = new();
    readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    readonly HashSet<Task> _running = new();
    readonly CancellationTokenSource _lifetime = new();
    Task? _disposeTask;

    public SingleFlightCache(Func<string, CancellationToken, Task<byte[]>> factory, TimeProvider clock, TimeSpan ttl)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ttl, TimeSpan.Zero);
        _factory = factory; _clock = clock; _ttl = ttl;
    }

    /// <summary>Diagnostics for tests: factory runs still in flight.</summary>
    public int InFlightCount { get { lock (_gate) return _running.Count; } }

    public async Task<byte[]> GetAsync(string key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ct.ThrowIfCancellationRequested();
        Task<byte[]> shared;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposeTask is not null, this);
            if (_entries.TryGetValue(key, out var e))
            {
                if (e.InFlight is not null) shared = e.InFlight;                         // join the running factory
                else if (_clock.GetUtcNow() < e.ExpiresAt) return (byte[])e.Value!.Clone();   // fresh hit: own copy
                else shared = Start(key, e);                                             // expired: one new run
            }
            else
            {
                e = new Entry();
                _entries.Add(key, e);
                shared = Start(key, e);
            }
        }
        byte[] result = await shared.WaitAsync(ct).ConfigureAwait(false);   // ct cancels only this wait
        return (byte[])result.Clone();                                       // every caller gets its own array
    }

    // Called under the lock.
    Task<byte[]> Start(string key, Entry e)
    {
        var run = RunFactoryAsync(key, e);
        e.InFlight = run;
        _running.Add(run);
        // Bookkeeping + observing the outcome, so that a failure seen by no caller (all waiters cancelled)
        // never becomes an unobserved task exception.
        _ = run.ContinueWith(t => { _ = t.Exception; lock (_gate) _running.Remove(t); }, CancellationToken.None,
                             TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return run;
    }

    async Task<byte[]> RunFactoryAsync(string key, Entry e)
    {
        await Task.Yield();                                      // never run user code under the lock
        try
        {
            byte[] produced = await _factory(key, _lifetime.Token).ConfigureAwait(false)
                              ?? throw new InvalidOperationException("factory returned null");
            byte[] copy = (byte[])produced.Clone();              // the factory's array may be modified later
            lock (_gate)
            {
                e.InFlight = null;
                if (_disposeTask is null)
                {
                    e.Value = copy;
                    e.ExpiresAt = _clock.GetUtcNow() + _ttl;     // TTL starts when the result is published
                }
                else if (_entries.TryGetValue(key, out var cur) && ReferenceEquals(cur, e)) _entries.Remove(key);   // disposing: deliver, do not cache
            }
            return copy;
        }
        catch
        {
            lock (_gate)
            {
                e.InFlight = null;                               // failures and cancellations are never cached
                if (_entries.TryGetValue(key, out var cur) && ReferenceEquals(cur, e)) _entries.Remove(key);
            }
            throw;                                               // every waiter sees the failure / cancellation
        }
    }
    /// <summary>Rejects new requests, cancels the factory token and waits for every running factory.
    /// A factory that ignores cancellation keeps DisposeAsync waiting (documented contract).</summary>
    public ValueTask DisposeAsync()
    {
        lock (_gate)
        {
            _disposeTask ??= DisposeCoreAsync();
            return new ValueTask(_disposeTask);
        }
    }

    async Task DisposeCoreAsync()
    {
        await Task.Yield();
        _lifetime.Cancel();
        Task[] pending;
        lock (_gate) pending = [.. _running];
        foreach (var t in pending)
        {
            try { await t.ConfigureAwait(false); } catch { /* already delivered to waiters / observed */ }
        }
        lock (_gate) _entries.Clear();
        _lifetime.Dispose();
    }
}
```

#### 3. ビルド方法・使用環境

```powershell
# 作業フォルダー: scratchpad\cs\q04（試験専用の一時領域）。cs\build.ps1 -Q q04 が次を順に実行する
# (1) 下記の q04.csproj を生成  (2) Release ビルド  (3) テスト実行（終了コード 0 = 全件合格）
dotnet build q04.csproj -c Release -nologo
dotnet bin\Release\net10.0\q04.dll
# 結果: ビルド成功・0 個の警告・0 エラー、実行の終了コード 0
#
# q04.csproj（全問共通。AssemblyName だけが問題ごとに異なる）
# <Project Sdk="Microsoft.NET.Sdk">
#   <PropertyGroup>
#     <OutputType>Exe</OutputType>
#     <TargetFramework>net10.0</TargetFramework>
#     <LangVersion>14.0</LangVersion>
#     <Nullable>enable</Nullable>
#     <ImplicitUsings>disable</ImplicitUsings>
#     <AnalysisLevel>latest</AnalysisLevel>
#     <AllowUnsafeBlocks>false</AllowUnsafeBlocks>
#     <InvariantGlobalization>true</InvariantGlobalization>
#     <AssemblyName>q04</AssemblyName>
#     <RootNamespace>Exam</RootNamespace>
#   </PropertyGroup>
# </Project>
```

**使用環境（全問共通）**

- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド）
- ランタイム：**Microsoft.NETCore.App 10.0.15（.NET 10）**。実行時に `Environment.Version` = 10.0.15 であることを確認しました。
- 言語とターゲット：`TargetFramework=net10.0`、`LangVersion=14.0`（C# 14。C# 14 の extension メンバーのコンパイルも確認済み）、`Nullable=enable`、`AnalysisLevel=latest`、`ImplicitUsings=disable`、`AllowUnsafeBlocks=false`、構成は **Release**。
- SDK：この PC にインストールされている SDK は **.NET SDK 11.0.100** だけです。.NET 10 用の参照パック（10.0.15）を使い、`net10.0` 向けにビルドしました。.NET 10 SDK そのものはインストールされておらず、無断導入の禁止に従って追加していません。そのため、コンパイラは SDK 11 に同梱のもので、言語バージョンを 14.0 に固定して使っています。
- NuGet パッケージ・第三者ライブラリ：使用していません（.NET 共有フレームワークの API だけ）。
- 各問は独立したコンソールプロジェクトです（`qNN.csproj` = 下記の内容、`QNN.cs` = 実装、`Tests.cs` = テスト）。ビルド時の警告は全問 **0 件**、テストの終了コードは全問 0 です。

#### 4. 実際に使用したテストコード

```csharp
// Q04 tests
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Exam;

static class T
{
    public static int Pass, Fail;
    static int _f0;
    public static void Check(bool ok, string what) { if (ok) Pass++; else { Fail++; Console.WriteLine($"  FAIL: {what}"); } }
    public static void Begin() => _f0 = Fail;
    public static void Report(string name) => Console.WriteLine($"[{(Fail == _f0 ? "PASS" : "FAIL")}] {name}");
    public static async Task<bool> ThrowsAsync<E>(Func<Task> a) where E : Exception { try { await a(); } catch (E) { return true; } catch { return false; } return false; }
    public static async Task<bool> Eventually(Func<bool> c, int ms = 5000) { var sw = System.Diagnostics.Stopwatch.StartNew(); while (!c()) { if (sw.ElapsedMilliseconds > ms) return false; await Task.Delay(1); } return true; }
}

sealed class FakeClock(DateTimeOffset start) : TimeProvider
{
    DateTimeOffset _now = start;
    public override DateTimeOffset GetUtcNow() { lock (this) return _now; }
    public void Advance(TimeSpan d) { lock (this) _now += d; }
}

/// <summary>Factory controlled by the test: counts runs, each run waits for Release().</summary>
sealed class GatedFactory
{
    public int Calls;
    public readonly List<CancellationToken> Tokens = new();
    TaskCompletionSource<byte[]> _gate = New();
    static TaskCompletionSource<byte[]> New() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool IgnoreCancellation;
    public async Task<byte[]> Run(string key, CancellationToken ct)
    {
        TaskCompletionSource<byte[]> g;
        lock (this) { Calls++; Tokens.Add(ct); g = _gate; }
        if (IgnoreCancellation) return await g.Task;
        return await g.Task.WaitAsync(ct);
    }
    public void Release(byte[] v) { TaskCompletionSource<byte[]> g; lock (this) { g = _gate; _gate = New(); } g.SetResult(v); }
    public void Fail(Exception e) { TaskCompletionSource<byte[]> g; lock (this) { g = _gate; _gate = New(); } g.SetException(e); }
    public void Cancel() { TaskCompletionSource<byte[]> g; lock (this) { g = _gate; _gate = New(); } g.SetCanceled(); }
}

static class Program
{
    static int s_unobserved;
    static readonly TimeSpan Ttl = TimeSpan.FromSeconds(10);

    static async Task<int> Main()
    {
        _ = Task.Delay(TimeSpan.FromMinutes(3)).ContinueWith(_ => { Console.WriteLine("WATCHDOG: test did not finish within 3 minutes (hang)"); Environment.Exit(2); });
        TaskScheduler.UnobservedTaskException += (_, e) => { Interlocked.Increment(ref s_unobserved); e.SetObserved(); };
        var t0 = new DateTimeOffset(2026, 11, 17, 0, 0, 0, TimeSpan.Zero);

        T.Begin();   // T1 100 concurrent callers share one run; distinct arrays; different keys in parallel
        {
            var clock = new FakeClock(t0); var f = new GatedFactory();
            await using var c = new SingleFlightCache(f.Run, clock, Ttl);
            var gets = Enumerable.Range(0, 100).Select(_ => c.GetAsync("a")).ToArray();
            T.Check(await T.Eventually(() => f.Calls == 1) && f.Calls == 1, "100 concurrent GetAsync on one key -> 1 factory run");
            var other = c.GetAsync("b");
            T.Check(await T.Eventually(() => f.Calls == 2) && c.InFlightCount == 2, "a different key runs its own factory concurrently");
            byte[] produced = [1, 2, 3];
            f.Release(produced);                        // releases the current gate; key b waits for the next
            var results = await Task.WhenAll(gets);
            T.Check(results.All(r => r.SequenceEqual(new byte[] { 1, 2, 3 })), "all 100 callers got the value");
            T.Check(results.Distinct(ReferenceEqualityComparer.Instance).Count() == 100 && !results.Any(r => ReferenceEquals(r, produced)), "100 distinct arrays, none is the factory's array");
            results[0][0] = 99; produced[1] = 77;
            var again = await c.GetAsync("a");
            T.Check(again.SequenceEqual(new byte[] { 1, 2, 3 }) && results[1][0] == 1 && f.Calls == 2, "modifying a returned array or the factory array changes neither other callers nor the cache");
            T.Check((await other).SequenceEqual(new byte[] { 1, 2, 3 }), "key b (waiting on the same test gate) completes with its own run's value");
        }
        T.Report("T1 100 concurrent callers -> 1 factory run, independent arrays, different keys in parallel");

        T.Begin();   // T2 TTL boundaries with a fake clock
        {
            var clock = new FakeClock(t0); var f = new GatedFactory();
            await using var c = new SingleFlightCache(f.Run, clock, Ttl);
            var first = c.GetAsync("k"); await T.Eventually(() => f.Calls == 1);
            clock.Advance(TimeSpan.FromSeconds(100));   // the factory takes long: TTL starts at publication
            f.Release([1]); await first;
            clock.Advance(Ttl - TimeSpan.FromTicks(1));
            await c.GetAsync("k"); T.Check(f.Calls == 1, "TTL - 1 tick after publication: cached");
            clock.Advance(TimeSpan.FromTicks(1));
            var atExpiry = c.GetAsync("k"); await T.Eventually(() => f.Calls == 2);
            T.Check(f.Calls == 2, "exactly at the expiry instant: expired, new run");
            f.Release([2]); T.Check((await atExpiry)[0] == 2, "refreshed value");
            clock.Advance(Ttl + TimeSpan.FromTicks(1));
            var batch = Enumerable.Range(0, 100).Select(_ => c.GetAsync("k")).ToArray();
            await T.Eventually(() => f.Calls == 3); await Task.Delay(20);
            T.Check(f.Calls == 3, "100 callers after expiry -> one new run");
            f.Release([3]); var rs = await Task.WhenAll(batch);
            T.Check(rs.All(r => r[0] == 3), "all 100 see the new value");
            T.Check(Throws(() => new SingleFlightCache(f.Run, clock, TimeSpan.Zero)) && Throws(() => new SingleFlightCache(f.Run, clock, TimeSpan.FromSeconds(-1))), "ttl must be positive");
        }
        T.Report("T2 TTL just before / exactly at / after expiry, clock advancing during a long factory, 100 expired callers re-aggregated");

        T.Begin();   // T3 caller-only cancellation
        {
            var clock = new FakeClock(t0); var f = new GatedFactory();
            await using var c = new SingleFlightCache(f.Run, clock, Ttl);
            using var one = new CancellationTokenSource();
            var a = c.GetAsync("x", one.Token); var b = c.GetAsync("x");
            await T.Eventually(() => f.Calls == 1);
            one.Cancel();
            T.Check(await T.ThrowsAsync<OperationCanceledException>(() => a) && !b.IsCompleted && !f.Tokens[0].IsCancellationRequested, "one waiter cancels: only its wait ends; shared run continues");
            f.Release([5]); T.Check((await b)[0] == 5 && f.Calls == 1, "the other waiter receives the result");
            clock.Advance(Ttl);
            var ctss = Enumerable.Range(0, 10).Select(_ => new CancellationTokenSource()).ToArray();
            var gs = ctss.Select(s => c.GetAsync("x", s.Token)).ToArray();
            await T.Eventually(() => f.Calls == 2);
            foreach (var s in ctss) s.Cancel();
            bool allCancelled = true; foreach (var g in gs) allCancelled &= await T.ThrowsAsync<OperationCanceledException>(() => g);
            T.Check(allCancelled && c.InFlightCount == 1 && !f.Tokens[1].IsCancellationRequested, "every waiter cancels: the factory keeps running (not cancelled)");
            f.Release([6]); await T.Eventually(() => c.InFlightCount == 0);
            T.Check((await c.GetAsync("x"))[0] == 6 && f.Calls == 2, "its result is still cached; no extra run");
            int dup = 0;
            for (int i = 0; i < 200; i++)
            {
                clock.Advance(Ttl); int before = f.Calls;
                using var s = new CancellationTokenSource();
                var g1 = c.GetAsync("x", s.Token); var g2 = c.GetAsync("x");
                await T.Eventually(() => f.Calls == before + 1);
                var cancel = Task.Run(() => s.Cancel()); f.Release([(byte)i]);
                await cancel; try { await g1; } catch (OperationCanceledException) { }
                await g2; if (f.Calls != before + 1) dup++;
            }
            T.Check(dup == 0, "200 cancel-vs-completion races: never a duplicate factory run");
        }
        T.Report("T3 one waiter cancels / all waiters cancel / cancellation racing completion: no duplicate factory runs");

        T.Begin();   // T4 exception, cancellation, retry, Dispose races, no unobserved exceptions
        {
            var clock = new FakeClock(t0); var f = new GatedFactory();
            var c = new SingleFlightCache(f.Run, clock, Ttl);
            var e1 = c.GetAsync("e"); var e2 = c.GetAsync("e"); await T.Eventually(() => f.Calls == 1);
            f.Fail(new InvalidOperationException("boom"));
            T.Check(await T.ThrowsAsync<InvalidOperationException>(() => e1) && await T.ThrowsAsync<InvalidOperationException>(() => e2), "factory exception reaches every waiter");
            var r1 = c.GetAsync("e"); await T.Eventually(() => f.Calls == 2);
            T.Check(f.Calls == 2, "failure not cached: next call runs the factory again");
            f.Cancel(); T.Check(await T.ThrowsAsync<OperationCanceledException>(() => r1), "factory cancellation reaches the waiter");
            var r2 = c.GetAsync("e"); await T.Eventually(() => f.Calls == 3); f.Release([4]);
            T.Check((await r2)[0] == 4, "retry after cancellation succeeds");
            var syncThrow = new SingleFlightCache((k, ct) => throw new FormatException("sync"), clock, Ttl);
            T.Check(await T.ThrowsAsync<FormatException>(() => syncThrow.GetAsync("s")), "synchronously throwing factory -> exception to the caller");
            await syncThrow.DisposeAsync();
            var inflight = c.GetAsync("d"); await T.Eventually(() => f.Calls == 4);
            var disposing = c.DisposeAsync().AsTask();
            T.Check(await T.ThrowsAsync<OperationCanceledException>(() => inflight), "DisposeAsync cancels the factory token; waiters see cancellation");
            await disposing;
            T.Check(f.Tokens[3].IsCancellationRequested && c.InFlightCount == 0, "factory token cancelled; nothing in flight after DisposeAsync");
            T.Check(await T.ThrowsAsync<ObjectDisposedException>(() => c.GetAsync("e")), "GetAsync after DisposeAsync -> ObjectDisposedException");
            await c.DisposeAsync(); T.Check(true, "second DisposeAsync returns");
            var g = new GatedFactory { IgnoreCancellation = true };
            var c2 = new SingleFlightCache(g.Run, clock, Ttl);
            var w = c2.GetAsync("i"); await T.Eventually(() => g.Calls == 1);
            var d2 = c2.DisposeAsync().AsTask(); await Task.Delay(50);
            T.Check(!d2.IsCompleted && !w.IsCompleted, "factory ignoring cancellation: DisposeAsync keeps waiting (documented)");
            g.Release([8]); await d2;
            T.Check((await w)[0] == 8, "the waiter still gets the result (not silently dropped, no hang)");
            int ok = 0;
            for (int i = 0; i < 200; i++)
            {
                var gf = new GatedFactory(); var cc = new SingleFlightCache(gf.Run, clock, Ttl);
                var get = Task.Run(() => cc.GetAsync("r")); var dis = Task.Run(async () => await cc.DisposeAsync());
                await Task.WhenAny(Task.Delay(1), get);
                gf.Release([1]);
                try { await get; ok++; } catch (ObjectDisposedException) { ok++; } catch (OperationCanceledException) { ok++; }
                await dis;
            }
            T.Check(ok == 200, "200 GetAsync vs DisposeAsync races: each ends with a value, ObjectDisposed or cancellation; no hang");
            for (int i = 0; i < 3; i++) { GC.Collect(); GC.WaitForPendingFinalizers(); }
            T.Check(s_unobserved == 0, $"no unobserved task exceptions ({s_unobserved})");
        }
        T.Report("T4 exception / cancellation / success paths, retry, DisposeAsync vs GetAsync, factory ignoring cancellation, no unobserved exceptions");

        Console.WriteLine($"RESULT: {T.Pass} checks passed, {T.Fail} failed");
        return T.Fail == 0 ? 0 : 1;
    }
    static bool Throws(Action a) { try { a(); } catch (ArgumentOutOfRangeException) { return true; } return false; }
}
```

#### 5. 検証結果（期待結果・実測結果・実施状況）

実行結果は `RESULT: 30 checks passed, 0 failed`、終了コード 0 でした。時計は独自の `FakeClock : TimeProvider` で、`Thread.Sleep` は使っていません。

| テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 同じキーへの100同時呼出し | factory の実行は1回、全員が値を受け取る | 1回 | 実施 |
| 異なるキー | 別々の factory が並行して実行中になる | 2件同時に実行中 | 実施 |
| 返却する byte[] | 100個すべて別の配列で、factory の配列とも別 | 一致 | 実施 |
| 書換えの独立性 | 返却配列や factory の配列を書き換えても、他の利用者とキャッシュは不変 | 一致 | 実施 |
| TTL の直前・ちょうど・直後 | 公開から TTL−1 tick は有効、ちょうどは期限切れ（再実行）、直後も期限切れ | 一致 | 実施 |
| factory の実行中に時計が進む | TTL は公開時刻から数える（実行中に 100 秒進めても、公開直後は有効） | 一致 | 実施 |
| 期限切れの100件の再集約 | factory の実行は1回で、全員が新しい値 | 一致 | 実施 |
| ttl ≤ 0 | ArgumentOutOfRangeException | 一致 | 実施 |
| 1人だけ待機をキャンセル | その人だけキャンセル。共有の実行と factory のトークンは無事で、他の人は値を受け取る | 一致 | 実施 |
| 全員が待機をキャンセル | factory は続行し（キャンセルされない）、その結果はキャッシュされ、次の呼出しは実行なしで当たる | 一致 | 実施 |
| キャンセルと完了の競合 ×200 | 重複した factory の実行は 0 | 0 | 実施 |
| factory の例外 / キャンセル / 正常値 | 例外は全待機者へ、キャンセルも伝わる。どちらもキャッシュせず、次の呼出しで再実行し成功する | 一致 | 実施 |
| 同期的に例外を投げる factory | 呼出し元へ伝わる | 一致 | 実施 |
| DisposeAsync と実行中の factory | 寿命トークンがキャンセルされ、待機者はキャンセルを受け取る。実行中は0件になる。その後の GetAsync は ObjectDisposedException。二重 Dispose も可 | 一致 | 実施 |
| キャンセルを無視する factory | DisposeAsync は待ち続け（仕様の明記どおり）、解放後に完了する。待機者は値を受け取る（黙って捨てない・ハングしない） | 一致 | 実施 |
| Dispose と Get の競合 ×200 | 値・ObjectDisposedException・キャンセルのいずれかで終わり、ハングしない | 200/200 | 実施 |
| 未監視の例外 | GC の後も `UnobservedTaskException` は 0 件 | 0 | 実施 |

**自律修正：**
- 実装：最初の版で、実行中タスクの管理に不要なコード（AsyncLocal など）が混ざっていました。テストの前に、継続タスクで「例外の観測と実行中リストからの削除」を行う形へ整理しました。
- テスト：テスト用の偽ファクトリーはゲートを全実行で共有するため、キー b の期待値が誤っていました。修正しました。

#### 6. 技術的な正当性と設計判断

- **Single-flight の線形化点：** `_gate` ロックの中で「エントリーが実行中ならそれに参加し、なければ新しい実行を登録する」を判定する時点です。登録と参加が同じロックの中で決まるので、同じキーの factory は同時に最大1つです。
- **キャンセル：** `Task.WaitAsync(CancellationToken)` は待機だけを止め、共有タスクには影響しません。factory には寿命トークンだけを渡します。
- **例外の観測：** 共有タスクには、常に `t.Exception` を読む継続を付けます。待機者が全員キャンセルした失敗でも、未監視の例外になりません。
- **寿命：** DisposeAsync は寿命トークンをキャンセルした後、実行中の全タスクを待ちます。CTS の Dispose は、それらの完了後に行います。Dispose 中に成功した結果は、待機者へ渡しますが、キャッシュはしません。
- **TTL：** 判定は `TimeProvider.GetUtcNow()` だけで行い、`now < ExpiresAt` の間だけ有効です（期限ちょうどは期限切れ）。

#### 7. 未実施項目・制約・残課題

- 制約：キャンセルを無視する factory がある場合、DisposeAsync はその factory が終わるまで完了しません（仕様として明記）。
- 期限切れのエントリーは、次のアクセスまで残ります（定期的な掃除はしません）。
- 未実施の項目はありません。

---

## Q05【地獄級】ImmutableArrayとCASによる銀行台帳の線形化可能な送金

**試験の狙い：** 不変スナップショット、資金保存則、Interlocked CAS、整数境界、競合時の再試行を保証する。

### 元コード（危険・不完全）

```csharp
using System;
class LedgerBad {
    public long[] balances = new long[32];
    public void Send(int from,int to,long n) {
        balances[from] -= n;
        balances[to] += n; // 途中状態の公開・残高不足・桁あふれ
    }
}
```

### 要求仕様

1. 32口座固定の `Ledger` を実装。状態は `record` 等の公開後変更不能なSnapshotと `ImmutableArray<long> Balances`（または同等の変造不可能な所有データ）で表し、`long Version` を持つ。`Snapshot Read()` と `bool TryTransfer(int from,int to,long cents)` を提供する。
2. コンストラクタは長さ32、全要素0以上、総和がlong.MaxValue以下の初期残高を受け付ける。違反はArgumentException/ArgumentOutOfRangeException。各残高はセント単位のlongで管理し、浮動小数点は使用しない。
3. 送金はfrom/to 0..31・別口座・cents>0のときだけ有効。残高不足または送金後の整数桁あふれはfalse。**失敗時は公開されているSnapshot参照もVersionも変わらない。**
4. 成功時はfromから引きtoに足し、総額不変・非負を保つ。Versionは成功した送金1件につきちょうど1増加し、long.MaxValueに達したらそれ以上の成功を認めない。
5. 公開状態は永続的に不変。更新候補は私有コピーを作り、`Interlocked.CompareExchange` による**参照CAS**成功時だけ公開。CAS失敗時は最新Snapshotを読んで条件を再評価して再試行。lockは禁止。
6. Readは一貫した全残高とVersionを同時に観測し、外部が受け取ったSnapshotを変更する経路を提供しない。例外時に既存状態を変更しない。Versionをlong.MaxValue付近に設定するテスト専用手段は本番APIから分離してよい。

### 必須検証

1. 0番/31番、自己送金、0円、負数、口座範囲外、残高不足、long.MaxValue、Version上限。
2. 逐次1万送金を参照モデルと比較し、古いSnapshot100個の不変と参照同一性を確認。
3. 8スレッド合計100万回のランダム送金。終了後の資金保存則、非負、Version=成功件数、取りこぼし/二重反映0。
4. CAS失敗を意図的に発生させるフック、候補生成時の例外、古いスナップショットの寿命とリソース管理。

### ★★FREYJAの回答【Q05・記入欄】

> **以下は★★FREYJA専用の解答領域。** 問題本文と元コードには触れず、7項目をすべて実際の回答で置き換える。未実施の検証は未実施と正直に記載する。

#### 1. 根本原因・危険性・影響

**元コード `LedgerBad.Send` の問題点**

| # | 根本原因 | 実害 |
| --- | --- | --- |
| 1 | 公開されている `long[]` を2回に分けて書き換える | 読み手が「引いたが足していない」途中状態を見る（総額が一時的に合わない） |
| 2 | 同期がない | 並列の `-=` / `+=` で更新が消える（ロストアップデート）、資金が増減する |
| 3 | 残高不足の検査がない | 負の残高 |
| 4 | 桁あふれの検査がない | `long` が周回して残高が壊れる |
| 5 | 口座番号・金額・自己送金の検査がない | 範囲外の添字の例外、負の送金による逆向きの移動 |
| 6 | `public` な配列 | 外部から直接書き換えられる |

**修正方針：** 状態は `record Snapshot(ImmutableArray<long> Balances, long Version)` で、公開した後は変更しません。
- `TryTransfer` は、最新のスナップショットを読んで条件（範囲・残高・桁あふれ・Version の上限）を評価し、私的な配列で新しい候補を作ります。そして `Interlocked.CompareExchange(ref _current, candidate, s)` の**参照 CAS** で公開します。
- CAS が失敗したら、最新の値を読み直して、条件の評価からやり直します。
- 失敗時（false または例外）は、公開中の参照も Version も一切変わりません。lock は使いません。

#### 2. 完全な修正版C#コード

```csharp
// Q05: Ledger - 32 accounts, immutable snapshots published by reference CAS (C# 14 / .NET 10)
// Original implementation by ★★FREYJA for the C# hell-level exam.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Exam;

/// <summary>Immutable published state: balances in cents and the number of successful transfers.</summary>
public sealed record Snapshot(ImmutableArray<long> Balances, long Version);

public sealed class Ledger
{
    public const int Accounts = 32;

    Snapshot _current;

    // Test-only hooks (internal: not part of the public API).
    internal static Action? BeforeCas;                       // runs between building the candidate and the CAS
    internal static Action? WhileBuildingCandidate;          // may throw to simulate a failure while building

    public Ledger(IReadOnlyList<long> initialBalances) : this(initialBalances, 0) { }

    /// <summary>Test-only: start at an arbitrary version (e.g. near long.MaxValue).</summary>
    internal Ledger(IReadOnlyList<long> initialBalances, long initialVersion)
    {
        ArgumentNullException.ThrowIfNull(initialBalances);
        if (initialBalances.Count != Accounts) throw new ArgumentException($"exactly {Accounts} balances are required", nameof(initialBalances));
        ArgumentOutOfRangeException.ThrowIfNegative(initialVersion);
        long total = 0;
        var b = ImmutableArray.CreateBuilder<long>(Accounts);
        for (int i = 0; i < Accounts; i++)
        {
            long v = initialBalances[i];
            if (v < 0) throw new ArgumentOutOfRangeException(nameof(initialBalances), v, $"balance {i} is negative");
            if (v > long.MaxValue - total) throw new ArgumentException("total of balances exceeds long.MaxValue", nameof(initialBalances));
            total += v;
            b.Add(v);
        }
        _current = new Snapshot(b.MoveToImmutable(), initialVersion);
    }

    /// <summary>The current committed snapshot (all balances and the version observed together).</summary>
    public Snapshot Read() => Volatile.Read(ref _current);

    public bool TryTransfer(int from, int to, long cents)
    {
        if ((uint)from >= Accounts || (uint)to >= Accounts || from == to || cents <= 0) return false;
        while (true)
        {
            Snapshot s = Volatile.Read(ref _current);
            if (s.Version == long.MaxValue) return false;                // no more successful transfers
            long fb = s.Balances[from], tb = s.Balances[to];
            if (fb < cents) return false;                                // insufficient funds
            if (tb > long.MaxValue - cents) return false;                // would overflow (cannot happen with total <= MaxValue)
            WhileBuildingCandidate?.Invoke();
            long[] next = new long[Accounts];                            // private copy
            s.Balances.CopyTo(next);
            next[from] = fb - cents;
            next[to] = tb + cents;
            var candidate = new Snapshot(ImmutableArray.Create(next), s.Version + 1);
            BeforeCas?.Invoke();
            if (ReferenceEquals(Interlocked.CompareExchange(ref _current, candidate, s), s))
                return true;                                             // linearization point
            // lost the race: re-read the newest snapshot and re-evaluate every condition
        }
    }
}
```

#### 3. ビルド方法・使用環境

```powershell
# 作業フォルダー: scratchpad\cs\q05（試験専用の一時領域）。cs\build.ps1 -Q q05 が次を順に実行する
# (1) 下記の q05.csproj を生成  (2) Release ビルド  (3) テスト実行（終了コード 0 = 全件合格）
dotnet build q05.csproj -c Release -nologo
dotnet bin\Release\net10.0\q05.dll
# 結果: ビルド成功・0 個の警告・0 エラー、実行の終了コード 0
#
# q05.csproj（全問共通。AssemblyName だけが問題ごとに異なる）
# <Project Sdk="Microsoft.NET.Sdk">
#   <PropertyGroup>
#     <OutputType>Exe</OutputType>
#     <TargetFramework>net10.0</TargetFramework>
#     <LangVersion>14.0</LangVersion>
#     <Nullable>enable</Nullable>
#     <ImplicitUsings>disable</ImplicitUsings>
#     <AnalysisLevel>latest</AnalysisLevel>
#     <AllowUnsafeBlocks>false</AllowUnsafeBlocks>
#     <InvariantGlobalization>true</InvariantGlobalization>
#     <AssemblyName>q05</AssemblyName>
#     <RootNamespace>Exam</RootNamespace>
#   </PropertyGroup>
# </Project>
```

**使用環境（全問共通）**

- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド）
- ランタイム：**Microsoft.NETCore.App 10.0.15（.NET 10）**。実行時に `Environment.Version` = 10.0.15 であることを確認しました。
- 言語とターゲット：`TargetFramework=net10.0`、`LangVersion=14.0`（C# 14。C# 14 の extension メンバーのコンパイルも確認済み）、`Nullable=enable`、`AnalysisLevel=latest`、`ImplicitUsings=disable`、`AllowUnsafeBlocks=false`、構成は **Release**。
- SDK：この PC にインストールされている SDK は **.NET SDK 11.0.100** だけです。.NET 10 用の参照パック（10.0.15）を使い、`net10.0` 向けにビルドしました。.NET 10 SDK そのものはインストールされておらず、無断導入の禁止に従って追加していません。そのため、コンパイラは SDK 11 に同梱のもので、言語バージョンを 14.0 に固定して使っています。
- NuGet パッケージ・第三者ライブラリ：使用していません（.NET 共有フレームワークの API だけ）。
- 各問は独立したコンソールプロジェクトです（`qNN.csproj` = 下記の内容、`QNN.cs` = 実装、`Tests.cs` = テスト）。ビルド時の警告は全問 **0 件**、テストの終了コードは全問 0 です。

#### 4. 実際に使用したテストコード

```csharp
// Q05 tests
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Exam;

static class T
{
    public static int Pass, Fail;
    static int _f0;
    public static void Check(bool ok, string what) { if (ok) Pass++; else { Fail++; Console.WriteLine($"  FAIL: {what}"); } }
    public static void Begin() => _f0 = Fail;
    public static void Report(string name) => Console.WriteLine($"[{(Fail == _f0 ? "PASS" : "FAIL")}] {name}");
    public static bool Throws<E>(Action a) where E : Exception { try { a(); } catch (E) { return true; } catch { return false; } return false; }
}

static class Program
{
    static long[] Init(long each) => Enumerable.Repeat(each, 32).ToArray();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static WeakReference MakeOldSnapshotRef(Ledger l) { var w = new WeakReference(l.Read()); l.TryTransfer(0, 1, 1); return w; }

    static int Main()
    {
        T.Begin();   // T1 argument and boundary rules
        {
            T.Check(T.Throws<ArgumentException>(() => new Ledger(new long[31])), "31 balances rejected");
            T.Check(T.Throws<ArgumentOutOfRangeException>(() => new Ledger([.. Init(0)[..31], -1])), "negative balance rejected");
            T.Check(T.Throws<ArgumentException>(() => new Ledger([long.MaxValue, 1, .. new long[30]])), "total above long.MaxValue rejected");
            var l = new Ledger(Init(100));
            var s0 = l.Read();
            T.Check(l.TryTransfer(0, 31, 40) && l.Read().Balances[0] == 60 && l.Read().Balances[31] == 140 && l.Read().Version == 1, "account 0 -> 31");
            var s1 = l.Read();
            bool allFalse = !l.TryTransfer(5, 5, 1) && !l.TryTransfer(1, 2, 0) && !l.TryTransfer(1, 2, -5) && !l.TryTransfer(-1, 2, 1) && !l.TryTransfer(1, 32, 1) && !l.TryTransfer(32, 1, 1) && !l.TryTransfer(3, 4, 101);
            T.Check(allFalse, "self transfer, 0 / negative cents, account -1 / 32, insufficient funds -> false");
            T.Check(ReferenceEquals(l.Read(), s1) && l.Read().Version == 1, "failed transfers change neither the published reference nor the version");
            T.Check(s0.Balances[0] == 100 && s0.Version == 0, "the old snapshot is unchanged");
            var big = new Ledger([long.MaxValue, .. new long[31]]);
            T.Check(big.TryTransfer(0, 1, long.MaxValue) && big.Read().Balances[1] == long.MaxValue && big.Read().Balances[0] == 0, "transfer of long.MaxValue cents");
            T.Check(!big.TryTransfer(0, 1, 1), "no funds left");
            var v = new Ledger(Init(10), long.MaxValue - 1);
            T.Check(v.TryTransfer(0, 1, 1) && v.Read().Version == long.MaxValue, "version reaches long.MaxValue");
            var vs = v.Read();
            T.Check(!v.TryTransfer(1, 0, 1) && ReferenceEquals(v.Read(), vs), "at long.MaxValue no further success; state unchanged");
        }
        T.Report("T1 accounts 0/31, self, 0, negative, out of range, insufficient, long.MaxValue, version limit");

        T.Begin();   // T2 10,000 sequential transfers vs reference; 100 old snapshots
        {
            var l = new Ledger(Init(1000)); var model = Init(1000); long version = 0;
            var rnd = new Random(1); var kept = new List<(Snapshot s, long[] copy, long ver)>(); int bad = 0;
            for (int i = 0; i < 10000; i++)
            {
                int f = rnd.Next(-1, 33), t = rnd.Next(-1, 33); long c = rnd.Next(-5, 1500);
                bool exp = f is >= 0 and < 32 && t is >= 0 and < 32 && f != t && c > 0 && model[f] >= c;
                bool got = l.TryTransfer(f, t, c);
                if (exp) { model[f] -= c; model[t] += c; version++; }
                if (got != exp || !l.Read().Balances.SequenceEqual(model) || l.Read().Version != version) bad++;
                if (i % 100 == 0) { var s = l.Read(); kept.Add((s, s.Balances.ToArray(), s.Version)); }
            }
            T.Check(bad == 0, $"10,000 transfers match the reference model ({bad} mismatches)");
            T.Check(kept.All(k => k.s.Balances.SequenceEqual(k.copy) && k.s.Version == k.ver && k.s.Balances.Sum() == 32_000), "100 old snapshots unchanged and consistent");
            var cur = l.Read(); T.Check(ReferenceEquals(cur, l.Read()), "Read without changes returns the identical snapshot object");
        }
        T.Report("T2 10,000 sequential transfers vs reference model, 100 retained snapshots immutable");

        T.Begin();   // T3 8 threads x 125,000 random transfers
        {
            const long each = 1_000_000;
            var l = new Ledger(Init(each)); long success = 0;
            var deltas = new long[8][];
            Parallel.For(0, 8, new ParallelOptions { MaxDegreeOfParallelism = 8 }, th =>
            {
                var d = deltas[th] = new long[32]; var rnd = new Random(th * 7919 + 3); long ok = 0;
                for (int i = 0; i < 125_000; i++)
                {
                    int f = rnd.Next(32), t = rnd.Next(32); long c = rnd.Next(1, 5000);
                    if (l.TryTransfer(f, t, c)) { d[f] -= c; d[t] += c; ok++; }
                }
                Interlocked.Add(ref success, ok);
            });
            var fin = l.Read();
            var expect = Enumerable.Range(0, 32).Select(a => each + deltas.Sum(d => d[a])).ToArray();
            T.Check(fin.Balances.Sum() == 32 * each, "money conserved");
            T.Check(fin.Balances.All(b => b >= 0), "no negative balance");
            T.Check(fin.Version == success, $"Version {fin.Version} == successful transfers {success}");
            T.Check(fin.Balances.SequenceEqual(expect), "final balances == initial + every thread's successful transfers (no loss, no double application)");
            Console.WriteLine($"  1,000,000 attempts on 8 threads: {success} succeeded");
        }
        T.Report("T3 8 threads, 1,000,000 random transfers: conservation, non-negative, Version = successes, exact per-account totals");

        T.Begin();   // T4 forced CAS failures, exception while building, snapshot lifetime
        {
            var l = new Ledger(Init(100)); int injections = 3, hookCalls = 0;
            Ledger.BeforeCas = () => { hookCalls++; if (injections > 0) { injections--; l.TryTransfer(10, 11, 1); } };
            bool ok = l.TryTransfer(0, 1, 50);
            Ledger.BeforeCas = null;
            var s = l.Read();
            T.Check(ok && s.Balances[0] == 50 && s.Balances[1] == 150 && s.Balances[10] == 97 && s.Balances[11] == 103 && s.Version == 4, "3 injected competing commits: the retried transfer and all 3 competitors applied exactly once");
            T.Check(hookCalls == 7, $"CAS failed and was retried (hook calls {hookCalls})");
            var before = l.Read();
            Ledger.WhileBuildingCandidate = () => throw new InvalidOperationException("injected");
            T.Check(T.Throws<InvalidOperationException>(() => l.TryTransfer(2, 3, 1)), "exception while building the candidate propagates");
            Ledger.WhileBuildingCandidate = null;
            T.Check(ReferenceEquals(l.Read(), before), "state unchanged after the exception");
            var weak = MakeOldSnapshotRef(l);
            for (int i = 0; i < 3; i++) { GC.Collect(); GC.WaitForPendingFinalizers(); }
            T.Check(!weak.IsAlive, "an old snapshot nobody references is reclaimed by the GC (no retention by the ledger)");
            var held = l.Read(); var heldCopy = held.Balances.ToArray(); long heldVer = held.Version;
            for (int i = 0; i < 100; i++) l.TryTransfer(i % 32, (i + 1) % 32, 1);
            GC.Collect(); GC.WaitForPendingFinalizers();
            T.Check(held.Balances.SequenceEqual(heldCopy) && held.Version == heldVer && l.Read().Version == heldVer + 100, "a snapshot still referenced stays valid and unchanged across 100 later commits and GC");
        }
        T.Report("T4 forced CAS failures via hook, exception while building the candidate, snapshot lifetime");

        Console.WriteLine($"RESULT: {T.Pass} checks passed, {T.Fail} failed");
        return T.Fail == 0 ? 0 : 1;
    }
}
```

#### 5. 検証結果（期待結果・実測結果・実施状況）

実行結果は `RESULT: 24 checks passed, 0 failed`、終了コード 0、所要 0.83 秒でした。

| テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| コンストラクターの検証 | 31口座 → ArgumentException、負の残高 → ArgumentOutOfRangeException、総和が long.MaxValue 超 → ArgumentException | 一致 | 実施 |
| 0番 → 31番 | 成功、Version 1 | 一致 | 実施 |
| 自己送金・0円・負数・口座 −1/32・残高不足 | すべて false。公開中の参照も Version も不変 | 一致 | 実施 |
| long.MaxValue | long.MaxValue セントの送金が成功し、その後は残高不足 | 一致 | 実施 |
| Version の上限 | テスト用コンストラクターで MaxValue−1 から開始し、1件成功で MaxValue、その後は false で不変 | 一致 | 実施 |
| 逐次1万送金と参照モデル | 各結果・残高・Version が一致 | 不一致0 | 実施 |
| 古い Snapshot 100個 | 内容と Version が不変、総額も整合。変更がなければ Read は同じオブジェクト | 一致 | 実施 |
| 8スレッド・合計100万回のランダム送金 | 資金保存則、非負、Version = 成功件数。最終残高 = 初期値 + 各スレッドが成功させた送金の合計（取りこぼし・二重反映なし） | 成功 966,932 件、すべて一致 | 実施 |
| CAS 失敗を起こすフック | CAS 直前のフックから別の送金を割り込ませる（入れ子で計3回）。割り込まれた側の CAS はすべて失敗して最新値から再評価され、4件の送金がちょうど1回ずつ反映される（Version 4） | 一致（フック呼出し7回 = 成功4回 + 失敗した CAS 3回） | 実施 |
| 候補生成中の例外 | 例外が伝わり、公開中の参照は同じオブジェクト | 一致 | 実施 |
| 古いスナップショットの寿命 | 誰も参照しない古いスナップショットは GC で回収される（台帳が保持しない）。参照中のものは、後の100件の確定と GC の後も有効で不変 | 一致 | 実施 |

#### 6. 技術的な正当性と設計判断

- **不変性：** `ImmutableArray<long>` は書換えの手段を外部に提供しません。`record` の `with` は新しいインスタンスを作るので、既存のスナップショットを変えることはできません。候補は私的な `long[]` で組み立て、`ImmutableArray.Create` で複製してから公開します。
- **線形化点：** 成功した送金は、参照 CAS が成功した時点です。失敗する送金（条件不成立）は、判定に使ったスナップショットを読んだ時点です。Read は `Volatile.Read` の時点です。
- **メモリ順序：** .NET の `Interlocked.CompareExchange` は完全なフェンスです。候補のフィールドの初期化は、公開より前に完了しています。`Volatile.Read` で参照を得た読み手は、完成したスナップショットだけを見ます。全残高と Version は同じオブジェクトにあるので、一貫して観測されます。
- **整数境界：** 総和 ≤ long.MaxValue なので、送金先の加算はあふれませんが、念のため `tb > long.MaxValue − cents` も判定します。Version は MaxValue に達したら、以後の成功を認めません。
- **進行：** CAS が失敗するのは、他の送金が成功した場合だけです。そのためシステム全体として進行します（lock-free）。

#### 7. 未実施項目・制約・残課題

- `BeforeCas`・`WhileBuildingCandidate`・Version を指定するコンストラクターは `internal` のテスト専用機能で、本番の公開 API とは分けています。
- 送金のたびに 32 要素の配列を2回確保します（私的な配列と ImmutableArray）。
- 未実施の項目はありません。

---

## Q06【地獄級】SafeHandleとDangerousAddRefによる外部リソース寿命の完全管理

**試験の狙い：** Disposeと進行中I/Oの競合、正確に1回の解放、例外安全、ネイティブハンドルの所有権を検証する。

### 元コード（危険・不完全）

```csharp
using System;
class NativeLikeBad : IDisposable {
    IntPtr handle;
    public int Read(Span<byte> b) => FakeNative.Read(handle,b);
    public void Dispose() => FakeNative.Close(handle); // 読込中に閉じる・二重解放
}
```

### 要求仕様

1. **外部DLLを使わない試験**とする。`IHandleBackend` を `IntPtr Open()`、`int Read(IntPtr handle,Span<byte> dst)`、`void Close(IntPtr handle)` として定義。テスト時はこのインターフェイスの偽実装で生存数と実行順序を制御する。
2. `SafeHandle` を継承する所有権型と、それを利用する `HandleSession : IDisposable` を独自実装する。APIは `HandleSession(IHandleBackend backend)`、`int Read(Span<byte> buffer)`、`void Dispose()`。
3. Openは**非0のハンドル**を返す契約。Open失敗・コンストラクタ失敗でリソースリークを起こさない。ReleaseHandleは成功したOpenに対して最終的にちょうど1回だけbackend.Closeを呼ぶ。Closeは例外を投げない契約とする。
4. ReadはDangerousAddRef/DangerousReleaseなどSafeHandleの正式な寿命管理APIを適切に使用し、**Read処理中に別スレッドでDisposeされてもCloseがReadの終了前に走らない**ようにする。
5. Dispose開始後の新規ReadはObjectDisposedException。DisposeとReadが競合した場合は、Readが有効に開始されたかDisposeが先だったかにより成功またはObjectDisposedExceptionとし、UAF・二重解放は一切禁止。
6. Readがbackendから例外を受けてもハンドル参照を必ず解放する。Disposeは冪等。GCファイナライザの即時実行を前提にしたテストは禁止し、明示的Disposeで正確性を確認する。
7. 外部OSハンドルやP/Invokeは不要。問題の目的は.NETのSafeHandle契約に従う寿命管理であり、DLL導入や実運用ファイルへの接続は禁止。

### 必須検証

1. Open/Read/Dispose通常系、Open失敗、Readの例外、Dispose二重呼出し、Dispose後Read。
2. Readがブロックしている間に別スレッドでDispose、Close時刻がRead終了後であることをTaskCompletionSource等の同期で確認。
3. 8並列Read＋Dispose競合を各1000回、Close回数=Open成功回数、使用中ハンドルのCloseなし。
4. GC.Collectに依存しない明示的解放と、SafeHandle所有権の例外安全。

### ★★FREYJAの回答【Q06・記入欄】

> **以下は★★FREYJA専用の解答領域。** 問題本文と元コードには触れず、7項目をすべて実際の回答で置き換える。未実施の検証は未実施と正直に記載する。

#### 1. 根本原因・危険性・影響

**元コード `NativeLikeBad` の問題点**

| # | 根本原因 | 発生条件 | 実害 |
| --- | --- | --- | --- |
| 1 | `Dispose` が、進行中の `Read` を待たずに `Close` する | 別スレッドの Read 中に Dispose | 閉じた（場合によっては再利用された）ハンドルの使用（use-after-free 相当）、別のリソースへの誤った読み書き |
| 2 | 二重解放の防止がない | Dispose の2回呼出し、ファイナライザとの競合 | 二重 Close。再利用された別のハンドルを閉じる |
| 3 | 生の IntPtr を所有権管理なしで持つ | Open の後の例外 | リーク |
| 4 | Dispose 後の Read を検出しない | — | 閉じたハンドルの使用 |
| 5 | ファイナライザ・SafeHandle がない | Dispose 忘れ | リソースリーク |

**修正方針：**
- `SafeHandle` を継承した `BackendSafeHandle` が、ハンドルを所有します。Open の前に作成して、所有されないハンドルが生じる隙をなくします。`ReleaseHandle` で `backend.Close` を呼びます。
- `Read` は `DangerousAddRef` → `backend.Read` → `finally` で `DangerousRelease` の順で、参照カウントを持ちます。
- `Dispose` は `SafeHandle.Dispose` を呼びます。参照カウントが残っている間は、SafeHandle 自身が ReleaseHandle を遅らせ、最後の `DangerousRelease` の時点でちょうど1回 Close します。
- Dispose の後は `DangerousAddRef` が `ObjectDisposedException` を投げるので、新規の Read を拒否します。

#### 2. 完全な修正版C#コード

```csharp
// Q06: HandleSession - SafeHandle reference counting so Close never runs during Read (C# 14 / .NET 10)
// No P/Invoke, no native DLL: the "native" side is the IHandleBackend abstraction.
// Original implementation by ★★FREYJA for the C# hell-level exam.
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Exam;

public interface IHandleBackend
{
    IntPtr Open();                              // returns a non-zero handle (contract)
    int Read(IntPtr handle, Span<byte> dst);
    void Close(IntPtr handle);                  // does not throw (contract)
}

/// <summary>Owns one backend handle. ReleaseHandle runs exactly once, after the last DangerousRelease.</summary>
public sealed class BackendSafeHandle : SafeHandle
{
    readonly IHandleBackend _backend;

    internal BackendSafeHandle(IHandleBackend backend) : base(IntPtr.Zero, ownsHandle: true) => _backend = backend;

    public override bool IsInvalid => handle == IntPtr.Zero;

    internal void Adopt(IntPtr raw) => SetHandle(raw);

    protected override bool ReleaseHandle()
    {
        _backend.Close(handle);                 // the SafeHandle guarantees: once, and only when no AddRef is outstanding
        return true;
    }
}

public sealed class HandleSession : IDisposable
{
    readonly IHandleBackend _backend;
    readonly BackendSafeHandle _handle;
    int _disposed;

    public HandleSession(IHandleBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);
        _backend = backend;
        var h = new BackendSafeHandle(backend);  // created before Open: no window where a raw handle is unowned
        IntPtr raw;
        try
        {
            raw = backend.Open();
        }
        catch
        {
            h.Dispose();                         // invalid handle: ReleaseHandle is not called, nothing leaks
            throw;
        }
        if (raw == IntPtr.Zero)
        {
            h.Dispose();
            throw new InvalidOperationException("backend.Open returned a zero handle (contract violation)");
        }
        h.Adopt(raw);                            // from now on the SafeHandle owns it
        _handle = h;
    }

    public int Read(Span<byte> buffer)
    {
        if (Volatile.Read(ref _disposed) != 0) throw new ObjectDisposedException(nameof(HandleSession));
        bool added = false;
        try
        {
            try
            {
                _handle.DangerousAddRef(ref added);   // fails once Dispose has marked the handle closed
            }
            catch (ObjectDisposedException)
            {
                throw new ObjectDisposedException(nameof(HandleSession));
            }
            return _backend.Read(_handle.DangerousGetHandle(), buffer);
        }
        finally
        {
            if (added) _handle.DangerousRelease();   // last release after Dispose triggers ReleaseHandle -> Close
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;   // idempotent
        _handle.Dispose();                       // closes now, or defers Close until in-flight Reads release
    }
}
```

#### 3. ビルド方法・使用環境

```powershell
# 作業フォルダー: scratchpad\cs\q06（試験専用の一時領域）。cs\build.ps1 -Q q06 が次を順に実行する
# (1) 下記の q06.csproj を生成  (2) Release ビルド  (3) テスト実行（終了コード 0 = 全件合格）
dotnet build q06.csproj -c Release -nologo
dotnet bin\Release\net10.0\q06.dll
# 結果: ビルド成功・0 個の警告・0 エラー、実行の終了コード 0
#
# q06.csproj（全問共通。AssemblyName だけが問題ごとに異なる）
# <Project Sdk="Microsoft.NET.Sdk">
#   <PropertyGroup>
#     <OutputType>Exe</OutputType>
#     <TargetFramework>net10.0</TargetFramework>
#     <LangVersion>14.0</LangVersion>
#     <Nullable>enable</Nullable>
#     <ImplicitUsings>disable</ImplicitUsings>
#     <AnalysisLevel>latest</AnalysisLevel>
#     <AllowUnsafeBlocks>false</AllowUnsafeBlocks>
#     <InvariantGlobalization>true</InvariantGlobalization>
#     <AssemblyName>q06</AssemblyName>
#     <RootNamespace>Exam</RootNamespace>
#   </PropertyGroup>
# </Project>
```

**使用環境（全問共通）**

- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド）
- ランタイム：**Microsoft.NETCore.App 10.0.15（.NET 10）**。実行時に `Environment.Version` = 10.0.15 であることを確認しました。
- 言語とターゲット：`TargetFramework=net10.0`、`LangVersion=14.0`（C# 14。C# 14 の extension メンバーのコンパイルも確認済み）、`Nullable=enable`、`AnalysisLevel=latest`、`ImplicitUsings=disable`、`AllowUnsafeBlocks=false`、構成は **Release**。
- SDK：この PC にインストールされている SDK は **.NET SDK 11.0.100** だけです。.NET 10 用の参照パック（10.0.15）を使い、`net10.0` 向けにビルドしました。.NET 10 SDK そのものはインストールされておらず、無断導入の禁止に従って追加していません。そのため、コンパイラは SDK 11 に同梱のもので、言語バージョンを 14.0 に固定して使っています。
- NuGet パッケージ・第三者ライブラリ：使用していません（.NET 共有フレームワークの API だけ）。
- 各問は独立したコンソールプロジェクトです（`qNN.csproj` = 下記の内容、`QNN.cs` = 実装、`Tests.cs` = テスト）。ビルド時の警告は全問 **0 件**、テストの終了コードは全問 0 です。

#### 4. 実際に使用したテストコード

```csharp
// Q06 tests
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Exam;

static class T
{
    public static int Pass, Fail;
    static int _f0;
    public static void Check(bool ok, string what) { if (ok) Pass++; else { Fail++; Console.WriteLine($"  FAIL: {what}"); } }
    public static void Begin() => _f0 = Fail;
    public static void Report(string name) => Console.WriteLine($"[{(Fail == _f0 ? "PASS" : "FAIL")}] {name}");
    public static bool Throws<E>(Action a) where E : Exception { try { a(); } catch (E) { return true; } catch { return false; } return false; }
}

/// <summary>Fake "native" backend: detects close-while-reading, double close, use after close.</summary>
sealed class FakeBackend : IHandleBackend
{
    long _next = 0x1000;
    public int Opens, Closes, Violations;
    public bool FailOpen, ReturnZero;
    public Func<IntPtr, int>? ReadBehaviour;
    readonly ConcurrentDictionary<IntPtr, int> _active = new();
    readonly ConcurrentDictionary<IntPtr, byte> _open = new(), _closed = new();
    public readonly ConcurrentQueue<string> Events = new();
    public IntPtr Open()
    {
        if (FailOpen) throw new System.IO.IOException("injected open failure");
        if (ReturnZero) return IntPtr.Zero;
        var h = (IntPtr)Interlocked.Add(ref _next, 16);
        _open[h] = 0; Interlocked.Increment(ref Opens);
        return h;
    }
    public int Read(IntPtr h, Span<byte> dst)
    {
        if (!_open.ContainsKey(h)) Interlocked.Increment(ref Violations);          // read of a closed / unknown handle
        _active.AddOrUpdate(h, 1, (_, v) => v + 1);
        try
        {
            int n = ReadBehaviour?.Invoke(h) ?? 1;
            if (dst.Length > 0) dst[0] = 0x5A;
            return n;
        }
        finally { _active.AddOrUpdate(h, 0, (_, v) => v - 1); Events.Enqueue("read-end"); }
    }
    public void Close(IntPtr h)
    {
        if (_active.TryGetValue(h, out int a) && a > 0) Interlocked.Increment(ref Violations);   // close during a read
        if (!_open.TryRemove(h, out _) || !_closed.TryAdd(h, 0)) Interlocked.Increment(ref Violations);  // double close
        Interlocked.Increment(ref Closes);
        Events.Enqueue("close");
    }
}

static class Program
{
    static int Main()
    {
        T.Begin();   // T1 normal path, Open failure, Read exception, double Dispose, Read after Dispose
        {
            var b = new FakeBackend(); var buf = new byte[4];
            var s = new HandleSession(b);
            T.Check(s.Read(buf) == 1 && buf[0] == 0x5A && b.Opens == 1 && b.Closes == 0, "open + read");
            s.Dispose(); T.Check(b.Closes == 1, "Dispose closes exactly once");
            s.Dispose(); T.Check(b.Closes == 1, "second Dispose is a no-op");
            T.Check(T.Throws<ObjectDisposedException>(() => s.Read(buf)), "Read after Dispose -> ObjectDisposedException");
            var f = new FakeBackend { FailOpen = true };
            T.Check(T.Throws<System.IO.IOException>(() => new HandleSession(f)) && f.Opens == 0 && f.Closes == 0, "Open failure propagates, nothing to close");
            var z = new FakeBackend { ReturnZero = true };
            T.Check(T.Throws<InvalidOperationException>(() => new HandleSession(z)) && z.Closes == 0, "zero handle rejected, Close not called");
            var e = new FakeBackend { ReadBehaviour = _ => throw new System.IO.IOException("read failed") };
            var se = new HandleSession(e);
            T.Check(T.Throws<System.IO.IOException>(() => se.Read(buf)), "backend Read exception propagates");
            se.Dispose();
            T.Check(e.Closes == 1, "after a failed Read the reference was released: Dispose closes immediately");
            T.Check(b.Violations + e.Violations == 0, "no backend contract violation");
            T.Check(T.Throws<ArgumentNullException>(() => new HandleSession(null!)), "null backend rejected");
        }
        T.Report("T1 Open/Read/Dispose, Open failure, zero handle, Read exception, double Dispose, Read after Dispose");

        T.Begin();   // T2 Dispose while a Read blocks: Close only after the Read ends
        {
            var gate = new ManualResetEventSlim(false); var inRead = new ManualResetEventSlim(false);
            var b = new FakeBackend { ReadBehaviour = _ => { inRead.Set(); gate.Wait(); return 7; } };
            var s = new HandleSession(b);
            var reader = Task.Run(() => s.Read(new byte[1]));
            T.Check(inRead.Wait(5000), "reader is inside backend.Read");
            s.Dispose();
            T.Check(b.Closes == 0, "Dispose returned but Close is deferred while the Read is running");
            T.Check(T.Throws<ObjectDisposedException>(() => s.Read(new byte[1])), "a new Read after Dispose -> ObjectDisposedException");
            gate.Set();
            T.Check(reader.Wait(5000) && reader.Result == 7, "the running Read completes successfully");
            T.Check(b.Closes == 1 && b.Events.SequenceEqual(new[] { "read-end", "close" }), "Close ran exactly once, after the Read ended");
            T.Check(b.Violations == 0, "no close during read");
        }
        T.Report("T2 Read blocked in the backend while another thread disposes: Close strictly after the Read");

        T.Begin();   // T3 8 parallel readers racing Dispose, 1000 rounds
        {
            var b = new FakeBackend { ReadBehaviour = _ => { Thread.SpinWait(50); return 1; } };
            long ok = 0, ode = 0, other = 0;
            for (int round = 0; round < 1000; round++)
            {
                var s = new HandleSession(b);
                using var start = new Barrier(9);
                var readers = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
                {
                    start.SignalAndWait(); var buf = new byte[1];
                    for (int k = 0; k < 50; k++)
                    {
                        try { s.Read(buf); Interlocked.Increment(ref ok); }
                        catch (ObjectDisposedException) { Interlocked.Increment(ref ode); return; }
                        catch { Interlocked.Increment(ref other); return; }
                    }
                })).ToArray();
                start.SignalAndWait();
                Thread.SpinWait(round % 50 * 20);
                s.Dispose();
                Task.WaitAll(readers);
            }
            T.Check(b.Opens == 1000 && b.Closes == 1000, $"Close count {b.Closes} == successful Open count {b.Opens}");
            T.Check(b.Violations == 0, $"no Close of a handle in use, no double close, no read after close (violations {b.Violations})");
            T.Check(other == 0, "every Read either succeeded or threw ObjectDisposedException");
            Console.WriteLine($"  1000 rounds x 8 readers: successful reads={ok}, ObjectDisposedException={ode}");
        }
        T.Report("T3 1000 rounds of 8 parallel Reads + Dispose: Close == Open, no close while in use");

        T.Begin();   // T4 explicit release without GC; GC afterwards does not close again
        {
            var b = new FakeBackend(); var sessions = Enumerable.Range(0, 100).Select(_ => new HandleSession(b)).ToList();
            foreach (var s in sessions) { s.Read(new byte[1]); s.Dispose(); }
            T.Check(b.Closes == 100, "100 sessions closed by explicit Dispose (no GC involved)");
            sessions.Clear();
            for (int i = 0; i < 3; i++) { GC.Collect(); GC.WaitForPendingFinalizers(); }
            T.Check(b.Closes == 100 && b.Violations == 0, "a later GC does not close anything again (finalizers suppressed by Dispose)");
        }
        T.Report("T4 explicit Dispose without GC, no double close after a later GC");

        Console.WriteLine($"RESULT: {T.Pass} checks passed, {T.Fail} failed");
        return T.Fail == 0 ? 0 : 1;
    }
}
```

#### 5. 検証結果（期待結果・実測結果・実施状況）

実行結果は `RESULT: 21 checks passed, 0 failed`、終了コード 0 でした。偽のバックエンド `FakeBackend` は、次を違反として数えます。
- Read 中の Close
- 二重 Close
- 閉じたハンドルの Read

| テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| Open / Read / Dispose の通常系 | 読める。Dispose で Close はちょうど1回 | 一致 | 実施 |
| Open の失敗 | 例外が伝わり、Close も 0 回 | 一致 | 実施 |
| 0 のハンドル | InvalidOperationException で、Close は呼ばない | 一致 | 実施 |
| Read の例外 | 例外が伝わり、参照は解放済み（直後の Dispose で即座に Close） | 一致 | 実施 |
| Dispose の二重呼出し / Dispose 後の Read | Close は1回 / ObjectDisposedException | 一致 | 実施 |
| Read がブロック中に別スレッドで Dispose | Dispose はすぐ戻るが、Close は保留される。新規の Read は ObjectDisposedException。Read を解放すると成功して返り、その後に Close が1回（事象の順序が `read-end` → `close`） | 一致 | 実施 |
| 8並列の Read と Dispose の競合 × 1000回 | Close 回数 = Open の成功回数（1000）、使用中のハンドルの Close 0、Read は成功か ObjectDisposedException のどちらか | 1000 = 1000、違反 0（成功した Read 95,621、ODE 7,022） | 実施 |
| GC に依存しない明示的な解放 | 100セッションを Dispose で閉じる（GC を使わない）。その後の GC でも再び Close されない | 一致 | 実施 |

#### 6. 技術的な正当性と設計判断

- **SafeHandle の契約：**
  - `DangerousAddRef(ref bool success)` は、ハンドルがすでに閉じている（Dispose 済み）場合、`ObjectDisposedException` を投げます。成功したら、`DangerousRelease` を必ず1回呼ぶ必要があります（finally で保証しています）。
  - `Dispose` / `Close` は、参照カウントが 0 になったときだけ `ReleaseHandle` を実行します。これは CLR の SafeHandle が内部の状態とカウントを原子的に管理して保証するもので、ReleaseHandle が2回呼ばれることはありません。
- **Read と Dispose の競合：** どちらが先かは、SafeHandle の内部状態の原子的な遷移で決まります。
  - AddRef が先：Read は有効に開始され、Close はその Read の DangerousRelease まで遅れます。
  - Dispose が先：AddRef が失敗し、ObjectDisposedException になります。
  - どちらの場合も、use-after-free と二重解放は起きません。
- **例外安全：** Open が失敗すると、無効なハンドルの SafeHandle を Dispose します（ReleaseHandle は呼ばれません）。backend.Read の例外でも、finally で DangerousRelease します。
- **ファイナライザ：** SafeHandle はクリティカルファイナライザを持ちますが、正しさは明示的な Dispose で確認しています。GC.Collect に依存したテストはしていません。

#### 7. 未実施項目・制約・残課題

- 制約：P/Invoke と実際の OS ハンドルは使っていません（問題文の指定どおり、偽のバックエンドを使用）。`backend.Close` は例外を投げないという契約を前提としています。
- 未実施の項目はありません。

---

## Q07【地獄級】Utf8JsonReaderを利用した厳密なチャンク分割JSON文書検証

**試験の狙い：** ref structの寿命、JsonReaderState、UTF-8とサロゲートの厳密性、数値制限、トークン境界を扱う。

### 元コード（危険・不完全）

```csharp
using System;
using System.Text.Json;
class JsonBad {
    public static JsonDocument Read(byte[] bytes) {
        using var doc = JsonDocument.Parse(bytes);
        return doc; // Dispose済み文書を返す
    }
}
```

### 要求仕様

1. `JsonValidator` を実装し、`void Feed(ReadOnlySpan<byte> chunk)`、`JsonDocument Finish()`、`void Reset()`、`Dispose()` を提供。入力を丸ごと保管して最後にJsonDocument.Parseする実装は禁止。**Utf8JsonReader + JsonReaderState** を使い、入力をチャンクごとに検証して最終的に所有権を持つDOMを組み立てる。
2. 許可するJSONはnull/true/false、**Int64整数のみ**、文字列、配列、オブジェクト。小数点・指数表記・コメント・末尾カンマ・BOMは拒否。文字列キーの重複は拒否（Ordinal比較、同じオブジェクト内のみ）。
3. 入力の生UTF-8は厳密なRFC 3629準拠のみ許可し、無効な継続バイト・過長符号化・サロゲートのUTF-8直接符号化を拒否。`\uXXXX`の不正なサロゲート（高位のみ、低位のみ、誤った対）も拒否する。標準ライブラリが置換文字を生成するだけでは拒否とみなさない。
4. 最大ネスト深さ64（トップの配列/オブジェクト開始を深さ1）、復号後の文字列値・キーは各65535 UTF-8バイト以内、入力総量は1MiB以内。1トークンが複数Feedにまたがっても、未解析部分を保持できる上限は65536バイトとする。トークン長超過は専用例外を投げる。
5. Finishは成功時のみ自立したJsonDocumentを返す。失敗時は初回の誤りの入力全体0始まりバイトオフセットとエラー種別を取得できるようにし、Resetまでstickyに失敗する。Finish後のFeed/FinishはInvalidOperationException。
6. 所有するJsonDocumentを返却後、JsonValidatorをDisposeしても文書が有効であること。JsonDocumentの破棄は返却先の責任。増分解析に必要な状態以外の無制限な生入力コピーは禁止。
7. 完全な独自JSONパーサーへの置換は不可。今回の評価対象はUtf8JsonReaderとチャンク境界・寿命管理の正しい利用である。

### 必須検証

1. null/真偽/long.MinValue/long.MaxValue/負の0、浮動小数点、指数、+符号、先頭0、整数範囲超過。
2. 空配列・空オブジェクト・重複キー、深さ64/65、65535/65536 UTF-8バイト文字列、1MiB境界。
3. 全1バイト分割と全2/3分割（短い複合文書）、UTF-8の2/3/4バイトが分割されるケース、escapeの途中切れ。
4. 無効UTF-8・不正サロゲート・生制御文字・末尾カンマ・BOM・コメント、エラーバイト位置、Reset後の正常再利用。
5. ランダム有効文書2万件と変異文書2万件を独立した厳密参照検証器と比較、Dispose後のDOM寿命、確保失敗時のリークなし。

### ★★FREYJAの回答【Q07・記入欄】

> **以下は★★FREYJA専用の解答領域。** 問題本文と元コードには触れず、7項目をすべて実際の回答で置き換える。未実施の検証は未実施と正直に記載する。

#### 1. 根本原因・危険性・影響

**元コード `JsonBad.Read` の問題点**

| # | 根本原因 | 実害 |
| --- | --- | --- |
| 1 | `using var doc` のスコープを抜けるときに破棄した JsonDocument を返す | 呼出し元が、Dispose 済みの文書（プールに返したバッファー）にアクセスする → ObjectDisposedException、または返却済みメモリの参照 |
| 2 | 入力全体を一度に Parse する | チャンクごとの増分検証ができず、大きな入力を全部保持する |
| 3 | 制限がない | 浮動小数点・重複キー・BOM・深さ・長さ・総量を検査しない |
| 4 | 厳密な UTF-8・サロゲートの検査を標準の挙動に任せる | 不正なサロゲートなどが置換文字や例外で曖昧に扱われる |
| 5 | エラー位置を返さない | 問題の箇所を特定できない |

**修正方針：** `Utf8JsonReader` と `JsonReaderState` で、チャンクごとにトークン化します（`isFinalBlock: false`。Finish の時だけ true）。
- 次の Feed に持ち越すのは、未完成のトークンの部分（`BytesConsumed` 以降。≤ 65536 バイト。超えたら `JsonTokenTooLongException`）だけです。
- トークンごとに、次の厳密な規則を独自に重ねて検査します。
  - Number：`.`・`e`・`E` を拒否し、`TryGetInt64` で範囲を確認
  - 文字列・キー：生バイトを RFC 3629 で検証し、`\uXXXX` のサロゲートの対を確認し、復号後の長さが 65535 以内か確認
  - 同じオブジェクト内の重複キー（Ordinal）
  - 深さ 64（自前で数える）
  - 入力総量 1MiB
  - 先頭の BOM
- DOM（JsonNode）は、トークンを受け取るたびに組み立てます。Finish が成功したときだけ、`JsonSerializer.SerializeToDocument` で所有権を持つ JsonDocument にして返します。
- エラーは種別と、入力全体での 0 始まりのオフセットを持ち、Reset まで保持します（sticky）。

#### 2. 完全な修正版C#コード

```csharp
// Q07: JsonValidator - chunked strict JSON validation with Utf8JsonReader + JsonReaderState (C# 14 / .NET 10)
// The reader does tokenisation; this class adds the strict rules (Int64 only, RFC 3629 UTF-8,
// surrogate escapes, duplicate keys, limits) and builds an owned DOM incrementally.
// Original implementation by ★★FREYJA for the C# hell-level exam.
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Exam;

public enum JsonErrorKind
{
    Syntax, NonInteger, IntegerOutOfRange, DuplicateKey, DepthExceeded, StringTooLong,
    InputTooLarge, TokenTooLong, InvalidUtf8, InvalidSurrogate, ByteOrderMark, Truncated, AllocationFailed
}

public class JsonValidationException(JsonErrorKind kind, long offset, string message, Exception? inner = null)
    : Exception($"{kind} at byte {offset}: {message}", inner)
{
    public JsonErrorKind Kind { get; } = kind;
    /// <summary>0-based offset in the whole input of the first error.</summary>
    public long Offset { get; } = offset;
}

/// <summary>A single token (with its unparsed prefix) does not fit into the 65,536-byte carry-over buffer.</summary>
public sealed class JsonTokenTooLongException(long offset)
    : JsonValidationException(JsonErrorKind.TokenTooLong, offset, "a token split across Feed calls exceeds 65536 bytes");

public sealed class JsonValidator : IDisposable
{
    public const int MaxDepth = 64, MaxStringBytes = 65535, MaxInput = 1 << 20, MaxCarry = 65536;

    static readonly JsonReaderOptions ReaderOptions = new()
    {
        MaxDepth = 128,                                 // our own limit (64) is checked first, with an exact offset
        CommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,
    };

    sealed class Frame(JsonNode container) { public readonly JsonNode Container = container; public readonly HashSet<string>? Keys = container is JsonObject ? new HashSet<string>(StringComparer.Ordinal) : null; }

    readonly ArrayPool<byte> _pool;
    JsonReaderState _state;
    byte[]? _carry;                // unconsumed tail (part of an unfinished token), rented, <= 65536 bytes used
    int _carryLen;
    long _total;                   // bytes accepted so far
    long _lineNo, _lineStart;      // line number and absolute start offset of the line at the carry start
    readonly Stack<Frame> _stack = new();
    JsonNode? _root;
    bool _haveRoot;
    string? _key;
    JsonValidationException? _error;
    bool _finished, _disposed;

    public JsonValidator() : this(ArrayPool<byte>.Shared) { }
    /// <summary>The pool is injectable so tests can count buffers and inject allocation failures.</summary>
    public JsonValidator(ArrayPool<byte> pool) { _pool = pool; Reset(); }

    public JsonValidationException? Error => _error;

    public void Feed(ReadOnlySpan<byte> chunk)
    {
        ThrowIfUnusable();
        try
        {
            if (_total == 0 && chunk.Length > 0 && chunk[0] == 0xEF)
                throw Fail(JsonErrorKind.ByteOrderMark, 0, "UTF-8 BOM (or a byte that can only start one) at the start");
            bool overflow = chunk.Length > MaxInput - _total;
            if (overflow) chunk = chunk[..(int)(MaxInput - _total)];   // validate the allowed prefix first
            Process(chunk, isFinal: false);
            if (overflow) throw Fail(JsonErrorKind.InputTooLarge, MaxInput, "input larger than 1 MiB");
        }
        catch (OutOfMemoryException oom) { throw Fail(JsonErrorKind.AllocationFailed, _total, "buffer allocation failed", oom); }
    }

    public JsonDocument Finish()
    {
        ThrowIfUnusable();
        try
        {
            Process(ReadOnlySpan<byte>.Empty, isFinal: true);
            if (!_haveRoot || _stack.Count != 0) throw Fail(JsonErrorKind.Truncated, _total, "input ended before the document was complete");
            var doc = JsonSerializer.SerializeToDocument(_root);        // owned, independent of this validator
            _finished = true;
            ReturnCarry();
            return doc;
        }
        catch (OutOfMemoryException oom) { throw Fail(JsonErrorKind.AllocationFailed, _total, "buffer allocation failed", oom); }
    }

    public void Reset()
    {
        ReturnCarry();
        _state = new JsonReaderState(ReaderOptions);
        _total = 0; _lineNo = 0; _lineStart = 0;
        _stack.Clear(); _root = null; _haveRoot = false; _key = null;
        _error = null; _finished = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ReturnCarry();
        _stack.Clear(); _root = null;
    }

    void ThrowIfUnusable()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_error is not null) throw _error;                           // sticky until Reset
        if (_finished) throw new InvalidOperationException("Finish already completed; call Reset to validate another document");
    }

    JsonValidationException Fail(JsonErrorKind kind, long offset, string msg, Exception? inner = null)
    {
        if (_error is null)
        {
            _error = kind == JsonErrorKind.TokenTooLong ? new JsonTokenTooLongException(offset) : new JsonValidationException(kind, offset, msg, inner);
            ReturnCarry();
            _stack.Clear(); _root = null;                               // no partial DOM is kept
        }
        return _error;
    }

    void ReturnCarry()
    {
        if (_carry is not null) { _carry.AsSpan(0, _carryLen).Clear(); _pool.Return(_carry); _carry = null; }
        _carryLen = 0;
    }

    // Runs the reader over carry + chunk; keeps only the unconsumed tail.
    void Process(ReadOnlySpan<byte> chunk, bool isFinal)
    {
        long baseOffset = _total - _carryLen;                           // absolute offset of data[0]
        byte[]? combined = null;
        ReadOnlySpan<byte> data = chunk;
        if (_carryLen > 0)
        {
            combined = _pool.Rent(_carryLen + chunk.Length);             // transient; returned below
            _carry.AsSpan(0, _carryLen).CopyTo(combined);
            chunk.CopyTo(combined.AsSpan(_carryLen));
            data = combined.AsSpan(0, _carryLen + chunk.Length);
        }
        try
        {
            int consumed = RunReader(data, baseOffset, isFinal);
            AdvanceLines(data[..consumed], baseOffset);
            var rest = data[consumed..];
            if (rest.Length > MaxCarry) throw Fail(JsonErrorKind.TokenTooLong, baseOffset + consumed, "");
            if (_carry is null && rest.Length > 0) _carry = _pool.Rent(MaxCarry);
            rest.CopyTo(_carry);                                         // only the unfinished token is kept
            if (_carry is not null && rest.Length < _carryLen) _carry.AsSpan(rest.Length, _carryLen - rest.Length).Clear();
            _carryLen = rest.Length;
            _total += chunk.Length;
        }
        finally
        {
            if (combined is not null) { combined.AsSpan(0, data.Length).Clear(); _pool.Return(combined); }
        }
    }

    int RunReader(ReadOnlySpan<byte> data, long baseOffset, bool isFinal)
    {
        var reader = new Utf8JsonReader(data, isFinal, _state);
        try
        {
            while (reader.Read()) Handle(ref reader, baseOffset);
        }
        catch (JsonException ex)
        {
            long off = ErrorOffset(data, baseOffset, ex);
            if (isFinal && off >= _total) throw Fail(JsonErrorKind.Truncated, _total, ex.Message, ex);
            throw Fail(JsonErrorKind.Syntax, off, ex.Message, ex);
        }
        _state = reader.CurrentState;
        return checked((int)reader.BytesConsumed);
    }

    // JsonException reports (line, byte position in line) relative to the whole stream: map it to an offset.
    long ErrorOffset(ReadOnlySpan<byte> data, long baseOffset, JsonException ex)
    {
        long line = ex.LineNumber ?? _lineNo, pos = ex.BytePositionInLine ?? 0;
        long lineStart = _lineStart, cur = _lineNo;
        for (int i = 0; i < data.Length && cur < line; i++)
            if (data[i] == (byte)'\n') { cur++; lineStart = baseOffset + i + 1; }
        return lineStart + pos;
    }

    void AdvanceLines(ReadOnlySpan<byte> consumed, long baseOffset)
    {
        int last = consumed.LastIndexOf((byte)'\n');
        if (last < 0) return;
        _lineNo += consumed.Count((byte)'\n');
        _lineStart = baseOffset + last + 1;
    }

    void Handle(ref Utf8JsonReader r, long baseOffset)
    {
        long tokenAt = baseOffset + r.TokenStartIndex;
        switch (r.TokenType)
        {
            case JsonTokenType.StartObject:
            case JsonTokenType.StartArray:
            {
                if (_stack.Count + 1 > MaxDepth) throw Fail(JsonErrorKind.DepthExceeded, tokenAt, "nesting deeper than 64");
                JsonNode c = r.TokenType == JsonTokenType.StartObject ? new JsonObject() : new JsonArray();
                Attach(c);
                _stack.Push(new Frame(c));
                break;
            }
            case JsonTokenType.EndObject:
            case JsonTokenType.EndArray:
                _stack.Pop();
                break;
            case JsonTokenType.PropertyName:
            {
                ValidateString(r.ValueSpan, tokenAt + 1);
                string name = r.GetString()!;
                if (!_stack.Peek().Keys!.Add(name)) throw Fail(JsonErrorKind.DuplicateKey, tokenAt, $"duplicate key \"{name}\"");
                _key = name;
                break;
            }
            case JsonTokenType.String:
                ValidateString(r.ValueSpan, tokenAt + 1);
                Attach(JsonValue.Create(r.GetString()!));
                break;
            case JsonTokenType.Number:
            {
                var raw = r.ValueSpan;
                if (raw.IndexOfAny((byte)'.', (byte)'e', (byte)'E') >= 0) throw Fail(JsonErrorKind.NonInteger, tokenAt, "only integers are allowed");
                if (!r.TryGetInt64(out long v)) throw Fail(JsonErrorKind.IntegerOutOfRange, tokenAt, "integer outside Int64");
                Attach(JsonValue.Create(v));
                break;
            }
            case JsonTokenType.True: Attach(JsonValue.Create(true)); break;
            case JsonTokenType.False: Attach(JsonValue.Create(false)); break;
            case JsonTokenType.Null: Attach(null); break;
            default: throw Fail(JsonErrorKind.Syntax, tokenAt, $"unexpected token {r.TokenType}");
        }
    }

    void Attach(JsonNode? node)
    {
        if (_stack.Count == 0) { _root = node; _haveRoot = true; return; }
        var top = _stack.Peek().Container;
        if (top is JsonArray a) a.Add(node);
        else ((JsonObject)top).Add(_key!, node);
    }

    // Raw (still escaped) string bytes: strict RFC 3629 UTF-8, valid \uXXXX surrogate pairs, decoded length limit.
    void ValidateString(ReadOnlySpan<byte> s, long at)
    {
        long decoded = 0;
        for (int i = 0; i < s.Length;)
        {
            byte b = s[i];
            int add;
            if (b == (byte)'\\')
            {
                if (s[i + 1] != (byte)'u') { add = 1; i += 2; }
                else
                {
                    int cu = Hex4(s, i + 2);
                    if (cu is >= 0xDC00 and <= 0xDFFF) throw Fail(JsonErrorKind.InvalidSurrogate, at + i, "lone low surrogate escape");
                    if (cu is >= 0xD800 and <= 0xDBFF)
                    {
                        if (i + 12 > s.Length || s[i + 6] != (byte)'\\' || s[i + 7] != (byte)'u')
                            throw Fail(JsonErrorKind.InvalidSurrogate, at + i, "high surrogate escape not followed by a low surrogate escape");
                        int lo = Hex4(s, i + 8);
                        if (lo is < 0xDC00 or > 0xDFFF) throw Fail(JsonErrorKind.InvalidSurrogate, at + i, "high surrogate escape followed by a non-low surrogate");
                        add = 4; i += 12;
                    }
                    else { add = cu < 0x80 ? 1 : cu < 0x800 ? 2 : 3; i += 6; }
                }
            }
            else if (b < 0x80) { add = 1; i++; }
            else
            {
                int need; byte lo = 0x80, hi = 0xBF;
                if (b is >= 0xC2 and <= 0xDF) need = 1;
                else if (b is >= 0xE0 and <= 0xEF) { need = 2; if (b == 0xE0) lo = 0xA0; if (b == 0xED) hi = 0x9F; }
                else if (b is >= 0xF0 and <= 0xF4) { need = 3; if (b == 0xF0) lo = 0x90; if (b == 0xF4) hi = 0x8F; }
                else throw Fail(JsonErrorKind.InvalidUtf8, at + i, "invalid UTF-8 lead byte");
                for (int k = 1; k <= need; k++)
                {
                    if (i + k >= s.Length) throw Fail(JsonErrorKind.InvalidUtf8, at + i + k, "truncated UTF-8 sequence");
                    byte c = s[i + k];
                    if (c < lo || c > hi) throw Fail(JsonErrorKind.InvalidUtf8, at + i + k, "invalid UTF-8 continuation / overlong / surrogate / out of range");
                    lo = 0x80; hi = 0xBF;
                }
                add = need + 1; i += need + 1;
            }
            decoded += add;
            if (decoded > MaxStringBytes) throw Fail(JsonErrorKind.StringTooLong, at + i - 1, "decoded string longer than 65535 UTF-8 bytes");
        }
    }

    static int Hex4(ReadOnlySpan<byte> s, int i)
    {
        int v = 0;
        for (int k = 0; k < 4; k++)
        {
            int c = s[i + k];
            v = v * 16 + (c <= '9' ? c - '0' : (c | 0x20) - 'a' + 10);   // the reader already checked the hex digits
        }
        return v;
    }
}
```

#### 3. ビルド方法・使用環境

```powershell
# 作業フォルダー: scratchpad\cs\q07（試験専用の一時領域）。cs\build.ps1 -Q q07 が次を順に実行する
# (1) 下記の q07.csproj を生成  (2) Release ビルド  (3) テスト実行（終了コード 0 = 全件合格）
dotnet build q07.csproj -c Release -nologo
dotnet bin\Release\net10.0\q07.dll
# 結果: ビルド成功・0 個の警告・0 エラー、実行の終了コード 0
#
# q07.csproj（全問共通。AssemblyName だけが問題ごとに異なる）
# <Project Sdk="Microsoft.NET.Sdk">
#   <PropertyGroup>
#     <OutputType>Exe</OutputType>
#     <TargetFramework>net10.0</TargetFramework>
#     <LangVersion>14.0</LangVersion>
#     <Nullable>enable</Nullable>
#     <ImplicitUsings>disable</ImplicitUsings>
#     <AnalysisLevel>latest</AnalysisLevel>
#     <AllowUnsafeBlocks>false</AllowUnsafeBlocks>
#     <InvariantGlobalization>true</InvariantGlobalization>
#     <AssemblyName>q07</AssemblyName>
#     <RootNamespace>Exam</RootNamespace>
#   </PropertyGroup>
# </Project>
```

**使用環境（全問共通）**

- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド）
- ランタイム：**Microsoft.NETCore.App 10.0.15（.NET 10）**。実行時に `Environment.Version` = 10.0.15 であることを確認しました。
- 言語とターゲット：`TargetFramework=net10.0`、`LangVersion=14.0`（C# 14。C# 14 の extension メンバーのコンパイルも確認済み）、`Nullable=enable`、`AnalysisLevel=latest`、`ImplicitUsings=disable`、`AllowUnsafeBlocks=false`、構成は **Release**。
- SDK：この PC にインストールされている SDK は **.NET SDK 11.0.100** だけです。.NET 10 用の参照パック（10.0.15）を使い、`net10.0` 向けにビルドしました。.NET 10 SDK そのものはインストールされておらず、無断導入の禁止に従って追加していません。そのため、コンパイラは SDK 11 に同梱のもので、言語バージョンを 14.0 に固定して使っています。
- NuGet パッケージ・第三者ライブラリ：使用していません（.NET 共有フレームワークの API だけ）。
- 各問は独立したコンソールプロジェクトです（`qNN.csproj` = 下記の内容、`QNN.cs` = 実装、`Tests.cs` = テスト）。ビルド時の警告は全問 **0 件**、テストの終了コードは全問 0 です。

#### 4. 実際に使用したテストコード

```csharp
// Q07 tests
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Exam;

static class T
{
    public static int Pass, Fail;
    static int _f0;
    public static void Check(bool ok, string what) { if (ok) Pass++; else { Fail++; Console.WriteLine($"  FAIL: {what}"); } }
    public static void Begin() => _f0 = Fail;
    public static void Report(string name) => Console.WriteLine($"[{(Fail == _f0 ? "PASS" : "FAIL")}] {name}");
}

sealed class CountingPool : ArrayPool<byte>
{
    public int Rented, Returned; public int FailAfter = -1;   // fail the n-th Rent (0-based count), -1 = never
    public override byte[] Rent(int n) { if (FailAfter >= 0 && Rented >= FailAfter) { FailAfter = -1; throw new OutOfMemoryException("injected"); } Rented++; return new byte[n]; }
    public override void Return(byte[] a, bool clear = false) => Returned++;
}

/// <summary>Independent strict reference validator (recursive descent, no Utf8JsonReader).</summary>
sealed class RefValidator(byte[] s)
{
    int _i;
    public static JsonNode? Parse(byte[] s, out bool ok)
    {
        ok = false;
        if (s.Length > JsonValidator.MaxInput) return null;
        var r = new RefValidator(s);
        try { r.Ws(); var v = r.Value(0); r.Ws(); if (r._i != s.Length) return null; ok = true; return v; }
        catch (FormatException) { return null; }
    }
    static FormatException Bad() => new();
    void Ws() { while (_i < s.Length && s[_i] is (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r') _i++; }
    byte Peek() => _i < s.Length ? s[_i] : throw Bad();
    void Expect(string lit) { foreach (char c in lit) { if (Peek() != c) throw Bad(); _i++; } }
    JsonNode? Value(int depth)
    {
        switch (Peek())
        {
            case (byte)'{':
            {
                if (depth + 1 > 64) throw Bad(); _i++; var o = new JsonObject(); var keys = new HashSet<string>(StringComparer.Ordinal);
                Ws(); if (Peek() == '}') { _i++; return o; }
                while (true) { Ws(); if (Peek() != '"') throw Bad(); string k = Str(); if (!keys.Add(k)) throw Bad(); Ws(); Expect(":"); Ws(); o.Add(k, Value(depth + 1)); Ws(); if (Peek() == ',') { _i++; continue; } Expect("}"); return o; }
            }
            case (byte)'[':
            {
                if (depth + 1 > 64) throw Bad(); _i++; var a = new JsonArray();
                Ws(); if (Peek() == ']') { _i++; return a; }
                while (true) { Ws(); a.Add(Value(depth + 1)); Ws(); if (Peek() == ',') { _i++; continue; } Expect("]"); return a; }
            }
            case (byte)'"': return JsonValue.Create(Str());
            case (byte)'t': Expect("true"); return JsonValue.Create(true);
            case (byte)'f': Expect("false"); return JsonValue.Create(false);
            case (byte)'n': Expect("null"); return null;
            default: return Num();
        }
    }
    JsonNode Num()
    {
        int st = _i; if (Peek() == '-') _i++;
        if (Peek() == '0') _i++; else if (Peek() is >= (byte)'1' and <= (byte)'9') { while (_i < s.Length && s[_i] is >= (byte)'0' and <= (byte)'9') _i++; } else throw Bad();
        if (_i < s.Length && s[_i] is (byte)'.' or (byte)'e' or (byte)'E' or (byte)'+' or (byte)'-' || _i < s.Length && s[_i] is >= (byte)'0' and <= (byte)'9') throw Bad();
        if (!long.TryParse(Encoding.ASCII.GetString(s, st, _i - st), System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out long v)) throw Bad();
        return JsonValue.Create(v);
    }
    string Str()
    {
        _i++; var sb = new StringBuilder(); int bytes = 0;
        while (true)
        {
            byte b = Peek();
            if (b == '"') { _i++; if (bytes > 65535) throw Bad(); return sb.ToString(); }
            if (b < 0x20) throw Bad();
            if (b == '\\')
            {
                _i++; byte e = Peek(); _i++;
                switch (e)
                {
                    case (byte)'"': sb.Append('"'); bytes++; break; case (byte)'\\': sb.Append('\\'); bytes++; break; case (byte)'/': sb.Append('/'); bytes++; break;
                    case (byte)'b': sb.Append('\b'); bytes++; break; case (byte)'f': sb.Append('\f'); bytes++; break; case (byte)'n': sb.Append('\n'); bytes++; break;
                    case (byte)'r': sb.Append('\r'); bytes++; break; case (byte)'t': sb.Append('\t'); bytes++; break;
                    case (byte)'u':
                    {
                        int u = Hex();
                        if (u is >= 0xDC00 and <= 0xDFFF) throw Bad();
                        if (u is >= 0xD800 and <= 0xDBFF) { Expect("\\u"); int l = Hex(); if (l is < 0xDC00 or > 0xDFFF) throw Bad(); sb.Append((char)u).Append((char)l); bytes += 4; }
                        else { sb.Append((char)u); bytes += u < 0x80 ? 1 : u < 0x800 ? 2 : 3; }
                        break;
                    }
                    default: throw Bad();
                }
                continue;
            }
            if (b < 0x80) { sb.Append((char)b); bytes++; _i++; continue; }
            // decode UTF-8 by value and check the ranges afterwards (different method from the validator)
            int len = (b & 0xE0) == 0xC0 ? 2 : (b & 0xF0) == 0xE0 ? 3 : (b & 0xF8) == 0xF0 ? 4 : throw Bad();
            int cp = b & (len == 2 ? 0x1F : len == 3 ? 0x0F : 0x07);
            for (int k = 1; k < len; k++) { if (_i + k >= s.Length || (s[_i + k] & 0xC0) != 0x80) throw Bad(); cp = cp << 6 | s[_i + k] & 0x3F; }
            int min = len == 2 ? 0x80 : len == 3 ? 0x800 : 0x10000;
            if (cp < min || cp > 0x10FFFF || cp is >= 0xD800 and <= 0xDFFF) throw Bad();
            sb.Append(char.ConvertFromUtf32(cp)); bytes += len; _i += len;
        }
    }
    int Hex() { int v = 0; for (int k = 0; k < 4; k++) { byte c = Peek(); _i++; int d = c is >= (byte)'0' and <= (byte)'9' ? c - '0' : c is >= (byte)'a' and <= (byte)'f' ? c - 'a' + 10 : c is >= (byte)'A' and <= (byte)'F' ? c - 'A' + 10 : throw Bad(); v = v * 16 + d; } return v; }
}

static class Program
{
    static byte[] U(string s) => Encoding.UTF8.GetBytes(s);
    // Run the validator over `cuts`; returns (document or null, error)
    static (JsonDocument? doc, JsonValidationException? err) Run(byte[] s, IEnumerable<int>? cuts = null, ArrayPool<byte>? pool = null)
    {
        using var v = pool is null ? new JsonValidator() : new JsonValidator(pool);
        int pos = 0;
        try
        {
            foreach (int c in (cuts ?? []).Append(s.Length)) { v.Feed(s.AsSpan(pos, c - pos)); pos = c; }
            return (v.Finish(), null);
        }
        catch (JsonValidationException e) { return (null, e); }
    }
    static bool SameDom(JsonDocument? d, JsonNode? n) => d is not null && JsonElement.DeepEquals(d.RootElement, JsonSerializer.SerializeToDocument(n).RootElement);
    static (JsonErrorKind? kind, long off) Err(string s) { var (d, e) = Run(U(s)); d?.Dispose(); return (e?.Kind, e?.Offset ?? -1); }
    static (JsonErrorKind? kind, long off) ErrB(byte[] s) { var (d, e) = Run(s); d?.Dispose(); return (e?.Kind, e?.Offset ?? -1); }

    static int Main()
    {
        T.Begin();   // T1 scalars and numbers
        {
            foreach (var (src, exp) in new[] { ("null", "null"), ("true", "true"), ("false", "false"), ("-9223372036854775808", "-9223372036854775808"), ("9223372036854775807", "9223372036854775807"), ("-0", "0") })
            { var (d, e) = Run(U(src)); T.Check(e is null && d!.RootElement.GetRawText() == exp, $"{src} -> {exp} ({d?.RootElement.GetRawText() ?? e!.Message})"); d?.Dispose(); }
            T.Check(Err("1.5") == (JsonErrorKind.NonInteger, 0), "1.5 -> NonInteger @0");
            T.Check(Err("[1e2]") == (JsonErrorKind.NonInteger, 1), "1e2 -> NonInteger @1");
            T.Check(Err("[-1E5]").kind == JsonErrorKind.NonInteger, "-1E5 -> NonInteger");
            T.Check(Err("+1").kind == JsonErrorKind.Syntax, "+1 -> Syntax");
            T.Check(Err("[01]").kind == JsonErrorKind.Syntax, "01 -> Syntax");
            T.Check(Err("9223372036854775808") == (JsonErrorKind.IntegerOutOfRange, 0), "9223372036854775808 -> IntegerOutOfRange");
            T.Check(Err("[-9223372036854775809]") == (JsonErrorKind.IntegerOutOfRange, 1), "-9223372036854775809 -> IntegerOutOfRange @1");
        }
        T.Report("T1 null / true / false / long.MinValue / long.MaxValue / -0, float, exponent, +, leading zero, out of range");

        T.Begin();   // T2 containers, duplicate keys, depth, string length, 1 MiB
        {
            var (a, _) = Run(U("[]")); var (o, _) = Run(U("{}"));
            T.Check(a!.RootElement.GetArrayLength() == 0 && o!.RootElement.EnumerateObject().Count() == 0, "empty array / object"); a!.Dispose(); o!.Dispose();
            T.Check(Err("{\"a\":1,\"b\":{\"a\":2},\"a\":3}") == (JsonErrorKind.DuplicateKey, 19), "duplicate key in the same object -> DuplicateKey @19 (opening quote of the second \"a\")");
            T.Check(Err("{\"a\":{\"a\":1}}").kind is null, "same key in nested objects is allowed");
            T.Check(Err(new string('[', 64) + new string(']', 64)).kind is null, "depth 64 accepted");
            T.Check(Err(new string('[', 65) + new string(']', 65)) == (JsonErrorKind.DepthExceeded, 64), "depth 65 -> DepthExceeded @64");
            string s65535 = "\"" + new string('x', 65535) + "\"";
            T.Check(Err(s65535).kind is null, "65535-byte string accepted");
            T.Check(Err("\"" + new string('x', 65536) + "\"").kind == JsonErrorKind.StringTooLong, "65536-byte string -> StringTooLong");
            T.Check(Err("\"" + string.Concat(Enumerable.Repeat("\u20AC", 21845)) + "\"").kind is null, "65535 bytes of 3-byte characters accepted");
            T.Check(Err("{\"" + new string('k', 65536) + "\":1}").kind == JsonErrorKind.StringTooLong, "65536-byte key -> StringTooLong");
            var big = new StringBuilder("["); while (big.Length < JsonValidator.MaxInput - 2) big.Append("1,"); big.Length = JsonValidator.MaxInput - 1; if (big[^1] == ',') big[^1] = '1'; big.Append(']');
            byte[] mib = U(big.ToString());
            T.Check(mib.Length == JsonValidator.MaxInput && ErrB(mib).kind is null, "exactly 1 MiB accepted");
            T.Check(ErrB([.. mib, (byte)' ']) == (JsonErrorKind.InputTooLarge, JsonValidator.MaxInput), "1 MiB + 1 -> InputTooLarge @1048576");
            byte[] longTok = U("[\"" + new string('y', 70000) + "\"]");
            var (_, tl) = Run(longTok, [100, 67000]);
            T.Check(tl is JsonTokenTooLongException { Offset: 1 }, $"a 70,000-byte token still unfinished after 66,900 carried bytes -> JsonTokenTooLongException @1 ({tl?.Offset})");
            var (lt2, lt2e) = Run(longTok, [100]); T.Check(lt2e is { Kind: JsonErrorKind.StringTooLong }, "the same token completed by the next Feed is not a carry problem: it fails the 65535-byte string limit instead"); lt2?.Dispose();
            var (d2, e2) = Run(U(s65535), [30000]); T.Check(e2 is null, "65535-byte string split across two Feeds (carry < 65536) accepted"); d2?.Dispose();
        }
        T.Report("T2 empty containers, duplicate keys, depth 64/65, 65535/65536-byte strings, 1 MiB boundary, token too long");

        T.Begin();   // T3 every split of a composite document
        {
            byte[] doc = U("{\"k\u00e9\":[1,-23,true,null,\"\u00e9\u20ac\U0001F600\\u00e9\\uD83D\\uDE00\\n\"],\"e\":{},\"z\":[]}");
            var (w, we) = Run(doc); T.Check(we is null, "composite document valid"); var whole = w!.RootElement.GetRawText();
            int bad = 0, runs = 0;
            var (one, _) = Run(doc, Enumerable.Range(1, doc.Length - 1)); if (one?.RootElement.GetRawText() != whole) bad++; runs++;
            for (int a = 0; a <= doc.Length; a++) for (int b = a; b <= doc.Length; b++)
            { var (d, e) = Run(doc, [a, b]); if (e is not null || d!.RootElement.GetRawText() != whole) bad++; d?.Dispose(); runs++; }
            T.Check(bad == 0, $"{bad} of {runs} splits (all 1-byte, all 2-way / 3-way) differ; includes splits inside 2/3/4-byte UTF-8 and inside escapes");
            byte[] cut = U("[\"\\u00"); var (cd, ce) = Run(cut, [4]);
            T.Check(ce is { Kind: JsonErrorKind.Truncated } && ce.Offset == cut.Length, "escape cut at the end of the input -> Truncated @input length");
            Console.WriteLine($"  split runs: {runs} (document {doc.Length} bytes)");
        }
        T.Report("T3 all 1-byte splits and all 2/3-way splits, UTF-8 sequences and escapes split across Feed calls");

        T.Begin();   // T4 invalid input, offsets, Reset reuse
        {
            T.Check(ErrB([(byte)'"', 0xC0, 0x80, (byte)'"']) is (JsonErrorKind.InvalidUtf8 or JsonErrorKind.Syntax, _), "overlong C0 80 rejected");
            T.Check(ErrB([(byte)'"', 0xE0, 0x80, 0x80, (byte)'"']) == (JsonErrorKind.InvalidUtf8, 2), $"overlong E0 80 80 -> InvalidUtf8 @2 ({ErrB([(byte)'"', 0xE0, 0x80, 0x80, (byte)'"'])})");
            T.Check(ErrB([(byte)'"', 0xED, 0xA0, 0x80, (byte)'"']) == (JsonErrorKind.InvalidUtf8, 2), "UTF-8 encoded surrogate ED A0 80 -> InvalidUtf8 @2");
            T.Check(ErrB([(byte)'"', 0xF5, 0x80, 0x80, 0x80, (byte)'"']).kind is JsonErrorKind.InvalidUtf8 or JsonErrorKind.Syntax, "F5 rejected");
            T.Check(ErrB([(byte)'"', 0x80, (byte)'"']).kind is JsonErrorKind.InvalidUtf8 or JsonErrorKind.Syntax, "lone continuation byte rejected");
            T.Check(Err("\"\\uD800\"") == (JsonErrorKind.InvalidSurrogate, 1), "lone high surrogate escape -> InvalidSurrogate @1");
            T.Check(Err("[\"\\uDC00\"]") == (JsonErrorKind.InvalidSurrogate, 2), "lone low surrogate escape -> InvalidSurrogate @2");
            T.Check(Err("\"\\uD800\\u0041\"").kind == JsonErrorKind.InvalidSurrogate, "high + non-low surrogate -> InvalidSurrogate");
            T.Check(Err("\"a\u0001\"").kind == JsonErrorKind.Syntax, "raw control character -> Syntax");
            T.Check(Err("[1,2,]").kind == JsonErrorKind.Syntax && Err("{\"a\":1,}").kind == JsonErrorKind.Syntax, "trailing commas rejected");
            T.Check(ErrB([0xEF, 0xBB, 0xBF, (byte)'1']) == (JsonErrorKind.ByteOrderMark, 0), "BOM -> ByteOrderMark @0");
            T.Check(Err("[1 /* c */]").kind == JsonErrorKind.Syntax && Err("[1 // c\n]").kind == JsonErrorKind.Syntax, "comments rejected");
            var (k, off) = Err("[1,]"); T.Check(k == JsonErrorKind.Syntax && off == 3, $"error offset of ']' in [1,] = 3 ({off})");
            var (k2, off2) = Err("[1,\n 2,\n x]"); T.Check(k2 == JsonErrorKind.Syntax && off2 == 9, $"error offset across lines (x at 9) = {off2}");
            T.Check(Err("[1").kind == JsonErrorKind.Truncated && Err("").kind == JsonErrorKind.Truncated, "incomplete / empty input -> Truncated");
            using var v = new JsonValidator();
            try { v.Feed(U("[1,]")); } catch (JsonValidationException) { }
            bool sticky = false; try { v.Feed(U("1")); } catch (JsonValidationException e) { sticky = e.Offset == 3; }
            bool sticky2 = false; try { v.Finish(); } catch (JsonValidationException) { sticky2 = true; }
            T.Check(sticky && sticky2, "error is sticky (Feed and Finish fail with the first error)");
            v.Reset(); v.Feed(U("[true]")); using var ok = v.Finish();
            T.Check(ok.RootElement[0].GetBoolean(), "Reset then normal reuse");
            bool inv1 = false, inv2 = false; try { v.Feed(U("1")); } catch (InvalidOperationException) { inv1 = true; } try { v.Finish(); } catch (InvalidOperationException) { inv2 = true; }
            T.Check(inv1 && inv2, "Feed / Finish after a successful Finish -> InvalidOperationException");
        }
        T.Report("T4 invalid UTF-8, bad surrogates, control chars, trailing commas, BOM, comments, error offsets, sticky, Reset");

        T.Begin();   // T5 random documents vs reference, DOM lifetime, allocation failure
        {
            var rnd = new Random(2026);
            string RandStr() { var sb = new StringBuilder("\""); int n = rnd.Next(6); for (int i = 0; i < n; i++) sb.Append(rnd.Next(7) switch { 0 => "a", 1 => "\\n", 2 => "\u00e9", 3 => "\\u20AC", 4 => "\\uD83D\\uDE00", 5 => "\U0001F600", _ => "\\\"" }); return sb.Append('"').ToString(); }
            string Gen(int depth)
            {
                string ws = rnd.Next(3) == 0 ? " \n" : "";
                int kind = rnd.Next(depth > 4 ? 5 : 7);
                return ws + kind switch
                {
                    0 => "null", 1 => rnd.Next(2) == 0 ? "true" : "false", 2 => rnd.NextInt64(long.MinValue, long.MaxValue).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    3 or 4 => RandStr(),
                    5 => "[" + string.Join(",", Enumerable.Range(0, rnd.Next(4)).Select(_ => Gen(depth + 1))) + "]",
                    _ => "{" + string.Join(",", Enumerable.Range(0, rnd.Next(4)).Select(i => $"\"k{i}\":" + Gen(depth + 1))) + "}",
                } + ws;
            }
            int validOk = 0, mutOk = 0, mutRejected = 0, shown = 0; byte[] pool = U("[]{}\",:\\-0123456789eE.ntfu \u0001x/*");
            for (int t = 0; t < 20000; t++)
            {
                byte[] d = U(Gen(0));
                var cuts = Enumerable.Range(0, rnd.Next(4)).Select(_ => rnd.Next(d.Length + 1)).Order().ToArray();
                var refNode = RefValidator.Parse(d, out bool refOk);
                var (doc, err) = Run(d, cuts);
                if (refOk && err is null && SameDom(doc, refNode)) validOk++;
                doc?.Dispose();
                byte[] m = (byte[])d.Clone();
                int p = rnd.Next(m.Length);
                switch (rnd.Next(4)) { case 0: m[p] = pool[rnd.Next(pool.Length)]; break; case 1: m = [.. m[..p], .. m[(p + 1)..]]; break; case 2: m = [.. m[..p], pool[rnd.Next(pool.Length)], .. m[p..]]; break; default: m[p] = (byte)rnd.Next(256); break; }
                var mn = RefValidator.Parse(m, out bool mOk);
                var mcuts = cuts.Where(c => c <= m.Length).ToArray();
                var (md, me) = Run(m, mcuts);
                if (mOk == (me is null) && (!mOk || SameDom(md, mn))) mutOk++;
                else if (shown++ < 3) Console.WriteLine($"  mismatch: ref={mOk} validator={(me is null ? "ok" : me.Kind.ToString())} doc={Encoding.UTF8.GetString(m)}");
                if (!mOk) mutRejected++;
                md?.Dispose();
            }
            T.Check(validOk == 20000, $"{validOk}/20000 random valid documents (random chunking) equal the reference DOM");
            T.Check(mutOk == 20000, $"{mutOk}/20000 mutated documents: same verdict and DOM as the reference ({mutRejected} rejected)");
            JsonDocument kept; using (var v = new JsonValidator()) { v.Feed(U("{\"a\":[1,2,\"x\"]}")); kept = v.Finish(); }
            T.Check(kept.RootElement.GetProperty("a")[2].GetString() == "x", "document stays valid after the validator is disposed"); kept.Dispose();
            var cp = new CountingPool { FailAfter = 1 };
            var (fd, fe) = Run(U("[\"abc"), [3], cp);   // second Rent (the combine buffer) fails
            T.Check(fe is { Kind: JsonErrorKind.AllocationFailed } && fd is null, "injected buffer allocation failure -> AllocationFailed (sticky)");
            T.Check(cp.Rented == cp.Returned, $"all rented buffers returned after the failure (rented {cp.Rented}, returned {cp.Returned})");
            var cp2 = new CountingPool(); var (okd, _) = Run(U("[\"abcdef\",12345]"), [2, 5, 9, 13], cp2); okd?.Dispose();
            T.Check(cp2.Rented == cp2.Returned && cp2.Rented > 0, $"normal chunked run: rented {cp2.Rented} = returned {cp2.Returned}");
        }
        T.Report("T5 20,000 random valid + 20,000 mutated documents vs independent reference, DOM after Dispose, allocation failure");

        Console.WriteLine($"RESULT: {T.Pass} checks passed, {T.Fail} failed");
        return T.Fail == 0 ? 0 : 1;
    }
}
```

#### 5. 検証結果（期待結果・実測結果・実施状況）

実行結果は `RESULT: 54 checks passed, 0 failed`、終了コード 0 でした。独立した参照検証器 `RefValidator` は、Utf8JsonReader を使わない再帰下降方式です（UTF-8 は値を計算してから範囲を判定する、別の方法で検証します）。

| テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| null / true / false / long.MinValue / long.MaxValue / -0 | 受理（-0 は 0） | 一致 | 実施 |
| 浮動小数点 1.5・指数 1e2 / -1E5 | NonInteger（オフセット 0 / 1） | 一致 | 実施 |
| +符号・先頭の 0 | Syntax | 一致 | 実施 |
| 整数の範囲外 | 9223372036854775808 → IntegerOutOfRange @0、−9223372036854775809 → @1 | 一致 | 実施 |
| 空の配列・空のオブジェクト | 受理 | 一致 | 実施 |
| 重複キー | 同じオブジェクト内 → DuplicateKey @19（2つ目のキーの開始引用符）。入れ子の別オブジェクトなら許可 | 一致 | 実施 |
| 深さ 64 / 65 | 受理 / DepthExceeded @64 | 一致 | 実施 |
| 65535 / 65536 UTF-8 バイトの文字列 | 受理 / StringTooLong（3バイト文字 21845 個も受理。65536 バイトのキーは拒否） | 一致 | 実施 |
| 1MiB の境界 | ちょうど 1,048,576 バイトは受理。+1 バイトで InputTooLarge @1048576 | 一致 | 実施 |
| トークン長の超過 | 7万バイトのトークンが 66,900 バイト持ち越されても未完成 → JsonTokenTooLongException @1。同じトークンが次の Feed で完成する場合は、持ち越しの問題ではなく StringTooLong になる | 一致 | 実施 |
| 全1バイト分割・全2/3分割 | 71バイトの複合文書（2/3/4 バイトの UTF-8 と各種エスケープを含む）で、1バイトずつと全 2,628 通りの分割の結果が一括入力と同一 | 不一致0（2,629 回） | 実施 |
| エスケープの途中切れ | `["\u00` → Truncated @入力長 | 一致 | 実施 |
| 不正な UTF-8 | E0 80 80 → InvalidUtf8 @2、ED A0 80 → InvalidUtf8 @2、C0 80・F5・単独の 80 → 拒否（InvalidUtf8 または Syntax） | 一致 | 実施 |
| 不正なサロゲート | 高位だけ → InvalidSurrogate @1、低位だけ @2、高位 + 非低位 → InvalidSurrogate | 一致 | 実施 |
| 生の制御文字・末尾カンマ・コメント | Syntax | 一致 | 実施 |
| BOM | ByteOrderMark @0 | 一致 | 実施 |
| エラーのバイト位置 | `[1,]` の `]` は @3、複数行 `[1,\n 2,\n x]` の x は @9 | 一致 | 実施 |
| sticky と Reset | エラー後は Feed も Finish も最初のエラーで失敗。Reset 後は正常に再利用できる。成功した Finish の後の Feed / Finish は InvalidOperationException | 一致 | 実施 |
| ランダムな有効文書2万件 | 乱数分割で Feed し、参照の DOM と一致（JsonElement.DeepEquals） | 20,000/20,000 | 実施 |
| 変異文書2万件 | 合否と DOM が参照と一致 | 20,000/20,000 | 実施 |
| Dispose 後の DOM の寿命 | 検証器を Dispose した後も、返した文書は有効 | 一致 | 実施 |
| 確保失敗時のリーク | 注入したプールで2回目の Rent を失敗させると AllocationFailed（sticky）になり、借りたバッファーはすべて返却される。通常のチャンク処理でも Rent 数 = Return 数 | 一致 | 実施 |

**自律修正（テスト側）：** 重複キーのオフセットの期待値（20 → 正しくは 19）、トークン長超過のテストの分割位置、7万バイト文字列の期待結果（StringTooLong）、null 許容警告（CS8602）を修正しました。修正後の警告は 0 件です。

#### 6. 技術的な正当性と設計判断

- **Utf8JsonReader の寿命：** ref struct なので、フィールドには保持しません。Feed ごとに `new Utf8JsonReader(data, isFinalBlock, _state)` で作り直し、`CurrentState` を `_state` に保存して、次のチャンクへ状態を引き継ぎます。
- **持ち越し：** 保持するのは `BytesConsumed` 以降の未完成部分だけです（プールから借りた最大 65536 バイトのバッファー）。Feed の中で、持ち越しと新しいチャンクを一時的に連結することがありますが、それは呼出しの間だけで、終われば返却します。無制限の生入力のコピーは保持しません。
- **エラーのオフセット：** Utf8JsonReader の JsonException は（行番号、行内のバイト位置）を報告します。そこで、消費したバイトに含まれる改行を自分で数えて、入力全体のオフセットに換算します。独自の検査では、`TokenStartIndex` を基準にします。
- **完全な独自パーサーではない理由：** トークン化・文法・エスケープの構文・制御文字・コメント・末尾カンマの判定は Utf8JsonReader に任せています。独自に行うのは、問題文が要求する追加の厳密規則だけです。
- **所有権：** 返す JsonDocument は、JsonNode の DOM から新しく作るので、検証器の内部バッファーとは独立しています。破棄は返却先の責任です。

#### 7. 未実施項目・制約・残課題

- Utf8JsonReader 自身が先に構文エラーとして検出する不正入力（例：C0 80 や F5 の一部）は、エラーの種別が Syntax になる場合があります（拒否されることは確認済み）。
- 構文エラーのオフセットは、Utf8JsonReader が報告する位置（行内のバイト位置）に基づきます。
- エスケープを多く含む文字列は、生のバイト長が 65536 を超えると、復号後の長さが 65535 以内でも、Feed をまたいだ時点で TokenTooLong になります（持ち越しの上限は仕様どおり）。
- 確保失敗は、注入したプールの Rent 失敗で確認しました。JsonNode など GC ヒープでの確保失敗は注入していません。

---

## Q08【地獄級】FileStreamによるクラッシュ整合スナップショット保存と障害注入

**試験の狙い：** 書込み・Flush・置換・復旧・CRC、障害発生時の旧版保護を現実的な保証範囲で検証する。

### 元コード（危険・不完全）

```csharp
using System.IO;
using System.Threading.Tasks;
class SaveBad {
    public Task SaveAsync(string path,byte[] data)
        => File.WriteAllBytesAsync(path,data);  // 途中書込みで旧データを破壊
}
```

### 要求仕様

1. `SnapshotStore` の `Task SaveAsync(string directory, ReadOnlyMemory<byte> payload, CancellationToken ct)` と `Task<byte[]> LoadAsync(string directory, CancellationToken ct)` を実装。directoryは試験専用の既存フォルダーであり、対象ファイル名は `snapshot.bin` 固定。payloadは最大1MiB。
2. ファイル形式は `[53 4E 50 31] [len:u32 LE] [payload] [CRC32:u32 LE]`。CRC32の対象はlenの4バイトとpayload。読み込み時はmagic、長さ、実ファイルサイズ、CRC、末尾余剰を厳密に検証し、誤形式はInvalidDataException。snapshot.bin不在の場合はFileNotFoundException。
3. 保存は同一directory内に一意の一時ファイルを作成し、全内容を書き込み、FileStream.Flush(flushToDisk:true)を呼んでから、**同一ディレクトリ内で既存snapshot.binの置換**を行う。途中で失敗した場合に保存済みの旧snapshotを上書きで破壊しない。
4. 明示的に定義する保証範囲：プロセスの通常の例外・キャンセル・強制終了の模擬実験において、**置換前の障害なら旧版、置換完了後なら新版**が観測できること。全OS/全FSの突然の電源断・ディレクトリメタデータ永続化まで保証すると主張してはならない。ファイル置換自体の原子性はOS/FSの対応を前提とし、その依存性を説明する。
5. SaveAsyncが例外/キャンセルになった場合、可能なら自身が作った一時ファイルだけを片づける。LoadAsyncは正規ファイルだけ読む。起動時に残っている他の一時ファイルを本番ファイルとして採用しない。ユーザーの無関係ファイルを削除しない。
6. 同じインスタンスに対する同時SaveAsyncは直列化する（SemaphoreSlim使用可）。LoadAsyncは完了した旧版または新版のいずれかを取得し、途中のファイルを見せない。呼出し元が渡したpayloadを非同期処理中に変更しないという契約を明記する。
7. 書込み・Flush・置換・後片づけの**各段階で失敗を注入できるテスト用抽象化**を設ける。実際のユーザーデータや外部システムには一切触れない。

### 必須検証

1. 空/1バイト/1MiB、CRCの全1ビット反転、途中切れ、偽長、末尾余剰、ファイル不在。
2. 既存snapshotにAを保存後、各障害点でBの保存を止めてLoadがAまたは正常コミット済みBを返し、途中状態を返さない。
3. Flush前後、置換直前/直後、キャンセル、書込み失敗、独自FileOpsの失敗注入、孤立した一時ファイル。
4. 同時SaveAsync 100件、LoadAsync混合アクセス、保存順と最終内容、旧版ファイルの破損0、余分なファイル変更0。

### ★★FREYJAの回答【Q08・記入欄】

> **以下は★★FREYJA専用の解答領域。** 問題本文と元コードには触れず、7項目をすべて実際の回答で置き換える。未実施の検証は未実施と正直に記載する。

#### 1. 根本原因・危険性・影響

**元コード `SaveBad.SaveAsync` の問題点**

| # | 根本原因 | 発生条件 | 実害 |
| --- | --- | --- | --- |
| 1 | `File.WriteAllBytesAsync` が対象ファイルを開いた瞬間に切り詰め、その場で上書きする | 書込みの途中で例外・キャンセル・強制終了 | 旧版も新版も失われ、途中まで書いたファイルが残る |
| 2 | フラッシュしない | 書込みの直後の電源断 | OS のキャッシュにしかないデータが失われる |
| 3 | 完全性の情報がない（magic・長さ・CRC） | 破損 | 読み込み時に壊れたデータを検出できない |
| 4 | 同時保存を直列化していない | 並列の SaveAsync | 内容が混ざる、共有違反 |
| 5 | 一時ファイルと置換の概念がない | — | 「旧版か新版のどちらか」という保証がない |

**修正方針：**
- 同じディレクトリに一意の一時ファイル `snapshot.bin.<GUID>.tmp` を `FileMode.CreateNew` で作ります。
- ヘッダー・payload・CRC を書き、`FileStream.Flush(flushToDisk: true)` でディスクへ書き出してから閉じます。その後、`File.Move(temp, "snapshot.bin", overwrite: true)` で置き換えます（コミット点）。
- 置換より前の失敗やキャンセルでは、自分の一時ファイルだけを削除します。旧版は一切触れません。
- 読み込みは `snapshot.bin` だけを開き、magic・長さ・実サイズ・CRC・末尾の余剰を厳密に検証します。
- 同じインスタンスの保存と読み込みは、`SemaphoreSlim(1,1)` で直列化します。
- 各段階で失敗を注入できる `IFileOps` 抽象化を設けました。

#### 2. 完全な修正版C#コード

```csharp
// Q08: SnapshotStore - temp file + Flush(flushToDisk) + same-directory replace, CRC-checked load (C# 14 / .NET 10)
// File: 53 4E 50 31 | len u32 LE | payload | CRC-32 u32 LE  (CRC over the 4 length bytes + payload)
// Original implementation by ★★FREYJA for the C# hell-level exam.
using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Exam;

/// <summary>File-system steps used by SnapshotStore; tests wrap them to inject failures at every stage.</summary>
public interface IFileOps
{
    FileStream CreateTemp(string path);                                         // CreateNew: never reuses a name
    ValueTask WriteAsync(FileStream s, ReadOnlyMemory<byte> data, CancellationToken ct);
    void Flush(FileStream s);                                                   // FileStream.Flush(flushToDisk: true)
    void Replace(string tempPath, string targetPath);                           // same-directory rename over the target
    void DeleteTemp(string tempPath);
    FileStream OpenForRead(string path);
}

public sealed class RealFileOps : IFileOps
{
    public static readonly RealFileOps Instance = new();
    public FileStream CreateTemp(string path) =>
        new(path, new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None, Options = FileOptions.Asynchronous });
    public ValueTask WriteAsync(FileStream s, ReadOnlyMemory<byte> data, CancellationToken ct) => s.WriteAsync(data, ct);
    public void Flush(FileStream s) => s.Flush(flushToDisk: true);
    public void Replace(string tempPath, string targetPath) => File.Move(tempPath, targetPath, overwrite: true);
    public void DeleteTemp(string tempPath) => File.Delete(tempPath);
    public FileStream OpenForRead(string path) =>
        new(path, new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read, Share = FileShare.Read | FileShare.Delete, Options = FileOptions.Asynchronous });
}

public sealed class SnapshotStore
{
    public const string FileName = "snapshot.bin";
    public const int MaxPayload = 1 << 20;
    static ReadOnlySpan<byte> Magic => "SNP1"u8;          // 53 4E 50 31
    const int HeaderSize = 8, TrailerSize = 4;

    readonly IFileOps _ops;
    readonly SemaphoreSlim _gate = new(1, 1);             // serialises saves and loads of this instance

    public SnapshotStore(IFileOps? ops = null) => _ops = ops ?? RealFileOps.Instance;

    /// <summary>Contract: the caller must not modify <paramref name="payload"/> until the returned task completes.</summary>
    public async Task SaveAsync(string directory, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        if (payload.Length > MaxPayload) throw new ArgumentOutOfRangeException(nameof(payload), payload.Length, "payload larger than 1 MiB");
        if (!Directory.Exists(directory)) throw new DirectoryNotFoundException(directory);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        string target = Path.Combine(directory, FileName);
        string temp = Path.Combine(directory, $"{FileName}.{Guid.NewGuid():N}.tmp");   // unique, same directory
        bool tempCreated = false, committed = false;
        try
        {
            byte[] header = new byte[HeaderSize], trailer = new byte[TrailerSize];
            Magic.CopyTo(header);
            BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(4), (uint)payload.Length);
            uint crc = Crc32.Update(0xFFFFFFFFu, header.AsSpan(4, 4));
            crc = Crc32.Update(crc, payload.Span) ^ 0xFFFFFFFFu;
            BinaryPrimitives.WriteUInt32LittleEndian(trailer, crc);

            using (FileStream fs = _ops.CreateTemp(temp))
            {
                tempCreated = true;
                await _ops.WriteAsync(fs, header, ct).ConfigureAwait(false);
                await _ops.WriteAsync(fs, payload, ct).ConfigureAwait(false);
                await _ops.WriteAsync(fs, trailer, ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                _ops.Flush(fs);                                   // data on disk before it becomes visible
            }
            ct.ThrowIfCancellationRequested();                    // last point where cancellation is honoured
            _ops.Replace(temp, target);                           // commit point (atomicity: OS/FS dependent)
            committed = true;
        }
        finally
        {
            if (tempCreated && !committed)
            {
                try { _ops.DeleteTemp(temp); } catch { /* best effort: only our own temp file */ }
            }
            _gate.Release();
        }
    }

    public async Task<byte[]> LoadAsync(string directory, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            string target = Path.Combine(directory, FileName);
            if (!File.Exists(target)) throw new FileNotFoundException("snapshot not found", target);
            byte[] all;
            using (FileStream fs = _ops.OpenForRead(target))
            {
                long size = fs.Length;
                if (size < HeaderSize + TrailerSize) throw new InvalidDataException($"file too short ({size} bytes)");
                if (size > HeaderSize + MaxPayload + TrailerSize) throw new InvalidDataException($"file too long ({size} bytes)");
                all = new byte[size];
                await fs.ReadExactlyAsync(all, ct).ConfigureAwait(false);
            }
            if (!all.AsSpan(0, 4).SequenceEqual(Magic)) throw new InvalidDataException("bad magic");
            uint len = BinaryPrimitives.ReadUInt32LittleEndian(all.AsSpan(4));
            if (len > MaxPayload) throw new InvalidDataException($"declared length {len} exceeds 1 MiB");
            if (all.Length != HeaderSize + len + TrailerSize) throw new InvalidDataException($"declared length {len} does not match the file size {all.Length}");
            uint crc = Crc32.Update(0xFFFFFFFFu, all.AsSpan(4, 4 + (int)len)) ^ 0xFFFFFFFFu;
            if (crc != BinaryPrimitives.ReadUInt32LittleEndian(all.AsSpan(HeaderSize + (int)len))) throw new InvalidDataException("CRC mismatch");
            return all.AsSpan(HeaderSize, (int)len).ToArray();
        }
        finally { _gate.Release(); }
    }
}

public static class Crc32
{
    static readonly uint[] Table = Build();
    static uint[] Build()
    {
        var t = new uint[256];
        for (uint i = 0; i < 256; i++) { uint c = i; for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? (c >> 1) ^ 0xEDB88320u : c >> 1; t[i] = c; }
        return t;
    }
    public static uint Update(uint reg, ReadOnlySpan<byte> d) { foreach (byte b in d) reg = Table[(reg ^ b) & 0xFF] ^ (reg >> 8); return reg; }
}
```

#### 3. ビルド方法・使用環境

```powershell
# 作業フォルダー: scratchpad\cs\q08（試験専用の一時領域）。cs\build.ps1 -Q q08 が次を順に実行する
# (1) 下記の q08.csproj を生成  (2) Release ビルド  (3) テスト実行（終了コード 0 = 全件合格）
dotnet build q08.csproj -c Release -nologo
dotnet bin\Release\net10.0\q08.dll
# 結果: ビルド成功・0 個の警告・0 エラー、実行の終了コード 0
#
# q08.csproj（全問共通。AssemblyName だけが問題ごとに異なる）
# <Project Sdk="Microsoft.NET.Sdk">
#   <PropertyGroup>
#     <OutputType>Exe</OutputType>
#     <TargetFramework>net10.0</TargetFramework>
#     <LangVersion>14.0</LangVersion>
#     <Nullable>enable</Nullable>
#     <ImplicitUsings>disable</ImplicitUsings>
#     <AnalysisLevel>latest</AnalysisLevel>
#     <AllowUnsafeBlocks>false</AllowUnsafeBlocks>
#     <InvariantGlobalization>true</InvariantGlobalization>
#     <AssemblyName>q08</AssemblyName>
#     <RootNamespace>Exam</RootNamespace>
#   </PropertyGroup>
# </Project>
```

**使用環境（全問共通）**

- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド）
- ランタイム：**Microsoft.NETCore.App 10.0.15（.NET 10）**。実行時に `Environment.Version` = 10.0.15 であることを確認しました。
- 言語とターゲット：`TargetFramework=net10.0`、`LangVersion=14.0`（C# 14。C# 14 の extension メンバーのコンパイルも確認済み）、`Nullable=enable`、`AnalysisLevel=latest`、`ImplicitUsings=disable`、`AllowUnsafeBlocks=false`、構成は **Release**。
- SDK：この PC にインストールされている SDK は **.NET SDK 11.0.100** だけです。.NET 10 用の参照パック（10.0.15）を使い、`net10.0` 向けにビルドしました。.NET 10 SDK そのものはインストールされておらず、無断導入の禁止に従って追加していません。そのため、コンパイラは SDK 11 に同梱のもので、言語バージョンを 14.0 に固定して使っています。
- NuGet パッケージ・第三者ライブラリ：使用していません（.NET 共有フレームワークの API だけ）。
- 各問は独立したコンソールプロジェクトです（`qNN.csproj` = 下記の内容、`QNN.cs` = 実装、`Tests.cs` = テスト）。ビルド時の警告は全問 **0 件**、テストの終了コードは全問 0 です。

- テストのデータは、テストの実行ファイルの隣の `q08-testdata`（専用フォルダー）にだけ作り、終了時に削除しました。強制終了の試験では、同じ dll を `--child` で起動した子プロセスを `Process.Kill` で終了させています。

#### 4. 実際に使用したテストコード

```csharp
// Q08 tests (all files live under a private test directory next to the test binary)
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Exam;

static class T
{
    public static int Pass, Fail;
    static int _f0;
    public static void Check(bool ok, string what) { if (ok) Pass++; else { Fail++; Console.WriteLine($"  FAIL: {what}"); } }
    public static void Begin() => _f0 = Fail;
    public static void Report(string name) => Console.WriteLine($"[{(Fail == _f0 ? "PASS" : "FAIL")}] {name}");
    public static async Task<bool> ThrowsAsync<E>(Func<Task> a) where E : Exception { try { await a(); } catch (E) { return true; } catch { return false; } return false; }
}

enum Stage { None, CreateTemp, WriteHeader, WritePayload, WriteTrailer, Flush, BeforeReplace, AfterReplace, Delete }

sealed class InjectedFault(string m) : IOException(m);

/// <summary>Wraps the real operations; throws at a chosen stage. Crash mode also suppresses the cleanup,
/// emulating a process that died at that point (the temp file is left behind).</summary>
sealed class FaultOps : IFileOps
{
    public Stage FailAt = Stage.None;
    public bool Crash;
    public CancellationTokenSource? CancelAtPayload;
    public readonly ConcurrentQueue<int> CommitOrder = new();
    int _writes;
    void Hit(Stage s) { if (s != Stage.None && FailAt == s) throw new InjectedFault($"injected failure at {s}"); }
    public FileStream CreateTemp(string p) { Hit(Stage.CreateTemp); _writes = 0; return RealFileOps.Instance.CreateTemp(p); }
    public async ValueTask WriteAsync(FileStream s, ReadOnlyMemory<byte> d, CancellationToken ct)
    {
        int n = Interlocked.Increment(ref _writes);
        if (n == 2 && CancelAtPayload is not null) { await RealFileOps.Instance.WriteAsync(s, d[..(d.Length / 2)], ct); CancelAtPayload.Cancel(); ct.ThrowIfCancellationRequested(); }
        if (n == 2 && FailAt == Stage.WritePayload) { await RealFileOps.Instance.WriteAsync(s, d[..(d.Length / 2)], ct); Hit(Stage.WritePayload); }   // half written, then failure
        Hit(n switch { 1 => Stage.WriteHeader, 3 => Stage.WriteTrailer, _ => Stage.None });
        await RealFileOps.Instance.WriteAsync(s, d, ct);
    }
    public void Flush(FileStream s) { Hit(Stage.Flush); RealFileOps.Instance.Flush(s); }
    public void Replace(string tmp, string target)
    {
        Hit(Stage.BeforeReplace);
        var bytes = File.ReadAllBytes(tmp); if (bytes.Length >= 12) CommitOrder.Enqueue(BitConverter.ToInt32(bytes, 8));
        RealFileOps.Instance.Replace(tmp, target);
        Hit(Stage.AfterReplace);
    }
    public void DeleteTemp(string tmp) { if (Crash) return; Hit(Stage.Delete); RealFileOps.Instance.DeleteTemp(tmp); }
    public FileStream OpenForRead(string p) => RealFileOps.Instance.OpenForRead(p);
}

static class Program
{
    static string s_root = "";
    static string NewDir() { var d = Path.Combine(s_root, Guid.NewGuid().ToString("N")[..8]); Directory.CreateDirectory(d); return d; }
    static byte[] Pat(int n, int seed) { var b = new byte[n]; for (int i = 0; i < n; i++) b[i] = (byte)(i * 7 + seed); if (n >= 4) BitConverter.TryWriteBytes(b, seed); return b; }
    static string[] Temps(string d) => Directory.GetFiles(d, SnapshotStore.FileName + ".*.tmp");

    static async Task<int> Main(string[] args)
    {
        if (args.Length == 3 && args[0] == "--child") return await Child(args[1], int.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture));
        _ = Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(_ => { Console.WriteLine("WATCHDOG: hang"); Environment.Exit(2); });
        s_root = Path.Combine(AppContext.BaseDirectory, "q08-testdata");
        if (Directory.Exists(s_root)) Directory.Delete(s_root, recursive: true);
        Directory.CreateDirectory(s_root);
        var ct = CancellationToken.None;

        T.Begin();   // T1 round trips and malformed files
        {
            var store = new SnapshotStore(); var d = NewDir();
            T.Check(await T.ThrowsAsync<FileNotFoundException>(() => store.LoadAsync(d, ct)), "missing snapshot.bin -> FileNotFoundException");
            foreach (int n in new[] { 0, 1, SnapshotStore.MaxPayload })
            { var p = Pat(n, n); await store.SaveAsync(d, p, ct); T.Check((await store.LoadAsync(d, ct)).AsSpan().SequenceEqual(p), $"{n}-byte payload round trip"); }
            T.Check(await T.ThrowsAsync<ArgumentOutOfRangeException>(() => store.SaveAsync(d, new byte[SnapshotStore.MaxPayload + 1], ct)), "payload of 1 MiB + 1 rejected");
            var file = Path.Combine(d, SnapshotStore.FileName);
            await store.SaveAsync(d, Pat(20, 3), ct);
            byte[] good = File.ReadAllBytes(file);
            T.Check(good.Length == 32 && good.AsSpan(0, 4).SequenceEqual("SNP1"u8) && BitConverter.ToUInt32(good, 4) == 20, "layout: magic SNP1, len u32 LE, payload, CRC");
            int flips = 0, rejected = 0;
            for (int i = 0; i < good.Length; i++) for (int k = 0; k < 8; k++)
            { var x = (byte[])good.Clone(); x[i] ^= (byte)(1 << k); File.WriteAllBytes(file, x); flips++; if (await T.ThrowsAsync<InvalidDataException>(() => store.LoadAsync(d, ct))) rejected++; }
            T.Check(rejected == flips, $"all {flips} single-bit flips (magic, length, payload, CRC) -> InvalidDataException");
            int cuts = 0; for (int n = 0; n < good.Length; n++) { File.WriteAllBytes(file, good[..n]); if (await T.ThrowsAsync<InvalidDataException>(() => store.LoadAsync(d, ct))) cuts++; }
            T.Check(cuts == good.Length, "every truncated length -> InvalidDataException");
            var fake = (byte[])good.Clone(); BitConverter.TryWriteBytes(fake.AsSpan(4), 2_000_000u); File.WriteAllBytes(file, fake);
            T.Check(await T.ThrowsAsync<InvalidDataException>(() => store.LoadAsync(d, ct)), "fake huge length -> InvalidDataException");
            File.WriteAllBytes(file, [.. good, 0]); T.Check(await T.ThrowsAsync<InvalidDataException>(() => store.LoadAsync(d, ct)), "trailing extra byte -> InvalidDataException");
        }
        T.Report("T1 empty / 1 byte / 1 MiB, all CRC bit flips, truncation, fake length, trailing data, missing file");

        T.Begin();   // T2/T3 failure and crash at every stage while replacing A with B
        {
            var stages = new[] { Stage.CreateTemp, Stage.WriteHeader, Stage.WritePayload, Stage.WriteTrailer, Stage.Flush, Stage.BeforeReplace, Stage.AfterReplace };
            byte[] A = Pat(300_000, 1), B = Pat(400_000, 2);
            foreach (bool crash in new[] { false, true })
                foreach (var st in stages)
                {
                    var d = NewDir(); var ops = new FaultOps(); var store = new SnapshotStore(ops);
                    await store.SaveAsync(d, A, ct);
                    ops.FailAt = st; ops.Crash = crash;
                    bool threw = await T.ThrowsAsync<InjectedFault>(() => store.SaveAsync(d, B, ct));
                    ops.FailAt = Stage.None; ops.Crash = false;
                    var got = await new SnapshotStore().LoadAsync(d, ct);
                    bool expectB = st == Stage.AfterReplace;
                    T.Check(threw && got.AsSpan().SequenceEqual(expectB ? B : A), $"{(crash ? "crash" : "failure")} at {st}: Load returns {(expectB ? "B (committed)" : "A")}");
                    int temps = Temps(d).Length;
                    bool tempOk = crash ? temps == (st is Stage.CreateTemp or Stage.AfterReplace ? 0 : 1) : temps == 0;
                    T.Check(tempOk, $"{(crash ? "crash" : "failure")} at {st}: temp files left = {temps} ({(crash ? "orphan kept, ignored by Load" : "own temp removed")})");
                    if (crash && temps == 1) { await store.SaveAsync(d, A, ct); T.Check((await store.LoadAsync(d, ct)).AsSpan().SequenceEqual(A) && Temps(d).Length == 1, "an orphaned temp file is never used as the snapshot; later saves leave it alone"); }
                }
            var dc = NewDir(); var s2 = new SnapshotStore(new FaultOps()); await s2.SaveAsync(dc, A, ct);
            using (var pre = new CancellationTokenSource()) { pre.Cancel(); T.Check(await T.ThrowsAsync<OperationCanceledException>(() => s2.SaveAsync(dc, B, pre.Token)), "cancelled before start"); }
            var cops = new FaultOps(); var s3 = new SnapshotStore(cops);
            using (var mid = new CancellationTokenSource()) { cops.CancelAtPayload = mid; T.Check(await T.ThrowsAsync<OperationCanceledException>(() => s3.SaveAsync(dc, B, mid.Token)), "cancelled while writing the payload"); }
            T.Check((await s2.LoadAsync(dc, ct)).AsSpan().SequenceEqual(A) && Temps(dc).Length == 0, "after cancellations: A intact, temp files removed");
            var dl = NewDir();                                // a failing cleanup must not mask the original error
            var s5 = new SnapshotStore(new CleanupFailOps());
            T.Check(await T.ThrowsAsync<InjectedFault>(() => s5.SaveAsync(dl, B, ct)), "cleanup failure after a write failure: the original failure is reported");
        }
        T.Report("T2/T3 failure and simulated crash at create / header / payload (half written) / trailer / flush / before and after replace, cancellation, orphans");

        T.Begin();   // T4 100 concurrent saves with concurrent loads
        {
            var d = NewDir(); var other = Path.Combine(d, "other-user-file.txt"); File.WriteAllText(other, "do not touch"); var otherTime = File.GetLastWriteTimeUtc(other);
            var ops = new FaultOps(); var store = new SnapshotStore(ops);
            await store.SaveAsync(d, Pat(1000, 0), ct);
            var valid = new ConcurrentDictionary<int, byte[]>(); valid[0] = Pat(1000, 0);
            for (int i = 1; i <= 100; i++) valid[i] = Pat(1000 + i * 100, i);
            int badLoads = 0, loads = 0;
            var saves = Enumerable.Range(1, 100).Select(i => Task.Run(() => store.SaveAsync(d, valid[i], ct))).ToArray();
            var loaders = Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
            {
                while (!saves.All(s => s.IsCompleted))
                {
                    var b = await store.LoadAsync(d, ct); Interlocked.Increment(ref loads);
                    int id = BitConverter.ToInt32(b, 0);
                    if (!valid.TryGetValue(id, out var exp) || !exp.AsSpan().SequenceEqual(b)) Interlocked.Increment(ref badLoads);
                }
            })).ToArray();
            await Task.WhenAll(saves); await Task.WhenAll(loaders);
            var final = await store.LoadAsync(d, ct);
            T.Check(ops.CommitOrder.Count == 101 && BitConverter.ToInt32(final, 0) == ops.CommitOrder.Last(), $"100 concurrent saves serialised; final content = last commit (id {ops.CommitOrder.Last()})");
            T.Check(badLoads == 0, $"{loads} concurrent loads all returned a complete committed version");
            T.Check(Temps(d).Length == 0 && Directory.GetFiles(d).Length == 2, "no temp file left; only snapshot.bin and the unrelated file");
            T.Check(File.ReadAllText(other) == "do not touch" && File.GetLastWriteTimeUtc(other) == otherTime, "unrelated file untouched");
        }
        T.Report("T4 100 concurrent SaveAsync + concurrent LoadAsync: order, final content, no corrupt reads, no stray files");

        T.Begin();   // T5 real process kill during saves
        {
            var d = NewDir(); var store = new SnapshotStore(); byte[] A = Pat(600_000, 0); await store.SaveAsync(d, A, ct);
            var rnd = new Random(5); int okLoads = 0, kills = 0, orphans = 0;
            string dll = Path.Combine(AppContext.BaseDirectory, "q08.dll");
            for (int round = 0; round < 20; round++)
            {
                var psi = new ProcessStartInfo("dotnet", $"\"{dll}\" --child \"{d}\" {round + 1}") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
                using var p = Process.Start(psi)!;
                await p.StandardOutput.ReadLineAsync();          // child signals that it started saving
                await Task.Delay(rnd.Next(5, 250));
                try { p.Kill(entireProcessTree: true); kills++; } catch (InvalidOperationException) { }
                await p.WaitForExitAsync();
                var b = await store.LoadAsync(d, ct);
                int id = BitConverter.ToInt32(b, 0);
                if (b.AsSpan().SequenceEqual(id == 0 ? A : Pat(600_000 + id % 97, id))) okLoads++;
            }
            orphans = Temps(d).Length;
            T.Check(okLoads == 20, $"after {kills} forced kills during saves, every load returned a complete old or new version ({okLoads}/20)");
            Console.WriteLine($"  forced kills={kills}, orphaned temp files left by killed processes={orphans} (ignored by Load)");
        }
        T.Report("T5 child process killed at random moments while saving: snapshot.bin always a complete version");

        Directory.Delete(s_root, recursive: true);           // remove only our own test data
        Console.WriteLine($"RESULT: {T.Pass} checks passed, {T.Fail} failed");
        return T.Fail == 0 ? 0 : 1;
    }

    // child: saves continuously until killed
    static async Task<int> Child(string dir, int seed)
    {
        var store = new SnapshotStore();
        Console.WriteLine("started"); Console.Out.Flush();
        for (int k = 0; ; k++) { int id = seed * 1000 + k; await store.SaveAsync(dir, Pat(600_000 + id % 97, id), CancellationToken.None); }
    }
}

/// <summary>Flush fails and the cleanup fails too.</summary>
sealed class CleanupFailOps : IFileOps
{
    public FileStream CreateTemp(string p) => RealFileOps.Instance.CreateTemp(p);
    public ValueTask WriteAsync(FileStream s, ReadOnlyMemory<byte> d, CancellationToken ct) => RealFileOps.Instance.WriteAsync(s, d, ct);
    public void Flush(FileStream s) => throw new InjectedFault("flush failed");
    public void Replace(string a, string b) => RealFileOps.Instance.Replace(a, b);
    public void DeleteTemp(string t) => throw new IOException("cleanup failed too");
    public FileStream OpenForRead(string p) => RealFileOps.Instance.OpenForRead(p);
}
```

#### 5. 検証結果（期待結果・実測結果・実施状況）

実行結果は `RESULT: 52 checks passed, 0 failed`、終了コード 0、所要 6.4 秒（NTFS、Windows 11）でした。

| テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 空 / 1 バイト / 1MiB | 往復が一致。1MiB+1 は ArgumentOutOfRangeException | 一致 | 実施 |
| ファイルの配置 | `53 4E 50 31`、len は u32 LE、payload、CRC | 一致 | 実施 |
| CRC の全1ビット反転 | 32バイトのファイルの全 256 ビットの反転 → InvalidDataException | 256/256 | 実施 |
| 途中切れ | 全 32 通りの長さ → InvalidDataException | 一致 | 実施 |
| 偽の長さ・末尾の余剰 | InvalidDataException | 一致 | 実施 |
| ファイル不在 | FileNotFoundException | 一致 | 実施 |
| 各障害点での B の保存の失敗（例外） | 一時ファイルの作成・ヘッダー・payload（半分書いた後）・トレーラー・Flush・置換の直前では、Load が A を返し、一時ファイルは削除される。置換の直後の失敗では B（コミット済み） | 7段階すべて一致 | 実施 |
| 各障害点での強制終了の模擬 | 後片付けをせずに停止。Load は A（置換後なら B）を返す。残った一時ファイルは本番として採用されず、後の保存もそれに触れない | 7段階すべて一致 | 実施 |
| Flush の前後・置換の直前・直後 | 上記の段階に含む | 一致 | 実施 |
| キャンセル | 開始前のキャンセル、payload を書いている途中のキャンセル → OperationCanceledException。A は無傷で、一時ファイルは削除される | 一致 | 実施 |
| 後片付けの失敗 | Flush と削除がともに失敗しても、元の失敗が報告される | 一致 | 実施 |
| 同時 SaveAsync 100件 + LoadAsync の混在 | 保存は直列化され、最終内容 = 最後にコミットされた版。8つの読み手が取得したものは、すべてコミット済みの完全な版。一時ファイルは残らない | 一致 | 実施 |
| 余分なファイルの変更 | 同じフォルダーの無関係なファイルは、内容も更新時刻も不変 | 一致 | 実施 |
| 実際のプロセス強制終了 ×20 | 保存を続ける子プロセスを、ランダムな時点で強制終了しても、Load は毎回完全な旧版か新版を返す | 20/20（強制終了 20 回、残った一時ファイル 15 個は Load が無視） | 実施 |

**自律修正（テスト側）：** 障害注入クラスで、注入しない状態（Stage.None）が失敗と一致して例外を投げていました。テストの誤りで、修正しました。また、使っていない変数の行を削除しました。

#### 6. 技術的な正当性と設計判断

- **明示的に定義した保証範囲：** プロセスの例外・キャンセル・強制終了では、置換より前なら旧版、置換の完了後なら新版が観測されます（模擬実験と、実際のプロセス強制終了で確認しました）。
- **原子性の前提：** 置換は同じディレクトリ内の `File.Move(overwrite: true)` です。Windows では、ファイルの置換を伴う名前変更（MoveFileEx の REPLACE_EXISTING）、Unix では rename(2) に相当し、その原子性は OS とファイルシステムに依存します。
- **主張しないこと：** 突然の電源断に対する耐性、とくにディレクトリのメタデータ（名前の変更そのもの）の永続化は、`Flush(true)` がファイルの内容しか書き出さないこともあり、全 OS・全ファイルシステムで保証するとは主張しません（電源断の試験はしていません）。
- **一時ファイル：** 名前に GUID を含み、`CreateNew` で作るので衝突しません。削除するのは自分が作った一時ファイルだけで、他の `.tmp` やユーザーのファイルには触れません。Load は `snapshot.bin` しか読みません。
- **直列化：** SemaphoreSlim によって、同じインスタンスの保存と読み込みが重なりません。そのため Load が途中のファイルを見ることはありません。別のインスタンスやプロセスからの同時アクセスでも、名前の置換によって、旧版か新版のどちらかが読まれます。読み込みは `FileShare.Read | Delete` で開きます。
- **呼出し契約：** SaveAsync が完了するまで、payload を変更しないこと（コードのコメントにも明記しています）。

#### 7. 未実施項目・制約・残課題

- **未実施：** 突然の電源断の試験（実機）、Windows 以外の OS とファイルシステム（ext4 など）での試験。
- 制約：強制終了されたプロセスの一時ファイル（`snapshot.bin.*.tmp`）は自動では削除しません（他のプロセスの保存中のファイルを誤って消さないため）。

---

## Q09【地獄級】10万頂点DAGのトランザクション編集と辞書順最小トポロジカルソート

**試験の狙い：** グラフ構造のイミュータブル公開、バッチ原子性、サイクル検出、性能とスレッド安全性を評価する。

### 元コード（危険・不完全）

```csharp
using System.Collections.Generic;
class GraphBad {
    public Dictionary<int,List<int>> Edges = new();
    public void Add(int a,int b) => Edges[a].Add(b); // 重複・閉路・並列操作未対策
}
```

### 要求仕様

1. `GraphStore(int vertices)` を実装。verticesは1..100000。頂点IDは0..vertices-1。有向辺は最大400000本、同一辺は1本として扱う。公開APIは `bool TryApply(IReadOnlyList<EdgeEdit> batch)`、`int[] TopologicalOrder()`、`long Version`。EdgeEditはFrom、To、Add(bool)を持つ独自record struct。
2. バッチ内の操作は並び順に適用。Addで既存辺があれば変化なし、Removeで存在しない辺なら変化なし。無効頂点、自分自身への辺、最終辺数超過、**バッチ結果に閉路がある場合**はfalseとし、元グラフとVersionを一切変更しない。
3. 成功したTryApplyは空バッチや実際の辺変化0でもVersionを**ちょうど1**増加。Versionがlong.MaxValueなら失敗。内部の新状態を完全に構築・検査した後に原子的に公開すること。
4. TopologicalOrderは、各段階で入次数0の頂点の**最小ID**を選ぶ、辞書順最小の唯一の出力。再帰DFS禁止。PriorityQueue/SortedSet等の標準機能で非再帰的に構築する。
5. 多重スレッドからTryApplyとTopologicalOrderを同時に呼べ、読者は常にコミット済みの一貫したグラフだけを見る。ロック方式または不変スナップショット方式を選び、線形化点と例外安全を説明する。
6. 10万頂点・40万辺のグラフでメモリはO(V+E)、TopologicalOrderの時間計算量はO((V+E)log V)以内。毎回全頂点の文字列探索や再帰に依存しないこと。

### 必須検証

1. 空グラフの辞書順、ダイヤモンドDAG、複数解があるグラフ、重複辺、削除済み辺への再削除。
2. バッチ途中に無効頂点/自己ループ/上限超過/閉路を混ぜて全体ロールバックを確認。
3. 100,000頂点・400,000辺で正当な順序、グラフモデルとの一致、応答時間とメモリ量の実測。
4. 4編集者＋8読者、合計10万操作以上。全取得順序が各時点のバージョンのDAGに対応すること、例外失敗時の不変を確認。

### ★★FREYJAの回答【Q09・記入欄】

> **以下は★★FREYJA専用の解答領域。** 問題本文と元コードには触れず、7項目をすべて実際の回答で置き換える。未実施の検証は未実施と正直に記載する。

#### 1. 根本原因・危険性・影響

**元コード `GraphBad.Add` の問題点**

| # | 根本原因 | 実害 |
| --- | --- | --- |
| 1 | `Edges[a]` は、キーがなければ `KeyNotFoundException` | 最初の辺の追加で例外 |
| 2 | `List<int>.Add` で重複を確認しない | 同じ辺が多重に登録される（入次数が狂う） |
| 3 | 閉路の検査がない | DAG の前提が壊れ、トポロジカル順序が存在しなくなる |
| 4 | 頂点の範囲・自己ループ・辺数の上限を検査しない | 不正なグラフ |
| 5 | 公開された可変の Dictionary を、同期なしで共有する | 並列の書込みで Dictionary が破壊される。読み手が途中状態を見る |
| 6 | バッチやロールバックの概念がない | 一部だけが適用された状態が残る |

**修正方針：** 状態は不変スナップショット `GraphSnapshot`（辺の集合 `HashSet<long>`、CSR の隣接配列、Version、順序のキャッシュ）です。
- 書き手は1つのロックで直列化します。現在の辺の集合を私的にコピーし、バッチを並び順に適用します。
- そのうえで、頂点の範囲・自己ループ・最終辺数（≤ 400,000）・閉路（Kahn 法）をすべて検査し、合格した場合だけ `Volatile.Write` で新しいスナップショットを公開します。不合格や例外では、何も公開しません。
- 読み手は、ロックを取らずに現在のスナップショットを読みます。
- 順序は、`PriorityQueue<int,int>`（最小 ID 優先）を使った非再帰の Kahn 法で求めます。

#### 2. 完全な修正版C#コード

```csharp
// Q09: GraphStore - transactional edits of a DAG (<=100,000 vertices, <=400,000 edges),
// immutable snapshots, lexicographically smallest topological order (C# 14 / .NET 10)
// Original implementation by ★★FREYJA for the C# hell-level exam.
using System;
using System.Collections.Generic;
using System.Threading;

namespace Exam;

public readonly record struct EdgeEdit(int From, int To, bool Add);

/// <summary>Immutable committed graph. Never modified after publication.</summary>
public sealed class GraphSnapshot
{
    readonly HashSet<long> _edges;      // (from << 32) | to ; private, read-only after construction
    readonly int[] _offsets;            // CSR: out-edges of v are _targets[_offsets[v] .. _offsets[v+1])
    readonly int[] _targets;
    int[]? _order;                      // cached topological order (computed once per snapshot)

    public int Vertices { get; }
    public long Version { get; }
    public int EdgeCount => _edges.Count;

    internal GraphSnapshot(int vertices, long version, HashSet<long> edges, int[] offsets, int[] targets, int[]? order = null)
    {
        Vertices = vertices; Version = version; _edges = edges; _offsets = offsets; _targets = targets; _order = order;
    }

    public bool ContainsEdge(int from, int to) => _edges.Contains(Key(from, to));
    internal HashSet<long> EdgesForCopy => _edges;
    internal static long Key(int from, int to) => ((long)from << 32) | (uint)to;
    public IEnumerable<(int From, int To)> Edges() { foreach (long k in _edges) yield return ((int)(k >> 32), (int)k); }

    /// <summary>Lexicographically smallest topological order (smallest available ID first). Returns a new array.</summary>
    public int[] TopologicalOrder()
    {
        var cached = Volatile.Read(ref _order);
        if (cached is null)
        {
            cached = Kahn(Vertices, _offsets, _targets) ?? throw new InvalidOperationException("snapshot contains a cycle (cannot happen)");
            Volatile.Write(ref _order, cached);       // benign race: every thread computes the same array
        }
        return (int[])cached.Clone();
    }

    // O((V + E) log V) time, O(V + E) memory, no recursion. Returns null if a cycle exists.
    internal static int[]? Kahn(int v, int[] offsets, int[] targets)
    {
        var indeg = new int[v];
        foreach (int t in targets) indeg[t]++;
        var heap = new PriorityQueue<int, int>(v);
        for (int i = 0; i < v; i++) if (indeg[i] == 0) heap.Enqueue(i, i);
        var order = new int[v]; int k = 0;
        while (heap.TryDequeue(out int u, out _))
        {
            order[k++] = u;
            for (int j = offsets[u]; j < offsets[u + 1]; j++)
                if (--indeg[targets[j]] == 0) heap.Enqueue(targets[j], targets[j]);
        }
        return k == v ? order : null;
    }
}

public sealed class GraphStore
{
    public const int MaxVertices = 100_000, MaxEdges = 400_000;

    readonly object _writeLock = new();          // serialises writers; readers never lock
    GraphSnapshot _current;

    public GraphStore(int vertices)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(vertices, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(vertices, MaxVertices);
        _current = new GraphSnapshot(vertices, 0, new HashSet<long>(), new int[vertices + 1], []);
    }

    /// <summary>Test-only seeding of the version (e.g. near long.MaxValue).</summary>
    internal GraphStore(int vertices, long version) : this(vertices)
        => _current = new GraphSnapshot(vertices, version, new HashSet<long>(), new int[vertices + 1], []);

    public long Version => Volatile.Read(ref _current).Version;
    public GraphSnapshot Current => Volatile.Read(ref _current);
    public int[] TopologicalOrder() => Current.TopologicalOrder();

    public bool TryApply(IReadOnlyList<EdgeEdit> batch) => TryApply(batch, out _);

    /// <summary>All-or-nothing: the new state is fully built and checked, then published atomically.</summary>
    /// <param name="committed">The snapshot this call published (null on failure).</param>
    public bool TryApply(IReadOnlyList<EdgeEdit> batch, out GraphSnapshot? committed)
    {
        ArgumentNullException.ThrowIfNull(batch);
        committed = null;
        lock (_writeLock)
        {
            GraphSnapshot cur = _current;
            int v = cur.Vertices;
            if (cur.Version == long.MaxValue) return false;
            foreach (var e in batch)                                         // validate first: cheap rejection
                if ((uint)e.From >= (uint)v || (uint)e.To >= (uint)v || e.From == e.To) return false;
            var edges = new HashSet<long>(cur.EdgesForCopy);                 // private copy: O(E)
            foreach (var e in batch)
            {
                long key = GraphSnapshot.Key(e.From, e.To);
                if (e.Add) edges.Add(key); else edges.Remove(key);           // duplicates / missing edges: no change
            }
            if (edges.Count > MaxEdges) return false;
            // CSR by counting sort, O(V + E)
            var offsets = new int[v + 1];
            foreach (long k in edges) offsets[(int)(k >> 32) + 1]++;
            for (int i = 0; i < v; i++) offsets[i + 1] += offsets[i];
            var targets = new int[edges.Count]; var fill = (int[])offsets.Clone();
            foreach (long k in edges) targets[fill[(int)(k >> 32)]++] = (int)k;
            var order = GraphSnapshot.Kahn(v, offsets, targets);
            if (order is null) return false;                                 // the batch result has a cycle
            var next = new GraphSnapshot(v, cur.Version + 1, edges, offsets, targets, order);   // order reused
            Volatile.Write(ref _current, next);                              // linearization point
            committed = next;
            return true;
        }
    }
}
```

#### 3. ビルド方法・使用環境

```powershell
# 作業フォルダー: scratchpad\cs\q09（試験専用の一時領域）。cs\build.ps1 -Q q09 が次を順に実行する
# (1) 下記の q09.csproj を生成  (2) Release ビルド  (3) テスト実行（終了コード 0 = 全件合格）
dotnet build q09.csproj -c Release -nologo
dotnet bin\Release\net10.0\q09.dll
# 結果: ビルド成功・0 個の警告・0 エラー、実行の終了コード 0
#
# q09.csproj（全問共通。AssemblyName だけが問題ごとに異なる）
# <Project Sdk="Microsoft.NET.Sdk">
#   <PropertyGroup>
#     <OutputType>Exe</OutputType>
#     <TargetFramework>net10.0</TargetFramework>
#     <LangVersion>14.0</LangVersion>
#     <Nullable>enable</Nullable>
#     <ImplicitUsings>disable</ImplicitUsings>
#     <AnalysisLevel>latest</AnalysisLevel>
#     <AllowUnsafeBlocks>false</AllowUnsafeBlocks>
#     <InvariantGlobalization>true</InvariantGlobalization>
#     <AssemblyName>q09</AssemblyName>
#     <RootNamespace>Exam</RootNamespace>
#   </PropertyGroup>
# </Project>
```

**使用環境（全問共通）**

- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド）
- ランタイム：**Microsoft.NETCore.App 10.0.15（.NET 10）**。実行時に `Environment.Version` = 10.0.15 であることを確認しました。
- 言語とターゲット：`TargetFramework=net10.0`、`LangVersion=14.0`（C# 14。C# 14 の extension メンバーのコンパイルも確認済み）、`Nullable=enable`、`AnalysisLevel=latest`、`ImplicitUsings=disable`、`AllowUnsafeBlocks=false`、構成は **Release**。
- SDK：この PC にインストールされている SDK は **.NET SDK 11.0.100** だけです。.NET 10 用の参照パック（10.0.15）を使い、`net10.0` 向けにビルドしました。.NET 10 SDK そのものはインストールされておらず、無断導入の禁止に従って追加していません。そのため、コンパイラは SDK 11 に同梱のもので、言語バージョンを 14.0 に固定して使っています。
- NuGet パッケージ・第三者ライブラリ：使用していません（.NET 共有フレームワークの API だけ）。
- 各問は独立したコンソールプロジェクトです（`qNN.csproj` = 下記の内容、`QNN.cs` = 実装、`Tests.cs` = テスト）。ビルド時の警告は全問 **0 件**、テストの終了コードは全問 0 です。

#### 4. 実際に使用したテストコード

```csharp
// Q09 tests
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Exam;

static class T
{
    public static int Pass, Fail;
    static int _f0;
    public static void Check(bool ok, string what) { if (ok) Pass++; else { Fail++; Console.WriteLine($"  FAIL: {what}"); } }
    public static void Begin() => _f0 = Fail;
    public static void Report(string name) => Console.WriteLine($"[{(Fail == _f0 ? "PASS" : "FAIL")}] {name}");
}

/// <summary>A batch whose indexer throws at a given position (exception during TryApply).</summary>
sealed class ThrowingBatch(IReadOnlyList<EdgeEdit> inner, int throwAt) : IReadOnlyList<EdgeEdit>
{
    public EdgeEdit this[int i] => i == throwAt ? throw new InvalidOperationException("injected") : inner[i];
    public int Count => inner.Count;
    public IEnumerator<EdgeEdit> GetEnumerator() { for (int i = 0; i < Count; i++) yield return this[i]; }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

static class Program
{
    static EdgeEdit A(int f, int t) => new(f, t, true);
    static EdgeEdit R(int f, int t) => new(f, t, false);

    // independent reference: bitset scan for the smallest available vertex each step (O(V^2/64 + E))
    static int[]? RefOrder(int v, IEnumerable<(int From, int To)> edges)
    {
        var adj = new List<int>[v]; var indeg = new int[v];
        foreach (var (f, t) in edges) { (adj[f] ??= new()).Add(t); indeg[t]++; }
        var avail = new ulong[(v + 63) / 64];
        for (int i = 0; i < v; i++) if (indeg[i] == 0) avail[i >> 6] |= 1UL << (i & 63);
        var res = new int[v];
        for (int k = 0; k < v; k++)
        {
            int w = 0; while (w < avail.Length && avail[w] == 0) w++;
            if (w == avail.Length) return null;
            int u = w * 64 + System.Numerics.BitOperations.TrailingZeroCount(avail[w]);
            avail[w] &= ~(1UL << (u & 63)); res[k] = u;
            if (adj[u] != null) foreach (int t in adj[u]) if (--indeg[t] == 0) avail[t >> 6] |= 1UL << (t & 63);
        }
        return res;
    }
    static bool IsTopo(GraphSnapshot s, int[] o)
    {
        var pos = new int[s.Vertices]; Array.Fill(pos, -1);
        for (int i = 0; i < o.Length; i++) { if ((uint)o[i] >= s.Vertices || pos[o[i]] >= 0) return false; pos[o[i]] = i; }
        return o.Length == s.Vertices && s.Edges().All(e => pos[e.From] < pos[e.To]);
    }
    static long Fingerprint(GraphSnapshot s) { long h = s.EdgeCount; foreach (var (f, t) in s.Edges()) h += (long)(((ulong)(uint)f * 0x9E3779B97F4A7C15UL) ^ ((ulong)(uint)t * 0xC2B2AE3D27D4EB4FUL)); return h; }

    static int Main()
    {
        _ = Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(_ => { Console.WriteLine("WATCHDOG: hang"); Environment.Exit(2); });

        T.Begin();   // T1 basic orders
        {
            var g = new GraphStore(5);
            T.Check(g.TopologicalOrder().SequenceEqual(new[] { 0, 1, 2, 3, 4 }) && g.Version == 0, "no edges: 0 1 2 3 4");
            T.Check(g.TryApply([A(0, 1), A(0, 2), A(1, 3), A(2, 3)]) && g.TopologicalOrder().SequenceEqual(new[] { 0, 1, 2, 3, 4 }), "diamond DAG");
            var h = new GraphStore(6);
            h.TryApply([A(5, 0), A(4, 0), A(2, 1)]);
            T.Check(h.TopologicalOrder().SequenceEqual(new[] { 2, 1, 3, 4, 5, 0 }), "several valid orders: lexicographically smallest 2 1 3 4 5 0");
            long v0 = h.Version;
            T.Check(h.TryApply([A(2, 1), A(2, 1)]) && h.Current.EdgeCount == 3 && h.Version == v0 + 1, "duplicate edges count once; Version +1 even without a change");
            T.Check(h.TryApply([R(3, 4), R(3, 4)]) && h.Current.EdgeCount == 3 && h.Version == v0 + 2, "removing a missing edge twice: no change, Version +1");
            T.Check(h.TryApply([]) && h.Version == v0 + 3, "empty batch: Version +1");
            T.Check(h.TryApply([R(2, 1)]) && !h.Current.ContainsEdge(2, 1) && h.TryApply([R(2, 1)]), "remove, then remove again");
            T.Check(RefOrder(6, h.Current.Edges())!.SequenceEqual(h.TopologicalOrder()), "matches the reference");
        }
        T.Report("T1 empty graph, diamond, several solutions, duplicate edges, re-removing a removed edge");

        T.Begin();   // T2 whole-batch rollback
        {
            var g = new GraphStore(1000);
            g.TryApply([A(0, 1), A(1, 2)]);
            var before = g.Current;
            bool[] r =
            [
                g.TryApply([A(3, 4), A(5, 1000)]),            // invalid vertex in the middle
                g.TryApply([A(3, 4), A(-1, 2)]),
                g.TryApply([A(3, 4), A(7, 7), A(8, 9)]),       // self loop
                g.TryApply([A(3, 4), A(2, 0), A(8, 9)]),       // closes the cycle 0 -> 1 -> 2 -> 0
                g.TryApply([A(10, 11), A(11, 12), A(12, 10)]), // cycle entirely inside the batch
            ];
            T.Check(r.All(x => !x) && ReferenceEquals(g.Current, before) && g.Version == 1 && !g.Current.ContainsEdge(3, 4), "invalid vertex / self loop / cycle anywhere in the batch: false, graph and Version unchanged");
            T.Check(g.TryApply([A(20, 21), A(21, 20), R(21, 20)]), "a cycle created and removed again inside one batch: the final result is a DAG -> accepted");
            var many = new List<EdgeEdit>(); for (int i = 0; i < 1000 && many.Count <= 400_000; i++) for (int j = i + 1; j < 1000 && many.Count <= 400_000; j++) many.Add(A(i, j));
            var b2 = g.Current;
            T.Check(!g.TryApply(many) && ReferenceEquals(g.Current, b2), $"final edge count above 400,000 ({many.Count} distinct new edges + existing): rejected, unchanged");
            T.Check(Throws(() => g.TryApply(new ThrowingBatch([A(30, 31), A(31, 32)], 1))) && ReferenceEquals(g.Current, b2), "an exception from the batch during TryApply leaves the graph unchanged");
            var mx = new GraphStore(3, long.MaxValue);
            T.Check(!mx.TryApply([]) && mx.Version == long.MaxValue, "Version at long.MaxValue: TryApply fails");
            T.Check(Throws(() => new GraphStore(0)) && Throws(() => new GraphStore(100_001)), "vertices must be 1..100,000");
        }
        T.Report("T2 invalid vertex / self loop / edge limit / cycle in the middle of a batch: complete rollback");

        T.Begin();   // T3 100,000 vertices / 400,000 edges
        {
            var rnd = new Random(42); const int V = 100_000, E = 400_000;
            var rank = Enumerable.Range(0, V).OrderBy(_ => rnd.Next()).ToArray();
            var set = new HashSet<long>(); var batch = new List<EdgeEdit>(E);
            while (batch.Count < E) { int a = rnd.Next(V), b = rnd.Next(V); if (a == b) continue; if (rank[a] > rank[b]) (a, b) = (b, a); if (set.Add(((long)a << 32) | (uint)b)) batch.Add(A(a, b)); }
            var g = new GraphStore(V);
            long mem0 = GC.GetTotalMemory(true);
            var sw = Stopwatch.StartNew(); bool ok = g.TryApply(batch); long tApply = sw.ElapsedMilliseconds;
            long mem1 = GC.GetTotalMemory(true);
            sw.Restart(); var o = g.Current.Kahn2(); long tOrder = sw.ElapsedMilliseconds;
            sw.Restart(); var o2 = g.TopologicalOrder(); long tCached = sw.ElapsedMilliseconds;
            T.Check(ok && g.Current.EdgeCount == E, "100,000 vertices / 400,000 edges committed");
            T.Check(IsTopo(g.Current, o2) && o.SequenceEqual(o2), "valid topological order");
            var sw2 = Stopwatch.StartNew(); var reference = RefOrder(V, g.Current.Edges()); long tRef = sw2.ElapsedMilliseconds;
            T.Check(reference!.SequenceEqual(o2), "equals the independent bitset reference (lexicographically smallest)");
            T.Check(set.Count == g.Current.EdgeCount && set.All(k => g.Current.ContainsEdge((int)(k >> 32), (int)k)), "graph equals the model edge set");
            Console.WriteLine($"  TryApply(400,000 edits)={tApply} ms, TopologicalOrder (Kahn + heap)={tOrder} ms, cached copy={tCached} ms, reference={tRef} ms, retained managed memory ~{(mem1 - mem0) / (1024 * 1024)} MiB");
        }
        T.Report("T3 100,000 vertices / 400,000 edges: valid and lexicographically smallest order, model equality, time and memory measured");

        T.Begin();   // T4 4 editors + 8 readers
        {
            var g = new GraphStore(2000); var committed = new ConcurrentDictionary<long, long> { [0] = Fingerprint(g.Current) };
            long ops = 0, badReads = 0, accepted = 0, rejected = 0; var readSnaps = new ConcurrentBag<GraphSnapshot>();
            var editors = Enumerable.Range(0, 4).Select(w => Task.Run(() =>
            {
                var rnd = new Random(w + 1);
                for (int i = 0; i < 2500; i++)
                {
                    var b = new List<EdgeEdit>();
                    for (int k = rnd.Next(1, 6); k > 0; k--) { int a = rnd.Next(2000), c = rnd.Next(2000); b.Add(new EdgeEdit(a, c, rnd.Next(4) != 0)); }
                    if (g.TryApply(b, out var snap)) { committed[snap!.Version] = Fingerprint(snap); Interlocked.Increment(ref accepted); }
                    else Interlocked.Increment(ref rejected);
                    Interlocked.Increment(ref ops);
                }
            })).ToArray();
            var readers = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < 12_000; i++)
                {
                    var s = g.Current; var o = s.TopologicalOrder();
                    if (!IsTopoFast(s, o)) Interlocked.Increment(ref badReads);
                    if (i % 500 == 0) readSnaps.Add(s);
                    Interlocked.Increment(ref ops);
                }
            })).ToArray();
            Task.WaitAll([.. editors, .. readers]);
            long mismatched = readSnaps.Count(s => !committed.TryGetValue(s.Version, out long fp) || fp != Fingerprint(s));
            T.Check(badReads == 0, "every order read by readers is a valid topological order of its snapshot");
            T.Check(mismatched == 0, $"{readSnaps.Count} sampled reader snapshots equal the committed graph of their version");
            T.Check(g.Version == accepted, $"final Version {g.Version} == accepted batches {accepted} (rejected {rejected})");
            Console.WriteLine($"  operations={ops} (10,000 TryApply + 96,000 TopologicalOrder)");
        }
        T.Report("T4 4 editors + 8 readers, 106,000 operations: readers see only committed, consistent graphs");

        Console.WriteLine($"RESULT: {T.Pass} checks passed, {T.Fail} failed");
        return T.Fail == 0 ? 0 : 1;
    }

    static bool IsTopoFast(GraphSnapshot s, int[] o)
    {
        var pos = new int[s.Vertices];
        for (int i = 0; i < o.Length; i++) pos[o[i]] = i;
        foreach (var (f, t) in s.Edges()) if (pos[f] >= pos[t]) return false;
        return o.Length == s.Vertices;
    }
    static bool Throws(Action a) { try { a(); } catch { return true; } return false; }
}

static class SnapshotTestExtensions
{
    // recompute the order from scratch (not the cached copy) for timing
    public static int[] Kahn2(this GraphSnapshot s)
    {
        var edges = s.Edges().ToList(); int v = s.Vertices;
        var offsets = new int[v + 1]; foreach (var (f, _) in edges) offsets[f + 1]++;
        for (int i = 0; i < v; i++) offsets[i + 1] += offsets[i];
        var targets = new int[edges.Count]; var fill = (int[])offsets.Clone();
        foreach (var (f, t) in edges) targets[fill[f]++] = t;
        return GraphSnapshot.Kahn(v, offsets, targets)!;
    }
}
```

#### 5. 検証結果（期待結果・実測結果・実施状況）

実行結果は `RESULT: 21 checks passed, 0 failed`、終了コード 0、所要 4.7 秒（Release）でした。

| テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 空グラフの辞書順 | 0 1 2 3 4 | 一致 | 実施 |
| ダイヤモンド DAG | 0 1 2 3 4 | 一致 | 実施 |
| 複数の解があるグラフ | 5→0、4→0、2→1 → 2 1 3 4 5 0（辞書順最小）。参照実装とも一致 | 一致 | 実施 |
| 重複した辺 | 1本として扱い、Version は +1 | 一致 | 実施 |
| 削除済みの辺の再削除 | 変化なし、Version は +1。空のバッチも +1 | 一致 | 実施 |
| バッチの途中の無効な頂点・自己ループ・閉路 | 5種類のバッチ（途中に無効な頂点・負の頂点・自己ループ・既存の辺と閉路を作る辺・バッチ内だけの閉路）はすべて false。グラフ（同じ参照）と Version は不変 | 一致 | 実施 |
| バッチ内で作って消す閉路 | 最終結果が DAG なら受理（バッチの結果で判定） | 一致 | 実施 |
| 上限超過 | 最終の辺数が 400,000 を超えるバッチは拒否され、不変 | 一致 | 実施 |
| 例外による失敗 | バッチの列挙中に例外が出ても、グラフは不変 | 一致 | 実施 |
| Version の上限・頂点数の範囲 | long.MaxValue では失敗。0 と 100,001 は拒否 | 一致 | 実施 |
| 100,000 頂点・400,000 辺 | 正しいトポロジカル順序。独立したビット集合の参照実装（辞書順最小）とモデルの辺集合に一致 | TryApply 115 ms、順序の計算 78 ms、キャッシュのコピー 0 ms、保持するマネージドメモリは約 10 MiB | 実施 |
| 4編集者 + 8読者・合計10万操作以上 | 106,000 操作（TryApply 10,000 + TopologicalOrder 96,000）。読者が得た順序はすべて、そのスナップショットの正しい順序。抽出した読者のスナップショットは、同じ Version で確定したグラフと一致。最終 Version = 受理されたバッチの数 | 一致 | 実施 |

**自律修正：** 並列試験で、書き手が自分の確定した版の指紋を記録するときに `Current` を読み直していたため、別の書き手が先に確定すると誤った版を記録するテスト上の欠陥がありました。そこで、`TryApply(batch, out GraphSnapshot? committed)` で確定したスナップショット自体を返すようにし、テストもそれを使う形に直しました。

#### 6. 技術的な正当性と設計判断

- **線形化点：** 成功した TryApply は、`Volatile.Write(ref _current, next)` の時点です。読み手が `Volatile.Read` で得たスナップショットは、完全に構築・検査が済んでいて、以後変更されません。そのため、読み手は常に確定済みの一貫したグラフを見ます。
- **例外安全：** 新しい状態は、すべて私的なコピーの上で作ります。公開は最後の1つの参照の書込みだけです。失敗（false）でも例外でも、公開中の状態は不変です。
- **計算量：** TryApply は O(V + E)（辺集合のコピー、計数ソートによる CSR、Kahn 法）＋ O(バッチ)。TopologicalOrder は O((V+E) log V)（PriorityQueue。スナップショットごとに1回計算してキャッシュし、呼出しごとに配列のコピーを返す）。メモリは O(V+E) です。
- **非再帰：** 再帰 DFS は使わず、Kahn 法の反復だけで計算します。
- **辞書順最小の定義：** 各段階で、入次数 0 の頂点のうち最小の ID を選びます。PriorityQueue の優先度に ID そのものを使います。

#### 7. 未実施項目・制約・残課題

- 書き手は1つのロックで直列化しているので、書込みの並列度は1です（仕様はロック方式を認めています）。各バッチで O(V+E) のコピーを作るため、巨大なグラフを高頻度に更新する用途には向きません。
- メモリ量は `GC.GetTotalMemory` による概算です。

---

## Q10【最終地獄級】CASベースのBounded MPMCキューとC#メモリモデル

**試験の狙い：** Interlocked/Volatile、環状世代番号、GCによるオブジェクト寿命、競合中のfalseの意味を正確に設計する最終地獄級問題。

### 元コード（危険・不完全）

```csharp
using System;
class QueueBad<T> {
    readonly T?[] q = new T?[1024];
    int head, tail;
    public void Enqueue(T x) => q[tail++ % 1024] = x;
    public T? Dequeue() => q[head++ % 1024]; // 競合・未公開読取・上書き
}
```

### 要求仕様

1. `BoundedMpmcQueue<T> where T : class` をC#/.NET10で実装。容量はコンストラクタで2の冪の正数から選択（2..65536）。公開APIは `bool TryEnqueue(T item)`、`bool TryDequeue(out T? item)`、`int Capacity`、`long ApproximateCount`。nullのenqueueはArgumentNullException。
2. リング各スロットに世代/sequence番号を持たせ、`Interlocked.CompareExchange`、`Volatile.Read/Write` を使って4生産者＋4消費者以上の並行アクセスを正しく制御。lock、Monitor、SemaphoreSlim、Channel、ConcurrentQueue、SpinLock、unsafe、ネイティブコードは禁止。
3. TryEnqueueは成功した場合だけアイテムをちょうど1回公開し、TryDequeueの成功はちょうど1つの公開済みアイテムを返す。各操作の**成功に関する線形化可能性**を説明。参照値がGCによって回収されないようスロットの参照を適切な時点まで保持し、dequeue後は参照を消去して保持リークを防止。
4. **falseの意味を明示：** TryEnqueueのfalseは満杯または競合中で今すぐ完了できなかったこと、TryDequeueのfalseは空または競合中で今すぐ完了できなかったことを表す。**競合中のfalseを『必ず満杯』『必ず空』と解釈する仕様ではない。** ただし競合がない静止状態では空/満杯の判定が正確でなければならない。
5. 予約後・公開前に別スレッドが停止しても、他のスレッドがデータ競合や未公開値の読出しをしないこと。待機が必要な箇所を無限ループで占有しない。CAS競合時に他スレッドの進行が保証される範囲を説明。
6. ApproximateCountは競合中の近似値でよいが、負数やCapacity超過は公開しない。静止時は実数と一致。longのsequenceカウンタが周回しない操作数（各生産者/消費者合計2^62未満）を今回の実行契約とする。
7. GCの利用は認めるが、実際のConcurrentQueueやChannelを内部実装として委譲することは禁止。競合時の仕様は上記の明示条件を優先し、出題者が後から『falseは空のときだけ』等の条件を追加して採点してはならない。

### 必須検証

1. 容量2/4/1024/65536、空・満杯、同じインスタンスの再利用、参照同一性、GC後も未取り出し要素が有効。
2. 1対1、4対4、8対8。4対4で各生産者25万件・計100万件、各IDは一度だけ取り出され欠損・重複0。
3. 予約直後・公開直前・読出し前・スロット再利用前の停止フックで決定的に順序を変え、未公開データの取得なし・永久停止なしを確認。
4. 1千万回のラップアラウンドに相当する繰返し、静止時Countの一致、取り出し済み参照の保持が解消されることをWeakReference等で検査。
5. 実行可能なら高負荷でGCの世代別回収とThreadPool競合を試し、メモリ順序・成功操作の線形化点・失敗時の扱いを文章で説明。

### ★★FREYJAの回答【Q10・記入欄】

> **以下は★★FREYJA専用の解答領域。** 問題本文と元コードには触れず、7項目をすべて実際の回答で置き換える。未実施の検証は未実施と正直に記載する。

#### 1. 根本原因・危険性・影響

**元コード `QueueBad` の問題点**

| # | 箇所 | 根本原因 | 実害 |
| --- | --- | --- | --- |
| 1 | `tail++` / `head++` | 非原子的な読み・加算・書込み | 2つの生産者が同じ位置に書く（要素の消失）。2つの消費者が同じ要素を取る（重複） |
| 2 | 満杯・空の判定がない | — | 未読の要素を上書きする。空の時に未初期化（null）や前の周回の値を読む |
| 3 | 公開のメモリ順序がない | 書込みと位置の更新が、他のスレッドから順不同に見える | 未公開の値の読出し |
| 4 | 取り出した後も参照を残す | 配列が参照を保持し続ける | 保持によるリーク（GC が回収できない） |
| 5 | `int` の位置が周回する | 長時間の運用 | 負の添字や順序の破綻 |

**修正方針：** 各スロットに `long Sequence` と `T? Item` を持つ、有界のリングキューです（シーケンス番号方式）。
- 生産者は「スロットの seq が自分の位置 pos と等しい」ことを `Volatile.Read` で確かめてから、`Interlocked.CompareExchange` で enqueue 位置を pos → pos+1 に進め、チケットを独占します。そして Item を書き、`Volatile.Write(seq = pos+1)` で公開します。
- 消費者は「seq == pos+1」を確かめてから CAS で dequeue 位置を独占し、Item を読んで **null に消去**し、`Volatile.Write(seq = pos+Capacity)` でスロットを次の周回へ返します。
- lock・Monitor・SemaphoreSlim・Channel・ConcurrentQueue・SpinLock・unsafe は使っていません。
- 2つの位置は、明示的なレイアウトで別々のキャッシュラインに置いています。

#### 2. 完全な修正版C#コード

```csharp
// Q10: BoundedMpmcQueue<T> - bounded multi-producer / multi-consumer ring with per-slot sequence numbers
// (Interlocked + Volatile only; no lock, Monitor, SemaphoreSlim, Channel, ConcurrentQueue, SpinLock, unsafe) (C# 14 / .NET 10)
// Original implementation by ★★FREYJA for the C# hell-level exam.
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Exam;

public enum MpmcPoint { AfterReserve, BeforePublish, BeforeRead, BeforeRelease }

/// <summary>The two ring positions on different 64-byte cache lines (explicit layout must be non-generic).</summary>
[StructLayout(LayoutKind.Explicit, Size = 192)]
internal struct MpmcCounters
{
    [FieldOffset(64)] public long Enqueue;
    [FieldOffset(128)] public long Dequeue;
}

public sealed class BoundedMpmcQueue<T> where T : class
{
    struct Slot
    {
        public long Sequence;       // generation: == pos (free for ticket pos), == pos + 1 (holds ticket pos)
        public T? Item;
    }

    readonly Slot[] _slots;
    readonly int _mask;
    MpmcCounters _pos;                // enqueue / dequeue positions on different cache lines

    /// <summary>Test-only pause points (per thread). Null in production.</summary>
    [ThreadStatic] internal static Action<MpmcPoint>? Hook;

    public BoundedMpmcQueue(int capacity)
    {
        if (capacity < 2 || capacity > 65536 || !BitOperations.IsPow2(capacity))
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "capacity must be a power of two in 2..65536");
        _slots = new Slot[capacity];
        _mask = capacity - 1;
        for (int i = 0; i < capacity; i++) _slots[i].Sequence = i;
    }

    public int Capacity => _slots.Length;

    /// <summary>Approximate while operations run; exact when quiescent. Never negative, never above Capacity.</summary>
    public long ApproximateCount
    {
        get
        {
            long deq = Volatile.Read(ref _pos.Dequeue);
            long enq = Volatile.Read(ref _pos.Enqueue);
            long n = enq - deq;
            return n < 0 ? 0 : n > _slots.Length ? _slots.Length : n;
        }
    }

    /// <summary>false: full, or another operation on the head slot is in progress (cannot complete right now).</summary>
    public bool TryEnqueue(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        long pos = Volatile.Read(ref _pos.Enqueue);
        while (true)
        {
            ref Slot slot = ref _slots[pos & _mask];
            long seq = Volatile.Read(ref slot.Sequence);            // acquire: the consumer's clear of Item is visible
            long diff = seq - pos;
            if (diff == 0)
            {
                long seen = Interlocked.CompareExchange(ref _pos.Enqueue, pos + 1, pos);
                if (seen == pos)                                    // ticket `pos` is ours alone
                {
                    Hook?.Invoke(MpmcPoint.AfterReserve);
                    slot.Item = item;                               // the slot keeps the reference alive (GC root)
                    Hook?.Invoke(MpmcPoint.BeforePublish);
                    Volatile.Write(ref slot.Sequence, pos + 1);     // release: publishes Item (linearization point of a success)
                    return true;
                }
                pos = seen;                                         // lost the CAS: retry with the current position
            }
            else if (diff < 0)
            {
                return false;                                       // slot still holds ticket pos - Capacity: full (or its consumer is in progress)
            }
            else
            {
                pos = Volatile.Read(ref _pos.Enqueue);         // another producer took this ticket
            }
        }
    }

    /// <summary>false: empty, or the head item is reserved but not yet published (cannot complete right now).
    /// On false, <paramref name="item"/> is set to null.</summary>
    public bool TryDequeue(out T? item)
    {
        long pos = Volatile.Read(ref _pos.Dequeue);
        while (true)
        {
            ref Slot slot = ref _slots[pos & _mask];
            long seq = Volatile.Read(ref slot.Sequence);            // acquire: pairs with the producer's release
            long diff = seq - (pos + 1);
            if (diff == 0)
            {
                long seen = Interlocked.CompareExchange(ref _pos.Dequeue, pos + 1, pos);
                if (seen == pos)                                    // this item is ours alone (linearization point)
                {
                    Hook?.Invoke(MpmcPoint.BeforeRead);
                    T value = slot.Item!;
                    slot.Item = null;                               // drop the reference: no retention leak
                    Hook?.Invoke(MpmcPoint.BeforeRelease);
                    Volatile.Write(ref slot.Sequence, pos + _slots.Length);   // release: free for ticket pos + Capacity
                    item = value;
                    return true;
                }
                pos = seen;
            }
            else if (diff < 0)
            {
                item = null;
                return false;                                       // nothing published at the head: empty (or producer in progress)
            }
            else
            {
                pos = Volatile.Read(ref _pos.Dequeue);
            }
        }
    }
}
```

#### 3. ビルド方法・使用環境

```powershell
# 作業フォルダー: scratchpad\cs\q10（試験専用の一時領域）。cs\build.ps1 -Q q10 が次を順に実行する
# (1) 下記の q10.csproj を生成  (2) Release ビルド  (3) テスト実行（終了コード 0 = 全件合格）
dotnet build q10.csproj -c Release -nologo
dotnet bin\Release\net10.0\q10.dll
# 結果: ビルド成功・0 個の警告・0 エラー、実行の終了コード 0
#
# q10.csproj（全問共通。AssemblyName だけが問題ごとに異なる）
# <Project Sdk="Microsoft.NET.Sdk">
#   <PropertyGroup>
#     <OutputType>Exe</OutputType>
#     <TargetFramework>net10.0</TargetFramework>
#     <LangVersion>14.0</LangVersion>
#     <Nullable>enable</Nullable>
#     <ImplicitUsings>disable</ImplicitUsings>
#     <AnalysisLevel>latest</AnalysisLevel>
#     <AllowUnsafeBlocks>false</AllowUnsafeBlocks>
#     <InvariantGlobalization>true</InvariantGlobalization>
#     <AssemblyName>q10</AssemblyName>
#     <RootNamespace>Exam</RootNamespace>
#   </PropertyGroup>
# </Project>
```

**使用環境（全問共通）**

- OS / CPU：Windows 11 Pro 10.0.26200（x64）／ Intel Core i7-4710MQ（4コア8スレッド）
- ランタイム：**Microsoft.NETCore.App 10.0.15（.NET 10）**。実行時に `Environment.Version` = 10.0.15 であることを確認しました。
- 言語とターゲット：`TargetFramework=net10.0`、`LangVersion=14.0`（C# 14。C# 14 の extension メンバーのコンパイルも確認済み）、`Nullable=enable`、`AnalysisLevel=latest`、`ImplicitUsings=disable`、`AllowUnsafeBlocks=false`、構成は **Release**。
- SDK：この PC にインストールされている SDK は **.NET SDK 11.0.100** だけです。.NET 10 用の参照パック（10.0.15）を使い、`net10.0` 向けにビルドしました。.NET 10 SDK そのものはインストールされておらず、無断導入の禁止に従って追加していません。そのため、コンパイラは SDK 11 に同梱のもので、言語バージョンを 14.0 に固定して使っています。
- NuGet パッケージ・第三者ライブラリ：使用していません（.NET 共有フレームワークの API だけ）。
- 各問は独立したコンソールプロジェクトです（`qNN.csproj` = 下記の内容、`QNN.cs` = 実装、`Tests.cs` = テスト）。ビルド時の警告は全問 **0 件**、テストの終了コードは全問 0 です。

- 並列の結果は、Windows 11・i7-4710MQ（4コア8スレッド、x86-64）・.NET 10.0.15・Release 構成での測定値です。

#### 4. 実際に使用したテストコード

```csharp
// Q10 tests
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Exam;

static class T
{
    public static int Pass, Fail;
    static int _f0;
    public static void Check(bool ok, string what) { if (ok) Pass++; else { Fail++; Console.WriteLine($"  FAIL: {what}"); } }
    public static void Begin() => _f0 = Fail;
    public static void Report(string name) => Console.WriteLine($"[{(Fail == _f0 ? "PASS" : "FAIL")}] {name}");
    public static bool Throws<E>(Action a) where E : Exception { try { a(); } catch (E) { return true; } catch { return false; } return false; }
}

sealed class Item(long id) { public readonly long Id = id; public readonly byte[] Payload = new byte[16]; }

static class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static List<WeakReference> EnqueueUnrooted(BoundedMpmcQueue<Item> q, int n, long start)
    { var w = new List<WeakReference>(); for (int i = 0; i < n; i++) { var it = new Item(start + i); w.Add(new WeakReference(it)); q.TryEnqueue(it); } return w; }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void DrainAndDrop(BoundedMpmcQueue<Item> q) { while (q.TryDequeue(out _)) { } }
    static void FullGc() { for (int i = 0; i < 3; i++) { GC.Collect(2, GCCollectionMode.Forced, blocking: true); GC.WaitForPendingFinalizers(); } }

    static bool Run(int np, int nc, long per, bool gcPressure, out double secs)
    {
        var q = new BoundedMpmcQueue<Item>(1024); long total = np * per;
        var seen = new int[total]; long consumed = 0, bad = 0, orderViolations = 0; int go = 0;
        var stop = new CancellationTokenSource();
        Thread? gc = gcPressure ? new Thread(() => { var junk = new List<byte[]>(); while (!stop.IsCancellationRequested) { junk.Add(new byte[4096]); if (junk.Count > 2000) junk.Clear(); if (Environment.TickCount % 7 == 0) GC.Collect(Environment.TickCount % 3); } }) { IsBackground = true } : null;
        gc?.Start();
        var threads = new List<Thread>();
        for (int p = 0; p < np; p++) { int pid = p; threads.Add(new Thread(() => { while (Volatile.Read(ref go) == 0) { } for (long i = 0; i < per; i++) { var it = new Item(pid * per + i); while (!q.TryEnqueue(it)) Thread.Yield(); } })); }
        for (int c = 0; c < nc; c++) threads.Add(new Thread(() =>
        {
            var last = new long[np]; Array.Fill(last, -1);
            while (Volatile.Read(ref go) == 0) { }
            while (Interlocked.Read(ref consumed) < total)
            {
                if (!q.TryDequeue(out var it)) { Thread.Yield(); continue; }
                Interlocked.Increment(ref consumed);
                if (it is null || it.Id < 0 || it.Id >= total) { Interlocked.Increment(ref bad); continue; }
                Interlocked.Increment(ref seen[it.Id]);
                int pid = (int)(it.Id / per); long seq = it.Id % per;
                if (seq <= last[pid]) Interlocked.Increment(ref orderViolations);
                last[pid] = seq;
            }
        }));
        foreach (var t in threads) t.Start();
        var sw = Stopwatch.StartNew(); Volatile.Write(ref go, 1);
        foreach (var t in threads) t.Join();
        secs = sw.Elapsed.TotalSeconds; stop.Cancel(); gc?.Join();
        long dup = seen.Where(s => s > 1).Sum(s => (long)s - 1), miss = seen.Count(s => s == 0);
        Console.WriteLine($"    {np}P x {nc}C, {total} items{(gcPressure ? " + GC pressure" : "")}: {secs:F3}s duplicates={dup} missing={miss} out_of_range={bad} per-producer order violations={orderViolations} count_after={q.ApproximateCount}");
        return dup == 0 && miss == 0 && bad == 0 && orderViolations == 0 && q.ApproximateCount == 0;
    }

    // Runs `paused` on its own thread with a hook that blocks at `point` until released.
    static (Thread t, ManualResetEventSlim reached, ManualResetEventSlim release) Paused(MpmcPoint point, Action body)
    {
        var reached = new ManualResetEventSlim(); var release = new ManualResetEventSlim();
        var t = new Thread(() => { bool once = false; BoundedMpmcQueue<Item>.Hook = p => { if (p == point && !once) { once = true; reached.Set(); release.Wait(); } }; body(); BoundedMpmcQueue<Item>.Hook = null; });
        t.Start();
        return (t, reached, release);
    }
    static bool Bounded(Func<bool> op, int ms = 2000) { var sw = Stopwatch.StartNew(); op(); return sw.ElapsedMilliseconds < ms; }

    static int Main()
    {
        _ = Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(_ => { Console.WriteLine("WATCHDOG: hang"); Environment.Exit(2); });

        T.Begin();   // T1 capacities, empty / full, reuse, identity, GC
        {
            foreach (int bad in new[] { 0, 1, 3, 6, 65537, 131072, -2 }) T.Check(T.Throws<ArgumentOutOfRangeException>(() => new BoundedMpmcQueue<Item>(bad)), $"capacity {bad} rejected");
            foreach (int cap in new[] { 2, 4, 1024, 65536 })
            {
                var q = new BoundedMpmcQueue<Item>(cap); bool ok = q.Capacity == cap;
                for (int round = 0; round < 3; round++)
                {
                    var items = Enumerable.Range(0, cap).Select(i => new Item(i)).ToArray();
                    ok &= !q.TryDequeue(out var none) && none is null && q.ApproximateCount == 0;
                    for (int i = 0; i < cap; i++) { ok &= q.TryEnqueue(items[i]); ok &= q.ApproximateCount == i + 1; }
                    ok &= !q.TryEnqueue(new Item(-1)) && q.ApproximateCount == cap;
                    for (int i = 0; i < cap; i++) { ok &= q.TryDequeue(out var it) && ReferenceEquals(it, items[i]); ok &= q.ApproximateCount == cap - i - 1; }
                }
                T.Check(ok, $"capacity {cap}: empty -> false (null), full -> false, FIFO, reference identity, exact count, 3 reuse rounds");
            }
            var qq = new BoundedMpmcQueue<Item>(4);
            T.Check(T.Throws<ArgumentNullException>(() => qq.TryEnqueue(null!)), "null enqueue -> ArgumentNullException");
            var weak = EnqueueUnrooted(qq, 4, 100);
            FullGc();
            T.Check(weak.All(w => w.IsAlive), "items held only by the queue survive a full GC");
            bool vals = true; for (int i = 0; i < 4; i++) vals &= qq.TryDequeue(out var it) && it!.Id == 100 + i;
            T.Check(vals, "and are intact when dequeued");
        }
        T.Report("T1 capacities 2/4/1024/65536, empty, full, reuse, reference identity, not-yet-dequeued items survive GC");

        T.Begin();   // T2 1:1, 4:4 (4 x 250,000), 8:8
        {
            T.Check(Run(1, 1, 1_000_000, false, out _), "1 producer : 1 consumer, 1,000,000 items");
            for (int r = 0; r < 3; r++) T.Check(Run(4, 4, 250_000, false, out _), $"4 x 4, 250,000 per producer = 1,000,000 (run {r})");
            for (int r = 0; r < 2; r++) T.Check(Run(8, 8, 125_000, false, out _), $"8 x 8 (run {r})");
        }
        T.Report("T2 1:1, 4:4 (3 runs), 8:8 (2 runs): every ID exactly once, no loss, no duplicate, per-producer order");

        T.Begin();   // T3 deterministic pauses
        {
            // (a)/(b) producer paused after reserving / before publishing ticket 0
            foreach (var pt in new[] { MpmcPoint.AfterReserve, MpmcPoint.BeforePublish })
            {
                var q = new BoundedMpmcQueue<Item>(4);
                var (t, reached, release) = Paused(pt, () => q.TryEnqueue(new Item(0)));
                T.Check(reached.Wait(5000), $"{pt}: producer paused with ticket 0 reserved");
                bool others = q.TryEnqueue(new Item(1)) && q.TryEnqueue(new Item(2)) && q.TryEnqueue(new Item(3));
                bool full = Bounded(() => !q.TryEnqueue(new Item(4))) && !q.TryEnqueue(new Item(4));
                bool noRead = true; for (int k = 0; k < 1000; k++) noRead &= Bounded(() => !q.TryDequeue(out _), 50) && !q.TryDequeue(out var x) && x is null;
                T.Check(others && full, $"{pt}: other producers continue; the ring then reports full without blocking");
                T.Check(noRead, $"{pt}: 1000 dequeue attempts return false promptly and never expose the unpublished item");
                release.Set(); t.Join();
                bool order = true; for (int i = 0; i < 4; i++) order &= q.TryDequeue(out var it) && it!.Id == i;
                T.Check(order && !q.TryDequeue(out _), $"{pt}: after resuming, items come out in ticket order 0,1,2,3");
            }
            // (c)/(d) consumer paused after claiming ticket 0 (before reading / before releasing the slot)
            foreach (var pt in new[] { MpmcPoint.BeforeRead, MpmcPoint.BeforeRelease })
            {
                var q = new BoundedMpmcQueue<Item>(4);
                for (int i = 0; i < 4; i++) q.TryEnqueue(new Item(i));
                Item? got = null;
                var (t, reached, release) = Paused(pt, () => { q.TryDequeue(out got); });
                T.Check(reached.Wait(5000), $"{pt}: consumer paused holding ticket 0");
                bool next = q.TryDequeue(out var a) && a!.Id == 1 && q.TryDequeue(out var b) && b!.Id == 2;
                bool wrapBlocked = Bounded(() => !q.TryEnqueue(new Item(5))) && !q.TryEnqueue(new Item(5)) && q.ApproximateCount <= q.Capacity;   // next ticket (4) maps to slot 0
                T.Check(next, $"{pt}: other consumers take tickets 1 and 2");
                T.Check(wrapBlocked, $"{pt}: the next producer ticket maps to the unreleased slot 0: TryEnqueue returns false at once (no blocking, no overwrite)");
                release.Set(); t.Join();
                T.Check(got?.Id == 0, $"{pt}: the paused consumer returns item 0 exactly once");
                bool rest = q.TryEnqueue(new Item(5)) && q.TryEnqueue(new Item(6)) && q.TryEnqueue(new Item(7))
                            && q.TryDequeue(out var c) && c!.Id == 3 && q.TryDequeue(out var d) && d!.Id == 5 && q.TryDequeue(out var e) && e!.Id == 6 && q.TryDequeue(out var f) && f!.Id == 7;
                T.Check(rest, $"{pt}: after release the slot is reusable and order is preserved");
            }
        }
        T.Report("T3 pauses after reserve / before publish / before read / before slot release: no unpublished reads, no endless waiting");

        T.Begin();   // T4 10,000,000 wraparounds, quiescent count, reference release
        {
            var q = new BoundedMpmcQueue<Item>(2); var one = new Item(1); bool ok = true;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 10_000_000; i++) { ok &= q.TryEnqueue(one); ok &= q.TryDequeue(out var x) && ReferenceEquals(x, one); if ((i & 0xFFFFF) == 0) ok &= q.ApproximateCount == 0; }
            T.Check(ok && q.ApproximateCount == 0, $"10,000,000 enqueue/dequeue pairs on capacity 2 (5,000,000 ring wraps) in {sw.ElapsedMilliseconds} ms, count exact");
            var big = new BoundedMpmcQueue<Item>(1024);
            var weak = EnqueueUnrooted(big, 1000, 0);
            DrainAndDrop(big);
            FullGc();
            T.Check(weak.All(w => !w.IsAlive), "after dequeue the slots no longer reference the items (all 1000 collected)");
            T.Check(big.ApproximateCount == 0, "count 0 after draining");
        }
        T.Report("T4 10,000,000 operations of wraparound, exact quiescent count, dequeued references released (WeakReference)");

        T.Begin();   // T5 high load with GC pressure and thread-pool contention
        {
            var hog = Enumerable.Range(0, Environment.ProcessorCount * 2).Select(_ => Task.Run(() => { var sw = Stopwatch.StartNew(); while (sw.ElapsedMilliseconds < 1500) Thread.SpinWait(1000); })).ToArray();
            T.Check(Run(4, 4, 250_000, true, out _), "4 x 4 with concurrent GC.Collect (all generations) and thread-pool contention");
            Task.WaitAll(hog);
            T.Check(Run(8, 8, 125_000, true, out _), "8 x 8 with GC pressure");
        }
        T.Report("T5 high load: generational GCs during the run, thread-pool contention");

        Console.WriteLine($"RESULT: {T.Pass} checks passed, {T.Fail} failed");
        return T.Fail == 0 ? 0 : 1;
    }
}
```

#### 5. 検証結果（期待結果・実測結果・実施状況）

実行結果は `RESULT: 43 checks passed, 0 failed`、終了コード 0、所要 6.9 秒でした。

| テストケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 容量 2 / 4 / 1024 / 65536 | 空は false（item は null）、満杯は false、FIFO、参照が同一、静止時の Count が正確、3回の再利用 | 4通りすべて一致 | 実施 |
| 不正な容量・null | 0・1・3・6・65537・131072・−2 は ArgumentOutOfRangeException、null の enqueue は ArgumentNullException | 一致 | 実施 |
| GC 後も取り出していない要素が有効 | キューだけが参照する要素が、完全な GC の後も生存し、内容も正しい | 一致 | 実施 |
| 1対1 | 100万件、重複0・欠損0 | 0.141 秒、すべて0 | 実施 |
| **4対4（各生産者25万件・計100万件）** | 各 ID はちょうど1回取り出され、欠損0・重複0・範囲外0。消費者ごとの生産者別の順序も守られ、終了時の Count は 0 | 3回とも0（0.174 / 0.188 / 0.183 秒） | 実施 |
| 8対8 | 100万件 ×2回 | 0（0.261 / 0.245 秒） | 実施 |
| 停止フック：予約の直後 | 生産者がチケット 0 を予約したまま停止しても、他の生産者は続行でき、その後リングは満杯の false を即座に返す。1000回の dequeue はすべて即座に false で、未公開の要素を返さない。再開後はチケット順 0〜3 で取り出せる | 一致 | 実施 |
| 停止フック：公開の直前 | Item を書いたが seq を公開する前に停止。同上（未公開のデータは取得されない） | 一致 | 実施 |
| 停止フック：読出しの前 | 消費者がチケット 0 を確保したまま停止。他の消費者はチケット 1 と 2 を取る。次の生産者のチケットは未解放のスロット 0 に当たるので、即座に false（上書きもブロックもしない）。再開後に要素 0 がちょうど1回返り、順序も保たれる | 一致 | 実施 |
| 停止フック：スロット再利用の前 | 読んで消去したが seq を返す前に停止。同上 | 一致 | 実施 |
| 1千万回のラップアラウンド | 容量 2 で enqueue と dequeue を 10,000,000 組（500万周）。静止時の Count が常に正確で、終了時は 0 | 一致 | 実施 |
| 取り出し済みの参照の保持の解消 | 1000個を enqueue してから dequeue し、参照を捨てて GC すると、WeakReference はすべて無効（スロットが参照を保持しない） | 1000/1000 回収 | 実施 |
| 高負荷・GC・ThreadPool の競合 | 別スレッドで世代 0〜2 の GC.Collect と確保を繰り返し、スレッドプールを占有する作業も並行させて、4対4・8対8 | 重複・欠損・順序違反 0（2.357 / 0.304 秒） | 実施 |

**自律修正：**
- 実装：カウンター用の構造体をジェネリッククラスの内部に入れ子にしていたため、ジェネリック型扱いとなり、明示的レイアウトが許されず TypeLoadException が出ました。非ジェネリックの `MpmcCounters` として外に出して修正しました。
- テスト：停止フックの試験 (c)(d) の期待値が誤っていました。チケットは連番なので、未解放のスロット 0 を飛び越えて生産者が書くことはありません。この仕様どおりの期待値に直しました。

#### 6. 技術的な正当性と設計判断

**メモリ順序（C#／.NET のメモリモデル）**
- `Volatile.Write` は release、`Volatile.Read` は acquire、`Interlocked.CompareExchange` は完全なフェンスです。
- 生産者の「Item の書込み → `Volatile.Write(seq = pos+1)`」と、消費者の「`Volatile.Read(seq)` が pos+1 を見る → Item の読取り」は release/acquire の対になり、Item の書込みは読取りより前に起こります（happens-before）。
- 消費者の「Item = null → `Volatile.Write(seq = pos+Capacity)`」と、次の周回の生産者の「`Volatile.Read(seq)` → Item の書込み」も同様に順序づけられます。
- そのため、非volatile の Item フィールドにデータ競合はありません。

**線形化点**
- 成功した TryEnqueue：チケットの CAS（FIFO の順序を決める）。要素が見えるようになるのは、seq の公開（`Volatile.Write`）の時点です。
- 成功した TryDequeue：dequeue 位置の CAS の成功。
- 各チケットを書く者・読む者は CAS で1人に決まるので、成功した enqueue はちょうど1回公開され、成功した dequeue はちょうど1つの公開済み要素を返します。

**false の意味（問題文の明示条件のとおり）**
- TryEnqueue の false は「満杯、または先頭スロットの消費者がまだ処理中で、今すぐは完了できない」ことを表します。
- TryDequeue の false は「空、または先頭のチケットが予約済みで未公開のため、今すぐは完了できない」ことを表します。
- 競合中の false を「必ず満杯」「必ず空」とは解釈しません。競合のない静止状態では、空・満杯の判定は正確です（T1 で確認）。

**進行保証**
- ループが繰り返すのは、CAS の失敗（他のスレッドの CAS が成功した）か、位置が古い（他のスレッドが進めた）場合だけです。そのため、システム全体として lock-free に進行します。
- 待機が必要な箇所（未公開・未解放のスロット）では、ループせずに即座に false を返します。どのスレッドも、他のスレッドを無限に待つことはありません。
- 予約した後・公開する前に停止したスレッドがいると、そのスロットは処理が止まりますが、他のスレッドはブロックされません（T3 で確認）。

**GC と寿命**
- スロットの Item は、取り出されるまで要素を参照し続けます（GC のルートになる）。取り出した時点で null に消去し、保持によるリークを防ぎます（WeakReference で確認）。
- ApproximateCount は enqueue と dequeue の位置の差を 0〜Capacity に丸めた値で、静止時には正確です。

#### 7. 未実施項目・制約・残課題

- 制約：long のカウンターは、生産者と消費者の操作の合計が 2^62 回未満で周回しないこと（問題文の実行契約）を前提にしています。
- 並列試験は x86-64（TSO）でだけ行いました。ARM64 などの弱いメモリ順序の CPU での実行はしていません。正しさの根拠は、上記の .NET のメモリモデルに基づく論証です。

---
## ★★FREYJA 最終提出レポート【記入欄】

- Q01〜Q10の回答済み/未完成の別：全10問に回答しました（未完成の問題はありません）。
  - 回答した問題は、Q01 FrameDecoder・Q02 SecretPooledWriter・Q03 KeyedExecutor・Q04 SingleFlightCache・Q05 Ledger・Q06 HandleSession（SafeHandle）・Q07 JsonValidator・Q08 SnapshotStore・Q09 GraphStore・Q10 BoundedMpmcQueue です。
  - 各回答欄に、7項目すべてを記入しています（根本原因、修正版コード全文、ビルドと実行のコマンド、テストコード全文、期待結果・実測結果・実施状況、技術的な正当性、未実施項目）。
- 各問題の修正版コード全文とテストコード全文が回答欄に存在するか：存在します。
  - 各回答欄の「2.」に `QNN.cs` の全文、「4.」に実際に実行した `Tests.cs` の全文を載せています。
  - どちらも、実際にビルドしたファイルを組み立てスクリプトで一字一句そのまま差し込みました。
- 使用した.NET SDK、C#バージョン、OS/CPU：
  - ランタイムは .NET 10（Microsoft.NETCore.App 10.0.15）で、`Environment.Version` で確認しました。
  - ターゲットは `net10.0`、言語は `LangVersion=14.0`（C# 14）で、C# 14 の extension メンバーがコンパイルできることも確認しました。
  - SDK は、この PC にインストールされている唯一の SDK である .NET SDK 11.0.100 です。.NET 10 SDK は無断導入の禁止に従って追加していません。
  - OS は Windows 11 Pro 10.0.26200（x64）、CPU は Intel Core i7-4710MQ（4コア8スレッド）です。
  - NuGet パッケージ・第三者ライブラリは使っていません。
- ビルド構成・ビルド成功/失敗・警告：
  - 構成は Release です。`Nullable=enable`、`AnalysisLevel=latest`、`ImplicitUsings=disable`、`AllowUnsafeBlocks=false` で、各問が独立した Console プロジェクトです。
  - 全10問でビルドに成功しました。最終版の警告は全問 0 件、エラーも 0 件です。
- 実施した検証項目と実測結果（各問別）：全問、テストの終了コードは 0 で、失敗は 0 件でした。

  | 問題 | 合格した検査数 | 主な検証内容と実測値 |
  | --- | --- | --- |
  | Q01 | 34 | 全分割の位置で Feed、ReadOnlySequence の複数セグメント、ULEB128 の境界、CRC の全ビット反転、エラーのオフセットと sticky |
  | Q02 | 33 | 予約と書込みの意味、上限 1MiB、拡張 52 回で旧バッファーの消去と返却がちょうど1回、Dispose 後の拒否 |
  | Q03 | 28 | 受付上限 128・実行上限 4、キーごとの受付順 FIFO、キャンセル、DisposeAsync、監視タイマー付き |
  | Q04 | 30 | 同時要求で factory 1回、TTL の境界（TimeProvider）、呼出し元だけのキャンセル、失敗をキャッシュしない、DisposeAsync |
  | Q05 | 24 | CAS 失敗の注入、残高の保存則、並列送金の成功 966,932 件 |
  | Q06 | 21 | Read と Dispose の競合 1000 回で Close 回数が一致（Read 95,621 件、ObjectDisposedException 7,022 件）、違反 0 |
  | Q07 | 54 | 71バイトの文書の全 2,629 通りの分割、参照検証器と比較（ランダム2万件 + 変異2万件）、境界（深さ・長さ・1MiB）、確保失敗の注入 |
  | Q08 | 52 | 7段階の障害注入（例外・強制終了の模擬）、キャンセル、同時保存 100件、実際のプロセス強制終了 20 回で旧版か新版のどちらか |
  | Q09 | 21 | 10万頂点・40万辺で TryApply 115 ms・順序 78 ms・約 10 MiB、4編集者 + 8読者で 106,000 操作、参照実装と一致 |
  | Q10 | 43 | 1対1・4対4（3回）・8対8（2回）で 100万件、欠損・重複 0、停止フック 4か所、1千万回のラップアラウンド、WeakReference による回収の確認、GC 負荷下の試験 |

  並列の試験結果は、すべて上記の OS・CPU・.NET 10.0.15・Release 構成での値です。
- 不具合・修正内容・再テスト結果：
  - **実装：**
    - Q01：sealed の InvalidDataException は継承できない（CS0509）ため、`Exception.Data` とデコーダーのプロパティでオフセットを返す方式にしました。
    - Q04：実行中の管理の後片付けを整理しました。
    - Q09：並列の確認のために、確定したスナップショットを返す `TryApply(batch, out committed)` を追加しました。
    - Q10：明示的レイアウトの構造体をジェネリック型の中に入れたことで出た TypeLoadException を、非ジェネリックの `MpmcCounters` に分けて解消しました。
  - **テスト：**
    - Q01：ビット反転の判定条件。
    - Q02：拡張回数の確認が弱かったため、強化しました。
    - Q03：開始していないジョブの解放、受付だけが保証された処理の完了を待っていたことによる停止、FIFO の比較基準（呼出し順 → 受付順）。停止したテストのプロセスは、すぐに終了させました。
    - Q04：キー b の期待値。
    - Q05・Q06：意味のない検査の修正・削除。
    - Q07：オフセットの期待値、分割の位置、期待される種別、CS8602 の警告。
    - Q08：注入しない状態で例外を投げていた障害注入クラス。
    - Q10：停止フック (c)(d) の期待値。
  - **再テスト：** 修正した問題を再ビルド・再実行し、全件合格しました。成功済みの無関係なテストは再実行していません。
- 未実施・未検証・未実装項目：
  - 未実装の要求仕様はありません。
  - **.NET 10 SDK でのビルド：** 未実施です。SDK 11.0.100 で `net10.0`・C# 14.0 に固定してビルドしました。
  - **Q08：** 実機の突然の電源断の試験と、Windows 以外の OS・ファイルシステムでの試験は未実施です。置換の原子性は OS とファイルシステムに依存します。電源断に対する耐性は主張しません。
  - **Q10 などの並列試験：** x86-64 でだけ行いました。ARM64 などの弱いメモリ順序の CPU での実行は未実施です。
  - **データ競合の検出ツール：** C# には ThreadSanitizer 相当のツールがないため使っていません。並列の正しさは、停止フック・参照モデル・大量の並列試験と、メモリモデルに基づく論証で確認しました。
  - **Q07：** GC ヒープでの確保失敗は注入していません（プールの Rent 失敗だけ）。Utf8JsonReader が先に検出する一部の不正な UTF-8 は、種別が Syntax になります。
  - その他の問題ごとの制約は、各回答欄の「7.」に記載しています。
- C#のメモリ所有権・非同期・並列保証に関する説明：
  - **所有権：**
    - ArrayPool から借りた配列は、借りた側が1回だけ返却します。返却する前に、物理長の全体を消去します（Q02）。
    - Utf8JsonReader は ref struct なので保持せず、JsonReaderState だけを引き継ぎます（Q07）。
    - 返す JsonDocument は、検証器のバッファーから独立したものです（Q07）。
    - ネイティブ相当のハンドルは SafeHandle の参照カウント（DangerousAddRef / DangerousRelease）で守り、使用中は解放しません。解放はちょうど1回です（Q06）。
  - **非同期：**
    - TaskCompletionSource は `RunContinuationsAsynchronously` で作り、完了させる側のスレッドで継続が同期的に走らないようにしています。
    - 呼出し元のキャンセルは `WaitAsync(ct)` で待機だけを打ち切り、共有の処理は止めません（Q04）。
    - 例外は継続で必ず観測し、未観測のタスク例外を残しません。
    - DisposeAsync は、新規の受付を止めてから実行中の処理を待ちます（Q03・Q04）。
  - **並列：**
    - 不変スナップショットを `Interlocked.CompareExchange`（Q05）や `Volatile.Write`（Q09）で公開し、読み手は確定した状態だけを見ます。
    - Q10 は、スロットごとのシーケンス番号の release/acquire の対（Volatile.Write / Volatile.Read）と CAS でチケットを独占する、lock-free の有界 MPMC キューです。待機が必要な場面では即座に false を返し、他のスレッドを無限に待ちません。
- 著作権・ライセンスと参考資料の確認：
  - コードとテストはすべて独自に実装しました。外部の完成コードは複製していません。使ったのは .NET の共有フレームワークの API だけです。
  - 参照した公開仕様は、RFC 8259（JSON）、RFC 3629（UTF-8）、IEEE 802.3 の CRC-32（反射多項式 0xEDB88320）、LEB128 の符号化、.NET の公式 API 仕様（SafeHandle、ArrayPool、IBufferWriter、Utf8JsonReader、TimeProvider、Volatile、Interlocked、File.Move）です。
  - 広く知られた手法（Kahn 法、シーケンス番号方式の有界 MPMC キュー〔D. Vyukov〕、一時ファイルと名前の置換による原子的な更新）は、考え方だけを参考にしました。
  - 問題文・元コード・著作権表示・README・LICENSE は変更していません。
- 作業ファイルの保存場所：
  - `C:\Users\user\AppData\Local\Temp\claude\ec29f026d20d029c\f8ca8f7c-9d91-45c8-b573-9bb9b309efc9\scratchpad\cs\` に、q01〜q10 の実装・テスト・log.txt、ビルドスクリプト、回答の原稿があります。
  - 回答を記入した試験ファイルは、同じ scratchpad の repo クローンの中にあります。
- GitHub commit・pushの実施状況（無断実施禁止）：Phantom Nightmare の指示書による、対象ファイルに限った明示的な commit・push の許可に基づいて、この試験ファイル1つだけを commit・push の対象にしています（main、force push なし、Co-Authored-By なし）。
- pushが許可・実行された場合のGitHub再取得結果、コミットSHA、対象ファイル確認：push の後に main から再取得し、全10問の回答が反映されていることを確認します。コミットの SHA と確認結果は、提出後の報告で示します。
- **自己採点しない。採点・合否判定はPhantom Nightmareが決定する。**

**指定作業の完了後は結果を報告し、追加指示があるまで別作業を開始しない。**
