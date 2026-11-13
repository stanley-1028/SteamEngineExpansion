<#
.SYNOPSIS
    Negative tests for the SEE data validator: break one contract at a time in a throw-away copy
    of the mod and prove that tools/validate fails with a specific, actionable message.

.DESCRIPTION
    Each case copies mod\data, the four built assemblies and the golden table into
    build\negative-tests\<case>\ (a temp mod root outside the source tree), applies exactly one
    deliberate defect, and runs the already-built validator DLL against it:

        N1  blueprint fuel rate drifted to 0.13 (the faction-derived value is 0.11)
        N2  one of the 64 localization keys deleted from enUS.csv
        N3  SEE.UI.dll rewritten so it references SEE.Gameplay (layering violation)
        N4  BuildingPipeSpec inserted back into a boiler blueprint (forbidden spec, AD-5)
        N5  SEE_Config.json perf.tickSeconds set out of range (0.1, range 0.5-2.0)
        N6  Water listed back in GoodConsumingBuildingSpec.ConsumedGoods (2026-11-08 tank-separation contract)

    A control run first proves that the untouched copy still prints VALIDATION PASSED, so a
    failure cannot come from the harness itself.

    Exit code 0 only when the control passes and all six cases fail for the expected reason.

.PARAMETER TimberbornDir
    Timberborn install folder (read-only metadata source). Defaults to the standard Steam path.

.PARAMETER KeepTemp
    Keep build\negative-tests for inspection instead of deleting it at the end.

.EXAMPLE
    pwsh -File mod/tools/validate/negative-tests.ps1

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File mod/tools/validate/negative-tests.ps1
#>

[CmdletBinding()]
param(
    [string]$TimberbornDir = 'C:\Program Files (x86)\Steam\steamapps\common\Timberborn',
    [string]$Configuration = 'Release',
    [switch]$KeepTemp
)

$ErrorActionPreference = 'Stop'

$toolsDir = $PSScriptRoot
$modRoot = Split-Path -Parent (Split-Path -Parent $toolsDir)
$validatorProject = Join-Path $modRoot 'tools\validate\validate.csproj'
$validatorDll = Join-Path $modRoot "tools\validate\bin\$Configuration\net9.0\SeeValidate.dll"
$managedDir = Join-Path $TimberbornDir 'Timberborn_Data\Managed'
$goldenSource = Join-Path $modRoot 'tests\SEE.Tests\Golden\blueprints.sha256'
$tempRoot = Join-Path $modRoot 'build\negative-tests'

if (-not (Test-Path $managedDir)) {
    throw "Timberborn managed assemblies not found at '$managedDir'. Pass -TimberbornDir <game folder>."
}

Write-Host '== Build the validator ====================================================' -ForegroundColor Cyan
& dotnet build $validatorProject -c $Configuration -m:1 --nologo -v m
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE"
}

if (-not (Test-Path $validatorDll)) {
    throw "validator assembly not found at $validatorDll"
}

$script:caseFailures = 0
$script:casePasses = 0

function Reset-TempModRoot {
    if (Test-Path $tempRoot) {
        Remove-Item -Recurse -Force $tempRoot
    }

    New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null

    # data\ (the whole shippable data set, so the control run is a true copy)
    Copy-Item (Join-Path $modRoot 'data') (Join-Path $tempRoot 'data') -Recurse -Force

    # golden table (the golden gate resolves it through <modRoot>\tests\...)
    $goldenDir = Join-Path $tempRoot 'tests\SEE.Tests\Golden'
    New-Item -ItemType Directory -Force -Path $goldenDir | Out-Null
    Copy-Item $goldenSource $goldenDir -Force

    # the four shipping assemblies at the paths the assembly gate expects
    foreach ($assembly in @('SEE.Core', 'SEE.Gameplay', 'SEE.Integration', 'SEE.UI')) {
        $target = Join-Path $tempRoot "src\$assembly\bin\$Configuration\netstandard2.1"
        New-Item -ItemType Directory -Force -Path $target | Out-Null
        $source = Join-Path $modRoot "src\$assembly\bin\$Configuration\netstandard2.1\$assembly.dll"
        if (-not (Test-Path $source)) {
            throw "expected build output missing: $source (run mod\build.ps1 first)"
        }

        Copy-Item $source $target -Force
    }

    # SourceFacts requires the src/tools/tests roots to exist; the copied assemblies live under src.
    New-Item -ItemType Directory -Force -Path (Join-Path $tempRoot 'tools') | Out-Null
}

function Invoke-TempValidator {
    # Keep native stderr from becoming a terminating error under Windows PowerShell 5.1; the
    # exit code and the printed gate lines are what this harness asserts on.
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & dotnet $validatorDll (Join-Path $tempRoot 'data') --managed $managedDir --mod $tempRoot 2>&1
        $code = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }

    return [pscustomobject]@{
        ExitCode = $code
        Text     = ($output | Out-String)
    }
}

function Get-RelevantErrors {
    param([string]$Text, [string]$Pattern)
    return @($Text -split "`r?`n" | Where-Object { $_ -match '^ERROR: ' -and $_ -match $Pattern } | Select-Object -First 3)
}

function Invoke-NegativeCase {
    param(
        [string]$Id,
        [string]$Title,
        [string]$ExpectPattern,
        [scriptblock]$Break
    )

    Write-Host ''
    Write-Host "[$Id] $Title" -ForegroundColor Yellow
    Reset-TempModRoot
    & $Break
    $result = Invoke-TempValidator
    $namedErrors = Get-RelevantErrors -Text $result.Text -Pattern $ExpectPattern
    $failedTheRightWay = ($result.ExitCode -ne 0) -and ($result.Text -match 'VALIDATION FAILED') -and ($namedErrors.Count -gt 0)

    if ($failedTheRightWay) {
        $script:casePasses++
        Write-Host "  PASS  exit=$($result.ExitCode), the validator failed and named the broken item"
        foreach ($line in $namedErrors) {
            Write-Host "        $line" -ForegroundColor DarkGray
        }
    }
    else {
        $script:caseFailures++
        Write-Host "  FAIL  expected exit != 0 and /$ExpectPattern/ but got exit=$($result.ExitCode)" -ForegroundColor Red
        Write-Host $result.Text
    }
}

# ---------------------------------------------------------------------------------------------
# Control: the untouched copy must pass, otherwise every later failure is meaningless.
# ---------------------------------------------------------------------------------------------
Write-Host ''
Write-Host '== Control: untouched copy must pass ======================================' -ForegroundColor Cyan
Reset-TempModRoot
$control = Invoke-TempValidator
if ($control.ExitCode -ne 0 -or $control.Text -notmatch 'VALIDATION PASSED') {
    Write-Host $control.Text
    throw "the untouched copy did not print VALIDATION PASSED; the harness or the mod data is broken"
}

Write-Host '  PASS  VALIDATION PASSED on the untouched copy' -ForegroundColor Green

# ---------------------------------------------------------------------------------------------
# N1: authoritative number drift in a generated blueprint (the PLAN §8 risk 3 scenario).
# ---------------------------------------------------------------------------------------------
Invoke-NegativeCase -Id 'N1' -Title 'blueprint fuel rate drifted to 0.13 (faction-derived value is 0.11)' `
    -ExpectPattern 'SEE\.SEEBoilerSpec\.FuelPerHour = 0\.13' -Break {
    $file = Join-Path $tempRoot 'data\Buildings\Power\SteamBoiler\SteamBoiler.Medium.T2.Folktails.blueprint.json'
    $json = Get-Content $file -Raw -Encoding UTF8 | ConvertFrom-Json
    $json.'SEE.SEEBoilerSpec'.FuelPerHour = 0.13
    $json | ConvertTo-Json -Depth 40 | Set-Content $file -Encoding UTF8
}

# ---------------------------------------------------------------------------------------------
# N2: a missing localization key (REQ-UI-04/05).
# ---------------------------------------------------------------------------------------------
Invoke-NegativeCase -Id 'N2' -Title 'one of the 64 localization keys deleted from enUS.csv' `
    -ExpectPattern "missing key 'Status\.SEE\.Running'" -Break {
    $file = Join-Path $tempRoot 'data\Localizations\enUS.csv'
    $lines = Get-Content $file -Encoding UTF8
    $kept = $lines | Where-Object { $_ -notmatch '^Status\.SEE\.Running,' }
    if ($kept.Count -eq $lines.Count) {
        throw 'could not find the Status.SEE.Running line in enUS.csv'
    }

    $kept | Set-Content $file -Encoding UTF8
}

# ---------------------------------------------------------------------------------------------
# N3: layering violation. Rewrite a COPY of the real SEE.UI.dll with one extra assembly
# reference to SEE.Gameplay (tools/validate --fixture-add-reference; build.ps1 never calls it).
# ---------------------------------------------------------------------------------------------
Invoke-NegativeCase -Id 'N3' -Title 'SEE.UI.dll rewritten to reference SEE.Gameplay (layering)' `
    -ExpectPattern 'SEE\.UI must not reference SEE\.Gameplay' -Break {
    $uiTarget = Join-Path $tempRoot "src\SEE.UI\bin\$Configuration\netstandard2.1\SEE.UI.dll"
    $badUi = Join-Path $tempRoot 'bad\SEE.UI.dll'
    & dotnet $validatorDll --fixture-add-reference $uiTarget $badUi 'SEE.Gameplay'
    if ($LASTEXITCODE -ne 0) {
        throw "could not build the layering fixture (exit $LASTEXITCODE)"
    }

    Copy-Item $badUi $uiTarget -Force
}

# ---------------------------------------------------------------------------------------------
# N4: a forbidden spec inserted back into a blueprint (AD-9 / AD-5 red line).
# ---------------------------------------------------------------------------------------------
Invoke-NegativeCase -Id 'N4' -Title 'BuildingPipeSpec inserted back into a boiler blueprint' `
    -ExpectPattern "forbidden spec 'BuildingPipeSpec'" -Break {
    $file = Join-Path $tempRoot 'data\Buildings\Power\SteamBoiler\SteamBoiler.Small.T1.Folktails.blueprint.json'
    $json = Get-Content $file -Raw -Encoding UTF8 | ConvertFrom-Json
    $json | Add-Member -NotePropertyName 'BuildingPipeSpec' -NotePropertyValue ([pscustomobject]@{ InputPorts = @(); OutputPorts = @() })
    $json | ConvertTo-Json -Depth 40 | Set-Content $file -Encoding UTF8
}

# ---------------------------------------------------------------------------------------------
# N5: a shipped config value outside its clamp range (requirements §20.4).
# ---------------------------------------------------------------------------------------------
Invoke-NegativeCase -Id 'N5' -Title 'SEE_Config.json perf.tickSeconds set to 0.1 (range 0.5-2.0)' `
    -ExpectPattern "perf\.tickSeconds' = 0\.1 is outside the supported range" -Break {
    $file = Join-Path $tempRoot 'data\SEE_Config.json'
    $json = Get-Content $file -Raw -Encoding UTF8 | ConvertFrom-Json
    $json.perf.tickSeconds = 0.1
    $json | ConvertTo-Json -Depth 20 | Set-Content $file -Encoding UTF8
}


# ---------------------------------------------------------------------------------------------
# N6: the dedicated water tank contract broken by listing Water back into the fuel consumer
# (2026-11-08 fix: a Water ConsumedGood would share the fuel inventory and re-create the bug).
# ---------------------------------------------------------------------------------------------
Invoke-NegativeCase -Id 'N6' -Title 'Water listed back in GoodConsumingBuildingSpec.ConsumedGoods' `
    -ExpectPattern "Water must not be a GoodConsumingBuildingSpec good|must not consume 'Water'" -Break {
    $file = Join-Path $tempRoot 'data\Buildings\Power\SteamBoiler\SteamBoiler.Small.T1.Folktails.blueprint.json'
    $json = Get-Content $file -Raw -Encoding UTF8 | ConvertFrom-Json
    $goods = @($json.GoodConsumingBuildingSpec.ConsumedGoods)
    $goods += [pscustomobject]@{ GoodId = 'Water'; GoodPerHour = 0.78 }
    $json.GoodConsumingBuildingSpec.ConsumedGoods = $goods
    $json | ConvertTo-Json -Depth 40 | Set-Content $file -Encoding UTF8
}

# ---------------------------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------------------------
if (-not $KeepTemp) {
    if (Test-Path $tempRoot) {
        Remove-Item -Recurse -Force $tempRoot
    }
}

Write-Host ''
Write-Host '== Negative test summary =================================================' -ForegroundColor Cyan
Write-Host ("  cases failed as expected : {0}/6" -f $script:casePasses)
Write-Host ("  cases that did not fail  : {0}" -f $script:caseFailures)
if ($KeepTemp) {
    Write-Host "  temp workspace kept      : $tempRoot"
}

if ($script:caseFailures -gt 0 -or $script:casePasses -ne 6) {
    Write-Host ''
    Write-Host 'NEGATIVE TESTS FAILED' -ForegroundColor Red
    exit 1
}

Write-Host ''
Write-Host 'NEGATIVE TESTS PASSED (control + 6/6 deliberate defects rejected)' -ForegroundColor Green
exit 0
