# Validation boundaries

Version 0.2.1 is a Windows x64 preview. Compatibility evidence is version-specific.
Local validation on 2026-10-12: all six Go packages passed `go test ./...`, `go vet ./...` passed, and 23 real-data/frontend integration checks passed.
After the 12-hour default-selection update, all 25 local real-data/frontend checks passed, including recent-session eligibility and manual cancellation; Go tests and vet passed again.

Validation consists of:

- `go mod verify` against the locked dependency graph.
- Upstream Go tests across all packages (profile persistence, recovery, locks and platform handling).
- Compilation of the combined WinForms application and bundled Go CLI/tray.
- Local real-data integration tests for account JSON, transcript discovery, argument transport, search, checkbox preservation and row-specific conversation previews.
- SHA-256 verification that read-only local tests do not change their inspected original transcript.

No synthetic backend substitutes, paid inference or quota calls are introduced by the integration tests. Upstream's isolated test fixtures remain its own test suite.

Private transcripts, account profiles, local configuration, raw logs and personal test results are excluded from Git and release packaging.

The hidden-window test harness creates native child-control handles before asynchronous preview assertions, matching the visible application's initialization order. Byte-preservation checks use a real older transcript; an actively running transcript may legitimately receive new metadata from Desktop during tests.

The native Desktop handoff previously succeeded with Desktop 2.31226.1.0 for registered and newly adopted Code transcripts. This does not promise unchanged behavior after Desktop updates. A full live A → B → A cycle through the newly combined UI is a separate acceptance test and must be recorded explicitly; compilation/read-only tests alone do not establish it.

At initial publication, the user chose to preserve running Desktop work and defer live acceptance. Subsequently, on 2026-10-12 the user explicitly authorized it. The live A → B → A account restore and native session-registration cycle passed, and the original account was restored with the selected session reopened. The test used the most recently modified registered real transcript, verified a different initialized account namespace, verified registration in both namespaces, and compared recent ordinary conversation content before and after adoption.

The live harness invokes the same `Continuity.SwitchAndOpen` implementation used by the main window. It does not verify Desktop's visible history rendering or send inference requests; those remain separate acceptance checks. The content comparison covers the bounded recent preview, not a byte-by-byte audit of the entire history. Private logs and transcript identifiers are excluded from public evidence.

Subsequent user-visible acceptance on 2026-10-12: the initial fast round trip was insufficient for the user's observation, so another one-way switch was performed and left at the second account. Process IDs from the old Desktop were replaced by a freshly launched Desktop. The user explicitly confirmed the second account, prior history and an actual test reply were normal. After the user authorized returning, Desktop was restarted to the original account and the user confirmed that the newly added test dialogue was present there. The user operated the visible conversation; no Computer Use automation was used. These confirmations establish the tested conversation flow, not an audit of every historical message.

Version 0.1.1 adds default selection based on actual ordinary dialogue timestamps within the configured 12-hour window, plus saved-email account labels. Launch and explicit refresh apply the default selection; ordinary search and project filtering preserve manual changes. Local integration checks exercise selection eligibility and manual cancellation using real transcripts.

Desktop workspace revision: three-column native layout and a root double-click launcher added. The same 25 real-data/frontend integration checks passed after the layout change. `RenderWorkspace` renders actual native controls and uses RichEdit's own print interface for conversation text; public exports redact sensitive display fields. This is application rendering, not capture of the user's desktop. Launcher compilation and actual launch with existing local config are separate checks; live account switching is not repeated for visual work.

Portable first-run acceptance: a real freshly packaged directory with no configuration initially exposed a Windows PowerShell 5.1 UTF-8-without-BOM issue in the Chinese shortcut string. Setup.ps1 is now explicitly UTF-8 with BOM; launcher failures record an event and show a diagnostic. After fixing it, another fresh package directory successfully generated config, created the Chinese desktop shortcut, displayed the native main window and scanned actual local sessions. The test restored the previous desktop shortcut and normally closed only its own test application; Claude Desktop was not restarted.

`Build.ps1` compiles `dist/LiveAcceptance.exe` but never runs it automatically. After completing or stopping Desktop tasks and explicitly consenting to two Desktop restarts, run it with the absolute local configuration path. It selects the latest registered real session, switches to another usable saved account, verifies adoption, and returns to the original account. A cycle error is logged and a return is attempted before reporting failure. This harness is not included in the portable release ZIP.

No permanent-login, cloud-chat migration, cache preservation or post-compaction context restoration guarantee is made.
