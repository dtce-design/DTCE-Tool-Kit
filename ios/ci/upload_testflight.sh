#!/bin/bash
# GitHub macOS runner only. Do not use shell tracing: secrets stay in environment.
set -euo pipefail
umask 077
: "${ASC_KEY_PATH:?Missing temporary API key path}"
: "${ASC_KEY_ID:?Missing API key ID}"
: "${ASC_ISSUER_ID:?Missing issuer ID}"
: "${GITHUB_RUN_NUMBER:?Missing run number}"
: "${GITHUB_RUN_ATTEMPT:?Missing run attempt}"
BUILD_NUMBER="${GITHUB_RUN_NUMBER}.${GITHUB_RUN_ATTEMPT}"
export BUILD_NUMBER
if ! [[ "$BUILD_NUMBER" =~ ^[1-9][0-9]{0,3}\.[1-9][0-9]?$ ]]; then
  echo 'Run number exceeds Apple build-number limits; update the version/build-number scheme.' >&2
  exit 1
fi
python3 - <<'PY'
import os, plistlib
from pathlib import Path
info=Path('ios/DTCEAccess/Info.plist')
value=plistlib.loads(info.read_bytes())
value['CFBundleVersion']=os.environ['BUILD_NUMBER']
info.write_bytes(plistlib.dumps(value))
export={'method':'app-store-connect','destination':'upload','teamID':'8R5VUGW834','signingStyle':'automatic','manageAppVersionAndBuildNumber':False,'uploadSymbols':False}
Path(os.environ['RUNNER_TEMP'],'DTCE-ExportOptions.plist').write_bytes(plistlib.dumps(export))
PY
ARCHIVE="$RUNNER_TEMP/DTCEAccess-TestFlight.xcarchive"
# Xcode exports/cloud-signs this tested archive using the authorized Apple team.
xcodebuild -project ios/DTCEAccess.xcodeproj -scheme DTCEAccess \
  -configuration Release -destination 'generic/platform=iOS' \
  -archivePath "$ARCHIVE" DEVELOPMENT_TEAM=8R5VUGW834 CODE_SIGNING_ALLOWED=NO archive
xcodebuild -exportArchive -archivePath "$ARCHIVE" \
  -exportOptionsPlist "$RUNNER_TEMP/DTCE-ExportOptions.plist" \
  -exportPath "$RUNNER_TEMP/DTCE-TestFlight-Export" \
  -allowProvisioningUpdates -authenticationKeyPath "$ASC_KEY_PATH" \
  -authenticationKeyID "$ASC_KEY_ID" -authenticationKeyIssuerID "$ASC_ISSUER_ID"
echo "Apple upload command completed for build $BUILD_NUMBER. Checking processing next."
