param([switch]$LayoutOnly,[ValidateSet('User','Admin')][string]$Edition='User')
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$dist=Join-Path $repo 'dist'
$isAdmin=$Edition -eq 'Admin'
$exeName=if($isAdmin){'DTCE-Tool-Kit-Admin.exe'}else{'DTCE-Tool-Kit.exe'}
$identityName=if($isAdmin){'DTCE.DTCEToolKit-Admin'}else{'DTCE.DTCEToolKit'}
$manifestName=if($isAdmin){'AppxManifest.Admin.xml'}else{'AppxManifest.xml'}
$packageStem=if($isAdmin){'DTCE-Tool-Kit-Admin'}else{'DTCE-Tool-Kit'}
$user=Join-Path $dist $Edition
if(!(Test-Path -LiteralPath (Join-Path $user $exeName))){& (Join-Path $PSScriptRoot ("Build-"+$Edition+".ps1"))}
$stage=Join-Path $dist ("MSIX-Layout-"+$Edition)
New-Item -ItemType Directory -Force $stage,(Join-Path $stage 'Assets') | Out-Null
Copy-Item (Join-Path $user $exeName),(Join-Path $user "$exeName.config") $stage -Force
# the PDF export needs tools/qpdf beside the exe (owner, 24 November 2026: "PDF security components are missing" in the Store app)
$qpdf=Join-Path $user 'tools\qpdf'
if(!(Test-Path -LiteralPath (Join-Path $qpdf 'qpdf.exe'))){throw 'tools/qpdf is missing from the edition build'}
New-Item -ItemType Directory -Force (Join-Path $stage 'tools') | Out-Null
Copy-Item -LiteralPath $qpdf -Destination (Join-Path $stage 'tools') -Recurse -Force
[xml]$manifest=Get-Content -LiteralPath (Join-Path (Join-Path $repo 'packaging') $manifestName) -Raw
$version=[Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $user $exeName)).FileVersion
$parts=$version.Split('.')
$manifest.Package.Identity.Version="$($parts[0]).$($parts[1]).$($parts[2]).0"
if($manifest.Package.Identity.Name-ne $identityName-or $manifest.Package.Identity.Publisher-ne 'CN=DBEDF414-F57B-457B-A9EC-C38F24535F31'){throw 'Store identity mismatch'}
$manifest.Save((Join-Path $stage 'AppxManifest.xml'))
Add-Type -AssemblyName System.Drawing
$logo=[Drawing.Image]::FromFile((Join-Path $repo 'src\DtceLogo.png'))
try{foreach($item in @(@('StoreLogo.png',50),@('Square44x44Logo.png',44),@('Square150x150Logo.png',150))){
 $size=[int]$item[1];$bmp=New-Object Drawing.Bitmap($size,$size)
 try{$g=[Drawing.Graphics]::FromImage($bmp);try{$g.Clear([Drawing.Color]::Transparent);$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.CompositingQuality=[Drawing.Drawing2D.CompositingQuality]::HighQuality;$g.DrawImage($logo,0,0,$size,$size)}finally{$g.Dispose()};$bmp.Save((Join-Path $stage ('Assets\'+$item[0])),[Drawing.Imaging.ImageFormat]::Png)}finally{$bmp.Dispose()}
}}finally{$logo.Dispose()}
$allowed=@($exeName,"$exeName.config",'AppxManifest.xml','Assets\StoreLogo.png','Assets\Square44x44Logo.png','Assets\Square150x150Logo.png')
$actual=Get-ChildItem $stage -Recurse -File | ForEach-Object {$_.FullName.Substring($stage.Length+1)}
if(@($actual | Where-Object {$_ -notin $allowed -and $_ -notlike 'tools\qpdf\*'}).Count){throw 'Unexpected content in MSIX layout'}
foreach($name in @('qpdf.exe','qpdf30.dll','libgcc_s_seh-1.dll','libstdc++-6.dll','libwinpthread-1.dll')){if(!(Test-Path -LiteralPath (Join-Path $stage ('tools\qpdf\'+$name)))){throw "MSIX layout is missing tools\qpdf\$name"}}
if($LayoutOnly){Write-Output "Validated $Edition MSIX layout: $stage";return}
$sdk=Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
$makeappx=Get-ChildItem $sdk -Recurse -Filter makeappx.exe | Where-Object {$_.FullName -match '\\x64\\makeappx.exe$'} | Sort-Object FullName -Descending | Select-Object -First 1
if(!$makeappx){throw 'Windows SDK MakeAppx.exe not installed. Use Windows GitHub runner or install Windows SDK.'}
$package=Join-Path $dist ($packageStem+'_'+$manifest.Package.Identity.Version+'_x64.msix')
& $makeappx.FullName pack /d $stage /p $package /o
if($LASTEXITCODE-ne 0){throw 'MSIX schema/package validation failed'}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=[IO.Compression.ZipFile]::OpenRead($package)
try{if(@($zip.Entries | Where-Object FullName -match '(?i)(AdminPrivateKey|\.dat$|\.pfx$|\.cs$)').Count){throw 'Private/Admin content in MSIX'}}finally{$zip.Dispose()}
Write-Output "Store upload package built (unsigned, for Store certification): $package"
