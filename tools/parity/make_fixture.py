#!/usr/bin/env python3
"""
Makes the parity fixture from the raw JPN files of a real card.

1. Replaces every personal field with made-up values of the same shape. Bytes that are not
   a known field keep their padding; any other byte there becomes "X".
2. Runs the Python bridge's own decoder on the cleaned files and saves its output.

Usage:
    python3 tools/parity/make_fixture.py <raw folder> [--bridge <mykad-bridge folder>] [--out <folder>]

The raw folder comes from the RecordsTheJpnFiles hardware test. Keep it outside the repo
and delete it when done.
"""

import argparse
import importlib
import json
import sys
import types
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
DEFAULT_BRIDGE = REPO.parent / "estatell" / "tools" / "mykad-bridge"
DEFAULT_OUT = REPO / "tests" / "Estatell.MyKad.Tests" / "fixtures" / "parity"

PADDING = (0x00, 0x20, 0xFF)

# (offset, length, made-up value). An empty field stays empty.
IDENTITY_TEXT = [
    (0x03, 150, "NUR AISYAH BINTI AHMAD"),  # name
    (0x111, 13, "880515125544"),  # IC
    (0x11F, 8, None),  # old IC
    (0x12B, 25, None),  # place of birth
    (0x148, 18, None),  # citizenship
    (0x15A, 25, None),  # race
    (0x173, 11, None),  # religion
]
IDENTITY_KEPT = [(0x00, 3), (0x11E, 1)]  # header, gender
IDENTITY_DOB = (0x127, bytes([0x19, 0x88, 0x05, 0x15]))

ADDRESS_TEXT = [
    (0x03, 30, "NO 7 JALAN SAMPEL"),
    (0x21, 30, "TAMAN PERCUBAAN"),
    (0x3F, 30, "KAMPUNG CONTOH"),
    (0x60, 25, "KUCHING"),
    (0x79, 30, "SARAWAK"),
]
ADDRESS_KEPT = [(0x00, 3)]
ADDRESS_POSTCODE = (0x5D, "9335")  # first four digits; the fifth and the extra digit stay

PORTRAIT_KEPT = [(0x00, 3)]
PORTRAIT = (0x03, 4000)


def content_range(field):
    """Start and end of the text between the padding, as the decoders trim it."""
    start, end = 0, len(field)
    while start < end and field[start] in (0x00, 0x20):
        start += 1
    while end > start and field[end - 1] in (0x00, 0x20):
        end -= 1
    return start, end


def replace_text(data, done, offset, length, fake):
    field = data[offset:offset + length]
    start, end = content_range(field)
    if start < end:
        value = (fake if fake is not None else "X" * (end - start)).encode("ascii")[:length - start]
        pad = field[end] if end < length else 0x20
        new = bytearray(field)
        new[start:start + len(value)] = value
        for i in range(start + len(value), end):
            new[i] = pad
        data[offset:offset + length] = new
    done.update(range(offset, offset + length))


def replace_portrait(data, done):
    offset, length = PORTRAIT
    if data[offset:offset + 2] != b"\xff\xd8":
        return  # not a JPEG: scrubbed with the unknown bytes
    jpeg = data[offset:offset + length]
    end = jpeg.rfind(b"\xff\xd9")
    end = end + 2 if end > 2 else length
    fake = bytearray(b"\xff\xd8" + bytes((i * 7 + 3) & 0xFF for i in range(end - 4)) + b"\xff\xd9")
    data[offset:offset + end] = fake
    for i in range(offset + end, offset + length):
        if data[i] not in PADDING:
            data[i] = ord("X")
    done.update(range(offset, offset + length))


def scrub_rest(data, done):
    for i in range(len(data)):
        if i not in done and data[i] not in PADDING:
            data[i] = ord("X")


def clean_identity(raw):
    data, done = bytearray(raw), set()
    for offset, length in IDENTITY_KEPT:
        done.update(range(offset, offset + length))
    for offset, length, fake in IDENTITY_TEXT:
        replace_text(data, done, offset, length, fake)
    offset, dob = IDENTITY_DOB
    if any(data[offset:offset + 4]):
        data[offset:offset + 4] = dob
    done.update(range(offset, offset + 4))
    scrub_rest(data, done)
    return data


def clean_address(raw):
    data, done = bytearray(raw), set()
    for offset, length in ADDRESS_KEPT:
        done.update(range(offset, offset + length))
    for offset, length, fake in ADDRESS_TEXT:
        replace_text(data, done, offset, length, fake)
    offset, digits = ADDRESS_POSTCODE
    if any(data[offset:offset + 3]):
        data[offset] = int(digits[:2], 16)
        data[offset + 1] = int(digits[2:], 16)
        data[offset + 2] &= 0x0F  # fifth digit 0, extra digit kept
    done.update(range(offset, offset + 3))
    scrub_rest(data, done)
    return data


def clean_portrait(raw):
    data, done = bytearray(raw), set()
    for offset, length in PORTRAIT_KEPT:
        done.update(range(offset, offset + length))
    replace_portrait(data, done)
    scrub_rest(data, done)
    return data


def check_nothing_left(raw_files, clean_files):
    """Fails if any personal text of four or more characters is still in the cleaned files."""
    identity, address = raw_files["jpn1-1"], raw_files["jpn1-4"]
    fields = [identity[o:o + n] for o, n, _ in IDENTITY_TEXT] + [address[o:o + n] for o, n, _ in ADDRESS_TEXT]
    fields.append(identity[0x99:0x111])  # the short names after the full name
    words = set()
    for field in fields:
        words.update(w for w in field.replace(b"\x00", b" ").split() if len(w) >= 4)
    # Common words such as BINTI or JALAN may be in both the card and the made-up values.
    fakes = {w.encode("ascii") for _, _, fake in IDENTITY_TEXT + ADDRESS_TEXT if fake for w in fake.split()}
    everything = b"".join(clean_files.values())
    left = sorted(w.decode("ascii", "replace") for w in words - fakes if w in everything)
    if left:
        sys.exit(f"Personal text is still in the cleaned files: {len(left)} words. Nothing was saved.")
    if raw_files["jpn1-2"][3:103] in everything:
        sys.exit("Portrait bytes are still in the cleaned files. Nothing was saved.")


class ReplayConnection:
    """Answers the Python bridge's commands from the cleaned files."""

    def __init__(self, files):
        self.files = files
        self.current = b""
        self.position = 0

    def transmit(self, cmd):
        if cmd[0] == 0xCC and cmd[1] == 0x00:
            file2 = cmd[5] | (cmd[6] << 8)
            self.current = self.files[f"jpn1-{file2}"]
            self.position = 0
        elif cmd[0] == 0xCC and cmd[1] == 0x06:
            chunk = self.current[self.position:self.position + cmd[4]]
            self.position += cmd[4]
            return list(chunk), 0x90, 0x00
        return [], 0x90, 0x00


def load_bridge(folder):
    # The decoder needs no reader, so pyscard does not have to be installed.
    smartcard = types.ModuleType("smartcard")
    for name, attrs in {
        "System": {"readers": lambda: []},
        "scard": {"SCARD_SHARE_SHARED": 2, "SCARD_LEAVE_CARD": 0},
        "Exceptions": {"NoCardException": Exception, "CardConnectionException": Exception},
    }.items():
        module = types.ModuleType(f"smartcard.{name}")
        module.__dict__.update(attrs)
        setattr(smartcard, name, module)
        sys.modules[f"smartcard.{name}"] = module
    sys.modules["smartcard"] = smartcard
    sys.path.insert(0, str(folder))
    return importlib.import_module("mykad_bridge")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("raw", type=Path)
    parser.add_argument("--bridge", type=Path, default=DEFAULT_BRIDGE)
    parser.add_argument("--out", type=Path, default=DEFAULT_OUT)
    args = parser.parse_args()

    names = {"jpn1-1": 459, "jpn1-4": 171, "jpn1-2": 4011}
    raw_files = {}
    for name, length in names.items():
        data = (args.raw / f"{name}.bin").read_bytes()
        if len(data) != length:
            sys.exit(f"{name}.bin is {len(data)} bytes, not {length}.")
        raw_files[name] = data

    clean_files = {
        "jpn1-1": bytes(clean_identity(raw_files["jpn1-1"])),
        "jpn1-4": bytes(clean_address(raw_files["jpn1-4"])),
        "jpn1-2": bytes(clean_portrait(raw_files["jpn1-2"])),
    }
    check_nothing_left(raw_files, clean_files)

    bridge = load_bridge(args.bridge)
    output = bridge.MyKadReaderSession(ReplayConnection(clean_files)).read_mykad_data()

    args.out.mkdir(parents=True, exist_ok=True)
    for name, data in clean_files.items():
        (args.out / f"{name}.bin").write_bytes(data)
    (args.out / "python.json").write_text(json.dumps(output, indent=2) + "\n", encoding="utf-8")
    print(f"Saved to {args.out}. Check the hex dumps before committing.")


if __name__ == "__main__":
    main()
