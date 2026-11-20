param([string]$OutputDirectory)
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$source=Join-Path $repo 'src'
if(-not $OutputDirectory){$OutputDirectory=Join-Path $repo 'dist'}
if(!(Test-Path -LiteralPath (Join-Path $source 'AccessPublicKey.xml'))){throw 'Existing public key required. CI must never create/rotate signing keys.'}
if(Get-ChildItem -LiteralPath $repo -Recurse -Filter AdminPrivateKey.dat){throw 'Private key must not be present in CI/repository.'}
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$stage=Join-Path $OutputDirectory 'User'
New-Item -ItemType Directory -Force $stage | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$refs=@('/nologo','/target:winexe','/optimize+','/reference:System.Windows.Forms.dll','/reference:System.Drawing.dll','/reference:System.Core.dll','/reference:Microsoft.CSharp.dll','/reference:System.Security.dll')
$resources=@("/resource:$source\SlabTemplate.dxf,SlabTemplate.dxf","/resource:$source\StandardTemplate.dxf,StandardTemplate.dxf","/resource:$source\AccessPublicKey.xml,AccessPublicKey.xml","/resource:$source\DpwhLogo.png,DpwhLogo.png","/resource:$source\DtceLogo.png,DtceLogo.png","/resource:$source\app.ico,DtceIcon.ico","/resource:$source\BeamSummaryTemplate.xlsx,BeamSummaryTemplate.xlsx" "/resource:$source\PowTemplate.xlsx,PowTemplate.xlsx")
$files=Get-ChildItem $source -Filter '*.cs' | ForEach-Object FullName
$exe=Join-Path $stage 'DTCE-Tool-Kit.exe'
& $compiler /reference:System.IO.Compression.dll "/resource:$source\WindSurfaces.txt,WindSurfaces.txt" "/resource:$source\WindMap27.jpg,WindMap27.jpg" "/resource:$source\WindMap28.jpg,WindMap28.jpg" "/resource:$source\WindMap29.jpg,WindMap29.jpg" @refs @resources "/win32manifest:$source\app.manifest" "/win32icon:$source\app.ico" "/out:$exe" @files
if($LASTEXITCODE -ne 0){throw 'User compilation failed'}
Copy-Item (Join-Path $source 'DTCE-Tool-Kit.exe.config') "$exe.config" -Force
[IO.File]::WriteAllText((Join-Path $stage 'README.txt'),"DTCE Tool Kit - User build. Keep EXE/config and tools/qpdf together. Requires Windows x64 and .NET Framework4.8. Access approval from owner required. No Admin/private signing key included.")
& (Join-Path $PSScriptRoot 'Install-PdfTools.ps1') -OutputDirectory (Join-Path $stage 'tools/qpdf')
$zip=Join-Path $OutputDirectory 'DTCE-Tool-Kit-User.zip'
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -Force
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive=[IO.Compression.ZipFile]::OpenRead($zip)
try{if(@($archive.Entries | Where-Object FullName -match 'Admin|\.dat$').Count){throw 'Unexpected/private files in User release'}}finally{$archive.Dispose()}
Write-Output "Built User-only release: $zip"
