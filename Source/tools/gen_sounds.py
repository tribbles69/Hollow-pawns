"""Placeholder sounds. Run from anywhere; writes into the mod's Sounds/ folder.

HP/EffigyWail.wav: the wailing effigy's scream. HP/RiteStart.wav and HP/SoulRelease.wav: the altar and press. A falling, wavering voice-like tone with breath noise.
Replace with a real recording when there is one.
"""
import math
import os
import random
import struct
import wave

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
RATE = 22050


def wail(seconds=2.6):
    random.seed(7)
    n = int(RATE * seconds)
    out = []
    phase = [0.0, 0.0, 0.0]
    noise = 0.0
    for k in range(n):
        t = k / RATE
        env = min(1.0, t / 0.25) * min(1.0, (seconds - t) / 0.9)
        f = 620 - 260 * (t / seconds) + 18 * math.sin(2 * math.pi * 5.5 * t)
        s = 0.0
        for j, (mult, amp) in enumerate([(1.0, 0.6), (2.01, 0.25), (3.02, 0.12)]):
            phase[j] += 2 * math.pi * f * mult / RATE
            s += amp * math.sin(phase[j])
        noise = 0.85 * noise + 0.15 * random.uniform(-1, 1)
        out.append(env * (0.8 * s + 0.35 * noise))
    peak = max(abs(v) for v in out)
    return [v / peak * 0.85 for v in out]


def drone(seconds=2.2):
    """RiteStart: a low, beating drone that swells in: the rite beginning."""
    n = int(RATE * seconds)
    out = []
    for k in range(n):
        t = k / RATE
        env = min(1.0, t / 0.8) * min(1.0, (seconds - t) / 0.6)
        s = 0.5 * math.sin(2 * math.pi * 82 * t) + 0.35 * math.sin(2 * math.pi * 85.5 * t) + 0.15 * math.sin(2 * math.pi * 164 * t)
        out.append(env * s)
    peak = max(abs(v) for v in out)
    return [v / peak * 0.8 for v in out]


def release(seconds=1.6):
    """SoulRelease: a rising, glassy tone with a breathy tail: the soul coming free."""
    random.seed(11)
    n = int(RATE * seconds)
    out, phase, noise = [], 0.0, 0.0
    for k in range(n):
        t = k / RATE
        env = min(1.0, t / 0.05) * math.exp(-2.2 * t)
        f = 300 + 900 * (t / seconds) ** 0.6
        phase += 2 * math.pi * f / RATE
        noise = 0.9 * noise + 0.1 * random.uniform(-1, 1)
        out.append(env * (0.7 * math.sin(phase) + 0.2 * math.sin(2.003 * phase) + 0.4 * noise))
    peak = max(abs(v) for v in out)
    return [v / peak * 0.85 for v in out]


def write_wav(path, samples):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(v * 32767)) for v in samples))


if __name__ == "__main__":
    write_wav(os.path.join(ROOT, "Sounds", "HP", "EffigyWail.wav"), wail())
    write_wav(os.path.join(ROOT, "Sounds", "HP", "RiteStart.wav"), drone())
    write_wav(os.path.join(ROOT, "Sounds", "HP", "SoulRelease.wav"), release())
