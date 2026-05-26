#!/usr/bin/env pwsh

$ErrorActionPreference = "Stop"

$PodmanVersion = "5.8.2"
$ToolsDir = "$env:USERPROFILE\Tools\Podman"
$DownloadDir = "$env:USERPROFILE\Downloads"

Write-Host "Installing Podman $PodmanVersion (User-Level)"
Write-Host "=================================================="
Write-Host ""

Write-Host "Creating tools directory..."
New-Item -ItemType Directory -Path $ToolsDir -Force | Out-Null
Write-Host "Directory created: $ToolsDir"

Write-Host ""
Write-Host "Downloading Podman $PodmanVersion..."

$urls = @(
    "https://github.com/containers/podman/releases/download/v$PodmanVersion/podman-$PodmanVersion.zip",
    "https://github.com/containers/podman/releases/download/v$PodmanVersion/podman-remote-release-windows_amd64.zip"
)

$downloaded = $false
$zipFile = ""

foreach ($url in $urls) {
    try {
        $zipFile = Join-Path $DownloadDir "podman-$PodmanVersion.zip"
        Write-Host "Trying: $url"
        Invoke-WebRequest -Uri $url -OutFile $zipFile -ErrorAction Stop
        Write-Host "Downloaded successfully"
        $downloaded = $true
        break
    }
    catch {
        Write-Host "Failed: $_"
        if (Test-Path $zipFile) {
            Remove-Item $zipFile -Force
        }
    }
}

if (-not $downloaded) {
    Write-Host "Failed to download Podman from all sources"
    Write-Host "Please download manually from: https://github.com/containers/podman/releases/v$PodmanVersion"
    exit 1
}

Write-Host ""
Write-Host "Extracting Podman..."

try {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    if (Test-Path $ToolsDir) {
        Remove-Item $ToolsDir -Recurse -Force -ErrorAction SilentlyContinue
    }
    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipFile, $ToolsDir)
    Write-Host "Extracted to: $ToolsDir"
}
catch {
    Write-Host "Extraction failed: $_"
    exit 1
}

Write-Host ""
Write-Host "Verifying installation..."

$podmanExe = Get-ChildItem -Path $ToolsDir -Filter "podman.exe" -Recurse | Select-Object -First 1

if ($null -eq $podmanExe) {
    Write-Host "podman.exe not found in extracted files"
    Write-Host "Contents of directory:"
    Get-ChildItem -Path $ToolsDir -Recurse | Select-Object -First 20
    exit 1
}

Write-Host "Found podman.exe at: $($podmanExe.FullName)"
$PodmanPath = Split-Path $podmanExe.FullName

Write-Host ""
Write-Host "Adding to user PATH..."

$currentPath = [Environment]::GetEnvironmentVariable("Path", "User")

if ($currentPath -like "*$PodmanPath*") {
    Write-Host "Already in PATH"
}
else {
    [Environment]::SetEnvironmentVariable("Path", "$currentPath;$PodmanPath", "User")
    Write-Host "Added to user PATH"
}

Write-Host ""
Write-Host "Verifying podman command..."

$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")

try {
    $version = & $podmanExe.FullName --version
    Write-Host "Podman is working: $version"
}
catch {
    Write-Host "Could not execute podman, but it's installed"
    Write-Host "Try restarting PowerShell or your terminal"
}

Write-Host ""
Write-Host "Installation Complete!"
Write-Host "=================================================="
Write-Host ""
Write-Host "Next Steps:"
Write-Host "1. Restart PowerShell or your terminal"
Write-Host "2. Verify installation:"
Write-Host "   podman --version"
Write-Host "   podman-compose --version"
Write-Host "3. Start Podman machine:"
Write-Host "   podman machine start"
Write-Host "4. Run your containers:"
Write-Host "   podman-compose up --build"
Write-Host ""

Remove-Item $zipFile -Force -ErrorAction SilentlyContinue
Write-Host "Cleanup complete"
