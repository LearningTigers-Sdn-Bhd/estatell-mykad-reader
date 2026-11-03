# MyKad Reader overview

## Description

MyKad Reader is a small native app for the front-desk computer. It reads the chip on a guest's MyKad through a USB card reader and gives the result to the Estatell web app. The Estatell guest form fills in about 5 seconds. The card itself sets that speed (see [Read time](01-card/01-reading-mykad.md#read-time)). Staff check the details and save.

A web page cannot talk to a USB card reader. MyKad Reader does it and answers Estatell over HTTP on `127.0.0.1:9292`. Developer docs call it **the bridge**. Screens and user docs call it **MyKad Reader**.

MyKad Reader shows an icon in the Windows tray and the macOS menu bar. Its menu shows whether the card reader is plugged in, whether a card is in, and whether an Estatell page is talking to it. It has no window. See [Tray](02-bridge/04-tray.md).

## Background

Estatell had a Python bridge: `estatell/tools/mykad-bridge/mykad_bridge.py` (version 1.1.0), bundled with PyInstaller. It worked, but:

- Antivirus tools often flag unsigned PyInstaller programs.
- Each build bundles a Python runtime.
- It has no icon, so staff cannot see whether it is running.
- Staff are asked to download the bridge from the check-in page.

MyKad Reader 2.0.0 replaces it. The Python bridge was removed after 2.0.0 shipped. Its last Estatell commit is `670c6a01`.

## Decisions

| Topic         | Decision                                                                                              |
| ------------- | ----------------------------------------------------------------------------------------------------- |
| Approach      | A local app that the Estatell page calls over HTTP (the bridge)                                       |
| Language      | .NET 10 LTS, C#, Native AOT. One native file, no runtime to install                                   |
| Platforms     | Windows 10 and 11 (x64, arm64), macOS 15 or later (Apple Silicon and Intel)                           |
| Older PCs     | Windows 7 and 8 are not supported. Hotels upgrade their devices                                       |
| Access        | Listen on `127.0.0.1` only, plus the Host and Origin checks, as in 1.x. No pairing and no token. See [Security](02-bridge/02-security.md#why-no-token) |
| Tray          | Native on each OS: Win32 `Shell_NotifyIcon` on Windows, `NSStatusItem` on macOS. Icon and menu only, no window, no Quit. Shows reader, card and Estatell status |
| Setup         | Install and go. No settings page in Estatell. Check-in shows a status chip that links to the set-up help |
| Install help  | In the Estatell user docs. The app links to it                                                        |
| Downloads     | GitHub Releases                                                                                       |
| Code signing  | None for now (no budget). The user docs explain the confirm prompts                                   |
| Wording       | Plain words on screens: "MyKad Reader", "running", "confirm". See [Words on screen](04-estatell/01-check-in-chip.md#words-on-screen) |
| Python bridge | Removed after 2.0.0. It was used only to compare output in M0                                         |

## How a scan works

![How a MyKad scan works](diagrams/scan-flow.svg)

1. The Estatell page asks MyKad Reader for its status every 2.5 seconds. The tray shows **Estatell: connected** while these requests come in.
2. Staff insert a card. The page asks MyKad Reader to read it. MyKad Reader checks Host and Origin, reads the chip and returns the data.

The Estatell server validates and stores everything. MyKad Reader never calls the server.

## Modules

| Module                                 | Doc                                                                                                                                                  |
| -------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| 01 Card                                | [Reading a MyKad](01-card/01-reading-mykad.md), [PC/SC access](01-card/02-pcsc.md)                                                                   |
| 02 Bridge                              | [HTTP API](02-bridge/01-http-api.md), [Security](02-bridge/02-security.md), [Tray](02-bridge/04-tray.md)                                             |
| 03 Packaging                           | [Installers](03-packaging/01-installers.md), [Build and release](03-packaging/02-build-and-release.md)                                                |
| 04 Estatell                            | [Check-in chip](04-estatell/01-check-in-chip.md), [App changes](04-estatell/02-app-changes.md), [User docs](04-estatell/03-user-docs.md)              |
| 05 Development                         | [Setup and testing](05-development/01-setup-and-testing.md)                                                                                          |

The build plan is in [checklist.md](checklist.md).

## Not in scope

- Linux.
- Code signing (later, when there is a budget).
- Reading passports or other ID cards.
- A window on the front-desk computer. The tray menu is the only screen MyKad Reader has.
- MyKad Reader calling the Estatell server.
- Connecting a computer to one hotel (pairing) and per-computer tokens. Dropped on 1 Nov 2026, see [Why no token](02-bridge/02-security.md#why-no-token).
- A settings page in Estatell.
