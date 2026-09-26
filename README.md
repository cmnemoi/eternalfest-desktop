# Eternalfest Desktop

> **Unofficial.** A personal project, not affiliated with, endorsed by or supported by the Eternalfest or Eternaltwin teams.

Play any public [Eternalfest](https://eternalfest.net) contrée offline, with every mode and option unlocked, by double-clicking an app. No terminal, no cloned repositories, no build tools, no account.

Eternalfest Desktop downloads a contrée once from the public Eternalfest API, then plays it forever without network in [Ruffle](https://ruffle.rs), against a tiny fake Eternalfest server running inside the app. Nothing is ever sent to eternalfest.net: this is just for fun, scores don't count.

Looking to play online, with your account and leaderboards? Use the official [Eternaltwin desktop app](https://eternaltwin.org/docs/desktop).

## Install

Download the archive for your system from the [releases](../../releases), extract it anywhere, and run:

- **Windows** (x64): `EternalfestDesktop.exe`. The app isn't signed yet, so Windows SmartScreen may warn on first launch: choose *More info*, then *Run anyway*.
- **Linux** (x64): `EternalfestDesktop`. Ruffle needs the usual desktop libraries (ALSA, udev, OpenGL or Vulkan), already present on typical desktops.

Nothing else to install. Downloaded contrées, the saved catalog, preferences and logs go to `%LOCALAPPDATA%\EternalfestDesktop` on Windows and `~/.local/share/eternalfest-desktop` on Linux.

## Status

Early development. See the [plan](docs/plan.md).

## Development

Requirements: [mise](https://mise.jdx.dev), which installs the .NET 10 SDK (`mise install`). Every project command is a mise task (`mise tasks` lists them):

```sh
mise run hooks                        # once per clone: text hygiene on commit, mise run ci on push
mise run build                        # the pinned Ruffle (eng/ruffle.json), needed to play, then the app
mise run test                         # mise run coverage measures it, mise run ci adds the format check
mise run format                       # mise run check fails on unformatted code instead
mise run app                          # the app
mise run cli play <id> --new-player   # a developer console: catalog, game, download, downloaded, play
mise run package linux-x64            # a release archive of version.txt, in artifacts/packages
```

Commits follow [Conventional Commits](https://www.conventionalcommits.org). Once the CI passes on `main`, [release-please](https://github.com/googleapis/release-please) keeps a release pull request open with the next version (`version.txt`) and the changelog; merging it publishes the Windows and Linux archives on GitHub Releases.

- [Plan and delivery lots](docs/plan.md)
- [Architecture decision records](docs/adr)
- [Specs](docs/specs): behaviors carry stable IDs such as `{#backend::serves-cached-blobs}`, referenced from tests and code with `@spec`.

## License

[GPL-3.0](LICENSE). Contrées are downloaded from eternalfest.net and belong to their authors; they are never redistributed by this project.
