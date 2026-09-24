# Play a game

## Why

This is the product: from a downloaded contrée and the player's choices, open a playable game window, and clean everything up when it closes.

## Scope

The "play" use case: validating the choices, creating the local run, starting the offline backend, launching Ruffle with the right arguments, and tearing down.

## Rules

### Plays only a downloaded contrée

`{#play::requires-downloaded-contree}`

Playing a contrée that isn't fully downloaded first downloads it (see the game-store spec). Without network, it fails with an explanation.

### Uses a mode and options the build offers

`{#play::valid-mode-and-options}`

The chosen mode must exist in the build. The chosen options must be visible options of that mode (all of them enabled under full options). By default, the build's first mode and each option's default value are used.

### Creates a local run

`{#play::creates-local-run}`

A run is created locally with a fresh id, the contrée, its channel and build version, the mode, the options, and settings (locale and volume). It has the same shape as a run from eternalfest.net, and is never sent there.

### Launches Ruffle like the Eternalfest website embeds the loader

`{#play::launches-ruffle}`

Ruffle is launched on `{origin}/assets/loader.swf` with:

- the base set to the origin;
- the URL spoofed to the loader URL;
- the referer set to `{origin}/runs/{run id}`;
- a dummy external interface;
- the FlashVars `object_id`, `run` (the run as JSON), `game` (the contrée id) and `options` (mode, options, settings with the volume, and locale).

When fullscreen is chosen, Ruffle starts fullscreen.

### One game at a time

`{#play::one-game-at-a-time}`

While a game runs, playing another contrée is refused.

### Tears down when the game closes

`{#play::tears-down-on-exit}`

When the Ruffle process exits, for whatever reason, the offline backend stops and the launcher can play again. Ruffle's output goes to the log file.

### Warns about a newer loader

`{#play::warns-newer-loader}`

When the build requires a loader version newer than the bundled one, the player is warned that the contrée may not work, and can still play.

## Acceptance criteria

- Given downloaded `hammerfest-deluxe` with default choices, when it is played, then Ruffle receives the loader URL on the backend's origin and the four FlashVars, and the `run` FlashVar holds the chosen mode, options, locale and volume.
- Given a mode that doesn't exist in the build, when it is played, then it is refused before anything starts.
- Given a running game, when another contrée is played, then it is refused.
- Given Ruffle crashes, when its process exits, then the backend is stopped and playing again works.
- Given the Ruffle binary is missing, when a contrée is played, then it fails with an explicit error and the backend is stopped.
- Given a build requiring loader `6.0.0` while `5.1.2` is bundled, when it is played, then a warning is shown first.
- Manual checklist: `hammerfest-deluxe` loads, levels play, sound works, and closing the window returns to the launcher. Checked on Linux x64 and Windows x64.

## Out of scope

- Keyboard remapping and gamepads.
- Graphics quality settings beyond Ruffle defaults.
- TAS and libTAS integration.
