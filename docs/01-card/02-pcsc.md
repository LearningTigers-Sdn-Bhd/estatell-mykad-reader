# PC/SC access

## Purpose

Talk to the USB card reader through the operating system's smart-card service, on Windows and macOS, in a Native AOT build.

## Interface

`IReader` in `src/Estatell.MyKad/` is the only way the card logic reaches a reader.

```
IReader
  ReaderStatus GetStatus()          reader name or none, card present
  byte[] Transmit(byte[] command)   one APDU, returns data + SW1 SW2
  IDisposable Connect()             exclusive session for one read
```

- `PcscReader` is the real one. `FakeReader` replays recorded responses for tests.
- The first reader with a card wins. If no reader has a card, the first reader is reported.

## Library

| Option                    | Use                                                                                                    |
| ------------------------- | ------------------------------------------------------------------------------------------------------ |
| `PCSC` NuGet 7.x (pcsc-sharp) | First choice. Windows, macOS and Linux. Targets `net8.0` and runs on .NET 10                       |
| Own `[LibraryImport]` bindings | Fallback if `PCSC` gives AOT or trim warnings, or fails in an AOT build                           |

The M0 spike decides. See [checklist](../checklist.md).

## Own bindings (fallback)

About seven calls are needed:

```
SCardEstablishContext   SCardListReaders   SCardGetStatusChange
SCardConnect            SCardTransmit      SCardDisconnect
SCardReleaseContext
```

| OS      | Library                                                 | Notes                                                       |
| ------- | ------------------------------------------------------- | ----------------------------------------------------------- |
| Windows | `winscard.dll`                                          | `DWORD` is 32-bit. Use the `W` (UTF-16) reader-name calls   |
| macOS   | `/System/Library/Frameworks/PCSC.framework/PCSC`        | `DWORD` and `LONG` are 32-bit. Reader names are UTF-8       |

Use `[LibraryImport]` (source-generated), not `[DllImport]`, so the build stays AOT-safe. Pick the type sizes per OS at build time.

## Rules

- Open the context once at start-up. Reopen it if a call returns `SCARD_E_SERVICE_STOPPED` or `SCARD_E_NO_SERVICE` (Windows stops the service when the last reader is unplugged).
- Connect with `SCARD_SHARE_EXCLUSIVE` for a read, and disconnect with `SCARD_LEAVE_CARD`.
- One read at a time. A second `/read` while one is running waits, up to the read timeout.
- Read timeout: 10 seconds. The Estatell hook aborts at 12.
- `GetStatus()` never connects to the card. It uses `SCardGetStatusChange` with a zero timeout.
- No driver is installed. The ACR39U uses the standard USB CCID driver on Windows 10+ and macOS.
