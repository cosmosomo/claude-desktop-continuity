# Attribution

Account engine and native tray: OpenScz/claude-desktop-switcher (formerly TinoA/claude-desktop-switcher), MIT.
Upstream base: 5b2c0cc778d3aaa9ac741187f29b1fda0a3b2b4b.
The inherited Go module path is retained for internal import compatibility; this distribution is a separate project.
Upstream documentation is preserved as UPSTREAM-README.md and UPSTREAM-AGENTS.md.

Changes: current Windows MSIX Roaming data path, credential-free account JSON command, Windows session catalog/preview and integrated switch-and-continue UI, portable packaging and local integration checks.

Go dependencies are locked in go.mod/go.sum and retain their individual licenses. The Windows distribution includes Go dependency license notices collected from the actual module cache at packaging time.

Copyright (c) 2026 Claude Desktop Continuity contributors. New code is MIT-licensed; the upstream copyright notice in LICENSE remains intact.
