#!/usr/bin/env python3
"""Convierte los JPEG de Assets/ (y portales.png) en sprites PNG con alfa para Assets/Game/Resources/Art/.

Uso: python3 Tools/art/process_sprites.py   (necesita Pillow; no modifica los JPEG)
"""
import math
from pathlib import Path

from PIL import Image, ImageDraw

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


# Hoja de cosméticos (2048x2048, bloques de ~20 px): bandas de fila medidas una vez y 5 columnas fijas.
COS_STEP = 8        # muestreo cada 8 px (~2.5 muestras por bloque) -> iconos de ~45 px, x2 con NEAREST
COS_ROWS = {        # fila: (y0, y1, ids de izquierda a derecha) — mismo orden que Cosmetics.cs
    "tenis": (348, 580, ["rojos", "turbo", "camuflaje", "morados", "alados"]),
    "camisetas": (670, 945, ["estrella", "marinera", "calavera", "jersey7", "glitch"]),
    "sombreros": (1000, 1295, ["gorra", "copa", "pirata", "mago", "fiesta"]),
    "guantes": (1395, 1585, ["box_rojo", "box_oro", "sin_dedos", "blancos", "cyber"]),
}
COS_COLS = [0, 445, 830, 1215, 1600, 2048]


def icon(img, box):
    """Recorta box, muestrea la rejilla (mediana 3x3), quita el fondo con flood-fill desde los bordes y recorta."""
    cell = img.crop(box)
    px = cell.load()
    w, h = cell.width // COS_STEP, cell.height // COS_STEP
    small = Image.new("RGB", (w, h))
    o = small.load()
    for cy in range(h):
        for cx in range(w):
            x0, y0 = cx * COS_STEP + COS_STEP // 2 - 1, cy * COS_STEP + COS_STEP // 2 - 1
            vals = [px[x, y] for y in range(y0, y0 + 3) for x in range(x0, x0 + 3)]
            o[cx, cy] = tuple(sorted(v[c] for v in vals)[4] for c in range(3))
    bg = (12, 12, 10)
    clear = set()
    stack = [(x, y) for x in range(w) for y in (0, h - 1)] + [(x, y) for y in range(h) for x in (0, w - 1)]
    while stack:
        p = stack.pop()
        if p in clear or not (0 <= p[0] < w and 0 <= p[1] < h) or math.dist(o[p], bg) >= 18:
            continue
        clear.add(p)
        x, y = p
        stack += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
    rgba = small.convert("RGBA")
    for p in clear:
        rgba.putpixel(p, (0, 0, 0, 0))
    rgba = rgba.crop(rgba.getbbox())
    return pad(rgba.resize((rgba.width * 2, rgba.height * 2), Image.NEAREST), MARGIN)


def cosmeticos():
    img = Image.open(SRC / "cosmeticos.jpeg").convert("RGB")
    out = {}
    for row, (y0, y1, ids) in COS_ROWS.items():
        for i, cid in enumerate(ids):
            # la etiqueta "SOMBREROS" baja hasta y≈1030 sobre las 2 primeras columnas; el gorro de fiesta sube más
            top = 1035 if row == "sombreros" and i < 2 else y0
            out["cos_" + cid] = icon(img, (COS_COLS[i], top, COS_COLS[i + 1], y1))
    out["tienda_icono"] = icon(img, (815, 1640, 1240, 2040))
    return out


# ---------------------------------------------------------------- Cosméticos puestos (wear_<id>.png)
# Capas del mismo tamaño de bloque que camarada.png y alineadas con ella: lienzo = camarada.png + WEAR_PAD bloques
# por lado (centro común), así en Unity van como hijo en (0,0) con escala 1. Coordenadas en bloques nativos de la
# rata (66x33, y hacia abajo), medidas sobre camarada.png.
WEAR_PAD = 12
HAT = dict(cx=55, bottom=8)  # centro y fila de abajo (se encaja 1-2 filas en la cabeza)
GLOVE = dict(paws=[(46, 25, 0.75), (54, 23, 1.0)])   # (cx, cy, brillo): primero la pata de atrás
SHOE = dict(feet=[(42, 34, 0.8), (23, 34, 1.0)])     # (cx, base, brillo)


# Tenis y guantes a 8-11 bloques no aguantan el detalle del icono (agujetas, costuras): plantillas de pixel art
# de perfil (miran a la derecha, como Camarada) coloreadas con la paleta de cada icono.
# O contorno, M color, L brillo, D sombra, A acento (agujetas/pulgar), S suela/puño, K nudillos, W/w alas.
SHOE_TPL = [
    "....OOOO.....",
    "...OLMMMO....",
    "...OAMAMMOO..",
    "..OMAMAMMMMO.",
    ".OMMMMMMMMMMO",
    "OSSSSSSSSSSSO",
    ".OOOOOOOOOOO.",
]
WING_TPL = ["w....", "Ww...", ".WWW.", "..WW."]  # sobre el talón de los tenis alados
GLOVE_TPL = [
    "..OOOOO..",
    ".OLLMMKO.",
    "OLMMMMMKO",
    "OLMMMMMKO",
    "OMMMAMMKO",
    "OMMMMMMMO",
    ".ODDDDDO.",
    ".OSSSSSO.",
    ".OOOOOOO.",
]
HAT_TPL = {  # de perfil; la punta del mago cae hacia atrás
    "gorra": [
        "....OOOOOO.......",
        "..OOLLMMMMOO.....",
        ".OLMMMMMMMMMO....",
        ".OMMMMMMAMMMMO...",
        "OMMMMMMMMMMMMMOOO",
        "ODDDDDDDDDDDDDDDO",
        ".OOOOOOOOOOOOOOO.",
    ],
    "copa": [
        "..OOOOOOOOOO..",
        "..OLMMMMMMMO..",
        "..OLMMMMMMMO..",
        "..OLMMMMMMMO..",
        "..OLMMMMMMMO..",
        "..OLMMMMMMMO..",
        "..OAAAAAAAAO..",
        "..OAAAAAAAAO..",
        "OOOOOOOOOOOOOO",
        "ODDDDDDDDDDDDO",
        ".OOOOOOOOOOOO.",
    ],
    "pirata": [
        "OO.....OOOO.....OO",
        "OTO..OOMMMMOO..OTO",
        "OTMOOMMMWWWMMOOMTO",
        ".OTMMMMMWOWMMMMTO.",
        ".OTMMMMMMWMMMMMTO.",
        "..OTTTMMMMMMTTTO..",
        "...OOOTTTTTTOOO...",
        "......OOOOOO......",
    ],
    "mago": [
        "..OOO.........",
        ".OMMMO........",
        "..OOMMO.......",
        "....OMMO......",
        "....OMAMO.....",
        "...OMMMMO.....",
        "...OMMMAMO....",
        "..OMAMMMMO....",
        "..OMMMMMAMO...",
        ".OMMMAMMMMO...",
        "OOOOOOOOOOOOOO",
        "OMMMMAMMMMMAMO",
        ".OOOOOOOOOOOO.",
    ],
    "fiesta": [
        ".....PPP.....",
        ".....PPP.....",
        "......O......",
        ".....ORO.....",
        ".....ORO.....",
        "....OYYYO....",
        "....OGGGO....",
        "...OBBBBBO...",
        "...ORRRRRO...",
        "..OYYYYYYYO..",
        "..OGGGGGGGO..",
        ".OBBBBBBBBBO.",
        ".ORRRRRRRRRO.",
        "OYYYYYYYYYYYO",
        "OOOOOOOOOOOOO",
    ],
}
OUTLINE = (26, 15, 36)
WEAR_PAL = {  # colores tomados de cada icono
    "gorra": dict(M=(210, 40, 45), L=(240, 110, 110), D=(150, 20, 35), A=(235, 235, 235)),
    "copa": dict(M=(55, 55, 65), L=(95, 95, 110), D=(40, 40, 48), A=(210, 35, 45)),
    "pirata": dict(M=(110, 65, 35), T=(230, 180, 60), W=(240, 240, 240)),
    "mago": dict(M=(40, 55, 130), A=(240, 200, 40)),
    "fiesta": dict(M=(235, 60, 120), P=(240, 90, 150), R=(235, 60, 120), G=(90, 200, 80), B=(60, 150, 235), Y=(245, 200, 50)),
    "rojos": dict(M=(200, 35, 40), L=(240, 100, 100), A=(235, 235, 235), S=(235, 235, 235)),
    "turbo": dict(M=(20, 170, 230), L=(110, 220, 255), A=(240, 200, 40), S=(240, 200, 40)),
    "camuflaje": dict(M=(60, 160, 70), L=(110, 190, 80), A=(35, 95, 60), S=(75, 75, 75)),
    "morados": dict(M=(110, 45, 145), L=(150, 85, 180), A=(235, 235, 235), S=(225, 225, 240)),
    "alados": dict(M=(230, 180, 50), L=(250, 230, 140), A=(150, 80, 10), S=(240, 240, 240), W=(250, 240, 190), w=(255, 255, 255)),
    "box_rojo": dict(M=(200, 40, 45), D=(130, 15, 35), L=(240, 110, 110), A=(130, 15, 35), S=(235, 235, 235)),
    "box_oro": dict(M=(235, 185, 40), D=(215, 125, 15), L=(255, 230, 130), A=(200, 120, 20), S=(170, 80, 35)),
    "sin_dedos": dict(M=(45, 45, 70), D=(30, 30, 40), L=(80, 80, 110), A=(230, 165, 165), S=(35, 35, 45), K=(230, 165, 165)),
    "blancos": dict(M=(235, 235, 235), D=(165, 165, 180), L=(255, 255, 255), A=(40, 30, 50), S=(200, 200, 210)),
    "cyber": dict(M=(10, 45, 90), D=(5, 30, 60), L=(40, 230, 230), A=(70, 230, 100), S=(70, 230, 100), K=(40, 230, 230)),
}


def paint(rows, pal, k=1.0):
    """Plantilla -> RGBA. Letras sin color en la paleta caen en M (K, D) y el contorno es fijo."""
    assert len({len(r) for r in rows}) == 1, "plantilla con filas de distinto ancho"
    out = Image.new("RGBA", (len(rows[0]), len(rows)), (0, 0, 0, 0))
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch != ".":
                c = OUTLINE if ch == "O" else pal.get(ch, pal["M"])
                out.putpixel((x, y), tuple(round(v * k) for v in c) + (255,))
    return out


def native_of(img):
    """Deshace pad + x2 de pixel_sprite: un pixel por bloque."""
    m = MARGIN
    img = img.crop((m, m, img.width - m, img.height - m))
    return img.resize((img.width // PIXEL_SCALE, img.height // PIXEL_SCALE), Image.NEAREST)


def shirt_layer(rat, icon):
    """Pinta el patrón de la camiseta sobre el pelaje del torso, con el sombreado de la rata."""
    rpx = rat.load()
    fur = lambda c: c[3] and sum(c[:3]) / 3 > 140 and c[0] - c[1] < 40
    mask = {(x, y) for y in range(rat.height) for x in range(rat.width)
            if fur(rpx[x, y]) and x >= 29 and y <= 21 and x <= 46 + (y - 6) * 0.4}
    xs, ys = [p[0] for p in mask], [p[1] for p in mask]
    x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
    body = icon.crop(icon.getbbox())
    bw, bh = body.size
    body = body.crop((round(bw * 0.22), round(bh * 0.14), round(bw * 0.78), round(bh * 0.94)))  # sin mangas ni cuello
    tex = body.resize((x1 - x0 + 1, y1 - y0 + 1), Image.NEAREST).load()
    out = Image.new("RGBA", rat.size, (0, 0, 0, 0))
    o = out.load()
    for x, y in mask:
        r, g, b, a = tex[x - x0, y - y0]
        if not a:
            r, g, b = tex[(x1 - x0) // 2, (y1 - y0) // 2][:3]
        k = (sum(rpx[x, y][:3]) / 3 / 255) ** 1.5
        if any((x + dx, y + dy) not in mask and fur(rpx[x + dx, y + dy]) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
               if 0 <= x + dx < rat.width and 0 <= y + dy < rat.height):
            k *= 0.7  # costura donde termina la tela
        o[x, y] = (round(r * k), round(g * k), round(b * k), 255)
    return out


def wear_layers(rat_png, icons):
    rat = native_of(rat_png)
    off = MARGIN // PIXEL_SCALE + WEAR_PAD
    size = (rat.width + 2 * off, rat.height + 2 * off)
    out = {}
    for name, icon in icons.items():
        cid = name[4:]
        layer = Image.new("RGBA", size, (0, 0, 0, 0))
        if cid in COS_ROWS["camisetas"][2]:
            layer.alpha_composite(shirt_layer(rat, icon), (off, off))
        elif cid in COS_ROWS["sombreros"][2]:
            hat = paint(HAT_TPL[cid], WEAR_PAL[cid])
            layer.alpha_composite(hat, (off + HAT["cx"] - hat.width // 2, off + HAT["bottom"] - hat.height))
        elif cid in COS_ROWS["guantes"][2]:
            for cx, cy, k in GLOVE["paws"]:
                glove = paint(GLOVE_TPL, WEAR_PAL[cid], k)
                layer.alpha_composite(glove, (off + cx - glove.width // 2, off + cy - glove.height // 2))
        else:
            for cx, base, k in SHOE["feet"]:
                shoe = paint(SHOE_TPL, WEAR_PAL[cid], k)
                x, y = off + cx - shoe.width // 2, off + base - shoe.height
                layer.alpha_composite(shoe, (x, y))
                if "W" in WEAR_PAL[cid]:
                    layer.alpha_composite(paint(WING_TPL, WEAR_PAL[cid], k), (x - 1, y - 1))
        out["wear_" + cid] = layer.resize((size[0] * PIXEL_SCALE, size[1] * PIXEL_SCALE), Image.NEAREST)
    return out


# Portales (Assets/portales.png, 1024x572, la imagen de referencia): 1 azul = volteo, 3 morado = dimensión.
# Recortes sin títulos ni reflejo; se comprimen en x para leerse como compuerta vista de lado en el 2D.
PORTAL_BOX = {"azul": (0, 70, 345, 458), "morado": (680, 70, 1024, 456)}
PORTAL_H = 340       # px = 3.4 u @PPU100 (Camarada mide ~0.75 u)
PORTAL_SQUASH = 0.55
PORTAL_FRONT = 0.7   # desde aquí (fracción del ancho) el aro va delante de Camarada


def portales():
    src = Image.open(SRC / "portales.png").convert("RGB")
    out = {}
    for name, box in PORTAL_BOX.items():
        img = color_to_alpha(src.crop(box))
        img = img.crop(img.getbbox())
        w = round(img.width * PORTAL_SQUASH * PORTAL_H / img.height)
        img = pad(img.resize((w, PORTAL_H), Image.LANCZOS), MARGIN)
        front = img.copy()
        a = front.getchannel("A").load()
        for y in range(front.height):
            for x in range(round(front.width * PORTAL_FRONT)):
                a[x, y] = 0
        front.putalpha(Image.frombytes("L", front.size, bytes(a[x, y] for y in range(front.height) for x in range(front.width))))
        out[f"portal_{name}"] = img
        out[f"portal_{name}_frente"] = front
    return out


# Dimensión "producción": pixel art limpio y colorido, sin código. Se dibuja a resolución nativa y se escala x6.
PROD_SCALE = 6


def _px(size):
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    return img, ImageDraw.Draw(img)


def produccion():
    s = {}
    img, d = _px((16, 16))  # moneda
    d.ellipse((1, 1, 14, 14), fill=(176, 110, 20, 255))
    d.ellipse((2, 2, 13, 13), fill=(255, 200, 40, 255))
    d.ellipse((4, 4, 11, 11), fill=(255, 225, 110, 255))
    d.rectangle((7, 5, 8, 10), fill=(214, 150, 25, 255))
    d.point([(4, 3), (3, 4)], fill=(255, 255, 230, 255))
    s["prod_moneda"] = img
    img, d = _px((16, 12))  # dos pinchos de metal
    for x0 in (0, 8):
        d.polygon([(x0, 11), (x0 + 3.5, 0), (x0 + 7, 11)], fill=(120, 130, 150, 255))
        d.polygon([(x0 + 2, 11), (x0 + 3.5, 2), (x0 + 3.5, 11)], fill=(215, 225, 240, 255))
    d.rectangle((0, 11, 15, 11), fill=(70, 75, 95, 255))
    s["prod_pinchos"] = img
    img, d = _px((26, 26))  # sierra de acero
    pts = []
    for i in range(32):
        r = 12.5 if i % 2 == 0 else 9.5
        ang = math.pi * 2 * i / 32
        pts.append((12.5 + r * math.cos(ang), 12.5 + r * math.sin(ang)))
    d.polygon(pts, fill=(150, 160, 175, 255))
    d.ellipse((4, 4, 21, 21), fill=(200, 210, 225, 255))
    d.ellipse((9, 9, 16, 16), fill=(230, 70, 60, 255))
    d.ellipse((11, 11, 14, 14), fill=(90, 30, 30, 255))
    s["prod_sierra"] = img
    img, d = _px((20, 20))  # bola de fuego con picos
    pts = []
    for i in range(16):
        r = 9.5 if i % 2 == 0 else 6.5
        ang = math.pi * 2 * i / 16
        pts.append((9.5 + r * math.cos(ang), 9.5 + r * math.sin(ang)))
    d.polygon(pts, fill=(230, 50, 40, 255))
    d.ellipse((4, 4, 15, 15), fill=(255, 140, 30, 255))
    d.ellipse((7, 7, 12, 12), fill=(255, 230, 90, 255))
    s["prod_fuego"] = img
    img, d = _px((32, 14))  # nube
    for box in ((0, 6, 12, 13), (6, 1, 20, 13), (15, 3, 27, 13), (21, 7, 31, 13)):
        d.ellipse(box, fill=(255, 255, 255, 255))
    d.rectangle((3, 12, 28, 13), fill=(215, 230, 245, 255))
    s["prod_nube"] = img
    img, d = _px((64, 24))  # colina
    d.ellipse((0, 0, 63, 47), fill=(60, 170, 80, 255))
    d.ellipse((6, 3, 40, 30), fill=(95, 205, 105, 255))
    s["prod_colina"] = img
    return {n: i.resize((i.width * PROD_SCALE, i.height * PROD_SCALE), Image.NEAREST) for n, i in s.items()}


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    sprites = {
        "camarada": pixel_sprite("camarada"),
        "bug": pixel_sprite("bug"),
        "obstaculo": obstaculo(),
        "sierra": sierra(),
        "fondo": Image.open(SRC / "fondo.jpeg").convert("RGB"),
        "duende": Image.open(SRC / "duende.jpeg").convert("RGB").resize((1200, 896), Image.LANCZOS),
        **cosmeticos(),
        **portales(),
        **produccion(),
    }
    sprites.update(wear_layers(sprites["camarada"], {n: i for n, i in sprites.items() if n.startswith("cos_")}))
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
    for name, img in sprites.items():
        if name.startswith("cos_") or name == "tienda_icono":
            a = img.getchannel("A")
            assert a.getpixel((0, 0)) == 0 and a.getpixel((img.width - 1, img.height - 1)) == 0, f"{name}: esquinas opacas"
            assert 40 <= max(img.size) <= 130, f"{name}: tamaño {img.size}"
    assert sum(n.startswith("cos_") for n in sprites) == 20
    wear = [i for n, i in sprites.items() if n.startswith("wear_")]
    pad = 2 * PIXEL_SCALE * WEAR_PAD
    assert len(wear) == 20 and all(i.size == (sprites["camarada"].width + pad, sprites["camarada"].height + pad) for i in wear)
    assert all(i.getbbox() for i in wear), "capa vacía"
    assert sprites["sierra"].size == (160, 160)
    for name in ("portal_azul", "portal_morado"):
        img, front = sprites[name], sprites[name + "_frente"]
        assert img.size == front.size and img.height == PORTAL_H + 2 * MARGIN, name
        assert front.getchannel("A").getbbox()[0] >= round(img.width * PORTAL_FRONT), f"{name}: frente tapa el centro"
    assert all(sprites[n].getbbox() for n in sprites if n.startswith("prod_")), "sprite de producción vacío"
    assert sprites["fondo"].mode == "RGB" and sprites["fondo"].size == (1024, 572)


if __name__ == "__main__":
    main()
