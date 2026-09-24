# 0002 — Ruffle desktop as a pinned child process

- Status: accepted
- Date: 2026-09-24

## Context

Eternalfest contrées are Flash content. The options were:

1. Ruffle desktop (a Rust binary) started as a separate process.
2. Ruffle web (WASM) inside an embedded WebView.
3. Ruffle linked natively through Rust/C# FFI.

`hammerfest-tas` already runs the Eternalfest loader successfully with Ruffle desktop 0.6.0, using `--base`, `--spoof-url`, `--referer`, `--dummy-external-interface` and `-P` FlashVars (`hammerfest-tas/src/hftas/ruffle.py`).

Ruffle is dual-licensed MIT/Apache-2.0, so it can be redistributed.

## Decision

- Run Ruffle desktop as a child process, in its own window.
- Pin one Ruffle version (0.6.0 to start, the one `hammerfest-tas` uses) and bundle its binary for each OS in the app archive.
- Upgrading Ruffle is a deliberate change, tested against the manual checklist, and shipped with a new app release.
- Only one game runs at a time.

## Consequences

- A proven path, with no WebView quirks (WebKitGTK on Linux) and no FFI.
- The game shows in a separate window, not inside the launcher.
- Archives grow by about 20 to 30 MB.
- Linux users need Ruffle's runtime libraries (ALSA, udev, OpenGL or Vulkan), which are present on typical desktops.
- Ruffle rendering bugs are accepted as they are, until a pinned upgrade fixes them.
