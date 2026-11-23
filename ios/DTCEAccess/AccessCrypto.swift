import Foundation
import CryptoKit
import Security
import CommonCrypto

enum AccessError: LocalizedError {
    case invalid(String)
    var errorDescription: String? { if case .invalid(let message) = self { return message }; return nil }
}
struct PublicConfiguration: Codable {
    let keyId: String
    let n: String
    let e: String
    static func bundled() throws -> PublicConfiguration {
        guard let url = Bundle.main.url(forResource: "public-key", withExtension: "json") else { throw AccessError.invalid("Missing public key configuration.") }
        return try JSONDecoder().decode(Self.self, from: Data(contentsOf: url))
    }
}
struct MobileEnvelope: Decodable {
    let format: String
    let iterations: Int
    let salt: String
    let iv: String
    let ciphertext: String
    let mac: String
}
private struct PrivatePayload: Decodable {
    let keyId: String
    let jwk: [String: String]
}
struct GeneratedAccess {
    let code: String
    let expires: Date
}
enum AccessCrypto {
    static func decode64(_ text: String) throws -> Data {
        guard let data = Data(base64Encoded: text), data.count <= 18000 else { throw AccessError.invalid("Invalid encrypted key file.") }
        return data
    }
    static func decodeURL64(_ text: String) throws -> Data {
        let normal = text.replacingOccurrences(of: "-", with: "+").replacingOccurrences(of: "_", with: "/")
        return try decode64(normal + String(repeating: "=", count: (4 - normal.count % 4) % 4))
    }
    static func decrypt(_ file: Data, password: String, expected: PublicConfiguration) throws -> Data {
        guard file.count <= 40000, !password.isEmpty else { throw AccessError.invalid("Select your exported mobile key and enter its password.") }
        let envelope = try JSONDecoder().decode(MobileEnvelope.self, from: file)
        guard envelope.format == "DTCE-MOBILE-KEY-V1", envelope.iterations == 600000 else { throw AccessError.invalid("Use the encrypted mobile key exported by Windows Admin v1.4.26 or newer.") }
        let salt = try decode64(envelope.salt), iv = try decode64(envelope.iv), cipher = try decode64(envelope.ciphertext), mac = try decode64(envelope.mac)
        guard salt.count == 16, iv.count == 16, mac.count == 32, !cipher.isEmpty, cipher.count % 16 == 0 else { throw AccessError.invalid("Invalid encrypted key file.") }
        var derived = [UInt8](repeating: 0, count: 64)
        var passwordBytes = Array(password.utf8)
        defer { derived.withUnsafeMutableBytes { $0.initializeMemory(as: UInt8.self, repeating: 0) }; passwordBytes.withUnsafeMutableBytes { $0.initializeMemory(as: UInt8.self, repeating: 0) } }
        let result: Int32 = passwordBytes.withUnsafeBytes { pw in
            salt.withUnsafeBytes { saltBytes in
                CCKeyDerivationPBKDF(CCPBKDFAlgorithm(kCCPBKDF2), pw.baseAddress?.assumingMemoryBound(to: Int8.self), pw.count, saltBytes.baseAddress?.assumingMemoryBound(to: UInt8.self), salt.count, CCPseudoRandomAlgorithm(kCCPRFHmacAlgSHA256), UInt32(envelope.iterations), &derived, 64)
            }
        }
        guard result == kCCSuccess else { throw AccessError.invalid("Unable to unlock the key.") }
        let authenticated = Data("DTCE-MOBILE-KEY-V1|600000|\(envelope.salt)|\(envelope.iv)|\(envelope.ciphertext)".utf8)
        let computed = Data(HMAC<SHA256>.authenticationCode(for: authenticated, using: SymmetricKey(data: Data(derived[32..<64]))))
        var difference: UInt8 = 0
        for (a,b) in zip(computed,mac) { difference |= a ^ b }
        guard difference == 0 else { throw AccessError.invalid("Wrong password or damaged key file.") }
        var plain = [UInt8](repeating: 0, count: cipher.count + kCCBlockSizeAES128)
        defer { plain.withUnsafeMutableBytes { $0.initializeMemory(as: UInt8.self, repeating: 0) } }
        var written = 0
        let capacity = plain.count
        let status: CCCryptorStatus = derived.withUnsafeBytes { keyBytes in
            iv.withUnsafeBytes { ivBytes in
                cipher.withUnsafeBytes { cipherBytes in
                    plain.withUnsafeMutableBytes { output in
                        CCCrypt(CCOperation(kCCDecrypt), CCAlgorithm(kCCAlgorithmAES), CCOptions(kCCOptionPKCS7Padding), keyBytes.baseAddress, kCCKeySizeAES256, ivBytes.baseAddress, cipherBytes.baseAddress, cipher.count, output.baseAddress, capacity, &written)
                    }
                }
            }
        }
        guard status == kCCSuccess else { throw AccessError.invalid("Unable to decrypt the key file.") }
        let payload = try JSONDecoder().decode(PrivatePayload.self, from: Data(plain.prefix(written)))
        guard payload.keyId == expected.keyId, payload.jwk["kty"] == "RSA", payload.jwk["n"] == expected.n, payload.jwk["e"] == expected.e else { throw AccessError.invalid("This key does not match DTCE Tool Kit.") }
        var integers = [Data([0x02,0x01,0x00])]
        for field in ["n","e","d","p","q","dp","dq","qi"] {
            guard let text = payload.jwk[field] else { throw AccessError.invalid("Incomplete RSA key.") }
            integers.append(integer(try decodeURL64(text)))
        }
        let der = tagged(0x30, integers.reduce(Data(), +))
        _ = try privateKey(der) // Security validates the RSA representation.
        return der
    }
    private static func tagged(_ tag: UInt8, _ value: Data) -> Data {
        var length = Data()
        if value.count < 128 { length.append(UInt8(value.count)) }
        else { var count = value.count; var bytes = [UInt8](); while count > 0 { bytes.insert(UInt8(count & 255), at: 0); count >>= 8 }; length.append(0x80 | UInt8(bytes.count)); length.append(contentsOf: bytes) }
        return Data([tag]) + length + value
    }
    private static func integer(_ data: Data) -> Data {
        var bytes = Array(data.drop(while: { $0 == 0 }))
        if bytes.isEmpty { bytes = [0] }
        if bytes[0] & 0x80 != 0 { bytes.insert(0, at: 0) }
        return tagged(0x02,Data(bytes))
    }
    static func privateKey(_ der: Data) throws -> SecKey {
        var error: Unmanaged<CFError>?
        guard let key = SecKeyCreateWithData(der as CFData, [kSecAttrKeyType:kSecAttrKeyTypeRSA, kSecAttrKeyClass:kSecAttrKeyClassPrivate] as CFDictionary, &error) else { if let error = error?.takeRetainedValue() { throw error }; throw AccessError.invalid("Invalid RSA signing key.") }
        return key
    }
    static func generate(key: SecKey, request input: String, apps: Int, now: Date = Date()) throws -> GeneratedAccess {
        let request = input.components(separatedBy: .whitespacesAndNewlines).joined().uppercased()
        guard request.range(of: "^(DTCE|BEAM)-[A-F0-9]{40}$", options: .regularExpression) != nil else { throw AccessError.invalid("Paste the complete request code (DTCE-... or BEAM-...) from the User app.") }
        guard apps > 0 && (apps & ~16375) == 0 else { throw AccessError.invalid("Select at least one app.") }
        guard now.timeIntervalSince1970.isFinite, (0...253402300799).contains(now.timeIntervalSince1970) else { throw AccessError.invalid("Check your iPhone date and time.") }
        let issued = Int64(now.timeIntervalSince1970)
        guard issued >= 0 else { throw AccessError.invalid("Check your iPhone date and time.") }
        let expiry = issued + 28800
        let message = Data("BEAM-ACCESS-V4|\(request)|\(issued).\(expiry)|\(apps)".utf8)
        var error: Unmanaged<CFError>?
        guard let signature = SecKeyCreateSignature(key, .rsaSignatureMessagePKCS1v15SHA256, message as CFData, &error) as Data? else { if let error = error?.takeRetainedValue() { throw error }; throw AccessError.invalid("Signing failed.") }
        return GeneratedAccess(code:"BA4.\(issued).\(expiry).\(apps).\(signature.base64EncodedString())", expires:Date(timeIntervalSince1970:TimeInterval(expiry)))
    }
    static func generateCertification(key: SecKey, version: String, now: Date = Date()) throws -> GeneratedAccess {
        let parts = version.split(separator: ".", omittingEmptySubsequences: false)
        guard parts.count == 3, parts.allSatisfy({ part in
            guard let value = Int(part), (0...65535).contains(value) else { return false }
            return String(value) == String(part)
        }) else { throw AccessError.invalid("Enter the exact User version, for example 1.4.27 (three numbers).") }
        guard now.timeIntervalSince1970.isFinite, (0...253399708799).contains(now.timeIntervalSince1970) else { throw AccessError.invalid("Check your iPhone date and time.") }
        let issued = Int64(now.timeIntervalSince1970), expiry = issued + 2592000
        let target = version.replacingOccurrences(of: ".", with: "-")
        let message = Data("DTCE-CERTIFICATION-V1|\(issued).\(expiry)|7|\(target)".utf8)
        var error: Unmanaged<CFError>?
        guard let signature = SecKeyCreateSignature(key, .rsaSignatureMessagePKCS1v15SHA256, message as CFData, &error) as Data? else { if let error = error?.takeRetainedValue() { throw error }; throw AccessError.invalid("Signing failed.") }
        return GeneratedAccess(code:"BC1.\(issued).\(expiry).7.\(target).\(signature.base64EncodedString())", expires:Date(timeIntervalSince1970:TimeInterval(expiry)))
    }

}


// A separate, transient signing identity for the visible review/demo mode.
// No production key can be supplied to this initializer or persisted by this class.
final class DemoAccess {
    static let sampleRequest = "BEAM-0123456789ABCDEF0123456789ABCDEF01234567"
    static let prefix = "DEMO."
    private let key: SecKey
    init() throws {
        var error: Unmanaged<CFError>?
        let attributes: [String:Any] = [kSecAttrKeyType as String:kSecAttrKeyTypeRSA, kSecAttrKeySizeInBits as String:2048, kSecPrivateKeyAttrs as String:[kSecAttrIsPermanent as String:false]]
        guard let value = SecKeyCreateRandomKey(attributes as CFDictionary, &error) else {
            if let error = error?.takeRetainedValue() { throw error }
            throw AccessError.invalid("Unable to start the demo session.")
        }
        key = value
    }
    func generate(request: String, apps: Int, now: Date = Date()) throws -> GeneratedAccess {
        let sample = try AccessCrypto.generate(key:key,request:request,apps:apps,now:now)
        return GeneratedAccess(code:Self.prefix+sample.code,expires:sample.expires)
    }
    func certification(version: String, now: Date = Date()) throws -> GeneratedAccess {
        let sample = try AccessCrypto.generateCertification(key:key,version:version,now:now)
        return GeneratedAccess(code:Self.prefix+sample.code,expires:sample.expires)
    }
}
