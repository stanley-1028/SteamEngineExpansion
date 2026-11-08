# P4 acceptance (P4B updated): localization parity between CONTRACT_SEE section 5, the shipped CSV
# files and the keys the SEE.UI sources actually reference. AC-16 requires at least 54 keys; P4B
# adds 9 keys (63 total). Prints "diff=0" for every checked direction.
$ErrorActionPreference = 'Stop'
$mod = 'C:\Users\user\Downloads\timber steam engine\mod'
$contract = Get-Content (Join-Path $mod 'docs\CONTRACT_SEE.md') -Raw
$section = $contract.Substring(
    $contract.IndexOf('## 5. ', [StringComparison]::Ordinal),
    $contract.IndexOf('## 6. ', [StringComparison]::Ordinal) - $contract.IndexOf('## 5. ', [StringComparison]::Ordinal))
$tableLines = @($section -split "`r?`n" | Where-Object { $_.TrimStart().StartsWith('|') })
$contractKeys = $tableLines |
    ForEach-Object { [regex]::Matches($_, '`((?:Building|Status|UI)\.SEE\.[A-Za-z0-9.]+)`') } |
    ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique

function Read-CsvKeys([string]$name) {
    $path = Join-Path $mod "data\Localizations\$name"
    $keys = @(Get-Content $path | ForEach-Object {
        $line = $_.TrimEnd("`r")
        if ($line.Length -eq 0 -or $line.StartsWith('ID,')) { return }
        $comma = $line.IndexOf(',')
        if ($comma -le 0) { return }
        $key = $line.Substring(0, $comma)
        if ($key -match '^(Building|Status|UI)\.SEE\.') { $key }
    })
    return @($keys | Sort-Object -Unique)
}

$enUs = Read-CsvKeys 'enUS.csv'
$zhTw = Read-CsvKeys 'zhTW.csv'

# Keys referenced by SEE.UI: SeeLocKeys constants used in C# + the 12 generated display names.
$locSource = Get-Content (Join-Path $mod 'src\SEE.Core\Constants\SeeLocKeys.cs') -Raw
$constMap = @{}
[regex]::Matches($locSource, 'const string (\w+) = "([^"]+)"') | ForEach-Object {
    $constMap[$_.Groups[1].Value] = $_.Groups[2].Value
}
$uiKeys = New-Object System.Collections.Generic.HashSet[string]
foreach ($file in Get-ChildItem (Join-Path $mod 'src\SEE.UI') -Recurse -Filter *.cs |
         Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }) {
    $text = Get-Content $file.FullName -Raw
    [regex]::Matches($text, 'SeeLocKeys\.([A-Za-z0-9_]+)') | ForEach-Object {
        $name = $_.Groups[1].Value
        if ($constMap.ContainsKey($name)) { [void]$uiKeys.Add($constMap[$name]) }
    }
    if ($text -match 'SeeLocKeys\.DisplayName\(') {
        foreach ($size in 'Small', 'Medium', 'Large') {
            foreach ($tier in 'T1', 'T2', 'T3', 'T4') {
                [void]$uiKeys.Add("Building.SEE.SteamBoiler.$size.$tier.DisplayName")
            }
        }
    }
}
# Keep only real SEE localization keys: some SeeLocKeys constants are key *fragments*
# (Good. / .DisplayName) used to build vanilla good-name keys, not SEE keys of their own.
$uiKeys = @($uiKeys | Where-Object { $_ -match '^(Building|Status|UI)\.SEE\.' } | Sort-Object -Unique)

# Blueprint label keys (section B) and the reserved keys documented in UiContractTests.
$blueprintKeys = New-Object System.Collections.Generic.HashSet[string]
foreach ($file in Get-ChildItem (Join-Path $mod 'data\Buildings\Power\SteamBoiler') -Filter *.blueprint.json) {
    $text = Get-Content $file.FullName -Raw
    [regex]::Matches($text, 'Building\.SEE\.[A-Za-z0-9.]+') | ForEach-Object { [void]$blueprintKeys.Add($_.Value) }
}
$blueprintKeys = @($blueprintKeys | Sort-Object -Unique)

$reserved = @(
    'Status.SEE.PipeSupply', 'Status.SEE.BeltSupply', 'Status.SEE.BeltNoPower',
    'Status.SEE.PipeContaminated', 'UI.SEE.Error.MaterialsMissing', 'UI.SEE.Error.BeltNoPower',
    'UI.SEE.Panel.NextMaintenance', 'UI.SEE.Panel.Materials'
)

function Show-Diff([string]$label, [string[]]$left, [string[]]$right) {
    $missing = @($left | Where-Object { $right -notcontains $_ })
    $extra = @($right | Where-Object { $left -notcontains $_ })
    Write-Host ("{0,-46} missing={1} extra={2} diff={3}" -f $label, $missing.Count, $extra.Count,
        ($missing.Count + $extra.Count))
    if ($missing.Count -gt 0) { Write-Host ('  left-only : ' + ($missing -join ', ')) }
    if ($extra.Count -gt 0) { Write-Host ('  right-only: ' + ($extra -join ', ')) }
}

Write-Host ("contract_keys={0}  enUS_keys={1}  zhTW_keys={2}  ui_used={3}  blueprint_label={4}  reserved={5}" -f
    $contractKeys.Count, $enUs.Count, $zhTw.Count, $uiKeys.Count, $blueprintKeys.Count, $reserved.Count)
Show-Diff 'CONTRACT_SEE vs enUS' $contractKeys $enUs
Show-Diff 'CONTRACT_SEE vs zhTW' $contractKeys $zhTw
Show-Diff 'enUS vs zhTW' $enUs $zhTw
Show-Diff 'SEE.UI used keys vs enUS' $uiKeys $enUs
$union = @($uiKeys + $blueprintKeys + $reserved | Sort-Object -Unique)
Show-Diff 'enUS vs (UI + label + reserved)' $enUs $union
$forbidden = @($enUs | Where-Object { $_ -match 'SteamEngine|SteamPlant|TWSteamEngine' })
Write-Host ("forbidden_prefix_hits={0}" -f $forbidden.Count)
$simple = @(Get-Content (Join-Path $mod 'data\Localizations\zhTW.csv') |
    Where-Object { $_ -match '^(Building|Status|UI)\.SEE\.' } |
    Where-Object { $_ -match '[\u9505\u7ef4\u62a4\u53d1\u52a8\u9f7f\u8f6e\u538b]' })
Write-Host ("zhTW_simplified_hits={0}" -f $simple.Count)
