# HTTP API

## Purpose

The routes the Estatell page calls. They keep the 1.x routes and field names. 2.0.0 adds an error `code` and makes `/read` need an allowed `Origin`.

## Where it lives

`src/Estatell.MyKad.Bridge/`. ASP.NET Core minimal API with `WebApplication.CreateSlimBuilder`, bound to `127.0.0.1:9292`. JSON uses a `System.Text.Json` source-generated context.

## Routes

| Route                          | `Origin`          | What                                                    |
| ------------------------------ | ----------------- | ------------------------------------------------------- |
| `GET /status` (`/`, `/health`) | Allowed or none   | Reader and card state. Never touches the card           |
| `GET` or `POST /read`          | Allowed, required | Read the card                                           |
| `OPTIONS *`                    | Allowed           | CORS and Private Network Access preflight               |

Every route runs the Host and Origin checks first. See [Security](02-security.md).

## `GET /status`

```json
{
  "online": true,
  "version": "2.0.0",
  "reader_connected": true,
  "reader_name": "ACS ACR39U ICC Reader",
  "card_present": false
}
```

- All fields keep their 1.x names, so a 1.x Estatell page still shows the status.
- A `/status` request with an allowed `Origin` marks Estatell as seen for the [tray](04-tray.md#menu).
- The 1.x `message` field is removed. Estatell writes its own wording.

## `GET /read`

Needs an allowed `Origin`. A request with no `Origin` gets `403`. For `curl`, send `-H 'Origin: https://app.estatell.com'`.

Success:

```json
{ "success": true, "data": { "name": "…", "ic_number": "…", "…": "…" } }
```

`data` is `MyKadData`. See [Reading a MyKad](../01-card/01-reading-mykad.md#output).

Failure:

```json
{ "success": false, "code": "no_card", "error": "No card in the reader." }
```

| HTTP | `code`          | When                                         |
| ---- | --------------- | -------------------------------------------- |
| 400  | `no_reader`     | No reader plugged in                         |
| 400  | `no_card`       | No card in the reader                        |
| 403  | `forbidden`     | Host or Origin refused, or `/read` with no `Origin` |
| 409  | `busy`          | Another read did not finish in 10 seconds    |
| 500  | `read_failed`   | A card step failed                           |

- Estatell shows its own wording from `code`. `error` is a plain English sentence for logs and developers. It never holds card data.
- 1.x had no `code`. The hook falls back to `error` for 1.x bridges.

## Command line

| Flag              | What                                                           |
| ----------------- | -------------------------------------------------------------- |
| *(none)*          | Run in the background, as the installers start it              |
| `--foreground`    | Run in a terminal window and print the log. For troubleshooting |
| `--verbose`       | Log each request and each card step (never card data)          |
| `--no-tray`       | Run without the tray icon. For tests and CI                    |
| `--version`       | Print the version and exit                                     |

## Headers

Every response sends `Cache-Control: no-store` and `Connection: close`. Allowed origins get `Access-Control-Allow-Origin: <origin>` and `Vary: Origin`.
