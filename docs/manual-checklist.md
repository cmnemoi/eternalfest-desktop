# Manual checklist

Rendering and input can't be tested automatically. Run this checklist on Linux x64 and Windows x64 before a release, and whenever Ruffle, the loader or the base engine is upgraded.

## Setup

```sh
dotnet run eng/fetch-ruffle.cs          # once per machine (pinned Ruffle, eng/ruffle.json)
dotnet build
cd src/EternalfestDesktop.Cli/bin/Debug/net10.0
dotnet EternalfestDesktop.Cli.dll play 0dc0d559-de83-4e0c-982d-fc56100dfdd5   # Les Cavernes de Hammerfest
```

## Checks

`{#play::launches-ruffle}` `{#play::tears-down-on-exit}`

- [ ] The Ruffle window opens and the loader shows its progress, then the game menu.
- [ ] Starting a game from the menu works; the log shows `POST /api/v1/runs/{id}/start` and no `Offline backend doesn't know` warning.
- [ ] Levels play: moving, jumping, bombs and items respond to the keyboard.
- [ ] Music and sound effects play.
- [ ] An option locked online (for example `play <id> solo ninja`) is active in game.
- [ ] Losing all lives ends the game; the log shows the discarded result (`ended, result discarded`).
- [ ] `--fullscreen` starts the game fullscreen.
- [ ] Closing the window returns to the console, and the command exits.
- [ ] With the network off, a downloaded contrée still plays.
