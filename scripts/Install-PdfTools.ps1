param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$version='12.4.2'
$expected='773d2fa0c7d161e2271430734338e560a07b25d0edf11ceee8e53de435012766'
$uri="https://github.com/qpdf/qpdf/releases/download/v$version/qpdf-$version-mingw64.zip"
$stage=Join-Path ([IO.Path]::GetTempPath()) ('DTCE-qpdf-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $stage | Out-Null
try {
 [Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12
 $archive=Join-Path $stage 'qpdf.zip'
 Invoke-WebRequest -UseBasicParsing -Uri $uri -OutFile $archive
 if((Get-FileHash -Algorithm SHA256 -LiteralPath $archive).Hash.ToLowerInvariant() -ne $expected){throw 'qpdf release checksum mismatch'}
 Expand-Archive -LiteralPath $archive -DestinationPath $stage
 $bin=Join-Path $stage "qpdf-$version-mingw64/bin"
 New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
 foreach($name in @('qpdf.exe','qpdf30.dll','libgcc_s_seh-1.dll','libstdc++-6.dll','libwinpthread-1.dll')){Copy-Item -LiteralPath (Join-Path $bin $name) -Destination (Join-Path $OutputDirectory $name) -Force}
 $licenses=Join-Path $OutputDirectory 'licenses'
 New-Item -ItemType Directory -Force $licenses | Out-Null
 Copy-Item -Path (Join-Path (Split-Path -Parent $PSScriptRoot) 'third-party/qpdf/*') -Destination $licenses -Force
} finally {Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue}
