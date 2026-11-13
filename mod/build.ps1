# Steam Engine Expansion (SEE) build + package script (SEE-P6 final form)
#
#   pwsh -File build.ps1                 # build, test, generate --check, validate, gate, stage
#   pwsh -File build.ps1 -SkipTests      # dev loop: skip the test runner gate
#   pwsh -File build.ps1 -Install        # additionally copy into Documents\Timberborn\Mods
#   pwsh -File build.ps1 -Clean          # wipe build/, obj/ and bin/ first, then the full flow
#
# Gates are data-driven: tools/release/release-gates.json is the authoritative list. Every
# blocking gate must be evaluated and pass, otherwise this script throws (non-zero exit):
#   1. all four shipping projects build with 0 warnings (TreatWarningsAsErrors=true);
#   2. the offline test runner prints "PASS n/n";
#   3. tools/generate-blueprints --check reproduces every generated file ("CHECK PASS");
#   4. the generator's five data gates pass;
#   5. tools/validate prints "VALIDATION PASSED" and every gate group named in
#      release-gates.json prints its own "PASS  <name>" line;
#   6. the staged package build\SteamEngineExpansion\ is verified structurally (exactly four
#      shipping assemblies, 24 blueprints, 2 template collections, 1 bottom-bar tool-group spec,
#      matching localization key counts >= 54 per language, 24 PNG icons + 24 .png.meta.json,
#      the tool-group icon + its isSprite meta, manifest/version tracks) and hashed
#      (build\SteamEngineExpansion.sha256).
#
# See mod/docs/RELEASE_GATES.md for the requirement mapping (requirements 19.2 Go/No-Go).
#
# IMPORTANT: every msbuild invocation uses -m:1. On this machine parallel MSBuild fails
# project-reference restore silently (exit 1, zero messages). See docs/API_FACTS_SEE.md ch.7.
#
# Host note: PowerShell 7 (`pwsh`) is preferred; this script also runs under Windows PowerShell
# 5.1 (`powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1 -Clean`), which is what the
# CI host uses when pwsh 7 is not installed. Keep the file ASCII-only because 5.1 reads a
# BOM-less .ps1 as ANSI.

[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$TimberbornDir = 'C:\Program Files (x86)\Steam\steamapps\common\Timberborn',
    [switch]$SkipTests,
    [switch]$Install,
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$buildDir = Join-Path $root 'build'
$stageDir = Join-Path $buildDir 'SteamEngineExpansion'
$managedDir = Join-Path $TimberbornDir 'Timberborn_Data\Managed'
$modId = 'SteamEngineExpansion'
$releaseGatesPath = Join-Path $root 'tools\release\release-gates.json'

# Shipping assemblies, in dependency order. SEE.Tests is deliberately not on this list:
# the test runner is a net9.0 development artifact and must never be shipped to the game.
$assemblies = @('SEE.Core', 'SEE.Gameplay', 'SEE.Integration', 'SEE.UI')

$script:gateResults = @{}

function Set-Gate {
    param(
        [Parameter(Mandatory = $true)][string]$Id,
        [Parameter(Mandatory = $true)][string]$Detail
    )

    $script:gateResults[$Id] = $Detail
    Write-Host ("  gate {0} PASS  {1}" -f $Id, $Detail) -ForegroundColor DarkGray
}

function Get-Sha256 {
    param([Parameter(Mandatory = $true)][string]$Path)
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-StringSha256 {
    param([Parameter(Mandatory = $true)][string]$Text)
    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($Text)
        return ([System.BitConverter]::ToString($algorithm.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $algorithm.Dispose()
    }
}

function Invoke-Dotnet {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [string]$ExpectPattern,
        [string]$StepName
    )

    Write-Host "> dotnet $($Arguments -join ' ')" -ForegroundColor DarkCyan

    # Native stderr must not become a terminating error: MSBuild occasionally writes an empty
    # stderr line (node reuse), and Windows PowerShell 5.1 turns any redirected stderr line into
    # a NativeCommandError when $ErrorActionPreference is Stop. Failures are detected through the
    # exit code and the expected gate patterns below, so stderr can stay Continue here.
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & dotnet @Arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }

    $output | ForEach-Object { Write-Host $_ }
    if ($exitCode -ne 0) {
        throw "$StepName failed with exit code $exitCode"
    }

    $text = ($output | Out-String)
    if ($ExpectPattern -and $text -notmatch $ExpectPattern) {
        throw "$StepName did not print the expected gate line (pattern: $ExpectPattern)"
    }

    return $text
}

# ------------------------------------------------------------------------------------------
# Preconditions + clean
# ------------------------------------------------------------------------------------------
if (-not (Test-Path $managedDir)) {
    throw "Timberborn assemblies not found at '$managedDir'. Pass -TimberbornDir <game folder>."
}

if (-not (Test-Path $releaseGatesPath)) {
    throw "release gate list not found: $releaseGatesPath"
}

$releaseGates = Get-Content $releaseGatesPath -Raw -Encoding UTF8 | ConvertFrom-Json

if ($Clean) {
    # Stop the MSBuild/VBCSCompiler servers first: a lingering process can hold
    # Mono.Cecil.dll or a project DLL and make Remove-Item fail with Access Denied.
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        & dotnet build-server shutdown 2>&1 | Out-Null
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }

    Remove-Item -Recurse -Force $buildDir -ErrorAction SilentlyContinue
    Get-ChildItem $root -Recurse -Directory -Force |
        Where-Object { $_.Name -in @('obj', 'bin') } |
        ForEach-Object {
            try { Remove-Item -Recurse -Force $_.FullName -ErrorAction Stop }
            catch { Write-Warning "clean skipped (file in use): $($_.FullName)" }
        }
}

# ------------------------------------------------------------------------------------------
# 1. Build the four shipping assemblies (each must report 0 warnings)      -> gate G01
# ------------------------------------------------------------------------------------------
$projects = @(
    'src\SEE.Core\SEE.Core.csproj',
    'src\SEE.Gameplay\SEE.Gameplay.csproj',
    'src\SEE.Integration\SEE.Integration.csproj',
    'src\SEE.UI\SEE.UI.csproj'
)

Write-Host ''
Write-Host '== Build ==================================================================' -ForegroundColor Cyan
foreach ($project in $projects) {
    $name = [System.IO.Path]::GetFileNameWithoutExtension($project)
    $text = Invoke-Dotnet -Arguments @(
        'build', (Join-Path $root $project), '-c', $Configuration, '-m:1', '--nologo', '-v', 'm',
        "-p:TimberbornDir=$TimberbornDir"
    ) -StepName "build $name"

    # TreatWarningsAsErrors already turns warnings into errors; this is the visible 0-warning gate.
    # The regex accepts the English "0 Warning(s)" and the localized dotnet output; the CJK code
    # points are written as \u escapes so this script stays pure ASCII (Windows PowerShell 5.1
    # reads BOM-less .ps1 files as ANSI and would otherwise corrupt the pattern).
    $zero = [regex]::Matches($text, '(?m)^\s*(0)\s*(?:(?:\u4E2A|\u500B)?\u8B66\u544A|Warning\(s\))')
    $warnCounts = [regex]::Matches($text, '(?m)^\s*(\d+)\s*(?:(?:\u4E2A|\u500B)?\u8B66\u544A|Warning\(s\))')
    if ($warnCounts.Count -eq 0) {
        throw "build $name did not report a warning count in its output"
    }

    foreach ($match in $warnCounts) {
        if ([int]$match.Groups[1].Value -ne 0) {
            throw "build $name reported $($match.Groups[1].Value) warning(s); the staging build must stay at 0"
        }
    }

    if ($zero.Count -eq 0) {
        throw "build $name did not report '0 warnings'"
    }
}

Set-Gate 'G01' '0 warnings x 4 shipping projects'

# ------------------------------------------------------------------------------------------
# 2. Offline test runner (zero NuGet): must print PASS n/n              -> gate G02
# ------------------------------------------------------------------------------------------
if (-not $SkipTests) {
    Write-Host ''
    Write-Host '== Tests ==================================================================' -ForegroundColor Cyan
    $testText = Invoke-Dotnet -Arguments @(
        'run', '--project', (Join-Path $root 'tests\SEE.Tests\SEE.Tests.csproj'),
        '-c', $Configuration, '-m:1', '--nologo'
    ) -ExpectPattern 'PASS \d+/\d+' -StepName 'SEE.Tests'

    $passMatch = [regex]::Match($testText, 'PASS (\d+)/(\d+)')
    if (-not $passMatch.Success) {
        throw 'SEE.Tests did not print a PASS n/n line'
    }

    if ($passMatch.Groups[1].Value -ne $passMatch.Groups[2].Value) {
        throw "SEE.Tests reported $($passMatch.Groups[1].Value)/$($passMatch.Groups[2].Value); the gate requires n/n"
    }

    Set-Gate 'G02' ("PASS {0}/{1}" -f $passMatch.Groups[1].Value, $passMatch.Groups[2].Value)
}
else {
    Write-Warning 'test runner skipped (-SkipTests); release gate G02 is SKIPPED, not passed'
}

# ------------------------------------------------------------------------------------------
# 3. Blueprint generator --check: generated files must be byte-identical  -> gates G03/G04
# ------------------------------------------------------------------------------------------
Write-Host ''
Write-Host '== Generate (--check) =====================================================' -ForegroundColor Cyan
$generatorText = Invoke-Dotnet -Arguments @(
    'run', '--project', (Join-Path $root 'tools\generate-blueprints\generate-blueprints.csproj'),
    '-c', $Configuration, '-m:1', '--nologo', '--', '--check'
) -ExpectPattern 'CHECK PASS' -StepName 'tools/generate-blueprints --check'

Set-Gate 'G03' 'CHECK PASS (0 differences)'

if ($generatorText -notmatch 'gates\s*:\s*5/5\s*PASS') {
    throw "the generator did not report 5/5 data gates (output: $generatorText)"
}

Set-Gate 'G04' 'generator data gates 5/5 PASS'

# ------------------------------------------------------------------------------------------
# 4. Data / metadata / assembly validator: must print VALIDATION PASSED   -> gates G05-G17
# ------------------------------------------------------------------------------------------
Write-Host ''
Write-Host '== Validate ==============================================================' -ForegroundColor Cyan
$validatorText = Invoke-Dotnet -Arguments @(
    'run', '--project', (Join-Path $root 'tools\validate\validate.csproj'),
    '-c', $Configuration, '-m:1', '--nologo',
    '--', (Join-Path $root 'data'), '--managed', $managedDir, '--mod', $root
) -ExpectPattern 'VALIDATION PASSED' -StepName 'tools/validate'

Set-Gate 'G05' 'VALIDATION PASSED'

# Every gate that names a validator check must see its own "PASS  <check>" line, so a validator
# that silently stops running a group cannot satisfy the release list by accident.
foreach ($gate in $releaseGates.gates) {
    $check = $gate.validatorCheck
    if (-not $check) {
        continue
    }

    $pattern = '(?m)^PASS\s+' + [regex]::Escape([string]$check) + '\s*$'
    if ($validatorText -notmatch $pattern) {
        throw "release gate $($gate.id) ('$($gate.name)') failed: the validator check '$check' did not print PASS"
    }

    Set-Gate $gate.id $check
}

# ------------------------------------------------------------------------------------------
# 5. Independent golden re-hash (the generator --check and the validator also cover this, but
#    the release gate re-computes the shipped bytes here as a third opinion).      -> G14
# ------------------------------------------------------------------------------------------
$goldenPath = Join-Path $root 'tests\SEE.Tests\Golden\blueprints.sha256'
if (-not (Test-Path $goldenPath)) {
    throw "golden hash table not found: $goldenPath"
}

$goldenEntries = 0
foreach ($line in Get-Content $goldenPath -Encoding UTF8) {
    $trimmed = $line.Trim()
    if ($trimmed.Length -eq 0) {
        continue
    }

    $match = [regex]::Match($trimmed, '^([0-9a-f]{64})  (.+)$')
    if (-not $match.Success) {
        throw "golden line is not '<sha256>  <path>': $trimmed"
    }

    $goldenEntries++
    $relative = $match.Groups[2].Value.Replace('/', '\')
    $file = Join-Path $root $relative
    if (-not (Test-Path $file)) {
        throw "golden entry is missing on disk: $relative"
    }

    if ((Get-Sha256 $file) -ne $match.Groups[1].Value) {
        throw "golden hash mismatch for $relative (the blueprint was edited by hand or the table is stale)"
    }
}

if ($goldenEntries -ne 24) {
    throw "golden table has $goldenEntries entries; expected 24"
}

Set-Gate 'G14' "golden hash 24/24 (build.ps1 independent re-hash)"

# ------------------------------------------------------------------------------------------
# 6. Stage a game-ready mod folder (build\SteamEngineExpansion)          -> gates G18-G22
# ------------------------------------------------------------------------------------------
Write-Host ''
Write-Host '== Stage =================================================================' -ForegroundColor Cyan
if (Test-Path $stageDir) {
    Remove-Item -Recurse -Force $stageDir
}

New-Item -ItemType Directory -Force -Path $stageDir | Out-Null

foreach ($assembly in $assemblies) {
    $dll = Join-Path $root "src\$assembly\bin\$Configuration\netstandard2.1\$assembly.dll"
    if (-not (Test-Path $dll)) {
        throw "expected build output missing: $dll"
    }

    Copy-Item $dll $stageDir
}

# Only game-facing artifacts are staged: the four DLLs above plus manifest, config, blueprints,
# the bottom-bar tool-group spec and icon, localizations, building icons and template collections.
# Generator inputs (engines.json, engines.derived.json, loc_keys.json) and documentation stay out
# of the shipped package.
foreach ($file in @('manifest.json', 'SEE_Config.json')) {
    $source = Join-Path $root "data\$file"
    if (-not (Test-Path $source)) {
        throw "expected data file missing: $source"
    }

    Copy-Item $source $stageDir
}

foreach ($directory in @('Buildings', 'BlockObjectToolGroups', 'Localizations', 'Icons', 'TemplateCollections', 'ToolGroupIcons')) {
    $source = Join-Path $root "data\$directory"
    if (-not (Test-Path $source)) {
        throw "expected data directory missing: $source"
    }

    Copy-Item $source (Join-Path $stageDir $directory) -Recurse -Force
}

# The bottom-bar group is new in P10: the folder copy above cannot fail on a missing member, so
# assert the three staged files exist (their contents are covered by tools/validate before staging).
foreach ($required in @(
    'BlockObjectToolGroups\BlockObjectToolGroup.SteamEngines.blueprint.json',
    'ToolGroupIcons\SteamEngines.png',
    'ToolGroupIcons\SteamEngines.png.meta.json'
)) {
    if (-not (Test-Path (Join-Path $stageDir $required))) {
        throw "staged package is missing $required (the collapsible Steam Engines building group would not appear)"
    }
}

$stagedFiles = @(Get-ChildItem $stageDir -Recurse -File)

# ---- G18: exactly the four shipping assemblies, no dev artifacts --------------------------
$stagedDlls = @($stagedFiles | Where-Object { $_.Extension -eq '.dll' })
if ($stagedDlls.Count -ne 4) {
    throw "staged package must contain exactly 4 DLLs, found $($stagedDlls.Count): $(($stagedDlls | ForEach-Object { $_.Name }) -join ', ')"
}

foreach ($assembly in $assemblies) {
    if (($stagedDlls | ForEach-Object { $_.BaseName }) -notcontains $assembly) {
        throw "staged package is missing $assembly.dll"
    }
}

foreach ($file in $stagedFiles) {
    $relative = $file.FullName.Substring($stageDir.Length + 1).Replace('\', '/')
    if ($relative -match '(^|/)(src|tools|tests)/') {
        throw "staged package must not contain the development folder $relative"
    }

    if ($file.Name -eq 'SEE.Tests.dll') {
        throw 'staged package must not contain SEE.Tests.dll (test runner is not a shipping assembly)'
    }

    if ($file.Extension -in @('.deps.json', '.pdb', '.exe', '.cs', '.ps1', '.xml')) {
        throw "staged package must not contain $($file.Name) (development artifact)"
    }
}

$depsJson = Get-ChildItem $stageDir -Recurse -Filter '*.deps.json' -File
if ($depsJson) {
    throw "staged package must not contain .deps.json (found: $($depsJson.Name -join ', '))"
}

Set-Gate 'G18' '4 shipping assemblies only, no deps/pdb/tests/src/tools'

# ---- G19: 24 blueprints + 2 template collections ------------------------------------------
$blueprints = @(Get-ChildItem (Join-Path $stageDir 'Buildings') -Recurse -Filter '*.blueprint.json' -File)
if ($blueprints.Count -ne 24) {
    throw "expected exactly 24 generated steam boiler blueprints (12 x 2 factions), found $($blueprints.Count)"
}

$collections = @(Get-ChildItem (Join-Path $stageDir 'TemplateCollections') -Recurse -Filter '*.blueprint.json' -File)
if ($collections.Count -ne 2) {
    throw "expected the 2 faction TemplateCollections, found $($collections.Count)"
}

Set-Gate 'G19' '24 blueprints + 2 template collections'

# ---- G20: localization keys per language (header + matching counts, >= 54 baseline) --------
# The exact count is read from the staged files instead of being hard-coded, so an intentional
# key addition (P4B: 54 -> 63, P10 tool group: 63 -> 64) does not have to touch this script
# again; the 54-key requirements baseline stays the floor and both languages must agree.
$localizationBaseline = 54
$localizationKeys = $null
foreach ($language in @('enUS', 'zhTW', 'zhCN')) {
    $languageFile = Join-Path $stageDir "Localizations\$language.csv"
    if (-not (Test-Path $languageFile)) {
        throw "staged package is missing Localizations\$language.csv"
    }

    $rows = @(Get-Content $languageFile -Encoding UTF8 | Where-Object { $_.Trim().Length -gt 0 })
    if ($rows[0] -notmatch '^ID,Text,Comment') {
        throw "$language.csv header must be ID,Text,Comment (found '$($rows[0])')"
    }

    $keyCount = $rows.Count - 1
    if ($keyCount -lt $localizationBaseline) {
        throw "$language.csv has $keyCount keys; expected the ID,Text,Comment header plus at least $localizationBaseline keys"
    }

    if ($null -eq $localizationKeys) {
        $localizationKeys = $keyCount
    }
    elseif ($keyCount -ne $localizationKeys) {
        throw "enUS.csv and zhTW.csv must contain the same number of keys (found $localizationKeys vs $keyCount)"
    }
}

Set-Gate 'G20' ("$localizationKeys localization keys x enUS/zhTW/zhCN")

# ---- G21: 24 PNG icons + 24 isSprite metas -----------------------------------------------
$iconDir = Join-Path $stageDir 'Icons'
$pngIcons = @(Get-ChildItem $iconDir -File | Where-Object { $_.Name -like '*.png' })
$iconMetas = @(Get-ChildItem $iconDir -File | Where-Object { $_.Name -like '*.png.meta.json' })
if ($pngIcons.Count -ne 24) {
    throw "expected 24 icon PNGs, found $($pngIcons.Count)"
}

if ($iconMetas.Count -ne 24) {
    throw "expected 24 icon .png.meta.json files, found $($iconMetas.Count)"
}

foreach ($meta in $iconMetas) {
    $metaJson = Get-Content $meta.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($metaJson.isSprite -ne $true) {
        throw ($meta.Name + ' must contain {"isSprite": true}')
    }

    $pngName = $meta.Name.Substring(0, $meta.Name.Length - '.meta.json'.Length)
    if (-not (Test-Path (Join-Path $iconDir $pngName))) {
        throw "$($meta.Name) has no matching $pngName"
    }
}

Set-Gate 'G21' '24 png icons + 24 isSprite metas'

# ---- G22: version tracks + package hash ---------------------------------------------------
$manifest = Get-Content (Join-Path $stageDir 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($field in @('Name', 'Version', 'Id', 'MinimumGameVersion', 'Description')) {
    if (-not $manifest.$field) {
        throw "manifest.json is missing required field $field"
    }
}

if ($manifest.Id -ne $modId) {
    throw "manifest.json Id must be $modId, found '$($manifest.Id)'"
}

if ($manifest.Version -ne '0.1.0') {
    throw "manifest.json Version must be 0.1.0 (contract section 9), found '$($manifest.Version)'"
}

if (@($manifest.RequiredMods).Count -ne 0 -or @($manifest.OptionalMods).Count -ne 0) {
    throw 'manifest.json RequiredMods and OptionalMods must stay empty (soft compatibility is runtime-detected)'
}

$stagedConfig = Get-Content (Join-Path $stageDir 'SEE_Config.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($stagedConfig.configSchemaVersion -ne 1) {
    throw "SEE_Config.json configSchemaVersion must be 1, found '$($stagedConfig.configSchemaVersion)'"
}

$versionSource = Get-Content (Join-Path $root 'src\SEE.Core\Constants\SEEVersion.cs') -Raw
foreach ($track in @(
    @{ Name = 'ModVersion'; Value = '0.1.0' },
    @{ Name = 'SaveSchemaVersion'; Value = '1' },
    @{ Name = 'ConfigSchemaVersion'; Value = '1' }
)) {
    $trackMatch = [regex]::Match($versionSource, $track.Name + '\s*=\s*"?' + [regex]::Escape($track.Value) + '"?\s*;')
    if (-not $trackMatch.Success) {
        throw "SEEVersion.$($track.Name) must be $($track.Value) (contract section 9 version tracks)"
    }

    if ($track.Name -eq 'ModVersion' -and $manifest.Version -ne $track.Value) {
        throw "manifest Version '$($manifest.Version)' does not match SEEVersion.ModVersion '$($track.Value)'"
    }
}

$inventory = $stagedFiles |
    ForEach-Object {
        [pscustomobject]@{
            Relative = $_.FullName.Substring($stageDir.Length + 1).Replace('\', '/')
            Hash     = Get-Sha256 $_.FullName
            Size     = $_.Length
        }
    } |
    Sort-Object Relative

$inventoryLines = @($inventory | ForEach-Object { "$($_.Hash)  $($_.Relative)" })
$inventoryPath = Join-Path $buildDir 'SteamEngineExpansion.sha256'
$inventoryLines | Set-Content $inventoryPath -Encoding ASCII

$packageHash = Get-StringSha256 ($inventoryLines -join "`n")
$packageSize = ($inventory | Measure-Object -Property Size -Sum).Sum

Set-Gate 'G22' ("version tracks 0.1.0/1/1, package hash " + $packageHash.Substring(0, 12) + '...')

$timbermeshes = @(Get-ChildItem $stageDir -Recurse -Filter '*.timbermesh' -File)

# ------------------------------------------------------------------------------------------
# 7. Release gate audit against tools/release/release-gates.json
# ------------------------------------------------------------------------------------------
Write-Host ''
Write-Host '== Release gates =========================================================' -ForegroundColor Cyan
$missing = @()
foreach ($gate in $releaseGates.gates) {
    if (-not $gate.blocking) {
        continue
    }

    if ($SkipTests -and $gate.id -eq 'G02') {
        continue
    }

    if (-not $script:gateResults.ContainsKey($gate.id)) {
        $missing += $gate.id
    }
}

if ($missing.Count -gt 0) {
    throw "release gate(s) were never evaluated: $($missing -join ', '); see tools/release/release-gates.json"
}

foreach ($gate in $releaseGates.gates) {
    $status = 'NOT-RUN'
    if ($script:gateResults.ContainsKey($gate.id)) {
        $status = 'PASS'
    }
    elseif ($SkipTests -and $gate.id -eq 'G02') {
        $status = 'SKIP'
    }

    Write-Host ("  {0}  {1}  {2}" -f $gate.id, $status, $gate.name)
}

# ------------------------------------------------------------------------------------------
# 8. Build summary + machine-readable report
# ------------------------------------------------------------------------------------------
$report = [ordered]@{
    modId        = $modId
    version      = $manifest.Version
    configuration = $Configuration
    packageDir   = $stageDir
    packageHash  = $packageHash
    inventory    = $inventoryPath
    fileCount    = $inventory.Count
    sizeBytes    = $packageSize
    assemblies   = @($assemblies)
    blueprints   = $blueprints.Count
    collections  = $collections.Count
    localizationKeys = $localizationKeys
    icons        = @{ png = $pngIcons.Count; meta = $iconMetas.Count }
    timbermesh   = $timbermeshes.Count
    gates        = @($releaseGates.gates | ForEach-Object {
            [ordered]@{
                id   = $_.id
                name = $_.name
                pass = $script:gateResults.ContainsKey($_.id)
            }
        })
}

$reportPath = Join-Path $buildDir 'build-report.json'
$report | ConvertTo-Json -Depth 6 | Set-Content $reportPath -Encoding UTF8

Write-Host ''
Write-Host 'BUILD PASSED' -ForegroundColor Green
Write-Host ("  mod id      : {0}" -f $modId)
Write-Host ("  version     : {0} (save schema 1, config schema 1)" -f $manifest.Version)
Write-Host ("  package     : {0}" -f $stageDir)
Write-Host ("  files       : {0}" -f $inventory.Count)
Write-Host ("  size        : {0:N0} KB" -f ($packageSize / 1KB))
Write-Host ("  assemblies  : {0}" -f ($assemblies -join ', '))
Write-Host ("  blueprints  : {0} (+{1} template collections)" -f $blueprints.Count, $collections.Count)
Write-Host ("  localizations: {0} keys x enUS/zhTW/zhCN" -f $localizationKeys)
Write-Host ("  icons       : {0} png + {1} meta" -f $pngIcons.Count, $iconMetas.Count)
Write-Host ("  timbermesh  : {0} (P9 art deliverable; not required for v1)" -f $timbermeshes.Count)
Write-Host ("  package sha : {0}" -f $packageHash)
Write-Host ("  inventory   : {0}" -f $inventoryPath)
Write-Host ("  build report: {0}" -f $reportPath)

# ------------------------------------------------------------------------------------------
# 9. Optional install into the game's mod folder
# ------------------------------------------------------------------------------------------
if ($Install) {
    $modsRoot = Join-Path $env:USERPROFILE 'Documents\Timberborn\Mods'
    $target = Join-Path $modsRoot $modId
    New-Item -ItemType Directory -Force -Path $target | Out-Null
    Copy-Item (Join-Path $stageDir '*') $target -Recurse -Force
    Write-Host "Installed to $target" -ForegroundColor Green
    Write-Host 'Enable it in Timberborn -> Mods, then restart the game.' -ForegroundColor Yellow
}
