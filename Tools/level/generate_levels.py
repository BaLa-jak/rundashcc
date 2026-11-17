#!/usr/bin/env python3
"""Genera los niveles 2..5 de RUNDASHC (determinista) en Assets/Game/Resources/Levels/nivelN.txt.

Uso: python3 Tools/level/generate_levels.py

La dificultad sube por nivel en todas las perillas a la vez (tabla LEVELS):
  velocidad (speed del archivo × GameSession.SpeedMultiplier) · suelo libre entre obstáculos
  (en "saltos completos") · tamaño de huecos y campos de púas · cambios de altura · combos que piden
  saltos cortos ("pasillo": púas con sierra encima) · menos checkpoints · más largo.
Cada obstáculo se coloca solo si simulate.py todavía encuentra una ruta SIN golpes (con holgura humana:
input cada 60 ms, peligros +0.1 u, 8 fases de los peligros móviles). Si 12 intentos fallan, pone suelo
plano. Al final cada nivel se valida otra vez completo con check_level.py y simulate.py.
"""
import math
import random
import sys
from pathlib import Path

import check_level
import simulate

ROWS = 12
OUT = check_level.LEVELS
MIN_FLOOR, MAX_FLOOR = 2, 5
TRIES = 12

# speed: u/s del archivo (se multiplica por 1 + 0.15·(N−1)) · room: suelo libre entre obstáculos en
# saltos completos (1.0 = lo que avanza un salto entero) · extra: columnas aleatorias de más ·
# dz: cambios de altura posibles al cruzar un hueco · weights: probabilidad de cada obstáculo.
LEVELS = {
    2: dict(name="bugs.c", speed=8.0, length=380, checkpoints=3, room=0.8, extra=2,
            gap=(3, 5), spikes=(1, 3), dz=(0,),
            weights=dict(gap=3, spikes=3, bug=3, saw=2, step=1, combo=1),
            hints=["// nivel_02: bugs.c", "// los bugs se pisan: +5 bits"]),
    3: dict(name="sierras.c", speed=8.5, length=440, checkpoints=3, room=0.6, extra=2,
            gap=(3, 6), spikes=(1, 4), dz=(-1, 0, 1),
            weights=dict(gap=3, spikes=3, bug=2, saw=2, saw_high=2, saw_moving=2, step=2, combo=2),
            hints=["// nivel_03: sierras.c", "// sierra alta: NO saltes, pasa abajo"]),
    4: dict(name="div_cero.c", speed=8.75, length=500, checkpoints=2, room=0.45, extra=1,
            gap=(4, 7), spikes=(2, 5), dz=(-2, -1, 0, 1, 2),
            weights=dict(gap=3, spikes=3, bug=1, saw=1, saw_high=1, saw_moving=2, div=2, div_high=2,
                         div_moving=2, step=2, combo=3, corridor=2),
            hints=["// nivel_04: div_cero.c", "// toque corto = salto bajo"]),
    5: dict(name="kernel.c", speed=9.0, length=560, checkpoints=1, room=0.3, extra=1,
            gap=(5, 7), spikes=(3, 7), dz=(-2, -1, 0, 1, 2),
            weights=dict(gap=3, spikes=3, bug=1, saw=1, saw_high=1, saw_moving=3, div=1, div_high=1,
                         div_moving=3, step=2, combo=4, corridor=3, sandwich=2),
            hints=["// nivel_05: kernel.c", "// sudo rm -rf /bugs  // sin piedad"]),
}


class Map:
    """Columnas sólidas: floor[x] = superficie (None = pozo), items[x] = {y: char}."""

    def __init__(self):
        self.floor, self.items = [], []

    def col(self, f, at=None):
        self.floor.append(f)
        self.items.append(dict(at or {}))

    def flat(self, n, f):
        for _ in range(n):
            self.col(f)

    def truncate(self, n):
        del self.floor[n:]
        del self.items[n:]

    def bits(self, x0, n, y):
        for x in range(x0, min(x0 + n, len(self.items))):
            if 0 < y < ROWS - 1 and y not in self.items[x]:
                self.items[x][y] = "*"

    def render(self):
        rows = []
        for y in range(ROWS - 1, -1, -1):
            line = ""
            for x, f in enumerate(self.floor):
                c = self.items[x].get(y)
                line += c if c else ("=" if f is not None and y < f else " ")
            rows.append(line.rstrip())
        return rows


def clamp_floor(f):
    return max(MIN_FLOOR, min(MAX_FLOOR, f))


def obstacle(kind, m, f, cfg, rng):
    """Agrega un obstáculo en la altura f y devuelve la altura del suelo después de él."""
    x = len(m.floor)
    if kind == "gap":
        w = rng.randint(*cfg["gap"])
        m.flat(w, None)
        m.bits(x + max(0, w // 2 - 1), 2, f + 3)
        return clamp_floor(f + rng.choice(cfg["dz"]))
    if kind == "spikes":
        for _ in range(rng.randint(*cfg["spikes"])):
            m.col(f, {f: "^"})
    elif kind == "bug":
        m.col(f, {f: "B"})
        m.flat(1, f)
        m.bits(x - 1, 3, f + 3)
    elif kind == "saw":
        m.col(f, {f: "S"})
    elif kind == "saw_high":  # pasar por debajo: los bits a ras de piso premian NO saltar
        m.flat(1, f)
        m.col(f, {f + 3: "S"})
        m.flat(1, f)
        m.bits(x, 3, f)
    elif kind == "saw_moving":
        m.col(f, {f + 2: "s"})
    elif kind == "div":
        m.col(f, {f: "Z"})
    elif kind == "div_high":
        m.flat(1, f)
        m.col(f, {f + 2: "Z"})
        m.flat(1, f)
        m.bits(x, 3, f)
    elif kind == "div_moving":
        m.col(f, {f + 2: "z"})
    elif kind == "step":
        return clamp_floor(f + rng.choice([-2, -1, 1, 2, 3]))
    elif kind == "combo":  # púas + hueco + púas: un salto largo
        a, w, b = rng.randint(1, 2), rng.randint(2, 4), rng.randint(1, 2)
        for _ in range(a):
            m.col(f, {f: "^"})
        m.flat(w, None)
        for _ in range(b):
            m.col(f, {f: "^"})
        m.bits(x + a, w, f + 4)
    elif kind == "corridor":  # púas con sierra encima: solo pasa un salto corto
        n = rng.randint(1, 3)
        for i in range(n):
            m.col(f, {f: "^", f + 3: "S"} if i == n // 2 else {f: "^"})
    elif kind == "sandwich":  # sierra abajo y sierra arriba seguidas: salto corto y preciso
        m.col(f, {f: "S"})
        m.col(f, {f + 3: "S"})
    return f


def build(level, cfg, rng):
    speed = cfg["speed"] * (1 + 0.15 * (level - 1))  # GameSession.SpeedMultiplier
    jump = simulate.JUMP_V / -simulate.GRAVITY * 2 * speed  # avance de un salto completo
    room = math.ceil(cfg["room"] * jump)
    m = Map()
    f = 2
    m.flat(22, f)
    m.items[3][f] = "P"
    world = simulate.World(m.floor, m.items)
    runs = [simulate.Run(world, speed, off) for off in simulate.OFFSETS]

    cp_every = cfg["length"] // (cfg["checkpoints"] + 1)
    next_cp = cp_every
    kinds, weights = zip(*cfg["weights"].items())
    placed = fallback = rejected = 0

    while len(m.floor) < cfg["length"]:
        if len(m.floor) >= next_cp:
            m.col(f, {f: "C"})
            m.flat(11, f)  # regla CP_CLEAR de check_level
            next_cp += cp_every
            continue
        start, snaps = len(m.floor), [r.snapshot() for r in runs]
        for _ in range(TRIES):
            nf = obstacle(rng.choices(kinds, weights)[0], m, f, cfg, rng)
            m.flat(room + rng.randint(0, cfg["extra"]), nf)
            if all(r.advance_to(len(m.floor) - 2) for r in runs):
                f = nf
                placed += 1
                break
            rejected += 1
            m.truncate(start)
            for r, s in zip(runs, snaps):
                r.restore(s)
        else:
            m.flat(room, f)  # nada pasó la simulación: suelo plano y seguimos
            fallback += 1

    # Vuelve a la altura 2 para la meta y deja 18 columnas planas de salida (regla FINISH_RUNOUT).
    if f != 2:
        f = 2
        m.flat(room + 4, f)
    m.flat(4, f)
    m.col(f, {f: "F"})
    m.flat(18, f)
    stats = f"{placed} obstáculos ({placed / (len(m.floor) / speed):.2f}/s) · {rejected} rechazados por imposibles · {fallback} tramos planos de relleno · {speed:.1f} u/s"
    return m, stats


def write(level, cfg, m):
    hints = [(1, 7, cfg["hints"][0]), (12, 7, cfg["hints"][1])]
    lines = [
        f"# RUNDASHC · Nivel {level}: {cfg['name']} (generado por Tools/level/generate_levels.py, no editar a mano).",
        "# Formato en Assets/Game/Scripts/Core/LevelData.cs. Validar con: python3 Tools/level/check_level.py",
        "# y python3 Tools/level/simulate.py (ruta sin golpes con holgura humana).",
        f"speed {cfg['speed']:g}",
        "map",
        *m.render(),
        "end",
        *[f"hint {c} {y} {t}" for c, y, t in hints],
    ]
    return "\n".join(lines) + "\n"


def main():
    ok = True
    for level, cfg in LEVELS.items():
        m, stats = build(level, cfg, random.Random(level * 7919))
        text = write(level, cfg, m)
        errors, report = check_level.check(text, dj_taught=True)
        passed, x = simulate.solve(text, level)
        if not passed:
            errors.append(f"simulate: imposible sin golpes cerca de la columna {x:.0f}")
        print(f"nivel{level}.txt: {report.splitlines()[0]} · {stats}")
        for e in errors:
            print("  ERROR:", e)
        if errors:
            ok = False
            continue
        (OUT / f"nivel{level}.txt").write_text(text, encoding="utf-8")
    sys.exit(0 if ok else 1)


if __name__ == "__main__":
    main()
