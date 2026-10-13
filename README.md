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

**[▶ ★HERCULESの能力テスト総合案内を開く](Agent-Test-Repository/Client-Order-HERCULES/★HERCULES-Test.md)**

### [▶ ★HERCULES：C#能力テスト10問を開く](Agent-Test-Repository/Client-Order-HERCULES/Client-Order-HERCULES.md)

**C# / .NET 8｜難易度：中級〜上級｜10問・100点満点。** 数値処理、売上集計、データ操作、JSON復旧、非同期処理、競合状態、デッドロックなどの不具合調査・修正・検証を行います。

### [▶ ★HERCULES：C++高難度テスト10問を開く](Agent-Test-Repository/Client-Order-HERCULES/★ヘラクレスC++テスト10問.md)

**C++17（標準ライブラリ）｜難易度：上級〜最難関｜10問・100点満点。** 所有権・メモリ安全性・例外安全性・イテレーター・データ競合・スレッドプールの停止制御など、高度なデバッグ能力を評価します。

### [▶ ★HERCULES：Python確認テスト10問を開く](Agent-Test-Repository/Client-Order-HERCULES/HERCULES-Python-Confirmation-Test.md)

**Python 3.11以上（標準ライブラリ）｜難易度：上級〜最難関｜10問・100点満点。** 入力検証、状態復元、CSV・SQLite処理、ファイル安全性、スレッド並行処理、asyncioのタイムアウト・キャンセル制御を含む問題を扱います。

各テストのリンク先にはクライアントオーダー、元コード、解答欄、検証・報告条件を掲載しています。採点・合否判定はクライアント側で行います。
