# Build a pure C# DSH.exe and prepare a release folder with DSH.exe + photo.
$ErrorActionPreference = 'Stop'
$base = $PSScriptRoot
if ([string]::IsNullOrEmpty($base)) {
  try { $base = Split-Path -Parent ([System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName) } catch {}
}

Write-Host '[1/2] Building self-contained DSH.exe ...'
& (Join-Path $base 'build-native.ps1')

$release = Join-Path $base 'release'
if (Test-Path $release) {
  Remove-Item -Path $release -Recurse -Force
}
New-Item -ItemType Directory -Path $release | Out-Null
New-Item -ItemType Directory -Path (Join-Path $release 'photo') | Out-Null

Write-Host '[2/2] Copying DSH.exe and photo folder ...'
Copy-Item (Join-Path $base 'DSH.exe') $release -Force

# If you have images ready, put them into this photo folder later.
# Copy-Item (Join-Path $base 'photo\*') (Join-Path $release 'photo') -Recurse -Force

Write-Host 'Done.'
Write-Host 'Release folder:' $release
Write-Host 'Contents: DSH.exe + photo\'
