$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$source=Join-Path $repo 'src'
$out=Join-Path $repo 'test-results'
New-Item -ItemType Directory -Force $out | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$files=Get-ChildItem $source -Filter '*.cs' | ForEach-Object FullName
foreach($test in @('PieceTests','CurtailmentTests','AccessProtocolTests','CommercialBarsTests','ExcelEstimateDataTests','BeamQuantitiesTests','SlabCurtailmentTests','SlabQuantitiesTests','SlabEstimatorTests','SlabScheduleTests','SlabEntryOptionsTests','ProgramOfWorksTests','OpeningHardwareLinkTests','FinishQuantitiesTests','FinishTypeGroupingTests','WorksSummaryLayoutTests','WorksWorkbookWriterTests','RoofingWorksTests','SlabFormatterTests')){
 $exe=Join-Path $out "$test.exe"
 & $compiler /nologo /target:exe "/main:$test" "/out:$exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll /reference:System.Security.dll /reference:Microsoft.CSharp.dll "/resource:$source\AccessPublicKey.xml,AccessPublicKey.xml" "/resource:$source\app.ico,DtceIcon.ico" "/resource:$source\SlabTemplate.dxf,SlabTemplate.dxf" @files (Join-Path $repo "tests\$test.cs")
 if($LASTEXITCODE-ne 0){throw "Compile failed: $test"}
 & $exe
 if($LASTEXITCODE-ne 0){throw "Test failed: $test"}
}

$exe=Join-Path $out 'BeamEarthworksTests.exe'
& $compiler /nologo /define:ADMIN_APP /target:exe /main:BeamEarthworksTests "/out:$exe" /reference:System.Security.dll /reference:System.Core.dll "/resource:$source\AccessPublicKey.xml,AccessPublicKey.xml" "/resource:$source\app.ico,DtceIcon.ico" @files (Join-Path $repo 'tests\BeamEarthworksTests.cs') /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:Microsoft.CSharp.dll
if($LASTEXITCODE-ne 0){throw 'Beam earthworks test compilation failed'}
& $exe
if($LASTEXITCODE-ne 0){throw 'Beam earthworks tests failed'}

$exe=Join-Path $out 'EarthworkFootingTests.exe'
& $compiler /nologo /define:ADMIN_APP /target:exe /main:EarthworkFootingTests "/out:$exe" /reference:System.Security.dll /reference:System.Core.dll "/resource:$source\AccessPublicKey.xml,AccessPublicKey.xml" "/resource:$source\app.ico,DtceIcon.ico" "/resource:$source\DtceLogo.png,DtceLogo.png" @files (Join-Path $repo 'tests\EarthworkFootingTests.cs') /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:Microsoft.CSharp.dll
if($LASTEXITCODE-ne 0){throw 'Earthwork and footing test compilation failed'}
& $exe
if($LASTEXITCODE-ne 0){throw 'Earthwork and footing tests failed'}

$exe=Join-Path $out 'AdminKeyStoreTests.exe'
& $compiler /nologo /define:ADMIN_APP /target:exe /main:AdminKeyStoreTests "/out:$exe" /reference:System.Security.dll /reference:System.Core.dll "/resource:$source\AccessPublicKey.xml,AccessPublicKey.xml" @files (Join-Path $repo 'tests\AdminKeyStoreTests.cs') /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:Microsoft.CSharp.dll
if($LASTEXITCODE-ne 0){throw 'Admin key test compilation failed'}
& $exe
if($LASTEXITCODE-ne 0){throw 'Admin key test failed'}

$exe=Join-Path $out 'MobileInteropTests.exe'
& $compiler /nologo /define:ADMIN_APP /target:exe /main:MobileInteropTests "/out:$exe" /reference:System.Security.dll /reference:System.Core.dll "/resource:$source\AccessPublicKey.xml,AccessPublicKey.xml" @files (Join-Path $repo 'tests\MobileInteropTests.cs') /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:Microsoft.CSharp.dll
if($LASTEXITCODE-ne 0){throw 'Mobile interop compilation failed'}
& $exe fixture $out
if($LASTEXITCODE-ne 0){throw 'Mobile encrypted fixture failed'}
& node (Join-Path $repo 'tests\mobile-crypto.test.mjs') $out
if($LASTEXITCODE-ne 0){throw 'Mobile Web Crypto test failed'}
& $exe verify $out
if($LASTEXITCODE-ne 0){throw 'Desktop/mobile signature compatibility failed'}

# Real activation/DPAPI restore with the ephemeral interop public key, isolated from owner approvals.
$exe=Join-Path $out 'CertificationCacheTests.exe'
& $compiler /nologo /target:exe /main:CertificationCacheTests "/out:$exe" /reference:System.Security.dll /reference:System.Core.dll "/resource:$out\mobile-public.xml,AccessPublicKey.xml" @files (Join-Path $repo 'tests\CertificationCacheTests.cs') /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:Microsoft.CSharp.dll
if($LASTEXITCODE-ne 0){throw 'Certification cache compilation failed'}
& $exe $out
if($LASTEXITCODE-ne 0){throw 'Certification activation/cache test failed'}
