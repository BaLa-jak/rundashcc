#!/usr/bin/env python3
"""Convierte los JPEG de Assets/ en sprites PNG con alfa para Assets/Game/Resources/Art/.

Uso: python3 Tools/art/process_sprites.py   (necesita Pillow; no modifica los JPEG)
"""
import math
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "Assets"
OUT = ROOT / "Assets/Game/Resources/Art"

GRID = 100          # los JPEG de pixel art son una rejilla de 100x100 bloques de 10.24 px (medido por autocorrelación)
BG_TOL = 13         # fondo ≤ ~10 de distancia a #0c0c0c; el contorno morado más oscuro está a ≥ 16
PIXEL_SCALE = 2     # mismo tamaño de pixel en camarada y bug: 2 px de textura por bloque
MARGIN = 4


def native_grid(img):
    """Muestrea el centro de cada bloque (mediana de 5x5) -> imagen a resolución nativa, sin ruido JPEG."""
    px = img.load()
    out = Image.new("RGB", (GRID, GRID))
    o = out.load()
    cell = img.width / GRID
    for cy in range(GRID):
        for cx in range(GRID):
            x0, y0 = int(cx * cell) + 3, int(cy * cell) + 3
            vals = [px[x, y] for y in range(y0, y0 + 5) for x in range(x0, x0 + 5)]
            o[cx, cy] = tuple(sorted(v[c] for v in vals)[12] for c in range(3))
    return out


def pixel_sprite(name):
    small = native_grid(Image.open(SRC / f"{name}.jpeg").convert("RGB"))
    px = small.load()
    bg = px[0, 0]
    # flood-fill del fondo desde todos los bordes; lo encerrado (ojo, líneas interiores) se conserva
    clear = set()
    stack = [(x, y) for x in range(GRID) for y in (0, GRID - 1)] + [(x, y) for y in range(GRID) for x in (0, GRID - 1)]
    while stack:
        p = stack.pop()
        if p in clear or not (0 <= p[0] < GRID and 0 <= p[1] < GRID) or math.dist(px[p], bg) >= BG_TOL:
            continue
        clear.add(p)
        x, y = p
        stack += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
    rgba = small.convert("RGBA")
    for p in clear:
        rgba.putpixel(p, (0, 0, 0, 0))
    rgba = rgba.crop(rgba.getbbox())
    rgba = rgba.resize((rgba.width * PIXEL_SCALE, rgba.height * PIXEL_SCALE), Image.NEAREST)
    return pad(rgba, MARGIN)


def pad(img, m):
    out = Image.new("RGBA", (img.width + 2 * m, img.height + 2 * m), (0, 0, 0, 0))
    out.paste(img, (m, m))
    return out


def color_to_alpha(img, lo=8):
    """Negro -> transparente. alpha = max(r,g,b) con umbral; el color se des-premultiplica."""
    out = Image.new("RGBA", img.size)
    src, dst = img.load(), out.load()
    for y in range(img.height):
        for x in range(img.width):
            r, g, b = src[x, y]
            m = max(r, g, b)
            a = max(0.0, (m - lo) / (255 - lo))
            if a <= 0:
                dst[x, y] = (0, 0, 0, 0)
            else:
                k = 255 / m
                dst[x, y] = (min(255, round(r * k)), min(255, round(g * k)), min(255, round(b * k)), round(a * 255))
    return out


def neon_bbox(img, thr=40):
    mask = img.convert("RGB").point(lambda v: 255 if v > thr else 0).convert("L")
    return mask.getbbox()


def obstaculo():
    img = Image.open(SRC / "obstaculo.jpeg").convert("RGB")
    l, t, r, b = neon_bbox(img)
    side = max(r - l, b - t)
    pad_src = round(side * MARGIN / 128)
    img = img.crop((l - pad_src, t - pad_src, r + pad_src, b + pad_src))
    s = 128 / max(img.size)
    img = img.resize((round(img.width * s), round(img.height * s)), Image.LANCZOS)
    return color_to_alpha(img)


def saw_center(img):
    """Ajuste de círculo (Kasa) a los dientes exteriores: el sprite debe girar sobre el centro real de la sierra."""
    px = img.convert("L").load()
    l, t, r, b = neon_bbox(img, 150)
    cx, cy = (l + r) / 2, (t + b) / 2
    for _ in range(3):
        pts = []
        for i in range(720):
            ang = 2 * math.pi * i / 720
            for rad in range(500, 100, -1):
                x, y = int(cx + rad * math.cos(ang)), int(cy + rad * math.sin(ang))
                if 0 <= x < img.width and 0 <= y < img.height and px[x, y] > 150:
                    pts.append((x, y))
                    break
        med = sorted(math.dist(p, (cx, cy)) for p in pts)[len(pts) // 2]
        pts = [p for p in pts if 0.85 * med < math.dist(p, (cx, cy)) < 1.15 * med]  # fuera chispas y huecos
        n = len(pts)
        sx = sum(p[0] for p in pts) / n
        sy = sum(p[1] for p in pts) / n
        suu = sum((p[0] - sx) ** 2 for p in pts)
        svv = sum((p[1] - sy) ** 2 for p in pts)
        suv = sum((p[0] - sx) * (p[1] - sy) for p in pts)
        suuu = sum((p[0] - sx) ** 3 + (p[0] - sx) * (p[1] - sy) ** 2 for p in pts) / 2
        svvv = sum((p[1] - sy) ** 3 + (p[1] - sy) * (p[0] - sx) ** 2 for p in pts) / 2
        det = suu * svv - suv * suv
        uc = (suuu * svv - svvv * suv) / det
        vc = (svvv * suu - suuu * suv) / det
        cx, cy = sx + uc, sy + vc
    outer = max(math.dist(p, (cx, cy)) for p in pts)
    return cx, cy, outer


def sierra():
    img = Image.open(SRC / "sierra.jpeg").convert("RGB")
    cx, cy, outer = saw_center(img)
    half = outer * 1.06  # margen para el glow de los dientes; las chispas sueltas más lejos se desvanecen
    img = img.crop((round(cx - half), round(cy - half), round(cx + half), round(cy + half)))
    img = img.resize((160, 160), Image.LANCZOS)
    out = color_to_alpha(img)
    # fundido radial en el último tramo para que nada quede cortado en seco por el recorte cuadrado
    px = out.load()
    rim = 80 * outer / half
    for y in range(160):
        for x in range(160):
            d = math.dist((x + 0.5, y + 0.5), (80, 80))
            if d > rim:
                f = max(0.0, 1 - (d - rim) / (80 - rim))
                r, g, b, a = px[x, y]
                px[x, y] = (r, g, b, round(a * f))
    print(f"sierra: centro ({cx:.1f}, {cy:.1f}) radio dientes {outer:.1f}px fuente = {rim / 160:.3f} del ancho")
    return out


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    sprites = {
        "camarada": pixel_sprite("camarada"),
        "bug": pixel_sprite("bug"),
        "obstaculo": obstaculo(),
        "sierra": sierra(),
        "fondo": Image.open(SRC / "fondo.jpeg").convert("RGB"),
    }
    for name, img in sprites.items():
        img.save(OUT / f"{name}.png", optimize=True)
        print(f"{name}.png {img.width}x{img.height} px = {img.width / 100:.2f}x{img.height / 100:.2f} u @PPU100")

    # self-check
    for name in ("camarada", "bug", "obstaculo", "sierra"):
        img = sprites[name]
        assert img.mode == "RGBA", name
        a = img.getchannel("A")
        assert a.getpixel((0, 0)) == 0 and a.getpixel((img.width - 1, img.height - 1)) == 0, f"{name}: esquinas opacas"
        assert 64 <= max(img.size) <= 176, f"{name}: tamaño {img.size}"
    assert sprites["sierra"].size == (160, 160)
    assert sprites["fondo"].mode == "RGB" and sprites["fondo"].size == (1024, 572)


if __name__ == "__main__":
    main()
