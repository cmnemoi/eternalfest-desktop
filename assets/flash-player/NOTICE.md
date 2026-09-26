# Flash projector

Contrées play in Adobe's Flash Player 32 projector (see [ADR 0008](../../docs/adr/0008-flash-projector-first-ruffle-fallback.md)). Everything in this folder is pinned in `eng/flash-player.json` and checked by SHA-256 when fetched: upgrading one is a deliberate change, tested with the manual checklist.

## Adobe Flash Player

| File | Version | License | Source |
|---|---|---|---|
| `flashplayer` (Linux x64) | 32.0.0.465 | Adobe's license, in `license.pdf`; the LGPL notices Adobe ships with it are in `LGPL/` | https://fpdownload.macromedia.com/pub/flashplayer/updaters/32/flash_player_sa_linux.x86_64.tar.gz |

Adobe no longer distributes Flash Player and never allowed redistributing it. It is shipped anyway, as the Eternaltwin desktop app does, so that contrées play as they do on the Eternalfest website.

## Libraries of the Linux projector, in `lib/`

Recent distributions no longer install GTK 2 and NSS, which the Linux projector links to. They are taken, unmodified, from Debian 12's binary packages:

| Files | Debian package | License | Copyright | Sources |
|---|---|---|---|---|
| `libgtk-x11-2.0.so.0`, `libgdk-x11-2.0.so.0` | `libgtk2.0-0` 2.24.33-2+deb12u1 | [LGPL-2.0-or-later](licenses/LGPL-2.txt) | [`licenses/libgtk2.0-0.copyright`](licenses/libgtk2.0-0.copyright) | https://snapshot.debian.org/package/gtk+2.0/2.24.33-2+deb12u1/ |
| `libnss3.so`, `libnssutil3.so`, `libsmime3.so`, `libssl3.so`, `libsoftokn3.so`, `libfreebl3.so`, `libfreeblpriv3.so`, `libnssckbi.so`, `libnssdbm3.so` | `libnss3` 2:3.87.1-1+deb12u4 | [MPL-2.0](licenses/MPL-2.0.txt), with parts under BSD-3-Clause, Zlib and the public domain | [`licenses/libnss3.copyright`](licenses/libnss3.copyright) | https://snapshot.debian.org/package/nss/2:3.87.1-1+deb12u4/ |
| `libnspr4.so`, `libplc4.so`, `libplds4.so` | `libnspr4` 2:4.35-1 | [MPL-2.0](licenses/MPL-2.0.txt) | [`licenses/libnspr4.copyright`](licenses/libnspr4.copyright) | https://snapshot.debian.org/package/nspr/2:4.35-1/ |
| `libprojector-window.so` | Built from `native/projector-window/` of Eternalfest Desktop | GPL-3.0-or-later, like the app (`LICENSE` at the root of the archive) | Eternalfest Desktop contributors | https://github.com/cmnemoi/eternalfest-desktop |

The complete corresponding source code of GTK 2, NSS and NSPR is available from the Debian snapshot archive at the addresses above. These libraries can be replaced with other builds of the same versions: the projector loads whatever `lib/` holds.
