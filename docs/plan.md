# Eternalfest Desktop — Plan

> Unofficial. Eternalfest Desktop is a personal project, not affiliated with the Eternalfest or Eternaltwin teams.

## Vision

Play any public Eternalfest *contrée* (custom Hammerfest game) offline, with every mode and option unlocked, by double-clicking an app. No terminal, no cloned repositories, no local build toolchain, no account.

Today, playing a contrée offline means what `hammerfest-tas` does: a Debian distrobox, Haxe 3.1.3, swfmill, a private npm registry, `eternaldev` in one terminal and `hftas run` in another. Eternalfest Desktop replaces all of it with one self-contained app.

## Audience

Non-technical players on Windows and Linux. Speedrunners, TASers and contrée creators aren't the primary audience: their needs can come later as features, but they don't drive the UX.

## Product principles

- **Offline only, just for fun.** Nothing is ever sent to eternalfest.net: no account, no runs, no scores. See [ADR 0001](adr/0001-offline-only.md).
- **Download once, play forever.** The network is only needed to browse the catalog and to download a contrée the first time. See [ADR 0004](adr/0004-download-contrees-never-redistribute.md).
- **Full options.** All item families, all modes, and all options that progression would otherwise lock: by default, the player gets what every quest of the contrée unlocks, and can choose to start as a new player instead. See [ADR 0007](adr/0007-bundle-eternalfest-quests.md).
- **Plug and play.** Self-contained archives bundle the .NET runtime, a pinned Flash projector (Linux) and Ruffle, the Eternalfest loader and the base game engine.
- **Predictable.** Nothing changes under the player's feet: pinned Flash Player and Ruffle, pinned loader, and contrée updates are offered, never forced.

## Architecture at a glance

```text
┌──────────────────────── Eternalfest Desktop (one process) ────────────────────────┐
│  Avalonia UI (MVVM) ──► use cases ──► ports                                       │
│                                        ├─ game catalog  ──► eternalfest.net API (read-only, anonymous)
│                                        ├─ game store    ──► local cache (OS data dir)
│                                        ├─ offline backend ─► Kestrel on 127.0.0.1:<random>
│                                        └─ flash player  ──► Adobe's Flash projector, or Ruffle desktop (child process)
└───────────────────────────────────────────────────────────────────────────────────┘
Player ──loads──► http://127.0.0.1:<port>/assets/loader.swf ──► same-origin API + blobs served from cache
```

See [ADR 0008](adr/0008-flash-projector-first-ruffle-fallback.md), [ADR 0002](adr/0002-ruffle-desktop-child-process.md), [ADR 0003](adr/0003-embedded-offline-backend.md), [ADR 0005](adr/0005-bundle-loader-and-base-engine.md) and [ADR 0006](adr/0006-light-hexagonal-architecture.md).

## Tech stack

| Concern | Choice |
|---|---|
| Runtime | .NET 10 (LTS), self-contained publish |
| UI | Avalonia UI, MVVM with CommunityToolkit.Mvvm |
| Offline backend | ASP.NET Core Kestrel minimal API, in process |
| Flash | Adobe's Flash projector 32.0.0.465 (Linux), Ruffle desktop elsewhere, pinned and bundled per OS |
| Logs | Serilog, local file only, no telemetry |
| Tests | xUnit, TDD, business DSL for acceptance tests |
| UI languages | English and French (`.resx`), Spanish later |
| Hosting | GitHub (sources, Actions, Releases) |
| License | GPL-3.0 |

## Delivery

**Status (2026-09-26):** lots 0 to 13 are implemented and covered by automated tests. The [manual checklist](manual-checklist.md) hasn't been run yet: in particular, starting a game from the loader menu and playing levels hasn't been observed, and nothing has run on Windows or in GitHub Actions.

Small lots, each ending with something that can be verified. Each lot links to the spec(s) it implements. A lot is done when its acceptance criteria are covered by tests tagged with the matching `@spec` IDs, or by the manual checklist when rendering is involved.

### Milestone 1 — Prove the chain (no UI)

The biggest technical risk is "Ruffle + Eternalfest loader + a C# fake backend". It gets retired first.

| # | Lot | Done when | Specs |
|---|---|---|---|
| 0 | Repository bootstrap | Empty solution builds and tests run on Linux and Windows in GitHub Actions; `LICENSE` (GPL-3.0) and README with the "unofficial" notice | — |
| 1 | Read the catalog | Public contrées and one contrée's details are read from the API; contract tests against recorded responses | [game-catalog](specs/game-catalog.md) |
| 2 | Download a contrée | Every blob of a build is downloaded to the cache with its digest checked; a second download fetches nothing | [game-store](specs/game-store.md) |
| 3 | Offline backend: static routes | Loader, base engine and cached blobs are served on one local origin | [offline-backend](specs/offline-backend.md) |
| 4 | Offline backend: game and runs | Game details with full options, run start and discarded results; recorded loader requests replay green | [offline-backend](specs/offline-backend.md) |
| 5 | Launch Ruffle | Ruffle arguments and FlashVars are built from a run; a child process is started and awaited | [play-game](specs/play-game.md) |
| 6 | Walking skeleton | A console command downloads and plays `hammerfest-deluxe` end to end (manual checklist) | [play-game](specs/play-game.md) |

### Milestone 2 — Minimal app

| # | Lot | Done when | Specs |
|---|---|---|---|
| 7 | Minimal window | An Avalonia window lists the catalog and plays a contrée with default mode and options | [launcher-ui](specs/launcher-ui.md) |
| 8 | Contrée page | Mode, options, volume, locale and fullscreen can be chosen before playing | [launcher-ui](specs/launcher-ui.md) |
| 9 | Library comfort | Search, "downloaded" badge, download progress, "Play again" | [launcher-ui](specs/launcher-ui.md) |
| 10 | Offline catalog and updates | Library works without network; "Update available" shown on outdated contrées | [game-catalog](specs/game-catalog.md), [game-store](specs/game-store.md) |
| 11 | Settings | UI language, cache folder, clear cache, open logs folder | [launcher-ui](specs/launcher-ui.md) |

### Milestone 3 — First release

| # | Lot | Done when | Specs |
|---|---|---|---|
| 12 | Packaging v1 | Merging the release pull request publishes `win-x64.zip` and `linux-x64.tar.gz` on GitHub Releases, runnable on a clean machine | [packaging](specs/packaging.md) |

### Milestone 4 — Complete game

| # | Lot | Done when | Specs |
|---|---|---|---|
| 13 | Player profile | The complete profile (default) unlocks what eternalfest.net's quests unlock, checked against a recorded session; "New player" keeps the published contrée; the profile is picked on the contrée page and with `--new-player` | [player-profile](specs/player-profile.md), [offline-backend](specs/offline-backend.md), [launcher-ui](specs/launcher-ui.md) |

### Later (unordered, not committed)

- Auto-update with Velopack (AppImage on Linux, installer on Windows).
- macOS arm64, with the Apple signing and notarization question.
- Stores: winget, Flathub.
- Import a local contrée build, for creators.
- Local score history.
- Spanish UI.

## Decision records

| # | Decision |
|---|---|
| [0001](adr/0001-offline-only.md) | Offline only |
| [0002](adr/0002-ruffle-desktop-child-process.md) | Ruffle desktop as a pinned child process |
| [0003](adr/0003-embedded-offline-backend.md) | Embedded Kestrel offline backend on a single local origin |
| [0004](adr/0004-download-contrees-never-redistribute.md) | Download contrées from the public API, never redistribute them |
| [0005](adr/0005-bundle-loader-and-base-engine.md) | Bundle the loader and base engine |
| [0006](adr/0006-light-hexagonal-architecture.md) | Light hexagonal architecture and naming conventions |
| [0007](adr/0007-bundle-eternalfest-quests.md) | Bundle the quests eternalfest.net hardcodes |
| [0008](adr/0008-flash-projector-first-ruffle-fallback.md) | Adobe's Flash projector first, Ruffle as a fallback |

## Open risks

- **Ruffle rendering.** Where contrées play in Ruffle, known issues with Eternalfest content (quality stuck on low, halos, fonts) are documented in `eternalfest/project-phoenix/RUFFLE.md`. We accept whatever the pinned Ruffle renders.
- **Flash Player redistribution.** Adobe never allowed it; Eternaltwin does it anyway, and so do we (ADR 0008). Adobe's download URL may also disappear: the pinned archive is checked by SHA-256, and a copy should be kept.
- **Wayland only.** The Linux projector needs X11 or XWayland.
- **Contrée licenses.** Contrées carry `"license": "UNLICENSED"` in their `package.json`, and no LICENSE file. We never redistribute them (ADR 0004), and asking the Eternalfest team for confirmation is still worth doing.
- **Archive size.** Self-contained archives weigh about 68 MB, mostly .NET, ASP.NET Core and Avalonia untrimmed. Trimming is possible later, with care for reflection in minimal APIs and JSON.
- **API stability.** The public API isn't a documented contract for third parties. A breaking change on eternalfest.net breaks downloads, but never already downloaded contrées.
