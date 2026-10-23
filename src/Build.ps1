# User-only repository entry point. No key creation or Admin packaging in CI.
& (Join-Path (Split-Path -Parent $PSScriptRoot) 'scripts\Build-User.ps1')
if($LASTEXITCODE){exit $LASTEXITCODE}
