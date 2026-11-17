#!/usr/bin/env python3
"""Simulador de Camarada: ¿se puede pasar un nivel SIN recibir golpes?

Uso: python3 Tools/level/simulate.py [ruta ...]   (sin ruta: todos los Resources/Levels/nivel*.txt)

Réplica de CamaradaController + RunTuning a 50 Hz (Fixed Timestep 0.02): auto-run, salto variable
(soltar corta la subida ×0.5 tras 0.08 s), un doble salto, paredes = golpe. Explora TODAS las
secuencias de input (búsqueda en anchura con estados deduplicados) con reglas de holgura humana:
  - el jugador solo puede presionar/soltar cada DECIDE ticks (60 ms): nada de inputs frame-perfect;
  - los peligros "crecen" MARGIN unidades;
  - los peligros móviles se prueban con varias fases (Time.time al empezar es impredecible);
  - sin coyote time, sin buffer y sin pisar bugs (todo eso solo ayuda al jugador real).
Si el simulador pasa, un jugador muy bueno puede pasar el nivel sin perder vidas.
"""
import math
import sys
from pathlib import Path

import check_level

DT = 0.02
GRAVITY = -9.81 * 4          # RunTuning.GravityScale
JUMP_V, DJ_V = 14.3, 12.5    # RunTuning.JumpVelocity / DoubleJumpVelocity
CUT, MIN_CUT_TICKS = 0.5, 4  # JumpCutMultiplier, MinJumpTimeBeforeCut (0.08 s)
HALF_W, HALF_H = 0.45, 0.3   # collider 0.8x0.5 + edgeRadius 0.05
FALL_Y = -4.0                # RunTuning.FallDeathY
WAKE, BUG_SPEED = 16.0, 1.5  # BugEnemy
DECIDE = 3                   # holgura: un input cada 60 ms
MARGIN = 0.1                 # holgura: peligros 0.1 u más grandes
OFFSETS = [i * 0.5 for i in range(8)]  # fases de sierras (periodo π s) y /0 móviles (periodo 3.9 s)

# Formas de los peligros relativas al centro de la celda (cx, cy) = (x + 0.5, y + 0.5).
SPIKE = (0.375, -0.5, -0.08)   # medio ancho, y0, y1  (SpikesHazard: 0.75x0.42 apoyado abajo)
BUG = (0.375, -0.475, 0.075)   # BugEnemy: 0.75x0.55 con offset -0.2
SAW_R, DIV_R = 0.6, 0.42


def box_hits_box(px0, px1, py0, py1, bx0, bx1, by0, by1):
    return px0 < bx1 and bx0 < px1 and py0 < by1 and by0 < py1


def box_hits_circle(px0, px1, py0, py1, cx, cy, r):
    dx = max(px0 - cx, 0.0, cx - px1)
    dy = max(py0 - cy, 0.0, cy - py1)
    return dx * dx + dy * dy < r * r


class World:
    """floor[x] = superficie de la columna (None = pozo); items[x] = {y: char}. Las listas pueden crecer."""

    def __init__(self, floor, items):
        self.floor, self.items = floor, items

    @classmethod
    def from_text(cls, text):
        grid, _ = check_level.parse(text)
        floor, items = [], []
        for col in grid:
            solid = [y for y in range(len(col)) if col[y] == "="]
            floor.append(max(solid) + 1 if solid else None)
            items.append({y: c for y, c in enumerate(col) if c not in "= "})
        return cls(floor, items)

    def find(self, char):
        return next((x, y) for x, col in enumerate(self.items) for y, c in col.items() if c == char)

    def top(self, x):
        f = self.floor[x] if 0 <= x < len(self.floor) else None
        return -1e9 if f is None else f


class Run:
    """Corrida con una fase fija: conjunto de estados posibles del jugador, tick a tick.
    Estado: (y, vy, grounded, air_jumps, hold, jumping, cut_done, ticks_since_jump)."""

    def __init__(self, world, speed, offset):
        self.w, self.speed, self.offset = world, speed, offset
        px, py = world.find("P")
        self.x0 = px + 0.5
        self.tick = 0
        self.states = [(py + 0.32, 0.0, True, 1, False, False, False, 99)]
        self.bugs = []        # [x, y_celda, dir]
        self.scanned = 0      # columnas ya revisadas en busca de 'B'

    @property
    def x(self):
        return self.x0 + self.speed * self.tick * DT

    def snapshot(self):
        return self.tick, list(self.states), [b[:] for b in self.bugs], self.scanned

    def restore(self, snap):
        self.tick, states, bugs, self.scanned = snap
        self.states, self.bugs = list(states), [b[:] for b in bugs]

    def _update_bugs(self, px):
        w = self.w
        while self.scanned < min(len(w.items), int(px + WAKE + 2)):
            for y, c in w.items[self.scanned].items():
                if c == "B":
                    self.bugs.append([self.scanned + 0.5, y, -1.0])
            self.scanned += 1
        for b in self.bugs:
            if abs(px - b[0]) > WAKE:
                continue
            cx = math.floor(b[0] + b[2] * 0.45)
            if w.top(cx) != b[1]:  # sin suelo delante o pared: da la vuelta
                b[2] = -b[2]
            else:
                b[0] += b[2] * BUG_SPEED * DT

    def step(self):
        w = self.w
        px = self.x
        self._update_bugs(px)
        self.tick += 1
        x = self.x
        t = self.offset + self.tick * DT
        decide = self.tick % DECIDE == 0

        c0, c1 = math.floor(x - HALF_W), math.floor(x + HALF_W - 1e-9)
        top = max(w.top(c) for c in range(c0, c1 + 1))
        hazards = []
        for c in range(math.floor(x - 2), math.floor(x + 2) + 1):
            if 0 <= c < len(w.items):
                for y, ch in w.items[c].items():
                    cx, cy = c + 0.5, y + 0.5
                    if ch == "^":
                        hazards.append(("box", cx - SPIKE[0], cx + SPIKE[0], cy + SPIKE[1], cy + SPIKE[2]))
                    elif ch in "Ss":
                        dy = 2.0 * math.sin(t * 2.0 + cx * 0.37) if ch == "s" else 0.0
                        hazards.append(("circle", cx, cy + dy, SAW_R))
                    elif ch in "Zz":
                        dy = 2.0 * math.sin(t * 1.6 + cx * 0.53) if ch == "z" else 0.0
                        hazards.append(("circle", cx, cy + dy, DIV_R))
        for bx, by, _ in self.bugs:
            if abs(bx - x) < 2:
                cy = by + 0.5
                hazards.append(("box", bx - BUG[0], bx + BUG[0], cy + BUG[1], cy + BUG[2]))
        px0, px1 = x - HALF_W - MARGIN, x + HALF_W + MARGIN

        seen = {}
        for s in self.states:
            for s2 in self._inputs(s, decide):
                r = self._physics(s2, top, hazards, px0, px1)
                if r is not None:
                    seen.setdefault((round(r[0] * 50), round(r[1] * 10)) + r[2:7] + (min(r[7], MIN_CUT_TICKS),), r)
        self.states = list(seen.values())
        return bool(self.states)

    @staticmethod
    def _inputs(s, decide):
        yield s
        if not decide:
            return
        y, vy, gr, air, hold, jumping, cut, tj = s
        if hold:
            yield (y, vy, gr, air, False, jumping, cut, tj)
        elif gr:
            yield (y, JUMP_V, False, air, True, True, False, 0)
        elif air > 0:
            yield (y, DJ_V, False, air - 1, True, True, False, 0)

    @staticmethod
    def _physics(s, top, hazards, px0, px1):
        y, vy, gr, air, hold, jumping, cut, tj = s
        if jumping and not cut and vy > 0 and tj >= MIN_CUT_TICKS and not hold:
            vy *= CUT
            cut = True
        vy += GRAVITY * DT
        ny = y + vy * DT
        feet_prev, feet = y - HALF_H, ny - HALF_H
        if feet < top:
            if feet_prev < top - 1e-3:
                return None  # chocó de lado con un bloque = golpe
            ny, vy, gr, air, jumping = top + HALF_H, 0.0, True, 1, False
        else:
            gr = False
        if ny < FALL_Y:
            return None
        py0, py1 = ny - HALF_H - MARGIN, ny + HALF_H + MARGIN
        for h in hazards:
            if h[0] == "box":
                if box_hits_box(px0, px1, py0, py1, h[1], h[2], h[3], h[4]):
                    return None
            elif box_hits_circle(px0, px1, py0, py1, h[1], h[2], h[3]):
                return None
        return (ny, vy, gr, air, hold, jumping, cut, tj + 1)

    def advance_to(self, x_end):
        """Avanza hasta que el jugador llega a x_end. False si ningún estado sobrevive."""
        while self.x < x_end:
            if not self.step():
                return False
        return True


def speed_of(text, level):
    speed = next((float(l.split()[1]) for l in text.splitlines() if l.strip().startswith("speed")), 8.0)
    return speed * (1 + 0.15 * (level - 1))  # GameSession.SpeedMultiplier


def solve(text, level):
    """Devuelve (ok, peor_x): peor_x = columna donde murió la primera fase que falló."""
    world = World.from_text(text)
    finish = world.find("F")[0]
    speed = speed_of(text, level)
    for off in OFFSETS:
        run = Run(world, speed, off)
        if not run.advance_to(finish + 0.5):
            return False, run.x
    return True, finish


def _selftest():
    def level(ground):
        return "map\n" + "\n" * 9 + "  P" + " " * 40 + "F\n" + ground + "\n" + ground + "\nend\n"
    ok = "=" * 20 + " " * 4 + "=" * 40
    assert solve(level(ok), 1)[0], "un hueco de 4 debe poderse"
    assert not solve(level("=" * 20 + " " * 14 + "=" * 30), 1)[0], "un hueco de 14 no se puede"
    spikes = level(ok).replace("  P" + " " * 40, "  P" + " " * 22 + "^" * 12 + " " * 6)
    assert not solve(spikes, 1)[0], "12 púas seguidas no se pueden saltar"


def main():
    _selftest()
    paths = [Path(p) for p in sys.argv[1:]] or sorted(check_level.LEVELS.glob("nivel*.txt"))
    ok = True
    for p in paths:
        level = int("".join(ch for ch in p.stem if ch.isdigit()) or 1)
        passed, x = solve(p.read_text(encoding="utf-8"), level)
        print(f"{p.name}: {'se puede pasar sin golpes' if passed else f'IMPOSIBLE sin golpes cerca de la columna {x:.0f}'}")
        ok &= passed
    sys.exit(0 if ok else 1)


if __name__ == "__main__":
    main()
