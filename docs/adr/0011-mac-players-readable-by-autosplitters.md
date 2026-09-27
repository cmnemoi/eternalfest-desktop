# 0011 — Mac players re-signed so autosplitters can read them

- Status: accepted
- Date: 2026-09-27
- Supersedes: [0009](0009-velopack-packages-and-macos.md) and [0010](0010-flash-projector-on-macos.md), for the players' signatures

## Context

Speedrunners time contrées with an autosplitter, which reads the player's memory. It works on Linux and Windows, and on macOS with Eternaltwin's Flash player, but not in the Mac app.

Ruffle LLC and Adobe sign their Mac players with the hardened runtime and without `com.apple.security.get-task-allow`. With System Integrity Protection on, macOS refuses `task_for_pid` on them, even as root: `(os/kern) failure`. Eternaltwin's player is signed ad hoc, without the hardened runtime.

The Mac app itself is signed ad hoc and isn't notarized: macOS doesn't trust it more because of the players' signatures.

## Decision

- When packaging the Mac app, re-sign `Ruffle.app` and `Flash Player.app` ad hoc, without the hardened runtime, with their authors' entitlements plus `get-task-allow`. Their nested code, like Ruffle's web extension, keeps its signature.
- The package check requires both players to have no hardened runtime and `get-task-allow`, instead of their authors' Developer ID.

## Consequences

- Autosplitters can read both players' memory on macOS, like on the other OSes.
- The players lose Ruffle LLC's and Adobe's signatures: nothing in the Mac app proves they are their authors' builds anymore, beyond the SHA-256 checked when fetching them.
- A process with enough rights can read their memory, and the projector accepts any library from `DYLD_INSERT_LIBRARIES`, not only `libprojector-window.dylib`.
- Ruffle keeps its sandbox entitlement, now under an ad hoc signature.
