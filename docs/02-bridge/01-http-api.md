# HTTP API

## Purpose

The routes the Estatell page calls. Version 2.0.0 changes the 1.x contract on purpose: reading a card needs a token.

## Where it lives

`src/Estatell.MyKad.Bridge/`. ASP.NET Core minimal API with `WebApplication.CreateSlimBuilder`, bound to `127.0.0.1:9292`. JSON uses a `System.Text.Json` source-generated context.

## Routes

| Route                          | Token  | What                                                                                   |
| ------------------------------ | ------ | -------------------------------------------------------------------------------------- |
| `GET /status` (`/`, `/health`) | No     | Reader, card and connection state. Never touches the card                              |
| `POST /pair`                   | No     | Connect this computer to a hotel. Only while not connected. See [Connecting](03-connecting.md) |
| `POST /unpair`                 | Yes    | Disconnect this computer                                                               |
| `GET` or `POST /read`          | Yes    | Read the card                                                                          |
| `OPTIONS *`                    | No     | CORS and Private Network Access preflight                                              |

Every route runs the Host and Origin checks first. See [Security](02-security.md).

## `GET /status`

```json
{
  "online": true,
  "version": "2.0.0",
  "device_id": "d4f3…",
  "paired": true,
  "property_name": "Hotel Seri Kota",
  "reader_connected": true,
  "reader_name": "ACS ACR39U ICC Reader",
  "card_present": false
}
```

- `online`, `version`, `reader_connected`, `reader_name` and `card_present` keep their 1.x names, so a 1.x Estatell page still shows the status.
- `device_id` is random, made on first start, and public. It names the computer, not a secret.
- `property_name` is for display only. It is `null` when not connected.
- The 1.x `message` field is removed. Estatell writes its own wording.

## `GET /read`

Needs `Authorization: Bearer <token>`.

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
| 401  | `not_connected` | No token, wrong token, or not connected      |
| 403  | `forbidden`     | Host or Origin refused                       |
| 409  | `busy`          | Another read did not finish in 10 seconds    |
| 500  | `read_failed`   | A card step failed                           |

- Estatell shows its own wording from `code`. `error` is a plain English sentence for logs and developers. It never holds card data.
- 1.x had no `code`. The hook falls back to `error` for 1.x bridges.

## `POST /pair`

Body: `{ "secret": "…", "code": "482917", "property_name": "Hotel Seri Kota" }`.

`code` is the connect code from the [tray menu](04-tray.md#connect-code), digits only.

| HTTP | `code`              | When                                |
| ---- | ------------------- | ----------------------------------- |
| 200  | —                   | Connected                           |
| 409  | `already_connected` | Disconnect first                    |
| 422  | `invalid`           | Secret missing or shorter than 32 bytes |
| 422  | `wrong_code`        | Connect code missing or wrong       |
| 423  | `locked`            | 5 wrong codes. Select **New connect code** in the tray |

## `POST /unpair`

Needs the token. Clears the stored hash and property name. Returns `200`.

## Command line

| Flag              | What                                                           |
| ----------------- | -------------------------------------------------------------- |
| *(none)*          | Run in the background, as the installers start it              |
| `--foreground`    | Run in a terminal window and print the log. For troubleshooting |
| `--verbose`       | Log each request and each card step (never card data)          |
| `--reset-pairing` | Clear the connection and exit                                  |
| `--no-tray`       | Run without the tray icon. For tests and CI                    |
| `--version`       | Print the version and exit                                     |

## Headers

Every response sends `Cache-Control: no-store` and `Connection: close`. Allowed origins get `Access-Control-Allow-Origin: <origin>` and `Vary: Origin`.
