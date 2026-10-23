param(
 [Parameter(Mandatory=$true)][ValidatePattern('^[A-Fa-f0-9 ]{40,80}$')][string]$CertificateThumbprint,
 [Parameter(Mandatory=$true)][ValidatePattern('^http://')][string]$TimestampServer
)
$ErrorActionPreference='Stop'
$releaseRoot=Split-Path -Parent $PSScriptRoot
$thumbprint=$CertificateThumbprint.Replace(' ','')
$certificates=@(Get-ChildItem Cert:\CurrentUser\My,Cert:\LocalMachine\My -CodeSigningCert | Where-Object { $_.Thumbprint -eq $thumbprint -and $_.HasPrivateKey })
if($certificates.Count -eq 0){throw 'Install your trusted publisher code-signing certificate/private-key provider first. Do not use a self-signed certificate.'}
$certificate=$certificates[0]
if($certificate.Subject -eq $certificate.Issuer){throw 'Self-signed certificates are not suitable for this public release.'}
if($certificate.NotAfter -lt (Get-Date) -or $certificate.NotBefore -gt (Get-Date)){throw 'Certificate is not currently valid.'}
$chain=New-Object Security.Cryptography.X509Certificates.X509Chain
try{if(-not $chain.Build($certificate)){throw 'Certificate trust chain validation failed.'}}finally{$chain.Dispose()}
$targets=@((Join-Path $releaseRoot 'BeamScheduleFormatter.exe'),(Join-Path $releaseRoot 'Admin\BeamAccessAdmin.exe'))
foreach($target in $targets){if(-not (Test-Path -LiteralPath $target)){throw "Missing build: $target"}}
foreach($target in $targets){
 $result=Set-AuthenticodeSignature -LiteralPath $target -Certificate $certificate -HashAlgorithm SHA256 -IncludeChain NotRoot -TimestampServer $TimestampServer
 if($result.Status -ne 'Valid'){throw "Signing failed for $target : $($result.Status)"}
 $verified=Get-AuthenticodeSignature -LiteralPath $target
 if($verified.Status -ne 'Valid' -or $verified.SignerCertificate.Thumbprint -ne $thumbprint -or $null -eq $verified.TimeStamperCertificate){throw "Signature/timestamp validation failed: $target"}
}
# Repackage only after BOTH binaries pass signature and timestamp checks.
Compress-Archive -LiteralPath $targets[0],(Join-Path $releaseRoot 'BeamScheduleFormatter.exe.config'),(Join-Path $releaseRoot 'USER-README.txt') -DestinationPath (Join-Path $releaseRoot 'BeamFormatter-User.zip') -Force
$report=@('Both EXEs have valid timestamped Authenticode signatures.','Publisher: '+$certificate.Subject,'Certificate: '+$certificate.Thumbprint,'Verified UTC: '+[DateTime]::UtcNow.ToString('o'),'Target-desktop Smart App Control and full UI tests still required. Rebuilding invalidates signatures; sign again after every rebuild.')
$report | Set-Content -LiteralPath (Join-Path $releaseRoot 'Signing-status.txt')
Compress-Archive -LiteralPath $releaseRoot -DestinationPath (Join-Path (Split-Path -Parent $releaseRoot) 'BeamFormatter-Owner-Signed.zip') -Force
Write-Output 'Signed both apps and created BeamFormatter-Owner-Signed.zip. Verify on the target desktops before distributing.'
