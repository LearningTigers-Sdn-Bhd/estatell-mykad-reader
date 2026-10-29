# Settings page

## Purpose

One page in Estatell, **Settings → MyKad Reader**, where an admin installs MyKad Reader on a computer, connects it to the hotel, and manages connected computers. Check-in only shows a status.

## Permission

**Manage MyKad reader** (`mykad-reader.manage`). Owners and managers have it by default. Front desk, finance and housekeeping do not.

## Setup steps

The page calls `GET /status` on load and every 2.5 seconds, and shows the first step that is not done.

```
 ① Download             ② Install                  ③ Connect                  ✓ All set
 [Download for Windows]  "Open the file and follow   Name this computer          Insert any MyKad
 Using a Mac? …          the steps.                  [Front Desk 1]              to try it.
                         Need help? How to install ↗" Connect code               Nothing is saved.
                         Waiting for MyKad Reader… ◌  [482-917]
                         (page moves on by itself)    Find it in the MyKad Reader
                                                      icon (tray picture ↗)
                                                      [Connect to <hotel>]
```

| Step     | Shown when                     | Content                                                                                       |
| -------- | ------------------------------ | --------------------------------------------------------------------------------------------- |
| Download | `/status` does not answer      | One button for the detected OS, a link for the other OS, the supported systems                 |
| Install  | After a download, until `/status` answers | "Open the file and follow the steps." and **How to install ↗** to the user docs. Waits by itself |
| Connect  | `/status` answers, `paired: false` | Computer name (required, 40 characters), connect code (6 digits, from the tray menu), **Connect to {hotel}**. A small picture shows where the icon is on Windows and on a Mac |
| All set  | Connected to this hotel        | A try-it read. Shows the name and a masked IC. Saves nothing                                   |

## Other states

| State                              | Message                                                                                         |
| ---------------------------------- | ----------------------------------------------------------------------------------------------- |
| App not running after install      | "MyKad Reader isn't running. Restart the computer, or download it again."                       |
| No reader                          | "Plug the card reader into this computer."                                                      |
| Connected to another hotel         | "This computer is connected to another hotel. Ask that hotel's admin to disconnect it first."    |
| Wrong connect code (`wrong_code`)  | "That code doesn't match. Check the MyKad Reader icon and try again."                           |
| Too many wrong codes (`locked`)    | "Too many tries. Click the MyKad Reader icon, choose New connect code, and type the new code."  |
| Update available                   | "A newer version of MyKad Reader is available. [Download update] It keeps working while you wait." |
| Update required (1.x)              | "Please update MyKad Reader to keep reading MyKad. [Download update]"                           |

Each state has a **Fix this ↗** link to the problems page in the user docs.

## Connected computers

```
 Name          OS       Version   Last seen
 Front Desk 1  Windows  2.0.0     just now     This computer
 Front Desk 2  Windows  1.1.0 ⚠   2 days ago   [Disconnect]
```

- **Rename** and **Disconnect** on each row.
- Disconnect asks first: "Disconnect Front Desk 2? Staff on that computer won't be able to read MyKad until it's connected again."
- **Last seen** is updated when any Estatell page on that computer checks MyKad Reader. At most once every 5 minutes per computer.

## Check-in chip

`MykadScanner` becomes a status chip. It has no download button.

| State                          | Chip                                   | On click                                                                                  |
| ------------------------------ | -------------------------------------- | ----------------------------------------------------------------------------------------- |
| Connected, reader ready        | ● Ready for MyKad                      | —                                                                                         |
| Connected, no reader           | ○ Plug in card reader                  | —                                                                                         |
| Not installed or not connected | ○ Card reading not set up              | Admin: opens Settings → MyKad Reader. Others: "Ask your manager to set up card reading on this computer." |
| Connected to another hotel     | ⚠ Connected to another hotel          | Same as above                                                                             |
| Update available               | ● Ready for MyKad · Update             | Admin: opens the settings page                                                            |
| Update required                | ⚠ Update required                     | Same as above                                                                             |

## Words on screen

Screens are for hotel owners and staff. Use plain, calm words.

| Avoid                                              | Use instead                                    |
| -------------------------------------------------- | ---------------------------------------------- |
| Bridge                                             | MyKad Reader                                   |
| Pair, unpair, secret, token                        | Connect, disconnect this computer              |
| Unsigned installer, code signing                   | *(never mention it)*                           |
| SmartScreen, Gatekeeper, "Windows protected your PC" | "Windows will ask you to confirm"            |
| Antivirus, quarantine                              | "If your security software asks, choose Allow" |
| Offline, port, localhost, version mismatch         | "isn't running", "Update available"            |
| Device ID, 401, revoked                            | *(never shown)*                                |
