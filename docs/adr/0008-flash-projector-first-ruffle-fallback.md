# 0008 — Adobe's Flash projector first, Ruffle as a fallback

- Status: accepted
- Date: 2026-09-26
- Supersedes: [0002](0002-ruffle-desktop-child-process.md), for the choice of the player

## Context

The app exists to play contrées. Ruffle breaks some of them and is sometimes slow on others, while the official Eternaltwin app, which runs Adobe's Flash Player 32 as a PPAPI plugin in an old Electron, plays them all.

The options were:

1. The PPAPI plugin in a Chromium old enough to load it (Electron ≤ 11, CEF): about 150 to 180 MB, a whole browser to ship.
2. Adobe's standalone Flash Player 32.0.0.465, the "projector": about 15 MB, one executable per OS, still downloadable from Adobe.

A spike showed the projector plays *Les Cavernes de Hammerfest* from the offline backend:

- The FlashVars go in the loader URL's query string: the loader reads them from `_root` like an `<embed>`'s `flashvars`, and its origin from `_url`.
- `ExternalInterface` isn't needed: the loader only uses it for its optional swf socket.
- The loader opens `/runs/{run id}` with `getURL(…, "_self")` at the end of a game, which the projector ignores.
- The `run` FlashVar makes the URL about 31 KB long, beyond Kestrel's 8 KB default limit.
- The projector sizes its window to the SWF stage, with a menu bar and, on Linux, a URL bar holding the whole query string. Rewriting the SWF header enlarges the stage but not the game; enlarging the window scales the game.
- On Linux, it needs GTK 2 and NSS, which recent distributions no longer install.

Adobe never allowed redistributing Flash Player. Eternaltwin redistributes it anyway, and this project accepts the same risk.

## Decision

- Play contrées in Adobe's Flash projector 32.0.0.465, started as a child process on the loader URL with the FlashVars in its query string, one game at a time.
- Pin the projector for each OS like Ruffle (`eng/flash-player.json`) and bundle it in `flash-player/` next to the app. On Linux, bundle GTK 2 and NSS, taken from Debian 12's binary packages: they are plain shared libraries, which any distribution with glibc 2.34 or newer and GTK 3's usual dependencies (GLib, Pango, Cairo, ATK, GdkPixbuf, X11) runs, and Debian keeps them on snapshot.debian.org for good.
- On Linux, shape the window from inside the projector with `libprojector-window.so`, a small library of ours, loaded both with `LD_PRELOAD` and as a GTK module: it hides the menu and URL bars, replaces the size the projector asks for its window with the game height the launcher chose (keeping the stage's proportions), and makes the window fullscreen when asked.
- On Windows, shape the window from the launcher with Win32 calls, for the first seconds after it opens: remove the menu bar, and size it in the projector's unscaled pixels, assuming it ignores the screen's scaling like the old app it is (to check on a scaled Windows screen). The size comes from the loader's stage, read from its SWF header.
- Raise the offline backend's request line limit so the loader URL fits.
- Keep Ruffle, pinned as before (ADR 0002), as the fallback when the projector isn't shipped for the OS.

## Consequences

- Contrées play in the same Flash Player as on the Eternalfest website and in the Eternaltwin app.
- Archives grow by about 15 MB on Windows and 25 MB on Linux, and still ship Ruffle.
- A native library is built from `native/projector-window/` with a C compiler, when packaging for Linux.
- The Windows window shaping races the projector for a few seconds: it was checked under Wine, not yet on Windows.
- The game aligns its stage to the left: in a fullscreen window wider than the stage, the game sits on the left of the screen.
- The window shaping depends on the projector's GTK 2 internals, which never change now: the pinned binary is the last one Adobe will ever publish.
- Players with neither the projector nor Ruffle for their OS can't play, as before.
