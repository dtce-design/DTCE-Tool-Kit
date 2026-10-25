$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$source=Join-Path $repo 'mobile'
$dist=Join-Path $repo 'dist'
$stage=Join-Path $dist 'Mobile'
New-Item -ItemType Directory -Force $stage | Out-Null
$allowed=@('index.html','style.css','app.mjs','crypto.mjs','public-key.json','manifest.webmanifest','sw.js','icon-180.png','icon-192.png','icon-512.png')
foreach($name in $allowed){Copy-Item -LiteralPath (Join-Path $source $name) -Destination (Join-Path $stage $name) -Force}
$actual=@(Get-ChildItem -LiteralPath $stage -File -Recurse)
if($actual.Count-ne $allowed.Count-or @($actual|Where-Object {$_.Name -notin $allowed}).Count){throw 'Unexpected file in mobile web app'}
[xml]$public=Get-Content (Join-Path $repo 'src\AccessPublicKey.xml') -Raw
$config=Get-Content (Join-Path $source 'public-key.json') -Raw | ConvertFrom-Json
$modulus=$public.RSAKeyValue.Modulus.TrimEnd('=').Replace('+','-').Replace('/','_')
$exponent=$public.RSAKeyValue.Exponent.TrimEnd('=').Replace('+','-').Replace('/','_')
if($config.n-ne $modulus-or $config.e-ne $exponent){throw 'Mobile public key does not match desktop'}
$hash=[Security.Cryptography.SHA256]::Create()
try{$keyId=([BitConverter]::ToString($hash.ComputeHash([Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText((Join-Path $repo 'src\AccessPublicKey.xml')))))).Replace('-','').Substring(0,16)}finally{$hash.Dispose()}
if($config.keyId-ne $keyId){throw 'Mobile key ID does not match desktop'}
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath (Join-Path $dist 'DTCE-Mobile-Access.zip') -Force
Write-Output 'Built key-free mobile web app: dist/DTCE-Mobile-Access.zip'
