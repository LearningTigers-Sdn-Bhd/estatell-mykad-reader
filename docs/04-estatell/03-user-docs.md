# User docs

## Purpose

The install steps, including the Windows and Mac confirm prompts, live in the Estatell user docs (`estatell-docs`). The check-in chip and the tray **Help** item link there.

## Pages

In `estatell-docs/content/docs/en/guests/identity-documents/`. `meta.json` lists them in this order:

```json
"pages": ["set-up-mykad-reader", "mykad-reader", "mykad-reader-problems"]
```

| Page                     | Status    | Sections                                                                                               |
| ------------------------ | --------- | ------------------------------------------------------------------------------------------------------ |
| `set-up-mykad-reader`    | New       | Before you start · Download · Install (Windows and Mac tabs) · Check it works · Update · Remove |
| `mykad-reader`           | Rewrite   | Read a card · What the chip says · Where it works. No more "bridge", **Install Bridge** or "macOS 11"   |
| `mykad-reader-problems`  | New       | One row per problem, with what to do                                                                   |

## `set-up-mykad-reader`

**Before you start**

- A card reader: ACS ACR39U.
- Windows 10 or 11, or macOS 15 or later.
- An admin account on the computer (to install).
- About 2 minutes for each computer.

**Download**

The page has the two GitHub Releases links from [Build and release](../03-packaging/02-build-and-release.md#release): **Download for Windows** and **Download for Mac**.

**Install, Windows**

1. Select **Download for Windows**.
2. Open the file.
3. Windows asks you to confirm, as it does for new apps. Select **More info**, then **Run anyway**, then **Yes**.
4. If your security software asks, choose **Allow**.

**Install, Mac**

1. Select **Download for Mac**.
2. Open the file.
3. Your Mac asks you to confirm the first time. Open **System Settings → Privacy & Security**, scroll down and select **Open Anyway**.
4. Open the file again, follow the steps and enter your Mac password.

**Check it works**

1. Click the MyKad Reader icon. On Windows it is near the clock (look under **^** if you don't see it). On a Mac it is at the top of the screen.
2. It shows **● Ready** and your card reader's name.
3. Open check-in in Estatell. The chip shows **Ready for MyKad**, and the icon menu shows **Estatell: connected**.
4. Insert any MyKad. The guest form fills. Nothing is saved until you select **Save**.

## `mykad-reader-problems`

| Problem                              | What to do                                                                 |
| ------------------------------------ | -------------------------------------------------------------------------- |
| "MyKad Reader isn't running"         | Select **Restart MyKad Reader** in the icon menu, or restart the computer. If there is no icon, install it again |
| "Plug in card reader"                | Plug the reader in. Try another USB port                                  |
| The card isn't read                  | Take the card out. Insert it chip first and face up, then wait            |
| Icon menu says **Estatell: not seen** while check-in is open | Reload the page. If it stays, see "MyKad Reader refused this page" |
| "MyKad Reader refused this page"     | Use Estatell at app.estatell.com. Contact support if you use another address |
| No MyKad Reader icon                 | On Windows, look under **^** near the clock. Otherwise restart the computer |
| Security software removed the app    | Restore it and choose **Allow**, then install again                       |
| "Update required"                    | Download the update and open it once                                      |

## Screenshots

Real screenshots of the Windows and Mac confirm prompts, with the buttons numbered, and of the MyKad Reader icon and menu on both. The estatell-docs plan says MyKad pages use special-case tables, not live reader screenshots (S8). Add an exception for the install prompts.

## Wording

Follow [Words on screen](01-check-in-chip.md#words-on-screen). Never say "unsigned", "bridge", "SmartScreen" or "quarantine".

## Links

- Link `set-up-mykad-reader` from the onboarding section, so it is part of getting a hotel live.
- The check-in chip and the tray **Help** item link here.
