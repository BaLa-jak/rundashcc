#!/usr/bin/env python3
"""Valida un nivel de RUNDASHC (formato de Assets/Game/Scripts/Core/LevelData.cs) y las reglas de RunTuning.

Uso: python3 Tools/level/check_level.py [ruta]   (por defecto Resources/Levels/nivel1.txt)

Supone niveles "por columnas sólidas": cada columna es un pozo o un bloque de '=' desde y=0
(la superficie es la fila de encima del '=' más alto). Imprime un mapa de alturas y de huecos.
"""
import sys
from pathlib import Path

ROWS = 12
LEGEND = set("=^BSsZz*CFP ")
MAX_GAP, MAX_GAP_DJ = 4, 7    # RunTuning.MaxGapSingleJump / MaxGapDoubleJump
MAX_STEP, MAX_STEP_DJ = 2, 3  # RunTuning.MaxStepUp; +3 solo con doble salto ya enseñado
HAZARDS = set("^BSsZz")
GROUNDED = set("CPB^F")
CP_CLEAR = 10       # columnas sin peligro después de un checkpoint
FINISH_RUNOUT = 15  # suelo plano después de la meta
HUD_ROW = 11        # la barra superior del HUD tapa esta fila
DEFAULT = Path(__file__).resolve().parents[2] / "Assets/Game/Resources/Levels/nivel1.txt"


def parse(text):
    """Réplica de LevelData.Parse: devuelve (grid[x][y], hints) o lanza ValueError."""
    map_lines, hints = [], []
    in_map = done = False
    for no, line in enumerate(text.replace("\r", "").split("\n"), 1):
        if in_map:
            if line.strip() == "end":
                in_map, done = False, True
            else:
                map_lines.append(line)
            continue
        t = line.strip()
        if not t or t.startswith("#"):
            continue
        parts = t.split(None, 3)
        if parts[0] == "map":
            if done:
                raise ValueError(f"Línea {no}: solo se permite un bloque 'map'.")
            in_map = True
        elif parts[0] == "speed":
            if len(parts) < 2 or float(parts[1]) <= 0:
                raise ValueError(f"Línea {no}: 'speed' necesita un número > 0.")
        elif parts[0] == "hint":
            if len(parts) < 4:
                raise ValueError(f"Línea {no}: formato 'hint <col> <y> <texto>'.")
            hints.append((int(parts[1]), int(parts[2]), parts[3]))
        else:
            raise ValueError(f"Línea {no}: directiva desconocida '{parts[0]}'.")
    if in_map:
        raise ValueError("Falta 'end' al final del mapa.")
    if len(map_lines) != ROWS:
        raise ValueError(f"El mapa debe tener {ROWS} filas y tiene {len(map_lines)}.")
    width = max(len(l) for l in map_lines)
    grid = [[" "] * ROWS for _ in range(width)]
    for row, l in enumerate(map_lines):
        for x, c in enumerate(l.ljust(width)):
            if c not in LEGEND:
                raise ValueError(f"Mapa fila {row + 1}, columna {x}: carácter desconocido '{c}'.")
            grid[x][ROWS - 1 - row] = c
    return grid, hints


def check(text):
    """Devuelve (errores, informe). Sin errores = nivel válido."""
    try:
        grid, hints = parse(text)
    except ValueError as e:
        return [str(e)], ""
    W = len(grid)
    errors = []
    cells = {c: [(x, y) for x in range(W) for y in range(ROWS) if grid[x][y] == c] for c in LEGEND}
    if len(cells["P"]) != 1:
        errors.append(f"Necesita exactamente un 'P' (tiene {len(cells['P'])}).")
    if not cells["F"]:
        errors.append("Necesita al menos una 'F'.")

    # Superficie por columna (None = pozo).
    floor = []
    for x in range(W):
        solid = [y for y in range(ROWS) if grid[x][y] == "="]
        if not solid:
            floor.append(None)
            continue
        top = max(solid)
        if len(solid) != top + 1:
            errors.append(f"col {x}: '=' flotante (el validador asume columnas sólidas desde y=0).")
        floor.append(top + 1)

    dj_cols = [c for c, _, txt in hints if "doble salto" in txt]
    dj_from = min(dj_cols) if dj_cols else W  # desde aquí se permiten huecos/escalones de doble salto

    gap_lines, x = [], 0
    while x < W:
        if floor[x] is not None:
            x += 1
            continue
        a = x
        while x < W and floor[x] is None:
            x += 1
        b, w = x - 1, x - a
        dj = a >= dj_from
        limit = MAX_GAP_DJ if dj else MAX_GAP
        if a == 0 or x == W:
            errors.append(f"cols {a}-{b}: pozo en el borde del mapa.")
            continue
        step = floor[x] - floor[a - 1]
        kind = "DOBLE" if w > MAX_GAP else "simple"
        gap_lines.append(f"  cols {a:3}-{b:3}  ancho {w}  h {floor[a - 1]}->{floor[x]}  [{kind}]")
        if w > limit:
            errors.append(f"cols {a}-{b}: hueco de {w} > {limit}{'' if dj else ' (doble salto aún no enseñado)'}.")
        if step > (MAX_STEP_DJ if dj else MAX_STEP):
            errors.append(f"cols {a}-{b}: aterrizaje {step} más alto que el despegue.")

    for x in range(1, W):
        if floor[x] is not None and floor[x - 1] is not None:
            step = floor[x] - floor[x - 1]
            limit = MAX_STEP_DJ if x >= dj_from else MAX_STEP
            if step > limit:
                errors.append(f"col {x}: escalón de {step} > {limit}.")

    for c in GROUNDED:
        for x, y in cells[c]:
            if floor[x] != y:
                errors.append(f"'{c}' en ({x},{y}) no está apoyado en el suelo (superficie {floor[x]}).")

    def flat_run(x):
        l = r = x
        while l > 0 and floor[l - 1] == floor[x]:
            l -= 1
        while r < W - 1 and floor[r + 1] == floor[x]:
            r += 1
        return l, r

    for x, y in cells["B"]:
        l, r = flat_run(x)
        if r - l + 1 < 3:
            errors.append(f"bug en col {x}: solo {r - l + 1} de suelo plano para caminar.")

    def danger(x):
        return (floor[x] is None or any(grid[x][y] in HAZARDS for y in range(ROWS))
                or (x > 0 and floor[x - 1] is not None and floor[x] > floor[x - 1]))

    for x, y in cells["C"]:
        bad = [i for i in range(x + 1, min(W, x + CP_CLEAR + 1)) if danger(i)]
        if bad:
            errors.append(f"checkpoint en col {x}: peligro {bad[0] - x} columnas después (mín. {CP_CLEAR}).")

    for x, y in cells["F"][:1]:
        _, r = flat_run(x)
        clear = not any(danger(i) for i in range(x, min(W, x + FINISH_RUNOUT + 1)))
        if r - x < FINISH_RUNOUT or not clear:
            errors.append(f"meta en col {x}: necesita {FINISH_RUNOUT} columnas de suelo plano y libre después.")

    for x in range(W):
        if grid[x][HUD_ROW] not in " =":
            errors.append(f"'{grid[x][HUD_ROW]}' en ({x},{HUD_ROW}): lo tapa la barra del HUD.")
    for c, y, txt in hints:
        if not (0 <= c < W and 0 <= y < HUD_ROW):
            errors.append(f"hint fuera de la vista: {c} {y} {txt}")

    # Informe: mapa de alturas (dígito = superficie, '_' = pozo) con regla de columnas.
    rep = [f"ancho {W} columnas (~{W / 8:.0f} s a 8 u/s) · doble salto desde col {dj_from}"]
    for s in range(0, W, 100):
        e = min(W, s + 100)
        rep.append("      " + "".join(str(i // 10 % 10) if i % 10 == 0 else " " for i in range(s, e)))
        rep.append(f"{s:4}  " + "".join("_" if floor[i] is None else str(floor[i]) for i in range(s, e)))
        rep.append("      " + "".join(next((grid[i][y] for y in range(ROWS) if grid[i][y] in HAZARDS | set("CPF")), " ")
                                      for i in range(s, e)))
    rep.append(f"huecos ({len(gap_lines)}):")
    rep += gap_lines
    count = lambda cs: sum(len(cells[c]) for c in cs)
    rep.append(f"bits {count('*')} · checkpoints {[x for x, _ in sorted(cells['C'])]} · "
               f"púas {count('^')} · bugs {count('B')} · sierras {count('S')}+{count('s')} móviles · "
               f"/0 {count('Z')}+{count('z')} móviles · hints {len(hints)}")
    return errors, "\n".join(rep)


def _selftest():
    ground = "=" * 45
    ok = "map\n" + "\n" * 9 + "  P" + " " * 10 + "C" + " " * 10 + "F\n" + ground + "\n" + ground + "\nend\n"
    assert check(ok)[0] == [], check(ok)[0]
    bad = ok.replace(ground, "=====" + " " * 5 + "=" * 20)  # hueco de 5 sin doble salto
    assert any("hueco de 5" in e for e in check(bad)[0])
    assert check("map\nend\n")[0]  # filas de menos


def main():
    _selftest()
    path = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT
    errors, report = check(path.read_text(encoding="utf-8"))
    print(report)
    for e in errors:
        print("ERROR:", e)
    print(f"{path.name}: {'OK' if not errors else f'{len(errors)} errores'}")
    sys.exit(1 if errors else 0)


if __name__ == "__main__":
    main()
