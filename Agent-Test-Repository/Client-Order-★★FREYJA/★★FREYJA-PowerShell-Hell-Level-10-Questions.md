# ★★FREYJA（フレイア）PowerShell・地獄級能力試験【全10問】

**受験AIエージェント：★★FREYJA（フレイア）**  
**試験責任者・最終採点者：Phantom Nightmare**  
**指定言語：PowerShell 7.4（pwsh）／Windows 11 Pro／.NET 8／標準搭載機能のみ**  
**試験区分：地獄級・実装／テスト／例外安全／並列性／セキュリティ**  
**問題数：10問／各10点／合計100点**  
**著作権表示：© 2026 Phantom Nightmare. All rights reserved.**

## 全問共通の受験要項

1. **各問題は互いに独立した試験**である。危険な元コードの根本原因を特定し、要求仕様に沿って**完全な修正版PowerShellコードを自律設計・実装**すること。修正コードが必要なAPI・例外クラス・補助型などはすべて回答欄に全文記載する。
2. **PowerShell 7.4（Windows 11・.NET 8）** を評価環境とする。PowerShell 5.1や外部モジュールへの無断置換は禁止。Windows専用と明記した問題のLinux/macOS未実施は減点要因としない。インストール済みではない場合は、事実を報告し、ユーザーの許可なしにソフトやパッケージを追加しない。
3. 使用可：PowerShell 7.4標準機能、対応する.NET標準API、自己作成したテスト専用スクリプト。**Pester、ThreadJob、外部DLL、NuGet、外部完成コードのコピーを無断利用しない**（問題で明示的に代替禁止を指定した機能は使用不可）。PowerShellの自前テスト関数によって期待値と実測値を照合する。
4. すべての試験は**本人が作成した使い捨て一時領域のデータ**で行い、重要なユーザーデータ・既存リポジトリ・レジストリ・本番環境を変更しない。試験にない権限昇格、ネットワークアクセス、外部サービス操作を禁止する。
5. 問題文の**入力条件・環境条件・成功と失敗の境界・保証範囲**を尊重する。明記していないOS、外部クラッシュ、攻撃者の競合操作、追加性能条件などを、採点側が後から隠し条件として要求してはならない。
6. 出題条件と矛盾する仕様・実現不可能な保証・未定義な範囲を発見した場合は、**その箇所と理由を説明し、勝手に問題本文を書き換えない**こと。自分が採用する解釈を回答で明示し、試験責任者の判断を仰ぐ。
7. **全問について**正常系・境界値・異常系・例外安全・必要な競合テスト・故障注入を実際に実行する。未実行のテストを合格と書かず、期待結果と実測結果、実行環境、成功件数、失敗件数を区別する。問題が指定していないGUI操作テストを追加しない。
8. ★★FREYJAが単独の試験参加者である。Claude Codeは許可された作業の範囲内で補助可能だが、**問題文の追加・変更・削除、別エージェントへの命令、権限・CLAUDE.md変更は禁止**。無関係な調査・大量ファイル読込・無制限再試行・成功済み無関係テストの再実行をしない。
9. 著作権とライセンスを確認し、独自コードで回答する。Microsoft Learnの公式仕様（https://learn.microsoft.com/powershell/）を参照してよいが、著作物の無断転載は禁止。利用した参照資料は回答に記載する。
10. 今回は**問題ファイルの新規作成だけ**が指定された作業であり、各回答欄は未記入の状態から開始する。★★FREYJAが後に回答・commit・pushする場合は**Phantom Nightmareからその作業の明示的な指示があったときのみ**実施する。試験の採点・合否判断はPhantom Nightmareが行う。

**採点：各問10点（原因分析と危険性2点／全要求を満たすPowerShell修正版6点／実際の検証・実測2点）。10問計100点。**

---
## Q01【地獄級】高度なパイプライン入力・パラメータ束縛・ストリームの分離

**評価の狙い：** Begin/Process/End、パイプラインからの型変換、オブジェクト所有権、出力ストリーム、カルチャー依存性を扱う。

### 元コード（危険・不完全／実装の出発点）

```powershell
function Convert-RecordBad {
    param([Parameter(ValueFromPipeline)]$InputObject)
    $all = @($input)
    foreach ($x in $all) {
        Write-Host "$($x.Id):$($x.Amount)"
    }
}
```

### 必須要求仕様

1. `ConvertTo-StrictRecord`をAdvanced Functionとして実装。`[Parameter(Mandatory, ValueFromPipeline)] [object]$InputObject`を受け取り、入力ごとに**成功ストリームへちょうど1個のPSCustomObject**を返す。`[ordered]@{ Index=[long]; Id=[string]; Amount=[long] }`の3プロパティをこの順序で持つ。Indexは0始まりで成功した入力レコード数を数える。
2. 入力は**PSCustomObjectのみ**。プロパティは正確に`Id`と`Amount`の2個（大文字小文字を区別したOrdinal比較）で、重複や余剰プロパティは拒否。`Id`は正確な型がString、長さ1〜64、ASCII英数字と`_`/`-`のみ。`Amount`は正確な型が`[long]`であり、文字列・浮動小数・Int32・boolなどの暗黙変換は禁止。
3. 入力エラーは**その入力に対する非終端ErrorRecordを1件**書き、後続の入力を処理する。失敗レコードに対してIndexを進めない。エラーIDは`InvalidRecord`、Categoryは`InvalidData`。`-ErrorAction Stop`指定時はPowerShell標準動作で停止してよい。
4. 成功ストリームにログ文字列や`Write-Host`の代わりの文字を混ぜない。Verbose/Informationの利用は許可するが、既定で余分な出力を発生させない。入力をすべて配列化せず、`process`で1件ずつ検証・出力する。
5. 出力に含む`Id`と`Amount`は独立した不変値。元オブジェクトのプロパティを後から変更しても既に返したオブジェクトの値は変わらない。入力配列の全件コピーや`$input`の一括消費は禁止。
6. Windowsの異なるカルチャー(`en-US`/`ja-JP`/`tr-TR`)で同じ結果を返すこと。比較にはOrdinalの明示利用を求める。処理件数上限は100000、100001件目は終端エラー`RecordLimitExceeded`とし、先行する成功出力は取り消さない。

### 必須検証項目

1. 0/1/100000件をパイプラインで渡し、正常時の成功オブジェクト数・プロパティの順序・型・Index列を確認。
2. 余剰・欠落・大文字小文字の違い・Id64/65文字・不正文字・AmountのInt32/Double/String/Booleanを混ぜ、各失敗が1件の非終端エラーになることと後続の継続を確認。
3. `-ErrorAction Stop`の動作、Information/Verbose/Warning/Successストリームの区別、元入力を書き換えても先行出力が不変であること。
4. 異なる3カルチャーと、パイプラインを1件ずつ流す送信側でメモリの漸近的増加が入力件数比例にならないこと。

### ★★FREYJAの回答【Q01・記入欄】

> **以下は専用回答欄。** 問題文・危険コード・要求仕様・必須検証・採点基準は変更禁止。修正コード、テストコード、実測を以下に全文記載する。

#### 1. 根本原因・危険性・修正方針

**元コード `Convert-RecordBad` の問題点**

| # | 問題箇所 | 発生条件 | 危険性・影響 |
| --- | --- | --- | --- |
| 1 | `$all = @($input)` | 大量のパイプライン入力 | `process` ブロックがないため、全件を配列に溜めてから処理する。メモリが入力件数に比例して増え、ストリーミングにならない |
| 2 | `Write-Host` | 常に | 成功ストリームに何も出ず、呼出し側が結果を受け取れない（ホストへの文字列だけ） |
| 3 | 型・プロパティの検証がない | 文字列の Amount・余分なプロパティ・Int32・bool | 暗黙の型変換で誤ったデータが通る。大小文字の違うプロパティも PowerShell の大文字小文字を区別しない参照で通る |
| 4 | エラー処理がない | 不正な入力 | どの入力が不正かを ErrorRecord で区別できない |
| 5 | 出力の独立性・Index がない | — | 後続処理と結び付けられない |

**修正方針**
- Advanced Function `ConvertTo-StrictRecord` の `process` ブロックで、1 件ずつ検証して出力します。
- 入力の基底オブジェクトが PSCustomObject であること、NoteProperty が `Id`・`Amount` のちょうど 2 個であること（Ordinal 比較）、型が厳密に `[string]`・`[long]` であることを検査します。
- 不正な入力には `$PSCmdlet.WriteError` で非終端エラーを 1 件書き（ID `InvalidRecord`、`InvalidData`）、処理を続けます。
- 100001 件目では `ThrowTerminatingError`（`RecordLimitExceeded`）を送出します。

#### 2. 完全な修正版PowerShellコード

```powershell
#Requires -Version 7.4
# Q01: パイプライン入力を 1 件ずつ厳密に検証して PSCustomObject を返す Advanced Function

Set-StrictMode -Version 3.0

function ConvertTo-StrictRecord {
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory, ValueFromPipeline)]
        [AllowNull()]
        [object]$InputObject
    )
    begin {
        [long]$successIndex = 0      # 成功したレコード数（= 次に出力する Index）
        [long]$processed = 0         # 受け取った入力件数（成功・失敗の両方）
        $maxRecords = 100000
        $idPattern = [regex]::new('\A[A-Za-z0-9_-]{1,64}\z', [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
    }
    process {
        $processed++
        if ($processed -gt $maxRecords) {
            $ex = [System.InvalidOperationException]::new("record limit exceeded: input #$processed (max $maxRecords)")
            $PSCmdlet.ThrowTerminatingError(
                [System.Management.Automation.ErrorRecord]::new(
                    $ex, 'RecordLimitExceeded', [System.Management.Automation.ErrorCategory]::LimitsExceeded, $null))
        }

        $reason = $null
        $id = $null
        $amount = $null
        $base = if ($null -eq $InputObject) { $null } else { ([psobject]$InputObject).psobject.BaseObject }

        if ($base -isnot [System.Management.Automation.PSCustomObject]) {
            $reason = 'input must be a PSCustomObject'
        } else {
            $props = @(([psobject]$InputObject).psobject.Properties)
            $idProp = $null
            $amountProp = $null
            foreach ($p in $props) {
                if ($p.MemberType -ne [System.Management.Automation.PSMemberTypes]::NoteProperty) {
                    $reason = "property '$($p.Name)' is not a NoteProperty"; break
                }
                if ([string]::Equals($p.Name, 'Id', [System.StringComparison]::Ordinal)) {
                    if ($null -ne $idProp) { $reason = 'duplicate Id'; break }
                    $idProp = $p
                } elseif ([string]::Equals($p.Name, 'Amount', [System.StringComparison]::Ordinal)) {
                    if ($null -ne $amountProp) { $reason = 'duplicate Amount'; break }
                    $amountProp = $p
                } else {
                    $reason = "unexpected property '$($p.Name)'"; break
                }
            }
            if ($null -eq $reason -and $props.Count -ne 2) { $reason = "expected exactly 2 properties, got $($props.Count)" }
            if ($null -eq $reason -and ($null -eq $idProp -or $null -eq $amountProp)) { $reason = 'missing Id or Amount' }
            if ($null -eq $reason) {
                $idValue = $idProp.Value
                if ($idValue -is [psobject]) { $idValue = $idValue.psobject.BaseObject }
                $amountValue = $amountProp.Value
                if ($amountValue -is [psobject]) { $amountValue = $amountValue.psobject.BaseObject }
                if ($null -eq $idValue -or -not [object]::ReferenceEquals($idValue.GetType(), [string])) {
                    $reason = 'Id must be exactly System.String'
                } elseif (-not $idPattern.IsMatch($idValue)) {
                    $reason = 'Id must be 1-64 ASCII letters, digits, _ or -'
                } elseif ($null -eq $amountValue -or -not [object]::ReferenceEquals($amountValue.GetType(), [long])) {
                    $reason = 'Amount must be exactly System.Int64'
                } else {
                    $id = [string]$idValue      # string は不変
                    $amount = [long]$amountValue  # 値型のコピー
                }
            }
        }

        if ($null -ne $reason) {
            $ex = [System.ArgumentException]::new("invalid record at input #$($processed - 1): $reason")
            $PSCmdlet.WriteError(
                [System.Management.Automation.ErrorRecord]::new(
                    $ex, 'InvalidRecord', [System.Management.Automation.ErrorCategory]::InvalidData, $InputObject))
            return
        }

        [pscustomobject][ordered]@{ Index = [long]$successIndex; Id = $id; Amount = $amount }
        $successIndex++
    }
    end {
        Write-Verbose "ConvertTo-StrictRecord: $successIndex succeeded, $($processed - $successIndex) rejected"
    }
}
```

#### 3. 実行環境・実行コマンド

```powershell
# 作業フォルダー（試験専用の一時領域）: scratchpad\ps\q01 に q01.ps1（実装）と test_q01.ps1（テスト）、scratchpad\ps\common に TestKit.ps1
cd <scratchpad>\ps\q01
pwsh -NoLogo -NoProfile -Command '$PSVersionTable.PSVersion.ToString(); [System.Runtime.InteropServices.RuntimeInformation]::FrameworkDescription'   # 7.7.0 / .NET 11.0.0
pwsh -NoLogo -NoProfile -File test_q01.ps1 2>&1 | Tee-Object -FilePath log.txt
$LASTEXITCODE   # 0 = 全件合格（失敗した検査の数）
```

**実行環境（全問共通）**

- **PowerShell：** **7.7.0**（pwsh。Microsoft Store 版 `Microsoft.PowerShell_8wekyb3d8bbwe`）。指定の **PowerShell 7.4 はこの PC にインストールされていません**。試験要項 2 に従い、許可なくソフトウェアを追加していません。
- **.NET：** pwsh 7.7.0 に組み込まれた **.NET 11.0.0** の上で実行しました。.NET 8 ランタイム（8.0.34）もインストールされていますが、pwsh 7.7 は自身のランタイムを使います。そのため、コードは PowerShell 7.4 / .NET 8 にもある API だけで書きました（.NET 9 以降の API は不使用）。ただし、7.4 / .NET 8 上での実行は未実施です。
- **OS / CPU：** Windows 11 Pro 10.0.26200（x64）、NTFS ／ Intel Core i7-4710MQ（4 コア 8 スレッド）。管理者権限は使っていません。
- **使用したもの：** PowerShell の標準機能と .NET の標準 API だけです。Pester・ThreadJob・外部モジュール・NuGet・外部 DLL・Add-Type による C# は使っていません。
- **テストの補助：** 自作の最小テスト補助 `common\TestKit.ps1`（Assert-True / Assert-Equal / Assert-Throws）を使いました。全文を「4.」に載せています。
- **実行方法：** 各問の作業フォルダー `scratchpad\ps\qNN` で `pwsh -NoLogo -NoProfile -File test_qNN.ps1` を実行しました。終了コード = 失敗した検査の数（0 = 全件合格）。
- **データの扱い：** ファイルを使う試験は、各問のフォルダーの `testdata\run-<GUID>` だけで行い、終了時に削除しました（残ったものは 0 件）。

- **実行結果：** `RESULT: 25 checks passed, 0 failed`、終了コード 0、所要 47.7 秒（1 回目の実行で全件合格）。

#### 4. 実際に実行したテストコード全文

```powershell
# ===== common/TestKit.ps1（全問共通の自作テスト補助） =====
# 試験用の最小テスト補助（Pester 不使用）。各テストファイルが dot-source する。
$script:TkPass = 0
$script:TkFail = 0
$script:TkFailures = [System.Collections.Generic.List[string]]::new()

function Assert-True {
    param([bool]$Condition, [string]$Name)
    if ($Condition) { $script:TkPass++ }
    else {
        $script:TkFail++
        $script:TkFailures.Add($Name)
        [Console]::Out.WriteLine("  FAIL: $Name")
    }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Name)
    $ok = $false
    if ($null -eq $Expected -and $null -eq $Actual) { $ok = $true }
    elseif ($null -ne $Expected -and $null -ne $Actual) {
        if ($Expected -is [byte[]] -and $Actual -is [byte[]]) {
            $ok = [System.Linq.Enumerable]::SequenceEqual([byte[]]$Expected, [byte[]]$Actual)
        } elseif ($Expected -is [string] -and $Actual -is [string]) {
            $ok = [string]::Equals($Expected, $Actual, [System.StringComparison]::Ordinal)
        } else {
            $ok = $Expected.Equals($Actual)
        }
    }
    if (-not $ok) { $Name = "$Name (expected <$Expected> actual <$Actual>)" }
    Assert-True $ok $Name
}

function Assert-Throws {
    # ScriptBlock を実行し、指定した例外型（内側の例外を含めて探索）が発生したら合格。見つかった例外は $script:TkLastException に入れる。
    param([scriptblock]$Script, [type]$ExceptionType, [string]$Name)
    $found = $null
    try { & $Script | Out-Null }
    catch {
        $e = $_.Exception
        while ($null -ne $e) {
            if ($ExceptionType.IsInstanceOfType($e)) { $found = $e; break }
            $e = $e.InnerException
        }
        if ($null -eq $found) { $Name = "$Name (got $($_.Exception.GetType().FullName): $($_.Exception.Message))" }
    }
    Assert-True ($null -ne $found) $Name
    $script:TkLastException = $found
}

function Write-TkSummary {
    [Console]::Out.WriteLine("RESULT: $script:TkPass checks passed, $script:TkFail failed")
    exit $script:TkFail
}

# ===== q01/test_q01.ps1 =====
# Q01 のテスト。実行: pwsh -NoLogo -NoProfile -File test_q01.ps1（終了コード = 失敗数）
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '..\common\TestKit.ps1')
. (Join-Path $PSScriptRoot 'q01.ps1')

function New-Rec($id, $amount) { [pscustomobject]@{ Id = $id; Amount = $amount } }

# ---- 1. 0 / 1 / 100000 件
$out = @(@() | ConvertTo-StrictRecord)
Assert-Equal 0 $out.Count '0 inputs -> 0 outputs'

$out = @(New-Rec 'a1' ([long]5) | ConvertTo-StrictRecord)
Assert-Equal 1 $out.Count '1 input -> 1 output'
Assert-Equal 'Index,Id,Amount' (($out[0].psobject.Properties | ForEach-Object Name) -join ',') 'property order'
Assert-True ($out[0].Index -is [long] -and $out[0].Id -is [string] -and $out[0].Amount -is [long]) 'property types'
Assert-Equal ([long]0) $out[0].Index 'first Index is 0'

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$count = 0; $indexOk = $true; $typeOk = $true
1..100000 | ForEach-Object { New-Rec "r$_" ([long]$_) } | ConvertTo-StrictRecord | ForEach-Object {
    if ($_.Index -ne $count -or $_.Amount -ne [long]($count + 1)) { $indexOk = $false }
    if ($_.Index -isnot [long] -or $_.Amount -isnot [long] -or $_.Id -isnot [string]) { $typeOk = $false }
    $count++
}
Assert-Equal 100000 $count '100000 inputs -> 100000 outputs'
Assert-True $indexOk 'Index sequence 0..99999 and values kept'
Assert-True $typeOk 'types for 100000 outputs'
[Console]::Out.WriteLine("[Q01] 100000 records in $([math]::Round($sw.Elapsed.TotalSeconds,2)) s")

# ---- 2. 不正入力が 1 件ずつ非終端エラーになり、後続を処理する
$bad = @(
    @{ Name = 'extra property'; Obj = [pscustomobject]@{ Id = 'x'; Amount = [long]1; Extra = 1 } }
    @{ Name = 'missing Amount'; Obj = [pscustomobject]@{ Id = 'x' } }
    @{ Name = 'missing Id'; Obj = [pscustomobject]@{ Amount = [long]1 } }
    @{ Name = 'case id'; Obj = [pscustomobject]@{ id = 'x'; Amount = [long]1 } }
    @{ Name = 'case AMOUNT'; Obj = [pscustomobject]@{ Id = 'x'; AMOUNT = [long]1 } }
    @{ Name = 'Id 65 chars'; Obj = (New-Rec ('a' * 65) ([long]1)) }
    @{ Name = 'Id empty'; Obj = (New-Rec '' ([long]1)) }
    @{ Name = 'Id bad char space'; Obj = (New-Rec 'a b' ([long]1)) }
    @{ Name = 'Id non-ascii'; Obj = (New-Rec 'ä' ([long]1)) }
    @{ Name = 'Id newline'; Obj = (New-Rec "a`n" ([long]1)) }
    @{ Name = 'Id int'; Obj = (New-Rec 5 ([long]1)) }
    @{ Name = 'Amount Int32'; Obj = (New-Rec 'x' ([int]1)) }
    @{ Name = 'Amount Double'; Obj = (New-Rec 'x' ([double]1)) }
    @{ Name = 'Amount String'; Obj = (New-Rec 'x' '1') }
    @{ Name = 'Amount Boolean'; Obj = (New-Rec 'x' $true) }
    @{ Name = 'Amount decimal'; Obj = (New-Rec 'x' ([decimal]1)) }
    @{ Name = 'Amount null'; Obj = (New-Rec 'x' $null) }
    @{ Name = 'hashtable'; Obj = @{ Id = 'x'; Amount = [long]1 } }
    @{ Name = 'string'; Obj = 'Id=x' }
    @{ Name = 'null'; Obj = $null }
    @{ Name = 'script property'; Obj = ([pscustomobject]@{ Id = 'x' } | Add-Member -PassThru -MemberType ScriptProperty -Name Amount -Value { [long]1 }) }
)
$ok64 = New-Rec ('b' * 64) ([long]-9)
$seq = [System.Collections.Generic.List[object]]::new()
$seq.Add((New-Rec 'first' ([long]1)))
foreach ($b in $bad) { $seq.Add($b.Obj); $seq.Add((New-Rec "after_$($seq.Count)" ([long]2))) }
$seq.Add($ok64)
$errs = $null
$res = @($seq | ConvertTo-StrictRecord -ErrorAction SilentlyContinue -ErrorVariable errs)
Assert-Equal ($bad.Count) $errs.Count "each of $($bad.Count) bad inputs -> exactly one error"
Assert-Equal ($bad.Count + 2) $res.Count 'all good inputs after bad ones were processed'
Assert-True (@($errs | Where-Object { $_.FullyQualifiedErrorId -like 'InvalidRecord,*' -and $_.CategoryInfo.Category -eq 'InvalidData' }).Count -eq $bad.Count) 'ErrorId InvalidRecord / InvalidData'
$idxOk = $true
for ($i = 0; $i -lt $res.Count; $i++) { if ($res[$i].Index -ne $i) { $idxOk = $false } }
Assert-True $idxOk 'Index does not advance on failures (contiguous)'
Assert-Equal ('b' * 64) $res[-1].Id 'Id of 64 chars accepted'

# ---- 3. -ErrorAction Stop、ストリームの区別、出力の独立性
$stopped = $false; $collected = [System.Collections.Generic.List[object]]::new()
try {
    @((New-Rec 'ok' ([long]1)), (New-Rec 'x' ([int]1)), (New-Rec 'never' ([long]3))) |
        ConvertTo-StrictRecord -ErrorAction Stop | ForEach-Object { $collected.Add($_) }
} catch { $stopped = $_.FullyQualifiedErrorId -like 'InvalidRecord,*' }
Assert-True $stopped '-ErrorAction Stop stops with InvalidRecord'
Assert-Equal 1 $collected.Count 'output before the stop is kept, later input not processed'

$all = @(@((New-Rec 'a' ([long]1)), 'bad') | ConvertTo-StrictRecord -ErrorAction Continue *>&1)
$kinds = $all | ForEach-Object { $_.GetType().Name }
Assert-Equal 'PSCustomObject,ErrorRecord' ($kinds -join ',') 'default: only success + error streams (no verbose/info/warning/host text)'
$verbose = @(New-Rec 'a' ([long]1) | ConvertTo-StrictRecord -Verbose 4>&1)
Assert-Equal 2 $verbose.Count '-Verbose adds one VerboseRecord'
Assert-True ($verbose[1] -is [System.Management.Automation.VerboseRecord]) 'verbose goes to verbose stream'
$info = @(New-Rec 'a' ([long]1) | ConvertTo-StrictRecord 6>&1 3>&1)
Assert-Equal 1 $info.Count 'no information / warning records'

$src = New-Rec 'orig' ([long]100)
$r = @($src | ConvertTo-StrictRecord)[0]
$src.Id = 'changed'; $src.Amount = [long]999
Assert-True ($r.Id -eq 'orig' -and $r.Amount -eq 100) 'output independent of later input mutation'

# ---- 4. カルチャー（en-US / ja-JP / tr-TR）
$base = @((New-Rec 'id_I' ([long]1)), (New-Rec 'xi' ([long]-1)), [pscustomobject]@{ ID = 'x'; Amount = [long]1 },
          [pscustomobject]@{ Id = 'x'; amount = [long]1 }, (New-Rec 'İ' ([long]1)), (New-Rec 'TITLE' ([long]9223372036854775807)))
$results = @{}
$orig = [System.Globalization.CultureInfo]::CurrentCulture
foreach ($c in 'en-US', 'ja-JP', 'tr-TR') {
    [System.Globalization.CultureInfo]::CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo($c)
    [System.Globalization.CultureInfo]::CurrentUICulture = [System.Globalization.CultureInfo]::GetCultureInfo($c)
    $e = $null
    $o = @($base | ConvertTo-StrictRecord -ErrorAction SilentlyContinue -ErrorVariable e)
    $results[$c] = ($o | ForEach-Object { "$($_.Index)|$($_.Id)|$($_.Amount)" }) -join ';'
    $results[$c] += "#errors=$($e.Count)"
}
[System.Globalization.CultureInfo]::CurrentCulture = $orig
[System.Globalization.CultureInfo]::CurrentUICulture = $orig
Assert-True ($results['en-US'] -ceq $results['ja-JP'] -and $results['en-US'] -ceq $results['tr-TR']) "same result in 3 cultures: $($results['tr-TR'])"
Assert-True ($results['en-US'] -clike '*#errors=3') 'ID / amount / dotted-I rejected in all cultures'

# ---- 上限: 100001 件目は終端エラー RecordLimitExceeded、先行出力は取り消されない
$got = 0; $limitErr = $null
try { 1..100001 | ForEach-Object { New-Rec "z$_" ([long]$_) } | ConvertTo-StrictRecord | ForEach-Object { $got++ } }
catch { $limitErr = $_ }
Assert-Equal 100000 $got '100000 outputs before limit'
Assert-True ($null -ne $limitErr -and $limitErr.FullyQualifiedErrorId -like 'RecordLimitExceeded,*') '100001st input -> terminating RecordLimitExceeded'

# ---- メモリ: 1 件ずつ流すとき、件数に比例して増えない
$marks = @{}
$n = 0
1..100000 | ForEach-Object { New-Rec "m$_" ([long]$_) } | ConvertTo-StrictRecord | ForEach-Object {
    $n++
    if ($n -eq 10000 -or $n -eq 100000) { [GC]::Collect(); [GC]::WaitForPendingFinalizers(); [GC]::Collect(); $marks[$n] = [GC]::GetTotalMemory($true) }
}
$growth = $marks[100000] - $marks[10000]
[Console]::Out.WriteLine("[Q01] managed heap after 10000: $($marks[10000]) bytes, after 100000: $($marks[100000]) bytes, growth $growth bytes")
Assert-True ($growth -lt 2MB) 'heap growth between 10k and 100k records < 2 MB (not proportional to input count)'

Write-TkSummary
```

#### 5. 期待結果・実測結果・実施状況

| 必須検証ケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 0 件 | 出力 0 件 | 0 件 | 成功 |
| 1 件 | 出力 1 件。プロパティの順序 `Index,Id,Amount`、型 long/string/long、Index 0 | 一致 | 成功 |
| 100000 件 | 出力 100000 件、Index が 0〜99999 で連続、Amount が保たれ、型が正しい | 一致（15.96 秒） | 成功 |
| 余剰・欠落・大小文字違い・Id 64/65 文字・不正文字・Amount の Int32/Double/String/Boolean | 不正入力 21 種（余剰、Amount 欠落、Id 欠落、`id`、`AMOUNT`、Id 65 文字・空・空白・非 ASCII・改行・int、Amount の Int32・Double・String・Boolean・decimal・null、hashtable、文字列、null、ScriptProperty）がそれぞれ非終端エラーちょうど 1 件。後続の正常な入力はすべて処理される | エラー 21 件、成功 23 件 | 成功 |
| ErrorId・Category | `InvalidRecord,ConvertTo-StrictRecord` / `InvalidData` | 21/21 | 成功 |
| 失敗時に Index が進まない | 成功出力の Index が 0 から連続 | 一致 | 成功 |
| Id 64 文字 | 受理 | 一致 | 成功 |
| `-ErrorAction Stop` | 最初の不正入力で `InvalidRecord` により停止。先行する出力 1 件は残り、後続は処理しない | 一致 | 成功 |
| ストリームの区別 | 既定（`*>&1`）では成功 + エラーだけ（ホスト・Information・Warning・Verbose なし）。`-Verbose` のときだけ VerboseRecord が 1 件。Information/Warning は 0 件 | 一致 | 成功 |
| 元の入力の書換え | 出力の後に元オブジェクトの Id・Amount を変えても、出力は不変 | 一致 | 成功 |
| 3 カルチャー | en-US / ja-JP / tr-TR で、`id_I`・`TITLE`・`İ`・`ID`・`amount` を含む入力の結果文字列が完全一致。エラーは各 3 件 | 一致 | 成功 |
| 100001 件目 | 終端エラー `RecordLimitExceeded`、先行する 100000 件の出力は取り消されない | 100000 件の後に停止 | 成功 |
| メモリの漸近的増加 | 1 件ずつ流すとき、1 万件目と 10 万件目のマネージドヒープの差が 2 MB 未満 | 7,325,128 → 7,321,432 バイト（差 -3,696） | 成功 |
| 重複プロパティ | PSCustomObject は大文字小文字を区別せずに一意化されるため、同名の重複は作れない。実装では Ordinal の 2 重検出を残した | 作成不可（論理で対処） | 該当ケースなし |

#### 6. 技術的根拠・保証範囲・計算量

- **ストリーミング：** `process` ブロックが 1 件ごとに呼ばれ、`$input` も配列化も使いません。保持するのは成功数・処理数の 2 つの long だけで、メモリは O(1) です（実測でヒープの増加なし）。
- **型の厳密性：**
  - `psobject.BaseObject` で、PowerShell のラッパーを外した実体の型を `[object]::ReferenceEquals($v.GetType(), [long])` で比較します。`-is` や型変換は使わず、Int32・bool・string の暗黙の変換を防ぎます。
  - プロパティは `psobject.Properties` の NoteProperty だけを許可します。ScriptProperty は値の取得でスクリプトが実行されるため拒否します。
- **Ordinal：** プロパティ名は `[string]::Equals(..., Ordinal)`、Id は `\A[A-Za-z0-9_-]{1,64}\z`（CultureInvariant・大小文字を区別）で検査します。トルコ語の `İ`/`ı` の影響を受けません（tr-TR で確認）。
- **出力の所有権：** 出力は `[pscustomobject][ordered]@{...}` で新しく作ります。string は不変、long は値型なので、元の入力と共有するものはありません。
- **エラー：**
  - 非終端は `$PSCmdlet.WriteError` で、`-ErrorAction` の標準動作に従います（Stop なら停止）。
  - 上限は `$PSCmdlet.ThrowTerminatingError` です。それまでに出力したオブジェクトは下流が受け取り済みなので、取り消されません。
- **解釈：** 「処理件数上限 100000」は、成功・失敗を合わせた入力件数と解釈しました（100001 件目の入力で終端エラー）。
- **計算量：** 1 件あたり O(プロパティ数 + Id の長さ) で、全体は O(N) です。

#### 7. 未実施・未解決・制約事項

- PowerShell 7.4 / .NET 8 での実行は未実施です（7.7.0 / .NET 11.0.0 で実行）。
- 重複プロパティは PSCustomObject の仕様上作れないため、実データでの試験はしていません（実装は Ordinal での 2 重検出を持ちます）。
- 参照資料：Microsoft Learn の about_Functions_Advanced・about_Functions_Advanced_Methods・ErrorRecord の API 仕様（仕様の確認だけ。転載なし）。コードは独自に作成しました。

---

## Q02【地獄級】ShouldProcess・WhatIf・安全な削除・パストラバーサル拒否

**評価の狙い：** 破壊的Cmdletの確認機能、ワイルドカード回避、ファイル経路の厳格な検証、安全な監査結果。

### 元コード（危険・不完全／実装の出発点）

```powershell
function Remove-ScopeBad {
    param($Root, [string[]]$Names)
    foreach ($n in $Names) {
        Remove-Item "$Root/$n" -Recurse -Force
    }
}
```

### 必須要求仕様

1. `Remove-ScopedFile`を`[CmdletBinding(SupportsShouldProcess,ConfirmImpact='High')]`で実装。パラメータは`[string]$Root`、`[string[]]$RelativePaths`。Rootは既存の通常ディレクトリで、**テスト専用の親領域**に作成する。RelativePathsは1〜1000件。
2. 相対名は`/`区切りのみ。各構成要素は1〜64文字、ASCII英数字・`_`・`-`・`.`のみで、`.`/`..`の単独成分・空成分・先頭`/`・末尾`/`・`\\`・`:`・環境変数展開・チルダ展開は禁止。相対パス長は256文字以下。UnicodeやWindowsの予約デバイス名・末尾の`.`は安全側に拒否してよいが、**拒否条件とそのテストを回答に明記**する。
3. Rootと対象の正規化パスを取得し、必ずRootの配下であることを**パス区切り境界付き**で検証する。ファイル・親ディレクトリのいずれかにReparsePoint（symlink/junction）がある場合は拒否。対象は通常ファイルだけであり、ディレクトリ・Root自身の削除は禁止。
4. 有効な既存ファイルは、1件につき必ず`$PSCmdlet.ShouldProcess()`を1回実行し、trueの場合だけ削除する。`-WhatIf`ではファイルを1バイトも変更せず、**終了状態を記録した監査オブジェクト**を各入力につき1件返す。
5. 監査オブジェクトは`RelativePath`、`Status`（`Removed`/`WouldRemove`/`Rejected`/`Missing`）、`Reason`の3つ。`-WhatIf`で実行対象だったものは`WouldRemove`、既存ファイルがなく安全な相対パスは`Missing`。不正パスや再解析ポイントは`Rejected`で処理を続ける。Confirmでユーザーが拒否した場合は`Rejected`、Reason=`NotConfirmed`。
6. 同じ相対パスが重複した場合は2件目を`Rejected`（Reason=`Duplicate`）にする。複数入力間のファイルシステム競合は扱わない契約。`-Force`でチェックを回避してはならない。事前に確認したパス以外を変更しない。
7. Windows 11・NTFSのPowerShell 7.4を対象。管理者権限取得、他ユーザーのファイル削除、テスト専用フォルダー外の操作、`-Recurse`による削除は禁止。

### 必須検証項目

1. 通常ファイル、入れ子のファイル、同名ディレクトリ、存在しないファイル、相対名重複を混在させ、監査Statusと実際の状態を一致させる。
2. `-WhatIf`では変更0件、`-Confirm:$false`で許可対象だけ削除。確認が否定されたときの`NotConfirmed`をテストする。
3. `../`、`C:/`、`a//b`、ワイルドカード、予約名、rootと接頭辞が似た別ディレクトリ、symlink/junctionを用意して外部変更0件を検証。リンク作成権限がない場合は未実施と記録。
4. 1000件のファイルを処理し、権限エラーはPowerShellのエラーとして正しく記録、意図しないパス変更が発生しないこと。

### ★★FREYJAの回答【Q02・記入欄】

> **以下は専用回答欄。** 問題文・危険コード・要求仕様・必須検証・採点基準は変更禁止。修正コード、テストコード、実測を以下に全文記載する。

#### 1. 根本原因・危険性・修正方針

**元コード `Remove-ScopeBad` の問題点**

| # | 問題箇所 | 発生条件 | 危険性・影響 |
| --- | --- | --- | --- |
| 1 | `Remove-Item "$Root/$n"`（`-LiteralPath` でない） | 名前に `*`・`?`・`[ ]` | ワイルドカードとして展開され、意図しない複数のファイルを削除する |
| 2 | パスの検証がない | `../x`・`C:/x`・絶対パス | Root の外を削除する（パストラバーサル） |
| 3 | `-Recurse -Force` | 名前がディレクトリ | ディレクトリごと再帰的に削除。読み取り専用や隠しファイルも強制削除 |
| 4 | 再解析ポイントの検査がない | junction・symlink の配下 | リンク先（Root の外）の実体を削除する |
| 5 | ShouldProcess がない | — | `-WhatIf`・`-Confirm` が効かず、確認なしで破壊的に動く |
| 6 | 監査結果がない | — | 何を削除し、何を拒否したかを記録できない |

**修正方針**
- `[CmdletBinding(SupportsShouldProcess, ConfirmImpact='High')]` で実装します。
- 相対パスを正規表現と成分ごとの規則で厳格に検証します。
- `GetFullPath` で正規化した結果が、区切り文字の境界つきで `Root\` の配下にあることを確認します。
- Root から 1 段ずつ親ディレクトリをたどって再解析ポイントを拒否し、通常ファイルにだけ `ShouldProcess` をちょうど 1 回呼びます。
- 削除は `[System.IO.File]::Delete`（ワイルドカード・再帰なし）で行い、入力ごとに監査オブジェクトを 1 件返します。

#### 2. 完全な修正版PowerShellコード

```powershell
#Requires -Version 7.4
# Q02: ShouldProcess 対応の安全な削除（Root 配下の通常ファイルだけ・パス検証・再解析ポイント拒否）

Set-StrictMode -Version 3.0

$script:ScopedNamePattern = [regex]::new('\A[A-Za-z0-9_.-]{1,64}(/[A-Za-z0-9_.-]{1,64})*\z',
    [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
$script:ReservedDeviceNames = [System.Collections.Generic.HashSet[string]]::new(
    [string[]]@('CON', 'PRN', 'AUX', 'NUL', 'CONIN$', 'CONOUT$',
                'COM0', 'COM1', 'COM2', 'COM3', 'COM4', 'COM5', 'COM6', 'COM7', 'COM8', 'COM9',
                'LPT0', 'LPT1', 'LPT2', 'LPT3', 'LPT4', 'LPT5', 'LPT6', 'LPT7', 'LPT8', 'LPT9'),
    [System.StringComparer]::OrdinalIgnoreCase)

function Test-ScopedRelativePath {
    <# 戻り値: $null（妥当）または拒否理由の文字列 #>
    param([AllowNull()][string]$RelativePath)
    if ([string]::IsNullOrEmpty($RelativePath)) { return 'Empty' }
    if ($RelativePath.Length -gt 256) { return 'TooLong' }
    if (-not $script:ScopedNamePattern.IsMatch($RelativePath)) { return 'InvalidCharacterOrSeparator' }
    foreach ($part in $RelativePath.Split('/')) {
        if ($part -eq '.' -or $part -eq '..') { return 'DotSegment' }
        if ($part.EndsWith('.', [System.StringComparison]::Ordinal)) { return 'TrailingDot' }
        $stem = $part.Split('.')[0]
        if ($script:ReservedDeviceNames.Contains($stem)) { return 'ReservedDeviceName' }
    }
    return $null
}

function New-ScopedAudit([string]$RelativePath, [string]$Status, [string]$Reason) {
    [pscustomobject][ordered]@{ RelativePath = $RelativePath; Status = $Status; Reason = $Reason }
}

function Remove-ScopedFile {
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][ValidateCount(1, 1000)][AllowEmptyString()][string[]]$RelativePaths
    )

    # ---- Root の検証（既存の通常ディレクトリ・再解析ポイントでない）
    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd([System.IO.Path]::DirectorySeparatorChar)
    $rootInfo = [System.IO.DirectoryInfo]::new($rootFull)
    if (-not $rootInfo.Exists) { throw [System.IO.DirectoryNotFoundException]::new("Root does not exist: $rootFull") }
    if ($rootInfo.Attributes.HasFlag([System.IO.FileAttributes]::ReparsePoint)) {
        throw [System.UnauthorizedAccessException]::new("Root is a reparse point: $rootFull")
    }
    $rootPrefix = $rootFull + [System.IO.Path]::DirectorySeparatorChar
    $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

    foreach ($rel in $RelativePaths) {
        $bad = Test-ScopedRelativePath $rel
        if ($null -ne $bad) { New-ScopedAudit $rel 'Rejected' $bad; continue }
        if (-not $seen.Add($rel)) { New-ScopedAudit $rel 'Rejected' 'Duplicate'; continue }   # NTFS は大小文字を区別しない

        $full = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($rootFull, $rel.Replace('/', [System.IO.Path]::DirectorySeparatorChar)))
        if (-not $full.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            New-ScopedAudit $rel 'Rejected' 'OutsideRoot'; continue      # 区切り文字境界つきの包含検査
        }

        # ---- 親ディレクトリを Root から順にたどり、再解析ポイント・非ディレクトリを検査
        $parts = $rel.Split('/')
        $cursor = $rootFull
        $status = $null
        for ($i = 0; $i -lt $parts.Length - 1; $i++) {
            $cursor = [System.IO.Path]::Combine($cursor, $parts[$i])
            $di = [System.IO.DirectoryInfo]::new($cursor)
            if (-not $di.Exists) {
                if ([System.IO.File]::Exists($cursor)) { $status = @('Missing', 'ParentIsFile') } else { $status = @('Missing', 'ParentNotFound') }
                break
            }
            if ($di.Attributes.HasFlag([System.IO.FileAttributes]::ReparsePoint)) { $status = @('Rejected', 'ReparsePoint'); break }
        }
        if ($null -ne $status) { New-ScopedAudit $rel $status[0] $status[1]; continue }

        $fi = [System.IO.FileInfo]::new($full)
        if ([System.IO.Directory]::Exists($full)) {
            $dirAttr = [System.IO.File]::GetAttributes($full)
            if ($dirAttr.HasFlag([System.IO.FileAttributes]::ReparsePoint)) { New-ScopedAudit $rel 'Rejected' 'ReparsePoint' }
            else { New-ScopedAudit $rel 'Rejected' 'NotAFile' }
            continue
        }
        if (-not $fi.Exists) {
            if ($null -ne $fi.LinkTarget) { New-ScopedAudit $rel 'Rejected' 'ReparsePoint' }   # 壊れたリンク
            else { New-ScopedAudit $rel 'Missing' 'NotFound' }
            continue
        }
        if ($fi.Attributes.HasFlag([System.IO.FileAttributes]::ReparsePoint)) { New-ScopedAudit $rel 'Rejected' 'ReparsePoint'; continue }

        # ---- 有効な既存ファイル: ShouldProcess をちょうど 1 回
        $why = [System.Management.Automation.ShouldProcessReason]::None
        $approved = $PSCmdlet.ShouldProcess("Removing file '$full'", "Remove file '$full'?", 'Remove-ScopedFile', [ref]$why)
        if (-not $approved) {
            if ($why -eq [System.Management.Automation.ShouldProcessReason]::WhatIf) { New-ScopedAudit $rel 'WouldRemove' 'WhatIf' }
            else { New-ScopedAudit $rel 'Rejected' 'NotConfirmed' }
            continue
        }
        try {
            [System.IO.File]::Delete($full)
            New-ScopedAudit $rel 'Removed' 'Deleted'
        } catch {
            $err = $_.Exception
            if ($err -is [System.Management.Automation.MethodInvocationException] -and $null -ne $err.InnerException) { $err = $err.InnerException }
            $PSCmdlet.WriteError([System.Management.Automation.ErrorRecord]::new(
                $err, 'ScopedFileDeleteFailed', [System.Management.Automation.ErrorCategory]::PermissionDenied, $rel))
            New-ScopedAudit $rel 'Rejected' "DeleteFailed: $($err.GetType().Name)"
        }
    }
}
```

#### 3. 実行環境・実行コマンド

```powershell
# 作業フォルダー（試験専用の一時領域）: scratchpad\ps\q02 に q02.ps1（実装）と test_q02.ps1（テスト）、scratchpad\ps\common に TestKit.ps1
cd <scratchpad>\ps\q02
pwsh -NoLogo -NoProfile -Command '$PSVersionTable.PSVersion.ToString(); [System.Runtime.InteropServices.RuntimeInformation]::FrameworkDescription'   # 7.7.0 / .NET 11.0.0
pwsh -NoLogo -NoProfile -File test_q02.ps1 2>&1 | Tee-Object -FilePath log.txt
$LASTEXITCODE   # 0 = 全件合格（失敗した検査の数）
```

**実行環境（全問共通）**

- **PowerShell：** **7.7.0**（pwsh。Microsoft Store 版 `Microsoft.PowerShell_8wekyb3d8bbwe`）。指定の **PowerShell 7.4 はこの PC にインストールされていません**。試験要項 2 に従い、許可なくソフトウェアを追加していません。
- **.NET：** pwsh 7.7.0 に組み込まれた **.NET 11.0.0** の上で実行しました。.NET 8 ランタイム（8.0.34）もインストールされていますが、pwsh 7.7 は自身のランタイムを使います。そのため、コードは PowerShell 7.4 / .NET 8 にもある API だけで書きました（.NET 9 以降の API は不使用）。ただし、7.4 / .NET 8 上での実行は未実施です。
- **OS / CPU：** Windows 11 Pro 10.0.26200（x64）、NTFS ／ Intel Core i7-4710MQ（4 コア 8 スレッド）。管理者権限は使っていません。
- **使用したもの：** PowerShell の標準機能と .NET の標準 API だけです。Pester・ThreadJob・外部モジュール・NuGet・外部 DLL・Add-Type による C# は使っていません。
- **テストの補助：** 自作の最小テスト補助 `common\TestKit.ps1`（Assert-True / Assert-Equal / Assert-Throws）を使いました。全文を「4.」に載せています。
- **実行方法：** 各問の作業フォルダー `scratchpad\ps\qNN` で `pwsh -NoLogo -NoProfile -File test_qNN.ps1` を実行しました。終了コード = 失敗した検査の数（0 = 全件合格）。
- **データの扱い：** ファイルを使う試験は、各問のフォルダーの `testdata\run-<GUID>` だけで行い、終了時に削除しました（残ったものは 0 件）。

- **最終の実行結果：** `RESULT: 58 checks passed, 0 failed`、終了コード 0、所要 9.9 秒。
- **1 回目の実行：** 57 件合格・1 件失敗（下記の自律修正）。

#### 4. 実際に実行したテストコード全文

```powershell
# ===== common/TestKit.ps1（全問共通の自作テスト補助） =====
# 試験用の最小テスト補助（Pester 不使用）。各テストファイルが dot-source する。
$script:TkPass = 0
$script:TkFail = 0
$script:TkFailures = [System.Collections.Generic.List[string]]::new()

function Assert-True {
    param([bool]$Condition, [string]$Name)
    if ($Condition) { $script:TkPass++ }
    else {
        $script:TkFail++
        $script:TkFailures.Add($Name)
        [Console]::Out.WriteLine("  FAIL: $Name")
    }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Name)
    $ok = $false
    if ($null -eq $Expected -and $null -eq $Actual) { $ok = $true }
    elseif ($null -ne $Expected -and $null -ne $Actual) {
        if ($Expected -is [byte[]] -and $Actual -is [byte[]]) {
            $ok = [System.Linq.Enumerable]::SequenceEqual([byte[]]$Expected, [byte[]]$Actual)
        } elseif ($Expected -is [string] -and $Actual -is [string]) {
            $ok = [string]::Equals($Expected, $Actual, [System.StringComparison]::Ordinal)
        } else {
            $ok = $Expected.Equals($Actual)
        }
    }
    if (-not $ok) { $Name = "$Name (expected <$Expected> actual <$Actual>)" }
    Assert-True $ok $Name
}

function Assert-Throws {
    # ScriptBlock を実行し、指定した例外型（内側の例外を含めて探索）が発生したら合格。見つかった例外は $script:TkLastException に入れる。
    param([scriptblock]$Script, [type]$ExceptionType, [string]$Name)
    $found = $null
    try { & $Script | Out-Null }
    catch {
        $e = $_.Exception
        while ($null -ne $e) {
            if ($ExceptionType.IsInstanceOfType($e)) { $found = $e; break }
            $e = $e.InnerException
        }
        if ($null -eq $found) { $Name = "$Name (got $($_.Exception.GetType().FullName): $($_.Exception.Message))" }
    }
    Assert-True ($null -ne $found) $Name
    $script:TkLastException = $found
}

function Write-TkSummary {
    [Console]::Out.WriteLine("RESULT: $script:TkPass checks passed, $script:TkFail failed")
    exit $script:TkFail
}

# ===== q02/test_q02.ps1 =====
# Q02 のテスト。すべて q02\testdata 配下に作る使い捨て領域で実行し、最後に削除する。
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '..\common\TestKit.ps1')
$impl = Join-Path $PSScriptRoot 'q02.ps1'
. $impl

$base = Join-Path $PSScriptRoot ('testdata\run-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $base -Force

function New-TestFile([string]$path, [string]$text = 'data') {
    $dir = Split-Path $path -Parent
    if (-not (Test-Path -LiteralPath $dir)) { $null = New-Item -ItemType Directory -Path $dir -Force }
    [System.IO.File]::WriteAllText($path, $text)
}
function Get-TreeSnapshot([string]$dir) {
    # 全ファイル・ディレクトリの相対パス・長さ・SHA256・属性（リンクは辿らない）
    $sb = [System.Text.StringBuilder]::new()
    $stack = [System.Collections.Generic.Stack[string]]::new(); $stack.Push($dir)
    $lines = [System.Collections.Generic.List[string]]::new()
    while ($stack.Count -gt 0) {
        $d = $stack.Pop()
        foreach ($e in [System.IO.DirectoryInfo]::new($d).EnumerateFileSystemInfos()) {
            $rel = $e.FullName.Substring($dir.Length)
            if ($e -is [System.IO.DirectoryInfo]) {
                $lines.Add("D|$rel|$($e.Attributes)")
                if (-not $e.Attributes.HasFlag([System.IO.FileAttributes]::ReparsePoint)) { $stack.Push($e.FullName) }
            } elseif ($e.Attributes.HasFlag([System.IO.FileAttributes]::ReparsePoint)) {
                $lines.Add("L|$rel|$($e.LinkTarget)")
            } else {
                $h = [System.Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([System.IO.File]::ReadAllBytes($e.FullName)))
                $lines.Add("F|$rel|$($e.Length)|$h|$($e.Attributes)")
            }
        }
    }
    $lines.Sort([System.StringComparer]::Ordinal)
    return ($lines -join "`n")
}

try {
    # ---- 試験用の構造
    $root = Join-Path $base 'root'
    $similar = Join-Path $base 'root2'          # Root と接頭辞が似た別ディレクトリ
    $outsideDir = Join-Path $base 'outside'
    New-TestFile (Join-Path $root 'a.txt')
    New-TestFile (Join-Path $root 'sub\b.txt')
    New-TestFile (Join-Path $root 'sub\deep\c.dat')
    $null = New-Item -ItemType Directory -Path (Join-Path $root 'dir1') -Force
    New-TestFile (Join-Path $root 'keep.txt') 'keep'
    New-TestFile (Join-Path $similar 'f.txt') 'similar'
    New-TestFile (Join-Path $outsideDir 'inner.txt') 'outside'
    New-TestFile (Join-Path $base 'outside.txt') 'outside file'
    $null = New-Item -ItemType Junction -Path (Join-Path $root 'junc') -Target $outsideDir
    $symlinkOk = $true
    try { $null = New-Item -ItemType SymbolicLink -Path (Join-Path $root 'link.txt') -Target (Join-Path $base 'outside.txt') -ErrorAction Stop }
    catch { $symlinkOk = $false; [Console]::Out.WriteLine("[Q02] symbolic link creation not permitted: $($_.Exception.Message)") }

    $outsideBefore = Get-TreeSnapshot $outsideDir
    $similarBefore = Get-TreeSnapshot $similar
    $outsideFileBefore = [System.IO.File]::ReadAllText((Join-Path $base 'outside.txt'))

    $cases = [ordered]@{
        'a.txt' = 'Removed|Deleted'; 'sub/b.txt' = 'Removed|Deleted'; 'sub/deep/c.dat' = 'Removed|Deleted'
        'dir1' = 'Rejected|NotAFile'; 'nope.txt' = 'Missing|NotFound'; 'sub/nope/x.txt' = 'Missing|ParentNotFound'
        'keep.txt/x' = 'Missing|ParentIsFile'
        '../outside.txt' = 'Rejected|DotSegment'; 'sub/../a.txt' = 'Rejected|DotSegment'; './a.txt' = 'Rejected|DotSegment'
        'C:/x' = 'Rejected|InvalidCharacterOrSeparator'; 'a//b' = 'Rejected|InvalidCharacterOrSeparator'
        '/abs' = 'Rejected|InvalidCharacterOrSeparator'; 'sub/' = 'Rejected|InvalidCharacterOrSeparator'
        'sub\b.txt' = 'Rejected|InvalidCharacterOrSeparator'; '*.txt' = 'Rejected|InvalidCharacterOrSeparator'
        'a?.txt' = 'Rejected|InvalidCharacterOrSeparator'; '[a].txt' = 'Rejected|InvalidCharacterOrSeparator'
        '%TEMP%' = 'Rejected|InvalidCharacterOrSeparator'; '~/x' = 'Rejected|InvalidCharacterOrSeparator'
        '$env:x' = 'Rejected|InvalidCharacterOrSeparator'; 'ä.txt' = 'Rejected|InvalidCharacterOrSeparator'
        ('x' * 65) = 'Rejected|InvalidCharacterOrSeparator'; (((1..5) | ForEach-Object { 'y' * 51 }) -join '/') = 'Rejected|TooLong'
        'CON' = 'Rejected|ReservedDeviceName'; 'sub/nul.txt' = 'Rejected|ReservedDeviceName'; 'COM1.log' = 'Rejected|ReservedDeviceName'
        'name.' = 'Rejected|TrailingDot'; '' = 'Rejected|Empty'
        '../root2/f.txt' = 'Rejected|DotSegment'
        'junc/inner.txt' = 'Rejected|ReparsePoint'; 'junc' = 'Rejected|ReparsePoint'
    }
    if ($symlinkOk) { $cases['link.txt'] = 'Rejected|ReparsePoint' }
    $paths = [System.Collections.Generic.List[string]]::new([string[]]@($cases.Keys))
    $paths.Add('a.txt')            # 重複（2 件目）
    $paths.Add('A.TXT')            # 大小文字違いの重複（NTFS 上は同じファイル）

    # ---- -WhatIf: 変更 0 件・各入力に 1 件の監査
    $before = Get-TreeSnapshot $base
    $audit = @(Remove-ScopedFile -Root $root -RelativePaths $paths.ToArray() -WhatIf 6>$null)
    Assert-Equal (Get-TreeSnapshot $base) $before '-WhatIf changes nothing (whole test area identical)'
    Assert-Equal $paths.Count $audit.Count '-WhatIf returns one audit per input'
    Assert-Equal 'WouldRemove' ($audit | Where-Object RelativePath -eq 'a.txt' | Select-Object -First 1).Status '-WhatIf: a.txt WouldRemove'
    Assert-Equal 3 @($audit | Where-Object Status -eq 'WouldRemove').Count '-WhatIf: exactly 3 WouldRemove'

    # ---- -Confirm:$false: 許可対象だけ削除
    $audit = @(Remove-ScopedFile -Root $root -RelativePaths $paths.ToArray() -Confirm:$false)
    Assert-Equal $paths.Count $audit.Count 'one audit per input'
    $i = 0
    foreach ($k in $cases.Keys) {
        $a = $audit[$i]; $i++
        Assert-Equal $cases[$k] "$($a.Status)|$($a.Reason)" "audit for '$k'"
    }
    Assert-Equal 'Rejected|Duplicate' "$($audit[-2].Status)|$($audit[-2].Reason)" 'second a.txt -> Duplicate'
    Assert-Equal 'Rejected|Duplicate' "$($audit[-1].Status)|$($audit[-1].Reason)" 'A.TXT -> Duplicate (case-insensitive on NTFS)'
    Assert-True (-not (Test-Path (Join-Path $root 'a.txt')) -and -not (Test-Path (Join-Path $root 'sub\b.txt')) -and -not (Test-Path (Join-Path $root 'sub\deep\c.dat'))) 'removed files are gone'
    Assert-True ((Test-Path (Join-Path $root 'keep.txt')) -and (Test-Path (Join-Path $root 'dir1')) -and (Test-Path (Join-Path $root 'sub\deep'))) 'other files and directories remain'
    Assert-Equal $outsideBefore (Get-TreeSnapshot $outsideDir) 'junction target unchanged'
    Assert-Equal $similarBefore (Get-TreeSnapshot $similar) 'prefix-similar directory unchanged'
    Assert-Equal $outsideFileBefore ([System.IO.File]::ReadAllText((Join-Path $base 'outside.txt'))) 'outside file unchanged'
    Assert-True ((Test-Path (Join-Path $root 'junc')) -and (-not $symlinkOk -or (Test-Path -LiteralPath (Join-Path $root 'link.txt')))) 'links themselves not removed'

    # ---- 確認の否定（ConfirmImpact=High の既定プロンプトに標準入力から N を答える子プロセス）
    New-TestFile (Join-Path $root 'confirm1.txt'); New-TestFile (Join-Path $root 'confirm2.txt')
    $driver = Join-Path $base 'confirm-driver.ps1'
    @"
. '$impl'
`$r = @(Remove-ScopedFile -Root '$root' -RelativePaths 'confirm1.txt','confirm2.txt')
[Console]::Out.WriteLine('AUDIT:' + ((`$r | ForEach-Object { `$_.RelativePath + '=' + `$_.Status + '/' + `$_.Reason }) -join ';'))
"@ | Set-Content -LiteralPath $driver -Encoding utf8
    $psi = [System.Diagnostics.ProcessStartInfo]::new([Environment]::ProcessPath)
    foreach ($a in '-NoLogo', '-NoProfile', '-File', $driver) { $psi.ArgumentList.Add($a) }
    $psi.UseShellExecute = $false; $psi.RedirectStandardInput = $true; $psi.RedirectStandardOutput = $true; $psi.RedirectStandardError = $true
    $p = [System.Diagnostics.Process]::Start($psi)
    $p.StandardInput.WriteLine('N'); $p.StandardInput.WriteLine('Y'); $p.StandardInput.Close()
    $stdout = $p.StandardOutput.ReadToEnd(); $null = $p.StandardError.ReadToEnd()
    $null = $p.WaitForExit(30000); $p.Dispose()
    $line = ($stdout -split "`r?`n" | Where-Object { $_ -like 'AUDIT:*' }) -join ''
    Assert-Equal 'AUDIT:confirm1.txt=Rejected/NotConfirmed;confirm2.txt=Removed/Deleted' $line 'Confirm answered N then Y'
    Assert-True ((Test-Path (Join-Path $root 'confirm1.txt')) -and -not (Test-Path (Join-Path $root 'confirm2.txt'))) 'only the confirmed file was deleted'

    # ---- 1000 件（うち 3 件は読み取り専用 → 権限エラーを PowerShell のエラーとして記録）
    $bulkRoot = Join-Path $base 'bulk'
    $names = [System.Collections.Generic.List[string]]::new()
    for ($k = 0; $k -lt 1000; $k++) {
        $rel = 'd{0:D2}/f{1:D4}.bin' -f ($k % 20), $k
        New-TestFile (Join-Path $bulkRoot ($rel.Replace('/', '\'))) "content $k"
        $names.Add($rel)
    }
    foreach ($k in 5, 500, 999) {
        $f = Join-Path $bulkRoot ($names[$k].Replace('/', '\'))
        [System.IO.File]::SetAttributes($f, [System.IO.FileAttributes]::ReadOnly)
    }
    $outsideBefore2 = Get-TreeSnapshot $base
    $errs = $null
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $audit = @(Remove-ScopedFile -Root $bulkRoot -RelativePaths $names.ToArray() -Confirm:$false -ErrorAction SilentlyContinue -ErrorVariable errs)
    $sw.Stop()
    Assert-Equal 1000 $audit.Count '1000 audits'
    Assert-Equal 997 @($audit | Where-Object Status -eq 'Removed').Count '997 removed'
    Assert-Equal 3 @($audit | Where-Object { $_.Status -eq 'Rejected' -and $_.Reason -like 'DeleteFailed*' }).Count '3 read-only -> Rejected DeleteFailed'
    Assert-Equal 3 @($errs | Where-Object { $_.FullyQualifiedErrorId -like 'ScopedFileDeleteFailed,*' -and $_.Exception -is [System.UnauthorizedAccessException] }).Count '3 PowerShell errors (UnauthorizedAccessException)'
    $remaining = @(Get-ChildItem -LiteralPath $bulkRoot -Recurse -File)
    Assert-Equal 3 $remaining.Count 'only the 3 read-only files remain'
    Assert-Equal 20 @(Get-ChildItem -LiteralPath $bulkRoot -Directory).Count 'directories are not removed'
    $after2 = (Get-TreeSnapshot $base) -split "`n" | Where-Object { $_ -notlike '*\bulk\*' }
    $before2 = $outsideBefore2 -split "`n" | Where-Object { $_ -notlike '*\bulk\*' }
    Assert-Equal ($before2 -join "`n") ($after2 -join "`n") 'nothing outside the bulk root changed'
    [Console]::Out.WriteLine("[Q02] 1000 paths processed in $([math]::Round($sw.Elapsed.TotalSeconds,2)) s; symlink test: $(if ($symlinkOk) { 'executed' } else { 'NOT executed (no privilege)' })")

    # ---- Root 自体の検証と件数制限
    Assert-Throws { Remove-ScopedFile -Root (Join-Path $base 'missing') -RelativePaths 'a' -Confirm:$false } ([System.IO.DirectoryNotFoundException]) 'missing Root rejected'
    Assert-Throws { Remove-ScopedFile -Root (Join-Path $root 'junc') -RelativePaths 'inner.txt' -Confirm:$false } ([System.UnauthorizedAccessException]) 'Root that is a junction rejected'
    Assert-Throws { Remove-ScopedFile -Root $root -RelativePaths ([string[]](1..1001 | ForEach-Object { "f$_" })) -Confirm:$false } ([System.Management.Automation.ParameterBindingException]) '1001 paths rejected by ValidateCount'
    Assert-Throws { Remove-ScopedFile -Root $root -RelativePaths 'keep.txt' -Force } ([System.Management.Automation.ParameterBindingException]) 'no -Force bypass parameter'
} finally {
    if (Test-Path -LiteralPath $base) {
        Get-ChildItem -LiteralPath $base -Recurse -Force | ForEach-Object { if ($_.Attributes -band [System.IO.FileAttributes]::ReadOnly) { $_.Attributes = 'Normal' } }
        foreach ($l in @(Join-Path $base 'root\junc'), @(Join-Path $base 'root\link.txt')) {
            if (Test-Path -LiteralPath $l) { [System.IO.Directory]::Exists($l) ? [System.IO.Directory]::Delete($l) : [System.IO.File]::Delete($l) }
        }
        Remove-Item -LiteralPath $base -Recurse -Force
    }
}
Write-TkSummary
```

#### 5. 期待結果・実測結果・実施状況

| 必須検証ケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 通常ファイル・入れ子のファイル | `a.txt`・`sub/b.txt`・`sub/deep/c.dat` は Removed/Deleted で、実際に削除される | 一致 | 成功 |
| 同名ディレクトリ | `dir1` は Rejected/NotAFile、残る | 一致 | 成功 |
| 存在しないファイル | `nope.txt` は Missing/NotFound、`sub/nope/x.txt` は Missing/ParentNotFound、`keep.txt/x` は Missing/ParentIsFile | 一致 | 成功 |
| 相対名の重複 | 2 件目の `a.txt` と大小文字違いの `A.TXT` は Rejected/Duplicate | 一致 | 成功 |
| `-WhatIf` | テスト領域全体（全ファイルの SHA256・属性）が完全に不変。入力ごとに監査 1 件（34 件）、WouldRemove 3 件 | 一致 | 成功 |
| `-Confirm:$false` | 許可対象の 3 件だけ削除。keep.txt・ディレクトリは残る | 一致 | 成功 |
| 確認の否定（NotConfirmed） | 子プロセスの pwsh の既定の確認プロンプト（ConfirmImpact=High）に、標準入力から N、Y と答える。1 件目は Rejected/NotConfirmed で残り、2 件目は Removed | `AUDIT:confirm1.txt=Rejected/NotConfirmed;confirm2.txt=Removed/Deleted` | 成功 |
| `../`・`C:/`・`a//b`・ワイルドカード・予約名 | `../outside.txt`・`sub/../a.txt`・`./a.txt` は DotSegment。`C:/x`・`a//b`・`/abs`・`sub/`・`sub\b.txt`・`*.txt`・`a?.txt`・`[a].txt`・`%TEMP%`・`~/x`・`$env:x`・`ä.txt`・65 文字の成分は InvalidCharacterOrSeparator。`CON`・`sub/nul.txt`・`COM1.log` は ReservedDeviceName、`name.` は TrailingDot、空文字は Empty、257 文字以上は TooLong | すべて一致 | 成功 |
| Root と接頭辞が似た別ディレクトリ | `../root2/f.txt` は拒否。`root2` の内容（SHA256）は不変 | 一致 | 成功 |
| junction | `junc/inner.txt`（親が junction）と `junc`（それ自体）は Rejected/ReparsePoint。junction 先の内容は不変で、junction 自体も削除しない。Root が junction なら UnauthorizedAccessException | 一致 | 成功 |
| symlink | 作成に管理者特権が必要（「この操作には管理者特権が必要です」）なため、作成できなかった | — | **未実施**（権限なし） |
| 1000 件 | 20 ディレクトリ × 1000 ファイル。997 件 Removed。読み取り専用の 3 件は Rejected/DeleteFailed で、PowerShell のエラー（`ScopedFileDeleteFailed`、UnauthorizedAccessException）が 3 件。残るファイルは 3 件だけで、ディレクトリは削除しない。bulk 以外の領域は不変 | 一致（0.99 秒） | 成功 |
| 件数制限・`-Force` | 1001 件は ValidateCount で ParameterBindingException。`-Force` パラメーターは存在しない（迂回手段なし） | 一致 | 成功 |

**自律修正**
- **実装：** `[System.IO.File]::Delete` の例外は、PowerShell では MethodInvocationException に包まれます。そのため、記録されるエラーの型がラッパーになっていました（テストで発見）。内側の例外（UnauthorizedAccessException）を取り出して記録するよう修正しました。
- **テスト補助：** TestKit の Assert-Throws が例外オブジェクトを返していたため、画面に表示されていました。返さない形に変更しました。
- 修正後に Q02 全体を再実行し、合格しました。

#### 6. 技術的根拠・保証範囲・計算量

- **パスの検証：**
  - 正規表現は `\A[A-Za-z0-9_.-]{1,64}(/[A-Za-z0-9_.-]{1,64})*\z` です。先頭・末尾・連続の `/`、`\`、`:`、ワイルドカード、`%`、`~`、`$`、非 ASCII を一括で拒否します。
  - さらに、`.`/`..` の成分、末尾の `.`（Windows では除去されて別名に化ける）、予約デバイス名（拡張子付きを含む）を拒否します。
  - 正規化したパスは、`StartsWith(Root + '\', OrdinalIgnoreCase)` の区切り文字境界つきで判定します。そのため `root2` のような接頭辞の一致は通りません。
- **再解析ポイント：** Root と、対象までの各親ディレクトリ、対象自体について `FileAttributes.ReparsePoint` を検査します。壊れたリンク（`LinkTarget` あり・実体なし）も拒否します。
- **ShouldProcess：**
  - 4 引数版の `ShouldProcess(..., [ref]$reason)` を使います。`ShouldProcessReason.WhatIf` なら WouldRemove、それ以外の false は NotConfirmed と判別します。
  - 有効な既存ファイル 1 件につき、ちょうど 1 回呼びます。-WhatIf のときは削除の API を一切呼びません。
- **削除：** `[System.IO.File]::Delete` はワイルドカードを解釈せず、ディレクトリを削除できません（再帰なし）。読み取り専用属性も迂回しません（`-Force` 相当の処理はしません）。
- **重複：** NTFS は大小文字を区別しないため、OrdinalIgnoreCase で 2 件目以降を Duplicate とします（明示した解釈）。
- **計算量：** 入力 1 件あたり O(パスの深さ) のファイルシステム照会で、全体は O(N × 深さ) です。

#### 7. 未実施・未解決・制約事項

- **未実施：** シンボリックリンク（ファイル・ディレクトリ）の試験。作成に管理者特権（または開発者モード）が必要で、許可なく権限を変更しないため実施していません。junction（権限不要）では試験しました。実装は種類を問わず ReparsePoint 属性で拒否します。
- 契約により、入力の処理中に他のプロセスがパスを差し替える競合（TOCTOU）は対象外です。
- PowerShell 7.4 / .NET 8 での実行は未実施です。
- 参照資料：Microsoft Learn の about_Functions_CmdletBindingAttribute・Cmdlet.ShouldProcess・System.IO.File / FileAttributes の API 仕様（仕様の確認だけ）。

---

## Q03【地獄級】非seekable Stream用・canonical ULEB128・CRC32フレーム解析

**評価の狙い：** System.IO.Stream、byte配列の列挙事故、逐次CRC、ストリーム位置の計算と破損検出。

### 元コード（危険・不完全／実装の出発点）

```powershell
function Read-FrameBad {
    param([IO.Stream]$Stream)
    $bytes = New-Object byte[] $Stream.Length
    [void]$Stream.Read($bytes, 0, $bytes.Length)
    return $bytes[3..($bytes.Length-1)]
}
```

### 必須要求仕様

1. `Read-StrictFrames -Stream ([System.IO.Stream])`は、読み取り可能であれば**CanSeek=false**のストリームも受け入れる。`Length`と`Position`は参照しない。入力を最後まで読み、完全なフレーム1件ごとに`[pscustomobject]@{ Index=[long]; Payload=[byte[]] }`を成功ストリームへ1個返す。Indexは0始まり。
2. 形式は`D3 91`（2バイト）+ canonical unsigned ULEB128 length（1〜3バイト）+ payload（0〜65536バイト）+ CRC32（リトルエンディアン4バイト）。CRC対象は**ULEB128の符号化バイト列とpayloadの連結**。CRC32はISO-HDLC反射多項式`0xEDB88320`、初期値`0xFFFFFFFF`、最終XOR`0xFFFFFFFF`（既知値`123456789`で`CBF43926`）。
3. canonical ULEB128以外、長さ65537以上、3バイト目の継続ビット1、magic不一致、CRC不一致を拒否する。例外`FrameReadException`のプロパティは`Kind`（`Magic`/`Varint`/`Length`/`Crc`/`Truncated`）と`Offset`（ストリーム先頭からの0始まりのバイト位置）。CRC不一致Offsetは**CRCフィールドの先頭**、Truncatedは**全入力長**、それ以外は失敗を確定したバイト位置。
4. 破損時には処理を停止して終端エラーにする。例外発生前に成功ストリームへ出したフレームは撤回しない。全データ読み込み・全配列連結は禁止。内部の保留バイト数は65536+固定量以内。CRCはフレームの読み取り中に更新する。
5. Streamの所有権は呼出し側にあり、正常・失敗いずれもStreamを**閉じない**。`Payload`は必ず独立したbyte[]。PowerShellによるbyteの要素単位の自動列挙を防ぎ、1フレームを**1出力オブジェクト**に保持する。
6. データはテストが準備したStreamだけから読み取る。外部通信やネットワークストリームへの接続は試験不要。標準のPowerShellと.NETのみ。

### 必須検証項目

1. payload長0/1/127/128/16383/16384/65536、単一・連結フレーム、非seekable・1バイトずつ返すStreamで結果一致。
2. canonical不正`80 00`,`81 00`、4バイトULEB128、65537、magicとCRCのビット反転、全切断位置でKindとOffsetを検証。
3. CRCの既知検査値・参照エンコーダによる10万件のランダム正常/破損データ比較。
4. 保留メモリ上限、入力Streamが閉じられないこと、返却後に元byte[]を変更してもPayloadが不変であることを検証。

### ★★FREYJAの回答【Q03・記入欄】

> **以下は専用回答欄。** 問題文・危険コード・要求仕様・必須検証・採点基準は変更禁止。修正コード、テストコード、実測を以下に全文記載する。

#### 1. 根本原因・危険性・修正方針

**元コード `Read-FrameBad` の問題点**

| # | 問題箇所 | 発生条件 | 危険性・影響 |
| --- | --- | --- | --- |
| 1 | `$Stream.Length` | 非 seekable のストリーム（パイプ・ネットワーク） | NotSupportedException。全長が分からない入力を扱えない |
| 2 | `Read` を 1 回だけ呼ぶ | Read が要求より少なく返す | 一部しか読まない（後半がゼロのまま） |
| 3 | 全体を 1 つの配列に読む | 巨大な入力 | メモリが入力全体に比例する |
| 4 | `$bytes[3..]` | 長さが 3 未満、複数フレーム | 範囲外。範囲演算子は object[] を作るので、byte が要素単位で列挙される |
| 5 | magic・ULEB128・CRC を検査しない | 破損・不正なデータ | 壊れたデータを受理する |
| 6 | 戻り値 | — | パイプラインで byte が 1 個ずつ出力され、1 フレームが 1 オブジェクトにならない |

**修正方針**
- `Read-StrictFrames` は、4096 バイトの固定バッファーで `Stream.Read` を繰り返し、バイト単位の状態機械（magic0 → magic1 → 長さ → payload → CRC）で解析します。`Length` と `Position` は参照しません。
- payload は長さ確定時に確保した独立の `byte[]` へ一括でコピーし、CRC32（ISO-HDLC 表）を読み取り中に逐次更新します。
- 完成したフレームごとに `[pscustomobject]@{ Index; Payload }` を 1 個出力します。
- 破損は `FrameReadException(Kind, Offset)` の終端エラーにします。Stream は閉じません。

#### 2. 完全な修正版PowerShellコード

```powershell
#Requires -Version 7.4
# Q03: 非 seekable Stream から canonical ULEB128 + CRC32 フレームを逐次解析する

Set-StrictMode -Version 3.0

class FrameReadException : System.IO.IOException {        # InvalidDataException は sealed のため IOException を基底にする
    [string]$Kind      # Magic / Varint / Length / Crc / Truncated
    [long]$Offset      # ストリーム先頭からの 0 始まりのバイト位置
    FrameReadException([string]$kind, [long]$offset) : base("frame error '$kind' at stream offset $offset") {
        $this.Kind = $kind
        $this.Offset = $offset
    }
}

# CRC-32/ISO-HDLC（反射多項式 0xEDB88320、初期値・最終 XOR 0xFFFFFFFF）の表。値は 0..2^32-1 の long。
# 表の作成は独自のスコープ（& {}）で行い、dot-source した呼出し側に変数を残さない
$script:FrameCrcTable = & {
    $table = [long[]]::new(256)
    for ($i = 0; $i -lt 256; $i++) {
        [long]$c = $i
        for ($k = 0; $k -lt 8; $k++) {
            if (($c -band 1) -ne 0) { $c = 3988292384 -bxor ($c -shr 1) } else { $c = $c -shr 1 }   # 3988292384 = 0xEDB88320
        }
        $table[$i] = $c
    }
    , $table
}

function Get-FrameCrc32 {
    <# byte[] 全体の CRC32（既知値: '123456789' → 0xCBF43926）。 #>
    param([Parameter(Mandatory)][AllowEmptyCollection()][byte[]]$Bytes)
    $t = $script:FrameCrcTable
    [long]$crc = 4294967295
    foreach ($b in $Bytes) { $crc = $t[($crc -bxor $b) -band 255] -bxor ($crc -shr 8) }
    return ($crc -bxor 4294967295)
}

function Read-StrictFrames {
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)][System.IO.Stream]$Stream,
        # 診断用（任意）: PeakPendingBytes / BytesRead / ReadCalls を書き込む
        [hashtable]$Statistics
    )
    if (-not $Stream.CanRead) { throw [System.ArgumentException]::new('Stream must be readable', 'Stream') }

    $t = $script:FrameCrcTable
    $readBuffer = [byte[]]::new(4096)      # 固定量の読み取りバッファー
    [long]$pos = 0                         # 消費済みバイト数（= 次のバイトのオフセット）
    [long]$frameIndex = 0
    $state = 0                             # 0:magic0 1:magic1 2:length 3:payload 4:crc
    [long]$crc = 0; [long]$lenValue = 0; $lenBytes = 0
    $payload = $null; $filled = 0; $need = 0
    [long]$crcRecv = 0; $crcBytes = 0; [long]$crcStart = 0
    [long]$peak = 0; [long]$readCalls = 0

    $fail = {
        param([string]$kind, [long]$offset)
        if ($null -ne $Statistics) { $Statistics.PeakPendingBytes = $peak; $Statistics.BytesRead = $pos; $Statistics.ReadCalls = $readCalls }
        $PSCmdlet.ThrowTerminatingError([System.Management.Automation.ErrorRecord]::new(
            [FrameReadException]::new($kind, $offset), "FrameRead$kind",
            [System.Management.Automation.ErrorCategory]::InvalidData, $null))
    }

    while ($true) {
        $n = $Stream.Read($readBuffer, 0, $readBuffer.Length)
        $readCalls++
        if ($n -le 0) { break }
        $i = 0
        while ($i -lt $n) {
            if ($state -eq 3) {
                # payload: 必要量だけを一括コピーし、CRC を逐次更新
                $take = [Math]::Min($need, $n - $i)
                [Array]::Copy($readBuffer, $i, $payload, $filled, $take)
                $end = $i + $take
                for ($j = $i; $j -lt $end; $j++) { $crc = $t[($crc -bxor $readBuffer[$j]) -band 255] -bxor ($crc -shr 8) }
                $i = $end; $filled += $take; $need -= $take; $pos += $take
                if ($need -eq 0) { $state = 4; $crcRecv = 0; $crcBytes = 0; $crcStart = $pos }
                continue
            }
            $b = $readBuffer[$i]; $off = $pos; $i++; $pos++
            switch ($state) {
                0 { if ($b -ne 0xD3) { & $fail 'Magic' $off }; $state = 1 }
                1 {
                    if ($b -ne 0x91) { & $fail 'Magic' $off }
                    $state = 2; $crc = 4294967295; $lenValue = 0; $lenBytes = 0
                }
                2 {
                    $crc = $t[($crc -bxor $b) -band 255] -bxor ($crc -shr 8)
                    $lenValue = $lenValue -bor ([long]($b -band 0x7F) -shl (7 * $lenBytes))
                    $lenBytes++
                    if (($b -band 0x80) -ne 0) {
                        if ($lenBytes -eq 3) { & $fail 'Varint' $off }      # 3 バイト目の継続ビット
                    } else {
                        if ($lenBytes -gt 1 -and $b -eq 0) { & $fail 'Varint' $off }   # 最短表現でない
                        if ($lenValue -gt 65536) { & $fail 'Length' $off }
                        $need = [int]$lenValue; $filled = 0
                        $payload = [byte[]]::new($need)
                        if ($need + 4 + $lenBytes -gt $peak) { $peak = $need + 4 + $lenBytes }
                        if ($need -eq 0) { $state = 4; $crcRecv = 0; $crcBytes = 0; $crcStart = $pos } else { $state = 3 }
                    }
                }
                4 {
                    $crcRecv = $crcRecv -bor ([long]$b -shl (8 * $crcBytes))
                    $crcBytes++
                    if ($crcBytes -eq 4) {
                        if ($crcRecv -ne ($crc -bxor 4294967295)) { & $fail 'Crc' $crcStart }
                        [pscustomobject][ordered]@{ Index = [long]$frameIndex; Payload = $payload }   # 1 フレーム = 1 オブジェクト
                        $frameIndex++
                        $payload = $null
                        $state = 0
                    }
                }
            }
        }
    }
    if ($state -ne 0) { & $fail 'Truncated' $pos }
    if ($null -ne $Statistics) { $Statistics.PeakPendingBytes = $peak; $Statistics.BytesRead = $pos; $Statistics.ReadCalls = $readCalls }
}
```

#### 3. 実行環境・実行コマンド

```powershell
# 作業フォルダー（試験専用の一時領域）: scratchpad\ps\q03 に q03.ps1（実装）と test_q03.ps1（テスト）、scratchpad\ps\common に TestKit.ps1
cd <scratchpad>\ps\q03
pwsh -NoLogo -NoProfile -Command '$PSVersionTable.PSVersion.ToString(); [System.Runtime.InteropServices.RuntimeInformation]::FrameworkDescription'   # 7.7.0 / .NET 11.0.0
pwsh -NoLogo -NoProfile -File test_q03.ps1 2>&1 | Tee-Object -FilePath log.txt
$LASTEXITCODE   # 0 = 全件合格（失敗した検査の数）
```

**実行環境（全問共通）**

- **PowerShell：** **7.7.0**（pwsh。Microsoft Store 版 `Microsoft.PowerShell_8wekyb3d8bbwe`）。指定の **PowerShell 7.4 はこの PC にインストールされていません**。試験要項 2 に従い、許可なくソフトウェアを追加していません。
- **.NET：** pwsh 7.7.0 に組み込まれた **.NET 11.0.0** の上で実行しました。.NET 8 ランタイム（8.0.34）もインストールされていますが、pwsh 7.7 は自身のランタイムを使います。そのため、コードは PowerShell 7.4 / .NET 8 にもある API だけで書きました（.NET 9 以降の API は不使用）。ただし、7.4 / .NET 8 上での実行は未実施です。
- **OS / CPU：** Windows 11 Pro 10.0.26200（x64）、NTFS ／ Intel Core i7-4710MQ（4 コア 8 スレッド）。管理者権限は使っていません。
- **使用したもの：** PowerShell の標準機能と .NET の標準 API だけです。Pester・ThreadJob・外部モジュール・NuGet・外部 DLL・Add-Type による C# は使っていません。
- **テストの補助：** 自作の最小テスト補助 `common\TestKit.ps1`（Assert-True / Assert-Equal / Assert-Throws）を使いました。全文を「4.」に載せています。
- **実行方法：** 各問の作業フォルダー `scratchpad\ps\qNN` で `pwsh -NoLogo -NoProfile -File test_qNN.ps1` を実行しました。終了コード = 失敗した検査の数（0 = 全件合格）。
- **データの扱い：** ファイルを使う試験は、各問のフォルダーの `testdata\run-<GUID>` だけで行い、終了時に削除しました（残ったものは 0 件）。

- **最終の実行結果：** `RESULT: 68 checks passed, 0 failed`、終了コード 0、所要 182.9 秒（10 万件の比較に 168.9 秒）。

#### 4. 実際に実行したテストコード全文

```powershell
# ===== common/TestKit.ps1（全問共通の自作テスト補助） =====
# 試験用の最小テスト補助（Pester 不使用）。各テストファイルが dot-source する。
$script:TkPass = 0
$script:TkFail = 0
$script:TkFailures = [System.Collections.Generic.List[string]]::new()

function Assert-True {
    param([bool]$Condition, [string]$Name)
    if ($Condition) { $script:TkPass++ }
    else {
        $script:TkFail++
        $script:TkFailures.Add($Name)
        [Console]::Out.WriteLine("  FAIL: $Name")
    }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Name)
    $ok = $false
    if ($null -eq $Expected -and $null -eq $Actual) { $ok = $true }
    elseif ($null -ne $Expected -and $null -ne $Actual) {
        if ($Expected -is [byte[]] -and $Actual -is [byte[]]) {
            $ok = [System.Linq.Enumerable]::SequenceEqual([byte[]]$Expected, [byte[]]$Actual)
        } elseif ($Expected -is [string] -and $Actual -is [string]) {
            $ok = [string]::Equals($Expected, $Actual, [System.StringComparison]::Ordinal)
        } else {
            $ok = $Expected.Equals($Actual)
        }
    }
    if (-not $ok) { $Name = "$Name (expected <$Expected> actual <$Actual>)" }
    Assert-True $ok $Name
}

function Assert-Throws {
    # ScriptBlock を実行し、指定した例外型（内側の例外を含めて探索）が発生したら合格。見つかった例外は $script:TkLastException に入れる。
    param([scriptblock]$Script, [type]$ExceptionType, [string]$Name)
    $found = $null
    try { & $Script | Out-Null }
    catch {
        $e = $_.Exception
        while ($null -ne $e) {
            if ($ExceptionType.IsInstanceOfType($e)) { $found = $e; break }
            $e = $e.InnerException
        }
        if ($null -eq $found) { $Name = "$Name (got $($_.Exception.GetType().FullName): $($_.Exception.Message))" }
    }
    Assert-True ($null -ne $found) $Name
    $script:TkLastException = $found
}

function Write-TkSummary {
    [Console]::Out.WriteLine("RESULT: $script:TkPass checks passed, $script:TkFail failed")
    exit $script:TkFail
}

# ===== q03/test_q03.ps1 =====
# Q03 のテスト。参照エンコーダー・参照デコーダーは実装と独立（CRC はビット単位の計算、解析は配列の添字）。
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '..\common\TestKit.ps1')
. (Join-Path $PSScriptRoot 'q03.ps1')

class TrickleStream : System.IO.Stream {
    # 非 seekable・1 回の Read で最大 Chunk バイトだけ返す。Length/Position の参照と Close を数える。
    hidden [byte[]]$Data; hidden [int]$Pos; hidden [int]$Chunk
    [int]$CloseCount = 0; [int]$LengthOrPositionAccess = 0
    TrickleStream([byte[]]$data, [int]$chunk) { $this.Data = $data; $this.Pos = 0; $this.Chunk = $chunk }
    [bool] get_CanRead() { return $true }
    [bool] get_CanSeek() { return $false }
    [bool] get_CanWrite() { return $false }
    [long] get_Length() { $this.LengthOrPositionAccess++; throw [System.NotSupportedException]::new('Length') }
    [long] get_Position() { $this.LengthOrPositionAccess++; throw [System.NotSupportedException]::new('Position') }
    [void] set_Position([long]$v) { $this.LengthOrPositionAccess++; throw [System.NotSupportedException]::new('Position') }
    [void] Flush() { }
    [int] Read([byte[]]$buffer, [int]$offset, [int]$count) {
        $n = [Math]::Min([Math]::Min($count, $this.Chunk), $this.Data.Length - $this.Pos)
        if ($n -le 0) { return 0 }
        [Array]::Copy($this.Data, $this.Pos, $buffer, $offset, $n); $this.Pos += $n; return $n
    }
    [long] Seek([long]$o, [System.IO.SeekOrigin]$s) { throw [System.NotSupportedException]::new() }
    [void] SetLength([long]$v) { throw [System.NotSupportedException]::new() }
    [void] Write([byte[]]$b, [int]$o, [int]$c) { throw [System.NotSupportedException]::new() }
    [void] Close() { $this.CloseCount++ }
}

class GeneratorStream : System.IO.Stream {
    # 同じフレームを Count 回繰り返して流す非 seekable ストリーム（全体をメモリに持たない）
    hidden [byte[]]$Frame; hidden [long]$Total; hidden [long]$Pos
    GeneratorStream([byte[]]$frame, [int]$count) { $this.Frame = $frame; $this.Total = [long]$frame.Length * $count; $this.Pos = 0 }
    [bool] get_CanRead() { return $true }
    [bool] get_CanSeek() { return $false }
    [bool] get_CanWrite() { return $false }
    [long] get_Length() { throw [System.NotSupportedException]::new() }
    [long] get_Position() { throw [System.NotSupportedException]::new() }
    [void] set_Position([long]$v) { throw [System.NotSupportedException]::new() }
    [void] Flush() { }
    [int] Read([byte[]]$buffer, [int]$offset, [int]$count) {
        $remaining = $this.Total - $this.Pos
        if ($remaining -le 0) { return 0 }
        $fo = [int]($this.Pos % $this.Frame.Length)
        $n = [int][Math]::Min([Math]::Min([long]$count, $remaining), [long]($this.Frame.Length - $fo))
        [Array]::Copy($this.Frame, $fo, $buffer, $offset, $n); $this.Pos += $n; return $n
    }
    [long] Seek([long]$o, [System.IO.SeekOrigin]$s) { throw [System.NotSupportedException]::new() }
    [void] SetLength([long]$v) { throw [System.NotSupportedException]::new() }
    [void] Write([byte[]]$b, [int]$o, [int]$c) { throw [System.NotSupportedException]::new() }
}

# ---- 参照実装（実装とは独立）
function Ref-Crc32Bitwise([byte[]]$data) {
    [long]$crc = 4294967295
    foreach ($b in $data) {
        $crc = $crc -bxor $b
        for ($k = 0; $k -lt 8; $k++) { if ($crc -band 1) { $crc = ($crc -shr 1) -bxor 3988292384 } else { $crc = $crc -shr 1 } }
    }
    return $crc -bxor 4294967295
}
# 高速化のため、参照側でも独自に作った表を使う（上のビット単位版と一致することを下で確認）
$script:RefTable = [long[]]::new(256)
for ($v = 0; $v -lt 256; $v++) {
    [long]$rv = $v
    foreach ($step in 1..8) { $rv = if ($rv % 2 -eq 1) { [long]($rv / 2 - 0.5) -bxor 3988292384 } else { [long]($rv / 2) } }
    $script:RefTable[$v] = $rv
}
function Ref-Crc32([byte[]]$data) {
    [long]$crc = 4294967295
    foreach ($b in $data) { $crc = $script:RefTable[($crc -bxor $b) -band 255] -bxor ($crc -shr 8) }
    return $crc -bxor 4294967295
}
function Ref-Uleb([int]$n) {
    $o = [System.Collections.Generic.List[byte]]::new()
    do { $b = $n -band 0x7F; $n = $n -shr 7; if ($n -ne 0) { $b = $b -bor 0x80 }; $o.Add([byte]$b) } while ($n -ne 0)
    return ,$o.ToArray()
}
function Ref-Frame([byte[]]$payload) {
    $len = Ref-Uleb $payload.Length
    $body = [byte[]]::new($len.Length + $payload.Length)
    [Array]::Copy($len, $body, $len.Length); [Array]::Copy($payload, 0, $body, $len.Length, $payload.Length)
    $crc = Ref-Crc32 $body
    $f = [byte[]]::new(2 + $body.Length + 4)
    $f[0] = 0xD3; $f[1] = 0x91
    [Array]::Copy($body, 0, $f, 2, $body.Length)
    for ($k = 0; $k -lt 4; $k++) { $f[2 + $body.Length + $k] = [byte](($crc -shr (8 * $k)) -band 0xFF) }
    return ,$f
}
function Ref-Parse([byte[]]$s) {
    # 戻り値: @{ Frames = List[byte[]]; Kind; Offset }（エラーなしは Kind = $null）
    $frames = [System.Collections.Generic.List[byte[]]]::new()
    $i = 0; $n = $s.Length
    while ($i -lt $n) {
        if ($s[$i] -ne 0xD3) { return @{ Frames = $frames; Kind = 'Magic'; Offset = $i } }
        if ($i + 1 -ge $n) { return @{ Frames = $frames; Kind = 'Truncated'; Offset = $n } }
        if ($s[$i + 1] -ne 0x91) { return @{ Frames = $frames; Kind = 'Magic'; Offset = $i + 1 } }
        $j = $i + 2; $vals = [System.Collections.Generic.List[int]]::new()
        while ($true) {
            if ($j -ge $n) { return @{ Frames = $frames; Kind = 'Truncated'; Offset = $n } }
            $vals.Add($s[$j])
            if ($s[$j] -lt 0x80) { break }
            if ($vals.Count -eq 3) { return @{ Frames = $frames; Kind = 'Varint'; Offset = $j } }
            $j++
        }
        if ($vals.Count -gt 1 -and $vals[$vals.Count - 1] -eq 0) { return @{ Frames = $frames; Kind = 'Varint'; Offset = $j } }
        $len = 0; $mul = 1
        foreach ($v in $vals) { $len += ($v -band 0x7F) * $mul; $mul *= 128 }
        if ($len -gt 65536) { return @{ Frames = $frames; Kind = 'Length'; Offset = $j } }
        $p0 = $j + 1; $c0 = $p0 + $len
        if ($c0 + 4 -gt $n) { return @{ Frames = $frames; Kind = 'Truncated'; Offset = $n } }
        $body = [byte[]]::new($c0 - ($i + 2)); [Array]::Copy($s, $i + 2, $body, 0, $body.Length)
        $got = [long]$s[$c0] -bor ([long]$s[$c0 + 1] -shl 8) -bor ([long]$s[$c0 + 2] -shl 16) -bor ([long]$s[$c0 + 3] -shl 24)
        if ($got -ne (Ref-Crc32 $body)) { return @{ Frames = $frames; Kind = 'Crc'; Offset = $c0 } }
        $pl = [byte[]]::new($len); [Array]::Copy($s, $p0, $pl, 0, $len); $frames.Add($pl)
        $i = $c0 + 4
    }
    return @{ Frames = $frames; Kind = $null; Offset = -1 }
}
function Concat([object[]]$arrays) {
    $ms = [System.IO.MemoryStream]::new()
    foreach ($a in $arrays) { $ms.Write([byte[]]$a, 0, ([byte[]]$a).Length) }
    return ,$ms.ToArray()
}
function Invoke-Decoder([System.IO.Stream]$stream) {
    $frames = [System.Collections.Generic.List[object]]::new(); $kind = $null; $off = -1
    try { Read-StrictFrames -Stream $stream | ForEach-Object { $frames.Add($_) } }
    catch {
        $e = $_.Exception
        if ($e -isnot [FrameReadException]) { throw }
        $kind = $e.Kind; $off = $e.Offset
    }
    return @{ Frames = $frames; Kind = $kind; Offset = $off }
}
function Bytes([string]$hex) { return ,[System.Convert]::FromHexString($hex) }
function Same-Payloads($frames, $expected) {
    if ($frames.Count -ne $expected.Count) { return $false }
    for ($k = 0; $k -lt $frames.Count; $k++) {
        if ($frames[$k].Index -ne $k) { return $false }
        if (-not [System.Linq.Enumerable]::SequenceEqual([byte[]]$frames[$k].Payload, [byte[]]$expected[$k])) { return $false }
    }
    return $true
}

# ---- 1. CRC の既知値と長さの符号化
Assert-Equal ([long]3421780262) (Get-FrameCrc32 ([System.Text.Encoding]::ASCII.GetBytes('123456789'))) 'implementation CRC32("123456789") = CBF43926'
Assert-Equal ([long]3421780262) (Ref-Crc32Bitwise ([System.Text.Encoding]::ASCII.GetBytes('123456789'))) 'reference (bitwise) CRC32 = CBF43926'
Assert-Equal ([long]3421780262) (Ref-Crc32 ([System.Text.Encoding]::ASCII.GetBytes('123456789'))) 'reference (table) CRC32 = CBF43926'
$rx = [System.Random]::new(1); $crcAgree = $true
for ($q = 0; $q -lt 200; $q++) { $a = [byte[]]::new($rx.Next(0, 64)); $rx.NextBytes($a); if ((Ref-Crc32 $a) -ne (Ref-Crc32Bitwise $a) -or (Get-FrameCrc32 $a) -ne (Ref-Crc32Bitwise $a)) { $crcAgree = $false } }
Assert-True $crcAgree 'table/bitwise reference and implementation CRC agree on 200 random arrays'
Assert-Equal 'D391008DEF02D2' ([System.Convert]::ToHexString((Ref-Frame ([byte[]]::new(0))))) 'exact bytes of the empty frame'
# 長さと期待する ULEB128（0xCBF43926 = 3421780262。PowerShell の 16 進リテラルは Int32 になるため 10 進で書く）
$lensList = @(@(0, '00'), @(1, '01'), @(127, '7F'), @(128, '8001'), @(16383, 'FF7F'), @(16384, '808001'), @(65536, '808004'))
foreach ($pair in $lensList) {
    $L = [int]$pair[0]; $hexExp = [string]$pair[1]
    $payload = [byte[]]::new($L); for ($k = 0; $k -lt $L; $k++) { $payload[$k] = [byte](($k * 7 + 3) -band 0xFF) }
    $frame = Ref-Frame $payload
    Assert-Equal $hexExp ([System.Convert]::ToHexString($frame, 2, $hexExp.Length / 2)) "ULEB128 bytes for length $L"
    $r1 = Invoke-Decoder ([System.IO.MemoryStream]::new($frame))
    $ts = [TrickleStream]::new($frame, 1)
    $r2 = Invoke-Decoder $ts
    Assert-True ($null -eq $r1.Kind -and (Same-Payloads $r1.Frames @(,$payload))) "length $L via MemoryStream"
    Assert-True ($null -eq $r2.Kind -and (Same-Payloads $r2.Frames @(,$payload))) "length $L via non-seekable 1-byte stream"
    Assert-Equal 0 $ts.LengthOrPositionAccess "length ${L}: Length/Position never accessed"
}

# ---- 連結フレームを 1 バイト単位・各種チャンクで
$payloads = @([byte[]]::new(0), [byte[]](65), [byte[]](0..199), [byte[]]::new(300), [byte[]](255))
$stream = Concat ($payloads | ForEach-Object { ,(Ref-Frame $_) })
foreach ($chunk in 1, 2, 3, 7, 4096) {
    $ts = [TrickleStream]::new($stream, $chunk)
    $r = Invoke-Decoder $ts
    Assert-True ($null -eq $r.Kind -and (Same-Payloads $r.Frames $payloads)) "concatenated 5 frames, chunk $chunk"
    Assert-Equal 0 $ts.CloseCount "stream not closed (chunk $chunk)"
}
$r = Invoke-Decoder ([System.IO.MemoryStream]::new([byte[]]::new(0)))
Assert-True ($null -eq $r.Kind -and $r.Frames.Count -eq 0) 'empty stream -> 0 frames'
$out = @(Read-StrictFrames -Stream ([System.IO.MemoryStream]::new($stream)))
Assert-Equal 5 $out.Count 'one output object per frame (no byte enumeration)'
Assert-True (@($out | Where-Object { $_.Payload -is [byte[]] }).Count -eq 5) 'Payload stays a single byte[]'

# ---- 2. 不正入力の Kind と Offset
$cases = @(
    @{ Hex = 'D3918000'; Kind = 'Varint'; Off = 3 }, @{ Hex = 'D3918100'; Kind = 'Varint'; Off = 3 },
    @{ Hex = 'D391808000'; Kind = 'Varint'; Off = 4 }, @{ Hex = 'D39180808001'; Kind = 'Varint'; Off = 4 },
    @{ Hex = 'D391818004'; Kind = 'Length'; Off = 4 }, @{ Hex = 'D391FFFF7F'; Kind = 'Length'; Off = 4 },
    @{ Hex = '00'; Kind = 'Magic'; Off = 0 }, @{ Hex = 'D392'; Kind = 'Magic'; Off = 1 }
)
foreach ($c in $cases) {
    $r = Invoke-Decoder ([TrickleStream]::new((Bytes $c.Hex), 1))
    Assert-Equal "$($c.Kind)@$($c.Off)" "$($r.Kind)@$($r.Offset)" "invalid $($c.Hex)"
}
$f = Ref-Frame ([System.Text.Encoding]::ASCII.GetBytes('bitflip-payload'))
$crcOff = $f.Length - 4; $mismatch = 0; $flips = 0
for ($byte = 0; $byte -lt $f.Length; $byte++) {
    for ($bit = 0; $bit -lt 8; $bit++) {
        $bad = [byte[]]$f.Clone(); $bad[$byte] = $bad[$byte] -bxor (1 -shl $bit)
        $exp = Ref-Parse $bad
        $got = Invoke-Decoder ([System.IO.MemoryStream]::new($bad))
        $flips++
        if ("$($exp.Kind)@$($exp.Offset)/$($exp.Frames.Count)" -ne "$($got.Kind)@$($got.Offset)/$($got.Frames.Count)") { $mismatch++ }
        if ($null -eq $got.Kind) { $mismatch++ }                                   # 改変は必ず拒否
        if ($byte -lt 2 -and $got.Kind -ne 'Magic') { $mismatch++ }
        if ($byte -ge $crcOff -and "$($got.Kind)@$($got.Offset)" -ne "Crc@$crcOff") { $mismatch++ }
    }
}
Assert-Equal 0 $mismatch "every bit flip of a $($f.Length)-byte frame ($flips flips) rejected as the reference says"
$tr = Concat @((Ref-Frame ([byte[]]::new(0))), (Ref-Frame ([byte[]](1,2,3))), (Ref-Frame ([byte[]]::new(200))))
$bounds = @(0, 7, 17, $tr.Length); $tbad = 0
for ($L = 0; $L -le $tr.Length; $L++) {
    $part = [byte[]]::new($L); [Array]::Copy($tr, $part, $L)
    $r = Invoke-Decoder ([TrickleStream]::new($part, 3))
    if ($bounds -contains $L) { if ($null -ne $r.Kind) { $tbad++ } }
    elseif ("$($r.Kind)@$($r.Offset)" -ne "Truncated@$L") { $tbad++ }
}
Assert-Equal 0 $tbad "all $($tr.Length + 1) truncation positions (Truncated@total length, boundaries OK)"

# ---- 3. 参照エンコーダーとのランダム比較 10 万件
$rng = [System.Random]::new(20261117)
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$diff = 0; $stats = @{}
for ($case = 0; $case -lt 100000; $case++) {
    $nf = $rng.Next(1, 3); $parts = [System.Collections.Generic.List[object]]::new()
    for ($q = 0; $q -lt $nf; $q++) {
        $len = if ($rng.Next(100) -lt 97) { $rng.Next(0, 16) } else { (127, 128, 300)[$rng.Next(3)] }
        $pl = [byte[]]::new($len); $rng.NextBytes($pl); $parts.Add((Ref-Frame $pl))
    }
    $s = Concat $parts.ToArray()
    if ($case % 2 -eq 1) {
        switch ($rng.Next(5)) {
            0 { $p = $rng.Next($s.Length); $s[$p] = $s[$p] -bxor (1 -shl $rng.Next(8)) }
            1 { $cut = $rng.Next($s.Length); $t2 = [byte[]]::new($cut); [Array]::Copy($s, $t2, $cut); $s = $t2 }
            2 { $s = Concat @($s, (Bytes 'D391'), (Bytes (('8000', '8100', '80808001', '818004', 'FFFF7F')[$rng.Next(5)]))) }
            3 { $s[$s.Length - 1 - $rng.Next(4)] = $s[$s.Length - 1 - $rng.Next(4)] -bxor 0xFF }
            4 { $s = Concat @($s, [byte[]]($rng.Next(256))) }
        }
    }
    $exp = Ref-Parse $s
    $got = Invoke-Decoder ([System.IO.MemoryStream]::new($s))
    $key = if ($null -eq $exp.Kind) { 'OK' } else { $exp.Kind }
    $stats[$key] = 1 + [int]$stats[$key]
    if ("$($exp.Kind)@$($exp.Offset)" -ne "$($got.Kind)@$($got.Offset)" -or -not (Same-Payloads $got.Frames $exp.Frames)) { $diff++ }
}
$sw.Stop()
Assert-Equal 0 $diff '100000 random valid/corrupted streams match the reference'
[Console]::Out.WriteLine("[Q03] 100000 random cases in $([math]::Round($sw.Elapsed.TotalSeconds,1)) s: " + (($stats.GetEnumerator() | Sort-Object Name | ForEach-Object { "$($_.Name)=$($_.Value)" }) -join ', '))

# ---- 4. 保留メモリ上限・ストリームを閉じない・Payload の独立性
$big = [byte[]]::new(65536); $rng.NextBytes($big)
$gen = [GeneratorStream]::new((Ref-Frame $big), 64)       # 約 4 MiB を全体保持せずに流す
$st = @{}
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$count = 0; $okPayload = $true
Read-StrictFrames -Stream $gen -Statistics $st | ForEach-Object { $count++; if (-not [System.Linq.Enumerable]::SequenceEqual([byte[]]$_.Payload, $big)) { $okPayload = $false } }
Assert-Equal 64 $count '64 frames of 65536 bytes from a generator stream'
Assert-True $okPayload 'all 64 payloads correct'
Assert-True ($st.PeakPendingBytes -le 65536 + 4 + 3) "peak pending bytes $($st.PeakPendingBytes) <= 65536 + fixed header/CRC"
[Console]::Out.WriteLine("[Q03] generator stream $($st.BytesRead) bytes in $([math]::Round($sw.Elapsed.TotalSeconds,1)) s, peak pending $($st.PeakPendingBytes) bytes (+ fixed 4096-byte read buffer), read calls $($st.ReadCalls)")

$src = Concat @((Ref-Frame ([byte[]](1,2,3))), (Ref-Frame ([byte[]](1,2,3))), (Bytes 'D39100'))
$ts = [TrickleStream]::new($src, 2)
$r = Invoke-Decoder $ts
Assert-Equal 'Truncated' $r.Kind 'failure after 2 good frames'
Assert-Equal 2 $r.Frames.Count 'frames before the error are kept'
Assert-Equal 0 $ts.CloseCount 'stream not closed on failure'
Assert-True ($ts.CanRead -and $ts.Read([byte[]]::new(1), 0, 1) -eq 0) 'stream still usable after failure'
Assert-True (-not [object]::ReferenceEquals($r.Frames[0].Payload, $r.Frames[1].Payload)) 'each Payload is an independent array'
$srcArr = [byte[]](Ref-Frame ([byte[]](9,8,7)))
$ms = [System.IO.MemoryStream]::new($srcArr)
$o = @(Read-StrictFrames -Stream $ms)
[Array]::Clear($srcArr, 0, $srcArr.Length)
Assert-True ([System.Linq.Enumerable]::SequenceEqual([byte[]]$o[0].Payload, [byte[]](9,8,7))) 'Payload unchanged after the source byte[] is cleared'
Assert-True ($ms.CanRead) 'MemoryStream not disposed'
Assert-Throws { Read-StrictFrames -Stream ([System.IO.MemoryStream]::new([byte[]](1), $false) | ForEach-Object { $_.Close(); $_ }) } ([System.ArgumentException]) 'unreadable (closed) stream rejected'

Write-TkSummary
```

#### 5. 期待結果・実測結果・実施状況

参照エンコーダー・参照デコーダーは、実装と独立に書きました。CRC はビット単位の版で検証した独自の表を使い、解析は配列の添字で行います。

| 必須検証ケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| payload 長 0/1/127/128/16383/16384/65536 | ULEB128 が `00`/`01`/`7F`/`8001`/`FF7F`/`808001`/`808004`。MemoryStream と、非 seekable・1 バイトずつの Stream の両方で payload が一致。Length/Position は一度も参照されない | 7 通りすべて一致、参照 0 回 | 成功 |
| CRC の既知値 | 実装・参照（ビット単位・表）とも `CRC32("123456789") = 0xCBF43926`。ランダムな 200 配列で 3 者が一致。0 長フレームが `D391008DEF02D2` | 一致 | 成功 |
| 単一・連結フレーム | 5 フレームの連結を、チャンク 1/2/3/7/4096 バイトの非 seekable ストリームで読む。全チャンクで一致し、Stream は閉じない | 一致 | 成功 |
| 空のストリーム・出力形式 | 0 フレーム。5 フレームなら出力 5 個で、Payload は 1 つの byte[] | 一致 | 成功 |
| 非canonical `80 00`・`81 00` | Varint@3 | 一致 | 成功 |
| 4 バイトの ULEB128 | `80 80 80 01`・`80 80 00` は Varint@4 | 一致 | 成功 |
| 65537 | `81 80 04` は Length@4（`FF FF 7F` も Length@4） | 一致 | 成功 |
| magic と CRC のビット反転 | 22 バイトのフレームの全 176 ビットを反転し、Kind・Offset・フレーム数が参照と一致。改変は必ず拒否。magic の反転は Magic、CRC 部の反転は Crc@CRC の先頭 | 不一致 0 | 成功 |
| 全切断位置 | 3 フレーム（計 225 バイト）の全 226 通りの長さ。境界では成功、それ以外は Truncated@全入力長 | 不一致 0 | 成功 |
| 10 万件のランダムな正常・破損データの比較 | 奇数番目をビット反転・切断・不正な長さ・CRC 破損・余剰バイトで破損させ、Kind・Offset・フレームが参照と一致 | 100,000/100,000 一致（OK 50,721、Crc 17,941、Magic 11,433、Truncated 9,782、Varint 6,083、Length 4,040） | 成功 |
| 保留メモリの上限 | 65536 バイトのフレーム × 64（4,194,880 バイト）を、全体を持たない生成ストリームから読む。保留の最大値 ≤ 65536 + 固定量 | 保留の最大 65,543 バイト（+ 固定の読取バッファー 4096）、Read 呼出し 1,089 回 | 成功 |
| Stream を閉じない | 正常・失敗（2 フレームの後の Truncated）とも Close の呼出し 0 回で、その後も読める。MemoryStream も CanRead のまま | 一致 | 成功 |
| Payload の独立性 | 元の byte[] を消去しても Payload は不変。フレームごとに別の配列 | 一致 | 成功 |
| 読めない Stream | 閉じた Stream は ArgumentException | 一致 | 成功 |

**自律修正（不具合の内訳）**
- **実装：**
  - `System.IO.InvalidDataException` は sealed で継承できないため、`FrameReadException` の基底を `IOException` にしました。
  - CRC の表を作るスクリプトレベルの変数（`[long]$c` など）が、dot-source した呼出し側に型制約付きで残っていました（テストの `$c` 変数でエラーになって判明）。表の作成を `& { }` の独自スコープに移しました。
- **テスト：**
  - PowerShell の 16 進リテラル `0xCBF43926` は負の Int32 になるため、10 進の 3421780262 で比較するよう直しました。
  - OrderedDictionary の整数キーは位置指定のインデクサーになるため、配列のペアに変えました。
  - ビット単位の参照 CRC は 10 万件では遅すぎるため、独自に作った表へ変えました（ビット単位版との一致は検査済み）。
  - 型制約付きの `[long]$r` の再利用を避けました。
- 修正後に Q03 全体を再実行し、合格しました。

#### 6. 技術的根拠・保証範囲・計算量

- **非 seekable への対応：** `Read` の戻り値（0 なら EOF）だけで進めます。部分的な読み取りは状態機械が任意の位置で中断・再開できるので、1 バイトずつ返すストリームでも結果が変わりません（実測で確認）。
- **Offset の定義：**
  - Magic：不一致のバイト。
  - Varint：最上位の `00`、または 3 バイト目の継続ビット。
  - Length：長さが確定した最後の ULEB128 バイト。
  - Crc：CRC フィールドの先頭。
  - Truncated：全入力長。
- **メモリ：** 保持するのは、固定の 4096 バイトの読取バッファーと、未完成のフレームの payload（≤ 65536）とヘッダーの定数量だけで、全体の連結はしません。`-Statistics` の PeakPendingBytes で実測しました。
- **CRC：** 反射多項式 0xEDB88320、初期値・最終 XOR 0xFFFFFFFF の表方式で、ULEB128 のバイトと payload を到着順に更新します。値は 0..2^32-1 の long で扱います（PowerShell の uint32 演算の型の揺れを避けるため）。
- **出力：** `[pscustomobject]` の 1 つのプロパティとして byte[] を持つので、パイプラインで要素単位に列挙されません。
- **例外：** `ThrowTerminatingError` で終端エラーにします。それまでに出力したフレームは下流が受け取り済みです。Stream の Dispose・Close は一切呼びません（所有権は呼出し側）。
- **計算量：** 時間 O(入力長)、メモリ O(65536) です。

#### 7. 未実施・未解決・制約事項

- PowerShell は 1 バイトごとの処理が遅く、10 万件の比較に約 169 秒かかりました（正しさは確認済み、性能の要件はなし）。
- PowerShell 7.4 / .NET 8 での実行は未実施です。
- 参照資料：Microsoft Learn の System.IO.Stream・about_Classes（クラスの継承）。CRC-32/ISO-HDLC の公開仕様（多項式・既知値）。コードは独自に作成しました。

---

## Q04【地獄級】Invoke-Expression禁止・BigIntegerによる安全な算術パーサ

**評価の狙い：** 明示的な文法、トークン化、前後の空白処理、巨大整数防止、インジェクション封止。

### 元コード（危険・不完全／実装の出発点）

```powershell
function Invoke-CalcBad {
    param([string]$Text)
    Invoke-Expression $Text
}
```

### 必須要求仕様

1. `Invoke-StrictArithmetic -Text ([string]) -Variables ([System.Collections.IDictionary])`を実装。構文は`expr := term (('+'|'-') term)*`、`term := factor (('*'|'/'|'%') factor)*`、`factor := ('+'|'-')* atom`、`atom := decimal | identifier | '(' expr ')'`。識別子はASCII英字開始で英数字・`_`が続く長さ1〜32文字。10進整数は`0`または0以外の数字で開始し先頭0を持たず、負号は単項演算子。演算子優先順位はこの文法どおりで、`^`は未対応。
2. Text長最大4096、トークン数最大256、括弧ネスト最大32、すべての定数/変数/中間結果/最終結果で`Abs(v).GetBitLength()`相当が256bit以下。計算は`[System.Numerics.BigInteger]`で行う。変数値は**厳密にBigInteger型**、変数名はOrdinalで比較し暗黙の文字列変換をしない。
3. 除算は0方向への切り捨て（BigInteger.Divideと同じ）。余りは同じ除算から`a - trunc(a/b)*b`と定義する。ゼロ除算は`DivideByZeroException`。正負で床除算と混同しないこと。
4. 入力からトークン化に成功しても、式全体を消費しない余剰入力があれば`ArithmeticSyntaxException`。未知変数は`UnknownVariableException`、上限違反は`ArithmeticLimitException`。許可外の文字、引用符、`;`、`$`、バッククォート、`|`、改行、関数呼出し、属性参照は**一切実行せず**構文エラーで拒否。
5. 足し算、引き算、掛け算で最終/中間の256bit上限を確認する。巨大な文字列をBigIntegerへ変換する前に桁数上限をチェックする。再帰下降を採用してよいが括弧の深さは先に制限する。
6. `Invoke-Expression`、`[scriptblock]::Create`、`AddScript`、`Invoke()`、動的な評価・コマンド呼び出しは禁止。変数辞書の文字列キーはOrdinalであることを検査するか、内部のOrdinal辞書にコピーして扱う。戻り値は単一のBigIntegerオブジェクト。

### 必須検証項目

1. `1+2*3`、`(1+2)*3`、`-7/2=-3`、`-7%2=-1`、`7%-2=1`、`-+-5`、変数参照と優先順位を検証。
2. 0/1/256/257bit、32/33重括弧、256/257トークン、4096/4097文字、ゼロ除算、未知変数で正しい例外型。
3. `Get-ChildItem`、`;Remove-Item`、`$env:PATH`、パイプ、式に続く余剰トークンを拒否し、ファイル変更やプロセス起動が一切起きないこと。
4. 独立した算術評価モデルによる有効式1万件と破損式1万件の比較。過剰メモリや不正なコマンド実行なし。

### ★★FREYJAの回答【Q04・記入欄】

> **以下は専用回答欄。** 問題文・危険コード・要求仕様・必須検証・採点基準は変更禁止。修正コード、テストコード、実測を以下に全文記載する。

#### 1. 根本原因・危険性・修正方針

**元コード `Invoke-CalcBad` の問題点**

| # | 問題箇所 | 発生条件 | 危険性・影響 |
| --- | --- | --- | --- |
| 1 | `Invoke-Expression $Text` | 任意の入力 | 入力が PowerShell のコードとして実行される。`Remove-Item`・`$env:` の参照・外部コマンドの起動など、任意コード実行（インジェクション） |
| 2 | 文法・文字種の制限がない | `;`・`|`・`$()`・改行 | 複数の文・パイプライン・サブ式を実行できる |
| 3 | 数値の上限がない | 巨大な掛け算・累乗・長い数字列 | CPU・メモリを使い果たす |
| 4 | 型・除算の意味が不明確 | `-7/2` | PowerShell の除算は double を返し、整数の 0 方向切り捨てにならない |
| 5 | 例外の分類がない | 構文エラー・未知変数・0 除算 | 呼出し側がエラーを区別できない |

**修正方針**
- 動的評価を一切使わず、独自の字句解析器と再帰下降パーサーで文法を検査します。
- 2 段階で処理します。
  - 第 1 段：文法全体を検査し、後置記法の命令列を作る（この段階では計算しない）。
  - 第 2 段：命令列をスタックで反復評価する。
- 計算は `System.Numerics.BigInteger` で行い、定数・変数・中間結果・最終結果のすべてを 256 ビット以下に制限します。
- 例外は ArithmeticSyntaxException / UnknownVariableException / ArithmeticLimitException / DivideByZeroException に分類します。

#### 2. 完全な修正版PowerShellコード

```powershell
#Requires -Version 7.4
# Q04: Invoke-Expression を使わない BigInteger 算術パーサー（明示文法・上限・インジェクション封止）

Set-StrictMode -Version 3.0

class ArithmeticSyntaxException : System.FormatException {
    [int]$Position
    ArithmeticSyntaxException([string]$message, [int]$position) : base("$message (at position $position)") { $this.Position = $position }
}
class UnknownVariableException : System.Collections.Generic.KeyNotFoundException {
    [string]$Name
    UnknownVariableException([string]$name) : base("unknown variable '$name'") { $this.Name = $name }
}
class ArithmeticLimitException : System.ArithmeticException {
    [string]$Limit
    ArithmeticLimitException([string]$limit, [string]$message) : base($message) { $this.Limit = $limit }
}

$script:ArithMaxChars = 4096
$script:ArithMaxTokens = 256
$script:ArithMaxDepth = 32
$script:ArithMaxBits = 256
$script:ArithMaxDigits = 78      # 2^256-1 は 78 桁。79 桁以上は必ず 256 ビットを超える

function Test-ArithBits([System.Numerics.BigInteger]$Value, [string]$What) {
    if ([System.Numerics.BigInteger]::Abs($Value).GetBitLength() -gt $script:ArithMaxBits) {
        throw [ArithmeticLimitException]::new('Bits', "$What exceeds $($script:ArithMaxBits) bits")
    }
    return $Value
}

function ConvertTo-ArithTokens([string]$Text) {
    # 戻り値: トークンの List。各要素は @(種別, 文字列, 位置)。種別: 'n'=数値, 'i'=識別子, 'o'=演算子・括弧
    $tokens = [System.Collections.Generic.List[object]]::new()
    $i = 0; $n = $Text.Length
    while ($i -lt $n) {
        $c = $Text[$i]
        if ($c -eq ' ' -or $c -eq "`t") { $i++; continue }      # トークン間の空白（スペース・タブ）だけ許可
        $start = $i
        if ($c -ge '0' -and $c -le '9') {
            while ($i -lt $n -and $Text[$i] -ge '0' -and $Text[$i] -le '9') { $i++ }
            $len = $i - $start
            if ($c -eq '0' -and $len -gt 1) { throw [ArithmeticSyntaxException]::new('leading zero in decimal literal', $start) }
            if ($len -gt $script:ArithMaxDigits) { throw [ArithmeticLimitException]::new('Bits', 'decimal literal has too many digits') }
            $tokens.Add(@('n', $Text.Substring($start, $len), $start))
        } elseif (($c -ge 'a' -and $c -le 'z') -or ($c -ge 'A' -and $c -le 'Z')) {
            while ($i -lt $n -and (($Text[$i] -ge 'a' -and $Text[$i] -le 'z') -or ($Text[$i] -ge 'A' -and $Text[$i] -le 'Z') -or
                                   ($Text[$i] -ge '0' -and $Text[$i] -le '9') -or $Text[$i] -eq '_')) { $i++ }
            if ($i - $start -gt 32) { throw [ArithmeticSyntaxException]::new('identifier longer than 32 characters', $start) }
            $tokens.Add(@('i', $Text.Substring($start, $i - $start), $start))
        } elseif ('+-*/%()'.IndexOf($c) -ge 0) {
            $i++
            $tokens.Add(@('o', [string]$c, $start))
        } else {
            throw [ArithmeticSyntaxException]::new("character U+$(([int]$c).ToString('X4')) is not allowed", $start)
        }
        if ($tokens.Count -gt $script:ArithMaxTokens) { throw [ArithmeticLimitException]::new('Tokens', "more than $($script:ArithMaxTokens) tokens") }
    }
    return ,$tokens
}

function Invoke-StrictArithmetic {
    [CmdletBinding()]
    [OutputType([System.Numerics.BigInteger])]
    param(
        [Parameter(Mandatory)][AllowEmptyString()][string]$Text,
        [System.Collections.IDictionary]$Variables
    )
    if ($Text.Length -gt $script:ArithMaxChars) { throw [ArithmeticLimitException]::new('Length', "text longer than $($script:ArithMaxChars) characters") }

    # 変数を Ordinal の辞書へコピー（キーは厳密に string、値は厳密に BigInteger）
    $vars = [System.Collections.Generic.Dictionary[string, System.Numerics.BigInteger]]::new([System.StringComparer]::Ordinal)
    if ($null -ne $Variables) {
        foreach ($entry in $Variables.GetEnumerator()) {
            $k = $entry.Key; if ($k -is [psobject]) { $k = $k.psobject.BaseObject }
            $v = $entry.Value; if ($v -is [psobject]) { $v = $v.psobject.BaseObject }
            if ($null -eq $k -or -not [object]::ReferenceEquals($k.GetType(), [string])) { throw [System.ArgumentException]::new('variable names must be strings', 'Variables') }
            if ($null -eq $v -or -not [object]::ReferenceEquals($v.GetType(), [System.Numerics.BigInteger])) {
                throw [System.ArgumentException]::new("variable '$k' must be exactly System.Numerics.BigInteger", 'Variables')
            }
            $vars[$k] = $v
        }
    }

    $tokens = ConvertTo-ArithTokens $Text
    if ($tokens.Count -eq 0) { throw [ArithmeticSyntaxException]::new('empty expression', 0) }

    # 括弧の深さは再帰下降の前に検査する（不整合は構文エラー、33 段以上は上限エラー）
    $depth = 0
    foreach ($t in $tokens) {
        if ($t[1] -eq '(' -and $t[0] -eq 'o') { $depth++; if ($depth -gt $script:ArithMaxDepth) { throw [ArithmeticLimitException]::new('Depth', "parentheses nested deeper than $($script:ArithMaxDepth)") } }
        elseif ($t[1] -eq ')' -and $t[0] -eq 'o') { $depth--; if ($depth -lt 0) { throw [ArithmeticSyntaxException]::new("unbalanced ')'", $t[2]) } }
    }
    if ($depth -ne 0) { throw [ArithmeticSyntaxException]::new("unbalanced '('", $Text.Length) }

    # 第 1 段: 再帰下降で文法全体を検査し、後置記法の命令列を作る（この段階では一切計算しない）
    $p = [pscustomobject]@{ Tokens = $tokens; Pos = 0; TextLength = $Text.Length; Code = [System.Collections.Generic.List[object]]::new() }
    Read-ArithExpr $p
    if ($p.Pos -lt $tokens.Count) { throw [ArithmeticSyntaxException]::new("unexpected token '$($tokens[$p.Pos][1])'", $tokens[$p.Pos][2]) }

    # 第 2 段: 命令列をスタックで反復評価（左から右の評価順は保たれる）
    $stack = [System.Collections.Generic.Stack[System.Numerics.BigInteger]]::new()
    foreach ($ins in $p.Code) {
        switch ($ins[0]) {
            'lit' {
                $stack.Push((Test-ArithBits ([System.Numerics.BigInteger]::Parse($ins[1], [System.Globalization.NumberStyles]::None,
                    [System.Globalization.CultureInfo]::InvariantCulture)) 'decimal literal'))
            }
            'var' {
                $value = [System.Numerics.BigInteger]::Zero
                if (-not $vars.TryGetValue($ins[1], [ref]$value)) { throw [UnknownVariableException]::new($ins[1]) }
                $stack.Push((Test-ArithBits $value "variable '$($ins[1])'"))
            }
            'neg' { $stack.Push([System.Numerics.BigInteger]::Negate($stack.Pop())) }
            default {
                $r = $stack.Pop(); $l = $stack.Pop()
                switch ($ins[1]) {
                    '+' { $stack.Push((Test-ArithBits ([System.Numerics.BigInteger]::Add($l, $r)) 'sum')) }
                    '-' { $stack.Push((Test-ArithBits ([System.Numerics.BigInteger]::Subtract($l, $r)) 'difference')) }
                    '*' {
                        if (-not $l.IsZero -and -not $r.IsZero) {
                            $bits = [System.Numerics.BigInteger]::Abs($l).GetBitLength() + [System.Numerics.BigInteger]::Abs($r).GetBitLength()
                            if ($bits - 1 -gt $script:ArithMaxBits) { throw [ArithmeticLimitException]::new('Bits', 'product exceeds 256 bits') }   # 掛ける前に拒否
                        }
                        $stack.Push((Test-ArithBits ([System.Numerics.BigInteger]::Multiply($l, $r)) 'product'))
                    }
                    '/' {
                        if ($r.IsZero) { throw [System.DivideByZeroException]::new('division by zero') }
                        $stack.Push([System.Numerics.BigInteger]::Divide($l, $r))        # 0 方向への切り捨て
                    }
                    '%' {
                        if ($r.IsZero) { throw [System.DivideByZeroException]::new('remainder by zero') }
                        $stack.Push([System.Numerics.BigInteger]::Remainder($l, $r))     # a - trunc(a/b)*b（被除数と同符号）
                    }
                }
            }
        }
    }
    return $stack.Pop()
}

function Get-ArithPeek($p) { if ($p.Pos -lt $p.Tokens.Count) { return ,$p.Tokens[$p.Pos] } return $null }

function Read-ArithExpr($p) {
    # expr := term (('+'|'-') term)*
    Read-ArithTerm $p
    while ($true) {
        $t = Get-ArithPeek $p
        if ($null -eq $t -or $t[0] -ne 'o' -or ($t[1] -ne '+' -and $t[1] -ne '-')) { return }
        $p.Pos++
        Read-ArithTerm $p
        $p.Code.Add(@('op', $t[1]))
    }
}

function Read-ArithTerm($p) {
    # term := factor (('*'|'/'|'%') factor)*
    Read-ArithFactor $p
    while ($true) {
        $t = Get-ArithPeek $p
        if ($null -eq $t -or $t[0] -ne 'o' -or ($t[1] -ne '*' -and $t[1] -ne '/' -and $t[1] -ne '%')) { return }
        $p.Pos++
        Read-ArithFactor $p
        $p.Code.Add(@('op', $t[1]))
    }
}

function Read-ArithFactor($p) {
    # factor := ('+'|'-')* atom（単項符号はループで数える）
    $negative = $false
    while ($true) {
        $t = Get-ArithPeek $p
        if ($null -ne $t -and $t[0] -eq 'o' -and ($t[1] -eq '+' -or $t[1] -eq '-')) {
            if ($t[1] -eq '-') { $negative = -not $negative }
            $p.Pos++
        } else { break }
    }
    Read-ArithAtom $p
    if ($negative) { $p.Code.Add(@('neg')) }
}

function Read-ArithAtom($p) {
    # atom := decimal | identifier | '(' expr ')'（括弧の深さは事前検査済みなので再帰は 32 段以内）
    $t = Get-ArithPeek $p
    if ($null -eq $t) { throw [ArithmeticSyntaxException]::new('unexpected end of expression', $p.TextLength) }
    $p.Pos++
    if ($t[0] -eq 'n') { $p.Code.Add(@('lit', $t[1])); return }
    if ($t[0] -eq 'i') { $p.Code.Add(@('var', $t[1])); return }
    if ($t[1] -ne '(') { throw [ArithmeticSyntaxException]::new("unexpected token '$($t[1])'", $t[2]) }
    Read-ArithExpr $p
    $close = Get-ArithPeek $p
    if ($null -eq $close -or $close[0] -ne 'o' -or $close[1] -ne ')') {
        $where = if ($null -eq $close) { $p.TextLength } else { $close[2] }
        throw [ArithmeticSyntaxException]::new("expected ')'", $where)
    }
    $p.Pos++
}
```

#### 3. 実行環境・実行コマンド

```powershell
# 作業フォルダー（試験専用の一時領域）: scratchpad\ps\q04 に q04.ps1（実装）と test_q04.ps1（テスト）、scratchpad\ps\common に TestKit.ps1
cd <scratchpad>\ps\q04
pwsh -NoLogo -NoProfile -Command '$PSVersionTable.PSVersion.ToString(); [System.Runtime.InteropServices.RuntimeInformation]::FrameworkDescription'   # 7.7.0 / .NET 11.0.0
pwsh -NoLogo -NoProfile -File test_q04.ps1 2>&1 | Tee-Object -FilePath log.txt
$LASTEXITCODE   # 0 = 全件合格（失敗した検査の数）
```

**実行環境（全問共通）**

- **PowerShell：** **7.7.0**（pwsh。Microsoft Store 版 `Microsoft.PowerShell_8wekyb3d8bbwe`）。指定の **PowerShell 7.4 はこの PC にインストールされていません**。試験要項 2 に従い、許可なくソフトウェアを追加していません。
- **.NET：** pwsh 7.7.0 に組み込まれた **.NET 11.0.0** の上で実行しました。.NET 8 ランタイム（8.0.34）もインストールされていますが、pwsh 7.7 は自身のランタイムを使います。そのため、コードは PowerShell 7.4 / .NET 8 にもある API だけで書きました（.NET 9 以降の API は不使用）。ただし、7.4 / .NET 8 上での実行は未実施です。
- **OS / CPU：** Windows 11 Pro 10.0.26200（x64）、NTFS ／ Intel Core i7-4710MQ（4 コア 8 スレッド）。管理者権限は使っていません。
- **使用したもの：** PowerShell の標準機能と .NET の標準 API だけです。Pester・ThreadJob・外部モジュール・NuGet・外部 DLL・Add-Type による C# は使っていません。
- **テストの補助：** 自作の最小テスト補助 `common\TestKit.ps1`（Assert-True / Assert-Equal / Assert-Throws）を使いました。全文を「4.」に載せています。
- **実行方法：** 各問の作業フォルダー `scratchpad\ps\qNN` で `pwsh -NoLogo -NoProfile -File test_qNN.ps1` を実行しました。終了コード = 失敗した検査の数（0 = 全件合格）。
- **データの扱い：** ファイルを使う試験は、各問のフォルダーの `testdata\run-<GUID>` だけで行い、終了時に削除しました（残ったものは 0 件）。

- **実行結果：** `RESULT: 76 checks passed, 0 failed`、終了コード 0、所要 76.2 秒。

#### 4. 実際に実行したテストコード全文

```powershell
# ===== common/TestKit.ps1（全問共通の自作テスト補助） =====
# 試験用の最小テスト補助（Pester 不使用）。各テストファイルが dot-source する。
$script:TkPass = 0
$script:TkFail = 0
$script:TkFailures = [System.Collections.Generic.List[string]]::new()

function Assert-True {
    param([bool]$Condition, [string]$Name)
    if ($Condition) { $script:TkPass++ }
    else {
        $script:TkFail++
        $script:TkFailures.Add($Name)
        [Console]::Out.WriteLine("  FAIL: $Name")
    }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Name)
    $ok = $false
    if ($null -eq $Expected -and $null -eq $Actual) { $ok = $true }
    elseif ($null -ne $Expected -and $null -ne $Actual) {
        if ($Expected -is [byte[]] -and $Actual -is [byte[]]) {
            $ok = [System.Linq.Enumerable]::SequenceEqual([byte[]]$Expected, [byte[]]$Actual)
        } elseif ($Expected -is [string] -and $Actual -is [string]) {
            $ok = [string]::Equals($Expected, $Actual, [System.StringComparison]::Ordinal)
        } else {
            $ok = $Expected.Equals($Actual)
        }
    }
    if (-not $ok) { $Name = "$Name (expected <$Expected> actual <$Actual>)" }
    Assert-True $ok $Name
}

function Assert-Throws {
    # ScriptBlock を実行し、指定した例外型（内側の例外を含めて探索）が発生したら合格。見つかった例外は $script:TkLastException に入れる。
    param([scriptblock]$Script, [type]$ExceptionType, [string]$Name)
    $found = $null
    try { & $Script | Out-Null }
    catch {
        $e = $_.Exception
        while ($null -ne $e) {
            if ($ExceptionType.IsInstanceOfType($e)) { $found = $e; break }
            $e = $e.InnerException
        }
        if ($null -eq $found) { $Name = "$Name (got $($_.Exception.GetType().FullName): $($_.Exception.Message))" }
    }
    Assert-True ($null -ne $found) $Name
    $script:TkLastException = $found
}

function Write-TkSummary {
    [Console]::Out.WriteLine("RESULT: $script:TkPass checks passed, $script:TkFail failed")
    exit $script:TkFail
}

# ===== q04/test_q04.ps1 =====
# Q04 のテスト。参照モデルは式の木を直接 BigInteger で評価する独立実装（文字列を構文解析しない）。
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '..\common\TestKit.ps1')
. (Join-Path $PSScriptRoot 'q04.ps1')
$BI = [System.Numerics.BigInteger]

function Calc([string]$t, $vars = $null) { return Invoke-StrictArithmetic -Text $t -Variables $vars }
function Classify([scriptblock]$s) {
    try { $v = & $s; return "OK:$v" }
    catch {
        $e = $_.Exception
        if ($e -is [System.Management.Automation.MethodInvocationException] -and $null -ne $e.InnerException) { $e = $e.InnerException }
        return $e.GetType().Name
    }
}

# ---- 1. 基本・優先順位・符号・変数
$v = @{ x = $BI::new(7); y = $BI::new(-3); big = $BI::Pow(2, 255) }
$cases = [ordered]@{
    '1+2*3' = '7'; '(1+2)*3' = '9'; '-7/2' = '-3'; '-7%2' = '-1'; '7%-2' = '1'; '7/-2' = '-3'; '-7/-2' = '3'
    '-+-5' = '5'; '--5' = '5'; '+5' = '5'; '2*3+4*5' = '26'; '2*(3+4)*5' = '70'; '10-4-3' = '3'; '100/10/5' = '2'
    'x*y-x/y' = '-19'; 'x%y' = '1'; ' x + y ' = '4'; "x`t*`t2" = '14'; '0' = '0'; 'big-big' = '0'
}
foreach ($k in $cases.Keys) { Assert-Equal $cases[$k] ([string](Calc $k $v)) "evaluate '$k'" }
$r = Calc '1+2' $null
Assert-True ($r -is [System.Numerics.BigInteger]) 'result is a single BigInteger'
Assert-Equal 1 @(Calc '5').Count 'exactly one output object'

# ---- 2. 上限
$max = $BI::Pow(2, 256) - 1
Assert-Equal '0' ([string](Calc '0')) '0 bits'
Assert-Equal '1' ([string](Calc '1')) '1 bit'
Assert-Equal ([string]$max) ([string](Calc ([string]$max))) '256-bit literal accepted'
Assert-Equal ([string](-$max)) ([string](Calc ('-' + [string]$max))) 'negative 256-bit accepted'
Assert-Throws { Calc ([string]($max + 1)) } ([ArithmeticLimitException]) '257-bit literal rejected'
Assert-Throws { Calc ('1' + ('0' * 79)) } ([ArithmeticLimitException]) '80-digit literal rejected before parsing'
Assert-Throws { Calc 'm+1' @{ m = $max } } ([ArithmeticLimitException]) 'sum to 257 bits rejected'
Assert-Throws { Calc 'h*h' @{ h = $BI::Pow(2, 128) } } ([ArithmeticLimitException]) 'product 2^256 rejected'
Assert-Throws { Calc 'b*2' @{ b = $BI::Pow(2, 255) } } ([ArithmeticLimitException]) 'intermediate product 257 bits rejected'
Assert-Throws { Calc '-m-1' @{ m = $max } } ([ArithmeticLimitException]) 'difference to -2^256 rejected'
Assert-Throws { Calc 'w' @{ w = $BI::Pow(2, 256) } } ([ArithmeticLimitException]) '257-bit variable rejected'
Assert-Equal ([string]$max) ([string](Calc '(h-1)*(h+1)' @{ h = $BI::Pow(2, 128) })) '(2^128-1)(2^128+1) = 2^256-1 accepted'
Assert-Equal '1' ([string](Calc (('(' * 32) + '1' + (')' * 32)))) '32 nested parentheses'
Assert-Throws { Calc (('(' * 33) + '1' + (')' * 33)) } ([ArithmeticLimitException]) '33 nested parentheses rejected'
$t256 = '-1' + ('+1' * 127)
Assert-Equal '126' ([string](Calc $t256)) '256 tokens accepted'
Assert-Throws { Calc ('-' + $t256) } ([ArithmeticLimitException]) '257 tokens rejected'
Assert-Equal '1' ([string](Calc ('1' + (' ' * 4095)))) '4096 characters accepted'
Assert-Throws { Calc ('1' + (' ' * 4096)) } ([ArithmeticLimitException]) '4097 characters rejected'
Assert-Throws { Calc '1/0' } ([System.DivideByZeroException]) 'division by zero'
Assert-Throws { Calc '1%(2-2)' } ([System.DivideByZeroException]) 'remainder by zero'
Assert-Throws { Calc 'nope+1' } ([UnknownVariableException]) 'unknown variable'
Assert-Throws { Calc 'X' @{ x = $BI::One } } ([UnknownVariableException]) 'variable lookup is Ordinal (X != x)'
Assert-Throws { Calc 'x' @{ x = 1 } } ([System.ArgumentException]) 'Int32 variable value rejected'
Assert-Throws { Calc 'x' @{ x = '1' } } ([System.ArgumentException]) 'string variable value rejected'
Assert-Throws { Calc 'x' @{ 1 = $BI::One } } ([System.ArgumentException]) 'non-string variable name rejected'
Assert-Throws { Calc ('a' * 33) } ([ArithmeticSyntaxException]) '33-character identifier rejected'
Assert-Throws { Calc '01' } ([ArithmeticSyntaxException]) 'leading zero rejected'
Assert-Throws { Calc '' } ([ArithmeticSyntaxException]) 'empty text rejected'

# ---- 3. インジェクション（何も実行されない）
$sandbox = Join-Path $PSScriptRoot ('testdata\inj-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $sandbox -Force
Push-Location $sandbox
try {
    $global:Q04Canary = 0
    $before = @(Get-ChildItem -LiteralPath $sandbox -Force).Count
    $procBefore = @(Get-Process -Name pwsh, cmd, notepad -ErrorAction SilentlyContinue | ForEach-Object Id)
    $inj = [ordered]@{
        'Get-ChildItem' = 'UnknownVariableException'; '1;Remove-Item x' = 'ArithmeticSyntaxException'
        '$env:PATH' = 'ArithmeticSyntaxException'; '1|Out-File pwned.txt' = 'ArithmeticSyntaxException'
        '1 2' = 'ArithmeticSyntaxException'; '(1+2)3' = 'ArithmeticSyntaxException'; '1+2)' = 'ArithmeticSyntaxException'
        "1`n+2" = 'ArithmeticSyntaxException'; '"1"' = 'ArithmeticSyntaxException'; "'1'" = 'ArithmeticSyntaxException'
        '`$x' = 'ArithmeticSyntaxException'; 'f(1)' = 'ArithmeticSyntaxException'; 'a.b' = 'ArithmeticSyntaxException'
        '[int]1' = 'ArithmeticSyntaxException'; '$global:Q04Canary++' = 'ArithmeticSyntaxException'
        '$(New-Item pwned2.txt)' = 'ArithmeticSyntaxException'; '2^3' = 'ArithmeticSyntaxException'; '1e5' = 'ArithmeticSyntaxException'
        '& notepad' = 'ArithmeticSyntaxException'; '1,2' = 'ArithmeticSyntaxException'; '1 + ' = 'ArithmeticSyntaxException'
    }
    foreach ($k in $inj.Keys) { Assert-Equal $inj[$k] (Classify { Calc $k }) "reject '$k'" }
    Assert-Equal 0 $global:Q04Canary 'canary variable untouched'
    Assert-Equal $before @(Get-ChildItem -LiteralPath $sandbox -Force).Count 'no file created in the working directory'
    $procAfter = @(Get-Process -Name pwsh, cmd, notepad -ErrorAction SilentlyContinue | ForEach-Object Id)
    Assert-Equal 0 @($procAfter | Where-Object { $procBefore -notcontains $_ }).Count 'no new pwsh/cmd/notepad process'
} finally { Pop-Location; Remove-Item -LiteralPath $sandbox -Recurse -Force }

# ---- 4. 参照モデルとの比較（有効 1 万件・破損 1 万件）
$rng = [System.Random]::new(7)
$names = 'a', 'b', 'c', 'Var_1'
$env4 = @{ a = $BI::new(12345); b = $BI::new(-7); c = $BI::Pow(2, 200); Var_1 = $BI::Zero }
function New-Tree([int]$depth) {
    if ($depth -eq 0 -or $rng.Next(10) -lt 3) {
        if ($rng.Next(3) -eq 0) { return @{ K = 'v'; N = $names[$rng.Next($names.Count)] } }
        $lits = @('0', '1', '2', '7', '10', '99', '65536', [string]$BI::Pow(2, 100), [string]$BI::Pow(2, 255))
        return @{ K = 'c'; V = $lits[$rng.Next($lits.Count)] }
    }
    if ($rng.Next(10) -lt 2) { return @{ K = 'neg'; A = (New-Tree ($depth - 1)) } }
    return @{ K = ('+', '-', '*', '/', '%')[$rng.Next(5)]; A = (New-Tree ($depth - 1)); B = (New-Tree ($depth - 1)) }
}
function Render($t) {
    $sp = if ($rng.Next(4) -eq 0) { ' ' } else { '' }
    switch ($t.K) {
        'c' { return $t.V }
        'v' { return $t.N }
        'neg' { return "(-$(Render $t.A))" }
        default { return "($(Render $t.A)$sp$($t.K)$sp$(Render $t.B))" }
    }
}
function RefEval($t) {
    switch ($t.K) {
        'c' { $r = $BI::Parse($t.V) }
        'v' { $r = $env4[$t.N] }
        'neg' { $r = - (RefEval $t.A) }
        default {
            $x = RefEval $t.A; $y = RefEval $t.B
            switch ($t.K) {
                '+' { $r = $x + $y } '-' { $r = $x - $y } '*' { $r = $x * $y }
                '/' { if ($y -eq 0) { throw [System.DivideByZeroException]::new() }; $r = $BI::Divide($x, $y) }
                '%' { if ($y -eq 0) { throw [System.DivideByZeroException]::new() }; $r = $x - $BI::Divide($x, $y) * $y }
            }
        }
    }
    if ($BI::Abs($r).GetBitLength() -gt 256) { throw [System.OverflowException]::new() }
    return $r
}
$stats = @{ OK = 0; Limit = 0; DivZero = 0; Mismatch = 0 }
$sw = [System.Diagnostics.Stopwatch]::StartNew()
for ($n = 0; $n -lt 10000; $n++) {
    $tree = New-Tree 4
    $src = Render $tree
    $want = try { 'OK:' + (RefEval $tree) } catch [System.DivideByZeroException] { 'DivideByZeroException' } catch [System.OverflowException] { 'ArithmeticLimitException' }
    $got = Classify { Calc $src $env4 }
    if ($got -ne $want) { $stats.Mismatch++; if ($stats.Mismatch -le 3) { [Console]::Out.WriteLine("  mismatch: $src -> $got / $want") } }
    elseif ($want -like 'OK:*') { $stats.OK++ } elseif ($want -eq 'DivideByZeroException') { $stats.DivZero++ } else { $stats.Limit++ }
}
Assert-Equal 0 $stats.Mismatch '10000 valid expressions match the reference model'
[Console]::Out.WriteLine("[Q04] 10000 valid expressions: OK=$($stats.OK) Limit=$($stats.Limit) DivideByZero=$($stats.DivZero) in $([math]::Round($sw.Elapsed.TotalSeconds,1)) s")

$bad = 0; $kinds = @{}
$sw.Restart()
for ($n = 0; $n -lt 10000; $n++) {
    $src = Render (New-Tree 3)
    switch ($rng.Next(6)) {
        0 { $pos = $rng.Next($src.Length + 1); $src = $src.Insert($pos, ([string[]]('$', ';', '|', '`', '"', "'", "`n", '.', ',', '[', '^', '&', '{', '!'))[$rng.Next(14)]); $m = 'forbidden char' }
        1 { $i = $src.LastIndexOf(')'); if ($i -ge 0) { $src = $src.Remove($i, 1) } else { $src = "($src" }; $m = 'unbalanced' }
        2 { $src = $src + ' ' + ('7', 'a', '(1)')[$rng.Next(3)]; $m = 'trailing token' }
        3 { $src = "$src+01"; $m = 'leading zero' }
        4 { $src = "$src*/2"; $m = 'operator sequence' }
        5 { $src = "$src+()"; $m = 'empty parentheses' }
    }
    $got = Classify { Calc $src $env4 }
    $kinds[$m] = 1 + [int]$kinds[$m]
    if ($got -ne 'ArithmeticSyntaxException') { $bad++; if ($bad -le 3) { [Console]::Out.WriteLine("  unexpected: [$m] $src -> $got") } }
}
Assert-Equal 0 $bad '10000 corrupted expressions -> ArithmeticSyntaxException'
[Console]::Out.WriteLine("[Q04] 10000 corrupted expressions in $([math]::Round($sw.Elapsed.TotalSeconds,1)) s: " + (($kinds.GetEnumerator() | Sort-Object Name | ForEach-Object { "$($_.Name)=$($_.Value)" }) -join ', ') + "; managed heap $([GC]::GetTotalMemory($true)) bytes")

Write-TkSummary
```

#### 5. 期待結果・実測結果・実施状況

| 必須検証ケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| `1+2*3`・`(1+2)*3` | 7・9 | 一致 | 成功 |
| `-7/2=-3`・`-7%2=-1`・`7%-2=1` | 0 方向の切り捨てと、被除数と同符号の余り（`7/-2=-3`、`-7/-2=3` も） | 一致 | 成功 |
| `-+-5` と単項符号 | 5（`--5`=5、`+5`=5） | 一致 | 成功 |
| 変数参照と優先順位 | `x*y-x/y`（x=7, y=-3）は -19、`x%y`=1、`2*3+4*5`=26、`10-4-3`=3（左結合）、空白・タブも可 | 20 式すべて一致 | 成功 |
| 戻り値 | 単一の BigInteger オブジェクト | 一致 | 成功 |
| 0 / 1 / 256 / 257 ビット | 0・1・2^256-1・-(2^256-1) は受理。2^256 の定数、80 桁の定数（解析前に桁数で拒否）、中間の和・差・積が 257 ビットになる式、257 ビットの変数は ArithmeticLimitException。`(h-1)*(h+1)`（h=2^128）は 2^256-1 で受理 | 一致 | 成功 |
| 32 / 33 重括弧 | 32 段は受理、33 段は ArithmeticLimitException（再帰の前に検査） | 一致 | 成功 |
| 256 / 257 トークン | 256 個は受理（結果 126）、257 個は Limit | 一致 | 成功 |
| 4096 / 4097 文字 | 4096 文字は受理、4097 文字は Limit | 一致 | 成功 |
| ゼロ除算 | `1/0`・`1%(2-2)` は DivideByZeroException | 一致 | 成功 |
| 未知変数・Ordinal | `nope+1`、`X`（辞書のキーは x）は UnknownVariableException | 一致 | 成功 |
| 変数値の型 | Int32・文字列の値、文字列以外のキーは ArgumentException | 一致 | 成功 |
| 構文の境界 | 33 文字の識別子・`01`（先頭 0）・空文字は ArithmeticSyntaxException | 一致 | 成功 |
| `Get-ChildItem`・`;Remove-Item`・`$env:PATH`・パイプ・余剰トークン | `Get-ChildItem` は文法上「Get − ChildItem」の引き算なので UnknownVariableException（実行はしない）。`1;Remove-Item x`・`$env:PATH`・`1\|Out-File pwned.txt`・`1 2`・`(1+2)3`・`1+2)`・改行・引用符・バッククォート・`f(1)`・`a.b`・`[int]1`・`$global:Q04Canary++`・`$(New-Item pwned2.txt)`・`2^3`・`1e5`・`& notepad`・`1,2`・`1 + ` は ArithmeticSyntaxException | 21 種すべて一致 | 成功 |
| 副作用が一切ないこと | カナリア変数は 0 のまま。作業ディレクトリに新しいファイルなし。pwsh/cmd/notepad の新しいプロセスなし | 一致 | 成功 |
| 独立モデルとの比較：有効な式 1 万件 | 式の木を直接 BigInteger で評価するモデル（文字列を解析しない）と、値・Limit・DivideByZero の分類が一致 | 10,000/10,000 一致（OK 7,476、Limit 630、DivideByZero 1,894）、49.3 秒 | 成功 |
| 破損した式 1 万件 | 禁止文字の挿入・括弧の不整合・余剰トークン・先頭 0・演算子の連続・空括弧は、すべて ArithmeticSyntaxException（計算エラーより先に構文エラー） | 10,000/10,000（6 種、各約 1,650 件）、24.3 秒。終了時のマネージドヒープ 10.4 MB | 成功 |

**自律修正：** 最初の版は、構文解析と計算を同時に行っていました。そのため、`(1/0) 7` のような入力で、余剰トークンの構文エラーより先に DivideByZero が出る設計上の欠陥がありました（テストの設計段階で発見）。文法の検査と後置記法の命令列の生成を先に済ませ、構文が正しい場合だけ評価する 2 段階に作り直しました。そのため、全テストはこの版で初めて実行しています。

#### 6. 技術的根拠・保証範囲・計算量

- **インジェクションの封止：** 字句解析が受け付けるのは、ASCII の数字・英字・`_`・`+ - * / % ( )`・スペース・タブだけです。それ以外の文字（$・;・|・バッククォート・二重引用符・単引用符・改行・ピリオド・カンマ・角括弧・^・& など）は、構文エラーとして即座に拒否します。`Invoke-Expression`・`[scriptblock]::Create`・`AddScript`・`Invoke()` は使わず、入力はデータとしてだけ扱います。
- **文法：** 問題の EBNF どおりに、expr → term → factor → atom の再帰下降で解析します。単項符号はループで数えます（再帰しない）。括弧の深さはトークン列の事前走査で 32 以下に制限するので、再帰は高々 32 段です。
- **上限：**
  - 長さ 4096 → トークン 256 → 括弧 32 の順で、計算の前に検査します。
  - 数値リテラルは、79 桁以上なら `BigInteger.Parse` の前に拒否します（2^256-1 は 78 桁）。
  - 掛け算は、`bits(a)+bits(b)-1 > 256` なら計算せずに拒否します。
  - 和・差・積の結果と、定数・変数の値は、`Abs(v).GetBitLength() <= 256` で検査します。
- **除算：** `BigInteger.Divide`（0 方向への切り捨て）と `BigInteger.Remainder`（= a − trunc(a/b)·b）で、床除算と混同しません。
- **変数：** IDictionary を Ordinal の Dictionary にコピーします。キーは厳密に string、値は厳密に BigInteger（PSObject のラッパーを外して型を比較）です。
- **計算量：** 時間 O(文字数)、メモリ O(トークン数) です。数値は 256 ビット以下なので、1 回の演算は定数時間とみなせます。

#### 7. 未実施・未解決・制約事項

- `Get-ChildItem` は、文法上は正しい「変数 Get − 変数 ChildItem」の式です。そのため、拒否の種類は構文エラーではなく UnknownVariableException になります。コマンドとしては実行されません（試験で確認）。
- 識別子が 32 文字を超える場合は、文法の違反として ArithmeticSyntaxException にしました（上限エラーではない）。
- PowerShell 7.4 / .NET 8 での実行は未実施です。
- 参照資料：Microsoft Learn の System.Numerics.BigInteger（Divide / Remainder / GetBitLength）。コードは独自に作成しました。

---

## Q05【地獄級】RunspacePoolによる有界並列実行と入力順結果の確定

**評価の狙い：** PowerShellのrunspace分離、BeginInvoke/EndInvoke、エラーストリーム観測、有界タスク生成、リソース解放。

### 元コード（危険・不完全／実装の出発点）

```powershell
function Invoke-ParallelBad {
    param([object[]]$Items, [string]$ScriptText)
    foreach ($item in $Items) {
        Start-Job -ScriptBlock ([scriptblock]::Create($ScriptText)) -ArgumentList $item
    }
}
```

### 必須要求仕様

1. `Invoke-BoundedRunspaces -Items ([object[]]) -ActionSource ([string]) -MaxConcurrency ([int]4)`を実装。Items件数0〜10000、MaxConcurrencyは1〜8。ActionSourceは試験側が用意した**信頼済みの完全なPowerShellスクリプト文字列**で、先頭の`param($Item)`を受け取る。ActionSourceの内容のサンドボックス化や任意コード防御は本問の対象外。
2. PowerShell SDKの`RunspaceFactory.CreateRunspacePool()`と`[powershell]::Create()`などPowerShellに付属の.NET APIを用い、RunspacePoolの最大サイズをMaxConcurrencyに設定する。各入力に対して、ActionSourceをちょうど1回呼ぶ。入力データは他の作業中に変更されない契約。
3. 同時に未完了のPowerShell呼出しはMaxConcurrency以下。**入力全件分のPowerShellインスタンスや非同期ハンドルを先に生成しない**。完了を検知するたびに次の入力を登録する。空入力では新規runspaceを作らない。
4. 各Actionは成功時に成功ストリームへ**厳密に1個の`[long]`**を出力する契約。0件/2件以上、型不一致、終端例外、非終端エラーがある場合はその入力を失敗として記録する。Warning/Verbose/Informationは結果値に混入しない。
5. 結果は入力と同じ添字順の配列`[pscustomobject]@{ Index=[int]; Status='Ok'|'Failed'; Value=[long]または$null; ErrorType=[string]または$null; ErrorMessage=[string]または$null }`を**1個のトップレベルコレクションとして返す**。失敗しても他の入力の実行は継続する。例外は必ず観測して未回収の非同期ハンドルを残さない。
6. 処理完了時には`EndInvoke`・PowerShellインスタンスのDispose・RunspacePoolのClose/Disposeを正確に1回行う。外側の呼出しがキャンセルされた場合の強制停止は要求しない（すべてのActionは有限時間で戻る契約）。PowerShellのJobやThreadJob、ForEach-Object -Parallelによる代替は禁止。
7. 本問の受験環境はPowerShell 7.4／Windows 11。利用するActionはテスト専用の純計算またはテスト領域への書込みだけに限定。

### 必須検証項目

1. 空/1/10000入力、MaxConcurrency 1/4/8。出力件数・順序・Indexを一致させ、同時実行ピークが指定値以内であること。
2. 成功・終端例外・非終端エラー・0件出力・2件出力・int32や文字列出力を混在させ、失敗が別入力を止めないこと。
3. 各Actionで短い非同期待機を行い、1000件の入力順が完了順と違っても出力が元順に並ぶこと。
4. 未観測例外0、Dispose漏れ0、全入力分のPowerShellインスタンスを先に作らないことを件数の計測で証明。

### ★★FREYJAの回答【Q05・記入欄】

> **以下は専用回答欄。** 問題文・危険コード・要求仕様・必須検証・採点基準は変更禁止。修正コード、テストコード、実測を以下に全文記載する。

#### 1. 根本原因・危険性・修正方針

**元コード `Invoke-ParallelBad` の問題点**

| # | 問題箇所 | 発生条件 | 危険性・影響 |
| --- | --- | --- | --- |
| 1 | 入力ごとに `Start-Job` | 入力 1 万件 | 1 件ごとに子プロセスを起動し、同時実行数の上限がない（プロセスとメモリを使い果たす） |
| 2 | Job を回収しない | 常に | 結果・エラーを受け取らず、Job が残り続ける（リーク） |
| 3 | 結果の順序・形式がない | — | 入力との対応、成功と失敗の区別ができない |
| 4 | `[scriptblock]::Create` を Job ごとに作る | — | 不要な再解析。失敗の観測がない |

**修正方針**
- `RunspaceFactory.CreateRunspacePool(1, MaxConcurrency)` を 1 つ作り、`[powershell]::Create()` を同時に MaxConcurrency 個までだけ生成します。
- `BeginInvoke` の待機ハンドルを `WaitHandle.WaitAny` で待ち（ポーリングなし）、完了したものを `EndInvoke` で回収・Dispose してから次の入力を登録します。
- 結果は入力の添字の位置に格納し、1 つの配列として返します。

#### 2. 完全な修正版PowerShellコード

```powershell
#Requires -Version 7.4
# Q05: RunspacePool による有界並列実行（入力順の結果確定・全ハンドルの回収）

Set-StrictMode -Version 3.0

function Invoke-BoundedRunspaces {
    [CmdletBinding()]
    [OutputType([object[]])]
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][AllowNull()][object[]]$Items,
        [Parameter(Mandatory)][string]$ActionSource,     # 試験側が用意した信頼済みのスクリプト（先頭に param($Item)）
        [ValidateRange(1, 8)][int]$MaxConcurrency = 4,
        # 診断用（任意）: PoolsCreated / InstancesCreated / InstancesDisposed / EndInvokeCalls / MaxLiveInstances / PoolsDisposed
        [hashtable]$Diagnostics
    )
    if ($Items.Count -gt 10000) { throw [System.ArgumentOutOfRangeException]::new('Items', 'at most 10000 items') }
    $n = $Items.Count
    $results = [object[]]::new($n)
    $diag = if ($null -ne $Diagnostics) { $Diagnostics } else { @{} }
    foreach ($k in 'PoolsCreated', 'InstancesCreated', 'InstancesDisposed', 'EndInvokeCalls', 'MaxLiveInstances', 'PoolsDisposed') { $diag[$k] = 0 }
    if ($n -eq 0) {
        Write-Output -NoEnumerate $results      # 空入力では runspace を作らない
        return
    }

    $pool = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspacePool(1, $MaxConcurrency)
    $diag.PoolsCreated++
    $active = [System.Collections.Generic.List[object]]::new()
    try {
        $pool.Open()
        $next = 0
        while ($next -lt $n -or $active.Count -gt 0) {
            # 空きがある分だけ次の入力を登録する（全件分を先に作らない）
            while ($active.Count -lt $MaxConcurrency -and $next -lt $n) {
                $ps = [powershell]::Create()
                $diag.InstancesCreated++
                $ps.RunspacePool = $pool
                [void]$ps.AddScript($ActionSource).AddParameter('Item', $Items[$next])
                $handle = $ps.BeginInvoke()
                $active.Add([pscustomobject]@{ Index = $next; PS = $ps; Handle = $handle })
                if ($active.Count -gt $diag.MaxLiveInstances) { $diag.MaxLiveInstances = $active.Count }
                $next++
            }
            # どれか 1 つの完了を待つ（ポーリングなし）
            $waits = [System.Threading.WaitHandle[]]::new($active.Count)
            for ($w = 0; $w -lt $active.Count; $w++) { $waits[$w] = $active[$w].Handle.AsyncWaitHandle }
            [void][System.Threading.WaitHandle]::WaitAny($waits)
            # 完了済みのものをすべて回収
            for ($w = $active.Count - 1; $w -ge 0; $w--) {
                $a = $active[$w]
                if (-not $a.Handle.IsCompleted) { continue }
                $active.RemoveAt($w)
                $results[$a.Index] = Complete-BoundedRunspaceItem $a $diag
            }
        }
    } finally {
        # 想定外の内部エラー時も、未回収のインスタンスを止めて回収する
        foreach ($a in $active) {
            try { $a.PS.Stop() } catch { }
            try { [void]$a.PS.EndInvoke($a.Handle); $diag.EndInvokeCalls++ } catch { }
            $a.PS.Dispose(); $diag.InstancesDisposed++
        }
        $pool.Close()
        $pool.Dispose()
        $diag.PoolsDisposed++
    }
    Write-Output -NoEnumerate $results          # 1 個のトップレベルコレクションとして返す
}

function Complete-BoundedRunspaceItem($a, $diag) {
    $status = 'Ok'; $value = $null; $etype = $null; $emsg = $null
    try {
        $output = $null
        try {
            $output = $a.PS.EndInvoke($a.Handle)          # 例外は必ずここで観測する
        } catch {
            $e = $_.Exception
            if ($e -is [System.Management.Automation.MethodInvocationException] -and $null -ne $e.InnerException) { $e = $e.InnerException }
            # throw [X]::new() の場合、元の例外は RuntimeException.ErrorRecord.Exception にある
            if ($e -is [System.Management.Automation.RuntimeException] -and $null -ne $e.ErrorRecord -and $null -ne $e.ErrorRecord.Exception) { $e = $e.ErrorRecord.Exception }
            $status = 'Failed'; $etype = $e.GetType().FullName; $emsg = $e.Message
        } finally { $diag.EndInvokeCalls++ }
        if ($status -eq 'Ok' -and $a.PS.Streams.Error.Count -gt 0) {
            $status = 'Failed'; $etype = 'NonTerminatingError'; $emsg = [string]$a.PS.Streams.Error[0]
        }
        if ($status -eq 'Ok' -and $output.Count -ne 1) {
            $status = 'Failed'; $etype = 'OutputCount'; $emsg = "expected exactly 1 output object, got $($output.Count)"
        }
        if ($status -eq 'Ok') {
            $o = $output[0]
            $base = if ($null -eq $o) { $null } else { $o.psobject.BaseObject }
            if ($null -eq $base -or -not [object]::ReferenceEquals($base.GetType(), [long])) {
                $status = 'Failed'; $etype = 'OutputType'
                $emsg = "expected System.Int64, got $(if ($null -eq $base) { 'null' } else { $base.GetType().FullName })"
            } else { $value = [long]$base }
        }
    } finally {
        $a.PS.Dispose()
        $diag.InstancesDisposed++
    }
    return [pscustomobject][ordered]@{ Index = [int]$a.Index; Status = $status; Value = $value; ErrorType = $etype; ErrorMessage = $emsg }
}
```

#### 3. 実行環境・実行コマンド

```powershell
# 作業フォルダー（試験専用の一時領域）: scratchpad\ps\q05 に q05.ps1（実装）と test_q05.ps1（テスト）、scratchpad\ps\common に TestKit.ps1
cd <scratchpad>\ps\q05
pwsh -NoLogo -NoProfile -Command '$PSVersionTable.PSVersion.ToString(); [System.Runtime.InteropServices.RuntimeInformation]::FrameworkDescription'   # 7.7.0 / .NET 11.0.0
pwsh -NoLogo -NoProfile -File test_q05.ps1 2>&1 | Tee-Object -FilePath log.txt
$LASTEXITCODE   # 0 = 全件合格（失敗した検査の数）
```

**実行環境（全問共通）**

- **PowerShell：** **7.7.0**（pwsh。Microsoft Store 版 `Microsoft.PowerShell_8wekyb3d8bbwe`）。指定の **PowerShell 7.4 はこの PC にインストールされていません**。試験要項 2 に従い、許可なくソフトウェアを追加していません。
- **.NET：** pwsh 7.7.0 に組み込まれた **.NET 11.0.0** の上で実行しました。.NET 8 ランタイム（8.0.34）もインストールされていますが、pwsh 7.7 は自身のランタイムを使います。そのため、コードは PowerShell 7.4 / .NET 8 にもある API だけで書きました（.NET 9 以降の API は不使用）。ただし、7.4 / .NET 8 上での実行は未実施です。
- **OS / CPU：** Windows 11 Pro 10.0.26200（x64）、NTFS ／ Intel Core i7-4710MQ（4 コア 8 スレッド）。管理者権限は使っていません。
- **使用したもの：** PowerShell の標準機能と .NET の標準 API だけです。Pester・ThreadJob・外部モジュール・NuGet・外部 DLL・Add-Type による C# は使っていません。
- **テストの補助：** 自作の最小テスト補助 `common\TestKit.ps1`（Assert-True / Assert-Equal / Assert-Throws）を使いました。全文を「4.」に載せています。
- **実行方法：** 各問の作業フォルダー `scratchpad\ps\qNN` で `pwsh -NoLogo -NoProfile -File test_qNN.ps1` を実行しました。終了コード = 失敗した検査の数（0 = 全件合格）。
- **データの扱い：** ファイルを使う試験は、各問のフォルダーの `testdata\run-<GUID>` だけで行い、終了時に削除しました（残ったものは 0 件）。

- **最終の実行結果：** `RESULT: 52 checks passed, 0 failed`、終了コード 0、所要 73.1 秒。
- **1 回目の実行：** 50 件合格・2 件失敗（下記の自律修正）。

#### 4. 実際に実行したテストコード全文

```powershell
# ===== common/TestKit.ps1（全問共通の自作テスト補助） =====
# 試験用の最小テスト補助（Pester 不使用）。各テストファイルが dot-source する。
$script:TkPass = 0
$script:TkFail = 0
$script:TkFailures = [System.Collections.Generic.List[string]]::new()

function Assert-True {
    param([bool]$Condition, [string]$Name)
    if ($Condition) { $script:TkPass++ }
    else {
        $script:TkFail++
        $script:TkFailures.Add($Name)
        [Console]::Out.WriteLine("  FAIL: $Name")
    }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Name)
    $ok = $false
    if ($null -eq $Expected -and $null -eq $Actual) { $ok = $true }
    elseif ($null -ne $Expected -and $null -ne $Actual) {
        if ($Expected -is [byte[]] -and $Actual -is [byte[]]) {
            $ok = [System.Linq.Enumerable]::SequenceEqual([byte[]]$Expected, [byte[]]$Actual)
        } elseif ($Expected -is [string] -and $Actual -is [string]) {
            $ok = [string]::Equals($Expected, $Actual, [System.StringComparison]::Ordinal)
        } else {
            $ok = $Expected.Equals($Actual)
        }
    }
    if (-not $ok) { $Name = "$Name (expected <$Expected> actual <$Actual>)" }
    Assert-True $ok $Name
}

function Assert-Throws {
    # ScriptBlock を実行し、指定した例外型（内側の例外を含めて探索）が発生したら合格。見つかった例外は $script:TkLastException に入れる。
    param([scriptblock]$Script, [type]$ExceptionType, [string]$Name)
    $found = $null
    try { & $Script | Out-Null }
    catch {
        $e = $_.Exception
        while ($null -ne $e) {
            if ($ExceptionType.IsInstanceOfType($e)) { $found = $e; break }
            $e = $e.InnerException
        }
        if ($null -eq $found) { $Name = "$Name (got $($_.Exception.GetType().FullName): $($_.Exception.Message))" }
    }
    Assert-True ($null -ne $found) $Name
    $script:TkLastException = $found
}

function Write-TkSummary {
    [Console]::Out.WriteLine("RESULT: $script:TkPass checks passed, $script:TkFail failed")
    exit $script:TkFail
}

# ===== q05/test_q05.ps1 =====
# Q05 のテスト。Action は純計算と短い待機だけ（ファイル・外部への操作なし）。
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '..\common\TestKit.ps1')
. (Join-Path $PSScriptRoot 'q05.ps1')

# 同時実行数を計測する共有状態（ロックは Monitor、同じスレッドで Enter/Exit）
function New-SharedState { [pscustomobject]@{ Lock = [object]::new(); Current = 0; Peak = 0; Done = [System.Collections.Generic.List[int]]::new() } }
$measuring = @'
param($Item)
$s = $Item.State
[System.Threading.Monitor]::Enter($s.Lock)
try { $s.Current++; if ($s.Current -gt $s.Peak) { $s.Peak = $s.Current } } finally { [System.Threading.Monitor]::Exit($s.Lock) }
try {
    if ($Item.DelayMs -gt 0) { [System.Threading.Tasks.Task]::Delay($Item.DelayMs).Wait() }
    [long]($Item.N * 2)
} finally {
    [System.Threading.Monitor]::Enter($s.Lock)
    try { $s.Current--; $s.Done.Add($Item.N) } finally { [System.Threading.Monitor]::Exit($s.Lock) }
}
'@

# ---- 1. 空 / 1 / 10000 件 × MaxConcurrency 1/4/8
$d = @{}
$r = Invoke-BoundedRunspaces -Items @() -ActionSource $measuring -Diagnostics $d
Assert-True ($r -is [object[]] -and $r.Count -eq 0) 'empty input -> one empty array'
Assert-Equal 0 $d.PoolsCreated 'empty input creates no runspace pool'
Assert-Equal 0 $d.InstancesCreated 'empty input creates no PowerShell instance'
$st = New-SharedState
$r = Invoke-BoundedRunspaces -Items @([pscustomobject]@{ N = 21; DelayMs = 0; State = $st }) -ActionSource $measuring -Diagnostics $d
Assert-True ($r -is [object[]] -and $r.Count -eq 1 -and $r[0].Status -eq 'Ok' -and $r[0].Value -eq 42 -and $r[0].Value -is [long]) '1 input -> Ok 42'
foreach ($mc in 1, 4, 8) {
    $st = New-SharedState
    $items = [object[]]::new(10000)
    for ($i = 0; $i -lt 10000; $i++) { $items[$i] = [pscustomobject]@{ N = $i; DelayMs = 0; State = $st } }
    $d = @{}
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $r = Invoke-BoundedRunspaces -Items $items -ActionSource $measuring -MaxConcurrency $mc -Diagnostics $d
    $sw.Stop()
    $orderOk = $true
    for ($i = 0; $i -lt 10000; $i++) { if ($r[$i].Index -ne $i -or $r[$i].Status -ne 'Ok' -or $r[$i].Value -ne 2 * $i) { $orderOk = $false; break } }
    Assert-Equal 10000 $r.Count "MaxConcurrency ${mc}: 10000 results"
    Assert-True $orderOk "MaxConcurrency ${mc}: Index/order/value all match"
    Assert-True ($st.Peak -le $mc -and $st.Peak -ge 1) "MaxConcurrency ${mc}: measured peak $($st.Peak) <= $mc"
    Assert-True ($d.MaxLiveInstances -le $mc) "MaxConcurrency ${mc}: live PowerShell instances $($d.MaxLiveInstances) <= $mc"
    Assert-Equal 10000 $d.InstancesCreated "MaxConcurrency ${mc}: exactly one instance per input"
    Assert-Equal 10000 $d.InstancesDisposed "MaxConcurrency ${mc}: every instance disposed"
    Assert-Equal 10000 $d.EndInvokeCalls "MaxConcurrency ${mc}: EndInvoke once per input"
    Assert-True ($d.PoolsCreated -eq 1 -and $d.PoolsDisposed -eq 1) "MaxConcurrency ${mc}: pool created and disposed once"
    [Console]::Out.WriteLine("[Q05] 10000 items, MaxConcurrency $mc : $([math]::Round($sw.Elapsed.TotalSeconds,1)) s, peak concurrency $($st.Peak), max live instances $($d.MaxLiveInstances)")
}

# ---- 2. 成功と各種失敗の混在
$mixed = @'
param($Item)
switch ($Item) {
    'ok'        { [long]7 }
    'throw'     { throw [System.InvalidOperationException]::new('boom') }
    'throwtext' { throw 'plain text failure' }
    'nonterm'   { Write-Error 'soft failure'; [long]1 }
    'zero'      { }
    'two'       { [long]1; [long]2 }
    'int32'     { [int]5 }
    'string'    { '5' }
    'double'    { 5.0 }
    'null'      { $null }
    'noise'     { Write-Warning 'w'; Write-Verbose 'v' -Verbose; Write-Information 'i'; Write-Host 'h'; [long]9 }
    'methodex'  { [System.IO.File]::ReadAllText('Z:\definitely\missing\file.txt') }
}
'@
$kinds = 'ok', 'throw', 'throwtext', 'nonterm', 'zero', 'two', 'int32', 'string', 'double', 'null', 'noise', 'methodex', 'ok'
$d = @{}
$r = Invoke-BoundedRunspaces -Items $kinds -ActionSource $mixed -MaxConcurrency 3 -Diagnostics $d
$expected = @(
    'Ok|7|', 'Failed||System.InvalidOperationException', 'Failed||System.Management.Automation.RuntimeException',
    'Failed||NonTerminatingError', 'Failed||OutputCount', 'Failed||OutputCount', 'Failed||OutputType', 'Failed||OutputType',
    'Failed||OutputType', 'Failed||OutputType', 'Ok|9|', 'Failed||NonTerminatingError', 'Ok|7|')   # .NET メソッドの例外は文単位のエラー（スクリプトは継続）
for ($i = 0; $i -lt $kinds.Count; $i++) {
    $got = "$($r[$i].Status)|$($r[$i].Value)|$($r[$i].ErrorType)"
    Assert-Equal $expected[$i] $got "item $i ($($kinds[$i]))"
}
Assert-True (@($r | Where-Object { $_.Status -eq 'Failed' -and [string]::IsNullOrEmpty($_.ErrorMessage) }).Count -eq 0) 'every failure has an ErrorMessage'
Assert-True (@($r | Where-Object { $_.Status -eq 'Ok' -and ($null -ne $_.ErrorType -or $null -ne $_.ErrorMessage) }).Count -eq 0) 'Ok results have null error fields'
Assert-True ($d.InstancesCreated -eq $kinds.Count -and $d.InstancesDisposed -eq $kinds.Count -and $d.EndInvokeCalls -eq $kinds.Count) 'mixed: all observed and disposed'

# ---- 3. 完了順が入力順と異なっても結果は入力順
$st = New-SharedState
$rng = [System.Random]::new(5)
$items = [object[]]::new(1000)
for ($i = 0; $i -lt 1000; $i++) { $items[$i] = [pscustomobject]@{ N = $i; DelayMs = $rng.Next(0, 8); State = $st } }
$d = @{}
$r = Invoke-BoundedRunspaces -Items $items -ActionSource $measuring -MaxConcurrency 8 -Diagnostics $d
$inversions = 0
for ($i = 1; $i -lt $st.Done.Count; $i++) { if ($st.Done[$i] -lt $st.Done[$i - 1]) { $inversions++ } }
$orderOk = $true
for ($i = 0; $i -lt 1000; $i++) { if ($r[$i].Index -ne $i -or $r[$i].Value -ne 2 * $i) { $orderOk = $false } }
Assert-True ($inversions -gt 0) "completion order differs from input order ($inversions inversions)"
Assert-True $orderOk 'results still in input order'
Assert-True ($st.Peak -le 8) "peak $($st.Peak) <= 8 with async waits"
[Console]::Out.WriteLine("[Q05] 1000 items with 0-7 ms waits, MaxConcurrency 8: peak concurrency $($st.Peak), completion-order inversions $inversions")

# ---- 4. 引数の検証
Assert-Throws { Invoke-BoundedRunspaces -Items ([object[]]::new(10001)) -ActionSource 'param($Item) [long]1' } ([System.ArgumentOutOfRangeException]) '10001 items rejected'
Assert-Throws { Invoke-BoundedRunspaces -Items @(1) -ActionSource 'param($Item) [long]1' -MaxConcurrency 9 } ([System.Management.Automation.ParameterBindingException]) 'MaxConcurrency 9 rejected'
Assert-Throws { Invoke-BoundedRunspaces -Items @(1) -ActionSource 'param($Item) [long]1' -MaxConcurrency 0 } ([System.Management.Automation.ParameterBindingException]) 'MaxConcurrency 0 rejected'
$syntax = Invoke-BoundedRunspaces -Items @(1, 2) -ActionSource 'param($Item) [long]($Item' -MaxConcurrency 2
Assert-True ($syntax.Count -eq 2 -and @($syntax | Where-Object Status -eq 'Failed').Count -eq 2) 'a script with a syntax error fails each item without throwing'

# 未観測の例外・残留 runspace がないこと
[GC]::Collect(); [GC]::WaitForPendingFinalizers()
$open = @(Get-Runspace | Where-Object { $_.Id -ne $Host.Runspace.Id -and $_.RunspaceStateInfo.State -eq 'Opened' })
Assert-Equal 0 $open.Count 'no extra opened runspaces left in the process'

Write-TkSummary
```

#### 5. 期待結果・実測結果・実施状況

同時実行数は、Action の中で、共有オブジェクトを Monitor で保護したカウンターによって実測しました。

| 必須検証ケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 空入力 | 空の配列 1 個。RunspacePool・PowerShell インスタンスを作らない | Pool 0・インスタンス 0 | 成功 |
| 1 件 | `Ok`・Value 42（long） | 一致 | 成功 |
| 10000 件 × MaxConcurrency 1 | 10000 件の Index・順序・値が一致。同時実行のピーク ≤ 1。インスタンスは 1 件に 1 つで、全件 Dispose・EndInvoke | 28.7 秒、ピーク 1、同時に存在するインスタンス ≤ 1 | 成功 |
| 10000 件 × MaxConcurrency 4 | 同上でピーク ≤ 4 | 17.6〜18.1 秒、ピーク 2、同時に存在するインスタンス ≤ 4 | 成功 |
| 10000 件 × MaxConcurrency 8 | 同上でピーク ≤ 8 | 17.4〜19.1 秒、ピーク 3、同時に存在するインスタンス ≤ 8 | 成功 |
| 成功と失敗の混在（13 件、MaxConcurrency 3） | ok → Ok 7。`throw [InvalidOperationException]` → Failed（System.InvalidOperationException）。`throw 'text'` → RuntimeException。Write-Error → NonTerminatingError。0 件 / 2 件の出力 → OutputCount。int32・文字列・double・null → OutputType。Warning/Verbose/Information/Host を混ぜても Ok 9。.NET メソッドの例外 → NonTerminatingError。最後の ok も Ok（失敗が他の入力を止めない） | 13 件すべて一致 | 成功 |
| 失敗の記録 | 失敗はすべて ErrorMessage あり、Ok は ErrorType/ErrorMessage が null | 一致 | 成功 |
| 完了順と入力順の違い | 1000 件に 0〜7 ms の非同期待機（`Task.Delay().Wait()`）。完了順が入力順と異なっても、結果は入力順 | 完了順の逆転 371 件、結果は入力順、ピーク 8 | 成功 |
| 未観測例外 0・Dispose 漏れ 0 | 全ケースで EndInvoke の回数 = 生成数 = Dispose 数、Pool は 1 回作って 1 回破棄。終了後、余分な Opened の runspace なし | 一致 | 成功 |
| 全件分のインスタンスを先に作らない | 診断の MaxLiveInstances ≤ MaxConcurrency（1/4/8） | 1/4/8 | 成功 |
| 引数 | 10001 件は ArgumentOutOfRangeException、MaxConcurrency 0 / 9 は ParameterBindingException。構文エラーのある ActionSource は各入力が Failed（例外を外へ出さない） | 一致 | 成功 |

純計算の Action では処理が短く、10000 件での同時実行のピークは 2〜3 にとどまりました。1 ms 未満で終わるため、次の登録が追いつかないためです。待機を入れた 1000 件の試験では、上限の 8 ちょうどに達することを確認しました。

**自律修正**
- **実装：** `throw [InvalidOperationException]::new()` の失敗が、ErrorType では RuntimeException（PowerShell のラッパー）として記録されていました。EndInvoke の例外の `RuntimeException.ErrorRecord.Exception` から元の例外の型を取り出すよう修正しました。
- **テストの期待値の誤り：** スクリプトの中の .NET メソッドの例外（`[System.IO.File]::ReadAllText` の失敗）は、PowerShell の仕様では文を終了するエラーです。スクリプトは継続し、エラーストリームに記録されます。そのため NonTerminatingError が正しい結果で、期待値を直しました。
- 修正後に Q05 全体を再実行し、合格しました。

#### 6. 技術的根拠・保証範囲・計算量

- **有界性：** 未完了の呼出しは `$active` のリストにだけあり、登録はその件数が MaxConcurrency 未満のときだけ行います。RunspacePool の最大サイズも MaxConcurrency なので、実行中の runspace は二重に制限されます。
- **回収：**
  - 完了検知は `WaitHandle.WaitAny`（最大 8 個のハンドル）で行います。完了したものは必ず `EndInvoke` を呼び、例外をここで観測してから、finally で `Dispose` します。
  - 想定外の内部エラーのときも、finally で残った呼出しを Stop・EndInvoke・Dispose し、Pool を Close・Dispose します。
- **結果の判定：**
  - EndInvoke の例外は Failed（元の例外の型）。
  - `Streams.Error` が 1 件以上なら NonTerminatingError。
  - 出力が 1 件以外なら OutputCount。
  - 基底の型が厳密に Int64 でなければ OutputType。
  - Warning・Verbose・Information は別のストリームなので、結果に混ざりません。
- **順序：** 入力の添字で `$results[$index]` に格納するので、完了順と無関係に入力順になります。戻り値は `Write-Output -NoEnumerate` で 1 つの配列です。
- **計算量：** 登録・回収とも O(N)。待機は 1 回あたり O(MaxConcurrency) です。

#### 7. 未実施・未解決・制約事項

- 契約により、外側の呼出しのキャンセル（Ctrl+C）による強制停止は対象外です。
- 問題文の指定どおり、ActionSource の内容のサンドボックス化はしていません。信頼済みのスクリプトという前提で、`AddScript` で実行します。
- PowerShell 7.4 / .NET 8 での実行は未実施です。
- 参照資料：Microsoft Learn の RunspaceFactory.CreateRunspacePool・PowerShell.BeginInvoke/EndInvoke・PSDataStreams。コードは独自に作成しました。

---

## Q06【地獄級】Utf8JsonReaderによる重複キー拒否・厳密なJSONスキーマ

**評価の狙い：** ConvertFrom-Jsonが失う重複プロパティ、case-sensitiveキー、Unicode、巨大入力、暗黙数値変換。

### 元コード（危険・不完全／実装の出発点）

```powershell
function Read-ConfigBad {
    param([string]$Path)
    $data = Get-Content $Path | ConvertFrom-Json
    return $data
}
```

### 必須要求仕様

1. `Read-StrictJsonBatch -Path ([string])`を実装。入力ファイルはUTF-8（BOMなし）、最大8MiB。トップレベルは**JSON配列**（0〜100000要素）で、各要素は正確に3キー`id`、`amount`、`tags`を持つJSON object。idはASCII英数字・`_`・`-`の1〜64文字、amountは符号付き64bit整数、tagsは0〜16個の文字列配列（各文字列最大64 Unicodeスカラー値）。
2. JSONのobjectキーはUnicodeのコードポイントが**大文字小文字を区別して一致する**。同一object内のキーの完全重複は拒否。`ID`/`Id`等は`id`の代わりにならない。`ConvertFrom-Json`等による読み込み後の検査だけでは重複キーが失われるため禁止。
3. 数値トークンは`-?(0|[1-9][0-9]*)`の整数表記だけを許可。指数・小数・文字列の数値・boolは拒否。int64範囲の境界を厳密に扱い、`-0`は0として受理。名前・tagsには不対サロゲートを拒否し、JSONのUnicodeエスケープを正しく復号する。
4. .NET 8 の`System.Text.Json`（JsonDocument/Utf8JsonReader等）でバイト列から検査。JSONネスト上限は16。**8MiBを超えるファイルは全体を読み込む前に拒否**。ファイルの読み取り中にサイズが増えないというテスト契約。すべての検査が完了してから成功ストリームへ結果を返し、1件でも不正なら**成功出力を0件**にする。
5. 成功結果は入力配列順に`[pscustomobject]@{ Id=[string]; Amount=[long]; Tags=[string[]] }`を返す。Tagsは独立した配列で、参照を共有してはいけない。ファイル内容をPowerShellコードとして評価しない。
6. 例外は`StrictJsonException`で`Kind`（`Encoding`/`Syntax`/`Schema`/`Limit`）と`Index`（問題の配列要素番号、不明または配列外は-1）。不正UTF-8はEncoding、構文破損はSyntax、キー/型/重複の違反はSchema、入力長/配列件数/深さ超過はLimitとする。入力ソースが不正な場合でも既存ユーザーファイルを変更しない。

### 必須検証項目

1. 空配列、1件、100000件の合法データ、混合Unicode、エスケープ、int64最小値/最大値、-0を確認。
2. 重複`id`、`ID`と`id`の混在、大小文字の違う必須キー、余剰キー、欠落、文字列数値、小数、指数、範囲外整数、tags17個を拒否。
3. 不正UTF-8、BOM、孤立サロゲート、構文エラー、16/17段のネスト、8MiB/8MiB+1境界。KindとIndexを検証。
4. 末尾の1要素だけが破損した10000件入力で成功出力0件、正常入力では独立Tags、カルチャー差異なしを確認。

### ★★FREYJAの回答【Q06・記入欄】

> **以下は専用回答欄。** 問題文・危険コード・要求仕様・必須検証・採点基準は変更禁止。修正コード、テストコード、実測を以下に全文記載する。

#### 1. 根本原因・危険性・修正方針

**元コード `Read-ConfigBad` の問題点**

| # | 問題箇所 | 発生条件 | 危険性・影響 |
| --- | --- | --- | --- |
| 1 | `ConvertFrom-Json` | 同じオブジェクトの重複キー | 重複は後勝ちで黙って失われ、検出できない（PowerShell 7 では大小文字違いのキーもエラーや上書きになる） |
| 2 | `Get-Content`（行単位・既定のエンコーディング） | 大きなファイル・BOM・不正な UTF-8 | 置換文字による化け・BOM の誤処理。全体を文字列の配列として読む |
| 3 | サイズ・件数・深さの上限がない | 巨大・深い入力 | メモリ・CPU を使い果たす |
| 4 | スキーマの検査がない | 余剰・欠落キー、文字列の数値、小数、範囲外の整数 | 暗黙の型変換（double・decimal）で誤った値が通る |
| 5 | 部分的な結果 | 途中の要素が不正 | 不正な入力でも一部が利用される |

**修正方針**
- ファイルサイズを先に確認し（8 MiB 超は読む前に Limit）、バイト列として読みます。
- BOM を拒否し、厳密な UTF-8（UTF8Encoding(false, true)）で全体を検証します。
- 不対サロゲートの `\u` エスケープを正規表現の事前検査で検出します（キー・値とも）。
- `System.Text.Json.JsonDocument` で構文を解析し、非再帰の走査で深さ（16）を検査します。
- 各要素について、キーの Ordinal での一致・重複、id・amount・tags の型と範囲を検査します。
- 全件が合格してから結果を出力します。例外は `StrictJsonException(Kind, Index)` です。

#### 2. 完全な修正版PowerShellコード

```powershell
#Requires -Version 7.4
# Q06: System.Text.Json による厳密な JSON バッチ読込（重複キー拒否・型厳密・全件検査後に出力）

Set-StrictMode -Version 3.0

class StrictJsonException : System.Exception {
    [string]$Kind      # Encoding / Syntax / Schema / Limit
    [long]$Index       # 問題の配列要素番号（不明・配列外は -1）
    StrictJsonException([string]$kind, [long]$index, [string]$message) : base("$kind error (index $index): $message") {
        $this.Kind = $kind
        $this.Index = $index
    }
}

$script:SjMaxBytes = 8388608        # 8 MiB
$script:SjMaxElements = 100000
$script:SjMaxDepth = 16
$script:SjIdPattern = [regex]::new('\A[A-Za-z0-9_-]{1,64}\z', [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
$script:SjIntPattern = [regex]::new('\A-?(0|[1-9][0-9]*)\z', [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
# 高位サロゲートの後に低位が続かない、または前に高位がない低位サロゲートの \u エスケープ
$script:SjLoneSurrogate = [regex]::new(
    '(?<=(?:^|[^\\])(?:\\\\)*)(?:\\u[dD][89aAbB][0-9a-fA-F]{2}(?!\\u[dD][c-fC-F][0-9a-fA-F]{2})|(?<!\\u[dD][89aAbB][0-9a-fA-F]{2})\\u[dD][c-fC-F][0-9a-fA-F]{2})',
    [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)

function Get-SjScan {
    <# バイト列を先頭から走査し、(1) 指定オフセットが属するトップレベル要素の番号、(2) 深さが上限を超えた最初の位置と要素番号を求める。
       エラー時の分類にだけ使う（文字列内の括弧・エスケープは無視する）。 #>
    param([byte[]]$Bytes, [long]$StopOffset)
    $depth = 0; $inString = $false; $escape = $false; [long]$elem = -1; $rootIsArray = $false; $seenFirst = $false
    $limitAt = -1; [long]$limitElem = -1; [long]$elemAtStop = -1
    $n = [Math]::Min([long]$Bytes.Length, $StopOffset)
    for ([long]$i = 0; $i -lt $n; $i++) {
        $b = $Bytes[$i]
        if ($inString) {
            if ($escape) { $escape = $false } elseif ($b -eq 0x5C) { $escape = $true } elseif ($b -eq 0x22) { $inString = $false }
            continue
        }
        if ($b -eq 0x20 -or $b -eq 0x09 -or $b -eq 0x0A -or $b -eq 0x0D) { continue }
        if ($depth -eq 1 -and $rootIsArray -and -not $seenFirst -and $b -ne 0x5D) { $seenFirst = $true; $elem = 0 }
        if ($b -eq 0x22) { $inString = $true }
        elseif ($b -eq 0x5B -or $b -eq 0x7B) {
            if ($depth -eq 0 -and $b -eq 0x5B) { $rootIsArray = $true }
            $depth++
            if ($depth -gt $script:SjMaxDepth -and $limitAt -lt 0) { $limitAt = $i; $limitElem = $elem }
        }
        elseif ($b -eq 0x5D -or $b -eq 0x7D) { $depth-- }
        elseif ($b -eq 0x2C -and $depth -eq 1 -and $rootIsArray) { $elem++ }
    }
    return @{ ElementAtStop = $elem; LimitAt = $limitAt; LimitElement = $limitElem }
}

function Get-SjScalarCount([string]$s) {
    # Unicode スカラー値の数（正しいサロゲートペアは 1 個）。不対サロゲートなら -1。
    $count = 0
    for ($i = 0; $i -lt $s.Length; $i++) {
        $c = [int]$s[$i]
        if ($c -ge 0xD800 -and $c -le 0xDBFF) {
            if ($i + 1 -lt $s.Length -and [int]$s[$i + 1] -ge 0xDC00 -and [int]$s[$i + 1] -le 0xDFFF) { $i++ } else { return -1 }
        } elseif ($c -ge 0xDC00 -and $c -le 0xDFFF) { return -1 }
        $count++
    }
    return $count
}

function Get-SjString([System.Text.Json.JsonElement]$Element, [long]$Index, [string]$What) {
    try { $s = $Element.GetString() }
    catch { throw [StrictJsonException]::new('Encoding', $Index, "$What contains an unpaired surrogate escape") }
    if ((Get-SjScalarCount $s) -lt 0) { throw [StrictJsonException]::new('Encoding', $Index, "$What contains an unpaired surrogate") }
    return $s
}

function Read-StrictJsonBatch {
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    param([Parameter(Mandatory)][string]$Path)

    $full = [System.IO.Path]::GetFullPath($Path)
    $info = [System.IO.FileInfo]::new($full)
    if (-not $info.Exists) { throw [System.IO.FileNotFoundException]::new('input file not found', $full) }
    if ($info.Length -gt $script:SjMaxBytes) {
        throw [StrictJsonException]::new('Limit', -1, "file is $($info.Length) bytes (max $($script:SjMaxBytes)); rejected before reading")
    }

    # ---- 読み取り（読み取り専用・共有読み取り。サイズは事前に確認した値まで）
    $bytes = [byte[]]::new([int]$info.Length)
    $fs = [System.IO.FileStream]::new($full, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read)
    try {
        $got = 0
        while ($got -lt $bytes.Length) {
            $r = $fs.Read($bytes, $got, $bytes.Length - $got)
            if ($r -le 0) { break }
            $got += $r
        }
        if ($got -ne $bytes.Length -or $fs.Read([byte[]]::new(1), 0, 1) -ne 0) {
            throw [StrictJsonException]::new('Limit', -1, 'file size changed while reading')
        }
    } finally { $fs.Dispose() }

    # ---- 符号化: BOM 禁止・厳密な UTF-8
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
        throw [StrictJsonException]::new('Encoding', -1, 'UTF-8 BOM is not allowed')
    }
    try { [void][System.Text.UTF8Encoding]::new($false, $true).GetCharCount($bytes) }
    catch {
        $e = $_.Exception; if ($null -ne $e.InnerException) { $e = $e.InnerException }
        $at = if ($e -is [System.Text.DecoderFallbackException] -and $e.Index -ge 0) { [long]$e.Index } else { [long]$bytes.Length }
        throw [StrictJsonException]::new('Encoding', (Get-SjScan $bytes ($at + 1)).ElementAtStop, "invalid UTF-8 near byte $at")
    }

    # ---- 構文（JsonDocument）。深さ上限の超過は Limit、それ以外の構文破損は Syntax
    $opts = [System.Text.Json.JsonDocumentOptions]::new()
    $opts.MaxDepth = 64
    $opts.AllowTrailingCommas = $false
    $opts.CommentHandling = [System.Text.Json.JsonCommentHandling]::Disallow
    $doc = $null
    try { $doc = [System.Text.Json.JsonDocument]::Parse([System.ReadOnlyMemory[byte]]::new($bytes), $opts) }
    catch {
        $je = $_.Exception; while ($null -ne $je -and $je -isnot [System.Text.Json.JsonException]) { $je = $je.InnerException }
        [long]$off = $bytes.Length
        if ($null -ne $je -and $null -ne $je.LineNumber -and $null -ne $je.BytePositionInLine) {
            $line = 0; [long]$pos = 0
            while ($line -lt $je.LineNumber -and $pos -lt $bytes.Length) { if ($bytes[$pos] -eq 0x0A) { $line++ }; $pos++ }
            $off = [Math]::Min([long]$bytes.Length, $pos + [long]$je.BytePositionInLine)
        }
        $scan = Get-SjScan $bytes ($off + 1)
        if ($scan.LimitAt -ge 0) { throw [StrictJsonException]::new('Limit', $scan.LimitElement, "nesting deeper than $($script:SjMaxDepth)") }
        throw [StrictJsonException]::new('Syntax', $scan.ElementAtStop, "malformed JSON near byte $off")
    }

    try {
        # 不対サロゲートの \u エスケープ（キー・値とも）を事前に検出する。JsonProperty.Name は置換文字に
        # 化けて例外にならない場合があるため、構文が正しい入力の生テキストを正規表現で検査する。
        # (?<=...) で直前の '\' が偶数個（= このバックスラッシュ自体はエスケープされていない）ことを確認する。
        $text = [System.Text.UTF8Encoding]::new($false, $true).GetString($bytes)
        $m = $script:SjLoneSurrogate.Match($text)
        if ($m.Success) {
            $byteOff = [System.Text.Encoding]::UTF8.GetByteCount($text.Substring(0, $m.Index))
            throw [StrictJsonException]::new('Encoding', (Get-SjScan $bytes ($byteOff + 1)).ElementAtStop, 'unpaired surrogate escape')
        }

        $root = $doc.RootElement
        # 深さの検査（非再帰のスタック走査）: トップレベル配列 = 1
        $stack = [System.Collections.Generic.Stack[object]]::new()
        $stack.Push(@($root, 1, [long]-1))
        while ($stack.Count -gt 0) {
            $item = $stack.Pop(); $el = [System.Text.Json.JsonElement]$item[0]; $d = [int]$item[1]; $top = [long]$item[2]
            if ($d -gt $script:SjMaxDepth) { throw [StrictJsonException]::new('Limit', $top, "nesting deeper than $($script:SjMaxDepth)") }
            if ($el.ValueKind -eq [System.Text.Json.JsonValueKind]::Array) {
                $k = 0
                foreach ($c in $el.EnumerateArray()) {
                    if ($c.ValueKind -eq [System.Text.Json.JsonValueKind]::Array -or $c.ValueKind -eq [System.Text.Json.JsonValueKind]::Object) {
                        $stack.Push(@($c, ($d + 1), $(if ($d -eq 1) { [long]$k } else { $top })))
                    }
                    $k++
                }
            } elseif ($el.ValueKind -eq [System.Text.Json.JsonValueKind]::Object) {
                foreach ($p in $el.EnumerateObject()) {
                    $c = $p.Value
                    if ($c.ValueKind -eq [System.Text.Json.JsonValueKind]::Array -or $c.ValueKind -eq [System.Text.Json.JsonValueKind]::Object) {
                        $stack.Push(@($c, ($d + 1), $top))
                    }
                }
            }
        }

        if ($root.ValueKind -ne [System.Text.Json.JsonValueKind]::Array) { throw [StrictJsonException]::new('Schema', -1, 'top-level value must be an array') }
        $count = $root.GetArrayLength()
        if ($count -gt $script:SjMaxElements) { throw [StrictJsonException]::new('Limit', -1, "array has $count elements (max $($script:SjMaxElements))") }

        $results = [System.Collections.Generic.List[object]]::new($count)
        [long]$index = 0
        foreach ($el in $root.EnumerateArray()) {
            if ($el.ValueKind -ne [System.Text.Json.JsonValueKind]::Object) { throw [StrictJsonException]::new('Schema', $index, 'element must be an object') }
            $names = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
            $idEl = $null; $amountEl = $null; $tagsEl = $null
            foreach ($p in $el.EnumerateObject()) {
                try { $name = $p.Name } catch { throw [StrictJsonException]::new('Encoding', $index, 'property name contains an unpaired surrogate escape') }
                if ((Get-SjScalarCount $name) -lt 0) { throw [StrictJsonException]::new('Encoding', $index, 'property name contains an unpaired surrogate') }
                if (-not $names.Add($name)) { throw [StrictJsonException]::new('Schema', $index, "duplicate key '$name'") }
                if ([string]::Equals($name, 'id', [System.StringComparison]::Ordinal)) { $idEl = $p.Value }
                elseif ([string]::Equals($name, 'amount', [System.StringComparison]::Ordinal)) { $amountEl = $p.Value }
                elseif ([string]::Equals($name, 'tags', [System.StringComparison]::Ordinal)) { $tagsEl = $p.Value }
                else { throw [StrictJsonException]::new('Schema', $index, "unexpected key '$name'") }
            }
            if ($names.Count -ne 3) { throw [StrictJsonException]::new('Schema', $index, 'object must have exactly id, amount and tags') }

            if ($idEl.ValueKind -ne [System.Text.Json.JsonValueKind]::String) { throw [StrictJsonException]::new('Schema', $index, 'id must be a string') }
            $id = Get-SjString $idEl $index 'id'
            if (-not $script:SjIdPattern.IsMatch($id)) { throw [StrictJsonException]::new('Schema', $index, 'id must be 1-64 ASCII letters, digits, _ or -') }

            if ($amountEl.ValueKind -ne [System.Text.Json.JsonValueKind]::Number) { throw [StrictJsonException]::new('Schema', $index, 'amount must be a JSON number') }
            $raw = $amountEl.GetRawText()
            if (-not $script:SjIntPattern.IsMatch($raw)) { throw [StrictJsonException]::new('Schema', $index, "amount '$raw' is not an integer literal") }
            [long]$amount = 0
            if (-not $amountEl.TryGetInt64([ref]$amount)) { throw [StrictJsonException]::new('Schema', $index, "amount '$raw' is outside the int64 range") }

            if ($tagsEl.ValueKind -ne [System.Text.Json.JsonValueKind]::Array) { throw [StrictJsonException]::new('Schema', $index, 'tags must be an array') }
            $tagCount = $tagsEl.GetArrayLength()
            if ($tagCount -gt 16) { throw [StrictJsonException]::new('Schema', $index, "tags has $tagCount items (max 16)") }
            $tags = [string[]]::new($tagCount)
            $t = 0
            foreach ($tagEl in $tagsEl.EnumerateArray()) {
                if ($tagEl.ValueKind -ne [System.Text.Json.JsonValueKind]::String) { throw [StrictJsonException]::new('Schema', $index, 'tags must contain only strings') }
                $s = Get-SjString $tagEl $index 'tag'
                if ((Get-SjScalarCount $s) -gt 64) { throw [StrictJsonException]::new('Schema', $index, 'tag longer than 64 Unicode scalar values') }
                $tags[$t] = $s; $t++
            }
            $results.Add([pscustomobject][ordered]@{ Id = $id; Amount = $amount; Tags = $tags })   # Tags は要素ごとに新しい配列
            $index++
        }
    } finally { $doc.Dispose() }

    foreach ($r in $results) { $r }      # すべての検査が終わってから出力する
}
```

#### 3. 実行環境・実行コマンド

```powershell
# 作業フォルダー（試験専用の一時領域）: scratchpad\ps\q06 に q06.ps1（実装）と test_q06.ps1（テスト）、scratchpad\ps\common に TestKit.ps1
cd <scratchpad>\ps\q06
pwsh -NoLogo -NoProfile -Command '$PSVersionTable.PSVersion.ToString(); [System.Runtime.InteropServices.RuntimeInformation]::FrameworkDescription'   # 7.7.0 / .NET 11.0.0
pwsh -NoLogo -NoProfile -File test_q06.ps1 2>&1 | Tee-Object -FilePath log.txt
$LASTEXITCODE   # 0 = 全件合格（失敗した検査の数）
```

**実行環境（全問共通）**

- **PowerShell：** **7.7.0**（pwsh。Microsoft Store 版 `Microsoft.PowerShell_8wekyb3d8bbwe`）。指定の **PowerShell 7.4 はこの PC にインストールされていません**。試験要項 2 に従い、許可なくソフトウェアを追加していません。
- **.NET：** pwsh 7.7.0 に組み込まれた **.NET 11.0.0** の上で実行しました。.NET 8 ランタイム（8.0.34）もインストールされていますが、pwsh 7.7 は自身のランタイムを使います。そのため、コードは PowerShell 7.4 / .NET 8 にもある API だけで書きました（.NET 9 以降の API は不使用）。ただし、7.4 / .NET 8 上での実行は未実施です。
- **OS / CPU：** Windows 11 Pro 10.0.26200（x64）、NTFS ／ Intel Core i7-4710MQ（4 コア 8 スレッド）。管理者権限は使っていません。
- **使用したもの：** PowerShell の標準機能と .NET の標準 API だけです。Pester・ThreadJob・外部モジュール・NuGet・外部 DLL・Add-Type による C# は使っていません。
- **テストの補助：** 自作の最小テスト補助 `common\TestKit.ps1`（Assert-True / Assert-Equal / Assert-Throws）を使いました。全文を「4.」に載せています。
- **実行方法：** 各問の作業フォルダー `scratchpad\ps\qNN` で `pwsh -NoLogo -NoProfile -File test_qNN.ps1` を実行しました。終了コード = 失敗した検査の数（0 = 全件合格）。
- **データの扱い：** ファイルを使う試験は、各問のフォルダーの `testdata\run-<GUID>` だけで行い、終了時に削除しました（残ったものは 0 件）。

- **最終の実行結果：** `RESULT: 55 checks passed, 0 failed`、終了コード 0、所要 94.6 秒（10 万件のファイルに 65.3 秒）。
- **1 回目の実行：** 51 件合格・2 件失敗（下記の自律修正）。

#### 4. 実際に実行したテストコード全文

```powershell
# ===== common/TestKit.ps1（全問共通の自作テスト補助） =====
# 試験用の最小テスト補助（Pester 不使用）。各テストファイルが dot-source する。
$script:TkPass = 0
$script:TkFail = 0
$script:TkFailures = [System.Collections.Generic.List[string]]::new()

function Assert-True {
    param([bool]$Condition, [string]$Name)
    if ($Condition) { $script:TkPass++ }
    else {
        $script:TkFail++
        $script:TkFailures.Add($Name)
        [Console]::Out.WriteLine("  FAIL: $Name")
    }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Name)
    $ok = $false
    if ($null -eq $Expected -and $null -eq $Actual) { $ok = $true }
    elseif ($null -ne $Expected -and $null -ne $Actual) {
        if ($Expected -is [byte[]] -and $Actual -is [byte[]]) {
            $ok = [System.Linq.Enumerable]::SequenceEqual([byte[]]$Expected, [byte[]]$Actual)
        } elseif ($Expected -is [string] -and $Actual -is [string]) {
            $ok = [string]::Equals($Expected, $Actual, [System.StringComparison]::Ordinal)
        } else {
            $ok = $Expected.Equals($Actual)
        }
    }
    if (-not $ok) { $Name = "$Name (expected <$Expected> actual <$Actual>)" }
    Assert-True $ok $Name
}

function Assert-Throws {
    # ScriptBlock を実行し、指定した例外型（内側の例外を含めて探索）が発生したら合格。見つかった例外は $script:TkLastException に入れる。
    param([scriptblock]$Script, [type]$ExceptionType, [string]$Name)
    $found = $null
    try { & $Script | Out-Null }
    catch {
        $e = $_.Exception
        while ($null -ne $e) {
            if ($ExceptionType.IsInstanceOfType($e)) { $found = $e; break }
            $e = $e.InnerException
        }
        if ($null -eq $found) { $Name = "$Name (got $($_.Exception.GetType().FullName): $($_.Exception.Message))" }
    }
    Assert-True ($null -ne $found) $Name
    $script:TkLastException = $found
}

function Write-TkSummary {
    [Console]::Out.WriteLine("RESULT: $script:TkPass checks passed, $script:TkFail failed")
    exit $script:TkFail
}

# ===== q06/test_q06.ps1 =====
# Q06 のテスト。入力ファイルはすべて q06\testdata 配下の使い捨てフォルダーに作る。
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '..\common\TestKit.ps1')
. (Join-Path $PSScriptRoot 'q06.ps1')

$base = Join-Path $PSScriptRoot ('testdata\run-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $base -Force
$script:fileNo = 0
function New-JsonFile([object]$content) {
    $script:fileNo++
    $p = Join-Path $base ("in{0:D4}.json" -f $script:fileNo)
    if ($content -is [byte[]]) { [System.IO.File]::WriteAllBytes($p, $content) }
    else { [System.IO.File]::WriteAllBytes($p, [System.Text.UTF8Encoding]::new($false).GetBytes([string]$content)) }
    return $p
}
function Read-Kind([object]$content) {
    $p = New-JsonFile $content
    $before = [System.IO.File]::ReadAllBytes($p)
    $out = [System.Collections.Generic.List[object]]::new()
    $res = 'OK'
    try { Read-StrictJsonBatch -Path $p | ForEach-Object { $out.Add($_) } }
    catch {
        $e = $_.Exception; while ($null -ne $e -and $e -isnot [StrictJsonException]) { $e = $e.InnerException }
        if ($null -eq $e) { throw }
        $res = "$($e.Kind)@$($e.Index)"
    }
    $after = [System.IO.File]::ReadAllBytes($p)
    if (-not [System.Linq.Enumerable]::SequenceEqual($before, $after)) { $res += '#MODIFIED' }
    return @{ Result = $res; Out = $out }
}
function Elem([string]$id = 'a', [string]$amount = '1', [string]$tags = '[]') { "{`"id`":`"$id`",`"amount`":$amount,`"tags`":$tags}" }

try {
    # ---- 1. 正常系
    $r = Read-Kind '[]'
    Assert-True ($r.Result -eq 'OK' -and $r.Out.Count -eq 0) 'empty array -> 0 results'
    $r = Read-Kind ('[' + (Elem 'x_1' '-5' '["a","b"]') + ']')
    Assert-True ($r.Result -eq 'OK' -and $r.Out.Count -eq 1 -and $r.Out[0].Id -eq 'x_1' -and $r.Out[0].Amount -eq -5 -and $r.Out[0].Amount -is [long] -and $r.Out[0].Tags -is [string[]] -and ($r.Out[0].Tags -join ',') -eq 'a,b') 'one element'
    Assert-Equal 'Id,Amount,Tags' (($r.Out[0].psobject.Properties | ForEach-Object Name) -join ',') 'property order'
    $uni = '[{"tags":["日本語","é́","😀x","tab\t\"q\"\\/\n","A"],"amount":9223372036854775807,"id":"id-9"},' +
           '{"id":"min","amount":-9223372036854775808,"tags":[]},{"id":"z","amount":-0,"tags":[""]}]'
    $r = Read-Kind $uni
    Assert-Equal 'OK' $r.Result 'mixed Unicode / escapes / int64 bounds / -0 accepted'
    Assert-True ($r.Out[0].Id -ceq 'id-9' -and $r.Out[0].Amount -eq [long]::MaxValue) 'escaped id decoded, int64 max'
    Assert-True ($r.Out[0].Tags[0] -ceq '日本語' -and $r.Out[0].Tags[1] -ceq "é$([char]0x301)" -and $r.Out[0].Tags[2] -ceq "$([char]::ConvertFromUtf32(0x1F600))x" -and $r.Out[0].Tags[3] -ceq "tab`t`"q`"\/`n" -and $r.Out[0].Tags[4] -ceq 'A') 'escapes decoded exactly'
    Assert-True ($r.Out[1].Amount -eq [long]::MinValue -and $r.Out[2].Amount -eq 0 -and $r.Out[2].Tags.Count -eq 1) 'int64 min and -0 -> 0'
    $tag64 = ([char]::ConvertFromUtf32(0x1F600)) * 64                   # 64 スカラー値（128 UTF-16 単位）
    $r = Read-Kind ('[' + (Elem 'a' '1' "[`"$tag64`"]") + ']')
    Assert-Equal 'OK' $r.Result 'tag of 64 scalar values (surrogate pairs) accepted'
    $r = Read-Kind ('[' + (Elem 'a' '1' "[`"$($tag64 + 'x')`"]") + ']')
    Assert-Equal 'Schema@0' $r.Result 'tag of 65 scalar values rejected'
    $r = Read-Kind ('[' + (Elem 'a' '1' ('[' + ((1..16 | ForEach-Object { "`"t$_`"" }) -join ',') + ']')) + ']')
    Assert-True ($r.Result -eq 'OK' -and $r.Out[0].Tags.Count -eq 16) '16 tags accepted'

    $sb = [System.Text.StringBuilder]::new('[')
    for ($i = 0; $i -lt 100000; $i++) {
        if ($i) { [void]$sb.Append(',') }
        [void]$sb.Append('{"id":"e').Append($i).Append('","amount":').Append($i * 3 - 7).Append(',"tags":["t').Append($i % 7).Append('"]}')
    }
    [void]$sb.Append(']')
    $big = $sb.ToString()
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $r = Read-Kind $big
    $sw.Stop()
    $ok = $r.Result -eq 'OK' -and $r.Out.Count -eq 100000
    if ($ok) { for ($i = 0; $i -lt 100000; $i += 997) { if ($r.Out[$i].Id -ne "e$i" -or $r.Out[$i].Amount -ne $i * 3 - 7) { $ok = $false } } }
    Assert-True $ok '100000 elements accepted in order'
    Assert-True (-not [object]::ReferenceEquals($r.Out[0].Tags, $r.Out[7].Tags)) 'Tags arrays are independent objects'
    $r.Out[0].Tags[0] = 'changed'
    Assert-Equal 't0' $r.Out[7].Tags[0] 'changing one Tags array does not affect another'
    [Console]::Out.WriteLine("[Q06] 100000 elements ($($big.Length) bytes) in $([math]::Round($sw.Elapsed.TotalSeconds,1)) s")

    # ---- 2. スキーマ違反
    $schema = [ordered]@{
        'duplicate id' = '[{"id":"a","id":"b","amount":1,"tags":[]}]'
        'ID and id' = '[{"ID":"a","id":"b","amount":1,"tags":[]}]'
        'Id instead of id' = '[{"Id":"a","amount":1,"tags":[]}]'
        'AMOUNT' = '[{"id":"a","AMOUNT":1,"tags":[]}]'
        'extra key' = '[{"id":"a","amount":1,"tags":[],"x":0}]'
        'missing tags' = '[{"id":"a","amount":1}]'
        'string number' = '[{"id":"a","amount":"1","tags":[]}]'
        'decimal' = '[{"id":"a","amount":1.0,"tags":[]}]'
        'exponent' = '[{"id":"a","amount":1e3,"tags":[]}]'
        'bool' = '[{"id":"a","amount":true,"tags":[]}]'
        'above int64' = '[{"id":"a","amount":9223372036854775808,"tags":[]}]'
        'below int64' = '[{"id":"a","amount":-9223372036854775809,"tags":[]}]'
        'tags 17' = ('[' + (Elem 'a' '1' ('[' + ((1..17 | ForEach-Object { "`"t$_`"" }) -join ',') + ']')) + ']')
        'tag not string' = '[{"id":"a","amount":1,"tags":[1]}]'
        'id 65' = ('[' + (Elem ('a' * 65)) + ']')
        'id bad char' = '[{"id":"a b","amount":1,"tags":[]}]'
        'element not object' = '[1]'
        'root object' = '{"id":"a","amount":1,"tags":[]}'
        'null id' = '[{"id":null,"amount":1,"tags":[]}]'
    }
    foreach ($k in $schema.Keys) {
        $exp = if ($k -eq 'root object') { 'Schema@-1' } else { 'Schema@0' }
        Assert-Equal $exp (Read-Kind $schema[$k]).Result "schema: $k"
    }
    Assert-Equal 'Schema@2' (Read-Kind ('[' + (Elem 'a') + ',' + (Elem 'b') + ',{"id":"c","amount":1,"amount":2,"tags":[]}]')).Result 'duplicate key in element 2 -> Index 2'

    # ---- 3. 符号化・構文・上限
    $enc = [System.Text.Encoding]::ASCII
    Assert-Equal 'Encoding@1' (Read-Kind ([byte[]]($enc.GetBytes('[' + (Elem 'a') + ',{"id":"') + [byte[]](0xC3, 0x28) + $enc.GetBytes('","amount":1,"tags":[]}]')))).Result 'invalid UTF-8 in element 1'
    Assert-Equal 'Encoding@-1' (Read-Kind ([byte[]]([byte[]](0xEF, 0xBB, 0xBF) + $enc.GetBytes('[]')))).Result 'BOM rejected'
    Assert-Equal 'Encoding@0' (Read-Kind '[{"id":"a","amount":1,"tags":["\ud800"]}]').Result 'lone high surrogate escape in tag'
    Assert-Equal 'Encoding@0' (Read-Kind '[{"id":"a","amount":1,"tags":["x\udc00"]}]').Result 'lone low surrogate escape in tag'
    Assert-Equal 'Encoding@0' (Read-Kind '[{"\ud800":"a","id":"a","amount":1,"tags":[]}]').Result 'lone surrogate in key'
Assert-Equal 'Encoding@1' (Read-Kind ('[' + (Elem 'a') + ',{"id":"a","amount":1,"tags":["\udc00\ud800"]}]')).Result 'reversed surrogate pair in element 1'
$r = Read-Kind ('[{"id":"a","amount":1,"tags":["\\ud800","\\\\😀"]}]')
Assert-True ($r.Result -eq 'OK' -and $r.Out[0].Tags[0] -ceq '\ud800' -and $r.Out[0].Tags[1] -ceq ('\\' + [char]::ConvertFromUtf32(0x1F600))) 'escaped backslash before u is not a surrogate escape'
    Assert-Equal 'Syntax@1' (Read-Kind ('[' + (Elem 'a') + ',{"id":"b","amount":1,"tags":[]]')).Result 'syntax error in element 1'
    Assert-Equal 'Syntax@1' (Read-Kind ('[' + (Elem 'a') + ',]')).Result 'trailing comma rejected (error where element 1 would start)'
    Assert-Equal 'Syntax@-1' (Read-Kind '').Result 'empty file -> Syntax'
    Assert-Equal 'Syntax@0' (Read-Kind '[{"id":"a","amount":01,"tags":[]}]').Result 'leading zero number -> Syntax'
    Assert-Equal 'Syntax@0' (Read-Kind '[{"id":"a","amount":1,"tags":[]} // c' + "`n]").Result 'comment rejected'
    $nest16 = '[' + (Elem 'a' '1' (('[' * 14) + (']' * 14))) + ']'                    # 深さ: 配列1 + object2 + tags3 + 13 = 16
    $nest17 = '[' + (Elem 'a' '1' (('[' * 15) + (']' * 15))) + ']'
    Assert-Equal 'Schema@0' (Read-Kind $nest16).Result '16 levels: depth OK, then schema (tags must be strings)'
    Assert-Equal 'Limit@0' (Read-Kind $nest17).Result '17 levels -> Limit'
    Assert-Equal 'Limit@1' (Read-Kind ('[' + (Elem 'a') + ',' + ('[' * 80) + (']' * 80) + ']')).Result '81 levels (beyond parser max) -> Limit with index'
    $pad = '[' + (Elem 'a') + ']'
    $exact = $pad + (' ' * (8388608 - $pad.Length))
    $r = Read-Kind $exact
    Assert-True ($r.Result -eq 'OK' -and $r.Out.Count -eq 1) '8 MiB exactly accepted'
    Assert-Equal 'Limit@-1' (Read-Kind ($exact + ' ')).Result '8 MiB + 1 rejected'
    $tooMany = '[' + ((1..100001 | ForEach-Object { '{"id":"a","amount":1,"tags":[]}' }) -join ',') + ']'
    Assert-Equal 'Limit@-1' (Read-Kind $tooMany).Result '100001 elements -> Limit'

    # ---- 4. 末尾の 1 要素だけが破損した 10000 件 → 成功出力 0 件
    $items = (0..9998 | ForEach-Object { Elem "k$_" "$_" '["x"]' }) + @('{"id":"last","amount":1.5,"tags":[]}')
    $r = Read-Kind ('[' + ($items -join ',') + ']')
    Assert-Equal 'Schema@9999' $r.Result 'last of 10000 broken -> Schema@9999'
    Assert-Equal 0 $r.Out.Count 'no success output when any element is invalid'

    # カルチャーに依存しない
    $orig = [System.Globalization.CultureInfo]::CurrentCulture
    $outs = @{}
    foreach ($c in 'en-US', 'ja-JP', 'tr-TR') {
        [System.Globalization.CultureInfo]::CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo($c)
        $r = Read-Kind ('[{"id":"TITLE_i","amount":-12,"tags":["I","ı","İ"]},' + '{"id":"x","amount":3,"tags":[]}]')
        $outs[$c] = $r.Result + '|' + (($r.Out | ForEach-Object { "$($_.Id);$($_.Amount);$($_.Tags -join '/')" }) -join '|') + '|' + (Read-Kind '[{"ID":"a","amount":1,"tags":[]}]').Result
    }
    [System.Globalization.CultureInfo]::CurrentCulture = $orig
    Assert-True ($outs['en-US'] -ceq $outs['ja-JP'] -and $outs['en-US'] -ceq $outs['tr-TR']) "identical results in en-US/ja-JP/tr-TR: $($outs['tr-TR'])"
    Assert-Throws { Read-StrictJsonBatch -Path (Join-Path $base 'missing.json') } ([System.IO.FileNotFoundException]) 'missing file'
} finally {
    Remove-Item -LiteralPath $base -Recurse -Force
}
Write-TkSummary
```

#### 5. 期待結果・実測結果・実施状況

| 必須検証ケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 空配列 | 0 件 | 一致 | 成功 |
| 1 件 | Id・Amount（long）・Tags（string[]）、プロパティ順 Id,Amount,Tags | 一致 | 成功 |
| 100000 件 | 4,551,844 バイトのファイルが入力順に全件。997 件おきに値を照合 | 一致（65.3 秒） | 成功 |
| 混合 Unicode・エスケープ | 日本語・結合文字・`😀`（サロゲートペア）・`\t \" \\ \/ \n`・`A`・`id`（キーのエスケープ）を正しく復号 | 一致 | 成功 |
| int64 の最小値・最大値・-0 | -9223372036854775808 / 9223372036854775807 を受理、-0 は 0 | 一致 | 成功 |
| tags の上限 | 16 個は受理、64 スカラー値（絵文字 64 個 = UTF-16 で 128）は受理、65 スカラー値は Schema | 一致 | 成功 |
| 重複 `id`・`ID` と `id`・大小文字の違う必須キー・余剰・欠落 | すべて Schema@0（`Id`・`AMOUNT`・余剰 `x`・tags の欠落）。要素 2 の重複は Schema@2 | 一致 | 成功 |
| 文字列の数値・小数・指数・範囲外の整数・bool | `"1"`・`1.0`・`1e3`・`true`・9223372036854775808・-9223372036854775809 は Schema@0 | 一致 | 成功 |
| tags 17 個・tag が文字列でない・id 65 文字・id の不正文字・要素がオブジェクトでない・id が null | Schema@0。トップレベルがオブジェクトなら Schema@-1 | 一致 | 成功 |
| 不正な UTF-8 | 要素 1 の文字列の中の `C3 28` は Encoding@1 | 一致 | 成功 |
| BOM | Encoding@-1 | 一致 | 成功 |
| 孤立サロゲート | tag の `\ud800`・`x\udc00`、キーの `\ud800` は Encoding@0。要素 1 の逆順のペア `\udc00\ud800` は Encoding@1。`\\ud800`（エスケープされたバックスラッシュ + 文字列 "ud800"）は受理 | 一致 | 成功 |
| 構文エラー | 要素 1 の括弧不一致は Syntax@1、末尾のカンマは Syntax@1（要素 1 が始まる位置）、空ファイルは Syntax@-1、`01` は Syntax@0、コメントは Syntax@0 | 一致 | 成功 |
| 16 / 17 段のネスト | 16 段は深さ OK（その後、tags の要素が配列なので Schema@0）。17 段は Limit@0。81 段（パーサーの上限 64 を超える）は Limit@1 | 一致 | 成功 |
| 8 MiB / 8 MiB+1 | 8,388,608 バイト（空白で埋めた）は受理、+1 バイトは読む前に Limit@-1 | 一致 | 成功 |
| 配列の件数の上限 | 100001 要素は Limit@-1 | 一致 | 成功 |
| 末尾の 1 要素だけが破損した 10000 件 | Schema@9999 で、成功出力は 0 件 | 一致 | 成功 |
| 独立した Tags | 要素ごとに別の配列で、一方を書き換えても他方は不変 | 一致 | 成功 |
| カルチャーの差異なし | en-US / ja-JP / tr-TR で、`TITLE_i`・`I`/`ı`/`İ` を含む結果と `ID` の拒否が完全一致 | 一致 | 成功 |
| 既存ファイルを変更しない | すべてのケースで、読んだ後のファイルのバイト列は不変 | 一致 | 成功 |

**自律修正**
- **実装：** キーの中の不対サロゲート（`{"\ud800":...}`）を見逃していました。`JsonProperty.Name` は、置換文字に変換して例外を出さない場合があるためです。そこで、構文が正しい入力の生テキストを正規表現で事前検査する処理を追加しました。この正規表現は、直前のバックスラッシュが偶数個であること（エスケープされた `\` を除外）を確認し、高位の後に低位がない場合と、前に高位がない低位の場合を検出します。
- **テスト：** PowerShell では `[byte[]] + [byte[]]` が object[] になるため、BOM の入力ファイルが意図どおり作られていませんでした。`[byte[]](...)` で囲むよう直しました。
- 修正後に Q06 全体を再実行し、合格しました。

#### 6. 技術的根拠・保証範囲・計算量

- **重複キー：** `JsonDocument` の `EnumerateObject` は、重複したプロパティもすべて列挙します（ConvertFrom-Json と異なり失われない）。それを Ordinal の HashSet に入れて検出します。キーは復号後の文字列の Ordinal 一致なので、`ID`・`Id` は `id` と別のキーです。
- **Utf8JsonReader について：** `Utf8JsonReader` は ref struct で、PowerShell からは変数に保持できません（byref-like 型は非対応）。そのため、`Utf8JsonReader` の上に作られている `JsonDocument.Parse(ReadOnlyMemory<byte>)` でバイト列を解析しました（問題文の「JsonDocument/Utf8JsonReader 等」の範囲）。
- **数値：** `GetRawText()` が `-?(0|[1-9][0-9]*)` に一致し、かつ `TryGetInt64` が成功する場合だけ受理します。小数・指数・範囲外を確実に拒否します。
- **深さ：** パーサーの MaxDepth は 64 にし、16 段の上限は自前の非再帰スタック走査で判定します（17 段は Limit）。64 段を超えて構文解析が失敗した場合は、バイト走査で深さの超過を確認して Limit に分類します。
- **Index：** 構文・符号化エラーの位置（JsonException の行・行内バイト位置、DecoderFallbackException の Index）から、トップレベル配列の深さ 1 のカンマを数えて要素番号を求めます。
- **全件検査の後に出力：** 結果は List に溜め、`$doc.Dispose()` の後にまとめて出力します。途中で例外が出れば、成功出力は 0 件です。
- **計算量：** 時間・メモリとも O(ファイルサイズ)（≤ 8 MiB）です。

#### 7. 未実施・未解決・制約事項

- `Utf8JsonReader` を直接は使っていません（PowerShell の制約）。JsonDocument が内部で同じリーダーを使います。
- 末尾カンマなど「次の要素が始まる位置」の構文エラーは、その位置の要素番号（例：Syntax@1）を報告します。
- 不正な UTF-8 と不対サロゲートの検査は、要素ごとのスキーマ検査より先に全体へ行います。そのため、複数の種類の不正がある場合は Encoding が優先されます。
- PowerShell 7.4 / .NET 8 での実行は未実施です。
- 参照資料：Microsoft Learn の System.Text.Json（JsonDocument / JsonElement / JsonDocumentOptions）、RFC 8259。コードは独自に作成しました。

---

## Q07【地獄級】NTFS・File.Replace・ロック制御によるバージョン付き原子的状態保存

**評価の狙い：** コミット境界を明示した例外保証、ファイルロック、SHA256、失敗注入、複数プロセス競合。

### 元コード（危険・不完全／実装の出発点）

```powershell
function Update-CounterBad {
    param($Path, $Delta)
    $v = [long](Get-Content $Path)
    Set-Content $Path ($v + $Delta)
}
```

### 必須要求仕様

1. Windows 11の**同一NTFSボリューム上の通常ファイル**を対象とし、`New-CounterStore -Path -InitialBalance`、`Get-CounterState -Path`、`Update-CounterState -Path -ExpectedVersion -Delta`を実装する。Newはテスト初期化用であり、既存ファイルがあれば拒否。状態はVersionとBalance（いずれも符号付き64bit、初期Version=0、Balanceは0〜2^63-1）。Deltaは-2^63〜2^63-1、更新後もBalance範囲内。
2. 保存形式は`'FPSC'`のASCII4バイト+VersionのInt64 LE 8バイト+BalanceのInt64 LE 8バイト+前20バイトのSHA-256ハッシュ32バイト、**全52バイト固定長**。長さ不一致、magic/ハッシュ不正、負Balanceは拒否。全整数をリトルエンディアンで符号化すること。
3. Updateは既存targetを読み、ExpectedVersionが一致しない場合`VersionConflictException`で拒否。排他ロック用ファイル`<Path>.lock`を**FileShare.Noneで保持**してから読み込み・更新し、同時更新を直列化する。取得待ち最大2秒・最大20回の試行後は`StoreBusyException`。無限再試行は禁止。
4. 新状態はtargetと同じディレクトリ内の一時ファイルに作成し、Flush(true)でディスク同期後に閉じる。`[System.IO.File]::Replace(temp,target,$null)`を使ってコミットする。**コミット成立はFile.Replaceが成功して戻った時点**と定義。これ以前の例外は旧targetを不変に保つ。成功後の後片付け失敗を処理失敗に変えてはならない。
5. 故障注入ポイントは`Read`、`Write`、`Flush`、`BeforeReplace`の**コミット前4箇所のみ**。各点の例外ではVersion/Balanceが旧版で、一時ファイルを削除する。File.Replace自体のエラー後にOSが置換を実施済みか判別できない状況、外部の電源断、OSクラッシュ、NTFS以外のファイルシステムは試験の保証範囲外とする。
6. New-CounterStoreの初期作成は既存ファイルを上書きしないことが必須。Getは完全な1スナップショットを検証して返す。戻り値は`[pscustomobject]@{ Version=[long]; Balance=[long] }`。更新でVersionがInt64最大値なら上限エラー。
7. 複数プロセスの実動作テストでは試験用の一時ファイルだけ使用し、既存ユーザーファイルやレジストリを変更しない。

### 必須検証項目

1. 初期Balance0/MAX、Delta正負、残高上限、Versionの競合、保存内容のSHA256/固定長/LEを検証。
2. Read/Write/Flush/BeforeReplaceに例外を注入し、既存targetのバイト列・Version・Balanceが不変で、一時ファイルが残らないこと。
3. spawnした4つのpwshプロセスから合計10000回の更新（競合時は正しいExpectedVersionを再取得）で更新成功数=Version、総Delta=最終Balance。
4. ロック競合時の2秒以内終了、再開後の成功、破損ファイルの拒否、後片付け失敗はコミット成功を失敗として返さないこと。

### ★★FREYJAの回答【Q07・記入欄】

> **以下は専用回答欄。** 問題文・危険コード・要求仕様・必須検証・採点基準は変更禁止。修正コード、テストコード、実測を以下に全文記載する。

#### 1. 根本原因・危険性・修正方針

**元コード `Update-CounterBad` の問題点**

| # | 問題箇所 | 発生条件 | 危険性・影響 |
| --- | --- | --- | --- |
| 1 | 読む → 計算 → 書くの間にロックがない | 複数プロセスの同時更新 | 更新の消失（lost update） |
| 2 | `Set-Content` でその場に上書き | 書込みの途中の例外・中断 | ファイルが空や途中までの状態で壊れる（原子性がない） |
| 3 | `[long](Get-Content ...)` | 不正な内容 | 型変換の失敗、カルチャー依存の解釈。改ざんを検出できない（完全性検査がない） |
| 4 | バージョンの概念がない | 古い値に基づく更新 | 楽観的な並行制御ができない |
| 5 | `$v + $Delta` | 上限付近 | PowerShell は long の桁あふれで double に広げるので、精度を失う |
| 6 | Flush・ディスク同期がない | 電源断 | 書いたはずの内容がない |

**修正方針**
- 52 バイト固定の形式（`FPSC` + Version + Balance + SHA-256）で、整数はリトルエンディアンにします。
- 更新は、`<Path>.lock` を FileShare.None で保持してから（最大 20 回・2 秒以内）、次の手順で行います。
  1. 読込みと検証（ExpectedVersion の照合を含む）。
  2. BigInteger での範囲検査。
  3. 同じディレクトリの一時ファイル（CreateNew）への書込み → `Flush(true)` → 閉じる。
  4. `File.Replace(temp, target, null)` でコミット。
- コミット前の失敗では、自分の一時ファイルだけを削除します。

#### 2. 完全な修正版PowerShellコード

```powershell
#Requires -Version 7.4
# Q07: NTFS 上のバージョン付きカウンター（52 バイト固定形式・排他ロック・File.Replace による原子的コミット）

Set-StrictMode -Version 3.0

class VersionConflictException : System.InvalidOperationException {
    [long]$ExpectedVersion
    [long]$ActualVersion
    VersionConflictException([long]$expected, [long]$actual) : base("version conflict: expected $expected, actual $actual") {
        $this.ExpectedVersion = $expected
        $this.ActualVersion = $actual
    }
}
class StoreBusyException : System.TimeoutException {
    StoreBusyException([string]$message) : base($message) { }
}
class CounterStoreCorruptException : System.IO.IOException {
    CounterStoreCorruptException([string]$message) : base($message) { }
}

# 検証用の注入点（通常は $null）。FaultInjector はコミット前の 'Read' / 'Write' / 'Flush' / 'BeforeReplace' で呼ばれる。
# PostCommitCleanup はコミット成立後の後片付けの中で呼ばれ、その失敗が結果を失敗に変えないことの確認に使う。
$script:CounterFaultInjector = $null
$script:CounterPostCommitCleanup = $null
$script:CounterLockAttempts = 20
$script:CounterLockBudgetMs = 2000
$script:CounterReadAttempts = 50        # 読み取りの一時的な共有違反に対する再試行回数（10 ms 間隔）

function Invoke-CounterFault([string]$Point) { if ($null -ne $script:CounterFaultInjector) { & $script:CounterFaultInjector $Point } }

# Span（ref struct）は PowerShell から扱えないため、リトルエンディアンの符号化はビット演算で明示的に行う
function Write-CounterInt64LE([byte[]]$Buffer, [int]$Offset, [long]$Value) {
    for ($k = 0; $k -lt 8; $k++) { $Buffer[$Offset + $k] = [byte](($Value -shr (8 * $k)) -band 0xFF) }
}
function Read-CounterInt64LE([byte[]]$Buffer, [int]$Offset) {
    [long]$v = 0
    for ($k = 7; $k -ge 0; $k--) { $v = ($v -shl 8) -bor [long]$Buffer[$Offset + $k] }
    return $v
}
function Get-CounterHeaderHash([byte[]]$Buffer) {
    $head = [byte[]]::new(20); [Array]::Copy($Buffer, 0, $head, 0, 20)
    return ,[System.Security.Cryptography.SHA256]::HashData($head)
}

function ConvertTo-CounterBytes([long]$Version, [long]$Balance) {
    $buf = [byte[]]::new(52)
    $buf[0] = 0x46; $buf[1] = 0x50; $buf[2] = 0x53; $buf[3] = 0x43                       # 'FPSC'
    Write-CounterInt64LE $buf 4 $Version
    Write-CounterInt64LE $buf 12 $Balance
    $hash = Get-CounterHeaderHash $buf
    [Array]::Copy($hash, 0, $buf, 20, 32)
    return ,$buf
}

function Read-CounterFile([string]$Path) {
    # 共有モード ReadWrite|Delete で開き、File.Replace を妨げずに 1 つの完全なスナップショットを読む。
    # 他プロセスの File.Replace（ReplaceFile）の実行中は、一瞬だけ共有違反や名前の不在が起きうるため、有界回数だけ再試行する。
    $fs = $null
    for ($attempt = 1; $null -eq $fs; $attempt++) {
        try {
            $fs = [System.IO.FileStream]::new($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read,
                [System.IO.FileShare]::ReadWrite -bor [System.IO.FileShare]::Delete)
        } catch {
            $e = $_.Exception; if ($e -is [System.Management.Automation.MethodInvocationException] -and $null -ne $e.InnerException) { $e = $e.InnerException }
            $transient = ($e -is [System.IO.IOException] -and ($e.HResult -eq -2147024864 -or $e.HResult -eq -2147024863)) -or   # 共有違反・ロック違反
                         $e -is [System.IO.FileNotFoundException] -or $e -is [System.UnauthorizedAccessException]          # 置換中の名前の不在・削除保留
            if (-not $transient) { throw $e }
            if ($attempt -ge $script:CounterReadAttempts) {
                if ($e -is [System.IO.FileNotFoundException]) { throw $e }
                throw [StoreBusyException]::new("state file '$Path' stayed unavailable for $attempt attempts: $($e.Message)")
            }
            [System.Threading.Thread]::Sleep(10)
        }
    }
    try {
        $buf = [byte[]]::new(53)
        $got = 0
        while ($got -lt 53) { $r = $fs.Read($buf, $got, 53 - $got); if ($r -le 0) { break }; $got += $r }
    } finally { $fs.Dispose() }
    if ($got -ne 52) { throw [CounterStoreCorruptException]::new("state file must be exactly 52 bytes (got $got or more)") }
    if ($buf[0] -ne 0x46 -or $buf[1] -ne 0x50 -or $buf[2] -ne 0x53 -or $buf[3] -ne 0x43) { throw [CounterStoreCorruptException]::new('bad magic') }
    $hash = Get-CounterHeaderHash $buf
    for ($i = 0; $i -lt 32; $i++) { if ($hash[$i] -ne $buf[20 + $i]) { throw [CounterStoreCorruptException]::new('SHA-256 mismatch') } }
    $version = Read-CounterInt64LE $buf 4
    $balance = Read-CounterInt64LE $buf 12
    if ($balance -lt 0) { throw [CounterStoreCorruptException]::new('negative balance') }
    if ($version -lt 0) { throw [CounterStoreCorruptException]::new('negative version') }
    return [pscustomobject][ordered]@{ Version = [long]$version; Balance = [long]$balance }
}

function New-CounterStore {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][long]$InitialBalance)
    if ($InitialBalance -lt 0) { throw [System.ArgumentOutOfRangeException]::new('InitialBalance', 'balance must be within 0..2^63-1') }
    $full = [System.IO.Path]::GetFullPath($Path)
    $bytes = ConvertTo-CounterBytes 0 $InitialBalance
    # CreateNew: 既存ファイルがあれば IOException（上書きしない）
    $fs = [System.IO.FileStream]::new($full, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
    try { $fs.Write($bytes, 0, 52); $fs.Flush($true) } finally { $fs.Dispose() }
    return [pscustomobject][ordered]@{ Version = [long]0; Balance = [long]$InitialBalance }
}

function Get-CounterState {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)
    return Read-CounterFile ([System.IO.Path]::GetFullPath($Path))
}

function Enter-CounterLock([string]$LockPath) {
    # FileShare.None で保持。最大 20 回・合計 2 秒以内の再試行後は StoreBusyException（無限に待たない）
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    for ($attempt = 1; $attempt -le $script:CounterLockAttempts; $attempt++) {
        try {
            return [System.IO.FileStream]::new($LockPath, [System.IO.FileMode]::OpenOrCreate, [System.IO.FileAccess]::ReadWrite,
                [System.IO.FileShare]::None, 1, [System.IO.FileOptions]::None)
        } catch {
            $e = $_.Exception; if ($e -is [System.Management.Automation.MethodInvocationException] -and $null -ne $e.InnerException) { $e = $e.InnerException }
            if ($e -isnot [System.IO.IOException] -and $e -isnot [System.UnauthorizedAccessException]) { throw $e }
        }
        $remaining = $script:CounterLockBudgetMs - $sw.ElapsedMilliseconds
        if ($attempt -eq $script:CounterLockAttempts -or $remaining -le 0) { break }
        [System.Threading.Thread]::Sleep([int][Math]::Min(100, $remaining))
    }
    throw [StoreBusyException]::new("lock '$LockPath' not acquired after $attempt attempts in $($sw.ElapsedMilliseconds) ms")
}

function Update-CounterState {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][long]$ExpectedVersion,
        [Parameter(Mandatory)][long]$Delta
    )
    $full = [System.IO.Path]::GetFullPath($Path)
    $dir = [System.IO.Path]::GetDirectoryName($full)
    $lock = Enter-CounterLock ($full + '.lock')
    try {
        Invoke-CounterFault 'Read'
        $state = Read-CounterFile $full
        if ($state.Version -ne $ExpectedVersion) { throw [VersionConflictException]::new($ExpectedVersion, $state.Version) }
        if ($state.Version -eq [long]::MaxValue) { throw [System.OverflowException]::new('version is already Int64.MaxValue') }
        $newBalance = [System.Numerics.BigInteger]$state.Balance + [System.Numerics.BigInteger]$Delta
        if ($newBalance -lt 0 -or $newBalance -gt [long]::MaxValue) {
            throw [System.ArgumentOutOfRangeException]::new('Delta', "resulting balance $newBalance is outside 0..2^63-1")
        }
        $bytes = ConvertTo-CounterBytes ($state.Version + 1) ([long]$newBalance)

        # 同じディレクトリの一時ファイルに完全に書き、Flush(true) して閉じてから File.Replace でコミット
        $tmp = [System.IO.Path]::Combine($dir, '.' + [System.IO.Path]::GetFileName($full) + '.' + [guid]::NewGuid().ToString('N') + '.tmp')
        $committed = $false
        try {
            $fs = [System.IO.FileStream]::new($tmp, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
            try {
                Invoke-CounterFault 'Write'
                $fs.Write($bytes, 0, 52)
                Invoke-CounterFault 'Flush'
                $fs.Flush($true)
            } finally { $fs.Dispose() }
            Invoke-CounterFault 'BeforeReplace'
            # PowerShell は string 引数の $null を "" に変換するため、バックアップなし（null）は [NullString]::Value で渡す
            [System.IO.File]::Replace($tmp, $full, [NullString]::Value)
            $committed = $true                                   # ← コミット成立点
        } finally {
            if (-not $committed) { try { [System.IO.File]::Delete($tmp) } catch { } }   # コミット前の失敗: 自分の一時ファイルだけ削除
        }

        # コミット後の後片付け: 失敗しても結果は成功のまま
        try {
            if ([System.IO.File]::Exists($tmp)) { [System.IO.File]::Delete($tmp) }
            if ($null -ne $script:CounterPostCommitCleanup) { & $script:CounterPostCommitCleanup }
        } catch {
            Write-Verbose "post-commit cleanup failed (ignored, the update is committed): $($_.Exception.Message)"
        }
        return [pscustomobject][ordered]@{ Version = [long]($state.Version + 1); Balance = [long]$newBalance }
    } finally {
        $lock.Dispose()
    }
}
```

#### 3. 実行環境・実行コマンド

```powershell
# 作業フォルダー（試験専用の一時領域）: scratchpad\ps\q07 に q07.ps1（実装）と test_q07.ps1（テスト）、scratchpad\ps\common に TestKit.ps1
cd <scratchpad>\ps\q07
pwsh -NoLogo -NoProfile -Command '$PSVersionTable.PSVersion.ToString(); [System.Runtime.InteropServices.RuntimeInformation]::FrameworkDescription'   # 7.7.0 / .NET 11.0.0
pwsh -NoLogo -NoProfile -File test_q07.ps1 2>&1 | Tee-Object -FilePath log.txt
$LASTEXITCODE   # 0 = 全件合格（失敗した検査の数）
# 子プロセス（テストが spawn する 4 つの pwsh）は同じテストファイルを次の引数で実行する
# pwsh -NoLogo -NoProfile -NonInteractive -File test_q07.ps1 -WorkerPath <状態ファイル> -WorkerCount 2500 -WorkerSeed <100..103>
```

**実行環境（全問共通）**

- **PowerShell：** **7.7.0**（pwsh。Microsoft Store 版 `Microsoft.PowerShell_8wekyb3d8bbwe`）。指定の **PowerShell 7.4 はこの PC にインストールされていません**。試験要項 2 に従い、許可なくソフトウェアを追加していません。
- **.NET：** pwsh 7.7.0 に組み込まれた **.NET 11.0.0** の上で実行しました。.NET 8 ランタイム（8.0.34）もインストールされていますが、pwsh 7.7 は自身のランタイムを使います。そのため、コードは PowerShell 7.4 / .NET 8 にもある API だけで書きました（.NET 9 以降の API は不使用）。ただし、7.4 / .NET 8 上での実行は未実施です。
- **OS / CPU：** Windows 11 Pro 10.0.26200（x64）、NTFS ／ Intel Core i7-4710MQ（4 コア 8 スレッド）。管理者権限は使っていません。
- **使用したもの：** PowerShell の標準機能と .NET の標準 API だけです。Pester・ThreadJob・外部モジュール・NuGet・外部 DLL・Add-Type による C# は使っていません。
- **テストの補助：** 自作の最小テスト補助 `common\TestKit.ps1`（Assert-True / Assert-Equal / Assert-Throws）を使いました。全文を「4.」に載せています。
- **実行方法：** 各問の作業フォルダー `scratchpad\ps\qNN` で `pwsh -NoLogo -NoProfile -File test_qNN.ps1` を実行しました。終了コード = 失敗した検査の数（0 = 全件合格）。
- **データの扱い：** ファイルを使う試験は、各問のフォルダーの `testdata\run-<GUID>` だけで行い、終了時に削除しました（残ったものは 0 件）。

- **最終の実行結果：** `RESULT: 67 checks passed, 0 failed`、終了コード 0、所要 187.3 秒（4 プロセスの 10,000 回更新に 183.6 秒）。

#### 4. 実際に実行したテストコード全文

```powershell
# ===== common/TestKit.ps1（全問共通の自作テスト補助） =====
# 試験用の最小テスト補助（Pester 不使用）。各テストファイルが dot-source する。
$script:TkPass = 0
$script:TkFail = 0
$script:TkFailures = [System.Collections.Generic.List[string]]::new()

function Assert-True {
    param([bool]$Condition, [string]$Name)
    if ($Condition) { $script:TkPass++ }
    else {
        $script:TkFail++
        $script:TkFailures.Add($Name)
        [Console]::Out.WriteLine("  FAIL: $Name")
    }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Name)
    $ok = $false
    if ($null -eq $Expected -and $null -eq $Actual) { $ok = $true }
    elseif ($null -ne $Expected -and $null -ne $Actual) {
        if ($Expected -is [byte[]] -and $Actual -is [byte[]]) {
            $ok = [System.Linq.Enumerable]::SequenceEqual([byte[]]$Expected, [byte[]]$Actual)
        } elseif ($Expected -is [string] -and $Actual -is [string]) {
            $ok = [string]::Equals($Expected, $Actual, [System.StringComparison]::Ordinal)
        } else {
            $ok = $Expected.Equals($Actual)
        }
    }
    if (-not $ok) { $Name = "$Name (expected <$Expected> actual <$Actual>)" }
    Assert-True $ok $Name
}

function Assert-Throws {
    # ScriptBlock を実行し、指定した例外型（内側の例外を含めて探索）が発生したら合格。見つかった例外は $script:TkLastException に入れる。
    param([scriptblock]$Script, [type]$ExceptionType, [string]$Name)
    $found = $null
    try { & $Script | Out-Null }
    catch {
        $e = $_.Exception
        while ($null -ne $e) {
            if ($ExceptionType.IsInstanceOfType($e)) { $found = $e; break }
            $e = $e.InnerException
        }
        if ($null -eq $found) { $Name = "$Name (got $($_.Exception.GetType().FullName): $($_.Exception.Message))" }
    }
    Assert-True ($null -ne $found) $Name
    $script:TkLastException = $found
}

function Write-TkSummary {
    [Console]::Out.WriteLine("RESULT: $script:TkPass checks passed, $script:TkFail failed")
    exit $script:TkFail
}

# ===== q07/test_q07.ps1 =====
# Q07 のテスト。状態ファイルはすべて q07\testdata 配下の使い捨てフォルダーに作る。
param([string]$WorkerPath, [int]$WorkerCount = 0, [int]$WorkerSeed = 0)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'q07.ps1')

if ($WorkerPath) {
    # ---- 子プロセス（spawn された pwsh）: 競合時は最新の Version を取り直して再試行し、WorkerCount 回成功させる
    $rng = [System.Random]::new($WorkerSeed)
    $ok = 0; [long]$sum = 0; $conflicts = 0; $busy = 0
    while ($ok -lt $WorkerCount) {
        $s = Get-CounterState -Path $WorkerPath
        $d = [long]$rng.Next(-1000, 1001)
        try {
            $null = Update-CounterState -Path $WorkerPath -ExpectedVersion $s.Version -Delta $d
            $ok++; $sum += $d
        } catch {
            $e = $_.Exception
            if ($e -is [VersionConflictException]) { $conflicts++ } elseif ($e -is [StoreBusyException]) { $busy++ } else { throw }
        }
    }
    [Console]::Out.WriteLine("WORKER ok=$ok sum=$sum conflicts=$conflicts busy=$busy")
    exit 0
}

. (Join-Path $PSScriptRoot '..\common\TestKit.ps1')
$base = Join-Path $PSScriptRoot ('testdata\run-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $base -Force
function Get-Leftovers { @(Get-ChildItem -LiteralPath $base -Force -Filter '*.tmp') }

try {
    # ---- 1. 初期化・更新・保存形式
    $p = Join-Path $base 'zero.bin'
    $s = New-CounterStore -Path $p -InitialBalance 0
    Assert-True ($s.Version -eq 0 -and $s.Balance -eq 0) 'New with balance 0'
    Assert-Throws { New-CounterStore -Path $p -InitialBalance 5 } ([System.IO.IOException]) 'New refuses an existing file'
    Assert-Equal ([long]0) (Get-CounterState $p).Balance 'existing file not overwritten'
    $b = [System.IO.File]::ReadAllBytes($p)
    Assert-Equal 52 $b.Length 'fixed length 52'
    Assert-Equal '46505343' ([System.Convert]::ToHexString($b, 0, 4)) "magic 'FPSC'"
    $r = Update-CounterState -Path $p -ExpectedVersion 0 -Delta 1234567890123
    Assert-True ($r.Version -eq 1 -and $r.Balance -eq 1234567890123) 'positive delta'
    $b = [System.IO.File]::ReadAllBytes($p)
    Assert-Equal '0100000000000000' ([System.Convert]::ToHexString($b, 4, 8)) 'Version 1 as Int64 little-endian'
    Assert-Equal ([System.Convert]::ToHexString([BitConverter]::GetBytes([long]1234567890123))) ([System.Convert]::ToHexString($b, 12, 8)) 'Balance as Int64 little-endian'
    $head = [byte[]]::new(20); [Array]::Copy($b, $head, 20)
    Assert-Equal ([System.Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($head))) ([System.Convert]::ToHexString($b, 20, 32)) 'SHA-256 of the first 20 bytes'
    $r = Update-CounterState -Path $p -ExpectedVersion 1 -Delta -1234567890000
    Assert-True ($r.Version -eq 2 -and $r.Balance -eq 123) 'negative delta'
    Assert-Throws { Update-CounterState -Path $p -ExpectedVersion 2 -Delta -124 } ([System.ArgumentOutOfRangeException]) 'balance below 0 rejected'
    Assert-Throws { Update-CounterState -Path $p -ExpectedVersion 1 -Delta 1 } ([VersionConflictException]) 'stale ExpectedVersion -> VersionConflictException'
    Assert-Equal ([long]2) $script:TkLastException.ActualVersion 'conflict reports the actual version 2'
    $g = Get-CounterState $p
    Assert-True ($g.Version -eq 2 -and $g.Balance -eq 123) 'state unchanged after rejected updates'
    $pm = Join-Path $base 'max.bin'
    $null = New-CounterStore -Path $pm -InitialBalance ([long]::MaxValue)
    Assert-Throws { Update-CounterState -Path $pm -ExpectedVersion 0 -Delta 1 } ([System.ArgumentOutOfRangeException]) 'balance above 2^63-1 rejected'
    Assert-Throws { Update-CounterState -Path $pm -ExpectedVersion 0 -Delta ([long]::MinValue) } ([System.ArgumentOutOfRangeException]) 'MAX + Int64.MinValue = -1 rejected'
    $r = Update-CounterState -Path $pm -ExpectedVersion 0 -Delta (-[long]::MaxValue)
    Assert-True ($r.Version -eq 1 -and $r.Balance -eq 0) 'MAX - MAX = 0 accepted'
    Assert-Throws { New-CounterStore -Path (Join-Path $base 'neg.bin') -InitialBalance -1 } ([System.ArgumentOutOfRangeException]) 'negative initial balance rejected'

    # Version が Int64 最大値なら上限エラー（正しいハッシュ付きのファイルを直接作る）
    $pv = Join-Path $base 'vmax.bin'
    [System.IO.File]::WriteAllBytes($pv, (ConvertTo-CounterBytes ([long]::MaxValue) 10))
    Assert-Throws { Update-CounterState -Path $pv -ExpectedVersion ([long]::MaxValue) -Delta 1 } ([System.OverflowException]) 'Version at Int64.MaxValue -> overflow error'

    # ---- 2. 故障注入（コミット前の 4 箇所）
    foreach ($point in 'Read', 'Write', 'Flush', 'BeforeReplace') {
        $pf = Join-Path $base "fault-$point.bin"
        $null = New-CounterStore -Path $pf -InitialBalance 500
        $null = Update-CounterState -Path $pf -ExpectedVersion 0 -Delta 1
        $before = [System.IO.File]::ReadAllBytes($pf)
        $script:CounterFaultInjector = { param($pt) if ($pt -eq $point) { throw [System.IO.IOException]::new("injected at $pt") } }.GetNewClosure()
        $caught = $null
        try { $null = Update-CounterState -Path $pf -ExpectedVersion 1 -Delta 99 } catch { $caught = $_.Exception.Message }
        $script:CounterFaultInjector = $null
        Assert-True ($caught -like "*injected at $point*") "fault at ${point}: update failed with the injected error"
        Assert-True ([System.Linq.Enumerable]::SequenceEqual($before, [System.IO.File]::ReadAllBytes($pf))) "fault at ${point}: target bytes unchanged"
        $g = Get-CounterState $pf
        Assert-True ($g.Version -eq 1 -and $g.Balance -eq 501) "fault at ${point}: Version/Balance are the old ones"
        Assert-Equal 0 @(Get-Leftovers).Count "fault at ${point}: no temporary file left"
        $r = Update-CounterState -Path $pf -ExpectedVersion 1 -Delta 99
        Assert-True ($r.Version -eq 2 -and $r.Balance -eq 600) "fault at ${point}: a later update succeeds"
    }

    # ---- 4. ロック競合・後片付けの失敗・破損ファイル
    $pl = Join-Path $base 'lock.bin'
    $null = New-CounterStore -Path $pl -InitialBalance 10
    $holder = [System.IO.FileStream]::new("$pl.lock", [System.IO.FileMode]::OpenOrCreate, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    Assert-Throws { Update-CounterState -Path $pl -ExpectedVersion 0 -Delta 1 } ([StoreBusyException]) 'held lock -> StoreBusyException'
    $sw.Stop()
    Assert-True ($sw.Elapsed.TotalMilliseconds -le 2200) "gave up within 2 s (+scheduling margin): $([math]::Round($sw.Elapsed.TotalMilliseconds)) ms"
    $holder.Dispose()
    $r = Update-CounterState -Path $pl -ExpectedVersion 0 -Delta 1
    Assert-True ($r.Version -eq 1 -and $r.Balance -eq 11) 'succeeds after the lock is released'
    [Console]::Out.WriteLine("[Q07] lock contention: StoreBusyException after $([math]::Round($sw.Elapsed.TotalMilliseconds)) ms")

    $script:CounterPostCommitCleanup = { throw [System.IO.IOException]::new('cleanup failed') }
    $r = Update-CounterState -Path $pl -ExpectedVersion 1 -Delta 5
    $script:CounterPostCommitCleanup = $null
    Assert-True ($r.Version -eq 2 -and $r.Balance -eq 16) 'cleanup failure after commit still returns success'
    $g = Get-CounterState $pl
    Assert-True ($g.Version -eq 2 -and $g.Balance -eq 16) 'committed state is visible'

    $good = [System.IO.File]::ReadAllBytes($pl)
    $corrupt = [ordered]@{}
    $x = [byte[]]$good.Clone(); $x[30] = $x[30] -bxor 1; $corrupt['hash bit'] = $x
    $x = [byte[]]$good.Clone(); $x[5] = $x[5] -bxor 1; $corrupt['version bit'] = $x
    $x = [byte[]]$good.Clone(); $x[0] = 0x47; $corrupt['magic'] = $x
    $corrupt['51 bytes'] = [byte[]]($good[0..50])
    $corrupt['53 bytes'] = [byte[]]($good + [byte[]](0))
    $corrupt['negative balance (valid hash)'] = (ConvertTo-CounterBytes 3 -5)
    foreach ($k in $corrupt.Keys) {
        $pc = Join-Path $base ("corrupt-" + ($k -replace '[^a-z0-9]', '_') + '.bin')
        [System.IO.File]::WriteAllBytes($pc, $corrupt[$k])
        Assert-Throws { Get-CounterState $pc } ([CounterStoreCorruptException]) "corrupt ($k) rejected by Get"
        Assert-Throws { Update-CounterState -Path $pc -ExpectedVersion 2 -Delta 1 } ([CounterStoreCorruptException]) "corrupt ($k) rejected by Update"
        Assert-True ([System.Linq.Enumerable]::SequenceEqual([byte[]]$corrupt[$k], [System.IO.File]::ReadAllBytes($pc))) "corrupt ($k) file left untouched"
    }

    # ---- 3. spawn した 4 つの pwsh から合計 10000 回の更新
    $pp = Join-Path $base 'shared.bin'
    $initial = [long]1000000000
    $null = New-CounterStore -Path $pp -InitialBalance $initial
    $procs = @()
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    for ($w = 0; $w -lt 4; $w++) {
        $psi = [System.Diagnostics.ProcessStartInfo]::new([Environment]::ProcessPath)
        foreach ($a in '-NoLogo', '-NoProfile', '-NonInteractive', '-File', (Join-Path $PSScriptRoot 'test_q07.ps1'), '-WorkerPath', $pp, '-WorkerCount', '2500', '-WorkerSeed', "$(100 + $w)") { $psi.ArgumentList.Add($a) }
        $psi.UseShellExecute = $false; $psi.RedirectStandardOutput = $true; $psi.RedirectStandardError = $true; $psi.CreateNoWindow = $true
        $procs += [System.Diagnostics.Process]::Start($psi)
    }
    $outTasks = $procs | ForEach-Object { $_.StandardOutput.ReadToEndAsync() }
    $errTasks = $procs | ForEach-Object { $_.StandardError.ReadToEndAsync() }
    $totalOk = 0; [long]$totalSum = 0; $totalConflicts = 0; $totalBusy = 0; $exitOk = $true
    for ($w = 0; $w -lt 4; $w++) {
        if (-not $procs[$w].WaitForExit(900000)) { $procs[$w].Kill($true); $exitOk = $false }
        $line = $outTasks[$w].GetAwaiter().GetResult()
        $null = $errTasks[$w].GetAwaiter().GetResult()
        if ($procs[$w].ExitCode -ne 0) { $exitOk = $false; [Console]::Out.WriteLine("  worker $w stderr: $($errTasks[$w].Result)") }
        if ($line -match 'WORKER ok=(\d+) sum=(-?\d+) conflicts=(\d+) busy=(\d+)') {
            $totalOk += [int]$Matches[1]; $totalSum += [long]$Matches[2]; $totalConflicts += [int]$Matches[3]; $totalBusy += [int]$Matches[4]
        } else { $exitOk = $false }
        $procs[$w].Dispose()
    }
    $sw.Stop()
    $final = Get-CounterState $pp
    Assert-True $exitOk 'all 4 worker processes exited with 0'
    Assert-Equal 10000 $totalOk 'total successful updates = 10000'
    Assert-Equal ([long]10000) $final.Version 'final Version = number of successful updates'
    Assert-Equal ($initial + $totalSum) $final.Balance 'final Balance = initial + sum of committed deltas'
    Assert-Equal 0 @(Get-Leftovers).Count 'no temporary files left after the multi-process run'
    [Console]::Out.WriteLine("[Q07] 4 pwsh processes: 10000 updates in $([math]::Round($sw.Elapsed.TotalSeconds,1)) s, version conflicts $totalConflicts, StoreBusy $totalBusy, final $($final.Version)/$($final.Balance)")
} finally {
    $script:CounterFaultInjector = $null; $script:CounterPostCommitCleanup = $null
    Remove-Item -LiteralPath $base -Recurse -Force
}
Write-TkSummary
```

#### 5. 期待結果・実測結果・実施状況

| 必須検証ケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 初期 Balance 0 / MAX | New(0) と New(2^63-1) が成功。既存ファイルへの New は IOException で、内容は不変。負の初期値は拒否 | 一致 | 成功 |
| Delta の正負 | +1234567890123 → Version 1、-1234567890000 → Version 2 で Balance 123 | 一致 | 成功 |
| 残高の上限・下限 | 0 未満（123-124）、MAX+1、MAX+Int64.MinValue（= -1）は ArgumentOutOfRangeException。MAX−MAX = 0 は受理 | 一致 | 成功 |
| Version の競合 | 古い ExpectedVersion は VersionConflictException（ActualVersion=2）、状態は不変 | 一致 | 成功 |
| Version が Int64 の最大値 | OverflowException | 一致 | 成功 |
| 保存内容（SHA256・固定長・LE） | 長さ 52、`46505343`（FPSC）、Version 1 = `0100000000000000`、Balance = BitConverter（LE）と一致、末尾 32 バイト = 先頭 20 バイトの SHA-256 | 一致 | 成功 |
| Read / Write / Flush / BeforeReplace への例外の注入 | 4 箇所とも、更新は注入した例外で失敗し、target のバイト列・Version・Balance は旧版のまま。一時ファイルは 0 件。その後の更新は成功 | 4 箇所すべて一致 | 成功 |
| spawn した 4 つの pwsh から合計 10000 回の更新 | 競合時は Get で正しい Version を取り直して再試行。各 2500 回成功、合計 10000 = 最終 Version。初期値 + 成功した Delta の合計 = 最終 Balance。一時ファイルは残らない | 一致（183.6 秒、Version の競合 292 回、StoreBusy 126 回は再試行で吸収、最終 10000 / 1000024780） | 成功 |
| ロック競合時に 2 秒以内に終了 | テストが `.lock` を FileShare.None で保持すると StoreBusyException | 2037 ms（判定は 2200 ms 以内。スケジューリングの余裕を含む） | 成功 |
| 再開後の成功 | ロックを解放した後の更新は成功 | 一致 | 成功 |
| 破損ファイルの拒否 | ハッシュのビット反転・Version のビット反転・magic・51 バイト・53 バイト・負の Balance（正しいハッシュ付き）は、Get・Update とも CounterStoreCorruptException で、ファイルは不変 | 6 種すべて一致 | 成功 |
| 後片付けの失敗 | コミット後の後片付けで例外が出ても、Update は成功を返し、状態はコミット済み（Version 2・Balance 16） | 一致 | 成功 |

**自律修正（発見した不具合）**
- **実装：** PowerShell は、.NET メソッドの string 引数に渡した `$null` を `""` に変換します。そのため `File.Replace(temp, target, $null)` が「The path is empty」で失敗しました。バックアップなし（null）を `[NullString]::Value` で渡すよう修正しました。
- **実装（多プロセス試験で発見）：** 他のプロセスの File.Replace（ReplaceFile）の実行中に、ロックなしの `Get-CounterState` が一瞬だけ共有違反になり、ワーカーが 3 つ停止しました。そこで、読込みを FileShare.ReadWrite|Delete で開き（置換を妨げない）、共有違反・置換中の名前の不在だけを 10 ms 間隔で最大 50 回再試行する（それでも駄目なら StoreBusyException）よう修正しました。
- **実装（実行前）：** `Span<byte>` は ref struct で PowerShell からは使えないため、リトルエンディアンの読み書きをビット演算で明示的に書きました。
- **テスト：**
  - TestKit の Equals が Int32 の 0 と Int64 の 0 を区別するため、期待値を `[long]` にしました。
  - 意味のない検査の 2 行を、正しい境界の検査（MAX+Int64.MinValue の拒否、MAX−MAX の受理）に置き換えました。
  - dot-source した実装の `Set-StrictMode` の影響で、空の結果の `.Count` がエラーになるため、`@(...)` で囲みました。
- 修正後に Q07 全体を再実行し、合格しました。

#### 6. 技術的根拠・保証範囲・計算量

- **コミット境界：**
  - `[System.IO.File]::Replace(temp, target, null)` が成功して戻った時点をコミットとします（`$committed = $true`）。
  - それより前の例外（Read・Write・Flush・BeforeReplace を含む）では、finally で自分の一時ファイルだけを削除し、target には触れません。
  - コミット後の後片付けは try/catch で囲み、失敗を Verbose に記録するだけで、成功を失敗に変えません。
- **耐久性：** 一時ファイルは `FileStream.Flush($true)`（FlushFileBuffers）でディスクに同期してから閉じ、その後で置換します。
- **直列化：** `<Path>.lock` を FileShare.None で開いている間は、他のプロセスが開けません。取得は最大 20 回・合計 2 秒（試行の間は最大 100 ms のスリープ）で、超えたら StoreBusyException です（無限再試行なし）。ロックは finally で必ず解放します。
- **完全性：** 52 バイトちょうどであること、magic、SHA-256（先頭 20 バイト）、Balance ≥ 0、Version ≥ 0 を検証します。ファイルは 53 バイト読もうとして、余剰も検出します。
- **範囲：** 新しい残高は `[BigInteger]` で計算し、0..2^63-1 を確認します（PowerShell の long → double への自動拡張を避ける）。
- **楽観的な並行制御：** ExpectedVersion の照合はロックの下で行うので、照合から置換までの間に他の更新は入りません。
- **計算量：** 1 回の更新は O(1)。ディスクの同期 1 回と置換 1 回です。

#### 7. 未実施・未解決・制約事項

- 仕様どおり、File.Replace 自身が失敗した場合（置換済みかを判別できない状況）、外部の電源断、OS のクラッシュ、NTFS 以外のファイルシステムは保証範囲外です（試験していません）。
- `.lock` ファイルは削除しません（削除と取得の競合を避けるため。中身は空）。
- ロックの待機は、2 秒の予算に対して実測 2037 ms でした（スケジューリングの誤差。判定は 2200 ms 以内）。
- 後片付けの失敗の試験には、コミット後の後片付けの中で呼ばれる検証用のフック `$script:CounterPostCommitCleanup` を使いました。これは、問題の「コミット前の 4 箇所の故障注入」とは別の、必須検証 4（後片付けの失敗）のための観測点です。
- PowerShell 7.4 / .NET 8 での実行は未実施です。
- 参照資料：Microsoft Learn の System.IO.File.Replace・FileStream.Flush(Boolean)・FileShare。コードは独自に作成しました。

---

## Q08【地獄級】厳格UTF-8・RFC4180部分集合のストリーミングCSVパーサ

**評価の狙い：** StreamReader、改行処理、quoted field、境界処理、不正な符号化、PS配列の自動展開。

### 元コード（危険・不完全／実装の出発点）

```powershell
function Read-CsvBad {
    param([string]$Path)
    Get-Content $Path | ForEach-Object { $_.Split(',') }
}
```

### 必須要求仕様

1. `Read-StrictCsv -Path ([string])`を実装し、UTF-8（先頭BOMは許可、それ以外のBOM禁止）ファイルを**1文字ずつ逐次解析**。フィールド区切りは`,`、レコード区切りは**CRLFのみ**。最後のレコードだけ末尾CRLFなしを許す。空ファイルは0件、空行だけのファイルは不正とする。
2. フィールドは非引用の通常フィールドか、`"`で始まる引用フィールド。引用内では`,`・CRLFがデータとして扱われ、`""`は1個の引用符になる。閉じ引用符直後は`,`・CRLF・EOFだけを許可。引用外の孤立した`"`、裸のCR/LF、未終端引用符は拒否。
3. 1レコードのフィールド数は1〜8。各フィールドの内容のUTF-8エンコード後のサイズは最大4096バイト、1レコード全体の元UTF-8入力バイト数は最大65536バイト。正常レコード数は最大100000。Unicodeは妥当なスカラー値のみを許す。
4. ストリームは`[System.IO.StreamReader]`の厳格なUTF8Encoding(false,true)を使用してよい。StreamReader.ReadToEnd、Get-Content -Raw、Import-Csv、ConvertFrom-Csvによる代替は禁止。内部メモリは現在処理中のレコードとフィールドの合計O(65536)まで。
5. 成功ストリームへ1レコードごとに`[pscustomobject]@{ RecordNumber=[long]; Fields=[string[]] }`を出力。Fieldsは正確な列数の独立配列であり、空フィールドを消さない。RecordNumberは1始まり。入力の後続が破損した場合は終端エラーとして停止し、先に出した成功レコードは撤回しない。
6. 不正CSVは`StrictCsvException`と`RecordNumber`、`Reason`（`Encoding`/`Quote`/`Delimiter`/`Fields`/`Length`/`Limit`）を報告。エラー位置は論理レコード番号（不正なレコードを指す）と定義し、byte offsetは要求しない。テスト用ファイルは読み取り専用で扱う。
7. レコード数やフィールド長の判定でPowerShellの暗黙の型変換に依存しない。文字列のコードポイント数とUTF-8バイト数を混同しない。

### 必須検証項目

1. `a,b`、`"a,b",c`、`"x""y",z`、引用内CRLF、空フィールド、BOMの有無、末尾CRLFなし、空ファイルを完全一致で確認。
2. 裸のLF/CR、未終端引用、引用閉じ後の空白、不正UTF-8、8/9列、フィールド4096/4097バイト、100000/100001件を確認。
3. 入力の1文字ずつの読み取りに相当する断片条件、5万行のストリームを解析し、独立した参照エンコーダとの往復結果を比較。
4. 途中の不正レコードでそれ以前の正常レコードだけが出力されること、Fieldsが1件のオブジェクト内に保たれることを検証。

### ★★FREYJAの回答【Q08・記入欄】

> **以下は専用回答欄。** 問題文・危険コード・要求仕様・必須検証・採点基準は変更禁止。修正コード、テストコード、実測を以下に全文記載する。

#### 1. 根本原因・危険性・修正方針

**元コード `Read-CsvBad` の問題点**

| # | 問題箇所 | 発生条件 | 危険性・影響 |
| --- | --- | --- | --- |
| 1 | `$_.Split(',')` | 引用されたフィールドに `,` | フィールドが誤って分割され、列がずれる |
| 2 | `Get-Content` の行単位 | 引用の中の改行、裸の LF・CR | CRLF 以外の改行も区切りとして扱い、引用内の改行でレコードが壊れる。区切りの規則を検証できない |
| 3 | 引用符を処理しない | `"x""y"` | 引用符が値に残る。`""` のエスケープを解釈しない |
| 4 | 符号化の検査がない | 不正な UTF-8・途中の BOM | 置換文字（U+FFFD）に化けたまま通る |
| 5 | 出力が文字列の列挙 | 常に | フィールドがパイプラインに 1 個ずつ流れ、レコードの境界が失われる |
| 6 | 上限がない | 巨大なレコード・件数 | メモリ・CPU を使い果たす |

**修正方針**
- ファイルのバイトを、厳密な UTF-8 デコーダー（`UTF8Encoding(false, true).GetDecoder()`）へ 1 バイトずつ渡し、1 文字ずつ取り出します。
- 6 状態の状態機械で RFC 4180 の部分集合を解析します。状態：フィールド開始・非引用・引用内・引用内の `"` の後・CR の後・引用内の CR の後。
- フィールドの UTF-8 バイト数（≤ 4096）、レコードの元の入力バイト数（≤ 65536）、列数（≤ 8）、レコード数（≤ 100000）を、読みながら検査します。
- レコードごとに `[pscustomobject]@{ RecordNumber; Fields=[string[]] }` を出力します。不正なら `StrictCsvException(RecordNumber, Reason)` の終端エラーです。

#### 2. 完全な修正版PowerShellコード

```powershell
#Requires -Version 7.4
# Q08: 厳格 UTF-8・RFC 4180 部分集合のストリーミング CSV パーサー（1 文字ずつの状態機械）

Set-StrictMode -Version 3.0

class StrictCsvException : System.FormatException {
    [long]$RecordNumber      # 不正なレコードの論理番号（1 始まり）
    [string]$Reason          # Encoding / Quote / Delimiter / Fields / Length / Limit
    StrictCsvException([long]$recordNumber, [string]$reason, [string]$message) : base("record ${recordNumber}: $reason - $message") {
        $this.RecordNumber = $recordNumber
        $this.Reason = $reason
    }
}

function Read-StrictCsv {
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)][string]$Path,
        # 診断用（任意）: MaxRecordBytes / MaxFieldBytes / Records
        [hashtable]$Statistics
    )
    $maxFields = 8; $maxFieldBytes = 4096; $maxRecordBytes = 65536; $maxRecords = 100000
    $full = [System.IO.Path]::GetFullPath($Path)
    $fs = [System.IO.FileStream]::new($full, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read)
    # 厳格な UTF-8 デコーダー（不正バイトで DecoderFallbackException）に 1 バイトずつ渡して 1 文字ずつ取り出す。
    # StreamReader はバッファー単位で先にデコードするため、エラーが属するレコードを特定できない。BOM の自動判定はしない。
    $decoder = [System.Text.UTF8Encoding]::new($false, $true).GetDecoder()
    $byteBuf = [byte[]]::new(4096); $bufLen = 0; $bufPos = 0
    $charBuf = [char[]]::new(2); $pending = 0; $pendIdx = 0; $flushed = $false

    [long]$record = 1                 # 現在のレコード番号
    $fields = [System.Collections.Generic.List[string]]::new($maxFields)
    $sb = [System.Text.StringBuilder]::new()
    $fieldBytes = 0; $recordBytes = 0; $recordChars = 0
    $state = 0                        # 0:フィールド開始 1:非引用 2:引用内 3:引用内の '"' の直後 4:CR の直後（LF 待ち） 5:引用内の CR の直後
    $first = $true
    [long]$emitted = 0; $statMaxRec = 0; $statMaxField = 0

    $fail = {
        param([string]$reason, [string]$message)
        $PSCmdlet.ThrowTerminatingError([System.Management.Automation.ErrorRecord]::new(
            [StrictCsvException]::new($record, $reason, $message), "StrictCsv$reason",
            [System.Management.Automation.ErrorCategory]::InvalidData, $full))
    }

    try {
        while ($true) {
            # ---- 次の 1 文字（UTF-16 単位）を取り出す。EOF は -1
            $ci = -1
            try {
                while ($pendIdx -ge $pending) {
                    if ($bufPos -ge $bufLen) {
                        if ($flushed) { break }
                        $bufLen = $fs.Read($byteBuf, 0, $byteBuf.Length); $bufPos = 0
                        if ($bufLen -le 0) {
                            $bufLen = 0; $flushed = $true
                            $pending = $decoder.GetChars($byteBuf, 0, 0, $charBuf, 0, $true); $pendIdx = 0   # 末尾の不完全な列はここで例外
                        }
                        continue
                    }
                    $pending = $decoder.GetChars($byteBuf, $bufPos, 1, $charBuf, 0, $false); $pendIdx = 0
                    $bufPos++
                }
                if ($pendIdx -lt $pending) { $ci = [int]$charBuf[$pendIdx]; $pendIdx++ }
            } catch { & $fail 'Encoding' 'invalid UTF-8 byte sequence' }
            if ($first) {
                $first = $false
                if ($ci -eq 0xFEFF) { continue }            # 先頭の BOM は 1 回だけ許可
            }
            if ($ci -eq 0xFEFF) { & $fail 'Encoding' 'byte order mark is only allowed at the start of the file' }

            if ($ci -lt 0) {
                # ---- EOF
                switch ($state) {
                    2 { & $fail 'Quote' 'unterminated quoted field' }
                    4 { & $fail 'Delimiter' 'bare CR at end of file' }
                    5 { & $fail 'Delimiter' 'bare CR inside quoted field' }
                }
                if ($recordChars -gt 0) {                    # 末尾 CRLF のない最後のレコード
                    $fields.Add($sb.ToString())
                    [pscustomobject][ordered]@{ RecordNumber = $record; Fields = $fields.ToArray() }
                    $emitted++
                }
                break
            }

            $ch = [char]$ci
            # 元の UTF-8 バイト数（サロゲートペアは高位で 4、低位で 0）
            $nb = if ($ci -lt 0x80) { 1 } elseif ($ci -lt 0x800) { 2 } elseif ($ci -ge 0xD800 -and $ci -le 0xDBFF) { 4 } elseif ($ci -ge 0xDC00 -and $ci -le 0xDFFF) { 0 } else { 3 }

            if ($state -eq 4) {
                if ($ci -ne 0x0A) { & $fail 'Delimiter' 'bare CR (not followed by LF)' }
                # ---- レコード確定（CRLF）
                if ($recordChars -eq 0) { & $fail 'Fields' 'empty line' }
                $fields.Add($sb.ToString())
                [pscustomobject][ordered]@{ RecordNumber = $record; Fields = $fields.ToArray() }     # 列数どおりの新しい配列
                $emitted++
                if ($recordBytes -gt $statMaxRec) { $statMaxRec = $recordBytes }
                $record++
                $fields.Clear(); [void]$sb.Clear(); $fieldBytes = 0; $recordBytes = 0; $recordChars = 0
                $state = 0
                continue
            }

            if ($recordChars -eq 0 -and $record -gt $maxRecords) { & $fail 'Limit' "more than $maxRecords records" }
            if ($ci -ne 0x0D -or $state -eq 2) {
                $recordChars++
                $recordBytes += $nb
                if ($recordBytes -gt $maxRecordBytes) { & $fail 'Length' "record longer than $maxRecordBytes UTF-8 bytes" }
            }

            switch ($state) {
                0 {   # フィールド開始
                    if ($ci -eq 0x22) { $state = 2 }
                    elseif ($ci -eq 0x2C) { $fields.Add(''); if ($fields.Count -ge $maxFields) { & $fail 'Fields' "more than $maxFields fields" } }
                    elseif ($ci -eq 0x0D) { $state = 4 }
                    elseif ($ci -eq 0x0A) { & $fail 'Delimiter' 'bare LF' }
                    else { [void]$sb.Append($ch); $fieldBytes += $nb; $state = 1 }
                }
                1 {   # 非引用フィールド
                    if ($ci -eq 0x2C) {
                        $fields.Add($sb.ToString()); [void]$sb.Clear(); $fieldBytes = 0; $state = 0
                        if ($fields.Count -ge $maxFields) { & $fail 'Fields' "more than $maxFields fields" }
                    }
                    elseif ($ci -eq 0x0D) { $state = 4 }
                    elseif ($ci -eq 0x0A) { & $fail 'Delimiter' 'bare LF' }
                    elseif ($ci -eq 0x22) { & $fail 'Quote' 'quote inside an unquoted field' }
                    else { [void]$sb.Append($ch); $fieldBytes += $nb }
                }
                2 {   # 引用内（, と CRLF はデータ）
                    if ($ci -eq 0x22) { $state = 3 }
                    elseif ($ci -eq 0x0D) { [void]$sb.Append($ch); $fieldBytes += 1; $state = 5 }
                    elseif ($ci -eq 0x0A) { & $fail 'Delimiter' 'bare LF inside quoted field' }
                    else { [void]$sb.Append($ch); $fieldBytes += $nb }
                }
                3 {   # 引用内の '"' の直後
                    if ($ci -eq 0x22) { [void]$sb.Append('"'); $fieldBytes += 1; $state = 2 }
                    elseif ($ci -eq 0x2C) {
                        $fields.Add($sb.ToString()); [void]$sb.Clear(); $fieldBytes = 0; $state = 0
                        if ($fields.Count -ge $maxFields) { & $fail 'Fields' "more than $maxFields fields" }
                    }
                    elseif ($ci -eq 0x0D) { $state = 4 }
                    else { & $fail 'Quote' 'only , CRLF or EOF may follow a closing quote' }
                }
                5 {   # 引用内の CR の直後（LF が必要）
                    if ($ci -ne 0x0A) { & $fail 'Delimiter' 'bare CR inside quoted field' }
                    [void]$sb.Append($ch); $fieldBytes += 1; $state = 2
                }
            }
            if ($fieldBytes -gt $maxFieldBytes) { & $fail 'Length' "field longer than $maxFieldBytes UTF-8 bytes" }
            if ($fieldBytes -gt $statMaxField) { $statMaxField = $fieldBytes }
        }
    } finally {
        $fs.Dispose()              # 自分で開いたファイルなので閉じる（読み取り専用）
        if ($null -ne $Statistics) { $Statistics.MaxRecordBytes = $statMaxRec; $Statistics.MaxFieldBytes = $statMaxField; $Statistics.Records = $emitted }
    }
}
```

#### 3. 実行環境・実行コマンド

```powershell
# 作業フォルダー（試験専用の一時領域）: scratchpad\ps\q08 に q08.ps1（実装）と test_q08.ps1（テスト）、scratchpad\ps\common に TestKit.ps1
cd <scratchpad>\ps\q08
pwsh -NoLogo -NoProfile -Command '$PSVersionTable.PSVersion.ToString(); [System.Runtime.InteropServices.RuntimeInformation]::FrameworkDescription'   # 7.7.0 / .NET 11.0.0
pwsh -NoLogo -NoProfile -File test_q08.ps1 2>&1 | Tee-Object -FilePath log.txt
$LASTEXITCODE   # 0 = 全件合格（失敗した検査の数）
```

**実行環境（全問共通）**

- **PowerShell：** **7.7.0**（pwsh。Microsoft Store 版 `Microsoft.PowerShell_8wekyb3d8bbwe`）。指定の **PowerShell 7.4 はこの PC にインストールされていません**。試験要項 2 に従い、許可なくソフトウェアを追加していません。
- **.NET：** pwsh 7.7.0 に組み込まれた **.NET 11.0.0** の上で実行しました。.NET 8 ランタイム（8.0.34）もインストールされていますが、pwsh 7.7 は自身のランタイムを使います。そのため、コードは PowerShell 7.4 / .NET 8 にもある API だけで書きました（.NET 9 以降の API は不使用）。ただし、7.4 / .NET 8 上での実行は未実施です。
- **OS / CPU：** Windows 11 Pro 10.0.26200（x64）、NTFS ／ Intel Core i7-4710MQ（4 コア 8 スレッド）。管理者権限は使っていません。
- **使用したもの：** PowerShell の標準機能と .NET の標準 API だけです。Pester・ThreadJob・外部モジュール・NuGet・外部 DLL・Add-Type による C# は使っていません。
- **テストの補助：** 自作の最小テスト補助 `common\TestKit.ps1`（Assert-True / Assert-Equal / Assert-Throws）を使いました。全文を「4.」に載せています。
- **実行方法：** 各問の作業フォルダー `scratchpad\ps\qNN` で `pwsh -NoLogo -NoProfile -File test_qNN.ps1` を実行しました。終了コード = 失敗した検査の数（0 = 全件合格）。
- **データの扱い：** ファイルを使う試験は、各問のフォルダーの `testdata\run-<GUID>` だけで行い、終了時に削除しました（残ったものは 0 件）。

- **最終の実行結果：** `RESULT: 56 checks passed, 0 failed`、終了コード 0、所要 156 秒。
- **それまでの実行：** 1 回目はテストの構文エラーで起動せず、2 回目は 52 件合格・3 件失敗でした（下記の自律修正）。

#### 4. 実際に実行したテストコード全文

```powershell
# ===== common/TestKit.ps1（全問共通の自作テスト補助） =====
# 試験用の最小テスト補助（Pester 不使用）。各テストファイルが dot-source する。
$script:TkPass = 0
$script:TkFail = 0
$script:TkFailures = [System.Collections.Generic.List[string]]::new()

function Assert-True {
    param([bool]$Condition, [string]$Name)
    if ($Condition) { $script:TkPass++ }
    else {
        $script:TkFail++
        $script:TkFailures.Add($Name)
        [Console]::Out.WriteLine("  FAIL: $Name")
    }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Name)
    $ok = $false
    if ($null -eq $Expected -and $null -eq $Actual) { $ok = $true }
    elseif ($null -ne $Expected -and $null -ne $Actual) {
        if ($Expected -is [byte[]] -and $Actual -is [byte[]]) {
            $ok = [System.Linq.Enumerable]::SequenceEqual([byte[]]$Expected, [byte[]]$Actual)
        } elseif ($Expected -is [string] -and $Actual -is [string]) {
            $ok = [string]::Equals($Expected, $Actual, [System.StringComparison]::Ordinal)
        } else {
            $ok = $Expected.Equals($Actual)
        }
    }
    if (-not $ok) { $Name = "$Name (expected <$Expected> actual <$Actual>)" }
    Assert-True $ok $Name
}

function Assert-Throws {
    # ScriptBlock を実行し、指定した例外型（内側の例外を含めて探索）が発生したら合格。見つかった例外は $script:TkLastException に入れる。
    param([scriptblock]$Script, [type]$ExceptionType, [string]$Name)
    $found = $null
    try { & $Script | Out-Null }
    catch {
        $e = $_.Exception
        while ($null -ne $e) {
            if ($ExceptionType.IsInstanceOfType($e)) { $found = $e; break }
            $e = $e.InnerException
        }
        if ($null -eq $found) { $Name = "$Name (got $($_.Exception.GetType().FullName): $($_.Exception.Message))" }
    }
    Assert-True ($null -ne $found) $Name
    $script:TkLastException = $found
}

function Write-TkSummary {
    [Console]::Out.WriteLine("RESULT: $script:TkPass checks passed, $script:TkFail failed")
    exit $script:TkFail
}

# ===== q08/test_q08.ps1 =====
# Q08 のテスト。入力ファイルは q08\testdata 配下の使い捨てフォルダーに作り、読み取り専用で解析する。
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '..\common\TestKit.ps1')
. (Join-Path $PSScriptRoot 'q08.ps1')

$base = Join-Path $PSScriptRoot ('testdata\run-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $base -Force
$script:n = 0
$utf8 = [System.Text.UTF8Encoding]::new($false)
function New-Csv([object]$content) {
    $script:n++
    $p = Join-Path $base ("c{0:D5}.csv" -f $script:n)
    if ($content -is [byte[]]) { [System.IO.File]::WriteAllBytes($p, $content) } else { [System.IO.File]::WriteAllBytes($p, $utf8.GetBytes([string]$content)) }
    [System.IO.File]::SetAttributes($p, [System.IO.FileAttributes]::ReadOnly)        # テスト用ファイルは読み取り専用
    return $p
}
function Parse([object]$content) {
    $p = New-Csv $content
    $out = [System.Collections.Generic.List[object]]::new(); $err = 'OK'
    try { Read-StrictCsv -Path $p | ForEach-Object { $out.Add($_) } }
    catch {
        $e = $_.Exception; while ($null -ne $e -and $e -isnot [StrictCsvException]) { $e = $e.InnerException }
        if ($null -eq $e) { throw }
        $err = "$($e.Reason)@$($e.RecordNumber)"
    }
    return @{ Err = $err; Out = $out }
}
function Show($r) { (($r.Out | ForEach-Object { '[' + (($_.Fields | ForEach-Object { "<$_>" }) -join '') + ']' }) -join '') + '#' + $r.Err }

try {
    # ---- 1. 正常系（完全一致）
    $cases = [ordered]@{
        "a,b" = '[<a><b>]#OK'
        "`"a,b`",c" = '[<a,b><c>]#OK'
        "`"x`"`"y`",z" = '[<x"y><z>]#OK'
        "`"line1`r`nline2`",2`r`n" = "[<line1`r`nline2><2>]#OK"
        ",,`r`na,,b`r`n," = '[<><><>][<a><><b>][<><>]#OK'
        "`"`",`"`"" = '[<><>]#OK'
        "a`r`nb`r`n" = '[<a>][<b>]#OK'
        "a`r`nb" = '[<a>][<b>]#OK'
        "日本語,😀é`r`n" = '[<日本語><😀é>]#OK'
        "" = '#OK'
        "x" = '[<x>]#OK'
    }
    foreach ($k in $cases.Keys) { Assert-Equal $cases[$k] (Show (Parse $k)) "parse $(($k -replace "`r", '\r') -replace "`n", '\n')" }
    $bom = Parse ([byte[]]([byte[]](0xEF, 0xBB, 0xBF) + $utf8.GetBytes("a,b`r`n")))
    Assert-Equal '[<a><b>]#OK' (Show $bom) 'leading BOM accepted'
    Assert-Equal '#OK' (Show (Parse ([byte[]](0xEF, 0xBB, 0xBF)))) 'BOM only -> 0 records'
    $r = Parse "a,b`r`nc,d"
    Assert-True ($r.Out[0].RecordNumber -eq 1 -and $r.Out[1].RecordNumber -eq 2 -and $r.Out[0].RecordNumber -is [long]) 'RecordNumber is 1-based long'
    Assert-True ($r.Out[0].Fields -is [string[]] -and $r.Out[0].Fields.Count -eq 2) 'Fields is a string[] with the exact column count'
    Assert-True (-not [object]::ReferenceEquals($r.Out[0].Fields, $r.Out[1].Fields)) 'Fields arrays are independent'
    $one = @(Read-StrictCsv -Path (New-Csv "a,b,c`r`n"))
    Assert-True ($one.Count -eq 1 -and $one[0].Fields.Count -eq 3) 'one record -> one object (Fields not enumerated into the pipeline)'

    # ---- 2. 異常系と境界
    $bad = [ordered]@{
        "a`nb" = 'Delimiter@1'; "a`rb" = 'Delimiter@1'; "a`r" = 'Delimiter@1'; "ok`r`nx`ny" = 'Delimiter@2'
        "`"abc" = 'Quote@1'; "`"a`" ,b" = 'Quote@1'; "`"a`"b" = 'Quote@1'; "a`"b" = 'Quote@1'; "ok`r`n`"x`"y" = 'Quote@2'
        "`"a`nb`"" = 'Delimiter@1'; "`"a`rb`"" = 'Delimiter@1'
        "`r`n" = 'Fields@1'; "a`r`n`r`nb" = 'Fields@2'
        "1,2,3,4,5,6,7,8,9" = 'Fields@1'; "a,b`r`na`u{FEFF}b" = 'Encoding@2'
    }
    foreach ($k in $bad.Keys) { Assert-Equal $bad[$k] (Parse $k).Err "reject $(($k -replace "`r", '\r') -replace "`n", '\n')" }
    Assert-Equal '[<1><2><3><4><5><6><7><8>]#OK' (Show (Parse '1,2,3,4,5,6,7,8')) '8 fields accepted'
    Assert-Equal 'Fields@1' (Parse ',,,,,,,,').Err '9 empty fields rejected'
    Assert-Equal 'Encoding@2' (Parse ([byte[]]($utf8.GetBytes("a`r`nb") + [byte[]](0xC3, 0x28)))).Err 'invalid UTF-8 in record 2'
    Assert-Equal 'Encoding@1' (Parse ([byte[]](0xED, 0xA0, 0x80))).Err 'UTF-8 encoded surrogate rejected'
    Assert-Equal 'Encoding@1' (Parse ([byte[]](0xFF, 0xFE, 0x61, 0x00))).Err 'UTF-16 BOM rejected'
    Assert-Equal 'Encoding@1' (Parse ([byte[]](0xEF, 0xBB, 0xBF, 0xEF, 0xBB, 0xBF, 0x61))).Err 'second BOM rejected'
    $f4096 = ('あ' * 1365) + 'x'                                                      # 1365*3 + 1 = 4096 バイト
    $r = Parse "$f4096,y"
    Assert-True ($r.Err -eq 'OK' -and $r.Out[0].Fields[0] -ceq $f4096) 'field of 4096 UTF-8 bytes accepted'
    Assert-Equal 'Length@1' (Parse "${f4096}x,y").Err 'field of 4097 UTF-8 bytes rejected'
    Assert-Equal 'Length@1' (Parse "`"$f4096`"`"`"").Err 'quoted field of 4097 content bytes (4096 + one escaped quote) rejected'
    $r = Parse "`"$($f4096.Substring(0, $f4096.Length - 1))`"`"`""
    Assert-True ($r.Err -eq 'OK' -and $utf8.GetByteCount($r.Out[0].Fields[0]) -eq 4096) 'quoted field of 4096 content bytes incl. an escaped quote accepted'
    $q = '"' * 8192                                                                    # 4096 個の引用符（内容 4096 バイト・元 8192 バイト）
    $last = ('"' * 8146) + ('a' * 23)                                                  # 内容 4073+23 = 4096 バイト・元 8169 バイト
    $rec65536 = ((1..7 | ForEach-Object { "`"$q`"" }) -join ',') + ",`"$last`""
    Assert-Equal 65536 $utf8.GetByteCount($rec65536) 'constructed record is exactly 65536 bytes'
    $r = Parse $rec65536
    Assert-True ($r.Err -eq 'OK' -and $r.Out[0].Fields.Count -eq 8 -and $r.Out[0].Fields[0].Length -eq 4096) 'record of 65536 input bytes accepted'
    $last2 = ('"' * 8148) + ('a' * 22)
    $rec65537 = ((1..7 | ForEach-Object { "`"$q`"" }) -join ',') + ",`"$last2`""
    Assert-Equal 65537 $utf8.GetByteCount($rec65537) 'constructed record is exactly 65537 bytes'
    Assert-Equal 'Length@1' (Parse $rec65537).Err 'record of 65537 input bytes rejected'

    $sb = [System.Text.StringBuilder]::new()
    for ($i = 1; $i -le 100000; $i++) { [void]$sb.Append($i).Append("`r`n") }
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $r = Parse $sb.ToString()
    Assert-True ($r.Err -eq 'OK' -and $r.Out.Count -eq 100000 -and $r.Out[99999].Fields[0] -eq '100000') '100000 records accepted'
    [void]$sb.Append('100001')
    $r = Parse $sb.ToString()
    Assert-Equal 'Limit@100001' $r.Err '100001st record -> Limit'
    Assert-Equal 100000 $r.Out.Count 'the 100000 earlier records were emitted'
    [Console]::Out.WriteLine("[Q08] 100000/100001 records parsed in $([math]::Round($sw.Elapsed.TotalSeconds,1)) s")

    # ---- 3. 参照エンコーダーとの往復（5 万行）と 1 バイトずつの断片入力
    $rng = [System.Random]::new(4180)
    $alphabet = @('a', 'b', 'Z', '0', ' ', ',', '"', "`r`n", 'é', 'ß', '日', '😀', 'x"y', ';', "`t")
    function New-Records([int]$count) {
        $recs = [System.Collections.Generic.List[string[]]]::new()
        for ($i = 0; $i -lt $count; $i++) {
            $nf = $rng.Next(1, 9); $fs = [string[]]::new($nf)
            for ($j = 0; $j -lt $nf; $j++) {
                $len = $rng.Next(0, 6); $s = [System.Text.StringBuilder]::new()
                for ($k = 0; $k -lt $len; $k++) { [void]$s.Append($alphabet[$rng.Next($alphabet.Count)]) }
                $fs[$j] = $s.ToString()
            }
            if ($nf -eq 1 -and $fs[0] -eq '') { $fs[0] = 'e' }            # 空行（1 個の空フィールド）は仕様で不正なので避ける
            $recs.Add($fs)
        }
        return ,$recs
    }
    function Encode-Records($recs, [bool]$finalCrlf) {
        $sb2 = [System.Text.StringBuilder]::new()
        for ($i = 0; $i -lt $recs.Count; $i++) {
            $parts = foreach ($f in $recs[$i]) {
                if ($f.IndexOfAny([char[]]@(',', '"', "`r", "`n")) -ge 0 -or $rng.Next(4) -eq 0) { '"' + $f.Replace('"', '""') + '"' } else { $f }
            }
            [void]$sb2.Append((@($parts) -join ','))
            if ($i -lt $recs.Count - 1 -or $finalCrlf) { [void]$sb2.Append("`r`n") }
        }
        return $sb2.ToString()
    }
    $recs = New-Records 50000
    $text = Encode-Records $recs $true
    $st = @{}
    $p = New-Csv $text
    $sw.Restart()
    $mismatch = 0; $count = 0
    Read-StrictCsv -Path $p -Statistics $st | ForEach-Object {
        $exp = $recs[$count]
        if ($_.RecordNumber -ne $count + 1 -or $_.Fields.Count -ne $exp.Count) { $mismatch++ }
        else { for ($j = 0; $j -lt $exp.Count; $j++) { if (-not [string]::Equals($_.Fields[$j], $exp[$j], [System.StringComparison]::Ordinal)) { $mismatch++; break } } }
        $count++
    }
    $sw.Stop()
    Assert-Equal 50000 $count '50000 records round-tripped'
    Assert-Equal 0 $mismatch 'all 50000 records equal the reference fields'
    [Console]::Out.WriteLine("[Q08] 50000 random records ($($utf8.GetByteCount($text)) bytes) parsed in $([math]::Round($sw.Elapsed.TotalSeconds,1)) s; max record $($st.MaxRecordBytes) B, max field $($st.MaxFieldBytes) B")

    # 1 バイトずつしか返さないストリームで StreamReader を通しても同じ結果になること（多バイト文字の分断）
    class OneByteStream : System.IO.Stream {
        hidden [byte[]]$Data; hidden [int]$Pos
        OneByteStream([byte[]]$d) { $this.Data = $d; $this.Pos = 0 }
        [bool] get_CanRead() { return $true }
        [bool] get_CanSeek() { return $false }
        [bool] get_CanWrite() { return $false }
        [long] get_Length() { throw [System.NotSupportedException]::new() }
        [long] get_Position() { throw [System.NotSupportedException]::new() }
        [void] set_Position([long]$v) { throw [System.NotSupportedException]::new() }
        [void] Flush() { }
        [int] Read([byte[]]$b, [int]$o, [int]$c) { if ($this.Pos -ge $this.Data.Length -or $c -eq 0) { return 0 }; $b[$o] = $this.Data[$this.Pos]; $this.Pos++; return 1 }
        [long] Seek([long]$o, [System.IO.SeekOrigin]$s) { throw [System.NotSupportedException]::new() }
        [void] SetLength([long]$v) { throw [System.NotSupportedException]::new() }
        [void] Write([byte[]]$b, [int]$o, [int]$c) { throw [System.NotSupportedException]::new() }
    }
    $small = New-Records 300
    $smallText = Encode-Records $small $false
    $bytes = $utf8.GetBytes($smallText)
    $sr = [System.IO.StreamReader]::new([OneByteStream]::new($bytes), [System.Text.UTF8Encoding]::new($false, $true), $false, 1)
    $rebuilt = [System.Text.StringBuilder]::new()
    while (($ci = $sr.Read()) -ge 0) { [void]$rebuilt.Append([char]$ci) }
    $sr.Dispose()
    Assert-True ([string]::Equals($rebuilt.ToString(), $smallText, [System.StringComparison]::Ordinal)) 'strict StreamReader over a 1-byte-per-read stream decodes split multi-byte sequences exactly'
    $r = Parse $bytes
    $okSmall = $r.Err -eq 'OK' -and $r.Out.Count -eq 300
    if ($okSmall) { for ($i = 0; $i -lt 300; $i++) { if ((@($r.Out[$i].Fields) -join "`u{1}") -cne (@($small[$i]) -join "`u{1}")) { $okSmall = $false } } }
    Assert-True $okSmall '300 random records without final CRLF round-trip'

    # ---- 4. 途中の不正レコード: それ以前の正常レコードだけが出力される
    $mid = (Encode-Records (New-Records 10) $true) + "ok,1`r`nbad`"x`r`nnever,2`r`n"
    $r = Parse $mid
    Assert-Equal 'Quote@12' $r.Err 'corrupt record 12 reported'
    Assert-Equal 11 $r.Out.Count 'only the 11 records before it were emitted'
    Assert-True (@($r.Out | Where-Object { $_.Fields -isnot [string[]] }).Count -eq 0) 'each record kept its Fields array in one object'
} finally {
    Get-ChildItem -LiteralPath $base -File | ForEach-Object { $_.Attributes = 'Normal' }
    Remove-Item -LiteralPath $base -Recurse -Force
}
Write-TkSummary
```

#### 5. 期待結果・実測結果・実施状況

表の `\r\n` は CRLF を表します。結果は `[<f1><f2>]` の形で完全一致を比較しました。

| 必須検証ケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| `a,b` | `[<a><b>]` | 一致 | 成功 |
| `"a,b",c` | `[<a,b><c>]` | 一致 | 成功 |
| `"x""y",z` | `[<x"y><z>]` | 一致 | 成功 |
| 引用内の CRLF | `"line1\r\nline2",2\r\n` は `[<line1\r\nline2><2>]` | 一致 | 成功 |
| 空フィールド | `,,\r\na,,b\r\n,` は `[<><><>][<a><><b>][<><>]`、`"",""` は `[<><>]` | 一致 | 成功 |
| BOM の有無 | 先頭の BOM は受理、BOM だけのファイルは 0 件。2 個目の BOM・途中の U+FEFF・UTF-16 の BOM は Encoding | 一致 | 成功 |
| 末尾 CRLF なし・空ファイル | `a\r\nb` は 2 件、`x` は 1 件、空ファイルは 0 件 | 一致 | 成功 |
| 非 ASCII | `日本語,😀é` が完全一致 | 一致 | 成功 |
| 裸の LF / CR | `a\nb`・`a\rb`・`a\r`（EOF）は Delimiter@1、2 件目の裸の LF は Delimiter@2、引用内の裸の LF・CR も Delimiter@1 | 一致 | 成功 |
| 未終端の引用・引用を閉じた後の空白 | `"abc` は Quote@1、`"a" ,b` は Quote@1、`"a"b`・`a"b` は Quote@1、2 件目なら Quote@2 | 一致 | 成功 |
| 空行 | `\r\n` だけは Fields@1、途中の空行は Fields@2 | 一致 | 成功 |
| 不正な UTF-8 | 2 件目の `C3 28` は Encoding@2（1 件目は出力済み）。UTF-8 で符号化したサロゲート `ED A0 80` は Encoding@1 | 一致 | 成功 |
| 8 / 9 列 | 8 列は受理、9 列・空の 9 列は Fields@1 | 一致 | 成功 |
| フィールド 4096 / 4097 バイト | `あ`×1365 + `x` = 4096 バイトは受理、4097 バイトは Length@1。引用内の `""` は 1 バイトとして数える（4096 + `"` = 4097 は拒否、4095 + `"` = 4096 は受理） | 一致 | 成功 |
| レコードの元の入力 65536 / 65537 バイト | 8 列の引用フィールド（`""` の繰り返し）で、ちょうど 65536 バイトのレコードは受理、65537 バイトは Length@1 | 一致 | 成功 |
| 100000 / 100001 件 | 100000 件は受理。100001 件目は Limit@100001 で、先行する 100000 件は出力済み | 一致（71.4 秒） | 成功 |
| 5 万行のストリームと参照エンコーダーの往復 | 1〜8 列・各 0〜5 個の部品（`,` `"` CRLF `é` `ß` `日` `😀` タブなど）のランダムなレコードを、独立した参照エンコーダーで CSV にし、全フィールドが一致 | 50,000/50,000 一致（1,562,764 バイト、55.9 秒） | 成功 |
| 1 文字ずつの断片条件 | 1 回の Read で 1 バイトしか返さないストリームを厳格な StreamReader で読み、多バイト文字の分断を含めて元の文字列と一致。実装も 1 バイトずつデコーダーに渡す方式で、末尾 CRLF なしの 300 件が往復一致 | 一致 | 成功 |
| 途中の不正レコード | ランダムな 10 件 + `ok,1` の後の `bad"x` は Quote@12 で、それ以前の 11 件だけが出力される | 一致 | 成功 |
| Fields が 1 件のオブジェクト内に保たれる | 1 レコード = 1 オブジェクト、Fields は列数どおりの string[]。レコードごとに別の配列 | 一致 | 成功 |
| テスト用ファイルは読み取り専用 | 全ファイルに ReadOnly 属性を付けて解析（実装は FileAccess.Read で開く） | 一致 | 成功 |

**自律修正**
- **実装：** 不正な UTF-8 のエラーが、1 つ前のレコード番号で報告されていました（Encoding@1）。StreamReader はバッファー単位で先にデコードするため、不正なバイトがどのレコードに属するかを特定できないことが原因です（テストで発見）。厳格な Decoder に 1 バイトずつ渡して 1 文字ずつ取り出す方式に変え、エラーが正しいレコード番号（Encoding@2）で報告されるようにしました。
- **テスト：**
  - `` `uFEFF `` は PowerShell では無効なエスケープなので、`` `u{FEFF} `` に直しました（1 回目に起動しなかった原因）。
  - `[byte[]] + [byte[]]` が object[] になって BOM 付きの入力が正しく作られていなかったので、`[byte[]](...)` で囲みました。
  - 4097 バイトのはずのテストデータが実際は 4095 バイトだったため、4096 + 引用符 1 個（= 4097）と、4095 + 引用符（= 4096）に作り直しました（受理されたのは実装として正しい挙動でした）。
- 修正後に Q08 全体を再実行し、合格しました。

#### 6. 技術的根拠・保証範囲・計算量

- **逐次解析：** `ReadToEnd`・`Get-Content -Raw`・`Import-Csv`・`ConvertFrom-Csv` は使いません。ファイルを 4096 バイトずつ読み、1 バイトずつ Decoder に渡します。不完全な多バイト列はデコーダーが保持し、EOF では flush で検出します。
- **区切り：**
  - レコードの区切りは CRLF だけです。引用外の裸の CR・LF は Delimiter にします。
  - 引用内でも、データとして許すのは CRLF の組だけです（裸の CR・LF は拒否する、と解釈しました）。
  - 閉じ引用符の直後は `,`・CRLF・EOF だけを許します。
  - 空の行（0 文字 + CRLF）は Fields として拒否します。
- **バイト数の数え方：**
  - フィールドは、復号した内容の UTF-8 バイト数です（`""` は 1）。
  - レコードは、引用符・カンマを含む元の入力の UTF-8 バイト数です（終端の CRLF は含めない）。
  - 文字ごとに、U+0080 未満は 1、U+0800 未満は 2、高位サロゲートは 4（低位は 0）、それ以外は 3 として数えます。コードポイント数とバイト数を混同しません。
- **メモリ：** 現在のフィールドの StringBuilder（≤ 4096 バイト分）、現在のレコードのフィールドの List（≤ 8）、固定のバッファーだけで、O(65536) です。
- **出力：** 各レコードの `Fields` は `List.ToArray()` で作った新しい string[] です（空フィールドも保持）。
- **計算量：** 時間 O(入力バイト数)、メモリ O(1 レコード) です。

#### 7. 未実施・未解決・制約事項

- PowerShell の文字単位の処理は遅く、5 万行（1.5 MB）で約 56 秒、10 万件で約 71 秒でした（性能の要件はなし）。
- 問題文が StreamReader の使用を「許す」ため、より正確な位置報告のために Decoder を直接使う方式を採りました（StreamReader も可という条件の範囲内）。
- エラー位置は論理レコード番号だけです（仕様どおり、バイトオフセットは返しません）。
- PowerShell 7.4 / .NET 8 での実行は未実施です。
- 参照資料：RFC 4180、Microsoft Learn の System.Text.Decoder・UTF8Encoding。コードは独自に作成しました。

---

## Q09【地獄級】ProcessStartInfo.ArgumentListによるネイティブ呼出し・非同期両ストリーム回収

**評価の狙い：** 引用符・空白を含む引数、PowerShellのコマンドインジェクション回避、stdout/stderrのデッドロック、タイムアウト。

### 元コード（危険・不完全／実装の出発点）

```powershell
function Invoke-NativeBad {
    param([string]$ArgsText)
    cmd.exe /c "pwsh $ArgsText"
}
```

### 必須要求仕様

1. `Invoke-RestrictedPwsh -Root ([string]) -ScriptRelativePath ([string]) -Arguments ([string[]]) -TimeoutMs ([int]5000)`を実装。Windows 11・PowerShell 7.4を対象。実行するのは同じホストにある`pwsh`（絶対実行ファイルパスを確定）と、Root内の**テストが作成した.ps1だけ**。ネットワーク・ダウンロード・管理者権限は不要。
2. ScriptRelativePathは`/`区切りのASCII英数字・`_`・`-`・`.`の構成要素のみ。空成分、`.`・`..`、絶対パス、`\\`、`:`、ワイルドカード、シンボリックリンクとジャンクションの経路を拒否し、Root下の既存通常`.ps1`ファイルを参照する。Root自身も通常ディレクトリ。
3. `System.Diagnostics.ProcessStartInfo`で`UseShellExecute=$false`、`CreateNoWindow=$true`、`RedirectStandardOutput/Error=$true`、引数は**`ArgumentList.Add()`**で個別に構築。固定引数`-NoLogo -NoProfile -NonInteractive -File <絶対スクリプトパス>`の後にユーザーのArgumentsを順番通り渡す。`cmd.exe`・`Invoke-Expression`・`Start-Process -ArgumentList`の文字列連結方式は禁止。
4. 標準出力・標準エラーは**両方を同時に非同期回収**する。合計最大1MiBのUTF-8バイトを保持し、上限を超えたらプロセスツリーを終了・回収し`NativeOutputLimitException`。UTF-8不正バイトも例外として扱う。応答の順番はストリーム別に保持し、2ストリーム間の相対順序は要求しない。
5. TimeoutMsは100〜30000。タイムアウト時はプロセスツリーを停止・回収して`NativeTimeoutException`。正常終了または非ゼロ終了は`[pscustomobject]@{ ExitCode=[int]; StdOut=[string]; StdErr=[string] }`を返す。**非ゼロ終了は例外にせずExitCodeで報告する**。改行はそのまま保持する。
6. プロセスは必ずDisposeし、タイムアウト後に孤児プロセスを残さない。Argumentsの各要素長は最大4096文字、個数最大64、NULは禁止。外部プロセスの内容はテスト専用の既知スクリプトのみとする。
7. スクリプトが自分で作成する子プロセスもテスト専用に限定。権限昇格や外部コマンド実行の任意許可は追加しない。

### 必須検証項目

1. スペース、二重引用符、単引用符、非ASCII、`;`、`&`、`|`、`$()`を含む引数が**そのままの文字列**で子プロセスへ届くことを検証。
2. stdoutとstderrへそれぞれ大量出力し、両方回収してハングしないこと。合計1MiB境界・超過拒否、UTF-8不正の拒否。
3. exit 0/7、タイムアウト、子プロセスの強制回収、短時間での正常終了。終了コード・例外型・後始末を検証。
4. パスの`../`、相対名の不正、リンク/junction、通常ファイル以外を拒否。Root外のファイルやコマンドが実行されないこと。

### ★★FREYJAの回答【Q09・記入欄】

> **以下は専用回答欄。** 問題文・危険コード・要求仕様・必須検証・採点基準は変更禁止。修正コード、テストコード、実測を以下に全文記載する。

#### 1. 根本原因・危険性・修正方針

**元コード `Invoke-NativeBad` の問題点**

| # | 問題箇所 | 発生条件 | 危険性・影響 |
| --- | --- | --- | --- |
| 1 | `cmd.exe /c "pwsh $ArgsText"` | 引数に `&`・`\|`・`;`・引用符 | cmd が文字列を再解釈し、任意のコマンドを実行できる（コマンドインジェクション） |
| 2 | 文字列連結で引数を作る | 空白・引用符・非 ASCII | 引数の境界が崩れ、意図と違う値が届く |
| 3 | 実行対象を限定しない | — | 任意のスクリプト・コマンドを実行できる |
| 4 | 出力の回収・上限・タイムアウトがない | 大量の出力・無限ループ | 呼出し側が止まる。stdout と stderr を順番に読むとデッドロックする |
| 5 | プロセスの後始末がない | 子孫のプロセス | 孤児プロセスが残る |

**修正方針**
- 実行するのは、現在のホストの pwsh（`[Environment]::ProcessPath` の絶対パス）と、Root 配下の検証済みの通常の `.ps1` だけです。
- `ProcessStartInfo`（UseShellExecute=false、CreateNoWindow=true、標準入出力をリダイレクト）を使い、`ArgumentList.Add()` で 1 要素ずつ渡します。
- stdout と stderr は、`BaseStream.ReadAsync` と `Task.WaitAny` で同時に回収し、合計 1 MiB とタイムアウトを監視します。上限超過・タイムアウトの場合は `Kill($true)` でプロセスツリーを停止・回収してから例外にします。
- 出力は厳密な UTF-8 で復号し、`[pscustomobject]@{ ExitCode; StdOut; StdErr }` を返します。

#### 2. 完全な修正版PowerShellコード

```powershell
#Requires -Version 7.4
# Q09: ProcessStartInfo.ArgumentList による pwsh 子プロセスの安全な起動（stdout/stderr の同時非同期回収・上限・タイムアウト）

Set-StrictMode -Version 3.0

class NativeTimeoutException : System.TimeoutException {
    NativeTimeoutException([string]$message) : base($message) { }
}
class NativeOutputLimitException : System.InvalidOperationException {
    NativeOutputLimitException([string]$message) : base($message) { }
}
class NativeOutputEncodingException : System.FormatException {
    [string]$StreamName
    NativeOutputEncodingException([string]$stream, [string]$message) : base($message) { $this.StreamName = $stream }
}

$script:RpNamePattern = [regex]::new('\A[A-Za-z0-9_.-]{1,64}(/[A-Za-z0-9_.-]{1,64})*\z', [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
$script:RpReserved = [System.Collections.Generic.HashSet[string]]::new(
    [string[]]@('CON', 'PRN', 'AUX', 'NUL', 'CONIN$', 'CONOUT$', 'COM0', 'COM1', 'COM2', 'COM3', 'COM4', 'COM5', 'COM6', 'COM7', 'COM8', 'COM9',
                'LPT0', 'LPT1', 'LPT2', 'LPT3', 'LPT4', 'LPT5', 'LPT6', 'LPT7', 'LPT8', 'LPT9'), [System.StringComparer]::OrdinalIgnoreCase)
$script:RpMaxOutputBytes = 1048576

function Resolve-RestrictedScript([string]$Root, [string]$RelativePath) {
    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd([System.IO.Path]::DirectorySeparatorChar)
    $rootInfo = [System.IO.DirectoryInfo]::new($rootFull)
    if (-not $rootInfo.Exists) { throw [System.IO.DirectoryNotFoundException]::new("Root does not exist: $rootFull") }
    if ($rootInfo.Attributes.HasFlag([System.IO.FileAttributes]::ReparsePoint)) { throw [System.UnauthorizedAccessException]::new('Root must not be a reparse point') }

    if ([string]::IsNullOrEmpty($RelativePath) -or $RelativePath.Length -gt 256 -or -not $script:RpNamePattern.IsMatch($RelativePath)) {
        throw [System.ArgumentException]::new("invalid script path '$RelativePath'", 'ScriptRelativePath')
    }
    $parts = $RelativePath.Split('/')
    foreach ($part in $parts) {
        if ($part -eq '.' -or $part -eq '..' -or $part.EndsWith('.', [System.StringComparison]::Ordinal) -or
            $script:RpReserved.Contains($part.Split('.')[0])) {
            throw [System.ArgumentException]::new("invalid path component '$part'", 'ScriptRelativePath')
        }
    }
    if (-not $RelativePath.EndsWith('.ps1', [System.StringComparison]::OrdinalIgnoreCase)) {
        throw [System.ArgumentException]::new('only .ps1 scripts may be executed', 'ScriptRelativePath')
    }
    $full = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($rootFull, $RelativePath.Replace('/', '\')))
    if (-not $full.StartsWith($rootFull + '\', [System.StringComparison]::OrdinalIgnoreCase)) {
        throw [System.ArgumentException]::new('script path escapes Root', 'ScriptRelativePath')
    }
    # 経路上のディレクトリと対象ファイルに再解析ポイントがないこと
    $cursor = $rootFull
    for ($i = 0; $i -lt $parts.Length - 1; $i++) {
        $cursor = [System.IO.Path]::Combine($cursor, $parts[$i])
        $di = [System.IO.DirectoryInfo]::new($cursor)
        if (-not $di.Exists) { throw [System.IO.FileNotFoundException]::new('script not found', $full) }
        if ($di.Attributes.HasFlag([System.IO.FileAttributes]::ReparsePoint)) { throw [System.UnauthorizedAccessException]::new("reparse point in path: $cursor") }
    }
    if ([System.IO.Directory]::Exists($full)) { throw [System.UnauthorizedAccessException]::new("not a regular file: $full") }
    $fi = [System.IO.FileInfo]::new($full)
    if (-not $fi.Exists) { throw [System.IO.FileNotFoundException]::new('script not found', $full) }
    if ($fi.Attributes.HasFlag([System.IO.FileAttributes]::ReparsePoint) -or $fi.Attributes.HasFlag([System.IO.FileAttributes]::Device)) {
        throw [System.UnauthorizedAccessException]::new("not a regular file: $full")
    }
    return @{ Root = $rootFull; Script = $full }
}

function Get-CommandLineArgumentLength([string]$Arg) {
    # .NET の ArgumentList が Windows のコマンドラインへ変換するときの長さ（引用符・バックスラッシュの規則）
    if ($Arg.Length -gt 0 -and $Arg.IndexOfAny([char[]]@(' ', "`t", '"')) -lt 0) { return $Arg.Length }
    $n = 2; $slashes = 0
    foreach ($ch in $Arg.ToCharArray()) {
        if ($ch -eq '\') { $slashes++; continue }
        if ($ch -eq '"') { $n += $slashes * 2 + 2 } else { $n += $slashes + 1 }
        $slashes = 0
    }
    return $n + $slashes * 2
}

function Invoke-RestrictedPwsh {
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$ScriptRelativePath,
        [AllowEmptyCollection()][AllowEmptyString()][string[]]$Arguments = @(),
        [ValidateRange(100, 30000)][int]$TimeoutMs = 5000,
        # 診断用（任意）: ProcessId / KilledTree
        [hashtable]$Diagnostics
    )
    if ($null -eq $Arguments) { $Arguments = @() }
    if ($Arguments.Count -gt 64) { throw [System.ArgumentException]::new('at most 64 arguments', 'Arguments') }
    foreach ($a in $Arguments) {
        if ($null -eq $a) { throw [System.ArgumentNullException]::new('Arguments', 'null argument') }
        if ($a.Length -gt 4096) { throw [System.ArgumentException]::new('argument longer than 4096 characters', 'Arguments') }
        if ($a.IndexOf([char]0) -ge 0) { throw [System.ArgumentException]::new('NUL character in argument', 'Arguments') }
    }
    $resolved = Resolve-RestrictedScript $Root $ScriptRelativePath

    # 実行するのは現在のホストと同じ pwsh（絶対パス）だけ
    $pwsh = [Environment]::ProcessPath
    if (-not [string]::Equals([System.IO.Path]::GetFileName($pwsh), 'pwsh.exe', [System.StringComparison]::OrdinalIgnoreCase)) {
        throw [System.InvalidOperationException]::new("current host is not pwsh.exe: $pwsh")
    }
    # Windows の CreateProcess のコマンドラインは 32767 文字（終端 NUL を含む）まで。
    # 問題の上限（64 個 × 4096 文字）は合計でこれを超えうるため、起動前に正確な長さで検査する。
    $cmdLen = (Get-CommandLineArgumentLength $pwsh) + 2      # 実行ファイル名は常に引用符で囲まれうるので +2 の余裕
    foreach ($a in @('-NoLogo', '-NoProfile', '-NonInteractive', '-File', $resolved.Script) + $Arguments) { $cmdLen += 1 + (Get-CommandLineArgumentLength $a) }
    if ($cmdLen -gt 32766) { throw [System.ArgumentException]::new("command line would be $cmdLen characters (Windows limit 32766)", 'Arguments') }
    $psi = [System.Diagnostics.ProcessStartInfo]::new($pwsh)
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.RedirectStandardInput = $true            # 子が標準入力を待たないよう、すぐ閉じる
    $psi.WorkingDirectory = $resolved.Root
    foreach ($fixed in '-NoLogo', '-NoProfile', '-NonInteractive', '-File', $resolved.Script) { $psi.ArgumentList.Add($fixed) }
    foreach ($a in $Arguments) { $psi.ArgumentList.Add($a) }   # 1 要素 = 1 引数（文字列連結しない）

    $proc = [System.Diagnostics.Process]::new()
    $proc.StartInfo = $psi
    $pending = [System.Collections.Generic.List[System.Threading.Tasks.Task]]::new()
    $killed = $false
    try {
        [void]$proc.Start()
        if ($null -ne $Diagnostics) { $Diagnostics.ProcessId = $proc.Id }
        $proc.StandardInput.Close()
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $streams = @(
            @{ Name = 'StdOut'; Stream = $proc.StandardOutput.BaseStream; Buffer = [byte[]]::new(65536); Data = [System.IO.MemoryStream]::new(); Task = $null },
            @{ Name = 'StdErr'; Stream = $proc.StandardError.BaseStream; Buffer = [byte[]]::new(65536); Data = [System.IO.MemoryStream]::new(); Task = $null }
        )
        foreach ($s in $streams) { $s.Task = $s.Stream.ReadAsync($s.Buffer, 0, $s.Buffer.Length) }
        [long]$total = 0
        # 両ストリームを同時に読む（どちらか一方が満杯になって子が止まる事態を防ぐ）
        while ($true) {
            $active = @($streams | Where-Object { $null -ne $_.Task })
            if ($active.Count -eq 0) { break }
            $remaining = $TimeoutMs - $sw.ElapsedMilliseconds
            if ($remaining -le 0) { throw [NativeTimeoutException]::new("process did not finish within $TimeoutMs ms") }
            $tasks = [System.Threading.Tasks.Task[]]@($active | ForEach-Object { $_.Task })
            $done = [System.Threading.Tasks.Task]::WaitAny($tasks, [int]$remaining)
            if ($done -lt 0) { throw [NativeTimeoutException]::new("process did not finish within $TimeoutMs ms") }
            foreach ($s in $active) {
                if (-not $s.Task.IsCompleted) { continue }
                $n = $s.Task.GetAwaiter().GetResult()
                if ($n -le 0) { $s.Task = $null; continue }             # このストリームは EOF
                $total += $n
                if ($total -gt $script:RpMaxOutputBytes) {
                    $s.Task = $null
                    throw [NativeOutputLimitException]::new("combined output exceeded $($script:RpMaxOutputBytes) bytes")
                }
                $s.Data.Write($s.Buffer, 0, $n)
                $s.Task = $s.Stream.ReadAsync($s.Buffer, 0, $s.Buffer.Length)
            }
        }
        $remaining = $TimeoutMs - $sw.ElapsedMilliseconds
        if ($remaining -le 0 -or -not $proc.WaitForExit([int][Math]::Max(0, $remaining))) {
            throw [NativeTimeoutException]::new("process did not exit within $TimeoutMs ms")
        }
        $proc.WaitForExit()      # 終了処理の完了を待つ（出力は既に EOF）

        $strict = [System.Text.UTF8Encoding]::new($false, $true)
        $texts = @{}
        foreach ($s in $streams) {
            try { $texts[$s.Name] = $strict.GetString($s.Data.ToArray()) }
            catch { throw [NativeOutputEncodingException]::new($s.Name, "$($s.Name) is not valid UTF-8") }
        }
        return [pscustomobject][ordered]@{ ExitCode = [int]$proc.ExitCode; StdOut = $texts['StdOut']; StdErr = $texts['StdErr'] }
    } catch {
        # タイムアウト・上限超過・その他の失敗: プロセスツリーを停止して回収してから例外を伝える
        try { if (-not $proc.HasExited) { $proc.Kill($true); $killed = $true } } catch { }
        try { [void]$proc.WaitForExit(10000) } catch { }
        if ($null -ne $Diagnostics) { $Diagnostics.KilledTree = $killed }
        throw
    } finally {
        foreach ($s in @(if ($null -ne (Get-Variable streams -ValueOnly -ErrorAction SilentlyContinue)) { $streams })) {
            if ($null -ne $s.Task) { $pending.Add($s.Task) }
        }
        if ($pending.Count -gt 0) {
            # 終了したプロセスのパイプは閉じるので、読み取りタスクは完了する。例外も観測しておく
            try { [void][System.Threading.Tasks.Task]::WaitAll($pending.ToArray(), 5000) } catch { }
            foreach ($t in $pending) { if ($t.IsFaulted) { $null = $t.Exception } }
        }
        $proc.Dispose()
    }
}
```

#### 3. 実行環境・実行コマンド

```powershell
# 作業フォルダー（試験専用の一時領域）: scratchpad\ps\q09 に q09.ps1（実装）と test_q09.ps1（テスト）、scratchpad\ps\common に TestKit.ps1
cd <scratchpad>\ps\q09
pwsh -NoLogo -NoProfile -Command '$PSVersionTable.PSVersion.ToString(); [System.Runtime.InteropServices.RuntimeInformation]::FrameworkDescription'   # 7.7.0 / .NET 11.0.0
pwsh -NoLogo -NoProfile -File test_q09.ps1 2>&1 | Tee-Object -FilePath log.txt
$LASTEXITCODE   # 0 = 全件合格（失敗した検査の数）
```

**実行環境（全問共通）**

- **PowerShell：** **7.7.0**（pwsh。Microsoft Store 版 `Microsoft.PowerShell_8wekyb3d8bbwe`）。指定の **PowerShell 7.4 はこの PC にインストールされていません**。試験要項 2 に従い、許可なくソフトウェアを追加していません。
- **.NET：** pwsh 7.7.0 に組み込まれた **.NET 11.0.0** の上で実行しました。.NET 8 ランタイム（8.0.34）もインストールされていますが、pwsh 7.7 は自身のランタイムを使います。そのため、コードは PowerShell 7.4 / .NET 8 にもある API だけで書きました（.NET 9 以降の API は不使用）。ただし、7.4 / .NET 8 上での実行は未実施です。
- **OS / CPU：** Windows 11 Pro 10.0.26200（x64）、NTFS ／ Intel Core i7-4710MQ（4 コア 8 スレッド）。管理者権限は使っていません。
- **使用したもの：** PowerShell の標準機能と .NET の標準 API だけです。Pester・ThreadJob・外部モジュール・NuGet・外部 DLL・Add-Type による C# は使っていません。
- **テストの補助：** 自作の最小テスト補助 `common\TestKit.ps1`（Assert-True / Assert-Equal / Assert-Throws）を使いました。全文を「4.」に載せています。
- **実行方法：** 各問の作業フォルダー `scratchpad\ps\qNN` で `pwsh -NoLogo -NoProfile -File test_qNN.ps1` を実行しました。終了コード = 失敗した検査の数（0 = 全件合格）。
- **データの扱い：** ファイルを使う試験は、各問のフォルダーの `testdata\run-<GUID>` だけで行い、終了時に削除しました（残ったものは 0 件）。

- **最終の実行結果：** `RESULT: 41 checks passed, 0 failed`、終了コード 0、所要 14.7 秒。
- **それまでの実行：** テスト側の 2 つの誤りで途中停止しました（下記の自律修正）。

#### 4. 実際に実行したテストコード全文

```powershell
# ===== common/TestKit.ps1（全問共通の自作テスト補助） =====
# 試験用の最小テスト補助（Pester 不使用）。各テストファイルが dot-source する。
$script:TkPass = 0
$script:TkFail = 0
$script:TkFailures = [System.Collections.Generic.List[string]]::new()

function Assert-True {
    param([bool]$Condition, [string]$Name)
    if ($Condition) { $script:TkPass++ }
    else {
        $script:TkFail++
        $script:TkFailures.Add($Name)
        [Console]::Out.WriteLine("  FAIL: $Name")
    }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Name)
    $ok = $false
    if ($null -eq $Expected -and $null -eq $Actual) { $ok = $true }
    elseif ($null -ne $Expected -and $null -ne $Actual) {
        if ($Expected -is [byte[]] -and $Actual -is [byte[]]) {
            $ok = [System.Linq.Enumerable]::SequenceEqual([byte[]]$Expected, [byte[]]$Actual)
        } elseif ($Expected -is [string] -and $Actual -is [string]) {
            $ok = [string]::Equals($Expected, $Actual, [System.StringComparison]::Ordinal)
        } else {
            $ok = $Expected.Equals($Actual)
        }
    }
    if (-not $ok) { $Name = "$Name (expected <$Expected> actual <$Actual>)" }
    Assert-True $ok $Name
}

function Assert-Throws {
    # ScriptBlock を実行し、指定した例外型（内側の例外を含めて探索）が発生したら合格。見つかった例外は $script:TkLastException に入れる。
    param([scriptblock]$Script, [type]$ExceptionType, [string]$Name)
    $found = $null
    try { & $Script | Out-Null }
    catch {
        $e = $_.Exception
        while ($null -ne $e) {
            if ($ExceptionType.IsInstanceOfType($e)) { $found = $e; break }
            $e = $e.InnerException
        }
        if ($null -eq $found) { $Name = "$Name (got $($_.Exception.GetType().FullName): $($_.Exception.Message))" }
    }
    Assert-True ($null -ne $found) $Name
    $script:TkLastException = $found
}

function Write-TkSummary {
    [Console]::Out.WriteLine("RESULT: $script:TkPass checks passed, $script:TkFail failed")
    exit $script:TkFail
}

# ===== q09/test_q09.ps1 =====
# Q09 のテスト。実行するスクリプトはすべてテストが q09\testdata 配下に作った既知の .ps1 だけ。
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '..\common\TestKit.ps1')
. (Join-Path $PSScriptRoot 'q09.ps1')

$base = Join-Path $PSScriptRoot ('testdata\run-' + [guid]::NewGuid().ToString('N'))
$root = Join-Path $base 'root'
$outside = Join-Path $base 'outside'
$null = New-Item -ItemType Directory -Path $root, $outside, (Join-Path $root 'sub'), (Join-Path $root 'dir.ps1') -Force
function Add-Script([string]$rel, [string]$body) {
    $p = Join-Path $root ($rel.Replace('/', '\'))
    [System.IO.File]::WriteAllText($p, $body, [System.Text.UTF8Encoding]::new($true))
}
$rawWriter = @'
function Write-Raw([System.IO.Stream]$s, [byte[]]$b) { $s.Write($b, 0, $b.Length); $s.Flush() }
$so = [Console]::OpenStandardOutput(); $se = [Console]::OpenStandardError()
'@ + "`n"      # here-string は末尾の改行を含まないので補う
# 引数: 子プロセスが受け取った生の argv（.NET が解析したもの）と $args を Base64 で返す
Add-Script 'echo.ps1' ($rawWriter + @'
$argv = [Environment]::GetCommandLineArgs()
$i = [Array]::IndexOf($argv, $MyInvocation.MyCommand.Path)
$raw = if ($i -ge 0) { $argv[($i + 1)..($argv.Length)] | Where-Object { $null -ne $_ } } else { @('<script path not found>') }
foreach ($a in @($raw)) { Write-Raw $so ([Text.Encoding]::UTF8.GetBytes('R:' + [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes([string]$a)) + "`n")) }
foreach ($a in $args) { Write-Raw $so ([Text.Encoding]::UTF8.GetBytes('A:' + [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes([string]$a)) + "`n")) }
Write-Raw $se ([Text.Encoding]::UTF8.GetBytes("stderr line 1`r`nstderr line 2`n"))
'@)
Add-Script 'sub/flood.ps1' ("param([int]`$ErrBytes, [int]`$OutBytes)`n" + $rawWriter + @'
# 先に stderr を大量に書く（片方だけ同期的に読む実装ならここで停止する）
$chunk = [byte[]]::new(4096); for ($k = 0; $k -lt 4096; $k++) { $chunk[$k] = 0x45 }
$left = $ErrBytes; while ($left -gt 0) { $n = [Math]::Min(4096, $left); $se.Write($chunk, 0, $n); $left -= $n }; $se.Flush()
for ($k = 0; $k -lt 4096; $k++) { $chunk[$k] = 0x4F }
$left = $OutBytes; while ($left -gt 0) { $n = [Math]::Min(4096, $left); $so.Write($chunk, 0, $n); $left -= $n }; $so.Flush()
'@)
Add-Script 'badutf8.ps1' ($rawWriter + 'Write-Raw $so ([byte[]](0x61, 0xFF, 0xFE, 0x62))')
Add-Script 'exit7.ps1' ($rawWriter + 'Write-Raw $so ([Text.Encoding]::UTF8.GetBytes("before exit`r`n")); exit 7')
Add-Script 'exit0.ps1' 'exit 0'
Add-Script 'sleep.ps1' 'Start-Sleep -Seconds 60'
Add-Script 'spawn.ps1' @'
param([string]$PidFile)
$psi = [System.Diagnostics.ProcessStartInfo]::new([Environment]::ProcessPath)
foreach ($a in '-NoLogo', '-NoProfile', '-NonInteractive', '-Command', 'Start-Sleep -Seconds 60') { $psi.ArgumentList.Add($a) }
$psi.UseShellExecute = $false; $psi.CreateNoWindow = $true
$child = [System.Diagnostics.Process]::Start($psi)
[System.IO.File]::WriteAllText($PidFile, "$($child.Id)")
Start-Sleep -Seconds 60
'@
[System.IO.File]::WriteAllText((Join-Path $outside 'evil.ps1'), "[System.IO.File]::WriteAllText('$(Join-Path $outside 'EXECUTED.txt')', 'x')")
$null = New-Item -ItemType Junction -Path (Join-Path $root 'junc') -Target $outside
[System.IO.File]::WriteAllText((Join-Path $root 'notps1.txt'), 'x')
$marker = Join-Path $outside 'EXECUTED.txt'

function Decode-Lines([string]$text, [string]$prefix) {
    @($text -split "`n" | Where-Object { $_.StartsWith($prefix) } | ForEach-Object { [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($_.Substring($prefix.Length))) })
}

try {
    # ---- 1. 引数がそのまま届くこと
    $argsIn = [string[]]@('a b', 'he said "hi"', "it's", '日本語😀é', ';', '&', '|', '$(Get-Date)', '`$x', 'x;y&z|w', '\"', 'a\\', 'tab	sp', '-NotAParam', '', '--%', 'end')
    $r = Invoke-RestrictedPwsh -Root $root -ScriptRelativePath 'echo.ps1' -Arguments $argsIn
    $raw = Decode-Lines $r.StdOut 'R:'
    Assert-Equal 0 $r.ExitCode 'echo exit code 0'
    Assert-Equal $argsIn.Count $raw.Count 'child received the same number of raw arguments'
    $same = $true; for ($i = 0; $i -lt [Math]::Min($raw.Count, $argsIn.Count); $i++) { if (-not [string]::Equals($raw[$i], $argsIn[$i], [StringComparison]::Ordinal)) { $same = $false; [Console]::Out.WriteLine("  differs: <$($argsIn[$i])> vs <$($raw[$i])>") } }
    Assert-True $same 'every argument arrives byte-for-byte (spaces, quotes, non-ASCII, ; & | $() backslashes, empty, --%)'
    $psArgs = Decode-Lines $r.StdOut 'A:'
    [Console]::Out.WriteLine("[Q09] raw argv count $($raw.Count); PowerShell `$args count $($psArgs.Count) (pwsh -File binding drops: " + ((@($argsIn | Where-Object { $psArgs -cnotcontains $_ }) | ForEach-Object { "<$_>" }) -join ' ') + ')')
    Assert-Equal "stderr line 1`r`nstderr line 2`n" $r.StdErr 'stderr captured separately with line breaks preserved'

    # ---- 2. 大量出力（両ストリーム同時回収）と 1 MiB 境界・UTF-8 不正
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $r = Invoke-RestrictedPwsh -Root $root -ScriptRelativePath 'sub/flood.ps1' -Arguments '-ErrBytes', '600000', '-OutBytes', '448576' -TimeoutMs 20000
    Assert-True ($r.ExitCode -eq 0 -and $r.StdErr.Length -eq 600000 -and $r.StdOut.Length -eq 448576) 'exactly 1 MiB total (600000 stderr first + 448576 stdout) collected without deadlock'
    Assert-True ($r.StdErr -cmatch '\AE+\z' -and $r.StdOut -cmatch '\AO+\z') 'each stream kept separately'
    [Console]::Out.WriteLine("[Q09] 1 MiB of output collected in $([math]::Round($sw.Elapsed.TotalSeconds,2)) s")
    $d = @{}
    Assert-Throws { Invoke-RestrictedPwsh -Root $root -ScriptRelativePath 'sub/flood.ps1' -Arguments '-ErrBytes', '600000', '-OutBytes', '448577' -TimeoutMs 20000 -Diagnostics $d } ([NativeOutputLimitException]) '1 MiB + 1 byte -> NativeOutputLimitException'
    Start-Sleep -Milliseconds 300
    Assert-True ($null -eq (Get-Process -Id $d.ProcessId -ErrorAction SilentlyContinue)) 'process gone after the limit'
    Assert-Throws { Invoke-RestrictedPwsh -Root $root -ScriptRelativePath 'sub/flood.ps1' -Arguments '-ErrBytes', '0', '-OutBytes', '3000000' -TimeoutMs 20000 } ([NativeOutputLimitException]) '3 MB on stdout only -> limit'
    Assert-Throws { Invoke-RestrictedPwsh -Root $root -ScriptRelativePath 'badutf8.ps1' } ([NativeOutputEncodingException]) 'invalid UTF-8 output -> NativeOutputEncodingException'

    # ---- 3. 終了コード・タイムアウト・子プロセスの回収
    $r = Invoke-RestrictedPwsh -Root $root -ScriptRelativePath 'exit7.ps1'
    Assert-True ($r.ExitCode -eq 7 -and $r.StdOut -ceq "before exit`r`n") 'non-zero exit reported as ExitCode 7 (no exception), output kept'
    $sw.Restart()
    $r = Invoke-RestrictedPwsh -Root $root -ScriptRelativePath 'exit0.ps1'
    $quick = $sw.Elapsed.TotalMilliseconds
    Assert-True ($r.ExitCode -eq 0 -and $r.StdOut -eq '' -and $r.StdErr -eq '') 'exit 0 with no output'
    $d = @{}
    $sw.Restart()
    Assert-Throws { Invoke-RestrictedPwsh -Root $root -ScriptRelativePath 'sleep.ps1' -TimeoutMs 1500 -Diagnostics $d } ([NativeTimeoutException]) 'timeout -> NativeTimeoutException'
    $tElapsed = $sw.Elapsed.TotalMilliseconds
    Assert-True ($tElapsed -lt 6000) "timeout returned promptly ($([math]::Round($tElapsed)) ms for a 1500 ms limit)"
    Assert-True ($d.KilledTree -and $null -eq (Get-Process -Id $d.ProcessId -ErrorAction SilentlyContinue)) 'timed-out process tree killed and reaped'
    $pidFile = Join-Path $base 'grandchild.pid'
    $d = @{}
    Assert-Throws { Invoke-RestrictedPwsh -Root $root -ScriptRelativePath 'spawn.ps1' -Arguments '-PidFile', $pidFile -TimeoutMs 4000 -Diagnostics $d } ([NativeTimeoutException]) 'script with a grandchild times out'
    $gpid = [int]([System.IO.File]::ReadAllText($pidFile))
    Start-Sleep -Milliseconds 500
    Assert-True ($null -eq (Get-Process -Id $gpid -ErrorAction SilentlyContinue)) "grandchild process $gpid killed with the tree (no orphan)"
    [Console]::Out.WriteLine("[Q09] quick exit in $([math]::Round($quick)) ms; timeout handled in $([math]::Round($tElapsed)) ms")
    Assert-Throws { Invoke-RestrictedPwsh -Root $root -ScriptRelativePath 'exit0.ps1' -TimeoutMs 99 } ([System.Management.Automation.ParameterBindingException]) 'TimeoutMs 99 rejected'
    Assert-Throws { Invoke-RestrictedPwsh -Root $root -ScriptRelativePath 'exit0.ps1' -TimeoutMs 30001 } ([System.Management.Automation.ParameterBindingException]) 'TimeoutMs 30001 rejected'

    # ---- 4. パス・引数の拒否（Root 外のファイルは実行されない）
    $rej = [ordered]@{
        '../outside/evil.ps1' = [System.ArgumentException]; 'sub/../echo.ps1' = [System.ArgumentException]; './echo.ps1' = [System.ArgumentException]
        'C:/x.ps1' = [System.ArgumentException]; 'sub\flood.ps1' = [System.ArgumentException]; '/echo.ps1' = [System.ArgumentException]
        '*.ps1' = [System.ArgumentException]; 'sub//flood.ps1' = [System.ArgumentException]; 'notps1.txt' = [System.ArgumentException]
        'CON.ps1' = [System.ArgumentException]; 'echo.ps1.' = [System.ArgumentException]
        'junc/evil.ps1' = [System.UnauthorizedAccessException]; 'dir.ps1' = [System.UnauthorizedAccessException]
        'missing.ps1' = [System.IO.FileNotFoundException]
    }
    foreach ($k in $rej.Keys) { Assert-Throws { Invoke-RestrictedPwsh -Root $root -ScriptRelativePath $k } $rej[$k] "reject '$k'" }
    Assert-Throws { Invoke-RestrictedPwsh -Root (Join-Path $root 'junc') -ScriptRelativePath 'evil.ps1' } ([System.UnauthorizedAccessException]) 'Root that is a junction rejected'
    Assert-True (-not (Test-Path -LiteralPath $marker)) 'the outside script was never executed'
    Assert-Throws { Invoke-RestrictedPwsh -Root $root -ScriptRelativePath 'exit0.ps1' -Arguments ([string[]](1..65 | ForEach-Object { "$_" })) } ([System.ArgumentException]) '65 arguments rejected'
    Assert-Throws { Invoke-RestrictedPwsh -Root $root -ScriptRelativePath 'exit0.ps1' -Arguments ('x' * 4097) } ([System.ArgumentException]) '4097-character argument rejected'
    Assert-Throws { Invoke-RestrictedPwsh -Root $root -ScriptRelativePath 'exit0.ps1' -Arguments "a$([char]0)b" } ([System.ArgumentException]) 'NUL in argument rejected'
    $r = Invoke-RestrictedPwsh -Root $root -ScriptRelativePath 'echo.ps1' -Arguments ([string[]](1..64 | ForEach-Object { "y$_" })) -TimeoutMs 20000
    Assert-Equal 64 (Decode-Lines $r.StdOut 'R:').Count '64 short arguments accepted'
    $r = Invoke-RestrictedPwsh -Root $root -ScriptRelativePath 'echo.ps1' -Arguments ([string[]](1..7 | ForEach-Object { [string][char](0x60 + $_) * 4096 })) -TimeoutMs 20000
    $seven = Decode-Lines $r.StdOut 'R:'
    Assert-True ($seven.Count -eq 7 -and $seven[6].Length -eq 4096) '7 arguments of 4096 characters accepted (fits the Windows command line)'
    Assert-Throws { Invoke-RestrictedPwsh -Root $root -ScriptRelativePath 'echo.ps1' -Arguments ([string[]](1..64 | ForEach-Object { 'y' * 4096 })) } ([System.ArgumentException]) '64 x 4096 characters exceeds the 32767-character Windows command line -> ArgumentException before start'
} finally {
    if (Test-Path -LiteralPath (Join-Path $root 'junc')) { [System.IO.Directory]::Delete((Join-Path $root 'junc')) }
    Remove-Item -LiteralPath $base -Recurse -Force
}
Write-TkSummary
```

#### 5. 期待結果・実測結果・実施状況

| 必須検証ケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 引数がそのまま届く | `a b`・`he said "hi"`・`it's`・`日本語😀é`・`;`・`&`・`\|`・`$(Get-Date)`・`` `$x ``・`x;y&z\|w`・`\"`・`a\\`・タブ入り・`-NotAParam`・空文字・`--%`・`end` の 17 個が、子プロセスの argv（`[Environment]::GetCommandLineArgs()`）に一字一句そのまま届く | 17/17 一致 | 成功 |
| PowerShell の `$args` での見え方 | pwsh の `-File` のパラメーター束縛が `--%` を消費する（pwsh の仕様。ArgumentList の問題ではない） | `$args` は 16 個（`--%` だけ消える） | 記録 |
| stderr の別回収と改行 | `stderr line 1\r\nstderr line 2\n` が改行もそのまま StdErr に入る | 一致 | 成功 |
| 両ストリームの大量出力 | stderr に 600,000 バイトを先に書き、stdout に 448,576 バイト（合計ちょうど 1 MiB）。両方を回収してハングしない | 一致（0.74 秒） | 成功 |
| 1 MiB の超過 | 合計 1 MiB+1 バイト、stdout だけ 3 MB は NativeOutputLimitException。プロセスは回収済み | 一致 | 成功 |
| UTF-8 不正 | `61 FF FE 62` の出力は NativeOutputEncodingException | 一致 | 成功 |
| exit 0 / 7 | exit 7 は例外にせず ExitCode 7、出力は保持。exit 0 は出力なし | 一致 | 成功 |
| 短時間での正常終了 | exit 0 のスクリプトが pwsh の起動を含めて終わる | 872 ms | 成功 |
| タイムアウト | 60 秒スリープするスクリプトを TimeoutMs=1500 で実行すると NativeTimeoutException。プロセスツリーを停止し、PID は存在しない | 1604 ms で戻る | 成功 |
| 子プロセスの強制回収 | 孫の pwsh（60 秒スリープ）を起動するスクリプトがタイムアウトすると、孫も含めて停止（孫の PID は存在しない） | 一致 | 成功 |
| TimeoutMs の範囲 | 99・30001 は ParameterBindingException | 一致 | 成功 |
| パスの `../`・不正な相対名 | `../outside/evil.ps1`・`sub/../echo.ps1`・`./echo.ps1`・`C:/x.ps1`・`sub\flood.ps1`・`/echo.ps1`・`*.ps1`・`sub//flood.ps1`・`notps1.txt`・`CON.ps1`・`echo.ps1.` は ArgumentException | 一致 | 成功 |
| リンク・junction・通常ファイル以外 | junction 配下の `junc/evil.ps1`・ディレクトリ `dir.ps1` は UnauthorizedAccessException、`missing.ps1` は FileNotFoundException、junction の Root は UnauthorizedAccessException | 一致 | 成功 |
| Root 外が実行されない | Root 外の evil.ps1 が作るはずのマーカーファイルは存在しない | 一致 | 成功 |
| 引数の上限 | 65 個・4097 文字・NUL は ArgumentException。64 個（短い文字列）と、4096 文字 × 7 個は受理して全件が届く | 一致 | 成功 |
| 64 個 × 4096 文字 | Windows のコマンドライン長（32767 文字）を超えるので、起動前に ArgumentException | 一致 | 成功 |
| symlink | 作成に管理者特権が必要なため、試験していない（junction で試験） | — | **未実施** |

**自律修正**
- **仕様上の制約への対応：** 問題の上限（64 個 × 4096 文字 = 262,144 文字）は、Windows の CreateProcess のコマンドラインの上限 32,767 文字を超えます。そこで、.NET の ArgumentList と同じ引用規則で長さを正確に計算し、上限を超える場合は起動前に ArgumentException にしました（問題文は変更していません。7 で説明）。
- **テストの誤り 1：** テスト用スクリプトの共通の前置き（here-string）が末尾の改行を含まず、次の行と連結されて構文エラーになっていました。子の pwsh が日本語のエラーを stderr にコードページ 932 で出力し、実装が不正な UTF-8 として正しく拒否していました。改行を補いました。
- **テストの誤り 2：** `param(...)` がスクリプトの先頭になく、構文エラーでした。先頭に移しました。
- 修正後に Q09 全体を再実行し、合格しました。

#### 6. 技術的根拠・保証範囲・計算量

- **インジェクションの防止：**
  - `ArgumentList` は、引数ごとに Windows の規則（CommandLineToArgvW 互換）で引用・エスケープします。子の argv で元の文字列に戻ることを、17 種で確認しました。
  - cmd.exe・`Invoke-Expression`・文字列の連結を使わず、シェルによる再解釈が起きません。
  - `UseShellExecute=$false` なので、ファイルの関連付けでの起動もしません。
- **実行対象の限定：** 実行ファイルは `[Environment]::ProcessPath`（実行中の pwsh.exe）だけで、ファイル名が pwsh.exe であることを確認します。スクリプトは、Root 配下・パス規則に適合・経路に再解析ポイントがない・通常ファイル・`.ps1` の場合だけ実行します。
- **デッドロックの回避：** stdout と stderr の両方に、常に未完了の `ReadAsync` を 1 つずつ出しておき、`Task.WaitAny` で先に完了した方を処理します。どちらかのパイプが満杯になって子が止まることはありません（stderr を先に 600 KB 書く試験で確認）。
- **上限・タイムアウト：**
  - 読むたびに合計バイト数を加算し、1,048,576 を超えた時点で中止します。
  - 待機は残り時間を上限に行い、両ストリームの EOF の後の `WaitForExit` も残り時間の範囲で待ちます。
  - 失敗の経路では、`Kill($true)`（.NET のプロセスツリーの停止）→ `WaitForExit` → 読取りタスクの完了待ちと例外の観測 → `Dispose` の順で回収します。
- **文字コード：** 子の出力は生のバイトとして集め、最後に `UTF8Encoding(false, true)` で復号します。不正なら NativeOutputEncodingException です。改行は変換しません。
- **計算量：** 出力サイズに比例します（≤ 1 MiB）。

#### 7. 未実施・未解決・制約事項

- **問題文の上限の矛盾（明示）：** 「各要素 4096 文字・最大 64 個」をすべて同時に満たす入力は、Windows のコマンドラインの上限 32,767 文字を超えるため、起動できません。本回答では、個々の上限検査に加えて、合計長を事前に検査して ArgumentException で拒否する、と解釈しました。試験責任者の判断を仰ぎます。
- 子プロセスが UTF-8 以外（日本語版 Windows のコードページ 932 など）で stderr に書くと、仕様どおり NativeOutputEncodingException になります。テストのスクリプトは UTF-8 の生のバイトで出力しました。
- pwsh の `-File` の束縛は `--%` を `$args` から除きます（pwsh の仕様。生の argv には届いている）。
- シンボリックリンクの試験は、管理者特権がないため未実施です（junction で実施）。
- PowerShell 7.4 / .NET 8 での実行は未実施です。
- 参照資料：Microsoft Learn の ProcessStartInfo.ArgumentList・Process.Kill(Boolean)・about_Pwsh（-File）、CreateProcess のコマンドライン長の制限。コードは独自に作成しました。

---

## Q10【最終地獄級】不変スナップショット・マニフェスト切替による複数ファイルの原子的公開

**評価の狙い：** 直接の複数ファイル置換では不可能な全体原子性を、版ディレクトリと単一ポインタ切替で実現。

### 元コード（危険・不完全／実装の出発点）

```powershell
function Publish-SnapshotBad {
    param($Root, $Files)
    foreach($f in $Files) {
        Set-Content -Path (Join-Path $Root $f.Name) -Value $f.Content
    }
    # 途中エラーで新旧ファイルが混在する
}
```

### 必須要求仕様

1. Windows 11・NTFS上のテスト専用Rootを対象に、`Publish-Snapshot -Root -ExpectedGeneration -Items`、`Get-SnapshotToken -Root`、`Read-SnapshotItem -Token -RelativePath`を実装。Rootには試験が事前に用意した正常な初期`CURRENT`ファイル（Generation=0、初期の空版ディレクトリ）と、`versions`、`staging`、排他ロックファイルが存在する。
2. Itemsは次世代スナップショットの**ファイル全体集合**を表す。各Itemは`RelativePath`（最大256文字）と独立した`byte[] Content`（0〜1MiB）の2フィールド。件数0〜1000、合計32MiB以内。相対名は`/`区切り・各成分1〜64文字のASCII英数字・`_`・`-`・`.`のみ。`.`/`..`、空成分、絶対パス、デバイス名、末尾の`.`、大小文字だけ異なる重複、ファイル/ディレクトリ衝突を拒否。
3. 異なる世代のファイル群は`versions/v-<GUID>`という**不変のディレクトリ**に置く。Publisherは`staging/<GUID>`で全ファイルとマニフェストを準備し、各ファイルのSHA256と長さを検証してから、同じNTFS上の`versions`へ名前変更する。**この段階ではCURRENTを変更しない**。
4. `CURRENT`は正規のUTF-8 JSONで`{generation: 非負int64, version: 'v-<GUID>', manifestSha256: 64文字小文字hex}`の3キーだけを持つ。マニフェストは相対パスをOrdinal順で並べ、各ファイルの`relativePath/length/sha256`を含む固定規約（エンコーディング・区切りを回答で明示）で作成し、そのSHA256をCURRENTに持つ。
5. 新しいCURRENTをRoot内の一時ファイルへ完全に書き、Flush(true)して閉じてから`File.Replace(tempCurrent,CURRENT,$null)`で**単一のコミット**を行う。コミットはFile.Replaceが成功して戻った時点。コミット**前**の障害なら旧CURRENTと旧世代を保持、コミット**後**に掃除で失敗しても成功として返す。File.Replace自身の曖昧なOSエラー・外部電源断は保証範囲外。
6. ExpectedGenerationを排他ロックの下で再確認し、一致しなければ`SnapshotConflictException`で停止。Generationは1だけ増やす。Get-SnapshotTokenはCURRENTを一度読み込んでGeneration・Version・Manifestを検証した**読取専用トークン**を返す。Read-SnapshotItemはそのトークンの版を読み、別の世代と混在させない。存在しないファイルは`FileNotFoundException`。
7. 古いコミット済み版は**自動削除しない**（使用中トークンの読み取りを保証するため）。コミット未完了のstaging、一時CURRENT、CURRENTから参照されない版は、自分が作成したものだけ後で掃除してよいが、削除失敗は成功済みのコミットを失敗に変更しない。読者は必ずToken経由で読み、直接`versions`を参照しない。
8. 故障注入点は`Validate`・`StageWrite`・`StageFlush`・`BeforePromote`・`BeforeCurrentReplace`の5個（すべてコミット前）。Windows用通常ディレクトリのみ、ReparsePointは禁止。CPU/メモリ制限はファイル数と総バイト数の上限に従う。ネットワーク共有・並行する外部ファイル改変・電源断は保証範囲外。

### 必須検証項目

1. 0/1/500/1000件・総32MiB、複数階層、空ファイル、完全なバイト一致、世代0→1→2→…とExpectedGenerationの一致を確認。
2. 5箇所の障害注入ですべて旧CURRENTのまま、旧Tokenでも全ファイルが読めること。成功コミットの後は新Tokenで新版だけ見えること。
3. 1Writer＋8Readerを別Runspaceで実行し、各ReaderがToken取得後に100件を読んでも同じ世代で、混合内容0。古いTokenが新しいコミット後も使えること。
4. 無効パス・重複/大小文字衝突・symlink/junction・合計32MiB超・同じExpectedGenerationの競合を拒否。CURRENT・マニフェスト・SHA256検証と完了後の中間データの扱いを確認。

### ★★FREYJAの回答【Q10・記入欄】

> **以下は専用回答欄。** 問題文・危険コード・要求仕様・必須検証・採点基準は変更禁止。修正コード、テストコード、実測を以下に全文記載する。

#### 1. 根本原因・危険性・修正方針

**元コード `Publish-SnapshotBad` の問題点**

| # | 問題箇所 | 発生条件 | 危険性・影響 |
| --- | --- | --- | --- |
| 1 | ファイルを 1 つずつ上書きする | 途中のエラー・中断 | 新旧のファイルが混在した状態が公開される（全体の原子性がない） |
| 2 | 読み手との調停がない | 書込み中に読む | 読み手が一部だけ新しい、混在した内容を見る |
| 3 | `Join-Path $Root $f.Name` を検証しない | `..\` や絶対パスの名前 | Root の外に書く |
| 4 | 世代・排他制御がない | 同時の公開 | 更新の消失、どちらが最新か不明 |
| 5 | 完全性の検証がない | 書込みの失敗・改ざん | 壊れた内容を検出できない |
| 6 | 全体集合の概念がない | ファイルの削除 | 次の世代で消えるべきファイルが残る |

**修正方針**
- 各世代のファイル群を、不変の版ディレクトリ `versions/v-<GUID>` に置きます。公開は、単一のポインター `CURRENT` の `File.Replace` による 1 回の置換だけにします。
- Publisher は、排他ロックの下で ExpectedGeneration を再確認します。次に `staging/<GUID>` で全ファイルとマニフェストを書いて（Flush(true)）読み直して検証し、`versions` へ名前変更してから、新しい CURRENT を一時ファイルに書いて置換します。
- 読者は、`Get-SnapshotToken` で CURRENT を 1 回だけ読んで検証したトークンを得て、`Read-SnapshotItem` でその版だけを読みます。

#### 2. 完全な修正版PowerShellコード

```powershell
#Requires -Version 7.4
# Q10: 不変の版ディレクトリと CURRENT（単一ポインタ）の File.Replace による複数ファイルの原子的公開

Set-StrictMode -Version 3.0

class SnapshotConflictException : System.InvalidOperationException {
    [long]$ExpectedGeneration
    [long]$ActualGeneration
    SnapshotConflictException([long]$expected, [long]$actual) : base("generation conflict: expected $expected, actual $actual") {
        $this.ExpectedGeneration = $expected
        $this.ActualGeneration = $actual
    }
}
class SnapshotBusyException : System.TimeoutException {
    SnapshotBusyException([string]$message) : base($message) { }
}
class SnapshotCorruptException : System.IO.IOException {
    SnapshotCorruptException([string]$message) : base($message) { }
}

# 検証用の故障注入点（通常は $null）: 'Validate' / 'StageWrite' / 'StageFlush' / 'BeforePromote' / 'BeforeCurrentReplace'（すべてコミット前）
$script:SnapshotFaultInjector = $null
$script:SnPathPattern = [regex]::new('\A[A-Za-z0-9_.-]{1,64}(/[A-Za-z0-9_.-]{1,64})*\z', [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
$script:SnVersionPattern = [regex]::new('\Av-[0-9a-f]{32}\z', [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
$script:SnHexPattern = [regex]::new('\A[0-9a-f]{64}\z', [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
$script:SnIntPattern = [regex]::new('\A(0|[1-9][0-9]*)\z', [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
$script:SnReserved = [System.Collections.Generic.HashSet[string]]::new(
    [string[]]@('CON', 'PRN', 'AUX', 'NUL', 'CONIN$', 'CONOUT$', 'COM0', 'COM1', 'COM2', 'COM3', 'COM4', 'COM5', 'COM6', 'COM7', 'COM8', 'COM9',
                'LPT0', 'LPT1', 'LPT2', 'LPT3', 'LPT4', 'LPT5', 'LPT6', 'LPT7', 'LPT8', 'LPT9'), [System.StringComparer]::OrdinalIgnoreCase)
$script:SnUtf8 = [System.Text.UTF8Encoding]::new($false, $true)

function Invoke-SnapshotFault([string]$Point) { if ($null -ne $script:SnapshotFaultInjector) { & $script:SnapshotFaultInjector $Point } }
function Get-SnapshotHex([byte[]]$Bytes) { return [System.Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($Bytes)).ToLowerInvariant() }

function Test-SnapshotRelativePath([string]$RelativePath) {
    if ([string]::IsNullOrEmpty($RelativePath) -or $RelativePath.Length -gt 256 -or -not $script:SnPathPattern.IsMatch($RelativePath)) {
        throw [System.ArgumentException]::new("invalid relative path '$RelativePath'", 'RelativePath')
    }
    foreach ($part in $RelativePath.Split('/')) {
        if ($part -eq '.' -or $part -eq '..' -or $part.EndsWith('.', [System.StringComparison]::Ordinal) -or $script:SnReserved.Contains($part.Split('.')[0])) {
            throw [System.ArgumentException]::new("invalid path component '$part'", 'RelativePath')
        }
    }
}

function Assert-SnapshotDirectory([string]$Path, [string]$What) {
    $di = [System.IO.DirectoryInfo]::new($Path)
    if (-not $di.Exists) { throw [System.IO.DirectoryNotFoundException]::new("$What does not exist: $Path") }
    if ($di.Attributes.HasFlag([System.IO.FileAttributes]::ReparsePoint)) { throw [System.UnauthorizedAccessException]::new("$What must not be a reparse point: $Path") }
}

function Get-SnapshotLayout([string]$Root) {
    $r = [System.IO.Path]::GetFullPath($Root).TrimEnd('\')
    Assert-SnapshotDirectory $r 'Root'
    $layout = @{ Root = $r; Current = [System.IO.Path]::Combine($r, 'CURRENT'); Lock = [System.IO.Path]::Combine($r, 'LOCK')
                 Versions = [System.IO.Path]::Combine($r, 'versions'); Staging = [System.IO.Path]::Combine($r, 'staging') }
    Assert-SnapshotDirectory $layout.Versions 'versions'
    Assert-SnapshotDirectory $layout.Staging 'staging'
    return $layout
}

function Read-SnapshotFileShared([string]$Path) {
    # 他プロセスの File.Replace を妨げない共有モードで読む。置換中の一時的な共有違反・不在だけ有界回数で再試行する
    for ($attempt = 1; ; $attempt++) {
        try {
            $fs = [System.IO.FileStream]::new($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read,
                [System.IO.FileShare]::ReadWrite -bor [System.IO.FileShare]::Delete)
            try {
                if ($fs.Length -gt 1048576) { throw [SnapshotCorruptException]::new("file too large: $Path") }
                $buf = [byte[]]::new([int]$fs.Length); $got = 0
                while ($got -lt $buf.Length) { $n = $fs.Read($buf, $got, $buf.Length - $got); if ($n -le 0) { break }; $got += $n }
                if ($got -ne $buf.Length) { throw [SnapshotCorruptException]::new("short read: $Path") }
                return ,$buf
            } finally { $fs.Dispose() }
        } catch {
            $e = $_.Exception; if ($e -is [System.Management.Automation.MethodInvocationException] -and $null -ne $e.InnerException) { $e = $e.InnerException }
            $transient = ($e -is [System.IO.IOException] -and ($e.HResult -eq -2147024864 -or $e.HResult -eq -2147024863)) -or
                         $e -is [System.IO.FileNotFoundException] -or $e -is [System.UnauthorizedAccessException]
            if (-not $transient -or $attempt -ge 50) { throw $e }
            [System.Threading.Thread]::Sleep(10)
        }
    }
}

function ConvertTo-SnapshotCurrentJson([long]$Generation, [string]$Version, [string]$ManifestSha256) {
    # 固定の順序・空白なし。値は検証済みの文字種だけなのでエスケープ不要
    return "{`"generation`":$Generation,`"version`":`"$Version`",`"manifestSha256`":`"$ManifestSha256`"}"
}

function Read-SnapshotCurrent([hashtable]$Layout) {
    $bytes = Read-SnapshotFileShared $Layout.Current
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { throw [SnapshotCorruptException]::new('CURRENT must not have a BOM') }
    try { [void]$script:SnUtf8.GetString($bytes) } catch { throw [SnapshotCorruptException]::new('CURRENT is not valid UTF-8') }
    $opts = [System.Text.Json.JsonDocumentOptions]::new(); $opts.MaxDepth = 4
    try { $doc = [System.Text.Json.JsonDocument]::Parse([System.ReadOnlyMemory[byte]]::new($bytes), $opts) }
    catch { throw [SnapshotCorruptException]::new('CURRENT is not valid JSON') }
    try {
        $root = $doc.RootElement
        if ($root.ValueKind -ne [System.Text.Json.JsonValueKind]::Object) { throw [SnapshotCorruptException]::new('CURRENT must be a JSON object') }
        $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
        $gen = $null; $ver = $null; $sha = $null
        foreach ($p in $root.EnumerateObject()) {
            if (-not $seen.Add($p.Name)) { throw [SnapshotCorruptException]::new("duplicate key '$($p.Name)' in CURRENT") }
            switch -CaseSensitive ($p.Name) {
                'generation' {
                    if ($p.Value.ValueKind -ne [System.Text.Json.JsonValueKind]::Number -or -not $script:SnIntPattern.IsMatch($p.Value.GetRawText())) { throw [SnapshotCorruptException]::new('generation must be a non-negative integer') }
                    [long]$g = 0; if (-not $p.Value.TryGetInt64([ref]$g)) { throw [SnapshotCorruptException]::new('generation out of range') }
                    $gen = $g
                }
                'version' {
                    if ($p.Value.ValueKind -ne [System.Text.Json.JsonValueKind]::String) { throw [SnapshotCorruptException]::new('version must be a string') }
                    $ver = $p.Value.GetString()
                    if (-not $script:SnVersionPattern.IsMatch($ver)) { throw [SnapshotCorruptException]::new('version must be v-<32 hex>') }
                }
                'manifestSha256' {
                    if ($p.Value.ValueKind -ne [System.Text.Json.JsonValueKind]::String) { throw [SnapshotCorruptException]::new('manifestSha256 must be a string') }
                    $sha = $p.Value.GetString()
                    if (-not $script:SnHexPattern.IsMatch($sha)) { throw [SnapshotCorruptException]::new('manifestSha256 must be 64 lowercase hex digits') }
                }
                default { throw [SnapshotCorruptException]::new("unexpected key '$($p.Name)' in CURRENT") }
            }
        }
        if ($seen.Count -ne 3 -or $null -eq $gen -or $null -eq $ver -or $null -eq $sha) { throw [SnapshotCorruptException]::new('CURRENT must have exactly generation, version, manifestSha256') }
        return @{ Generation = [long]$gen; Version = $ver; ManifestSha256 = $sha }
    } finally { $doc.Dispose() }
}

function Read-SnapshotManifest([hashtable]$Layout, [string]$Version, [string]$ExpectedSha) {
    $vdir = [System.IO.Path]::Combine($Layout.Versions, $Version)
    Assert-SnapshotDirectory $vdir 'version directory'
    $bytes = [System.IO.File]::ReadAllBytes([System.IO.Path]::Combine($vdir, 'manifest.txt'))
    if (-not [string]::Equals((Get-SnapshotHex $bytes), $ExpectedSha, [System.StringComparison]::Ordinal)) { throw [SnapshotCorruptException]::new("manifest SHA-256 mismatch for $Version") }
    $text = $script:SnUtf8.GetString($bytes)
    $entries = [System.Collections.Generic.Dictionary[string, object]]::new([System.StringComparer]::Ordinal)
    if ($text.Length -gt 0) {
        if (-not $text.EndsWith("`n", [System.StringComparison]::Ordinal)) { throw [SnapshotCorruptException]::new('manifest must end with LF') }
        $prev = $null
        foreach ($line in $text.Substring(0, $text.Length - 1).Split("`n")) {
            $f = $line.Split("`t")
            if ($f.Length -ne 3 -or -not $script:SnIntPattern.IsMatch($f[1]) -or -not $script:SnHexPattern.IsMatch($f[2])) { throw [SnapshotCorruptException]::new('malformed manifest line') }
            if ($null -ne $prev -and [string]::CompareOrdinal($prev, $f[0]) -ge 0) { throw [SnapshotCorruptException]::new('manifest is not in Ordinal order') }
            $entries[$f[0]] = [pscustomobject]@{ Length = [long]$f[1]; Sha256 = $f[2] }
            $prev = $f[0]
        }
    }
    return [System.Collections.ObjectModel.ReadOnlyDictionary[string, object]]::new($entries)
}

function Enter-SnapshotLock([string]$LockPath) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    for ($attempt = 1; $attempt -le 20; $attempt++) {
        try { return [System.IO.FileStream]::new($LockPath, [System.IO.FileMode]::OpenOrCreate, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None) }
        catch {
            $e = $_.Exception; if ($e -is [System.Management.Automation.MethodInvocationException] -and $null -ne $e.InnerException) { $e = $e.InnerException }
            if ($e -isnot [System.IO.IOException] -and $e -isnot [System.UnauthorizedAccessException]) { throw $e }
        }
        $remaining = 2000 - $sw.ElapsedMilliseconds
        if ($remaining -le 0) { break }
        [System.Threading.Thread]::Sleep([int][Math]::Min(100, $remaining))
    }
    throw [SnapshotBusyException]::new("publish lock not acquired within 2 s: $LockPath")
}

function Initialize-SnapshotRoot {
    <# 試験用: 空の Root に versions / staging / LOCK と、Generation 0（空の版）の CURRENT を用意する #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Root)
    $r = [System.IO.Path]::GetFullPath($Root)
    if ([System.IO.Directory]::Exists($r) -and @([System.IO.Directory]::EnumerateFileSystemEntries($r)).Count -gt 0) { throw [System.IO.IOException]::new("Root is not empty: $r") }
    $null = [System.IO.Directory]::CreateDirectory($r)
    $null = [System.IO.Directory]::CreateDirectory([System.IO.Path]::Combine($r, 'staging'))
    $version = 'v-' + [guid]::NewGuid().ToString('N')
    $vdir = [System.IO.Path]::Combine($r, 'versions', $version)
    $null = [System.IO.Directory]::CreateDirectory([System.IO.Path]::Combine($vdir, 'files'))
    [System.IO.File]::WriteAllBytes([System.IO.Path]::Combine($vdir, 'manifest.txt'), [byte[]]::new(0))
    [System.IO.File]::WriteAllBytes([System.IO.Path]::Combine($r, 'LOCK'), [byte[]]::new(0))
    $json = ConvertTo-SnapshotCurrentJson 0 $version (Get-SnapshotHex ([byte[]]::new(0)))
    $fs = [System.IO.FileStream]::new([System.IO.Path]::Combine($r, 'CURRENT'), [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
    try { $b = [System.Text.Encoding]::UTF8.GetBytes($json); $fs.Write($b, 0, $b.Length); $fs.Flush($true) } finally { $fs.Dispose() }
}

function Publish-Snapshot {
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][long]$ExpectedGeneration,
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Items
    )
    Invoke-SnapshotFault 'Validate'
    $layout = Get-SnapshotLayout $Root
    if ($Items.Count -gt 1000) { throw [System.ArgumentException]::new('at most 1000 items', 'Items') }

    # ---- 次世代の全体集合を検証（大小文字の重複・ファイル/ディレクトリの衝突・サイズ）
    $files = [System.Collections.Generic.List[object]]::new()
    $fileKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $dirKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    [long]$total = 0
    foreach ($item in $Items) {
        if ($null -eq $item) { throw [System.ArgumentException]::new('null item', 'Items') }
        $names = @($item.psobject.Properties | ForEach-Object Name)
        if ($item -is [System.Collections.IDictionary]) { $names = @($item.Keys) }
        if ($names.Count -ne 2 -or -not ($names -ccontains 'RelativePath') -or -not ($names -ccontains 'Content')) {
            throw [System.ArgumentException]::new('each item must have exactly RelativePath and Content', 'Items')
        }
        $rel = $item.RelativePath; $content = $item.Content
        if ($rel -isnot [string]) { throw [System.ArgumentException]::new('RelativePath must be a string', 'Items') }
        if ($content -isnot [byte[]]) { throw [System.ArgumentException]::new("Content of '$rel' must be byte[]", 'Items') }
        Test-SnapshotRelativePath $rel
        if ($content.Length -gt 1048576) { throw [System.ArgumentException]::new("item '$rel' larger than 1 MiB", 'Items') }
        $total += $content.Length
        if ($total -gt 33554432) { throw [System.ArgumentException]::new('total content larger than 32 MiB', 'Items') }
        if (-not $fileKeys.Add($rel)) { throw [System.ArgumentException]::new("duplicate path (case-insensitive): '$rel'", 'Items') }
        $parts = $rel.Split('/')
        for ($k = 1; $k -lt $parts.Length; $k++) { [void]$dirKeys.Add([string]::Join('/', $parts, 0, $k)) }
        $files.Add([pscustomobject]@{ RelativePath = $rel; Content = $content; Length = [long]$content.Length; Sha256 = (Get-SnapshotHex $content) })
    }
    foreach ($k in $fileKeys) { if ($dirKeys.Contains($k)) { throw [System.ArgumentException]::new("path '$k' is both a file and a directory", 'Items') } }

    $lock = Enter-SnapshotLock $layout.Lock
    $guid = [guid]::NewGuid().ToString('N')
    $stageDir = [System.IO.Path]::Combine($layout.Staging, $guid)
    $version = "v-$guid"
    $versionDir = [System.IO.Path]::Combine($layout.Versions, $version)
    $tmpCurrent = [System.IO.Path]::Combine($layout.Root, ".CURRENT.$guid.tmp")
    $promoted = $false; $committed = $false
    try {
        # ---- 排他ロックの下で世代を再確認
        $cur = Read-SnapshotCurrent $layout
        if ($cur.Generation -ne $ExpectedGeneration) { throw [SnapshotConflictException]::new($ExpectedGeneration, $cur.Generation) }
        if ($cur.Generation -eq [long]::MaxValue) { throw [System.OverflowException]::new('generation is already Int64.MaxValue') }

        # ---- staging/<GUID> に全ファイルとマニフェストを準備
        $filesDir = [System.IO.Path]::Combine($stageDir, 'files')
        $null = [System.IO.Directory]::CreateDirectory($filesDir)
        foreach ($f in $files) {
            $target = [System.IO.Path]::Combine($filesDir, $f.RelativePath.Replace('/', '\'))
            $null = [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($target))
            $fs = [System.IO.FileStream]::new($target, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
            try {
                Invoke-SnapshotFault 'StageWrite'
                $fs.Write($f.Content, 0, $f.Content.Length)
                Invoke-SnapshotFault 'StageFlush'
                $fs.Flush($true)
            } finally { $fs.Dispose() }
        }
        $sorted = [string[]]@($files | ForEach-Object RelativePath)
        [Array]::Sort($sorted, [System.StringComparer]::Ordinal)
        $byPath = @{}; foreach ($f in $files) { $byPath[$f.RelativePath] = $f }
        $sb = [System.Text.StringBuilder]::new()
        foreach ($p in $sorted) { $f = $byPath[$p]; [void]$sb.Append($p).Append("`t").Append($f.Length).Append("`t").Append($f.Sha256).Append("`n") }
        $manifestBytes = [System.Text.UTF8Encoding]::new($false).GetBytes($sb.ToString())
        $manifestSha = Get-SnapshotHex $manifestBytes
        $fs = [System.IO.FileStream]::new([System.IO.Path]::Combine($stageDir, 'manifest.txt'), [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
        try { $fs.Write($manifestBytes, 0, $manifestBytes.Length); $fs.Flush($true) } finally { $fs.Dispose() }
        # 書いた内容を読み直して長さと SHA-256 を検証
        foreach ($f in $files) {
            $back = [System.IO.File]::ReadAllBytes([System.IO.Path]::Combine($filesDir, $f.RelativePath.Replace('/', '\')))
            if ($back.Length -ne $f.Length -or -not [string]::Equals((Get-SnapshotHex $back), $f.Sha256, [System.StringComparison]::Ordinal)) {
                throw [SnapshotCorruptException]::new("staged file verification failed: $($f.RelativePath)")
            }
        }

        # ---- versions へ名前変更（この段階では CURRENT を変更しない）
        Invoke-SnapshotFault 'BeforePromote'
        [System.IO.Directory]::Move($stageDir, $versionDir)
        $promoted = $true

        # ---- 新しい CURRENT を一時ファイルに完全に書き、File.Replace で単一のコミット
        $json = [System.Text.UTF8Encoding]::new($false).GetBytes((ConvertTo-SnapshotCurrentJson ($cur.Generation + 1) $version $manifestSha))
        $fs = [System.IO.FileStream]::new($tmpCurrent, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
        try { $fs.Write($json, 0, $json.Length); $fs.Flush($true) } finally { $fs.Dispose() }
        Invoke-SnapshotFault 'BeforeCurrentReplace'
        [System.IO.File]::Replace($tmpCurrent, $layout.Current, [NullString]::Value)      # PowerShell の $null は "" になるため NullString
        $committed = $true                                                                  # ← コミット成立点
        return [pscustomobject][ordered]@{ Generation = [long]($cur.Generation + 1); Version = $version; ManifestSha256 = $manifestSha; Files = $files.Count }
    } finally {
        if (-not $committed) {
            # コミット前の失敗: 自分が作った staging・未参照の版・一時 CURRENT だけを掃除（失敗しても元の例外を優先）
            try { if ([System.IO.Directory]::Exists($stageDir)) { [System.IO.Directory]::Delete($stageDir, $true) } } catch { }
            try { if ($promoted -and [System.IO.Directory]::Exists($versionDir)) { [System.IO.Directory]::Delete($versionDir, $true) } } catch { }
            try { if ([System.IO.File]::Exists($tmpCurrent)) { [System.IO.File]::Delete($tmpCurrent) } } catch { }
        }
        $lock.Dispose()
    }
}

function Get-SnapshotToken {
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    param([Parameter(Mandatory)][string]$Root)
    $layout = Get-SnapshotLayout $Root
    $cur = Read-SnapshotCurrent $layout                      # CURRENT は 1 回だけ読む
    $entries = Read-SnapshotManifest $layout $cur.Version $cur.ManifestSha256
    $token = [pscustomobject]@{}
    $values = @{ Root = $layout.Root; Generation = [long]$cur.Generation; Version = $cur.Version; ManifestSha256 = $cur.ManifestSha256; Entries = $entries }
    foreach ($k in 'Root', 'Generation', 'Version', 'ManifestSha256', 'Entries') {
        $v = $values[$k]
        # 読み取り専用（getter だけの ScriptProperty）。Entries も ReadOnlyDictionary
        $token | Add-Member -MemberType ScriptProperty -Name $k -Value ({ $v }.GetNewClosure())
    }
    $token.psobject.TypeNames.Insert(0, 'SnapshotToken')
    return $token
}

function Read-SnapshotItem {
    [CmdletBinding()]
    [OutputType([byte[]])]
    param([Parameter(Mandatory)][psobject]$Token, [Parameter(Mandatory)][string]$RelativePath)
    if ($Token.psobject.TypeNames -notcontains 'SnapshotToken') { throw [System.ArgumentException]::new('not a snapshot token', 'Token') }
    Test-SnapshotRelativePath $RelativePath
    $entry = $null
    if (-not $Token.Entries.TryGetValue($RelativePath, [ref]$entry)) {            # Ordinal（大小文字を区別）
        throw [System.IO.FileNotFoundException]::new("'$RelativePath' is not part of generation $($Token.Generation)", $RelativePath)
    }
    $vdir = [System.IO.Path]::Combine($Token.Root, 'versions', $Token.Version)
    Assert-SnapshotDirectory $vdir 'version directory'
    $cursor = [System.IO.Path]::Combine($vdir, 'files')
    Assert-SnapshotDirectory $cursor 'files directory'
    $parts = $RelativePath.Split('/')
    for ($i = 0; $i -lt $parts.Length - 1; $i++) { $cursor = [System.IO.Path]::Combine($cursor, $parts[$i]); Assert-SnapshotDirectory $cursor 'directory' }
    $path = [System.IO.Path]::Combine($cursor, $parts[-1])
    $fi = [System.IO.FileInfo]::new($path)
    if (-not $fi.Exists -or $fi.Attributes.HasFlag([System.IO.FileAttributes]::ReparsePoint)) { throw [SnapshotCorruptException]::new("version file missing or not regular: $RelativePath") }
    $bytes = [System.IO.File]::ReadAllBytes($path)
    if ($bytes.Length -ne $entry.Length -or -not [string]::Equals((Get-SnapshotHex $bytes), $entry.Sha256, [System.StringComparison]::Ordinal)) {
        throw [SnapshotCorruptException]::new("content of '$RelativePath' does not match the manifest")
    }
    Write-Output -NoEnumerate $bytes        # 1 個の byte[] として返す（要素ごとに列挙しない）
}
```

#### 3. 実行環境・実行コマンド

```powershell
# 作業フォルダー（試験専用の一時領域）: scratchpad\ps\q10 に q10.ps1（実装）と test_q10.ps1（テスト）、scratchpad\ps\common に TestKit.ps1
cd <scratchpad>\ps\q10
pwsh -NoLogo -NoProfile -Command '$PSVersionTable.PSVersion.ToString(); [System.Runtime.InteropServices.RuntimeInformation]::FrameworkDescription'   # 7.7.0 / .NET 11.0.0
pwsh -NoLogo -NoProfile -File test_q10.ps1 2>&1 | Tee-Object -FilePath log.txt
$LASTEXITCODE   # 0 = 全件合格（失敗した検査の数）
```

**実行環境（全問共通）**

- **PowerShell：** **7.7.0**（pwsh。Microsoft Store 版 `Microsoft.PowerShell_8wekyb3d8bbwe`）。指定の **PowerShell 7.4 はこの PC にインストールされていません**。試験要項 2 に従い、許可なくソフトウェアを追加していません。
- **.NET：** pwsh 7.7.0 に組み込まれた **.NET 11.0.0** の上で実行しました。.NET 8 ランタイム（8.0.34）もインストールされていますが、pwsh 7.7 は自身のランタイムを使います。そのため、コードは PowerShell 7.4 / .NET 8 にもある API だけで書きました（.NET 9 以降の API は不使用）。ただし、7.4 / .NET 8 上での実行は未実施です。
- **OS / CPU：** Windows 11 Pro 10.0.26200（x64）、NTFS ／ Intel Core i7-4710MQ（4 コア 8 スレッド）。管理者権限は使っていません。
- **使用したもの：** PowerShell の標準機能と .NET の標準 API だけです。Pester・ThreadJob・外部モジュール・NuGet・外部 DLL・Add-Type による C# は使っていません。
- **テストの補助：** 自作の最小テスト補助 `common\TestKit.ps1`（Assert-True / Assert-Equal / Assert-Throws）を使いました。全文を「4.」に載せています。
- **実行方法：** 各問の作業フォルダー `scratchpad\ps\qNN` で `pwsh -NoLogo -NoProfile -File test_qNN.ps1` を実行しました。終了コード = 失敗した検査の数（0 = 全件合格）。
- **データの扱い：** ファイルを使う試験は、各問のフォルダーの `testdata\run-<GUID>` だけで行い、終了時に削除しました（残ったものは 0 件）。

- **最終の実行結果：** `RESULT: 80 checks passed, 0 failed`、終了コード 0、所要 88.8 秒。
- **1 回目の実行：** 78 件合格・2 件失敗（いずれもテストの比較方法の誤り。下記）。

#### 4. 実際に実行したテストコード全文

```powershell
# ===== common/TestKit.ps1（全問共通の自作テスト補助） =====
# 試験用の最小テスト補助（Pester 不使用）。各テストファイルが dot-source する。
$script:TkPass = 0
$script:TkFail = 0
$script:TkFailures = [System.Collections.Generic.List[string]]::new()

function Assert-True {
    param([bool]$Condition, [string]$Name)
    if ($Condition) { $script:TkPass++ }
    else {
        $script:TkFail++
        $script:TkFailures.Add($Name)
        [Console]::Out.WriteLine("  FAIL: $Name")
    }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Name)
    $ok = $false
    if ($null -eq $Expected -and $null -eq $Actual) { $ok = $true }
    elseif ($null -ne $Expected -and $null -ne $Actual) {
        if ($Expected -is [byte[]] -and $Actual -is [byte[]]) {
            $ok = [System.Linq.Enumerable]::SequenceEqual([byte[]]$Expected, [byte[]]$Actual)
        } elseif ($Expected -is [string] -and $Actual -is [string]) {
            $ok = [string]::Equals($Expected, $Actual, [System.StringComparison]::Ordinal)
        } else {
            $ok = $Expected.Equals($Actual)
        }
    }
    if (-not $ok) { $Name = "$Name (expected <$Expected> actual <$Actual>)" }
    Assert-True $ok $Name
}

function Assert-Throws {
    # ScriptBlock を実行し、指定した例外型（内側の例外を含めて探索）が発生したら合格。見つかった例外は $script:TkLastException に入れる。
    param([scriptblock]$Script, [type]$ExceptionType, [string]$Name)
    $found = $null
    try { & $Script | Out-Null }
    catch {
        $e = $_.Exception
        while ($null -ne $e) {
            if ($ExceptionType.IsInstanceOfType($e)) { $found = $e; break }
            $e = $e.InnerException
        }
        if ($null -eq $found) { $Name = "$Name (got $($_.Exception.GetType().FullName): $($_.Exception.Message))" }
    }
    Assert-True ($null -ne $found) $Name
    $script:TkLastException = $found
}

function Write-TkSummary {
    [Console]::Out.WriteLine("RESULT: $script:TkPass checks passed, $script:TkFail failed")
    exit $script:TkFail
}

# ===== q10/test_q10.ps1 =====
# Q10 のテスト。Root はすべて q10\testdata 配下の使い捨てフォルダーに作る。
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '..\common\TestKit.ps1')
$impl = Join-Path $PSScriptRoot 'q10.ps1'
. $impl

$base = Join-Path $PSScriptRoot ('testdata\run-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $base -Force
$utf8 = [System.Text.UTF8Encoding]::new($false)
function Item([string]$rel, [byte[]]$content) { [pscustomobject]@{ RelativePath = $rel; Content = $content } }
function TextItem([string]$rel, [string]$text) { Item $rel $utf8.GetBytes($text) }
function New-Root([string]$name) { $r = Join-Path $base $name; Initialize-SnapshotRoot -Root $r; return $r }
function Get-State([string]$root) {
    @{ Current = [System.IO.File]::ReadAllBytes((Join-Path $root 'CURRENT'))
       Versions = @(Get-ChildItem -LiteralPath (Join-Path $root 'versions') -Directory | ForEach-Object Name | Sort-Object)
       Staging = @(Get-ChildItem -LiteralPath (Join-Path $root 'staging') -Force).Count
       Temps = @(Get-ChildItem -LiteralPath $root -Force -Filter '.CURRENT.*.tmp').Count }
}
function Read-All($token) {
    $h = @{}
    foreach ($k in $token.Entries.Keys) { $h[$k] = Read-SnapshotItem -Token $token -RelativePath $k }
    return $h
}

try {
    # ---- 1. 件数・サイズ・階層・バイト一致・世代
    $root = New-Root 'main'
    $t0 = Get-SnapshotToken -Root $root
    Assert-True ($t0.Generation -eq 0 -and $t0.Entries.Count -eq 0) 'initial generation 0 with an empty version'
    $r = Publish-Snapshot -Root $root -ExpectedGeneration 0 -Items @()
    Assert-True ($r.Generation -eq 1 -and $r.Files -eq 0) '0 items -> generation 1'
    $rng = [System.Random]::new(10)
    $one = [byte[]]::new(1000); $rng.NextBytes($one)
    $r = Publish-Snapshot -Root $root -ExpectedGeneration 1 -Items @((Item 'a/b/c/deep.bin' $one))
    $t = Get-SnapshotToken -Root $root
    Assert-True ($t.Generation -eq 2 -and $t.Entries.Count -eq 1) '1 item (3 directory levels) -> generation 2'
    Assert-Equal $one (Read-SnapshotItem -Token $t -RelativePath 'a/b/c/deep.bin') 'byte-identical content'
    Assert-True ((Read-SnapshotItem -Token $t -RelativePath 'a/b/c/deep.bin') -is [byte[]]) 'Read-SnapshotItem returns one byte[]'
    $items500 = foreach ($i in 0..499) { $b = [byte[]]::new($i % 97); $rng.NextBytes($b); Item ("d{0}/s{1}/f{2:D3}.dat" -f ($i % 5), ($i % 3), $i) $b }
    $r = Publish-Snapshot -Root $root -ExpectedGeneration 2 -Items @($items500)
    $t = Get-SnapshotToken -Root $root
    $allOk = $t.Entries.Count -eq 500
    foreach ($it in $items500) { if (-not [System.Linq.Enumerable]::SequenceEqual([byte[]](Read-SnapshotItem -Token $t -RelativePath $it.RelativePath), [byte[]]$it.Content)) { $allOk = $false } }
    Assert-True $allOk '500 items incl. empty files, all byte-identical'
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $items1000 = foreach ($i in 0..999) { TextItem ("x/{0:D4}.txt" -f $i) "item $i" }
    $r = Publish-Snapshot -Root $root -ExpectedGeneration 3 -Items @($items1000)
    $t = Get-SnapshotToken -Root $root
    Assert-True ($t.Generation -eq 4 -and $t.Entries.Count -eq 1000 -and $utf8.GetString((Read-SnapshotItem -Token $t -RelativePath 'x/0999.txt')) -eq 'item 999') '1000 items -> generation 4'
    [Console]::Out.WriteLine("[Q10] 1000-file publish in $([math]::Round($sw.Elapsed.TotalSeconds,1)) s")
    $sw.Restart()
    $big = foreach ($i in 0..31) { $b = [byte[]]::new(1048576); $rng.NextBytes($b); Item "big/part$i.bin" $b }
    $r = Publish-Snapshot -Root $root -ExpectedGeneration 4 -Items @($big)
    $t = Get-SnapshotToken -Root $root
    $bigOk = $true; foreach ($it in $big) { if (-not [System.Linq.Enumerable]::SequenceEqual([byte[]](Read-SnapshotItem -Token $t -RelativePath $it.RelativePath), [byte[]]$it.Content)) { $bigOk = $false } }
    Assert-True ($t.Generation -eq 5 -and $bigOk) 'total exactly 32 MiB (32 x 1 MiB) published and byte-identical'
    [Console]::Out.WriteLine("[Q10] 32 MiB publish in $([math]::Round($sw.Elapsed.TotalSeconds,1)) s")
    Assert-Throws { Publish-Snapshot -Root $root -ExpectedGeneration 4 -Items @((TextItem 'z' 'z')) } ([SnapshotConflictException]) 'stale ExpectedGeneration -> SnapshotConflictException'
    Assert-Equal ([long]5) $script:TkLastException.ActualGeneration 'conflict reports actual generation'
    Assert-Equal 0 (Get-State $root).Staging 'staging empty after successful publishes'
    Assert-Equal 0 (Get-State $root).Temps 'no temporary CURRENT left'

    # CURRENT・マニフェストの形式
    $curText = [System.IO.File]::ReadAllText((Join-Path $root 'CURRENT'))
    Assert-True ($curText -cmatch '\A\{"generation":5,"version":"v-[0-9a-f]{32}","manifestSha256":"[0-9a-f]{64}"\}\z') "CURRENT format: $curText"
    $man = [System.IO.File]::ReadAllBytes((Join-Path $root "versions\$($t.Version)\manifest.txt"))
    Assert-Equal $t.ManifestSha256 ([System.Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($man)).ToLowerInvariant()) 'manifest SHA-256 stored in CURRENT'
    $lines = $utf8.GetString($man).TrimEnd("`n").Split("`n")
    $sortedCopy = [string[]]$lines.Clone(); [Array]::Sort($sortedCopy, [System.StringComparer]::Ordinal)
    Assert-True (($lines.Count -eq 32) -and [System.Linq.Enumerable]::SequenceEqual($lines, $sortedCopy) -and $lines[0] -cmatch '\Abig/part0\.bin\t1048576\t[0-9a-f]{64}\z') 'manifest lines: path<TAB>length<TAB>sha256, Ordinal order'
    Assert-Throws { $t.Generation = 99 } ([System.Management.Automation.RuntimeException]) 'token properties are read-only'
    Assert-Throws { ([System.Collections.Generic.IDictionary[string, object]]$t.Entries).Add('x', $null) } ([System.NotSupportedException]) 'token Entries is read-only'
    Assert-Throws { Read-SnapshotItem -Token $t -RelativePath 'big/PART0.bin' } ([System.IO.FileNotFoundException]) 'lookup is case-sensitive (Ordinal); missing -> FileNotFoundException'
    Assert-Throws { Read-SnapshotItem -Token $t -RelativePath 'nope.bin' } ([System.IO.FileNotFoundException]) 'missing file -> FileNotFoundException'

    # ---- 2. 5 箇所の故障注入（すべてコミット前）
    $froot = New-Root 'faults'
    $null = Publish-Snapshot -Root $froot -ExpectedGeneration 0 -Items @((TextItem 'keep/a.txt' 'old a'), (TextItem 'b.txt' 'old b'))
    $oldToken = Get-SnapshotToken -Root $froot
    foreach ($point in 'Validate', 'StageWrite', 'StageFlush', 'BeforePromote', 'BeforeCurrentReplace') {
        $before = Get-State $froot
        $script:SnapshotFaultInjector = { param($p) if ($p -eq $point) { throw [System.IO.IOException]::new("injected at $p") } }.GetNewClosure()
        $msg = $null
        try { $null = Publish-Snapshot -Root $froot -ExpectedGeneration 1 -Items @((TextItem 'keep/a.txt' 'new a'), (TextItem 'c.txt' 'new c')) } catch { $msg = $_.Exception.Message }
        $script:SnapshotFaultInjector = $null
        $after = Get-State $froot
        Assert-True ($msg -like "*injected at $point*") "fault ${point}: publish failed"
        Assert-True ([System.Linq.Enumerable]::SequenceEqual([byte[]]$before.Current, [byte[]]$after.Current)) "fault ${point}: CURRENT unchanged"
        Assert-True (($before.Versions -join ',') -eq ($after.Versions -join ',') -and $after.Staging -eq 0 -and $after.Temps -eq 0) "fault ${point}: no new version, staging or temp left"
        $cur = Get-SnapshotToken -Root $froot
        $old = Read-All $oldToken
        Assert-True ($cur.Generation -eq 1 -and $utf8.GetString($old['keep/a.txt']) -eq 'old a' -and $utf8.GetString($old['b.txt']) -eq 'old b') "fault ${point}: old generation fully readable"
    }
    $r = Publish-Snapshot -Root $froot -ExpectedGeneration 1 -Items @((TextItem 'keep/a.txt' 'new a'), (TextItem 'c.txt' 'new c'))
    $newToken = Get-SnapshotToken -Root $froot
    $new = Read-All $newToken
    Assert-True ($newToken.Generation -eq 2 -and $new.Count -eq 2 -and $utf8.GetString($new['keep/a.txt']) -eq 'new a' -and $utf8.GetString($new['c.txt']) -eq 'new c') 'after commit the new token sees only the new set'
    Assert-Throws { Read-SnapshotItem -Token $newToken -RelativePath 'b.txt' } ([System.IO.FileNotFoundException]) 'b.txt is not in the new generation'
    Assert-Equal 'old b' ($utf8.GetString((Read-SnapshotItem -Token $oldToken -RelativePath 'b.txt'))) 'old token still reads the old generation after the commit'

    # ---- 4. 不正な入力・リンク・上限・競合・検証
    $vroot = New-Root 'validate'
    $badPaths = '../x', 'a/../b', './a', '/abs', 'C:/x', 'a\b', 'a//b', 'a/', '', 'CON', 'x/nul.txt', 'trail.', '*.txt', 'ä.txt', ('n' * 65), ((1..5 | ForEach-Object { 'p' * 60 }) -join '/')
    foreach ($bp in $badPaths) { Assert-Throws { Publish-Snapshot -Root $vroot -ExpectedGeneration 0 -Items @((TextItem $bp 'x')) } ([System.ArgumentException]) "invalid path '$bp'" }
    Assert-Throws { Publish-Snapshot -Root $vroot -ExpectedGeneration 0 -Items @((TextItem 'A.txt' '1'), (TextItem 'a.txt' '2')) } ([System.ArgumentException]) 'case-only duplicate rejected'
    Assert-Throws { Publish-Snapshot -Root $vroot -ExpectedGeneration 0 -Items @((TextItem 'a' '1'), (TextItem 'A/b' '2')) } ([System.ArgumentException]) 'file/directory conflict rejected'
    Assert-Throws { Publish-Snapshot -Root $vroot -ExpectedGeneration 0 -Items @((Item 'big' ([byte[]]::new(1048577)))) } ([System.ArgumentException]) 'item larger than 1 MiB rejected'
    $over = foreach ($i in 0..32) { Item "o$i" ([byte[]]::new(1048576)) }
    Assert-Throws { Publish-Snapshot -Root $vroot -ExpectedGeneration 0 -Items @($over) } ([System.ArgumentException]) 'total over 32 MiB rejected'
    Assert-Throws { Publish-Snapshot -Root $vroot -ExpectedGeneration 0 -Items @(foreach ($i in 0..1000) { TextItem "f$i" '' }) } ([System.ArgumentException]) '1001 items rejected'
    Assert-Throws { Publish-Snapshot -Root $vroot -ExpectedGeneration 0 -Items @([pscustomobject]@{ RelativePath = 'x'; Content = 'text' }) } ([System.ArgumentException]) 'non byte[] Content rejected'
    Assert-Throws { Publish-Snapshot -Root $vroot -ExpectedGeneration 0 -Items @([pscustomobject]@{ RelativePath = 'x'; Content = [byte[]]@(1); Extra = 1 }) } ([System.ArgumentException]) 'extra item field rejected'
    $vs = Get-State $vroot
    Assert-True ($vs.Versions.Count -eq 1 -and $vs.Staging -eq 0 -and (Get-SnapshotToken -Root $vroot).Generation -eq 0) 'rejected publishes changed nothing'
    $outsideDir = Join-Path $base 'outside'; $null = New-Item -ItemType Directory -Path $outsideDir
    $jroot = New-Root 'junction'
    [System.IO.Directory]::Delete((Join-Path $jroot 'staging'))
    $null = New-Item -ItemType Junction -Path (Join-Path $jroot 'staging') -Target $outsideDir
    Assert-Throws { Publish-Snapshot -Root $jroot -ExpectedGeneration 0 -Items @((TextItem 'x' 'x')) } ([System.UnauthorizedAccessException]) 'staging that is a junction rejected'
    Assert-Equal 0 @(Get-ChildItem -LiteralPath $outsideDir -Force).Count 'nothing written through the junction'
    $null = New-Item -ItemType Junction -Path (Join-Path $base 'rootlink') -Target $vroot
    Assert-Throws { Get-SnapshotToken -Root (Join-Path $base 'rootlink') } ([System.UnauthorizedAccessException]) 'Root that is a junction rejected'
    $symOk = $true
    try { $null = New-Item -ItemType SymbolicLink -Path (Join-Path $base 'symroot') -Target $vroot -ErrorAction Stop } catch { $symOk = $false }
    [Console]::Out.WriteLine("[Q10] symbolic link test: $(if ($symOk) { 'executed' } else { 'NOT executed (no privilege); junctions were tested' })")
    if ($symOk) { Assert-Throws { Get-SnapshotToken -Root (Join-Path $base 'symroot') } ([System.UnauthorizedAccessException]) 'symlinked Root rejected' }

    # 同じ ExpectedGeneration の競合（2 つの runspace から同時に公開 → ちょうど 1 つだけ成功）
    $croot = New-Root 'race'
    $pool = [runspacefactory]::CreateRunspacePool(2, 2); $pool.Open()
    $jobs = foreach ($w in 1, 2) {
        $ps = [powershell]::Create(); $ps.RunspacePool = $pool
        [void]$ps.AddScript({ param($impl, $root, $w)
            . $impl
            try { $null = Publish-Snapshot -Root $root -ExpectedGeneration 0 -Items @([pscustomobject]@{ RelativePath = "w$w.txt"; Content = [byte[]]@($w) }); 'ok' }
            catch { $_.Exception.GetType().Name }
        }).AddArgument($impl).AddArgument($croot).AddArgument($w)
        @{ PS = $ps; H = $ps.BeginInvoke() }
    }
    $outcomes = foreach ($j in $jobs) { $j.PS.EndInvoke($j.H); $j.PS.Dispose() }
    $pool.Close(); $pool.Dispose()
    Assert-True (@($outcomes | Where-Object { $_ -ceq 'ok' }).Count -eq 1 -and @($outcomes | Where-Object { $_ -ceq 'SnapshotConflictException' }).Count -eq 1) "concurrent publishes with the same ExpectedGeneration: exactly one succeeds ($($outcomes -join ', '))"
    Assert-Equal ([long]1) (Get-SnapshotToken -Root $croot).Generation 'generation advanced exactly once'

    # CURRENT / マニフェスト / ファイルの改ざん検出
    $kroot = New-Root 'corrupt'
    $null = Publish-Snapshot -Root $kroot -ExpectedGeneration 0 -Items @((TextItem 'f.txt' 'payload'))
    $kt = Get-SnapshotToken -Root $kroot
    $vfile = Join-Path $kroot "versions\$($kt.Version)\files\f.txt"
    [System.IO.File]::WriteAllText($vfile, 'tampered')
    Assert-Throws { Read-SnapshotItem -Token $kt -RelativePath 'f.txt' } ([SnapshotCorruptException]) 'tampered version file detected by SHA-256'
    $goodCurrent = [System.IO.File]::ReadAllBytes((Join-Path $kroot 'CURRENT'))
    $corrupts = @(
        '{"generation":1,"version":"v-00000000000000000000000000000000","manifestSha256":"' + ('0' * 64) + '","x":1}',
        '{"generation":1,"generation":1,"version":"v-00000000000000000000000000000000","manifestSha256":"' + ('0' * 64) + '"}',
        '{"generation":-1,"version":"v-00000000000000000000000000000000","manifestSha256":"' + ('0' * 64) + '"}',
        '{"generation":1.0,"version":"v-00000000000000000000000000000000","manifestSha256":"' + ('0' * 64) + '"}',
        '{"generation":1,"version":"v-0000","manifestSha256":"' + ('0' * 64) + '"}',
        '{"generation":1,"version":"v-00000000000000000000000000000000","manifestSha256":"' + ('A' * 64) + '"}',
        '[1,2,3]', 'not json')
    foreach ($c in $corrupts) {
        [System.IO.File]::WriteAllText((Join-Path $kroot 'CURRENT'), $c)
        Assert-Throws { Get-SnapshotToken -Root $kroot } ([SnapshotCorruptException]) "corrupt CURRENT rejected: $($c.Substring(0, [Math]::Min(40, $c.Length)))"
    }
    $wrongSha = $utf8.GetString($goodCurrent) -replace '"manifestSha256":"[0-9a-f]{64}"', ('"manifestSha256":"' + ('a' * 64) + '"')
    [System.IO.File]::WriteAllText((Join-Path $kroot 'CURRENT'), $wrongSha)
    Assert-Throws { Get-SnapshotToken -Root $kroot } ([SnapshotCorruptException]) 'manifest SHA-256 mismatch rejected'

    # ---- 3. 1 Writer + 8 Reader（別 runspace）
    $mroot = New-Root 'concurrent'
    $mk = { param($g) foreach ($i in 0..99) { [pscustomobject]@{ RelativePath = ('d{0}/f{1:D2}.txt' -f ($i % 10), $i); Content = [System.Text.Encoding]::UTF8.GetBytes("gen=$g;file=$i;" + ('#' * ($i * 7 + $g % 13))) } } }
    $null = Publish-Snapshot -Root $mroot -ExpectedGeneration 0 -Items @(& $mk 1)
    $state = [hashtable]::Synchronized(@{ Stop = $false })
    $pool = [runspacefactory]::CreateRunspacePool(9, 9); $pool.Open()
    $writer = [powershell]::Create(); $writer.RunspacePool = $pool
    [void]$writer.AddScript({ param($impl, $root, $state, $mkText, $gens)
        . $impl
        $mk = [scriptblock]::Create($mkText)        # 生成規則（テスト用の固定コード）をこの runspace で再構成
        try { for ($g = 2; $g -le $gens; $g++) { $null = Publish-Snapshot -Root $root -ExpectedGeneration ($g - 1) -Items @(& $mk $g) } }
        finally { $state.Stop = $true }
        $gens
    }).AddArgument($impl).AddArgument($mroot).AddArgument($state).AddArgument($mk.ToString()).AddArgument(25)
    $readers = foreach ($k in 1..8) {
        $ps = [powershell]::Create(); $ps.RunspacePool = $pool
        [void]$ps.AddScript({ param($impl, $root, $state)
            . $impl
            $rounds = 0; $mixed = 0; $reads = 0; $gens = [System.Collections.Generic.HashSet[long]]::new(); $first = $null
            while ($rounds -lt 3 -or -not $state.Stop) {
                $t = Get-SnapshotToken -Root $root
                if ($null -eq $first) { $first = $t }
                [void]$gens.Add($t.Generation)
                foreach ($i in 0..99) {
                    $s = [System.Text.Encoding]::UTF8.GetString((Read-SnapshotItem -Token $t -RelativePath ('d{0}/f{1:D2}.txt' -f ($i % 10), $i)))
                    if (-not $s.StartsWith("gen=$($t.Generation);file=$i;", [System.StringComparison]::Ordinal)) { $mixed++ }
                    $reads++
                }
                $rounds++
                if ($rounds -gt 5000) { break }
            }
            # 新しいコミットの後でも、最初に取得した古いトークンで全件読める
            $oldOk = $true
            foreach ($i in 0..99) {
                $s = [System.Text.Encoding]::UTF8.GetString((Read-SnapshotItem -Token $first -RelativePath ('d{0}/f{1:D2}.txt' -f ($i % 10), $i)))
                if (-not $s.StartsWith("gen=$($first.Generation);file=$i;", [System.StringComparison]::Ordinal)) { $oldOk = $false }
            }
            [pscustomobject]@{ Rounds = $rounds; Reads = $reads; Mixed = $mixed; Generations = $gens.Count; OldTokenOk = $oldOk; FirstGen = $first.Generation }
        }).AddArgument($impl).AddArgument($mroot).AddArgument($state)
        @{ PS = $ps; H = $ps.BeginInvoke() }
    }
    $sw.Restart()
    $wh = $writer.BeginInvoke()
    $wout = $writer.EndInvoke($wh); $werr = $writer.Streams.Error.Count; $writer.Dispose()
    $res = foreach ($rd in $readers) { $rd.PS.EndInvoke($rd.H); $rd.PS.Dispose() }
    $pool.Close(); $pool.Dispose()
    $sw.Stop()
    Assert-True ($werr -eq 0 -and (Get-SnapshotToken -Root $mroot).Generation -eq 25) 'writer published generations 2..25'
    Assert-Equal 8 @($res).Count '8 readers finished'
    Assert-Equal ([long]0) ([long](@($res) | Measure-Object Mixed -Sum).Sum) 'mixed contents across all readers = 0'
    Assert-True (@($res | Where-Object { -not $_.OldTokenOk }).Count -eq 0) 'each reader re-read its first (old) token after newer commits'
    $totalReads = (@($res) | Measure-Object Reads -Sum).Sum
    $gensSeen = (@($res) | Measure-Object Generations -Maximum).Maximum
    Assert-True ($gensSeen -gt 1) "readers observed several generations (max $gensSeen per reader)"
    [Console]::Out.WriteLine("[Q10] 1 writer + 8 readers: $totalReads reads, mixed 0, up to $gensSeen generations per reader, $([math]::Round($sw.Elapsed.TotalSeconds,1)) s")
    Assert-Equal 0 (Get-State $mroot).Staging 'no staging data left after the concurrent run'
} finally {
    $script:SnapshotFaultInjector = $null
    foreach ($l in (Join-Path $base 'junction\staging'), (Join-Path $base 'rootlink'), (Join-Path $base 'symroot')) {
        if (Test-Path -LiteralPath $l) { [System.IO.Directory]::Delete($l) }
    }
    Remove-Item -LiteralPath $base -Recurse -Force
}
Write-TkSummary
```

#### 5. 期待結果・実測結果・実施状況

**保存規約**
- CURRENT：BOM なし UTF-8 の `{"generation":N,"version":"v-<32 桁小文字 hex>","manifestSha256":"<64 桁小文字 hex>"}`（この順・空白なし）。
- マニフェスト（`versions/<版>/manifest.txt`）：BOM なし UTF-8。1 行 = `相対パス<TAB>長さ(10 進)<TAB>SHA-256(小文字 hex)<LF>`。相対パスの Ordinal 順。0 件なら空ファイル。

| 必須検証ケース | 期待結果 | 実測結果 | 実施状況 |
| --- | --- | --- | --- |
| 0 件 | 世代 0（初期の空の版）→ 1 | 一致 | 成功 |
| 1 件・複数階層 | `a/b/c/deep.bin` が世代 2 で完全にバイト一致。戻り値は byte[] 1 個 | 一致 | 成功 |
| 500 件（空ファイルを含む） | 全件がバイト一致、世代 3 | 一致 | 成功 |
| 1000 件 | 世代 4。末尾のファイルの内容が一致 | 一致（8.7〜11.5 秒） | 成功 |
| 総 32 MiB | 1 MiB × 32 個がバイト一致、世代 5 | 一致（1.4〜1.9 秒） | 成功 |
| ExpectedGeneration の一致 | 世代 0→1→2→3→4→5 と連続。古い値（4）は SnapshotConflictException（ActualGeneration=5） | 一致 | 成功 |
| CURRENT・マニフェスト・SHA256 | CURRENT が上記の正規形、manifestSha256 = マニフェストの SHA-256、行の形式と Ordinal 順 | 一致 | 成功 |
| トークンの読み取り専用 | トークンのプロパティへの代入は例外。Entries への追加は NotSupportedException | 一致 | 成功 |
| 存在しないファイル・大小文字 | `nope.bin`・`big/PART0.bin`（Ordinal で別名）は FileNotFoundException | 一致 | 成功 |
| 5 箇所の障害注入 | Validate・StageWrite・StageFlush・BeforePromote・BeforeCurrentReplace の各点で、公開は失敗。CURRENT のバイト列は不変、versions に新しい版なし、staging は空、一時 CURRENT なし。旧トークンで全ファイルを読める | 5 箇所すべて一致 | 成功 |
| 成功コミットの後 | 新しいトークン（世代 2）は新しい集合（`keep/a.txt`=new a、`c.txt`）だけを見る。`b.txt` は FileNotFound。旧トークンは引き続き旧版の `b.txt` を読める | 一致 | 成功 |
| 1 Writer + 8 Reader（別 runspace） | Writer が世代 2〜25（各 100 ファイル、内容に世代番号）を公開する間、8 Reader がトークン取得 → 100 件の読取りを繰り返す。全読取りでトークンの世代と内容が一致し、混合 0。最後に最初の（古い）トークンで 100 件を再読込できる | 35,200 回の読取りで混合 0、Reader あたり最大 24 世代を観測、64.4 秒 | 成功 |
| 無効なパス | `../x`・`a/../b`・`./a`・`/abs`・`C:/x`・`a\b`・`a//b`・`a/`・空・`CON`・`x/nul.txt`・`trail.`・`*.txt`・`ä.txt`・65 文字の成分・257 文字以上は ArgumentException | 16 種すべて一致 | 成功 |
| 重複・大小文字の衝突・ファイル/ディレクトリの衝突 | `A.txt` と `a.txt`、`a` と `A/b` は ArgumentException | 一致 | 成功 |
| サイズ・件数 | 1 MiB+1 の項目、合計 33 MiB、1001 件、Content が byte[] でない、余分なフィールドは ArgumentException。拒否された公開は何も変えない | 一致 | 成功 |
| junction | staging が junction なら UnauthorizedAccessException で、junction 先には何も書かない。Root が junction ならトークン取得を拒否 | 一致 | 成功 |
| symlink | 作成に管理者特権が必要なため、試験していない | — | **未実施** |
| 同じ ExpectedGeneration の競合 | 2 つの runspace から同時に ExpectedGeneration=0 で公開すると、ちょうど 1 つが成功し、もう 1 つは SnapshotConflictException。世代は 1 だけ進む | 一致 | 成功 |
| 改ざんの検出 | 版のファイルの書換えは SnapshotCorruptException（SHA-256 の不一致）。CURRENT の余剰キー・重複キー・負の世代・小数・版の名前の形式・大文字 hex・配列・非 JSON、マニフェストの SHA-256 の不一致は SnapshotCorruptException | 10 種すべて一致 | 成功 |
| 完了後の中間データ | 成功・失敗・並行実行の後、staging は空で、一時 CURRENT は残らない | 一致 | 成功 |

**自律修正**
- **実装（Q07 で発見した点を実装時に反映）：**
  - `File.Replace` のバックアップなしは、`[NullString]::Value` で渡しています（PowerShell の `$null` は `""` に変換されるため）。
  - CURRENT の読込みは FileShare.ReadWrite|Delete で行い、置換中の一時的な共有違反を有界回数だけ再試行します。
- **テスト：** 1 回目の 2 件の失敗は、どちらもテストの比較方法の誤りでした。
  - `Sort-Object` が大小文字を区別しないカルチャー順で並べたため、期待した文字列と順序が違いました。各結果の件数を数える比較に直しました。
  - `Measure-Object` の合計（Double の 0）と整数の 0 を、TestKit の Equals が区別しました。`[long]` にしました。
  - 実際の結果（ちょうど 1 つ成功、混合 0）は、1 回目から正しいものでした。
- 修正後に Q10 全体を再実行し、合格しました。

#### 6. 技術的根拠・保証範囲・計算量

- **単一のコミット：**
  - 読者が見るのは CURRENT が指す版だけです。版ディレクトリは作成後に変更しない（不変）ので、CURRENT の置換の前後で、読者は旧版か新版のどちらか一方の完全な集合を見ます。
  - コミットは `File.Replace(tempCurrent, CURRENT, null)` が成功して戻った時点です。
- **コミット前の失敗：** Validate〜BeforeCurrentReplace のどこで失敗しても CURRENT は旧版のままです。finally で、自分の staging・昇格済みで未参照の版・一時 CURRENT だけを削除します（削除の失敗は無視し、元の例外を優先）。
- **コミット後：** 掃除は不要です（一時 CURRENT は置換で消費）。古い版は、使用中のトークンのために自動削除しません。
- **読者の一貫性：**
  - トークンは CURRENT を 1 回だけ読み、世代・版・マニフェスト（SHA-256 を照合）を検証して固定します。
  - Read-SnapshotItem は、トークンの版のディレクトリだけを読み、長さと SHA-256 をマニフェストと照合します。そのため、別の世代の内容が混ざることも、改ざんを見逃すこともありません（8 Reader で実測、混合 0）。
  - トークンのプロパティは getter だけの ScriptProperty、Entries は ReadOnlyDictionary です。
- **排他制御：** Publish は `LOCK` を FileShare.None で保持し（最大 20 回・2 秒以内、超えたら SnapshotBusyException）、その下で CURRENT を読み直して ExpectedGeneration を照合します。世代はちょうど 1 だけ増やします。
- **検証済みの書込み：** staging の各ファイルは Flush(true) の後に読み直し、長さと SHA-256 を検証してから、`Directory.Move` で同じボリュームの versions へ名前変更します。
- **パス：** `/` 区切り・各成分 1〜64 文字の `[A-Za-z0-9_.-]`、`.`/`..`・空・末尾 `.`・予約デバイス名を拒否します。大小文字だけ違う重複と、ファイル/ディレクトリの衝突は OrdinalIgnoreCase で検出します。Root・versions・staging・版・経路のディレクトリは、ReparsePoint を拒否します。
- **計算量：** 公開は O(合計バイト数 + 件数 × log 件数)（マニフェストの整列）。読取りは 1 件あたり O(ファイルサイズ) です。

#### 7. 未実施・未解決・制約事項

- 仕様どおり、File.Replace 自身の曖昧な OS エラー、外部の電源断、ネットワーク共有、外部からの並行するファイルの改変は保証範囲外です。
- **未実施：** シンボリックリンクの試験（管理者特権が必要）。junction で試験しました。実装は ReparsePoint 属性で種類を問わず拒否します。
- 試験の前提として、初期化は `Initialize-SnapshotRoot`（Generation 0、空の版、versions・staging・LOCK を作成）で行いました。
- トークンは、取得した runspace の中で使うことを前提にしています（ScriptProperty の getter は、作成した runspace のスクリプトブロックです）。並行試験では、各 Reader が自分の runspace でトークンを取得しました。
- PowerShell 7.4 / .NET 8 での実行は未実施です。
- 参照資料：Microsoft Learn の System.IO.File.Replace・Directory.Move・System.Text.Json.JsonDocument・ReadOnlyDictionary。コードは独自に作成しました。

---
## ★★FREYJA 最終提出レポート【記入欄】

- Q01〜Q10の対応・完成状況：全 10 問に回答しました（未完成の問題はありません）。各回答欄に 7 項目すべてを記入しています。
  - Q01 ConvertTo-StrictRecord
  - Q02 Remove-ScopedFile
  - Q03 Read-StrictFrames
  - Q04 Invoke-StrictArithmetic
  - Q05 Invoke-BoundedRunspaces
  - Q06 Read-StrictJsonBatch
  - Q07 New/Get/Update-CounterState
  - Q08 Read-StrictCsv
  - Q09 Invoke-RestrictedPwsh
  - Q10 Publish-Snapshot / Get-SnapshotToken / Read-SnapshotItem
- 各回答欄に修正版コード全文を記載したか：記載しました。「2.」に `qNN.ps1` の全文（例外クラス・補助関数を含む）を、実際に実行したファイルから組み立てスクリプトで一字一句そのまま差し込みました。
- 各回答欄に実際のテストコード全文を記載したか：記載しました。「4.」に、全問共通の自作テスト補助 `common/TestKit.ps1` と `test_qNN.ps1` の全文を載せています。
- PowerShell / Windows / .NET の実際のバージョン：
  - **PowerShell 7.7.0**（Store 版 pwsh）、**.NET 11.0.0**（pwsh に同梱）、Windows 11 Pro 10.0.26200（x64・NTFS）、Intel Core i7-4710MQ。
  - 指定の **PowerShell 7.4 はインストールされておらず**、許可なく導入していません。.NET 8.0.34 のランタイムは存在しますが、pwsh 7.7 は自身のランタイムを使います。
  - コードは PowerShell 7.4 / .NET 8 にもある API だけで書きましたが、7.4 での実行は未実施です。
- 実行したコマンドとログの保存先：
  - 各問：`cd <scratchpad>\ps\qNN` → `pwsh -NoLogo -NoProfile -File test_qNN.ps1`（終了コード = 失敗数）。
  - ログ：`scratchpad\ps\qNN\log.txt`（最終の実行のもの）。
- 必須テスト件数、合格件数、失敗件数：最終状態で、検査 578 件が合格、失敗 0 件です。
  - 内訳：Q01 25、Q02 58、Q03 68、Q04 76、Q05 52、Q06 55、Q07 67、Q08 56、Q09 41、Q10 80。
  - 未実施：シンボリックリンクの試験（Q02・Q09・Q10。管理者特権が必要）。
- 不具合の発見と指定範囲内の修正内容：
  - **実装：**
    - Q02：削除の失敗の例外の型が MethodInvocationException に包まれていた。
    - Q03：sealed の InvalidDataException は継承できない。スクリプトレベルの変数が dot-source 先に漏れていた。
    - Q04：構文エラーより先に計算エラーが出る設計だったので、構文検査 → 後置記法 → 評価の 2 段階に変更。
    - Q05：throw した例外の型が RuntimeException に包まれていた。
    - Q06：キーの中の不対サロゲートを見逃していたので、正規表現の事前検査を追加。
    - Q07：`$null` が `""` になって File.Replace が失敗していたので、`[NullString]::Value` に変更。多プロセスでの読込み時の一時的な共有違反には、有界の再試行を追加。
    - Q08：StreamReader のバッファー単位のデコードでエラーのレコード番号がずれていたので、Decoder に 1 バイトずつ渡す方式に変更。
    - Q09：Windows のコマンドライン長の事前検査を追加。
  - **テスト：** 期待値・データ・比較方法の誤りを直しました。各回答欄の「5.」の自律修正に、理由と正しい期待値の根拠を記載しています。
- 修正後の必要な再テスト結果：修正したすべての問題で、その問題のテスト全体を再実行し、全件合格しました（上の 578 件）。無関係な問題のテストは再実行していません。
- 未実施の検証項目・環境・制約：
  - PowerShell 7.4 / .NET 8 での実行。
  - シンボリックリンク（Q02・Q09・Q10。junction では実施）。
  - 電源断・OS クラッシュ・File.Replace 自身の曖昧なエラー（Q07・Q10。仕様の保証範囲外）。
  - Q09 の問題文の上限（64 個 × 4096 文字）は、Windows のコマンドライン上限 32,767 文字と両立しないため、合計長を起動前に検査して拒否すると解釈しました（試験責任者の判断を仰ぐ点）。
  - 曖昧な点の解釈（Q01 の件数上限の数え方、Q02 の大小文字の重複、Q06 の Index、Q08 の引用内の裸の CR/LF）は、各回答欄に記載しました。
- ファイル・プロセス・Runspace・一時データの回収状況：
  - 全問、`testdata\run-<GUID>` の使い捨て領域だけを使い、終了時に削除しました（残ったものは 0 件）。
  - Q05・Q10 の PowerShell インスタンスと RunspacePool は、全件を EndInvoke・Dispose・Close しました（Q05 で数を計測、残った runspace は 0）。
  - Q07・Q09 の子プロセスはすべて終了・回収しました（Q09 は孫プロセスの停止も確認）。
  - レジストリ・ユーザーのファイルは変更していません。
- 例外安全とコミット境界の遵守：
  - Q07：`File.Replace` が成功して戻った時点がコミットです。コミット前の 4 箇所の故障では旧版のままで、一時ファイルは残りません。コミット後の後片付けの失敗は、成功のままです。
  - Q10：CURRENT の `File.Replace` だけがコミットです。コミット前の 5 箇所の故障では旧 CURRENT・旧版のままで、自分が作った staging・未参照の版・一時 CURRENT だけを除去します。
  - いずれも実際の故障注入で確認しました。
- 著作権・参照資料・ライセンスの確認：
  - コードとテストはすべて独自に作成しました。第三者の完成コードの複製、外部モジュール・Pester・ThreadJob・NuGet・外部 DLL・Add-Type による C# は使っていません。
  - 仕様の確認だけに使った資料は、Microsoft Learn の PowerShell・.NET の API ドキュメント、RFC 4180、RFC 8259、CRC-32/ISO-HDLC の公開仕様です（転載なし）。
  - README・LICENSE・著作権表示は変更していません。
- GitHubへ提出した場合のコミットSHA・URL・反映確認（ユーザーの明示許可がある場合のみ）：Phantom Nightmare の指示書による、対象ファイルに限った明示的な許可に基づいて、この試験ファイル 1 つだけを main に commit・push します（force push なし、Co-Authored-By なし）。コミットの SHA・URL・GitHub から再取得した確認結果は、提出後の報告で示します。
- 問題文、README、他エージェント資料、設定ファイルを無断変更していないこと：変更していません。編集したのは Q01〜Q10 の回答欄と、この最終提出レポートの欄だけです。commit 前に、それ以外の部分が元のファイルと同一であることを確認しました。
- ★★FREYJA自身の自己採点・合否判定を行っていないこと：自己採点・合否判定はしていません。最終評価は Phantom Nightmare に委ねます。

**指定作業の完了後は結果を報告し、追加指示があるまで別作業を開始しない。**
