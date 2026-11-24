#!/usr/bin/env python3
"""Pone 3 estrellas 'E' ocultas en cada Resources/Levels/nivel*.txt: difíciles pero posibles.

Uso: python3 Tools/level/place_stars.py [ruta ...]   (idempotente: quita las 'E' y las vuelve a poner; determinista por nivel)

Una estrella por tercio del nivel, cada una de un tipo distinto (en orden rotado por nivel):
  alta    → 4-5 sobre el piso: solo con doble salto al ápice;
  pozo    → encima de un hueco;
  peligro → pegada a púas/sierras//0/bugs.
simulate.reach confirma que, SIN habilidades (salto + doble salto), se toca en todas las fases sin golpes y se sigue
vivo hasta el próximo checkpoint (o la meta), y que no es trivial: como mucho EASY_MAX de los estados vivos la tocan.
"""
import random
import sys
from pathlib import Path

import check_level
import simulate

STAR = "E"
EASY_MAX = 0.25
TRIES = 60          # candidatas probadas por tercio
KEEP_AWAY = "PCFVD"  # nada de estrellas a menos de 3 columnas de estos
KINDS = ("alta", "pozo", "peligro")


def map_rows(text):
    """Índices de las líneas del bloque map (fila 0 = arriba)."""
    lines = text.split("\n")
    a = next(i for i, l in enumerate(lines) if l.strip() == "map") + 1
    return lines, a


def strip_stars(text):
    lines, a = map_rows(text)
    for i in range(a, a + check_level.ROWS):
        lines[i] = lines[i].replace(STAR, " ").rstrip()
    return "\n".join(lines)


def put(text, cells):
    lines, a = map_rows(text)
    for x, y in cells:
        i = a + check_level.ROWS - 1 - y
        row = lines[i].ljust(x + 1)
        assert row[x] == " ", (x, y, row[x])
        lines[i] = row[:x] + STAR + row[x + 1:]
    return "\n".join(lines)


def candidates(grid, hints, floor, x0, x1):
    W = len(grid)
    away = {x + d for x in range(W) for y in range(check_level.ROWS) if grid[x][y] in KEEP_AWAY for d in range(-3, 4)}
    covered = {(c + i, y) for c, y, txt in hints for i in range(-1, round(len(txt) * 0.55) + 2)}
    hazard_cols = {x for x in range(W) if any(grid[x][y] in check_level.HAZARDS for y in range(check_level.ROWS))}
    out = {k: [] for k in KINDS}
    last = 2
    for x in range(x0, x1):
        if floor[x] is not None:
            last = floor[x]
        if x in away:
            continue
        base = floor[x] if floor[x] is not None else last
        for y in range(base + 1, check_level.HUD_ROW):
            if grid[x][y] != " " or (x, y) in covered:
                continue
            h = y - base
            if floor[x] is None and 1 <= h <= 4:
                out["pozo"].append((x, y))
            elif floor[x] is not None and h in (4, 5):
                out["alta"].append((x, y))
            elif floor[x] is not None and 1 <= h <= 4 and any(c in hazard_cols for c in (x - 1, x, x + 1)):
                out["peligro"].append((x, y))
    return out


def place(text, level):
    text = strip_stars(text)
    grid, hints = check_level.parse(text)
    world, speed, snaps = simulate.phase_runs(text, level)
    floor = world.floor
    px, fx = world.find("P")[0], world.find("F")[0]
    checkpoints = sorted(x for x in range(len(grid)) for y in range(check_level.ROWS) if grid[x][y] == "C")
    rng = random.Random(level * 104729)
    cells = []
    for third in range(3):
        x0 = px + (fx - px) * third // 3
        x1 = px + (fx - px) * (third + 1) // 3
        cands = candidates(grid, hints, floor, x0, x1)
        order = KINDS[(level + third) % 3:] + KINDS[:(level + third) % 3]
        best = None  # (facilidad, celda, tipo) por si ninguna cumple EASY_MAX
        for kind in order:
            pool = cands[kind][:]
            rng.shuffle(pool)
            for cell in pool[:TRIES]:
                until = min([c for c in checkpoints if c > cell[0] + 2] + [fx]) + 0.5
                ok, easy = simulate.reach(world, speed, snaps, cell, until)
                if ok and (best is None or easy < best[0]):
                    best = (easy, cell, kind)
                if ok and easy <= EASY_MAX:
                    break
            if best and best[0] <= EASY_MAX:
                break
        assert best, f"nivel{level}: sin estrella posible en el tercio {third + 1}"
        cells.append(best[1])
        print(f"  tercio {third + 1}: {best[2]:7} en {best[1]} · la tocan {best[0]:.0%} de las rutas")
    return put(text, cells)


def run(paths):
    """Coloca y guarda; True si todos quedaron válidos (generate_levels.py la llama con los niveles que escribió)."""
    ok = True
    for path in paths:
        level = int("".join(c for c in path.stem if c.isdigit()))
        print(f"{path.name}:")
        text = place(path.read_text(encoding="utf-8"), level)
        errors, _ = check_level.check(text, dj_taught=level > 1)
        if not simulate.solve(text, level)[0]:
            errors.append("simulate: ya no se puede pasar")
        stars = sum(col.count(STAR) for col in check_level.parse(text)[0])
        if stars != 3:
            errors.append(f"{stars} estrellas en el mapa")
        for e in errors:
            print("  ERROR:", e)
        if errors:
            ok = False
            continue
        path.write_text(text, encoding="utf-8")
    return ok


def main():
    paths = [Path(p) for p in sys.argv[1:]] or sorted(check_level.LEVELS.glob("nivel*.txt"))
    sys.exit(0 if run(paths) else 1)


if __name__ == "__main__":
    main()
