# Validation boundaries

Version 0.1.0 is a Windows x64 preview. Compatibility evidence is version-specific.
Local validation on 2026-10-12: all six Go packages passed `go test ./...`, `go vet ./...` passed, and 23 real-data/frontend integration checks passed.

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

For this preview, the user explicitly chose to preserve currently running Desktop work and defer the live A → B → A acceptance test. It was not run or claimed successful.

No permanent-login, cloud-chat migration, cache preservation or post-compaction context restoration guarantee is made.
