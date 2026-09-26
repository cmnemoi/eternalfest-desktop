# Manual checklist

Rendering and input can't be tested automatically. Run this checklist on Linux x64, Windows x64 and macOS arm64 before a release, and whenever the Flash projector, Ruffle, the loader or the base engine is upgraded. On Linux, run it in the projector, then once in Ruffle (`mv src/EternalfestDesktop.Cli/bin/Debug/net10.0/flash-player{,.off}`).

## Setup

```sh
mise run cli play 0dc0d559-de83-4e0c-982d-fc56100dfdd5   # Les Cavernes de Hammerfest, in the pinned Flash projector (eng/flash-player.json), or Ruffle (eng/ruffle.json) without it
```

To check a release instead, build it with `mise run package <rid>` and use `artifacts/packages`: on Windows, run `eternalfest-desktop-win-Setup.exe`, or extract `eternalfest-desktop-win-Portable.zip` in a read-only folder; on Linux, run `eternalfest-desktop.AppImage` once made executable, or extract `EternalfestDesktop-<version>-linux-x64.tar.gz` in a read-only folder; on a Mac, extract `eternalfest-desktop-osx-Portable.zip` and move the app to Applications.

## Checks

`{#play::flash-projector-first}` `{#play::launches-flash-projector}` `{#play::launches-ruffle}` `{#play::tears-down-on-exit}`

- [ ] The game window opens (Flash Player; the log says which player) and the loader shows its progress, then the game menu. On a system without GTK 2 installed, Flash Player still starts.
- [ ] Starting a game from the menu works; the log shows `POST /api/v1/runs/{id}/start` and no `Offline backend doesn't know` warning.
- [ ] Levels play: moving, jumping, bombs and items respond to the keyboard.
- [ ] Music and sound effects play.
- [ ] An option locked online (for example `play <id> solo ninja`) is active in game.
- [ ] `{#profile::unlocks-like-eternalfest}` *Les Cavernes de Hammerfest* with the complete profile (the default) starts with 6 lives and 2 bombs, and `play <id> solo insight` shows *Intuition* in game. With `--new-player`, it starts with 1 life and 1 bomb.
- [ ] `{#play::closes-on-game-end}` `{#play::never-opens-websites}` `{#ui::game-summary}` Losing all lives closes the game window with no "open website" dialog and no browser; the launcher comes to the front with the game summary (the CLI prints it).
- [ ] `--fullscreen` starts the game fullscreen.
- [ ] `{#play::fills-screen-height}` From the app (`mise run app`), outside fullscreen, the game window is as high as the screen allows: its title bar is visible, it doesn't go under the taskbar, it has no menu or URL bar and no black bars. Also on a screen scaled to 150 % or more.
- [ ] Closing the window returns to the console, and the command exits.
- [ ] With the network off, a downloaded contrée still plays.
- [ ] From each download of the release (installer, portable zip, AppImage, tar.gz): the library opens with no prior step, and a contrée downloads and plays.
- [ ] `{#packaging::windows-installer}` On Windows, the installer asks for no administrator rights, installs in `%LOCALAPPDATA%\eternalfest-desktop`, adds an "Eternalfest Desktop" shortcut to the Start menu and the desktop, and starts the app. Uninstalling it from the installed apps removes that folder and leaves `%LOCALAPPDATA%\EternalfestDesktop` (contrées, preferences) in place.
- [ ] `{#packaging::macos-app}` On a Mac with Apple Silicon, the app downloaded from the release is refused at first, opens after *Open Anyway* in Privacy & Security, shows its icon in the Dock, keeps its data in `~/Library/Application Support/EternalfestDesktop`, and plays a contrée in Ruffle, with sound; closing the game returns to the launcher.
- [ ] `{#packaging::linux-appimage}` On Ubuntu 24.04 without `libfuse2`, the AppImage starts once made executable, and its applications menu entry runs the AppImage file, still after moving it.
- [ ] `{#packaging::linux-desktop-entry}` On Linux (GNOME and KDE), after the release archive's first launch: "Eternalfest Desktop" is in the applications menu with its icon and starts the app, and its window shows that icon in the dock and task switcher. Once the extracted folder is deleted, the entry is gone from the menu.
