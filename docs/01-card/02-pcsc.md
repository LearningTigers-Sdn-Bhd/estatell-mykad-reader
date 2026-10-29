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

**Chosen: `PCSC` NuGet 7.0.1** (pcsc-sharp), BSD-2-Clause. Ship its copyright notice with the app. Own `[LibraryImport]` bindings stay the fallback if Windows shows a problem.

The M0 spike (`spikes/PcscAot/`, run on 30 Oct 2026):

| Check                 | osx-arm64                                                  | win-x64 · win-arm64 · win-x86          |
| --------------------- | ---------------------------------------------------------- | -------------------------------------- |
| AOT publish           | Yes. 2.0 MB                                                | CI: `spike-pcsc-aot` workflow           |
| AOT and trim warnings | One: `IL2075` in `SCardHelper.GetAttrDesc`                 | CI                                     |
| Reads a MyKad         | Yes, also on a freshly inserted card                       | Pending: a real x64 Windows PC         |
| Full read             | 4.64 to 4.72 s over 4 runs                                 | Pending                                |

- `IL2075` is safe to suppress. The method only turns an error code into text, and it falls back to "Error code: …" when the description is trimmed away.
- Connecting with `SCardProtocol.Any` to a freshly inserted card fails with `SCARD_W_UNRESPONSIVE_CARD` on macOS. `T0` works. See [Rules](#rules).

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
- Connect with `SCARD_SHARE_EXCLUSIVE` and `SCARD_PROTOCOL_T0` for a read, and disconnect with `SCARD_LEAVE_CARD`. MyKad only offers T=0 (its ATR lists no other protocol). Asking for any protocol fails on the first connect after the card goes in.
- One read at a time. A second `/read` while one is running waits, up to the read timeout.
- Read timeout: 10 seconds. The Estatell hook aborts at 12.
- `GetStatus()` never connects to the card. It uses `SCardGetStatusChange` with a zero timeout.
- No driver is installed. The ACR39U uses the standard USB CCID driver on Windows 10+ and macOS.
