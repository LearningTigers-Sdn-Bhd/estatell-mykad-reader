# Check-in chip

## Purpose

How Estatell shows MyKad Reader to staff. There is no settings page: an admin installs MyKad Reader and it works. Every screen with the MyKad box shows one status chip.

## Chip

`MykadScanner` becomes a status chip. It has no download button. The hook polls `GET /status` every 2.5 seconds, as today.

| State                      | Chip                          | On click                                                       |
| -------------------------- | ----------------------------- | -------------------------------------------------------------- |
| Reader ready               | ● Ready for MyKad             | —                                                              |
| No reader                  | ○ Plug in card reader         | Opens the problems page in the user docs                       |
| `/status` does not answer  | ○ MyKad Reader isn't running  | Opens `set-up-mykad-reader` in the user docs (install or restart) |
| Update required (1.x)      | ⚠ Update required             | Opens the download for this OS                                 |

- "Update required" means `version` in `/status` is lower than `REQUIRED_BRIDGE_VERSION`.
- Read errors use the `code` from `/read`. See [GET /read](../02-bridge/01-http-api.md#get-read).

| `code`        | Message                                                   |
| ------------- | --------------------------------------------------------- |
| `no_reader`   | "Plug the card reader into this computer."                |
| `no_card`     | "Insert the MyKad, chip first and face up."               |
| `busy`        | "Still reading the last card. Try again in a moment."     |
| `read_failed` | "The card couldn't be read. Take it out and insert it again." |
| `forbidden`   | "MyKad Reader refused this page. Fix this ↗"               |

## Words on screen

Screens are for hotel owners and staff. Use plain, calm words.

| Avoid                                              | Use instead                                    |
| -------------------------------------------------- | ---------------------------------------------- |
| Bridge                                             | MyKad Reader                                   |
| Unsigned installer, code signing                   | *(never mention it)*                           |
| SmartScreen, Gatekeeper, "Windows protected your PC" | "Windows will ask you to confirm"            |
| Antivirus, quarantine                              | "If your security software asks, choose Allow" |
| Offline, port, localhost, version mismatch         | "isn't running", "Update required"             |
| Origin, CORS, 403                                  | *(never shown)*                                |
