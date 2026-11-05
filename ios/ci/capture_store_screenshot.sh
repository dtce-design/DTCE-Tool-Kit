#!/bin/bash
set -euo pipefail
SCREENSHOT_WORK="$RUNNER_TEMP/DTCE-AppStore-Screenshot"
export SCREENSHOT_WORK
mkdir -p "$SCREENSHOT_WORK"
DEVICE_TYPE=$(xcrun simctl list devicetypes -j | python3 -c 'import json,sys; types=json.load(sys.stdin)["devicetypes"]; matches=[d["identifier"] for d in types if "iPhone-17-Pro-Max" in d["identifier"] or "iPhone-16-Pro-Max" in d["identifier"]]; print(matches[0])')
RUNTIME=$(xcrun simctl list runtimes -j | python3 -c 'import json,sys; r=[r for r in json.load(sys.stdin)["runtimes"] if r.get("isAvailable") and "iOS" in r["name"]]; print(sorted(r,key=lambda x:tuple(map(int,x["version"].split("."))))[-1]["identifier"])')
DEVICE=$(xcrun simctl create DTCE-AppStore "$DEVICE_TYPE" "$RUNTIME")
trap 'xcrun simctl shutdown "$DEVICE" >/dev/null 2>&1 || true; xcrun simctl delete "$DEVICE" >/dev/null 2>&1 || true' EXIT
xcrun simctl boot "$DEVICE"
xcrun simctl bootstatus "$DEVICE" -b
xcodebuild -project ios/DTCEAccess.xcodeproj -scheme DTCEAccess -configuration Debug -sdk iphonesimulator -destination "id=$DEVICE" -derivedDataPath "$SCREENSHOT_WORK/build" CODE_SIGNING_ALLOWED=NO build > "$SCREENSHOT_WORK/build.log" 2>&1
xcrun simctl install "$DEVICE" "$SCREENSHOT_WORK/build/Build/Products/Debug-iphonesimulator/DTCEAccess.app"
xcrun simctl status_bar "$DEVICE" override --time '9:41' --dataNetwork wifi --wifiMode active --wifiBars 3 --batteryState charged --batteryLevel 100
xcrun simctl launch "$DEVICE" com.dtce.accessadmin
sleep 5
mkdir -p test-results/ios-app-store
xcrun simctl io "$DEVICE" screenshot --type=png test-results/ios-app-store/DTCE-Access-iPhone.png
echo 'Captured the actual iPhone UI without importing any signing key.'
