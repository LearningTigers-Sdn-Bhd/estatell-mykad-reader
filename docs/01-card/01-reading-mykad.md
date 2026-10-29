# Reading a MyKad

## Purpose

Read the holder's details from the JPN application on the MyKad chip and return them in the shape Estatell expects (`MyKadData`).

## Where it lives

`src/Estatell.MyKad/`: `Apdu.cs`, `MyKadCard.cs`, `Decode.cs`, `States.cs`. The project has no networking. The card logic talks to the card only through `IReader`; `PcscReader` and `FakeReader` are in `Reader/` (see [PC/SC access](02-pcsc.md)).

## Read sequence

The sequence and byte positions come from the Python bridge (`mykad_bridge.py` 1.1.0). M0 checks that both give the same JSON for the same card.

| Step                | Command                                                                  | Accept SW1         |
| ------------------- | ------------------------------------------------------------------------ | ------------------ |
| Select JPN          | `00 A4 04 00 0A A0 00 00 00 74 4A 50 4E 00 10`                           | `90`, `61`         |
| Set length          | `C8 32 00 00 05 08 00 00 <len LE16>`                                     | `90`, `91`         |
| Select file         | `CC 00 00 00 08 <file2 LE16> <file1 LE16> <offset LE16> <len LE16>`      | `90`, `91`, `94`   |
| Read binary (chunk) | `CC 06 00 00 <chunk>`, repeated until `len` bytes are read               | `90`, `91`, `94`   |

Files, in read order:

| File   | Length | Holds                 |
| ------ | ------ | --------------------- |
| JPN1-1 | 459    | Identity              |
| JPN1-4 | 171    | Address               |
| JPN1-2 | 4011   | Portrait (JPEG)       |

The portrait is the slowest part. It is read last, so a later `?photo=false` can skip it (M4).

## Read time

Measured in the M0 spike on an ACR39U, macOS, T=0:

| Step              | Time      |
| ----------------- | --------- |
| Connect           | 14–83 ms  |
| JPN1-1 (identity) | 0.48 s    |
| JPN1-4 (address)  | 0.20 s    |
| JPN1-2 (portrait) | 3.93 s    |
| Full read         | about 4.65 s |

- The card sets the speed. Its ATR offers no rate above the default 9600 baud, about 1 KB a second. Code cannot make it faster.
- Without the portrait a read takes about 0.7 s. This makes `?photo=false` (M4) worth more, or filling the form before the portrait arrives.
- The 10-second read timeout and the hook's 12 seconds still fit.

## Fields

Text is ASCII and trimmed.

| Field          | File   | Offset  | Length | Notes                                  |
| -------------- | ------ | ------- | ------ | -------------------------------------- |
| Name           | JPN1-1 | `0x03`  | 150    |                                        |
| IC number      | JPN1-1 | `0x111` | 13     | 12 digits                              |
| Gender         | JPN1-1 | `0x11E` | 1      | `L` → `male`, anything else → `female` |
| Old IC         | JPN1-1 | `0x11F` | 8      | Not returned                           |
| Date of birth  | JPN1-1 | `0x127` | 4      | BCD `YYYYMMDD`                         |
| Place of birth | JPN1-1 | `0x12B` | 25     | Not returned                           |
| Citizenship    | JPN1-1 | `0x148` | 18     | Not returned                           |
| Race           | JPN1-1 | `0x15A` | 25     | Not returned                           |
| Religion       | JPN1-1 | `0x173` | 11     | Not returned                           |
| Address 1      | JPN1-4 | `0x03`  | 30     |                                        |
| Address 2      | JPN1-4 | `0x21`  | 30     |                                        |
| Address 3      | JPN1-4 | `0x3F`  | 30     | Joined to address line 2, see Rules    |
| Postcode       | JPN1-4 | `0x5D`  | 3      | BCD, divide by 10, pad to 5 digits     |
| City           | JPN1-4 | `0x60`  | 25     |                                        |
| State          | JPN1-4 | `0x79`  | 30     | Mapped to an LHDN state code           |
| Portrait       | JPN1-2 | `0x03`  | 4000   | Kept only if it starts with `FF D8`    |

## Rules

- Return only the fields Estatell uses. Race, religion, place of birth, citizenship and the old IC are read but never returned or logged. The Python bridge sends them all; 2.0.0 stops that.
- Address line 3 is joined to line 2 with `, `. The Python bridge sends it as `address_line3`, and the hook drops it, so today the third line is lost.
- City and state are title case, as in the Python bridge.
- `formatted_ic` is `######-##-####`.
- `nationality_code` and `country_code` are always `MY`.
- The state is mapped to the LHDN code with the same table as `EInvoice::MalaysiaStates` in Estatell. If the state text does not match, the first two postcode digits decide (for example 88–91 Sabah, 50–60 Kuala Lumpur). Port the table and the fallback from `STATE_CODES` and `resolve_state_code()` in the Python bridge. No match gives an empty code.
- The portrait is returned as a `data:image/jpeg;base64,…` string, or `null` if it is missing or not a JPEG.
- A failed step throws `CardReadException` with the step name and the status words. The message never holds card data.
- Card data lives in memory only. Byte buffers are cleared after the response is sent.

## Output

`MyKadData`, the same shape as in `estatell/resources/js/hooks/use-mykad-reader.ts`:

```
name, ic_number, formatted_ic, date_of_birth (YYYY-MM-DD), gender,
nationality_code, address_line1, address_line2, city, state,
state_code, postal_code, country_code, photo_base64
```

2.0.0 adds `state_code` (the LHDN code). `state` stays the state name in title case, because the guest forms copy it into `guest.state` today.

The Python bridge sends other names (`raw_ic`, `government_id`, `state_name`, `home_address`) and the hook maps them. 2.0.0 sends the `MyKadData` names only and the hook is cleaned up at the same time. See [App changes](../04-estatell/02-app-changes.md).

## Tests

- Recorded responses for each file in `tests/Estatell.MyKad.Tests/fixtures/`, with personal data replaced.
- One test per field, including an empty portrait, a non-JPEG portrait, an unknown state with a postcode, and each failing status word.
- A parity test: the fixture output must equal the Python bridge output saved next to it.
