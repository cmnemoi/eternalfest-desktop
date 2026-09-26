# Manual checklist

Rendering and input can't be tested automatically. Run this checklist on Linux x64 and Windows x64 before a release, and whenever the Flash projector, Ruffle, the loader or the base engine is upgraded. On Linux, run it in the projector, then once in Ruffle (`mv src/EternalfestDesktop.Cli/bin/Debug/net10.0/flash-player{,.off}`).

## Setup

```sh
mise run cli play 0dc0d559-de83-4e0c-982d-fc56100dfdd5   # Les Cavernes de Hammerfest, in the pinned Flash projector (eng/flash-player.json), or Ruffle (eng/ruffle.json) without it
```

To check a release archive instead, extract `artifacts/packages/EternalfestDesktop-<version>-<rid>.*` (from `mise run package <rid>`) in a read-only folder and run `EternalfestDesktop` from it.

## Checks

`{#play::flash-projector-first}` `{#play::launches-flash-projector}` `{#play::launches-ruffle}` `{#play::tears-down-on-exit}`

- [ ] The game window opens (Flash Player on Linux, Ruffle on Windows: the log says which) and the loader shows its progress, then the game menu. On a system without GTK 2 installed, Flash Player still starts.
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
- [ ] From the release archive: the library opens with no prior step, and a contrée downloads and plays.
- [ ] `{#packaging::linux-desktop-entry}` On Linux (GNOME and KDE), after the release archive's first launch: "Eternalfest Desktop" is in the applications menu with its icon and starts the app, and its window shows that icon in the dock and task switcher. Once the extracted folder is deleted, the entry is gone from the menu.
