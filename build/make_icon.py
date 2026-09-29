"""Thunderstore icon: 256x256 PNG. A dark plate with a golden border, a pale eye whose iris is
water-blue, and a red-and-white fishing float as the pupil: "an eye on your fishing".
Written without PIL, which the build box lacks.
Run from the repo root: python3 build/make_icon.py"""
import math
import struct
import zlib

S = 256
SS = 3                       # supersample
W = S * SS

px = bytearray(W * W * 4)

def blend(x, y, col, a=1.0):
    if x < 0 or y < 0 or x >= W or y >= W:
        return
    i = (y * W + x) * 4
    ia = 1.0 - a
    px[i] = int(col[0] * a + px[i] * ia)
    px[i + 1] = int(col[1] * a + px[i + 1] * ia)
    px[i + 2] = int(col[2] * a + px[i + 2] * ia)
    px[i + 3] = int(min(255, 255 * a + px[i + 3] * ia))

def fill(inside, bbox, col, a=1.0):
    x0, y0, x1, y1 = (int(v * SS) for v in bbox)
    for y in range(max(0, y0), min(W, y1)):
        for x in range(max(0, x0), min(W, x1)):
            if inside((x + 0.5) / SS, (y + 0.5) / SS):
                blend(x, y, col, a)

def rounded(x0, y0, x1, y1, r):
    def f(x, y):
        dx = max(x0 + r - x, 0, x - (x1 - r))
        dy = max(y0 + r - y, 0, y - (y1 - r))
        return x0 <= x < x1 and y0 <= y < y1 and dx * dx + dy * dy <= r * r
    return f

def circle(cx, cy, r):
    return lambda x, y: (x - cx) ** 2 + (y - cy) ** 2 <= r * r

def eye(cx, cy, w, h):
    # Almond: intersection of two circles through the eye corners.
    half = w / 2
    R = (half * half + h * h) / (2 * h)
    return lambda x, y: ((x - cx) ** 2 + (y - (cy + R - h)) ** 2 <= R * R and
                         (x - cx) ** 2 + (y - (cy - R + h)) ** 2 <= R * R)

PLATE = (0x1c, 0x1a, 0x17)
BORDER = (0xc8, 0xa0, 0x50)
WHITE = (0xee, 0xe8, 0xd8)
IRIS = (0x3a, 0x8f, 0xb0)
IRIS_DARK = (0x1f, 0x5a, 0x75)
RED = (0xd8, 0x44, 0x3a)
FLOAT_WHITE = (0xf4, 0xf0, 0xe6)
LINE = (0x9a, 0x94, 0x88)

fill(rounded(0, 0, 256, 256, 34), (0, 0, 256, 256), BORDER)
fill(rounded(10, 10, 246, 246, 26), (0, 0, 256, 256), PLATE)

cx, cy = 128, 132
fill(eye(cx, cy, 200, 70), (20, 55, 236, 210), WHITE)
fill(circle(cx, cy, 52), (70, 75, 190, 190), IRIS_DARK)
fill(circle(cx, cy, 46), (70, 75, 190, 190), IRIS)

# the float as the pupil: white top half, red bottom half, a thin line up to the top edge
fill(lambda x, y: abs(x - cx) <= 1.5 and 30 <= y <= cy - 22, (120, 25, 136, cy), LINE)
fill(lambda x, y: circle(cx, cy, 22)(x, y) and y < cy, (100, 105, 156, 160), FLOAT_WHITE)
fill(lambda x, y: circle(cx, cy, 22)(x, y) and y >= cy, (100, 105, 156, 160), RED)
fill(circle(cx - 8, cy - 9, 5), (110, 115, 130, 130), (255, 255, 255), 0.8)

# downsample
out = bytearray()
for y in range(S):
    row = bytearray([0])
    for x in range(S):
        r = g = b = a = 0
        for sy in range(SS):
            for sx in range(SS):
                i = ((y * SS + sy) * W + (x * SS + sx)) * 4
                r += px[i]; g += px[i + 1]; b += px[i + 2]; a += px[i + 3]
        n = SS * SS
        row += bytes((r // n, g // n, b // n, a // n))
    out += row

def chunk(tag, data):
    c = tag + data
    return struct.pack(">I", len(data)) + c + struct.pack(">I", zlib.crc32(c) & 0xffffffff)

png = b"\x89PNG\r\n\x1a\n"
png += chunk(b"IHDR", struct.pack(">IIBBBBB", S, S, 8, 6, 0, 0, 0))
png += chunk(b"IDAT", zlib.compress(bytes(out), 9))
png += chunk(b"IEND", b"")
open("thunderstore/icon.png", "wb").write(png)
