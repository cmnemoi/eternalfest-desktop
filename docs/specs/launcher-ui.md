# Launcher UI

## Why

A non-technical player must go from opening the app to playing in a few clicks, without any setup.

## Scope

The library, the contrée page, "Play again" and settings. Behaviors are those of the view models; layout and styling are free.

## Rules

### No setup on first launch

`{#ui::no-first-launch-setup}`

The first launch opens the library directly: no wizard, no account, no path to choose. Every window shows that the app is unofficial (the about screen and the library footer).

### Library

`{#ui::library}`

The library shows every catalog contrée with its icon, name and description, a "downloaded" badge, and an "update available" badge. It can be filtered by a text search on the name and description. The search is case- and accent-insensitive.

### Contrée page

`{#ui::contree-page}`

The contrée page shows the contrée details, the mode picker, the options of the selected mode (visible ones only), volume, game locale (among the locales the build offers), fullscreen, and a Play button. Choices are remembered per contrée.

### Download progress

`{#ui::download-progress}`

Playing a contrée that isn't downloaded shows download progress, and the download can be cancelled. Errors are shown in plain language, never as a stack trace.

### Play again

`{#ui::play-again}`

The library offers "Play again", which replays the last played contrée with the same choices, when that contrée is still in the cache.

### Settings

`{#ui::settings}`

Settings allow:

- choosing the UI language (English or French, defaulting to the system language when supported, English otherwise);
- choosing the cache folder;
- clearing the cache;
- opening the logs folder.

## Acceptance criteria

- Given a fresh install with network, when the app opens, then the library lists the catalog with no prior step.
- Given the search "cavernes", when the library is filtered, then "Les Cavernes de Hammerfest" is listed and non-matching contrées aren't.
- Given the search "élite", when it matches "Elite" in a name, then that contrée is listed.
- Given a search with no match, then an explicit "no contrée matches" message is shown.
- Given a contrée played with mode "Multi" and an option checked, when its page is opened again, then the same choices are preselected.
- Given no contrée was ever played, then "Play again" isn't offered.
- Given the last played contrée was removed by clearing the cache, then "Play again" isn't offered.
- Given a French system, when the app starts for the first time, then the UI is in French. Given a German system, then English.

## Out of scope

- Spanish UI (later).
- Themes, accessibility audit beyond Avalonia defaults (later).
- Showing leaderboards or scores.
