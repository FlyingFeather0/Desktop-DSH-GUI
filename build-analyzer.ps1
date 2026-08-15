# Build the WebUI analyzer tool.
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

$out = Join-Path $base 'WebUiAnalyzer.exe'
$src = Join-Path $base 'WebUiAnalyzer.cs'
$coreDll = Join-Path $base 'wv2\Microsoft.Web.WebView2.Core.dll'
$winDll = Join-Path $base 'wv2\Microsoft.Web.WebView2.WinForms.dll'
$loaderDll = Join-Path $base 'wv2\WebView2Loader.dll'

$refs = @(
  'System.dll',
  'System.Core.dll',
  'System.Drawing.dll',
  'System.Windows.Forms.dll',
  $coreDll,
  $winDll
)
$refArgs = @()
foreach ($r in $refs) {
  $refArgs += "/reference:$r"
}

& $csc /nologo /target:winexe "/out:$out" @refArgs $src
if ($LASTEXITCODE -ne 0) {
  throw 'Analyzer compilation failed.'
}
Write-Host 'WebUI Analyzer created:' $out
