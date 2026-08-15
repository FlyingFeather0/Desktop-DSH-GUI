# Build the download-confirm dialog preview.
$ErrorActionPreference = 'Stop'
$base = $PSScriptRoot
if ([string]::IsNullOrEmpty($base)) {
  try { $base = Split-Path -Parent ([System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName) } catch {}
}

$cscCandidates = @(
  (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),
  (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe')
)
$csc = $cscCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $csc) {
  throw 'C# compiler csc.exe not found.'
}

$out = Join-Path $base 'ConfirmPreview.exe'
$src = Join-Path $base 'ConfirmPreview.cs'
$fw = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$wpf = Join-Path $fw 'WPF'

$refs = @(
  'System.dll',
  'System.Core.dll',
  (Join-Path $fw 'System.Xaml.dll'),
  (Join-Path $wpf 'WindowsBase.dll'),
  (Join-Path $wpf 'PresentationCore.dll'),
  (Join-Path $wpf 'PresentationFramework.dll')
)
$refArgs = @()
foreach ($r in $refs) {
  $refArgs += "/reference:$r"
}

& $csc /nologo /target:winexe "/out:$out" @refArgs $src
if ($LASTEXITCODE -ne 0) {
  throw 'Confirm preview compilation failed.'
}
Write-Host 'Confirm Preview created:' $out
