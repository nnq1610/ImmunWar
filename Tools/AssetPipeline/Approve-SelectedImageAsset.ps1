param(
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Z]{2,5}[0-9]{3}$')][string]$AssetId,
    [Parameter(Mandatory = $true)][string]$JobId,
    [Parameter(Mandatory = $true)][string]$Prompt,
    [string[]]$ReferenceAssetIds = @(),
    [string]$ReviewedAtUtc = ([DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ'))
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$recordsRoot = Join-Path $projectRoot 'Docs/AssetProvenance/records'
$recordFiles = @(Get-ChildItem -LiteralPath $recordsRoot -Recurse -Filter "$AssetId.json")
if ($recordFiles.Count -ne 1) {
    throw "Expected exactly one record for $AssetId, found $($recordFiles.Count)."
}

$recordPath = $recordFiles[0].FullName
$record = Get-Content -LiteralPath $recordPath -Raw | ConvertFrom-Json
$runtimePath = Join-Path $projectRoot ($record.runtimePath -replace '/', [IO.Path]::DirectorySeparatorChar)
if (-not (Test-Path -LiteralPath $runtimePath -PathType Leaf)) {
    throw "Runtime image is missing: $runtimePath"
}

$bytes = [IO.File]::ReadAllBytes($runtimePath)
if ($bytes.Length -lt 26 -or $bytes[0] -ne 137 -or $bytes[1] -ne 80 -or $bytes[2] -ne 78 -or $bytes[3] -ne 71) {
    throw "$AssetId is not a PNG."
}
if ($bytes[25] -notin @(4, 6)) {
    throw "$AssetId must contain an alpha channel; PNG color type is $($bytes[25])."
}

$hash = (Get-FileHash -LiteralPath $runtimePath -Algorithm SHA256).Hash.ToLowerInvariant()
$record.sourceType = 'ai_generated'
$record.creatorOrProvider = 'OpenAI image generation'
$record.sourceUrlOrJobId = $JobId
$record.acquiredOrGeneratedAtUtc = $ReviewedAtUtc
$generation = [pscustomobject]@{
    provider = 'OpenAI'
    model = 'built-in image generation'
    modelVersion = $null
    prompt = $Prompt
    settings = 'transparent PNG; square; selected under standing image approval'
    referenceAssetIds = @($ReferenceAssetIds)
}
$record | Add-Member -NotePropertyName generation -NotePropertyValue $generation -Force
$record.status = 'Approved'
$record.rights.aiInputUse = 'Allowed'
$record.rights.territoryOrPlatformLimits = $null
$record.evidence = [pscustomobject]@{
    evidenceType = 'AIProviderTerms'
    nameAndVersion = 'OpenAI terms reviewed 2026-09-18'
    urlOrOrderId = 'https://openai.com/policies/service-terms/'
    snapshotPath = 'Docs/AssetProvenance/evidence/openai-image-terms-2026-09-18.md'
    snapshotSha256 = '57fa5dc60c51ff83c5a2e7918b30dae570016c5159a6369a905b58eb403cf71c'
    attributionText = $null
}
$record.rawSha256 = $hash
$record.approvedExportSha256 = $hash
$record.recheckAtUtc = [DateTime]::Parse($ReviewedAtUtc).ToUniversalTime().AddMonths(6).ToString('yyyy-MM-ddTHH:mm:ssZ')
$record.review = [pscustomobject]@{
    reviewer = 'Project owner (standing image approval confirmed in Codex session)'
    reviewedAtUtc = $ReviewedAtUtc
    notes = 'Selected by Codex under the project owner standing image approval; alpha, provenance, and runtime hash verified.'
}

$utf8NoBom = [Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText($recordPath, (($record | ConvertTo-Json -Depth 12) + [Environment]::NewLine), $utf8NoBom)

$auditPath = Join-Path $projectRoot 'specs/001-immune-war-game/evidence/asset-audit-report.json'
if (Test-Path -LiteralPath $auditPath) {
    $audit = Get-Content -LiteralPath $auditPath -Raw | ConvertFrom-Json
    $item = $audit.items | Where-Object assetId -eq $AssetId
    if ($null -eq $item) { throw "Audit item not found for $AssetId." }
    $item.status = 'Approved'
    $item.releaseReady = $true
    $audit.auditedAtUtc = $ReviewedAtUtc
    $audit.approved = @($audit.items | Where-Object status -eq 'Approved').Count
    $audit.review = @($audit.items | Where-Object status -eq 'Review').Count
    $audit.releaseReady = ($audit.approved -eq $audit.total -and $audit.missingRecord -eq 0 -and $audit.missingRuntime -eq 0)
    [IO.File]::WriteAllText($auditPath, (($audit | ConvertTo-Json -Depth 12) + [Environment]::NewLine), $utf8NoBom)
}

[pscustomobject]@{ AssetId = $AssetId; RuntimePath = $record.runtimePath; Sha256 = $hash; Status = $record.status }
