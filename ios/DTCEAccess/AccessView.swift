import SwiftUI
import UniformTypeIdentifiers
import UIKit

struct AccessView: View {
    @StateObject private var model = AccessModel()
    @Environment(\.scenePhase) private var phase
    @State private var choosingFile = false
    @State private var confirmRemove = false
    private let navy = Color(red:0.08,green:0.13,blue:0.20)
    private let timer = Timer.publish(every:1,on:.main,in:.common).autoconnect()
    var body: some View {
        ZStack {
            NavigationStack {
                ScrollView {
                    VStack(alignment:.leading,spacing:20) {
                        HStack(spacing:14) {
                            Image("DTCELogo").resizable().scaledToFit().frame(width:64,height:64).background(.white).clipShape(RoundedRectangle(cornerRadius:14))
                            VStack(alignment:.leading) { Text("OWNER TOOLS").font(.caption2.bold()).tracking(2); Text("Access Admin").font(.title2.bold()); Text("DTCE Tool Kit").foregroundStyle(.secondary).font(.subheadline) }
                            Spacer()
                            Image(systemName:model.unlocked ? "lock.open.fill":"faceid").font(.title2).foregroundStyle(model.unlocked ? .green:navy).accessibilityLabel(model.unlocked ? "Unlocked":"Locked")
                        }
                        card {
                            HStack { Text("1. Signing key").font(.headline); Spacer(); if model.unlocked { Button("Lock") { model.lock() } } }
                            if model.hasSavedKey {
                                Label("Saved in this iPhone Keychain",systemImage:"checkmark.shield").font(.subheadline).foregroundStyle(.secondary)
                                if !model.unlocked { Button { Task { await model.unlockKey() } } label: { Label("Unlock with Face ID",systemImage:"faceid").frame(maxWidth:.infinity) }.buttonStyle(.borderedProminent).disabled(model.busy) }
                            }
                            DisclosureGroup(model.hasSavedKey ? "Import / replace signing key":"Import your mobile key") {
                                VStack(alignment:.leading,spacing:12) {
                                    Text("Use the password-protected .dtcekey exported by Windows Admin. The Windows DAT cannot be imported directly.").font(.footnote).foregroundStyle(.secondary)
                                    Button("Choose mobile key from Files") { choosingFile = true }.buttonStyle(.bordered).disabled(model.busy)
                                    Text(model.fileName).font(.caption).foregroundStyle(.secondary)
                                    SecureField("Export password",text:$model.password).textContentType(.password).textInputAutocapitalization(.never).autocorrectionDisabled().textFieldStyle(.roundedBorder)
                                    Button("Import & protect with Face ID") { Task { await model.importKey() } }.buttonStyle(.borderedProminent).disabled(model.busy || model.password.isEmpty)
                                }.padding(.top,10)
                            }
                            if model.busy { ProgressView("Processing securely on this iPhone…").font(.caption) }
                            Text("Locks when backgrounded and after 5 minutes of inactivity. Face ID enrollment changes require importing the key again.").font(.caption).foregroundStyle(.secondary)
                        }
                        card {
                            Text("2. Request & permissions").font(.headline)
                            TextEditor(text:$model.request).font(.system(.footnote,design:.monospaced)).frame(minHeight:78).padding(8).background(Color(.secondarySystemBackground)).clipShape(RoundedRectangle(cornerRadius:10)).textInputAutocapitalization(.characters).autocorrectionDisabled().accessibilityLabel("User request code")
                            Text("Paste the complete BEAM request from the User app.").font(.caption).foregroundStyle(.secondary)
                            ForEach([(1,"Beam Schedule Formatter"),(2,"Beam Estimator"),(4,"Splice, Development & Hooks")],id:\.0) { app in
                                Toggle(app.1,isOn:Binding(get:{model.allowedApps.contains(app.0)},set:{enabled in if enabled {model.allowedApps.insert(app.0)} else {model.allowedApps.remove(app.0)} })).tint(.teal)
                            }
                            Label("Project Information included",systemImage:"checkmark.circle.fill").foregroundStyle(.teal).font(.caption)
                            Button("Generate 8-hour access") { model.generate() }.buttonStyle(.borderedProminent).frame(maxWidth:.infinity).disabled(!model.unlocked || model.busy || model.allowedApps.isEmpty)
                            Text("Eight hours starts at generation. Keep iPhone Date & Time set automatically.").font(.caption).foregroundStyle(.secondary)
                        }
                        card {
                            Text("3. Send access code").font(.headline)
                            if model.code.isEmpty { Text("Your code will appear here.").font(.footnote).foregroundStyle(.secondary) }
                            else {
                                Text(model.code).font(.system(.caption,design:.monospaced)).textSelection(.enabled).frame(maxWidth:.infinity,alignment:.leading)
                                if let expiry = model.expires { Text("Expires \(expiry.formatted(date:.abbreviated,time:.shortened))").font(.caption).foregroundStyle(.secondary) }
                                HStack { Button { UIPasteboard.general.string = model.code; model.message = "Access code copied."; model.touch() } label: { Label("Copy",systemImage:"doc.on.doc") }.buttonStyle(.bordered)
                                    ShareLink(item:model.code) { Label("Share",systemImage:"square.and.arrow.up") }.buttonStyle(.bordered)
                                }
                            }
                        }
                        Text(model.message).font(.footnote).accessibilityAddTraits(.updatesFrequently)
                        VStack(alignment:.leading,spacing:8) {
                            Text("Key ID: \(model.keyId)").font(.caption.monospaced())
                            Text("No signing server. Key import and access generation work on this device.").font(.caption).foregroundStyle(.secondary)
                            if model.hasSavedKey { Button("Remove saved signing key",role:.destructive) { confirmRemove = true }.font(.footnote) }
                        }.padding(.horizontal,4)
                    }.padding(20)
                }.background(Color(.systemGroupedBackground)).navigationTitle("DTCE Access").navigationBarTitleDisplayMode(.inline)
            }.tint(navy)
            if phase != .active { navy.ignoresSafeArea().overlay { VStack(spacing:14) { Image(systemName:"faceid").font(.system(size:46)); Text("DTCE Access Admin").font(.headline) }.foregroundStyle(.white) } }
        }
        .fileImporter(isPresented:$choosingFile,allowedContentTypes:[.item],allowsMultipleSelection:false) { result in
            switch result { case .success(let urls): if let url=urls.first { model.select(url) }; case .failure(let error): model.message=error.localizedDescription }
        }
        .confirmationDialog("Remove the key from this iPhone? Keep the original encrypted export so you can import it again.",isPresented:$confirmRemove,titleVisibility:.visible) { Button("Remove saved key",role:.destructive) { model.forget() } }
        .onChange(of:phase) { value in if value == .background { model.lock() } }
        .onReceive(timer) { _ in model.checkTimeout() }
        .simultaneousGesture(TapGesture().onEnded { model.touch() })
    }
    private func card<Content:View>(@ViewBuilder content:()->Content)->some View {
        VStack(alignment:.leading,spacing:14,content:content).frame(maxWidth:.infinity,alignment:.leading).padding(20).background(.background,in:RoundedRectangle(cornerRadius:18))
    }
}
