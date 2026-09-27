# Third-party notices

Eternalfest Desktop is licensed under the [GPL-3.0](LICENSE). It ships with:

| Component | License | Where |
|---|---|---|
| Adobe Flash Player 32.0.0.465 projector for Linux, macOS and Windows, pinned in `eng/flash-player.json` | Adobe's license; Adobe doesn't allow redistributing it, see [ADR 0008](docs/adr/0008-flash-projector-first-ruffle-fallback.md) | `flash-player/NOTICE.md` |
| GTK 2 (`libgtk2.0-0` 2.24.33-2+deb12u1), NSS (`libnss3` 2:3.87.1-1+deb12u4) and NSPR (`libnspr4` 2:4.35-1) from Debian 12, unmodified, for the Linux projector | LGPL-2.0-or-later (GTK 2); MPL-2.0 (NSS, NSPR), with parts of NSS under BSD-3-Clause and Zlib; sources on https://snapshot.debian.org | `flash-player/NOTICE.md`, `flash-player/licenses/` |
| `libprojector-window.so` and `libprojector-window.dylib`, built from `native/projector-window/` | GPL-3.0-or-later, like the app | `flash-player/NOTICE.md` |
| [Ruffle](https://ruffle.rs) desktop, pinned in `eng/ruffle.json` | MIT or Apache-2.0 | `ruffle/LICENSE.md` |
| Eternalfest loader (`@eternalfest/loader` 5.1.2), unmodified | AGPL-3.0-or-later, sources at https://gitlab.com/eternalfest/loader | `flash/NOTICE.md`, `flash/licenses/` |
| Hammerfest base engine (`@eternalfest/game` 1.1.0) | MIT | `flash/NOTICE.md`, `flash/licenses/` |
| Quests hardcoded by the Eternalfest server (`crates/core/src/inventory/quest_db.rs`), ported to `quests.json` (see [ADR 0007](docs/adr/0007-bundle-eternalfest-quests.md)) | AGPL-3.0-or-later, sources at https://gitlab.com/eternaltwin/hammerfest/eternalfest | Embedded in the app |
| .NET runtime, ASP.NET Core | MIT | https://github.com/dotnet/runtime, https://github.com/dotnet/aspnetcore |
| Avalonia UI, CommunityToolkit.Mvvm, Serilog | MIT / Apache-2.0 | https://github.com/AvaloniaUI/Avalonia, https://github.com/CommunityToolkit/dotnet, https://github.com/serilog/serilog |
| [Velopack](https://velopack.io) 1.2.158: its library, and the installer, updater and portable launcher it adds to the Windows and Linux packages | MIT | https://github.com/velopack/velopack |
| AppImage [type2-runtime](https://github.com/AppImage/type2-runtime), at the start of the AppImage, with libfuse and squashfuse linked in | MIT; libfuse LGPL-2.1, squashfuse BSD-2-Clause | https://github.com/AppImage/type2-runtime, https://github.com/libfuse/libfuse, https://github.com/vasi/squashfuse |

Contrées are not part of this app: they are downloaded from eternalfest.net when a player asks for them, and belong to their authors.
