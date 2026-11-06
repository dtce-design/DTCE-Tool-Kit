import Foundation
import Security

@main struct DemoModelTests {
    @MainActor static func main() async throws {
        func check(_ condition: Bool, _ message: String) throws { if !condition { throw AccessError.invalid(message) } }
        let model = AccessModel()
        let savedKeyBefore = KeyVault.hasKey
        let ownerRequest = "BEAM-AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        model.request = ownerRequest
        model.allowedApps = [16]
        model.certification = true
        model.userVersion = "1.4.35"
        model.startDemo()
        try check(model.isDemo && model.unlocked, "Demo must work without importing or authenticating a real key")
        try check(model.hasSavedKey == savedKeyBefore, "Demo changed the saved-key indicator")
        model.generate()
        try check(model.code.hasPrefix("DEMO.BA4."), "Normal demo output must carry an unmistakable marker")
        try check(model.expires != nil, "Demo expiry is missing")
        model.certification = true
        model.generate()
        try check(model.code.hasPrefix("DEMO.BC1."), "Certification demo output must carry the marker")
        model.forget()
        try check(model.isDemo && KeyVault.hasKey == savedKeyBefore, "Demo attempted to remove a real key")
        await model.unlockKey()
        try check(model.isDemo, "Owner unlock must not run within demo mode")
        model.lock()
        try check(!model.isDemo && !model.unlocked && model.code.isEmpty, "Lock must discard the demo session and output")
        try check(model.request == ownerRequest && model.allowedApps == [16] && model.certification && model.userVersion == "1.4.35", "Demo did not restore the owner's input choices")
        try check(KeyVault.hasKey == savedKeyBefore, "Demo persisted a key or changed the Keychain")
        model.unlocked = true
        model.generate()
        try check(model.code.isEmpty, "Owner mode generated a code without an imported key")
        model.lock()
        model.startDemo()
        model.request = "invalid request"
        model.generate()
        try check(model.code.isEmpty, "Demo bypassed normal request validation")
        model.lock()
        print("PASS: demo/owner separation, Keychain preservation, output markers, owner-input restoration and request validation.")
    }
}
