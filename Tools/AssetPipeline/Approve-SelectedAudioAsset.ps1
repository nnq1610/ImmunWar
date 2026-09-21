param(
    [Parameter(Mandatory = $true)][ValidatePattern('^AUD[0-9]{3}$')][string]$AssetId,
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
    throw "Runtime audio is missing: $runtimePath"
}

$stream = [IO.File]::OpenRead($runtimePath)
try {
    $reader = [IO.BinaryReader]::new($stream)
    $riff = [Text.Encoding]::ASCII.GetString($reader.ReadBytes(4))
    $null = $reader.ReadInt32()
    $wave = [Text.Encoding]::ASCII.GetString($reader.ReadBytes(4))
    $fmt = [Text.Encoding]::ASCII.GetString($reader.ReadBytes(4))
    $fmtSize = $reader.ReadInt32()
    $format = $reader.ReadInt16()
    $channels = $reader.ReadInt16()
    $sampleRate = $reader.ReadInt32()
    $byteRate = $reader.ReadInt32()
    $blockAlign = $reader.ReadInt16()
    $bits = $reader.ReadInt16()
    if ($riff -ne 'RIFF' -or $wave -ne 'WAVE' -or $fmt -ne 'fmt ' -or $format -ne 1 -or $channels -ne 1 -or $sampleRate -ne 48000 -or $bits -ne 16 -or $byteRate -ne 96000 -or $blockAlign -ne 2) {
        throw "$AssetId must be PCM WAV, mono, 48 kHz, 16-bit."
    }
    if ($fmtSize -gt 16) { $reader.ReadBytes($fmtSize - 16) | Out-Null }
    $chunk = [Text.Encoding]::ASCII.GetString($reader.ReadBytes(4))
    while ($chunk -ne 'data' -and $stream.Position -lt $stream.Length) {
        $size = $reader.ReadInt32()
        $reader.ReadBytes($size) | Out-Null
        $chunk = [Text.Encoding]::ASCII.GetString($reader.ReadBytes(4))
    }
    if ($chunk -ne 'data' -or $reader.ReadInt32() -le 0) { throw "$AssetId has no PCM sample data." }
}
finally {
    $stream.Dispose()
}

$hash = (Get-FileHash -LiteralPath $runtimePath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($record.rawSha256 -ne $hash -or $record.approvedExportSha256 -ne $hash) {
    throw "$AssetId runtime hash does not match its technical-intake record. Regenerate or re-audit before approval."
}

$record.status = 'Approved'
$record.recheckAtUtc = [DateTime]::Parse($ReviewedAtUtc).ToUniversalTime().AddMonths(6).ToString('yyyy-MM-ddTHH:mm:ssZ')
$record.review = [pscustomobject]@{
    reviewer = 'Project owner (explicit audio listening approval in Codex session)'
    reviewedAtUtc = $ReviewedAtUtc
    notes = 'Approved by the project owner after human listening review; PCM format, provenance, and runtime hash verified.'
}

$utf8NoBom = [Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText($recordPath, (($record | ConvertTo-Json -Depth 12) + [Environment]::NewLine), $utf8NoBom)

$auditPath = Join-Path $projectRoot 'specs/001-immune-war-game/evidence/asset-audit-report.json'
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

[pscustomobject]@{ AssetId = $AssetId; RuntimePath = $record.runtimePath; Sha256 = $hash; Status = $record.status }
