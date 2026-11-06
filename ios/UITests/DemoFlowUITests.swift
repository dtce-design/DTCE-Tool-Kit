import XCTest

final class DemoFlowUITests: XCTestCase {
    @MainActor func attach(_ name: String, _ app: XCUIApplication) {
        let image = XCTAttachment(screenshot:app.screenshot())
        image.name = name; image.lifetime = .keepAlways; add(image)
    }
    @MainActor func reveal(_ element: XCUIElement, in app: XCUIApplication, up: Bool = true) {
        for _ in 0..<10 {
            if element.isHittable { return }
            if up { app.swipeUp() } else { app.swipeDown() }
        }
    }
    @MainActor func testDemoWithoutSigningKey() throws {
        continueAfterFailure = false
        let app = XCUIApplication(); app.launch()
        attach("DTCE-01-Owner-mode",app)
        let start = app.buttons["start-demo"]
        XCTAssertTrue(start.waitForExistence(timeout:15))
        reveal(start,in:app); start.tap()
        XCTAssertTrue(app.descendants(matching:.any).matching(identifier:"demo-banner").firstMatch.waitForExistence(timeout:5))
        attach("DTCE-02-Demo-mode",app)
        let generate = app.buttons["generate-code"]
        reveal(generate,in:app); XCTAssertTrue(generate.isEnabled); generate.tap()
        let output = app.staticTexts["generated-code"]
        reveal(output,in:app)
        XCTAssertTrue(output.waitForExistence(timeout:5))
        XCTAssertTrue(output.label.hasPrefix("DEMO.BA4."))
        attach("DTCE-03-Demo-output",app)
        let type = app.segmentedControls["code-type"]
        reveal(type,in:app,up:false); type.buttons["Certification"].tap()
        reveal(generate,in:app); generate.tap()
        reveal(output,in:app)
        XCTAssertTrue(output.label.hasPrefix("DEMO.BC1."))
        attach("DTCE-04-Demo-certification",app)
        let exit = app.buttons["exit-demo"]
        reveal(exit,in:app,up:false); exit.tap()
        XCTAssertTrue(app.buttons["start-demo"].waitForExistence(timeout:5))
        XCTAssertFalse(generate.isEnabled)
        reveal(start,in:app,up:false); start.tap()
        app.terminate(); app.launch()
        XCTAssertTrue(app.buttons["start-demo"].waitForExistence(timeout:10))
        XCTAssertFalse(app.descendants(matching:.any).matching(identifier:"demo-banner").firstMatch.exists)
    }
}
