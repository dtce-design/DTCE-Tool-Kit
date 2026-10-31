import SwiftUI
import UniformTypeIdentifiers
import UIKit

struct AccessView: View {
    @StateObject private var model = AccessModel()
    @Environment(\.scenePhase) private var phase
    @Environment(\.colorScheme) private var appearance
    @Environment(\.colorSchemeContrast) private var contrast
    @State private var choosingFile = false
    @State private var confirmRemove = false
    private let navy = Color(red:0.08,green:0.13,blue:0.20)
    private var accent: Color { appearance == .dark ? Color(red:0.42,green:0.86,blue:0.87) : Color(red:0.00,green:0.37,blue:0.42) }
    private var primaryButton: Color { appearance == .dark ? Color(red:0.06,green:0.38,blue:0.43) : Color(red:0.03,green:0.28,blue:0.34) }
    private var outline: Color { Color.primary.opacity(contrast == .increased ? 0.45 : appearance == .dark ? 0.22 : 0.14) }
    private var inputSurface: Color { Color(uiColor:.tertiarySystemGroupedBackground) }
    private let timer = Timer.publish(every:1,on:.main,in:.common).autoconnect()
    var body: some View {
        ZStack {
            NavigationStack {
                ScrollView {
                    VStack(alignment:.leading,spacing:20) {
                        HStack(spacing:14) {
                            Image("DTCELogo").resizable().scaledToFit().frame(width:56,height:56).padding(6).background(Color.white).clipShape(RoundedRectangle(cornerRadius:14)).overlay(RoundedRectangle(cornerRadius:14).strokeBorder(outline,lineWidth:1))
                            VStack(alignment:.leading) { Text("OWNER TOOLS").font(.caption2.bold()).tracking(2); Text("Access Admin").font(.title2.bold()); Text("DTCE Tool Kit").foregroundStyle(.secondary).font(.subheadline) }
                            Spacer()
                            Image(systemName:model.unlocked ? "lock.open.fill":"faceid").font(.title2).foregroundStyle(model.unlocked ? Color.green:accent).accessibilityLabel(model.unlocked ? "Unlocked":"Locked")
                        }
                        card {
                            HStack { Text("1. Signing key").font(.headline); Spacer(); if model.unlocked { Button("Lock") { model.lock() } } }
                            if model.hasSavedKey {
                                Label("Saved in this iPhone Keychain",systemImage:"checkmark.shield").font(.subheadline).foregroundStyle(.secondary)
                                if !model.unlocked { Button { Task { await model.unlockKey() } } label: { Label("Unlock with Face ID",systemImage:"faceid").frame(maxWidth:.infinity) }.buttonStyle(.borderedProminent).tint(primaryButton).disabled(model.busy) }
                            }
                            DisclosureGroup(model.hasSavedKey ? "Import / replace signing key":"Import your mobile key") {
                                VStack(alignment:.leading,spacing:12) {
                                    Text("Use the password-protected .dtcekey exported by Windows Admin. The Windows DAT cannot be imported directly.").font(.footnote).foregroundStyle(.secondary)
                                    Button("Choose mobile key from Files") { choosingFile = true }.buttonStyle(.bordered).disabled(model.busy)
                                    Text(model.fileName).font(.caption).foregroundStyle(.secondary)
                                    SecureField("Export password",text:$model.password).textContentType(.password).textInputAutocapitalization(.never).autocorrectionDisabled().textFieldStyle(.roundedBorder)
                                    Button("Import & protect with Face ID") { Task { await model.importKey() } }.buttonStyle(.borderedProminent).tint(primaryButton).disabled(model.busy || model.password.isEmpty)
                                }.padding(.top,10)
                            }
                            if model.busy { ProgressView("Processing securely on this iPhone…").font(.caption) }
                            Text("Locks when backgrounded and after 5 minutes of inactivity. Face ID enrollment changes require importing the key again.").font(.caption).foregroundStyle(.secondary)
                        }
                        card {
                            Text("2. Access type & permissions").font(.headline)
                            Picker("Code type",selection:$model.certification) {
                                Text("Normal access").tag(false)
                                Text("Certification").tag(true)
                            }.pickerStyle(.segmented)
                            if model.certification {
                                TextField("Exact User app version",text:$model.userVersion).textInputAutocapitalization(.never).autocorrectionDisabled().textFieldStyle(.roundedBorder).accessibilityLabel("Exact User app version")
                                Text("30 days. No request needed. All apps included. Anyone holding this code can use the matching version until expiry. Share only in Notes for certification.").font(.footnote).foregroundStyle(.secondary)
                            } else {
                            ZStack(alignment:.topLeading) {
                                TextEditor(text:$model.request).font(.system(.footnote,design:.monospaced)).foregroundStyle(.primary).scrollContentBackground(.hidden).frame(minHeight:100).padding(8).textInputAutocapitalization(.characters).autocorrectionDisabled().accessibilityLabel("User request code")
                                if model.request.isEmpty { Text("Paste the BEAM request code here").font(.footnote.monospaced()).foregroundStyle(.secondary).padding(.horizontal,13).padding(.top,16).allowsHitTesting(false) }
                            }.background(inputSurface,in:RoundedRectangle(cornerRadius:10)).overlay(RoundedRectangle(cornerRadius:10).strokeBorder(outline,lineWidth:1))
                            Text("Paste the complete BEAM request from the User app.").font(.caption).foregroundStyle(.secondary)
                            ForEach([(1,"Beam / Slab Schedule Formatter"),(2,"Program of Works / Estimators"),(4,"Splice, Development & Hooks")],id:\.0) { app in
                                Toggle(app.1,isOn:Binding(get:{model.allowedApps.contains(app.0)},set:{enabled in if enabled {model.allowedApps.insert(app.0)} else {model.allowedApps.remove(app.0)} })).tint(accent)
                            }
                            Label("Project Information included",systemImage:"checkmark.circle.fill").foregroundStyle(accent).font(.caption)
                            }
                            Button(model.certification ? "Generate certification code":"Generate 8-hour access") { model.generate() }.buttonStyle(.borderedProminent).tint(primaryButton).frame(maxWidth:.infinity).disabled(!model.unlocked || model.busy || (!model.certification && model.allowedApps.isEmpty))
                            Text(model.certification ? "Requires updated User v1.4.27 or newer, matching the version above. Thirty days starts at generation." : "Eight hours starts at generation. Keep iPhone Date & Time set automatically.").font(.caption).foregroundStyle(.secondary)
                        }
                        card {
                            Text("3. Send access code").font(.headline)
                            if model.code.isEmpty { Text("Your code will appear here.").font(.footnote).foregroundStyle(.secondary) }
                            else {
                                Text(model.code).font(.system(.caption,design:.monospaced)).foregroundStyle(.primary).textSelection(.enabled).fixedSize(horizontal:false,vertical:true).frame(maxWidth:.infinity,alignment:.leading).padding(12).background(inputSurface,in:RoundedRectangle(cornerRadius:10)).overlay(RoundedRectangle(cornerRadius:10).strokeBorder(outline,lineWidth:1))
                                if let expiry = model.expires { Text("Expires \(expiry.formatted(date:.abbreviated,time:.shortened))").font(.caption).foregroundStyle(.secondary) }
                                HStack { Button { UIPasteboard.general.string = model.code; model.message = "Access code copied."; model.touch() } label: { Label("Copy",systemImage:"doc.on.doc") }.buttonStyle(.bordered)
                                    ShareLink(item:model.code) { Label("Share",systemImage:"square.and.arrow.up") }.buttonStyle(.bordered)
                                }
                            }
                        }
                        Text(model.message).font(.footnote).foregroundStyle(.primary).frame(maxWidth:.infinity,alignment:.leading).padding(14).background(Color(uiColor:.secondarySystemGroupedBackground),in:RoundedRectangle(cornerRadius:12)).overlay(RoundedRectangle(cornerRadius:12).strokeBorder(outline,lineWidth:1)).accessibilityAddTraits(.updatesFrequently)
                        VStack(alignment:.leading,spacing:8) {
                            Text("Key ID: \(model.keyId)").font(.caption.monospaced())
                            Text("No signing server. Key import and access generation work on this device.").font(.caption).foregroundStyle(.secondary)
                            if model.hasSavedKey { Button("Remove saved signing key",role:.destructive) { confirmRemove = true }.font(.footnote) }
                        }.padding(.horizontal,4)
                    }.padding(20)
                }.background(Color(.systemGroupedBackground)).navigationTitle("DTCE Access").navigationBarTitleDisplayMode(.inline)
            }.tint(accent)
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
        VStack(alignment:.leading,spacing:14,content:content).frame(maxWidth:.infinity,alignment:.leading).padding(20).foregroundStyle(.primary).background(Color(uiColor:.secondarySystemGroupedBackground),in:RoundedRectangle(cornerRadius:18)).overlay(RoundedRectangle(cornerRadius:18).strokeBorder(outline,lineWidth:1))
    }
}

#if DEBUG
struct AccessView_Previews: PreviewProvider {
    static var previews: some View {
        Group {
            AccessView().preferredColorScheme(.light).previewDisplayName("Light appearance")
            AccessView().preferredColorScheme(.dark).previewDisplayName("Dark appearance")
        }
    }
}
#endif
