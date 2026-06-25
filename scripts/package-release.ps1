param(
    [string]$Configuration = "Release"
)

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$buildYamlPath = Join-Path $root "build.yaml"
$binDir = Join-Path $root "src\Jellyfin.Plugin.Dlna\bin\$Configuration\net9.0"

function Get-BuildYamlScalar([string]$key) {
    $line = Select-String -Path $buildYamlPath -Pattern "^${key}:\s*(.+)$" | Select-Object -First 1
    if (-not $line) {
        return $null
    }

    $value = $line.Matches[0].Groups[1].Value.Trim()
    if ($value.StartsWith('"') -and $value.EndsWith('"')) {
        $value = $value.Substring(1, $value.Length - 2)
    }

    return $value
}

function Get-BuildYamlDescription() {
    $lines = Get-Content $buildYamlPath
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^description:\s*>\s*$') {
            $descLines = @()
            for ($j = $i + 1; $j -lt $lines.Count; $j++) {
                if ($lines[$j] -match '^\S') {
                    break
                }

                if ($lines[$j] -match '^\s+(.+)$') {
                    $descLines += $Matches[1].Trim()
                }
            }

            if ($descLines.Count -gt 0) {
                return ($descLines -join ' ')
            }
        }
    }

    return Get-BuildYamlScalar "overview"
}

function Get-BuildYamlArtifacts() {
    $lines = Get-Content $buildYamlPath
    $artifacts = @()
    $inArtifacts = $false

    foreach ($line in $lines) {
        if ($line -match '^artifacts:\s*$') {
            $inArtifacts = $true
            continue
        }

        if ($inArtifacts) {
            if ($line -match '^\s*-\s*"(.+)"\s*$') {
                $artifacts += $Matches[1]
                continue
            }

            if ($line -match '^\S') {
                break
            }
        }
    }

    return $artifacts
}

if (-not (Test-Path $binDir)) {
    Write-Error "Build output not found: $binDir. Run dotnet build -c $Configuration first."
    exit 1
}

$versionNumber = Get-BuildYamlScalar "version"
$fullVersion = "${versionNumber}.0.0.0"
$folderName = "DLNA_${fullVersion}"
$releaseDir = Join-Path $root "dist\$folderName"
$zipPath = Join-Path $root "dist\jellyfin-plugin-dlna_${fullVersion}.zip"
$artifacts = Get-BuildYamlArtifacts

if ($artifacts.Count -eq 0) {
    Write-Error "No artifacts found in build.yaml"
    exit 1
}

Write-Host "Packaging release $fullVersion..." -ForegroundColor Green
New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null

foreach ($artifact in $artifacts) {
    $sourceFile = Join-Path $binDir $artifact
    if (-not (Test-Path $sourceFile)) {
        Write-Error "Required artifact not found: $sourceFile"
        exit 1
    }

    Copy-Item -Path $sourceFile -Destination $releaseDir -Force
}

$changelogLines = @()
$lines = Get-Content $buildYamlPath
$inChangelog = $false
foreach ($line in $lines) {
    if ($line -match '^changelog:\s*\|-\s*$') {
        $inChangelog = $true
        continue
    }

    if ($inChangelog) {
        if ($line -match '^\s*-\s+(.+)$') {
            $changelogLines += $Matches[1]
            continue
        }

        if ($line -match '^\S') {
            break
        }
    }
}

$meta = [ordered]@{
    category = Get-BuildYamlScalar "category"
    changelog = ($changelogLines -join "`n")
    description = Get-BuildYamlDescription
    guid = Get-BuildYamlScalar "guid"
    name = Get-BuildYamlScalar "name"
    overview = Get-BuildYamlScalar "overview"
    owner = Get-BuildYamlScalar "owner"
    targetAbi = Get-BuildYamlScalar "targetAbi"
    timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    version = $fullVersion
    status = "Active"
    autoUpdate = $true
    imagePath = Get-BuildYamlScalar "imageUrl"
    assemblies = $artifacts
}

$meta | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $releaseDir "meta.json") -Encoding UTF8

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path (Join-Path $releaseDir '*') -DestinationPath $zipPath -Force

Write-Host "Release folder: $releaseDir" -ForegroundColor Green
Write-Host "Release zip:    $zipPath" -ForegroundColor Green
Get-ChildItem $releaseDir | Format-Table Name, Length -AutoSize
