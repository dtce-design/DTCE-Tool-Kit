import Foundation
import Security
let folder = URL(fileURLWithPath:CommandLine.arguments[1])
func read(_ name:String) throws -> Data { try Data(contentsOf:folder.appendingPathComponent(name)) }
func rejects(_ action:() throws -> Void) throws { do { try action() } catch { return }; throw AccessError.invalid("Expected rejection") }
let envelope = try read("mobile-fixture.json")
let expected = try JSONDecoder().decode(PublicConfiguration.self,from:read("mobile-public.json"))
let password = "test-only Unicode passphrase \u{00f1} 2026"
var der = try AccessCrypto.decrypt(envelope,password:password,expected:expected)
let key = try AccessCrypto.privateKey(der)
der.resetBytes(in:0..<der.count)
try rejects { _ = try AccessCrypto.decrypt(envelope,password:"wrong password",expected:expected) }
let wrong = PublicConfiguration(keyId:"WRONG",n:expected.n,e:expected.e)
try rejects { _ = try AccessCrypto.decrypt(envelope,password:password,expected:wrong) }
var damaged = try JSONSerialization.jsonObject(with:envelope) as! [String:Any]
damaged["mac"] = Data(repeating:0,count:32).base64EncodedString()
try rejects { _ = try AccessCrypto.decrypt(JSONSerialization.data(withJSONObject:damaged),password:password,expected:expected) }
let code = String(decoding:try read("mobile-code.txt"),as:UTF8.self)
let parts = code.split(separator:".")
let issued = TimeInterval(parts[1])!
let request = "BEAM-0123456789ABCDEF0123456789ABCDEF01234567"
let actual = try AccessCrypto.generate(key:key,request:request.lowercased(),apps:5,now:Date(timeIntervalSince1970:issued))
guard actual.code == code, actual.expires.timeIntervalSince1970 == issued+28800 else { throw AccessError.invalid("Native signature not compatible with desktop/Web Crypto") }
for mask in [0,8,-1] { try rejects { _ = try AccessCrypto.generate(key:key,request:request,apps:mask) } }
try rejects { _ = try AccessCrypto.generate(key:key,request:"invalid",apps:1) }
print("PASS: native Swift decrypts actual .NET encrypted export and produces byte-identical BA3 signature; tamper/password/key/mask/expiry checks passed. Ephemeral test key only.")

let certification = String(decoding:try read("mobile-cert.txt"),as:UTF8.self)
let certParts = certification.split(separator:".")
let certIssued = TimeInterval(certParts[1])!
let certVersion = certParts[4].replacingOccurrences(of:"-",with:".")
let nativeCertification = try AccessCrypto.generateCertification(key:key,version:certVersion,now:Date(timeIntervalSince1970:certIssued))
guard nativeCertification.code == certification, nativeCertification.expires.timeIntervalSince1970 == certIssued+2592000 else { throw AccessError.invalid("Native certification signature not compatible with desktop/Web Crypto") }
for version in ["", "1.4", "1.4.27.0", "01.4.27", "1.4.-1", "1.4.65536", "1-4-27"] { try rejects { _ = try AccessCrypto.generateCertification(key:key,version:version) } }
for time in [Double.nan,Double.infinity,-1] { try rejects { _ = try AccessCrypto.generateCertification(key:key,version:certVersion,now:Date(timeIntervalSince1970:time)) } }
print("PASS: native 30-day certification signature exactly matches desktop/Web Crypto; invalid versions/dates rejected.")
