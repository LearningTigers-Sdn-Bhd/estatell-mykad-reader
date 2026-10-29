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
| 5 | Code    | `/pair` without the connect code from the tray                | 422    |

## Allowed origins

- `https://app.estatell.com`, `https://demo.estatell.com`.
- For development: `http(s)://localhost`, `127.0.0.1` and any `*.test`, with any port. These stay allowed in release builds, so a local Estatell can test against an installed MyKad Reader. Reading still needs the token.
- `ESTATELL_BRIDGE_ORIGINS` adds more, comma separated, each `http(s)://host[:port]`. A trailing `/` is ignored. Any other entry is skipped with a warning in the log.
- Matching ignores case and compares the parsed host, so `app.estatell.com.evil.com` and `foo.test.evil.com` are refused.
- `Origin: null` (sandboxed frames, `file://` pages) is refused.

A request with no `Origin` passes check 3, as in 1.x (for example `curl` on the same computer). It still needs the token for `/read` and the connect code for `/pair`.

## Connect code

`/pair` takes no token, because the computer has none yet. Without a second check, any program on the computer could call `/pair` first with a secret it knows, and then read cards. The connect code closes that gap: only a person who sees the [tray menu](04-tray.md#connect-code) knows it. 5 wrong codes lock `/pair`, so the 6 digits cannot be guessed.

## Private Network Access

Chrome asks before an HTTPS page calls `127.0.0.1`. The preflight answer sends `Access-Control-Allow-Private-Network: true` for allowed origins.

An `OPTIONS` request from an allowed origin gets `204` and never reaches a route:

```
Access-Control-Allow-Origin: <origin>
Vary: Origin
Access-Control-Allow-Methods: GET, POST, OPTIONS
Access-Control-Allow-Headers: Authorization, Content-Type
Access-Control-Max-Age: 600
Access-Control-Allow-Private-Network: true    (only when the browser sends Access-Control-Request-Private-Network)
```

A refused origin gets `403` with `code: forbidden` and no CORS headers.

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
- A second copy for the same user exits with code 0. launchd does not restart a clean exit.
- A copy for another user keeps its tray and waits for the port. See [More than one user](04-tray.md#more-than-one-user).
