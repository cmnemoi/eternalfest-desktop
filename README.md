# Eternalfest Desktop

> **Unofficial.** A personal project, not affiliated with, endorsed by or supported by the Eternalfest or Eternaltwin teams.

Play any public [Eternalfest](https://eternalfest.net) contrée offline, with every mode and option unlocked, by double-clicking an app. No terminal, no cloned repositories, no build tools, no account.

Eternalfest Desktop downloads a contrée once from the public Eternalfest API, then plays it forever without network in [Ruffle](https://ruffle.rs), against a tiny fake Eternalfest server running inside the app. Nothing is ever sent to eternalfest.net: this is just for fun, scores don't count.

Looking to play online, with your account and leaderboards? Use the official [Eternaltwin desktop app](https://eternaltwin.org/docs/desktop).

## Status

Early development. See the [plan](docs/plan.md).

## Development

Requirements: the .NET 10 SDK (`mise install` sets it up).

```sh
dotnet build
dotnet test
```

- [Plan and delivery lots](docs/plan.md)
- [Architecture decision records](docs/adr)
- [Specs](docs/specs): behaviors carry stable IDs such as `{#backend::serves-cached-blobs}`, referenced from tests and code with `@spec`.

## License

[GPL-3.0](LICENSE). Contrées are downloaded from eternalfest.net and belong to their authors; they are never redistributed by this project.
