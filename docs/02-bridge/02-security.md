# Security

## Purpose

Only the Estatell page of the connected hotel, on this computer, can read a card.

## Checks

Every request runs these in order. The card is never touched before all pass.

| # | Check   | Refuses                                                       | Answer |
| - | ------- | ------------------------------------------------------------- | ------ |
| 1 | Bind    | Other computers. The server listens on `127.0.0.1` only       | —      |
| 2 | Host    | A `Host` other than `127.0.0.1:9292` or `localhost:9292` (DNS rebinding) | 403 |
| 3 | Origin  | An `Origin` not on the allowlist (other websites)             | 403    |
| 4 | Token   | `/read` and `/unpair` without the right token                 | 401    |

## Allowed origins

- `https://app.estatell.com`, `https://demo.estatell.com`.
- For development: `http(s)://localhost`, `127.0.0.1` and any `*.test`, with any port.
- `ESTATELL_BRIDGE_ORIGINS` adds more, comma separated.

A request with no `Origin` passes check 3, as in 1.x (for example `curl` on the same computer). It still needs the token for `/read`.

## Private Network Access

Chrome asks before an HTTPS page calls `127.0.0.1`. The preflight answer sends `Access-Control-Allow-Private-Network: true` for allowed origins.

## Token

- The token is the secret made by Estatell when the computer connects. See [Connecting](03-connecting.md).
- MyKad Reader stores only `SHA-256(secret)` and compares with a constant-time check.
- The connection belongs to the computer, not to one Windows or macOS user. Front desks share a computer between staff accounts.
- MyKad Reader runs as the logged-in user, not as an administrator. So the installer makes a shared folder that every local user can write:

| OS      | File                                                                    | Protection                       |
| ------- | ----------------------------------------------------------------------- | -------------------------------- |
| Windows | `%ProgramData%\Estatell\MyKadReader\pairing.dat`                        | DPAPI, machine scope             |
| macOS   | `/Library/Application Support/Estatell/MyKadReader/pairing.json`        | File mode `0660`, group `staff`  |

- The file holds the hash, the property name and `device_id`. The hash of a random 32-byte secret cannot be turned back into the token.
- A local user can delete the file. That only disconnects the computer. Connecting again needs a new secret from Estatell, which needs **Manage MyKad reader** in Estatell.
- The browser gets the token from Estatell for each session and keeps it in memory, never in `localStorage`.

## Card data

- Lives in memory only. Buffers are cleared after the response.
- Never written to disk or to the log.
- Only the fields in `MyKadData` leave the app. See [Reading a MyKad](../01-card/01-reading-mykad.md#rules).

## Logs

| OS      | Path                                                     |
| ------- | -------------------------------------------------------- |
| Windows | `%LOCALAPPDATA%\Estatell\estatell-mykad-bridge.log`      |
| macOS   | `~/Library/Logs/estatell-mykad-bridge.log`               |

- Same paths as 1.x.
- 1 MB per file, plus 2 old files.
- Holds requests (route, status, origin), errors and card steps. Never card data, the secret or the token.

## Single instance

- On Windows the socket uses `SO_EXCLUSIVEADDRUSE`, so two copies cannot share the port.
- If the port is taken, the new copy exits with code 0. launchd does not restart a clean exit.
