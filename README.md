# Eternalfest Desktop

[![Continuous Integration](https://github.com/cmnemoi/eternalfest-desktop/actions/workflows/continuous-integration.yml/badge.svg)](https://github.com/cmnemoi/eternalfest-desktop/actions/workflows/continuous-integration.yml)
[![Continuous Delivery](https://github.com/cmnemoi/eternalfest-desktop/actions/workflows/continuous-delivery.yml/badge.svg)](https://github.com/cmnemoi/eternalfest-desktop/actions/workflows/continuous-delivery.yml)
[![Coverage](https://codecov.io/gh/cmnemoi/eternalfest-desktop/graph/badge.svg)](https://codecov.io/gh/cmnemoi/eternalfest-desktop)

> **Unofficial.** A personal project, not affiliated with Eternalfest or Eternaltwin.

Play [Eternalfest](https://eternalfest.net) contrées offline, with every mode and option unlocked. Scores aren't saved: to play with your account, use the official [Eternaltwin app](https://eternaltwin.org/docs/desktop).

![The contrée catalog of Eternalfest Desktop](docs/images/catalog.png)

## Install

- **Windows**: download [the installer](../../releases/latest/download/eternalfest-desktop-win-Setup.exe) and run it. If Windows SmartScreen warns you, choose *More info*, then *Run anyway*.
- **Linux**: download [the AppImage](../../releases/latest/download/eternalfest-desktop-linux-AppImage.tar.gz), extract it, and double-click `eternalfest-desktop.AppImage`.
- **macOS** (Apple Silicon): download [the app](../../releases/latest/download/eternalfest-desktop-osx-Portable.zip), extract it, and move it to *Applications*. The first time, macOS refuses to open it: go to *System Settings › Privacy & Security* and choose *Open Anyway*.

A portable zip for Windows and a plain archive for Linux are also in the [releases](../../releases/latest).

## Development

Requirements: [mise](https://mise.jdx.dev), which installs the .NET 10 SDK (`mise install`). Every project command is a mise task (`mise tasks` lists them):

```sh
mise run hooks                        # once per clone: text hygiene on commit, mise run ci on push
mise run build                        # the app, with the pinned Flash players (needs cc on Linux)
mise run test                         # mise run coverage measures it, mise run ci adds the format check
mise run format                       # mise run check fails on unformatted code instead
mise run mutation -m '**/StageSize.cs' # how many mutants the tests kill (Stryker.NET), report in artifacts/stryker
mise run app                          # the app
mise run cli play <id> --new-player   # a developer console: catalog, game, download, downloaded, play
mise run package [win-x64]            # a release for this machine or win-x64, in artifacts/packages (Linux needs mksquashfs)
```

Commits follow [Conventional Commits](https://www.conventionalcommits.org). [release-please](https://github.com/googleapis/release-please) keeps a release pull request open; merging it publishes the release.

- [Plan and delivery lots](docs/plan.md)
- [Architecture decision records](docs/adr)
- [Specs](docs/specs): behaviors carry stable IDs such as `{#backend::serves-cached-blobs}`, referenced from tests and code with `@spec`.

## License

[GPL-3.0](LICENSE). Contrées are downloaded from eternalfest.net and belong to their authors; they are never redistributed by this project.
