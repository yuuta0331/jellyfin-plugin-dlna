param (
    [Parameter(Position = 0, Mandatory = $true)]
    [string]$Action
)

# Set execution context to the workspace root
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$settingsPath = Join-Path $root ".vscode\settings.json"
$buildYamlPath = Join-Path $root "build.yaml"

if (-not (Test-Path $settingsPath)) {
    Write-Error "settings.json not found at $settingsPath"
    exit 1
}

if (-not (Test-Path $buildYamlPath)) {
    Write-Error "build.yaml not found at $buildYamlPath"
    exit 1
}

$settings = Get-Content $settingsPath | ConvertFrom-Json

# Helper to read configurations
function Get-ConfigSetting([string]$name) {
    if ($settings.PSObject.Properties[$name]) {
        return $settings.$name
    }
    return $null
}

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

function Get-PluginVersionInfo() {
    $versionNumber = Get-BuildYamlScalar "version"
    if ([string]::IsNullOrWhiteSpace($versionNumber)) {
        Write-Error "Could not read version from build.yaml"
        exit 1
    }

    $fullVersion = "${versionNumber}.0.0.0"
    $folderName = "DLNA_${fullVersion}"

    return [PSCustomObject]@{
        Number = $versionNumber
        Full = $fullVersion
        FolderName = $folderName
    }
}

$jellyfinExe = Get-ConfigSetting "jellyfin.exe"
$workingDir = Get-ConfigSetting "jellyfin.workingDir"
$dataDir = Get-ConfigSetting "jellyfin.dataDir"
$cacheDir = Get-ConfigSetting "jellyfin.cacheDir"
$pluginVersion = Get-PluginVersionInfo

# Plugin output source directories
$debugBinDir = Join-Path $root "src\Jellyfin.Plugin.Dlna\bin\Debug\net9.0"
$releaseBinDir = Join-Path $root "src\Jellyfin.Plugin.Dlna\bin\Release\net9.0"

# Target plugins folder
$pluginTargetDir = $null
if ($dataDir) {
    $pluginTargetDir = Join-Path $dataDir "plugins\$($pluginVersion.FolderName)"
}

# Functions implementing actions

function Build-Debug {
    Write-Host "Building Jellyfin DLNA Plugin (Debug)..." -ForegroundColor Green
    dotnet build (Join-Path $root "Jellyfin.Plugin.Dlna.sln") -c Debug
}

function Build-Release {
    Write-Host "Building Jellyfin DLNA Plugin (Release)..." -ForegroundColor Green
    dotnet build (Join-Path $root "Jellyfin.Plugin.Dlna.sln") -c Release
}

function New-MetaJson([string]$targetDir) {
    $artifacts = Get-BuildYamlArtifacts
    if ($artifacts.Count -eq 0) {
        Write-Error "No artifacts found in build.yaml"
        exit 1
    }

    $meta = [ordered]@{
        category = Get-BuildYamlScalar "category"
        changelog = (Get-BuildYamlScalar "changelog") ?? ""
        description = Get-BuildYamlDescription
        guid = Get-BuildYamlScalar "guid"
        name = Get-BuildYamlScalar "name"
        overview = Get-BuildYamlScalar "overview"
        owner = Get-BuildYamlScalar "owner"
        targetAbi = Get-BuildYamlScalar "targetAbi"
        timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        version = $pluginVersion.Full
        status = "Active"
        autoUpdate = $true
        imagePath = Get-BuildYamlScalar "imageUrl"
        assemblies = $artifacts
    }

    $metaPath = Join-Path $targetDir "meta.json"
    $meta | ConvertTo-Json -Depth 4 | Set-Content -Path $metaPath -Encoding UTF8
    Write-Host "Generated meta.json ($($pluginVersion.Full)) at $metaPath" -ForegroundColor Green
}

function Deploy-Files([string]$sourceDir) {
    if (-not $pluginTargetDir) {
        Write-Error "jellyfin.dataDir is not configured in settings.json."
        exit 1
    }
    if (-not (Test-Path $sourceDir)) {
        Write-Error "Source directory not found: $sourceDir. Did you run the build task first?"
        exit 1
    }

    Write-Host "Deploying plugin to: $pluginTargetDir" -ForegroundColor Green
    if (-not (Test-Path $pluginTargetDir)) {
        New-Item -ItemType Directory -Path $pluginTargetDir -Force | Out-Null
    }

    $artifacts = Get-BuildYamlArtifacts
    foreach ($artifact in $artifacts) {
        $sourceFile = Join-Path $sourceDir $artifact
        if (-not (Test-Path $sourceFile)) {
            Write-Error "Required artifact not found: $sourceFile"
            exit 1
        }

        Copy-Item -Path $sourceFile -Destination $pluginTargetDir -Force
        $pdbFile = [System.IO.Path]::ChangeExtension($sourceFile, ".pdb")
        if (Test-Path $pdbFile) {
            Copy-Item -Path $pdbFile -Destination $pluginTargetDir -Force
        }
    }

    New-MetaJson -targetDir $pluginTargetDir
    Write-Host "Deployment completed successfully." -ForegroundColor Green
}

function Stop-JellyfinProcess {
    if (-not $jellyfinExe) {
        Write-Warning "jellyfin.exe path is not configured. Skipping stopping process."
        return
    }

    $exeName = [System.IO.Path]::GetFileNameWithoutExtension($jellyfinExe)
    $processes = Get-Process -Name $exeName -ErrorAction SilentlyContinue

    if ($processes) {
        Write-Host "Stopping running process: $exeName" -ForegroundColor Yellow
        $processes | Stop-Process -Force
        Start-Sleep -Seconds 1 # Wait for process to fully exit
    } else {
        Write-Host "Jellyfin is not running." -ForegroundColor Gray
    }
}

function Run-JellyfinProcess {
    if (-not $jellyfinExe -or -not (Test-Path $jellyfinExe)) {
        Write-Error "jellyfin.exe is not configured correctly or does not exist at: $jellyfinExe"
        exit 1
    }

    Write-Host "Starting Jellyfin process..." -ForegroundColor Green
    $argsList = @()
    if ($dataDir) {
        $argsList += "--datadir"
        $argsList += $dataDir
    }
    if ($cacheDir) {
        $argsList += "--cachedir"
        $argsList += $cacheDir
    }

    Start-Process -FilePath $jellyfinExe -ArgumentList $argsList -WorkingDirectory $workingDir -NoNewWindow
}

function Open-JellyfinLogFile {
    if (-not $dataDir) {
        Write-Error "jellyfin.dataDir is not configured in settings.json."
        exit 1
    }

    $logDir = Join-Path $dataDir "log"
    if (-not (Test-Path $logDir)) {
        Write-Error "Log directory not found at $logDir"
        exit 1
    }

    # Find the newest log file
    $latestLog = Get-ChildItem -Path $logDir -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $latestLog) {
        Write-Warning "No log files found in $logDir."
        return
    }

    Write-Host "Monitoring log file: $($latestLog.FullName)" -ForegroundColor Green
    Write-Host "Press Ctrl+C to stop monitoring." -ForegroundColor Yellow
    Get-Content -Path $latestLog.FullName -Wait -Tail 50
}

function Package-Release {
    $packageScript = Join-Path $PSScriptRoot "package-release.ps1"
    if (-not (Test-Path $packageScript)) {
        Write-Error "package-release.ps1 not found at $packageScript"
        exit 1
    }

    & $packageScript -Configuration Release
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

function Open-ReleaseFolder {
    $releaseRoot = Join-Path $root "dist"
    if (-not (Test-Path $releaseRoot)) {
        Write-Error "Release folder not found: $releaseRoot. Run the release task first."
        exit 1
    }

    Write-Host "Opening $releaseRoot" -ForegroundColor Green
    Start-Process explorer.exe $releaseRoot
}

# Route actions
switch ($Action) {
    "build-jellyfin-debug" {
        Build-Debug
    }
    "publish-jellyfin-release" {
        Build-Release
    }
    "publish-jellyfin-compat-matrix" {
        Build-Release
    }
    "package-jellyfin-release" {
        Package-Release
    }
    "release-jellyfin-plugin" {
        Build-Release
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        Package-Release
    }
    "release-and-deploy-jellyfin" {
        Build-Release
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        Package-Release
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        Deploy-Files -sourceDir $releaseBinDir
    }
    "deploy-jellyfin-debug" {
        Deploy-Files -sourceDir $debugBinDir
    }
    "deploy-jellyfin-release" {
        Deploy-Files -sourceDir $releaseBinDir
    }
    "stop-jellyfin" {
        Stop-JellyfinProcess
    }
    "run-jellyfin" {
        Run-JellyfinProcess
    }
    "open-jellyfin-log" {
        Open-JellyfinLogFile
    }
    "open-release-folder" {
        Open-ReleaseFolder
    }
    default {
        Write-Error "Unknown action: $Action"
        exit 1
    }
}
