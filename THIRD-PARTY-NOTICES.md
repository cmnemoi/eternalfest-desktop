# Third-party notices

Eternalfest Desktop is licensed under the [GPL-3.0](LICENSE). It ships with:

| Component | License | Where |
|---|---|---|
| [Ruffle](https://ruffle.rs) desktop, pinned in `eng/ruffle.json` | MIT or Apache-2.0 | `ruffle/LICENSE.md` |
| Eternalfest loader (`@eternalfest/loader` 5.1.2), unmodified | AGPL-3.0-or-later, sources at https://gitlab.com/eternalfest/loader | `flash/NOTICE.md`, `flash/licenses/` |
| Hammerfest base engine (`@eternalfest/game` 1.1.0) | MIT | `flash/NOTICE.md`, `flash/licenses/` |
| Quests hardcoded by the Eternalfest server (`crates/core/src/inventory/quest_db.rs`), ported to `quests.json` (see [ADR 0007](docs/adr/0007-bundle-eternalfest-quests.md)) | AGPL-3.0-or-later, sources at https://gitlab.com/eternaltwin/hammerfest/eternalfest | Embedded in the app |
| .NET runtime, ASP.NET Core | MIT | https://github.com/dotnet/runtime, https://github.com/dotnet/aspnetcore |
| Avalonia UI, CommunityToolkit.Mvvm, Serilog | MIT / Apache-2.0 | https://github.com/AvaloniaUI/Avalonia, https://github.com/CommunityToolkit/dotnet, https://github.com/serilog/serilog |

Contrées are not part of this app: they are downloaded from eternalfest.net when a player asks for them, and belong to their authors.
