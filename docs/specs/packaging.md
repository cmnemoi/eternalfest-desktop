# Packaging

## Why

"Plug and play" means a player downloads one file and double-clicks. Nothing else to install.

## Scope

What each release publishes for Windows x64 and Linux x64 on GitHub Releases, how it is built with Velopack, and where the installed app lives. Auto-update, macOS and stores come later.

## Rules

### Self-contained downloads

`{#packaging::self-contained-archives}`

Each release publishes:

- `eternalfest-desktop-win-Setup.exe`, the Windows installer (see below);
- `eternalfest-desktop-win-Portable.zip`, the same app for Windows, to extract anywhere;
- `eternalfest-desktop.AppImage`, the app for Linux in one executable file;
- `EternalfestDesktop-{version}-linux-x64.tar.gz`, the same app for Linux, to extract anywhere.

The first three names carry no version, so `…/releases/latest/download/{name}` always links to the latest one. Each download contains the launcher (with the .NET runtime), the pinned Flash projector for that OS (on Linux, with its GTK 2 and NSS libraries and `libprojector-window.so`), the pinned Ruffle binary for that OS, the bundled loader and base engine, the app icon as `icon.png` (for a Linux shortcut), and the license notices. No other installation is needed.

### Windows installer

`{#packaging::windows-installer}`

The installer needs no administrator rights: it installs the app for the current user in `%LOCALAPPDATA%\eternalfest-desktop`, a folder of its own, apart from the app's data, adds a Start menu and a desktop shortcut named "Eternalfest Desktop", and starts it. The app can be uninstalled from Windows' installed apps, which leaves the downloaded contrées and preferences in place.

### Linux AppImage

`{#packaging::linux-appimage}`

The AppImage runs once made executable, with nothing to install on a desktop with FUSE 3 (Ubuntu 22.04 and newer), and without FUSE 2. Wherever it is moved, it keeps its data in the same place as the tar.gz.

### Ready to update

`{#packaging::update-feed}`

Each release also publishes what a later version of the app needs to update an installed one: the Velopack feeds `releases.win.json` and `releases.linux.json` and the full packages they list. The installer, the portable zip and the AppImage are installed apps that can update; the tar.gz isn't.

### License notices

`{#packaging::license-notices}`

Archives ship the app's GPL-3.0 license, a notice for the Flash projector and its libraries (`flash-player/NOTICE.md`), with Adobe's license and the LGPL notices it ships with, and, on Linux, the LGPL-2.0 and MPL-2.0 texts and the Debian copyright files of GTK 2, NSS and NSPR, with links to their sources, Ruffle's MIT/Apache-2.0 notices, the loader's AGPL-3.0 notice with a link to its sources, the base engine's MIT notice, and the notices of Velopack (MIT) and, in the AppImage, of the AppImage runtime.

### Released by merging the release pull request

`{#packaging::released-from-release-pr}`

Once the integration checks pass on `main`, release-please keeps one pull request open with the next version and its changelog, computed from the conventional commits since the last release. Merging it tags `v{version}`, creates the GitHub Release, and attaches the downloads and the update feeds. Nothing is released without that merge, and a failing check on `main` releases nothing.

Versions follow semantic versioning from `0.1.0`: a `fix` bumps the patch, a `feat` the minor, and a breaking change (`feat!`, `BREAKING CHANGE`) the major, even before `1.0.0`.

### One version everywhere

`{#packaging::one-version}`

`version.txt`, maintained by release-please, is the only place the version is written. The app, its logs, its packages, its update feeds and the release tag all carry it.

### Data outside the install folder

`{#packaging::data-outside-install}`

The app never writes inside its own folder. Cache, catalog, settings and logs go to `%LOCALAPPDATA%\EternalfestDesktop` on Windows, and `$XDG_DATA_HOME/eternalfest-desktop` (default `~/.local/share/eternalfest-desktop`) on Linux, unless another cache folder is chosen in settings.

### Linux applications menu

`{#packaging::linux-desktop-entry}`

On Linux, the app adds itself to the user's applications menu each time it starts: a desktop entry in `$XDG_DATA_HOME/applications` (default `~/.local/share/applications`) that runs the app from where it is (the AppImage file itself, not the temporary folder it runs from), and its icon in the user's `hicolor` icon theme. The entry is only rewritten when it changed, so moving the extracted folder or the AppImage moves the entry with it. An entry whose app was deleted is hidden by the desktop, since it names the app as `TryExec`. The window's class matches the entry, so docks and task switchers show the app's icon and name. A failure to write the entry is logged and never prevents the app from starting. Development builds leave the menu alone.

## Acceptance criteria

- Given a clean Windows 11 x64 machine, when the installer is double-clicked, then the app installs without administrator rights, starts, and a contrée can be played; the Start menu has an "Eternalfest Desktop" shortcut.
- Given contrées downloaded by an earlier version in `%LOCALAPPDATA%\EternalfestDesktop`, when the app is installed then uninstalled, then the contrées are still there.
- Given a clean Windows 11 x64 machine, when the portable zip is extracted and the executable double-clicked, then the library opens and a contrée can be played.
- Given a clean Ubuntu LTS desktop, when the AppImage is made executable and launched, then the library opens and a contrée can be played.
- Given a clean Ubuntu LTS desktop, when the tar.gz is extracted and the executable launched, then the library opens and a contrée can be played.
- Given the app is extracted in a read-only folder, when it runs, then it works.
- Given the Linux app extracted in `/home/player/Jeux/Eternalfest Desktop`, when it starts, then the applications menu has an "Eternalfest Desktop" entry with the app's icon, which starts that app.
- Given the AppImage `/home/player/Applications/eternalfest-desktop.AppImage`, when it starts, then the menu entry runs that file.
- Given the entry is already up to date, when the app starts, then the entry file isn't written again. Given the folder was moved, then the entry points to the new place.
- Given the applications folder can't be written, when the app starts, then it starts anyway and a warning is logged.
- Given the release pull request is merged, when the delivery runs, then the four downloads, with their license notices, and the Windows and Linux update feeds with their full packages are attached to the GitHub Release.
- Given `version.txt` holds `0.5.0`, when the app is packaged, then the update feeds and full packages carry `0.5.0`.
- Given `version.txt` holds `0.2.0`, when the app is built, then its assembly version is `0.2.0`.

## Out of scope

- Code signing (Windows SmartScreen will warn on first launch, and the README explains it).
- Checking for, downloading and applying updates in the app: a later lot, which the update feeds prepare.
- Delta packages: every update downloads the full package.
- An installer or a menu entry for the tar.gz, and AppImage integration tools (AppImageLauncher, Gear Lever): the app adds itself to the menu.
- Flatpak, winget and macOS: later lots.
