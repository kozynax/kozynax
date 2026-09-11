#!/usr/bin/env python3
"""Build kozynax.ico from the authored PNGs next to this script.

Small sizes (16/32/48/64) are 32-bit DIB so System.Drawing.Icon on net46
can load them. 256 is PNG-in-ICO (Vista+). Do not feed a PNG-only ICO to
WinForms — GDI+ on .NET Framework often rejects PNG frames except 256.
"""
from __future__ import annotations

import struct
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent
OUT = ROOT / "kozynax.ico"
# (filename, store as PNG-in-ICO)
FRAMES = (
    ("16x16.png", False),
    ("32x32.png", False),
    ("48x48.png", False),
    ("64x64.png", False),
    ("256x256.png", True),
)


def dib32(im: Image.Image) -> bytes:
    im = im.convert("RGBA")
    w, h = im.size
    pixels = im.tobytes()
    row = w * 4
    xor_rows = []
    for y in range(h - 1, -1, -1):
        src = pixels[y * row : (y + 1) * row]
        out = bytearray(row)
        for x in range(0, row, 4):
            r, g, b, a = src[x : x + 4]
            out[x : x + 4] = bytes((b, g, r, a))
        xor_rows.append(bytes(out))
    xor = b"".join(xor_rows)

    stride = ((w + 31) // 32) * 4
    mask_rows = []
    for y in range(h - 1, -1, -1):
        bits = 0
        count = 0
        rowb = bytearray(stride)
        pos = 0
        for x in range(w):
            a = pixels[(y * w + x) * 4 + 3]
            bits = (bits << 1) | (1 if a == 0 else 0)
            count += 1
            if count == 8:
                rowb[pos] = bits
                pos += 1
                bits = 0
                count = 0
        if count:
            bits <<= 8 - count
            rowb[pos] = bits
        mask_rows.append(bytes(rowb))
    and_mask = b"".join(mask_rows)

    header = struct.pack(
        "<IiiHHIIiiII",
        40,
        w,
        h * 2,
        1,
        32,
        0,
        len(xor) + len(and_mask),
        0,
        0,
        0,
        0,
    )
    return header + xor + and_mask


def main() -> None:
    items: list[tuple[int, int, bytes]] = []
    for name, as_png in FRAMES:
        path = ROOT / name
        if not path.is_file():
            raise SystemExit(f"missing {path}")
        im = Image.open(path)
        w, h = im.size
        data = path.read_bytes() if as_png else dib32(im)
        kind = "PNG" if as_png else "DIB"
        print(f"  + {name} {w}x{h} {kind} {len(data)}B")
        items.append((w, h, data))

    header = struct.pack("<HHH", 0, 1, len(items))
    offset = 6 + 16 * len(items)
    directory = b""
    payload = b""
    for w, h, data in items:
        directory += struct.pack(
            "<BBBBHHII",
            0 if w >= 256 else w,
            0 if h >= 256 else h,
            0,
            0,
            1,
            32,
            len(data),
            offset,
        )
        offset += len(data)
        payload += data

    OUT.write_bytes(header + directory + payload)
    print(f"wrote {OUT} ({OUT.stat().st_size} bytes)")


if __name__ == "__main__":
    main()
