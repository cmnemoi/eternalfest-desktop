# Bundled Flash files

These files are served to Ruffle by the offline backend (see [ADR 0005](../../docs/adr/0005-bundle-loader-and-base-engine.md)). They are pinned: upgrading one is a deliberate change, tested with the manual checklist.

| File | Package | Version | License | Sources | SHA-256 |
|---|---|---|---|---|---|
| `loader.swf` | `@eternalfest/loader` (`build/lib/loader.flash8.swf`) | 5.1.2 | [AGPL-3.0-or-later](licenses/eternalfest-loader-AGPL-3.0.txt) | https://gitlab.com/eternalfest/loader | `53ea633d0709ff621adab4a3582e75f4721e895f0e40618ad86eaf2b5e97f3ea` |
| `game.swf` | `@eternalfest/game` | 1.1.0 | [MIT](licenses/eternalfest-game-MIT.md) | https://gitlab.com/eternalfest/game | `ada9f39eef7bc8f7a93dafb9c2d2b803c25d2e73badf4543114b7f6550bb63fd` |

`loader.swf` is byte for byte the loader eternalfest.net serves at `/assets/loader.swf`. It is distributed unmodified, and its complete corresponding source code is available at the repository above.
