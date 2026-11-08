#Requires -Version 5.1
<#
.SYNOPSIS
    Stage the "SEE JSON MVP" package: a zero-C# (JSON only) slice of Steam Engine Expansion,
    generated from the P1 data set in mod\data.

.DESCRIPTION
    The P1 blueprints carry two custom specs (SEE.SEEBoilerSpec / SEE.SEEMaintenanceSpec) that
    only exist once SEE.Gameplay is built and loaded. Without a C# assembly the game cannot
    deserialize them, so this script produces an independent, installable package that keeps
    only vanilla-parseable specs:

      * 24 blueprints (12 size/tier rows x 2 factions) under Buildings/Power/SteamBoiler/
        - custom specs removed
        - MechanicalNodeSpec.PowerOutput = the row's derived horsepower
        - GoodConsumingBuildingSpec keeps a single Log fuel entry (Plank is dropped for T3/T4;
          FullInventoryWorkHours is copied from P1)
        - liquid StockpileSpec keeps the P1 water-tank capacity
        - TransputProviderSpec / BuildingSpec / PlaceableBlockObjectSpec / LabeledEntitySpec /
          Children copied from P1 (same faction model, same tool order)
      * manifest.json (Id SteamEngineExpansionMVP, Name "SEE JSON MVP", Version 0.1.0-mvp)
      * TemplateCollections/ (2 files, appendix-style, 12 blueprints per faction)
      * Localizations/ (enUS.csv + zhTW.csv, the P1 54-key set)
      * Icons/ (24 PNG + 24 *.png.meta.json)
      * no DLL, no .deps.json, no config, no engines.json

.PARAMETER Check
    Regenerate into a temporary folder and byte-compare it with the staged package in -OutDir.
    Exits 1 when the two differ; never modifies -OutDir.

.PARAMETER Install
    Copy the staged package to Documents\Timberborn\Mods\SteamEngineExpansionMVP.
    A sandboxed session cannot write there; on failure the exact robocopy command is printed
    (exit code 4) and nothing is forced.

.PARAMETER InstallTarget
    Override the install folder (used for an in-workspace install smoke test).

.PARAMETER List
    Print the generated package file list.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File stage-json-mvp.ps1
    powershell -NoProfile -ExecutionPolicy Bypass -File stage-json-mvp.ps1 -Check
    powershell -NoProfile -ExecutionPolicy Bypass -File stage-json-mvp.ps1 -Install

.NOTES
    Pure ASCII script: Windows PowerShell 5.1 reads BOM-less .ps1 files as ANSI.
    No network access, no NuGet, writes only below mod\build and (with -Install) the mod folder.
#>
[CmdletBinding()]
param(
    [string]$SourceDir,
    [string]$OutDir,
    [switch]$Check,
    [switch]$Install,
    [string]$InstallTarget,
    [switch]$List
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$script:ModId = 'SteamEngineExpansionMVP'
$script:ModName = 'SEE JSON MVP'
$script:ModVersion = '0.1.0-mvp'
$script:MinGameVersion = '1.1.0.0'
$script:Description = 'JSON-only slice of Steam Engine Expansion: twelve data-driven steam boilers (Small/Medium/Large x Mk.I-Mk.IV) for both Folktails and Iron Teeth. No C# assembly is shipped; this MVP has no maintenance, no proportional fuel scaling, no water consumption and no custom panel.'

$script:RemovedCustomSpecs = @('SEE.SEEBoilerSpec', 'SEE.SEEMaintenanceSpec')
$script:VanillaSpecs = @(
    'BuildingSpec',
    'BuildingModelSpec',
    'EnterableSpec',
    'TemplateSpec',
    'BlockObjectSpec',
    'BuildingAccessibleSpec',
    'MechanicalNodeSpec',
    'TransputProviderSpec',
    'GoodConsumingBuildingSpec',
    'StockpileSpec',
    'MechanicalBuildingSpec',
    'PowerGeneratorParticleControllerSpec',
    'PlaceableBlockObjectSpec',
    'PowerGeneratorSoundsSpec',
    'LabeledEntitySpec',
    'ConstructionSiteProgressVisualizerSpec',
    'MechanicalNodeIlluminatorSpec',
    'TemplateAttachmentsSpec'
)
$script:BlueprintDir = 'Buildings/Power/SteamBoiler'

# ----------------------------------------------------------------------------------------------
# Paths
# ----------------------------------------------------------------------------------------------
$scriptDir = $PSScriptRoot
$toolsDir = Split-Path -Parent $scriptDir
$modRoot = Split-Path -Parent $toolsDir
if (-not $SourceDir) { $SourceDir = Join-Path $modRoot 'data' }
if (-not $OutDir) { $OutDir = Join-Path (Join-Path $modRoot 'build') 'SEE-JSON-MVP' }
$SourceDir = [System.IO.Path]::GetFullPath($SourceDir)
$OutDir = [System.IO.Path]::GetFullPath($OutDir)
$buildRoot = Split-Path -Parent $OutDir
$stagingDir = Join-Path $buildRoot '.SEE-JSON-MVP.staging'
$script:Invariant = [System.Globalization.CultureInfo]::InvariantCulture
$script:Utf8NoBom = New-Object System.Text.UTF8Encoding($false)

# ----------------------------------------------------------------------------------------------
# Small file/JSON helpers
# ----------------------------------------------------------------------------------------------
function Get-TextFile {
    param([string]$Path)
    return [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
}

function Read-JsonFile {
    param([string]$Path)
    return (Get-TextFile $Path | ConvertFrom-Json)
}

function Add-JsonProblem {
    param($Problems, [string]$Message)
    [void]$Problems.Add($Message)
}

# Deterministic JSON writer. ConvertTo-Json is deliberately not used: its escaping/formatting
# changes between PowerShell hosts, which would make -Check depend on the host version.
function ConvertTo-InvariantNumberString {
    param($Value)
    if ($Value -is [decimal]) {
        return $Value.ToString($script:Invariant)
    }
    if (($Value -is [double]) -or ($Value -is [single])) {
        $d = [double]$Value
        if ([double]::IsNaN($d) -or [double]::IsInfinity($d)) { throw 'non-finite JSON number' }
        return $d.ToString('R', $script:Invariant)
    }
    return $Value.ToString($script:Invariant)
}

function ConvertTo-JsonStringLiteral {
    param([string]$Text)
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.Append([char]34)
    foreach ($ch in $Text.ToCharArray()) {
        $code = [int]$ch
        if ($ch -eq [char]34) { [void]$sb.Append('\"') }
        elseif ($ch -eq [char]92) { [void]$sb.Append('\\') }
        elseif ($code -eq 8) { [void]$sb.Append('\b') }
        elseif ($code -eq 9) { [void]$sb.Append('\t') }
        elseif ($code -eq 10) { [void]$sb.Append('\n') }
        elseif ($code -eq 12) { [void]$sb.Append('\f') }
        elseif ($code -eq 13) { [void]$sb.Append('\r') }
        elseif ($code -lt 32) { [void]$sb.Append(('\u{0:x4}' -f $code)) }
        else { [void]$sb.Append($ch) }
    }
    [void]$sb.Append([char]34)
    return $sb.ToString()
}

function ConvertTo-JsonScalar {
    param($Value)
    if ($null -eq $Value) { return 'null' }
    if ($Value -is [bool]) { if ($Value) { return 'true' } return 'false' }
    if ($Value -is [string]) { return (ConvertTo-JsonStringLiteral $Value) }
    if (($Value -is [int]) -or ($Value -is [long]) -or ($Value -is [double]) -or
        ($Value -is [single]) -or ($Value -is [decimal])) {
        return (ConvertTo-InvariantNumberString $Value)
    }
    throw ('unsupported JSON value type: ' + $Value.GetType().FullName)
}

function Format-JsonValue {
    param($Value, [int]$Depth = 0)
    if ($null -eq $Value) { return 'null' }
    $indent = (' ' * ($Depth * 2))
    $childIndent = (' ' * (($Depth + 1) * 2))

    if ($Value -is [System.Management.Automation.PSCustomObject]) {
        $props = @($Value.PSObject.Properties)
        if ($props.Count -eq 0) { return '{}' }
        $lines = New-Object System.Collections.ArrayList
        [void]$lines.Add('{')
        for ($i = 0; $i -lt $props.Count; $i++) {
            $p = $props[$i]
            $sep = ','
            if ($i -eq ($props.Count - 1)) { $sep = '' }
            $child = Format-JsonValue -Value $p.Value -Depth ($Depth + 1)
            [void]$lines.Add($childIndent + (ConvertTo-JsonStringLiteral $p.Name) + ': ' + $child + $sep)
        }
        [void]$lines.Add($indent + '}')
        return ($lines -join "`n")
    }

    if (($Value -is [System.Collections.IEnumerable]) -and -not ($Value -is [string])) {
        $items = @($Value)
        if ($items.Count -eq 0) { return '[]' }
        $lines = New-Object System.Collections.ArrayList
        [void]$lines.Add('[')
        for ($i = 0; $i -lt $items.Count; $i++) {
            $sep = ','
            if ($i -eq ($items.Count - 1)) { $sep = '' }
            $child = Format-JsonValue -Value $items[$i] -Depth ($Depth + 1)
            [void]$lines.Add($childIndent + $child + $sep)
        }
        [void]$lines.Add($indent + ']')
        return ($lines -join "`n")
    }

    return (ConvertTo-JsonScalar $Value)
}

function Write-JsonFile {
    param([string]$Path, $Value)
    $text = Format-JsonValue -Value $Value -Depth 0
    [System.IO.File]::WriteAllText($Path, ($text + "`n"), $script:Utf8NoBom)
}

# Minimal 3-column CSV reader (ID,Text,Comment) that honours quoted fields.
function Read-LocCsv {
    param([string]$Path)
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
    $text = [System.Text.Encoding]::UTF8.GetString($bytes)
    if ($hasBom) { $text = $text.Substring(1) }

    $rows = New-Object System.Collections.ArrayList
    $row = New-Object System.Collections.ArrayList
    $sb = New-Object System.Text.StringBuilder
    $inQuotes = $false
    for ($i = 0; $i -lt $text.Length; $i++) {
        $c = $text[$i]
        if ($inQuotes) {
            if ($c -eq [char]34) {
                if (($i + 1) -lt $text.Length -and $text[$i + 1] -eq [char]34) {
                    [void]$sb.Append([char]34)
                    $i++
                } else {
                    $inQuotes = $false
                }
            } else {
                [void]$sb.Append($c)
            }
        } else {
            if ($c -eq [char]34) {
                $inQuotes = $true
            } elseif ($c -eq [char]44) {
                [void]$row.Add($sb.ToString())
                [void]$sb.Clear()
            } elseif ($c -eq [char]13) {
                # ignore CR
            } elseif ($c -eq [char]10) {
                [void]$row.Add($sb.ToString())
                [void]$sb.Clear()
                [void]$rows.Add($row.ToArray())
                $row.Clear()
            } else {
                [void]$sb.Append($c)
            }
        }
    }
    if (($sb.Length -gt 0) -or ($row.Count -gt 0)) {
        [void]$row.Add($sb.ToString())
        [void]$rows.Add($row.ToArray())
    }

    return [pscustomobject]@{ HasBom = $hasBom; Rows = $rows.ToArray() }
}

# ----------------------------------------------------------------------------------------------
# Context: engines.json + engines.derived.json -> 24 variants
# ----------------------------------------------------------------------------------------------
function New-PackageContext {
    param([string]$DataDir)
    $enginesPath = Join-Path $DataDir 'engines.json'
    $derivedPath = Join-Path $DataDir 'engines.derived.json'
    foreach ($path in @($enginesPath, $derivedPath)) {
        if (-not (Test-Path $path)) { throw "required input missing: $path" }
    }
    $engines = Read-JsonFile $enginesPath
    $derived = Read-JsonFile $derivedPath

    $variants = New-Object System.Collections.ArrayList
    foreach ($row in @($derived.rows)) {
        foreach ($faction in @('Folktails', 'IronTeeth')) {
            $data = $row.variants.$faction
            if ($null -eq $data) { throw ("engines.derived.json: missing variant {0}.{1}.{2}" -f $row.size, $row.tier, $faction) }
            $rel = [string]$data.blueprint
            if ($rel.StartsWith('data/')) { $rel = $rel.Substring(5) }
            $variant = [pscustomobject]@{
                Size        = [string]$row.size
                Tier        = [string]$row.tier
                Faction     = $faction
                Key         = ('{0}.{1}.{2}' -f $row.size, $row.tier, $faction)
                Row         = $row
                Data        = $data
                SourceRel   = $rel
                SourceName  = (Split-Path -Leaf ($rel -replace '/', '\'))
            }
            [void]$variants.Add($variant)
        }
    }
    if ($variants.Count -ne 24) { throw ("engines.derived.json must describe 24 variants, found {0}" -f $variants.Count) }

    return [pscustomobject]@{
        SourceDir = $DataDir
        Engines   = $engines
        Derived   = $derived
        Variants  = $variants.ToArray()
    }
}

# ----------------------------------------------------------------------------------------------
# Generation
# ----------------------------------------------------------------------------------------------
function New-BlueprintObject {
    param($Blueprint, [string]$SourceRel)
    foreach ($name in $script:RemovedCustomSpecs) {
        if ($null -ne $Blueprint.PSObject.Properties[$name]) {
            $Blueprint.PSObject.Properties.Remove($name)
        }
    }

    $gc = $Blueprint.GoodConsumingBuildingSpec
    if ($null -eq $gc) { throw ("{0}: GoodConsumingBuildingSpec missing" -f $SourceRel) }
    $logGoods = @()
    foreach ($good in @($gc.ConsumedGoods)) {
        if ([string]$good.GoodId -eq 'Log') { $logGoods += $good }
    }
    if ($logGoods.Count -ne 1) {
        throw ("{0}: expected exactly one Log fuel entry in P1 data, found {1}" -f $SourceRel, $logGoods.Count)
    }
    $gc.ConsumedGoods = @($logGoods)
    return $Blueprint
}

function New-StagedPackage {
    param([string]$Root, $Context)

    if (Test-Path $Root) { Remove-Item -Recurse -Force $Root }
    $bpRoot = Join-Path $Root ($script:BlueprintDir -replace '/', '\')
    $collRoot = Join-Path $Root 'TemplateCollections'
    $locRoot = Join-Path $Root 'Localizations'
    $iconRoot = Join-Path $Root 'Icons'
    foreach ($dir in @($bpRoot, $collRoot, $locRoot, $iconRoot)) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }

    $manifest = [pscustomobject][ordered]@{
        Name               = $script:ModName
        Version            = $script:ModVersion
        Id                 = $script:ModId
        MinimumGameVersion = $script:MinGameVersion
        Description        = $script:Description
        RequiredMods       = @()
        OptionalMods       = @()
    }
    Write-JsonFile -Path (Join-Path $Root 'manifest.json') -Value $manifest

    $generated = New-Object System.Collections.ArrayList
    $iconNames = New-Object System.Collections.ArrayList
    foreach ($variant in $Context.Variants) {
        $sourcePath = Join-Path $Context.SourceDir ($variant.SourceRel -replace '/', '\')
        if (-not (Test-Path $sourcePath)) { throw "source blueprint not found: $sourcePath" }
        $source = Read-JsonFile $sourcePath
        $blueprint = New-BlueprintObject -Blueprint $source -SourceRel $variant.SourceRel
        $outPath = Join-Path $bpRoot $variant.SourceName
        Write-JsonFile -Path $outPath -Value $blueprint

        $iconName = Split-Path -Leaf ([string]$blueprint.LabeledEntitySpec.Icon)
        if (-not $iconNames.Contains($iconName)) { [void]$iconNames.Add($iconName) }
        [void]$generated.Add($variant.SourceName)
    }

    foreach ($collection in @('TemplateCollection.Buildings.Folktails.blueprint.json',
                              'TemplateCollection.Buildings.IronTeeth.blueprint.json')) {
        $from = Join-Path (Join-Path $Context.SourceDir 'TemplateCollections') $collection
        if (-not (Test-Path $from)) { throw "source template collection missing: $from" }
        Copy-Item $from (Join-Path $collRoot $collection)
    }

    foreach ($language in @('enUS.csv', 'zhTW.csv')) {
        $from = Join-Path (Join-Path $Context.SourceDir 'Localizations') $language
        if (-not (Test-Path $from)) { throw "source localization missing: $from" }
        Copy-Item $from (Join-Path $locRoot $language)
    }

    $sourceIconRoot = Join-Path $Context.SourceDir 'Icons'
    foreach ($iconName in $iconNames) {
        foreach ($suffix in @('.png', '.png.meta.json')) {
            $leaf = $iconName + $suffix
            $from = Join-Path $sourceIconRoot $leaf
            if (-not (Test-Path $from)) { throw "source icon missing: $from" }
            Copy-Item $from (Join-Path $iconRoot $leaf)
        }
    }

    return [pscustomobject]@{
        Blueprints  = $generated.Count
        Collections = 2
        Languages   = 2
        Icons       = $iconNames.Count
    }
}

# ----------------------------------------------------------------------------------------------
# Validation
# ----------------------------------------------------------------------------------------------
function New-CheckErrors {
    # The unary comma stops PowerShell from unrolling the empty ArrayList into "no output",
    # which would silently turn the caller's error list into $null.
    $list = New-Object System.Collections.ArrayList
    return ,$list
}

function Add-CheckResult {
    param($Results, [string]$Name, [bool]$Ok, [string]$Detail)
    [void]$Results.Add([pscustomobject]@{ Name = $Name; Ok = $Ok; Detail = $Detail })
}

function Test-StagedPackage {
    param([string]$Root, $Context)
    $results = New-Object System.Collections.ArrayList
    $allProblems = New-Object System.Collections.ArrayList

    $bpRoot = Join-Path $Root ($script:BlueprintDir -replace '/', '\')
    $bpFiles = @(Get-ChildItem $bpRoot -File -Filter '*.blueprint.json' -ErrorAction SilentlyContinue |
        Sort-Object Name)

    # ---- 1. manifest ------------------------------------------------------------------------
    $errs = New-CheckErrors
    $manifest = $null
    $manifestPath = Join-Path $Root 'manifest.json'
    if (-not (Test-Path $manifestPath)) {
        Add-JsonProblem $errs 'manifest.json missing'
    } else {
        try { $manifest = Read-JsonFile $manifestPath } catch { Add-JsonProblem $errs ('manifest.json unreadable: ' + $_.Exception.Message) }
    }
    if ($null -ne $manifest) {
        $expectedFields = @('Name', 'Version', 'Id', 'MinimumGameVersion', 'Description', 'RequiredMods', 'OptionalMods')
        $actualFields = @($manifest.PSObject.Properties | ForEach-Object { $_.Name })
        foreach ($field in $expectedFields) {
            if ($actualFields -notcontains $field) { Add-JsonProblem $errs ("manifest.json missing field {0}" -f $field) }
        }
        foreach ($field in $actualFields) {
            if ($expectedFields -notcontains $field) { Add-JsonProblem $errs ("manifest.json has unexpected field {0}" -f $field) }
        }
        if ([string]$manifest.Name -ne $script:ModName) { Add-JsonProblem $errs ("manifest.json Name is '{0}', expected '{1}'" -f $manifest.Name, $script:ModName) }
        if ([string]$manifest.Version -ne $script:ModVersion) { Add-JsonProblem $errs ("manifest.json Version is '{0}', expected '{1}'" -f $manifest.Version, $script:ModVersion) }
        if ([string]$manifest.Id -ne $script:ModId) { Add-JsonProblem $errs ("manifest.json Id is '{0}', expected '{1}'" -f $manifest.Id, $script:ModId) }
        if ([string]$manifest.MinimumGameVersion -ne $script:MinGameVersion) { Add-JsonProblem $errs ("manifest.json MinimumGameVersion is '{0}', expected '{1}'" -f $manifest.MinimumGameVersion, $script:MinGameVersion) }
        if ([string]::IsNullOrWhiteSpace([string]$manifest.Description)) { Add-JsonProblem $errs 'manifest.json Description must not be empty' }
        foreach ($listField in @('RequiredMods', 'OptionalMods')) {
            $property = $manifest.PSObject.Properties[$listField]
            if ($null -eq $property) {
                Add-JsonProblem $errs ("manifest.json missing field {0}" -f $listField)
            } elseif (@($property.Value).Count -ne 0) {
                Add-JsonProblem $errs ("manifest.json {0} must be an empty array" -f $listField)
            }
        }
    }
    Add-CheckResult $results 'manifest.json' ($errs.Count -eq 0) ("Id {0}, Version {1}, 7 fields, no required/optional mods" -f $script:ModId, $script:ModVersion)
    foreach ($e in $errs) { Add-JsonProblem $allProblems $e }

    # ---- 2. blueprint set -------------------------------------------------------------------
    $errs = New-CheckErrors
    $expectedNames = @($Context.Variants | ForEach-Object { $_.SourceName } | Sort-Object)
    $actualNames = @($bpFiles | ForEach-Object { $_.Name } | Sort-Object)
    foreach ($name in $expectedNames) {
        if ($actualNames -notcontains $name) { Add-JsonProblem $errs ("missing blueprint {0}" -f $name) }
    }
    foreach ($name in $actualNames) {
        if ($expectedNames -notcontains $name) { Add-JsonProblem $errs ("unexpected blueprint {0}" -f $name) }
    }
    $totalBlueprintFiles = @(Get-ChildItem $Root -Recurse -File -Filter '*.blueprint.json').Count
    if ($totalBlueprintFiles -ne 26) { Add-JsonProblem $errs ("package must hold exactly 26 *.blueprint.json (24 buildings + 2 collections), found {0}" -f $totalBlueprintFiles) }
    Add-CheckResult $results 'blueprints (24)' ($errs.Count -eq 0) ("12 size/tier rows x 2 factions; 24 building blueprints + 2 template collections")
    foreach ($e in $errs) { Add-JsonProblem $allProblems $e }

    # ---- 3. vanilla-spec whitelist ----------------------------------------------------------
    $errs = New-CheckErrors
    $requiredSpecs = @('BuildingSpec', 'BuildingModelSpec', 'TemplateSpec', 'BlockObjectSpec',
        'BuildingAccessibleSpec', 'MechanicalNodeSpec', 'TransputProviderSpec',
        'GoodConsumingBuildingSpec', 'StockpileSpec', 'MechanicalBuildingSpec',
        'PlaceableBlockObjectSpec', 'LabeledEntitySpec', 'Children')
    $allowedSpecs = @($script:VanillaSpecs + 'Children')
    foreach ($file in $bpFiles) {
        $raw = Get-TextFile $file.FullName
        foreach ($customSpec in $script:RemovedCustomSpecs) {
            $hits = [regex]::Matches($raw, [regex]::Escape($customSpec)).Count
            if ($hits -ne 0) { Add-JsonProblem $errs ("{0}: {1} hit(s) for {2}" -f $file.Name, $hits, $customSpec) }
        }
        $bp = Read-JsonFile $file.FullName
        $keys = @($bp.PSObject.Properties | ForEach-Object { $_.Name })
        foreach ($key in $keys) {
            if ($allowedSpecs -notcontains $key) { Add-JsonProblem $errs ("{0}: non-vanilla top-level spec '{1}'" -f $file.Name, $key) }
            if ($key.Contains('.')) { Add-JsonProblem $errs ("{0}: dotted spec name '{1}' cannot resolve without a mod assembly" -f $file.Name, $key) }
        }
        foreach ($required in $requiredSpecs) {
            if ($keys -notcontains $required) { Add-JsonProblem $errs ("{0}: required vanilla spec {1} missing" -f $file.Name, $required) }
        }
    }
    Add-CheckResult $results 'vanilla specs only' ($errs.Count -eq 0) ("24 files, 0 SEEBoilerSpec/SEEMaintenanceSpec hits, all top-level specs vanilla")
    foreach ($e in $errs) { Add-JsonProblem $allProblems $e }

    # ---- per-blueprint value checks ---------------------------------------------------------
    $errsPower = New-CheckErrors
    $errsFuel = New-CheckErrors
    $errsCost = New-CheckErrors
    $errsBlock = New-CheckErrors
    $errsModel = New-CheckErrors
    $errsLoc = New-CheckErrors
    $blueprintsByKey = @{}
    $locMaps = @{}

    $locRoot = Join-Path $Root 'Localizations'
    foreach ($language in @('enUS.csv', 'zhTW.csv')) {
        $locPath = Join-Path $locRoot $language
        if (-not (Test-Path $locPath)) { Add-JsonProblem $errsLoc ("missing localization {0}" -f $language); continue }
        try { $locMaps[$language] = Read-LocCsv $locPath } catch { Add-JsonProblem $errsLoc ("{0} unreadable: {1}" -f $language, $_.Exception.Message) }
    }
    $locText = @{}
    foreach ($language in $locMaps.Keys) {
        $map = @{}
        foreach ($row in @($locMaps[$language].Rows)) {
            if (@($row).Count -eq 3) { $map[[string]$row[0]] = [string]$row[1] }
        }
        $locText[$language] = $map
    }

    foreach ($variant in $Context.Variants) {
        $file = Join-Path $bpRoot $variant.SourceName
        if (-not (Test-Path $file)) { continue }
        $bp = Read-JsonFile $file
        $blueprintsByKey[$variant.Key] = $bp

        # power output
        $power = $bp.MechanicalNodeSpec.PowerOutput
        if ([int]$power -ne [int]$variant.Data.horsepower) {
            Add-JsonProblem $errsPower ("{0}: PowerOutput {1} != engines.derived.json {2}" -f $variant.Key, $power, $variant.Data.horsepower)
        }
        if ([int]$bp.MechanicalNodeSpec.PowerInput -ne 0) { Add-JsonProblem $errsPower ("{0}: PowerInput must be 0" -f $variant.Key) }
        if ([bool]$bp.MechanicalNodeSpec.IsShaft) { Add-JsonProblem $errsPower ("{0}: IsShaft must be false" -f $variant.Key) }

        # fuel + water
        if ([int]$bp.GoodConsumingBuildingSpec.FullInventoryWorkHours -ne [int]$Context.Engines.fullInventoryWorkHours) {
            Add-JsonProblem $errsFuel ("{0}: FullInventoryWorkHours {1} != engines.json {2}" -f $variant.Key, $bp.GoodConsumingBuildingSpec.FullInventoryWorkHours, $Context.Engines.fullInventoryWorkHours)
        }
        $goods = @($bp.GoodConsumingBuildingSpec.ConsumedGoods)
        if ($goods.Count -ne 1) {
            Add-JsonProblem $errsFuel ("{0}: expected exactly 1 ConsumedGoods entry (Log), found {1}" -f $variant.Key, $goods.Count)
        } else {
            if ([string]$goods[0].GoodId -ne 'Log') { Add-JsonProblem $errsFuel ("{0}: fuel good must be Log, found {1}" -f $variant.Key, $goods[0].GoodId) }
            $sourcePath = Join-Path $Context.SourceDir ($variant.SourceRel -replace '/', '\')
            $source = Read-JsonFile $sourcePath
            $sourceLog = @($source.GoodConsumingBuildingSpec.ConsumedGoods | Where-Object { [string]$_.GoodId -eq 'Log' })
            if ($sourceLog.Count -eq 0) {
                Add-JsonProblem $errsFuel ("{0}: P1 source has no Log fuel entry" -f $variant.Key)
            } elseif ([decimal]$goods[0].GoodPerHour -ne [decimal]$sourceLog[0].GoodPerHour) {
                Add-JsonProblem $errsFuel ("{0}: Log rate {1} != P1 {2}" -f $variant.Key, $goods[0].GoodPerHour, $sourceLog[0].GoodPerHour)
            }
        }
        if ([int]$bp.StockpileSpec.MaxCapacity -ne [int]$variant.Data.waterTankGoods) {
            Add-JsonProblem $errsFuel ("{0}: water tank {1} != engines.derived.json {2}" -f $variant.Key, $bp.StockpileSpec.MaxCapacity, $variant.Data.waterTankGoods)
        }
        if ([string]$bp.StockpileSpec.WhitelistedGoodType -ne 'Liquid') {
            Add-JsonProblem $errsFuel ("{0}: water StockpileSpec must whitelist Liquid" -f $variant.Key)
        }

        # cost / tool order
        if ([int]$bp.BuildingSpec.ScienceCost -ne [int]$variant.Row.scienceCost) {
            Add-JsonProblem $errsCost ("{0}: ScienceCost {1} != engines.json {2}" -f $variant.Key, $bp.BuildingSpec.ScienceCost, $variant.Row.scienceCost)
        }
        $expectedCosts = @($variant.Row.buildCost)
        $actualCosts = @($bp.BuildingSpec.BuildingCost)
        if ($expectedCosts.Count -ne $actualCosts.Count) {
            Add-JsonProblem $errsCost ("{0}: BuildingCost entries {1} != {2}" -f $variant.Key, $actualCosts.Count, $expectedCosts.Count)
        } else {
            for ($i = 0; $i -lt $expectedCosts.Count; $i++) {
                if ([string]$actualCosts[$i].Id -ne [string]$expectedCosts[$i].good) {
                    Add-JsonProblem $errsCost ("{0}: BuildingCost[{1}].Id {2} != {3}" -f $variant.Key, $i, $actualCosts[$i].Id, $expectedCosts[$i].good)
                }
                if ([int]$actualCosts[$i].Amount -ne [int]$expectedCosts[$i].amount) {
                    Add-JsonProblem $errsCost ("{0}: BuildingCost[{1}].Amount {2} != {3}" -f $variant.Key, $i, $actualCosts[$i].Amount, $expectedCosts[$i].amount)
                }
            }
        }
        $placeable = $bp.PlaceableBlockObjectSpec
        if ([string]$placeable.ToolGroupId -ne 'Power') { Add-JsonProblem $errsCost ("{0}: ToolGroupId must be Power" -f $variant.Key) }
        if ([int]$placeable.ToolOrder -ne [int]$variant.Data.toolOrder) {
            Add-JsonProblem $errsCost ("{0}: ToolOrder {1} != engines.derived.json {2}" -f $variant.Key, $placeable.ToolOrder, $variant.Data.toolOrder)
        }
        if ([int]$placeable.ToolOrder -le 50) { Add-JsonProblem $errsCost ("{0}: ToolOrder {1} must be after the vanilla Steam Engine (50)" -f $variant.Key, $placeable.ToolOrder) }

        # block object footprint (X width, Y depth, Z height)
        $size = $bp.BlockObjectSpec.Size
        if ([int]$size.X -ne [int]$variant.Row.footprint.x -or [int]$size.Y -ne [int]$variant.Row.footprint.y -or [int]$size.Z -ne [int]$variant.Row.height) {
            Add-JsonProblem $errsBlock ("{0}: Size {1}x{2}x{3} != {4}x{5}x{6}" -f $variant.Key, $size.X, $size.Y, $size.Z, $variant.Row.footprint.x, $variant.Row.footprint.y, $variant.Row.height)
        }
        $blocks = @($bp.BlockObjectSpec.Blocks)
        $expectedBlocks = [int]$size.X * [int]$size.Y * [int]$size.Z
        if ($blocks.Count -ne $expectedBlocks) {
            Add-JsonProblem $errsBlock ("{0}: Blocks {1} != X*Y*Z {2}" -f $variant.Key, $blocks.Count, $expectedBlocks)
        }

        # models / children (same faction placeholder model from P1)
        $finished = $bp.Children.'#Finished'.TimbermeshSpec.Model
        $expectedFinished = $Context.Engines.artPlaceholders.models.$($variant.Faction).finished
        if ([string]$finished -ne [string]$expectedFinished) {
            Add-JsonProblem $errsModel ("{0}: finished model {1} != {2}" -f $variant.Key, $finished, $expectedFinished)
        }
        $unfinished = $bp.Children.'#Unfinished'.Children
        if ($null -eq $unfinished) {
            Add-JsonProblem $errsModel ("{0}: #Unfinished.Children missing" -f $variant.Key)
        } else {
            $stage = $unfinished.ConstructionStage0.TimbermeshSpec.Model
            $expectedStage = $Context.Engines.artPlaceholders.models.$($variant.Faction).constructionStage
            if ([string]$stage -ne [string]$expectedStage) {
                Add-JsonProblem $errsModel ("{0}: construction stage model {1} != {2}" -f $variant.Key, $stage, $expectedStage)
            }
            $expectedBase = [string]$Context.Engines.artPlaceholders.constructionBases.($variant.Size)
            $expectedBaseName = ((Split-Path -Leaf ($expectedBase -replace '/', '\')) -replace '\.blueprint$', '') + '#nested'
            $baseProperty = $unfinished.PSObject.Properties[$expectedBaseName]
            if ($null -eq $baseProperty) {
                Add-JsonProblem $errsModel ("{0}: {1} missing" -f $variant.Key, $expectedBaseName)
            } elseif ([string]$baseProperty.Value.BlueprintPath -ne $expectedBase) {
                Add-JsonProblem $errsModel ("{0}: construction base {1} != {2}" -f $variant.Key, $baseProperty.Value.BlueprintPath, $expectedBase)
            }
        }

        # loc keys referenced by the blueprint
        $label = $bp.LabeledEntitySpec
        foreach ($locKey in @([string]$label.DisplayNameLocKey, [string]$label.DescriptionLocKey, [string]$label.FlavorDescriptionLocKey)) {
            if ([string]::IsNullOrEmpty($locKey)) { Add-JsonProblem $errsLoc ("{0}: empty loc key" -f $variant.Key); continue }
            foreach ($language in @('enUS.csv', 'zhTW.csv')) {
                if (-not $locText.ContainsKey($language)) { continue }
                if (-not $locText[$language].ContainsKey($locKey)) {
                    Add-JsonProblem $errsLoc ("{0}: loc key {1} missing from {2}" -f $variant.Key, $locKey, $language)
                }
            }
        }
    }

    Add-CheckResult $results 'power output' ($errsPower.Count -eq 0) ("24/24 PowerOutput equal engines.derived.json (faction-adjusted hp), PowerInput 0")
    Add-CheckResult $results 'fuel / water' ($errsFuel.Count -eq 0) ("Log only (T3/T4 Plank dropped), FullInventoryWorkHours 25, liquid StockpileSpec = P1 tank")
    Add-CheckResult $results 'cost / tool order' ($errsCost.Count -eq 0) ("ScienceCost/BuildingCost = engines.json, ToolGroup Power, ToolOrder 110-132 (> vanilla 50)")
    Add-CheckResult $results 'block object' ($errsBlock.Count -eq 0) ("Size X/Y/Z = footprint/height; Blocks = X*Y*Z")
    Add-CheckResult $results 'models / children' ($errsModel.Count -eq 0) ("same-faction P1 placeholder model + matching ConstructionBase")
    Add-CheckResult $results 'blueprint loc keys' ($errsLoc.Count -eq 0) ("every LabeledEntitySpec key present in enUS and zhTW")
    foreach ($bucket in @($errsPower, $errsFuel, $errsCost, $errsBlock, $errsModel, $errsLoc)) {
        foreach ($e in $bucket) { Add-JsonProblem $allProblems $e }
    }

    # ---- 4. localizations -------------------------------------------------------------------
    $errs = New-CheckErrors
    $locFiles = @(Get-ChildItem $locRoot -File -ErrorAction SilentlyContinue | Sort-Object Name)
    if ($locFiles.Count -ne 2) { Add-JsonProblem $errs ("Localizations must hold exactly 2 files, found {0}" -f $locFiles.Count) }
    $keySets = @{}
    foreach ($language in @('enUS.csv', 'zhTW.csv')) {
        $path = Join-Path $locRoot $language
        if (-not (Test-Path $path)) { Add-JsonProblem $errs ("missing {0}" -f $language); continue }
        $parsed = Read-LocCsv $path
        if ($parsed.HasBom) { Add-JsonProblem $errs ("{0} must not have a UTF-8 BOM" -f $language) }
        $rows = @($parsed.Rows)
        if ($rows.Count -lt 1) { Add-JsonProblem $errs ("{0} is empty" -f $language); continue }
        $header = @($rows[0])
        if ($header.Count -ne 3 -or [string]$header[0] -ne 'ID' -or [string]$header[1] -ne 'Text' -or [string]$header[2] -ne 'Comment') {
            Add-JsonProblem $errs ("{0}: header must be ID,Text,Comment" -f $language)
        }
        $keys = New-Object System.Collections.ArrayList
        $seen = @{}
        for ($i = 1; $i -lt $rows.Count; $i++) {
            $cells = @($rows[$i])
            if ($cells.Count -ne 3) { Add-JsonProblem $errs ("{0} line {1}: expected 3 columns, found {2}" -f $language, ($i + 1), $cells.Count); continue }
            $key = [string]$cells[0]
            if ([string]::IsNullOrWhiteSpace($key)) { Add-JsonProblem $errs ("{0} line {1}: empty key" -f $language, ($i + 1)); continue }
            if ($seen.ContainsKey($key)) { Add-JsonProblem $errs ("{0} line {1}: duplicate key {2}" -f $language, ($i + 1), $key); continue }
            $seen[$key] = $true
            [void]$keys.Add($key)
        }
        $keySets[$language] = $keys.ToArray()
        if ($keys.Count -ne 54) { Add-JsonProblem $errs ("{0}: expected 54 keys, found {1}" -f $language, $keys.Count) }
    }
    if ($keySets.ContainsKey('enUS.csv') -and $keySets.ContainsKey('zhTW.csv')) {
        foreach ($key in $keySets['enUS.csv']) {
            if ($keySets['zhTW.csv'] -notcontains $key) { Add-JsonProblem $errs ("key {0} present in enUS but missing from zhTW" -f $key) }
        }
        foreach ($key in $keySets['zhTW.csv']) {
            if ($keySets['enUS.csv'] -notcontains $key) { Add-JsonProblem $errs ("key {0} present in zhTW but missing from enUS" -f $key) }
        }
    }
    Add-CheckResult $results 'localizations (54 keys x 2)' ($errs.Count -eq 0) ("enUS/zhTW 3-column CSV, identical 54-key sets, no BOM")
    foreach ($e in $errs) { Add-JsonProblem $allProblems $e }

    # ---- 5. template collections ------------------------------------------------------------
    $errs = New-CheckErrors
    $collRoot = Join-Path $Root 'TemplateCollections'
    $collFiles = @(Get-ChildItem $collRoot -File -Filter '*.blueprint.json' -ErrorAction SilentlyContinue | Sort-Object Name)
    if ($collFiles.Count -ne 2) { Add-JsonProblem $errs ("TemplateCollections must hold exactly 2 files, found {0}" -f $collFiles.Count) }
    $factionByCollection = @{
        'Buildings.Folktails' = 'Folktails'
        'Buildings.IronTeeth' = 'IronTeeth'
    }
    foreach ($collectionFile in $collFiles) {
        $collection = $null
        try { $collection = Read-JsonFile $collectionFile.FullName } catch { Add-JsonProblem $errs ("{0} unreadable: {1}" -f $collectionFile.Name, $_.Exception.Message); continue }
        $spec = $collection.TemplateCollectionSpec
        if ($null -eq $spec) { Add-JsonProblem $errs ("{0}: TemplateCollectionSpec missing" -f $collectionFile.Name); continue }
        $collectionId = [string]$spec.CollectionId
        if (-not $factionByCollection.ContainsKey($collectionId)) {
            Add-JsonProblem $errs ("{0}: unexpected CollectionId {1}" -f $collectionFile.Name, $collectionId)
            continue
        }
        $faction = $factionByCollection[$collectionId]
        $entries = @($spec.'Blueprints#append')
        if ($entries.Count -ne 12) { Add-JsonProblem $errs ("{0}: expected 12 appended blueprints, found {1}" -f $collectionFile.Name, $entries.Count) }
        # A TemplateCollection entry is the blueprint path without the trailing ".json".
        $expectedPaths = @($Context.Variants | Where-Object { $_.Faction -eq $faction } | ForEach-Object {
            $script:BlueprintDir + '/' + ($_.SourceName -replace '\.json$', '') })
        foreach ($entry in $entries) {
            $entryPath = [string]$entry
            $target = Join-Path $Root (($entryPath + '.json') -replace '/', '\')
            if (-not (Test-Path $target)) { Add-JsonProblem $errs ("{0}: appended blueprint {1} does not exist" -f $collectionFile.Name, $entryPath) }
            if ($entryPath -notmatch ('\.' + $faction + '\.blueprint$')) {
                Add-JsonProblem $errs ("{0}: appended blueprint {1} does not belong to {2}" -f $collectionFile.Name, $entryPath, $faction)
            }
            if ($expectedPaths -notcontains $entryPath) {
                Add-JsonProblem $errs ("{0}: appended blueprint {1} is not one of the generated {2} rows" -f $collectionFile.Name, $entryPath, $faction)
            }
        }
        foreach ($path in $expectedPaths) {
            if (@($entries | ForEach-Object { [string]$_ }) -notcontains $path) {
                Add-JsonProblem $errs ("{0}: generated blueprint {1} is not appended" -f $collectionFile.Name, $path)
            }
        }
    }
    Add-CheckResult $results 'template collections' ($errs.Count -eq 0) ("2 factions x 12 appendix entries, every entry resolves to a generated blueprint")
    foreach ($e in $errs) { Add-JsonProblem $allProblems $e }

    # ---- 6. icons ---------------------------------------------------------------------------
    $errs = New-CheckErrors
    $iconRoot = Join-Path $Root 'Icons'
    $pngFiles = @(Get-ChildItem $iconRoot -File -Filter '*.png' -ErrorAction SilentlyContinue)
    $metaFiles = @(Get-ChildItem $iconRoot -File -Filter '*.png.meta.json' -ErrorAction SilentlyContinue)
    if ($pngFiles.Count -ne 24) { Add-JsonProblem $errs ("expected 24 PNG icons, found {0}" -f $pngFiles.Count) }
    if ($metaFiles.Count -ne 24) { Add-JsonProblem $errs ("expected 24 *.png.meta.json, found {0}" -f $metaFiles.Count) }
    foreach ($variant in $Context.Variants) {
        $bp = $blueprintsByKey[$variant.Key]
        if ($null -eq $bp) { continue }
        $iconValue = [string]$bp.LabeledEntitySpec.Icon
        if (-not $iconValue.StartsWith('Icons/')) { Add-JsonProblem $errs ("{0}: Icon must be under Icons/, found {1}" -f $variant.Key, $iconValue); continue }
        $iconLeaf = Split-Path -Leaf ($iconValue -replace '/', '\')
        $pngPath = Join-Path $iconRoot ($iconLeaf + '.png')
        $metaPath = Join-Path $iconRoot ($iconLeaf + '.png.meta.json')
        if (-not (Test-Path $pngPath)) { Add-JsonProblem $errs ("{0}: icon {1}.png missing" -f $variant.Key, $iconLeaf) }
        if (-not (Test-Path $metaPath)) {
            Add-JsonProblem $errs ("{0}: icon {1}.png.meta.json missing" -f $variant.Key, $iconLeaf)
        } else {
            try {
                $meta = Read-JsonFile $metaPath
                if (-not [bool]$meta.isSprite) { Add-JsonProblem $errs ("{0}: {1}.png.meta.json must contain isSprite true" -f $variant.Key, $iconLeaf) }
            } catch {
                Add-JsonProblem $errs ("{0}: {1}.png.meta.json unreadable" -f $variant.Key, $iconLeaf)
            }
        }
    }
    Add-CheckResult $results 'icons (24 png + 24 meta)' ($errs.Count -eq 0) ("every LabeledEntitySpec.Icon resolves to a PNG and an isSprite meta file")
    foreach ($e in $errs) { Add-JsonProblem $allProblems $e }

    # ---- 7. shipping shape ----------------------------------------------------------------
    $errs = New-CheckErrors
    $allFiles = @(Get-ChildItem $Root -Recurse -File | Sort-Object FullName)
    $forbiddenExtensions = @('.dll', '.pdb', '.xml', '.cs')
    foreach ($file in $allFiles) {
        if ($forbiddenExtensions -contains $file.Extension.ToLowerInvariant()) {
            Add-JsonProblem $errs ("forbidden file {0}" -f $file.Name)
        }
        if ($file.Name -match '\.(deps|runtimeconfig)\.json$') {
            Add-JsonProblem $errs ("forbidden file {0}" -f $file.Name)
        }
        if (@('engines.json', 'engines.derived.json', 'SEE_Config.json', 'loc_keys.json', 'ART_PLACEHOLDER.md') -contains $file.Name) {
            Add-JsonProblem $errs ("generator input must not ship: {0}" -f $file.Name)
        }
    }
    $expectedFileCount = 1 + 24 + 2 + 2 + 48
    if ($allFiles.Count -ne $expectedFileCount) {
        Add-JsonProblem $errs ("expected {0} files (manifest 1 + blueprints 24 + collections 2 + loc 2 + icons 48), found {1}" -f $expectedFileCount, $allFiles.Count)
    }
    Add-CheckResult $results 'shipping shape' ($errs.Count -eq 0) ("77 files, no DLL / .deps.json / config / generator inputs")
    foreach ($e in $errs) { Add-JsonProblem $allProblems $e }

    return [pscustomobject]@{
        Results  = $results.ToArray()
        Problems = $allProblems.ToArray()
    }
}

function Write-CheckTable {
    param($Results)
    foreach ($result in $Results) {
        $status = 'FAIL'
        if ($result.Ok) { $status = 'PASS' }
        Write-Host ('  {0,-30} {1}  {2}' -f $result.Name, $status, $result.Detail)
    }
}

# ----------------------------------------------------------------------------------------------
# Package comparison (-Check)
# ----------------------------------------------------------------------------------------------
function Compare-StagedPackages {
    param([string]$ReferenceDir, [string]$CandidateDir)
    $differences = New-Object System.Collections.ArrayList
    if (-not (Test-Path $ReferenceDir)) {
        Add-JsonProblem $differences ("staged package not found: {0} (run without -Check first)" -f $ReferenceDir)
        return ,$differences.ToArray()
    }
    if (-not (Test-Path $CandidateDir)) {
        Add-JsonProblem $differences ("candidate package not found: {0}" -f $CandidateDir)
        return ,$differences.ToArray()
    }

    $hash = @{}
    foreach ($file in @(Get-ChildItem $ReferenceDir -Recurse -File)) {
        $relative = $file.FullName.Substring($ReferenceDir.Length).TrimStart('\')
        $hash[$relative] = (Get-FileHash $file.FullName -Algorithm SHA256).Hash
    }
    $candidateNames = @{}
    foreach ($file in @(Get-ChildItem $CandidateDir -Recurse -File)) {
        $relative = $file.FullName.Substring($CandidateDir.Length).TrimStart('\')
        $candidateNames[$relative] = $true
        if (-not $hash.ContainsKey($relative)) {
            Add-JsonProblem $differences ("only in the fresh generation: {0}" -f $relative)
            continue
        }
        $candidateHash = (Get-FileHash $file.FullName -Algorithm SHA256).Hash
        if ($candidateHash -ne $hash[$relative]) {
            Add-JsonProblem $differences ("content differs: {0}" -f $relative)
        }
    }
    foreach ($relative in $hash.Keys) {
        if (-not $candidateNames.ContainsKey($relative)) {
            Add-JsonProblem $differences ("only in the staged package: {0}" -f $relative)
        }
    }
    return ,$differences.ToArray()
}

# ----------------------------------------------------------------------------------------------
# Install
# ----------------------------------------------------------------------------------------------
function Invoke-Install {
    param([string]$PackageDir, [string]$TargetDir)
    if (-not (Test-Path $PackageDir)) { throw "staged package not found: $PackageDir" }

    $copyCommand = ('robocopy "{0}" "{1}" /MIR /NFL /NDL /NJH /NJS' -f $PackageDir, $TargetDir)
    try {
        if (Test-Path $TargetDir) {
            $existingManifestPath = Join-Path $TargetDir 'manifest.json'
            if (-not (Test-Path $existingManifestPath)) {
                throw ("target folder exists but is not an SEE JSON MVP package: {0}" -f $TargetDir)
            }
            $existing = Read-JsonFile $existingManifestPath
            if ([string]$existing.Id -ne $script:ModId) {
                throw ("target folder belongs to another mod (Id '{0}'), refusing to touch it: {1}" -f $existing.Id, $TargetDir)
            }
            Remove-Item -Recurse -Force $TargetDir
        }
        New-Item -ItemType Directory -Force -Path $TargetDir | Out-Null
        Copy-Item (Join-Path $PackageDir '*') $TargetDir -Recurse -Force
        Write-Host ("Installed SEE JSON MVP to {0}" -f $TargetDir) -ForegroundColor Green
        Write-Host 'Enable it in Timberborn -> Mods (or it is enabled by default) and restart the game.' -ForegroundColor Yellow
        return 0
    } catch {
        Write-Warning ("install failed: {0}" -f $_.Exception.Message)
        Write-Host ''
        Write-Host 'The sandbox cannot write outside the workspace. Run this from a normal desktop terminal:' -ForegroundColor Yellow
        Write-Host ('  New-Item -ItemType Directory -Force -Path "{0}" | Out-Null' -f $TargetDir)
        Write-Host ('  ' + $copyCommand)
        return 4
    }
}

# ----------------------------------------------------------------------------------------------
# Main
# ----------------------------------------------------------------------------------------------
Write-Host ''
Write-Host '== SEE JSON MVP ==========================================================' -ForegroundColor Cyan
Write-Host ("  source       : {0}" -f $SourceDir)
Write-Host ("  output       : {0}" -f $OutDir)
Write-Host ''

if (-not (Test-Path $SourceDir)) { Write-Error "source dir not found: $SourceDir"; exit 2 }
if (-not (Test-Path $buildRoot)) { New-Item -ItemType Directory -Force -Path $buildRoot | Out-Null }

$context = New-PackageContext -DataDir $SourceDir
Write-Host ("  P1 variants  : {0} (12 size/tier rows x 2 factions)" -f $context.Variants.Count)

# Always generate fresh into the staging folder; -Check compares it against the shipped package.
if (Test-Path $stagingDir) { Remove-Item -Recurse -Force $stagingDir }
Write-Host ''
Write-Host '== Generate =============================================================' -ForegroundColor Cyan
$stats = New-StagedPackage -Root $stagingDir -Context $context
Write-Host ("  blueprints       : {0}" -f $stats.Blueprints)
Write-Host ("  template colls   : {0}" -f $stats.Collections)
Write-Host ("  localizations    : {0}" -f $stats.Languages)
Write-Host ("  icons            : {0} png + {0} meta" -f $stats.Icons)

Write-Host ''
Write-Host '== Validate =============================================================' -ForegroundColor Cyan
$validation = Test-StagedPackage -Root $stagingDir -Context $context
Write-CheckTable $validation.Results
$failed = @($validation.Results | Where-Object { -not $_.Ok })
if ($failed.Count -gt 0 -or $validation.Problems.Count -gt 0) {
    Write-Host ''
    Write-Host ("VALIDATION FAILED: {0} check(s), {1} problem(s)" -f $failed.Count, $validation.Problems.Count) -ForegroundColor Red
    $shown = 0
    foreach ($problem in $validation.Problems) {
        if ($shown -ge 40) { Write-Host '  ... (more problems omitted)'; break }
        Write-Host ("  - {0}" -f $problem)
        $shown++
    }
    exit 1
}
Write-Host ''
Write-Host ("VALIDATION PASSED: {0}/{0} checks, 0 errors" -f $validation.Results.Count) -ForegroundColor Green

if ($Check) {
    Write-Host ''
    Write-Host '== Check (staged package vs fresh generation) ============================' -ForegroundColor Cyan
    $differences = Compare-StagedPackages -ReferenceDir $OutDir -CandidateDir $stagingDir
    if ($differences.Count -gt 0) {
        Write-Host ("CHECK FAILED: {0} difference(s)" -f $differences.Count) -ForegroundColor Red
        $shown = 0
        foreach ($difference in $differences) {
            if ($shown -ge 40) { Write-Host '  ... (more differences omitted)'; break }
            Write-Host ("  - {0}" -f $difference)
            $shown++
        }
        Remove-Item -Recurse -Force $stagingDir -ErrorAction SilentlyContinue
        exit 1
    }
    $fileCount = @(Get-ChildItem $OutDir -Recurse -File).Count
    Write-Host ("CHECK PASS: 0 differences ({0} files, SHA256 per file)" -f $fileCount) -ForegroundColor Green
    Remove-Item -Recurse -Force $stagingDir -ErrorAction SilentlyContinue
    exit 0
}

# Promote the staged generation to the shipped folder.
if (Test-Path $OutDir) { Remove-Item -Recurse -Force $OutDir }
Move-Item $stagingDir $OutDir

$files = @(Get-ChildItem $OutDir -Recurse -File)
$sizeBytes = ($files | Measure-Object -Sum Length).Sum
Write-Host ''
Write-Host ("Staged JSON MVP package: {0}" -f $OutDir) -ForegroundColor Green
Write-Host ("  blueprints    : {0}" -f @(Get-ChildItem (Join-Path $OutDir 'Buildings') -Recurse -File -Filter '*.blueprint.json').Count)
Write-Host ("  collections   : {0}" -f @(Get-ChildItem (Join-Path $OutDir 'TemplateCollections') -File -Filter '*.blueprint.json').Count)
Write-Host ("  localizations : {0}" -f ((@(Get-ChildItem (Join-Path $OutDir 'Localizations') -File | ForEach-Object { $_.Name })) -join ', '))
Write-Host ("  icons         : {0} png + {1} meta" -f @(Get-ChildItem (Join-Path $OutDir 'Icons') -File -Filter '*.png').Count,
                                                    @(Get-ChildItem (Join-Path $OutDir 'Icons') -File -Filter '*.png.meta.json').Count)
Write-Host ("  dll / deps    : 0 / 0")
Write-Host ("  files         : {0}" -f $files.Count)
Write-Host ("  size          : {0:N0} KB" -f ($sizeBytes / 1KB))

if ($List) {
    Write-Host ''
    Write-Host '== File list ============================================================' -ForegroundColor Cyan
    foreach ($file in @(Get-ChildItem $OutDir -Recurse -File | Sort-Object FullName)) {
        $relative = $file.FullName.Substring($OutDir.Length).TrimStart('\') -replace '\\', '/'
        Write-Host ("  {0,7}  {1}" -f $file.Length, $relative)
    }
}

if ($Install) {
    Write-Host ''
    Write-Host '== Install ==============================================================' -ForegroundColor Cyan
    if (-not $InstallTarget) {
        $InstallTarget = Join-Path $env:USERPROFILE 'Documents\Timberborn\Mods\SteamEngineExpansionMVP'
    }
    Write-Host ("  target       : {0}" -f $InstallTarget)
    $code = Invoke-Install -PackageDir $OutDir -TargetDir $InstallTarget
    exit $code
}

exit 0
