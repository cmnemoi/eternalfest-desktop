# 0009 — Velopack packages, and a Mac app playing in Ruffle

- Status: accepted
- Date: 2026-09-26

## Context

Releases shipped a Windows zip and a Linux tar.gz. Players asked for less: a Windows installer, a single Linux file, and a Mac app. Auto-update is wanted later.

Facts that shaped the decision:

- Velopack (MIT) makes, from one self-contained build, a Windows installer and portable zip, a Linux AppImage and a Mac app, each with an update feed its library reads later. It packs Windows and Linux from Linux, and Macs only on macOS.
- Its installer installs in `%LOCALAPPDATA%\{package id}`, and its uninstaller removes that folder: `EternalfestDesktop`, the Windows data folder, would lose the downloaded contrées.
- Its AppImage embeds the static type2 runtime: no `libfuse2` is needed. It adds nothing to the applications menu.
- Adobe's Flash projector for macOS is Intel only. Ruffle's Mac build is a universal app bundle signed and notarized by Ruffle LLC.
- An Apple developer account costs 99 USD a year. Without it, an app can only be signed ad hoc, which Apple Silicon requires, and macOS blocks it until the player allows it once.
- By default, Velopack copies every file into `Contents/MacOS` and signs the bundle with `codesign --deep --force`, which would replace Ruffle's signature.

## Decision

- Package with Velopack 1.2.158, as the package id `eternalfest-desktop`, apart from the data folder. Publish the Windows installer and portable zip, the Linux AppImage, the Mac app zipped, and keep the Linux tar.gz. Attach the update feeds and full packages to each release; the app doesn't read them yet.
- Keep the download names without version, so the README links to `releases/latest/download/…`.
- Ship macOS for Apple Silicon only, playing in Ruffle: the projector isn't shipped there, which ADR 0008's fallback already handles.
- Build the Mac app bundle ourselves, with every file but the executable in `Contents/Resources`, Ruffle's bundle copied untouched, and let Velopack sign it ad hoc without `--deep`. Don't notarize.
- Make the packages on every change in the integration, on Linux for Windows and Linux, and on a Mac runner for macOS.

## Consequences

- Players get a Start menu shortcut on Windows, one file on Linux, and a Mac app, with nothing else to install.
- macOS refuses the app the first time; the README explains *Open Anyway*. Signing and notarizing later needs no change of format.
- Each download is about 85 MB, and the first update will download a full package too: no delta packages.
- The Mac app can't be checked on a Linux machine: the integration's Mac runner packs it and checks Ruffle's signature, and the manual checklist covers playing.
- Existing Windows players who used the zip keep their data, since the install folder differs from it.
