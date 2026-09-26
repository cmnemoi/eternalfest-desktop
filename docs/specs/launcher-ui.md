# Launcher UI

## Why

A non-technical player must go from opening the app to playing in a few clicks, without any setup.

## Scope

The library, the contrée page, "Play again", the game summary and settings. Behaviors are those of the view models; layout and styling are free.

## Rules

### No setup on first launch

`{#ui::no-first-launch-setup}`

The first launch opens the library directly: no wizard, no account, no path to choose. Every window shows that the app is unofficial (the about screen and the library footer).

### App icon

`{#ui::app-icon}`

The app has its own icon, made from the Eternalfest logo: a rounded tile, cropped closer to the triangle at small sizes so it stays readable. Windows shows it on the executable, the window and the taskbar; Linux on the window.

### Library

`{#ui::library}`

The library shows every catalog contrée with its icon, name and description, a "downloaded" badge, and an "update available" badge. It can be filtered by a text search on the name and description. The search is case- and accent-insensitive.

### Contrée page

`{#ui::contree-page}`

The contrée page shows the contrée details, the player profile picker, the mode picker, the options of the selected mode (visible ones only), volume, game locale (among the locales the build offers), fullscreen, and a Play button. Choices are remembered per contrée.

### Player profile picker

`{#ui::profile-picker}`

Above the mode picker, the player picks "Complete profile" or "New player" (see the player-profile spec). "Complete profile" is selected unless the contrée was last played as a new player. The modes and options offered are those of the build unlocked for the selected profile. Switching profiles keeps the selected mode and the checked options that are still offered, drops the others, and falls back to the first mode when the selected one is no longer offered.

### Download progress

`{#ui::download-progress}`

Playing a contrée that isn't downloaded shows download progress, and the download can be cancelled. Errors are shown in plain language, never as a stack trace.

### Play again

`{#ui::play-again}`

The library offers "Play again", which replays the last played contrée with the same choices, when that contrée is still in the cache.

### Game summary

`{#ui::game-summary}`

When a game ends with a result (see `play::closes-on-game-end`), the launcher comes back to the front and shows a "Game over" dialog: victory or defeat, the highest level reached, and the score (one per player in multicoop, as "Player 1" and "Player 2"). It explains that scores aren't saved offline, with a link to eternalfest.net that opens the browser, for a persistent account. "Play again" replays the same contrée with the same choices; "Close" dismisses it. A game that ends without a result shows nothing.

### Settings

`{#ui::settings}`

Settings allow:

- choosing the UI language (English or French, defaulting to the system language when supported, English otherwise);
- choosing the cache folder;
- clearing the cache;
- opening the logs folder.

## Acceptance criteria

- Given a fresh install with network, when the app opens, then the library lists the catalog with no prior step.
- Given the app's icon, then it holds every size Windows uses (16, 24, 32, 48, 64, 128 and 256 pixels), and the window uses it.
- Given the search "cavernes", when the library is filtered, then "Les Cavernes de Hammerfest" is listed and non-matching contrées aren't.
- Given the search "élite", when it matches "Elite" in a name, then that contrée is listed.
- Given a search with no match, then an explicit "no contrée matches" message is shown.
- Given a contrée played with mode "Multi" and an option checked, when its page is opened again, then the same choices are preselected.
- Given a contrée never played, when its page opens, then "Complete profile" is selected.
- Given *Les Cavernes de Hammerfest*, when "Complete profile" is selected, then *Intuition* and the *Deluxe Edition* mode are offered. When "New player" is selected, they aren't.
- Given the *Deluxe Edition* mode selected with *Miroir* and *Intuition* checked, when switching to "New player", then the first mode is selected. Given *Aventure* with *Miroir* and *Intuition* checked, when switching to "New player", then *Aventure* stays selected with *Miroir* checked.
- Given a contrée played as a new player, when its page is opened again, then "New player" is selected.
- Given no contrée was ever played, then "Play again" isn't offered.
- Given the last played contrée was removed by clearing the cache, then "Play again" isn't offered.
- Given a solo game ends with a defeat at level 11 and score 12345, then the summary shows a defeat, level 11 and 12345, with the eternalfest.net link.
- Given a multicoop game ends with scores 100 and 200, then the summary shows "Player 1: 100" and "Player 2: 200".
- Given the game summary, when "Play again" is chosen, then the same contrée is played with the same choices.
- Given the player closes the Ruffle window, then no summary is shown.
- Given a French system, when the app starts for the first time, then the UI is in French. Given a German system, then English.

## Out of scope

- Spanish UI (later).
- Themes, accessibility audit beyond Avalonia defaults (later).
- Leaderboards, and scores of past games.
- Items picked up and stats in the game summary.
