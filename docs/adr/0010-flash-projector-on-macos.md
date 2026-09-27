# 0010 — Adobe's Flash projector on macOS, under Rosetta 2

- Status: accepted; the projector's signature superseded by [0011](0011-mac-players-readable-by-autosplitters.md)
- Date: 2026-09-27
- Supersedes: [0009](0009-velopack-packages-and-macos.md), for the player on macOS

## Context

ADR 0009 shipped the Mac app playing in Ruffle, since Adobe's Mac projector is Intel only. Players want the same player on every OS: Ruffle breaks some contrées (ADR 0008).

A spike on an Apple Silicon Mac showed:

- Adobe's projector 32.0.0.465 for macOS, `flashplayer_32_sa.dmg`, holds `Flash Player.app`, x86_64 only, signed with Adobe's Developer ID, notarized, and accepted by Gatekeeper.
- Under Rosetta 2, it plays *Les Cavernes de Hammerfest* from the offline backend, started on the loader URL like on Linux.
- It has the hardened runtime, with the entitlements `allow-dyld-environment-variables` and `disable-library-validation`: it loads a library of ours from `DYLD_INSERT_LIBRARIES`.
- It sizes its window, `FP_FPWindow`, to the stage with `-[NSWindow setFrame:display:]`, in points. macOS's menu bar is global: the window has none to hide.
- Its bundle has no symbolic links: copying it keeps Adobe's signature valid.
- Apple announced that macOS 27 is the last version with Rosetta 2 for all apps.

## Decision

- Ship Adobe's Mac projector in the Mac app, in `flash-player/Flash Player.app`, copied untouched from Adobe's disk image, pinned in `eng/flash-player.json` like the others.
- Shape its window from inside with `libprojector-window.dylib`, built for x86_64 from `native/projector-window/projector-window.m`, loaded with `DYLD_INSERT_LIBRARIES`: it replaces the size the projector asks for its window with the game height the launcher chose, in points, and makes the window fullscreen when asked, like the Linux library. It also quits the projector when the player closes its window: like any Mac app, the projector keeps running without windows, and the launcher would wait for it forever.
- Keep Ruffle as the fallback: when macOS can't run the projector (no Rosetta 2), the launcher plays in Ruffle.

## Consequences

- Contrées play in the same Flash Player on every OS the app ships for.
- The Mac app grows by about 25 MB, and still ships Ruffle.
- Players need Rosetta 2. The launcher starts the projector directly, so macOS may not offer to install it: without it, contrées play in Ruffle, and the log says why. The README gives the command that installs it.
- When Apple removes Rosetta 2 for apps like this one, the Mac app falls back to Ruffle with no change.
- The window shaping depends on the projector's AppKit internals, which never change now, like on Linux. macOS's zoom also resizes through `setFrame:display:` and gets the game's height.
- The Mac projector and its library are fetched on a Mac only: they need `hdiutil`, `ditto`, `clang` and `codesign`.
