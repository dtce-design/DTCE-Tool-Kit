import Foundation
import LocalAuthentication
import Security

enum KeyVault {
    private static let service = "com.dtce.accessadmin.signing"
    private static let account = "desktop-rsa-v1"
    private static var query: [String:Any] { [kSecClass as String:kSecClassGenericPassword, kSecAttrService as String:service, kSecAttrAccount as String:account] }
    static var hasKey: Bool {
        var q = query; q[kSecReturnAttributes as String] = true; q[kSecUseAuthenticationUI as String] = kSecUseAuthenticationUIFail
        let status = SecItemCopyMatching(q as CFDictionary, nil)
        return status == errSecSuccess || status == errSecInteractionNotAllowed
    }
    static func authorize() async throws -> LAContext {
        let context = LAContext(); context.localizedFallbackTitle = ""; context.touchIDAuthenticationAllowableReuseDuration = 0
        var error: NSError?
        guard context.canEvaluatePolicy(.deviceOwnerAuthenticationWithBiometrics, error:&error), context.biometryType == .faceID else {
            throw AccessError.invalid("Face ID must be enrolled and available on this iPhone. Enable it in Settings before continuing.")
        }
        let granted = try await context.evaluatePolicy(.deviceOwnerAuthenticationWithBiometrics, localizedReason:"Unlock the DTCE access signing key.")
        guard granted else { throw AccessError.invalid("Face ID authentication was not completed.") }
        return context
    }
    static func store(_ der: Data, context: LAContext) throws {
        var error: Unmanaged<CFError>?
        guard let access = SecAccessControlCreateWithFlags(nil, kSecAttrAccessibleWhenPasscodeSetThisDeviceOnly, .biometryCurrentSet, &error) else { throw AccessError.invalid("Unable to create Face ID protection.") }
        // Update existing data without deleting a working key if an update fails.
        var lookup = query; lookup[kSecUseAuthenticationContext as String] = context
        let values: [String:Any] = [kSecValueData as String:der, kSecAttrAccessControl as String:access]
        let update = SecItemUpdate(lookup as CFDictionary, values as CFDictionary)
        if update == errSecItemNotFound {
            var add = query; for (key,value) in values { add[key] = value }
            add[kSecUseAuthenticationContext as String] = context
            guard SecItemAdd(add as CFDictionary,nil) == errSecSuccess else { throw AccessError.invalid("Unable to save the key in the iPhone Keychain.") }
        } else if update != errSecSuccess { throw AccessError.invalid("Unable to replace the saved key. Your existing key was kept.") }
    }
    static func unlock(context: LAContext) throws -> SecKey {
        var q = query; q[kSecReturnData as String] = true; q[kSecUseAuthenticationContext as String] = context
        var item: CFTypeRef?
        guard SecItemCopyMatching(q as CFDictionary,&item) == errSecSuccess, var der = item as? Data else {
            throw AccessError.invalid("Cannot unlock the saved key. If Face ID enrollment changed, remove it and import the encrypted export again.")
        }
        defer { der.resetBytes(in:0..<der.count) }
        return try AccessCrypto.privateKey(der)
    }
    static func remove() throws {
        let status = SecItemDelete(query as CFDictionary)
        guard status == errSecSuccess || status == errSecItemNotFound else { throw AccessError.invalid("Unable to remove the saved key.") }
    }
}
