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

def render(size, layers, height=None):
    """layers: list of (shape, (r, g, b), alpha) or ('glow', cx, cy, radius, rgb, alpha).
    size is the width; height defaults to square."""
    width, height = size, height or size
    px = [[(0.0, 0.0, 0.0, 0.0)] * width for _ in range(height)]

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
            for y in range(height):
                for x in range(width):
                    d = math.hypot(x + 0.5 - cx, y + 0.5 - cy) / rad
                    if d < 1:
                        a = alpha * (1 - d) ** 2
                        px[y][x] = over(px[y][x], rgb, a)
            continue
        shape, rgb, alpha = layer
        for y in range(height):
            for x in range(width):
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
    w, h = len(px[0]), len(px)
    raw = bytearray()
    for row in px:
        raw.append(0)
        for r, g, b, a in row:
            raw += bytes((round(r), round(g), round(b), round(a * 255)))
    chunk = lambda t, d: struct.pack(">I", len(d)) + t + d + struct.pack(">I", zlib.crc32(t + d) & 0xFFFFFFFF)
    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(bytes(raw), 9)) + chunk(b"IEND", b"")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as f:
        f.write(png)
    print("wrote", os.path.relpath(path, ROOT))


# ---------- sprites (128 x 128) ----------

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


STONE = (125, 120, 112)
STONE_DARK = (88, 84, 78)
CLAY = (150, 95, 65)
CLAY_DARK = (110, 68, 45)
STEEL = (120, 128, 136)
STEEL_DARK = (70, 76, 84)
FLESH = (120, 55, 50)
FLESH_DARK = (80, 35, 32)
WIRE = (60, 60, 60)
GREEN = (110, 200, 120)
SILVER = (200, 210, 220)
WATER = (130, 190, 235)
PURPLE = (120, 30, 90)


def urn(filled):
    body = union(circle(64, 80, 30), rrect(64, 54, 16, 12, 6))
    layers = [("glow", 64, 74, 60, CYAN, 0.45)] if filled else []
    layers += [
        (body, CLAY, 1.0),
        (rrect(64, 96, 24, 3, 1.5), CLAY_DARK, 0.9),
        (rrect(64, 40, 20, 6, 3), (90, 90, 95), 1.0),  # lead seal
        (rrect(64, 33, 12, 3, 1.5), (180, 150, 70), 1.0),  # wax
    ]
    if filled:
        layers += [(ring(64, 78, 14, 2.5), CYAN_PALE, 0.9), (circle(64, 78, 5), CYAN_PALE, 0.9)]
    return layers


def canister(filled):
    layers = [("glow", 64, 64, 58, CYAN, 0.5)] if filled else []
    layers += [
        (rrect(64, 66, 26, 44, 8), STEEL, 1.0),
        (rrect(64, 66, 16, 30, 5), CYAN if filled else (60, 70, 80), 0.95 if filled else 0.7),
        (rrect(64, 24, 18, 6, 2), STEEL_DARK, 1.0),
        (rrect(64, 108, 18, 6, 2), STEEL_DARK, 1.0),
        (circle(84, 34, 6), (230, 230, 220), 1.0),  # gauge
        (rrect(84, 33, 0.8, 4, 0.5), (40, 40, 40), 1.0),
    ]
    if filled:
        layers.append((rrect(58, 60, 2.5, 20, 2.5), WHITE, 0.35))
    return layers


def lattice(filled):
    hexagon = poly([(64, 18), (100, 40), (100, 88), (64, 110), (28, 88), (28, 40)])
    inner = poly([(64, 34), (86, 48), (86, 80), (64, 94), (42, 80), (42, 48)])
    layers = [("glow", 64, 64, 62, CYAN, 0.6)] if filled else []
    layers += [
        (hexagon, (200, 230, 240) if filled else (170, 180, 190), 0.55),
        (ring(64, 64, 30, 1.5), (98, 196, 206), 0.9),
        (inner, CYAN if filled else (150, 160, 170), 0.85 if filled else 0.5),
        (poly([(64, 34), (64, 94), (60, 64)]), WHITE, 0.4),
    ]
    return layers


def holy_water():
    return [
        ("glow", 64, 74, 46, WATER, 0.35),
        (union(circle(64, 82, 24), rrect(64, 52, 9, 12, 3)), (215, 235, 245), 0.6),
        (circle(64, 86, 18), WATER, 0.9),
        (rrect(64, 36, 10, 5, 2), SILVER, 1.0),
        (rrect(64, 86, 2, 9, 1), WHITE, 0.9),  # a small cross
        (rrect(64, 83, 7, 2, 1), WHITE, 0.9),
    ]


def carrion_bale():
    return [
        (rrect(64, 70, 46, 34, 10), FLESH, 1.0),
        (circle(44, 62, 10), FLESH_DARK, 0.8),
        (circle(82, 80, 12), FLESH_DARK, 0.8),
        (rrect(40, 70, 3, 36, 1), WIRE, 1.0),
        (rrect(88, 70, 3, 36, 1), WIRE, 1.0),
        (rrect(64, 52, 30, 2.5, 1), BONE, 0.9),
        (circle(30, 52, 4), BONE, 0.9),
    ]


def carrion_wall():
    return [
        (rrect(64, 64, 62, 62, 8), FLESH_DARK, 1.0),
        (circle(36, 40, 22), FLESH, 1.0),
        (circle(88, 46, 24), FLESH, 1.0),
        (circle(56, 90, 26), FLESH, 1.0),
        (circle(98, 96, 16), FLESH, 0.9),
        (rrect(60, 60, 30, 3, 1.5), BONE, 0.9),
        (circle(28, 60, 5), BONE, 0.9),
        (rrect(80, 100, 3, 18, 1.5), BONE, 0.8),
    ]


def bone_altar():
    # 256 x 128: a 2x1 slab.
    return [
        ("glow", 128, 64, 80, EMBER_RED, 0.35),
        (rrect(128, 66, 116, 46, 10), STONE, 1.0),
        (rrect(128, 66, 104, 34, 6), STONE_DARK, 1.0),
        (rrect(128, 66, 96, 3, 1.5), BLOOD, 0.95),  # blood channel
        (rrect(64, 66, 3, 26, 1.5), BLOOD, 0.9),
        (rrect(192, 66, 3, 26, 1.5), BLOOD, 0.9),
        (circle(22, 66, 12), BONE, 1.0),
        (circle(234, 66, 12), BONE, 1.0),
        (circle(18, 63, 2.5), OBSIDIAN, 1.0),
        (circle(26, 63, 2.5), OBSIDIAN, 1.0),
        (circle(230, 63, 2.5), OBSIDIAN, 1.0),
        (circle(238, 63, 2.5), OBSIDIAN, 1.0),
    ]


def soul_press():
    return [
        ("glow", 128, 120, 120, CYAN, 0.45),
        (rrect(128, 132, 110, 110, 14), STEEL_DARK, 1.0),
        (ring(128, 120, 76, 10), STEEL, 1.0),
        (ring(128, 120, 54, 6), (98, 196, 206), 0.9),
        (circle(128, 120, 34), (40, 70, 80), 1.0),
        (circle(128, 120, 22), CYAN, 0.9),
        (circle(128, 120, 10), CYAN_PALE, 1.0),
        (rrect(128, 232, 90, 10, 4), STEEL, 1.0),
        (rrect(40, 40, 10, 10, 3), STEEL, 1.0),
        (rrect(216, 40, 10, 10, 3), STEEL, 1.0),
    ]


def reanimation_slab():
    return [
        ("glow", 128, 64, 80, GREEN, 0.3),
        (rrect(128, 66, 116, 44, 8), STEEL, 1.0),
        (rrect(128, 66, 100, 30, 5), (80, 95, 90), 1.0),
        (rrect(128, 66, 90, 2, 1), GREEN, 0.8),
        (circle(20, 40, 6), GREEN, 1.0),
        (circle(20, 92, 6), GREEN, 1.0),
        (rrect(236, 66, 10, 30, 3), STEEL_DARK, 1.0),
    ]


def corpse_grinder():
    teeth = union(*[poly([(70 + i * 24, 108), (82 + i * 24, 108), (76 + i * 24, 128)]) for i in range(5)])
    return [
        (rrect(128, 128, 120, 120, 16), STEEL_DARK, 1.0),
        (rrect(128, 118, 92, 82, 10), (40, 40, 44), 1.0),
        (rrect(128, 120, 70, 50, 8), FLESH_DARK, 1.0),
        (teeth, STEEL, 1.0),
        (circle(100, 150, 10), BLOOD, 0.8),
        (circle(156, 140, 7), BLOOD, 0.7),
        (rrect(128, 236, 50, 10, 4), STEEL, 1.0),
        (circle(30, 30, 8), EMBER_RED, 1.0),
    ]


def line_forge():
    # 256 x 128: a 4x2 production line.
    rollers = union(*[circle(36 + i * 23, 92, 7) for i in range(9)])
    return [
        (rrect(128, 66, 124, 58, 8), STEEL_DARK, 1.0),
        (rrect(128, 92, 110, 10, 4), (50, 52, 56), 1.0),
        (rollers, STEEL, 1.0),
        (rrect(70, 40, 20, 22, 4), STEEL, 1.0),  # press heads
        (rrect(128, 40, 20, 22, 4), STEEL, 1.0),
        (rrect(186, 40, 20, 22, 4), STEEL, 1.0),
        (rrect(128, 18, 100, 4, 2), (230, 160, 60), 1.0),  # warning stripe
        (circle(240, 20, 6), (230, 160, 60), 1.0),
    ]


def clone_vat():
    # 192 x 128: a 3x2 vat.
    figure = union(circle(96, 44, 10), rrect(96, 76, 12, 22, 6))
    return [
        ("glow", 96, 64, 80, GREEN, 0.4),
        (rrect(96, 64, 90, 58, 12), STEEL_DARK, 1.0),
        (rrect(96, 62, 76, 44, 10), (60, 120, 80), 0.95),
        (figure, (40, 70, 55), 0.85),
        (rrect(96, 122, 70, 5, 2), STEEL, 1.0),
        (circle(14, 20, 5), GREEN, 1.0),
        (rrect(70, 50, 3, 22, 3), WHITE, 0.25),
    ]


def corruption_icon():
    return [
        (union(circle(64, 70, 34), circle(40, 50, 18), circle(90, 44, 16), circle(98, 90, 14)), PURPLE, 1.0),
        (circle(64, 70, 14), (200, 60, 150), 1.0),
    ]


def soulless_icon():
    return [
        (ring(64, 58, 30, 6), WHITE, 1.0),
        (rrect(64, 100, 22, 14, 6), WHITE, 1.0),
        (circle(52, 56, 5), WHITE, 1.0),
        (circle(76, 56, 5), WHITE, 1.0),
    ]


def cleanse_icon():
    drop = union(circle(64, 76, 30), poly([(36, 66), (92, 66), (64, 14)]))
    return [
        (drop, WHITE, 1.0),
        (rrect(64, 78, 4, 18, 1.5), WATER, 1.0),
        (rrect(64, 74, 12, 4, 1.5), WATER, 1.0),
    ]


# ---------- v0.8: reliquary, shrines ----------

def reliquary():
    return [
        ("glow", 64, 54, 50, CYAN, 0.35),
        (rrect(64, 108, 44, 12, 4), STONE_DARK, 1.0),  # plinth
        (rrect(64, 60, 34, 42, 6), STONE, 1.0),  # case
        (rrect(64, 58, 26, 34, 4), GLASS, 0.55),  # glass front
        (circle(64, 62, 12), CYAN_PALE, 0.9),  # the vessel's glow inside
        (rrect(64, 22, 38, 6, 3), STONE_DARK, 1.0),
    ]


def bound_shrine():
    hollow = circle(64, 56, 13)
    return [
        ("glow", 64, 56, 54, (200, 220, 240), 0.35),
        (poly([(30, 118), (98, 118), (88, 20), (64, 8), (40, 20)]), STONE, 1.0),
        (poly([(36, 118), (92, 118), (84, 26), (64, 16), (44, 26)]), STONE_DARK, 0.35),
        (hollow, OBSIDIAN, 1.0),
        (circle(64, 56, 7), (220, 235, 250), 0.9),
        (rrect(64, 96, 18, 3, 1.5), BONE, 0.8),
    ]


def essence_shrine():
    bowl = union(rrect(64, 70, 40, 12, 10), poly([(28, 66), (100, 66), (84, 88), (44, 88)]))
    return [
        ("glow", 64, 52, 56, CYAN, 0.5),
        (rrect(64, 106, 10, 20, 3), METAL_DARK, 1.0),
        (rrect(64, 122, 30, 5, 2), METAL_DARK, 1.0),
        (bowl, METAL, 1.0),
        (union(circle(64, 50, 16), poly([(50, 54), (78, 54), (64, 18)])), CYAN, 0.85),
        (circle(64, 54, 8), CYAN_PALE, 1.0),
    ]


def prisoner_battery():
    """2x1 steel casket with a window: a figure lying inside, soul light leaking out."""
    body = union(circle(76, 64, 10), rrect(130, 64, 46, 12, 8))
    return [
        ("glow", 128, 64, 90, CYAN, 0.35),
        (rrect(128, 66, 120, 50, 10), METAL_DARK, 1.0),
        (rrect(128, 64, 108, 38, 8), METAL, 1.0),
        (rrect(124, 64, 86, 26, 6), (40, 60, 66), 1.0),
        (body, CYAN, 0.55),
        (rrect(224, 64, 12, 34, 4), METAL_DARK, 1.0),
        (circle(224, 50, 4), CYAN_PALE, 1.0),
        (circle(224, 64, 4), CYAN, 1.0),
        (circle(224, 78, 4), CYAN, 0.5),
        (rrect(128, 112, 112, 3, 1), (40, 44, 50), 1.0),
    ]


def effigy(wailing):
    """A kneeling soulsteel figure on a plinth; the wailing one has its head thrown back, mouth open."""
    steel = (70, 82, 96)
    figure = union(
        circle(64, 34 if not wailing else 30, 13),
        poly([(46, 50), (82, 50), (88, 86), (40, 86)]),
        rrect(64, 92, 30, 10, 6),
        poly([(46, 54), (34, 74), (40, 78), (52, 62)]),
        poly([(82, 54), (94, 74), (88, 78), (76, 62)]),
    )
    layers = [
        (rrect(64, 112, 40, 12, 3), METAL_DARK, 1.0),
        (figure, steel, 1.0),
    ]
    if wailing:
        layers = [("glow", 64, 40, 50, CYAN, 0.45)] + layers + [
            (circle(58, 28, 2.5), CYAN_PALE, 1.0),
            (circle(70, 28, 2.5), CYAN_PALE, 1.0),
            (rrect(64, 38, 4, 6, 3), CYAN_PALE, 1.0),
            (rrect(64, 68, 6, 14, 3), CYAN, 0.6),
        ]
    else:
        layers += [(rrect(64, 70, 18, 3, 1), (120, 40, 40), 0.9)]
    return layers


def fountain_of_youth():
    """2x2 stone basin, bright water, a little spout."""
    stone, stone_dark = (150, 145, 135), (110, 105, 98)
    return [
        ("glow", 128, 128, 120, CYAN, 0.35),
        (circle(128, 132, 108), stone_dark, 1.0),
        (circle(128, 128, 100), stone, 1.0),
        (circle(128, 128, 78), (60, 110, 120), 1.0),
        (circle(128, 128, 70), CYAN, 0.75),
        (ring(128, 128, 40, 4), CYAN_PALE, 0.6),
        (circle(128, 128, 22), stone, 1.0),
        (circle(128, 124, 10), CYAN_PALE, 1.0),
    ]


def soul_cradle():
    """1x2 sealed pod seen from above: a lid with a long window over a lattice glow."""
    return [
        ("glow", 64, 128, 90, CYAN, 0.3),
        (rrect(64, 128, 54, 120, 26), METAL_DARK, 1.0),
        (rrect(64, 128, 46, 112, 22), (200, 205, 210), 1.0),
        (rrect(64, 120, 28, 84, 14), (40, 60, 66), 1.0),
        (rrect(64, 120, 22, 78, 11), CYAN, 0.5),
        (union(*[rrect(64, 60 + k * 20, 20, 1.5, 1) for k in range(7)]), CYAN_PALE, 0.6),
        (circle(64, 226, 6), CYAN, 1.0),
    ]


def bone_fetish():
    """Finger bones on a cord around a knot of hair."""
    bone, hair = (225, 215, 190), (70, 50, 40)
    cord = ring(64, 50, 34, 3)
    bones = union(*[rrect(64 + dx, 86 + abs(dx) * 0.2, 4, 14, 4) for dx in (-18, -9, 0, 9, 18)])
    return [
        (intersect(cord, rrect(64, 74, 40, 30, 0)), (110, 80, 55), 1.0),
        (bones, bone, 1.0),
        (circle(64, 76, 10), hair, 1.0),
        (circle(64, 76, 4), CYAN, 0.8),
    ]


def wraith_lantern():
    """Iron lantern, shutters half open, a pale flame."""
    return [
        ("glow", 64, 72, 44, CYAN, 0.45),
        (ring(64, 22, 10, 3), METAL_DARK, 1.0),
        (rrect(64, 36, 22, 6, 2), METAL_DARK, 1.0),
        (rrect(64, 72, 24, 32, 4), METAL, 1.0),
        (rrect(64, 72, 16, 26, 3), (40, 60, 66), 1.0),
        (union(circle(64, 78, 9), poly([(56, 76), (72, 76), (64, 56)])), CYAN, 0.9),
        (circle(64, 78, 4), CYAN_PALE, 1.0),
        (rrect(48, 72, 3, 30, 1), METAL_DARK, 1.0),
        (rrect(80, 72, 3, 30, 1), METAL_DARK, 1.0),
        (rrect(64, 108, 26, 5, 2), METAL_DARK, 1.0),
    ]


def soul_arrows():
    """A small bundle of arrows with glowing heads."""
    layers = []
    for k, dx in enumerate((-14, 0, 14)):
        x = 64 + dx
        layers += [
            (poly([(x - 2, 104), (x + 2, 104), (x + 2 + dx * 0.4, 34), (x - 2 + dx * 0.4, 34)]), (150, 115, 75), 1.0),
            (poly([(x - 7 + dx * 0.4, 36), (x + 7 + dx * 0.4, 36), (x + dx * 0.4, 18)]), CYAN, 1.0),
            (poly([(x - 6, 112), (x + 6, 112), (x + 2, 98), (x - 2, 98)]), (200, 200, 205), 1.0),
        ]
    return [("glow", 64, 30, 30, CYAN, 0.35)] + layers


def soul_vat():
    """2x2 squat vat from above: steel rim, glass lid, essence inside."""
    return [
        ("glow", 128, 128, 110, CYAN, 0.3),
        (circle(128, 132, 112), METAL_DARK, 1.0),
        (circle(128, 128, 104), METAL, 1.0),
        (circle(128, 128, 84), (40, 60, 66), 1.0),
        (circle(128, 128, 76), CYAN, 0.6),
        (ring(128, 128, 50, 3), CYAN_PALE, 0.5),
        (union(*[rrect(128 + dx, 128, 3, 86, 1) for dx in (-40, 0, 40)]), METAL_DARK, 0.9),
        (union(*[circle(128 + 96 * c, 128 + 96 * s_, 7) for c, s_ in ((1, 0), (-1, 0), (0, 1), (0, -1))]), METAL_DARK, 1.0),
    ]


def soul_valve():
    """Valve wheel on a short pipe."""
    return [
        (rrect(64, 64, 60, 12, 3), (120, 220, 230), 0.7),
        (circle(64, 64, 26), METAL_DARK, 1.0),
        (ring(64, 64, 22, 5), METAL, 1.0),
        (union(rrect(64, 64, 20, 3, 1), rrect(64, 64, 3, 20, 1)), METAL, 1.0),
        (circle(64, 64, 6), CYAN, 1.0),
    ]


def soul_basin(drain):
    """Tap or drain: a square steel basin; the drain has a grate, the tap a spout."""
    layers = [
        (rrect(64, 66, 52, 50, 8), METAL_DARK, 1.0),
        (rrect(64, 64, 44, 42, 6), METAL, 1.0),
        (rrect(64, 64, 34, 32, 4), (40, 60, 66), 1.0),
        (rrect(64, 64, 30, 28, 3), CYAN, 0.45),
    ]
    if drain:
        layers.append((union(*[rrect(64, 46 + k * 9, 30, 1.8, 1) for k in range(5)]), METAL_DARK, 1.0))
    else:
        layers += [(rrect(64, 22, 8, 14, 3), METAL_DARK, 1.0), (circle(64, 40, 5), CYAN_PALE, 1.0)]
    return layers


def kit_belt():
    """A plain belt with two pouches, drawn pale so the set colour tints it."""
    pale, dark = (225, 225, 225), (150, 150, 150)
    return [
        (rrect(64, 64, 54, 8, 4), pale, 1.0),
        (rrect(40, 76, 12, 14, 3), dark, 1.0),
        (rrect(88, 76, 12, 14, 3), dark, 1.0),
        (rrect(64, 64, 9, 11, 2), (190, 190, 190), 1.0),
        (rrect(64, 64, 5, 7, 1), dark, 1.0),
    ]


def kit_goggles():
    """Two round lenses on a strap, pale for tinting."""
    pale = (225, 225, 225)
    return [
        (rrect(64, 64, 58, 4, 2), (170, 170, 170), 1.0),
        (circle(42, 64, 18), pale, 1.0),
        (circle(86, 64, 18), pale, 1.0),
        (circle(42, 64, 12), (90, 110, 115), 1.0),
        (circle(86, 64, 12), (90, 110, 115), 1.0),
        (rrect(64, 62, 6, 4, 2), pale, 1.0),
    ]


SKIN = (215, 185, 165)
SKIN_DARK = (150, 110, 95)


def face_mask_icon():
    """A flayed face on its leather backing, eye holes and stitches."""
    return [
        (union(circle(64, 58, 34), rrect(64, 80, 26, 22, 14)), (110, 80, 55), 1.0),
        (union(circle(64, 58, 30), rrect(64, 80, 22, 18, 12)), SKIN, 1.0),
        (circle(51, 56, 6), (40, 25, 20), 1.0),
        (circle(77, 56, 6), (40, 25, 20), 1.0),
        (rrect(64, 86, 10, 2.5, 1), SKIN_DARK, 1.0),
        (union(*[rrect(64 + dx, 34, 1.2, 4, 1) for dx in range(-20, 21, 8)]), SKIN_DARK, 1.0),
        (union(*[rrect(64, 40 + dy, 30, 0.8, 0.5) for dy in (0,)]), SKIN_DARK, 0.6),
    ]


def face_mask_worn(facing):
    """Worn on the head: front shows the face, side a sliver of it, back only the ties."""
    if facing == "north":
        return [(rrect(64, 62, 26, 2.5, 1), (90, 60, 40), 1.0)]
    if facing == "east":
        return [
            (union(circle(76, 64, 15), rrect(78, 74, 10, 10, 6)), SKIN, 1.0),
            (circle(82, 62, 3), (40, 25, 20), 1.0),
            (rrect(56, 62, 10, 2, 1), (90, 60, 40), 1.0),
        ]
    return [
        (union(circle(64, 64, 18), rrect(64, 76, 13, 11, 8)), SKIN, 1.0),
        (circle(57, 62, 3.5), (40, 25, 20), 1.0),
        (circle(71, 62, 3.5), (40, 25, 20), 1.0),
        (rrect(64, 79, 6, 1.5, 1), SKIN_DARK, 1.0),
    ]


def ghost(facing):
    """A hooded, legless figure trailing off into mist. Drawn pale; each ghost kind tints it."""
    pale, dim = (240, 245, 255), (200, 210, 230)
    if facing == "east":
        body = union(circle(68, 38, 16), poly([(52, 46), (82, 46), (90, 96), (70, 120), (44, 104)]))
        eyes = [(circle(76, 38, 3), (30, 40, 60), 0.9)]
    else:
        body = union(circle(64, 38, 18), poly([(44, 48), (84, 48), (96, 100), (64, 122), (32, 100)]))
        eyes = [] if facing == "north" else [(circle(57, 38, 3.5), (30, 40, 60), 0.9), (circle(71, 38, 3.5), (30, 40, 60), 0.9)]
    return [
        ("glow", 64, 64, 60, pale, 0.25),
        (body, dim, 0.55),
        (intersect(body, rrect(64, 60, 60, 32, 10)), pale, 0.45),
    ] + eyes


if __name__ == "__main__":
    tx = os.path.join(ROOT, "Textures")
    t = os.path.join(tx, "Things")
    b, i, ui = os.path.join(t, "Building"), os.path.join(t, "Item"), os.path.join(tx, "UI")
    write_png(os.path.join(i, "Drug", "HP_Soulfire.png"), render(128, soulfire()))
    write_png(os.path.join(i, "Drug", "HP_BorrowedTime.png"), render(128, borrowed_time()))
    write_png(os.path.join(i, "HP_CorruptedEssence.png"), render(128, corrupted_essence()))
    write_png(os.path.join(i, "Equipment", "HP_CursedIdol.png"), render(128, cursed_idol()))
    write_png(os.path.join(b, "HP_HellGate.png"), render(256, scaled(hell_gate(), 2)))
    write_png(os.path.join(tx, "World", "HP_DemonFaction.png"), render(128, horned_head(WHITE)))
    write_png(os.path.join(ui, "Icons", "HP_Demon.png"), render(128, horned_head(WHITE, OBSIDIAN)))
    write_png(os.path.join(ui, "Icons", "HP_CorruptedSoul.png"), render(128, corrupted_soul_icon()))
    write_png(os.path.join(ui, "HeroArt", "HP_HellUnbound.png"), render(512, scaled(storyteller_portrait(), 4)))
    write_png(os.path.join(ui, "HeroArt", "HP_HellUnboundTiny.png"), render(128, storyteller_portrait()))
    # v0.7: extraction, corruption, endless war
    write_png(os.path.join(i, "HP_UrnEmpty.png"), render(128, urn(False)))
    write_png(os.path.join(i, "HP_Urn.png"), render(128, urn(True)))
    write_png(os.path.join(i, "HP_CanisterEmpty.png"), render(128, canister(False)))
    write_png(os.path.join(i, "HP_Canister.png"), render(128, canister(True)))
    write_png(os.path.join(i, "HP_LatticeEmpty.png"), render(128, lattice(False)))
    write_png(os.path.join(i, "HP_Lattice.png"), render(128, lattice(True)))
    write_png(os.path.join(i, "HP_HolyWater.png"), render(128, holy_water()))
    write_png(os.path.join(i, "HP_CarrionBale.png"), render(128, carrion_bale()))
    write_png(os.path.join(b, "HP_CarrionWall.png"), render(128, carrion_wall()))
    write_png(os.path.join(b, "HP_BoneAltar.png"), render(256, bone_altar(), 128))
    write_png(os.path.join(b, "HP_SoulPress.png"), render(256, soul_press()))
    write_png(os.path.join(b, "HP_ReanimationSlab.png"), render(256, reanimation_slab(), 128))
    write_png(os.path.join(b, "HP_CorpseGrinder.png"), render(256, corpse_grinder()))
    write_png(os.path.join(b, "HP_LineForge.png"), render(256, line_forge(), 128))
    write_png(os.path.join(b, "HP_CloneVat.png"), render(192, clone_vat(), 128))
    write_png(os.path.join(ui, "Icons", "HP_CorruptionOverlay.png"), render(128, corruption_icon()))
    write_png(os.path.join(ui, "Icons", "HP_Soulless.png"), render(128, soulless_icon()))
    write_png(os.path.join(ui, "Designators", "HP_Cleanse.png"), render(128, cleanse_icon()))
    # v0.8: reliquary and shrines (4.35)
    write_png(os.path.join(b, "HP_Reliquary.png"), render(128, reliquary()))
    write_png(os.path.join(b, "HP_BoundShrine.png"), render(128, bound_shrine()))
    write_png(os.path.join(b, "HP_EssenceShrine.png"), render(128, essence_shrine()))
    # v0.8: prisoner battery, blood debts
    write_png(os.path.join(b, "HP_PrisonerBattery.png"), render(256, prisoner_battery(), 128))
    write_png(os.path.join(b, "HP_VengeanceEffigy.png"), render(128, effigy(False)))
    write_png(os.path.join(b, "HP_WailingEffigy.png"), render(128, effigy(True)))
    write_png(os.path.join(b, "HP_FountainOfYouth.png"), render(256, fountain_of_youth()))
    write_png(os.path.join(b, "HP_SoulCradle.png"), render(128, soul_cradle(), 256))
    write_png(os.path.join(i, "Equipment", "HP_BoneFetish.png"), render(128, bone_fetish()))
    write_png(os.path.join(i, "Equipment", "HP_WraithLantern.png"), render(128, wraith_lantern()))
    write_png(os.path.join(i, "HP_SoulArrow.png"), render(128, soul_arrows()))
    # VEF soul pipes (Mods/VEF)
    write_png(os.path.join(b, "HP_SoulVat.png"), render(256, soul_vat()))
    write_png(os.path.join(b, "HP_SoulValve.png"), render(128, soul_valve()))
    write_png(os.path.join(b, "HP_SoulTap.png"), render(128, soul_basin(False)))
    write_png(os.path.join(b, "HP_SoulDrain.png"), render(128, soul_basin(True)))
    # Full-kit apparel sets (gen_apparel_sets.py): shared belt and goggles icons, tinted per set.
    write_png(os.path.join(i, "Equipment", "HP_KitBelt.png"), render(128, kit_belt()))
    write_png(os.path.join(i, "Equipment", "HP_KitGoggles.png"), render(128, kit_goggles()))
    # Flayed face mask (4.23): item icon and worn head art (Graphic_Multi, no body type for EyeCover).
    write_png(os.path.join(i, "Equipment", "HP_FaceMask.png"), render(128, face_mask_icon()))
    for facing in ("north", "east", "south"):
        write_png(os.path.join(t, "Pawn", "Humanlike", "Apparel", "HP_FaceMask", "HP_FaceMask_" + facing + ".png"), render(128, face_mask_worn(facing)))
    # Ghosts (4.8): one pale figure, tinted per kind by the pawn kind (Graphic_Multi, Transparent).
    for facing in ("north", "east", "south"):
        write_png(os.path.join(t, "Pawn", "HP_Ghost", "HP_Ghost_" + facing + ".png"), render(128, ghost(facing)))
