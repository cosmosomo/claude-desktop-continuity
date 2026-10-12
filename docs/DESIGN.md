# Desktop product direction

This remains a preview, not a mature installed desktop product.

## Primary workflow

Open `Continuity.exe`, inspect the target account, review the automatically selected active sessions, inspect conversation history, then choose either open-in-current-account or switch-and-continue. Switching visibly restarts Claude Desktop; ongoing tasks should be finished first.

## Workspace layout

- Left: saved-account email, current identity/mode, the two continuation actions and account-manager entry.
- Center: project/search filters and session table. Ordinary dialogue activity determines the default 12-hour selection; file metadata updates do not.
- Right: read-only conversation preview. The selected transcript title and bounded history evidence remain visible.
- Bottom: scanned/displayed/selected counts. Filters retain choices; refresh reapplies defaults.

The visual direction is dark pine navigation, warm paper workspace and green selection/action states. Spacing, headings, row height and contrast are shared across the native controls. Secondary metadata stays in existing internal columns/tooltips rather than dominating the list.

## Distribution

The portable ZIP has a root `Continuity.exe`. First-run configuration uses the same existing Setup flow with explicit errors; subsequent runs open the main GUI. Downloading does not require Go, Python or a terminal. Source builds still need Go and the Windows .NET Framework compiler.

## Evidence and gaps

Screenshots are rendered from the actual executable and real local data. Public exports explicitly hide account emails, transcript titles, project paths and conversation text. No generated or sample conversation data is used. Unredacted captures remain local.

25 real-data/control integration checks passed after the layout change. Rendering is separately inspected; a successful compile is not visual acceptance. Existing saved-account switching was already tested with user-visible restarts and replies before this layout revision; it is not automatically repeated during design work.

Account creation and backup still open the upstream tray manager. An installer, signed executables, integrated account enrollment, broader DPI testing and accessibility review remain unfinished. No maturity or universal compatibility guarantee is made.
