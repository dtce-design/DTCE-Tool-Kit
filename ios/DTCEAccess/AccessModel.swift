import Foundation
import Combine
import Security
import LocalAuthentication

@MainActor final class AccessModel: ObservableObject {
    @Published var request = "" { didSet { clearOutput() } }
    @Published var allowedApps: Set<Int> = [] { didSet { clearOutput() } }
    @Published var password = ""
    @Published var fileName = "No file selected"
    @Published var code = ""
    @Published var expires: Date?
    @Published var message = "Import the encrypted mobile key exported from Windows Admin."
    @Published var busy = false
    @Published var unlocked = false
    @Published var hasSavedKey = KeyVault.hasKey
    private var encryptedFile: Data?
    private var key: SecKey?
    private var lastInteraction = Date()
    private var generation = 0
    private var authContext: LAContext?
    var keyId: String { (try? PublicConfiguration.bundled().keyId) ?? "Unavailable" }
    func touch() { lastInteraction = Date() }
    func clearOutput() { code = ""; expires = nil; touch() }
    func select(_ url: URL) {
        lock(); let access = url.startAccessingSecurityScopedResource(); defer { if access { url.stopAccessingSecurityScopedResource() } }
        do {
            let values = try url.resourceValues(forKeys:[.fileSizeKey]); guard let size = values.fileSize, size <= 40000 else { throw AccessError.invalid("Choose a mobile key file smaller than 40 KB.") }
            let data = try Data(contentsOf:url); let envelope = try JSONDecoder().decode(MobileEnvelope.self,from:data)
            guard envelope.format == "DTCE-MOBILE-KEY-V1" else { throw AccessError.invalid("Choose the .dtcekey mobile export, not the Windows DAT.") }
            encryptedFile = data; fileName = url.lastPathComponent; message = "Enter the export password, then protect the key with Face ID."
        } catch { encryptedFile = nil; fileName = "No file selected"; message = error.localizedDescription }
    }
    func importKey() async {
        guard !busy, let encrypted = encryptedFile, !password.isEmpty else { message = "Select your mobile key file and enter its password."; return }
        generation += 1; let attempt = generation; let secret = password; password = ""; busy = true
        defer { busy = false }
        do {
            let expected = try PublicConfiguration.bundled()
            var der = try await Task.detached(priority:.userInitiated) { try AccessCrypto.decrypt(encrypted,password:secret,expected:expected) }.value
            defer { der.resetBytes(in:0..<der.count) }
            guard attempt == generation else { return }
            let context = try await KeyVault.authorize(); defer { context.invalidate() }
            guard attempt == generation else { return }
            try KeyVault.store(der,context:context)
            key = try KeyVault.unlock(context:context); unlocked = true; hasSavedKey = true; encryptedFile = nil; fileName = "Key saved on this iPhone"; touch()
            message = "Face ID protection enabled. Paste a request code and select allowed apps."
        } catch { if attempt == generation { message = error.localizedDescription } }
    }
    func unlockKey() async {
        guard !busy else { return }; busy = true; generation += 1; let attempt = generation
        defer { busy = false }
        do {
            let context = try await KeyVault.authorize(); authContext = context; defer { context.invalidate(); authContext = nil }
            guard attempt == generation else { return }
            key = try KeyVault.unlock(context:context); unlocked = true; touch(); message = "Unlocked with Face ID."
        } catch { if attempt == generation { message = error.localizedDescription } }
    }
    func generate() {
        guard unlocked, let key = key else { message = "Unlock with Face ID first."; return }
        clearOutput()
        do { let result = try AccessCrypto.generate(key:key,request:request,apps:allowedApps.reduce(0) { $0 | $1 }); code = result.code; expires = result.expires; message = "Access code ready. Valid for eight hours from generation." }
        catch { message = error.localizedDescription }
    }
    func lock() { generation += 1; authContext?.invalidate(); authContext = nil; key = nil; unlocked = false; password = ""; clearOutput(); message = "Locked. Use Face ID to continue." }
    func checkTimeout() { if unlocked && Date().timeIntervalSince(lastInteraction) >= 300 { lock() } }
    func forget() {
        lock()
        do { try KeyVault.remove(); hasSavedKey = false; message = "Saved signing key removed. Your original encrypted export is unchanged." }
        catch { message = error.localizedDescription }
    }
}
