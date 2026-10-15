"""Draws Hollow Pawns' simple placeholder sprites, in the same flat style as the essence phial.

Usage:  python3 Source/tools/gen_textures.py

Pure Python (no image library): each sprite is a stack of shapes given as signed distance
functions, rendered with 4x4 supersampling and written as RGBA PNG. Re-run after a tweak.
"""
import math
import os
import struct
import zlib

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SS = 4  # supersamples per axis

CYAN = (120, 220, 230)
CYAN_PALE = (200, 245, 250)
GLASS = (215, 240, 245)
CORK = (110, 80, 55)
METAL = (90, 100, 110)
METAL_DARK = (60, 66, 74)
EMBER = (255, 170, 80)


# ---------- shapes: signed distance (negative inside), in pixel units ----------

def rrect(cx, cy, hw, hh, r):
    def d(x, y):
        qx, qy = abs(x - cx) - hw + r, abs(y - cy) - hh + r
        return math.hypot(max(qx, 0), max(qy, 0)) + min(max(qx, qy), 0) - r
    return d


def circle(cx, cy, r):
    return lambda x, y: math.hypot(x - cx, y - cy) - r


def ring(cx, cy, r, w):
    return lambda x, y: abs(math.hypot(x - cx, y - cy) - r) - w


def poly(pts):
    """Convex or concave polygon, by even-odd rule plus distance to edges."""
    def d(x, y):
        best, inside = 1e9, False
        n = len(pts)
        for i in range(n):
            (ax, ay), (bx, by) = pts[i], pts[(i + 1) % n]
            ex, ey = bx - ax, by - ay
            t = max(0, min(1, ((x - ax) * ex + (y - ay) * ey) / (ex * ex + ey * ey)))
            best = min(best, math.hypot(x - ax - ex * t, y - ay - ey * t))
            if (ay > y) != (by > y) and x < ax + (y - ay) * ex / (by - ay):
                inside = not inside
        return -best if inside else best
    return d


def union(*ds):
    return lambda x, y: min(d(x, y) for d in ds)


def intersect(a, b):
    return lambda x, y: max(a(x, y), b(x, y))


# ---------- rendering ----------

def render(size, layers):
    """layers: list of (shape, (r, g, b), alpha) or ('glow', cx, cy, radius, rgb, alpha)."""
    px = [[(0.0, 0.0, 0.0, 0.0)] * size for _ in range(size)]

    def over(dst, rgb, a):
        r, g, b, da = dst
        oa = a + da * (1 - a)
        if oa <= 0:
            return (0.0, 0.0, 0.0, 0.0)
        mix = lambda s, t: (s * a + t * da * (1 - a)) / oa
        return (mix(rgb[0], r), mix(rgb[1], g), mix(rgb[2], b), oa)

    for layer in layers:
        if layer[0] == "glow":
            _, cx, cy, rad, rgb, alpha = layer
            for y in range(size):
                for x in range(size):
                    d = math.hypot(x + 0.5 - cx, y + 0.5 - cy) / rad
                    if d < 1:
                        a = alpha * (1 - d) ** 2
                        px[y][x] = over(px[y][x], rgb, a)
            continue
        shape, rgb, alpha = layer
        for y in range(size):
            for x in range(size):
                if shape(x + 0.5, y + 0.5) > 2:
                    continue
                hits = 0
                for sy in range(SS):
                    for sx in range(SS):
                        if shape(x + (sx + 0.5) / SS, y + (sy + 0.5) / SS) <= 0:
                            hits += 1
                if hits:
                    px[y][x] = over(px[y][x], rgb, alpha * hits / (SS * SS))
    return px


def write_png(path, px):
    size = len(px)
    raw = bytearray()
    for row in px:
        raw.append(0)
        for r, g, b, a in row:
            raw += bytes((round(r), round(g), round(b), round(a * 255)))
    chunk = lambda t, d: struct.pack(">I", len(d)) + t + d + struct.pack(">I", zlib.crc32(t + d) & 0xFFFFFFFF)
    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(bytes(raw), 9)) + chunk(b"IEND", b"")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as f:
        f.write(png)
    print("wrote", os.path.relpath(path, ROOT))


# ---------- sprites (128 x 128) ----------

def soul_jar():
    body = rrect(64, 76, 30, 34, 12)
    return [
        ("glow", 64, 74, 62, CYAN, 0.55),
        (body, GLASS, 0.55),
        (rrect(64, 80, 24, 26, 10), CYAN, 0.35),
        # the soul: a pale wisp with two dark eyes
        (union(circle(64, 70, 13), poly([(51, 72), (77, 72), (72, 98), (64, 90), (56, 98)])), CYAN_PALE, 0.95),
        (circle(59, 68, 2.6), (40, 90, 100), 0.9),
        (circle(69, 68, 2.6), (40, 90, 100), 0.9),
        (rrect(64, 40, 26, 6, 3), METAL, 1.0),
        (rrect(64, 33, 20, 4, 2), METAL_DARK, 1.0),
        (rrect(48, 64, 3, 18, 3), (255, 255, 255), 0.35),  # glass highlight
    ]


def soulfire():
    vial = rrect(64, 72, 17, 34, 8)
    flame = union(circle(64, 84, 11), poly([(53, 84), (75, 84), (64, 52)]))
    core = union(circle(64, 87, 6), poly([(58, 87), (70, 87), (64, 68)]))
    return [
        ("glow", 64, 78, 56, (255, 190, 110), 0.35),
        ("glow", 64, 78, 40, CYAN, 0.45),
        (vial, GLASS, 0.6),
        (flame, CYAN, 0.95),
        (core, EMBER, 0.95),
        (rrect(64, 35, 11, 6, 2), METAL_DARK, 1.0),
        (rrect(64, 25, 2, 8, 1), METAL, 1.0),  # needle
        (rrect(54, 66, 2.5, 18, 2.5), (255, 255, 255), 0.35),
    ]


def borrowed_time():
    top = poly([(42, 34), (86, 34), (66, 64), (62, 64)])
    bottom = poly([(62, 64), (66, 64), (86, 94), (42, 94)])
    sand_top = intersect(top, rrect(64, 54, 30, 8, 0))
    sand_bottom = intersect(bottom, rrect(64, 88, 30, 7, 0))
    return [
        ("glow", 64, 64, 58, CYAN, 0.5),
        (union(top, bottom), GLASS, 0.6),
        (sand_top, CYAN, 0.95),
        (sand_bottom, CYAN, 0.95),
        (rrect(64, 74, 1.2, 12, 1), CYAN_PALE, 0.95),  # falling stream
        (rrect(64, 30, 28, 5, 2), CORK, 1.0),
        (rrect(64, 98, 28, 5, 2), CORK, 1.0),
        (rrect(38, 64, 2.5, 34, 2), CORK, 1.0),
        (rrect(90, 64, 2.5, 34, 2), CORK, 1.0),
    ]


BLOOD = (170, 30, 30)
EMBER_RED = (235, 80, 40)
BONE = (215, 205, 185)
OBSIDIAN = (35, 28, 32)
WHITE = (255, 255, 255)


def scaled(layers, k):
    """Scales a 128px design to 128*k px: shapes and glows alike."""
    out = []
    for layer in layers:
        if layer[0] == "glow":
            _, cx, cy, r, rgb, a = layer
            out.append(("glow", cx * k, cy * k, r * k, rgb, a))
        else:
            shape, rgb, a = layer
            out.append(((lambda s: lambda x, y: s(x / k, y / k) * k)(shape), rgb, a))
    return out


def corrupted_essence():
    return [
        ("glow", 64, 70, 60, EMBER_RED, 0.45),
        (rrect(64, 76, 22, 30, 10), (90, 30, 35), 0.85),
        (rrect(64, 80, 17, 22, 8), BLOOD, 0.9),
        (ring(64, 80, 8, 2.5), OBSIDIAN, 0.9),
        (rrect(64, 40, 8, 8, 2), OBSIDIAN, 1.0),
        (rrect(52, 72, 2.5, 16, 2.5), WHITE, 0.25),
    ]


def cursed_idol():
    horns = union(poly([(50, 40), (56, 46), (44, 22)]), poly([(78, 40), (72, 46), (84, 22)]))
    head = circle(64, 50, 15)
    body = poly([(50, 62), (78, 62), (74, 104), (54, 104)])
    return [
        ("glow", 64, 64, 58, EMBER_RED, 0.3),
        (union(horns, head, body), OBSIDIAN, 1.0),
        (circle(58, 49, 3), EMBER_RED, 1.0),
        (circle(70, 49, 3), EMBER_RED, 1.0),
        (ring(64, 82, 9, 2), (98, 196, 206), 0.9),  # soulsteel wire
        (rrect(64, 100, 18, 4, 2), BONE, 1.0),
    ]


def hell_gate():
    # Designed at 128 and scaled up to 256 (the gate is 3x3 cells).
    return [
        ("glow", 64, 66, 64, EMBER_RED, 0.55),
        (ring(64, 66, 46, 9), (70, 75, 85), 1.0),  # soulsteel ring
        (ring(64, 66, 46, 3), (98, 196, 206), 0.7),
        (circle(64, 66, 36), (60, 8, 10), 1.0),  # the wound
        (circle(64, 66, 26), BLOOD, 0.9),
        (circle(64, 66, 14), EMBER_RED, 0.9),
        (circle(64, 66, 6), (255, 200, 120), 0.9),
        (rrect(64, 116, 40, 6, 3), (55, 58, 66), 1.0),  # base
        (poly([(30, 30), (36, 34), (22, 12)]), BONE, 1.0),  # horns on the ring
        (poly([(98, 30), (92, 34), (106, 12)]), BONE, 1.0),
    ]


def horned_head(fg, eye=None):
    """White-on-transparent silhouette for faction, xenotype and gene icons (the game tints them)."""
    layers = [
        (union(poly([(40, 52), (50, 58), (26, 14)]), poly([(88, 52), (78, 58), (102, 14)]),
               circle(64, 70, 30), poly([(44, 80), (84, 80), (64, 116)])), fg, 1.0),
    ]
    if eye:
        layers += [(circle(52, 68, 5), eye, 1.0), (circle(76, 68, 5), eye, 1.0)]
    return layers


def corrupted_soul_icon():
    return [
        ("glow", 64, 64, 60, EMBER_RED, 0.5),
        (union(circle(64, 56, 22), poly([(42, 60), (86, 60), (78, 104), (64, 92), (50, 104)])), BLOOD, 1.0),
        (circle(56, 54, 4), OBSIDIAN, 1.0),
        (circle(72, 54, 4), OBSIDIAN, 1.0),
        (poly([(48, 40), (54, 44), (40, 18)]), OBSIDIAN, 1.0),
        (poly([(80, 40), (74, 44), (88, 18)]), OBSIDIAN, 1.0),
    ]


def storyteller_portrait():
    return [
        ("glow", 64, 70, 70, EMBER_RED, 0.6),
        (circle(64, 70, 34), (50, 6, 8), 1.0),
        (circle(64, 70, 22), BLOOD, 0.9),
        (union(poly([(44, 50), (52, 56), (30, 12)]), poly([(84, 50), (76, 56), (98, 12)]),
               circle(64, 66, 22), poly([(48, 74), (80, 74), (64, 104)])), OBSIDIAN, 1.0),
        (circle(56, 64, 3.5), EMBER_RED, 1.0),
        (circle(72, 64, 3.5), EMBER_RED, 1.0),
    ]


if __name__ == "__main__":
    tx = os.path.join(ROOT, "Textures")
    t = os.path.join(tx, "Things")
    write_png(os.path.join(t, "Building", "HP_SoulJar.png"), render(128, soul_jar()))
    write_png(os.path.join(t, "Item", "Drug", "HP_Soulfire.png"), render(128, soulfire()))
    write_png(os.path.join(t, "Item", "Drug", "HP_BorrowedTime.png"), render(128, borrowed_time()))
    write_png(os.path.join(t, "Item", "HP_CorruptedEssence.png"), render(128, corrupted_essence()))
    write_png(os.path.join(t, "Item", "Equipment", "HP_CursedIdol.png"), render(128, cursed_idol()))
    write_png(os.path.join(t, "Building", "HP_HellGate.png"), render(256, scaled(hell_gate(), 2)))
    write_png(os.path.join(tx, "World", "HP_DemonFaction.png"), render(128, horned_head(WHITE)))
    write_png(os.path.join(tx, "UI", "Icons", "HP_Demon.png"), render(128, horned_head(WHITE, OBSIDIAN)))
    write_png(os.path.join(tx, "UI", "Icons", "HP_CorruptedSoul.png"), render(128, corrupted_soul_icon()))
    write_png(os.path.join(tx, "UI", "HeroArt", "HP_HellUnbound.png"), render(512, scaled(storyteller_portrait(), 4)))
    write_png(os.path.join(tx, "UI", "HeroArt", "HP_HellUnboundTiny.png"), render(128, storyteller_portrait()))
