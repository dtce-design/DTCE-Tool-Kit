$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$source=Join-Path $repo 'src'
$out=Join-Path $repo 'test-results'
New-Item -ItemType Directory -Force $out | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$files=Get-ChildItem $source -Filter '*.cs' | ForEach-Object FullName
foreach($test in @('PieceTests','CurtailmentTests','AccessProtocolTests','CommercialBarsTests','ExcelEstimateDataTests','BeamQuantitiesTests','SlabCurtailmentTests','SlabQuantitiesTests','SlabEstimatorTests','SlabScheduleTests','SlabEntryOptionsTests','ProgramOfWorksTests','OpeningHardwareLinkTests','FinishQuantitiesTests','FinishTypeGroupingTests','WorksSummaryLayoutTests','WorksWorkbookWriterTests','RoofingWorksTests','SlabFormatterTests')){
 $exe=Join-Path $out "$test.exe"
 & $compiler /reference:System.IO.Compression.dll "/resource:$source\WindSurfaces.txt,WindSurfaces.txt" "/resource:$source\WindMap27.jpg,WindMap27.jpg" "/resource:$source\WindMap28.jpg,WindMap28.jpg" "/resource:$source\WindMap29.jpg,WindMap29.jpg" /nologo /target:exe "/main:$test" "/out:$exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll /reference:System.Security.dll /reference:Microsoft.CSharp.dll "/resource:$source\AccessPublicKey.xml,AccessPublicKey.xml" "/resource:$source\app.ico,DtceIcon.ico" "/resource:$source\SlabTemplate.dxf,SlabTemplate.dxf" @files (Join-Path $repo "tests\$test.cs")
 if($LASTEXITCODE-ne 0){throw "Compile failed: $test"}
 & $exe
 if($LASTEXITCODE-ne 0){throw "Test failed: $test"}
}

$exe=Join-Path $out 'BeamEarthworksTests.exe'
& $compiler /reference:System.IO.Compression.dll "/resource:$source\WindSurfaces.txt,WindSurfaces.txt" "/resource:$source\WindMap27.jpg,WindMap27.jpg" "/resource:$source\WindMap28.jpg,WindMap28.jpg" "/resource:$source\WindMap29.jpg,WindMap29.jpg" /nologo /define:ADMIN_APP /target:exe /main:BeamEarthworksTests "/out:$exe" /reference:System.Security.dll /reference:System.Core.dll "/resource:$source\AccessPublicKey.xml,AccessPublicKey.xml" "/resource:$source\app.ico,DtceIcon.ico" @files (Join-Path $repo 'tests\BeamEarthworksTests.cs') /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:Microsoft.CSharp.dll
if($LASTEXITCODE-ne 0){throw 'Beam earthworks test compilation failed'}
& $exe
if($LASTEXITCODE-ne 0){throw 'Beam earthworks tests failed'}

$exe=Join-Path $out 'EarthworkFootingTests.exe'
& $compiler /reference:System.IO.Compression.dll "/resource:$source\WindSurfaces.txt,WindSurfaces.txt" "/resource:$source\WindMap27.jpg,WindMap27.jpg" "/resource:$source\WindMap28.jpg,WindMap28.jpg" "/resource:$source\WindMap29.jpg,WindMap29.jpg" /nologo /define:ADMIN_APP /target:exe /main:EarthworkFootingTests "/out:$exe" /reference:System.Security.dll /reference:System.Core.dll "/resource:$source\AccessPublicKey.xml,AccessPublicKey.xml" "/resource:$source\app.ico,DtceIcon.ico" "/resource:$source\DtceLogo.png,DtceLogo.png" @files (Join-Path $repo 'tests\EarthworkFootingTests.cs') /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:Microsoft.CSharp.dll
if($LASTEXITCODE-ne 0){throw 'Earthwork and footing test compilation failed'}
& $exe
if($LASTEXITCODE-ne 0){throw 'Earthwork and footing tests failed'}

$exe=Join-Path $out 'AdminKeyStoreTests.exe'
& $compiler /reference:System.IO.Compression.dll "/resource:$source\WindSurfaces.txt,WindSurfaces.txt" "/resource:$source\WindMap27.jpg,WindMap27.jpg" "/resource:$source\WindMap28.jpg,WindMap28.jpg" "/resource:$source\WindMap29.jpg,WindMap29.jpg" /nologo /define:ADMIN_APP /target:exe /main:AdminKeyStoreTests "/out:$exe" /reference:System.Security.dll /reference:System.Core.dll "/resource:$source\AccessPublicKey.xml,AccessPublicKey.xml" @files (Join-Path $repo 'tests\AdminKeyStoreTests.cs') /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:Microsoft.CSharp.dll
if($LASTEXITCODE-ne 0){throw 'Admin key test compilation failed'}
& $exe
if($LASTEXITCODE-ne 0){throw 'Admin key test failed'}

$exe=Join-Path $out 'MobileInteropTests.exe'
& $compiler /reference:System.IO.Compression.dll "/resource:$source\WindSurfaces.txt,WindSurfaces.txt" "/resource:$source\WindMap27.jpg,WindMap27.jpg" "/resource:$source\WindMap28.jpg,WindMap28.jpg" "/resource:$source\WindMap29.jpg,WindMap29.jpg" /nologo /define:ADMIN_APP /target:exe /main:MobileInteropTests "/out:$exe" /reference:System.Security.dll /reference:System.Core.dll "/resource:$source\AccessPublicKey.xml,AccessPublicKey.xml" @files (Join-Path $repo 'tests\MobileInteropTests.cs') /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:Microsoft.CSharp.dll
if($LASTEXITCODE-ne 0){throw 'Mobile interop compilation failed'}
& $exe fixture $out
if($LASTEXITCODE-ne 0){throw 'Mobile encrypted fixture failed'}
& node (Join-Path $repo 'tests\mobile-crypto.test.mjs') $out
if($LASTEXITCODE-ne 0){throw 'Mobile Web Crypto test failed'}
& $exe verify $out
if($LASTEXITCODE-ne 0){throw 'Desktop/mobile signature compatibility failed'}

# Real activation/DPAPI restore with the ephemeral interop public key, isolated from owner approvals.
$exe=Join-Path $out 'CertificationCacheTests.exe'
& $compiler /reference:System.IO.Compression.dll "/resource:$source\WindSurfaces.txt,WindSurfaces.txt" "/resource:$source\WindMap27.jpg,WindMap27.jpg" "/resource:$source\WindMap28.jpg,WindMap28.jpg" "/resource:$source\WindMap29.jpg,WindMap29.jpg" /nologo /target:exe /main:CertificationCacheTests "/out:$exe" /reference:System.Security.dll /reference:System.Core.dll "/resource:$out\mobile-public.xml,AccessPublicKey.xml" @files (Join-Path $repo 'tests\CertificationCacheTests.cs') /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:Microsoft.CSharp.dll
if($LASTEXITCODE-ne 0){throw 'Certification cache compilation failed'}
& $exe $out
if($LASTEXITCODE-ne 0){throw 'Certification activation/cache test failed'}


$exe=Join-Path $out 'SteelFeatureTests.exe'
& $compiler /reference:System.IO.Compression.dll "/resource:$source\WindSurfaces.txt,WindSurfaces.txt" "/resource:$source\WindMap27.jpg,WindMap27.jpg" "/resource:$source\WindMap28.jpg,WindMap28.jpg" "/resource:$source\WindMap29.jpg,WindMap29.jpg" /nologo /define:ADMIN_APP /target:exe /main:SteelFeatureTests "/out:$exe" /reference:System.Security.dll /reference:System.Core.dll "/resource:$source\AccessPublicKey.xml,AccessPublicKey.xml" "/resource:$source\app.ico,DtceIcon.ico" "/resource:$source\DtceLogo.png,DtceLogo.png" @files (Join-Path $repo 'tests\SteelFeatureTests.cs') (Join-Path $repo 'tests\WorksWorkbookWriterTests.cs') /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:Microsoft.CSharp.dll
if($LASTEXITCODE-ne 0){throw 'Steel and design sheet test compilation failed'}
& $exe
if($LASTEXITCODE-ne 0){throw 'Steel and design sheet tests failed'}

# Capture each edition from its compiled UI using synthetic, saved test projects.
foreach($edition in @('Admin','User')){
 $exe=Join-Path $out "StoreScreenshots-$edition.exe"
 $screenshotCompilerArgs=@('/nologo','/target:exe','/main:StoreScreenshotTests',"/out:$exe",'/reference:System.Security.dll','/reference:System.Core.dll','/reference:System.Windows.Forms.dll','/reference:System.Drawing.dll','/reference:Microsoft.CSharp.dll',"/resource:$source\AccessPublicKey.xml,AccessPublicKey.xml","/resource:$source\app.ico,DtceIcon.ico","/resource:$source\DtceLogo.png,DtceLogo.png","/resource:$source\DpwhLogo.png,DpwhLogo.png","/resource:$source\SlabTemplate.dxf,SlabTemplate.dxf")
 if($edition -eq 'Admin'){$screenshotCompilerArgs+= '/define:ADMIN_APP'}
 & $compiler /reference:System.IO.Compression.dll "/resource:$source\WindSurfaces.txt,WindSurfaces.txt" "/resource:$source\WindMap27.jpg,WindMap27.jpg" "/resource:$source\WindMap28.jpg,WindMap28.jpg" "/resource:$source\WindMap29.jpg,WindMap29.jpg" @screenshotCompilerArgs @files (Join-Path $repo 'tests\StoreScreenshotTests.cs')
 if($LASTEXITCODE-ne 0){throw "$edition screenshot compilation failed"}
 & $exe (Join-Path $out "store-screenshots\$edition")
 if($LASTEXITCODE-ne 0){throw "$edition screenshot capture failed"}
}

# Background Store access policy and purchase-quantity contracts, plus Windows UI persistence.
$exe=Join-Path $out 'UpdateAndQuantityTests.exe'
& $compiler /reference:System.IO.Compression.dll "/resource:$source\WindSurfaces.txt,WindSurfaces.txt" "/resource:$source\WindMap27.jpg,WindMap27.jpg" "/resource:$source\WindMap28.jpg,WindMap28.jpg" "/resource:$source\WindMap29.jpg,WindMap29.jpg" /nologo /define:ADMIN_APP,WINDOWS_TESTS /target:exe /main:UpdateAndQuantityTests "/out:$exe" /reference:System.Security.dll /reference:System.Core.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:Microsoft.CSharp.dll "/resource:$source\AccessPublicKey.xml,AccessPublicKey.xml" "/resource:$source\app.ico,DtceIcon.ico" "/resource:$source\DtceLogo.png,DtceLogo.png" "/resource:$source\SlabTemplate.dxf,SlabTemplate.dxf" @files (Join-Path $repo 'tests\UpdateAndQuantityTests.cs')
if($LASTEXITCODE-ne 0){throw 'Update and quantity tests compilation failed'}
& $exe
if($LASTEXITCODE-ne 0){throw 'Update and quantity tests failed'}

# Reconstructed workbook TIN, directional terrain, exposure, and offline persistence.
$exe=Join-Path $out 'WindRecoveryTests.exe'
& $compiler /nologo /target:exe /define:ADMIN_APP /main:WindRecoveryTests "/out:$exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Security.dll /reference:System.Core.dll /reference:System.IO.Compression.dll /reference:Microsoft.CSharp.dll "/resource:$source\WindSurfaces.txt,WindSurfaces.txt" "/resource:$source\WindMap27.jpg,WindMap27.jpg" "/resource:$source\WindMap28.jpg,WindMap28.jpg" "/resource:$source\WindMap29.jpg,WindMap29.jpg" @files (Join-Path $repo 'tests\WindRecoveryTests.cs')
if($LASTEXITCODE-ne 0){throw 'Wind recovery compilation failed'}
& $exe
if($LASTEXITCODE-ne 0){throw 'Wind recovery checks failed'}
