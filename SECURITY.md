# Security and privacy

This app operates on local Claude Desktop authentication snapshots. Windows cookie values remain in Chromium's encrypted form; the new frontend never reads cookie/OAuth values or sends model requests.

The account JSON protocol contains only saved names, local profile health and the matching current profile name. Local identity and profile health do not independently prove that Anthropic still accepts a login.

Do not share `.claude-swap`, Cookies, account-state.json, Desktop config, transcript files or generated config.local.json. Diagnostic logs can reveal local paths, account UUIDs and selected profile names. Redact them before sharing.

No automatic cross-account bulk sync is enabled. The user selects specific Code transcripts. Original transcript bytes are not edited by the integration, and no conversation bodies are uploaded by this app. Continuing a selected transcript in Desktop will send its effective context to the provider selected in Desktop.

Use the repository's private security reporting channel if available. Otherwise open an issue containing only the defect description and a contact request, never credentials or private conversation logs.

Account restoration is provided by the upstream MIT engine and retains its locking, private profile directories and recovery behavior. A Desktop update may change authentication or native handoff behavior. Expired authentication requires a normal new sign-in.

The upstream tray may check its upstream GitHub release endpoint for updates. Windows account switching does not use the macOS account-label network helper.
