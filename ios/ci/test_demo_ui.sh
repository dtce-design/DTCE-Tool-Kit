#!/bin/bash
set -euo pipefail
DEVICE_TYPE=$(xcrun simctl list devicetypes -j | python3 -c 'import json,sys; rows=json.load(sys.stdin)["devicetypes"]; names=[r["identifier"] for r in rows if "iPhone-17-Pro-Max" in r["identifier"] or "iPhone-16-Pro-Max" in r["identifier"]]; print(names[0])')
RUNTIME=$(xcrun simctl list runtimes -j | python3 -c 'import json,sys; rows=[r for r in json.load(sys.stdin)["runtimes"] if r.get("isAvailable") and "iOS" in r["name"]]; print(sorted(rows,key=lambda r:tuple(map(int,r["version"].split("."))))[-1]["identifier"])')
DEVICE=$(xcrun simctl create DTCE-Demo-Tests "$DEVICE_TYPE" "$RUNTIME")
trap 'xcrun simctl shutdown "$DEVICE" >/dev/null 2>&1 || true; xcrun simctl delete "$DEVICE" >/dev/null 2>&1 || true' EXIT
xcrun simctl boot "$DEVICE"
xcrun simctl bootstatus "$DEVICE" -b
xcrun simctl status_bar "$DEVICE" override --time '9:41' --dataNetwork wifi --wifiMode active --wifiBars 3 --batteryState charged --batteryLevel 100
xcodebuild test -project ios/DTCEAccess.xcodeproj -scheme DTCEAccess -configuration Debug -destination "id=$DEVICE" -derivedDataPath ios-ui-build -resultBundlePath ios-ui-results.xcresult -parallel-testing-enabled NO CODE_SIGNING_ALLOWED=NO -only-testing:DTCEAccessUITests/DemoFlowUITests/testDemoWithoutSigningKey
mkdir -p test-results/ios-demo-screenshots
xcrun xcresulttool export attachments --path ios-ui-results.xcresult --output-path test-results/ios-demo-screenshots
