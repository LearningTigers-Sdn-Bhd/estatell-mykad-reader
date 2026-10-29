# User docs

## Purpose

The install steps, including the Windows and Mac confirm prompts, live in the Estatell user docs (`estatell-docs`). The settings page stays short and links there.

## Pages

In `estatell-docs/content/docs/en/guests/identity-documents/`. `meta.json` lists them in this order:

```json
"pages": ["set-up-mykad-reader", "mykad-reader", "mykad-reader-problems"]
```

| Page                     | Status    | Sections                                                                                               |
| ------------------------ | --------- | ------------------------------------------------------------------------------------------------------ |
| `set-up-mykad-reader`    | New       | Before you start · Install (Windows and Mac tabs) · Connect the computer · Manage connected computers · Update · Remove |
| `mykad-reader`           | Rewrite   | Read a card · What the chip says · Where it works. No more "bridge", **Install Bridge** or "macOS 11"   |
| `mykad-reader-problems`  | New       | One row per problem, with what to do                                                                   |

## `set-up-mykad-reader`

**Before you start**

- A card reader: ACS ACR39U.
- Windows 10 or 11, or macOS 15 or later.
- Permission: **Manage MyKad reader**. Owners and managers have it.
- About 2 minutes for each computer.

**Install, Windows**

1. Open **Settings → MyKad Reader** and select **Download for Windows**.
2. Open the file.
3. Windows asks you to confirm, as it does for new apps. Select **More info**, then **Run anyway**, then **Yes**.
4. If your security software asks, choose **Allow**.

**Install, Mac**

1. Select **Download for Mac**.
2. Open the file.
3. Your Mac asks you to confirm the first time. Open **System Settings → Privacy & Security**, scroll down and select **Open Anyway**.
4. Open the file again, follow the steps and enter your Mac password.

**Connect the computer**

1. Go back to **Settings → MyKad Reader**. The page moves on by itself.
2. Name the computer, for example "Front Desk 1".
3. Click the MyKad Reader icon. On Windows it is near the clock (look under **^** if you don't see it). On a Mac it is at the top of the screen. Type the **Connect code** it shows.
4. Select **Connect**.
5. Insert any MyKad to try it. Nothing is saved.

## `mykad-reader-problems`

| Problem                              | What to do                                                                 |
| ------------------------------------ | -------------------------------------------------------------------------- |
| "Card reading not set up"            | Ask an owner or manager to follow *Set up the MyKad reader*               |
| "MyKad Reader isn't running"         | Restart the computer. If it still doesn't work, install it again          |
| "Plug in card reader"                | Plug the reader in. Try another USB port                                  |
| The card isn't read                  | Take the card out. Insert it chip first and face up, then wait            |
| "Connected to another hotel"         | That hotel's admin disconnects it first                                   |
| "That code doesn't match"            | Click the MyKad Reader icon and type the code it shows now                |
| "Too many tries"                     | Click the MyKad Reader icon, choose **New connect code**, type the new code |
| No MyKad Reader icon                 | On Windows, look under **^** near the clock. Otherwise restart the computer |
| Security software removed the app    | Restore it and choose **Allow**, then install again                       |
| "Update required"                    | Download the update and open it once                                      |

## Screenshots

Real screenshots of the Windows and Mac confirm prompts, with the buttons numbered, and of the MyKad Reader icon and menu on both. The estatell-docs plan says MyKad pages use special-case tables, not live reader screenshots (S8). Add an exception for the install prompts.

## Wording

Follow [Words on screen](01-settings-page.md#words-on-screen). Never say "unsigned", "bridge", "SmartScreen" or "quarantine".

## Links

- Link `set-up-mykad-reader` from the onboarding section, so it is part of getting a hotel live.
- The settings page links **How to install ↗** and each **Fix this ↗** here.
