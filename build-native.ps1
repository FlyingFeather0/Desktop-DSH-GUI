# Build a pure C# native DSH.exe (no PowerShell scripts, no wscript/vbs).
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

$exeOut = Join-Path $base 'DDSH.exe'
$csPath = Join-Path $base 'DSHNative.cs'
$manifest = Join-Path $base 'app.manifest'
$iconPath = Join-Path $base 'app.ico'
$coreDll = Join-Path $base 'wv2\Microsoft.Web.WebView2.Core.dll'
$winDll = Join-Path $base 'wv2\Microsoft.Web.WebView2.WinForms.dll'
$loaderDll = Join-Path $base 'wv2\WebView2Loader.dll'
$photo1 = Join-Path $base 'photo\1.jpg'
$photo2 = Join-Path $base 'photo\2.png'
$photo6 = Join-Path $base 'photo\6.png'

# Generate a multi-size high-quality icon set from photo\4.jpg as PNG entries inside a proper .ico.
Add-Type -AssemblyName System.Drawing
$iconSource = Join-Path $base 'photo\icon-source.png'
if (-not (Test-Path $iconSource)) {
  $iconSource = Join-Path $base 'photo\5.jpg'
}
$src = $null
$srcRect = $null
if (Test-Path $iconSource) {
  $src = [System.Drawing.Image]::FromFile($iconSource)
  $side = [Math]::Min($src.Width, $src.Height)
  $cropX = [int](($src.Width - $side) / 2)
  $cropY = [int](($src.Height - $side) / 2)
  $srcRect = New-Object System.Drawing.Rectangle($cropX, $cropY, $side, $side)
}

$sizes = @(16, 32, 48, 64, 128, 256)
$entries = @()
$offset = 6 + 16 * $sizes.Count

foreach ($sz in $sizes) {
  $bmp = New-Object System.Drawing.Bitmap($sz, $sz)
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
  $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
  $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
  if ($src -ne $null) {
    $destRect = New-Object System.Drawing.Rectangle(0, 0, $sz, $sz)
    $g.DrawImage($src, $destRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
  } else {
    $g.Clear([System.Drawing.Color]::FromArgb(30, 36, 48))
  }
  $g.Dispose()

  # Save as PNG.
  $ms = New-Object System.IO.MemoryStream
  $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
  $data = $ms.ToArray()
  $ms.Dispose()
  $bmp.Dispose()

  
  
  
  
  
  
  

  $entries += ,[pscustomobject]@{
    Size = $sz
    Data = $data
    Offset = $offset
  }
  $offset += $data.Length
}

if ($src -ne $null) { $src.Dispose() }

# Write ICO header + multiple PNG entries.
$fs = [System.IO.File]::Create($iconPath)
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([UInt16]0)                 # reserved
$bw.Write([UInt16]1)                 # type: icon
$bw.Write([UInt16]$entries.Count)    # image count
foreach ($e in $entries) {
  $dim = if ($e.Size -eq 256) { 0 } else { $e.Size }
  $bw.Write([Byte]$dim)              # width
  $bw.Write([Byte]$dim)              # height
  $bw.Write([Byte]0)                 # color count
  $bw.Write([Byte]0)                 # reserved
  $bw.Write([UInt16]1)               # color planes
  $bw.Write([UInt16]32)              # bits per pixel
  $bw.Write([UInt32]$e.Data.Length)  # size of image data
  $bw.Write([UInt32]$e.Offset)       # offset
}
foreach ($e in $entries) {
  $bw.Write($e.Data)
}
$bw.Close()

$fw = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$wpf = Join-Path $fw 'WPF'
$refs = @(
  'System.dll',
  'System.Core.dll',
  'System.Drawing.dll',
  'System.Windows.Forms.dll',
  'System.Web.Extensions.dll',
  'System.IO.Compression.dll',
  'System.IO.Compression.FileSystem.dll',
  (Join-Path $fw 'System.Xaml.dll'),
  (Join-Path $wpf 'WindowsBase.dll'),
  (Join-Path $wpf 'PresentationCore.dll'),
  (Join-Path $wpf 'PresentationFramework.dll'),
  (Join-Path $wpf 'WindowsFormsIntegration.dll'),
  $coreDll,
  $winDll
)

$refArgs = @()
foreach ($r in $refs) {
  $refArgs += "/reference:$r"
}

& $csc /nologo /target:winexe "/out:$exeOut" "/win32manifest:$manifest" "/win32icon:$iconPath" `
  "/resource:$coreDll,DSH.Microsoft.Web.WebView2.Core.dll" `
  "/resource:$winDll,DSH.Microsoft.Web.WebView2.WinForms.dll" `
  "/resource:$loaderDll,DSH.WebView2Loader.dll" `
    "/resource:$photo1,DSH.photo1.jpg" `
    "/resource:$photo2,DSH.photo2.png" `
    "/resource:$photo6,DSH.photo6.png" `
  @refArgs `
  $csPath

if ($LASTEXITCODE -ne 0) {
  throw 'C# compilation failed.'
}

# Optional: sign with a self-signed certificate to reduce heuristic flags.
$signtool = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin\*\x64\signtool.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
if ($signtool) {
  try {
    $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject 'CN=DSH Desktop' -CertStoreLocation Cert:\CurrentUser\My -KeyExportPolicy Exportable -ErrorAction Stop
    & $signtool.FullName sign /a /fd SHA256 /s My /n 'DSH Desktop' $exeOut | Out-Null
    Write-Host 'Signed with self-signed certificate.'
  } catch {
    Write-Warning 'Self-signing skipped (optional).'
  }
}

Write-Host 'Pure C# DSH.exe created:' $exeOut
