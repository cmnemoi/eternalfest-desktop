# Play a game

## Why

This is the product: from a downloaded contrée and the player's choices, open a playable game window, and clean everything up when it closes.

## Scope

The "play" use case: validating the choices, creating the local run, starting the offline backend, launching the Flash player with the right arguments, and tearing down.

## Rules

### Plays only a downloaded contrée

`{#play::requires-downloaded-contree}`

Playing a contrée that isn't fully downloaded first downloads it (see the game-store spec). Without network, it fails with an explanation.

### Uses a mode and options the build offers

`{#play::valid-mode-and-options}`

The chosen mode must be visible in the build unlocked for the chosen player profile (see the player-profile spec). The chosen options must be visible options of that mode (all of them enabled under full options). By default, the complete profile, the first visible mode and each option's default value are used.

### Creates a local run

`{#play::creates-local-run}`

A run is created locally with a fresh id, the contrée, its channel and build version, the mode, the options, and settings (locale and volume). It has the same shape as a run from eternalfest.net, and is never sent there.

### Plays in Adobe's Flash projector, or Ruffle without it

`{#play::flash-projector-first}`

Contrées play in Adobe's Flash projector, bundled for the OS (see ADR 0008 and 0010). When the app ships no projector for the OS, or the OS can't run it (a Mac without Rosetta 2), they play in Ruffle.

### Launches the Flash projector like the Eternalfest website embeds the loader

`{#play::launches-flash-projector}`

The projector is launched on `{origin}/assets/loader.swf`, with the FlashVars `object_id`, `run`, `game` and `options` (as for Ruffle below) in its query string. On Linux, it runs with its bundled GTK 2 and NSS libraries, and with `libprojector-window.so`, which shapes its window from inside. On macOS, it runs under Rosetta 2, with `libprojector-window.dylib`, which shapes its window from inside, in points. On Windows, the launcher shapes the window from outside for the first seconds, while the projector sizes it to the loader: it removes the menu bar and sizes it, in the projector's unscaled pixels.

When fullscreen is chosen, the projector's window is made fullscreen.

### Launches Ruffle like the Eternalfest website embeds the loader

`{#play::launches-ruffle}`

Ruffle is launched on `{origin}/assets/loader.swf` with:

- the base set to the origin;
- the URL spoofed to the loader URL;
- the referer set to `{origin}/runs/{run id}`;
- a dummy external interface;
- opening websites denied;
- the FlashVars `object_id`, `run` (the run as JSON), `game` (the contrée id) and `options` (mode, options, settings with the volume, and locale).

When fullscreen is chosen, Ruffle starts fullscreen.

### Fills the screen's height

`{#play::fills-screen-height}`

Outside fullscreen, the game window takes the whole height the launcher's screen leaves to windows (the taskbar excluded), keeping room for the window's title bar and a margin from the screen's edges, and its width follows the loader's proportions. The player's menu bar (and the projector's URL bar) is hidden, so that the game takes all of the window. When the screen can't be known, the player sizes the window to the loader.

### Never opens websites

`{#play::never-opens-websites}`

The player opens no website the movie asks for: no dialog, no browser. When a game ends, the loader asks to open `/runs/{run id}` like on eternalfest.net; offline, the launcher shows the game summary instead. Ruffle is told to deny it; the projector ignores it, since the loader targets `_self`.

### One game at a time

`{#play::one-game-at-a-time}`

While a game runs, playing another contrée is refused.

### Tears down when the game closes

`{#play::tears-down-on-exit}`

When the player's process exits, for whatever reason, the offline backend stops and the launcher can play again. The player's output goes to the log file.

### Closes when the game ends

`{#play::closes-on-game-end}`

When the loader posts the run result (the player lost all their lives, won, or gave up in game), the player is closed right away and playing returns that result: whether it's a victory, the highest level reached, and the score of each player. A game that ends without a result (the window closed by the player, a crash, an end of set the loader treats as a crash) returns no result. The result is never persisted or sent to eternalfest.net.

### Warns about a newer loader

`{#play::warns-newer-loader}`

When the build requires a loader version newer than the bundled one, the player is warned that the contrée may not work, and can still play.

## Acceptance criteria

- Given the app ships the projector, when a contrée is played, then it plays in the projector. Given it doesn't, then it plays in Ruffle.
- Given a contrée is played in the projector, then it opens the loader URL on the backend's origin with the four FlashVars in its query string.
- Given the Linux projector, when a contrée is played, then it runs with its bundled libraries and `libprojector-window.so`; fullscreen is asked for when chosen.
- Given the Windows projector and a screen leaving 1040 pixels of height at 100 % (or 1560 at 150 %), when a contrée is played, then the game is 782 × 968 of the projector's pixels, read from the loader's stage (420 × 520).
- Given downloaded `hammerfest-deluxe` with default choices, when it is played in Ruffle, then Ruffle receives the loader URL on the backend's origin and the four FlashVars, and the `run` FlashVar holds the chosen mode, options, locale and volume.
- Given a mode that doesn't exist in the build, when it is played, then it is refused before anything starts.
- Given *Les Cavernes de Hammerfest*, when it is played with *Intuition* and the complete profile, then the game starts. With the new player profile, it is refused.
- Given a running game, when another contrée is played, then it is refused.
- Given a running game, when the loader posts a defeat at level 11 with score 12345, then the player is stopped, the backend is stopped, and playing returns that result.
- Given a running game, when the player closes the game window, then playing returns no result.
- Given a contrée is played in Ruffle, then Ruffle is asked to deny opening websites.
- Given a screen leaving 1040 pixels of height to windows at 100 % scaling, when a contrée is played outside fullscreen, then the player opens a 968-pixel-high game without its menu bar, and its width follows the loader's. At 150 % with 1560 pixels, the game is 1452 pixels high.
- Given the player crashes, when its process exits, then the backend is stopped and playing again works.
- Given neither the projector nor Ruffle is in the app folder, when a contrée is played, then it fails with an explicit error and the backend is stopped.
- Given a build requiring loader `6.0.0` while `5.1.2` is bundled, when it is played, then a warning is shown first.
- Manual checklist: `hammerfest-deluxe` loads, levels play, sound works, and closing the window returns to the launcher. Losing all lives closes the window without any dialog or browser, and the launcher shows the game summary. Checked on Linux x64 and Windows x64.

## Out of scope

- Keyboard remapping and gamepads.
- Graphics quality settings beyond the player's defaults.
- Choosing Ruffle when the projector is shipped.
- TAS and libTAS integration.
