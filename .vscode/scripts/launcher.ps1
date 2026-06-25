# Jellyfin & Emby launcher: Build (Release) -> Copy results -> Start executables
$root = $PSScriptRoot
Write-Host "Launcher: Jellyfin & Emby (Build + Copy + Start)" -ForegroundColor Green

# Detect repos
$JellyfinDir = Join-Path $root "Jellyfin"
$EmbyDir = Join-Path $root "Emby"

function Find-Solution([string]$repoDir) {
  if (-Not (Test-Path $repoDir)) { return $null }
  $sln = Get-ChildItem -Path $repoDir -Filter *.sln -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
  return $sln
}

$jellyfinSln = Find-Solution $JellyfinDir
$embySln = Find-Solution $EmbyDir

function Build-Solution([string]$slnPath) {
  if (-Not (Test-Path $slnPath)) { return $false }
  $workDir = Split-Path -Path $slnPath -Parent
  Push-Location $workDir
  try {
    dotnet restore "$slnPath" -v minimal
    dotnet build "$slnPath" -c Release -v minimal
  } catch {
    Write-Error "Build failed for $slnPath: $_"
    return $false
  } finally {
    Pop-Location
  }
  return $true
}

# Build Jellyfin and Emby if they exist
$buildResults = @()
if ($jellyfinSln) { $buildResults += (Build-Solution -slnPath $jellyfinSln.FullName) }
if ($embySln) { $buildResults += (Build-Solution -slnPath $embySln.FullName) }

if (-not ($buildResults -contains $true)) {
    Write-Warning "No solutions built. Exiting launcher."
}

# Prepare dist folders
$distRoot = Join-Path $root "dist"
foreach ($dir in @("Jellyfin","Emby")) {
  $targetDir = Join-Path $distRoot $dir
  if (-Not (Test-Path $targetDir)) { New-Item -ItemType Directory -Path $targetDir | Out-Null }
}

# Copy outputs from binaries to dist
function Copy-Outputs([string]$slnPath,[string]$destFolderName) {
  if (-Not (Test-Path $slnPath)) { return }
  $binRoot = Join-Path (Split-Path -Path $slnPath -Parent) "bin"
  if (Test-Path $binRoot) {
    $targetDir = Join-Path (Join-Path $root "dist") $destFolderName
    Get-ChildItem -Path $binRoot -Recurse -Filter *.exe -ErrorAction SilentlyContinue | Copy-Item -Destination $targetDir -Recurse -Force
  }
}
if ($jellyfinSln) { Copy-Outputs -slnPath $jellyfinSln.FullName -destFolderName "Jellyfin" }
if ($embySln) { Copy-Outputs -slnPath $embySln.FullName -destFolderName "Emby" }

# Start executables found in dist
function Start-Executables([string]$folder) {
  $exeFiles = Get-ChildItem -Path $folder -Filter *.exe -Recurse -ErrorAction SilentlyContinue
  foreach ($exe in $exeFiles) {
    try { Start-Process -FilePath $exe.FullName -WorkingDirectory $exe.DirectoryName -NoNewWindow } catch { Write-Error "Failed to start $($exe.FullName): $_" }
  }
}
Start-Executables -folder (Join-Path $distRoot "Jellyfin")
Start-Executables -folder (Join-Path $distRoot "Emby")

Write-Host "Launcher finished." -ForegroundColor Green
