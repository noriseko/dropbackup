# DropBackup Release Build Script
# This script builds and packages DropBackup for distribution

param(
	[Parameter(Mandatory=$true)]
	[string]$Version
)

$ErrorActionPreference = "Stop"

# Paths
$RootDir = $PSScriptRoot
$ProjectPath = Join-Path $RootDir "DropBackup\DropBackup.csproj"
$OutputDir = Join-Path $RootDir "Release\DropBackup-$Version-win-x64"
$ZipPath = Join-Path $RootDir "Release\DropBackup-$Version-win-x64.zip"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  DropBackup Release Builder" -ForegroundColor Cyan
Write-Host "  Version: $Version" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Validate project file exists
if (-not (Test-Path $ProjectPath)) {
	Write-Host "Error: Project file not found at $ProjectPath" -ForegroundColor Red
	exit 1
}

# Cleanup release directory
Write-Host "Cleaning up release directory..." -ForegroundColor Yellow
if (Test-Path $OutputDir) {
	Remove-Item $OutputDir -Recurse -Force
}
if (Test-Path $ZipPath) {
	Remove-Item $ZipPath -Force
}

# Build project
Write-Host "Building project..." -ForegroundColor Yellow
dotnet publish $ProjectPath `
	-c Release `
	-r win-x64 `
	--self-contained false `
	-p:PublishSingleFile=false `
	-p:PublishReadyToRun=true `
	-o $OutputDir

if ($LASTEXITCODE -ne 0) {
	Write-Host "Build failed!" -ForegroundColor Red
	exit 1
}

Write-Host "Build completed successfully!" -ForegroundColor Green

# Create README.txt
Write-Host "Creating README.txt..." -ForegroundColor Yellow
$readmeText = @"
DropBackup - Timestamped File Backup Tool
Version: $Version
https://github.com/noriseko/dropbackup

=======================================
INSTALLATION
=======================================

1. Install .NET 10.0 Desktop Runtime if not already installed:
   https://dotnet.microsoft.com/download/dotnet/10.0

2. Extract all files from this ZIP to a folder of your choice.
   Example: C:\Program Files\DropBackup

3. (Optional) Create a shortcut to DropBackup.exe on your desktop.
   This shortcut becomes your drop target for quick backups.


=======================================
USAGE
=======================================

Method 1: Run DropBackup.exe without arguments to configure settings
Method 2: Drag and drop files/folders onto DropBackup.exe (or its shortcut)
Method 3: Command line - DropBackup.exe "C:\path\to\file.txt"

The application will create a timestamped backup copy according
to your configured settings.


=======================================
FEATURES
=======================================

- Timestamped backups with customizable format
- File and folder backup support
- Flexible naming options (prefix/suffix)
- Smart conflict handling (rename/overwrite/delete)
- Exclude symbolic links option
- Wildcard-based exclude filters
- Custom destination folder support
- Optional logging with retention policy
- Toast notifications
- Automatic language switching (English/Japanese)


=======================================
CONFIGURATION
=======================================

Settings are stored in:
%APPDATA%\DropBackup\Settings.ini

You can edit this file manually or use the settings window
(run DropBackup.exe without arguments).


=======================================
UNINSTALLATION
=======================================

1. Delete the folder containing DropBackup files
2. (Optional) Delete settings: %APPDATA%\DropBackup


=======================================
LICENSE
=======================================

This software is released under the MIT License.
See LICENSE file for details.

Copyright (c) 2025 Noriyuki Seko


=======================================
SUPPORT
=======================================

For issues and questions:
https://github.com/noriseko/dropbackup/issues
"@

$readmeText | Out-File -FilePath "$OutputDir\README.txt" -Encoding utf8

# Copy LICENSE file
Write-Host "Copying LICENSE..." -ForegroundColor Yellow
if (Test-Path "LICENSE") {
	Copy-Item -Path "LICENSE" -Destination $OutputDir -Force
}

# Create ZIP archive
Write-Host "Creating ZIP archive..." -ForegroundColor Yellow
Compress-Archive -Path "$OutputDir\*" -DestinationPath $ZipPath -Force

$zipSize = [math]::Round((Get-Item $ZipPath).Length / 1MB, 2)

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Release package created successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Output file: $ZipPath" -ForegroundColor Cyan
Write-Host "File size:   $zipSize MB" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Test the ZIP package" -ForegroundColor White
Write-Host "2. Create a new release on GitHub" -ForegroundColor White
Write-Host "3. Upload $ZipPath to GitHub Releases" -ForegroundColor White
Write-Host "4. Update the release notes with changelog" -ForegroundColor White
Write-Host ""
