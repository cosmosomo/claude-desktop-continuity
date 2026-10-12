# Claude Desktop Continuity

Windows companion for the original Claude Desktop. Preserve upstream MIT notices.
Go owns account snapshots; app/*.cs owns transcript discovery, preview and native handoff.
UPSTREAM-AGENTS.md is historical reference, not this fork's execution policy.

- Build and test before publishing. Use scripts/continuity/Build.ps1.
- Use apply_patch. Keep user configuration in ignored config.local.json.
- Never publish profiles, cookies, OAuth values, transcripts, local logs or private test outputs.
- Never automatically merge/rewrite JSONL or Desktop indexes. Native handoff depends on Desktop compatibility.
- Validate account, provider and selected transcripts before switching; keep upstream recovery and locks intact.
- Local integration tests read real files without credential changes or model requests.
- Live switching tests need explicit authorization and completed Desktop work.
- Commit conventionally, preserve attribution and document compatibility limits.
