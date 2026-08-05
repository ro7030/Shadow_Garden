#!/usr/bin/env python3
"""Deterministically synthesize Shadow Garden's original WebGL audio set."""

from __future__ import annotations

import math
import shutil
import wave
from pathlib import Path

import numpy as np


RATE = 44_100
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets" / "Game" / "Audio"
SOURCE = ROOT / "Assets" / "Game" / "Art" / "Source" / "Audio"
RNG = np.random.default_rng(7302026)


def timebase(seconds: float) -> np.ndarray:
    return np.arange(int(RATE * seconds), dtype=np.float64) / RATE


def seamless_tone(t: np.ndarray, frequency: float, seconds: float, phase: float = 0.0) -> np.ndarray:
    frequency = round(frequency * seconds) / seconds
    return np.sin(2.0 * math.pi * frequency * t + phase)


def soft_clip(signal: np.ndarray) -> np.ndarray:
    peak = max(1e-6, float(np.max(np.abs(signal))))
    signal = np.tanh(signal * (1.2 / peak))
    return signal / max(1e-6, float(np.max(np.abs(signal)))) * 0.84


def stereo(left: np.ndarray, right: np.ndarray | None = None) -> np.ndarray:
    if right is None:
        right = left
    return np.column_stack((left, right))


def bell(t: np.ndarray, at: float, freq: float, gain: float = 1.0, decay: float = 2.8) -> np.ndarray:
    local = t - at
    active = local >= 0.0
    env = np.where(active, np.exp(-np.maximum(local, 0.0) * decay), 0.0)
    body = np.sin(2 * math.pi * freq * np.maximum(local, 0.0))
    overtone = 0.34 * np.sin(2 * math.pi * freq * 2.01 * np.maximum(local, 0.0) + 0.3)
    air = 0.12 * np.sin(2 * math.pi * freq * 3.98 * np.maximum(local, 0.0) + 1.1)
    return gain * env * (body + overtone + air)


def write_wav(path: Path, data: np.ndarray) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    encoded = np.clip(data, -1.0, 1.0)
    encoded = (encoded * 32767.0).astype("<i2")
    with wave.open(str(path), "wb") as handle:
        handle.setnchannels(2 if encoded.ndim == 2 else 1)
        handle.setsampwidth(2)
        handle.setframerate(RATE)
        handle.writeframes(encoded.tobytes())


def copy_runtime_master(wav_path: Path, runtime_path: Path) -> None:
    """Unity performs the WebGL Vorbis compression from this lossless master."""
    runtime_path.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(wav_path, runtime_path)


def make_music(name: str, root_hz: float, color_hz: float, notes: list[float], seed_phase: float) -> None:
    seconds = 24.0
    t = timebase(seconds)
    breathing = 0.72 + 0.28 * seamless_tone(t, 0.083333, seconds, seed_phase)
    left = 0.18 * seamless_tone(t, root_hz, seconds) + 0.09 * seamless_tone(t, root_hz * 1.5, seconds, 0.6)
    right = 0.18 * seamless_tone(t, root_hz * 1.002, seconds, 0.25) + 0.09 * seamless_tone(t, color_hz, seconds, 1.0)
    left *= breathing
    right *= breathing[::-1]
    for i, ratio in enumerate(notes):
        at = 1.6 + i * (20.0 / max(1, len(notes) - 1))
        chime = bell(t, at, root_hz * ratio * 4.0, 0.11, 2.25)
        if i % 2:
            left += chime * 0.55
            right += chime
        else:
            left += chime
            right += chime * 0.55
    mix = soft_clip(stereo(left, right))
    wav_path = SOURCE / f"{name}.wav"
    write_wav(wav_path, mix)
    copy_runtime_master(wav_path, OUT / "Music" / f"{name}.wav")


def make_ambience(name: str, tones: tuple[float, ...], brightness: float) -> None:
    seconds = 20.0
    t = timebase(seconds)
    left = np.zeros_like(t)
    right = np.zeros_like(t)
    for i, tone in enumerate(tones):
        amp = brightness / (i + 2.2)
        left += amp * seamless_tone(t, tone, seconds, i * 0.7)
        right += amp * seamless_tone(t, tone * 1.003, seconds, i * 0.9 + 0.3)
    swell = 0.55 + 0.45 * seamless_tone(t, 0.1, seconds, 0.2)
    left *= swell
    right *= np.roll(swell, len(swell) // 5)
    mix = soft_clip(stereo(left, right)) * 0.58
    wav_path = SOURCE / f"{name}.wav"
    write_wav(wav_path, mix)
    copy_runtime_master(wav_path, OUT / "Ambience" / f"{name}.wav")


def make_sfx(name: str, seconds: float, builder) -> None:
    t = timebase(seconds)
    mono = builder(t)
    attack = np.minimum(1.0, t / min(0.012, seconds / 4.0))
    release = np.minimum(1.0, np.maximum(0.0, seconds - t) / min(0.045, seconds / 3.0))
    mono = soft_clip(mono * attack * release)
    width = 0.003 * np.sin(2 * math.pi * 2.0 * t)
    mix = stereo(mono * (0.94 + width), mono * (0.94 - width))
    write_wav(OUT / "SFX" / f"{name}.wav", mix)


def noise_burst(t: np.ndarray, decay: float, amount: float = 0.25) -> np.ndarray:
    noise = RNG.normal(0.0, 1.0, len(t))
    smooth = np.convolve(noise, np.ones(15) / 15.0, mode="same")
    return smooth * np.exp(-t * decay) * amount


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    SOURCE.mkdir(parents=True, exist_ok=True)

    make_music("BGM_CommonMotif", 65.406, 98.0, [1.0, 1.25, 1.5, 2.0, 1.5, 1.25], 0.1)
    make_music("BGM_OrchardLayer", 73.416, 110.0, [1.0, 1.2, 1.5, 1.8, 2.0, 1.5], 0.4)
    make_music("BGM_CanyonLayer", 55.0, 82.407, [1.0, 1.333, 1.5, 1.777, 2.0, 1.333], 1.2)
    make_music("BGM_GreenhouseLayer", 61.735, 92.499, [1.0, 1.25, 1.6, 2.0, 2.5, 1.6], 2.0)

    make_ambience("AMB_Orchard", (0.2, 0.35, 247.0, 330.0), 0.13)
    make_ambience("AMB_Canyon", (0.15, 0.3, 196.0, 293.0), 0.16)
    make_ambience("AMB_Greenhouse", (0.1, 0.25, 220.0, 370.0, 554.0), 0.12)

    make_sfx("SFX_Move", 0.16, lambda t: noise_burst(t, 22.0, 0.6) + 0.15 * np.sin(2 * math.pi * 92 * t) * np.exp(-t * 18))
    make_sfx("SFX_Rotate", 0.18, lambda t: 0.32 * np.sin(2 * math.pi * (420 + 520 * t) * t) + bell(t, 0.05, 740, 0.35, 13))
    make_sfx("SFX_ShadowCell", 0.14, lambda t: bell(t, 0.0, 520, 0.5, 16))
    make_sfx("SFX_Warning30", 0.42, lambda t: bell(t, 0.0, 392, 0.7, 6) + bell(t, 0.16, 494, 0.5, 7))
    make_sfx("SFX_Warning10", 0.18, lambda t: bell(t, 0.0, 660, 0.8, 12))
    make_sfx("SFX_Blocked", 0.18, lambda t: 0.5 * np.sin(2 * math.pi * 105 * t) * np.exp(-t * 18) + noise_burst(t, 18, 0.22))
    make_sfx("SFX_OverlapDeath", 0.55, lambda t: 0.38 * np.sin(2 * math.pi * (130 - 70 * t) * t) * np.exp(-t * 2.2) + noise_burst(t, 3.5, 0.28))
    make_sfx("SFX_CliffDeath", 0.85, lambda t: 0.28 * np.sin(2 * math.pi * (230 - 190 * t) * t) * np.exp(-t * 1.6) + noise_burst(t, 2.6, 0.32))
    make_sfx("SFX_TimeDeath", 0.65, lambda t: 0.32 * np.sin(2 * math.pi * (180 + 520 * t) * t) * (1 - t / 0.65) + noise_burst(t, 2.0, 0.24))
    make_sfx("SFX_DoorOpen", 0.45, lambda t: 0.18 * np.sin(2 * math.pi * 84 * t) + bell(t, 0.12, 330, 0.55, 6))
    make_sfx("SFX_DoorPass", 0.35, lambda t: 0.22 * np.sin(2 * math.pi * (240 + 280 * t) * t) * np.exp(-t * 3) + noise_burst(t, 8, 0.16))
    make_sfx("SFX_FlowerBloom", 1.5, lambda t: bell(t, 0.0, 392, 0.32, 2.8) + bell(t, 0.28, 494, 0.34, 2.6) + bell(t, 0.62, 659, 0.4, 2.4))
    make_sfx("SFX_Complete", 1.1, lambda t: bell(t, 0.0, 523, 0.35, 3.2) + bell(t, 0.22, 659, 0.36, 3.0) + bell(t, 0.46, 784, 0.42, 2.8))
    make_sfx("SFX_UiMove", 0.09, lambda t: bell(t, 0.0, 610, 0.36, 24))
    make_sfx("SFX_UiSubmit", 0.15, lambda t: bell(t, 0.0, 720, 0.42, 17) + bell(t, 0.05, 910, 0.22, 18))

    print(f"Generated original audio in {OUT}")


if __name__ == "__main__":
    main()
