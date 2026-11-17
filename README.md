# AI-Integration-Test

> **利用条件：All Rights Reserved** — Phantom Nightmareが保有する権利について、法令・GitHub利用規約で認められる場合を除き、問題文等の転載・再配布・改変・他のテストへの転用には事前の許可が必要です。所有者が指示するVELKRIS等のテスト実施・回答提出は許可します。[利用条件全文](LICENSE.md)

AIエージェントのGitHub連携・能力テスト用の公開リポジトリです。

## VELKRIS（ノーマルエージェント）能力テスト

VELKRISが取り組んだC#・C++・Pythonの能力テストです。各言語10問の課題で、不具合の原因調査・修正実装・動作検証・クライアント指示への対応能力を確認します。

## [▶ Client Order VELKRIS：C#の問題10問を開く](Agent-Test-Repository/Client-Order-VELKRIS/README.md)

既存C#能力テスト。依頼内容・全10問の問題コード・回答が掲載されています。

## [▶ Client Order VELKRIS：C++の問題10問を開く](Agent-Test-Repository/Client-Order-VELKRIS/VELKRIS-CPP-Test.md)

**新規C++17能力テスト**。クライアントオーダー、エラー・バグ対応10問、各問題の未記入回答欄があります。Q01から順に実施してください。

両テストとも元の問題コードは変更せず、指定の回答欄だけに原因・修正版コード・確認結果を記入してください。GitHubへのpushは所有者の明示指示がある場合のみ行ってください。

## [▶ Client Order VELKRIS：Pythonの問題10問を開く](Agent-Test-Repository/Client-Order-VELKRIS/VELKRIS-Python-Test.md)

**Python 3.11以上の応用能力テスト**。参照・コピー、日時処理、Decimal、SQLite、ファイル安全性、非同期リトライ等の新規10問とクライアントオーダーを収録しています。

---

## ★HERCULES エージェント能力テスト

★HERCULES（スターエージェント）の実務を想定した能力テストです。C#・C++・Pythonの3言語で、エラー・バグの原因調査、修正・実装、動作検証、指示への対応能力を評価します。**各言語10問・100点満点**です。

**[▶ ★HERCULESの能力テスト総合案内を開く](Agent-Test-Repository/Client-Order-★HERCULES/★HERCULES-Test.md)**

### [▶ ★HERCULES：C#能力テスト10問を開く](Agent-Test-Repository/Client-Order-★HERCULES/Client-Order-HERCULES.md)

**C# / .NET 8｜難易度：中級〜上級｜10問・100点満点。** 数値処理、売上集計、データ操作、JSON復旧、非同期処理、競合状態、デッドロックなどの不具合調査・修正・検証を行います。

### [▶ ★HERCULES：C++高難度テスト10問を開く](Agent-Test-Repository/Client-Order-★HERCULES/★ヘラクレスC++テスト10問.md)

**C++17（標準ライブラリ）｜難易度：上級〜最難関｜10問・100点満点。** 所有権・メモリ安全性・例外安全性・イテレーター・データ競合・スレッドプールの停止制御など、高度なデバッグ能力を評価します。

### [▶ ★HERCULES：Python確認テスト10問を開く](Agent-Test-Repository/Client-Order-★HERCULES/HERCULES-Python-Confirmation-Test.md)

**Python 3.11以上（標準ライブラリ）｜難易度：上級〜最難関｜10問・100点満点。** 入力検証、状態復元、CSV・SQLite処理、ファイル安全性、スレッド並行処理、asyncioのタイムアウト・キャンセル制御を含む問題を扱います。

各テストのリンク先にはクライアントオーダー、元コード、解答欄、検証・報告条件を掲載しています。採点・合否判定はクライアント側で行います。

---

## ★★FREYJA（フレイア）地獄級能力テスト

**[▶ ★★FREYJA・5言語すべての試験一覧と解説を開く](Agent-Test-Repository/Client-Order-%E2%98%85%E2%98%85FREYJA/README.md)**

★★FREYJAは、実装型の**地獄級プログラミング能力試験**に取り組みます。**C言語・C++20・C#14・Python 3.12・PowerShellの5言語、各10問、合計50問**です。各言語100点満点（全5試験合計500点満点）で、元コードの原因分析、完全な修正版、実行・検証結果と技術的根拠を評価します。

**共通評価項目：** 仕様理解、危険な元コードの根本原因解析、安全な修正版の自律設計・実装、メモリ・所有権・例外・並行性・障害への対処、実際のテスト結果、未検証事項の正確な報告。

**提出形式：** 各問の専用回答欄に、修正版コードと実行したテストコードの全文、実行環境、期待値・実測値、設計根拠、未実施・未解決項目を記載。全5言語とも**GitHubに提出済みの回答**を閲覧できます。

### ★★FREYJA C言語（C17）・地獄級能力試験（全10問）

**[▶ ★★FREYJA C言語テスト10問・提出済み回答を開く](Agent-Test-Repository/Client-Order-%E2%98%85%E2%98%85FREYJA/%E2%98%85%E2%98%85FREYJA-C-Hell-Level-10-Questions.md)**

**言語・環境：** C17（ISO C）／**難易度：** 地獄級／**満点：** 100点（各10点）。メモリ安全性、整数計算、ストリーミング入力、アトミック操作、失敗からの復旧までを検証します。

| 問題 | 出題内容と確認する技術 |
| --- | --- |
| Q01 | **バイナリフレーム解析：** ULEB128・CRC32・破損検出・分割受信 |
| Q02 | **固定メモリアロケータ：** 64KiBプール・アライメント・不正ポインター・再確保 |
| Q03 | **SPSCリングバッファ：** C11アトミック・1生産者/1消費者のデータ受け渡し |
| Q04 | **UTF-8デコーダ：** RFC 3629・逐次復号・過長符号化の拒否 |
| Q05 | **高精度整数演算：** 128bit整数型なしの64bit演算・オーバーフロー・偶数丸め |
| Q06 | **トランザクション保存：** POSIXの一時ファイル・fsync・rename・障害復旧 |
| Q07 | **大規模グラフ解析：** 10万頂点・40万辺・閉路検出・トポロジカル順序 |
| Q08 | **Robin Hoodハッシュテーブル：** 衝突処理・キーの所有権・削除・例外安全 |
| Q09 | **ストリーミングCSV解析：** 引用符・空欄・改行・分割受信・不正データ |
| Q10 | **Lock-Free MPMCキュー：** CAS・メモリ順序・進行保証・並行実行 |

### ★★FREYJA C++20・地獄級能力試験（全10問）

**[▶ ★★FREYJA C++20テスト10問・提出済み回答を開く](Agent-Test-Repository/Client-Order-%E2%98%85%E2%98%85FREYJA/%E2%98%85%E2%98%85FREYJA-CPP-Hell-Level-10-Questions.md)**

**言語・環境：** ISO C++20／**難易度：** 地獄級／**満点：** 100点（各10点）。所有権・RAII・例外保証・コルーチン・テンプレート・並行処理の厳密な実装力を確認します。

| 問題 | 出題内容と確認する技術 |
| --- | --- |
| Q01 | **SBOバイト列とRule of Five：** 48byteのインライン領域・コピー/ムーブ・例外保証 |
| Q02 | **move-only型消去タスク：** コピー不可の呼出し可能オブジェクト・64byte SBO |
| Q03 | **世代付きハンドル管理：** 1024スロット・失効拒否・世代管理・スレッド安全性 |
| Q04 | **C++20コルーチンGenerator：** co_yield・遅延実行・RAII・例外伝搬 |
| Q05 | **不変スナップショット：** atomic<shared_ptr>・CAS・整合性・オーバーフロー |
| Q06 | **PMRメモリリソース：** 64KiB予算・アライメント・確保失敗・並列アクセス |
| Q07 | **canonical TLVバイナリ形式：** リトルエンディアン・CRC32・形式検証・出力保持 |
| Q08 | **スレッド安全LRUキャッシュ：** 最大128件・追い出し・例外安全性 |
| Q09 | **ストリーミングJSON解析：** 分割UTF-8・Unicode・64bit整数・深さ制限 |
| Q10 | **ハザードポインター付きlock-freeスタック：** Treiber stack・ABA対策・安全なメモリ回収 |

### ★★FREYJA C#14（.NET 10）・地獄級能力試験（全10問）

**[▶ ★★FREYJA C#14テスト10問・提出済み回答を開く](Agent-Test-Repository/Client-Order-%E2%98%85%E2%98%85FREYJA/%E2%98%85%E2%98%85FREYJA-CS-Hell-Level-10-Questions.md)**

**言語・環境：** C# 14／.NET 10（net10.0）／**難易度：** 地獄級／**満点：** 100点（各10点）。非同期処理、メモリ寿命、スレッド安全性、障害耐性とデータ整合性を評価します。

| 問題 | 出題内容と確認する技術 |
| --- | --- |
| Q01 | **ReadOnlySequenceフレーム解析：** 分割入力・ULEB128・CRC32・バッファー所有権 |
| Q02 | **ArrayPoolとIBufferWriter：** 1MiB上限・Span/Memory・機密データの消去 |
| Q03 | **有界KeyedExecutor：** キー別FIFO・128件の受付・最大4並列・キャンセル |
| Q04 | **TimeProvider非同期キャッシュ：** TTL・同一キーの処理集約・キャンセル |
| Q05 | **ImmutableArray銀行台帳：** 32口座・CAS・資金保存則・並行更新 |
| Q06 | **SafeHandleとDangerousAddRef：** ハンドル所有権・Disposeとの競合・二重解放防止 |
| Q07 | **Utf8JsonReaderのJSON検証：** 分割UTF-8・Unicode・重複キー・深さと位置 |
| Q08 | **FileStreamスナップショット保存：** 一時ファイル・Flush・CRC・障害注入と復旧 |
| Q09 | **大規模DAGのトランザクション編集：** 10万頂点・40万辺・原子的な更新・閉路検出 |
| Q10 | **有界MPMCキュー：** Interlocked/Volatile・sequence番号・GC参照寿命 |

### ★★FREYJA Python 3.12・地獄級能力試験（全10問）

**[▶ ★★FREYJA Python 3.12テスト10問・提出済み回答を開く](Agent-Test-Repository/Client-Order-%E2%98%85%E2%98%85FREYJA/%E2%98%85%E2%98%85FREYJA-Python-Hell-Level-10-Questions.md)**

**言語・環境：** Python 3.12／標準ライブラリのみ／**難易度：** 地獄級／**満点：** 100点（各10点）。非同期処理、AST、SQLite、共有メモリ、外部ファイル、安全性、大規模DAGを検証します。

| 問題 | 出題内容と確認する技術 |
| --- | --- |
| Q01 | **逐次フレーム解析器：** memoryview・ULEB128・CRC32・分割入力 |
| Q02 | **asyncio有界KeyedExecutor：** キー別FIFO・128件受付・最大4並列・キャンセル |
| Q03 | **安全な式インタープリタ：** ASTホワイトリスト・危険構文拒否・256bit制限 |
| Q04 | **SQLite WAL送金台帳：** 冪等性・トランザクション・4プロセス競合 |
| Q05 | **共有メモリSPSCリング：** multiprocessing.shared_memory・同期・FIFO |
| Q06 | **デスクリプタとメタクラス：** Field/SchemaMeta・継承・型検証・原子的更新 |
| Q07 | **外部安定マージソート：** 100万行JSONL・メモリ制限・原子的な出力 |
| Q08 | **安全なZIP展開器：** ZIP Slip・symlink・ZIP bomb・ZIP64の拒否 |
| Q09 | **JSON Patch適用器：** RFC 6901・6操作・失敗時のロールバック |
| Q10 | **非同期DAG実行エンジン：** 10万ノード・40万辺・依存順序・全取消 |

### ★★FREYJA PowerShell（指定7.4）・地獄級能力試験（全10問）

**[▶ ★★FREYJA PowerShellテスト10問・提出済み回答を開く](Agent-Test-Repository/Client-Order-%E2%98%85%E2%98%85FREYJA/%E2%98%85%E2%98%85FREYJA-PowerShell-Hell-Level-10-Questions.md)**

**指定環境：** PowerShell 7.4／Windows 11／.NET 8／**難易度：** 地獄級／**満点：** 100点（各10点）。PowerShellのストリーム制御と.NET APIによる並列実行、安全なファイル・プロセス操作を評価します。

| 問題 | 出題内容と確認する技術 |
| --- | --- |
| Q01 | **厳密なパイプライン入力：** Advanced Function・PSCustomObject検証・出力ストリーム |
| Q02 | **ShouldProcessの安全な削除：** -WhatIf・確認操作・パストラバーサル対策 |
| Q03 | **ストリーミングフレーム解析：** 非seekable Stream・ULEB128・CRC32 |
| Q04 | **安全な算術パーサ：** Invoke-Expression禁止・BigInteger・構文と演算上限 |
| Q05 | **RunspacePool有界並列実行：** 最大8並列・入力順結果・EndInvoke/Dispose |
| Q06 | **厳密なJSONバッチ検証：** System.Text.Json・重複キー・Unicode・int64境界 |
| Q07 | **NTFS状態の原子的保存：** File.Replace・SHA-256・排他ロック・障害注入 |
| Q08 | **ストリーミングCSV解析：** 厳密UTF-8・RFC 4180部分集合・エラー位置 |
| Q09 | **安全な子プロセス起動：** ArgumentList・stdout/stderr回収・タイムアウト |
| Q10 | **原子的スナップショット公開：** 不変版ディレクトリ・CURRENT切替・並行Reader |

**実行結果の扱い：** C#試験では340件、Python試験では101テストメソッド、PowerShell試験では578件の成功が★★FREYJAの提出レポートに記録されています。これは**受験エージェント自身の実行報告**であり、第三者が全テストを再実行したことを意味しません。

**環境・未実施項目：** Python試験にはLinux・macOSのOS固有経路に未検証項目があります。PowerShell試験は指定7.4／.NET 8に対し**実際の検証はPowerShell 7.7／.NET 11**で行われ、指定バージョンと一部のシンボリックリンク試験は未検証と報告されています。正確な環境・検証範囲は各回答ファイルを参照してください。

**採点・権限：** 各問10点＝原因分析2点／要求仕様を満たす修正版コード6点／検証と実測2点。最終的な得点・合否は**Phantom Nightmare**が決定します。★★FREYJA以外を試験参加者として加えません。
