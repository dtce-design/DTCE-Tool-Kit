$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$source=Join-Path $repo 'src'
$out=Join-Path $repo 'test-results'
New-Item -ItemType Directory -Force $out | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$files=Get-ChildItem $source -Filter '*.cs' | ForEach-Object FullName
foreach($test in @('PieceTests','CurtailmentTests','AccessProtocolTests')){
 $exe=Join-Path $out "$test.exe"
 & $compiler /nologo /target:exe "/main:$test" "/out:$exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll /reference:System.Security.dll /reference:Microsoft.CSharp.dll "/resource:$source\AccessPublicKey.xml,AccessPublicKey.xml" @files (Join-Path $repo "tests\$test.cs")
 if($LASTEXITCODE-ne 0){throw "Compile failed: $test"}
 & $exe
 if($LASTEXITCODE-ne 0){throw "Test failed: $test"}
}

$exe=Join-Path $out 'AdminKeyStoreTests.exe'
& $compiler /nologo /define:ADMIN_APP /target:exe /main:AdminKeyStoreTests "/out:$exe" /reference:System.Security.dll /reference:System.Core.dll "/resource:$source\AccessPublicKey.xml,AccessPublicKey.xml" @files (Join-Path $repo 'tests\AdminKeyStoreTests.cs') /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:Microsoft.CSharp.dll
if($LASTEXITCODE-ne 0){throw 'Admin key test compilation failed'}
& $exe
if($LASTEXITCODE-ne 0){throw 'Admin key test failed'}
