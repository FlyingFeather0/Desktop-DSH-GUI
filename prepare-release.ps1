# Prepare a clean GitHub release folder.
$ErrorActionPreference = 'Stop'
$base = $PSScriptRoot
if ([string]::IsNullOrEmpty($base)) {
  try { $base = Split-Path -Parent ([System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName) } catch {}
}
$root = Split-Path -Parent $base
$release = $base

# Clean old files except this script
Get-ChildItem $release -Exclude 'prepare-release.ps1' -Force | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

$files = @(
  'README.md',
  'DSHNative.cs',
  'WpfGlassPreview.cs',
  'WpfMainPreview.cs',
  'WebUiAnalyzer.cs',
  'build-native.ps1',
  'make-package.ps1',
  'build-confirm-preview.ps1',
  'build-wpf-main-preview.ps1',
  'build-wpf-preview.ps1',
  'build-analyzer.ps1',
  'app.manifest'
)

foreach ($f in $files) {
  $src = Join-Path $root $f
  if (Test-Path $src) {
    Copy-Item $src (Join-Path $release $f) -Force
  }
}

# Photo resources (optional; only if you are allowed to redistribute them)
$photoSrc = Join-Path $root 'photo'
$photoDst = Join-Path $release 'photo'
if (Test-Path $photoSrc) {
  New-Item -ItemType Directory -Path $photoDst -Force | Out-Null
  Copy-Item (Join-Path $photoSrc '*') $photoDst -Force
}

# Built exe
$exeSrc = Join-Path $root 'release\DDSH.exe'
if (Test-Path $exeSrc) {
  Copy-Item $exeSrc (Join-Path $release 'DDSH.exe') -Force
}

# .gitignore
$gitignore = @"
node_modules/
release/
*.log
dsh-config.json
wv2-userdata/
DSH.exe.WebView2/
github-release/
"@
Set-Content -Path (Join-Path $release '.gitignore') -Value $gitignore -Encoding ASCII

Write-Host 'Release folder prepared:'
Get-ChildItem $release -Recurse | Select-Object FullName | Format-Table -AutoSize
Write-Host ''
Write-Host 'Upload these files to GitHub:'
Write-Host '  README.md'
Write-Host '  DSHNative.cs'
Write-Host '  WpfGlassPreview.cs'
Write-Host '  WpfMainPreview.cs'
Write-Host '  WebUiAnalyzer.cs'
Write-Host '  build-native.ps1'
Write-Host '  make-package.ps1'
Write-Host '  build-confirm-preview.ps1'
Write-Host '  build-wpf-main-preview.ps1'
Write-Host '  build-wpf-preview.ps1'
Write-Host '  build-analyzer.ps1'
Write-Host '  app.manifest'
Write-Host '  photo/  (if allowed)'
Write-Host '  DDSH.exe'
