# Security

## Purpose

Only an Estatell page open on this computer can read a card. Other computers and other websites cannot.

## Checks

Every request runs these in order. The card is never touched before all pass.

| # | Check   | Refuses                                                       | Answer |
| - | ------- | ------------------------------------------------------------- | ------ |
| 1 | Bind    | Other computers. The server listens on `127.0.0.1` only       | —      |
| 2 | Host    | A `Host` other than `127.0.0.1:9292` or `localhost:9292` (DNS rebinding) | 403 |
| 3 | Origin  | An `Origin` not on the allowlist (other websites)             | 403    |
| 4 | Origin required | `/read` with no `Origin`                              | 403    |

## Allowed origins

- `https://app.estatell.com`, `https://demo.estatell.com`.
- For development: `http(s)://localhost`, `127.0.0.1` and any `*.test`, with any port. Debug builds only. A release build allows them only when `ESTATELL_BRIDGE_ORIGINS` lists them, because no token stands behind the Origin check.
- `ESTATELL_BRIDGE_ORIGINS` adds more, comma separated, each `http(s)://host[:port]`. A trailing `/` is ignored. Any other entry is skipped with a warning in the log.
- Matching ignores case and compares the parsed host, so `app.estatell.com.evil.com` and `foo.test.evil.com` are refused.
- `Origin: null` (sandboxed frames, `file://` pages) is refused.

A request with no `Origin` passes check 3, as in 1.x (for example `curl` on the same computer), so `/status` still works. `/read` refuses it (check 4): browsers send no `Origin` on a plain `GET` from an `<img>` or a link, so without check 4 any website could start a read. It could not see the data, but the card would be read.

## Why no token

The 2.0.0 plan first had pairing: a connect code in the tray, a token from Estatell, a device table. It was dropped on 1 Nov 2026 because it stopped nothing the checks above do not already stop.

| Who tries to read a card                  | Stopped by                                                 |
| ----------------------------------------- | ---------------------------------------------------------- |
| Another computer                          | Bind (check 1)                                             |
| Another website                           | Origin (checks 3 and 4). A web page cannot fake its `Origin` |
| DNS rebinding                             | Host (check 2)                                             |
| A program on this computer                | Nothing. It can use PC/SC directly, with or without MyKad Reader |
| Injected script on an Estatell page (XSS) | Nothing. It could also ask Estatell for a token            |

What 2.0.0 gives up: Estatell does not know which computers have a reader, and an admin cannot cut one computer off. 1.x has neither. Add pairing later only if a hotel needs that.

## Private Network Access

Chrome asks before an HTTPS page calls `127.0.0.1`. The preflight answer sends `Access-Control-Allow-Private-Network: true` for allowed origins.

An `OPTIONS` request from an allowed origin gets `204` and never reaches a route:

```
Access-Control-Allow-Origin: <origin>
Vary: Origin
Access-Control-Allow-Methods: GET, POST, OPTIONS
Access-Control-Allow-Headers: Content-Type
Access-Control-Max-Age: 600
Access-Control-Allow-Private-Network: true    (only when the browser sends Access-Control-Request-Private-Network)
```

A refused origin gets `403` with `code: forbidden` and no CORS headers.

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
- Holds requests (method, path, status, origin), errors and card steps. Never card data.
- A request line has no query string, no body and no header except `Origin`. Control characters in the path or origin are replaced by `?`, so a caller cannot forge a line.
- A successful `/status` poll (every 2.5 seconds) is logged only with `--verbose`. Every other request is logged.
- An exception is logged as its type and message, without the stack.
- macOS writes the file today. The Windows path comes with the Windows item.

## Single instance

- On Windows the socket uses `SO_EXCLUSIVEADDRUSE`, so two copies cannot share the port.
- A second copy for the same user exits with code 0. launchd does not restart a clean exit.
- macOS holds an exclusive lock on `~/Library/Application Support/Estatell/mykad-bridge.lock`. The system drops the lock when the process ends, so a crash leaves no stale lock. If the lock file cannot be created, the copy starts without a lock.
- Before it serves, a copy checks that the port is free. If not, it logs a warning and checks every 30 seconds. It cannot be stopped during that wait until the tray item adds the signal handling.
- Windows uses a `Local\` mutex instead of the lock file. The handle closes when the process ends, so a crash leaves no stale lock.
- A copy for another user keeps its tray and waits for the port. See [More than one user](04-tray.md#more-than-one-user).
