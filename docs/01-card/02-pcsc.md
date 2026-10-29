# PC/SC access

## Purpose

Talk to the USB card reader through the operating system's smart-card service, on Windows and macOS, in a Native AOT build.

## Interface

`IReader` in `src/Estatell.MyKad/Reader/` is the only way the card logic reaches a reader.

```
IReader
  ReaderStatus GetStatus()                        reader name or null, card present
  IReaderSession Connect(CancellationToken)       exclusive session for one read

IReaderSession : IDisposable                      dispose = disconnect, leave the card powered
  ResponseApdu Transmit(ReadOnlySpan<byte>)       one APDU, returns data + SW1 SW2

ReaderException.Code   NoReader | NoCard | Busy | Unavailable
```

- `PcscReader` is the real one. `FakeReader` takes the expected commands and their responses, and records what was sent. Both test projects use it.
- The first reader with a card wins. If no reader has a card, the first reader is reported.
- The token stops a read: `Connect` throws `Busy` if it fires while another read runs, and `Transmit` throws `OperationCanceledException` once it has fired. One APDU takes 250 ms at most, so a read stops within that.
- The bridge maps `NoReader` to `no_reader`, `NoCard` to `no_card`, `Busy` to `busy`, and `Unavailable` to `read_failed`.

## Library

**Chosen: `PCSC` NuGet 7.0.1** (pcsc-sharp), BSD-2-Clause. Ship its copyright notice with the app. Own `[LibraryImport]` bindings stay the fallback if Windows shows a problem.

The M0 spike (`spikes/PcscAot/`, run on 30 Oct 2026):

| Check                 | osx-arm64                                                  | win-x64 · win-arm64 · win-x86          |
| --------------------- | ---------------------------------------------------------- | -------------------------------------- |
| AOT publish           | Yes. 2.0 MB                                                | Yes. x64 2.0 MB, arm64 2.0 MB, x86 1.7 MB |
| AOT and trim warnings | One: `IL2075` in `SCardHelper.GetAttrDesc`                 | Same, on all three                     |
| Starts and reaches the smart-card service | Yes                                    | x64 and x86 on the CI runner: `SCARD_E_NO_SERVICE`, as expected with no reader. arm64 not run |
| Reads a MyKad         | Yes, also on a freshly inserted card                       | Pending: a real x64 Windows PC         |
| Full read             | 4.64 to 4.72 s over 4 runs                                 | Pending                                |

- Windows results come from the `spike-pcsc-aot` workflow on `windows-latest`. Download the `.exe` from its run to try a read on a Windows PC.
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

- PC/SC contexts should not be shared across threads. `GetStatus()` keeps one context and reopens it once if a call returns `SCARD_E_SERVICE_STOPPED` or `SCARD_E_NO_SERVICE` (Windows stops the service when the last reader is unplugged). Each read opens its own context and releases it on disconnect.
- Connect with `SCARD_SHARE_EXCLUSIVE` and `SCARD_PROTOCOL_T0` for a read, and disconnect with `SCARD_LEAVE_CARD`. MyKad only offers T=0 (its ATR lists no other protocol). Asking for any protocol fails on the first connect after the card goes in.
- One read at a time. A second `/read` while one is running waits, up to the read timeout.
- Read timeout: 10 seconds. The Estatell hook aborts at 12.
- `GetStatus()` never connects to the card. It uses `SCardGetStatusChange` with a zero timeout.
- No driver is installed. The ACR39U uses the standard USB CCID driver on Windows 10+ and macOS.
