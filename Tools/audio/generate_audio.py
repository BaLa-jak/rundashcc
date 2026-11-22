#!/usr/bin/env python3
"""Sintetiza los SFX y la música chiptune de RUNDASHC en Assets/Game/Resources/Audio/.

Uso: python3 -I Tools/audio/generate_audio.py   (solo biblioteca estándar; determinista por la seed)
Los nombres siguen el contrato de AudioManager.FileName: Sfx.DoubleJump -> sfx_double_jump.wav.
"""
import math
import random
import struct
import wave
from pathlib import Path

SR = 44100
OUT = Path(__file__).resolve().parents[2] / "Assets/Game/Resources/Audio"
rng = random.Random(1987)

SEMI = {"C": 0, "C#": 1, "D": 2, "D#": 3, "E": 4, "F": 5, "F#": 6, "G": 7, "G#": 8, "A": 9, "A#": 10, "B": 11}


def hz(note):
    """'A4' -> 440.0"""
    return 440 * 2 ** ((12 * (int(note[-1]) + 1) + SEMI[note[:-1]] - 69) / 12)


def add(buf, t, gate, f0, f1=None, wave="square", duty=0.5, vol=0.5,
        a=0.002, d=0.08, s=0.6, r=0.04, sweep=None, vib=0.0, lp=None):
    """Suma una voz a buf desde t (s): ADSR con la nota pulsada `gate` s y luego release `r`.
    El tono va de f0 a f1 (exponencial) en `sweep` s. wave: square (con duty), tri, saw, sine o noise
    (ruido sample-and-hold: f0 es la tasa de cambio, SR = ruido blanco). lp = filtro paso bajo de 1 polo."""
    f1 = f1 or f0
    start = round(t * SR)
    na, nd, ng, nr = max(1, int(a * SR)), max(1, int(d * SR)), int(gate * SR), max(1, int(r * SR))
    nsw = max(1, int((sweep or gate + r) * SR))
    ratio = math.log(f1 / f0)
    k = 1 - math.exp(-2 * math.pi * lp / SR) if lp else 1.0
    dc = 2 * duty - 1  # quita el offset DC de la cuadrada con duty != 50 %
    phase = y = lvl = 0.0
    held = rng.uniform(-1, 1)
    for i in range(min(ng + nr, len(buf) - start)):
        if i < ng:
            lvl = i / na if i < na else s + (1 - s) * math.exp(-5 * (i - na) / nd)
            env = lvl
        else:
            x = (i - ng) / nr
            env = lvl * math.exp(-4 * x) * (1 - x)  # llega exactamente a 0: sin clics
        f = f0 * math.exp(ratio * min(i, nsw) / nsw) if ratio else f0
        if vib and i > 0.12 * SR:
            f *= 2 ** (vib * math.sin(2 * math.pi * 5.5 * i / SR) / 12)
        phase += f / SR
        if phase >= 1:
            phase -= int(phase)
            if wave == "noise":
                held = rng.uniform(-1, 1)
        if wave == "square":
            v = (1.0 if phase < duty else -1.0) - dc
        elif wave == "tri":
            v = 4 * abs(phase - 0.5) - 1
        elif wave == "saw":
            v = 2 * phase - 1
        elif wave == "sine":
            v = math.sin(2 * math.pi * phase)
        else:
            v = held
        y += k * (v - y)
        buf[start + i] += y * env * vol


def seq(buf, t0, step, bars, gate=0.9, **kw):
    """Tracker mínimo: cada token es un paso; nota ('A4'), '-' la sostiene, '.' silencio."""
    toks = " ".join(bars).split()
    for i, tok in enumerate(toks):
        if tok in ".-":
            continue
        n = 1
        while i + n < len(toks) and toks[i + n] == "-":
            n += 1
        add(buf, t0 + i * step, n * step * gate, hz(tok), **kw)


def kick(b, t, vol=0.9):
    add(b, t, 0.11, 165, 42, "sine", vol=vol, a=0.001, d=0.09, s=0.0, r=0.06, sweep=0.07)
    add(b, t, 0.004, SR, wave="noise", vol=vol * 0.25, a=0.0005, d=0.003, s=0.0, r=0.002)


def snare(b, t, vol=0.5):
    add(b, t, 0.09, SR, wave="noise", vol=vol, a=0.001, d=0.07, s=0.0, r=0.05, lp=7000)
    add(b, t, 0.05, 230, 150, "tri", vol=vol * 0.8, a=0.001, d=0.05, s=0.0, r=0.03)


def hat(b, t, vol=0.12):
    add(b, t, 0.025, SR, wave="noise", vol=vol, a=0.0005, d=0.018, s=0.0, r=0.012)


# ---------------------------------------------------------------- SFX
def sfx_click(b):
    add(b, 0, 0.045, 880, 1320, duty=0.5, vol=0.6, a=0.001, d=0.04, s=0.3, r=0.02, sweep=0.03)


def sfx_hover(b):
    add(b, 0, 0.012, 1760, wave="tri", vol=0.5, a=0.001, d=0.01, s=0.0, r=0.012)


def sfx_type(b):
    add(b, 0, 0.006, SR, wave="noise", vol=0.6, a=0.0005, d=0.005, s=0.0, r=0.004, lp=6000)
    add(b, 0.002, 0.018, 1150, 1000, duty=0.25, vol=0.25, a=0.001, d=0.015, s=0.0, r=0.01)


def sfx_start(b):
    for i, n in enumerate(["A4", "C5", "E5", "A5", "C6", "E6"]):
        add(b, i * 0.045, 0.04, hz(n), duty=0.25, vol=0.35, a=0.001, d=0.05, s=0.5, r=0.02)
        add(b, i * 0.045, 0.04, hz(n) / 2, wave="tri", vol=0.3, a=0.001, d=0.05, s=0.5, r=0.02)
    for n in ("A5", "E6", "A6"):  # "OK"
        add(b, 0.29, 0.1, hz(n), duty=0.5, vol=0.22, a=0.002, d=0.08, s=0.6, r=0.12, vib=0.1)


def sfx_jump(b):
    add(b, 0, 0.11, 280, 720, duty=0.5, vol=0.5, a=0.002, d=0.1, s=0.4, r=0.04)


def sfx_double_jump(b):
    add(b, 0, 0.04, 520, 900, duty=0.25, vol=0.45, a=0.001, d=0.04, s=0.3, r=0.015)
    add(b, 0.05, 0.1, 620, 1500, duty=0.25, vol=0.5, a=0.001, d=0.08, s=0.35, r=0.04)


def sfx_land(b):
    add(b, 0, 0.03, 140, 55, "tri", vol=0.9, a=0.001, d=0.03, s=0.2, r=0.03)
    add(b, 0, 0.02, 3000, wave="noise", vol=0.35, a=0.0005, d=0.015, s=0.0, r=0.015, lp=1500)


def sfx_bit(b):
    add(b, 0, 0.035, hz("B5"), duty=0.5, vol=0.45, a=0.001, d=0.05, s=0.8, r=0.005)
    add(b, 0.04, 0.04, hz("E6"), duty=0.5, vol=0.45, a=0.001, d=0.06, s=0.5, r=0.05)


def sfx_hit(b):
    add(b, 0, 0.22, 700, 70, duty=0.5, vol=0.4, a=0.001, d=0.2, s=0.3, r=0.12)
    add(b, 0, 0.22, 745, 74, duty=0.3, vol=0.3, a=0.001, d=0.2, s=0.3, r=0.12)  # desafinada: aspereza
    add(b, 0, 0.2, 9000, 1500, "noise", vol=0.5, a=0.001, d=0.15, s=0.2, r=0.13)


def sfx_stomp(b):
    add(b, 0, 0.05, 260, 70, duty=0.5, vol=0.55, a=0.001, d=0.05, s=0.1, r=0.02)
    add(b, 0, 0.03, 4000, wave="noise", vol=0.4, a=0.0005, d=0.025, s=0.0, r=0.02, lp=2500)
    add(b, 0.08, 0.08, 660, 1320, duty=0.25, vol=0.4, a=0.001, d=0.06, s=0.4, r=0.04)


def sfx_checkpoint(b):
    for t, n, g, r in ((0, "E5", 0.08, 0.02), (0.1, "A5", 0.12, 0.09)):
        add(b, t, g, hz(n), duty=0.25, vol=0.4, a=0.002, d=0.1, s=0.6, r=r, vib=0.15)
        add(b, t, g, hz(n) / 2, wave="tri", vol=0.35, a=0.002, d=0.1, s=0.6, r=r)


def sfx_pause(b):
    add(b, 0, 0.07, hz("E5"), wave="tri", vol=0.5, a=0.003, d=0.06, s=0.5, r=0.02)
    add(b, 0.09, 0.07, hz("A4"), wave="tri", vol=0.5, a=0.003, d=0.06, s=0.5, r=0.06)


def sfx_countdown(b):
    add(b, 0, 0.08, hz("E5"), duty=0.5, vol=0.45, a=0.001, d=0.1, s=0.7, r=0.025)


def sfx_go(b):
    add(b, 0, 0.2, hz("E6"), duty=0.5, vol=0.35, a=0.001, d=0.1, s=0.7, r=0.05, vib=0.2)
    add(b, 0, 0.2, hz("E5"), wave="tri", vol=0.4, a=0.001, d=0.1, s=0.7, r=0.05)


def sfx_level_complete(b):
    for i, n in enumerate(["A4", "C#5", "E5", "A5", "C#6", "E6"]):
        add(b, i * 0.07, 0.06, hz(n), duty=0.25, vol=0.35, a=0.001, d=0.06, s=0.6, r=0.02)
    for i, n in enumerate(["A3", "E3"]):
        add(b, i * 0.21, 0.19, hz(n), wave="tri", vol=0.5, a=0.002, d=0.1, s=0.7, r=0.02)
    for n, duty in (("A5", 0.5), ("C#6", 0.25), ("E6", 0.25)):  # acorde final de victoria
        add(b, 0.42, 0.75, hz(n), duty=duty, vol=0.2, a=0.004, d=0.2, s=0.7, r=0.3, vib=0.18)
    add(b, 0.42, 0.75, hz("A2"), wave="tri", vol=0.55, a=0.004, d=0.2, s=0.8, r=0.3)


def sfx_game_over(b):
    for i, (n, g) in enumerate([("E5", 0.3), ("D#5", 0.3), ("D5", 0.3), ("C#5", 0.6)]):
        vib = 0.35 if g > 0.3 else 0.0
        add(b, i * 0.36, g, hz(n), duty=0.5, vol=0.35, a=0.004, d=0.15, s=0.6, r=0.2, vib=vib)
        add(b, i * 0.36, g, hz(n) / 4, wave="tri", vol=0.45, a=0.004, d=0.15, s=0.7, r=0.2, vib=vib)
    for k in range(16):  # glitch final: notas al azar cada vez más débiles + ruido
        t = 1.95 + k * 0.03
        add(b, t, 0.02, rng.uniform(150, 2500), duty=rng.choice((0.125, 0.25, 0.5)),
            vol=0.35 * (1 - k / 16), a=0.0005, d=0.02, s=0.5, r=0.005)
    add(b, 1.95, 0.4, 2500, 300, "noise", vol=0.25, a=0.001, d=0.3, s=0.1, r=0.12)


SFX = {  # nombre: (duración s, función, pico dBFS)
    "sfx_click": (0.07, sfx_click, -1),
    "sfx_hover": (0.03, sfx_hover, -10),  # se dispara mucho: más bajo
    "sfx_type": (0.035, sfx_type, -6),    # idem (typewriter)
    "sfx_start": (0.55, sfx_start, -1),
    "sfx_jump": (0.16, sfx_jump, -1),
    "sfx_double_jump": (0.2, sfx_double_jump, -1),
    "sfx_land": (0.07, sfx_land, -1),
    "sfx_bit": (0.13, sfx_bit, -1),
    "sfx_hit": (0.36, sfx_hit, -1),
    "sfx_stomp": (0.21, sfx_stomp, -1),
    "sfx_checkpoint": (0.32, sfx_checkpoint, -1),
    "sfx_pause": (0.22, sfx_pause, -1),
    "sfx_countdown": (0.11, sfx_countdown, -1),
    "sfx_go": (0.26, sfx_go, -1),
    "sfx_level_complete": (1.5, sfx_level_complete, -1),
    "sfx_game_over": (2.5, sfx_game_over, -1),
}

# ---------------------------------------------------------------- Música
CHORD = {  # bajo, notas del acorde
    "Am": ("A2", ("A3", "C4", "E4")), "F": ("F2", ("F3", "A3", "C4")), "C": ("C3", ("C4", "E4", "G4")),
    "G": ("G2", ("G3", "B3", "D4")), "Dm": ("D3", ("D4", "F4", "A4")), "E": ("E2", ("E3", "G#3", "B3")),
    "Em": ("E2", ("E3", "G3", "B3")), "Gm": ("G2", ("G3", "A#3", "D4")), "A": ("A2", ("A3", "C#4", "E4")),
    "Bb": ("A#2", ("A#3", "D4", "F4")),
}


def loop_buffer(bpm, bars):
    n = bars * 4 * 60 * SR
    assert n % bpm == 0, "el loop debe medir un número exacto de muestras"
    return [0.0] * (n // bpm), 60 / bpm / 4  # buffer, duración de una semicorchea


def music_menu():
    """Synthwave tranquilo en La menor, 100 BPM, 8 compases."""
    b, st = loop_buffer(100, 8)
    prog = ["Am", "F", "C", "G", "Am", "F", "Dm", "E"]
    arp = [0, 1, 2, 3, 1, 2, 3, 4, 2, 3, 4, 5, 4, 3, 2, 1]
    for bar, name in enumerate(prog):
        t0 = bar * 16 * st
        root, tones = CHORD[name]
        last = bar == len(prog) - 1
        for i in range(8):  # bajo en triángulo, corcheas que laten
            add(b, t0 + i * 2 * st, 1.6 * st, hz(root), wave="tri", vol=0.55 if i % 2 == 0 else 0.42,
                a=0.005, d=0.12, s=0.6, r=0.05)
        high = [hz(n) * 2 for n in tones] + [hz(n) * 4 for n in tones]
        for i, idx in enumerate(arp):  # arpegio en cuadrado suave + eco de corchea con puntillo
            add(b, t0 + i * st, 0.06, high[idx], duty=0.25, vol=0.13, a=0.002, d=0.08, s=0.3, r=0.06, lp=3500)
            if not (last and i >= 13):
                add(b, t0 + (i + 3) * st, 0.06, high[idx], duty=0.25, vol=0.05, a=0.002, d=0.08, s=0.3, r=0.06, lp=2500)
        gate = 16 * st - (0.7 if last else 0.25)  # el último acorde se apaga antes del final del loop
        for n in tones:  # pad: dos sierras desafinadas con paso bajo
            for det in (-0.1, 0.1):
                add(b, t0, gate, hz(n) * 2 ** (det / 12), wave="saw", vol=0.07, a=0.5, d=0.6, s=0.8, r=0.6, lp=1400)
        for beat in (0, 8):
            kick(b, t0 + beat * st, vol=0.35)
        if bar >= 2:
            for i in range(2, 16, 4):
                hat(b, t0 + i * st, vol=0.05)
    melody = [  # aparece en la segunda mitad
        "E5 - - - - - . . C5 - D5 - E5 - - -",
        "A5 - - - - - . . G5 - E5 - C5 - - -",
        "D5 - - - F5 - - - A5 - - - G5 - F5 -",
        "E5 - - - - - - - G#4 - - - . . . .",  # sin nota al final: el eco no se pasa del loop
    ]
    seq(b, 4 * 16 * st, st, melody, gate=0.85, duty=0.125, vol=0.15, a=0.004, d=0.2, s=0.6, r=0.1, vib=0.15, lp=4000)
    seq(b, 4 * 16 * st + 3 * st, st, melody, gate=0.85, duty=0.125, vol=0.05, a=0.004, d=0.2, s=0.6, r=0.1, lp=2500)
    return b, melody


def music_level1():
    """Chiptune enérgico, 140 BPM, 16 compases (A: compases 1-8, B: 9-16 con más energía)."""
    b, st = loop_buffer(140, 16)
    prog = ["Am", "F", "C", "G", "Am", "F", "C", "G", "F", "G", "Em", "Am", "F", "G", "E", "E"]
    lead = [
        "A4 - A4 C5 - E5 - A5 - G5 E5 - D5 - E5 -",
        "C5 - - - A4 - - - . . C5 - D5 - E5 -",
        "G4 - G4 C5 - E5 - G5 - A5 G5 - E5 - C5 -",
        "D5 - - - B4 - - - . . G4 - A4 - B4 -",
        "A4 - A4 C5 - E5 - A5 - G5 E5 - D5 - E5 -",
        "C5 - - - A4 - - - . . A4 - C5 - D5 -",
        "E5 - E5 G5 - C6 - B5 - A5 G5 - E5 - G5 -",
        "A5 - - - - - - - G5 - - - B5 - - -",
        "C6 - - - A5 - - - F5 - A5 - C6 - A5 -",
        "B5 - - - G5 - - - D5 - G5 - B5 - D6 -",
        "E6 - - - B5 - - - G5 - B5 - E6 - D6 -",
        "C6 - - - - - A5 - - - E5 - A5 - C6 -",
        "A5 - - - C6 - - - F6 - - - E6 - C6 -",
        "D6 - - - B5 - - - G5 - - - A5 - B5 -",
        "G#5 - - - B5 - - - E6 - - - D6 - B5 -",
        "B5 - - - G#5 - - - E5 - D5 - B4 - G#4 -",
    ]
    for bar, name in enumerate(prog):
        t0 = bar * 16 * st
        root, tones = CHORD[name]
        part_b = bar >= 8
        fill = bar in (7, 15)
        for i in range(8):  # bajo pulsante: octavas en corcheas
            f = hz(root) * (2 if i % 2 else 1)
            add(b, t0 + i * 2 * st, 1.5 * st, f, wave="tri", vol=0.6, a=0.002, d=0.08, s=0.6, r=0.03)
        chord = [hz(n) * 2 for n in tones] + [hz(tones[0]) * 4]
        for i in range(16):  # arpegio de acorde estilo NES
            add(b, t0 + i * st, 0.045, chord[i % 4], duty=0.125, vol=0.06, a=0.001, d=0.04, s=0.3, r=0.03)
        for i in ((0, 4, 8, 12) if part_b else (0, 8, 10)):
            kick(b, t0 + i * st)
        for i in (4, 12):
            snare(b, t0 + i * st)
        if fill:
            for i, v in ((13, 0.3), (14, 0.4), (15, 0.5)):
                snare(b, t0 + i * st, vol=v)
        for i in range(0, 16, 1 if part_b else 2):
            if not (fill and i >= 13):
                hat(b, t0 + i * st, vol=0.11 if i % 4 == 2 else 0.06)
    add(b, 8 * 16 * st, 0.05, SR, wave="noise", vol=0.18, a=0.001, d=0.6, s=0.0, r=0.5, lp=9000)  # crash: entra la parte B
    seq(b, 0, st, lead, gate=0.85, duty=0.25, vol=0.2, a=0.003, d=0.1, s=0.7, r=0.04, vib=0.12)
    seq(b, 8 * 16 * st, st, lead[8:], gate=0.85, duty=0.5, vol=0.07, a=0.003, d=0.1, s=0.7, r=0.04)  # B: lead más gordo
    return b, lead


def music_shop():
    """Mercado negro del duende: Re menor, 96 BPM, 8 compases, bajo caminante con swing y lead sigiloso."""
    b, st = loop_buffer(96, 8)
    prog = ["Dm", "Gm", "A", "Dm", "Bb", "Gm", "A", "A"]
    walk = [0, 2, 1, 3]  # índices en [raíz, 3a, 5a, 8a]: el bajo "camina"
    lead = [
        "D5 - . F5 - . A5 - G#5 - A5 - . . . .",
        "A#5 - . A5 - . G5 - F5 - - - . . . .",
        "E5 - . G5 - . C#5 - E5 - A4 - - - . .",
        "D5 - - - - - . . A4 - C#5 - D5 - . .",
        "F5 - . A#5 - . D6 - C6 - A#5 - . . . .",
        "A#5 - . A5 - . G5 - A#5 - D5 - - - . .",
        "C#5 - E5 - G5 - A#5 - A5 - G5 - E5 - C#5 -",
        "A4 - - - - - - - . . . . . . . .",
    ]
    for bar, name in enumerate(prog):
        t0 = bar * 16 * st
        root, tones = CHORD[name]
        notes = [hz(root), hz(tones[1]) / 2, hz(tones[2]) / 2, hz(root) * 2]
        for k in range(8):  # corcheas con swing: la de contratiempo se retrasa 1/3 de semicorchea
            t = t0 + k * 2 * st + (st / 3 if k % 2 else 0)
            add(b, t, 0.6 * st, notes[walk[k // 2]] if k % 2 == 0 else notes[0] * 2, wave="tri",
                vol=0.55 if k % 2 == 0 else 0.3, a=0.002, d=0.07, s=0.2, r=0.05)  # pizzicato
            if k % 2:  # acorde staccato en el contratiempo
                for n in tones:
                    add(b, t, 0.05, hz(n) * 2, duty=0.25, vol=0.045, a=0.001, d=0.04, s=0.2, r=0.03, lp=3000)
            hat(b, t, vol=0.07 if k % 2 else 0.04)
        kick(b, t0, vol=0.4)
        kick(b, t0 + 8 * st, vol=0.3)
        snare(b, t0 + 12 * st + st / 3, vol=0.18)
    seq(b, 0, st, lead, gate=0.7, duty=0.25, vol=0.16, a=0.003, d=0.12, s=0.5, r=0.06, vib=0.2, lp=4500)
    return b, lead


# ---------------------------------------------------------------- Escritura y self-check
def save(name, buf, peak_db):
    fade = int(0.002 * SR)  # la primera y la última muestra quedan en 0 (sin clic al hacer loop)
    for i in range(fade):
        g = i / fade
        buf[i] *= g
        buf[-1 - i] *= g
    peak = max(abs(v) for v in buf) or 1.0
    gain = 10 ** (peak_db / 20) * 32767 / peak
    with wave.open(str(OUT / f"{name}.wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(struct.pack(f"<{len(buf)}h", *(round(v * gain) for v in buf)))


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for name, (secs, fn, peak) in SFX.items():
        buf = [0.0] * round(secs * SR)
        fn(buf)
        save(name, buf, peak)
    menu, mel = music_menu()
    save("music_menu", menu, -1)
    level, lead = music_level1()
    save("music_level1", level, -1)
    shop, shop_lead = music_shop()
    save("music_shop", shop, -1)

    # self-check
    for bars in (mel, lead, shop_lead):
        assert all(len(bar.split()) == 16 for bar in bars), "cada compás debe tener 16 semicorcheas"
    expected = {n: (s * 0.8, s * 1.05) for n, (s, _, _) in SFX.items()}
    expected["music_menu"] = (19.2, 19.2)
    expected["music_level1"] = (64 * 60 / 140, 64 * 60 / 140)
    expected["music_shop"] = (20.0, 20.0)
    assert len(expected) == 19
    for name, (lo, hi) in expected.items():
        path = OUT / f"{name}.wav"
        assert path.exists(), path
        with wave.open(str(path)) as w:
            assert (w.getnchannels(), w.getsampwidth(), w.getframerate()) == (1, 2, SR), name
            n = w.getnframes()
            data = struct.unpack(f"<{n}h", w.readframes(n))
        dur = n / SR
        assert lo - 1e-9 <= dur <= hi + 1e-9, f"{name}: {dur:.3f}s fuera de [{lo}, {hi}]"
        peak = max(abs(v) for v in data)
        assert peak < 32767, f"{name}: clipping"
        assert abs(data[0]) < 100 and abs(data[-1]) < 100, f"{name}: no empieza/termina cerca de 0"
        print(f"{name}.wav {dur:.3f}s ({n} muestras) pico {20 * math.log10(peak / 32767):.1f} dBFS")
    assert len(menu) == 846720 and len(level) == 1209600 and len(shop) == 882000  # 8 @100, 16 @140, 8 @96


if __name__ == "__main__":
    main()
